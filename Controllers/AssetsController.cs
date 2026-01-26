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

        /// <summary>
        /// Lista todos os assets disponíveis
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
                _logger.LogError(ex, "Erro ao listar assets");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Obtém um asset específico por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AssetDto>> GetAsset(Guid id)
        {
            try
            {
                var asset = await _assetService.GetAssetByIdAsync(id);

                if (asset == null)
                {
                    return NotFound(new { message = "Asset não encontrado" });
                }

                return Ok(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter asset: {AssetId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Obtém um asset por símbolo
        /// </summary>
        [HttpGet("symbol/{symbol}")]
        public async Task<ActionResult<AssetDto>> GetAssetBySymbol(string symbol)
        {
            try
            {
                var asset = await _assetService.GetAssetBySymbolAsync(symbol);

                if (asset == null)
                {
                    return NotFound(new { message = $"Asset não encontrado para o símbolo {symbol}" });
                }

                return Ok(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter asset por símbolo: {Symbol}", symbol);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Cria um novo asset (apenas USD suportado por enquanto)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AssetDto>> CreateAsset([FromBody] CreateAssetDto createAssetDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var asset = await _assetService.CreateAssetAsync(createAssetDto);
                
                _logger.LogInformation("Asset criado: {Symbol} ({Name})", asset.Symbol, asset.Name);
                
                return CreatedAtAction(nameof(GetAsset), new { id = asset.Id }, asset);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Tentativa de criar asset com dados inválidos: {Symbol}", createAssetDto.Symbol);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Tentativa de criar asset duplicado: {Symbol}", createAssetDto.Symbol);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar asset: {Symbol}", createAssetDto.Symbol);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Atualiza o preço atual de um asset
        /// </summary>
        [HttpPut("{id}/price")]
        public async Task<ActionResult<AssetDto>> UpdateAssetPrice(Guid id, [FromBody] UpdateAssetPriceDto updatePriceDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var asset = await _assetService.UpdateAssetPriceAsync(id, updatePriceDto);
                
                _logger.LogInformation("Preço do asset {Symbol} atualizado para {Price}", asset.Symbol, asset.CurrentPrice);
                
                return Ok(asset);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Tentativa de atualizar preço de asset inexistente: {AssetId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar preço do asset: {AssetId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }
    }
}
