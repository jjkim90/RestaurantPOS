using Microsoft.Web.WebView2.Core;
using RestaurantPOS.Core.DTOs.TossPayments;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Serilog;

namespace RestaurantPOS.WPF.Controls
{
    public partial class PaymentWebView : System.Windows.Controls.UserControl
    {
        public event EventHandler<PaymentResultEventArgs> PaymentCompleted;
        public event EventHandler PaymentCancelled;

        private PaymentRequestDto _paymentRequest;
        private string _clientKey;
        private bool _isInitialized = false;
        private bool _navigationCancelledIntentionally = false;

        public PaymentWebView()
        {
            InitializeComponent();
            // 초기화를 나중으로 미룸
        }

        private async Task InitializeWebView()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("InitializeWebView 시작");
                Log.Information("PaymentWebView InitializeWebView 시작");
                
                if (PaymentWebView2 == null)
                {
                    System.Diagnostics.Debug.WriteLine("PaymentWebView2가 null입니다!");
                    Log.Error("PaymentWebView2가 null입니다.");
                    throw new InvalidOperationException("PaymentWebView2가 초기화되지 않았습니다.");
                }
                
                // --- WSL 환경 문제 해결을 위한 핵심 수정 ---
                // 1. 사용자 데이터 폴더로 사용할 Windows 경로 지정
                string userDataFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "RestaurantPOS", 
                    "WebView2_UserData");
                
                System.Diagnostics.Debug.WriteLine($"WebView2 사용자 데이터 폴더: {userDataFolder}");
                Log.Information("WebView2 사용자 데이터 폴더: {UserDataFolder}", userDataFolder);
                
                // 2. 폴더가 없으면 생성
                if (!Directory.Exists(userDataFolder))
                {
                    Directory.CreateDirectory(userDataFolder);
                    System.Diagnostics.Debug.WriteLine("사용자 데이터 폴더 생성됨");
                }
                
                // 3. 해당 경로로 WebView2 환경(Environment) 생성
                System.Diagnostics.Debug.WriteLine("WebView2 환경 생성 중...");
                Log.Information("WebView2 환경 생성 중");
                var environment = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null, 
                    userDataFolder: userDataFolder);
                System.Diagnostics.Debug.WriteLine("WebView2 환경 생성 완료");
                Log.Information("WebView2 환경 생성 완료");
                
                // 4. 생성된 환경으로 EnsureCoreWebView2Async 호출
                System.Diagnostics.Debug.WriteLine("EnsureCoreWebView2Async 호출 전");
                await PaymentWebView2.EnsureCoreWebView2Async(environment);
                System.Diagnostics.Debug.WriteLine("EnsureCoreWebView2Async 호출 완료");
                Log.Information("EnsureCoreWebView2Async 호출 완료");
                
                // DevTools 활성화 (디버깅용)
                #if DEBUG
                PaymentWebView2.CoreWebView2.Settings.AreDevToolsEnabled = true;
                System.Diagnostics.Debug.WriteLine("DevTools 활성화됨 - F12 키로 개발자 도구를 열 수 있습니다.");
                #endif
                
                // file:// 프로토콜에서 외부 리소스 접근을 위한 설정
                PaymentWebView2.CoreWebView2.Settings.IsWebMessageEnabled = true;
                PaymentWebView2.CoreWebView2.Settings.IsScriptEnabled = true;
                PaymentWebView2.CoreWebView2.Settings.IsStatusBarEnabled = false;
                PaymentWebView2.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                
                // DevTools 활성화 (F12 키로 개발자 도구 사용 가능)
                PaymentWebView2.CoreWebView2.Settings.AreDevToolsEnabled = true;
                System.Diagnostics.Debug.WriteLine("DevTools 활성화 - F12 키로 개발자 도구 사용 가능");
                
                // JavaScript와 C# 간 통신 설정
                PaymentWebView2.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                System.Diagnostics.Debug.WriteLine("WebMessageReceived 이벤트 핸들러 등록");
                
                // 네비게이션 이벤트 처리
                PaymentWebView2.NavigationCompleted += OnNavigationCompleted;
                PaymentWebView2.NavigationStarting += OnNavigationStarting;
                System.Diagnostics.Debug.WriteLine("Navigation 이벤트 핸들러 등록");
                
                System.Diagnostics.Debug.WriteLine("InitializeWebView 완료");
                Log.Information("PaymentWebView InitializeWebView 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeWebView 오류: {ex.Message}");
                Log.Error(ex, "PaymentWebView InitializeWebView 오류");
                throw new Exception($"WebView2 초기화 실패: {ex.Message}", ex);
            }
        }


        public async Task InitializePayment(PaymentRequestDto paymentRequest, string clientKey)
        {
            System.Diagnostics.Debug.WriteLine("PaymentWebView.InitializePayment 시작");
            Log.Information("PaymentWebView InitializePayment 시작 - OrderId: {OrderId}, Amount: {Amount}, ClientKeyPrefix: {ClientKeyPrefix}",
                paymentRequest?.OrderId, paymentRequest?.Amount, string.IsNullOrEmpty(clientKey) ? "(empty)" : clientKey.Substring(0, Math.Min(12, clientKey.Length)));
            _paymentRequest = paymentRequest;
            _clientKey = clientKey;

            try
            {
                ShowLoading("결제창을 준비하고 있습니다...");
                System.Diagnostics.Debug.WriteLine("ShowLoading 호출 완료");
                
                // WebView2 초기화
                if (!_isInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("WebView2 초기화 시작...");
                    await InitializeWebView();
                    _isInitialized = true;
                    System.Diagnostics.Debug.WriteLine("WebView2 초기화 완료");
                }
                
                // HTML 파일 경로 설정
                var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "TossPayments", "payment.html");
                System.Diagnostics.Debug.WriteLine($"HTML 경로: {htmlPath}");
                
                if (!File.Exists(htmlPath))
                {
                    Log.Error("payment.html 파일을 찾을 수 없습니다. Path: {HtmlPath}", htmlPath);
                    throw new FileNotFoundException("payment.html 파일을 찾을 수 없습니다.", htmlPath);
                }
                
                System.Diagnostics.Debug.WriteLine("payment.html 파일 존재 확인");

                // HTML 내용을 직접 읽어서 로드 (file:// 프로토콜의 보안 제한 회피)
                var htmlContent = File.ReadAllText(htmlPath);
                System.Diagnostics.Debug.WriteLine("HTML 파일 읽기 완료");
                Log.Information("payment.html 파일 읽기 완료");
                
                // NavigateToString으로 HTML을 직접 로드
                PaymentWebView2.CoreWebView2.NavigateToString(htmlContent);
                System.Diagnostics.Debug.WriteLine("NavigateToString 호출 완료");
                Log.Information("NavigateToString 호출 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializePayment 오류: {ex.Message}");
                Log.Error(ex, "PaymentWebView InitializePayment 오류");
                ShowError($"결제 초기화 실패: {ex.Message}");
            }
        }

        private async void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                try
                {
                    Log.Information("PaymentWebView NavigationCompleted 성공");
                    // 페이지가 로드되면 결제 정보 전달
                    var paymentData = new
                    {
                        clientKey = _clientKey,
                        paymentRequest = _paymentRequest
                    };

                    var json = JsonConvert.SerializeObject(paymentData);
                    System.Diagnostics.Debug.WriteLine($"전달할 데이터: {json}");
                    
                    var script = $"initializePayment({json})";
                    System.Diagnostics.Debug.WriteLine($"실행할 스크립트: {script.Substring(0, Math.Min(200, script.Length))}...");
                    
                    await PaymentWebView2.CoreWebView2.ExecuteScriptAsync(script);
                    
                    ShowWebView();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "결제 정보 전달 실패");
                    ShowError($"결제 정보 전달 실패: {ex.Message}");
                }
            }
            else
            {
                Log.Error("PaymentWebView NavigationCompleted 실패");
                if (!_navigationCancelledIntentionally)
                {
                    ShowError("페이지 로드 실패");
                }
            }
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation starting to: {e.Uri}");
            Log.Information("PaymentWebView NavigationStarting - Uri: {Uri}", e.Uri);
            
            // 결제 완료/실패 URL 체크
            try
            {
                var uri = new Uri(e.Uri);
                
                // about:blank나 초기 로드는 무시
                if (uri.Scheme == "about")
                {
                    return;
                }
                
                if (uri.AbsolutePath.Contains("/success"))
                {
                    // URL 파라미터에서 결제 정보 추출
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    var paymentKey = query["paymentKey"];
                    var orderId = query["orderId"];
                    var amount = query["amount"];

                    if (!string.IsNullOrEmpty(paymentKey))
                    {
                        _navigationCancelledIntentionally = true;
                        e.Cancel = true;
                        PaymentCompleted?.Invoke(this, new PaymentResultEventArgs
                        {
                            PaymentKey = paymentKey,
                            OrderId = orderId,
                            Amount = decimal.Parse(amount ?? "0"),
                            IsSuccess = true
                        });
                    }
                }
                else if (uri.AbsolutePath.Contains("/fail"))
                {
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    var errorCode = query["code"];
                    var errorMessage = query["message"];

                    _navigationCancelledIntentionally = true;
                    e.Cancel = true;
                    PaymentCompleted?.Invoke(this, new PaymentResultEventArgs
                    {
                        IsSuccess = false,
                        ErrorCode = errorCode,
                        ErrorMessage = errorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation URL parsing error: {ex.Message}");
                Log.Error(ex, "Navigation URL parsing error");
            }
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = e.TryGetWebMessageAsString();
                Log.Information("PaymentWebView WebMessageReceived: {Message}", message);
                dynamic data = JsonConvert.DeserializeObject(message);
                
                string action = data.action;
                
                switch (action)
                {
                    case "paymentSuccess":
                        PaymentCompleted?.Invoke(this, new PaymentResultEventArgs
                        {
                            PaymentKey = data.paymentKey,
                            OrderId = data.orderId,
                            Amount = data.amount,
                            IsSuccess = true
                        });
                        break;
                        
                    case "paymentFailed":
                        PaymentCompleted?.Invoke(this, new PaymentResultEventArgs
                        {
                            IsSuccess = false,
                            ErrorCode = data.code,
                            ErrorMessage = data.message
                        });
                        break;
                        
                    case "paymentCancelled":
                        PaymentCancelled?.Invoke(this, EventArgs.Empty);
                        break;
                        
                    case "error":
                        Log.Error("PaymentWebView error message from web content: {Message}", data.message.ToString());
                        ShowError(data.message.ToString());
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "메시지 처리 실패");
                ShowError($"메시지 처리 실패: {ex.Message}");
            }
        }

        private void ShowLoading(string message)
        {
            LoadingOverlay.Message = message;
            LoadingOverlay.Visibility = Visibility.Visible;
            PaymentWebView2.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowWebView()
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            PaymentWebView2.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            Log.Error("PaymentWebView ShowError: {Message}", message);
            ErrorMessage.Text = message;
            LoadingOverlay.Visibility = Visibility.Collapsed;
            PaymentWebView2.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
        }

        private async void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_paymentRequest != null && !string.IsNullOrEmpty(_clientKey))
            {
                await InitializePayment(_paymentRequest, _clientKey);
            }
        }
    }

    public class PaymentResultEventArgs : EventArgs
    {
        public string PaymentKey { get; set; }
        public string OrderId { get; set; }
        public decimal Amount { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
