using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using RestaurantPOS.WPF.Modules.SettingsModule.ViewModels;
using RestaurantPOS.WPF.Modules.SettingsModule.Views;

namespace RestaurantPOS.WPF.Modules.SettingsModule
{
    public class SettingsModule : IModule
    {
        private readonly IRegionManager _regionManager;

        public SettingsModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<SettingsView, SettingsViewModel>();
            containerRegistry.RegisterForNavigation<TableSettingsView, TableSettingsViewModel>();
        }
    }
}