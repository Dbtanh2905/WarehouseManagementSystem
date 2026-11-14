using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Data;
using WarehouseManagementSystem.Services;

namespace WarehouseManagementSystem.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;
        private readonly ApplicationDbContext _context;

        public ReportController(IReportService reportService, ApplicationDbContext context)
        {
            _reportService = reportService;
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoryId, DateTime? fromDate, DateTime? toDate)
        {
            var inventoryReport = await _reportService.GetInventoryReportAsync(categoryId);
            var topIssued = await _reportService.GetTopIssuedProductsAsync(fromDate, toDate, 10);

            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync(), "Id", "CategoryName", categoryId);
            ViewBag.InventoryReport = inventoryReport;
            ViewBag.TopIssued = topIssued;
            ViewData["CurrentCategory"] = categoryId;
            ViewData["CurrentFromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["CurrentToDate"] = toDate?.ToString("yyyy-MM-dd");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportInventoryCsv(int? categoryId)
        {
            var csvBytes = await _reportService.ExportInventoryToCsvAsync(categoryId);
            string fileName = $"BaoCaoTonKho_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
            return File(csvBytes, "text/csv; charset=utf-8", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> ExportTransactionsCsv(DateTime? fromDate, DateTime? toDate, int? transactionType)
        {
            var csvBytes = await _reportService.ExportStockTransactionsToCsvAsync(fromDate, toDate, transactionType);
            string fileName = $"SoCaiGiaoDichKho_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
            return File(csvBytes, "text/csv; charset=utf-8", fileName);
        }
    }
}
