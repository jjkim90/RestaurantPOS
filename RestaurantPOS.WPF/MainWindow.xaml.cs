using DevExpress.Xpf.Core;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
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
        private readonly IRegionManager _regionManager;
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
            // Try to get IRegionManager from container
            try
            {
                _regionManager = Prism.Ioc.ContainerLocator.Container.Resolve<IRegionManager>();
            }
            catch
            {
                // Design time or container not ready
            }

            // Initialize commands
            ExitCommand = new DelegateCommand(() => System.Windows.Application.Current.Shutdown());
            NavigateTableCommand = new DelegateCommand(NavigateToTable);
            NavigateMenuCommand = new DelegateCommand(NavigateToMenu);
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

        private void NavigateToTable()
        {
            _regionManager?.RequestNavigate("MainRegion", "TableManagementView");
        }

        private void NavigateToMenu()
        {
            _regionManager?.RequestNavigate("MainRegion", "MenuManagementView");
        }

        private void ShowAbout()
        {
            DXMessageBox.Show("Restaurant POS System v1.0\n\n음식점 주문 관리 시스템", "정보", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Information);
        }
    }
}