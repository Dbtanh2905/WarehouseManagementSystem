using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.Models
{
    public class StockIssueDetail
    {
        public int Id { get; set; }

        public int StockIssueId { get; set; }
        public virtual StockIssue? StockIssue { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn linh kiện")]
        [Display(Name = "Linh kiện")]
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vị trí kho xuất")]
        [Display(Name = "Vị trí kho xuất")]
        public int LocationId { get; set; }
        public virtual WarehouseLocation? Location { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng xuất phải lớn hơn 0")]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }
    }
}
