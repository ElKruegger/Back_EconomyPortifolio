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
}
