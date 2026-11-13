using StarTalkSessions.Data;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.ViewComponents
{
    public class CategoriesNavViewComponent : ViewComponent
    {
        private readonly CategoryDatabaseAccess _categoryDb;

        public CategoriesNavViewComponent(CategoryDatabaseAccess categoryDb)
        {
            _categoryDb = categoryDb;
        }

        public async Task<IViewComponentResult> InvokeAsync(CancellationToken ctl)
        {
            var categories = await _categoryDb.QueryManyAsync(c => c.IsActive, ctl);
            return View(categories.OrderBy(c => c.SortOrder).ToList());
        }
    }
}