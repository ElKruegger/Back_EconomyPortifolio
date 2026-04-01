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
    ///
    /// Notas sobre o banco:
    ///   - O e-mail é sempre armazenado em lowercase para evitar duplicatas case-insensitive.
    ///   - PasswordHash usa BCrypt (fator 10) — nunca armazene senha em texto puro.
    ///   - EmailVerified=false até o usuário completar o fluxo de verificação via 2FA.
    ///   - Convenção de nomenclatura: a classe está no plural (Users) por decisão inicial do projeto;
    ///     recomenda-se renomear para User em refactoring futuro com migration.
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

        /// <summary>Timestamp UTC de criação da conta.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
