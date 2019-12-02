using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using Microsoft.Win32;
using Utility;
using System.Web;
using Newtonsoft.Json;

namespace Report_Viewer_2.Window
{
    /// <summary>
    /// Interaction logic for ErrorIdWindow.xaml
    /// </summary>
    public partial class ErrorIdWindow : ModernWindow
    {
        private const string PiwikServerUrl = @"http://10.10.15.62/index.php?";
        private const string Token = "4791a3edddeb2f8e2161b7f51da27ed4";
        private const int TIMEOUT_MS = 5000;
        //private const string NO_REGION = "無有效地區";
        private string timeLimitString;
        private string visitStartDateString;
        private string visitEndDateString;
        private string rangeStartDateString;
        private DateTime timeLimit;
        private DateTime firstDayOfWeek;
        private DateTime rangeStartDate;
        private long timeStampLowLimit;
        private long timeStampHighLimit;
        private ReportDuration _duration;
        private int _projectID = 6;
        private RemoteManager rManager;
        private string _userId;
        private BackgroundWorker worker;
        private List<DisplayUserAction> displayActions;
        private string lastCountry;
        private string lastOS;
        private string lastLanguage;
        private string lastResolution;

        public ErrorIdWindow(string userId, ReportDuration duration, DateTime endDate, DateTime? startDate = null)
        {
            _duration = duration;
            // check parameters
            if (_duration == ReportDuration.range)
            {
                if (startDate == null)
                    throw new ArgumentException("neead a start date of range duration", nameof(startDate));
                if (startDate >= endDate)
                    throw new ArgumentOutOfRangeException(nameof(startDate), "start date must earlier than end date");
                rangeStartDate = startDate.Value;
            }

            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));
            _userId = userId;
            _duration = duration;

            InitializeComponent();

            // disable ssl warning
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            timeLimit = endDate;
            timeLimitString = timeLimit.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            switch (_duration)
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
                        lbConditions.Content = "WorldCard 8 使用者 " + _userId + " 於 " + timeLimitString + viewPeriod;
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
                        lbConditions.Content = "WorldCard 8 使用者 " + _userId + " 於 " + displayDate + " 至 " + displayDate2 + viewPeriod 
                                               + " (Matomo 計算每周從周一開始)";
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
                        lbConditions.Content = "WorldCard 8 使用者 " + _userId + " 於 " + visitStartDateString + viewPeriod;
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
                        lbConditions.Content = "WorldCard 8 使用者 " + _userId + " 於 " + visitStartDateString + viewPeriod;
                        break;
                    }
                case ReportDuration.range:
                    {
                        firstDayOfWeek = rangeStartDate.AddDays(-1);
                        visitStartDateString = firstDayOfWeek.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        rangeStartDateString = rangeStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        timeStampLowLimit = ((DateTimeOffset)rangeStartDate).ToUnixTimeSeconds();
                        timeStampHighLimit = ((DateTimeOffset)timeLimit.AddDays(1)).ToUnixTimeSeconds();
                        lbConditions.Content = "WorldCard 8 使用者 " + _userId + " 於自訂檢視 " + rangeStartDateString + " ~ " + timeLimitString;
                        visitEndDateString = timeLimitString;
                    }
                    break;
            }
        }

        private void ErrorIdWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;

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
            progressBar.Visibility = Visibility.Collapsed;

            if (!e.Cancelled && e.Result != null)
            {
                if (displayActions.Count > 0)
                {
                    userListView.ItemsSource = displayActions;
                    userListView.Visibility = Visibility.Visible;
                    btnSaveExcel.Visibility = Visibility.Visible;
                    logOutput.Visibility = Visibility.Collapsed;
                    userText.Text = "上次來自國家：" + lastCountry + "  作業系統：" + lastOS + "  " + lastResolution +  "  " + lastLanguage;
                }
                else
                    logOutput.Text += "此次搜尋找不到任何資料。\n";
            }
            else
                logOutput.Text += "Get visitor log failed!\n";
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            logOutput.Text += e.UserState.ToString() + "\n";
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            worker.ReportProgress(0, "Raising display limit...");
            rManager = new RemoteManager();
            if (rManager.InitWithKey("10.10.15.62"))
                rManager.RaiseActionDisplayLimit(1500);
            else
                worker.ReportProgress(0, "Could not login to remote server.");

            worker.ReportProgress(0, "Getting visitor name: " + _userId);
            displayActions = new List<DisplayUserAction>();
            if (GetVisitorProfile(_userId, out VisitorProfie profile) == HttpStatusCode.OK)
            {
                if (profile == null)
                {
                    worker.ReportProgress(0, "Visitor profile is null!");
                    e.Cancel = true;
                    return;
                }
                worker.ReportProgress(0, "Parsing visit logs...");

                // remove redundant information
                string match = "http://wcretail/windows/";
                foreach (VisitRecord visitRecord in profile.lastVisits)
                {
                    foreach (ActionDetail detail in visitRecord.actionDetails)
                    {
                        // compare timestamp must later than limit
                        if (visitRecord.firstActionTimestamp > timeStampHighLimit || visitRecord.firstActionTimestamp < timeStampLowLimit)
                            continue;

                        DisplayUserAction action = new DisplayUserAction();
                        string url;
                        // remove http...
                        if (detail.url.StartsWith(match, StringComparison.OrdinalIgnoreCase))
                            url = detail.url.Substring(match.Length, detail.url.Length - match.Length);
                        else
                            url = detail.url;

                        // version
                        if (url.StartsWith("v"))
                        {
                            action.Version = ParseFirstArgument(ref url);
                        }

                        // add card event
                        if (detail.type == "event")
                        {
                            url = $"{detail.eventCategory} - {detail.eventAction} ({detail.eventValue})";
                            action.Module = "Event";
                        }
                        else
                        {   // not event, should be error log or normal action
                            // module of normal action
                            action.Module = ParseFirstArgument(ref url);

                            // error log
                            if (url.Contains("LL_SERIOUS_ERROR"))
                            {
                                action.IsError = true;
                                // module of error
                                action.Module = ParseFirstArgument(ref url);
                            }
                        }

                        action.url = url;
                        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(detail.timestamp);
                        action.time = dateTimeOffset.UtcDateTime;
                        displayActions.Add(action);
                    }

                    // last user info
                    if (visitRecord.firstActionTimestamp <= timeStampHighLimit && visitRecord.firstActionTimestamp >= timeStampLowLimit)
                    {
                        lastCountry = visitRecord.country;
                        lastLanguage = visitRecord.language;
                        lastOS = visitRecord.operatingSystem;
                        lastResolution = visitRecord.resolution;
                    }
                }

                displayActions = displayActions.OrderBy(x => x.time).ToList();
            }

            if (string.IsNullOrEmpty(lastCountry))
                lastCountry = profile.countries.FirstOrDefault()?.prettyName ?? "未知";

            e.Result = displayActions;
        }

        private string GenerateSegment(string visitorId)
        {
            return "&segment="
                   + HttpUtility.UrlEncode("userId==" + visitorId);
            //+ ";"
            //+ HttpUtility.UrlEncode("visitEndServerDate>" + visitStartDateString) 
            //+ ";"
            //+ HttpUtility.UrlEncode("visitEndServerDate<=" + visitEndDateString);
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
                //+ "&userId=" + visitorId
                + "&date=" + rangeStartDateString + "," + timeLimitString
                + GenerateSegment(visitorId)
                + "&token_auth=" + Token
                + "&idSite=" + _projectID.ToString()
                + "&filter_limit=-1";  // Set to -1 to return all rows, otherwise only 100 rows by default.
            }
            else
            {
                url = PiwikServerUrl
                + "module=API&method=Live.getVisitorProfile&format=json&expanded=1&limitVisits=1000"
                + "&period=" + _duration.ToString()
                //+ "&userId=" + visitorId
                //+ "&segment=" + HttpUtility.UrlEncode("visitEndServerDate>" + visitStartDateString) + ";" + HttpUtility.UrlEncode("visitEndServerDate<=" + visitEndDateString)
                + "&date=" + timeLimitString
                + GenerateSegment(visitorId)
                + "&token_auth=" + Token
                + "&idSite=" + _projectID.ToString()
                + "&filter_limit=-1";  // Set to -1 to return all rows, otherwise only 100 rows by default.
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = TIMEOUT_MS;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                string result = "";
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

        private void btnSaveExcel_Click(object sender, RoutedEventArgs e)
        {
            string fileName = "WC8 User Action " + DateTime.Now.ToString("yyyyMMddHHmm_") + _duration.ToString() + ".xlsx";

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "MS Excel | *.xlsx";
            saveFileDialog1.Title = "儲存至...";
            saveFileDialog1.FileName = fileName;
            if (saveFileDialog1.ShowDialog() == true && !string.IsNullOrEmpty(saveFileDialog1.FileName))
            {
                //建立 xlxs 轉換物件
                ExcelHelper helper = new ExcelHelper();
                //取得轉為 xlsx 的物件
                ClosedXML.Excel.XLWorkbook xlsx = helper.Export(displayActions.ToList());

                //存檔至指定位置
                xlsx.SaveAs(saveFileDialog1.FileName);
                ModernDialog.ShowMessage("儲存至：\n" + saveFileDialog1.FileName, "結果", MessageBoxButton.OK);
            }
        }

        private void ErrorIdWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (worker.IsBusy)
                e.Cancel = true;
        }

        private string ParseFirstArgument(ref string input)
        {
            int index = input.IndexOf('/');
            if (index > 0)
            {
                string match = input.Remove(index);
                input = input.Substring(match.Length + 1, input.Length - match.Length - 1);
                return match;
            }

            // not found
            return null;
        }
    }
}
