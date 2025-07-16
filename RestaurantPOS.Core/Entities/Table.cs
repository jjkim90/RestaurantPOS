using RestaurantPOS.Core.Enums;
using System;
using System.Collections.Generic;

namespace RestaurantPOS.Core.Entities
{
    public class Table
    {
        public int TableId { get; set; }
        public int SpaceId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public int TableNumber { get; set; }
        public float? PositionX { get; set; }
        public float? PositionY { get; set; }
        public string? Shape { get; set; }
        public TableStatus TableStatus { get; set; } = TableStatus.Available;
        public bool IsEditable { get; set; } = true;
        public DateTime? LastOrderTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Space Space { get; set; } = null!;
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}