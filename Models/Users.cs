using EconomyBackPortifolio.Enums;

namespace EconomyBackPortifolio.Models
{
    /// <summary>
    /// Representa um usuário cadastrado na plataforma Economy Portfolio.
    ///
    /// Tabela: users
    /// Índices: PK (id), UNIQUE (email)
    ///
    /// Relacionamentos:
    ///   - 1 usuário → N wallets (via Wallets.UserId)
    ///   - 1 usuário → N verification_codes (via VerificationCodes.UserId)
    ///   - 1 usuário → N financial_entries (via FinancialEntries.UserId)
    ///   - 1 usuário → N categories (via Categories.UserId)
    ///   - 1 usuário (Contador) → N clients (via Clients.AccountantUserId)
    ///
    /// Perfis de usuário (ProfileType):
    ///   - PessoaFisica: controle financeiro pessoal
    ///   - Empresa: controle financeiro empresarial
    ///   - Contador: gestão multi-tenant de clientes
    ///
    /// Planos de assinatura (PlanType):
    ///   - Basic: funcionalidades essenciais (gratuito)
    ///   - Pro: acesso completo (pago)
    ///
    /// Notas sobre o banco:
    ///   - O e-mail é sempre armazenado em lowercase para evitar duplicatas case-insensitive.
    ///   - PasswordHash usa BCrypt (fator 10) — nunca armazene senha em texto puro.
    ///   - EmailVerified=false até o usuário completar o fluxo de verificação via 2FA.
    /// </summary>
    public class Users
    {
        /// <summary>Identificador único do usuário (UUID v4).</summary>
        public Guid Id { get; set; }

        /// <summary>Nome de exibição do usuário. Máx 100 caracteres.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>E-mail do usuário (sempre lowercase). Máx 150 caracteres. Único no banco.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Hash BCrypt da senha. Nunca exposto em DTOs ou logs.</summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Indica se o e-mail foi verificado via código 2FA.
        /// false = conta criada mas não confirmada; true = login liberado.
        /// </summary>
        public bool EmailVerified { get; set; }

        /// <summary>
        /// Perfil do usuário na plataforma.
        /// Define quais funcionalidades estarão disponíveis na interface.
        /// Padrão: PessoaFisica.
        /// </summary>
        public ProfileType ProfileType { get; set; } = ProfileType.PessoaFisica;

        /// <summary>
        /// Plano de assinatura atual do usuário.
        /// Padrão: Basic (conta gratuita ao registrar).
        /// </summary>
        public PlanType PlanType { get; set; } = PlanType.Basic;

        /// <summary>
        /// Nome da empresa ou razão social. Preenchido apenas para o perfil Empresa.
        /// Máx 200 caracteres. Nullable.
        /// </summary>
        public string? CompanyName { get; set; }

        /// <summary>
        /// Data de expiração do plano Pro.
        /// Null = plano Basic (sem expiração) ou plano Pro vitalício.
        /// Quando definida, o sistema deve regredir para Basic ao vencer.
        /// </summary>
        public DateTime? PlanExpiresAt { get; set; }

        /// <summary>Timestamp UTC de criação da conta.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
