using DevExpress.Xpf.Core;
using RestaurantPOS.WPF.Modules.TableModule.ViewModels;
using System.ComponentModel;

namespace RestaurantPOS.WPF.Modules.TableModule.Views
{
    /// <summary>
    /// SpaceEditDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SpaceEditDialog : ThemedWindow
    {
        public SpaceEditDialog()
        {
            InitializeComponent();
            
            // ViewModel의 DialogResult 변경을 감지하여 Window의 DialogResult 설정
            if (DataContext is SpaceEditDialogViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SpaceEditDialogViewModel.DialogResult))
            {
                if (DataContext is SpaceEditDialogViewModel viewModel && viewModel.DialogResult.HasValue)
                {
                    this.DialogResult = viewModel.DialogResult;
                }
            }
        }

        public SpaceEditDialogViewModel ViewModel => DataContext as SpaceEditDialogViewModel;
    }
}