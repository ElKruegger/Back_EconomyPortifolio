using EconomyBackPortifolio.Enums;

namespace EconomyBackPortifolio.Models
{
    /// <summary>
    /// Representa uma categoria de lançamento financeiro no Economy Portfolio.
    ///
    /// Tabela: categories
    /// Índices: PK (id), IX (user_id), IX (type, is_system)
    ///
    /// Relacionamentos:
    ///   - N categorias → 1 usuário (via UserId, nullable para categorias do sistema)
    ///   - 1 categoria → N financial_entries (via FinancialEntries.CategoryId)
    ///
    /// Existem dois tipos de categorias:
    ///   1. Categorias do sistema (IsSystem=true, UserId=null): pré-cadastradas pela plataforma,
    ///      disponíveis para todos os usuários, não editáveis nem removíveis.
    ///   2. Categorias do usuário (IsSystem=false, UserId=<guid>): criadas pelo próprio usuário,
    ///      disponíveis apenas no plano Pro.
    ///
    /// Categorias do sistema padrão (seed):
    ///   Receita: Salário, Freelance, Investimentos, Outros
    ///   Despesa: Alimentação, Moradia, Transporte, Saúde, Educação, Lazer, Outros
    ///   Ambas: Transferência
    /// </summary>
    public class Category
    {
        /// <summary>Identificador único da categoria (UUID v4).</summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID do usuário proprietário desta categoria.
        /// Null para categorias do sistema (IsSystem=true).
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Nome da categoria. Máx 100 caracteres.
        /// Ex: "Alimentação", "Salário", "Aluguel".
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de lançamento ao qual esta categoria se aplica.
        /// Receita, Despesa ou Ambas.
        /// </summary>
        public CategoryType Type { get; set; }

        /// <summary>
        /// Ícone representando a categoria. Pode ser um emoji ou nome de ícone (ex: "home", "💰").
        /// Máx 50 caracteres. Nullable.
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Cor associada à categoria em formato hexadecimal (ex: "#4CAF50").
        /// Usada para visualização em gráficos e listas.
        /// Máx 7 caracteres. Nullable.
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Indica se esta é uma categoria padrão do sistema (não editável pelo usuário).
        /// true = categoria da plataforma; false = categoria criada pelo usuário.
        /// </summary>
        public bool IsSystem { get; set; }

        /// <summary>
        /// Indica se a categoria está ativa. Categorias inativas não aparecem nas opções de lançamento.
        /// Permite "deletar" categorias sem remover registros históricos.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Timestamp UTC de criação da categoria.</summary>
        public DateTime CreatedAt { get; set; }

        // ─────────────────────────────────────────────────────────────────────
        // NAVIGATION PROPERTIES
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Usuário proprietário da categoria. Null para categorias do sistema.</summary>
        public Users? User { get; set; }

        /// <summary>Lançamentos financeiros associados a esta categoria.</summary>
        public ICollection<FinancialEntry> FinancialEntries { get; set; } = new List<FinancialEntry>();
    }
}
