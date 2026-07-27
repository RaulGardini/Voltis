namespace Voltis.Domain.Entities;

public class Usuario
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string Email { get; private set; }
    public string SenhaHash { get; private set; }
    public DateTime CriadoEm { get; private set; }

    // Construtor privado sem parâmetros: o EF Core precisa dele para
    // reconstruir o objeto ao ler do banco. Fica privado para que o
    // resto do código não consiga criar um Usuario "pela metade".
    private Usuario() { }

    public Usuario(string nome, string email, string senhaHash)
    {
        Id = Guid.NewGuid();
        Nome = nome;
        Email = email;
        SenhaHash = senhaHash;
        CriadoEm = DateTime.UtcNow;
    }
}