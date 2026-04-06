using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EconomyBackPortifolio.Controllers
{
    /// <summary>
    /// Controller responsável pelo gerenciamento de lançamentos financeiros (receitas e despesas).
    ///
    /// Este é o endpoint central do Economy Portfolio como plataforma de controle financeiro.
    ///
    /// Isolamento de dados:
    ///   - Cada usuário acessa apenas seus próprios lançamentos.
    ///   - Contadores acessam lançamentos de seus clientes via filtro ClientId.
    ///
    /// Endpoints disponíveis:
    ///   GET  /api/financial-entries         — lista com filtros
    ///   GET  /api/financial-entries/summary — resumo agregado por período
    ///   GET  /api/financial-entries/{id}    — detalhe de um lançamento
    ///   POST /api/financial-entries         — criar lançamento
    ///   PUT  /api/financial-entries/{id}    — atualizar lançamento
    ///   DELETE /api/financial-entries/{id}  — excluir lançamento
    /// </summary>
    [ApiController]
    [Route("api/financial-entries")]
    [Authorize]
    public class FinancialEntriesController : BaseApiController
    {
        private readonly IFinancialEntryService _financialEntryService;
        private readonly ILogger<FinancialEntriesController> _logger;

        public FinancialEntriesController(
            IFinancialEntryService financialEntryService,
            ILogger<FinancialEntriesController> logger)
        {
            _financialEntryService = financialEntryService;
            _logger = logger;
        }

        /// <summary>
        /// Lista os lançamentos financeiros do usuário com suporte a filtros combinados.
        /// Resultados ordenados por data do lançamento (mais recente primeiro).
        /// </summary>
        /// <param name="filter">Filtros opcionais: tipo, categoria, cliente, período, busca e recorrência.</param>
        /// <response code="200">Lista de lançamentos retornada com sucesso.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<FinancialEntryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<FinancialEntryDto>>> GetEntries(
            [FromQuery] FinancialEntryFilterDto filter)
        {
            try
            {
                var userId = GetUserId();
                var entries = await _financialEntryService.GetEntriesAsync(userId, filter);
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar lançamentos para usuário {UserId}", GetUserId());
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Retorna um resumo financeiro agregado por período.
        /// Inclui totais de receitas e despesas, breakdown por categoria e histórico mensal.
        /// Ideal para alimentar o dashboard e os gráficos da plataforma.
        /// </summary>
        /// <param name="filter">Filtros de período para o resumo (fromDate, toDate, type, etc.).</param>
        /// <response code="200">Resumo financeiro retornado com sucesso.</response>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(FinancialSummaryDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<FinancialSummaryDto>> GetSummary(
            [FromQuery] FinancialEntryFilterDto filter)
        {
            try
            {
                var userId = GetUserId();
                var summary = await _financialEntryService.GetSummaryAsync(userId, filter);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar resumo financeiro para usuário {UserId}", GetUserId());
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Retorna um lançamento financeiro específico pelo ID.
        /// </summary>
        /// <param name="id">ID do lançamento.</param>
        /// <response code="200">Lançamento encontrado.</response>
        /// <response code="404">Lançamento não encontrado ou sem acesso.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(FinancialEntryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FinancialEntryDto>> GetEntryById(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var entry = await _financialEntryService.GetEntryByIdAsync(id, userId);
                return Ok(entry);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar lançamento {EntryId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Cria um novo lançamento financeiro de receita ou despesa.
        /// </summary>
        /// <param name="dto">Dados do lançamento: tipo, valor, categoria, descrição e data.</param>
        /// <response code="201">Lançamento criado com sucesso.</response>
        /// <response code="400">Dados inválidos ou categoria incompatível com o tipo.</response>
        /// <response code="404">Categoria ou cliente não encontrado.</response>
        [HttpPost]
        [ProducesResponseType(typeof(FinancialEntryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FinancialEntryDto>> CreateEntry([FromBody] CreateFinancialEntryDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetUserId();
                var entry = await _financialEntryService.CreateEntryAsync(userId, dto);
                _logger.LogInformation(
                    "Lançamento criado: {EntryId} ({Type} R${Amount}) por usuário {UserId}",
                    entry.Id, entry.Type, entry.Amount, userId);

                return CreatedAtAction(nameof(GetEntryById), new { id = entry.Id }, entry);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar lançamento para usuário {UserId}", GetUserId());
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Atualiza um lançamento financeiro existente.
        /// Apenas o usuário proprietário pode editar o lançamento.
        /// </summary>
        /// <param name="id">ID do lançamento a atualizar.</param>
        /// <param name="dto">Dados para atualização. Apenas campos preenchidos são atualizados.</param>
        /// <response code="200">Lançamento atualizado com sucesso.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="404">Lançamento não encontrado ou sem acesso.</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(FinancialEntryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FinancialEntryDto>> UpdateEntry(Guid id, [FromBody] UpdateFinancialEntryDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetUserId();
                var entry = await _financialEntryService.UpdateEntryAsync(id, userId, dto);
                _logger.LogInformation("Lançamento atualizado: {EntryId} por usuário {UserId}", id, userId);

                return Ok(entry);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar lançamento {EntryId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Remove permanentemente um lançamento financeiro.
        /// Esta ação é irreversível — use com cautela.
        /// </summary>
        /// <param name="id">ID do lançamento a excluir.</param>
        /// <response code="204">Lançamento excluído com sucesso.</response>
        /// <response code="404">Lançamento não encontrado ou sem acesso.</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEntry(Guid id)
        {
            try
            {
                var userId = GetUserId();
                await _financialEntryService.DeleteEntryAsync(id, userId);
                _logger.LogInformation("Lançamento excluído: {EntryId} por usuário {UserId}", id, userId);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir lançamento {EntryId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }
    }
}
