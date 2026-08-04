using System.ComponentModel.DataAnnotations;
using Voltis.Domain.Entities;

namespace Voltis.Api.DTOs;

public class AtualizarContaRequest
{
    [Required(ErrorMessage = "O nome da conta é obrigatório.")]
    [MaxLength(Conta.NomeTamanhoMaximo,
        ErrorMessage = "O nome da conta deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;
}
