using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Models;

namespace EconomyBackPortifolio.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<bool> ValidateUserAsync(string email, string password);
        Task<Users?> GetUserByEmailAsync(string email);
    }
}
