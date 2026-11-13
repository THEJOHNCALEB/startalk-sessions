using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarTalkSessions.Data;
using StarTalkSessions.Models;
using StarTalkSessions.Services;
using StarTalkSessions.Utils;

namespace StarTalkSessions.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly UserDatabaseAccess _userDb;
        private readonly NotificationService _notifications;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserDatabaseAccess userDb, NotificationService notifications, ILogger<AuthController> logger)
        {
            _userDb = userDb;
            _notifications = notifications;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = "")
        {
            this.SetStore();
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string returnUrl, CancellationToken ctl)
        {
            this.SetStore();
            ViewBag.ReturnUrl = returnUrl;
            if (!Functions.IsEmailValid(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter a valid email and password.";
                return View();
            }

            var user = await _userDb.QueryOneAsync(u => u.Email == email.Trim().ToLowerInvariant() && u.IsActive, ctl);
            if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(password, user.PasswordHash))
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            await SignInUserAsync(user);
            user.LastLogin = DateTime.Now;
            await _userDb.UpdateAsync(user, ctl);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            if (user.IsAdmin)
                return RedirectToAction("Index", "Admin");
            return RedirectToAction("Dashboard", "Account");
        }

        [HttpGet]
        public IActionResult Register()
        {
            this.SetStore();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string firstName, string lastName, string email,
            string phone, string password, string confirmPassword, CancellationToken ctl)
        {
            this.SetStore();
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                ViewBag.Error = "Please provide your first and last name.";
                return View();
            }
            if (!Functions.IsEmailValid(email))
            {
                ViewBag.Error = "Invalid email address.";
                return View();
            }
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters.";
                return View();
            }
            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            var normalized = email!.Trim().ToLowerInvariant();
            var existing = await _userDb.QueryOneAsync(u => u.Email == normalized, ctl);
            if (existing != null)
            {
                ViewBag.Error = "An account with this email already exists. Please sign in.";
                return View();
            }

            var user = new UserModel
            {
                Email = normalized,
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password),
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                Phone = phone?.Trim() ?? "",
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            await _userDb.InsertAsync(user, ctl);
            await SignInUserAsync(user);

            _ = _notifications.SendEmailAsync(normalized, $"Welcome to {StarTalkSessions.Utils.Store.Name}", _notifications.WelcomeEmailHtml(user));

            return RedirectToAction("Dashboard", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("FURN_COOKIE");
            return RedirectToAction("Index", "Home");
        }

        // ---------- Password reset ----------
        private const int ResetTokenValidMinutes = 30;

        [HttpGet, Route("auth/forgot-password")]
        public IActionResult ForgotPassword()
        {
            this.SetStore();
            return View();
        }

        [HttpPost, Route("auth/forgot-password")]
        public async Task<IActionResult> ForgotPassword(string email, CancellationToken ctl)
        {
            this.SetStore();
            if (!Functions.IsEmailValid(email))
            {
                ViewBag.Error = "Please enter a valid email address.";
                return View();
            }

            var normalized = email!.Trim().ToLowerInvariant();
            var user = await _userDb.QueryOneAsync(u => u.Email == normalized && u.IsActive, ctl);

            // Always show the same confirmation so the form can't be used to discover
            // which email addresses have accounts.
            if (user != null)
            {
                var raw = GenerateResetToken();
                user.PasswordResetTokenHash = HashResetToken(raw);
                user.PasswordResetExpiresAt = DateTime.Now.AddMinutes(ResetTokenValidMinutes);
                await _userDb.UpdateAsync(user, ctl);

                var link = $"{StarTalkSessions.Utils.Store.SiteUrl}/auth/reset-password?token={Uri.EscapeDataString(raw)}&email={Uri.EscapeDataString(normalized)}";
                var sent = await _notifications.TrySendEmailAsync(
                    normalized,
                    $"Reset your {StarTalkSessions.Utils.Store.Name} password",
                    _notifications.PasswordResetHtml(user.FullName, link, ResetTokenValidMinutes));

                if (!sent)
                {
                    _logger.LogWarning("Password reset email could not be sent for {Email}.", normalized);
                }
            }

            TempData["ResetSent"] = true;
            return RedirectToAction(nameof(ForgotPassword));
        }

        [HttpGet, Route("auth/reset-password")]
        public IActionResult ResetPassword(string token, string email)
        {
            this.SetStore();
            ViewBag.Token = token ?? "";
            ViewBag.Email = email ?? "";
            return View();
        }

        [HttpPost, Route("auth/reset-password")]
        public async Task<IActionResult> ResetPassword(string token, string email, string newPassword, string confirmPassword, CancellationToken ctl)
        {
            this.SetStore();
            ViewBag.Token = token ?? "";
            ViewBag.Email = email ?? "";

            if (!Functions.IsEmailValid(email) || string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Error = "This reset link is incomplete. Please request a new one.";
                return View();
            }
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                ViewBag.Error = "Your new password must be at least 8 characters.";
                return View();
            }
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }
            if (newPassword.Any(char.IsWhiteSpace))
            {
                ViewBag.Error = "Password cannot contain spaces.";
                return View();
            }

            var normalized = email!.Trim().ToLowerInvariant();
            var user = await _userDb.QueryOneAsync(u => u.Email == normalized, ctl);

            if (user == null
                || string.IsNullOrEmpty(user.PasswordResetTokenHash)
                || user.PasswordResetExpiresAt == null
                || user.PasswordResetExpiresAt < DateTime.Now
                || !FixedTimeEquals(user.PasswordResetTokenHash, HashResetToken(token!)))
            {
                ViewBag.Error = "This reset link is invalid or has expired. Please request a new one.";
                return View();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(newPassword);
            user.PasswordResetTokenHash = null;
            user.PasswordResetExpiresAt = null;
            await _userDb.UpdateAsync(user, ctl);

            _ = _notifications.SendEmailAsync(
                normalized,
                $"Your {StarTalkSessions.Utils.Store.Name} password was changed",
                _notifications.PasswordChangedHtml(user.FullName));

            await HttpContext.SignOutAsync("FURN_COOKIE");
            TempData["PasswordChanged"] = true;
            return RedirectToAction(nameof(Login));
        }

        private static string GenerateResetToken() =>
            Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        private static string HashResetToken(string raw) =>
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw))
                .Select(b => b.ToString("x2")).Aggregate("", (acc, b) => acc + b);

        private static bool FixedTimeEquals(string? a, string b)
        {
            if (string.IsNullOrEmpty(a)) return false;
            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(a), System.Text.Encoding.UTF8.GetBytes(b));
        }

        private async Task SignInUserAsync(UserModel user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.FullName),
                new("Name", user.FullName),
                new("UserId", user.Id.ToString())
            };

            if (user.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var identity = new ClaimsIdentity(claims, "FURN_COOKIE");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("FURN_COOKIE", principal,
                new AuthenticationProperties { IsPersistent = true });
        }
    }
}