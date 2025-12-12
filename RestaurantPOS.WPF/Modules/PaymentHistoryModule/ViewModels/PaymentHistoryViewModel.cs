using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Interfaces;
using RestaurantPOS.WPF.Modules.PaymentHistoryModule.Views;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RestaurantPOS.WPF.Modules.PaymentHistoryModule.ViewModels
{
    public class PaymentHistoryViewModel : BindableBase, INavigationAware
    {
        private readonly IPaymentHistoryService _paymentHistoryService;
        private readonly IRegionManager _regionManager;
        private readonly IOrderService _orderService;
        private readonly IPrintService _printService;
        private readonly ITossPaymentsService? _tossPaymentsService;

        #region Properties
        private ObservableCollection<PaymentHistoryDTO> _orders;
        public ObservableCollection<PaymentHistoryDTO> Orders
        {
            get { return _orders; }
            set { SetProperty(ref _orders, value); }
        }

        private PaymentHistoryDTO _selectedOrder;
        public PaymentHistoryDTO SelectedOrder
        {
            get { return _selectedOrder; }
            set 
            { 
                SetProperty(ref _selectedOrder, value);
                RaisePropertyChanged(nameof(IsOrderSelected));
            }
        }

        public bool IsOrderSelected => SelectedOrder != null;

        private DateTime _startDate = DateTime.Today;
        public DateTime StartDate
        {
            get { return _startDate; }
            set { SetProperty(ref _startDate, value); }
        }

        private DateTime _endDate = DateTime.Today;
        public DateTime EndDate
        {
            get { return _endDate; }
            set { SetProperty(ref _endDate, value); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { SetProperty(ref _statusMessage, value); }
        }

        private ObservableCollection<string> _paymentMethodOptions;
        public ObservableCollection<string> PaymentMethodOptions
        {
            get { return _paymentMethodOptions; }
            set { SetProperty(ref _paymentMethodOptions, value); }
        }

        private string _selectedPaymentMethod = "전체";
        public string SelectedPaymentMethod
        {
            get { return _selectedPaymentMethod; }
            set { SetProperty(ref _selectedPaymentMethod, value); }
        }

        private bool _showOnlySyncFailed;
        public bool ShowOnlySyncFailed
        {
            get { return _showOnlySyncFailed; }
            set { SetProperty(ref _showOnlySyncFailed, value); }
        }
        #endregion

        #region Commands
        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand ReprintReceiptCommand { get; }
        public ICommand CancelPaymentCommand { get; }
        public ICommand RetryPaymentCommand { get; }
        public ICommand RetrySyncCommand { get; }
        #endregion

        public PaymentHistoryViewModel(
            IPaymentHistoryService paymentHistoryService,
            IRegionManager regionManager,
            IOrderService orderService,
            IPrintService printService,
            ITossPaymentsService? tossPaymentsService = null)
        {
            _paymentHistoryService = paymentHistoryService;
            _regionManager = regionManager;
            _orderService = orderService;
            _printService = printService;
            _tossPaymentsService = tossPaymentsService;

            Orders = new ObservableCollection<PaymentHistoryDTO>();
            PaymentMethodOptions = new ObservableCollection<string> { "전체", "현금", "카드" };

            // Commands
            SearchCommand = new DelegateCommand(async () => await LoadPaymentHistoryAsync());
            RefreshCommand = new DelegateCommand(async () => await LoadPaymentHistoryAsync());
            BackCommand = new DelegateCommand(() => 
            {
                _regionManager.RequestNavigate("MainRegion", "TableManagementView");
            });
            
            ReprintReceiptCommand = new DelegateCommand<PaymentHistoryDTO>(async (order) => await ReprintReceiptAsync(order));
            CancelPaymentCommand = new DelegateCommand<PaymentTransactionDTO>(async (payment) => await CancelPaymentAsync(payment));
            RetryPaymentCommand = new DelegateCommand<PaymentTransactionDTO>(async (payment) => await RetryPaymentAsync(payment));
            RetrySyncCommand = new DelegateCommand<PaymentTransactionDTO>(async (payment) => await RetrySyncAsync(payment));
        }

        private async Task LoadPaymentHistoryAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "결제 내역을 불러오는 중...";

                var filter = new PaymentHistoryFilterDTO
                {
                    StartDate = StartDate,
                    EndDate = EndDate.AddDays(1).AddSeconds(-1), // 끝날짜의 23:59:59
                    PaymentMethod = SelectedPaymentMethod == "전체" ? null : SelectedPaymentMethod,
                    ShowOnlySyncFailed = ShowOnlySyncFailed,
                    PageSize = 100
                };

                var (items, totalCount) = await _paymentHistoryService.GetPaymentHistoryAsync(filter);

                Orders.Clear();
                foreach (var item in items)
                {
                    Orders.Add(item);
                }

                StatusMessage = $"총 {totalCount}건의 결제 내역을 찾았습니다.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"오류 발생: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #region INavigationAware
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 화면 진입 시 오늘 날짜의 결제 내역 자동 조회
            Task.Run(async () => await LoadPaymentHistoryAsync());
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 화면 떠날 때 필요한 정리 작업
        }
        #endregion

        #region Command Methods
        private async Task ReprintReceiptAsync(PaymentHistoryDTO order)
        {
            if (order == null) return;

            try
            {
                StatusMessage = "영수증을 재출력하는 중...";
                
                // PrintService를 통한 영수증 재출력
                var result = await _printService.PrintReceiptForPaymentHistoryAsync(order);
                
                if (result)
                {
                    StatusMessage = "영수증 재출력이 완료되었습니다.";
                }
                else
                {
                    StatusMessage = "영수증 재출력에 실패했습니다. PDF로 저장되었을 수 있습니다.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"영수증 재출력 실패: {ex.Message}";
            }
        }

        private async Task CancelPaymentAsync(PaymentTransactionDTO payment)
        {
            if (payment == null || !payment.CanCancel) return;

            try
            {
                // 취소 확인 다이얼로그 표시
                var dialog = new CancelPaymentDialog(payment);
                if (dialog.ShowDialog() != true)
                {
                    StatusMessage = "결제 취소가 취소되었습니다.";
                    return;
                }

                StatusMessage = "결제를 취소하는 중...";
                
                // 결제 취소 처리
                var result = await _orderService.CancelPaymentTransactionAsync(
                    payment.PaymentTransactionId, 
                    "사용자 요청");

                if (result)
                {
                    StatusMessage = "결제가 성공적으로 취소되었습니다.";
                    
                    // 목록 새로고침
                    await LoadPaymentHistoryAsync();
                    
                    // 선택된 주문이 있다면 해당 주문 정보도 다시 로드
                    if (SelectedOrder != null)
                    {
                        var updatedOrder = await _paymentHistoryService.GetPaymentHistoryDetailAsync(SelectedOrder.OrderId);
                        if (updatedOrder != null)
                        {
                            SelectedOrder = updatedOrder;
                        }
                    }
                }
                else
                {
                    StatusMessage = "결제 취소에 실패했습니다.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"결제 취소 실패: {ex.Message}";
            }
        }

        private async Task RetryPaymentAsync(PaymentTransactionDTO payment)
        {
            if (payment == null || !payment.CanRetry) return;

            try
            {
                // 재결제 다이얼로그 표시
                var dialog = new RetryPaymentDialog(payment);
                if (dialog.ShowDialog() != true)
                {
                    StatusMessage = "재결제가 취소되었습니다.";
                    return;
                }

                var newPaymentMethod = dialog.GetSelectedPaymentMethod();
                StatusMessage = $"{(newPaymentMethod == "Cash" ? "현금" : "카드")}으로 재결제를 처리하는 중...";

                if (newPaymentMethod == "Cash")
                {
                    // 현금 결제는 바로 처리
                    var result = await _orderService.RetryPaymentAsync(
                        payment.PaymentTransactionId,
                        newPaymentMethod);

                    if (result != null)
                    {
                        StatusMessage = "현금 재결제가 완료되었습니다.";
                        await RefreshAfterPaymentChange();
                    }
                }
                else // Card
                {
                    // 카드 재결제는 토스페이먼츠 연동이 필요하므로 일단 간단히 처리
                    // TODO: [Future Feature] 실제 카드 결제 창 연동 필요 - v2.0에서 구현 예정
                    StatusMessage = "카드 재결제 기능은 준비 중입니다. 현금 재결제를 이용해주세요.";
                    
                    // 테스트를 위해 임시로 카드 재결제도 바로 처리
                    /*
                    var result = await _orderService.RetryPaymentAsync(
                        payment.PaymentTransactionId,
                        newPaymentMethod,
                        "test_payment_key",
                        "test_transaction_id");

                    if (result != null)
                    {
                        StatusMessage = "카드 재결제가 완료되었습니다 (테스트).";
                        await RefreshAfterPaymentChange();
                    }
                    */
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"재결제 실패: {ex.Message}";
            }
        }

        private async Task RefreshAfterPaymentChange()
        {
            // 목록 새로고침
            await LoadPaymentHistoryAsync();
            
            // 선택된 주문이 있다면 해당 주문 정보도 다시 로드
            if (SelectedOrder != null)
            {
                var updatedOrder = await _paymentHistoryService.GetPaymentHistoryDetailAsync(SelectedOrder.OrderId);
                if (updatedOrder != null)
                {
                    SelectedOrder = updatedOrder;
                }
            }
        }

        private async Task RetrySyncAsync(PaymentTransactionDTO payment)
        {
            if (payment == null || !payment.NeedsSync) return;

            try
            {
                StatusMessage = "동기화를 재시도하는 중...";
                // TODO: [Future Feature] PaymentSyncService를 통한 동기화 재시도 - v2.0에서 구현 예정
                await LoadPaymentHistoryAsync(); // 목록 새로고침
                StatusMessage = "동기화가 완료되었습니다.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"동기화 실패: {ex.Message}";
            }
        }
        #endregion
    }
}