using System.Text.RegularExpressions;
using StarTalkSessions.Models;

namespace StarTalkSessions.Utils
{
    public static class Functions
    {
        public static bool IsEmailValid(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch
            {
                return false;
            }
        }

        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GenerateQuoteRef()
        {
            var random = new Random();
            var year = DateTime.Now.Year.ToString().Substring(2);
            var num = random.Next(1000, 9999);
            return $"SF/{year}/{num}";
        }

        public static string GenerateOrderNumber()
        {
            var random = new Random();
            var year = DateTime.Now.Year.ToString().Substring(2);
            var num = random.Next(100000, 999999);
            return $"SF-{year}{num}";
        }

        public static string GenerateTrackingRef()
        {
            return "WG" + new Random().Next(10000000, 99999999) + GenerateRandomString(3).ToUpperInvariant();
        }

        public static string ToSlug(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var lower = value.Trim().ToLowerInvariant();
            lower = Regex.Replace(lower, @"[^a-z0-9\s-]", "");
            lower = Regex.Replace(lower, @"[\s-]+", "-").Trim('-');
            return lower;
        }

        public static string NumericAmount(decimal value)
        {
            return value.ToString("N2");
        }

        /// <summary>
        /// Normalises a locally-formatted number ("0803…", "+234 803…") into the
        /// international, digits-only form wa.me expects ("234803…").
        /// </summary>
        public static string WhatsAppDigits(string? number)
        {
            var digits = new string((number ?? "").Where(char.IsDigit).ToArray());
            if (digits.Length == 0) return "";

            // Already international (no national trunk prefix).
            if (!digits.StartsWith("0")) return digits;

            var cc = (Environment.GetEnvironmentVariable("WHATSAPP_COUNTRY_CODE") ?? "234").Trim();
            if (string.IsNullOrWhiteSpace(cc)) return digits.TrimStart('0');
            return cc + digits.TrimStart('0');
        }

        public static string WhatsAppLink(string number, string message)
        {
            var digits = WhatsAppDigits(number);
            return $"https://wa.me/{digits}?text={Uri.EscapeDataString(message ?? "")}";
        }
    }
}