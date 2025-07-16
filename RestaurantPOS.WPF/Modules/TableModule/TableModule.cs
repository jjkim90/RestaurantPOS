using Prism.Ioc;
using Prism.Modularity;
using RestaurantPOS.WPF.Modules.TableModule.Views;
using RestaurantPOS.WPF.Modules.TableModule.ViewModels;
using RestaurantPOS.WPF.Modules.TableModule.Services;

namespace RestaurantPOS.WPF.Modules.TableModule
{
    public class TableModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // Navigation will be handled differently without IRegionManager
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<TableManagementView>();
            
            // Services
            containerRegistry.RegisterScoped<ITableUIService, TableUIService>();
        }
    }
}