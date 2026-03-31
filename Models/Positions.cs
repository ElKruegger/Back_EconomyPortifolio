namespace EconomyBackPortifolio.Models
{
    /// <summary>
    /// Representa uma posição aberta de um ativo em uma wallet específica.
    /// Uma posição é criada na primeira compra e removida quando toda a quantidade é vendida.
    ///
    /// Tabela: positions
    /// Índices: PK (id), UNIQUE (wallet_id, asset_id)
    ///
    /// Relacionamentos:
    ///   - N positions → 1 wallet (via WalletId)
    ///   - N positions → 1 asset (via AssetId)
    ///
    /// Notas sobre o banco:
    ///   - O índice UNIQUE (wallet_id, asset_id) garante no banco que um usuário
    ///     não tenha duas posições do mesmo ativo na mesma wallet.
    ///   - Quantity e AveragePrice têm precision(18,6) para suportar frações de cripto.
    ///   - TotalInvested tem precision(18,2) — valor total em reais/dólares investidos.
    ///   - AveragePrice é recalculado a cada compra usando média ponderada:
    ///     novoPrecoMedio = (totalInvestidoAnterior + novoTotal) / (qtdAnterior + novaQtd)
    ///   - TotalInvested é reduzido proporcionalmente a cada venda:
    ///     totalInvestidoReduzido = qtdVendida × precoMedioCusto
    ///   - Não há campo CreatedAt nesta tabela — usar UpdatedAt como referência temporal.
    /// </summary>
    public class Positions
    {
        /// <summary>Identificador único da posição (UUID v4).</summary>
        public Guid Id { get; set; }

        /// <summary>ID da wallet onde o ativo está posicionado. FK para wallets.id.</summary>
        public Guid WalletId { get; set; }

        /// <summary>ID do ativo desta posição. FK para assets.id.</summary>
        public Guid AssetId { get; set; }

        /// <summary>Quantidade atual do ativo. Precision(18,6).</summary>
        public decimal Quantity { get; set; }

        /// <summary>Preço médio de custo calculado por compra ponderada. Precision(18,6).</summary>
        public decimal AveragePrice { get; set; }

        /// <summary>Valor total investido neste ativo (custo base). Precision(18,2).</summary>
        public decimal TotalInvested { get; set; }

        /// <summary>Timestamp UTC da última atualização da posição (compra ou venda parcial).</summary>
        public DateTime UpdatedAt { get; set; }

        // ─── Navigation Properties ────────────────────────────────────────────
        /// <summary>Wallet onde esta posição está registrada. Carregado via Include.</summary>
        public Wallets? Wallet { get; set; }

        /// <summary>Ativo desta posição com preço atual para cálculo de P&amp;L. Carregado via Include.</summary>
        public Assets? Asset { get; set; }
    }
}
