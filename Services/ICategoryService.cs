using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Enums;

namespace EconomyBackPortifolio.Services
{
    /// <summary>
    /// Contrato do serviço de gerenciamento de categorias de lançamentos financeiros.
    ///
    /// Categorias do sistema (IsSystem=true) são somente leitura para o usuário.
    /// Categorias personalizadas (IsSystem=false) só podem ser criadas por usuários com plano Pro.
    /// </summary>
    public interface ICategoryService
    {
        /// <summary>
        /// Retorna todas as categorias disponíveis para o usuário:
        /// as categorias do sistema + as categorias personalizadas do próprio usuário.
        /// </summary>
        /// <param name="userId">ID do usuário autenticado.</param>
        /// <param name="type">Filtra pelo tipo de categoria (Receita, Despesa ou Ambas). Null = todas.</param>
        /// <param name="onlyActive">Se true, retorna apenas categorias ativas. Padrão: true.</param>
        /// <returns>Lista de CategoryDto ordenada por IsSystem desc, Name asc.</returns>
        Task<IEnumerable<CategoryDto>> GetCategoriesAsync(Guid userId, CategoryType? type = null, bool onlyActive = true);

        /// <summary>
        /// Retorna uma categoria específica pelo ID.
        /// O usuário só pode acessar categorias do sistema ou as suas próprias.
        /// </summary>
        /// <param name="categoryId">ID da categoria.</param>
        /// <param name="userId">ID do usuário autenticado (para validar acesso).</param>
        /// <returns>CategoryDto se encontrada e acessível.</returns>
        /// <exception cref="KeyNotFoundException">Se a categoria não existir ou não pertencer ao usuário.</exception>
        Task<CategoryDto> GetCategoryByIdAsync(Guid categoryId, Guid userId);

        /// <summary>
        /// Cria uma categoria personalizada para o usuário.
        /// Disponível apenas para usuários com plano Pro.
        /// </summary>
        /// <param name="userId">ID do usuário autenticado.</param>
        /// <param name="dto">Dados da nova categoria.</param>
        /// <param name="userPlan">Plano atual do usuário (Basic ou Pro).</param>
        /// <returns>CategoryDto da categoria criada.</returns>
        /// <exception cref="InvalidOperationException">Se o usuário estiver no plano Basic.</exception>
        Task<CategoryDto> CreateCategoryAsync(Guid userId, CreateCategoryDto dto, PlanType userPlan);

        /// <summary>
        /// Atualiza uma categoria personalizada do usuário.
        /// Categorias do sistema (IsSystem=true) não podem ser editadas.
        /// </summary>
        /// <param name="categoryId">ID da categoria a atualizar.</param>
        /// <param name="userId">ID do usuário autenticado (para validar propriedade).</param>
        /// <param name="dto">Dados para atualização (apenas campos preenchidos são atualizados).</param>
        /// <returns>CategoryDto atualizada.</returns>
        /// <exception cref="KeyNotFoundException">Se a categoria não existir ou não pertencer ao usuário.</exception>
        /// <exception cref="InvalidOperationException">Se a categoria for do sistema.</exception>
        Task<CategoryDto> UpdateCategoryAsync(Guid categoryId, Guid userId, UpdateCategoryDto dto);

        /// <summary>
        /// Desativa uma categoria personalizada do usuário (soft delete).
        /// Categorias do sistema não podem ser desativadas.
        /// Categorias com lançamentos existentes são apenas desativadas, não excluídas.
        /// </summary>
        /// <param name="categoryId">ID da categoria a desativar.</param>
        /// <param name="userId">ID do usuário autenticado.</param>
        /// <exception cref="KeyNotFoundException">Se a categoria não existir ou não pertencer ao usuário.</exception>
        /// <exception cref="InvalidOperationException">Se a categoria for do sistema.</exception>
        Task DeleteCategoryAsync(Guid categoryId, Guid userId);
    }
}
