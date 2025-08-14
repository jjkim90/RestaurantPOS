using RestaurantPOS.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantPOS.Core.Interfaces
{
    public interface IMenuCacheService
    {
        Task<IEnumerable<Category>> GetCategoriesAsync();
        Task<IEnumerable<MenuItem>> GetMenuItemsByCategoryAsync(int categoryId);
        void InvalidateCache();
    }
}