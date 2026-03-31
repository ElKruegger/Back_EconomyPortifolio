namespace EconomyBackPortifolio.Enums
{
    /// <summary>
    /// Define os tipos de transações financeiras suportados pelo sistema.
    /// Substitui as magic strings "DEPOSIT", "BUY", "SELL", "CONVERSION".
    /// </summary>
    public static class TransactionType
    {
        public const string Deposit    = "DEPOSIT";
        public const string Buy        = "BUY";
        public const string Sell       = "SELL";
        public const string Conversion = "CONVERSION";

        private static readonly HashSet<string> _validTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            Deposit, Buy, Sell, Conversion
        };

        /// <summary>Verifica se o tipo informado é válido.</summary>
        public static bool IsValid(string type) =>
            !string.IsNullOrWhiteSpace(type) && _validTypes.Contains(type);

        /// <summary>Normaliza o tipo para uppercase.</summary>
        public static string Normalize(string type) =>
            type?.ToUpperInvariant() ?? string.Empty;
    }
}
