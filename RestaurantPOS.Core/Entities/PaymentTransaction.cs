using System;

namespace RestaurantPOS.Core.Entities
{
    public class PaymentTransaction
    {
        public int PaymentTransactionId { get; set; }
        public int OrderId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Card
        public decimal Amount { get; set; }
        public string? PaymentKey { get; set; }  // 토스페이먼츠 결제 키
        public string? TransactionId { get; set; }  // 거래 ID
        public string Status { get; set; } = "Completed"; // Completed, Cancelled, Pending
        public string SyncStatus { get; set; } = "Synced"; // Synced, Pending, Failed - API와 DB 동기화 상태
        public DateTime PaymentDate { get; set; }
        public DateTime? CancelledDate { get; set; }
        public string? CancelReason { get; set; }
        public int? ReferenceTransactionId { get; set; } // 재결제 시 이전 취소 건 참조
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Order Order { get; set; } = null!;
        public virtual PaymentTransaction? ReferenceTransaction { get; set; }
    }
}