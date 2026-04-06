using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Enums;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EconomyBackPortifolio.Controllers
{
    /// <summary>
    /// Controller responsável pelo gerenciamento de categorias de lançamentos financeiros.
    ///
    /// Endpoints públicos (leitura): acessíveis por qualquer usuário autenticado.
    /// Endpoints de escrita: criação e edição de categorias requerem plano Pro.
    /// Categorias do sistema (IsSystem=true) são somente leitura.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriesController : BaseApiController
    {
        private readonly ICategoryService _categoryService;
        private readonly IAuthService _authService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(
            ICategoryService categoryService,
            IAuthService authService,
            ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Lista as categorias disponíveis para o usuário autenticado.
        /// Inclui categorias do sistema + categorias personalizadas do próprio usuário.
        /// </summary>
        /// <param name="type">Filtra por tipo: 0=Receita, 1=Despesa, 2=Ambas. Null=todas.</param>
        /// <param name="onlyActive">Se true (padrão), retorna apenas categorias ativas.</param>
        /// <response code="200">Lista de categorias retornada com sucesso.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(
            [FromQuery] CategoryType? type = null,
            [FromQuery] bool onlyActive = true)
        {
            try
            {
                var userId = GetUserId();
                var categories = await _categoryService.GetCategoriesAsync(userId, type, onlyActive);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar categorias para usuário {UserId}", GetUserId());
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Retorna uma categoria específica pelo ID.
        /// </summary>
        /// <param name="id">ID da categoria.</param>
        /// <response code="200">Categoria encontrada.</response>
        /// <response code="404">Categoria não encontrada ou sem acesso.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CategoryDto>> GetCategoryById(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var category = await _categoryService.GetCategoryByIdAsync(id, userId);
                return Ok(category);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar categoria {CategoryId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Cria uma nova categoria personalizada para o usuário.
        /// Disponível apenas para usuários com plano Pro.
        /// </summary>
        /// <param name="dto">Dados da nova categoria.</param>
        /// <response code="201">Categoria criada com sucesso.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="402">Plano Pro necessário para criar categorias personalizadas.</response>
        [HttpPost]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetUserId();
                var user = await _authService.GetUserByIdAsync(userId);
                if (user is null)
                    return Unauthorized(new { message = "Usuário não encontrado" });

                var category = await _categoryService.CreateCategoryAsync(userId, dto, user.PlanType);
                _logger.LogInformation("Categoria criada: {CategoryId} por usuário {UserId}", category.Id, userId);

                return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status402PaymentRequired, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar categoria para usuário {UserId}", GetUserId());
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Atualiza uma categoria personalizada do usuário.
        /// Categorias do sistema não podem ser editadas.
        /// </summary>
        /// <param name="id">ID da categoria a atualizar.</param>
        /// <param name="dto">Dados para atualização (apenas campos preenchidos são atualizados).</param>
        /// <response code="200">Categoria atualizada com sucesso.</response>
        /// <response code="404">Categoria não encontrada ou sem acesso.</response>
        /// <response code="400">Tentativa de editar categoria do sistema.</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CategoryDto>> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetUserId();
                var category = await _categoryService.UpdateCategoryAsync(id, userId, dto);
                _logger.LogInformation("Categoria atualizada: {CategoryId} por usuário {UserId}", id, userId);

                return Ok(category);
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
                _logger.LogError(ex, "Erro ao atualizar categoria {CategoryId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Desativa uma categoria personalizada do usuário (soft delete).
        /// Categorias do sistema não podem ser desativadas.
        /// Lançamentos existentes são preservados.
        /// </summary>
        /// <param name="id">ID da categoria a desativar.</param>
        /// <response code="204">Categoria desativada com sucesso.</response>
        /// <response code="404">Categoria não encontrada ou sem acesso.</response>
        /// <response code="400">Tentativa de desativar categoria do sistema.</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            try
            {
                var userId = GetUserId();
                await _categoryService.DeleteCategoryAsync(id, userId);
                _logger.LogInformation("Categoria desativada: {CategoryId} por usuário {UserId}", id, userId);

                return NoContent();
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
                _logger.LogError(ex, "Erro ao desativar categoria {CategoryId}", id);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }
    }
}
