using EconomyBackPortifolio.Enums;
using EconomyBackPortifolio.Models;

namespace EconomyBackPortifolio.Services
{
    public interface IVerificationCodeService
    {
        Task GenerateAndSendCodeAsync(Guid userId, string email, string userName, VerificationCodeType type);
        Task<Users> ValidateCodeAsync(string email, string code, VerificationCodeType type);
    }
}
