namespace EconomyBackPortifolio.Models
{
    /// <summary>
    /// Representa um registro imutável de uma operação financeira realizada na plataforma.
    /// Transactions são criadas pelas operações de depósito, conversão, compra e venda.
    ///
    /// Tabela: transactions
    /// Índices: PK (id), FK (wallet_id), FK (asset_id — nullable)
    ///
    /// Relacionamentos:
    ///   - N transactions → 1 wallet (via WalletId)
    ///   - N transactions → 0..1 asset (via AssetId — nullable para DEPOSIT/CONVERSION)
    ///
    /// Tipos de transação (campo Type):
    ///   - "DEPOSIT"    — depósito de dinheiro em BRL. AssetId = null.
    ///   - "BUY"        — compra de ativo. Quantity = qtd comprada, Price = preço pago, Total = Qty × Price.
    ///   - "SELL"       — venda de ativo. Quantity = qtd vendida, Price = preço recebido.
    ///   - "CONVERSION" — conversão entre moedas. Quantity = valor origem, Price = taxa de câmbio, Total = valor destino.
    ///
    /// Notas sobre o banco:
    ///   - Quantity e Price são nullable para suportar DEPOSIT (sem ativo).
    ///   - Total tem precision(18,2); Quantity e Price têm precision(18,6).
    ///   - TransactionAt representa quando a operação ocorreu (pode ser diferente de CreatedAt em importações).
    ///   - Registros são imutáveis — nunca atualizar ou deletar uma transação existente.
    ///   - TODO: o campo Type deve ser migrado para usar o enum TransactionType em vez de string.
    /// </summary>
    public class Transactions
    {
        /// <summary>Identificador único da transação (UUID v4).</summary>
        public Guid Id { get; set; }

        /// <summary>ID da wallet envolvida na transação. FK para wallets.id.</summary>
        public Guid WalletId { get; set; }

        /// <summary>
        /// ID do ativo envolvido (apenas para BUY e SELL).
        /// Null para DEPOSIT e CONVERSION.
        /// </summary>
        public Guid? AssetId { get; set; }

        /// <summary>Tipo da transação: DEPOSIT, BUY, SELL ou CONVERSION. Máx 20 caracteres.</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Quantidade negociada.
        /// - BUY/SELL: quantidade do ativo.
        /// - CONVERSION: valor da moeda de origem.
        /// - DEPOSIT: null.
        /// </summary>
        public decimal? Quantity { get; set; }

        /// <summary>
        /// Preço unitário.
        /// - BUY/SELL: cotação do ativo.
        /// - CONVERSION: taxa de câmbio aplicada.
        /// - DEPOSIT: null.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>Valor total da transação. Sempre preenchido. Precision(18,2).</summary>
        public decimal Total { get; set; }

        /// <summary>Timestamp UTC quando a operação financeira ocorreu.</summary>
        public DateTime TransactionAt { get; set; }

        /// <summary>Timestamp UTC de criação do registro no banco.</summary>
        public DateTime CreatedAt { get; set; }

        // ─── Navigation Properties ────────────────────────────────────────────
        /// <summary>Wallet envolvida nesta transação. Carregado via Include.</summary>
        public Wallets? Wallet { get; set; }

        /// <summary>Ativo envolvido (null para DEPOSIT/CONVERSION). Carregado via Include.</summary>
        public Assets? Asset { get; set; }
    }
}
