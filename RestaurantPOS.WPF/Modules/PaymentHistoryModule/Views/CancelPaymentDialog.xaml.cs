using RestaurantPOS.Core.DTOs;
using System.Windows;

namespace RestaurantPOS.WPF.Modules.PaymentHistoryModule.Views
{
    public partial class CancelPaymentDialog : Window
    {
        public CancelPaymentDialog(PaymentTransactionDTO payment)
        {
            InitializeComponent();
            DataContext = new CancelPaymentDialogViewModel(payment);
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
    }

    public class CancelPaymentDialogViewModel
    {
        public string PaymentMethod { get; }
        public decimal Amount { get; }
        public System.DateTime PaymentDate { get; }

        public CancelPaymentDialogViewModel(PaymentTransactionDTO payment)
        {
            PaymentMethod = payment.DisplayPaymentMethod;
            Amount = payment.Amount;
            PaymentDate = payment.PaymentDate;
        }
    }
}