namespace EconomyBackPortifolio.Enums
{
    /// <summary>
    /// Define a qual tipo de lançamento uma categoria pertence.
    ///
    /// Categorias podem ser exclusivas de Receita, exclusivas de Despesa,
    /// ou aplicáveis a ambas (ex: "Transferência").
    /// </summary>
    public enum CategoryType
    {
        /// <summary>Categoria aplicável apenas a lançamentos de receita.</summary>
        Receita = 0,

        /// <summary>Categoria aplicável apenas a lançamentos de despesa.</summary>
        Despesa = 1,

        /// <summary>Categoria aplicável a qualquer tipo de lançamento.</summary>
        Ambas = 2
    }
}
