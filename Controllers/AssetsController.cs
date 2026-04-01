using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EconomyBackPortifolio.Controllers
{
    /// <summary>
    /// Manages the global asset catalog (stocks, crypto, ETFs, etc.).
    /// Assets are shared across all users — they represent tradeable instruments,
    /// not individual holdings. A user's holdings are tracked in Positions.
    /// All endpoints require a valid JWT token.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AssetsController : ControllerBase
    {
        private readonly IAssetService _assetService;
        private readonly ILogger<AssetsController> _logger;

        public AssetsController(IAssetService assetService, ILogger<AssetsController> logger)
        {
            _assetService = assetService;
            _logger = logger;
        }

        /// <summary>
        /// Returns the full list of assets available for trading.
        /// This is the catalog the frontend uses to populate search/select fields.
        /// Example response: [{ symbol: "AAPL", name: "Apple Inc.", currentPrice: 213.49 }]
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssets()
        {
            try
            {
                var assets = await _assetService.GetAllAssetsAsync();
                return Ok(assets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing assets");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Returns a single asset by its unique ID (GUID).
        /// Use this when you already know the asset ID (e.g. from a position or transaction).
        /// Returns 404 if the asset does not exist.
        /// </summary>
        /// <param name="id">The asset's unique identifier (GUID).</param>
        [HttpGet("{id}")]
        public async Task<ActionResult<AssetDto>> GetAsset(Guid id)
        {
            try
            {
                var asset = await _assetService.GetAssetByIdAsync(id);

                if (asset == null)
                    return NotFound(new { message = "Asset not found" });

                return Ok(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching asset: {AssetId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Returns a single asset by its ticker symbol (e.g. "AAPL", "BTC").
        /// Useful for lookups by symbol without knowing the ID in advance.
        /// The symbol lookup is case-insensitive.
        /// Returns 404 if no asset matches the given symbol.
        /// </summary>
        /// <param name="symbol">The asset ticker symbol (e.g. AAPL, BTC, PETR4).</param>
        [HttpGet("symbol/{symbol}")]
        public async Task<ActionResult<AssetDto>> GetAssetBySymbol(string symbol)
        {
            try
            {
                var asset = await _assetService.GetAssetBySymbolAsync(symbol);

                if (asset == null)
                    return NotFound(new { message = $"Asset not found for symbol '{symbol}'" });

                return Ok(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching asset by symbol: {Symbol}", symbol);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Registers a new asset in the global catalog.
        /// Once created, any user can buy/sell this asset via the Transactions endpoints.
        ///
        /// Rules:
        /// - Symbol must be unique (returns 409 Conflict if it already exists).
        /// - Currency must be valid (USD, BRL, EUR, BTC, etc.).
        /// - CurrentPrice must be greater than zero.
        ///
        /// Example body:
        /// { "symbol": "AAPL", "name": "Apple Inc.", "type": "stock", "currency": "USD", "currentPrice": 213.49 }
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AssetDto>> CreateAsset([FromBody] CreateAssetDto createAssetDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var asset = await _assetService.CreateAssetAsync(createAssetDto);

                _logger.LogInformation("Asset created: {Symbol} ({Name})", asset.Symbol, asset.Name);

                // Returns 201 Created with a Location header pointing to GET /api/assets/{id}
                return CreatedAtAction(nameof(GetAsset), new { id = asset.Id }, asset);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid data when creating asset: {Symbol}", createAssetDto.Symbol);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Duplicate asset symbol: {Symbol}", createAssetDto.Symbol);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating asset: {Symbol}", createAssetDto.Symbol);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Updates the current market price of an existing asset.
        /// This does NOT create a transaction — it only refreshes the reference price
        /// used to calculate P&amp;L (profit and loss) across all user positions.
        ///
        /// In a production environment this would be called by a background job
        /// that pulls prices from a market data provider (e.g. Alpha Vantage, Yahoo Finance).
        /// For now, the frontend sends the price manually before buy/sell operations.
        ///
        /// Returns 404 if the asset ID does not exist.
        /// </summary>
        /// <param name="id">The asset's unique identifier (GUID).</param>
        /// <param name="updatePriceDto">Object containing the new current price.</param>
        [HttpPut("{id}/price")]
        public async Task<ActionResult<AssetDto>> UpdateAssetPrice(Guid id, [FromBody] UpdateAssetPriceDto updatePriceDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var asset = await _assetService.UpdateAssetPriceAsync(id, updatePriceDto);

                _logger.LogInformation("Asset price updated: {Symbol} -> {Price}", asset.Symbol, asset.CurrentPrice);

                return Ok(asset);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Asset not found for price update: {AssetId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating asset price: {AssetId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
