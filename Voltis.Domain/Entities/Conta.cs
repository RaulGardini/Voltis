namespace Voltis.Domain.Entities;

public class Conta
{
    public const int NomeTamanhoMaximo = 100;

    public long ContaId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Nome { get; private set; }

    /// <summary>
    /// Preenchido pelo banco (DEFAULT CURRENT_TIMESTAMP), nunca pelo código.
    /// Define qual é a conta "mais antiga", então precisa de uma fonte única
    /// de tempo — o relógio do servidor de aplicação poderia divergir.
    /// </summary>
    public DateTime CriadoEm { get; private set; }

    // Construtor privado sem parâmetros: o EF Core precisa dele para
    // reconstruir o objeto ao ler do banco.
    private Conta() { }

    public Conta(Guid usuarioId, string nome)
    {
        UsuarioId = usuarioId;
        Nome = NormalizarNome(nome);
    }

    public void Renomear(string nome)
    {
        Nome = NormalizarNome(nome);
    }

    private static string NormalizarNome(string nome)
    {
        var normalizado = nome?.Trim() ?? string.Empty;

        // O [Required] do DTO deixa passar string só de espaços — aqui não passa.
        if (normalizado.Length == 0)
            throw new ArgumentException(
                "O nome da conta é obrigatório.",
                nameof(nome));

        if (normalizado.Length > NomeTamanhoMaximo)
            throw new ArgumentException(
                $"O nome da conta deve ter no máximo {NomeTamanhoMaximo} caracteres.",
                nameof(nome));

        return normalizado;
    }
}
