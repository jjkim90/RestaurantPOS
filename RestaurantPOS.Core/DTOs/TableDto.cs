using RestaurantPOS.Core.Enums;
using System;
using System.Collections.Generic;

namespace RestaurantPOS.Core.DTOs
{
    public class TableDto
    {
        public int TableId { get; set; }
        public int SpaceId { get; set; }
        public string SpaceName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public int TableNumber { get; set; }
        public TableStatus TableStatus { get; set; }
        public bool IsEditable { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // 추가 정보
        public decimal CurrentOrderAmount { get; set; }
        public int? CurrentOrderId { get; set; }
        public DateTime? OccupiedSince { get; set; }
        
        // 주문 상세 정보 (메뉴 미리보기용)
        public List<OrderDetailDto> CurrentOrderDetails { get; set; } = new List<OrderDetailDto>();
    }
    
    // 간단한 주문 상세 정보 DTO (미리보기용)
    public class OrderDetailDto
    {
        public string MenuItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class CreateTableDto
    {
        public int SpaceId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public int TableNumber { get; set; }
        public TableStatus TableStatus { get; set; } = TableStatus.Available;
        public bool IsEditable { get; set; } = true;
    }

    public class UpdateTableDto
    {
        public string TableName { get; set; } = string.Empty;
        public int TableNumber { get; set; }
        public TableStatus TableStatus { get; set; }
    }

    public class TableStatusUpdateDto
    {
        public TableStatus NewStatus { get; set; }
        public int? OrderId { get; set; }
    }

    public class TableReservationDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime ReservationTime { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}