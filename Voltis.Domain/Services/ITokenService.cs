using Voltis.Domain.Entities;

namespace Voltis.Domain.Services;

public interface ITokenService
{
    string GerarToken(Usuario usuario);
}