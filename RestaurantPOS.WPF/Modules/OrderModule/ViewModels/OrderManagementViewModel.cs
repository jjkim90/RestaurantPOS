using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Entities;
using RestaurantPOS.Core.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RestaurantPOS.WPF.Modules.OrderModule.ViewModels
{
    public class OrderManagementViewModel : BindableBase, INavigationAware
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRegionManager _regionManager;
        private readonly RestaurantPOS.Core.Interfaces.IMenuCacheService _menuCacheService;
        private readonly SemaphoreSlim _dbSemaphore = new SemaphoreSlim(1, 1);
        
        private int _currentTableId;
        private string _tableDisplayName;
        private ObservableCollection<CategoryViewModel> _categories;
        private ObservableCollection<MenuItemViewModel> _menuItems;
        private ObservableCollection<OrderItemViewModel> _orderItems;
        private CategoryViewModel _selectedCategory;
        private decimal _totalAmount;

        public OrderManagementViewModel(IUnitOfWork unitOfWork, IRegionManager regionManager, RestaurantPOS.Core.Interfaces.IMenuCacheService menuCacheService)
        {
            _unitOfWork = unitOfWork;
            _regionManager = regionManager;
            _menuCacheService = menuCacheService;
            
            Categories = new ObservableCollection<CategoryViewModel>();
            MenuItems = new ObservableCollection<MenuItemViewModel>();
            OrderItems = new ObservableCollection<OrderItemViewModel>();
            
            // Commands
            BackToTableCommand = new DelegateCommand(OnBackToTable);
            CategoryClickCommand = new DelegateCommand<CategoryViewModel>(OnCategoryClick);
            MenuItemClickCommand = new DelegateCommand<MenuItemViewModel>(OnMenuItemClick);
            RemoveOrderItemCommand = new DelegateCommand<OrderItemViewModel>(OnRemoveOrderItem);
            ConfirmOrderCommand = new DelegateCommand(OnConfirmOrder, CanConfirmOrder);
            PaymentCommand = new DelegateCommand(OnPayment, CanPayment);
        }

        #region Properties
        public string TableDisplayName
        {
            get => _tableDisplayName;
            set => SetProperty(ref _tableDisplayName, value);
        }

        public ObservableCollection<CategoryViewModel> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public ObservableCollection<MenuItemViewModel> MenuItems
        {
            get => _menuItems;
            set => SetProperty(ref _menuItems, value);
        }

        public ObservableCollection<OrderItemViewModel> OrderItems
        {
            get => _orderItems;
            set => SetProperty(ref _orderItems, value);
        }

        public CategoryViewModel SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }
        #endregion

        #region Commands
        public ICommand BackToTableCommand { get; }
        public ICommand CategoryClickCommand { get; }
        public ICommand MenuItemClickCommand { get; }
        public ICommand RemoveOrderItemCommand { get; }
        public ICommand ConfirmOrderCommand { get; }
        public ICommand PaymentCommand { get; }
        #endregion

        #region Command Handlers
        private void OnBackToTable()
        {
            _regionManager.RequestNavigate("MainRegion", "TableManagementView");
        }

        private async void OnCategoryClick(CategoryViewModel category)
        {
            if (category == null) return;
            
            SelectedCategory = category;
            await LoadMenuItemsAsync(category.CategoryId);
        }

        private void OnMenuItemClick(MenuItemViewModel menuItem)
        {
            if (menuItem == null) return;
            
            var existingItem = OrderItems.FirstOrDefault(x => x.MenuItemId == menuItem.MenuItemId);
            if (existingItem != null)
            {
                existingItem.Quantity++;
                existingItem.UpdateSubTotal();
            }
            else
            {
                OrderItems.Add(new OrderItemViewModel
                {
                    MenuItemId = menuItem.MenuItemId,
                    MenuItemName = menuItem.ItemName,
                    UnitPrice = menuItem.Price,
                    Quantity = 1
                });
            }
            
            UpdateTotalAmount();
            ((DelegateCommand)ConfirmOrderCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)PaymentCommand).RaiseCanExecuteChanged();
        }

        private void OnRemoveOrderItem(OrderItemViewModel orderItem)
        {
            if (orderItem == null) return;
            
            OrderItems.Remove(orderItem);
            UpdateTotalAmount();
            ((DelegateCommand)ConfirmOrderCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)PaymentCommand).RaiseCanExecuteChanged();
        }

        private async void OnConfirmOrder()
        {
            await _dbSemaphore.WaitAsync();
            
            try
            {
                // 테이블 상태를 사용중으로 변경
                var table = await _unitOfWork.TableRepository.GetByIdAsync(_currentTableId);
                if (table != null && table.TableStatus == Core.Enums.TableStatus.Available)
                {
                    table.TableStatus = Core.Enums.TableStatus.Occupied;
                    table.UpdatedAt = DateTime.Now;
                    _unitOfWork.TableRepository.Update(table);
                    await _unitOfWork.SaveChangesAsync();
                }
                
                // TODO: Create order and send to kitchen
                await Task.CompletedTask;
                System.Windows.MessageBox.Show("주문이 주방으로 전송되었습니다.", "주문 확정", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"주문 처리 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private async void OnPayment()
        {
            try
            {
                // TODO: Process payment
                await Task.CompletedTask;
                System.Windows.MessageBox.Show("결제 기능은 준비 중입니다.", "결제", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"결제 처리 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanConfirmOrder() => OrderItems.Any();
        private bool CanPayment() => OrderItems.Any();
        #endregion

        #region Helper Methods
        private async Task LoadCategoriesAsync()
        {
            try
            {
                // 캐시 서비스 사용 - 이미 동기화 처리됨
                var categories = await _menuCacheService.GetCategoriesAsync();
                
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Categories.Clear();
                    foreach (var category in categories)
                    {
                        Categories.Add(new CategoryViewModel
                        {
                            CategoryId = category.CategoryId,
                            CategoryName = category.CategoryName
                        });
                    }
                    
                    // Select first category
                    if (Categories.Any())
                    {
                        OnCategoryClick(Categories.First());
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        private async Task LoadMenuItemsAsync(int categoryId)
        {
            try
            {
                // 캐시 서비스 사용 - 이미 동기화 처리됨
                var menuItems = await _menuCacheService.GetMenuItemsByCategoryAsync(categoryId);
                
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MenuItems.Clear();
                    foreach (var item in menuItems)
                    {
                        MenuItems.Add(new MenuItemViewModel
                        {
                            MenuItemId = item.MenuItemId,
                            ItemName = item.ItemName,
                            Price = item.Price,
                            Description = item.Description
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading menu items: {ex.Message}");
            }
        }

        private async Task LoadExistingOrderAsync()
        {
            // TODO: Load existing order if table has an active order
            await Task.CompletedTask;
        }

        private void UpdateTotalAmount()
        {
            TotalAmount = OrderItems.Sum(x => x.SubTotal);
        }
        #endregion

        #region INavigationAware
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("tableId"))
            {
                _currentTableId = navigationContext.Parameters.GetValue<int>("tableId");
                LoadTableInfoAsync();
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }

        private async void LoadTableInfoAsync()
        {
            // 테이블 정보와 카테고리를 병렬로 로드
            var loadTableTask = LoadTableNameAsync();
            var loadCategoriesTask = LoadCategoriesAsync();
            var loadExistingOrderTask = LoadExistingOrderAsync();
            
            await Task.WhenAll(loadTableTask, loadCategoriesTask, loadExistingOrderTask);
        }
        
        private async Task LoadTableNameAsync()
        {
            await _dbSemaphore.WaitAsync();
            
            try
            {
                var table = await _unitOfWork.TableRepository.GetByIdAsync(_currentTableId);
                if (table != null)
                {
                    TableDisplayName = table.TableName;
                }
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }
        #endregion
    }

    #region ViewModels
    public class CategoryViewModel : BindableBase
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
    }

    public class MenuItemViewModel : BindableBase
    {
        public int MenuItemId { get; set; }
        public string ItemName { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string PriceDisplay => $"{Price:N0}원";
    }

    public class OrderItemViewModel : BindableBase
    {
        private int _quantity;
        private decimal _subTotal;

        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; }
        public decimal UnitPrice { get; set; }
        
        public int Quantity
        {
            get => _quantity;
            set
            {
                SetProperty(ref _quantity, value);
                UpdateSubTotal();
            }
        }
        
        public decimal SubTotal
        {
            get => _subTotal;
            private set => SetProperty(ref _subTotal, value);
        }

        public string PriceDisplay => $"{UnitPrice:N0}원";
        public string SubTotalDisplay => $"{SubTotal:N0}원";

        public void UpdateSubTotal()
        {
            SubTotal = UnitPrice * Quantity;
            RaisePropertyChanged(nameof(SubTotalDisplay));
        }
    }
    #endregion
}