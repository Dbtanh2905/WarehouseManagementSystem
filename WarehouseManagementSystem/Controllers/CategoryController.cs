using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Data;
using WarehouseManagementSystem.Models;

namespace WarehouseManagementSystem.Controllers
{
    [Authorize]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Categories
                .Include(c => c.Products)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c => c.CategoryName.ToLower().Contains(term) 
                    || (c.Description != null && c.Description.ToLower().Contains(term)));
            }

            ViewData["CurrentSearch"] = search;
            var list = await query.OrderBy(c => c.CategoryName).ToListAsync();
            return View(list);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                bool exists = await _context.Categories.AnyAsync(c => c.CategoryName.ToLower() == category.CategoryName.Trim().ToLower());
                if (exists)
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục này đã tồn tại.");
                    return View(category);
                }

                category.CategoryName = category.CategoryName.Trim();
                category.CreatedAt = DateTime.UtcNow;
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Thêm danh mục '{category.CategoryName}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id) return NotFound();

            if (ModelState.IsValid)
            {
                bool exists = await _context.Categories.AnyAsync(c => c.Id != id && c.CategoryName.ToLower() == category.CategoryName.Trim().ToLower());
                if (exists)
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục này đã tồn tại.");
                    return View(category);
                }

                var existing = await _context.Categories.FindAsync(id);
                if (existing == null) return NotFound();

                existing.CategoryName = category.CategoryName.Trim();
                existing.Description = category.Description;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật danh mục '{existing.CategoryName}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();

            if (category.Products.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa danh mục '{category.CategoryName}' vì đang có {category.Products.Count} linh kiện thuộc danh mục này.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa danh mục '{category.CategoryName}' thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
