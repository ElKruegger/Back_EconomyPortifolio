using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EconomyBackPortifolio.Controllers
{
    /// <summary>
    /// Handles all financial operations for the authenticated user.
    /// Every money movement in the system goes through this controller and generates an immutable transaction record.
    ///
    /// Available transaction types:
    /// - DEPOSIT   : Adds BRL funds to the user's BRL wallet (entry point for all capital).
    /// - CONVERSION: Exchanges balance between two of the user's wallets (e.g. BRL -> USD).
    /// - BUY       : Purchases an asset, debiting the wallet and creating/updating a position.
    /// - SELL      : Sells an asset, crediting the wallet and reducing/removing a position.
    ///
    /// All endpoints require a valid JWT token and are scoped to the authenticated user.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionsController : BaseApiController
    {
        private readonly ITransactionService _transactionService;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(ITransactionService transactionService, ILogger<TransactionsController> logger)
        {
            _transactionService = transactionService;
            _logger = logger;
        }

        /// <summary>
        /// Returns the authenticated user's transaction history with optional filters.
        /// All filters are optional — omitting them returns the full history.
        /// Results are ordered by most recent first.
        ///
        /// Use this to render the transaction history list or feed export data.
        /// </summary>
        /// <param name="type">Filter by transaction type: DEPOSIT, BUY, SELL, CONVERSION.</param>
        /// <param name="currency">Filter by wallet currency (e.g. BRL, USD).</param>
        /// <param name="assetId">Filter by a specific asset ID (GUID).</param>
        /// <param name="fromDate">Filter transactions on or after this date (UTC).</param>
        /// <param name="toDate">Filter transactions on or before this date (UTC).</param>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
            [FromQuery] string? type,
            [FromQuery] string? currency,
            [FromQuery] Guid? assetId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                var userId = GetUserId();
                var filter = new TransactionFilterDto
                {
                    Type = type,
                    Currency = currency,
                    AssetId = assetId,
                    FromDate = fromDate,
                    ToDate = toDate
                };

                var transactions = await _transactionService.GetUserTransactionsAsync(userId, filter);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing transactions for user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Returns a consolidated summary of the user's transactions, grouped by type and by month.
        /// Accepts the same optional filters as GET /api/transactions.
        ///
        /// Includes:
        /// - TotalDeposits, TotalBuys, TotalSells, TotalConversions: aggregated totals.
        /// - ByType: count and total amount for each transaction type (for bar/pie charts).
        /// - MonthlyHistory: month-by-month breakdown (for line charts on the dashboard).
        ///
        /// Use this to power the analytics charts on the dashboard.
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<TransactionsSummaryDto>> GetTransactionsSummary(
            [FromQuery] string? type,
            [FromQuery] string? currency,
            [FromQuery] Guid? assetId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                var userId = GetUserId();
                var filter = new TransactionFilterDto
                {
                    Type = type,
                    Currency = currency,
                    AssetId = assetId,
                    FromDate = fromDate,
                    ToDate = toDate
                };

                var summary = await _transactionService.GetTransactionsSummaryAsync(userId, filter);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching transaction summary");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Returns a single transaction by its ID.
        /// Only returns the transaction if it belongs to the authenticated user.
        /// Returns 404 if the transaction does not exist or belongs to another user.
        /// </summary>
        /// <param name="id">The transaction's unique identifier (GUID).</param>
        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionDto>> GetTransaction(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var transaction = await _transactionService.GetTransactionByIdAsync(id, userId);

                if (transaction == null)
                    return NotFound(new { message = "Transaction not found" });

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching transaction: {TransactionId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Deposits an amount of BRL into the user's BRL wallet.
        /// This is the entry point for all capital in the system — users must deposit
        /// before they can buy assets or convert to other currencies.
        ///
        /// Rules:
        /// - Amount must be greater than zero.
        /// - The BRL wallet must exist (it is auto-created on registration).
        ///
        /// Returns 201 Created with the generated transaction record.
        /// Example body: { "amount": 1000.00 }
        /// </summary>
        [HttpPost("deposit")]
        public async Task<ActionResult<TransactionDto>> Deposit([FromBody] DepositDto depositDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetUserId();
                var transaction = await _transactionService.DepositAsync(userId, depositDto);

                _logger.LogInformation("Deposit completed: {Amount} BRL for user {UserId}", depositDto.Amount, userId);

                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Deposit failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing deposit");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Converts an amount from one of the user's wallets to another (e.g. BRL to USD).
        /// Both wallets must already exist. The source wallet must have sufficient balance.
        /// The exchange rate must be provided by the frontend using a live market quote.
        ///
        /// Rules:
        /// - FromCurrency and ToCurrency must be different.
        /// - Both wallets must belong to the authenticated user.
        /// - Source wallet balance must be >= amount.
        /// - ExchangeRate must be greater than zero.
        ///
        /// Example body: { "fromCurrency": "BRL", "toCurrency": "USD", "amount": 500.00, "exchangeRate": 5.75 }
        /// </summary>
        [HttpPost("convert")]
        public async Task<ActionResult<TransactionDto>> ConvertCurrency([FromBody] ConvertCurrencyDto convertDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetUserId();
                var transaction = await _transactionService.ConvertCurrencyAsync(userId, convertDto);

                _logger.LogInformation("Conversion completed: {Amount} {From} -> {To} for user {UserId}",
                    convertDto.Amount, convertDto.FromCurrency, convertDto.ToCurrency, userId);

                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid conversion data: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Conversion failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing currency conversion");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Purchases a given quantity of an asset using the wallet that matches the asset's currency.
        /// Debits (wallet balance - total cost) and creates or updates the user's position for that asset.
        /// If the user already holds this asset, the average price is recalculated automatically.
        ///
        /// Rules:
        /// - The asset must exist in the catalog (use POST /api/assets to register it first).
        /// - The user must have a wallet in the asset's currency with sufficient balance.
        /// - Quantity and Price must be greater than zero.
        /// - The price sent here should match the current market price at the time of the operation.
        ///
        /// Example body: { "assetId": "uuid", "quantity": 2.5, "price": 213.49 }
        /// </summary>
        [HttpPost("buy")]
        public async Task<ActionResult<TransactionDto>> BuyAsset([FromBody] BuyAssetDto buyDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetUserId();
                var transaction = await _transactionService.BuyAssetAsync(userId, buyDto);

                _logger.LogInformation("Buy completed: {Quantity} of asset {AssetId} for user {UserId}",
                    buyDto.Quantity, buyDto.AssetId, userId);

                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Buy failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing buy");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Sells a given quantity of an asset from the user's position.
        /// Credits the wallet with (quantity * price) and reduces the position accordingly.
        /// If the entire quantity is sold, the position is automatically removed.
        ///
        /// Rules:
        /// - The user must have an open position for that asset.
        /// - The quantity sold cannot exceed the quantity currently held.
        /// - Quantity and Price must be greater than zero.
        /// - The price sent here should match the current market price at the time of the operation.
        ///
        /// Example body: { "assetId": "uuid", "quantity": 1.0, "price": 220.00 }
        /// </summary>
        [HttpPost("sell")]
        public async Task<ActionResult<TransactionDto>> SellAsset([FromBody] SellAssetDto sellDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetUserId();
                var transaction = await _transactionService.SellAssetAsync(userId, sellDto);

                _logger.LogInformation("Sell completed: {Quantity} of asset {AssetId} for user {UserId}",
                    sellDto.Quantity, sellDto.AssetId, userId);

                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Sell failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sell");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
