using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EconomyBackPortifolio.Controllers
{
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
