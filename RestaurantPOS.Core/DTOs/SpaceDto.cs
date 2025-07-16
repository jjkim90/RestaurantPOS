using System;
using System.Collections.Generic;

namespace RestaurantPOS.Core.DTOs
{
    public class SpaceDto
    {
        public int SpaceId { get; set; }
        public string SpaceName { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
        public int TableCount { get; set; }
        public DateTime CreatedAt { get; set; }
        // ModifiedAt 대신 UpdatedAt 사용 (Space 엔티티에는 없음)
        public List<TableDto> Tables { get; set; } = new List<TableDto>();
    }

    public class CreateSpaceDto
    {
        public string SpaceName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateSpaceDto
    {
        public string SpaceName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}