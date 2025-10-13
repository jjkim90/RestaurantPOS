using Prism.Commands;
using RestaurantPOS.Core.DTOs;
using System.Windows.Input;

namespace RestaurantPOS.WPF.Modules.MenuModule.ViewModels
{
    public class CategoryEditDialogViewModel : Prism.Mvvm.BindableBase
    {
        private int _categoryId;
        private string _categoryName = string.Empty;
        private int _displayOrder;
        private bool _isActive = true;
        private bool _isEditMode;

        public string CategoryName
        {
            get => _categoryName;
            set => SetProperty(ref _categoryName, value);
        }

        public int DisplayOrder
        {
            get => _displayOrder;
            set => SetProperty(ref _displayOrder, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            private set => SetProperty(ref _isEditMode, value);
        }

        public string Title => IsEditMode ? "Edit Category" : "Add Category";

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Dialog result properties
        public bool DialogResult { get; private set; }

        public CategoryEditDialogViewModel()
        {
            SaveCommand = new DelegateCommand(Save, CanSave)
                .ObservesProperty(() => CategoryName);
            CancelCommand = new DelegateCommand(Cancel);
        }

        public void SetCategory(CategoryDto category)
        {
            _categoryId = category.CategoryId;
            CategoryName = category.CategoryName;
            DisplayOrder = category.DisplayOrder;
            IsActive = category.IsActive;
            IsEditMode = true;
        }

        public CategoryDto GetCategory()
        {
            return new CategoryDto
            {
                CategoryId = _categoryId,
                CategoryName = CategoryName,
                DisplayOrder = DisplayOrder,
                IsActive = IsActive
            };
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(CategoryName);
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