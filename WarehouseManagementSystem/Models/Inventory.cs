using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.Models
{
    public class Inventory
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

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không được âm")]
        [Display(Name = "Số lượng tồn")]
        public int Quantity { get; set; } = 0;

        [Display(Name = "Cập nhật lần cuối")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
