namespace Voltis.Api.DTOs;

public class ContaResponse
{
    public long ContaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}
