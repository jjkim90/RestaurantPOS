using DevExpress.Xpf.LayoutControl;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using RestaurantPOS.Core.Interfaces;
using RestaurantPOS.WPF.Modules.TableModule.Services;
using RestaurantPOS.WPF.Modules.TableModule.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace RestaurantPOS.WPF.Modules.TableModule.ViewModels
{
    public class TableManagementViewModel : BindableBase, INavigationAware
    {
        private readonly ITableUIService _tableUIService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITableService _tableService;
        private readonly DispatcherTimer _timer;
        private readonly SemaphoreSlim _loadDataSemaphore = new SemaphoreSlim(1, 1);

        private string _currentTime = string.Empty;
        public string CurrentTime
        {
            get { return _currentTime; }
            set { SetProperty(ref _currentTime, value); }
        }

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set { SetProperty(ref _selectedTabIndex, value); }
        }

        // 공간 컬렉션
        public ObservableCollection<SpaceViewModel> Spaces { get; }
        
        // 현재 선택된 공간의 테이블
        private ObservableCollection<TableViewModel> _currentTables = new ObservableCollection<TableViewModel>();
        public ObservableCollection<TableViewModel> CurrentTables
        {
            get { return _currentTables; }
            set { SetProperty(ref _currentTables, value); }
        }
        
        // 포장/배달/대기 테이블
        public ObservableCollection<TableViewModel> SystemTables { get; }
        public ObservableCollection<TableViewModel> TakeoutTables { get; }
        public ObservableCollection<TableViewModel> DeliveryTables { get; }
        public ObservableCollection<TableViewModel> WaitingTables { get; }
        public ObservableCollection<WaitingViewModel> WaitingList { get; }

        // Commands
        public DelegateCommand<object> TableClickCommand { get; }
        public DelegateCommand AddSpaceCommand { get; }
        public DelegateCommand<SpaceViewModel> EditSpaceCommand { get; }
        public DelegateCommand<SpaceViewModel> DeleteSpaceCommand { get; }
        public DelegateCommand<SpaceViewModel> SelectSpaceCommand { get; }
        public DelegateCommand ShowSystemTablesCommand { get; }
        public DelegateCommand AddTableCommand { get; }
        public DelegateCommand<TableViewModel> EditTableCommand { get; }
        public DelegateCommand<TableViewModel> DeleteTableCommand { get; }
        public DelegateCommand<TableViewModel> SelectTableCommand { get; }
        
        // Selected Space
        private SpaceViewModel? _selectedSpace;
        public SpaceViewModel? SelectedSpace
        {
            get { return _selectedSpace; }
            set 
            { 
                SetProperty(ref _selectedSpace, value);
                AddTableCommand?.RaiseCanExecuteChanged();
                EditTableCommand?.RaiseCanExecuteChanged();
                DeleteTableCommand?.RaiseCanExecuteChanged();
            }
        }
        
        // Selected Table
        private TableViewModel? _selectedTable;
        public TableViewModel? SelectedTable
        {
            get { return _selectedTable; }
            set { SetProperty(ref _selectedTable, value); }
        }
        
        // System Tables Mode
        private bool _isSystemTablesMode;
        public bool IsSystemTablesMode
        {
            get { return _isSystemTablesMode; }
            set 
            { 
                SetProperty(ref _isSystemTablesMode, value);
                AddTableCommand?.RaiseCanExecuteChanged();
                EditTableCommand?.RaiseCanExecuteChanged();
                DeleteTableCommand?.RaiseCanExecuteChanged();
            }
        }

        public TableManagementViewModel(ITableUIService tableUIService, IUnitOfWork unitOfWork, ITableService tableService)
        {
            _tableUIService = tableUIService;
            _unitOfWork = unitOfWork;
            _tableService = tableService;

            // Initialize collections
            Spaces = new ObservableCollection<SpaceViewModel>();
            CurrentTables = new ObservableCollection<TableViewModel>();
            SystemTables = new ObservableCollection<TableViewModel>();
            TakeoutTables = new ObservableCollection<TableViewModel>();
            DeliveryTables = new ObservableCollection<TableViewModel>();
            WaitingTables = new ObservableCollection<TableViewModel>();
            WaitingList = new ObservableCollection<WaitingViewModel>();

            // Initialize commands
            TableClickCommand = new DelegateCommand<object>(OnTableClick);
            AddSpaceCommand = new DelegateCommand(OnAddSpace);
            EditSpaceCommand = new DelegateCommand<SpaceViewModel>(OnEditSpace);
            DeleteSpaceCommand = new DelegateCommand<SpaceViewModel>(OnDeleteSpace);
            SelectSpaceCommand = new DelegateCommand<SpaceViewModel>(OnSelectSpace);
            ShowSystemTablesCommand = new DelegateCommand(OnShowSystemTables);
            AddTableCommand = new DelegateCommand(OnAddTable, CanAddTable);
            EditTableCommand = new DelegateCommand<TableViewModel>(OnEditTable, CanEditTable);
            DeleteTableCommand = new DelegateCommand<TableViewModel>(OnDeleteTable, CanDeleteTable);
            SelectTableCommand = new DelegateCommand<TableViewModel>(OnSelectTable);

            // Setup timer for current time
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) => CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _timer.Start();

            // Load initial data
            Task.Run(async () => await LoadSpacesAndTablesAsync());
        }

        private async Task LoadSpacesAndTablesAsync()
        {
            // SemaphoreSlim을 사용하여 동시 접근 방지
            await _loadDataSemaphore.WaitAsync();
            
            try
            {
                var spaces = await _unitOfWork.SpaceRepository.GetAllAsync();
                var tables = await _unitOfWork.TableRepository.GetAllAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Clear existing
                    Spaces.Clear();
                    SystemTables.Clear();
                    TakeoutTables.Clear();
                    DeliveryTables.Clear();
                    WaitingTables.Clear();

                    foreach (var space in spaces)
                    {
                        // IsDeleted가 false인 테이블만 필터링
                        var spaceTables = tables.Where(t => t.SpaceId == space.SpaceId && !t.IsDeleted)
                            .Select(t => new TableViewModel(t))
                            .OrderBy(t => t.TableNumber);

                        if (space.IsSystem)
                        {
                            // 포장/배달/대기 테이블을 구분
                            foreach (var table in spaceTables)
                            {
                                SystemTables.Add(table);
                                
                                // TableName으로 구분 (DisplayName은 TableName과 동일)
                                if (table.DisplayName.StartsWith("포장"))
                                {
                                    TakeoutTables.Add(table);
                                }
                                else if (table.DisplayName.StartsWith("배달"))
                                {
                                    DeliveryTables.Add(table);
                                }
                                else if (table.DisplayName.StartsWith("대기"))
                                {
                                    WaitingTables.Add(table);
                                }
                                
                            }
                        }
                        else
                        {
                            // 사용자 정의 공간 (홀)
                            var spaceVm = new SpaceViewModel(space, spaceTables.ToList());
                            Spaces.Add(spaceVm);
                        }
                    }

                    // 첫 번째 공간의 테이블을 현재 테이블로 설정
                    if (Spaces.Count > 0)
                    {
                        SelectedSpace = Spaces[0];
                        CurrentTables.Clear();
                        foreach (var table in Spaces[0].Tables)
                        {
                            CurrentTables.Add(table);
                        }
                    }
                    
                });
            }
            catch (Exception ex)
            {
                // Log error
                System.Windows.MessageBox.Show($"데이터 로드 중 오류 발생: {ex.Message}", "오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                // 항상 세마포어를 해제하여 다음 작업이 진행될 수 있도록 함
                _loadDataSemaphore.Release();
            }
        }

        private async void OnTableClick(object parameter)
        {
            System.Diagnostics.Debug.WriteLine($"OnTableClick called with parameter type: {parameter?.GetType().Name ?? "null"}");
            
            if (parameter is TableViewModel tableViewModel)
            {
                System.Diagnostics.Debug.WriteLine($"Table clicked: {tableViewModel.DisplayName}, TableId: {tableViewModel.TableId}");
                await _tableUIService.ShowTableOptionsAsync(tableViewModel);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Parameter is not TableViewModel. Actual type: {parameter?.GetType().FullName ?? "null"}");
            }
        }
        
        private async void OnAddSpace()
        {
            var dialog = new SpaceEditDialog();
            dialog.ViewModel.Initialize();
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var createDto = new Core.DTOs.CreateSpaceDto
                    {
                        SpaceName = dialog.ViewModel.SpaceName,
                        IsActive = dialog.ViewModel.IsActive
                    };
                    
                    var newSpace = await _tableService.CreateSpaceAsync(createDto);
                    
                    // 전체 데이터 다시 로드
                    await LoadSpacesAndTablesAsync();
                    
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
        
        private async void OnEditSpace(SpaceViewModel space)
        {
            if (space == null) return;
            
            var dialog = new SpaceEditDialog();
            dialog.ViewModel.Initialize(space);
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var updateDto = new Core.DTOs.UpdateSpaceDto
                    {
                        SpaceName = dialog.ViewModel.SpaceName,
                        IsActive = dialog.ViewModel.IsActive
                    };
                    
                    await _tableService.UpdateSpaceAsync(space.SpaceId, updateDto);
                    
                    // 전체 데이터 다시 로드
                    await LoadSpacesAndTablesAsync();
                    
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
        
        private async void OnDeleteSpace(SpaceViewModel space)
        {
            if (space == null) return;
            
            var result = System.Windows.MessageBox.Show(
                $"'{space.SpaceName}' 홀을 삭제하시겠습니까?\n\n주의: 홀에 속한 모든 테이블도 삭제되며, 진행 중인 주문이 있는 경우 삭제할 수 없습니다.", 
                "홀 삭제 확인", 
                System.Windows.MessageBoxButton.YesNo, 
                System.Windows.MessageBoxImage.Warning);
            
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    var canDelete = await _tableService.CanDeleteSpaceAsync(space.SpaceId);
                    if (!canDelete)
                    {
                        System.Windows.MessageBox.Show("진행 중인 주문이 있어 삭제할 수 없습니다.", 
                            "삭제 불가", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        return;
                    }
                    
                    await _tableService.DeleteSpaceAsync(space.SpaceId);
                    
                    // 전체 데이터 다시 로드
                    await LoadSpacesAndTablesAsync();
                    
                    System.Windows.MessageBox.Show("홀이 삭제되었습니다.", "성공", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"홀 삭제 중 오류가 발생했습니다: {ex.Message}", 
                        "오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
        
        private async void OnSelectSpace(SpaceViewModel space)
        {
            if (space != null)
            {
                IsSystemTablesMode = false;
                SelectedSpace = space;
                
                // 데이터 새로고침 후 현재 테이블 표시
                await LoadSpacesAndTablesAsync();
                
                // 새로고침된 데이터에서 선택된 공간 찾기
                var refreshedSpace = Spaces.FirstOrDefault(s => s.SpaceId == space.SpaceId);
                if (refreshedSpace != null)
                {
                    SelectedSpace = refreshedSpace;
                    CurrentTables.Clear();
                    foreach (var table in refreshedSpace.Tables)
                    {
                        CurrentTables.Add(table);
                    }
                }
            }
        }
        
        private void OnShowSystemTables()
        {
            IsSystemTablesMode = true;
            SelectedSpace = null;
            
        }
        
        private void OnSelectTable(TableViewModel table)
        {
            SelectedTable = table;
        }

        #region Table Management Commands
        
        private bool CanAddTable()
        {
            // 선택된 홀이 있고, 시스템 공간이 아닌 경우에만 가능
            return SelectedSpace != null && !IsSystemTablesMode && !SelectedSpace.IsSystem;
        }

        private async void OnAddTable()
        {
            if (SelectedSpace == null) return;

            var dialog = new TableEditDialog();
            
            // 다음 테이블 번호 계산
            var nextTableNumber = CurrentTables.Any() 
                ? CurrentTables.Max(t => t.TableNumber) + 1 
                : 1;
            
            dialog.ViewModel.InitializeForAdd(nextTableNumber);
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var createDto = new Core.DTOs.CreateTableDto
                    {
                        SpaceId = SelectedSpace.SpaceId,
                        TableName = dialog.ViewModel.TableName,
                        TableNumber = dialog.ViewModel.TableNumber,
                        TableStatus = dialog.ViewModel.SelectedTableStatus.Value,
                        IsEditable = true
                    };
                    
                    await _tableService.CreateTableAsync(createDto);
                    
                    // 데이터 새로고침
                    await LoadSpacesAndTablesAsync();
                    
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

        private bool CanEditTable(TableViewModel table)
        {
            // 테이블이 있고, 시스템 공간이 아닌 경우에만 가능
            return table != null && !IsSystemTablesMode && SelectedSpace != null && !SelectedSpace.IsSystem;
        }

        private async void OnEditTable(TableViewModel table)
        {
            if (table == null || SelectedSpace == null) return;

            var dialog = new TableEditDialog();
            dialog.ViewModel.InitializeForEdit(table);
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var updateDto = new Core.DTOs.UpdateTableDto
                    {
                        TableName = dialog.ViewModel.TableName,
                        TableNumber = dialog.ViewModel.TableNumber,
                        TableStatus = dialog.ViewModel.SelectedTableStatus.Value
                    };
                    
                    await _tableService.UpdateTableAsync(table.TableId, updateDto);
                    
                    // 데이터 새로고침
                    await LoadSpacesAndTablesAsync();
                    
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

        private bool CanDeleteTable(TableViewModel table)
        {
            // 테이블이 있고, 시스템 공간이 아니며, 사용중이 아닌 경우에만 가능
            return table != null && !IsSystemTablesMode && SelectedSpace != null && !SelectedSpace.IsSystem 
                && table.StatusText != "사용중";
        }

        private async void OnDeleteTable(TableViewModel table)
        {
            if (table == null) return;
            
            var result = System.Windows.MessageBox.Show(
                $"'{table.DisplayName}' 테이블을 삭제하시겠습니까?", 
                "테이블 삭제 확인", 
                System.Windows.MessageBoxButton.YesNo, 
                System.Windows.MessageBoxImage.Warning);
            
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    await _tableService.DeleteTableAsync(table.TableId);
                    
                    // 데이터 새로고침
                    await LoadSpacesAndTablesAsync();
                    
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

        #region INavigationAware Implementation
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            System.Diagnostics.Debug.WriteLine("TableManagementViewModel - OnNavigatedTo");
            // 테이블 화면으로 돌아왔을 때 데이터 새로고침
            Task.Run(async () => await LoadSpacesAndTablesAsync());
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            System.Diagnostics.Debug.WriteLine("TableManagementViewModel - OnNavigatedFrom");
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true; // 항상 동일한 인스턴스 사용
        }
        #endregion
    }
    
    // Space ViewModel
    public class SpaceViewModel : BindableBase
    {
        private readonly Core.Entities.Space _space;
        
        public SpaceViewModel(Core.Entities.Space space, List<TableViewModel> tables)
        {
            _space = space;
            Tables = tables;
        }
        
        public int SpaceId => _space.SpaceId;
        public string SpaceName => _space.SpaceName;
        public bool IsSystem => _space.IsSystem;
        public List<TableViewModel> Tables { get; }
    }

    // Table ViewModel for UI binding
    public class TableViewModel : BindableBase
    {
        private readonly Core.Entities.Table _table;

        public TableViewModel(Core.Entities.Table table)
        {
            _table = table;
        }

        public int TableId => _table.TableId;
        public int TableNumber => _table.TableNumber;
        public string DisplayName => _table.TableName;
        public string TableStatus => _table.TableStatus.ToString();
        
        public string StatusText
        {
            get
            {
                switch (_table.TableStatus)
                {
                    case Core.Enums.TableStatus.Available:
                        return "빈 테이블";
                    case Core.Enums.TableStatus.Occupied:
                        return "사용중";
                    case Core.Enums.TableStatus.Reserved:
                        return "예약됨";
                    case Core.Enums.TableStatus.Cleaning:
                        return "정리중";
                    default:
                        return "알 수 없음";
                }
            }
        }

        public string BorderColor
        {
            get
            {
                switch (_table.TableStatus)
                {
                    case Core.Enums.TableStatus.Available:
                        return "#2196F3"; // Blue
                    case Core.Enums.TableStatus.Occupied:
                        return "#FF5722"; // Deep Orange
                    case Core.Enums.TableStatus.Reserved:
                        return "#FF9800"; // Orange
                    case Core.Enums.TableStatus.Cleaning:
                        return "#9E9E9E"; // Gray
                    default:
                        return "#607D8B"; // Blue Gray
                }
            }
        }
        
        public string HeaderColor
        {
            get
            {
                switch (_table.TableStatus)
                {
                    case Core.Enums.TableStatus.Available:
                        return "#2196F3"; // Blue
                    case Core.Enums.TableStatus.Occupied:
                        return "#FF5722"; // Deep Orange
                    case Core.Enums.TableStatus.Reserved:
                        return "#FF9800"; // Orange
                    case Core.Enums.TableStatus.Cleaning:
                        return "#9E9E9E"; // Gray
                    default:
                        return "#607D8B"; // Blue Gray
                }
            }
        }

        public decimal CurrentAmount => 0; // TODO: Calculate from current orders
        public string AmountVisibility => _table.TableStatus == Core.Enums.TableStatus.Occupied ? "Visible" : "Collapsed";
    }

    // Waiting list ViewModel
    public class WaitingViewModel : BindableBase
    {
        public int WaitingNumber { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int PartySize { get; set; }
        public string WaitingTime { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}