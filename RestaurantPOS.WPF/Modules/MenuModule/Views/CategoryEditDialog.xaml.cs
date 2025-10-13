using DevExpress.Xpf.Core;
using RestaurantPOS.WPF.Modules.MenuModule.ViewModels;
using System.Windows;

namespace RestaurantPOS.WPF.Modules.MenuModule.Views
{
    /// <summary>
    /// Interaction logic for CategoryEditDialog.xaml
    /// </summary>
    public partial class CategoryEditDialog : ThemedWindow
    {
        public CategoryEditDialog()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is CategoryEditDialogViewModel viewModel)
            {
                // Command가 실행되어 DialogResult가 설정되었는지 확인
                if (viewModel.SaveCommand.CanExecute(null))
                {
                    viewModel.SaveCommand.Execute(null);
                }
                
                if (viewModel.DialogResult)
                {
                    DialogResult = true;
                    Close();
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}