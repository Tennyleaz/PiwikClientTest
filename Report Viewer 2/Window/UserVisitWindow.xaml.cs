using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Newtonsoft.Json;
using Utility;

namespace Report_Viewer_2.Window
{
    /// <summary>
    /// Interaction logic for UserVisitWindow.xaml
    /// </summary>
    public partial class UserVisitWindow : ModernWindow
    {
        // real server
        private const string PiwikServerUrl = @"http://10.10.15.62/index.php?";
        private const string Token = "4791a3edddeb2f8e2161b7f51da27ed4";

        private const int TIMEOUT_MS = 30000;
        private const string NO_OP = "無有效操作";
        private const string NO_REGION = "無有效地區";
        private const string MIX = "Mix";

        private string timeLimitString;
        private string visitStartDateString;
        private string segmentStartDateString;
        private string visitEndDateString;
        private string rangeStartDateString;
        private DateTime timeLimit;
        private DateTime firstDayOfWeek;
        private DateTime rangeStartDate;
        private ReportDuration _duration;
        private long timeStampLowLimit;
        private long timeStampHighLimit;
        private int _favoriteLimit;
        private List<string> _favoriteList;
        private Platform _platform;
        private int _projectID = 1;
        private RemoteManager rManager;
        private BackgroundWorker worker;

        public ObservableCollection<DisplayUser> DisplayUserCollection = new ObservableCollection<DisplayUser>();
        public ObservableCollection<DisplayRegion> DisplayRegionCollection = new ObservableCollection<DisplayRegion>();
        public ObservableCollection<DisplayCountry> DisplayCountryCollection = new ObservableCollection<DisplayCountry>();

        public UserVisitWindow(int projcetID, Platform platform, ReportDuration duration, int favoriteLimit, List<string> userDefinedFavorite, DateTime endDate, DateTime? startDate = null)
        {
            // check parameters
            if (duration == ReportDuration.range)
            {
                if (startDate == null)
                    throw new ArgumentException("neead a start date of range duration", "startDate");
                if (startDate >= endDate)
                    throw new ArgumentOutOfRangeException("startDate", "start date must earlier than end date");
                rangeStartDate = startDate.Value;
            }

            InitializeComponent();
            // disable ssl warning
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            _projectID = projcetID;
            _platform = platform;
            _favoriteLimit = favoriteLimit;
            _favoriteList = userDefinedFavorite;
            _duration = duration;
            timeLimit = endDate;
            timeLimitString = timeLimit.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            switch (duration)
            {
                default:
                case ReportDuration.day:
                    {
                        firstDayOfWeek = timeLimit.AddDays(-1);
                        visitStartDateString = firstDayOfWeek.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);  // 因為 segment 用 >，所以要比前一天大
                        visitEndDateString = timeLimit.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        timeStampLowLimit = ((DateTimeOffset)timeLimit).ToUnixTimeSeconds();
                        timeStampHighLimit = ((DateTimeOffset)timeLimit.AddDays(1)).ToUnixTimeSeconds();

                        string viewPeriod = " 一日內的檢視";
                        lbConditions.Content = "專案 " + _projectID + " " + timeLimitString + viewPeriod;

                        DateTime segmentStartDate = timeLimit.AddDays(-1);
                        segmentStartDateString = segmentStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        break;
                    }
                case ReportDuration.week:
                    {
                        // matomo 的每周第一天是星期一!!
                        DayOfWeek dayOfWeek = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(timeLimit);
                        int shiftedDay = (int)dayOfWeek;
                        if (dayOfWeek == DayOfWeek.Sunday)
                            shiftedDay = 7;  // 週日視為一周的結束，算出第一天要 -7
                        firstDayOfWeek = timeLimit.AddDays(-shiftedDay);
                        visitStartDateString = firstDayOfWeek.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        visitEndDateString = firstDayOfWeek.AddDays(7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        timeStampLowLimit = ((DateTimeOffset)firstDayOfWeek.AddDays(1)).ToUnixTimeSeconds();
                        timeStampHighLimit = ((DateTimeOffset)firstDayOfWeek.AddDays(8)).ToUnixTimeSeconds();

                        string viewPeriod = " 一周間的檢視";
                        string displayDate = firstDayOfWeek.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        string displayDate2 = firstDayOfWeek.AddDays(7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        lbConditions.Content = "專案 " + _projectID + " " + displayDate + " 至 " + displayDate2 + viewPeriod
                                               + " (Matomo 計算每周從周一開始)";

                        DateTime segmentStartDate = firstDayOfWeek;
                        segmentStartDateString = segmentStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        break;
                    }
                case ReportDuration.month:
                    {
                        firstDayOfWeek = new DateTime(endDate.Year, endDate.Month, 1);
                        int daysThisMonth = DateTime.DaysInMonth(endDate.Year, endDate.Month);
                        visitStartDateString = firstDayOfWeek.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        visitEndDateString = firstDayOfWeek.AddDays(daysThisMonth).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        timeStampLowLimit = ((DateTimeOffset)firstDayOfWeek).ToUnixTimeSeconds();
                        timeStampHighLimit = ((DateTimeOffset)firstDayOfWeek.AddDays(daysThisMonth)).ToUnixTimeSeconds();

                        string viewPeriod = " 起一月間的檢視";
                        lbConditions.Content = "專案 " + _projectID + " " + visitStartDateString + viewPeriod;

                        DateTime segmentStartDate = firstDayOfWeek.AddDays(-1);
                        segmentStartDateString = segmentStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        break;
                    }
                case ReportDuration.year:
                    {
                        firstDayOfWeek = new DateTime(endDate.Year, 1, 1);
                        visitStartDateString = firstDayOfWeek.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        visitEndDateString = firstDayOfWeek.AddDays(365).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        timeStampLowLimit = ((DateTimeOffset)firstDayOfWeek).ToUnixTimeSeconds();
                        timeStampHighLimit = ((DateTimeOffset)firstDayOfWeek.AddDays(365)).ToUnixTimeSeconds();

                        string viewPeriod = " 起一年間的檢視";
                        lbConditions.Content = "專案 " + _projectID + " " + visitStartDateString + viewPeriod;

                        DateTime segmentStartDate = firstDayOfWeek.AddDays(-1);
                        segmentStartDateString = segmentStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        break;
                    }
                case ReportDuration.range:
                    {
                        firstDayOfWeek = rangeStartDate.AddDays(-1);
                        visitStartDateString = firstDayOfWeek.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        rangeStartDateString = rangeStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        lbConditions.Content = "專案 " + _projectID + " 自訂檢視 " + rangeStartDateString + " ~ " + timeLimitString;
                        timeStampLowLimit = ((DateTimeOffset)rangeStartDate).ToUnixTimeSeconds();
                        timeStampHighLimit = ((DateTimeOffset)timeLimit.AddDays(1)).ToUnixTimeSeconds();

                        DateTime segmentStartDate = rangeStartDate.AddDays(-1);
                        segmentStartDateString = segmentStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rManager = new RemoteManager();
            if (rManager.InitWithKey("10.10.15.62"))
                rManager.RaiseActionDisplayLimit(1500);
            else
                MessageBox.Show("Could not login to remote server.");

            progressBar.Visibility = Visibility.Visible;
            progressText.Visibility = Visibility.Visible;
            userListView.IsEnabled = false;
            gridRegion.IsEnabled = false;

            userListView.ItemsSource = DisplayUserCollection;
            regionListView.ItemsSource = DisplayRegionCollection;
            countryListView.ItemsSource = DisplayCountryCollection;

            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.WorkerReportsProgress = true;
            worker.RunWorkerAsync();
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            rManager.ResetActionDisplayLimit();
            rManager.Dispose();

            userListView.IsEnabled = true;
            gridRegion.IsEnabled = true;
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Collapsed;
            progressText.Visibility = Visibility.Collapsed;

            object[] objResults = e.Result as object[];
            if (objResults != null && objResults.Count() == 3)
            {
                List<DisplayUser> resultList = objResults[0] as List<DisplayUser>;
                if (resultList != null)
                {
                    foreach (var du in resultList)
                        DisplayUserCollection.Add(du);
                }

                List<DisplayRegion> resultRegion = objResults[1] as List<DisplayRegion>;
                if (resultRegion != null)
                {
                    foreach (var du in resultRegion)
                        DisplayRegionCollection.Add(du);
                }

                List<DisplayCountry> resultCountry = objResults[2] as List<DisplayCountry>;
                if (resultCountry != null)
                {
                    foreach (var du in resultCountry)
                        DisplayCountryCollection.Add(du);
                }
            }

            lbConditions.Content += "，共 " + DisplayUserCollection.Count + " 位有效使用者的紀錄";
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Maximum = (int)e.UserState;
            progressBar.Value = e.ProgressPercentage;
            progressText.Content = e.ProgressPercentage + "/" + e.UserState;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            int progress = 0;
            int maxProgress = 0;
            List<DisplayUser> resultList = new List<DisplayUser>();
            List<DisplayRegion> resultRegion = new List<DisplayRegion>();
            List<DisplayCountry> resultCountry = new List<DisplayCountry>();
            List<MatomoUser> userList = null;

            if (GetVisitorIDs(out userList) == HttpStatusCode.OK)
            {
                maxProgress = userList.Count + 10;
                // Dict<region, Dict<favorite, count>>
                Dictionary<string, Dictionary<string, int>> dictRegion = new Dictionary<string, Dictionary<string, int>>();
                Dictionary<string, Dictionary<string, int>> dictCountry = new Dictionary<string, Dictionary<string, int>>();

                foreach (MatomoUser user in userList)
                {
                    if (GetVisitorProfile(user.idvisitor, out VisitorProfie profile) == HttpStatusCode.OK)
                    {
                        if (profile == null)
                        {
                            Console.WriteLine("Visitor profile is null!");
                            progress++;
                            worker.ReportProgress(progress, maxProgress);
                            continue;
                        }

                        // basic information
                        DisplayUser displayUser = new DisplayUser();
                        displayUser.id = user.idvisitor;
                        displayUser.name = user.label;
                        displayUser.country = profile.countries?.FirstOrDefault()?.prettyName;
                        displayUser.continent = profile.continents?.FirstOrDefault()?.prettyName;
                        displayUser.usageNumberArray = new int[_favoriteList.Count];
                        if (string.IsNullOrEmpty(displayUser.country))
                            displayUser.country = NO_REGION;
                        if (string.IsNullOrEmpty(displayUser.continent))
                            displayUser.continent = NO_REGION;

                        // find favorite url                        
                        CalculateFavorite(profile, displayUser);
                        if (displayUser.nb_visits > 0)  // 沒有算出次數user就不計
                        {
                            resultList.Add(displayUser);

                            // add favorite to region/country
                            if (dictRegion.ContainsKey(displayUser.continent))
                            {
                                var valueDict = dictRegion[displayUser.continent];
                                if (valueDict.ContainsKey(displayUser.favoriteUrl))
                                    valueDict[displayUser.favoriteUrl]++;
                                else
                                    valueDict.Add(displayUser.favoriteUrl, 1);
                            }
                            else
                            {
                                Dictionary<string, int> valueDict = new Dictionary<string, int>();
                                valueDict.Add(displayUser.favoriteUrl, 1);
                                dictRegion.Add(displayUser.continent, valueDict);
                            }

                            if (dictCountry.ContainsKey(displayUser.country))
                            {
                                var valueDict = dictCountry[displayUser.country];
                                if (valueDict.ContainsKey(displayUser.favoriteUrl))
                                    valueDict[displayUser.favoriteUrl]++;
                                else
                                    valueDict.Add(displayUser.favoriteUrl, 1);
                            }
                            else
                            {
                                Dictionary<string, int> valueDict = new Dictionary<string, int>();
                                valueDict.Add(displayUser.favoriteUrl, 1);
                                dictCountry.Add(displayUser.country, valueDict);
                            }
                        }
                    }
                    else
                        Console.WriteLine("Error get visitor profile!");

                    progress++;
                    worker.ReportProgress(progress, maxProgress);
                    System.Threading.Thread.Sleep(100);
                }

                // calculate favorite for region/country                
                foreach (var kvPairs in dictRegion)
                {
                    int maxFavorite = kvPairs.Value.Values.Max();
                    var maxKeyPair = kvPairs.Value.FirstOrDefault(x => x.Value == maxFavorite);  // maximum count of <favorite, count>
                    string favoriteUrl = maxKeyPair.Key;
                    DisplayRegion region = new DisplayRegion();
                    region.favoriteUrl = favoriteUrl;
                    region.region = kvPairs.Key;
                    region.nb_visits = maxFavorite;
                    resultRegion.Add(region);
                }
                worker.ReportProgress(maxProgress - 5, maxProgress);

                foreach (var kvPairs in dictCountry)
                {
                    int maxFavorite = kvPairs.Value.Values.Max();
                    var maxKeyPair = kvPairs.Value.FirstOrDefault(x => x.Value == maxFavorite);
                    string favoriteUrl = maxKeyPair.Key;
                    DisplayCountry country = new DisplayCountry();
                    country.favoriteUrl = favoriteUrl;
                    country.country = kvPairs.Key;
                    country.nb_visits = maxFavorite;
                    resultCountry.Add(country);
                }
                worker.ReportProgress(maxProgress, maxProgress);

                object[] objResults = { resultList, resultRegion, resultCountry };
                e.Result = objResults;
            }
        }

        private void CalculateFavorite(VisitorProfie profile, DisplayUser displayUser)
        {
            if (profile == null || displayUser == null)
                return;

            // calculate using a dictionary
            Dictionary<string, int> dictVisits = new Dictionary<string, int>();
            foreach (VisitRecord visitRecord in profile.lastVisits)
            {
                // compare timestamp must later than limit
                if (visitRecord.firstActionTimestamp > timeStampHighLimit || visitRecord.firstActionTimestamp < timeStampLowLimit)
                    continue;

                foreach (ActionDetail detail in visitRecord.actionDetails)
                {
                    // filter platforms
                    switch (_platform)
                    {
                        case Platform.IOS:
                            if (!detail.url.Contains("iOS"))
                                continue;
                            break;
                        case Platform.Mac:
                            if (!detail.url.Contains("Mac"))
                                continue;
                            break;
                        case Platform.Win:
                            if (!detail.url.Contains("Windows"))
                                continue;
                            break;
                    }

                    // parse url to favorite category
                    /*WPSX_OP operation;
                    if (detail.url.Contains("/Scan_text/"))
                        operation = WPSX_OP.Scan_text;
                    else if (detail.url.Contains("/Translate/"))
                        operation = WPSX_OP.Translate;
                    else if (detail.url.Contains("/Dictionary/"))
                        operation = WPSX_OP.Dictionary;
                    else
                        continue;*/

                    string operation = string.Empty;
                    bool found = false;
                    //foreach (string strFavorite in _favoriteList)
                    for (int i = 0; i < _favoriteList.Count; i++)
                    {
                        string enumKey = _favoriteList[i];
                        if (detail.url.Contains(enumKey))
                        {
                            operation = enumKey;
                            found = true;
                            displayUser.usageNumberArray[i]++;
                            break;
                        }
                    }
                    if (!found)
                    {
                        Console.WriteLine("skipped: " + detail.url);
                        continue;
                    }

                    // add count to dictionary
                    if (dictVisits.ContainsKey(operation))
                        dictVisits[operation]++;
                    else
                        dictVisits.Add(operation, 1);
                }
            }

            // get max favorite value
            if (dictVisits.Keys.Count > 0 && dictVisits.Values.Count > 0)
            {
                int maxValue = dictVisits.Values.Max();
                var maxKeyPair = dictVisits.FirstOrDefault(x => x.Value == maxValue);
                displayUser.nb_visits = maxValue;
                if (maxValue > _favoriteLimit)
                    displayUser.favoriteUrl = maxKeyPair.Key;
                else if (maxValue > 1)
                    displayUser.favoriteUrl = MIX;
                else
                    displayUser.favoriteUrl = NO_OP;
            }
            else
                displayUser.favoriteUrl = NO_OP;
        }

        private HttpStatusCode GetVisitorIDs(out List<MatomoUser> users)
        {
            string url;
            if (_duration == ReportDuration.range)
            {
                url = PiwikServerUrl
                    + "module=API&method=UserId.getUsers&format=json"
                    + "&period=" + _duration.ToString()
                    + "&date=" + rangeStartDateString + "," + timeLimitString
                    //+ "&segment=" + HttpUtility.UrlEncode("visitEndServerDate>" + rangeStartDateString)
                    + "&token_auth=" + Token
                    + "&idSite=" + _projectID.ToString()
                    + "&filter_limit=-1";  // Set to -1 to return all rows, otherwise only 100 rows by default.
            }
            else
            {
                url = PiwikServerUrl
                    + "module=API&method=UserId.getUsers&format=json"
                    + "&period=" + _duration.ToString()
                    + "&date=" + timeLimitString
                    //+ "&segment=" + HttpUtility.UrlEncode("visitEndServerDate>" + segmentStartDateString)
                    + "&token_auth=" + Token
                    + "&idSite=" + _projectID.ToString()
                    + "&filter_limit=-1";  // Set to -1 to return all rows, otherwise only 100 rows by default.
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = TIMEOUT_MS;

            string result = "";
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    result = sr.ReadToEnd();
                }

                // parse each user id
                try
                {
                    users = JsonConvert.DeserializeObject<List<MatomoUser>>(result);
                }
                catch (JsonSerializationException ex)
                {
                    users = new List<MatomoUser>();
                }

                return response.StatusCode;
            }
        }

        private HttpStatusCode GetVisitorProfile(string visitorId, out VisitorProfie profile)
        {
            // get visit for each visitor
            // date seeems no effect?
            profile = null;
            string url;
            if (_duration == ReportDuration.range)
            {
                url = PiwikServerUrl
                + "module=API&method=Live.getVisitorProfile&format=json&expanded=1&limitVisits=1000"
                + "&period=" + _duration.ToString()
                + "&visitorId=" + visitorId
                + "&date=" + rangeStartDateString + "," + timeLimitString
                + "&segment=" + HttpUtility.UrlEncode("visitEndServerDate>" + segmentStartDateString)
                + "&token_auth=" + Token
                + "&idSite=" + _projectID.ToString()
                + "&filter_limit=-1";  // Set to -1 to return all rows, otherwise only 100 rows by default.
            }
            else
            {
                url = PiwikServerUrl
                + "module=API&method=Live.getVisitorProfile&format=json&expanded=1&limitVisits=1000"
                + "&period=" + _duration.ToString()
                + "&visitorId=" + visitorId
                + "&date=" + timeLimitString
                + "&segment=" + HttpUtility.UrlEncode("visitEndServerDate>" + segmentStartDateString)
                + "&token_auth=" + Token
                + "&idSite=" + _projectID.ToString()
                + "&filter_limit=-1";  // Set to -1 to return all rows, otherwise only 100 rows by default.
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = TIMEOUT_MS;

            string result = "";
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    result = sr.ReadToEnd();
                }
                try
                {
                    profile = JsonConvert.DeserializeObject<VisitorProfie>(result);
                }
                catch (JsonSerializationException ex)
                {
                    profile = null;
                }
                return response.StatusCode;
            }
        }

        private void btnSwitchView_Click(object sender, RoutedEventArgs e)
        {
            if (userListView.Visibility == Visibility.Visible)
            {
                userListView.Visibility = Visibility.Collapsed;
                gridRegion.Visibility = Visibility.Visible;
            }
            else
            {
                userListView.Visibility = Visibility.Visible;
                gridRegion.Visibility = Visibility.Collapsed;
            }
        }

        private void btnSaveExcel_Click(object sender, RoutedEventArgs e)
        {
            string fileName = "Report_Proj" + _projectID + "_" + _platform.ToString() + DateTime.Now.ToString("_yyyyMMddHHmm_") + _duration.ToString() + ".xlsx";

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "MS Excel | *.xlsx";
            saveFileDialog1.Title = "儲存至...";
            saveFileDialog1.FileName = fileName;
            if (saveFileDialog1.ShowDialog() == true && !string.IsNullOrEmpty(saveFileDialog1.FileName))
            {
                //建立 xlxs 轉換物件
                ExcelHelper helper = new ExcelHelper();
                //取得轉為 xlsx 的物件
                ClosedXML.Excel.XLWorkbook xlsx = helper.Export(DisplayUserCollection.ToList(), _favoriteList);
                helper.AddExport(xlsx, DisplayCountryCollection.ToList());
                helper.AddExport(xlsx, DisplayRegionCollection.ToList());

                //存檔至指定位置
                xlsx.SaveAs(saveFileDialog1.FileName);
                MessageBox.Show(this, "儲存至：\n" + saveFileDialog1.FileName);
            }
        }

        private void UserVisitWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (worker.IsBusy)
                e.Cancel = true;
        }
    }
}
