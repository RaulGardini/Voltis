using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Voltis.Api.Erros;
using Voltis.Domain.Services;
using Voltis.Infrastructure.Persistence;
using Voltis.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// --- Banco (já existia) ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// --- Serviços de segurança (novos) ---
// AddScoped: uma instância por requisição HTTP. É o tempo de vida certo
// para serviços que podem tocar no banco.
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();

// --- Autenticação JWT (nova) ---
var chaveJwt = builder.Configuration["Jwt:Chave"]
    ?? throw new InvalidOperationException(
        "Configuração 'Jwt:Chave' ausente. Defina via user-secrets ou variável de ambiente.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Por padrão o .NET renomeia os claims do JWT para URLs longas do
        // WS-Federation (`sub` vira ClaimTypes.NameIdentifier). Desligar isso
        // mantém os nomes exatamente como o TokenService os escreveu, e evita
        // a confusão de procurar por "sub" e receber null.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Cada um destes "true" é uma verificação que o servidor faz
            // em todo token recebido. Desligar qualquer um abre um buraco.
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,          // rejeita token expirado
            ValidateIssuerSigningKey = true,  // confere a assinatura

            ValidIssuer = builder.Configuration["Jwt:Emissor"],
            ValidAudience = builder.Configuration["Jwt:Audiencia"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(chaveJwt)),

            // Por padrão o .NET tolera 5 min de folga na expiração.
            // Zero deixa a expiração exata.
            ClockSkew = TimeSpan.Zero
        };
    });

// --- Autorização: fechado por padrão ---
// A FallbackPolicy vale para todo endpoint que NÃO declare [Authorize] nem
// [AllowAnonymous]. Ou seja: o padrão passa a ser "exige token", e um
// controller novo nasce protegido. Sem ela, esquecer o atributo = endpoint
// aberto na internet, que é exatamente o tipo de erro que não dá aviso.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// --- CORS (novo) ---
// O browser bloqueia chamadas do front (outra origem) para a API se ela
// não devolver os cabeçalhos CORS. Origens vêm da configuração e NUNCA de
// AllowAnyOrigin: em produção isso deixaria qualquer site chamar a API
// com o token do usuário.
const string PoliticaCorsFrontend = "frontend";

var origensPermitidas = builder.Configuration
    .GetSection("Cors:OrigensPermitidas")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(PoliticaCorsFrontend, policy =>
    {
        policy.WithOrigins(origensPermitidas)
              .AllowAnyHeader()
              .AllowAnyMethod();
        // Sem AllowCredentials de propósito: o token vai no cabeçalho
        // Authorization, não em cookie. Se um dia migrar para cookie
        // httpOnly, aí sim precisa (e junto vem proteção CSRF).
    });
});

// --- Rate limiting nos endpoints de autenticação ---
// Duas ameaças de uma vez:
//   1. Força bruta de senha — sem limite, dá para testar milhares por minuto.
//   2. DoS — o BCrypt custa ~250ms de CPU por tentativa DE PROPÓSITO. Essa
//      lentidão protege contra quebra offline, mas viraria uma arma contra a
//      própria API se qualquer um pudesse dispará-la à vontade.
const string PoliticaRateLimitAuth = "auth";

builder.Services.AddRateLimiter(options =>
{
    // Particiona por IP: um atacante consome a cota dele, não a dos outros.
    // ATENÇÃO ao publicar atrás de proxy/load balancer — lá o RemoteIpAddress
    // vira o IP do proxy e TODO mundo cai na mesma partição. Nesse cenário é
    // preciso configurar o ForwardedHeadersMiddleware antes daqui.
    options.AddPolicy(PoliticaRateLimitAuth, http =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                // Fila zero: excedeu, recusa na hora. Enfileirar só seguraria
                // conexão aberta e ajudaria o atacante a consumir recurso.
                QueueLimit = 0
            }));

    // Sem isto o 429 volta com corpo vazio e o front cai na mensagem genérica.
    options.OnRejected = async (contexto, cancellationToken) =>
    {
        contexto.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        await contexto.HttpContext.RequestServices
            .GetRequiredService<IProblemDetailsService>()
            .TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = contexto.HttpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Muitas tentativas.",
                    Detail = "Você fez muitas tentativas seguidas. "
                           + "Aguarde um minuto e tente novamente."
                }
            });
    };
});

// --- Tratamento de erro global ---
// AddProblemDetails registra o serviço que formata a resposta no padrão
// RFC 7807, o mesmo que o ASP.NET já usa nos erros de validação.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorGlobalDeExcecoes>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Primeiro middleware do pipeline: só captura o que acontece DEPOIS dele,
// então precisa envolver todo o resto.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS antes de Authentication: o preflight (OPTIONS) chega sem token e
// precisa ser respondido antes de qualquer checagem de identidade.
app.UseCors(PoliticaCorsFrontend);

// Depois do CORS: assim a resposta 429 também sai com os cabeçalhos CORS e
// o browser deixa o front ler a mensagem, em vez de acusar erro de origem.
app.UseRateLimiter();

// A ORDEM aqui importa e é fonte clássica de bug:
// Authentication (quem é você?) SEMPRE antes de Authorization (você pode?).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
