namespace EconomyBackPortifolio.Enums
{
    /// <summary>
    /// Define o tipo de um lançamento financeiro (FinancialEntry).
    ///
    /// Usado para classificar entradas e saídas de caixa no controle financeiro.
    /// Cada lançamento é obrigatoriamente uma Receita ou uma Despesa.
    /// </summary>
    public enum EntryType
    {
        /// <summary>Receita — valor que entrou (salário, venda, rendimento, etc.).</summary>
        Receita = 0,

        /// <summary>Despesa — valor que saiu (aluguel, alimentação, conta, etc.).</summary>
        Despesa = 1
    }
}
