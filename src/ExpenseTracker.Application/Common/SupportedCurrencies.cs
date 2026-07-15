namespace ExpenseTracker.Application.Common
{
    /// <summary>
    /// Currency is a display attribute only — money stays a bare decimal and nothing is converted
    /// between currencies. Codes are ISO 4217. The set is curated for the picker; validation keeps
    /// stored values to something the client can format.
    /// </summary>
    public static class SupportedCurrencies
    {
        public const string Default = "USD";

        public static readonly IReadOnlySet<string> Codes = new HashSet<string>(StringComparer.Ordinal)
        {
            "USD", "EUR", "GBP", "UAH", "PLN", "CAD", "AUD", "JPY", "CHF", "CNY",
            "INR", "SEK", "NOK", "DKK", "CZK", "NZD", "SGD", "HKD", "MXN", "BRL",
            "ZAR", "TRY", "AED", "KRW", "ILS"
        };

        public static bool IsSupported(string? code) =>
            code is not null && Codes.Contains(code);

        /// <summary>Trims and upper-cases so "usd" and " USD " both normalise to a stored "USD".</summary>
        public static string Normalize(string? code) =>
            string.IsNullOrWhiteSpace(code) ? Default : code.Trim().ToUpperInvariant();
    }
}
