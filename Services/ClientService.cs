using EconomyBackPortifolio.Data;
using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Enums;
using EconomyBackPortifolio.Models;
using Microsoft.EntityFrameworkCore;

namespace EconomyBackPortifolio.Services
{
    /// <summary>
    /// Implementação do serviço de gerenciamento de clientes para o perfil Contador.
    ///
    /// Responsabilidades:
    ///   - CRUD de clientes do escritório do contador
    ///   - Validação de limites por plano (Basic: máx 3 clientes ativos)
    ///   - Isolamento de dados por contador (sem acesso cruzado entre contadores)
    /// </summary>
    public class ClientService : IClientService
    {
        private const int BasicPlanClientLimit = 3;

        private readonly ApplicationDbContext _context;

        public ClientService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ClientDto>> GetClientsAsync(Guid accountantUserId, bool onlyActive = true)
        {
            var query = _context.Clients
                .Where(c => c.AccountantUserId == accountantUserId);

            if (onlyActive)
                query = query.Where(c => c.IsActive);

            var clients = await query
                .OrderBy(c => c.Name)
                .ToListAsync();

            var clientIds = clients.Select(c => c.Id).ToList();

            // Contabiliza lançamentos por cliente em uma única query.
            var lancamentosPorCliente = await _context.FinancialEntries
                .Where(e => e.ClientId != null && clientIds.Contains(e.ClientId!.Value))
                .GroupBy(e => e.ClientId!.Value)
                .Select(g => new { ClientId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.ClientId, x => x.Total);

            return clients.Select(c => MapToDto(c, lancamentosPorCliente.GetValueOrDefault(c.Id, 0)));
        }

        /// <inheritdoc />
        public async Task<ClientDto> GetClientByIdAsync(Guid clientId, Guid accountantUserId)
        {
            var client = await FindClientAsync(clientId, accountantUserId);

            var totalLancamentos = await _context.FinancialEntries
                .CountAsync(e => e.ClientId == clientId);

            return MapToDto(client, totalLancamentos);
        }

        /// <inheritdoc />
        public async Task<ClientDto> CreateClientAsync(
            Guid accountantUserId,
            CreateClientDto dto,
            PlanType accountantPlan)
        {
            if (accountantPlan == PlanType.Basic)
            {
                var activeCount = await _context.Clients
                    .CountAsync(c => c.AccountantUserId == accountantUserId && c.IsActive);

                if (activeCount >= BasicPlanClientLimit)
                    throw new InvalidOperationException(
                        $"O plano Basic permite até {BasicPlanClientLimit} clientes ativos. " +
                        "Arquive um cliente existente ou faça upgrade para o plano Pro.");
            }

            var client = new Client
            {
                Id = Guid.NewGuid(),
                AccountantUserId = accountantUserId,
                Name = dto.Name.Trim(),
                Email = dto.Email?.Trim().ToLowerInvariant(),
                Phone = dto.Phone?.Trim(),
                Document = dto.Document?.Trim(),
                Notes = dto.Notes?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return MapToDto(client, 0);
        }

        /// <inheritdoc />
        public async Task<ClientDto> UpdateClientAsync(
            Guid clientId,
            Guid accountantUserId,
            UpdateClientDto dto)
        {
            var client = await FindClientAsync(clientId, accountantUserId);

            if (dto.Name is not null)
                client.Name = dto.Name.Trim();

            if (dto.Email is not null)
                client.Email = dto.Email.Trim().ToLowerInvariant();

            if (dto.Phone is not null)
                client.Phone = dto.Phone.Trim();

            if (dto.Document is not null)
                client.Document = dto.Document.Trim();

            if (dto.Notes is not null)
                client.Notes = dto.Notes.Trim();

            if (dto.IsActive.HasValue)
                client.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();

            var totalLancamentos = await _context.FinancialEntries
                .CountAsync(e => e.ClientId == clientId);

            return MapToDto(client, totalLancamentos);
        }

        /// <inheritdoc />
        public async Task ArchiveClientAsync(Guid clientId, Guid accountantUserId)
        {
            var client = await FindClientAsync(clientId, accountantUserId);
            client.IsActive = false;
            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // MÉTODOS PRIVADOS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Busca um cliente pelo ID validando que pertence ao contador.
        /// Lança KeyNotFoundException se não encontrar.
        /// </summary>
        private async Task<Client> FindClientAsync(Guid clientId, Guid accountantUserId)
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == clientId && c.AccountantUserId == accountantUserId);

            return client ?? throw new KeyNotFoundException($"Cliente {clientId} não encontrado.");
        }

        /// <summary>
        /// Mapeia um Client para ClientDto.
        /// </summary>
        private static ClientDto MapToDto(Client client, int totalLancamentos) => new()
        {
            Id = client.Id,
            Name = client.Name,
            Email = client.Email,
            Phone = client.Phone,
            Document = client.Document,
            Notes = client.Notes,
            IsActive = client.IsActive,
            CreatedAt = client.CreatedAt,
            TotalLancamentos = totalLancamentos
        };
    }
}
