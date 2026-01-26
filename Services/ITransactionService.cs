using EconomyBackPortifolio.DTOs;

namespace EconomyBackPortifolio.Services
{
    public interface ITransactionService
    {
        Task<TransactionDto> DepositAsync(Guid userId, DepositDto depositDto);
        Task<TransactionDto> ConvertCurrencyAsync(Guid userId, ConvertCurrencyDto convertDto);
        Task<TransactionDto> BuyAssetAsync(Guid userId, BuyAssetDto buyDto);
        Task<TransactionDto> SellAssetAsync(Guid userId, SellAssetDto sellDto);
        Task<IEnumerable<TransactionDto>> GetUserTransactionsAsync(Guid userId);
        Task<TransactionDto?> GetTransactionByIdAsync(Guid transactionId, Guid userId);
    }
}
