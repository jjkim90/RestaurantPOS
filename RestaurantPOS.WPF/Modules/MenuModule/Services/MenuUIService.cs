using DevExpress.Xpf.Core;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.WPF.Modules.MenuModule.ViewModels;
using RestaurantPOS.WPF.Modules.MenuModule.Views;
using System.Threading.Tasks;

namespace RestaurantPOS.WPF.Modules.MenuModule.Services
{
    public class MenuUIService : IMenuUIService
    {
        public Task<CategoryDto?> ShowCategoryEditDialogAsync(CategoryDto? category = null)
        {
            var viewModel = new CategoryEditDialogViewModel();
            
            if (category != null)
            {
                viewModel.SetCategory(category);
            }

            var dialog = new CategoryEditDialog { DataContext = viewModel };

            var result = dialog.ShowDialog();
            
            return Task.FromResult(result == true ? viewModel.GetCategory() : null);
        }

        public async Task<MenuItemDto?> ShowMenuItemEditDialogAsync(MenuItemDto? menuItem = null, int? defaultCategoryId = null)
        {
            var viewModel = new MenuItemEditDialogViewModel();
            
            // 먼저 카테고리를 로드
            await viewModel.LoadCategoriesAsync();
            
            if (menuItem != null)
            {
                // SetMenuItemAsync에서 카테고리 로드 제거
                viewModel.SetMenuItem(menuItem);
            }
            else if (defaultCategoryId.HasValue)
            {
                viewModel.CategoryId = defaultCategoryId.Value;
            }

            var dialog = new MenuItemEditDialog { DataContext = viewModel };

            var result = dialog.ShowDialog();
            
            return result == true ? viewModel.GetMenuItem() : null;
        }

        public async Task<bool> ShowDeleteConfirmationAsync(string itemName, string itemType = "item")
        {
            var message = $"Are you sure you want to delete the {itemType} '{itemName}'?";
            var title = $"Delete {itemType}";
            
            var result = DXMessageBox.Show(message, title, 
                System.Windows.MessageBoxButton.YesNo, 
                System.Windows.MessageBoxImage.Warning);
            
            return await Task.FromResult(result == System.Windows.MessageBoxResult.Yes);
        }
    }
}