using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Data;
using WarehouseManagementSystem.Models;
using WarehouseManagementSystem.Services;
using WarehouseManagementSystem.ViewModels;

namespace WarehouseManagementSystem.Controllers
{
    [Authorize]
    public class StockIssueController : Controller
    {
        private readonly IStockIssueService _stockIssueService;
        private readonly IInventoryService _inventoryService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StockIssueController(
            IStockIssueService stockIssueService,
            IInventoryService inventoryService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _stockIssueService = stockIssueService;
            _inventoryService = inventoryService;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, IssueStatus? status, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            var model = await _stockIssueService.GetIssuesAsync(search, status, fromDate, toDate, page, 10);
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var issue = await _stockIssueService.GetIssueByIdAsync(id);
            if (issue == null) return NotFound();
            return View(issue);
        }

        public async Task<IActionResult> Create()
        {
            string generatedCode = await _stockIssueService.GenerateIssueCodeAsync();
            await PopulateDropdownsAsync();

            var model = new StockIssueCreateViewModel
            {
                IssueCode = generatedCode,
                IssueDate = DateTime.Today,
                Details = new List<StockIssueDetailItemInput>
                {
                    new() { Quantity = 1 }
                }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockIssueCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var result = await _stockIssueService.CreateIssueAsync(model, user?.Id ?? "");

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Details), new { id = result.IssueId });
                }

                ModelState.AddModelError(string.Empty, result.Message);
            }

            await PopulateDropdownsAsync();
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var issue = await _stockIssueService.GetIssueByIdAsync(id);
            if (issue == null) return NotFound();

            if (issue.Status != IssueStatus.Draft)
            {
                TempData["ErrorMessage"] = "Chỉ có thể chỉnh sửa phiếu xuất ở trạng thái bản nháp.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await PopulateDropdownsAsync();

            var model = new StockIssueCreateViewModel
            {
                IssueCode = issue.IssueCode,
                IssueDate = issue.IssueDate,
                Note = issue.Note,
                Details = issue.IssueDetails.Select(d => new StockIssueDetailItemInput
                {
                    Id = d.Id,
                    ProductId = d.ProductId,
                    LocationId = d.LocationId,
                    Quantity = d.Quantity
                }).ToList()
            };

            ViewBag.IssueId = id;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StockIssueCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var result = await _stockIssueService.UpdateIssueDraftAsync(id, model, user?.Id ?? "");

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Details), new { id });
                }

                ModelState.AddModelError(string.Empty, result.Message);
            }

            await PopulateDropdownsAsync();
            ViewBag.IssueId = id;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var result = await _stockIssueService.CancelIssueAsync(id, user?.Id ?? "");

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var result = await _stockIssueService.ConfirmIssueAsync(id, user?.Id ?? "");

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetStockAtLocation(int productId, int locationId)
        {
            int stock = await _inventoryService.GetStockAtLocationAsync(productId, locationId);
            return Json(new { availableStock = stock });
        }

        private async Task PopulateDropdownsAsync()
        {
            ViewBag.Products = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.ProductCode).ToListAsync();
            ViewBag.Locations = await _context.WarehouseLocations.Where(l => l.IsActive).OrderBy(l => l.LocationCode).ToListAsync();
        }
    }
}
