using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for FavoritePage.xaml
    /// </summary>
    public partial class FavoritePage : UserControl
    {
        private ObservableCollection<string> userDefindedFavorite = new ObservableCollection<string>();
        private List<Projects> projectNames;

        public FavoritePage()
        {
            InitializeComponent();
            Loaded += FavoritePage_Loaded;
        }

        private void FavoritePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (comboProjectList.SelectedIndex < 0)
            {
                projectNames = Enum.GetValues(typeof(Projects)).Cast<Projects>().ToList();
                tbFavoriteLimit.Text = "15";
                platformPanel.Visibility = Visibility.Collapsed;
                operationsGrid.Visibility = Visibility.Collapsed;
                resultPanel.Visibility = Visibility.Collapsed;
                comboProjectList.ItemsSource = projectNames;
                userDefinedList.ItemsSource = userDefindedFavorite;
            }
        }

        private void comboProjectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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

            platformPanel.Visibility = Visibility.Visible;
            operationsGrid.Visibility = Visibility.Visible;
            resultPanel.Visibility = Visibility.Visible;
            tbDescription.Text = $"計算專案 {p} \n中使用者最愛的功能。";
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

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            if (!dateSelector.datePicker.SelectedDate.HasValue)
            {
                ModernDialog.ShowMessage("請先選擇日期。    ", "警告", MessageBoxButton.OK);
                return;
            }

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

            ReportDuration duration = dateSelector.Duration;
            DateTime date = dateSelector.datePicker.SelectedDate.Value;

            try
            {
                if (duration == ReportDuration.range)  // 自訂日期
                {
                    if (!dateSelector.rangeStartDatePicker.SelectedDate.HasValue || dateSelector.rangeStartDatePicker.SelectedDate.Value >= date)
                    {
                        ModernDialog.ShowMessage("Start date must earlier than end date.", "警告", MessageBoxButton.OK);
                        return;
                    }
                    UserVisitWindow uw = new UserVisitWindow(projectID, platform, duration, favoriteLimit, userDefindedFavorite.ToList(), date, dateSelector.rangeStartDatePicker.SelectedDate.Value);
                    uw.Owner = System.Windows.Window.GetWindow(this);
                    uw.ShowDialog();
                }
                else
                {
                    UserVisitWindow uw = new UserVisitWindow(projectID, platform, duration, favoriteLimit, userDefindedFavorite.ToList(), date);
                    uw.Owner = System.Windows.Window.GetWindow(this);
                    uw.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    MessageBox.Show(ex.InnerException.ToString());
                else
                    MessageBox.Show(ex.ToString());
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

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            string newText = tbAdd.Text;
            if (string.IsNullOrEmpty(newText))
            {
                MessageBox.Show("Please type something!");
                return;
            }
            else if (userDefindedFavorite.Contains(newText))
            {
                MessageBox.Show("Duplicated item!");
                return;
            }
            else
            {
                userDefindedFavorite.Add(newText);
                tbAdd.Text = string.Empty;
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

    }
}
