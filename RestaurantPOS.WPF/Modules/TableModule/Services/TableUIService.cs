using DevExpress.Xpf.Core;
using Microsoft.EntityFrameworkCore;
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
        private readonly IRegionManager _regionManager;

        public TableUIService(IUnitOfWork unitOfWork, IRegionManager regionManager)
        {
            _unitOfWork = unitOfWork;
            _regionManager = regionManager;
        }

        public async Task ShowTableOptionsAsync(TableViewModel table)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ShowTableOptionsAsync called for table: {table.DisplayName}, Status: {table.StatusText}");
                
                // 빈 테이블 클릭 시 주문 화면으로 이동 (상태 변경하지 않음)
                if (table.StatusText == "빈 테이블")
                {
                    // 주문이 확정될 때만 상태를 변경하도록 수정
                    await MoveToOrderScreenAsync(table.TableId);
                }
                // 사용중 테이블 클릭 시 바로 주문 화면으로 이동
                else if (table.StatusText == "사용중")
                {
                    await MoveToOrderScreenAsync(table.TableId);
                }
                // 예약됨 또는 정리중 상태의 테이블은 상태 변경 확인
                else
                {
                    var result = DXMessageBox.Show(
                        $"테이블을 사용 가능 상태로 변경하시겠습니까?\n현재 상태: {table.StatusText}",
                        $"테이블 {table.TableNumber}",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        await ChangeTableStatusAsync(table.TableId, Core.Enums.TableStatus.Available);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ShowTableOptionsAsync: {ex}");
                System.Windows.MessageBox.Show($"테이블 처리 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ChangeTableStatusAsync(int tableId, Core.Enums.TableStatus newStatus)
        {
            try
            {
                // 먼저 테이블이 존재하는지 확인 (tracking 비활성화)
                var exists = await _unitOfWork.TableRepository.Query()
                    .AsNoTracking()
                    .AnyAsync(t => t.TableId == tableId);
                    
                if (!exists)
                {
                    throw new InvalidOperationException($"Table with ID {tableId} not found.");
                }
                
                // 업데이트를 위해 테이블 조회 (tracking 활성화)
                var table = await _unitOfWork.TableRepository.GetByIdAsync(tableId);
                if (table != null)
                {
                    table.TableStatus = newStatus;
                    table.UpdatedAt = DateTime.Now;
                    
                    _unitOfWork.TableRepository.Update(table);
                    await _unitOfWork.SaveChangesAsync();
                    
                    // ChangeTracker 정리
                    _unitOfWork.ClearChangeTracker();
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
            try
            {
                System.Diagnostics.Debug.WriteLine($"MoveToOrderScreenAsync called with tableId: {tableId}");
                
                var parameters = new NavigationParameters
                {
                    { "tableId", tableId }
                };
                
                _regionManager.RequestNavigate("MainRegion", new Uri("OrderManagementView", UriKind.Relative), navigationResult =>
                {
                    System.Diagnostics.Debug.WriteLine($"Navigation result: Success={navigationResult.Result}, Error={navigationResult.Error?.Message}");
                    if (!navigationResult.Result.Value && navigationResult.Error != null)
                    {
                        System.Windows.MessageBox.Show($"네비게이션 실패: {navigationResult.Error.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }, parameters);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in MoveToOrderScreenAsync: {ex}");
                System.Windows.MessageBox.Show($"주문 화면 이동 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}