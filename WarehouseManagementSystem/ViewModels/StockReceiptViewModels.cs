using System.ComponentModel.DataAnnotations;
using WarehouseManagementSystem.Models;

namespace WarehouseManagementSystem.ViewModels
{
    public class StockReceiptDetailItemInput
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn linh kiện")]
        [Display(Name = "Linh kiện")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vị trí kho")]
        [Display(Name = "Vị trí kho")]
        public int LocationId { get; set; }

        [Range(1, 1000000, ErrorMessage = "Số lượng nhập phải lớn hơn 0")]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }
    }

    public class StockReceiptCreateViewModel
    {
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

        [MaxLength(1000)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        public List<StockReceiptDetailItemInput> Details { get; set; } = new();
    }

    public class StockReceiptListViewModel
    {
        public string? SearchTerm { get; set; }
        public ReceiptStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public List<StockReceipt> Receipts { get; set; } = new();
    }
}
