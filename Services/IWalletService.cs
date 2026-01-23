using EconomyBackPortifolio.DTOs;

namespace EconomyBackPortifolio.Services
{
    public interface IWalletService
    {
        Task<WalletDto> CreateWalletAsync(Guid userId, CreateWalletDto createWalletDto);
        Task<IEnumerable<WalletDto>> GetUserWalletsAsync(Guid userId);
        Task<WalletDto?> GetWalletByIdAsync(Guid walletId, Guid userId);
        Task<bool> WalletExistsAsync(Guid userId, string currency);
        Task<WalletDto?> GetWalletByCurrencyAsync(Guid userId, string currency);
    }
}
