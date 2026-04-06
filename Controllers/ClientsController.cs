using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Enums;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EconomyBackPortifolio.Controllers
{
    /// <summary>
    /// Controller responsável pelo gerenciamento de clientes do perfil Contador.
    ///
    /// Acesso exclusivo para usuários com ProfileType=Contador.
    /// Usuários com outros perfis receberão 403 Forbidden ao tentar acessar estes endpoints.
    ///
    /// O conceito de Cliente implementa multi-tenancy para contadores:
    /// cada cliente tem seu espaço isolado de lançamentos financeiros.
    ///
    /// Limites por plano:
    ///   - Basic: até 3 clientes ativos simultaneamente
    ///   - Pro: clientes ilimitados
    ///
    /// Endpoints:
    ///   GET    /api/clients         — listar clientes do contador
    ///   GET    /api/clients/{id}    — detalhe de um cliente
    ///   POST   /api/clients         — criar cliente
    ///   PUT    /api/clients/{id}    — atualizar cliente
    ///   DELETE /api/clients/{id}    — arquivar cliente (soft delete)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientsController : BaseApiController
    {
        private readonly IClientService _clientService;
        private readonly IAuthService _authService;
        private readonly ILogger<ClientsController> _logger;

        public ClientsController(
            IClientService clientService,
            IAuthService authService,
            ILogger<ClientsController> logger)
        {
            _clientService = clientService;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Lista os clientes do Contador autenticado.
        /// </summary>
        /// <param name="onlyActive">Se true (padrão), retorna apenas clientes ativos.</param>
        /// <response code="200">Lista de clientes retornada com sucesso.</response>
        /// <response code="403">Acesso negado — endpoint exclusivo para Contadores.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ClientDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<ClientDto>>> GetClients(
            [FromQuery] bool onlyActive = true)
        {
            try
            {
                var userId = GetUserId();
                if (!await IsContadorAsync(userId))
                    return Forbid();

                var clients = await _clientService.GetClientsAsync(userId, onlyActive);
                return Ok(clients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar clientes para usuário {UserId}", GetUserId());
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Retorna um cliente específico do Contador pelo ID.
        /// </summary>
        /// <param name="id">ID do cliente.</param>
        /// <response code="200">Cliente encontrado.</response>
        /// <response code="403">Acesso negado.</response>
        /// <response code="404">Cliente não encontrado.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClientDto>> GetClientById(Guid id)
        {
            try
            {
                var userId = GetUserId();
                if (!await IsContadorAsync(userId))
                    return Forbid();

                var client = await _clientService.GetClientByIdAsync(id, userId);
                return Ok(client);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cliente {ClientId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Cria um novo cliente para o Contador.
        /// Valida limite de clientes conforme o plano (Basic: máx 3; Pro: ilimitado).
        /// </summary>
        /// <param name="dto">Dados do novo cliente.</param>
        /// <response code="201">Cliente criado com sucesso.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="402">Limite de clientes do plano Basic atingido.</response>
        /// <response code="403">Acesso negado — endpoint exclusivo para Contadores.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ClientDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ClientDto>> CreateClient([FromBody] CreateClientDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetUserId();
                var user = await _authService.GetUserByIdAsync(userId);
                if (user is null)
                    return Unauthorized(new { message = "Usuário não encontrado" });

                if (user.ProfileType != ProfileType.Contador)
                    return Forbid();

                var client = await _clientService.CreateClientAsync(userId, dto, user.PlanType);
                _logger.LogInformation("Cliente criado: {ClientId} por Contador {UserId}", client.Id, userId);

                return CreatedAtAction(nameof(GetClientById), new { id = client.Id }, client);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status402PaymentRequired, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar cliente para usuário {UserId}", GetUserId());
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Atualiza os dados de um cliente existente do Contador.
        /// </summary>
        /// <param name="id">ID do cliente a atualizar.</param>
        /// <param name="dto">Dados para atualização. Apenas campos preenchidos são atualizados.</param>
        /// <response code="200">Cliente atualizado com sucesso.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="403">Acesso negado.</response>
        /// <response code="404">Cliente não encontrado.</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClientDto>> UpdateClient(Guid id, [FromBody] UpdateClientDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetUserId();
                if (!await IsContadorAsync(userId))
                    return Forbid();

                var client = await _clientService.UpdateClientAsync(id, userId, dto);
                _logger.LogInformation("Cliente atualizado: {ClientId} por Contador {UserId}", id, userId);

                return Ok(client);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar cliente {ClientId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Arquiva um cliente (soft delete — IsActive=false).
        /// Os lançamentos do cliente são preservados no histórico.
        /// </summary>
        /// <param name="id">ID do cliente a arquivar.</param>
        /// <response code="204">Cliente arquivado com sucesso.</response>
        /// <response code="403">Acesso negado.</response>
        /// <response code="404">Cliente não encontrado.</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ArchiveClient(Guid id)
        {
            try
            {
                var userId = GetUserId();
                if (!await IsContadorAsync(userId))
                    return Forbid();

                await _clientService.ArchiveClientAsync(id, userId);
                _logger.LogInformation("Cliente arquivado: {ClientId} por Contador {UserId}", id, userId);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao arquivar cliente {ClientId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // MÉTODOS PRIVADOS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifica se o usuário autenticado possui o perfil de Contador.
        /// Usado como guard nos endpoints exclusivos do contador.
        /// </summary>
        private async Task<bool> IsContadorAsync(Guid userId)
        {
            var user = await _authService.GetUserByIdAsync(userId);
            return user?.ProfileType == ProfileType.Contador;
        }
    }
}
