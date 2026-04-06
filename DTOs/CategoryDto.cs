using System.ComponentModel.DataAnnotations;
using EconomyBackPortifolio.Enums;

namespace EconomyBackPortifolio.DTOs
{
    /// <summary>
    /// DTO de saída representando uma categoria de lançamento financeiro.
    /// Retornado nos endpoints GET /categories e GET /categories/{id}.
    /// </summary>
    public class CategoryDto
    {
        /// <summary>Identificador único da categoria.</summary>
        public Guid Id { get; set; }

        /// <summary>Nome da categoria (ex: "Alimentação", "Salário").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Tipo de lançamento: Receita (0), Despesa (1) ou Ambas (2).</summary>
        public CategoryType Type { get; set; }

        /// <summary>Ícone representando a categoria (emoji ou nome de ícone).</summary>
        public string? Icon { get; set; }

        /// <summary>Cor hexadecimal da categoria para uso em gráficos (ex: "#4CAF50").</summary>
        public string? Color { get; set; }

        /// <summary>Indica se é uma categoria do sistema (não editável pelo usuário).</summary>
        public bool IsSystem { get; set; }

        /// <summary>Indica se a categoria está ativa e disponível para novos lançamentos.</summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO de entrada para criação de uma categoria personalizada pelo usuário.
    /// Disponível apenas para usuários com plano Pro.
    /// Categorias do sistema (IsSystem=true) não podem ser criadas via API.
    /// </summary>
    public class CreateCategoryDto
    {
        /// <summary>Nome da categoria. Entre 2 e 100 caracteres.</summary>
        [Required(ErrorMessage = "O nome da categoria é obrigatório")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Tipo de lançamento ao qual a categoria se aplica.</summary>
        [Required(ErrorMessage = "O tipo da categoria é obrigatório")]
        public CategoryType Type { get; set; }

        /// <summary>Ícone da categoria. Pode ser um emoji ou nome de ícone. Máx 50 caracteres.</summary>
        [StringLength(50, ErrorMessage = "O ícone deve ter no máximo 50 caracteres")]
        public string? Icon { get; set; }

        /// <summary>Cor em hexadecimal (ex: "#FF5722"). Máx 7 caracteres.</summary>
        [StringLength(7, ErrorMessage = "A cor deve ter no máximo 7 caracteres (ex: #FF5722)")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "A cor deve estar no formato hexadecimal (ex: #FF5722)")]
        public string? Color { get; set; }
    }

    /// <summary>
    /// DTO de entrada para atualização de uma categoria personalizada do usuário.
    /// Todos os campos são opcionais — apenas os preenchidos serão atualizados.
    /// Categorias do sistema (IsSystem=true) não podem ser editadas.
    /// </summary>
    public class UpdateCategoryDto
    {
        /// <summary>Novo nome da categoria. Máx 100 caracteres.</summary>
        [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
        public string? Name { get; set; }

        /// <summary>Novo ícone da categoria. Máx 50 caracteres.</summary>
        [StringLength(50, ErrorMessage = "O ícone deve ter no máximo 50 caracteres")]
        public string? Icon { get; set; }

        /// <summary>Nova cor em hexadecimal. Máx 7 caracteres.</summary>
        [StringLength(7, ErrorMessage = "A cor deve ter no máximo 7 caracteres (ex: #FF5722)")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "A cor deve estar no formato hexadecimal (ex: #FF5722)")]
        public string? Color { get; set; }

        /// <summary>Ativa ou desativa a categoria. Categorias inativas não aparecem na lista de seleção.</summary>
        public bool? IsActive { get; set; }
    }
}
