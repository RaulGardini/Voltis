namespace Voltis.Domain.Entities;

public class ConfiguracaoUsuario
{
    public static readonly string[] MoedasPermitidas = ["BRL", "USD", "EUR", "JPY"];

    public long ConfiguracaoId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public short DiaFechamentoMes { get; private set; }
    public string Moeda { get; private set; }

    // Construtor privado sem parâmetros: o EF Core precisa dele para
    // reconstruir o objeto ao ler do banco.
    private ConfiguracaoUsuario() { }

    public ConfiguracaoUsuario(Guid usuarioId)
    {
        UsuarioId = usuarioId;
        DiaFechamentoMes = 1;
        Moeda = "BRL";
    }

    public void Atualizar(short diaFechamentoMes, string moeda)
    {
        if (diaFechamentoMes is < 1 or > 31)
            throw new ArgumentOutOfRangeException(
                nameof(diaFechamentoMes),
                "O dia de fechamento do mês deve estar entre 1 e 31.");

        var moedaNormalizada = moeda.Trim().ToUpperInvariant();
        if (!MoedasPermitidas.Contains(moedaNormalizada))
            throw new ArgumentException(
                $"Moeda inválida. Valores aceitos: {string.Join(", ", MoedasPermitidas)}.",
                nameof(moeda));

        DiaFechamentoMes = diaFechamentoMes;
        Moeda = moedaNormalizada;
    }
}
