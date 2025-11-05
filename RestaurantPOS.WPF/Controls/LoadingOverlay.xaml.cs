using System.Windows;
using System.Windows.Controls;

namespace RestaurantPOS.WPF.Controls
{
    public partial class LoadingOverlay : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(LoadingOverlay), 
                new PropertyMetadata("로딩 중...", OnMessageChanged));

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public LoadingOverlay()
        {
            InitializeComponent();
        }

        private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LoadingOverlay overlay && e.NewValue is string message)
            {
                overlay.LoadingText.Text = message;
            }
        }
    }
}