namespace EconomyBackPortifolio.Models
{
    /// <summary>
    /// Representa um ativo financeiro disponível para negociação na plataforma.
    /// Por enquanto, apenas ativos em USD são suportados (ações, ETFs, cripto).
    ///
    /// Tabela: assets
    /// Índices: PK (id), UNIQUE (symbol)
    ///
    /// Relacionamentos:
    ///   - 1 asset → N transactions (via Transactions.AssetId — nullable)
    ///   - 1 asset → N positions (via Positions.AssetId)
    ///
    /// Notas sobre o banco:
    ///   - Symbol é sempre uppercase (ex: AAPL, BTC, ETH). Máx 20 caracteres.
    ///   - CurrentPrice tem precision(18,6) para suportar preços de criptomoedas (ex: 0.000045 BTC).
    ///   - O preço é atualizado manualmente via PUT /api/assets/{id}/price.
    ///     TODO: implementar atualização automática via integração com API de cotações.
    ///   - Type: exemplos de valores aceitos: "STOCK", "ETF", "CRYPTO", "REIT".
    /// </summary>
    public class Assets
    {
        /// <summary>Identificador único do ativo (UUID v4).</summary>
        public Guid Id { get; set; }

        /// <summary>Símbolo de negociação do ativo (ex: AAPL, BTC). Sempre uppercase. Único no banco.</summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>Nome completo do ativo (ex: Apple Inc., Bitcoin). Máx 150 caracteres.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Tipo do ativo: STOCK, ETF, CRYPTO, REIT etc. Máx 20 caracteres.</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Moeda de negociação do ativo (ex: USD). Máx 10 caracteres.</summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>Preço atual do ativo na moeda de negociação. Precision(18,6).</summary>
        public decimal CurrentPrice { get; set; }

        /// <summary>Timestamp UTC de criação do registro.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
