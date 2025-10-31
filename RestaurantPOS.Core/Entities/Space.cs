using System;
using System.Collections.Generic;

namespace RestaurantPOS.Core.Entities
{
    public class Space
    {
        public int SpaceId { get; set; }
        public string SpaceName { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        
        // Soft delete fields
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation property
        public virtual ICollection<Table> Tables { get; set; } = new List<Table>();
    }
}