using DevExpress.Xpf.Core;
using RestaurantPOS.Core.Interfaces;
using RestaurantPOS.WPF.Modules.TableModule.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace RestaurantPOS.WPF.Modules.TableModule.Services
{
    public class TableUIService : ITableUIService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TableUIService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task ShowTableOptionsAsync(TableViewModel table)
        {
            string message = "작업을 선택하세요:\n\n";
            
            if (table.StatusText == "빈 테이블")
            {
                message += "1. 새 주문 시작\n2. 테이블 예약";
                var result = DXMessageBox.Show(
                    message,
                    $"테이블 {table.TableNumber}",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    await ChangeTableStatusAsync(table.TableId, Core.Enums.TableStatus.Occupied);
                    await MoveToOrderScreenAsync(table.TableId);
                }
                else if (result == MessageBoxResult.No)
                {
                    await ChangeTableStatusAsync(table.TableId, Core.Enums.TableStatus.Reserved);
                }
            }
            else if (table.StatusText == "사용중")
            {
                message += "1. 주문 보기\n2. 테이블 비우기";
                var result = DXMessageBox.Show(
                    message,
                    $"테이블 {table.TableNumber}",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    await MoveToOrderScreenAsync(table.TableId);
                }
                else if (result == MessageBoxResult.No)
                {
                    await ChangeTableStatusAsync(table.TableId, Core.Enums.TableStatus.Available);
                }
            }
            else
            {
                var result = DXMessageBox.Show(
                    "테이블 상태를 변경하시겠습니까?",
                    $"테이블 {table.TableNumber}",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    await ChangeTableStatusAsync(table.TableId, Core.Enums.TableStatus.Available);
                }
            }
        }

        public async Task ChangeTableStatusAsync(int tableId, Core.Enums.TableStatus newStatus)
        {
            try
            {
                var table = await _unitOfWork.TableRepository.GetByIdAsync(tableId);
                if (table != null)
                {
                    table.TableStatus = newStatus;
                    table.UpdatedAt = DateTime.Now;
                    
                    _unitOfWork.TableRepository.Update(table);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log error
                System.Windows.MessageBox.Show($"테이블 상태 변경 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task MoveToOrderScreenAsync(int tableId)
        {
            // TODO: Navigate to order screen with table ID
            // This will be implemented when order module is created
            await Task.CompletedTask;
            
            System.Windows.MessageBox.Show($"주문 화면으로 이동 (테이블 {tableId})", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}