using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EconomyBackPortifolio.Controllers
{
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

        [HttpPost]
        public async Task<ActionResult<AssetDto>> CreateAsset([FromBody] CreateAssetDto createAssetDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var asset = await _assetService.CreateAssetAsync(createAssetDto);

                _logger.LogInformation("Asset created: {Symbol} ({Name})", asset.Symbol, asset.Name);

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
