using DevExpress.Xpf.Core;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Windows.Threading;

namespace RestaurantPOS.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ThemedWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }

    public class MainWindowViewModel : BindableBase
    {
        private readonly DispatcherTimer _timer;
        private string _currentTime = string.Empty;

        public string CurrentTime
        {
            get { return _currentTime; }
            set { SetProperty(ref _currentTime, value); }
        }

        // Commands
        public DelegateCommand ExitCommand { get; }
        public DelegateCommand NavigateTableCommand { get; }
        public DelegateCommand NavigateMenuCommand { get; }
        public DelegateCommand NavigateEmployeeCommand { get; }
        public DelegateCommand DailySalesCommand { get; }
        public DelegateCommand MonthlySalesCommand { get; }
        public DelegateCommand AboutCommand { get; }

        public MainWindowViewModel()
        {
            // Initialize commands
            ExitCommand = new DelegateCommand(() => System.Windows.Application.Current.Shutdown());
            NavigateTableCommand = new DelegateCommand(() => { /* Navigation will be handled by modules */ });
            NavigateMenuCommand = new DelegateCommand(() => { /* TODO: Navigate to menu module */ });
            NavigateEmployeeCommand = new DelegateCommand(() => { /* TODO: Navigate to employee module */ });
            DailySalesCommand = new DelegateCommand(() => { /* TODO: Show daily sales */ });
            MonthlySalesCommand = new DelegateCommand(() => { /* TODO: Show monthly sales */ });
            AboutCommand = new DelegateCommand(ShowAbout);

            // Setup timer for current time
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) => CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _timer.Start();
        }

        private void ShowAbout()
        {
            DXMessageBox.Show("Restaurant POS System v1.0\n\n음식점 주문 관리 시스템", "정보", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Information);
        }
    }
}