using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Data;
using WarehouseManagementSystem.Models;
using WarehouseManagementSystem.Services;
using WarehouseManagementSystem.ViewModels;

namespace WarehouseManagementSystem.Controllers
{
    [Authorize]
    public class StockReceiptController : Controller
    {
        private readonly IStockReceiptService _stockReceiptService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StockReceiptController(
            IStockReceiptService stockReceiptService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _stockReceiptService = stockReceiptService;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, ReceiptStatus? status, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            var model = await _stockReceiptService.GetReceiptsAsync(search, status, fromDate, toDate, page, 10);
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var receipt = await _stockReceiptService.GetReceiptByIdAsync(id);
            if (receipt == null) return NotFound();
            return View(receipt);
        }

        public async Task<IActionResult> Create()
        {
            string generatedCode = await _stockReceiptService.GenerateReceiptCodeAsync();
            await PopulateDropdownsAsync();

            var model = new StockReceiptCreateViewModel
            {
                ReceiptCode = generatedCode,
                ReceiptDate = DateTime.Today,
                Details = new List<StockReceiptDetailItemInput>
                {
                    new() { Quantity = 1 }
                }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockReceiptCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var result = await _stockReceiptService.CreateReceiptAsync(model, user?.Id ?? "");

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Details), new { id = result.ReceiptId });
                }

                ModelState.AddModelError(string.Empty, result.Message);
            }

            await PopulateDropdownsAsync(model.SupplierId);
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var receipt = await _stockReceiptService.GetReceiptByIdAsync(id);
            if (receipt == null) return NotFound();

            if (receipt.Status != ReceiptStatus.Draft)
            {
                TempData["ErrorMessage"] = "Chỉ có thể chỉnh sửa phiếu ở trạng thái bản nháp.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await PopulateDropdownsAsync(receipt.SupplierId);

            var model = new StockReceiptCreateViewModel
            {
                ReceiptCode = receipt.ReceiptCode,
                ReceiptDate = receipt.ReceiptDate,
                SupplierId = receipt.SupplierId,
                Note = receipt.Note,
                Details = receipt.ReceiptDetails.Select(d => new StockReceiptDetailItemInput
                {
                    Id = d.Id,
                    ProductId = d.ProductId,
                    LocationId = d.LocationId,
                    Quantity = d.Quantity
                }).ToList()
            };

            ViewBag.ReceiptId = id;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StockReceiptCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var result = await _stockReceiptService.UpdateReceiptDraftAsync(id, model, user?.Id ?? "");

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Details), new { id });
                }

                ModelState.AddModelError(string.Empty, result.Message);
            }

            await PopulateDropdownsAsync(model.SupplierId);
            ViewBag.ReceiptId = id;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var result = await _stockReceiptService.CancelReceiptAsync(id, user?.Id ?? "");

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
            var result = await _stockReceiptService.ConfirmReceiptAsync(id, user?.Id ?? "");

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

        private async Task PopulateDropdownsAsync(int? selectedSupplier = null)
        {
            ViewBag.Suppliers = new SelectList(await _context.Suppliers.OrderBy(s => s.SupplierName).ToListAsync(), "Id", "SupplierName", selectedSupplier);
            ViewBag.Products = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.ProductCode).ToListAsync();
            ViewBag.Locations = await _context.WarehouseLocations.Where(l => l.IsActive).OrderBy(l => l.LocationCode).ToListAsync();
        }
    }
}
