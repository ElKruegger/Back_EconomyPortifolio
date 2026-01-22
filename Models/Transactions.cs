namespace EconomyBackPortifolio.Models
{
    public class Transactions
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public Guid? AssetId { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal? Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal Total { get; set; }
        public DateTime TransactionAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Wallets? Wallet { get; set; }
        public Assets? Asset { get; set; }
    }
}
