using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Enums;
using EconomyBackPortifolio.Models;

namespace EconomyBackPortifolio.Services
{
    public interface IAuthService
    {
        Task<MessageResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<MessageResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> VerifyCodeAsync(VerifyCodeDto verifyCodeDto);
        Task<MessageResponseDto> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
        Task<MessageResponseDto> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task<bool> ValidateUserAsync(string email, string password);
        Task<Users?> GetUserByEmailAsync(string email);
        Task<UserInfoDto?> GetUserByIdAsync(Guid userId);
    }
}
