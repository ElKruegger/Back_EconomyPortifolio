using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EconomyBackPortifolio.Controllers
{
    /// <summary>
    /// Controller responsável por todas as operações financeiras do usuário:
    /// depósito, conversão de moeda, compra/venda de ativos e histórico de transações.
    /// Todos os endpoints requerem autenticação JWT.
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
        /// Lista transações do usuário com filtros opcionais.
        /// </summary>
        /// <param name="type">Tipo da transação: DEPOSIT, BUY, SELL, CONVERSION.</param>
        /// <param name="currency">Moeda da wallet (ex: BRL, USD).</param>
        /// <param name="assetId">ID do ativo para filtrar.</param>
        /// <param name="fromDate">Data inicial do período.</param>
        /// <param name="toDate">Data final do período.</param>
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
                _logger.LogError(ex, "Erro ao listar transações do usuário");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Retorna o resumo consolidado de transações agrupado por tipo e por mês.
        /// Usado nos gráficos do dashboard.
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
                _logger.LogError(ex, "Erro ao obter resumo de transações");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Obtém uma transação específica por ID.
        /// Retorna 404 se a transação não existir ou não pertencer ao usuário autenticado.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionDto>> GetTransaction(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var transaction = await _transactionService.GetTransactionByIdAsync(id, userId);

                if (transaction == null)
                    return NotFound(new { message = "Transação não encontrada" });

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter transação: {TransactionId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Realiza um depósito em BRL na wallet padrão do usuário.
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

                _logger.LogInformation("Depósito realizado: {Amount} BRL para usuário {UserId}", depositDto.Amount, userId);

                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Erro ao realizar depósito: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao realizar depósito");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Converte um valor de uma moeda para outra entre as wallets do usuário.
        /// O campo ExchangeRate deve ser fornecido pelo front-end com a cotação atual.
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

                _logger.LogInformation("Conversão realizada: {Amount} {From} → {To} - usuário {UserId}",
                    convertDto.Amount, convertDto.FromCurrency, convertDto.ToCurrency, userId);

                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Dados inválidos na conversão: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Erro ao realizar conversão: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao realizar conversão");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Compra um ativo investindo o valor da wallet correspondente.
        /// O preço deve ser fornecido pelo front-end com a cotação atual do ativo.
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

                _logger.LogInformation("Compra realizada: {Quantity} de {AssetId} - usuário {UserId}",
                    buyDto.Quantity, buyDto.AssetId, userId);

                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Erro ao realizar compra: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao realizar compra");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Vende um ativo creditando o valor na wallet e atualizando a posição.
        /// Se toda a quantidade for vendida, a posição é removida automaticamente.
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

                _logger.LogInformation("Venda realizada: {Quantity} de {AssetId} - usuário {UserId}",
                    sellDto.Quantity, sellDto.AssetId, userId);

                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Erro ao realizar venda: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao realizar venda");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }
    }
}
