using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EconomyBackPortifolio.Controllers
{
    /// <summary>
    /// Controller responsável pelo gerenciamento de wallets (carteiras) do usuário.
    /// Cada wallet representa um saldo em uma moeda específica (BRL, USD, BTC etc).
    /// Todos os endpoints requerem autenticação JWT.
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
        /// Lista todas as wallets do usuário autenticado ordenadas por moeda.
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
                _logger.LogError(ex, "Erro ao listar wallets do usuário");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Obtém uma wallet específica por ID.
        /// Retorna 404 se não encontrada ou se não pertencer ao usuário autenticado.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<WalletDto>> GetWallet(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var wallet = await _walletService.GetWalletByIdAsync(id, userId);

                if (wallet == null)
                    return NotFound(new { message = "Wallet não encontrada" });

                return Ok(wallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter wallet: {WalletId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Obtém a wallet do usuário para uma moeda específica.
        /// Útil para verificar o saldo antes de uma operação.
        /// </summary>
        /// <param name="currency">Código da moeda (ex: BRL, USD, BTC).</param>
        [HttpGet("currency/{currency}")]
        public async Task<ActionResult<WalletDto>> GetWalletByCurrency(string currency)
        {
            try
            {
                var userId = GetUserId();
                var wallet = await _walletService.GetWalletByCurrencyAsync(userId, currency);

                if (wallet == null)
                    return NotFound(new { message = $"Wallet não encontrada para a moeda {currency}" });

                return Ok(wallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter wallet por moeda: {Currency}", currency);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Cria uma nova wallet para o usuário na moeda informada.
        /// Cada usuário pode ter apenas uma wallet por moeda.
        /// A wallet BRL é criada automaticamente no cadastro.
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

                _logger.LogInformation("Wallet criada: {Currency} para usuário {UserId}", createWalletDto.Currency, userId);

                return CreatedAtAction(nameof(GetWallet), new { id = wallet.Id }, wallet);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Moeda inválida ao criar wallet: {Currency}", createWalletDto.Currency);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Tentativa de criar wallet duplicada: {Currency}", createWalletDto.Currency);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar wallet: {Currency}", createWalletDto.Currency);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }
    }
}
