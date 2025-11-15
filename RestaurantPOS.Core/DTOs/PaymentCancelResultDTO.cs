using System;

namespace RestaurantPOS.Core.DTOs
{
    public class PaymentCancelResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? PaymentKey { get; set; }
        public string? TransactionKey { get; set; }
        public string? CancelReason { get; set; }
        public decimal CancelAmount { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        
        // 재시도 가능 여부
        public bool CanRetry => !IsSuccess && IsRetryableError(ErrorCode);
        
        private bool IsRetryableError(string? errorCode)
        {
            // 네트워크 오류나 일시적인 오류는 재시도 가능
            var retryableErrors = new[] { "NETWORK_ERROR", "TIMEOUT", "SERVICE_UNAVAILABLE" };
            return errorCode != null && retryableErrors.Contains(errorCode);
        }
        
        public static PaymentCancelResultDTO Success(string paymentKey, string transactionKey, decimal amount, DateTime cancelledAt)
        {
            return new PaymentCancelResultDTO
            {
                IsSuccess = true,
                PaymentKey = paymentKey,
                TransactionKey = transactionKey,
                CancelAmount = amount,
                CancelledAt = cancelledAt
            };
        }
        
        public static PaymentCancelResultDTO Failure(string errorCode, string errorMessage)
        {
            return new PaymentCancelResultDTO
            {
                IsSuccess = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
    }
}