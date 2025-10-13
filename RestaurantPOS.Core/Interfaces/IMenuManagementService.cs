using RestaurantPOS.Core.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantPOS.Core.Interfaces
{
    public interface IMenuManagementService
    {
        // Category 관련
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<CategoryDto?> GetCategoryByIdAsync(int categoryId);
        Task<CategoryDto> CreateCategoryAsync(CategoryDto categoryDto);
        Task<CategoryDto> UpdateCategoryAsync(CategoryDto categoryDto);
        Task<bool> DeleteCategoryAsync(int categoryId);

        // MenuItem 관련
        Task<IEnumerable<MenuItemDto>> GetAllMenuItemsAsync();
        Task<IEnumerable<MenuItemDto>> GetMenuItemsByCategoryAsync(int categoryId);
        Task<MenuItemDto?> GetMenuItemByIdAsync(int menuItemId);
        Task<MenuItemDto> CreateMenuItemAsync(MenuItemDto menuItemDto);
        Task<MenuItemDto> UpdateMenuItemAsync(MenuItemDto menuItemDto);
        Task<bool> DeleteMenuItemAsync(int menuItemId);
    }
}