using System.ComponentModel.DataAnnotations;

namespace EconomyBackPortifolio.DTOs
{
    public class TransactionDto
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public string WalletCurrency { get; set; } = string.Empty;
        public Guid? AssetId { get; set; }
        public string? AssetSymbol { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal? Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal Total { get; set; }
        public DateTime TransactionAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DepositDto
    {
        [Required(ErrorMessage = "O valor é obrigatório")]
        [Range(0.01, 999999999.99, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Amount { get; set; }
    }

    public class ConvertCurrencyDto
    {
        [Required(ErrorMessage = "A moeda de origem é obrigatória")]
        public string FromCurrency { get; set; } = string.Empty;

        [Required(ErrorMessage = "A moeda de destino é obrigatória")]
        public string ToCurrency { get; set; } = string.Empty;

        [Required(ErrorMessage = "O valor é obrigatório")]
        [Range(0.01, 999999999.99, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "A taxa de câmbio é obrigatória")]
        [Range(0.0001, 999999.99, ErrorMessage = "A taxa de câmbio deve ser maior que zero")]
        public decimal ExchangeRate { get; set; }
    }

    public class BuyAssetDto
    {
        [Required(ErrorMessage = "O ID do asset é obrigatório")]
        public Guid AssetId { get; set; }

        [Required(ErrorMessage = "A quantidade é obrigatória")]
        [Range(0.000001, 999999999.99, ErrorMessage = "A quantidade deve ser maior que zero")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "O preço é obrigatório")]
        [Range(0.000001, 999999999.99, ErrorMessage = "O preço deve ser maior que zero")]
        public decimal Price { get; set; }
    }

    public class SellAssetDto
    {
        [Required(ErrorMessage = "O ID do asset é obrigatório")]
        public Guid AssetId { get; set; }

        [Required(ErrorMessage = "A quantidade é obrigatória")]
        [Range(0.000001, 999999999.99, ErrorMessage = "A quantidade deve ser maior que zero")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "O preço é obrigatório")]
        [Range(0.000001, 999999999.99, ErrorMessage = "O preço deve ser maior que zero")]
        public decimal Price { get; set; }
    }

    /// <summary>
    /// Filtros para busca de transações (query parameters)
    /// </summary>
    public class TransactionFilterDto
    {
        /// <summary>Tipo: DEPOSIT, BUY, SELL, CONVERSION</summary>
        public string? Type { get; set; }

        /// <summary>Moeda da wallet (BRL, USD, EUR, etc.)</summary>
        public string? Currency { get; set; }

        /// <summary>ID do asset específico</summary>
        public Guid? AssetId { get; set; }

        /// <summary>Data inicial do período</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Data final do período</summary>
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Resumo de transações agrupado por tipo (para gráficos)
    /// </summary>
    public class TransactionsSummaryDto
    {
        public decimal TotalDeposits { get; set; }
        public decimal TotalBuys { get; set; }
        public decimal TotalSells { get; set; }
        public decimal TotalConversions { get; set; }
        public int TransactionCount { get; set; }
        public List<TransactionsByTypeDto> ByType { get; set; } = new();
        public List<MonthlyTransactionDto> MonthlyHistory { get; set; } = new();
    }

    /// <summary>
    /// Totais agrupados por tipo de transação
    /// </summary>
    public class TransactionsByTypeDto
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Total { get; set; }
    }

    /// <summary>
    /// Totais mensais de transações (para gráficos de linha/barra ao longo do tempo)
    /// </summary>
    public class MonthlyTransactionDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal TotalDeposits { get; set; }
        public decimal TotalBuys { get; set; }
        public decimal TotalSells { get; set; }
        public decimal TotalConversions { get; set; }
        public int TransactionCount { get; set; }
    }
}
