using System;
using System.Windows;
using System.Windows.Controls;
using Utility;

namespace Report_Viewer_2
{
    /// <summary>
    /// DateSelector.xaml 的互動邏輯
    /// </summary>
    public partial class DateSelector : UserControl
    {
        public ReportDuration Duration
        {
            get { return (ReportDuration) cbReportDuration.SelectedIndex; }
        }

        public DateSelector()
        {
            InitializeComponent();
            Loaded += DateSelector_Loaded;
        }

        private void DateSelector_Loaded(object sender, RoutedEventArgs e)
        {
            if (cbReportDuration.SelectedIndex < 0)
                cbReportDuration.SelectedIndex = 0;
        }

        private void cbReportDuration_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbReportDuration.SelectedIndex == 4)
            {
                rangeStartDatePicker.Visibility = Visibility.Visible;
                lbStart.Visibility = Visibility.Visible;
                lbEnd.Visibility = Visibility.Visible;
            }
            else
            {
                rangeStartDatePicker.Visibility = Visibility.Collapsed;
                lbStart.Visibility = Visibility.Collapsed;
                lbEnd.Visibility = Visibility.Collapsed;
            }
        }
    }
}
