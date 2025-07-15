using System;
using System.Collections.Generic;

namespace RestaurantPOS.Core.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        // Navigation property
        public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}