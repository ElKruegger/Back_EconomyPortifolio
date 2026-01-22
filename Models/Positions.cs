namespace EconomyBackPortifolio.Models
{
    public class Positions
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public Guid AssetId { get; set; }
        public decimal Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal TotalInvested { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public Wallets? Wallet { get; set; }
        public Assets? Asset { get; set; }
    }
}
