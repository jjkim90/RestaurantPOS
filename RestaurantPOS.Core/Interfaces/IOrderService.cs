using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantPOS.Core.Interfaces
{
    public interface IOrderService
    {
        // 기존 메서드들
        Task<OrderDTO> CreateOrderAsync(int tableId, OrderItemDTO[] orderItems);
        Task<OrderDTO> GetActiveOrderByTableIdAsync(int tableId);
        Task UpdateOrderStatusAsync(int orderId, string status);
        Task<OrderDTO> ProcessPaymentAsync(int orderId, string paymentMethod);
        Task<string> GenerateOrderNumberAsync();
        
        // 새로운 메서드들
        Task<OrderDTO> GetOrCreateActiveOrderAsync(int tableId);  // 활성 주문 조회 또는 생성
        Task<OrderDTO> AddOrderItemsAsync(int orderId, IEnumerable<OrderItemDTO> orderItems);  // 기존 주문에 항목 추가
        Task<OrderDTO> ConfirmPendingItemsAsync(int orderId);  // 대기 중인 항목만 확정
        Task<OrderDTO> GetOrderWithDetailsAsync(int orderId);  // 상세 정보 포함한 주문 조회
        Task<IEnumerable<OrderDetailDTO>> GetPendingOrderDetailsAsync(int orderId);  // 대기 중인 주문 항목 조회
        Task<IEnumerable<OrderDetailDTO>> GetConfirmedOrderDetailsAsync(int orderId);  // 확정된 주문 항목 조회
    }
}