using Prism.Commands;
using Prism.Mvvm;
using RestaurantPOS.Core.DTOs;
using System;
using System.Windows.Input;

namespace RestaurantPOS.WPF.Modules.OrderModule.ViewModels
{
    public class PaymentViewModel : BindableBase
    {
        private OrderDTO _order;
        private string _selectedPaymentMethod;
        
        public event EventHandler<string> PaymentCompleted;
        public event EventHandler PaymentCancelled;

        public PaymentViewModel()
        {
            CashPaymentCommand = new DelegateCommand(() => ProcessPayment("Cash"));
            CardPaymentCommand = new DelegateCommand(() => ProcessPayment("Card"));
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
            PaymentCompleted?.Invoke(this, paymentMethod);
        }

        private void OnCancel()
        {
            PaymentCancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}