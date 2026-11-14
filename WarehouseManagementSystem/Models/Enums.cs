using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.Models
{
    public enum ReceiptStatus
    {
        [Display(Name = "Bản nháp")]
        Draft = 0,

        [Display(Name = "Đã nhập kho")]
        Confirmed = 1,

        [Display(Name = "Đã hủy")]
        Cancelled = 2
    }

    public enum IssueStatus
    {
        [Display(Name = "Bản nháp")]
        Draft = 0,

        [Display(Name = "Đã xuất kho")]
        Confirmed = 1,

        [Display(Name = "Đã hủy")]
        Cancelled = 2
    }

    public enum TransactionType
    {
        [Display(Name = "Nhập kho")]
        In = 1,

        [Display(Name = "Xuất kho")]
        Out = 2
    }

    public enum StockStatus
    {
        [Display(Name = "Bình thường")]
        Normal = 0,

        [Display(Name = "Sắp hết hàng")]
        LowStock = 1,

        [Display(Name = "Hết hàng")]
        OutOfStock = 2
    }
}
