using Microsoft.EntityFrameworkCore;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Entities;
using RestaurantPOS.Core.Enums;
using RestaurantPOS.Core.Interfaces;
using RestaurantPOS.WPF.Modules.TableModule.Services;
using RestaurantPOS.WPF.Modules.TableModule.ViewModels;
using RestaurantPOS.WPF.Modules.TableModule.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace RestaurantPOS.WPF.Modules.SettingsModule.ViewModels
{
    public class TableSettingsViewModel : BindableBase, INavigationAware
    {
        private readonly ITableService _tableService;
        private readonly ITableUIService _tableUIService;
        private readonly IUnitOfWork _unitOfWork;

        public TableSettingsViewModel(ITableService tableService, ITableUIService tableUIService, IUnitOfWork unitOfWork)
        {
            _tableService = tableService;
            _tableUIService = tableUIService;
            _unitOfWork = unitOfWork;

            Spaces = new ObservableCollection<SpaceViewModel>();
            SpacesForFilter = new ObservableCollection<SpaceViewModel>();
            FilteredTables = new ObservableCollection<TableDetailViewModel>();

            InitializeCommands();
        }

        #region Properties
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set 
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    // 탭이 변경될 때마다 데이터 새로고침 (UI 스레드에서 실행)
                    _ = LoadDataAsync();
                }
            }
        }

        public ObservableCollection<SpaceViewModel> Spaces { get; }
        public ObservableCollection<SpaceViewModel> SpacesForFilter { get; }
        public ObservableCollection<TableDetailViewModel> FilteredTables { get; }
        
        private ObservableCollection<TableDetailViewModel> _allTables = new ObservableCollection<TableDetailViewModel>();

        private SpaceViewModel? _selectedSpace;
        public SpaceViewModel? SelectedSpace
        {
            get => _selectedSpace;
            set
            {
                SetProperty(ref _selectedSpace, value);
                EditSpaceCommand?.RaiseCanExecuteChanged();
                DeleteSpaceCommand?.RaiseCanExecuteChanged();
            }
        }

        private SpaceViewModel? _selectedSpaceForFilter;
        public SpaceViewModel? SelectedSpaceForFilter
        {
            get => _selectedSpaceForFilter;
            set
            {
                SetProperty(ref _selectedSpaceForFilter, value);
                FilterTables();
            }
        }

        private TableDetailViewModel? _selectedTable;
        public TableDetailViewModel? SelectedTable
        {
            get => _selectedTable;
            set
            {
                SetProperty(ref _selectedTable, value);
                EditTableCommand?.RaiseCanExecuteChanged();
                DeleteTableCommand?.RaiseCanExecuteChanged();
            }
        }
        #endregion

        #region Commands
        public DelegateCommand AddSpaceCommand { get; private set; }
        public DelegateCommand<SpaceViewModel> EditSpaceCommand { get; private set; }
        public DelegateCommand<SpaceViewModel> DeleteSpaceCommand { get; private set; }
        public DelegateCommand AddTableCommand { get; private set; }
        public DelegateCommand<TableDetailViewModel> EditTableCommand { get; private set; }
        public DelegateCommand<TableDetailViewModel> DeleteTableCommand { get; private set; }
        #endregion

        #region Methods
        private void InitializeCommands()
        {
            AddSpaceCommand = new DelegateCommand(OnAddSpace);
            EditSpaceCommand = new DelegateCommand<SpaceViewModel>(OnEditSpace, CanEditSpace);
            DeleteSpaceCommand = new DelegateCommand<SpaceViewModel>(OnDeleteSpace, CanDeleteSpace);
            AddTableCommand = new DelegateCommand(OnAddTable, CanAddTable);
            EditTableCommand = new DelegateCommand<TableDetailViewModel>(OnEditTable, CanEditTable);
            DeleteTableCommand = new DelegateCommand<TableDetailViewModel>(OnDeleteTable, CanDeleteTable);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                
                // ChangeTracker 정리
                _unitOfWork.ClearChangeTracker();

                // Load spaces with table counts
                var allSpaces = await _unitOfWork.SpaceRepository.Query()
                    .AsNoTracking()
                    .Include(s => s.Tables.Where(t => !t.IsDeleted))
                    .Where(s => !s.IsSystem)
                    .OrderBy(s => s.SpaceId)
                    .ToListAsync();

                Spaces.Clear();
                SpacesForFilter.Clear();
                SpacesForFilter.Add(new SpaceViewModel(new Space { SpaceId = 0, SpaceName = "전체" }, new System.Collections.Generic.List<TableViewModel>()));

                foreach (var space in allSpaces)
                {
                    // 테이블 수를 계산하여 SpaceViewModel 생성
                    var tableViewModels = space.Tables
                        .Select(t => new TableViewModel(t))
                        .ToList();
                    
                    var spaceVm = new SpaceViewModel(space, tableViewModels);
                    Spaces.Add(spaceVm);
                    SpacesForFilter.Add(spaceVm);
                }

                // Load all tables
                await LoadTablesAsync();
                
                // Select "전체" by default
                SelectedSpaceForFilter = SpacesForFilter.FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"데이터 로드 중 오류 발생: {ex.Message}", "오류", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadTablesAsync()
        {
            // ChangeTracker 정리
            _unitOfWork.ClearChangeTracker();
            
            // Use Query() to include Space navigation property
            var tables = await _unitOfWork.TableRepository.Query()
                .AsNoTracking()
                .Include(t => t.Space)
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.SpaceId)
                .ThenBy(t => t.TableNumber)
                .ToListAsync();

            _allTables.Clear();
            foreach (var table in tables)
            {
                _allTables.Add(new TableDetailViewModel(table));
            }
            
            // Apply current filter
            FilterTables();
        }

        private void FilterTables()
        {
            FilteredTables.Clear();
            
            if (SelectedSpaceForFilter == null || SelectedSpaceForFilter.SpaceId == 0)
            {
                // Show all tables
                foreach (var table in _allTables)
                {
                    FilteredTables.Add(table);
                }
            }
            else
            {
                // Show only tables from selected space
                foreach (var table in _allTables.Where(t => t.SpaceId == SelectedSpaceForFilter.SpaceId))
                {
                    FilteredTables.Add(table);
                }
            }
            
            // Update command states after filtering
            AddTableCommand?.RaiseCanExecuteChanged();
        }

        #region Space Commands
        private async void OnAddSpace()
        {
            var dialog = new SpaceEditDialog();
            dialog.ViewModel.Initialize();

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var createDto = new CreateSpaceDto
                    {
                        SpaceName = dialog.ViewModel.SpaceName,
                        IsActive = dialog.ViewModel.IsActive
                    };

                    await _tableService.CreateSpaceAsync(createDto);
                    
                    // ChangeTracker 정리
                    _unitOfWork.ClearChangeTracker();
                    
                    await LoadDataAsync();

                    System.Windows.MessageBox.Show("홀이 추가되었습니다.", "성공",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"홀 추가 중 오류가 발생했습니다: {ex.Message}",
                        "오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private bool CanEditSpace(SpaceViewModel space)
        {
            return space != null && !space.IsSystem;
        }

        private async void OnEditSpace(SpaceViewModel space)
        {
            if (space == null) return;

            var dialog = new SpaceEditDialog();
            dialog.ViewModel.Initialize(space);

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var updateDto = new UpdateSpaceDto
                    {
                        SpaceName = dialog.ViewModel.SpaceName,
                        IsActive = dialog.ViewModel.IsActive
                    };

                    await _tableService.UpdateSpaceAsync(space.SpaceId, updateDto);
                    
                    // ChangeTracker 정리
                    _unitOfWork.ClearChangeTracker();
                    
                    await LoadDataAsync();

                    System.Windows.MessageBox.Show("홀이 수정되었습니다.", "성공",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"홀 수정 중 오류가 발생했습니다: {ex.Message}",
                        "오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private bool CanDeleteSpace(SpaceViewModel space)
        {
            return space != null && !space.IsSystem;
        }

        private async void OnDeleteSpace(SpaceViewModel space)
        {
            if (space == null) return;

            var result = System.Windows.MessageBox.Show(
                $"'{space.SpaceName}' 홀을 삭제하시겠습니까?\n\n주의: 이 홀에 속한 모든 테이블도 함께 삭제됩니다.",
                "홀 삭제 확인",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    // 직접 삭제 진행 (테이블도 함께 soft delete 됨)
                    await _tableService.DeleteSpaceAsync(space.SpaceId);
                    
                    // ChangeTracker 정리
                    _unitOfWork.ClearChangeTracker();
                    
                    await LoadDataAsync();

                    System.Windows.MessageBox.Show("홀이 삭제되었습니다.", "성공",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    var innerMessage = ex.InnerException != null ? $"\n\n상세 오류: {ex.InnerException.Message}" : "";
                    System.Windows.MessageBox.Show($"홀 삭제 중 오류가 발생했습니다: {ex.Message}{innerMessage}",
                        "오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
        #endregion

        #region Table Commands
        private bool CanAddTable()
        {
            return SelectedSpaceForFilter != null && SelectedSpaceForFilter.SpaceId != 0;
        }

        private async void OnAddTable()
        {
            if (SelectedSpaceForFilter == null || SelectedSpaceForFilter.SpaceId == 0)
            {
                System.Windows.MessageBox.Show("테이블을 추가할 홀을 먼저 선택해주세요.",
                    "알림", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var dialog = new TableEditDialog();

            // Calculate next table number
            var spaceTables = FilteredTables.Where(t => t.SpaceId == SelectedSpaceForFilter.SpaceId);
            var nextTableNumber = spaceTables.Any()
                ? spaceTables.Max(t => t.TableNumber) + 1
                : 1;

            dialog.ViewModel.InitializeForAdd(nextTableNumber);

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var createDto = new CreateTableDto
                    {
                        SpaceId = SelectedSpaceForFilter.SpaceId,
                        TableName = dialog.ViewModel.TableName,
                        TableNumber = dialog.ViewModel.TableNumber,
                        TableStatus = TableStatus.Available,  // 항상 사용 가능 상태로 생성
                        IsEditable = true
                    };

                    await _tableService.CreateTableAsync(createDto);
                    
                    // ChangeTracker 정리
                    _unitOfWork.ClearChangeTracker();
                    
                    await LoadTablesAsync();
                    FilterTables();

                    System.Windows.MessageBox.Show("테이블이 추가되었습니다.", "성공",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"테이블 추가 중 오류가 발생했습니다: {ex.Message}",
                        "오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private bool CanEditTable(TableDetailViewModel table)
        {
            return table != null;
        }

        private async void OnEditTable(TableDetailViewModel table)
        {
            if (table == null) return;

            var dialog = new TableEditDialog();
            
            // Create TableViewModel for dialog
            var tableViewModel = new TableViewModel(new TableDto 
            { 
                TableId = table.TableId,
                TableName = table.TableName,
                TableNumber = table.TableNumber,
                TableStatus = table.Status
            });
            
            dialog.ViewModel.InitializeForEdit(tableViewModel);

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var updateDto = new UpdateTableDto
                    {
                        TableName = dialog.ViewModel.TableName,
                        TableNumber = dialog.ViewModel.TableNumber,
                        TableStatus = dialog.ViewModel.SelectedTableStatus.Value
                    };

                    await _tableService.UpdateTableAsync(table.TableId, updateDto);
                    
                    // ChangeTracker 정리
                    _unitOfWork.ClearChangeTracker();
                    
                    await LoadTablesAsync();
                    FilterTables();

                    System.Windows.MessageBox.Show("테이블이 수정되었습니다.", "성공",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"테이블 수정 중 오류가 발생했습니다: {ex.Message}",
                        "오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private bool CanDeleteTable(TableDetailViewModel table)
        {
            return table != null && table.Status != TableStatus.Occupied;
        }

        private async void OnDeleteTable(TableDetailViewModel table)
        {
            if (table == null) return;

            var result = System.Windows.MessageBox.Show(
                $"'{table.TableName}' 테이블을 삭제하시겠습니까?",
                "테이블 삭제 확인",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    await _tableService.DeleteTableAsync(table.TableId);
                    
                    // ChangeTracker 정리
                    _unitOfWork.ClearChangeTracker();
                    
                    await LoadTablesAsync();
                    FilterTables();

                    System.Windows.MessageBox.Show("테이블이 삭제되었습니다.", "성공",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"테이블 삭제 중 오류가 발생했습니다: {ex.Message}",
                        "오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
        #endregion
        #endregion

        #region INavigationAware
        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            await LoadDataAsync();
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

    // TableDetailViewModel for grid display
    public class TableDetailViewModel : BindableBase
    {
        private readonly Table _table;

        public TableDetailViewModel(Table table)
        {
            _table = table;
        }

        public int TableId => _table.TableId;
        public string TableName => _table.TableName;
        public int TableNumber => _table.TableNumber;
        public int SpaceId => _table.SpaceId;
        public string SpaceName => _table.Space?.SpaceName ?? string.Empty;
        public int Capacity => 4; // Default capacity
        public TableStatus Status => _table.TableStatus;
        public string StatusText => GetStatusText(Status);
        public System.Windows.Media.Brush StatusColor => GetStatusColor(Status);
        public DateTime CreatedAt => _table.CreatedAt;

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        private string GetStatusText(TableStatus status)
        {
            return status switch
            {
                TableStatus.Available => "사용 가능",
                TableStatus.Occupied => "사용 중",
                TableStatus.PaymentPending => "결제 대기",
                TableStatus.Reserved => "예약됨",
                TableStatus.Cleaning => "정리 중",
                _ => "알 수 없음"
            };
        }

        private System.Windows.Media.Brush GetStatusColor(TableStatus status)
        {
            return status switch
            {
                TableStatus.Available => System.Windows.Media.Brushes.Green,
                TableStatus.Occupied => System.Windows.Media.Brushes.Red,
                TableStatus.PaymentPending => System.Windows.Media.Brushes.Blue,
                TableStatus.Reserved => System.Windows.Media.Brushes.Orange,
                TableStatus.Cleaning => System.Windows.Media.Brushes.Gray,
                _ => System.Windows.Media.Brushes.Black
            };
        }
    }
}