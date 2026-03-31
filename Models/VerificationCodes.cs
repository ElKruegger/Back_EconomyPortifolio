using EconomyBackPortifolio.Enums;

namespace EconomyBackPortifolio.Models
{
    /// <summary>
    /// Representa um código de verificação OTP (One-Time Password) de 6 dígitos
    /// enviado por e-mail para autenticação de dois fatores (2FA).
    ///
    /// Tabela: verification_codes
    /// Índices: PK (id), FK (user_id → users.id), INDEX (user_id, type, is_used)
    ///
    /// Fluxo de uso:
    ///   1. Sistema gera código criptograficamente seguro (RandomNumberGenerator).
    ///   2. Códigos anteriores não usados do mesmo tipo/usuário são invalidados (IsUsed=true).
    ///   3. Código é enviado por e-mail e armazenado com ExpiresAt = UtcNow + 10 min.
    ///   4. Usuário submete o código → ValidateCodeAsync verifica IsUsed=false e ExpiresAt>UtcNow.
    ///   5. Após validação bem-sucedida, IsUsed=true (uso único garantido).
    ///
    /// Tipos suportados (VerificationCodeType):
    ///   - Registration  — confirmação de e-mail no cadastro.
    ///   - Login         — segundo fator no login.
    ///   - PasswordReset — autorização para redefinição de senha.
    ///
    /// Notas sobre o banco:
    ///   - Código de 6 dígitos ("D6") — máx 999999, armazenado como string com zero-padding.
    ///   - O índice (user_id, type, is_used) acelera as queries de invalidação e validação.
    ///   - Registros expirados/usados podem ser limpos por job periódico (não implementado).
    /// </summary>
    public class VerificationCodes
    {
        /// <summary>Identificador único do código (UUID v4).</summary>
        public Guid Id { get; set; }

        /// <summary>ID do usuário dono deste código. FK para users.id.</summary>
        public Guid UserId { get; set; }

        /// <summary>Código OTP de 6 dígitos (ex: "042731"). Máx 6 caracteres.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tipo da operação que gerou este código (Registration, Login, PasswordReset).</summary>
        public VerificationCodeType Type { get; set; }

        /// <summary>Timestamp UTC de expiração. Códigos acessados após esta data são inválidos.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Indica se o código já foi utilizado.
        /// true = usado ou invalidado; false = disponível para uso.
        /// </summary>
        public bool IsUsed { get; set; }

        /// <summary>Timestamp UTC de criação do código.</summary>
        public DateTime CreatedAt { get; set; }

        // ─── Navigation Properties ────────────────────────────────────────────
        /// <summary>Usuário dono deste código. Non-nullable: todo código pertence a um usuário.</summary>
        public Users User { get; set; } = null!;
    }
}
