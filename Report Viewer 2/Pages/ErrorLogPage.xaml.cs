using System;
using System.Windows;
using System.Windows.Controls;
using FirstFloor.ModernUI.Windows.Controls;

namespace Report_Viewer_2.Pages
{
    /// <summary>
    /// Interaction logic for ErrorLogPage.xaml
    /// </summary>
    public partial class ErrorLogPage : UserControl
    {
        public ErrorLogPage()
        {
            InitializeComponent();
        }

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbMatomoId.Text))
            {
                ModernDialog.ShowMessage("請先填入使用者 ID。", "警告", MessageBoxButton.OK);
                return;
            }
            if (!dateSelector.datePicker.SelectedDate.HasValue)
            {
                ModernDialog.ShowMessage("請先選擇日期。    ", "警告", MessageBoxButton.OK);
                return;
            }

            try
            {
                Window.ErrorIdWindow eiw = new Window.ErrorIdWindow(tbMatomoId.Text, dateSelector.Duration, dateSelector.datePicker.SelectedDate.Value, dateSelector.rangeStartDatePicker.SelectedDate);
                eiw.Owner = System.Windows.Window.GetWindow(this);
                eiw.ShowDialog();
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.ToString(), "錯誤", MessageBoxButton.OK);
            }
        }
    }
}
