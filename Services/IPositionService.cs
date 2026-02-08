using EconomyBackPortifolio.DTOs;

namespace EconomyBackPortifolio.Services
{
    public interface IPositionService
    {
        Task<IEnumerable<PositionDto>> GetUserPositionsAsync(Guid userId);
        Task<PositionDto?> GetPositionByIdAsync(Guid positionId, Guid userId);
        Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(Guid userId);
    }
}
