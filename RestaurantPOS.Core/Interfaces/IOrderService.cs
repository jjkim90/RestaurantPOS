using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Entities;
using System.Threading.Tasks;

namespace RestaurantPOS.Core.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDTO> CreateOrderAsync(int tableId, OrderItemDTO[] orderItems);
        Task<OrderDTO> GetActiveOrderByTableIdAsync(int tableId);
        Task UpdateOrderStatusAsync(int orderId, string status);
        Task<OrderDTO> ProcessPaymentAsync(int orderId, string paymentMethod);
        Task<string> GenerateOrderNumberAsync();
    }
}