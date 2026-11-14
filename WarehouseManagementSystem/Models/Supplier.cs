using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp")]
        [MaxLength(200, ErrorMessage = "Tên nhà cung cấp không quá 200 ký tự")]
        [Display(Name = "Tên nhà cung cấp")]
        public string SupplierName { get; set; } = string.Empty;

        [MaxLength(20, ErrorMessage = "Số điện thoại không quá 20 ký tự")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [MaxLength(100, ErrorMessage = "Email không quá 100 ký tự")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [MaxLength(300, ErrorMessage = "Địa chỉ không quá 300 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<StockReceipt> StockReceipts { get; set; } = new List<StockReceipt>();
    }
}
