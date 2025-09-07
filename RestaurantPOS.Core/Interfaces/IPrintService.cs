using RestaurantPOS.Core.DTOs;
using System.Threading.Tasks;

namespace RestaurantPOS.Core.Interfaces
{
    public interface IPrintService
    {
        Task<bool> PrintReceiptAsync(OrderDTO order);
    }
}