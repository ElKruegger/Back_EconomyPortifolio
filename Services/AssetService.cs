using EconomyBackPortifolio.Data;
using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Enums;
using EconomyBackPortifolio.Models;
using Microsoft.EntityFrameworkCore;

namespace EconomyBackPortifolio.Services
{
    public class AssetService : IAssetService
    {
        private readonly ApplicationDbContext _context;

        public AssetService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AssetDto> CreateAssetAsync(CreateAssetDto createAssetDto)
        {
            var normalizedSymbol = createAssetDto.Symbol.ToUpperInvariant().Trim();
            var normalizedCurrency = Currency.Normalize(createAssetDto.Currency);

            // Validar moeda (por enquanto apenas USD)
            if (normalizedCurrency != "USD")
            {
                throw new ArgumentException("Por enquanto, apenas assets em USD são suportados");
            }

            if (!Currency.IsValid(normalizedCurrency))
            {
                throw new ArgumentException($"Moeda inválida: {createAssetDto.Currency}. Moedas válidas: {string.Join(", ", Currency.ValidCurrencies)}");
            }

            // Verificar se já existe asset com esse símbolo
            var existingAsset = await _context.Assets
                .FirstOrDefaultAsync(a => a.Symbol == normalizedSymbol);

            if (existingAsset != null)
            {
                throw new InvalidOperationException($"Já existe um asset com o símbolo {normalizedSymbol}");
            }

            // Criar novo asset
            var asset = new Assets
            {
                Id = Guid.NewGuid(),
                Symbol = normalizedSymbol,
                Name = createAssetDto.Name.Trim(),
                Type = createAssetDto.Type.Trim(),
                Currency = normalizedCurrency,
                CurrentPrice = createAssetDto.CurrentPrice,
                CreatedAt = DateTime.UtcNow
            };

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            return new AssetDto
            {
                Id = asset.Id,
                Symbol = asset.Symbol,
                Name = asset.Name,
                Type = asset.Type,
                Currency = asset.Currency,
                CurrentPrice = asset.CurrentPrice,
                CreatedAt = asset.CreatedAt
            };
        }

        public async Task<IEnumerable<AssetDto>> GetAllAssetsAsync()
        {
            var assets = await _context.Assets
                .OrderBy(a => a.Symbol)
                .Select(a => new AssetDto
                {
                    Id = a.Id,
                    Symbol = a.Symbol,
                    Name = a.Name,
                    Type = a.Type,
                    Currency = a.Currency,
                    CurrentPrice = a.CurrentPrice,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return assets;
        }

        public async Task<AssetDto?> GetAssetByIdAsync(Guid assetId)
        {
            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.Id == assetId);

            if (asset == null)
                return null;

            return new AssetDto
            {
                Id = asset.Id,
                Symbol = asset.Symbol,
                Name = asset.Name,
                Type = asset.Type,
                Currency = asset.Currency,
                CurrentPrice = asset.CurrentPrice,
                CreatedAt = asset.CreatedAt
            };
        }

        public async Task<AssetDto?> GetAssetBySymbolAsync(string symbol)
        {
            var normalizedSymbol = symbol.ToUpperInvariant().Trim();
            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.Symbol == normalizedSymbol);

            if (asset == null)
                return null;

            return new AssetDto
            {
                Id = asset.Id,
                Symbol = asset.Symbol,
                Name = asset.Name,
                Type = asset.Type,
                Currency = asset.Currency,
                CurrentPrice = asset.CurrentPrice,
                CreatedAt = asset.CreatedAt
            };
        }

        public async Task<bool> AssetExistsAsync(string symbol)
        {
            var normalizedSymbol = symbol.ToUpperInvariant().Trim();
            return await _context.Assets
                .AnyAsync(a => a.Symbol == normalizedSymbol);
        }

        public async Task<AssetDto> UpdateAssetPriceAsync(Guid assetId, UpdateAssetPriceDto updatePriceDto)
        {
            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null)
            {
                throw new InvalidOperationException("Asset não encontrado");
            }

            asset.CurrentPrice = updatePriceDto.CurrentPrice;
            await _context.SaveChangesAsync();

            return new AssetDto
            {
                Id = asset.Id,
                Symbol = asset.Symbol,
                Name = asset.Name,
                Type = asset.Type,
                Currency = asset.Currency,
                CurrentPrice = asset.CurrentPrice,
                CreatedAt = asset.CreatedAt
            };
        }
    }
}
