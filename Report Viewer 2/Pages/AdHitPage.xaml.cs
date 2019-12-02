using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FirstFloor.ModernUI.Windows.Controls;
using Report_Viewer_2.Window;
using Utility;

namespace Report_Viewer_2.Pages
{
    /// <summary>
    /// Interaction logic for AdHitPage.xaml
    /// </summary>
    public partial class AdHitPage : UserControl
    {
        public AdHitPage()
        {
            InitializeComponent();
        }

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            if (!dateSelector.datePicker.SelectedDate.HasValue)
            {
                ModernDialog.ShowMessage("請先選擇日期。    ", "警告", MessageBoxButton.OK);
                return;
            }

            ReportDuration duration = (ReportDuration)dateSelector.cbReportDuration.SelectedIndex;
            DateTime date = dateSelector.datePicker.SelectedDate.Value;

            DateTime? startDate = null;
            if (dateSelector.Duration == ReportDuration.range) // 自訂日期
            {
                if (!dateSelector.rangeStartDatePicker.SelectedDate.HasValue)
                {
                    ModernDialog.ShowMessage("請先選擇日期。    ", "警告", MessageBoxButton.OK);
                    return;
                }
                startDate = dateSelector.rangeStartDatePicker.SelectedDate.Value;
            }

            try
            {
                AdVerifyWindow r = new AdVerifyWindow(
                    "10.10.15.62", 3306, "matomodb", "matomouser", "penpower",
                    duration, date, startDate);
                r.Owner = System.Windows.Window.GetWindow(this);
                r.ShowDialog();
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.ToString(), "錯誤", MessageBoxButton.OK);
            }
        }
    }
}
