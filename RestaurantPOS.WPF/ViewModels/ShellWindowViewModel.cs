using Prism.Mvvm;
using System;
using System.Windows.Threading;

namespace RestaurantPOS.WPF.ViewModels
{
    public class ShellWindowViewModel : BindableBase
    {
        private string _currentDateTime = string.Empty;
        private string _statusMessage = string.Empty;
        private DispatcherTimer _timer;

        public string CurrentDateTime
        {
            get { return _currentDateTime; }
            set { SetProperty(ref _currentDateTime, value); }
        }

        public string StatusMessage
        {
            get { return _statusMessage; }
            set { SetProperty(ref _statusMessage, value); }
        }

        public ShellWindowViewModel()
        {
            StatusMessage = "시스템 준비 완료";
            
            // 시간 업데이트 타이머 설정
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateDateTime();
            _timer.Start();
            
            UpdateDateTime();
        }

        private void UpdateDateTime()
        {
            CurrentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}