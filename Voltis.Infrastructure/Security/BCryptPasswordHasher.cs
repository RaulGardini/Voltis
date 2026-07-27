using Voltis.Domain.Services;

namespace Voltis.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    // workFactor 12: número de rounds do BCrypt. Cada +1 dobra o tempo
    // de cálculo. 12 é um bom equilíbrio hoje entre segurança e o login
    // não ficar lento demais. Quanto mais alto, mais caro fica para um
    // atacante testar senhas em massa.
    private const int WorkFactor = 12;

    public string Hash(string senha) =>
        BCrypt.Net.BCrypt.HashPassword(senha, WorkFactor);

    public bool Verificar(string senha, string hash) =>
        BCrypt.Net.BCrypt.Verify(senha, hash);
}