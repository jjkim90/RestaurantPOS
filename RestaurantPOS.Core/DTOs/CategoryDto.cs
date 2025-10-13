using System;

namespace RestaurantPOS.Core.DTOs
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public int MenuItemCount { get; set; } // 해당 카테고리의 메뉴 아이템 개수
    }
}