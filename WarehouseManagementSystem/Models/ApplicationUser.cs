using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(150)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<StockReceipt> CreatedStockReceipts { get; set; } = new List<StockReceipt>();
        public virtual ICollection<StockIssue> CreatedStockIssues { get; set; } = new List<StockIssue>();
        public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    }
}
