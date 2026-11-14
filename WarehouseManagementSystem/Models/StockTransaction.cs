using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.Models
{
    public class StockTransaction
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Linh kiện")]
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Required]
        [Display(Name = "Vị trí kho")]
        public int LocationId { get; set; }
        public virtual WarehouseLocation? Location { get; set; }

        [Required]
        [Display(Name = "Loại giao dịch")]
        public TransactionType TransactionType { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng giao dịch phải lớn hơn 0")]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Mã chứng từ đối chiếu")]
        public string ReferenceCode { get; set; } = string.Empty;

        [Display(Name = "Người thực hiện")]
        public string? PerformedById { get; set; }
        public virtual ApplicationUser? PerformedBy { get; set; }

        [Display(Name = "Thời gian ghi nhận")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }
    }
}
