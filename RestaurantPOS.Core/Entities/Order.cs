using System;
using System.Collections.Generic;

namespace RestaurantPOS.Core.Entities
{
    public class Order
    {
        public int OrderId { get; set; }
        public int TableId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public string? PaymentMethod { get; set; }
        public DateTime? PaymentDate { get; set; }
        public bool IsPrinted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Table Table { get; set; } = null!;
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}