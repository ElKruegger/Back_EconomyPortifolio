using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EconomyBackPortifolio.Controllers
{
    /// <summary>
    /// Manages the authenticated user's wallets.
    /// A wallet represents a balance in a specific currency (BRL, USD, BTC, etc.).
    /// Each user can hold one wallet per currency.
    /// A BRL wallet is created automatically when the user registers.
    /// All endpoints require a valid JWT token and are scoped to the authenticated user.
    /// </summary>
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

        /// <summary>
        /// Returns all wallets belonging to the authenticated user, ordered by currency.
        /// Each wallet includes the current balance.
        /// A newly registered user will have at least one wallet (BRL).
        /// </summary>
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

        /// <summary>
        /// Returns a specific wallet by its ID.
        /// Only returns the wallet if it belongs to the authenticated user.
        /// Returns 404 if the wallet does not exist or belongs to another user.
        /// </summary>
        /// <param name="id">The wallet's unique identifier (GUID).</param>
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

        /// <summary>
        /// Returns the wallet for a specific currency belonging to the authenticated user.
        /// Useful for checking available balance before executing a buy or convert operation.
        /// Returns 404 if the user does not have a wallet for that currency yet.
        ///
        /// Example: GET /api/wallets/currency/USD
        /// </summary>
        /// <param name="currency">Currency code (e.g. BRL, USD, EUR, BTC).</param>
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

        /// <summary>
        /// Creates a new wallet for the authenticated user in the specified currency.
        /// Each user can have only one wallet per currency.
        /// The BRL wallet is auto-created on registration, so you don't need to call this for BRL.
        ///
        /// Rules:
        /// - Currency must be a valid code (BRL, USD, EUR, GBP, BTC, ETH, etc.).
        /// - Returns 409 Conflict if the user already has a wallet in that currency.
        ///
        /// Example body: { "currency": "USD" }
        /// </summary>
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

                // Returns 201 Created with a Location header pointing to GET /api/wallets/{id}
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
