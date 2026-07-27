using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

builder.Services.AddAuthorization();

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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS antes de Authentication: o preflight (OPTIONS) chega sem token e
// precisa ser respondido antes de qualquer checagem de identidade.
app.UseCors(PoliticaCorsFrontend);

// A ORDEM aqui importa e é fonte clássica de bug:
// Authentication (quem é você?) SEMPRE antes de Authorization (você pode?).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();