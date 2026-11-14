using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.Models
{
    public class StockIssue
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã phiếu xuất")]
        [MaxLength(50)]
        [Display(Name = "Mã phiếu xuất")]
        public string IssueCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày xuất")]
        [Display(Name = "Ngày xuất")]
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Người lập")]
        public string? CreatedById { get; set; }
        public virtual ApplicationUser? CreatedBy { get; set; }

        [Display(Name = "Trạng thái")]
        public IssueStatus Status { get; set; } = IssueStatus.Draft;

        [MaxLength(1000)]
        [Display(Name = "Ghi chú / Mục đích xuất")]
        public string? Note { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Ngày xác nhận")]
        public DateTime? ConfirmedAt { get; set; }

        // Navigation properties
        public virtual ICollection<StockIssueDetail> IssueDetails { get; set; } = new List<StockIssueDetail>();
    }
}
