using EconomyBackPortifolio.Data;
using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Enums;
using EconomyBackPortifolio.Models;
using Microsoft.EntityFrameworkCore;

namespace EconomyBackPortifolio.Services
{
    /// <summary>
    /// Implementação do serviço de gerenciamento de categorias de lançamentos financeiros.
    ///
    /// Regras de negócio:
    ///   - Categorias do sistema (IsSystem=true) são somente leitura para o usuário.
    ///   - Categorias personalizadas só podem ser criadas por usuários Pro.
    ///   - Ao deletar, a categoria é apenas desativada (soft delete) para preservar histórico.
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync(
            Guid userId,
            CategoryType? type = null,
            bool onlyActive = true)
        {
            var query = _context.Categories
                .Where(c => c.IsSystem || c.UserId == userId);

            if (type.HasValue)
                query = query.Where(c => c.Type == type.Value || c.Type == CategoryType.Ambas);

            if (onlyActive)
                query = query.Where(c => c.IsActive);

            var categories = await query
                .OrderByDescending(c => c.IsSystem)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return categories.Select(MapToDto);
        }

        /// <inheritdoc />
        public async Task<CategoryDto> GetCategoryByIdAsync(Guid categoryId, Guid userId)
        {
            var category = await FindAccessibleCategoryAsync(categoryId, userId);
            return MapToDto(category);
        }

        /// <inheritdoc />
        public async Task<CategoryDto> CreateCategoryAsync(Guid userId, CreateCategoryDto dto, PlanType userPlan)
        {
            if (userPlan == PlanType.Basic)
                throw new InvalidOperationException("Categorias personalizadas estão disponíveis apenas no plano Pro. Faça upgrade para continuar.");

            var category = new Category
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = dto.Name.Trim(),
                Type = dto.Type,
                Icon = dto.Icon?.Trim(),
                Color = dto.Color?.Trim(),
                IsSystem = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return MapToDto(category);
        }

        /// <inheritdoc />
        public async Task<CategoryDto> UpdateCategoryAsync(Guid categoryId, Guid userId, UpdateCategoryDto dto)
        {
            var category = await FindUserOwnedCategoryAsync(categoryId, userId);

            if (dto.Name is not null)
                category.Name = dto.Name.Trim();

            if (dto.Icon is not null)
                category.Icon = dto.Icon.Trim();

            if (dto.Color is not null)
                category.Color = dto.Color.Trim();

            if (dto.IsActive.HasValue)
                category.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();

            return MapToDto(category);
        }

        /// <inheritdoc />
        public async Task DeleteCategoryAsync(Guid categoryId, Guid userId)
        {
            var category = await FindUserOwnedCategoryAsync(categoryId, userId);

            // Soft delete: desativa a categoria para preservar o histórico de lançamentos.
            category.IsActive = false;
            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // MÉTODOS PRIVADOS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Busca uma categoria que seja do sistema ou pertença ao usuário.
        /// Lança KeyNotFoundException se não encontrar.
        /// </summary>
        private async Task<Category> FindAccessibleCategoryAsync(Guid categoryId, Guid userId)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == categoryId && (c.IsSystem || c.UserId == userId));

            return category ?? throw new KeyNotFoundException($"Categoria {categoryId} não encontrada.");
        }

        /// <summary>
        /// Busca uma categoria que pertença ao usuário (não do sistema).
        /// Lança exceções adequadas para categorias inexistentes ou do sistema.
        /// </summary>
        private async Task<Category> FindUserOwnedCategoryAsync(Guid categoryId, Guid userId)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == categoryId && (c.IsSystem || c.UserId == userId));

            if (category is null)
                throw new KeyNotFoundException($"Categoria {categoryId} não encontrada.");

            if (category.IsSystem)
                throw new InvalidOperationException("Categorias do sistema não podem ser editadas ou excluídas.");

            if (category.UserId != userId)
                throw new KeyNotFoundException($"Categoria {categoryId} não encontrada.");

            return category;
        }

        /// <summary>
        /// Mapeia um objeto Category para CategoryDto.
        /// </summary>
        private static CategoryDto MapToDto(Category category) => new()
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type,
            Icon = category.Icon,
            Color = category.Color,
            IsSystem = category.IsSystem,
            IsActive = category.IsActive
        };
    }
}
