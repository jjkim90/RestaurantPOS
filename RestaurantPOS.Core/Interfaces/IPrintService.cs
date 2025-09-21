using RestaurantPOS.Core.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantPOS.Core.Interfaces
{
    public interface IPrintService
    {
        Task<bool> PrintReceiptAsync(OrderDTO order);
        Task<bool> PrintKitchenOrderAsync(OrderDTO order, IEnumerable<OrderDetailDTO> newItems);
    }
}