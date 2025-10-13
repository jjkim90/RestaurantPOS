using Prism.Commands;
using Prism.Ioc;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Interfaces;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RestaurantPOS.WPF.Modules.MenuModule.ViewModels
{
    public class MenuItemEditDialogViewModel : Prism.Mvvm.BindableBase
    {
        private readonly IMenuManagementService _menuManagementService;
        
        private int _menuItemId;
        private int _categoryId;
        private string _itemName = string.Empty;
        private decimal _price = 1000; // 기본값을 1000원으로 설정
        private string? _description;
        private bool _isAvailable = true;
        private bool _isEditMode;

        public ObservableCollection<CategoryDto> Categories { get; }

        public int CategoryId
        {
            get => _categoryId;
            set => SetProperty(ref _categoryId, value);
        }

        public string ItemName
        {
            get => _itemName;
            set => SetProperty(ref _itemName, value);
        }

        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public bool IsAvailable
        {
            get => _isAvailable;
            set => SetProperty(ref _isAvailable, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            private set => SetProperty(ref _isEditMode, value);
        }

        public string Title => IsEditMode ? "Edit Menu Item" : "Add Menu Item";

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Dialog result properties
        public bool DialogResult { get; private set; }

        public MenuItemEditDialogViewModel()
        {
            var containerProvider = Prism.Ioc.ContainerLocator.Container;
            _menuManagementService = containerProvider.Resolve<IMenuManagementService>();
            
            Categories = new ObservableCollection<CategoryDto>();
            
            SaveCommand = new DelegateCommand(Save, CanSave)
                .ObservesProperty(() => ItemName)
                .ObservesProperty(() => Price)
                .ObservesProperty(() => CategoryId);
            CancelCommand = new DelegateCommand(Cancel);
        }

        public void SetMenuItem(MenuItemDto menuItem)
        {
            _menuItemId = menuItem.MenuItemId;
            CategoryId = menuItem.CategoryId;
            ItemName = menuItem.ItemName;
            Price = menuItem.Price;
            Description = menuItem.Description;
            IsAvailable = menuItem.IsAvailable;
            IsEditMode = true;
        }

        public MenuItemDto GetMenuItem()
        {
            return new MenuItemDto
            {
                MenuItemId = _menuItemId,
                CategoryId = CategoryId,
                ItemName = ItemName,
                Price = Price,
                Description = Description,
                IsAvailable = IsAvailable
            };
        }

        public async Task LoadCategoriesAsync()
        {
            var categories = await _menuManagementService.GetAllCategoriesAsync();
            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            // If no category is selected and we have categories, select the first one
            if (CategoryId == 0 && Categories.Any())
            {
                CategoryId = Categories.First().CategoryId;
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(ItemName) && Price >= 0 && CategoryId > 0;
        }

        private void Save()
        {
            DialogResult = true;
        }

        private void Cancel()
        {
            DialogResult = false;
        }
    }
}