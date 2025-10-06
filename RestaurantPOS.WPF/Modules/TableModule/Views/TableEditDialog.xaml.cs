using DevExpress.Xpf.Core;
using RestaurantPOS.WPF.Modules.TableModule.ViewModels;
using System.ComponentModel;

namespace RestaurantPOS.WPF.Modules.TableModule.Views
{
    public partial class TableEditDialog : ThemedWindow
    {
        public TableEditDialogViewModel ViewModel => DataContext as TableEditDialogViewModel;

        public TableEditDialog()
        {
            InitializeComponent();
            
            // ViewModel의 DialogResult 변경을 감지하여 Window의 DialogResult 설정
            if (DataContext is TableEditDialogViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TableEditDialogViewModel.DialogResult))
            {
                if (DataContext is TableEditDialogViewModel viewModel && viewModel.DialogResult.HasValue)
                {
                    this.DialogResult = viewModel.DialogResult;
                }
            }
        }
    }
}