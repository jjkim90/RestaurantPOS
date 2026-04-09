using Prism.Commands;
using Prism.Mvvm;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.DTOs.TossPayments;
using RestaurantPOS.Core.Interfaces;
using RestaurantPOS.WPF.Controls;
using System;
using System.Windows;
using System.Windows.Input;

namespace RestaurantPOS.WPF.Modules.OrderModule.ViewModels
{
    public class PaymentViewModel : BindableBase
    {
        private OrderDTO _order;
        private string _selectedPaymentMethod;
        private readonly ITossPaymentsService? _tossPaymentsService;
        private Window _paymentWindow;
        private bool _isRetryMode = false;
        private int _retryTransactionId = 0;
        
        public event EventHandler<PaymentCompletedEventArgs> PaymentCompleted;
        public event EventHandler PaymentCancelled;

        public PaymentViewModel(ITossPaymentsService? tossPaymentsService = null)
        {
            System.Diagnostics.Debug.WriteLine($"PaymentViewModel 생성자 - TossPaymentsService null 여부: {tossPaymentsService == null}");
            _tossPaymentsService = tossPaymentsService;
            CashPaymentCommand = new DelegateCommand(() => ProcessPayment("Cash"));
            CardPaymentCommand = new DelegateCommand(() => 
            {
                System.Diagnostics.Debug.WriteLine("CardPaymentCommand 실행됨");
                ProcessCardPayment();
            });
            CancelCommand = new DelegateCommand(OnCancel);
        }

        public OrderDTO Order
        {
            get => _order;
            set
            {
                SetProperty(ref _order, value);
                RaisePropertyChanged(nameof(OrderNumber));
                RaisePropertyChanged(nameof(TableName));
                RaisePropertyChanged(nameof(TotalAmount));
                RaisePropertyChanged(nameof(TotalAmountDisplay));
            }
        }

        public string OrderNumber => _order?.OrderNumber ?? "";
        public string TableName => _order?.TableName ?? "";
        public decimal TotalAmount => _order?.TotalAmount ?? 0;
        public string TotalAmountDisplay => $"{TotalAmount:N0}원";

        public ICommand CashPaymentCommand { get; }
        public ICommand CardPaymentCommand { get; }
        public ICommand CancelCommand { get; }

        private void ProcessPayment(string paymentMethod)
        {
            _selectedPaymentMethod = paymentMethod;
            PaymentCompleted?.Invoke(this, new PaymentCompletedEventArgs
            {
                PaymentMethod = paymentMethod,
                IsSuccess = true
            });
        }

        private async void ProcessCardPayment()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ProcessCardPayment 시작");
                
                if (_tossPaymentsService == null)
                {
                    System.Diagnostics.Debug.WriteLine("TossPaymentsService is null!");
                    System.Windows.MessageBox.Show("토스페이먼츠 서비스가 초기화되지 않았습니다.\n현재 카드 결제를 사용할 수 없습니다.", 
                        "서비스 오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("TossPaymentsService 확인 완료");
                
                if (_order == null)
                {
                    System.Diagnostics.Debug.WriteLine("Order is null!");
                    System.Windows.MessageBox.Show("주문 정보가 없습니다.", 
                        "오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"Order 정보: OrderNumber={_order.OrderNumber}, TotalAmount={_order.TotalAmount}");
                
                // 토스페이먼츠 결제창 표시
                var paymentWebViewWindow = new Window
                {
                    Title = "카드 결제",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = System.Windows.Application.Current.MainWindow
                };
                
                System.Diagnostics.Debug.WriteLine("결제창 Window 생성 완료");

                var paymentWebView = new PaymentWebView();
                paymentWebViewWindow.Content = paymentWebView;

                // 결제 완료 이벤트 처리
                paymentWebView.PaymentCompleted += async (s, e) =>
                {
                    if (e.IsSuccess)
                    {
                        try
                        {
                            // 결제 승인 API 호출
                            var paymentResponse = await _tossPaymentsService.ConfirmPaymentAsync(
                                e.PaymentKey, 
                                e.OrderId, 
                                e.Amount);

                            if (paymentResponse.Status == "DONE")
                            {
                                _selectedPaymentMethod = "Card";
                                PaymentCompleted?.Invoke(this, new PaymentCompletedEventArgs
                                {
                                    PaymentMethod = "Card",
                                    IsSuccess = true,
                                    PaymentKey = e.PaymentKey,
                                    TransactionId = paymentResponse.LastTransactionKey
                                });
                                
                                paymentWebViewWindow.Close();
                            }
                            else
                            {
                                System.Windows.MessageBox.Show($"결제 승인 실패: {paymentResponse.Status}", "결제 실패", 
                                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"결제 승인 중 오류 발생: {ex.Message}", "오류", 
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show($"결제 실패: {e.ErrorMessage}", "결제 실패", 
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        paymentWebViewWindow.Close();
                    }
                };

                // 결제 취소 이벤트 처리
                paymentWebView.PaymentCancelled += (s, e) =>
                {
                    paymentWebViewWindow.Close();
                };

                // 결제 요청 데이터 생성
                var paymentRequest = new PaymentRequestDto
                {
                    Method = "카드",
                    Amount = (int)Math.Round(_order.TotalAmount),
                    OrderId = _order.OrderNumber,
                    OrderName = $"테이블 {_order.TableName} 주문",
                    CustomerName = "매장고객",
                    CustomerEmail = "customer@restaurant.com",
                    CustomerMobilePhone = "01012345678",
                    SuccessUrl = "restaurantpos://payment/success",
                    FailUrl = "restaurantpos://payment/fail"
                };

                System.Diagnostics.Debug.WriteLine("결제 요청 데이터 생성 완료");
                System.Diagnostics.Debug.WriteLine($"OrderId: {paymentRequest.OrderId}");
                System.Diagnostics.Debug.WriteLine($"Amount: {paymentRequest.Amount}");
                System.Diagnostics.Debug.WriteLine($"OrderName: {paymentRequest.OrderName}");
                
                var clientKey = _tossPaymentsService.GetClientKey();
                var secretKey = _tossPaymentsService.GetSecretKey();
                System.Diagnostics.Debug.WriteLine($"Client Key: {clientKey}");

                if (string.IsNullOrWhiteSpace(clientKey))
                {
                    System.Windows.MessageBox.Show(
                        "카드 결제를 사용하려면 RestaurantPOS.WPF\\appsettings.json 파일의 TossPayments:ClientKey 설정이 필요합니다.\n\n" +
                        "appsettings.template.json을 복사해 appsettings.json을 만든 뒤 테스트용 ClientKey/SecretKey를 입력해주세요.",
                        "결제 설정 필요",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    System.Windows.MessageBox.Show(
                        "카드 결제를 승인하려면 RestaurantPOS.WPF\\appsettings.json 파일의 TossPayments:SecretKey 설정이 필요합니다.\n\n" +
                        "appsettings.template.json을 복사해 appsettings.json을 만든 뒤 테스트용 ClientKey/SecretKey를 입력해주세요.",
                        "결제 설정 필요",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                _paymentWindow = paymentWebViewWindow;
                System.Diagnostics.Debug.WriteLine($"Window 생성 확인 - IsLoaded: {paymentWebViewWindow.IsLoaded}");
                System.Diagnostics.Debug.WriteLine($"Window Content: {paymentWebViewWindow.Content?.GetType().Name ?? "null"}");
                
                // 창을 먼저 보여주고 초기화
                System.Diagnostics.Debug.WriteLine("Show 호출 전");
                paymentWebViewWindow.Show();
                System.Diagnostics.Debug.WriteLine("Show 호출 완료");
                
                // 잠시 대기하여 창이 완전히 렌더링되도록 함
                await Task.Delay(100);
                
                // 결제창 초기화를 창이 표시된 후에 수행
                System.Diagnostics.Debug.WriteLine("InitializePayment 호출 전");
                await paymentWebView.InitializePayment(paymentRequest, clientKey);
                System.Diagnostics.Debug.WriteLine("InitializePayment 호출 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"결제창 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                System.Windows.MessageBox.Show($"결제창 초기화 실패: {ex.Message}", "오류", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void OnCancel()
        {
            PaymentCancelled?.Invoke(this, EventArgs.Empty);
        }

        public void SetRetryMode(int retryTransactionId)
        {
            _isRetryMode = true;
            _retryTransactionId = retryTransactionId;
        }
    }

    public class PaymentCompletedEventArgs : EventArgs
    {
        public string PaymentMethod { get; set; }
        public bool IsSuccess { get; set; }
        public string PaymentKey { get; set; }
        public string TransactionId { get; set; }
        public string ErrorMessage { get; set; }
    }
}
