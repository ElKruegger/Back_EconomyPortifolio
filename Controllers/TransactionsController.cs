using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EconomyBackPortifolio.Controllers
{
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
