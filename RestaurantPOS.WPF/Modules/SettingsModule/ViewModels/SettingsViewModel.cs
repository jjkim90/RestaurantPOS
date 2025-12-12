using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;

namespace RestaurantPOS.WPF.Modules.SettingsModule.ViewModels
{
    public class SettingsViewModel : BindableBase, INavigationAware
    {
        private readonly IRegionManager _regionManager;
        private string _currentSettingsView = string.Empty;

        public SettingsViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;

            GoBackCommand = new DelegateCommand(GoBack);
            NavigateToTableSettingsCommand = new DelegateCommand(NavigateToTableSettings);
            NavigateToMenuSettingsCommand = new DelegateCommand(NavigateToMenuSettings);
            NavigateToSystemSettingsCommand = new DelegateCommand(NavigateToSystemSettings);
        }

        #region Properties
        private bool _isTableSettingsSelected;
        public bool IsTableSettingsSelected
        {
            get => _isTableSettingsSelected;
            set => SetProperty(ref _isTableSettingsSelected, value);
        }

        private bool _isMenuSettingsSelected;
        public bool IsMenuSettingsSelected
        {
            get => _isMenuSettingsSelected;
            set => SetProperty(ref _isMenuSettingsSelected, value);
        }

        private bool _isSystemSettingsSelected;
        public bool IsSystemSettingsSelected
        {
            get => _isSystemSettingsSelected;
            set => SetProperty(ref _isSystemSettingsSelected, value);
        }
        #endregion

        #region Commands
        public DelegateCommand GoBackCommand { get; }
        public DelegateCommand NavigateToTableSettingsCommand { get; }
        public DelegateCommand NavigateToMenuSettingsCommand { get; }
        public DelegateCommand NavigateToSystemSettingsCommand { get; }
        #endregion

        #region Methods
        private void GoBack()
        {
            _regionManager.RequestNavigate("MainRegion", "TableManagementView");
        }

        private void NavigateToTableSettings()
        {
            UpdateSelectedMenu("TableSettings");
            _regionManager.RequestNavigate("SettingsContentRegion", "TableSettingsView");
        }

        private void NavigateToMenuSettings()
        {
            UpdateSelectedMenu("MenuSettings");
            _regionManager.RequestNavigate("SettingsContentRegion", "MenuManagementView");
        }

        private void NavigateToSystemSettings()
        {
            UpdateSelectedMenu("SystemSettings");
            // TODO: [Future Feature] 시스템 설정 화면 구현 예정 - v2.0
        }

        private void UpdateSelectedMenu(string menuName)
        {
            IsTableSettingsSelected = menuName == "TableSettings";
            IsMenuSettingsSelected = menuName == "MenuSettings";
            IsSystemSettingsSelected = menuName == "SystemSettings";
            _currentSettingsView = menuName;
        }
        #endregion

        #region INavigationAware
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // Default to table settings
            if (string.IsNullOrEmpty(_currentSettingsView))
            {
                NavigateToTableSettings();
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }
        #endregion
    }
}