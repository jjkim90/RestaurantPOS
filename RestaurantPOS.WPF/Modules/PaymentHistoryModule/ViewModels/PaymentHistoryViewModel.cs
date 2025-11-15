using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Interfaces;
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

        #region Properties
        private ObservableCollection<PaymentHistoryDTO> _paymentHistories;
        public ObservableCollection<PaymentHistoryDTO> PaymentHistories
        {
            get { return _paymentHistories; }
            set { SetProperty(ref _paymentHistories, value); }
        }

        private PaymentHistoryDTO _selectedPaymentHistory;
        public PaymentHistoryDTO SelectedPaymentHistory
        {
            get { return _selectedPaymentHistory; }
            set { SetProperty(ref _selectedPaymentHistory, value); }
        }

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
        #endregion

        #region Commands
        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }
        #endregion

        public PaymentHistoryViewModel(
            IPaymentHistoryService paymentHistoryService,
            IRegionManager regionManager)
        {
            _paymentHistoryService = paymentHistoryService;
            _regionManager = regionManager;

            PaymentHistories = new ObservableCollection<PaymentHistoryDTO>();

            // Commands
            SearchCommand = new DelegateCommand(async () => await LoadPaymentHistoryAsync());
            RefreshCommand = new DelegateCommand(async () => await LoadPaymentHistoryAsync());
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
                    PageSize = 100
                };

                var (items, totalCount) = await _paymentHistoryService.GetPaymentHistoryAsync(filter);

                PaymentHistories.Clear();
                foreach (var item in items)
                {
                    PaymentHistories.Add(item);
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
    }
}