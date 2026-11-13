using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarTalkSessions.Data;
using StarTalkSessions.Services;
using StarTalkSessions.Utils;

namespace StarTalkSessions.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserDatabaseAccess _userDb;
        private readonly OrderDatabaseAccess _orderDb;
        private readonly CustomQuoteDatabaseAccess _quoteDb;
        private readonly NotificationService _notifications;

        public AccountController(UserDatabaseAccess userDb, OrderDatabaseAccess orderDb,
            CustomQuoteDatabaseAccess quoteDb, NotificationService notifications)
        {
            _userDb = userDb;
            _orderDb = orderDb;
            _quoteDb = quoteDb;
            _notifications = notifications;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(CancellationToken ctl)
        {
            this.SetStore();
            var user = await GetCurrentUser(ctl);
            if (user == null) return RedirectToAction("Login", "Auth");

            ViewBag.Orders = (await _orderDb.QueryManyAsync(o => o.UserEmail == user.Email, ctl))
                .OrderByDescending(o => o.CreatedAt).ToList();
            ViewBag.Quotes = (await _quoteDb.QueryManyAsync(q => q.Email == user.Email, ctl))
                .OrderByDescending(q => q.CreatedAt).ToList();
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Orders(CancellationToken ctl)
        {
            this.SetStore();
            var user = await GetCurrentUser(ctl);
            if (user == null) return RedirectToAction("Login", "Auth");
            ViewBag.Orders = (await _orderDb.QueryManyAsync(o => o.UserEmail == user.Email, ctl))
                .OrderByDescending(o => o.CreatedAt).ToList();
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Quotes(CancellationToken ctl)
        {
            this.SetStore();
            var user = await GetCurrentUser(ctl);
            if (user == null) return RedirectToAction("Login", "Auth");
            ViewBag.Quotes = (await _quoteDb.QueryManyAsync(q => q.Email == user.Email, ctl))
                .OrderByDescending(q => q.CreatedAt).ToList();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string phone,
            string address, string city, string country, CancellationToken ctl)
        {
            var user = await GetCurrentUser(ctl);
            if (user == null) return RedirectToAction("Login", "Auth");

            user.FirstName = firstName?.Trim() ?? user.FirstName;
            user.LastName = lastName?.Trim() ?? user.LastName;
            user.Phone = phone?.Trim() ?? user.Phone;
            user.Address = address?.Trim() ?? user.Address;
            user.City = city?.Trim() ?? user.City;
            user.Country = country?.Trim() ?? user.Country;
            await _userDb.UpdateAsync(user, ctl);

            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string customerName, string customerPhone,
            string shippingAddress, string country, string currency, string json, CancellationToken ctl)
        {
            var user = await GetCurrentUser(ctl);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(json))
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction(nameof(Dashboard));
            }

            var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Models.OrderItem>>(json) ?? new();
            if (items.Count == 0)
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction(nameof(Dashboard));
            }

            var order = new Models.Order
            {
                OrderNumber = Functions.GenerateOrderNumber(),
                UserId = user.Id,
                CustomerName = string.IsNullOrWhiteSpace(customerName) ? user.FullName : customerName,
                CustomerEmail = user.Email,
                UserEmail = user.Email,
                CustomerPhone = customerPhone ?? user.Phone ?? "",
                ShippingAddress = shippingAddress ?? user.Address ?? "",
                Country = country ?? user.Country ?? "",
                Items = items,
                Subtotal = items.Sum(i => i.UnitPrice * i.Quantity),
                Shipping = 0,
                Total = items.Sum(i => i.UnitPrice * i.Quantity),
                Currency = string.IsNullOrWhiteSpace(currency) ? "NGN" : currency,
                Status = "Processing",
                TrackingRef = Functions.GenerateTrackingRef(),
                CreatedAt = DateTime.Now,
                EstimatedDelivery = DateTime.Now.AddDays(companyLeadDays(items.Count))
            };
            await _orderDb.InsertAsync(order, ctl);

            _ = _notifications.SendEmailAsync(user.Email, $"Order {order.OrderNumber} confirmed", _notifications.OrderConfirmationHtml(order));
            _ = _notifications.NotifyWhatsAppAsync($"New order {order.OrderNumber} — {order.CustomerName}, {order.Items.Count} item(s), {order.Currency} {order.Total:N0}.");

            return Json(new { ok = true, orderNumber = order.OrderNumber, trackingRef = order.TrackingRef });
        }

        private static int companyLeadDays(int itemCount) => 21 + itemCount * 3;

        private async Task<Models.UserModel?> GetCurrentUser(CancellationToken ctl)
        {
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(emailClaim)) return null;
            return await _userDb.QueryOneAsync(u => u.Email == emailClaim, ctl);
        }
    }
}