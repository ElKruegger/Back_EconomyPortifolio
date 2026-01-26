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

        public async Task<IEnumerable<TransactionDto>> GetUserTransactionsAsync(Guid userId)
        {
            // Buscar todas as wallets do usuário
            var wallets = await _context.Wallets
                .Where(w => w.UserId == userId)
                .Select(w => w.Id)
                .ToListAsync();

            var transactions = await _context.Transactions
                .Where(t => wallets.Contains(t.WalletId))
                .Include(t => t.Wallet)
                .Include(t => t.Asset)
                .OrderByDescending(t => t.TransactionAt)
                .ToListAsync();

            var result = new List<TransactionDto>();
            foreach (var transaction in transactions)
            {
                result.Add(await MapToTransactionDtoAsync(transaction));
            }

            return result;
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
