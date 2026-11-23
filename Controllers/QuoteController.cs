using StarTalkSessions.Data;
using StarTalkSessions.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using PuppeteerSharp;

namespace StarTalkSessions.Controllers
{
    public class QuoteController : Controller
    {
        private readonly CustomQuoteDatabaseAccess _quoteDb;

        public QuoteController(CustomQuoteDatabaseAccess quoteDb)
        {
            _quoteDb = quoteDb;
        }

        public async Task<IActionResult> Pdf(Guid id, CancellationToken ctl)
        {
            this.SetStore();
            var quote = await _quoteDb.QueryOneAsync(q => q.Id == id, ctl);
            if (quote == null) return NotFound();

            ViewBag.Quote = quote;

            try
            {
                await new BrowserFetcher().DownloadAsync();
                await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                });
                await using var page = await browser.NewPageAsync();

                var html = await RenderViewToStringAsync("QuotePdf");
                await page.SetContentAsync(html, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } });
                var pdf = await page.PdfDataAsync(new PdfOptions { Format = PuppeteerSharp.Media.PaperFormat.A4 });
                return File(pdf, "application/pdf", $"Quote-{quote.RefNumber}.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PDF generation failed: {ex.Message}");
                return View("QuotePrint");
            }
        }

        private async Task<string> RenderViewToStringAsync(string viewName)
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var engine = scope.ServiceProvider.GetRequiredService<ICompositeViewEngine>();
            var result = engine.FindView(ControllerContext, viewName, false);
            if (result.View == null)
                throw new InvalidOperationException($"View {viewName} not found.");

            using var writer = new StringWriter();
            var context = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext(ControllerContext, result.View, ViewData, TempData, writer, new Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions());
            await result.View.RenderAsync(context);
            return writer.ToString();
        }
    }
}