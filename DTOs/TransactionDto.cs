using System.ComponentModel.DataAnnotations;

namespace EconomyBackPortifolio.DTOs
{
    public class TransactionDto
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public string WalletCurrency { get; set; } = string.Empty;
        public Guid? AssetId { get; set; }
        public string? AssetSymbol { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal? Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal Total { get; set; }
        public DateTime TransactionAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DepositDto
    {
        [Required(ErrorMessage = "O valor é obrigatório")]
        [Range(0.01, 999999999.99, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Amount { get; set; }
    }

    public class ConvertCurrencyDto
    {
        [Required(ErrorMessage = "A moeda de origem é obrigatória")]
        public string FromCurrency { get; set; } = string.Empty;

        [Required(ErrorMessage = "A moeda de destino é obrigatória")]
        public string ToCurrency { get; set; } = string.Empty;

        [Required(ErrorMessage = "O valor é obrigatório")]
        [Range(0.01, 999999999.99, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "A taxa de câmbio é obrigatória")]
        [Range(0.0001, 999999.99, ErrorMessage = "A taxa de câmbio deve ser maior que zero")]
        public decimal ExchangeRate { get; set; }
    }

    public class BuyAssetDto
    {
        [Required(ErrorMessage = "O ID do asset é obrigatório")]
        public Guid AssetId { get; set; }

        [Required(ErrorMessage = "A quantidade é obrigatória")]
        [Range(0.000001, 999999999.99, ErrorMessage = "A quantidade deve ser maior que zero")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "O preço é obrigatório")]
        [Range(0.000001, 999999999.99, ErrorMessage = "O preço deve ser maior que zero")]
        public decimal Price { get; set; }
    }

    public class SellAssetDto
    {
        [Required(ErrorMessage = "O ID do asset é obrigatório")]
        public Guid AssetId { get; set; }

        [Required(ErrorMessage = "A quantidade é obrigatória")]
        [Range(0.000001, 999999999.99, ErrorMessage = "A quantidade deve ser maior que zero")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "O preço é obrigatório")]
        [Range(0.000001, 999999999.99, ErrorMessage = "O preço deve ser maior que zero")]
        public decimal Price { get; set; }
    }
}
