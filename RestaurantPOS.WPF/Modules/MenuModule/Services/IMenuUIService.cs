using RestaurantPOS.Core.DTOs;
using System.Threading.Tasks;

namespace RestaurantPOS.WPF.Modules.MenuModule.Services
{
    public interface IMenuUIService
    {
        Task<CategoryDto?> ShowCategoryEditDialogAsync(CategoryDto? category = null);
        Task<MenuItemDto?> ShowMenuItemEditDialogAsync(MenuItemDto? menuItem = null, int? defaultCategoryId = null);
        Task<bool> ShowDeleteConfirmationAsync(string itemName, string itemType = "item");
    }
}