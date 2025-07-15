using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Data.Context;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RestaurantPOS.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly RestaurantContext _context;

        public MainWindow(RestaurantContext context)
        {
            InitializeComponent();
            _context = context;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var spaceCount = await _context.Spaces.CountAsync();
                var tableCount = await _context.Tables.CountAsync();
                var categoryCount = await _context.Categories.CountAsync();
                var menuCount = await _context.MenuItems.CountAsync();

                DbInfoText.Text = $"공간: {spaceCount}개\n" +
                                 $"테이블: {tableCount}개\n" +
                                 $"카테고리: {categoryCount}개\n" +
                                 $"메뉴: {menuCount}개";
            }
            catch (Exception ex)
            {
                DbInfoText.Text = $"데이터 조회 오류: {ex.Message}";
            }
        }
    }
}