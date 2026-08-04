using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Voltis.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Id do usuário autenticado, lido do claim `sub` do JWT.
    ///
    /// Este é o ÚNICO lugar de onde a identidade do usuário deve sair. Aceitar
    /// o id por rota, query ou corpo permitiria a um usuário logado operar
    /// sobre os dados de outro só trocando o Guid (IDOR).
    /// </summary>
    public static Guid ObterUsuarioId(this ClaimsPrincipal principal)
    {
        // Funciona porque Program.cs desliga o MapInboundClaims. Com ele ligado
        // (padrão do .NET), `sub` seria renomeado para ClaimTypes.NameIdentifier
        // e a busca por "sub" voltaria null.
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        // Só chega aqui com token já validado pelo pipeline. Se o `sub` não for
        // um Guid, quem gerou o token errou — é bug nosso, e vira 500.
        if (!Guid.TryParse(sub, out var usuarioId))
            throw new InvalidOperationException(
                "Token autenticado sem claim 'sub' válido.");

        return usuarioId;
    }
}
