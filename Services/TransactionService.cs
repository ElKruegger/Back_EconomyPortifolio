using EconomyBackPortifolio.Data;
using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Enums;
using EconomyBackPortifolio.Models;
using Microsoft.EntityFrameworkCore;

namespace EconomyBackPortifolio.Services
{
    /// <summary>
    /// Serviço responsável por todas as operações financeiras: depósito, conversão de moeda,
    /// compra e venda de ativos. Utiliza <see cref="IUnitOfWork"/> para garantir atomicidade
    /// das transações de banco de dados — se qualquer etapa falhar, todas são revertidas.
    /// </summary>
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;

        public TransactionService(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        /// <summary>
        /// Realiza um depósito em BRL na wallet do usuário.
        /// CORREÇÃO: usa transação de banco para garantir consistência entre
        /// a atualização do saldo e o registro da transação.
        /// </summary>
        /// <exception cref="InvalidOperationException">Wallet BRL não encontrada.</exception>
        public async Task<TransactionDto> DepositAsync(Guid userId, DepositDto depositDto)
        {
            // Busca diretamente a entidade tracked pelo EF — evita o double-query anterior.
            var walletEntity = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId && w.Currency == "BRL");

            if (walletEntity == null)
                throw new InvalidOperationException("Wallet BRL não encontrada. Contate o suporte.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Atualiza o saldo da wallet.
                walletEntity.Balance += depositDto.Amount;

                // Registra a transação de depósito.
                var transaction = new Transactions
                {
                    Id = Guid.NewGuid(),
                    WalletId = walletEntity.Id,
                    AssetId = null,
                    Type = TransactionType.Deposit,
                    Quantity = null,
                    Price = null,
                    Total = depositDto.Amount,
                    TransactionAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(transaction);
                await _unitOfWork.CommitTransactionAsync();

                return await MapToTransactionDtoAsync(transaction);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Converte um valor de uma wallet para outra (ex: BRL → USD).
        /// Cria a wallet de destino automaticamente se não existir.
        /// CORREÇÃO: usa transação de banco para atomicidade das duas atualizações de saldo.
        /// </summary>
        /// <exception cref="ArgumentException">Moedas inválidas ou iguais.</exception>
        /// <exception cref="InvalidOperationException">Saldo insuficiente ou wallet de origem não encontrada.</exception>
        public async Task<TransactionDto> ConvertCurrencyAsync(Guid userId, ConvertCurrencyDto convertDto)
        {
            var fromCurrency = Currency.Normalize(convertDto.FromCurrency);
            var toCurrency = Currency.Normalize(convertDto.ToCurrency);

            if (!Currency.IsValid(fromCurrency) || !Currency.IsValid(toCurrency))
                throw new ArgumentException("Moeda inválida");

            if (fromCurrency == toCurrency)
                throw new ArgumentException("As moedas de origem e destino devem ser diferentes");

            // Busca diretamente as entidades tracked — elimina o double-query anterior.
            var fromWalletEntity = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId && w.Currency == fromCurrency)
                ?? throw new InvalidOperationException($"Wallet {fromCurrency} não encontrada");

            if (fromWalletEntity.Balance < convertDto.Amount)
                throw new InvalidOperationException(
                    $"Saldo insuficiente. Saldo disponível: {fromWalletEntity.Balance}, necessário: {convertDto.Amount}");

            var toWalletEntity = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId && w.Currency == toCurrency);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Cria wallet de destino se não existir.
                if (toWalletEntity == null)
                {
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

                var convertedAmount = convertDto.Amount * convertDto.ExchangeRate;

                // Atualiza os dois saldos atomicamente.
                fromWalletEntity.Balance -= convertDto.Amount;
                toWalletEntity.Balance += convertedAmount;

                var transaction = new Transactions
                {
                    Id = Guid.NewGuid(),
                    WalletId = fromWalletEntity.Id,
                    AssetId = null,
                    Type = TransactionType.Conversion,
                    Quantity = convertDto.Amount,
                    Price = convertDto.ExchangeRate,
                    Total = convertedAmount,
                    TransactionAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(transaction);
                await _unitOfWork.CommitTransactionAsync();

                return await MapToTransactionDtoAsync(transaction);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Compra um ativo (asset) descontando o valor da wallet correspondente
        /// e criando ou atualizando a posição com preço médio ponderado.
        /// CORREÇÃO: usa transação de banco para garantir consistência.
        /// </summary>
        /// <exception cref="InvalidOperationException">Asset não encontrado, wallet inexistente ou saldo insuficiente.</exception>
        public async Task<TransactionDto> BuyAssetAsync(Guid userId, BuyAssetDto buyDto)
        {
            var asset = await _context.Assets.FindAsync(buyDto.AssetId)
                ?? throw new InvalidOperationException("Asset não encontrado");

            // Busca diretamente a entidade tracked — elimina o double-query anterior.
            var walletEntity = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId && w.Currency == asset.Currency)
                ?? throw new InvalidOperationException($"Wallet {asset.Currency} não encontrada");

            var total = buyDto.Quantity * buyDto.Price;

            if (walletEntity.Balance < total)
                throw new InvalidOperationException(
                    $"Saldo insuficiente. Saldo disponível: {walletEntity.Balance}, necessário: {total}");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Desconta o valor da wallet.
                walletEntity.Balance -= total;

                // Cria ou atualiza a posição com preço médio ponderado.
                var position = await _context.Positions
                    .FirstOrDefaultAsync(p => p.WalletId == walletEntity.Id && p.AssetId == buyDto.AssetId);

                if (position == null)
                {
                    // Primeira compra deste ativo nessa wallet: cria nova posição.
                    position = new Positions
                    {
                        Id = Guid.NewGuid(),
                        WalletId = walletEntity.Id,
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
                    // Compra adicional: recalcula o preço médio ponderado.
                    // Fórmula: (totalInvestido + novoTotal) / (quantidadeAtual + novaQuantidade)
                    var totalQuantity = position.Quantity + buyDto.Quantity;
                    var totalInvested = position.TotalInvested + total;

                    position.AveragePrice = totalInvested / totalQuantity;
                    position.Quantity = totalQuantity;
                    position.TotalInvested = totalInvested;
                    position.UpdatedAt = DateTime.UtcNow;
                }

                var transaction = new Transactions
                {
                    Id = Guid.NewGuid(),
                    WalletId = walletEntity.Id,
                    AssetId = buyDto.AssetId,
                    Type = TransactionType.Buy,
                    Quantity = buyDto.Quantity,
                    Price = buyDto.Price,
                    Total = total,
                    TransactionAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(transaction);
                await _unitOfWork.CommitTransactionAsync();

                return await MapToTransactionDtoAsync(transaction);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Vende um ativo creditando o valor na wallet e reduzindo (ou removendo) a posição.
        /// CORREÇÃO: usa transação de banco para garantir consistência.
        /// </summary>
        /// <exception cref="InvalidOperationException">Asset não encontrado, posição inexistente ou quantidade insuficiente.</exception>
        public async Task<TransactionDto> SellAssetAsync(Guid userId, SellAssetDto sellDto)
        {
            var asset = await _context.Assets.FindAsync(sellDto.AssetId)
                ?? throw new InvalidOperationException("Asset não encontrado");

            // Busca diretamente a entidade tracked — elimina o double-query anterior.
            var walletEntity = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId && w.Currency == asset.Currency)
                ?? throw new InvalidOperationException($"Wallet {asset.Currency} não encontrada");

            var position = await _context.Positions
                .FirstOrDefaultAsync(p => p.WalletId == walletEntity.Id && p.AssetId == sellDto.AssetId);

            if (position == null || position.Quantity < sellDto.Quantity)
                throw new InvalidOperationException(
                    $"Quantidade insuficiente. Quantidade disponível: {position?.Quantity ?? 0}");

            var total = sellDto.Quantity * sellDto.Price;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Credita o valor recebido na wallet.
                walletEntity.Balance += total;

                // Reduz a posição proporcionalmente ao preço médio de custo.
                position.Quantity -= sellDto.Quantity;
                position.TotalInvested -= sellDto.Quantity * position.AveragePrice;

                // Se a quantidade zerou, remove a posição da carteira.
                if (position.Quantity <= 0)
                    _context.Positions.Remove(position);
                else
                    position.UpdatedAt = DateTime.UtcNow;

                var transaction = new Transactions
                {
                    Id = Guid.NewGuid(),
                    WalletId = walletEntity.Id,
                    AssetId = sellDto.AssetId,
                    Type = TransactionType.Sell,
                    Quantity = sellDto.Quantity,
                    Price = sellDto.Price,
                    Total = total,
                    TransactionAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(transaction);
                await _unitOfWork.CommitTransactionAsync();

                return await MapToTransactionDtoAsync(transaction);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Retorna o histórico de transações do usuário com filtros opcionais
        /// (tipo, moeda, asset, período).
        /// </summary>
        public async Task<IEnumerable<TransactionDto>> GetUserTransactionsAsync(Guid userId, TransactionFilterDto? filter = null)
        {
            var walletsQuery = _context.Wallets.Where(w => w.UserId == userId);

            if (!string.IsNullOrWhiteSpace(filter?.Currency))
            {
                var normalizedCurrency = Currency.Normalize(filter.Currency);
                walletsQuery = walletsQuery.Where(w => w.Currency == normalizedCurrency);
            }

            var walletIds = await walletsQuery.Select(w => w.Id).ToListAsync();

            // Carrega Wallet e Asset de uma só vez com Include para evitar N+1 queries.
            var query = _context.Transactions
                .Where(t => walletIds.Contains(t.WalletId))
                .Include(t => t.Wallet)
                .Include(t => t.Asset)
                .AsQueryable();

            query = ApplyTransactionFilters(query, filter);

            var transactions = await query
                .OrderByDescending(t => t.TransactionAt)
                .ToListAsync();

            // Com navigation properties já carregadas, MapToTransactionDtoAsync não
            // fará queries adicionais — N+1 eliminado.
            return transactions.Select(t => MapToTransactionDto(t)).ToList();
        }

        /// <summary>
        /// Retorna o resumo consolidado de transações agrupado por tipo e por mês,
        /// utilizado nos gráficos do dashboard.
        /// </summary>
        public async Task<TransactionsSummaryDto> GetTransactionsSummaryAsync(Guid userId, TransactionFilterDto? filter = null)
        {
            var walletsQuery = _context.Wallets.Where(w => w.UserId == userId);

            if (!string.IsNullOrWhiteSpace(filter?.Currency))
            {
                var normalizedCurrency = Currency.Normalize(filter.Currency);
                walletsQuery = walletsQuery.Where(w => w.Currency == normalizedCurrency);
            }

            var walletIds = await walletsQuery.Select(w => w.Id).ToListAsync();

            var query = _context.Transactions
                .Where(t => walletIds.Contains(t.WalletId))
                .AsQueryable();

            query = ApplyTransactionFilters(query, filter);

            var transactions = await query.ToListAsync();

            // Agrupamentos feitos em memória (LINQ to Objects) após a query.
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

            var monthlyHistory = transactions
                .GroupBy(t => new { t.TransactionAt.Year, t.TransactionAt.Month })
                .Select(g => new MonthlyTransactionDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Label = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TotalDeposits = g.Where(t => t.Type == TransactionType.Deposit).Sum(t => t.Total),
                    TotalBuys = g.Where(t => t.Type == TransactionType.Buy).Sum(t => t.Total),
                    TotalSells = g.Where(t => t.Type == TransactionType.Sell).Sum(t => t.Total),
                    TotalConversions = g.Where(t => t.Type == TransactionType.Conversion).Sum(t => t.Total),
                    TransactionCount = g.Count()
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();

            return new TransactionsSummaryDto
            {
                TotalDeposits = transactions.Where(t => t.Type == TransactionType.Deposit).Sum(t => t.Total),
                TotalBuys = transactions.Where(t => t.Type == TransactionType.Buy).Sum(t => t.Total),
                TotalSells = transactions.Where(t => t.Type == TransactionType.Sell).Sum(t => t.Total),
                TotalConversions = transactions.Where(t => t.Type == TransactionType.Conversion).Sum(t => t.Total),
                TransactionCount = transactions.Count,
                ByType = byType,
                MonthlyHistory = monthlyHistory
            };
        }

        /// <summary>
        /// Busca uma transação específica pelo ID, garantindo que pertence ao usuário autenticado.
        /// Retorna null se não encontrada ou se não pertencer ao usuário.
        /// </summary>
        public async Task<TransactionDto?> GetTransactionByIdAsync(Guid transactionId, Guid userId)
        {
            var walletIds = await _context.Wallets
                .Where(w => w.UserId == userId)
                .Select(w => w.Id)
                .ToListAsync();

            var transaction = await _context.Transactions
                .Include(t => t.Wallet)
                .Include(t => t.Asset)
                .FirstOrDefaultAsync(t => t.Id == transactionId && walletIds.Contains(t.WalletId));

            if (transaction == null)
                return null;

            return MapToTransactionDto(transaction);
        }

        // ─────────────────────────────────────────────────────────────────────
        // MÉTODOS PRIVADOS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Aplica os filtros opcionais (tipo, asset, período) na query de transações.
        /// Centraliza a lógica de filtragem para evitar duplicação entre GetUserTransactions
        /// e GetTransactionsSummary.
        /// </summary>
        private static IQueryable<Transactions> ApplyTransactionFilters(
            IQueryable<Transactions> query, TransactionFilterDto? filter)
        {
            if (!string.IsNullOrWhiteSpace(filter?.Type))
            {
                var normalizedType = TransactionType.Normalize(filter.Type);
                query = query.Where(t => t.Type == normalizedType);
            }

            if (filter?.AssetId.HasValue == true)
                query = query.Where(t => t.AssetId == filter.AssetId.Value);

            if (filter?.FromDate.HasValue == true)
            {
                var fromDate = DateTime.SpecifyKind(filter.FromDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(t => t.TransactionAt >= fromDate);
            }

            if (filter?.ToDate.HasValue == true)
            {
                var toDate = DateTime.SpecifyKind(filter.ToDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(t => t.TransactionAt <= toDate);
            }

            return query;
        }

        /// <summary>
        /// Mapeia uma entidade <see cref="Transactions"/> para o DTO de resposta.
        /// Usa as navigation properties já carregadas pelo Include — sem queries adicionais.
        /// CORREÇÃO: versão síncrona que exige Include prévio, eliminando o N+1 anterior.
        /// </summary>
        private static TransactionDto MapToTransactionDto(Transactions transaction)
        {
            return new TransactionDto
            {
                Id = transaction.Id,
                WalletId = transaction.WalletId,
                WalletCurrency = transaction.Wallet?.Currency ?? string.Empty,
                AssetId = transaction.AssetId,
                AssetSymbol = transaction.Asset?.Symbol,
                Type = transaction.Type,
                Quantity = transaction.Quantity,
                Price = transaction.Price,
                Total = transaction.Total,
                TransactionAt = transaction.TransactionAt,
                CreatedAt = transaction.CreatedAt
            };
        }

        /// <summary>
        /// Versão async do mapeamento — usada após operações de escrita onde
        /// as navigation properties ainda não foram carregadas via Include.
        /// </summary>
        private async Task<TransactionDto> MapToTransactionDtoAsync(Transactions transaction)
        {
            // Se as navigation properties não foram carregadas, busca individualmente.
            var walletCurrency = transaction.Wallet?.Currency
                ?? (await _context.Wallets.FindAsync(transaction.WalletId))?.Currency
                ?? string.Empty;

            string? assetSymbol = null;
            if (transaction.AssetId.HasValue)
            {
                assetSymbol = transaction.Asset?.Symbol
                    ?? (await _context.Assets.FindAsync(transaction.AssetId.Value))?.Symbol;
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
