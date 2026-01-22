namespace EconomyBackPortifolio.Models
{
    public class Assets
    {
        public Guid Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
