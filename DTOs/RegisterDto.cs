using System.ComponentModel.DataAnnotations;
using EconomyBackPortifolio.Enums;

namespace EconomyBackPortifolio.DTOs
{
    /// <summary>
    /// DTO de entrada para o registro de novos usuários.
    ///
    /// O perfil (ProfileType) define a experiência que o usuário terá na plataforma:
    ///   - PessoaFisica (0): controle financeiro pessoal
    ///   - Empresa (1): controle financeiro empresarial
    ///   - Contador (2): gestão multi-tenant de clientes
    ///
    /// CompanyName é obrigatório apenas quando ProfileType=Empresa.
    /// </summary>
    public class RegisterDto
    {
        /// <summary>Nome completo ou de exibição do usuário. Entre 2 e 100 caracteres.</summary>
        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        /// <summary>E-mail do usuário. Usado como identificador único de acesso.</summary>
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(150, ErrorMessage = "O email deve ter no máximo 150 caracteres")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Senha de acesso. Entre 8 e 100 caracteres.
        /// Deve conter ao menos uma letra maiúscula, uma minúscula e um número.
        /// </summary>
        [Required(ErrorMessage = "A senha é obrigatória")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 100 caracteres")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$",
            ErrorMessage = "A senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Perfil do usuário na plataforma. Padrão: PessoaFisica (0).
        /// Valores aceitos: 0 = PessoaFisica, 1 = Empresa, 2 = Contador.
        /// </summary>
        public ProfileType ProfileType { get; set; } = ProfileType.PessoaFisica;

        /// <summary>
        /// Razão social ou nome da empresa. Obrigatório quando ProfileType=Empresa.
        /// Máx 200 caracteres. Ignorado para outros perfis.
        /// </summary>
        [StringLength(200, ErrorMessage = "O nome da empresa deve ter no máximo 200 caracteres")]
        public string? CompanyName { get; set; }
    }
}
