using DevExpress.Xpf.LayoutControl;
using Prism.Commands;
using Prism.Mvvm;
using RestaurantPOS.Core.Interfaces;
using RestaurantPOS.WPF.Modules.TableModule.Services;
using RestaurantPOS.WPF.Modules.TableModule.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace RestaurantPOS.WPF.Modules.TableModule.ViewModels
{
    public class TableManagementViewModel : BindableBase
    {
        private readonly ITableUIService _tableUIService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITableService _tableService;
        private readonly DispatcherTimer _timer;

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
        public DelegateCommand<TileClickEventArgs> TableClickCommand { get; }
        public DelegateCommand AddSpaceCommand { get; }
        public DelegateCommand<SpaceViewModel> EditSpaceCommand { get; }
        public DelegateCommand<SpaceViewModel> DeleteSpaceCommand { get; }
        public DelegateCommand<SpaceViewModel> SelectSpaceCommand { get; }
        public DelegateCommand ShowSystemTablesCommand { get; }
        
        // Selected Space
        private SpaceViewModel? _selectedSpace;
        public SpaceViewModel? SelectedSpace
        {
            get { return _selectedSpace; }
            set { SetProperty(ref _selectedSpace, value); }
        }
        
        // System Tables Mode
        private bool _isSystemTablesMode;
        public bool IsSystemTablesMode
        {
            get { return _isSystemTablesMode; }
            set { SetProperty(ref _isSystemTablesMode, value); }
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
            TableClickCommand = new DelegateCommand<TileClickEventArgs>(OnTableClick);
            AddSpaceCommand = new DelegateCommand(OnAddSpace);
            EditSpaceCommand = new DelegateCommand<SpaceViewModel>(OnEditSpace);
            DeleteSpaceCommand = new DelegateCommand<SpaceViewModel>(OnDeleteSpace);
            SelectSpaceCommand = new DelegateCommand<SpaceViewModel>(OnSelectSpace);
            ShowSystemTablesCommand = new DelegateCommand(OnShowSystemTables);

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
                        var spaceTables = tables.Where(t => t.SpaceId == space.SpaceId)
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
        }

        private async void OnTableClick(TileClickEventArgs e)
        {
            if (e?.Tile?.Tag is TableViewModel tableViewModel)
            {
                // TODO: Navigate to order screen or show table options
                await _tableUIService.ShowTableOptionsAsync(tableViewModel);
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
        
        private void OnSelectSpace(SpaceViewModel space)
        {
            if (space != null)
            {
                IsSystemTablesMode = false;
                SelectedSpace = space;
                CurrentTables.Clear();
                foreach (var table in space.Tables)
                {
                    CurrentTables.Add(table);
                }
            }
        }
        
        private void OnShowSystemTables()
        {
            IsSystemTablesMode = true;
            SelectedSpace = null;
            
        }

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