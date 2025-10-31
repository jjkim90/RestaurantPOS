using Prism.Commands;
using Prism.Mvvm;
using RestaurantPOS.Core.Enums;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace RestaurantPOS.WPF.Modules.TableModule.ViewModels
{
    public class TableEditDialogViewModel : BindableBase
    {
        public class TableStatusItem
        {
            public string DisplayName { get; set; }
            public TableStatus Value { get; set; }
        }

        private string _title = "테이블 추가";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private string _tableName = string.Empty;
        public string TableName
        {
            get { return _tableName; }
            set 
            { 
                SetProperty(ref _tableName, value);
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        private int _tableNumber = 1;
        public int TableNumber
        {
            get { return _tableNumber; }
            set { SetProperty(ref _tableNumber, value); }
        }

        private bool _isStatusEditable = true;
        public bool IsStatusEditable
        {
            get { return _isStatusEditable; }
            set { SetProperty(ref _isStatusEditable, value); }
        }

        private bool _isTableNumberEditable = true;
        public bool IsTableNumberEditable
        {
            get { return _isTableNumberEditable; }
            set { SetProperty(ref _isTableNumberEditable, value); }
        }

        private TableStatusItem _selectedTableStatus;
        public TableStatusItem SelectedTableStatus
        {
            get { return _selectedTableStatus; }
            set { SetProperty(ref _selectedTableStatus, value); }
        }

        public ObservableCollection<TableStatusItem> TableStatuses { get; }

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get { return _dialogResult; }
            set { SetProperty(ref _dialogResult, value); }
        }

        // 원본 데이터 (수정 모드일 때)
        private int _originalTableId;
        private string _originalTableName;

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public TableEditDialogViewModel()
        {
            SaveCommand = new DelegateCommand(OnSave, CanSave);
            CancelCommand = new DelegateCommand(OnCancel);

            // 테이블 상태 목록 초기화
            TableStatuses = new ObservableCollection<TableStatusItem>
            {
                new TableStatusItem { DisplayName = "사용 가능", Value = TableStatus.Available },
                new TableStatusItem { DisplayName = "사용 중", Value = TableStatus.Occupied },
                new TableStatusItem { DisplayName = "예약됨", Value = TableStatus.Reserved },
                new TableStatusItem { DisplayName = "정리 중", Value = TableStatus.Cleaning }
            };

            // 기본값 설정
            SelectedTableStatus = TableStatuses.First(s => s.Value == TableStatus.Available);
        }

        public void InitializeForAdd(int nextTableNumber)
        {
            Title = "테이블 추가";
            TableName = string.Empty;
            TableNumber = nextTableNumber;
            SelectedTableStatus = TableStatuses.First(s => s.Value == TableStatus.Available);
            IsStatusEditable = false;  // 추가 시에는 상태 선택 불가 (항상 사용 가능)
            IsTableNumberEditable = true;  // 테이블 번호는 수정 가능
            _originalTableId = 0;
            _originalTableName = string.Empty;
        }

        public void InitializeForEdit(TableViewModel table)
        {
            if (table == null) return;

            Title = "테이블 수정";
            TableName = table.DisplayName;
            TableNumber = table.TableNumber;
            
            // TableStatus enum 값으로 매칭
            var status = (TableStatus)Enum.Parse(typeof(TableStatus), table.TableStatus);
            SelectedTableStatus = TableStatuses.FirstOrDefault(s => s.Value == status) 
                ?? TableStatuses.First(s => s.Value == TableStatus.Available);
            
            // 사용 중인 테이블은 상태 변경 불가
            IsStatusEditable = status != TableStatus.Occupied;
            IsTableNumberEditable = true;  // 수정 시에도 테이블 번호 변경 가능
            
            _originalTableId = table.TableId;
            _originalTableName = table.DisplayName;
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(TableName);
        }

        private void OnSave()
        {
            DialogResult = true;
        }

        private void OnCancel()
        {
            DialogResult = false;
        }

        public int GetOriginalTableId()
        {
            return _originalTableId;
        }

        public bool IsNameChanged()
        {
            return _originalTableName != TableName;
        }
    }
}