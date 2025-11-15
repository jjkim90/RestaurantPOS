using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using RestaurantPOS.WPF.Modules.PaymentHistoryModule.Views;

namespace RestaurantPOS.WPF.Modules.PaymentHistoryModule
{
    public class PaymentHistoryModule : IModule
    {
        private readonly IRegionManager _regionManager;

        public PaymentHistoryModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 모듈 초기화 시 필요한 작업
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // View 등록
            containerRegistry.RegisterForNavigation<PaymentHistoryView>();
        }
    }
}