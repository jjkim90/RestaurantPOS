using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Entities;
using RestaurantPOS.Core.Interfaces;
using RestaurantPOS.WPF.Modules.OrderModule.Views;
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
        private readonly IOrderService _orderService;
        private readonly IPrintService _printService;
        private readonly SemaphoreSlim _dbSemaphore = new SemaphoreSlim(1, 1);
        
        private int _currentTableId;
        private int _currentOrderId;
        private string _tableDisplayName;
        private ObservableCollection<CategoryViewModel> _categories;
        private ObservableCollection<MenuItemViewModel> _menuItems;
        private ObservableCollection<OrderItemViewModel> _orderItems;
        private ObservableCollection<OrderItemViewModel> _existingOrderItems;  // 기존 주문 항목
        private ObservableCollection<OrderItemViewModel> _newOrderItems;  // 새 주문 항목
        private CategoryViewModel _selectedCategory;
        private decimal _totalAmount;
        private decimal _existingOrderAmount;  // 기존 주문 금액
        private decimal _newOrderAmount;  // 새 주문 금액

        public OrderManagementViewModel(IUnitOfWork unitOfWork, IRegionManager regionManager, RestaurantPOS.Core.Interfaces.IMenuCacheService menuCacheService, IOrderService orderService, IPrintService printService)
        {
            _unitOfWork = unitOfWork;
            _regionManager = regionManager;
            _menuCacheService = menuCacheService;
            _orderService = orderService;
            _printService = printService;
            
            Categories = new ObservableCollection<CategoryViewModel>();
            MenuItems = new ObservableCollection<MenuItemViewModel>();
            OrderItems = new ObservableCollection<OrderItemViewModel>();
            ExistingOrderItems = new ObservableCollection<OrderItemViewModel>();
            NewOrderItems = new ObservableCollection<OrderItemViewModel>();
            
            // Commands
            BackToTableCommand = new DelegateCommand(OnBackToTable);
            CategoryClickCommand = new DelegateCommand<CategoryViewModel>(async (category) => await OnCategoryClickAsync(category));
            MenuItemClickCommand = new DelegateCommand<MenuItemViewModel>(OnMenuItemClick);
            RemoveOrderItemCommand = new DelegateCommand<OrderItemViewModel>(OnRemoveOrderItem);
            ConfirmOrderCommand = new DelegateCommand(OnConfirmOrder, CanConfirmOrder);
            PaymentCommand = new DelegateCommand(OnPayment, CanPayment);
            IncreaseQuantityCommand = new DelegateCommand<OrderItemViewModel>(OnIncreaseQuantity);
            DecreaseQuantityCommand = new DelegateCommand<OrderItemViewModel>(OnDecreaseQuantity);
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
        
        public ObservableCollection<OrderItemViewModel> ExistingOrderItems
        {
            get => _existingOrderItems;
            set => SetProperty(ref _existingOrderItems, value);
        }
        
        public ObservableCollection<OrderItemViewModel> NewOrderItems
        {
            get => _newOrderItems;
            set => SetProperty(ref _newOrderItems, value);
        }
        
        public decimal ExistingOrderAmount
        {
            get => _existingOrderAmount;
            set => SetProperty(ref _existingOrderAmount, value);
        }
        
        public decimal NewOrderAmount
        {
            get => _newOrderAmount;
            set => SetProperty(ref _newOrderAmount, value);
        }
        #endregion

        #region Commands
        public ICommand BackToTableCommand { get; }
        public ICommand CategoryClickCommand { get; }
        public ICommand MenuItemClickCommand { get; }
        public ICommand RemoveOrderItemCommand { get; }
        public ICommand ConfirmOrderCommand { get; }
        public ICommand PaymentCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        #endregion

        #region Command Handlers
        private void OnBackToTable()
        {
            _regionManager.RequestNavigate("MainRegion", "TableManagementView");
        }

        private async Task OnCategoryClickAsync(CategoryViewModel category)
        {
            System.Diagnostics.Debug.WriteLine($"OnCategoryClickAsync called with category: {category?.CategoryName}");
            
            if (category == null) return;
            
            // 이전 선택 해제
            if (SelectedCategory != null)
            {
                SelectedCategory.IsSelected = false;
            }
            
            SelectedCategory = category;
            category.IsSelected = true;
            System.Diagnostics.Debug.WriteLine($"SelectedCategory set to: {SelectedCategory.CategoryName}");
            
            await LoadMenuItemsAsync(category.CategoryId);
        }

        private void OnMenuItemClick(MenuItemViewModel menuItem)
        {
            if (menuItem == null) return;
            
            // 새 주문 항목에서만 찾기
            var existingItem = NewOrderItems.FirstOrDefault(x => x.MenuItemId == menuItem.MenuItemId);
            if (existingItem != null)
            {
                existingItem.Quantity++;
                existingItem.UpdateSubTotal();
            }
            else
            {
                var newItem = new OrderItemViewModel
                {
                    MenuItemId = menuItem.MenuItemId,
                    MenuItemName = menuItem.ItemName,
                    UnitPrice = menuItem.Price,
                    Quantity = 1,
                    IsNewItem = true
                };
                newItem.PropertyChanged += (s, e) => 
                {
                    if (e.PropertyName == nameof(OrderItemViewModel.SubTotal))
                    {
                        UpdateTotalAmount();
                    }
                };
                NewOrderItems.Add(newItem);
            }
            
            UpdateTotalAmount();
            ((DelegateCommand)ConfirmOrderCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)PaymentCommand).RaiseCanExecuteChanged();
        }

        private void OnRemoveOrderItem(OrderItemViewModel orderItem)
        {
            if (orderItem == null) return;
            
            // 새 항목만 삭제 가능
            if (orderItem.IsNewItem && NewOrderItems.Contains(orderItem))
            {
                NewOrderItems.Remove(orderItem);
                UpdateTotalAmount();
                ((DelegateCommand)ConfirmOrderCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)PaymentCommand).RaiseCanExecuteChanged();
            }
        }
        
        private void OnIncreaseQuantity(OrderItemViewModel orderItem)
        {
            if (orderItem == null || !orderItem.IsNewItem) return;
            
            if (orderItem.Quantity < 99)
            {
                orderItem.Quantity++;
            }
        }
        
        private void OnDecreaseQuantity(OrderItemViewModel orderItem)
        {
            if (orderItem == null || !orderItem.IsNewItem) return;
            
            if (orderItem.Quantity > 1)
            {
                orderItem.Quantity--;
            }
        }

        private async void OnConfirmOrder()
        {
            try
            {
                if (!NewOrderItems.Any())
                {
                    System.Windows.MessageBox.Show("추가할 주문 항목이 없습니다.", "알림", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // 새 주문 항목들을 OrderItemDTO로 변환
                var newOrderItems = NewOrderItems.Select(oi => new OrderItemDTO
                {
                    MenuItemId = oi.MenuItemId,
                    MenuItemName = oi.MenuItemName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    SubTotal = oi.SubTotal
                }).ToList();

                OrderDTO updatedOrder;
                
                // 기존 주문이 있으면 항목 추가, 없으면 새 주문 생성
                if (_currentOrderId > 0)
                {
                    // 기존 주문에 항목 추가
                    updatedOrder = await _orderService.AddOrderItemsAsync(_currentOrderId, newOrderItems);
                }
                else
                {
                    // 새 주문 생성
                    var order = await _orderService.GetOrCreateActiveOrderAsync(_currentTableId);
                    _currentOrderId = order.OrderId;
                    updatedOrder = await _orderService.AddOrderItemsAsync(_currentOrderId, newOrderItems);
                    
                    // 첫 주문일 때만 테이블 상태 변경
                    await _dbSemaphore.WaitAsync();
                    try
                    {
                        var table = await _unitOfWork.TableRepository.GetByIdAsync(_currentTableId);
                        if (table != null && table.TableStatus == Core.Enums.TableStatus.Available)
                        {
                            table.TableStatus = Core.Enums.TableStatus.Occupied;
                            table.UpdatedAt = DateTime.Now;
                            table.LastOrderTime = DateTime.Now;
                            _unitOfWork.TableRepository.Update(table);
                            await _unitOfWork.SaveChangesAsync();
                        }
                    }
                    finally
                    {
                        _dbSemaphore.Release();
                    }
                }
                
                // 새 항목들을 주방으로 전송 (확정)
                await _orderService.ConfirmPendingItemsAsync(_currentOrderId);
                
                // 새 주문 항목을 기존 주문 항목으로 이동
                foreach (var item in NewOrderItems)
                {
                    item.IsNewItem = false;
                    ExistingOrderItems.Add(item);
                }
                NewOrderItems.Clear();
                UpdateTotalAmount();
                
                System.Windows.MessageBox.Show($"주문번호 {updatedOrder.OrderNumber}의 추가 주문이 주방으로 전송되었습니다.", "주문 확정", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    
                // 주방 프린터 출력은 OrderService.ConfirmPendingItemsAsync에서 자동으로 처리됨
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"주문 처리 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void OnPayment()
        {
            try
            {
                // 새 주문 항목이 있으면 먼저 확정
                if (NewOrderItems.Any())
                {
                    var result = System.Windows.MessageBox.Show(
                        "확정되지 않은 주문이 있습니다. 먼저 주문을 확정하시겠습니까?", 
                        "주문 확정", 
                        System.Windows.MessageBoxButton.YesNo, 
                        System.Windows.MessageBoxImage.Question);
                    
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        OnConfirmOrder();
                        return;
                    }
                }

                // 결제할 주문이 없는지 확인
                if (_currentOrderId == 0 || !ExistingOrderItems.Any())
                {
                    System.Windows.MessageBox.Show("결제할 주문이 없습니다.", "알림", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // 최신 주문 정보 조회
                var currentOrder = await _orderService.GetOrderWithDetailsAsync(_currentOrderId);
                if (currentOrder == null)
                {
                    System.Windows.MessageBox.Show("주문 정보를 찾을 수 없습니다.", "오류", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                // 결제 다이얼로그 표시
                var paymentViewModel = new PaymentViewModel { Order = currentOrder };
                var paymentDialog = new PaymentDialog { DataContext = paymentViewModel };
                
                bool paymentProcessed = false;
                string paymentMethod = "";

                paymentViewModel.PaymentCompleted += (s, method) =>
                {
                    paymentProcessed = true;
                    paymentMethod = method;
                    paymentDialog.Close();
                };

                paymentViewModel.PaymentCancelled += (s, e) =>
                {
                    paymentDialog.Close();
                };

                paymentDialog.ShowDialog();

                if (paymentProcessed)
                {
                    // 결제 처리
                    var completedOrder = await _orderService.ProcessPaymentAsync(_currentOrderId, paymentMethod);
                    
                    // UI 초기화
                    ExistingOrderItems.Clear();
                    NewOrderItems.Clear();
                    UpdateTotalAmount();
                    _currentOrderId = 0;
                    
                    System.Windows.MessageBox.Show(
                        $"주문번호 {completedOrder.OrderNumber}의 결제가 완료되었습니다.\n결제방법: {paymentMethod}", 
                        "결제 완료", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Information);
                        
                    // 영수증 출력
                    var printResult = await _printService.PrintReceiptAsync(completedOrder);
                    if (!printResult)
                    {
                        var retryResult = System.Windows.MessageBox.Show(
                            "영수증 출력에 실패했습니다. 다시 시도하시겠습니까?", 
                            "출력 실패", 
                            System.Windows.MessageBoxButton.YesNo, 
                            System.Windows.MessageBoxImage.Warning);
                            
                        if (retryResult == System.Windows.MessageBoxResult.Yes)
                        {
                            await _printService.PrintReceiptAsync(completedOrder);
                        }
                    }
                    
                    // 테이블 화면으로 돌아가기
                    _regionManager.RequestNavigate("MainRegion", "TableManagementView");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"결제 처리 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanConfirmOrder() => NewOrderItems.Any();
        private bool CanPayment() => ExistingOrderItems.Any() || NewOrderItems.Any();
        #endregion

        #region Helper Methods
        private async Task LoadCategoriesAsync()
        {
            try
            {
                // 캐시 서비스 사용 - 이미 동기화 처리됨
                var categories = await _menuCacheService.GetCategoriesAsync();
                
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
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
                        var firstCategory = Categories.First();
                        // OnCategoryClick 내부에서 IsSelected를 설정하므로 여기서는 제거
                        await OnCategoryClickAsync(firstCategory);
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
            System.Diagnostics.Debug.WriteLine($"LoadMenuItemsAsync called for categoryId: {categoryId}");
            
            try
            {
                // 캐시 서비스 사용 - 이미 동기화 처리됨
                var menuItems = await _menuCacheService.GetMenuItemsByCategoryAsync(categoryId);
                System.Diagnostics.Debug.WriteLine($"Retrieved {menuItems.Count()} menu items from cache");
                
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
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
                    System.Diagnostics.Debug.WriteLine($"Added {MenuItems.Count} items to MenuItems collection");
                    
                    // 명시적으로 PropertyChanged 이벤트 발생
                    RaisePropertyChanged(nameof(MenuItems));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading menu items: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task LoadExistingOrderAsync()
        {
            try
            {
                var existingOrder = await _orderService.GetActiveOrderByTableIdAsync(_currentTableId);
                if (existingOrder != null)
                {
                    _currentOrderId = existingOrder.OrderId;
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ExistingOrderItems.Clear();
                        NewOrderItems.Clear();
                        
                        // 기존 주문 항목 로드 (모든 항목)
                        foreach (var detail in existingOrder.OrderDetails)
                        {
                            var orderItem = new OrderItemViewModel
                            {
                                MenuItemId = detail.MenuItemId,
                                MenuItemName = detail.MenuItemName,
                                UnitPrice = detail.UnitPrice,
                                Quantity = detail.Quantity,
                                IsNewItem = false
                            };
                            orderItem.PropertyChanged += (s, e) => 
                            {
                                if (e.PropertyName == nameof(OrderItemViewModel.SubTotal))
                                {
                                    UpdateTotalAmount();
                                }
                            };
                            orderItem.UpdateSubTotal();
                            ExistingOrderItems.Add(orderItem);
                        }
                        
                        UpdateTotalAmount();
                        ((DelegateCommand)ConfirmOrderCommand).RaiseCanExecuteChanged();
                        ((DelegateCommand)PaymentCommand).RaiseCanExecuteChanged();
                    });
                }
                else
                {
                    // 주문이 없으면 초기화
                    _currentOrderId = 0;
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ExistingOrderItems.Clear();
                        NewOrderItems.Clear();
                        UpdateTotalAmount();
                        ((DelegateCommand)ConfirmOrderCommand).RaiseCanExecuteChanged();
                        ((DelegateCommand)PaymentCommand).RaiseCanExecuteChanged();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading existing order: {ex.Message}");
            }
        }

        private void UpdateTotalAmount()
        {
            ExistingOrderAmount = ExistingOrderItems.Sum(x => x.SubTotal);
            NewOrderAmount = NewOrderItems.Sum(x => x.SubTotal);
            TotalAmount = ExistingOrderAmount + NewOrderAmount;
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

        public void OnNavigatedFrom(NavigationContext navigationContext) 
        {
            // 다른 화면으로 이동할 때 미확정 주문 초기화
            NewOrderItems.Clear();
            UpdateTotalAmount();
        }

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
        private bool _isSelected;
        
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
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
        private bool _isNewItem;

        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; }
        public decimal UnitPrice { get; set; }
        
        public bool IsNewItem
        {
            get => _isNewItem;
            set => SetProperty(ref _isNewItem, value);
        }
        
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