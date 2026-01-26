using EconomyBackPortifolio.Data;
using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Models;
using Microsoft.EntityFrameworkCore;

namespace EconomyBackPortifolio.Services
{
    public class PositionService : IPositionService
    {
        private readonly ApplicationDbContext _context;

        public PositionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PositionDto>> GetUserPositionsAsync(Guid userId)
        {
            // Buscar todas as wallets do usuário
            var walletIds = await _context.Wallets
                .Where(w => w.UserId == userId)
                .Select(w => w.Id)
                .ToListAsync();

            // Buscar posições com informações do asset e wallet
            var positions = await _context.Positions
                .Where(p => walletIds.Contains(p.WalletId))
                .Include(p => p.Wallet)
                .Include(p => p.Asset)
                .OrderBy(p => p.Asset!.Symbol)
                .ToListAsync();

            var result = new List<PositionDto>();

            foreach (var position in positions)
            {
                if (position.Asset == null || position.Wallet == null)
                    continue;

                var currentValue = position.Quantity * position.Asset.CurrentPrice;
                var profitLoss = currentValue - position.TotalInvested;
                var profitLossPercentage = position.TotalInvested > 0 
                    ? (profitLoss / position.TotalInvested) * 100 
                    : 0;

                result.Add(new PositionDto
                {
                    Id = position.Id,
                    WalletId = position.WalletId,
                    WalletCurrency = position.Wallet.Currency,
                    AssetId = position.AssetId,
                    AssetSymbol = position.Asset.Symbol,
                    AssetName = position.Asset.Name,
                    AssetType = position.Asset.Type,
                    AssetCurrentPrice = position.Asset.CurrentPrice,
                    Quantity = position.Quantity,
                    AveragePrice = position.AveragePrice,
                    TotalInvested = position.TotalInvested,
                    CurrentValue = currentValue,
                    ProfitLoss = profitLoss,
                    ProfitLossPercentage = profitLossPercentage,
                    UpdatedAt = position.UpdatedAt
                });
            }

            return result;
        }

        public async Task<PositionDto?> GetPositionByIdAsync(Guid positionId, Guid userId)
        {
            // Buscar todas as wallets do usuário
            var walletIds = await _context.Wallets
                .Where(w => w.UserId == userId)
                .Select(w => w.Id)
                .ToListAsync();

            // Buscar posição específica
            var position = await _context.Positions
                .Include(p => p.Wallet)
                .Include(p => p.Asset)
                .FirstOrDefaultAsync(p => p.Id == positionId && walletIds.Contains(p.WalletId));

            if (position == null || position.Asset == null || position.Wallet == null)
                return null;

            var currentValue = position.Quantity * position.Asset.CurrentPrice;
            var profitLoss = currentValue - position.TotalInvested;
            var profitLossPercentage = position.TotalInvested > 0 
                ? (profitLoss / position.TotalInvested) * 100 
                : 0;

            return new PositionDto
            {
                Id = position.Id,
                WalletId = position.WalletId,
                WalletCurrency = position.Wallet.Currency,
                AssetId = position.AssetId,
                AssetSymbol = position.Asset.Symbol,
                AssetName = position.Asset.Name,
                AssetType = position.Asset.Type,
                AssetCurrentPrice = position.Asset.CurrentPrice,
                Quantity = position.Quantity,
                AveragePrice = position.AveragePrice,
                TotalInvested = position.TotalInvested,
                CurrentValue = currentValue,
                ProfitLoss = profitLoss,
                ProfitLossPercentage = profitLossPercentage,
                UpdatedAt = position.UpdatedAt
            };
        }
    }
}
