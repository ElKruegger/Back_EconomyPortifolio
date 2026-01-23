namespace EconomyBackPortifolio.Enums
{
    public static class Currency
    {
        public static readonly HashSet<string> ValidCurrencies = new()
        {
            "BRL", // Real Brasileiro
            "USD", // Dólar Americano
            "EUR", // Euro
            "GBP", // Libra Esterlina
            "JPY", // Iene Japonês
            "CNY", // Yuan Chinês
            "CHF", // Franco Suíço
            "CAD", // Dólar Canadense
            "AUD", // Dólar Australiano
            "ARS", // Peso Argentino
            "MXN", // Peso Mexicano
            "BTC", // Bitcoin
            "ETH"  // Ethereum
        };

        public static bool IsValid(string currency)
        {
            return !string.IsNullOrWhiteSpace(currency) && 
                   ValidCurrencies.Contains(currency.ToUpperInvariant());
        }

        public static string Normalize(string currency)
        {
            return currency?.ToUpperInvariant() ?? string.Empty;
        }
    }
}
