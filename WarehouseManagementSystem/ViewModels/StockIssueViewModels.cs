using System.ComponentModel.DataAnnotations;
using WarehouseManagementSystem.Models;

namespace WarehouseManagementSystem.ViewModels
{
    public class StockIssueDetailItemInput
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn linh kiện")]
        [Display(Name = "Linh kiện")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vị trí kho xuất")]
        [Display(Name = "Vị trí kho")]
        public int LocationId { get; set; }

        [Range(1, 1000000, ErrorMessage = "Số lượng xuất phải lớn hơn 0")]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }

        // Hiển thị tồn khả dụng tại vị trí lúc chọn
        public int AvailableStock { get; set; }
    }

    public class StockIssueCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã phiếu xuất")]
        [MaxLength(50)]
        [Display(Name = "Mã phiếu xuất")]
        public string IssueCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày xuất")]
        [Display(Name = "Ngày xuất")]
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        [Display(Name = "Ghi chú / Mục đích xuất")]
        public string? Note { get; set; }

        public List<StockIssueDetailItemInput> Details { get; set; } = new();
    }

    public class StockIssueListViewModel
    {
        public string? SearchTerm { get; set; }
        public IssueStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public List<StockIssue> Issues { get; set; } = new();
    }
}
