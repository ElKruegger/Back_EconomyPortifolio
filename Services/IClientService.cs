using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Enums;

namespace EconomyBackPortifolio.Services
{
    /// <summary>
    /// Contrato do serviço de gerenciamento de clientes para o perfil Contador.
    ///
    /// O cliente (Client) representa uma entidade gerenciada pelo contador:
    /// uma pessoa física ou empresa cujos dados financeiros são administrados pelo contador.
    ///
    /// Limites por plano:
    ///   - Basic: até 3 clientes ativos simultaneamente
    ///   - Pro: clientes ilimitados
    ///
    /// Isolamento:
    ///   - Um contador acessa apenas seus próprios clientes.
    ///   - Os lançamentos de um cliente são isolados dos outros clientes do mesmo contador.
    /// </summary>
    public interface IClientService
    {
        /// <summary>
        /// Lista os clientes do contador autenticado.
        /// </summary>
        /// <param name="accountantUserId">ID do usuário Contador autenticado.</param>
        /// <param name="onlyActive">Se true, retorna apenas clientes ativos. Padrão: true.</param>
        /// <returns>Lista de ClientDto ordenada por Name asc.</returns>
        Task<IEnumerable<ClientDto>> GetClientsAsync(Guid accountantUserId, bool onlyActive = true);

        /// <summary>
        /// Retorna um cliente específico do contador.
        /// </summary>
        /// <param name="clientId">ID do cliente.</param>
        /// <param name="accountantUserId">ID do Contador autenticado.</param>
        /// <returns>ClientDto se encontrado.</returns>
        /// <exception cref="KeyNotFoundException">Se o cliente não existir ou não pertencer ao contador.</exception>
        Task<ClientDto> GetClientByIdAsync(Guid clientId, Guid accountantUserId);

        /// <summary>
        /// Cria um novo cliente para o contador.
        /// Valida o limite de clientes ativos conforme o plano do contador.
        /// </summary>
        /// <param name="accountantUserId">ID do Contador autenticado.</param>
        /// <param name="dto">Dados do novo cliente.</param>
        /// <param name="accountantPlan">Plano atual do contador (Basic ou Pro).</param>
        /// <returns>ClientDto do cliente criado.</returns>
        /// <exception cref="InvalidOperationException">Se o limite de clientes do plano Basic foi atingido.</exception>
        Task<ClientDto> CreateClientAsync(Guid accountantUserId, CreateClientDto dto, PlanType accountantPlan);

        /// <summary>
        /// Atualiza os dados de um cliente existente.
        /// </summary>
        /// <param name="clientId">ID do cliente a atualizar.</param>
        /// <param name="accountantUserId">ID do Contador autenticado.</param>
        /// <param name="dto">Dados para atualização (apenas campos preenchidos são atualizados).</param>
        /// <returns>ClientDto atualizado.</returns>
        /// <exception cref="KeyNotFoundException">Se o cliente não existir ou não pertencer ao contador.</exception>
        Task<ClientDto> UpdateClientAsync(Guid clientId, Guid accountantUserId, UpdateClientDto dto);

        /// <summary>
        /// Arquiva um cliente (soft delete — IsActive=false).
        /// Os lançamentos do cliente são preservados no histórico.
        /// </summary>
        /// <param name="clientId">ID do cliente a arquivar.</param>
        /// <param name="accountantUserId">ID do Contador autenticado.</param>
        /// <exception cref="KeyNotFoundException">Se o cliente não existir ou não pertencer ao contador.</exception>
        Task ArchiveClientAsync(Guid clientId, Guid accountantUserId);
    }
}
