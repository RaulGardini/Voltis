using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Voltis.Domain.Entities;
using Voltis.Domain.Services;

namespace Voltis.Infrastructure.Security;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GerarToken(Usuario usuario)
    {
        var chave = _config["Jwt:Chave"]!;
        var emissor = _config["Jwt:Emissor"]!;
        var audiencia = _config["Jwt:Audiencia"]!;

        // A chave secreta assina o token. Com ela, o servidor consegue
        // depois verificar que o token não foi adulterado. Quem não tem
        // a chave não consegue forjar um token válido.
        var credenciais = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
            SecurityAlgorithms.HmacSha256);

        // Claims = as afirmações dentro do token. Coloco só o necessário
        // para identificar o usuário. NUNCA a senha ou dados sensíveis,
        // porque o conteúdo do JWT é legível por qualquer um.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("nome", usuario.Nome),
            // jti: id único do token. Útil se um dia você quiser
            // invalidar tokens específicos (logout server-side).
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: emissor,
            audience: audiencia,
            claims: claims,
            // Expiração curta. Se um token vazar, ele só vale por 2h.
            // Para sessão longa o padrão é usar refresh token (fica para depois).
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}