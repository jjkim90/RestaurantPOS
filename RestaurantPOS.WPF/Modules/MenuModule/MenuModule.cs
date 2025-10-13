using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using RestaurantPOS.WPF.Modules.MenuModule.Services;
using RestaurantPOS.WPF.Modules.MenuModule.ViewModels;
using RestaurantPOS.WPF.Modules.MenuModule.Views;

namespace RestaurantPOS.WPF.Modules.MenuModule
{
    public class MenuModule : IModule
    {
        private readonly IRegionManager _regionManager;

        public MenuModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // View는 RegisterTypes에서 Navigation을 위해 등록되므로 여기서는 아무것도 하지 않음
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register views for navigation
            containerRegistry.RegisterForNavigation<MenuManagementView, MenuManagementViewModel>();
            
            // Register services
            containerRegistry.RegisterScoped<IMenuUIService, MenuUIService>();
        }
    }
}