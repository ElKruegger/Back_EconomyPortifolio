using System.ComponentModel.DataAnnotations;

namespace EconomyBackPortifolio.DTOs
{
    /// <summary>
    /// DTO de saída representando um cliente gerenciado por um Contador.
    /// Retornado nos endpoints GET /clients e GET /clients/{id}.
    /// </summary>
    public class ClientDto
    {
        /// <summary>Identificador único do cliente.</summary>
        public Guid Id { get; set; }

        /// <summary>Nome completo ou razão social do cliente.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>E-mail de contato do cliente.</summary>
        public string? Email { get; set; }

        /// <summary>Telefone de contato do cliente.</summary>
        public string? Phone { get; set; }

        /// <summary>CPF ou CNPJ do cliente (apenas dígitos).</summary>
        public string? Document { get; set; }

        /// <summary>Notas internas do contador sobre o cliente.</summary>
        public string? Notes { get; set; }

        /// <summary>Indica se o cliente está ativo no escritório do contador.</summary>
        public bool IsActive { get; set; }

        /// <summary>Timestamp UTC de criação do registro.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Total de lançamentos registrados para este cliente.</summary>
        public int TotalLancamentos { get; set; }
    }

    /// <summary>
    /// DTO de entrada para criação de um novo cliente.
    /// Exclusivo para usuários com ProfileType=Contador.
    /// No plano Basic, o limite é de 3 clientes ativos.
    /// </summary>
    public class CreateClientDto
    {
        /// <summary>Nome completo ou razão social do cliente. Entre 2 e 150 caracteres.</summary>
        [Required(ErrorMessage = "O nome do cliente é obrigatório")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 150 caracteres")]
        public string Name { get; set; } = string.Empty;

        /// <summary>E-mail de contato do cliente. Máx 150 caracteres. Opcional.</summary>
        [EmailAddress(ErrorMessage = "E-mail de contato inválido")]
        [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres")]
        public string? Email { get; set; }

        /// <summary>
        /// Telefone de contato do cliente. Máx 20 caracteres. Opcional.
        /// Armazenado sem formatação (apenas dígitos).
        /// </summary>
        [StringLength(20, ErrorMessage = "O telefone deve ter no máximo 20 caracteres")]
        public string? Phone { get; set; }

        /// <summary>
        /// CPF ou CNPJ do cliente. Máx 20 caracteres. Opcional.
        /// Armazenado apenas com dígitos, sem pontuação.
        /// </summary>
        [StringLength(20, ErrorMessage = "O documento deve ter no máximo 20 caracteres")]
        public string? Document { get; set; }

        /// <summary>Notas internas do contador sobre o cliente. Máx 1000 caracteres. Opcional.</summary>
        [StringLength(1000, ErrorMessage = "As notas devem ter no máximo 1000 caracteres")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO de entrada para atualização de um cliente existente.
    /// Todos os campos são opcionais — apenas os preenchidos serão atualizados.
    /// </summary>
    public class UpdateClientDto
    {
        /// <summary>Novo nome do cliente. Entre 2 e 150 caracteres.</summary>
        [StringLength(150, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 150 caracteres")]
        public string? Name { get; set; }

        /// <summary>Novo e-mail de contato. Máx 150 caracteres.</summary>
        [EmailAddress(ErrorMessage = "E-mail de contato inválido")]
        [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres")]
        public string? Email { get; set; }

        /// <summary>Novo telefone de contato. Máx 20 caracteres.</summary>
        [StringLength(20, ErrorMessage = "O telefone deve ter no máximo 20 caracteres")]
        public string? Phone { get; set; }

        /// <summary>Novo CPF ou CNPJ. Máx 20 caracteres.</summary>
        [StringLength(20, ErrorMessage = "O documento deve ter no máximo 20 caracteres")]
        public string? Document { get; set; }

        /// <summary>Novas notas internas. Máx 1000 caracteres.</summary>
        [StringLength(1000, ErrorMessage = "As notas devem ter no máximo 1000 caracteres")]
        public string? Notes { get; set; }

        /// <summary>Arquiva (false) ou reativa (true) o cliente.</summary>
        public bool? IsActive { get; set; }
    }
}
