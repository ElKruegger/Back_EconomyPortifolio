using EconomyBackPortifolio.Enums;

namespace EconomyBackPortifolio.DTOs
{
    /// <summary>
    /// DTO de resposta retornado após autenticação bem-sucedida (verify-code).
    /// Contém o JWT, o refresh token, a expiração e os dados públicos do usuário.
    /// </summary>
    public class AuthResponseDto
    {
        /// <summary>Token JWT para autenticação nas requisições subsequentes.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token para renovação do JWT expirado.
        /// TODO: implementar endpoint /auth/refresh-token com persistência no banco.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>Timestamp UTC de expiração do Token JWT.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Dados públicos do usuário autenticado.</summary>
        public UserInfoDto User { get; set; } = null!;
    }

    /// <summary>
    /// DTO com as informações públicas do usuário.
    /// Retornado em GET /auth/me e dentro do AuthResponseDto.
    /// Nunca inclui PasswordHash, códigos de verificação ou dados sensíveis.
    /// </summary>
    public class UserInfoDto
    {
        /// <summary>Identificador único do usuário (UUID v4).</summary>
        public Guid Id { get; set; }

        /// <summary>Nome de exibição do usuário.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>E-mail do usuário (sempre em lowercase).</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Perfil do usuário na plataforma.
        /// Determina funcionalidades disponíveis: PessoaFisica, Empresa ou Contador.
        /// </summary>
        public ProfileType ProfileType { get; set; }

        /// <summary>
        /// Plano de assinatura atual: Basic (gratuito) ou Pro (pago).
        /// </summary>
        public PlanType PlanType { get; set; }

        /// <summary>
        /// Nome da empresa. Preenchido apenas para o perfil Empresa.
        /// </summary>
        public string? CompanyName { get; set; }

        /// <summary>
        /// Data de expiração do plano Pro. Null para plano Basic.
        /// </summary>
        public DateTime? PlanExpiresAt { get; set; }

        /// <summary>Timestamp UTC de criação da conta.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
