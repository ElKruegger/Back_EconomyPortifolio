namespace EconomyBackPortifolio.Models
{
    /// <summary>
    /// Representa um cliente gerenciado por um Contador na plataforma Economy Portfolio.
    ///
    /// Tabela: clients
    /// Índices: PK (id), IX (accountant_user_id), IX (accountant_user_id, is_active)
    ///
    /// Relacionamentos:
    ///   - N clients → 1 usuário contador (via AccountantUserId)
    ///   - 1 client → N financial_entries (via FinancialEntries.ClientId)
    ///
    /// O modelo Client implementa o conceito de multi-tenancy para contadores:
    /// cada cliente tem seu espaço isolado de lançamentos financeiros dentro
    /// do contexto do contador responsável.
    ///
    /// Regras de negócio:
    ///   - Apenas usuários com ProfileType=Contador podem criar clients.
    ///   - Um lançamento financeiro pertence a um cliente OU diretamente ao usuário, nunca ambos.
    ///   - No plano Basic, o contador pode ter até 3 clientes ativos.
    ///   - No plano Pro, o contador pode ter clientes ilimitados.
    /// </summary>
    public class Client
    {
        /// <summary>Identificador único do cliente (UUID v4).</summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID do usuário Contador responsável por este cliente.
        /// FK para a tabela users (perfil Contador).
        /// </summary>
        public Guid AccountantUserId { get; set; }

        /// <summary>
        /// Nome completo ou razão social do cliente. Máx 150 caracteres.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// E-mail de contato do cliente. Máx 150 caracteres. Nullable.
        /// Não precisa ser um e-mail cadastrado na plataforma.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Telefone de contato do cliente. Máx 20 caracteres. Nullable.
        /// Armazenado sem formatação (apenas dígitos).
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// CPF ou CNPJ do cliente. Máx 20 caracteres. Nullable.
        /// Armazenado apenas dígitos, sem pontuação.
        /// </summary>
        public string? Document { get; set; }

        /// <summary>
        /// Notas internas do contador sobre o cliente. Máx 1000 caracteres. Nullable.
        /// Não visível para o próprio cliente.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Indica se o cliente está ativo no escritório do contador.
        /// false = cliente arquivado (não aparece na listagem padrão, mas os lançamentos são mantidos).
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Timestamp UTC de criação do registro do cliente.</summary>
        public DateTime CreatedAt { get; set; }

        // ─────────────────────────────────────────────────────────────────────
        // NAVIGATION PROPERTIES
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Usuário Contador responsável por este cliente.</summary>
        public Users Accountant { get; set; } = null!;

        /// <summary>Lançamentos financeiros associados a este cliente.</summary>
        public ICollection<FinancialEntry> FinancialEntries { get; set; } = new List<FinancialEntry>();
    }
}
