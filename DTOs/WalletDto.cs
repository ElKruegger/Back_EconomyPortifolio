namespace EconomyBackPortifolio.DTOs
{
    public class WalletDto
    {
        public Guid Id { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateWalletDto
    {
        public string Currency { get; set; } = string.Empty;
    }
}
