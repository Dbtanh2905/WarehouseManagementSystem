using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.Models
{
    public class StockReceipt
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã phiếu nhập")]
        [MaxLength(50)]
        [Display(Name = "Mã phiếu nhập")]
        public string ReceiptCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày nhập")]
        [Display(Name = "Ngày nhập")]
        public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp")]
        [Display(Name = "Nhà cung cấp")]
        public int SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        [Display(Name = "Người lập")]
        public string? CreatedById { get; set; }
        public virtual ApplicationUser? CreatedBy { get; set; }

        [Display(Name = "Trạng thái")]
        public ReceiptStatus Status { get; set; } = ReceiptStatus.Draft;

        [MaxLength(1000)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Ngày xác nhận")]
        public DateTime? ConfirmedAt { get; set; }

        // Navigation properties
        public virtual ICollection<StockReceiptDetail> ReceiptDetails { get; set; } = new List<StockReceiptDetail>();
    }
}
