using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.Utils
{
    public static class Store
    {
        public static string Name =>
            Environment.GetEnvironmentVariable("STORE_NAME") ?? "Star Talk Sessions";

    public static string Tagline =>
        Environment.GetEnvironmentVariable("STORE_TAGLINE") ?? "Conversations worth staying for.";

        /// <summary>Canonical public site URL, no trailing slash. Used in emails and share links.</summary>
        public static string SiteUrl =>
            (Environment.GetEnvironmentVariable("SITE_URL") ?? "https://startalksessions.com").TrimEnd('/');

        public static string WhatsappNumber =>
            Environment.GetEnvironmentVariable("WHATSAPP_NUMBER") ?? "2348000000000";

        public static string SupportEmail =>
            Environment.GetEnvironmentVariable("SUPPORT_EMAIL") ?? "hello@startalksessions.com";

        public static string SupportPhone =>
            Environment.GetEnvironmentVariable("SUPPORT_PHONE") ?? "+234 800 000 0000";

        /// <summary>Studio address shown in the footer, contact page and quotes.</summary>
        public static string AddressLine1 =>
            Environment.GetEnvironmentVariable("STUDIO_ADDRESS_1") ?? "Federal University of Technology, Minna";

        public static string AddressLine2 =>
            Environment.GetEnvironmentVariable("STUDIO_ADDRESS_2") ?? "Gidan Kwano Campus, Niger State";

        public static string AddressFormatted => $"{AddressLine1}, {AddressLine2}";


        public static readonly string WholesaleWhatsapp = WhatsappNumber;

        // ---------- Google AdSense (optional, silently skipped when unconfigured) ----------
        private static string AdSenseClientFromEnv =>
            Environment.GetEnvironmentVariable("ADSENSE_CLIENT_ID")
            ?? Environment.GetEnvironmentVariable("NEXT_PUBLIC_ADSENSE_CLIENT_ID")
            ?? "ca-pub-2447961066740044";

        public static string AdSenseClientId => AdSenseClientFromEnv.Trim();

        public static bool AdSenseEnabled => !string.IsNullOrWhiteSpace(AdSenseClientId);

        public static string AdSenseSlot(string placement) =>
            Environment.GetEnvironmentVariable($"ADSENSE_SLOT_{placement.ToUpperInvariant()}")?.Trim() ?? "";

        public static void SetStore(this Controller c)
        {
            c.ViewBag.StoreName = Name;
            c.ViewBag.StoreTagline = Tagline;
            c.ViewBag.SiteUrl = SiteUrl;
            c.ViewBag.WhatsappNumber = WhatsappNumber;
            c.ViewBag.SupportEmail = SupportEmail;
            c.ViewBag.SupportPhone = SupportPhone;
            c.ViewBag.AddressLine1 = AddressLine1;
            c.ViewBag.AddressLine2 = AddressLine2;
        }
    }

    public class Currency
    {
        public string Code { get; set; } = "USD";
        public string Symbol { get; set; } = "$";
        public decimal Rate { get; set; } = 1m;

        public static readonly List<Currency> All = new()
        {
            new() { Code = "NGN", Symbol = "₦", Rate = 1550m },
            new() { Code = "USD", Symbol = "$", Rate = 1m },
            new() { Code = "EUR", Symbol = "€", Rate = 0.92m },
            new() { Code = "GBP", Symbol = "£", Rate = 0.79m },
            new() { Code = "AED", Symbol = "د.إ", Rate = 3.67m }
        };

        public static Currency? Find(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            return All.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
        }
    }
}