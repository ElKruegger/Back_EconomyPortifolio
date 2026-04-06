using System.ComponentModel.DataAnnotations;
using EconomyBackPortifolio.Enums;

namespace EconomyBackPortifolio.DTOs
{
    /// <summary>
    /// DTO de saída representando um lançamento financeiro (receita ou despesa).
    /// Retornado nos endpoints GET /financial-entries e GET /financial-entries/{id}.
    /// </summary>
    public class FinancialEntryDto
    {
        /// <summary>Identificador único do lançamento.</summary>
        public Guid Id { get; set; }

        /// <summary>ID do cliente associado. Null para lançamentos pessoais/empresariais.</summary>
        public Guid? ClientId { get; set; }

        /// <summary>Nome do cliente associado. Null para lançamentos pessoais/empresariais.</summary>
        public string? ClientName { get; set; }

        /// <summary>ID da categoria do lançamento.</summary>
        public Guid CategoryId { get; set; }

        /// <summary>Nome da categoria do lançamento.</summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>Ícone da categoria.</summary>
        public string? CategoryIcon { get; set; }

        /// <summary>Cor da categoria.</summary>
        public string? CategoryColor { get; set; }

        /// <summary>Tipo do lançamento: Receita (0) ou Despesa (1).</summary>
        public EntryType Type { get; set; }

        /// <summary>Valor monetário do lançamento (sempre positivo).</summary>
        public decimal Amount { get; set; }

        /// <summary>Descrição do lançamento.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Data efetiva em que o lançamento ocorreu.</summary>
        public DateTime EntryDate { get; set; }

        /// <summary>Indica se o lançamento é recorrente.</summary>
        public bool IsRecurring { get; set; }

        /// <summary>Intervalo de recorrência (Daily, Weekly, Monthly, Yearly). Null se não recorrente.</summary>
        public string? RecurrenceInterval { get; set; }

        /// <summary>Notas adicionais do lançamento.</summary>
        public string? Notes { get; set; }

        /// <summary>Timestamp UTC de criação.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Timestamp UTC da última atualização. Null se nunca editado.</summary>
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO de entrada para criação de um novo lançamento financeiro.
    /// </summary>
    public class CreateFinancialEntryDto
    {
        /// <summary>
        /// ID do cliente ao qual o lançamento pertence.
        /// Obrigatório apenas para Contadores. Null para PessoaFisica/Empresa.
        /// </summary>
        public Guid? ClientId { get; set; }

        /// <summary>ID da categoria do lançamento. Deve ser uma categoria ativa.</summary>
        [Required(ErrorMessage = "A categoria é obrigatória")]
        public Guid CategoryId { get; set; }

        /// <summary>Tipo do lançamento: Receita ou Despesa.</summary>
        [Required(ErrorMessage = "O tipo do lançamento é obrigatório")]
        public EntryType Type { get; set; }

        /// <summary>
        /// Valor monetário do lançamento. Deve ser maior que zero.
        /// Sempre positivo — o tipo define o sinal no relatório.
        /// </summary>
        [Required(ErrorMessage = "O valor é obrigatório")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Amount { get; set; }

        /// <summary>Descrição do lançamento. Entre 2 e 500 caracteres.</summary>
        [Required(ErrorMessage = "A descrição é obrigatória")]
        [StringLength(500, MinimumLength = 2, ErrorMessage = "A descrição deve ter entre 2 e 500 caracteres")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Data efetiva do lançamento. Permite lançamentos retroativos.
        /// Se não informada, usa a data atual (UTC).
        /// </summary>
        public DateTime? EntryDate { get; set; }

        /// <summary>Indica se o lançamento é recorrente. Padrão: false.</summary>
        public bool IsRecurring { get; set; }

        /// <summary>
        /// Intervalo de recorrência. Obrigatório quando IsRecurring=true.
        /// Valores aceitos: "Daily", "Weekly", "Monthly", "Yearly".
        /// </summary>
        [StringLength(20)]
        public string? RecurrenceInterval { get; set; }

        /// <summary>Notas adicionais. Máx 1000 caracteres. Opcional.</summary>
        [StringLength(1000, ErrorMessage = "As notas devem ter no máximo 1000 caracteres")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO de entrada para atualização de um lançamento financeiro existente.
    /// Todos os campos são opcionais — apenas os preenchidos serão atualizados.
    /// </summary>
    public class UpdateFinancialEntryDto
    {
        /// <summary>Novo ID de categoria. Deve ser uma categoria ativa.</summary>
        public Guid? CategoryId { get; set; }

        /// <summary>Novo valor monetário. Deve ser maior que zero.</summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal? Amount { get; set; }

        /// <summary>Nova descrição. Entre 2 e 500 caracteres.</summary>
        [StringLength(500, MinimumLength = 2, ErrorMessage = "A descrição deve ter entre 2 e 500 caracteres")]
        public string? Description { get; set; }

        /// <summary>Nova data efetiva do lançamento.</summary>
        public DateTime? EntryDate { get; set; }

        /// <summary>Atualiza se o lançamento é recorrente.</summary>
        public bool? IsRecurring { get; set; }

        /// <summary>Novo intervalo de recorrência. Aceitos: "Daily", "Weekly", "Monthly", "Yearly".</summary>
        [StringLength(20)]
        public string? RecurrenceInterval { get; set; }

        /// <summary>Novas notas adicionais. Máx 1000 caracteres.</summary>
        [StringLength(1000, ErrorMessage = "As notas devem ter no máximo 1000 caracteres")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO de filtros para listagem de lançamentos financeiros.
    /// Todos os parâmetros são opcionais e combinam-se como filtros AND.
    /// </summary>
    public class FinancialEntryFilterDto
    {
        /// <summary>Filtra por tipo: Receita (0) ou Despesa (1).</summary>
        public EntryType? Type { get; set; }

        /// <summary>Filtra por ID de categoria.</summary>
        public Guid? CategoryId { get; set; }

        /// <summary>Filtra por ID de cliente (apenas para Contadores).</summary>
        public Guid? ClientId { get; set; }

        /// <summary>Data inicial do filtro de período (inclusive).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Data final do filtro de período (inclusive).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>Filtra apenas lançamentos recorrentes quando true.</summary>
        public bool? IsRecurring { get; set; }

        /// <summary>
        /// Texto livre para busca na descrição e notas do lançamento.
        /// Case-insensitive, busca por substring.
        /// </summary>
        [StringLength(200)]
        public string? Search { get; set; }
    }

    /// <summary>
    /// DTO de resumo financeiro por período.
    /// Retornado em GET /financial-entries/summary.
    /// </summary>
    public class FinancialSummaryDto
    {
        /// <summary>Total de receitas no período filtrado.</summary>
        public decimal TotalReceitas { get; set; }

        /// <summary>Total de despesas no período filtrado.</summary>
        public decimal TotalDespesas { get; set; }

        /// <summary>Saldo do período (TotalReceitas - TotalDespesas).</summary>
        public decimal Saldo { get; set; }

        /// <summary>Quantidade de lançamentos de receita no período.</summary>
        public int QuantidadeReceitas { get; set; }

        /// <summary>Quantidade de lançamentos de despesa no período.</summary>
        public int QuantidadeDespesas { get; set; }

        /// <summary>Resumo por categoria com totais e percentuais.</summary>
        public IEnumerable<CategorySummaryDto> PorCategoria { get; set; } = new List<CategorySummaryDto>();

        /// <summary>Histórico mensal de receitas e despesas.</summary>
        public IEnumerable<MonthlyFinancialDto> HistoricoMensal { get; set; } = new List<MonthlyFinancialDto>();
    }

    /// <summary>
    /// DTO de resumo de lançamentos agrupados por categoria.
    /// Usado dentro de FinancialSummaryDto.
    /// </summary>
    public class CategorySummaryDto
    {
        /// <summary>ID da categoria.</summary>
        public Guid CategoryId { get; set; }

        /// <summary>Nome da categoria.</summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>Ícone da categoria.</summary>
        public string? Icon { get; set; }

        /// <summary>Cor da categoria.</summary>
        public string? Color { get; set; }

        /// <summary>Tipo: Receita ou Despesa.</summary>
        public EntryType Type { get; set; }

        /// <summary>Total do período para esta categoria.</summary>
        public decimal Total { get; set; }

        /// <summary>Percentual sobre o total geral do mesmo tipo (0-100).</summary>
        public decimal Percentual { get; set; }

        /// <summary>Quantidade de lançamentos nesta categoria no período.</summary>
        public int Quantidade { get; set; }
    }

    /// <summary>
    /// DTO de histórico mensal de receitas e despesas.
    /// Usado dentro de FinancialSummaryDto para compor o gráfico de linha.
    /// </summary>
    public class MonthlyFinancialDto
    {
        /// <summary>Ano de referência (ex: 2026).</summary>
        public int Ano { get; set; }

        /// <summary>Mês de referência (1-12).</summary>
        public int Mes { get; set; }

        /// <summary>Label formatado para exibição (ex: "Jan/26").</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Total de receitas no mês.</summary>
        public decimal TotalReceitas { get; set; }

        /// <summary>Total de despesas no mês.</summary>
        public decimal TotalDespesas { get; set; }

        /// <summary>Saldo do mês (TotalReceitas - TotalDespesas).</summary>
        public decimal Saldo { get; set; }
    }
}
