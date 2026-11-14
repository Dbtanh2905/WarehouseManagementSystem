using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Data;
using WarehouseManagementSystem.Models;

namespace WarehouseManagementSystem.Controllers
{
    [Authorize]
    public class StockTransactionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StockTransactionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, TransactionType? type, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            int pageSize = 15;
            var query = _context.StockTransactions
                .Include(t => t.Product)
                .Include(t => t.Location)
                .Include(t => t.PerformedBy)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(t => t.ReferenceCode.ToLower().Contains(term)
                    || (t.Product != null && (t.Product.ProductCode.ToLower().Contains(term) || t.Product.ProductName.ToLower().Contains(term)))
                    || (t.Location != null && t.Location.LocationCode.ToLower().Contains(term))
                    || (t.Note != null && t.Note.ToLower().Contains(term)));
            }

            if (type.HasValue)
            {
                query = query.Where(t => t.TransactionType == type.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            int totalItems = await query.CountAsync();
            var list = await query
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["CurrentSearch"] = search;
            ViewData["CurrentType"] = type;
            ViewData["CurrentFromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["CurrentToDate"] = toDate?.ToString("yyyy-MM-dd");
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewData["TotalItems"] = totalItems;

            return View(list);
        }
    }
}
