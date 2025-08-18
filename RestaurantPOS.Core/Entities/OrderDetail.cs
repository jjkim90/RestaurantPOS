using System;
using RestaurantPOS.Core.Enums;

namespace RestaurantPOS.Core.Entities
{
    public class OrderDetail
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // 새로 추가된 속성들
        public bool IsNewItem { get; set; }  // 새로 추가된 항목인지 구분
        public OrderDetailStatus Status { get; set; } = OrderDetailStatus.Pending;  // 항목 상태
        public DateTime? ConfirmedAt { get; set; }  // 주방 확정 시간

        // Navigation properties
        public virtual Order Order { get; set; } = null!;
        public virtual MenuItem MenuItem { get; set; } = null!;
    }
}