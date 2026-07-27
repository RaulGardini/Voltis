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
var chaveJwt = builder.Configuration["Jwt:Chave"]!;

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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// A ORDEM aqui importa e é fonte clássica de bug:
// Authentication (quem é você?) SEMPRE antes de Authorization (você pode?).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();