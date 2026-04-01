using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EconomyBackPortifolio.Controllers
{
    /// <summary>
    /// Manages the authenticated user's investment positions (portfolio).
    /// A position is automatically created or updated whenever the user buys an asset,
    /// and reduced or removed when they sell. You cannot create or delete positions directly —
    /// they are always the result of a buy or sell transaction.
    ///
    /// Key concepts:
    /// - AveragePrice: the weighted average cost of all purchases of that asset.
    /// - CurrentValue: quantity * asset's current market price.
    /// - ProfitLoss (P&amp;L): currentValue - totalInvested.
    ///
    /// All endpoints require a valid JWT token and are scoped to the authenticated user.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PositionsController : BaseApiController
    {
        private readonly IPositionService _positionService;
        private readonly ILogger<PositionsController> _logger;

        public PositionsController(IPositionService positionService, ILogger<PositionsController> logger)
        {
            _positionService = positionService;
            _logger = logger;
        }

        /// <summary>
        /// Returns all open positions for the authenticated user, with real-time P&amp;L.
        /// P&amp;L is calculated on the fly using the current price stored in the assets table.
        /// Returns an empty array [] if the user has not bought any assets yet.
        ///
        /// Use this to render the portfolio list on the dashboard.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PositionDto>>> GetPositions()
        {
            try
            {
                var userId = GetUserId();
                var positions = await _positionService.GetUserPositionsAsync(userId);
                return Ok(positions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing positions for user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Returns a consolidated summary of the entire portfolio for the dashboard.
        /// Aggregates all positions and wallet balances into a single response object.
        ///
        /// Includes:
        /// - TotalInvested: sum of all purchase costs across positions.
        /// - TotalCurrentValue: sum of all positions at current market prices.
        /// - TotalProfitLoss and TotalProfitLossPercentage: overall P&amp;L.
        /// - WalletBalances: list of all wallets with their current balance (for pie chart).
        /// - AssetAllocations: each position's weight in the portfolio (for pie chart).
        ///
        /// Use this to populate the main dashboard overview cards and charts.
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<PortfolioSummaryDto>> GetPortfolioSummary()
        {
            try
            {
                var userId = GetUserId();
                var summary = await _positionService.GetPortfolioSummaryAsync(userId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching portfolio summary");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Returns a single position by its ID, with real-time P&amp;L calculated.
        /// Only returns the position if it belongs to the authenticated user.
        /// Returns 404 if the position does not exist or belongs to another user.
        ///
        /// Use this for a position detail page or when refreshing a single card.
        /// </summary>
        /// <param name="id">The position's unique identifier (GUID).</param>
        [HttpGet("{id}")]
        public async Task<ActionResult<PositionDto>> GetPosition(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var position = await _positionService.GetPositionByIdAsync(id, userId);

                if (position == null)
                    return NotFound(new { message = "Position not found" });

                return Ok(position);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching position: {PositionId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
