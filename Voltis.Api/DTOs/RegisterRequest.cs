using System.ComponentModel.DataAnnotations;

namespace Voltis.Api.DTOs;

public class RegistrarRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "A senha deve ter ao menos 8 caracteres.")]
    public string Senha { get; set; } = string.Empty;
}