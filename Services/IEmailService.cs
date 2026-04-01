using EconomyBackPortifolio.Enums;

namespace EconomyBackPortifolio.Services
{
    public interface IEmailService
    {
        Task SendVerificationCodeAsync(string toEmail, string userName, string code, VerificationCodeType type);
    }
}
