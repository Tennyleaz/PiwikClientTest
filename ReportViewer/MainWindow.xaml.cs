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
            comboProjectList.SelectedIndex = 0;
            
            DateTime localDate = Updater.GetLocalBuildDate();
            DateTime remoteDate = Updater.GetRemoteBuildDate();
            if (remoteDate > localDate)
            {
                string str = "New version:\nbuild time " + remoteDate.ToString() + "\nat\n" + @"\\10.10.10.3\Share\Tenny\Piwik Testing\Test App\";
                MessageBox.Show(str);
            }
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
                if (cbReportDuration.SelectedIndex == 4)
                {
                    if (rangeStartDatePicker.SelectedDate.Value >= date)
                    {
                        MessageBox.Show(this, "start date must earlier than end date");
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
            throw new NotImplementedException();
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
    }
}
