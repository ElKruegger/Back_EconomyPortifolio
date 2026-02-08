using EconomyBackPortifolio.Data;
using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Enums;
using EconomyBackPortifolio.Models;
using Microsoft.EntityFrameworkCore;

namespace EconomyBackPortifolio.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWalletService _walletService;

        public TransactionService(ApplicationDbContext context, IWalletService walletService)
        {
            _context = context;
            _walletService = walletService;
        }

        public async Task<TransactionDto> DepositAsync(Guid userId, DepositDto depositDto)
        {
            // Buscar wallet BRL do usuário
            var wallet = await _walletService.GetWalletByCurrencyAsync(userId, "BRL");
            if (wallet == null)
            {
                throw new InvalidOperationException("Wallet BRL não encontrada. Contate o suporte.");
            }

            // Buscar wallet completa do banco para atualizar
            var walletEntity = await _context.Wallets.FindAsync(wallet.Id);
            if (walletEntity == null)
            {
                throw new InvalidOperationException("Wallet não encontrada");
            }

            // Atualizar balance
            walletEntity.Balance += depositDto.Amount;

            // Criar transação
            var transaction = new Transactions
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                AssetId = null,
                Type = "DEPOSIT",
                Quantity = null,
                Price = null,
                Total = depositDto.Amount,
                TransactionAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return await MapToTransactionDtoAsync(transaction);
        }

        public async Task<TransactionDto> ConvertCurrencyAsync(Guid userId, ConvertCurrencyDto convertDto)
        {
            var fromCurrency = Currency.Normalize(convertDto.FromCurrency);
            var toCurrency = Currency.Normalize(convertDto.ToCurrency);

            // Validar moedas
            if (!Currency.IsValid(fromCurrency) || !Currency.IsValid(toCurrency))
            {
                throw new ArgumentException("Moeda inválida");
            }

            if (fromCurrency == toCurrency)
            {
                throw new ArgumentException("As moedas de origem e destino devem ser diferentes");
            }

            // Buscar wallets diretamente do banco para garantir tracking correto
            var fromWalletEntity = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId && w.Currency == fromCurrency);
            
            if (fromWalletEntity == null)
            {
                throw new InvalidOperationException($"Wallet {fromCurrency} não encontrada");
            }

            // Verificar se tem saldo suficiente
            if (fromWalletEntity.Balance < convertDto.Amount)
            {
                throw new InvalidOperationException($"Saldo insuficiente. Saldo disponível: {fromWalletEntity.Balance}, necessário: {convertDto.Amount}");
            }

            // Buscar ou criar wallet de destino
            var toWalletEntity = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId && w.Currency == toCurrency);
            
            if (toWalletEntity == null)
            {
                // Criar wallet de destino diretamente no contexto
                toWalletEntity = new Wallets
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Currency = toCurrency,
                    Balance = 0.00m,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Wallets.Add(toWalletEntity);
            }

            // Calcular valor convertido
            var convertedAmount = convertDto.Amount * convertDto.ExchangeRate;

            // Atualizar balances (garantindo que está no contexto)
            fromWalletEntity.Balance -= convertDto.Amount;
            toWalletEntity.Balance += convertedAmount;

            // Criar transação
            var transaction = new Transactions
            {
                Id = Guid.NewGuid(),
                WalletId = fromWalletEntity.Id,
                AssetId = null,
                Type = "CONVERSION",
                Quantity = convertDto.Amount,
                Price = convertDto.ExchangeRate,
                Total = convertedAmount,
                TransactionAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return await MapToTransactionDtoAsync(transaction);
        }

        public async Task<TransactionDto> BuyAssetAsync(Guid userId, BuyAssetDto buyDto)
        {
            // Buscar asset
            var asset = await _context.Assets.FindAsync(buyDto.AssetId);
            if (asset == null)
            {
                throw new InvalidOperationException("Asset não encontrado");
            }

            // Buscar wallet na moeda do asset
            var wallet = await _walletService.GetWalletByCurrencyAsync(userId, asset.Currency);
            if (wallet == null)
            {
                throw new InvalidOperationException($"Wallet {asset.Currency} não encontrada");
            }

            // Calcular total
            var total = buyDto.Quantity * buyDto.Price;

            // Verificar saldo
            if (wallet.Balance < total)
            {
                throw new InvalidOperationException($"Saldo insuficiente. Saldo disponível: {wallet.Balance}, necessário: {total}");
            }

            // Buscar wallet completa do banco
            var walletEntity = await _context.Wallets.FindAsync(wallet.Id);
            if (walletEntity == null)
            {
                throw new InvalidOperationException("Wallet não encontrada");
            }

            // Atualizar balance
            walletEntity.Balance -= total;

            // Criar ou atualizar position
            var position = await _context.Positions
                .FirstOrDefaultAsync(p => p.WalletId == wallet.Id && p.AssetId == buyDto.AssetId);

            if (position == null)
            {
                // Criar nova position
                position = new Positions
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    AssetId = buyDto.AssetId,
                    Quantity = buyDto.Quantity,
                    AveragePrice = buyDto.Price,
                    TotalInvested = total,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Positions.Add(position);
            }
            else
            {
                // Atualizar position existente (calcular preço médio ponderado)
                var totalQuantity = position.Quantity + buyDto.Quantity;
                var totalInvested = position.TotalInvested + total;
                position.AveragePrice = totalInvested / totalQuantity;
                position.Quantity = totalQuantity;
                position.TotalInvested = totalInvested;
                position.UpdatedAt = DateTime.UtcNow;
            }

            // Criar transação
            var transaction = new Transactions
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                AssetId = buyDto.AssetId,
                Type = "BUY",
                Quantity = buyDto.Quantity,
                Price = buyDto.Price,
                Total = total,
                TransactionAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return await MapToTransactionDtoAsync(transaction);
        }

        public async Task<TransactionDto> SellAssetAsync(Guid userId, SellAssetDto sellDto)
        {
            // Buscar asset
            var asset = await _context.Assets.FindAsync(sellDto.AssetId);
            if (asset == null)
            {
                throw new InvalidOperationException("Asset não encontrado");
            }

            // Buscar wallet na moeda do asset
            var wallet = await _walletService.GetWalletByCurrencyAsync(userId, asset.Currency);
            if (wallet == null)
            {
                throw new InvalidOperationException($"Wallet {asset.Currency} não encontrada");
            }

            // Buscar position
            var position = await _context.Positions
                .FirstOrDefaultAsync(p => p.WalletId == wallet.Id && p.AssetId == sellDto.AssetId);

            if (position == null || position.Quantity < sellDto.Quantity)
            {
                throw new InvalidOperationException($"Quantidade insuficiente. Quantidade disponível: {position?.Quantity ?? 0}");
            }

            // Calcular total
            var total = sellDto.Quantity * sellDto.Price;

            // Buscar wallet completa do banco
            var walletEntity = await _context.Wallets.FindAsync(wallet.Id);
            if (walletEntity == null)
            {
                throw new InvalidOperationException("Wallet não encontrada");
            }

            // Atualizar balance
            walletEntity.Balance += total;

            // Atualizar position
            position.Quantity -= sellDto.Quantity;
            position.TotalInvested -= (sellDto.Quantity * position.AveragePrice);
            
            // Se quantity zerou, remover position
            if (position.Quantity <= 0)
            {
                _context.Positions.Remove(position);
            }
            else
            {
                position.UpdatedAt = DateTime.UtcNow;
            }

            // Criar transação
            var transaction = new Transactions
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                AssetId = sellDto.AssetId,
                Type = "SELL",
                Quantity = sellDto.Quantity,
                Price = sellDto.Price,
                Total = total,
                TransactionAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return await MapToTransactionDtoAsync(transaction);
        }

        public async Task<IEnumerable<TransactionDto>> GetUserTransactionsAsync(Guid userId, TransactionFilterDto? filter = null)
        {
            // Buscar wallets do usuário (com filtro de moeda se aplicável)
            var walletsQuery = _context.Wallets.Where(w => w.UserId == userId);

            if (!string.IsNullOrWhiteSpace(filter?.Currency))
            {
                var normalizedCurrency = Currency.Normalize(filter.Currency);
                walletsQuery = walletsQuery.Where(w => w.Currency == normalizedCurrency);
            }

            var walletIds = await walletsQuery.Select(w => w.Id).ToListAsync();

            // Construir query com filtros
            var query = _context.Transactions
                .Where(t => walletIds.Contains(t.WalletId))
                .Include(t => t.Wallet)
                .Include(t => t.Asset)
                .AsQueryable();

            // Filtro por tipo
            if (!string.IsNullOrWhiteSpace(filter?.Type))
            {
                var normalizedType = filter.Type.ToUpperInvariant().Trim();
                query = query.Where(t => t.Type == normalizedType);
            }

            // Filtro por asset
            if (filter?.AssetId.HasValue == true)
            {
                query = query.Where(t => t.AssetId == filter.AssetId.Value);
            }

            // Filtro por data inicial
            if (filter?.FromDate.HasValue == true)
            {
                var fromDate = DateTime.SpecifyKind(filter.FromDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(t => t.TransactionAt >= fromDate);
            }

            // Filtro por data final
            if (filter?.ToDate.HasValue == true)
            {
                var toDate = DateTime.SpecifyKind(filter.ToDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(t => t.TransactionAt <= toDate);
            }

            var transactions = await query
                .OrderByDescending(t => t.TransactionAt)
                .ToListAsync();

            var result = new List<TransactionDto>();
            foreach (var transaction in transactions)
            {
                result.Add(await MapToTransactionDtoAsync(transaction));
            }

            return result;
        }

        public async Task<TransactionsSummaryDto> GetTransactionsSummaryAsync(Guid userId, TransactionFilterDto? filter = null)
        {
            // Buscar wallets do usuário (com filtro de moeda se aplicável)
            var walletsQuery = _context.Wallets.Where(w => w.UserId == userId);

            if (!string.IsNullOrWhiteSpace(filter?.Currency))
            {
                var normalizedCurrency = Currency.Normalize(filter.Currency);
                walletsQuery = walletsQuery.Where(w => w.Currency == normalizedCurrency);
            }

            var walletIds = await walletsQuery.Select(w => w.Id).ToListAsync();

            // Construir query com filtros
            var query = _context.Transactions
                .Where(t => walletIds.Contains(t.WalletId))
                .AsQueryable();

            // Filtro por tipo
            if (!string.IsNullOrWhiteSpace(filter?.Type))
            {
                var normalizedType = filter.Type.ToUpperInvariant().Trim();
                query = query.Where(t => t.Type == normalizedType);
            }

            // Filtro por asset
            if (filter?.AssetId.HasValue == true)
            {
                query = query.Where(t => t.AssetId == filter.AssetId.Value);
            }

            // Filtro por data inicial
            if (filter?.FromDate.HasValue == true)
            {
                var fromDate = DateTime.SpecifyKind(filter.FromDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(t => t.TransactionAt >= fromDate);
            }

            // Filtro por data final
            if (filter?.ToDate.HasValue == true)
            {
                var toDate = DateTime.SpecifyKind(filter.ToDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(t => t.TransactionAt <= toDate);
            }

            var transactions = await query.ToListAsync();

            // Agrupar por tipo
            var byType = transactions
                .GroupBy(t => t.Type)
                .Select(g => new TransactionsByTypeDto
                {
                    Type = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(t => t.Total)
                })
                .OrderBy(t => t.Type)
                .ToList();

            // Agrupar por mês (para gráficos de evolução)
            var monthlyHistory = transactions
                .GroupBy(t => new { t.TransactionAt.Year, t.TransactionAt.Month })
                .Select(g => new MonthlyTransactionDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Label = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TotalDeposits = g.Where(t => t.Type == "DEPOSIT").Sum(t => t.Total),
                    TotalBuys = g.Where(t => t.Type == "BUY").Sum(t => t.Total),
                    TotalSells = g.Where(t => t.Type == "SELL").Sum(t => t.Total),
                    TotalConversions = g.Where(t => t.Type == "CONVERSION").Sum(t => t.Total),
                    TransactionCount = g.Count()
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToList();

            return new TransactionsSummaryDto
            {
                TotalDeposits = transactions.Where(t => t.Type == "DEPOSIT").Sum(t => t.Total),
                TotalBuys = transactions.Where(t => t.Type == "BUY").Sum(t => t.Total),
                TotalSells = transactions.Where(t => t.Type == "SELL").Sum(t => t.Total),
                TotalConversions = transactions.Where(t => t.Type == "CONVERSION").Sum(t => t.Total),
                TransactionCount = transactions.Count,
                ByType = byType,
                MonthlyHistory = monthlyHistory
            };
        }

        public async Task<TransactionDto?> GetTransactionByIdAsync(Guid transactionId, Guid userId)
        {
            // Buscar todas as wallets do usuário
            var wallets = await _context.Wallets
                .Where(w => w.UserId == userId)
                .Select(w => w.Id)
                .ToListAsync();

            var transaction = await _context.Transactions
                .Include(t => t.Wallet)
                .Include(t => t.Asset)
                .FirstOrDefaultAsync(t => t.Id == transactionId && wallets.Contains(t.WalletId));

            if (transaction == null)
                return null;

            return await MapToTransactionDtoAsync(transaction);
        }

        private async Task<TransactionDto> MapToTransactionDtoAsync(Transactions transaction)
        {
            string walletCurrency;
            string? assetSymbol = null;

            // Se já tem as navigation properties carregadas, usar elas
            if (transaction.Wallet != null)
            {
                walletCurrency = transaction.Wallet.Currency;
            }
            else
            {
                var wallet = await _context.Wallets.FindAsync(transaction.WalletId);
                walletCurrency = wallet?.Currency ?? string.Empty;
            }

            if (transaction.AssetId.HasValue)
            {
                if (transaction.Asset != null)
                {
                    assetSymbol = transaction.Asset.Symbol;
                }
                else
                {
                    var asset = await _context.Assets.FindAsync(transaction.AssetId.Value);
                    assetSymbol = asset?.Symbol;
                }
            }

            return new TransactionDto
            {
                Id = transaction.Id,
                WalletId = transaction.WalletId,
                WalletCurrency = walletCurrency,
                AssetId = transaction.AssetId,
                AssetSymbol = assetSymbol,
                Type = transaction.Type,
                Quantity = transaction.Quantity,
                Price = transaction.Price,
                Total = transaction.Total,
                TransactionAt = transaction.TransactionAt,
                CreatedAt = transaction.CreatedAt
            };
        }
    }
}
