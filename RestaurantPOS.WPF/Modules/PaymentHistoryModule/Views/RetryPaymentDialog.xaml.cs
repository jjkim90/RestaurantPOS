using RestaurantPOS.Core.DTOs;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace RestaurantPOS.WPF.Modules.PaymentHistoryModule.Views
{
    public partial class RetryPaymentDialog : Window
    {
        public RetryPaymentDialogViewModel ViewModel { get; }

        public RetryPaymentDialog(PaymentTransactionDTO cancelledPayment)
        {
            InitializeComponent();
            ViewModel = new RetryPaymentDialogViewModel(cancelledPayment);
            DataContext = ViewModel;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CashButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsCashSelected = true;
            ViewModel.IsCardSelected = false;
            UpdateButtonStyles();
        }

        private void CardButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsCashSelected = false;
            ViewModel.IsCardSelected = true;
            UpdateButtonStyles();
        }

        private void UpdateButtonStyles()
        {
            // 현금 버튼 스타일
            if (ViewModel.IsCashSelected)
            {
                CashButton.BorderBrush = System.Windows.Media.Brushes.Black;
                CashButton.BorderThickness = new Thickness(3);
            }
            else
            {
                CashButton.BorderBrush = System.Windows.Media.Brushes.Transparent;
                CashButton.BorderThickness = new Thickness(0);
            }

            // 카드 버튼 스타일
            if (ViewModel.IsCardSelected)
            {
                CardButton.BorderBrush = System.Windows.Media.Brushes.Black;
                CardButton.BorderThickness = new Thickness(3);
            }
            else
            {
                CardButton.BorderBrush = System.Windows.Media.Brushes.Transparent;
                CardButton.BorderThickness = new Thickness(0);
            }
        }

        public string GetSelectedPaymentMethod()
        {
            return ViewModel.IsCashSelected ? "Cash" : "Card";
        }
    }

    public class RetryPaymentDialogViewModel : INotifyPropertyChanged
    {
        private readonly PaymentTransactionDTO _cancelledPayment;
        private bool _isCashSelected;
        private bool _isCardSelected;

        public string OriginalPaymentMethod { get; }
        public decimal Amount { get; }
        public DateTime? CancelledDate { get; }
        public string? CancelReason { get; }

        public bool IsCashSelected
        {
            get => _isCashSelected;
            set
            {
                _isCashSelected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanConfirm));
            }
        }

        public bool IsCardSelected
        {
            get => _isCardSelected;
            set
            {
                _isCardSelected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanConfirm));
            }
        }

        public bool CanConfirm => IsCashSelected || IsCardSelected;

        public RetryPaymentDialogViewModel(PaymentTransactionDTO cancelledPayment)
        {
            _cancelledPayment = cancelledPayment;
            OriginalPaymentMethod = cancelledPayment.DisplayPaymentMethod;
            Amount = cancelledPayment.Amount;
            CancelledDate = cancelledPayment.CancelledDate;
            CancelReason = cancelledPayment.CancelReason;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}