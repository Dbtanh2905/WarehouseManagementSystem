using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.Models
{
    public class WarehouseLocation
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã vị trí kho")]
        [MaxLength(50, ErrorMessage = "Mã vị trí không quá 50 ký tự")]
        [Display(Name = "Mã vị trí")]
        public string LocationCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên/mô tả vị trí")]
        [MaxLength(150, ErrorMessage = "Tên vị trí không quá 150 ký tự")]
        [Display(Name = "Tên vị trí kệ")]
        public string LocationName { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Description { get; set; }

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public virtual ICollection<StockReceiptDetail> StockReceiptDetails { get; set; } = new List<StockReceiptDetail>();
        public virtual ICollection<StockIssueDetail> StockIssueDetails { get; set; } = new List<StockIssueDetail>();
        public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    }
}
