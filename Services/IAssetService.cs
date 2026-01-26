using EconomyBackPortifolio.DTOs;

namespace EconomyBackPortifolio.Services
{
    public interface IAssetService
    {
        Task<AssetDto> CreateAssetAsync(CreateAssetDto createAssetDto);
        Task<IEnumerable<AssetDto>> GetAllAssetsAsync();
        Task<AssetDto?> GetAssetByIdAsync(Guid assetId);
        Task<AssetDto?> GetAssetBySymbolAsync(string symbol);
        Task<bool> AssetExistsAsync(string symbol);
        Task<AssetDto> UpdateAssetPriceAsync(Guid assetId, UpdateAssetPriceDto updatePriceDto);
    }
}
