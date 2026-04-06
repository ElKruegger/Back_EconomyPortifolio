using EconomyBackPortifolio.DTOs;

namespace EconomyBackPortifolio.Services
{
    /// <summary>
    /// Contrato do serviço de gerenciamento de lançamentos financeiros (receitas e despesas).
    ///
    /// Lançamentos são o núcleo do controle financeiro do Economy Portfolio.
    /// Diferentemente das Transactions (investimentos), os FinancialEntries podem ser editados.
    ///
    /// Isolamento de dados:
    ///   - Usuário PessoaFisica/Empresa: acessa apenas seus próprios lançamentos.
    ///   - Contador: acessa lançamentos seus e de seus clientes (filtrado por ClientId).
    /// </summary>
    public interface IFinancialEntryService
    {
        /// <summary>
        /// Lista lançamentos financeiros do usuário com suporte a filtros.
        /// Para Contadores, inclui lançamentos de clientes se ClientId for especificado no filtro.
        /// </summary>
        /// <param name="userId">ID do usuário autenticado.</param>
        /// <param name="filter">Filtros opcionais (tipo, categoria, cliente, período, busca).</param>
        /// <returns>Lista de FinancialEntryDto ordenada por EntryDate desc.</returns>
        Task<IEnumerable<FinancialEntryDto>> GetEntriesAsync(Guid userId, FinancialEntryFilterDto filter);

        /// <summary>
        /// Retorna um lançamento específico pelo ID.
        /// Valida que o lançamento pertence ao usuário (ou a um cliente do Contador).
        /// </summary>
        /// <param name="entryId">ID do lançamento.</param>
        /// <param name="userId">ID do usuário autenticado.</param>
        /// <returns>FinancialEntryDto se encontrado e acessível.</returns>
        /// <exception cref="KeyNotFoundException">Se o lançamento não existir ou não pertencer ao usuário.</exception>
        Task<FinancialEntryDto> GetEntryByIdAsync(Guid entryId, Guid userId);

        /// <summary>
        /// Cria um novo lançamento financeiro.
        /// Valida a categoria e, para Contadores, valida que o ClientId pertence ao contador.
        /// </summary>
        /// <param name="userId">ID do usuário autenticado.</param>
        /// <param name="dto">Dados do novo lançamento.</param>
        /// <returns>FinancialEntryDto do lançamento criado.</returns>
        /// <exception cref="KeyNotFoundException">Se a categoria ou o cliente não forem encontrados.</exception>
        /// <exception cref="InvalidOperationException">Se a categoria estiver inativa ou o tipo for incompatível.</exception>
        Task<FinancialEntryDto> CreateEntryAsync(Guid userId, CreateFinancialEntryDto dto);

        /// <summary>
        /// Atualiza um lançamento financeiro existente.
        /// Somente o usuário proprietário pode editar o lançamento.
        /// </summary>
        /// <param name="entryId">ID do lançamento a atualizar.</param>
        /// <param name="userId">ID do usuário autenticado.</param>
        /// <param name="dto">Dados para atualização (apenas campos preenchidos são atualizados).</param>
        /// <returns>FinancialEntryDto atualizado.</returns>
        /// <exception cref="KeyNotFoundException">Se o lançamento não for encontrado ou não pertencer ao usuário.</exception>
        Task<FinancialEntryDto> UpdateEntryAsync(Guid entryId, Guid userId, UpdateFinancialEntryDto dto);

        /// <summary>
        /// Remove permanentemente um lançamento financeiro.
        /// Somente o usuário proprietário pode excluir o lançamento.
        /// </summary>
        /// <param name="entryId">ID do lançamento a excluir.</param>
        /// <param name="userId">ID do usuário autenticado.</param>
        /// <exception cref="KeyNotFoundException">Se o lançamento não for encontrado ou não pertencer ao usuário.</exception>
        Task DeleteEntryAsync(Guid entryId, Guid userId);

        /// <summary>
        /// Gera um resumo financeiro agregado por período.
        /// Inclui totais de receitas e despesas, breakdown por categoria e histórico mensal.
        /// </summary>
        /// <param name="userId">ID do usuário autenticado.</param>
        /// <param name="filter">Filtros de período e tipo para o resumo.</param>
        /// <returns>FinancialSummaryDto com totais, categorias e histórico mensal.</returns>
        Task<FinancialSummaryDto> GetSummaryAsync(Guid userId, FinancialEntryFilterDto filter);
    }
}
