using System.ComponentModel.DataAnnotations;
using EconomyBackPortifolio.Enums;

namespace EconomyBackPortifolio.DTOs
{
    public class VerifyCodeDto
    {
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O código é obrigatório")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "O código deve ter 6 dígitos")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "O código deve conter apenas números")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "O tipo de verificação é obrigatório")]
        public VerificationCodeType Type { get; set; }
    }
}
