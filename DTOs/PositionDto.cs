namespace EconomyBackPortifolio.DTOs
{
    public class PositionDto
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public string WalletCurrency { get; set; } = string.Empty;
        public Guid AssetId { get; set; }
        public string AssetSymbol { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public decimal AssetCurrentPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ProfitLossPercentage { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Resumo consolidado do portfólio para o dashboard
    /// </summary>
    public class PortfolioSummaryDto
    {
        public decimal TotalInvested { get; set; }
        public decimal TotalCurrentValue { get; set; }
        public decimal TotalProfitLoss { get; set; }
        public decimal TotalProfitLossPercentage { get; set; }
        public int PositionCount { get; set; }
        public decimal TotalWalletBalance { get; set; }
        public List<WalletBalanceDto> WalletBalances { get; set; } = new();
        public List<AssetAllocationDto> AssetAllocations { get; set; } = new();
    }

    /// <summary>
    /// Saldo de cada wallet (para gráfico de pizza de moedas)
    /// </summary>
    public class WalletBalanceDto
    {
        public Guid WalletId { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }

    /// <summary>
    /// Alocação por asset (para gráfico de pizza do portfólio)
    /// </summary>
    public class AssetAllocationDto
    {
        public string AssetSymbol { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal Percentage { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ProfitLossPercentage { get; set; }
    }
}
