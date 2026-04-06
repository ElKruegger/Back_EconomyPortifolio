using EconomyBackPortifolio.Data;
using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Enums;
using EconomyBackPortifolio.Models;
using Microsoft.EntityFrameworkCore;

namespace EconomyBackPortifolio.Services
{
    /// <summary>
    /// Implementação do serviço de gerenciamento de lançamentos financeiros.
    ///
    /// Responsabilidades:
    ///   - CRUD completo de lançamentos de receitas e despesas
    ///   - Geração de resumos financeiros por período e categoria
    ///   - Isolamento de dados por usuário (e por cliente para Contadores)
    /// </summary>
    public class FinancialEntryService : IFinancialEntryService
    {
        private readonly ApplicationDbContext _context;

        public FinancialEntryService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<FinancialEntryDto>> GetEntriesAsync(Guid userId, FinancialEntryFilterDto filter)
        {
            var query = BuildBaseQuery(userId);
            query = ApplyFilters(query, filter);

            var entries = await query
                .Include(e => e.Category)
                .Include(e => e.Client)
                .OrderByDescending(e => e.EntryDate)
                .ThenByDescending(e => e.CreatedAt)
                .ToListAsync();

            return entries.Select(MapToDto);
        }

        /// <inheritdoc />
        public async Task<FinancialEntryDto> GetEntryByIdAsync(Guid entryId, Guid userId)
        {
            var entry = await FindEntryAsync(entryId, userId);
            return MapToDto(entry);
        }

        /// <inheritdoc />
        public async Task<FinancialEntryDto> CreateEntryAsync(Guid userId, CreateFinancialEntryDto dto)
        {
            await ValidateCategoryAsync(dto.CategoryId, dto.Type, userId);

            if (dto.ClientId.HasValue)
                await ValidateClientOwnershipAsync(dto.ClientId.Value, userId);

            var entry = new FinancialEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ClientId = dto.ClientId,
                CategoryId = dto.CategoryId,
                Type = dto.Type,
                Amount = dto.Amount,
                Description = dto.Description.Trim(),
                EntryDate = dto.EntryDate?.ToUniversalTime() ?? DateTime.UtcNow,
                IsRecurring = dto.IsRecurring,
                RecurrenceInterval = dto.IsRecurring ? dto.RecurrenceInterval?.Trim() : null,
                Notes = dto.Notes?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            _context.FinancialEntries.Add(entry);
            await _context.SaveChangesAsync();

            // Recarrega com navigation properties para montar o DTO corretamente.
            await _context.Entry(entry).Reference(e => e.Category).LoadAsync();
            await _context.Entry(entry).Reference(e => e.Client).LoadAsync();

            return MapToDto(entry);
        }

        /// <inheritdoc />
        public async Task<FinancialEntryDto> UpdateEntryAsync(Guid entryId, Guid userId, UpdateFinancialEntryDto dto)
        {
            var entry = await FindEntryAsync(entryId, userId);

            if (dto.CategoryId.HasValue)
            {
                await ValidateCategoryAsync(dto.CategoryId.Value, entry.Type, userId);
                entry.CategoryId = dto.CategoryId.Value;
            }

            if (dto.Amount.HasValue)
                entry.Amount = dto.Amount.Value;

            if (dto.Description is not null)
                entry.Description = dto.Description.Trim();

            if (dto.EntryDate.HasValue)
                entry.EntryDate = dto.EntryDate.Value.ToUniversalTime();

            if (dto.IsRecurring.HasValue)
            {
                entry.IsRecurring = dto.IsRecurring.Value;
                if (!entry.IsRecurring)
                    entry.RecurrenceInterval = null;
            }

            if (dto.RecurrenceInterval is not null && entry.IsRecurring)
                entry.RecurrenceInterval = dto.RecurrenceInterval.Trim();

            if (dto.Notes is not null)
                entry.Notes = dto.Notes.Trim();

            entry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _context.Entry(entry).Reference(e => e.Category).LoadAsync();
            await _context.Entry(entry).Reference(e => e.Client).LoadAsync();

            return MapToDto(entry);
        }

        /// <inheritdoc />
        public async Task DeleteEntryAsync(Guid entryId, Guid userId)
        {
            var entry = await FindEntryAsync(entryId, userId);
            _context.FinancialEntries.Remove(entry);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<FinancialSummaryDto> GetSummaryAsync(Guid userId, FinancialEntryFilterDto filter)
        {
            var query = BuildBaseQuery(userId);
            query = ApplyFilters(query, filter);

            var entries = await query
                .Include(e => e.Category)
                .ToListAsync();

            var totalReceitas = entries
                .Where(e => e.Type == EntryType.Receita)
                .Sum(e => e.Amount);

            var totalDespesas = entries
                .Where(e => e.Type == EntryType.Despesa)
                .Sum(e => e.Amount);

            var porCategoria = entries
                .GroupBy(e => new { e.CategoryId, e.Category.Name, e.Category.Icon, e.Category.Color, e.Type })
                .Select(g =>
                {
                    var totalGrupo = g.Sum(e => e.Amount);
                    var totalTipo = g.Key.Type == EntryType.Receita ? totalReceitas : totalDespesas;
                    return new CategorySummaryDto
                    {
                        CategoryId = g.Key.CategoryId,
                        CategoryName = g.Key.Name,
                        Icon = g.Key.Icon,
                        Color = g.Key.Color,
                        Type = g.Key.Type,
                        Total = totalGrupo,
                        Percentual = totalTipo > 0 ? Math.Round(totalGrupo / totalTipo * 100, 2) : 0,
                        Quantidade = g.Count()
                    };
                })
                .OrderByDescending(c => c.Total)
                .ToList();

            var historicoMensal = entries
                .GroupBy(e => new { e.EntryDate.Year, e.EntryDate.Month })
                .Select(g => new MonthlyFinancialDto
                {
                    Ano = g.Key.Year,
                    Mes = g.Key.Month,
                    Label = $"{new DateTime(g.Key.Year, g.Key.Month, 1):MMM/yy}",
                    TotalReceitas = g.Where(e => e.Type == EntryType.Receita).Sum(e => e.Amount),
                    TotalDespesas = g.Where(e => e.Type == EntryType.Despesa).Sum(e => e.Amount),
                    Saldo = g.Where(e => e.Type == EntryType.Receita).Sum(e => e.Amount)
                           - g.Where(e => e.Type == EntryType.Despesa).Sum(e => e.Amount)
                })
                .OrderBy(m => m.Ano)
                .ThenBy(m => m.Mes)
                .ToList();

            return new FinancialSummaryDto
            {
                TotalReceitas = totalReceitas,
                TotalDespesas = totalDespesas,
                Saldo = totalReceitas - totalDespesas,
                QuantidadeReceitas = entries.Count(e => e.Type == EntryType.Receita),
                QuantidadeDespesas = entries.Count(e => e.Type == EntryType.Despesa),
                PorCategoria = porCategoria,
                HistoricoMensal = historicoMensal
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // MÉTODOS PRIVADOS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Retorna a query base de lançamentos do usuário,
        /// incluindo lançamentos de clientes do contador.
        /// </summary>
        private IQueryable<FinancialEntry> BuildBaseQuery(Guid userId)
        {
            return _context.FinancialEntries
                .Where(e => e.UserId == userId);
        }

        /// <summary>
        /// Aplica os filtros do DTO à query de lançamentos.
        /// </summary>
        private static IQueryable<FinancialEntry> ApplyFilters(
            IQueryable<FinancialEntry> query,
            FinancialEntryFilterDto filter)
        {
            if (filter.Type.HasValue)
                query = query.Where(e => e.Type == filter.Type.Value);

            if (filter.CategoryId.HasValue)
                query = query.Where(e => e.CategoryId == filter.CategoryId.Value);

            if (filter.ClientId.HasValue)
                query = query.Where(e => e.ClientId == filter.ClientId.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(e => e.EntryDate >= filter.FromDate.Value.ToUniversalTime());

            if (filter.ToDate.HasValue)
                query = query.Where(e => e.EntryDate <= filter.ToDate.Value.ToUniversalTime());

            if (filter.IsRecurring.HasValue)
                query = query.Where(e => e.IsRecurring == filter.IsRecurring.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(e =>
                    e.Description.ToLower().Contains(search) ||
                    (e.Notes != null && e.Notes.ToLower().Contains(search)));
            }

            return query;
        }

        /// <summary>
        /// Busca um lançamento pelo ID validando que pertence ao usuário.
        /// Inclui Category e Client para o mapeamento.
        /// </summary>
        private async Task<FinancialEntry> FindEntryAsync(Guid entryId, Guid userId)
        {
            var entry = await _context.FinancialEntries
                .Include(e => e.Category)
                .Include(e => e.Client)
                .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId);

            return entry ?? throw new KeyNotFoundException($"Lançamento {entryId} não encontrado.");
        }

        /// <summary>
        /// Valida que a categoria existe, está ativa e é compatível com o tipo de lançamento.
        /// Aceita categorias do sistema ou do próprio usuário.
        /// </summary>
        private async Task ValidateCategoryAsync(Guid categoryId, EntryType entryType, Guid userId)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == categoryId && (c.IsSystem || c.UserId == userId));

            if (category is null)
                throw new KeyNotFoundException($"Categoria {categoryId} não encontrada.");

            if (!category.IsActive)
                throw new InvalidOperationException("A categoria selecionada está inativa.");

            // Valida compatibilidade de tipo (ex: não usar categoria de Receita em Despesa).
            if (category.Type != CategoryType.Ambas && (int)category.Type != (int)entryType)
                throw new InvalidOperationException(
                    $"A categoria '{category.Name}' é do tipo {category.Type} e não pode ser usada em um lançamento de {entryType}.");
        }

        /// <summary>
        /// Valida que o ClientId pertence ao contador (userId).
        /// Previne que um contador acesse dados de clientes de outro contador.
        /// </summary>
        private async Task ValidateClientOwnershipAsync(Guid clientId, Guid userId)
        {
            var exists = await _context.Clients
                .AnyAsync(c => c.Id == clientId && c.AccountantUserId == userId);

            if (!exists)
                throw new KeyNotFoundException($"Cliente {clientId} não encontrado.");
        }

        /// <summary>
        /// Mapeia um FinancialEntry para FinancialEntryDto.
        /// Depende que Category e Client estejam carregados (Include ou Load).
        /// </summary>
        private static FinancialEntryDto MapToDto(FinancialEntry entry) => new()
        {
            Id = entry.Id,
            ClientId = entry.ClientId,
            ClientName = entry.Client?.Name,
            CategoryId = entry.CategoryId,
            CategoryName = entry.Category.Name,
            CategoryIcon = entry.Category.Icon,
            CategoryColor = entry.Category.Color,
            Type = entry.Type,
            Amount = entry.Amount,
            Description = entry.Description,
            EntryDate = entry.EntryDate,
            IsRecurring = entry.IsRecurring,
            RecurrenceInterval = entry.RecurrenceInterval,
            Notes = entry.Notes,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        };
    }
}
