using System;

namespace RestaurantPOS.Core.DTOs
{
    public class PaymentHistoryFilterDTO
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? PaymentMethod { get; set; } // null=전체, "Cash"=현금, "Card"=카드
        public string? OrderStatus { get; set; } // null=전체, "Completed"=완료, "Cancelled"=취소
        public bool ShowOnlySyncErrors { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        
        // 기본값 설정
        public void SetDefaults()
        {
            if (!StartDate.HasValue)
                StartDate = DateTime.Today;
                
            if (!EndDate.HasValue)
                EndDate = DateTime.Today.AddDays(1).AddSeconds(-1); // 오늘 23:59:59
        }
    }
}