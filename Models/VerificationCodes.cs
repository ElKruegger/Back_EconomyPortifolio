using EconomyBackPortifolio.Enums;

namespace EconomyBackPortifolio.Models
{
    public class VerificationCodes
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Code { get; set; } = string.Empty;
        public VerificationCodeType Type { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public Users User { get; set; } = null!;
    }
}
