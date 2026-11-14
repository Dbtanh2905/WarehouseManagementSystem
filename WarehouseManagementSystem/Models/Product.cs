using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã linh kiện")]
        [MaxLength(50, ErrorMessage = "Mã linh kiện không quá 50 ký tự")]
        [Display(Name = "Mã linh kiện")]
        public string ProductCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên linh kiện")]
        [MaxLength(200, ErrorMessage = "Tên linh kiện không quá 200 ký tự")]
        [Display(Name = "Tên linh kiện")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp")]
        [Display(Name = "Nhà cung cấp")]
        public int SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đơn vị tính")]
        [MaxLength(50)]
        [Display(Name = "Đơn vị tính")]
        public string Unit { get; set; } = "Cái";

        [Range(0, 1000000, ErrorMessage = "Mức tồn tối thiểu phải từ 0 trở lên")]
        [Display(Name = "Mức tồn tối thiểu")]
        public int MinimumStock { get; set; } = 10;

        [MaxLength(1000)]
        [Display(Name = "Mô tả / Thông số kỹ thuật")]
        public string? Description { get; set; }

        [Display(Name = "Đang sử dụng")]
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
