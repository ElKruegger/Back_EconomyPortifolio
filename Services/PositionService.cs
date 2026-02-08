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

        public async Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(Guid userId)
        {
            // Buscar todas as wallets do usuário
            var wallets = await _context.Wallets
                .Where(w => w.UserId == userId)
                .ToListAsync();

            var walletIds = wallets.Select(w => w.Id).ToList();

            // Buscar posições com informações do asset e wallet
            var positions = await _context.Positions
                .Where(p => walletIds.Contains(p.WalletId))
                .Include(p => p.Wallet)
                .Include(p => p.Asset)
                .ToListAsync();

            // Calcular totais
            decimal totalInvested = 0;
            decimal totalCurrentValue = 0;
            var assetAllocations = new List<AssetAllocationDto>();

            foreach (var position in positions)
            {
                if (position.Asset == null || position.Wallet == null)
                    continue;

                var currentValue = position.Quantity * position.Asset.CurrentPrice;
                var profitLoss = currentValue - position.TotalInvested;
                var profitLossPercentage = position.TotalInvested > 0
                    ? (profitLoss / position.TotalInvested) * 100
                    : 0;

                totalInvested += position.TotalInvested;
                totalCurrentValue += currentValue;

                assetAllocations.Add(new AssetAllocationDto
                {
                    AssetSymbol = position.Asset.Symbol,
                    AssetName = position.Asset.Name,
                    AssetType = position.Asset.Type,
                    CurrentValue = currentValue,
                    ProfitLoss = profitLoss,
                    ProfitLossPercentage = profitLossPercentage
                });
            }

            // Calcular percentual de alocação de cada asset
            if (totalCurrentValue > 0)
            {
                foreach (var allocation in assetAllocations)
                {
                    allocation.Percentage = (allocation.CurrentValue / totalCurrentValue) * 100;
                }
            }

            var totalProfitLoss = totalCurrentValue - totalInvested;
            var totalProfitLossPercentage = totalInvested > 0
                ? (totalProfitLoss / totalInvested) * 100
                : 0;

            // Saldos das wallets
            var walletBalances = wallets.Select(w => new WalletBalanceDto
            {
                WalletId = w.Id,
                Currency = w.Currency,
                Balance = w.Balance
            }).ToList();

            return new PortfolioSummaryDto
            {
                TotalInvested = totalInvested,
                TotalCurrentValue = totalCurrentValue,
                TotalProfitLoss = totalProfitLoss,
                TotalProfitLossPercentage = totalProfitLossPercentage,
                PositionCount = positions.Count(p => p.Asset != null && p.Wallet != null),
                TotalWalletBalance = wallets.Sum(w => w.Balance),
                WalletBalances = walletBalances,
                AssetAllocations = assetAllocations.OrderByDescending(a => a.CurrentValue).ToList()
            };
        }
    }
}
