using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.Models
{
    public class StockReceiptDetail
    {
        public int Id { get; set; }

        public int StockReceiptId { get; set; }
        public virtual StockReceipt? StockReceipt { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn linh kiện")]
        [Display(Name = "Linh kiện")]
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vị trí kho")]
        [Display(Name = "Vị trí kho")]
        public int LocationId { get; set; }
        public virtual WarehouseLocation? Location { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập phải lớn hơn 0")]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }
    }
}
