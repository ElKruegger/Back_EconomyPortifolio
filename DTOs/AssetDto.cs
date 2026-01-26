using System.ComponentModel.DataAnnotations;

namespace EconomyBackPortifolio.DTOs
{
    public class AssetDto
    {
        public Guid Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateAssetDto
    {
        [Required(ErrorMessage = "O símbolo é obrigatório")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "O símbolo deve ter entre 1 e 20 caracteres")]
        public string Symbol { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(150, MinimumLength = 1, ErrorMessage = "O nome deve ter entre 1 e 150 caracteres")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O tipo é obrigatório")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "O tipo deve ter entre 1 e 20 caracteres")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "A moeda é obrigatória")]
        [StringLength(10, MinimumLength = 3, ErrorMessage = "A moeda deve ter entre 3 e 10 caracteres")]
        public string Currency { get; set; } = "USD";

        [Required(ErrorMessage = "O preço atual é obrigatório")]
        [Range(0.000001, 999999999.99, ErrorMessage = "O preço deve ser maior que zero")]
        public decimal CurrentPrice { get; set; }
    }

    public class UpdateAssetPriceDto
    {
        [Required(ErrorMessage = "O preço atual é obrigatório")]
        [Range(0.000001, 999999999.99, ErrorMessage = "O preço deve ser maior que zero")]
        public decimal CurrentPrice { get; set; }
    }
}
