using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Utility;

namespace ReportViewer
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<string> userDefindedFavorite = new ObservableCollection<string>();
        private List<Projects> projectNames;        

        public MainWindow()
        {
            InitializeComponent();
            datePicker.SelectedDate = DateTime.Today;
            rangeStartDatePicker.SelectedDate = DateTime.Today.AddDays(-1);
            tbFavoriteLimit.Text = "15";

            userDefinedList.ItemsSource = userDefindedFavorite;
            lbBuildNum.Content += Build.Timestamp;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            projectNames = Enum.GetValues(typeof(Projects)).Cast<Projects>().ToList();
            comboProjectList.ItemsSource = projectNames;
            //comboProjectList.SelectedIndex = 0;
            matomoIdBox.Visibility = Visibility.Hidden;
            favoriteBox.Visibility = Visibility.Hidden;
            operationsGrid.Visibility = Visibility.Hidden;
            resultBox.Visibility = Visibility.Hidden;
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

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int favoriteLimit = 15;
                if (!int.TryParse(tbFavoriteLimit.Text, out favoriteLimit))
                    favoriteLimit = 15;  // default value is 15

                Platform platform = Platform.All;
                if (radioAll.IsChecked == true)
                    platform = Platform.All;
                else if (radioAndroid.IsChecked == true)
                    platform = Platform.Android;
                else if (radioIos.IsChecked == true)
                    platform = Platform.IOS;
                else if (radioMac.IsChecked == true)
                    platform = Platform.Mac;
                else if (radioWin.IsChecked == true)
                    platform = Platform.Win;

                Projects p = (Projects)comboProjectList.SelectedItem;
                int projectID = (int)p;

                ReportDuration duration = (ReportDuration)cbReportDuration.SelectedIndex;
                DateTime date = datePicker.SelectedDate.Value;

                /*if (p == Projects.WC8_Ad_Verify)
                {
                    DateTime? startDate = null;
                    if (cbReportDuration.SelectedIndex == 4) // 自訂日期
                        startDate = rangeStartDatePicker.SelectedDate.Value;
                    RegisterVerifyWindow r = new RegisterVerifyWindow(
                        "10.10.15.62", 3306, "matomodb", "matomouser", "penpower",
                        duration, date, startDate);
                    r.Owner = this;
                    r.ShowDialog();
                    return;
                }
                else if (p == Projects.View_WC8_Error)
                {
                    if (string.IsNullOrWhiteSpace(tbMatomoId.Text))
                    {
                        MessageBox.Show("請先填入使用者 ID。");
                        return;
                    }
                    ErrorIdWindow eiw = new ErrorIdWindow(tbMatomoId.Text, duration, date, rangeStartDatePicker.SelectedDate);
                    eiw.Owner = this;
                    eiw.ShowDialog();
                    return;
                }*/

                if (cbReportDuration.SelectedIndex == 4)  // 自訂日期
                {
                    if (rangeStartDatePicker.SelectedDate.Value >= date)
                    {
                        MessageBox.Show(this, "Start date must earlier than end date.");
                        return;
                    }
                    UserVisitWindow uw = new UserVisitWindow(projectID, platform, duration, favoriteLimit, userDefindedFavorite.ToList(), date, rangeStartDatePicker.SelectedDate.Value);
                    uw.Owner = this;
                    uw.ShowDialog();
                }
                else
                {
                    UserVisitWindow uw = new UserVisitWindow(projectID, platform, duration, favoriteLimit, userDefindedFavorite.ToList(), date);
                    uw.Owner = this;
                    uw.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            string newText = tbAdd.Text;
            if (string.IsNullOrEmpty(newText))
            {
                MessageBox.Show(this, "Please type something!");
                return;
            }
            else if (userDefindedFavorite.Contains(newText))
            {
                MessageBox.Show(this, "Duplicated item!");
                return;
            }
            else
            {
                userDefindedFavorite.Add(newText);
                tbAdd.Text = string.Empty;
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            int index = userDefinedList.SelectedIndex;
            if (index >= 0)
            {
                userDefindedFavorite.RemoveAt(index);
            }
        }

        private void tbFavoriteLimit_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = IsTextAllowed(e.Text);
        }

        private bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9]"); //regex that matches disallowed text
            return regex.IsMatch(text);
        }

        private void comboProjectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboProjectList.SelectedIndex == -1)
            {
                favoriteBox.Visibility = Visibility.Hidden;
                operationsGrid.Visibility = Visibility.Hidden;
                resultBox.Visibility = Visibility.Hidden;
                return;
            }

            // 決定不同專案的類別字串
            userDefindedFavorite.Clear();
            Projects p = (Projects)comboProjectList.SelectedItem;
            switch (p)
            {
                case Projects.WC8:
                    foreach (string op in GetWC8FavoriteList())
                    {
                        userDefindedFavorite.Add(op);
                    }
                    break;
                case Projects.WCT:
                    foreach (string wObject in GetWCTFavoriteList())
                    {
                        userDefindedFavorite.Add(wObject.ToString());
                    }
                    break;
                case Projects.WDUSB:
                    foreach (string wObject in GetWDUSBFavoriteList())
                    {
                        userDefindedFavorite.Add(wObject.ToString());
                    }
                    break;
                case Projects.WPSX:
                    foreach (string wObject in GetWPSXFavoriteList())
                    {
                        userDefindedFavorite.Add(wObject.ToString());
                    }
                    break;
            }

            // UI control
            matomoIdBox.Visibility = Visibility.Collapsed;
            /*if (p == Projects.WC8_Ad_Verify || p == Projects.View_WC8_Error)
            {
                favoriteBox.Visibility = Visibility.Hidden;
                operationsGrid.Visibility = Visibility.Hidden;
                resultBox.Visibility = Visibility.Visible;
                if (p == Projects.View_WC8_Error)
                {
                    tbDescription.Text = "尋找使用者 Matomo ID \n指定範圍內的操作與錯誤紀錄。";
                    matomoIdBox.Visibility = Visibility.Visible;
                }
                else if (p == Projects.WC8_Ad_Verify)
                    tbDescription.Text = "尋找 WorldCard 8 中點擊了 WorldCard Team 廣告並完成註冊的使用者紀錄。";
            }
            else*/
            {
                favoriteBox.Visibility = Visibility.Visible;
                operationsGrid.Visibility = Visibility.Visible;
                resultBox.Visibility = Visibility.Visible;
                tbDescription.Text = $"計算專案 {p} \n中使用者最愛的功能。";
            }
        }

        #region Generate operatrion list for different projects

        private List<string> GetWC8FavoriteList()
        {
            List<string> operations = new List<string>();
            Array vEnum = Enum.GetValues(typeof(WC8.Tracker.WCR_OP));
            foreach (var wObject in vEnum)
            {
                operations.Add(wObject.ToString());
            }
            vEnum = Enum.GetValues(typeof(WC8.Tracker.WCR_Import_OP));
            foreach (var wObject in vEnum)
            {
                operations.Add(wObject.ToString());
            }
            vEnum = Enum.GetValues(typeof(WC8.Tracker.WCR_Export_OP));
            foreach (var wObject in vEnum)
            {
                operations.Add(wObject.ToString());
            }
            vEnum = Enum.GetValues(typeof(WC8.Tracker.WCR_SYNC_OP));
            foreach (var wObject in vEnum)
            {
                operations.Add(wObject.ToString());
            }
            return operations;
        }

        private List<string> GetWCTFavoriteList()
        {
            List<string> operations = new List<string>();
            Array vEnum = Enum.GetValues(typeof(WCT.Tracker.WCT_OP));
            foreach (var wObject in vEnum)
            {
                operations.Add(wObject.ToString());
            }
            vEnum = Enum.GetValues(typeof(WCT.Tracker.WCT_Import_OP));
            foreach (var wObject in vEnum)
            {
                operations.Add(wObject.ToString());
            }
            vEnum = Enum.GetValues(typeof(WCT.Tracker.WCT_Export_OP));
            foreach (var wObject in vEnum)
            {
                operations.Add(wObject.ToString());
            }
            vEnum = Enum.GetValues(typeof(WCT.Tracker.WCT_SYNC_OP));
            foreach (var wObject in vEnum)
            {
                operations.Add(wObject.ToString());
            }
            return operations;
        }

        private List<string> GetWPSXFavoriteList()
        {
            List<string> operations = new List<string>();
            Array vWPSX = Enum.GetValues(typeof(WPSX.Tracker.WPSX_OP));
            foreach (var wObject in vWPSX)
            {
                operations.Add(wObject.ToString());
            }
            return operations;
        }

        private List<string> GetWDUSBFavoriteList()
        {
            List<string> operations = new List<string>();
            Array vWPSX = Enum.GetValues(typeof(WDUSB.Tracker.WDUSB_OP));
            foreach (var wObject in vWPSX)
            {
                operations.Add(wObject.ToString());
            }
            return operations;
        }

        #endregion

        private void BtnUpdate_OnClick(object sender, RoutedEventArgs e)
        {
            DateTime localDate = Updater.GetLocalBuildDate();
            DateTime remoteDate = Updater.GetRemoteBuildDate();
            if (remoteDate == DateTime.MinValue)
                MessageBox.Show("無法連線至\n" + @"\\10.10.10.3\Share\Tenny\Piwik-Matomo 文件與工具\Matomo 報告產生工具\");
            else
            {
                if (remoteDate > localDate)
                {
                    string str = "New version:\nbuild time " + remoteDate.ToString() + "\nat\n" + @"\\10.10.10.3\Share\Tenny\Piwik-Matomo 文件與工具\Matomo 報告產生工具\";
                    MessageBox.Show(str);
                }
                else
                    MessageBox.Show("目前找不到更新。");
            }
        }
    }
}
