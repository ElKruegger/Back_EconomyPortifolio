using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EconomyBackPortifolio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PositionsController : ControllerBase
    {
        private readonly IPositionService _positionService;
        private readonly ILogger<PositionsController> _logger;

        public PositionsController(IPositionService positionService, ILogger<PositionsController> logger)
        {
            _positionService = positionService;
            _logger = logger;
        }

        /// <summary>
        /// Lista todas as posições (portfólio) do usuário autenticado
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
                _logger.LogError(ex, "Erro ao listar posições do usuário");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Resumo consolidado do portfólio (totais, alocação, P&L) para o dashboard
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
                _logger.LogError(ex, "Erro ao obter resumo do portfólio");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Obtém uma posição específica por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PositionDto>> GetPosition(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var position = await _positionService.GetPositionByIdAsync(id, userId);

                if (position == null)
                {
                    return NotFound(new { message = "Posição não encontrada" });
                }

                return Ok(position);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter posição: {PositionId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("Usuário não autenticado");
            }
            return userId;
        }
    }
}
