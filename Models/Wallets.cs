namespace EconomyBackPortifolio.Models
{
    /// <summary>
    /// Representa uma carteira (wallet) de um usuário em uma moeda específica.
    /// Cada usuário pode ter múltiplas wallets, uma por moeda suportada.
    ///
    /// Tabela: wallets
    /// Índices: PK (id), FK (user_id → users.id)
    ///
    /// Relacionamentos:
    ///   - N wallets → 1 usuário (via UserId)
    ///   - 1 wallet → N transactions (via Transactions.WalletId)
    ///   - 1 wallet → N positions (via Positions.WalletId)
    ///
    /// Notas sobre o banco:
    ///   - Balance tem precision(18,2) — adequado para valores monetários em BRL/USD.
    ///   - Toda conta nova recebe automaticamente uma wallet BRL com saldo 0.
    ///   - Não existe índice UNIQUE (userId, currency) na migration atual — recomenda-se adicionar.
    ///     A unicidade é controlada apenas na camada de serviço (WalletService).
    /// </summary>
    public class Wallets
    {
        /// <summary>Identificador único da wallet (UUID v4).</summary>
        public Guid Id { get; set; }

        /// <summary>ID do usuário dono desta wallet. FK para users.id.</summary>
        public Guid UserId { get; set; }

        /// <summary>Código ISO da moeda (ex: BRL, USD, BTC). Máx 10 caracteres.</summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>Saldo disponível na moeda desta wallet. Precision(18,2).</summary>
        public decimal Balance { get; set; }

        /// <summary>Timestamp UTC de criação da wallet.</summary>
        public DateTime CreatedAt { get; set; }

        // ─── Navigation Properties ────────────────────────────────────────────
        /// <summary>Usuário dono da wallet. Carregado via Include no EF Core.</summary>
        public Users? User { get; set; }
    }
}
