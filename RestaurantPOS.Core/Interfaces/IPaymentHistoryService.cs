using RestaurantPOS.Core.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantPOS.Core.Interfaces
{
    public interface IPaymentHistoryService
    {
        // 결제 내역 조회
        Task<(List<PaymentHistoryDTO> Items, int TotalCount)> GetPaymentHistoryAsync(PaymentHistoryFilterDTO filter);
        Task<PaymentHistoryDTO?> GetPaymentHistoryDetailAsync(int orderId);
        
        // 결제 취소 관련
        Task<PaymentTransactionDTO> MarkPaymentAsCancelPendingAsync(int paymentTransactionId);
        Task<PaymentTransactionDTO> ConfirmCancellationAsync(int paymentTransactionId, PaymentCancelResultDTO result);
        Task<PaymentTransactionDTO> MarkCancellationFailedAsync(int paymentTransactionId, string errorMessage);
        
        // 동기화 관련
        Task<List<PaymentTransactionDTO>> GetFailedSyncTransactionsAsync();
        Task<PaymentTransactionDTO> UpdateSyncStatusAsync(int paymentTransactionId, string syncStatus);
        
        // 통계
        Task<decimal> GetTotalSalesAsync(PaymentHistoryFilterDTO filter);
        Task<Dictionary<string, decimal>> GetSalesByPaymentMethodAsync(PaymentHistoryFilterDTO filter);
        Task<int> GetTransactionCountAsync(PaymentHistoryFilterDTO filter);
    }
}