using RestaurantPOS.Core.DTOs.TossPayments;
using System.Threading.Tasks;

namespace RestaurantPOS.Core.Interfaces
{
    public interface ITossPaymentsService
    {
        Task<PaymentResponseDto> ConfirmPaymentAsync(string paymentKey, string orderId, decimal amount);
        Task<PaymentResponseDto> GetPaymentAsync(string paymentKey);
        Task<PaymentResponseDto> CancelPaymentAsync(string paymentKey, string cancelReason, decimal? cancelAmount = null);
        string GetClientKey();
        string GetSecretKey();
        bool IsTestMode();
    }
}