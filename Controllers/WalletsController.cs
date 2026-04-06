using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EconomyBackPortifolio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletsController : BaseApiController
    {
        private readonly IWalletService _walletService;
        private readonly ILogger<WalletsController> _logger;

        public WalletsController(IWalletService walletService, ILogger<WalletsController> logger)
        {
            _walletService = walletService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WalletDto>>> GetWallets()
        {
            try
            {
                var userId = GetUserId();
                var wallets = await _walletService.GetUserWalletsAsync(userId);
                return Ok(wallets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing wallets for user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<WalletDto>> GetWallet(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var wallet = await _walletService.GetWalletByIdAsync(id, userId);

                if (wallet == null)
                    return NotFound(new { message = "Wallet not found" });

                return Ok(wallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching wallet: {WalletId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("currency/{currency}")]
        public async Task<ActionResult<WalletDto>> GetWalletByCurrency(string currency)
        {
            try
            {
                var userId = GetUserId();
                var wallet = await _walletService.GetWalletByCurrencyAsync(userId, currency);

                if (wallet == null)
                    return NotFound(new { message = $"Wallet not found for currency '{currency}'" });

                return Ok(wallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching wallet by currency: {Currency}", currency);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<WalletDto>> CreateWallet([FromBody] CreateWalletDto createWalletDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetUserId();
                var wallet = await _walletService.CreateWalletAsync(userId, createWalletDto);

                _logger.LogInformation("Wallet created: {Currency} for user {UserId}", createWalletDto.Currency, userId);

                return CreatedAtAction(nameof(GetWallet), new { id = wallet.Id }, wallet);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid currency when creating wallet: {Currency}", createWalletDto.Currency);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Duplicate wallet for currency: {Currency}", createWalletDto.Currency);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating wallet: {Currency}", createWalletDto.Currency);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
