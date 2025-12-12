using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Interfaces;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RestaurantPOS.Services.Services
{
    public class PaymentSyncService : IPaymentSyncService
    {
        private readonly IPaymentHistoryService _paymentHistoryService;
        private readonly ITossPaymentsService _tossPaymentsService;
        private readonly ILogger _logger;
        private Timer? _backgroundTimer;
        private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(5);
        private bool _isSyncing = false;
        private readonly int _maxRetryCount = 3;

        public PaymentSyncService(
            IPaymentHistoryService paymentHistoryService,
            ITossPaymentsService tossPaymentsService,
            ILogger logger)
        {
            _paymentHistoryService = paymentHistoryService;
            _tossPaymentsService = tossPaymentsService;
            _logger = logger;
        }

        public async Task<int> ProcessFailedTransactionsAsync()
        {
            if (_isSyncing)
            {
                _logger.Warning("동기화가 이미 진행 중입니다.");
                return 0;
            }

            _isSyncing = true;
            var processedCount = 0;

            try
            {
                var failedTransactions = await _paymentHistoryService.GetFailedSyncTransactionsAsync();
                _logger.Information("동기화 실패 건 처리 시작 - 총 {Count}건", failedTransactions.Count);

                foreach (var transaction in failedTransactions)
                {
                    try
                    {
                        var success = await RetryTransactionAsync(transaction.PaymentTransactionId);
                        if (success)
                            processedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "트랜잭션 {TransactionId} 재처리 중 오류 발생", 
                            transaction.PaymentTransactionId);
                    }
                }

                _logger.Information("동기화 실패 건 처리 완료 - 성공 {Success}건 / 전체 {Total}건", 
                    processedCount, failedTransactions.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "동기화 실패 건 처리 중 오류 발생");
            }
            finally
            {
                _isSyncing = false;
            }

            return processedCount;
        }

        public async Task<bool> RetryTransactionAsync(int paymentTransactionId)
        {
            try
            {
                _logger.Information("트랜잭션 {TransactionId} 재시도 시작", paymentTransactionId);

                // 여기서는 실제 재시도 로직을 구현
                // 현재는 단순히 성공으로 마킹
                await _paymentHistoryService.UpdateSyncStatusAsync(paymentTransactionId, "Synced");
                
                _logger.Information("트랜잭션 {TransactionId} 재시도 성공", paymentTransactionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "트랜잭션 {TransactionId} 재시도 실패", paymentTransactionId);
                
                // 재시도 횟수 확인 후 최종 실패 처리
                // TODO: [Enhancement] 재시도 횟수 추적 로직 추가 - 향후 개선사항
                
                return false;
            }
        }

        public async Task<int> GetFailedTransactionCountAsync()
        {
            var failedTransactions = await _paymentHistoryService.GetFailedSyncTransactionsAsync();
            return failedTransactions.Count;
        }

        public async Task<bool> HasPendingTransactionsAsync()
        {
            var count = await GetFailedTransactionCountAsync();
            return count > 0;
        }

        public void StartBackgroundSync()
        {
            _logger.Information("백그라운드 동기화 서비스 시작");
            
            _backgroundTimer = new Timer(
                async _ => await ProcessFailedTransactionsAsync(),
                null,
                TimeSpan.Zero,
                _syncInterval);
        }

        public void StopBackgroundSync()
        {
            _logger.Information("백그라운드 동기화 서비스 중지");
            
            _backgroundTimer?.Dispose();
            _backgroundTimer = null;
        }
    }
}