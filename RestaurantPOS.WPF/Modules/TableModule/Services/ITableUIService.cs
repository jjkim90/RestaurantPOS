using RestaurantPOS.WPF.Modules.TableModule.ViewModels;
using System.Threading.Tasks;

namespace RestaurantPOS.WPF.Modules.TableModule.Services
{
    public interface ITableUIService
    {
        Task ShowTableOptionsAsync(TableViewModel table);
        Task ChangeTableStatusAsync(int tableId, Core.Enums.TableStatus newStatus);
        Task MoveToOrderScreenAsync(int tableId);
    }
}