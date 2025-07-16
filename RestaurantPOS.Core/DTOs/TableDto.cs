using RestaurantPOS.Core.Enums;
using System;

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