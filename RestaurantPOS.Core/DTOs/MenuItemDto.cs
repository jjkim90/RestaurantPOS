using System;

namespace RestaurantPOS.Core.DTOs
{
    public class MenuItemDto
    {
        public int MenuItemId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty; // 조인을 통해 가져올 카테고리명
        public string ItemName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public bool IsAvailable { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}