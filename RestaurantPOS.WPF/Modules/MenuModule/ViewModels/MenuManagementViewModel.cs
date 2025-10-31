using DevExpress.Xpf.Core;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Interfaces;
using RestaurantPOS.WPF.Modules.MenuModule.Services;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RestaurantPOS.WPF.Modules.MenuModule.ViewModels
{
    public class MenuManagementViewModel : Prism.Mvvm.BindableBase, INavigationAware
    {
        private readonly IMenuManagementService _menuManagementService;
        private readonly IMenuUIService _menuUIService;
        private readonly ILogger _logger;

        #region Properties

        private ObservableCollection<CategoryDto> _categories;
        public ObservableCollection<CategoryDto> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        private CategoryDto? _selectedCategory;
        public CategoryDto? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    LoadMenuItemsCommand.Execute(null);
                    RaisePropertyChanged(nameof(CanEditCategory));
                    RaisePropertyChanged(nameof(CanDeleteCategory));
                    RaisePropertyChanged(nameof(CanAddMenuItem));
                }
            }
        }

        private ObservableCollection<MenuItemDto> _menuItems;
        public ObservableCollection<MenuItemDto> MenuItems
        {
            get => _menuItems;
            set => SetProperty(ref _menuItems, value);
        }

        private MenuItemDto? _selectedMenuItem;
        public MenuItemDto? SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                SetProperty(ref _selectedMenuItem, value);
                RaisePropertyChanged(nameof(CanEditMenuItem));
                RaisePropertyChanged(nameof(CanDeleteMenuItem));
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool CanEditCategory => SelectedCategory != null;
        public bool CanDeleteCategory => SelectedCategory != null;
        public bool CanAddMenuItem => SelectedCategory != null;
        public bool CanEditMenuItem => SelectedMenuItem != null;
        public bool CanDeleteMenuItem => SelectedMenuItem != null;

        #endregion

        #region Commands

        public ICommand LoadCategoriesCommand { get; }
        public ICommand LoadMenuItemsCommand { get; }
        public ICommand AddCategoryCommand { get; }
        public ICommand EditCategoryCommand { get; }
        public ICommand DeleteCategoryCommand { get; }
        public ICommand AddMenuItemCommand { get; }
        public ICommand EditMenuItemCommand { get; }
        public ICommand DeleteMenuItemCommand { get; }

        #endregion

        public MenuManagementViewModel(
            IMenuManagementService menuManagementService,
            IMenuUIService menuUIService,
            ILogger logger)
        {
            _menuManagementService = menuManagementService;
            _menuUIService = menuUIService;
            _logger = logger;

            Categories = new ObservableCollection<CategoryDto>();
            MenuItems = new ObservableCollection<MenuItemDto>();

            // Initialize Commands
            LoadCategoriesCommand = new Prism.Commands.DelegateCommand(async () => await LoadCategoriesAsync());
            LoadMenuItemsCommand = new Prism.Commands.DelegateCommand(async () => await LoadMenuItemsAsync());
            AddCategoryCommand = new Prism.Commands.DelegateCommand(async () => await AddCategoryAsync());
            EditCategoryCommand = new Prism.Commands.DelegateCommand(async () => await EditCategoryAsync()).ObservesCanExecute(() => CanEditCategory);
            DeleteCategoryCommand = new Prism.Commands.DelegateCommand(async () => await DeleteCategoryAsync()).ObservesCanExecute(() => CanDeleteCategory);
            AddMenuItemCommand = new Prism.Commands.DelegateCommand(async () => await AddMenuItemAsync()).ObservesCanExecute(() => CanAddMenuItem);
            EditMenuItemCommand = new Prism.Commands.DelegateCommand(async () => await EditMenuItemAsync()).ObservesCanExecute(() => CanEditMenuItem);
            DeleteMenuItemCommand = new Prism.Commands.DelegateCommand(async () => await DeleteMenuItemAsync()).ObservesCanExecute(() => CanDeleteMenuItem);
        }

        #region Methods

        private async Task LoadCategoriesAsync()
        {
            try
            {
                IsLoading = true;
                var categories = await _menuManagementService.GetAllCategoriesAsync();
                Categories.Clear();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading categories");
                DXMessageBox.Show($"Failed to load categories: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadMenuItemsAsync()
        {
            if (SelectedCategory == null)
            {
                MenuItems.Clear();
                return;
            }

            try
            {
                IsLoading = true;
                var menuItems = await _menuManagementService.GetMenuItemsByCategoryAsync(SelectedCategory.CategoryId);
                MenuItems.Clear();
                foreach (var menuItem in menuItems)
                {
                    MenuItems.Add(menuItem);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading menu items for category: {CategoryName}", SelectedCategory.CategoryName);
                DXMessageBox.Show($"Failed to load menu items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddCategoryAsync()
        {
            try
            {
                var newCategory = await _menuUIService.ShowCategoryEditDialogAsync();
                if (newCategory != null)
                {
                    var createdCategory = await _menuManagementService.CreateCategoryAsync(newCategory);
                    Categories.Add(createdCategory);
                    SelectedCategory = createdCategory;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error adding category");
                DXMessageBox.Show($"Failed to add category: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task EditCategoryAsync()
        {
            if (SelectedCategory == null) return;

            try
            {
                var editedCategory = await _menuUIService.ShowCategoryEditDialogAsync(SelectedCategory);
                if (editedCategory != null)
                {
                    var updatedCategory = await _menuManagementService.UpdateCategoryAsync(editedCategory);
                    var index = Categories.IndexOf(SelectedCategory);
                    Categories[index] = updatedCategory;
                    SelectedCategory = updatedCategory;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error editing category");
                DXMessageBox.Show($"Failed to edit category: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteCategoryAsync()
        {
            if (SelectedCategory == null) return;

            try
            {
                var confirmed = await _menuUIService.ShowDeleteConfirmationAsync(SelectedCategory.CategoryName, "category");
                if (confirmed)
                {
                    await _menuManagementService.DeleteCategoryAsync(SelectedCategory.CategoryId);
                    Categories.Remove(SelectedCategory);
                    SelectedCategory = null;
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("menu items"))
            {
                DXMessageBox.Show("Cannot delete category with menu items. Please delete all menu items first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error deleting category");
                DXMessageBox.Show($"Failed to delete category: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AddMenuItemAsync()
        {
            if (SelectedCategory == null) return;

            try
            {
                var newMenuItem = await _menuUIService.ShowMenuItemEditDialogAsync(defaultCategoryId: SelectedCategory.CategoryId);
                if (newMenuItem != null)
                {
                    var createdMenuItem = await _menuManagementService.CreateMenuItemAsync(newMenuItem);
                    MenuItems.Add(createdMenuItem);
                    SelectedMenuItem = createdMenuItem;

                    // Update category menu item count
                    SelectedCategory.MenuItemCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error adding menu item");
                DXMessageBox.Show($"Failed to add menu item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task EditMenuItemAsync()
        {
            if (SelectedMenuItem == null) return;

            try
            {
                var editedMenuItem = await _menuUIService.ShowMenuItemEditDialogAsync(SelectedMenuItem);
                if (editedMenuItem != null)
                {
                    var updatedMenuItem = await _menuManagementService.UpdateMenuItemAsync(editedMenuItem);
                    var index = MenuItems.IndexOf(SelectedMenuItem);
                    MenuItems[index] = updatedMenuItem;
                    SelectedMenuItem = updatedMenuItem;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error editing menu item");
                DXMessageBox.Show($"Failed to edit menu item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteMenuItemAsync()
        {
            if (SelectedMenuItem == null) return;

            try
            {
                var confirmed = await _menuUIService.ShowDeleteConfirmationAsync(SelectedMenuItem.ItemName, "menu item");
                if (confirmed)
                {
                    await _menuManagementService.DeleteMenuItemAsync(SelectedMenuItem.MenuItemId);
                    MenuItems.Remove(SelectedMenuItem);
                    SelectedMenuItem = null;

                    // Update category menu item count
                    if (SelectedCategory != null)
                    {
                        SelectedCategory.MenuItemCount--;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error deleting menu item");
                DXMessageBox.Show($"Failed to delete menu item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            LoadCategoriesCommand.Execute(null);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // Cleanup if needed
        }

        #endregion
    }
}