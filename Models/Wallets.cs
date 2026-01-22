namespace EconomyBackPortifolio.Models
{
    public class Wallets
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation property
        public Users? User { get; set; }
    }
}
