using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Utility;
using MySql.Data.MySqlClient;

namespace Report_Viewer_2.Window
{
    /// <summary>
    /// Interaction logic for AdVerifyWindow.xaml
    /// </summary>
    public partial class AdVerifyWindow : ModernWindow
    {
        private List<VerifyLog> logs = new List<VerifyLog>();
        private readonly string connStr;
        private readonly DateTime startTime;
        private readonly DateTime timeLimit;
        private BackgroundWorker worker;

        public AdVerifyWindow(string server, int port, string name, string user, string password, ReportDuration duration, DateTime endDate, DateTime? startDate = null)
        {
            // check parameters
            if (duration == ReportDuration.range)
            {
                if (startDate == null)
                    throw new ArgumentException("neead a start date of range duration", nameof(startDate));
                if (startDate >= endDate)
                    throw new ArgumentOutOfRangeException(nameof(startDate), "start date must earlier than end date");
            }

            InitializeComponent();
            connStr = $"server={server};user={user};database={name};port={port};password={password};Connection Timeout=300";

            string viewPeriod;
            switch (duration)
            {
                case ReportDuration.day:
                    startTime = endDate;
                    timeLimit = endDate.AddDays(1);  // 一日包含今天，不含明天
                    viewPeriod = " 一日內的檢視";
                    lbConditions.Content = startTime.ToString("yyyy MMMM dd") + viewPeriod;
                    break;
                case ReportDuration.week:
                    DayOfWeek dayOfWeek = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(endDate);
                    startTime = endDate.AddDays(-(int)dayOfWeek);
                    timeLimit = endDate.AddDays(7 - (int)dayOfWeek);
                    viewPeriod = " 起一周間的檢視";
                    lbConditions.Content = startTime.ToString("yyyy MMMM dd") + viewPeriod;
                    break;
                case ReportDuration.month:
                    startTime = new DateTime(endDate.Year, endDate.Month, 1);
                    int daysThisMonth = DateTime.DaysInMonth(endDate.Year, endDate.Month);
                    timeLimit = startTime.AddDays(daysThisMonth);
                    viewPeriod = " 起一月間的檢視";
                    lbConditions.Content = startTime.ToString("yyyy MMMM dd") + viewPeriod;
                    break;
                case ReportDuration.year:
                    startTime = new DateTime(endDate.Year, 1, 1);
                    timeLimit = new DateTime(endDate.Year + 1, 1, 1).AddDays(-1);
                    viewPeriod = " 起一年間的檢視";
                    lbConditions.Content = startTime.ToString("yyyy MMMM dd") + viewPeriod;
                    break;
                case ReportDuration.range:
                    startTime = startDate.Value;
                    timeLimit = endDate;
                    lbConditions.Content = " 自訂檢視 " + startTime + " ~ " + timeLimit;
                    break;
            }
        }

        private void AdVerifyWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (worker.IsBusy)
                e.Cancel = true;
        }

        private void AdVerifyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = false;
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerAsync();
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            logOutput.Text += e.UserState.ToString();
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (logs != null && logs.Count > 0)
            {
                logOutput.Visibility = Visibility.Hidden;
                logListView.Visibility = Visibility.Visible;
                logListView.ItemsSource = logs;
                btnSaveExcel.IsEnabled = true;
            }
            else
                logOutput.Text += "期間內找不到任何資料。";
            progressBar.Visibility = Visibility.Collapsed;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                worker.ReportProgress(0, "Connecting to MySQL...\n");
                conn.Open();

                worker.ReportProgress(0, "Reading tables...\n");
                // Perform database operations
                HashSet<string> tableSet = new HashSet<string>();
                using (MySqlCommand cmd = new MySqlCommand("show tables;", conn))
                {
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                        tableSet.Add(reader[0].ToString());
                    reader.Close();
                    reader.Dispose();
                }

                List<DateTime> unProcessedDates = new List<DateTime>();
                string sql = "CREATE TEMPORARY TABLE IF NOT EXISTS tableLogs as select * from ad_archive_2019_01";
                DateTime startDate = new DateTime(2019, 1, 1);
                DateTime thisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                DateTime endDate = thisMonth.AddDays(-1);
                for (DateTime dt = startDate; dt <= endDate; dt = dt.AddMonths(1))
                {
                    string tableName = $@"ad_archive_{dt.Year}_{dt.Month.ToString("D2")}";
                    sql += $@" union select * from {tableName}";
                    if (!tableSet.Contains(tableName))
                        unProcessedDates.Add(dt);
                }

                if (timeLimit > thisMonth)
                {
                    worker.ReportProgress(0, "Generating this month's log... (this may take some time)\n");
                    // 本月的報告還沒彙整，先做成臨時表格
                    string thisMonthTempTable = $"temp_{thisMonth.Year}_{thisMonth.Month.ToString("D2")}";
                    string sqlTemp = GenerateTempTableSQL(thisMonth, thisMonthTempTable);
                    using (MySqlCommand cmd = new MySqlCommand(sqlTemp, conn))
                        cmd.ExecuteNonQuery();

                    // 記得加上本月臨時表格
                    sql += " union select * from " + thisMonthTempTable;
                    System.Threading.Thread.Sleep(1000);
                }

                // 如果有剩下沒產生的月份，產生永久表格
                foreach (DateTime d in unProcessedDates)
                {
                    worker.ReportProgress(0, $"Generating log for {d}...\n");
                    string sql2 = GenerateCreateTableSQL(d);
                    using (MySqlCommand cmd = new MySqlCommand(sql2, conn))
                        cmd.ExecuteNonQuery();
                    System.Threading.Thread.Sleep(1000);
                }

                worker.ReportProgress(0, "Generating temporary tables...\n");
                // 讀取全部的紀錄，產生 tableLogs 臨時表格
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    cmd.ExecuteNonQuery();

                // 點擊廣告的訪客 tableAd
                string sqlAdTable = @"
                    CREATE TEMPORARY TABLE IF NOT EXISTS tableAd AS
                    (select * from tableLogs
                    where idaction != 15289);";
                using (MySqlCommand cmd1 = new MySqlCommand(sqlAdTable, conn))
                    cmd1.ExecuteNonQuery();

                // 實際註冊的訪客 tableRegistered
                string sqlRegisteredTable = @"
                    CREATE TEMPORARY TABLE IF NOT EXISTS tableRegistered AS
                    (select * from tableLogs
                    where idaction = 15289 and ip != '218.161.53.0' and ip != '10.10.12.0' and ip != '10.10.8.0');";
                using (MySqlCommand cmd2 = new MySqlCommand(sqlRegisteredTable, conn))
                    cmd2.ExecuteNonQuery();

                worker.ReportProgress(0, "Finding same ip...\n");
                // 尋找相同的 ip，僅限同一天內的
                string finalQuery = @"
                    select tableAd.idsite, tableRegistered.idvisit, tableRegistered.ip, tableRegistered.country, tableAd.actionName, tableRegistered.server_time as registedTime, tableAd.server_time as adClickTime
                    from tableRegistered
                    inner join tableAd
                    on tableRegistered.ip = tableAd.ip
                    where DATE(tableRegistered.server_time) = DATE(tableAd.server_time)
                    ORDER BY registedTime";

                using (MySqlCommand cmd3 = new MySqlCommand(finalQuery, conn))
                {
                    var reader = cmd3.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader.FieldCount >= 7)
                        {
                            VerifyLog verifyLog = new VerifyLog();
                            verifyLog.idsite = (uint)reader[0];
                            verifyLog.idvisit = (ulong)reader[1];
                            verifyLog.ip = reader[2].ToString();
                            verifyLog.country = reader[3].ToString();
                            verifyLog.actionName = reader[4].ToString();
                            verifyLog.registeredTime = (DateTime)reader[5];
                            verifyLog.adClickTime = (DateTime)reader[6];
                            // 只留最早一組註冊的 idvisit
                            var existed = logs.FirstOrDefault(x => x.idvisit == verifyLog.idvisit);
                            if (existed == null)
                                logs.Add(verifyLog);
                            else
                            {
                                if (existed.adClickTime > verifyLog.adClickTime)
                                {
                                    existed.adClickTime = verifyLog.adClickTime;
                                    existed.actionName = verifyLog.actionName;
                                }
                            }
                        }
                    }
                    reader.Close();
                    reader.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                worker.ReportProgress(0, ex.ToString());
            }
            conn.Close();
            conn.Dispose();

            for (int i = logs.Count - 1; i >= 0; i--)
            {
                if (logs[i].registeredTime > timeLimit || logs[i].registeredTime < startTime)
                    logs.RemoveAt(i);
            }

            worker.ReportProgress(0, "Done.\n");
        }

        private string GenerateCreateTableSQL(DateTime givenMonth)
        {
            DateTime end = givenMonth.AddMonths(1);
            string sql = $@"
create table ad_archive_{givenMonth.Year}_{givenMonth.Month.ToString("D2")} as
 select idsite, matomo_log_visit.idvisit, INET_NTOA(CONV(HEX(location_ip), 16, 10)) AS ip, matomo_log_visit.location_country as country, tableVisitID.name as actionName, idaction, tableVisitID.server_time
 from matomo_log_visit
 inner join
 (select idvisit, idaction_url, server_time, tableActionID.name, tableActionID.idaction
 from matomo_log_link_visit_action
 inner join 
 (select matomo_log_action.idaction, matomo_log_action.name
 from matomo_log_action
 where matomo_log_action.name LIKE '%/Ad/%' or matomo_log_action.idaction = 15289) as tableActionID
 on tableActionID.idaction = matomo_log_link_visit_action.idaction_url
 where server_time >= '{givenMonth.Year}-{givenMonth.Month.ToString("D2")}-01 00:00:00' and server_time < '{end.Year}-{end.Month.ToString("D2")}-01 00:00:00') as tableVisitID
 on matomo_log_visit.idvisit = tableVisitID.idvisit";
            return sql;
        }

        private string GenerateTempTableSQL(DateTime givenMonth, string tempTableName)
        {
            DateTime end = givenMonth.AddMonths(1);
            string sql = $@"
CREATE TEMPORARY TABLE IF NOT EXISTS {tempTableName} as
 select idsite, matomo_log_visit.idvisit, INET_NTOA(CONV(HEX(location_ip), 16, 10)) AS ip, matomo_log_visit.location_country as country, tableVisitID.name as actionName, idaction, tableVisitID.server_time
 from matomo_log_visit
 inner join
 (select idvisit, idaction_url, server_time, tableActionID.name, tableActionID.idaction
 from matomo_log_link_visit_action
 inner join 
 (select matomo_log_action.idaction, matomo_log_action.name
 from matomo_log_action
 where matomo_log_action.name LIKE '%/Ad/%' or matomo_log_action.idaction = 15289) as tableActionID
 on tableActionID.idaction = matomo_log_link_visit_action.idaction_url
 where server_time >= '{givenMonth.Year}-{givenMonth.Month.ToString("D2")}-01 00:00:00' and server_time < '{end.Year}-{end.Month.ToString("D2")}-01 00:00:00') as tableVisitID
 on matomo_log_visit.idvisit = tableVisitID.idvisit";
            return sql;
        }

        private void btnSaveExcel_Click(object sender, RoutedEventArgs e)
        {
            string fileName = "Ad Verify " + DateTime.Now.ToString("yyyyMMddHHmm") + ".xlsx";

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "MS Excel | *.xlsx";
            saveFileDialog1.Title = "儲存至...";
            saveFileDialog1.FileName = fileName;
            if (saveFileDialog1.ShowDialog() == true && !string.IsNullOrEmpty(saveFileDialog1.FileName))
            {
                //建立 xlxs 轉換物件
                ExcelHelper helper = new ExcelHelper();

                //取得轉為 xlsx 的物件
                List<string> headers = new List<string> { "Site", "SiteID", "VisitID", "IP", "國家代碼", "當日首個廣告行為", "註冊完成的時間", "首次點廣告時間" };
                ClosedXML.Excel.XLWorkbook xlsx = helper.Export(logs.ToList(), headers);

                //存檔至指定位置
                xlsx.SaveAs(saveFileDialog1.FileName);
                MessageBox.Show(this, "儲存至：\n" + saveFileDialog1.FileName);
            }
        }
    }

    class VerifyLog
    {
        public SiteName Site => (SiteName)idsite;
        public uint idsite { get; set; }
        public ulong idvisit { get; set; }
        public string ip { get; set; }
        public string country { get; set; }
        public string actionName { get; set; }
        public DateTime registeredTime { get; set; }
        public DateTime adClickTime { get; set; }
    }

    enum SiteName
    {
        WPSX = 1,
        WCT = 2,
        WDUSB = 4,
        W2G = 5,
        WC8 = 6,
        WCSF = 7,
        O365 = 8
    }
}
