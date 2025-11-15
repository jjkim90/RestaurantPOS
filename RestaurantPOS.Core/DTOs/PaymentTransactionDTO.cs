using System;

namespace RestaurantPOS.Core.DTOs
{
    public class PaymentTransactionDTO
    {
        public int PaymentTransactionId { get; set; }
        public int OrderId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? PaymentKey { get; set; }
        public string? TransactionId { get; set; }
        public string Status { get; set; } = "Completed";
        public string SyncStatus { get; set; } = "Synced";
        public DateTime PaymentDate { get; set; }
        public DateTime? CancelledDate { get; set; }
        public string? CancelReason { get; set; }
        public int? ReferenceTransactionId { get; set; }
        
        // 추가 표시용 속성
        public string DisplayStatus => Status switch
        {
            "Completed" => "완료",
            "Cancelled" => "취소됨",
            "Pending" => "대기중",
            _ => Status
        };

        public string DisplayPaymentMethod => PaymentMethod switch
        {
            "Cash" => "현금",
            "Card" => "카드",
            _ => PaymentMethod
        };

        public bool CanCancel => Status == "Completed" && SyncStatus == "Synced";
        public bool CanRetry => Status == "Cancelled" && SyncStatus == "Synced";
        public bool NeedsSync => SyncStatus == "Failed";
    }
}