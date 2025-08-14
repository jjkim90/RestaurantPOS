using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using RestaurantPOS.WPF.Modules.OrderModule.ViewModels;
using RestaurantPOS.WPF.Modules.OrderModule.Views;

namespace RestaurantPOS.WPF.Modules.OrderModule
{
    public class OrderModule : IModule
    {
        private readonly IRegionManager _regionManager;

        public OrderModule(IRegionManager regionManager)
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
            containerRegistry.RegisterForNavigation<OrderManagementView, OrderManagementViewModel>();
        }
    }
}