using System.ComponentModel.DataAnnotations;

namespace Voltis.Api.DTOs;

public class AtualizarConfiguracaoUsuarioRequest
{
    [Required]
    [Range(1, 31, ErrorMessage = "O dia de fechamento do mês deve estar entre 1 e 31.")]
    public short DiaFechamentoMes { get; set; }

    [Required(ErrorMessage = "Moeda é obrigatória.")]
    public string Moeda { get; set; } = string.Empty;
}
