using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EconomyBackPortifolio.Controllers
{
    /// <summary>
    /// Controller responsável pelo portfólio de investimentos do usuário.
    /// Uma posição (Position) representa a quantidade de um ativo mantido em uma wallet,
    /// juntamente com o preço médio de custo e o valor atual.
    /// Todos os endpoints requerem autenticação JWT.
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
        /// Lista todas as posições abertas do usuário com P&amp;L (lucro/prejuízo) calculado.
        /// O P&amp;L é calculado em tempo real usando o preço atual do ativo armazenado na tabela assets.
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
        /// Retorna o resumo consolidado do portfólio para o dashboard:
        /// total investido, valor atual, P&amp;L total, saldos por wallet e alocação por ativo.
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
        /// Obtém uma posição específica por ID com P&amp;L calculado.
        /// Retorna 404 se não encontrada ou se não pertencer ao usuário autenticado.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PositionDto>> GetPosition(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var position = await _positionService.GetPositionByIdAsync(id, userId);

                if (position == null)
                    return NotFound(new { message = "Posição não encontrada" });

                return Ok(position);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter posição: {PositionId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }
    }
}
