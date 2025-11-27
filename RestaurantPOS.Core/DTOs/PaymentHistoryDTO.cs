using System;
using System.Collections.Generic;
using System.Linq;

namespace RestaurantPOS.Core.DTOs
{
    public class PaymentHistoryDTO
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int TableId { get; set; }
        public string? TableName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        
        // 주문 상세
        public List<OrderDetailDTO> OrderDetails { get; set; } = new List<OrderDetailDTO>();
        
        // 결제 트랜잭션
        public List<PaymentTransactionDTO> PaymentTransactions { get; set; } = new List<PaymentTransactionDTO>();
        
        // 계산된 속성
        public decimal PaidAmount => PaymentTransactions
            .Where(pt => pt.Status == "Completed")
            .Sum(pt => pt.Amount);
            
        public decimal CancelledAmount => PaymentTransactions
            .Where(pt => pt.Status == "Cancelled")
            .Sum(pt => pt.Amount);
            
        public bool HasSyncError => PaymentTransactions
            .Any(pt => pt.SyncStatus == "Failed");
            
        public string Status => OrderStatus switch
        {
            "Pending" => "대기중",
            "Confirmed" => "확정됨",
            "Completed" => "완료",
            "Cancelled" => "취소됨",
            _ => OrderStatus
        };
        
        public string PaymentMethodsSummary => string.Join(", ", 
            PaymentTransactions
                .Where(pt => pt.Status == "Completed")
                .GroupBy(pt => pt.DisplayPaymentMethod)
                .Select(g => $"{g.Key}: {g.Sum(pt => pt.Amount):N0}원"));
    }
}