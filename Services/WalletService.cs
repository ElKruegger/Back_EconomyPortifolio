using EconomyBackPortifolio.Data;
using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Enums;
using EconomyBackPortifolio.Models;
using Microsoft.EntityFrameworkCore;

namespace EconomyBackPortifolio.Services
{
    public class WalletService : IWalletService
    {
        private readonly ApplicationDbContext _context;

        public WalletService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<WalletDto> CreateWalletAsync(Guid userId, CreateWalletDto createWalletDto)
        {
            var normalizedCurrency = Currency.Normalize(createWalletDto.Currency);

            // Validar moeda
            if (!Currency.IsValid(normalizedCurrency))
            {
                throw new ArgumentException($"Moeda inválida: {createWalletDto.Currency}. Moedas válidas: {string.Join(", ", Currency.ValidCurrencies)}");
            }

            // Verificar se já existe wallet para essa moeda
            var existingWallet = await GetWalletByCurrencyAsync(userId, normalizedCurrency);
            if (existingWallet != null)
            {
                throw new InvalidOperationException($"Já existe uma wallet para a moeda {normalizedCurrency}");
            }

            // Criar nova wallet
            var wallet = new Wallets
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Currency = normalizedCurrency,
                Balance = 0.00m,
                CreatedAt = DateTime.UtcNow
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            return new WalletDto
            {
                Id = wallet.Id,
                Currency = wallet.Currency,
                Balance = wallet.Balance,
                CreatedAt = wallet.CreatedAt
            };
        }

        public async Task<IEnumerable<WalletDto>> GetUserWalletsAsync(Guid userId)
        {
            var wallets = await _context.Wallets
                .Where(w => w.UserId == userId)
                .OrderBy(w => w.Currency)
                .Select(w => new WalletDto
                {
                    Id = w.Id,
                    Currency = w.Currency,
                    Balance = w.Balance,
                    CreatedAt = w.CreatedAt
                })
                .ToListAsync();

            return wallets;
        }

        public async Task<WalletDto?> GetWalletByIdAsync(Guid walletId, Guid userId)
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.Id == walletId && w.UserId == userId);

            if (wallet == null)
                return null;

            return new WalletDto
            {
                Id = wallet.Id,
                Currency = wallet.Currency,
                Balance = wallet.Balance,
                CreatedAt = wallet.CreatedAt
            };
        }

        public async Task<bool> WalletExistsAsync(Guid userId, string currency)
        {
            var normalizedCurrency = Currency.Normalize(currency);
            return await _context.Wallets
                .AnyAsync(w => w.UserId == userId && w.Currency == normalizedCurrency);
        }

        public async Task<WalletDto?> GetWalletByCurrencyAsync(Guid userId, string currency)
        {
            var normalizedCurrency = Currency.Normalize(currency);
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId && w.Currency == normalizedCurrency);

            if (wallet == null)
                return null;

            return new WalletDto
            {
                Id = wallet.Id,
                Currency = wallet.Currency,
                Balance = wallet.Balance,
                CreatedAt = wallet.CreatedAt
            };
        }
    }
}
