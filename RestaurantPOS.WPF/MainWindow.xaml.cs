using DevExpress.Xpf.Core;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RestaurantPOS.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ThemedWindow
    {
        private const int IconBig = 1;
        private const int IconSmall = 0;
        private const int WmSetIcon = 0x0080;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            SourceInitialized += OnSourceInitialized;
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            ApplyWindowIcons();
        }

        private void ApplyWindowIcons()
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            if (!File.Exists(iconPath))
            {
                return;
            }

            using var icon = new Icon(iconPath);
            Icon = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            var windowHandle = new WindowInteropHelper(this).Handle;
            SendMessage(windowHandle, WmSetIcon, (IntPtr)IconSmall, icon.Handle);
            SendMessage(windowHandle, WmSetIcon, (IntPtr)IconBig, icon.Handle);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
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
            NavigateEmployeeCommand = new DelegateCommand(() => { /* TODO: [Future Feature] 직원 관리 모듈 - v2.0 */ });
            DailySalesCommand = new DelegateCommand(() => { /* TODO: [Future Feature] 일별 매출 통계 - v2.0 */ });
            MonthlySalesCommand = new DelegateCommand(() => { /* TODO: [Future Feature] 월별 매출 통계 - v2.0 */ });
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
