using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Entities;
using RestaurantPOS.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantPOS.Services.Services
{
    public class PaymentHistoryService : IPaymentHistoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PaymentHistoryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<(List<PaymentHistoryDTO> Items, int TotalCount)> GetPaymentHistoryAsync(PaymentHistoryFilterDTO filter)
        {
            filter.SetDefaults();

            var query = _unitOfWork.OrderRepository.Query()
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                .Include(o => o.PaymentTransactions)
                .Include(o => o.Table)
                .Where(o => o.Status == "Completed" || o.Status == "Cancelled");

            // 날짜 필터
            if (filter.StartDate.HasValue)
                query = query.Where(o => o.PaymentDate >= filter.StartDate.Value);
            
            if (filter.EndDate.HasValue)
                query = query.Where(o => o.PaymentDate <= filter.EndDate.Value);

            // 주문 상태 필터
            if (!string.IsNullOrEmpty(filter.OrderStatus))
                query = query.Where(o => o.Status == filter.OrderStatus);

            // 결제 수단 필터
            if (!string.IsNullOrEmpty(filter.PaymentMethod))
                query = query.Where(o => o.PaymentTransactions.Any(pt => 
                    pt.PaymentMethod == filter.PaymentMethod && pt.Status == "Completed"));

            // 동기화 오류 필터
            if (filter.ShowOnlySyncErrors)
                query = query.Where(o => o.PaymentTransactions.Any(pt => pt.SyncStatus == "Failed"));

            // 총 개수
            var totalCount = await query.CountAsync();

            // 페이징
            var items = await query
                .OrderByDescending(o => o.PaymentDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var dtos = items.Select(MapToPaymentHistoryDTO).ToList();

            return (dtos, totalCount);
        }

        public async Task<PaymentHistoryDTO?> GetPaymentHistoryDetailAsync(int orderId)
        {
            var order = await _unitOfWork.OrderRepository.Query()
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                .Include(o => o.PaymentTransactions)
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            return order != null ? MapToPaymentHistoryDTO(order) : null;
        }

        public async Task<PaymentTransactionDTO> MarkPaymentAsCancelPendingAsync(int paymentTransactionId)
        {
            var transaction = await _unitOfWork.PaymentTransactionRepository.GetByIdAsync(paymentTransactionId);
            if (transaction == null)
                throw new InvalidOperationException($"결제 트랜잭션 {paymentTransactionId}을(를) 찾을 수 없습니다.");

            transaction.Status = "Pending";
            transaction.SyncStatus = "Pending";
            transaction.UpdatedAt = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PaymentTransactionDTO>(transaction);
        }

        public async Task<PaymentTransactionDTO> ConfirmCancellationAsync(int paymentTransactionId, PaymentCancelResultDTO result)
        {
            var transaction = await _unitOfWork.PaymentTransactionRepository.GetByIdAsync(paymentTransactionId);
            if (transaction == null)
                throw new InvalidOperationException($"결제 트랜잭션 {paymentTransactionId}을(를) 찾을 수 없습니다.");

            if (result.IsSuccess)
            {
                transaction.Status = "Cancelled";
                transaction.SyncStatus = "Synced";
                transaction.CancelledDate = result.CancelledAt;
                transaction.CancelReason = result.CancelReason;
            }
            else
            {
                transaction.Status = "Completed"; // 취소 실패 시 원상태로
                transaction.SyncStatus = result.CanRetry ? "Failed" : "Synced";
            }

            transaction.UpdatedAt = DateTime.Now;
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PaymentTransactionDTO>(transaction);
        }

        public async Task<PaymentTransactionDTO> MarkCancellationFailedAsync(int paymentTransactionId, string errorMessage)
        {
            var transaction = await _unitOfWork.PaymentTransactionRepository.GetByIdAsync(paymentTransactionId);
            if (transaction == null)
                throw new InvalidOperationException($"결제 트랜잭션 {paymentTransactionId}을(를) 찾을 수 없습니다.");

            transaction.Status = "Completed"; // 취소 실패 시 원상태로
            transaction.SyncStatus = "Failed";
            transaction.CancelReason = $"취소 실패: {errorMessage}";
            transaction.UpdatedAt = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PaymentTransactionDTO>(transaction);
        }

        public async Task<List<PaymentTransactionDTO>> GetFailedSyncTransactionsAsync()
        {
            var failedTransactions = await _unitOfWork.PaymentTransactionRepository.Query()
                .Where(pt => pt.SyncStatus == "Failed")
                .OrderBy(pt => pt.UpdatedAt)
                .Take(10) // 한 번에 10개씩 처리
                .ToListAsync();

            return _mapper.Map<List<PaymentTransactionDTO>>(failedTransactions);
        }

        public async Task<PaymentTransactionDTO> UpdateSyncStatusAsync(int paymentTransactionId, string syncStatus)
        {
            var transaction = await _unitOfWork.PaymentTransactionRepository.GetByIdAsync(paymentTransactionId);
            if (transaction == null)
                throw new InvalidOperationException($"결제 트랜잭션 {paymentTransactionId}을(를) 찾을 수 없습니다.");

            transaction.SyncStatus = syncStatus;
            transaction.UpdatedAt = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PaymentTransactionDTO>(transaction);
        }

        public async Task<decimal> GetTotalSalesAsync(PaymentHistoryFilterDTO filter)
        {
            filter.SetDefaults();

            var query = _unitOfWork.PaymentTransactionRepository.Query()
                .Where(pt => pt.Status == "Completed");

            if (filter.StartDate.HasValue)
                query = query.Where(pt => pt.PaymentDate >= filter.StartDate.Value);
            
            if (filter.EndDate.HasValue)
                query = query.Where(pt => pt.PaymentDate <= filter.EndDate.Value);

            if (!string.IsNullOrEmpty(filter.PaymentMethod))
                query = query.Where(pt => pt.PaymentMethod == filter.PaymentMethod);

            return await query.SumAsync(pt => pt.Amount);
        }

        public async Task<Dictionary<string, decimal>> GetSalesByPaymentMethodAsync(PaymentHistoryFilterDTO filter)
        {
            filter.SetDefaults();

            var query = _unitOfWork.PaymentTransactionRepository.Query()
                .Where(pt => pt.Status == "Completed");

            if (filter.StartDate.HasValue)
                query = query.Where(pt => pt.PaymentDate >= filter.StartDate.Value);
            
            if (filter.EndDate.HasValue)
                query = query.Where(pt => pt.PaymentDate <= filter.EndDate.Value);

            var result = await query
                .GroupBy(pt => pt.PaymentMethod)
                .Select(g => new { Method = g.Key, Total = g.Sum(pt => pt.Amount) })
                .ToDictionaryAsync(x => x.Method, x => x.Total);

            return result;
        }

        public async Task<int> GetTransactionCountAsync(PaymentHistoryFilterDTO filter)
        {
            filter.SetDefaults();

            var query = _unitOfWork.OrderRepository.Query()
                .Where(o => o.Status == "Completed");

            if (filter.StartDate.HasValue)
                query = query.Where(o => o.PaymentDate >= filter.StartDate.Value);
            
            if (filter.EndDate.HasValue)
                query = query.Where(o => o.PaymentDate <= filter.EndDate.Value);

            return await query.CountAsync();
        }

        private PaymentHistoryDTO MapToPaymentHistoryDTO(Order order)
        {
            var dto = new PaymentHistoryDTO
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                TableId = order.TableId,
                TableName = order.Table?.TableName,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                OrderStatus = order.Status,
                OrderDetails = _mapper.Map<List<OrderDetailDTO>>(order.OrderDetails),
                PaymentTransactions = _mapper.Map<List<PaymentTransactionDTO>>(order.PaymentTransactions)
            };

            return dto;
        }
    }
}