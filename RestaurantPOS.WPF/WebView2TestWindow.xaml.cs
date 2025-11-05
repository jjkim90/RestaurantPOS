using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace RestaurantPOS.WPF
{
    public partial class WebView2TestWindow : Window
    {
        public WebView2TestWindow()
        {
            InitializeComponent();
            AddStatus("WebView2 Test Window 초기화됨");
        }

        private void AddStatus(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            StatusTextBox.AppendText($"[{timestamp}] {message}\n");
            StatusTextBox.ScrollToEnd();
            Debug.WriteLine($"[WebView2Test] {message}");
        }

        private async void TestInitButton_Click(object sender, RoutedEventArgs e)
        {
            TestInitButton.IsEnabled = false;
            AddStatus("초기화 버튼 클릭됨");

            try
            {
                AddStatus("WebView2 초기화 시작...");
                
                // 방법 1: 기본 초기화 (null 전달)
                AddStatus("방법 1: EnsureCoreWebView2Async(null) 호출");
                await TestWebView.EnsureCoreWebView2Async(null);
                
                AddStatus("WebView2 초기화 성공!");
                LoadGoogleButton.IsEnabled = true;
                System.Windows.MessageBox.Show("WebView2 초기화 성공!", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AddStatus($"초기화 실패: {ex.GetType().Name}: {ex.Message}");
                
                // 방법 2: 사용자 데이터 폴더 지정
                try
                {
                    AddStatus("\n방법 2 시도: 사용자 데이터 폴더 지정");
                    
                    string userDataFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "RestaurantPOS_Test",
                        "WebView2_UserData");
                    
                    AddStatus($"사용자 데이터 폴더: {userDataFolder}");
                    
                    if (!Directory.Exists(userDataFolder))
                    {
                        Directory.CreateDirectory(userDataFolder);
                        AddStatus("폴더 생성됨");
                    }
                    
                    var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                    AddStatus("환경 생성 완료");
                    
                    await TestWebView.EnsureCoreWebView2Async(environment);
                    AddStatus("WebView2 초기화 성공! (방법 2)");
                    
                    LoadGoogleButton.IsEnabled = true;
                    System.Windows.MessageBox.Show("WebView2 초기화 성공! (방법 2)", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex2)
                {
                    AddStatus($"방법 2도 실패: {ex2.GetType().Name}: {ex2.Message}");
                    System.Windows.MessageBox.Show($"WebView2 초기화 실패:\n\n원인: {ex2.Message}", "오류", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                TestInitButton.IsEnabled = true;
            }
        }

        private void LoadGoogleButton_Click(object sender, RoutedEventArgs e)
        {
            AddStatus("Google 로드 시도...");
            TestWebView.Source = new Uri("https://www.google.com");
            AddStatus("Navigation 시작됨");
        }
    }
}