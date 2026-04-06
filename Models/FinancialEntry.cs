using EconomyBackPortifolio.Enums;

namespace EconomyBackPortifolio.Models
{
    /// <summary>
    /// Representa um lançamento financeiro (receita ou despesa) no Economy Portfolio.
    ///
    /// Tabela: financial_entries
    /// Índices:
    ///   - PK (id)
    ///   - IX (user_id, entry_date) — filtros por período para o usuário
    ///   - IX (user_id, type) — filtros por tipo (receita/despesa)
    ///   - IX (client_id) — isolamento de dados por cliente (multi-tenant contador)
    ///   - IX (category_id) — relatórios por categoria
    ///
    /// Relacionamentos:
    ///   - N financial_entries → 1 usuário (via UserId)
    ///   - N financial_entries → 1 categoria (via CategoryId)
    ///   - N financial_entries → 0 ou 1 cliente (via ClientId, apenas para Contador)
    ///
    /// Lógica de propriedade:
    ///   - UserId sempre preenchido (dono do registro).
    ///   - ClientId preenchido apenas para Contadores gerenciando um cliente.
    ///   - Um lançamento pertence a um cliente OU ao próprio usuário, nunca ambos.
    ///
    /// Imutabilidade:
    ///   - Ao contrário de Transactions (investimentos), FinancialEntry pode ser editado.
    ///   - O campo UpdatedAt rastreia a última modificação para auditoria.
    /// </summary>
    public class FinancialEntry
    {
        /// <summary>Identificador único do lançamento (UUID v4).</summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID do usuário proprietário do lançamento.
        /// Para Contadores, é o ID do próprio Contador, não do cliente.
        /// FK para a tabela users.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// ID do cliente ao qual este lançamento pertence.
        /// Preenchido apenas para lançamentos gerenciados por Contadores.
        /// Null = lançamento do próprio usuário (PessoaFisica ou Empresa).
        /// FK para a tabela clients.
        /// </summary>
        public Guid? ClientId { get; set; }

        /// <summary>
        /// ID da categoria do lançamento.
        /// Pode ser categoria do sistema (IsSystem=true) ou do usuário (plano Pro).
        /// FK para a tabela categories.
        /// </summary>
        public Guid CategoryId { get; set; }

        /// <summary>
        /// Tipo do lançamento: Receita ou Despesa.
        /// Determina se o valor entra ou sai do saldo do período.
        /// </summary>
        public EntryType Type { get; set; }

        /// <summary>
        /// Valor monetário do lançamento. Precisão (18,2).
        /// Sempre positivo — o tipo (Receita/Despesa) define o sinal no relatório.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Descrição do lançamento. Máx 500 caracteres.
        /// Ex: "Pagamento de aluguel", "Salário mensal", "Conta de energia".
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Data efetiva do lançamento (quando ocorreu de fato).
        /// Pode diferir da data de criação (CreatedAt) — útil para lançamentos retroativos.
        /// </summary>
        public DateTime EntryDate { get; set; }

        /// <summary>
        /// Indica se este lançamento é recorrente.
        /// true = se repete em intervalos regulares (ex: aluguel mensal).
        /// O sistema não cria os lançamentos futuros automaticamente (MVP).
        /// </summary>
        public bool IsRecurring { get; set; }

        /// <summary>
        /// Intervalo de recorrência. Nullable, relevante apenas quando IsRecurring=true.
        /// Valores esperados: "Daily", "Weekly", "Monthly", "Yearly".
        /// Máx 20 caracteres.
        /// </summary>
        public string? RecurrenceInterval { get; set; }

        /// <summary>
        /// Notas adicionais sobre o lançamento. Máx 1000 caracteres. Nullable.
        /// Campo livre para anotações internas.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>Timestamp UTC de criação do lançamento.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp UTC da última atualização.
        /// Null quando nunca editado desde a criação.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // ─────────────────────────────────────────────────────────────────────
        // NAVIGATION PROPERTIES
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Usuário proprietário do lançamento.</summary>
        public Users User { get; set; } = null!;

        /// <summary>Categoria do lançamento.</summary>
        public Category Category { get; set; } = null!;

        /// <summary>Cliente associado ao lançamento. Null para lançamentos pessoais/empresariais.</summary>
        public Client? Client { get; set; }
    }
}
