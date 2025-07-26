using Prism.Commands;
using Prism.Mvvm;
using System;

namespace RestaurantPOS.WPF.Modules.TableModule.ViewModels
{
    public class SpaceEditDialogViewModel : BindableBase
    {
        private string _title = "홀 추가";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private string _spaceName = string.Empty;
        public string SpaceName
        {
            get { return _spaceName; }
            set 
            { 
                SetProperty(ref _spaceName, value);
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get { return _isActive; }
            set { SetProperty(ref _isActive, value); }
        }

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get { return _dialogResult; }
            set { SetProperty(ref _dialogResult, value); }
        }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public SpaceEditDialogViewModel()
        {
            SaveCommand = new DelegateCommand(OnSave, CanSave);
            CancelCommand = new DelegateCommand(OnCancel);
        }

        public void Initialize(SpaceViewModel space = null)
        {
            if (space != null)
            {
                Title = "홀 수정";
                SpaceName = space.SpaceName;
                // IsActive는 SpaceViewModel에 없으므로 기본값 사용
            }
            else
            {
                Title = "홀 추가";
                SpaceName = string.Empty;
                IsActive = true;
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(SpaceName);
        }

        private void OnSave()
        {
            DialogResult = true;
        }

        private void OnCancel()
        {
            DialogResult = false;
        }
    }
}