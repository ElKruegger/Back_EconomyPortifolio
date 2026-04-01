using System.ComponentModel.DataAnnotations;

namespace EconomyBackPortifolio.DTOs
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; } = string.Empty;
    }
}
