using System.Threading.Tasks;

namespace RestaurantPOS.Core.Interfaces
{
    public interface IPaymentSyncService
    {
        // 동기화 실패 건 재처리
        Task<int> ProcessFailedTransactionsAsync();
        
        // 특정 트랜잭션 재시도
        Task<bool> RetryTransactionAsync(int paymentTransactionId);
        
        // 동기화 상태 모니터링
        Task<int> GetFailedTransactionCountAsync();
        Task<bool> HasPendingTransactionsAsync();
        
        // 백그라운드 서비스 시작/중지
        void StartBackgroundSync();
        void StopBackgroundSync();
    }
}