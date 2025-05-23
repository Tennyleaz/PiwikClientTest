﻿using Piwik.Tracker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using Utility;
using WPSX.Tracker;

namespace PiwikClientTest
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        /// 桌上型電腦的品牌UA：
        /// https://www.whatismybrowser.com/developers/guides/unknown-user-agent-fragments
        /// 
        private string UA;// = "Mozilla/5.0 (Windows NT 10.0; WOW64; en-US;)"; //"Mozilla/5.0 (Windows NT 10.0; WOW64; rv:55.0)";
        private static readonly string LocalUrl = "http://10.10.15.65";
        private static readonly string GCPUrl = "https://35.233.143.71";
        private static readonly string RealUrl = "http://matomo.penpower.net";
        private static int SiteId = 1;  //Piwik控制台裡面設定的site id號碼，對應不同產品

        //private BackgroundWorker worker;
        private string PiwikBaseUrl;
        private string workerException;
        private string uID;
        private string titleString;
        private string versionNumber;
        private string responceString;
        private string localeName;
        private string searchKey;
        private int width, height;

        private WPSXTracker wpsxTracker;

        /*enum PPPiwikClient.RecordType
        {
            Use,
            Use_Daily,
            Manual,
            Facebook,
            LinkedIn,
            SinaWeibo,
            Twitter,
            ShowMap,
            Route,
            SkypeOut,
            SkypeSMS,
            ImageView,
            Recovery,
            Print,
            SendMail,
            OpenWebSite,
            EditCard,
            FindDuplicate,
            FindTheSameName,
            SetCategory,
            AddCardCount,
            GoogleSync,
            NasSync,
            ActSync,
            OutlookSync,
            LotusNoteSync,
            SalesforceSync,
            DBankImport,
            OutlookImport,
            ActImport,
            LotusNoteImport,
            SaleforceImport,
            WcxfImport,
            CsvImport,
            VcardImport,
            JpegImport,
            DropboxImport,
            WcfImport,
            WorldCardv8DBImport,
            DBankExport,
            OutlookExport,
            ActExport,
            LotusNotesExport,
            SaleforceExport,
            SaleforceLeadExport,
            WcxfExport,
            ExcelExport,
            CsvExport,
            VcardExport,
            JpegExport,
            DropboxExport,
            TxtExport,
            Search
        }*/

        public MainWindow()
        {
            //UA = "Mozilla/5.0 (Windows NT 10.0; WOW64; en-US;)";
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // locale of user environment
            CultureInfo ci = CultureInfo.CurrentCulture;
            localeName = ci.Name;
            // basic windows version
            int osMajor = Environment.OSVersion.Version.Major;
            int osMinor = Environment.OSVersion.Version.Minor;            
            UA = "Mozilla/5.0 (Windows NT " + osMajor + "." + osMinor + "; WOW64)";
            lbOS.Content = "Windows NT " + osMajor + "." + osMinor + ", " + localeName;
            // default app is worldcardteam
            cbAppName.SelectedIndex = 1;
            // default screen resuolsion
            tbWidth.Text = SystemParameters.PrimaryScreenWidth.ToString();
            tbHeight.Text = SystemParameters.PrimaryScreenHeight.ToString();

            PiwikBaseUrl = GCPUrl;
            //this.Title = "Test app (" + PiwikBaseUrl + ")";

            datePicker.SelectedDate = DateTime.Today;
            rangeStartDatePicker.SelectedDate = DateTime.Today.AddDays(-1);
          
            //wpsxTracker.SendRecordCompleted += WpsxTracker_SendRecordCompleted;
            /*foreach (string lang in Enum.GetNames(typeof(Languages)))
            {
                cbSourceLanguage.Items.Add(lang);
                cbDestLanguage.Items.Add(lang);
            }
            cbSourceLanguage.SelectedIndex = 0;
            cbDestLanguage.SelectedIndex = 1;*/
        }

        /*private void WpsxTracker_SendRecordCompleted(WPSX.Tracker.SendRecordCompleteEventArgs e)
        {
            if (e.ExceptionMessage != null)
            {
                MessageBox.Show(this, e.ExceptionMessage);
                lbResult.Content = e.ExceptionMessage;
            }
            else
            {
                //MessageBox.Show(this, e.SendResult);
                lbResult.Content = e.SendResult;
            }
            progressBar.Visibility = Visibility.Hidden;
            tabs.IsEnabled = true;
            tbSearchKey.Text = "";
        }*/

        private void btnFakeDevice_Click(object sender, RoutedEventArgs e)
        {
            User_agent_window uaw = new User_agent_window();
            uaw.Owner = this;
            uaw.ShowDialog();
            
            if (!string.IsNullOrEmpty(uaw.Culture_String))
                localeName = uaw.Culture_String;
            if (!string.IsNullOrEmpty(uaw.UA_String))
            {
                UA = uaw.UA_String;
                lbOS.Content = uaw.UA_Display_String + ", " + localeName;
            }
            else
            {
                int osMajor = Environment.OSVersion.Version.Major;
                int osMinor = Environment.OSVersion.Version.Minor;
                lbOS.Content = "Windows NT " + osMajor + "." + osMinor + ", " + localeName;
            }
        }

        #region Background Workers
        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!string.IsNullOrEmpty(workerException))
            {
                MessageBox.Show(this, workerException);
                lbResult.Content = workerException;
            }
            else
                lbResult.Content = responceString;
            progressBar.Visibility = Visibility.Hidden;
            tabs.IsEnabled = true;
            tbSearchKey.Text = "";
            // show time spent in console
            sw.Stop();
            float timeperloop = (float)sw.Elapsed.Seconds / (float)(totalloops * threads);
            string s = "total " + sw.Elapsed.Seconds + " seconds for " + totalloops + "*" + threads + " requests,  average " + timeperloop + " seconds per request";            
            sw.Reset();
            TimeSpan delta = DateTime.Now.Subtract(lastDT);
            s = "timespan is " + delta.TotalSeconds;
            Console.WriteLine(s);
            timeperloop = (float)delta.TotalSeconds / (float)(totalloops * threads);
            s = "total " + delta.TotalSeconds + " seconds for " + totalloops + "*" + threads + " requests,  average " + timeperloop + " seconds per request";
            Console.WriteLine(s);
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            worker.ReportProgress(1);
            object obj = e.Argument;
            string url = obj as string;
            if (!string.IsNullOrEmpty(url))
                RecordSimplePageViewWithCustomProperties(url);
        }

        private void Bkworker_DoWorkSearch(object sender, DoWorkEventArgs e)
        {
            object[] argumenrts = e.Argument as object[];
            if (argumenrts.Length != 2)
                return;
            string url = argumenrts[0] as string;
            string key = argumenrts[1] as string;
            if (!string.IsNullOrEmpty(url))
                RecordSimplePageViewWithCustomProperties(url, key);
        }
        #endregion

        private void cbServer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbServer.SelectedIndex == 0)
            {
                PiwikBaseUrl = GCPUrl;
                this.Title = "Test app (" + PiwikBaseUrl + ")";

                wpsxTracker = new WPSXTracker(GCPUrl, "v5.2.0", "Tenny", "zh-TW", 1);
            }
            else if (cbServer.SelectedIndex == 1)
            {
                PiwikBaseUrl = LocalUrl;
                this.Title = "Test app (" + PiwikBaseUrl + ")";

                wpsxTracker = new WPSXTracker(LocalUrl, "v5.2.0", "Tenny", "zh-TW", 2);
            }
            else
            {
                PiwikBaseUrl = RealUrl;
                this.Title = "Test app (" + PiwikBaseUrl + ")";

                wpsxTracker = new WPSXTracker(RealUrl, "v5.2.0", "Tenny", "zh-TW", 1);
            }
        }

        private void cbAppName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbAppName.SelectedIndex == 0)  //WCT
            {
                btnNasSync.IsEnabled = true;
                btnGmailSync.IsEnabled = false;
                btnActSync.IsEnabled = false;
                btnLotusNoteSync.IsEnabled = false;
                btnSalesforceSync.IsEnabled = false;
                btnRecovery.IsEnabled = false;
                btnWC8Import.IsEnabled = true;
                btnDBankExport.IsEnabled = false;
                btnDbankImport.IsEnabled = false;
                btnLeadExport.IsEnabled = false;
                SiteId = 1;
                tbVersionNumber.Text = "v1.0.0";
            }
            else if (cbAppName.SelectedIndex == 1)//WC8
            {
                btnNasSync.IsEnabled = false;
                btnGmailSync.IsEnabled = true;
                btnActSync.IsEnabled = true;
                btnLotusNoteSync.IsEnabled = true;
                btnSalesforceSync.IsEnabled = true;
                btnRecovery.IsEnabled = true;
                btnWC8Import.IsEnabled = false;
                btnDBankExport.IsEnabled = true;
                btnDbankImport.IsEnabled = true;
                btnLeadExport.IsEnabled = true;
                SiteId = 2;
                tbVersionNumber.Text = "v5.2.0";
            }
        }

        private void tbWidth_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = IsTextAllowed(e.Text);
        }

        private bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9]"); //regex that matches disallowed text
            return regex.IsMatch(text);
        }

        #region Prepare URL data
        /// <summary>
        /// 送出一筆url紀錄，結果記錄到responceString和workerException
        /// </summary>
        /// <param name="url"></param>
        /// <param name="key"></param>
        private void RecordSimplePageViewWithCustomProperties(string url, string key = null)
        {
            workerException = null;

            var piwikTracker = new PiwikTracker(SiteId, PiwikBaseUrl, true);
            piwikTracker.SetUserAgent(UA);
            piwikTracker.SetBrowserLanguage(localeName);
            piwikTracker.SetResolution(width, height);

            //piwikTracker.SetIp("218.161.53.45");
            if (!string.IsNullOrWhiteSpace(uID))
                piwikTracker.SetUserId(uID);

            //piwikTracker.SetForceVisitDateTime(new DateTime(2017, 9, 22, 10, 20, 50, DateTimeKind.Utc));

            //piwikTracker.SetResolution(1600, 1400);

            //piwikTracker.SetTokenAuth("XYZ");

            /*var browserPluginsToSet = new BrowserPlugins();
            browserPluginsToSet.Silverlight = true;
            browserPluginsToSet.Flash = true;
            piwikTracker.SetPlugins(browserPluginsToSet);
            piwikTracker.SetBrowserHasCookies(true);*/

            //piwikTracker.SetLocalTime(new DateTime(2000, 1, 1, 9, 10, 25, DateTimeKind.Utc));

            //piwikTracker.SetUrl("http://piwik-1.5/supernova");
            //piwikTracker.SetUrlReferrer("http://supernovadirectory.org");
            piwikTracker.SetUrl(url);
            //piwikTracker.SetCustomTrackingParameter("dimension2", versionNumber);
            
            //piwikTracker.SetBrowserLanguage("en-US");
            piwikTracker.SetGenerationTime(1000);

            if (!string.IsNullOrEmpty(key))
            {
                piwikTracker.DoTrackSiteSearch(key);
            }

            try
            {
                var response = piwikTracker.DoTrackPageView(titleString);
                responceString = response.HttpStatusCode.ToString();                
                //DisplayDebugInfo(response);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                workerException = e.Message;
                //MessageBox.Show(e.Message);
            }
        }

        private int lastSeletedIndex = 0;
        private string GenerateURL(PPPiwikClient.RecordType recordType, bool isForeground = true)
        {
            string url = "http://";
            if (isForeground)
            {
                tabs.IsEnabled = false;
                progressBar.Visibility = Visibility.Visible;

                uID = tbUserID.Text;
                titleString = tbTitle.Text;
                versionNumber = tbVersionNumber.Text;
                lbResult.Content = "";

                int.TryParse(tbWidth.Text, out width);
                int.TryParse(tbHeight.Text, out height);

                //string url = "http://";
                if (cbAppName.SelectedIndex == 0) //WCT
                {
                    url += "WorldCardTeam/";
                    if (string.IsNullOrWhiteSpace(versionNumber) || !versionNumber.StartsWith("v"))
                        versionNumber = "v1.0.0";
                    lastSeletedIndex = 0;
                }
                else  //WC8
                {
                    url += "WorldCard8/";
                    if (string.IsNullOrWhiteSpace(versionNumber) || !versionNumber.StartsWith("v"))
                        versionNumber = "v8.5.6";
                    lastSeletedIndex = 1;
                }
            }
            else
            {
                if (lastSeletedIndex == 0) //WCT
                {
                    url += "WorldCardTeam/";
                    if (string.IsNullOrWhiteSpace(versionNumber) || !versionNumber.StartsWith("v"))
                        versionNumber = "v1.0.0";
                    lastSeletedIndex = 0;
                }
                else  //WC8
                {
                    url += "WorldCard8/";
                    if (string.IsNullOrWhiteSpace(versionNumber) || !versionNumber.StartsWith("v"))
                        versionNumber = "v8.5.6";
                    lastSeletedIndex = 1;
                }
            }

            url += versionNumber;

            switch (recordType)
            {
                case PPPiwikClient.RecordType.Use:
                case PPPiwikClient.RecordType.Use_Daily:
                case PPPiwikClient.RecordType.Manual:                
                case PPPiwikClient.RecordType.ShowMap:
                case PPPiwikClient.RecordType.Route:
                case PPPiwikClient.RecordType.SkypeOut:
                case PPPiwikClient.RecordType.SkypeSMS:
                case PPPiwikClient.RecordType.ImageView:
                case PPPiwikClient.RecordType.Recovery:
                case PPPiwikClient.RecordType.Print:
                case PPPiwikClient.RecordType.SendMail:
                case PPPiwikClient.RecordType.OpenWebSite:
                case PPPiwikClient.RecordType.EditCard:
                case PPPiwikClient.RecordType.FindDuplicate:
                case PPPiwikClient.RecordType.FindTheSameName:
                case PPPiwikClient.RecordType.SetCategory:
                case PPPiwikClient.RecordType.AddCardCount:
                case PPPiwikClient.RecordType.Search:
                    url += "/General/" + recordType.ToString();
                    break;
                case PPPiwikClient.RecordType.Facebook:
                case PPPiwikClient.RecordType.LinkedIn:
                case PPPiwikClient.RecordType.SinaWeibo:
                case PPPiwikClient.RecordType.Twitter:
                    url += "/SocialNetwork/" + recordType.ToString();
                    break;
                case PPPiwikClient.RecordType.GoogleSync:
                case PPPiwikClient.RecordType.NasSync:
                case PPPiwikClient.RecordType.ActSync:
                case PPPiwikClient.RecordType.OutlookSync:
                case PPPiwikClient.RecordType.LotusNoteSync:
                case PPPiwikClient.RecordType.SalesforceSync:
                    url += "/Sync/" + recordType.ToString();
                    break;
                case PPPiwikClient.RecordType.DBankImport:
                case PPPiwikClient.RecordType.OutlookImport:
                case PPPiwikClient.RecordType.ActImport:
                case PPPiwikClient.RecordType.LotusNoteImport:
                case PPPiwikClient.RecordType.SaleforceImport:
                case PPPiwikClient.RecordType.WcxfImport:
                case PPPiwikClient.RecordType.CsvImport:
                case PPPiwikClient.RecordType.VcardImport:
                case PPPiwikClient.RecordType.JpegImport:
                case PPPiwikClient.RecordType.DropboxImport:
                case PPPiwikClient.RecordType.WcfImport:
                case PPPiwikClient.RecordType.WorldCardv8DBImport:
                    url += "/Import/" + recordType.ToString();
                    break;
                case PPPiwikClient.RecordType.DBankExport:
                case PPPiwikClient.RecordType.OutlookExport:
                case PPPiwikClient.RecordType.ActExport:
                case PPPiwikClient.RecordType.LotusNotesExport:
                case PPPiwikClient.RecordType.SaleforceExport:
                case PPPiwikClient.RecordType.SaleforceLeadExport:
                case PPPiwikClient.RecordType.WcxfExport:
                case PPPiwikClient.RecordType.ExcelExport:
                case PPPiwikClient.RecordType.CsvExport:
                case PPPiwikClient.RecordType.VcardExport:
                case PPPiwikClient.RecordType.JpegExport:
                case PPPiwikClient.RecordType.DropboxExport:
                case PPPiwikClient.RecordType.TxtExport:
                    url += "/Export/" + recordType.ToString();
                    break;
                default:
                    url += "/Undefined/" + recordType.ToString();
                    break;
            }
            //if (string.IsNullOrEmpty(uID))
            //    uID = Guid.NewGuid().ToString();
            return url;
        }
        #endregion

        /*private void ButtonFunction(PPPiwikClient.RecordType recordType)
        {
            GenerateURL(recordType);
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            if (!pc.SendRecord(recordType, null, "en-US"))
                MessageBox.Show("busy...");
        }*/

        private void MySendCompleted(object sender, Piwik.Tracker.SendRecordCompleteEventArgs e)
        {
            if (e.ExceptionMessage != null)
            {
                MessageBox.Show(this, e.ExceptionMessage);
                lbResult.Content = e.ExceptionMessage;
            }
            else
            {
                //MessageBox.Show(this, e.SendResult);
                lbResult.Content = e.SendResult;
            }
            progressBar.Visibility = Visibility.Hidden;
            tabs.IsEnabled = true;
            tbSearchKey.Text = "";            
        }

        #region General Usage Buttons
        private void btnUse_Click(object sender, RoutedEventArgs e)
        {
            //ButtonFunction(PPPiwikClient.RecordType.Use);
            GenerateURL(PPPiwikClient.RecordType.Use);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);            
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.Use, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnEvent_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.Use);
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            //if (!string.IsNullOrEmpty(tbDimentionValue.Text) && !string.IsNullOrEmpty(tbDimentionName.Text))
            //{
            //    int dimentionIndex = 1;
            //    int.TryParse(tbDimentionName.Text, out dimentionIndex);
            //    pc.SetDimention(dimentionIndex, tbDimentionValue.Text);
            //}
            if (!pc.SendEvent(tbEventCategory.Text, tbEventAction.Text, tbEventName.Text, tbEventValue.Text))
                MessageBox.Show(this, "busy...");
        }

        private void btnLinkedin_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.LinkedIn);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.LinkedIn, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnFacebook_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.Facebook);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.Facebook, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnWeibo_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.SinaWeibo);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.SinaWeibo, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnTwitter_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.Twitter);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.Twitter, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnShowMap_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.ShowMap);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.ShowMap, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnRoute_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.Route);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.Route, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnSkype_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.SkypeOut);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.SkypeOut, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnSkypeSMS_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.SkypeSMS);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.SkypeSMS, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnImageView_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.ImageView);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.ImageView, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnRecovery_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.Recovery);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.Recovery, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.Print);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.Print, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnEmail_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.SendMail);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.SendMail, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnWebsite_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.OpenWebSite);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.OpenWebSite, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnEditCard_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.EditCard);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.EditCard, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnFindDuplicate_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.FindDuplicate);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.FindDuplicate, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnFindSameName_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.FindTheSameName);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.FindTheSameName, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnSetCategory_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.SetCategory);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.SetCategory, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnAddCardCount_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.AddCardCount);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.AddCardCount, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnManual_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.Manual);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.Manual, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.Search);
            searchKey = tbSearchKey.Text;
            if (string.IsNullOrEmpty(searchKey))
                return;

            int serchResults = 0;
            int.TryParse(tbSearchResults.Text, out serchResults);

            /*object[] arguments = { url, searchKey };
            BackgroundWorker bkworker = new BackgroundWorker();
            bkworker.DoWork += Bkworker_DoWorkSearch;
            bkworker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            bkworker.ProgressChanged += Worker_ProgressChanged;
            bkworker.RunWorkerAsync(arguments);*/
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.Search, tbTitle.Text, localeName, searchKey, serchResults))
                MessageBox.Show(this, "busy...");
        }
        

        #endregion

        #region Sync Buttons
        private void btnGmailSync_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.GoogleSync);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.GoogleSync, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnNasSync_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.NasSync);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.NasSync, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnActSync_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.ActSync);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.ActSync, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnOutlookSync_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.OutlookSync);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.OutlookSync, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnLotusNoteSync_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.LotusNoteSync);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.LotusNoteSync, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnSalesforceSync_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.SalesforceSync);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.SalesforceSync, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }
        #endregion

        #region Import Buttons
        private void btnDbankImport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.DBankImport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.DBankImport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnOutlookImport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.OutlookImport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.OutlookImport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnActImport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.ActImport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.ActImport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnLotusImport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.LotusNoteImport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.LotusNoteImport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnSalesforceImport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.SaleforceImport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.SaleforceImport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnWC8Import_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.WorldCardv8DBImport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.WorldCardv8DBImport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnWCXFImport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.WcxfImport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.WcxfImport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnCSVImport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.CsvImport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.CsvImport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnVCFImport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.VcardImport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.VcardImport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnJpegImport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.JpegImport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.JpegImport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnDrpoboxImport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.DropboxImport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.DropboxImport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnWCFImport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.WcxfImport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.WcxfImport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        #endregion

        #region Export Buttons
        private void btnDBankExport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.DBankExport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.DBankExport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnOutlookExport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.OutlookExport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.OutlookExport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnACTExport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.ActExport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.ActExport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnLotusExport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.LotusNotesExport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.LotusNotesExport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnSalesforceExport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.SaleforceExport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.SaleforceExport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnLeadExport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.SaleforceLeadExport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.SaleforceLeadExport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnWCXFExport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.WcxfExport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.WcxfExport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnExcelExport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.ExcelExport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.ExcelExport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnCSVExport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.CsvExport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.CsvExport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnVCFExport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.VcardExport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.VcardExport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnJpegExport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.JpegExport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.JpegExport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }

        private void btnDropboxExport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.DropboxExport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.DropboxExport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }        

        private void btnTxtExport_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.TxtExport);
            
            PPPiwikClient pc = new PPPiwikClient(SiteId, PiwikBaseUrl, cbAppName.SelectedValue.ToString(), versionNumber, uID, width, height);
            pc.SendRecordCompleted += MySendCompleted;
            pc.SetUA(UA);
            if (!pc.SendRecord(PPPiwikClient.RecordType.TxtExport, tbTitle.Text, localeName))
                MessageBox.Show(this, "busy...");
        }
        #endregion

        #region Random Tests
        int threads = 3;
        int totalloops;
        Stopwatch sw = new Stopwatch();
        DateTime lastDT;
        private void btnRandom_Click(object sender, RoutedEventArgs e)
        {
            lastDT = DateTime.Now;
            Console.WriteLine(lastDT);
            sw.Start();
            int loops = 10;
            int.TryParse(tbRandom.Text, out loops);
            int.TryParse(tbThreads.Text, out threads);
            GenerateURL(PPPiwikClient.RecordType.Use); // place-holder
            BackgroundWorker randomWorker = new BackgroundWorker();
            randomWorker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            randomWorker.DoWork += RandomWorker_DoWork;
            randomWorker.RunWorkerAsync(loops);
            totalloops = loops;
            tabs.IsEnabled = false;
        }

        private void btnRandomFavorite_Click(object sender, RoutedEventArgs e)
        {
            GenerateURL(PPPiwikClient.RecordType.CustomEvent); // place-holder
            tabs.IsEnabled = false;
            BackgroundWorker randomWorker = new BackgroundWorker();
            randomWorker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            randomWorker.DoWork += RandomFavoriteWorker_DoWork;
            randomWorker.RunWorkerAsync();
            
        }

        private void RandomFavoriteWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Random random = new Random();            
            string[] names = { "Alice", "Bob", "Chris", "David", "Emily", "Frank", "George", "Helen", "Irma", "James", "Kevin", "Linda", "Michael",
            "Nancy", "Oscar", "Patricia", "Quincy", "Robert", "Susan", "Thomas", "Ursula", "Victoria", "William", "Xavier", "Young", "Zachery"};
            for (int i = 0; i < (names.Length - 6); i++)
            {
                PiwikTracker tracker = new PiwikTracker(SiteId, PiwikBaseUrl, true);
                tracker.SetUserAgent(UA);
                tracker.SetUserId(names[i]);
                tracker.SetResolution(width, height);
                tracker.SetBrowserLanguage(localeName);

                // random favorite uri                
                Array enumValues = Enum.GetValues(typeof(PPPiwikClient.RecordType));
                string randomType = enumValues.GetValue(random.Next(enumValues.Length)).ToString();
                string uri = @"http://WorldCardTeam/最愛的功能/" + randomType;
                tracker.SetUrl(uri);
                tracker.DoTrackPageView();
                System.Threading.Thread.Sleep(10);
            }
        }

        private void RandomWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            object obj = e.Argument;
            int loops = (int)obj;
            //System.Threading.Thread.Sleep(5000);
            try
            {
                
                //Task[] taskArray = new Task[threads];
                List<Task> taskArray = new List<Task>();
                for (int i=0; i< threads; i++)
                {
                    int count = i +1 ;
                    Task t = new Task(() => DoStuff(loops, count));  //Task.Factory.StartNew(() => DoStuff(loops, i));
                    t.Start();
                    taskArray.Add(t);
                }
                /*Task task1 = Task.Factory.StartNew(() => DoStuff(loops, 0));
                Task task2 = Task.Factory.StartNew(() => DoStuff(loops, 1));
                Task task3 = Task.Factory.StartNew(() => DoStuff(loops, 2));*/

                Task.WaitAll(taskArray.ToArray());
                Console.WriteLine("done");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void DoStuff(int loops, int threadID)
        {
            Array values = Enum.GetValues(typeof(PPPiwikClient.RecordType));
            string[] os = { "6.0", "6.1", "6.2", "6.3", "10.0" };
            Random random = new Random();
            try
            {
                Console.WriteLine("thread[" + threadID + "] start...");
                //Stopwatch sw = new Stopwatch();
                //sw.Start();
                for (int i = 0; i < loops; i++)
                {
                    PPPiwikClient.RecordType randomType = (PPPiwikClient.RecordType)values.GetValue(random.Next(values.Length));
                    // 隨機姓名
                    uID = GenerateName();
                    // 隨機OS版本
                    string osver = (string)os.GetValue(random.Next(os.Length));
                    UA = "Mozilla/5.0 (Windows NT " + osver + "; WOW64)";
                    string url = GenerateURL(randomType, false);                    
                    if (!string.IsNullOrEmpty(url))
                        RecordSimplePageViewWithCustomProperties(url);
                    System.Threading.Thread.Sleep(10);
                }
                //sw.Stop();
                Console.WriteLine("thread[" + threadID + "] stop");//watch time = " + sw.Elapsed.Seconds + " seconds in " + loops + " loops");
                //float timeperloop = (float)sw.Elapsed.Seconds / (float)loops;
                //Console.WriteLine("thread[" + threadID + "] average " + timeperloop  + " seconds per loop");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static string GenerateName()
        {
            string[] names = { "Alice", "Bob", "Chris", "David", "Emily", "Frank", "George", "Helen", "Irma", "James", "Kevin", "Linda", "Michael",
            "Nancy", "Oscar", "Patricia", "Quincy", "Robert", "Susan", "Thomas", "Ursula", "Victoria", "William", "Xavier", "Young", "Zachery"};
            Random random = new Random();
            string Name = (string)names.GetValue(random.Next(names.Length));
            return Name;
        }

        private static string GenerateName(int len)
        {
            Random r = new Random();
            string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
            string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
            string Name = "";
            Name += consonants[r.Next(consonants.Length)].ToUpper();
            Name += vowels[r.Next(vowels.Length)];
            int b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
            while (b < len)
            {
                Name += consonants[r.Next(consonants.Length)];
                b++;
                Name += vowels[r.Next(vowels.Length)];
                b++;
            }

            return Name;
        }
        #endregion

        #region test
        private void btnContent_Click(object sender, RoutedEventArgs e)
        {
            var piwikTracker = new PiwikTracker(SiteId, PiwikBaseUrl, true);
            piwikTracker.SetUserAgent(UA);
            piwikTracker.SetBrowserLanguage(localeName);
            piwikTracker.SetResolution(width, height);

            string url = GenerateURL(PPPiwikClient.RecordType.CustomEvent);
            if (!string.IsNullOrWhiteSpace(uID))
                piwikTracker.SetUserId(uID);
            
            piwikTracker.SetUrl(url);
            piwikTracker.SetResolution(width, height);
            //if (!string.IsNullOrEmpty(tbDimentionValue.Text) && !string.IsNullOrEmpty(tbDimentionName.Text))
            //{
            //    int dimentionIndex = 1;
            //    int.TryParse(tbDimentionName.Text, out dimentionIndex);
            //    piwikTracker.SetCustomDimension(dimentionIndex, tbDimentionValue.Text);
            //}
            TrackingResponse response = piwikTracker.DoTrackContentImpression(tbContentName.Text, tbContentPiece.Text, tbContentTarget.Text);
            ShowResponse(response);
        }


        private void btnEcommerce_Click(object sender, RoutedEventArgs e)
        {
            double price = 0;
            double.TryParse(tbPrice.Text, out price);
            int quantity = 0;
            int.TryParse(tbQuantity.Text, out quantity);

            string orderID = Guid.NewGuid().ToString();

            var piwikTracker = new PiwikTracker(SiteId, PiwikBaseUrl, true);
            string url = GenerateURL(PPPiwikClient.RecordType.CustomEvent);
            piwikTracker.SetUserAgent(UA);
            piwikTracker.SetBrowserLanguage(localeName);
            if (!string.IsNullOrWhiteSpace(uID))
                piwikTracker.SetUserId(uID);
            piwikTracker.SetUrl(url);
            piwikTracker.SetResolution(width, height);
            //if (!string.IsNullOrEmpty(tbDimentionValue.Text) && !string.IsNullOrEmpty(tbDimentionName.Text))
            //{
            //    piwikTracker.SetCustomTrackingParameter(tbDimentionName.Text, tbDimentionValue.Text);
            //}
            piwikTracker.AddEcommerceItem(tbSKU.Text, tbProductName.Text, null, price, (ulong)quantity);
            TrackingResponse response = piwikTracker.DoTrackEcommerceOrder(orderID, price * quantity);
            /// Only the parameters $orderId and $grandTotal are required.
            ShowResponse(response);
        }

        private void btnEcommerceView_Click(object sender, RoutedEventArgs e)
        {
            double price = 0;
            double.TryParse(tbPrice2.Text, out price);

            var piwikTracker = new PiwikTracker(SiteId, PiwikBaseUrl, true);
            GenerateURL(PPPiwikClient.RecordType.CustomEvent);
            piwikTracker.SetUserAgent(UA);
            piwikTracker.SetBrowserLanguage(localeName);
            if (!string.IsNullOrWhiteSpace(uID))
                piwikTracker.SetUserId(uID);
            piwikTracker.SetUrl(tbEcommercePage.Text);
            piwikTracker.SetResolution(width, height);
            //if (!string.IsNullOrEmpty(tbDimentionValue.Text) && !string.IsNullOrEmpty(tbDimentionName.Text))
            //{
            //    int dimentionIndex = 1;
            //    int.TryParse(tbDimentionName.Text, out dimentionIndex);
            //    piwikTracker.SetCustomDimension(dimentionIndex, tbDimentionValue.Text);
            //}
            piwikTracker.SetEcommerceView(tbSKU2.Text, tbProductName2.Text, null, price);
            TrackingResponse response = piwikTracker.DoTrackPageView(tbTitle.Text);
            /// Only the parameters $orderId and $grandTotal are required.
            ShowResponse(response);
        }

        private void btnCart_Click(object sender, RoutedEventArgs e)
        {
            double price = 0;
            double.TryParse(tbPrice.Text, out price);
            int quantity = 0;
            int.TryParse(tbQuantity.Text, out quantity);

            string orderID = Guid.NewGuid().ToString();

            var piwikTracker = new PiwikTracker(SiteId, PiwikBaseUrl, true);
            string url = GenerateURL(PPPiwikClient.RecordType.CustomEvent);
            piwikTracker.SetUserAgent(UA);
            piwikTracker.SetBrowserLanguage(localeName);
            if (!string.IsNullOrWhiteSpace(uID))
                piwikTracker.SetUserId(uID);
            piwikTracker.SetUrl(url);
            piwikTracker.SetResolution(width, height);
            //if (!string.IsNullOrEmpty(tbDimentionValue.Text) && !string.IsNullOrEmpty(tbDimentionName.Text))
            //{
            //    piwikTracker.SetCustomTrackingParameter(tbDimentionName.Text, tbDimentionValue.Text);
            //}
            piwikTracker.AddEcommerceItem(tbSKU.Text, tbProductName.Text, null, price, (ulong)quantity);
            TrackingResponse response = piwikTracker.DoTrackEcommerceCartUpdate(price * quantity);
            /// Only the parameters $orderId and $grandTotal are required.
            ShowResponse(response);
        }        

        private void ShowResponse(TrackingResponse response)
        {
            MessageBox.Show(this, response.HttpStatusCode.ToString());
            progressBar.Visibility = Visibility.Hidden;
            tabs.IsEnabled = true;
            tbSearchKey.Text = "";
        }        

        private void btnFreeInput_Click(object sender, RoutedEventArgs e)
        {
            string strFreeInput = tbFreeInput.Text;
            if (string.IsNullOrWhiteSpace(strFreeInput))
                return;

            GenerateURL(PPPiwikClient.RecordType.CustomEvent);
            var piwikTracker = new PiwikTracker(SiteId, PiwikBaseUrl, true);
            string url = tbFreeInput.Text;
            piwikTracker.SetUserAgent(UA);
            piwikTracker.SetBrowserLanguage(localeName);
            if (!string.IsNullOrWhiteSpace(uID))
                piwikTracker.SetUserId(uID);
            piwikTracker.SetUrl(url);
            piwikTracker.SetResolution(width, height);
            //if (!string.IsNullOrEmpty(tbDimentionValue.Text) && !string.IsNullOrEmpty(tbDimentionName.Text))
            //{
            //    int dimentionIndex = 1;
            //    int.TryParse(tbDimentionName.Text, out dimentionIndex);                
            //    piwikTracker.SetCustomDimension(dimentionIndex, tbDimentionValue.Text);
            //}
            tbFreeInput.Text = string.Empty;
            TrackingResponse response = piwikTracker.DoTrackPageView(tbTitle.Text);
            ShowResponse(response);            
        }
        #endregion

        private string sourceLanguage = "fr";
        private string destLanguage = "en";

        /*private void ParseLanguages()
        {
            int sourceIndex = cbSourceLanguage.SelectedIndex;
            int destIndex = cbDestLanguage.SelectedIndex;
            sourceLanguage = (Languages)sourceIndex;
            destLanguage = (Languages)destIndex;
        }*/

        private void btnScanText_Click(object sender, RoutedEventArgs e)
        {
            //ParseLanguages();
            progressBar.Visibility = Visibility.Visible;
            tabs.IsEnabled = false;
            wpsxTracker.SetUserID(tbUserID.Text);
            //var result = await wpsxTracker.SendScanRecordAsync(sourceLanguage);
            var result = wpsxTracker.SendScanRecord(sourceLanguage);

            progressBar.Visibility = Visibility.Collapsed;
            tabs.IsEnabled = true;
            lbResult.Content = result.Message;
        }

        private void btnTranslate_Click(object sender, RoutedEventArgs e)
        {
            //ParseLanguages();
            progressBar.Visibility = Visibility.Visible;
            tabs.IsEnabled = false;
            wpsxTracker.SetUserID(tbUserID.Text);
            var result = wpsxTracker.SendTranslateRecord("Google", sourceLanguage, destLanguage);

            progressBar.Visibility = Visibility.Collapsed;
            tabs.IsEnabled = true;
            lbResult.Content = result.Message;
        }

        private void btnDictionary_Click(object sender, RoutedEventArgs e)
        {
            //ParseLanguages();
            progressBar.Visibility = Visibility.Visible;
            tabs.IsEnabled = false;
            wpsxTracker.SetUserID(tbUserID.Text);
            var result = wpsxTracker.SendDictionaryRecord("Basic", sourceLanguage, destLanguage);

            progressBar.Visibility = Visibility.Collapsed;
            tabs.IsEnabled = true;
            lbResult.Content = result.Message;
        }

        private void btnFavoriteScan_Click(object sender, RoutedEventArgs e)
        {
            var result = wpsxTracker.CheckServerStatus();            
        }

        private void btnFavoriteTranslate_Click(object sender, RoutedEventArgs e)
        {
            //wpsxTracker.SendEasyDictRecord("Basic", "fr", "en");
        }

        private void btnFavoriteDict_Click(object sender, RoutedEventArgs e)
        {
            //wpsxTracker.SendScanRecord("fr");
        }

        private void btnRandom2_Click(object sender, RoutedEventArgs e)
        {
            tabs.IsEnabled = false;
            GenerateURL(PPPiwikClient.RecordType.CustomEvent);
            BackgroundWorker random2Worker = new BackgroundWorker();
            random2Worker.DoWork += Random2Worker_DoWork;
            random2Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            random2Worker.RunWorkerAsync();
        }

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {

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

        private void Random2Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            CultureInfo[] cultureList = CultureInfo.GetCultures(CultureTypes.FrameworkCultures);
            List<string> vCultureNames = new List<string>();
            foreach (CultureInfo culture in cultureList)
            {
                vCultureNames.Add(culture.Name);
            }
            vCultureNames.RemoveAll(c => c.Length <= 3);

            string[] vInputLanguages = { "en", "zh-TW", "zh-CN", "jp", "es", "de", "fr", "ko", "it", "ru", "ar", "nl" };
            string piwikServerURL = @"http://matomo.penpower.net";
            int siteID = 1;

            // 產生 100 user 隨機資料
            Random random = new Random();
            for (int i = 0; i < 50; i++)
            {
                string userLocale = vCultureNames[random.Next(vCultureNames.Count)];
                string userID = "StressTest_" + i.ToString("D4");
                int operation_per_user = random.Next(50);
                // 每user隨機行為                
                //int dictCount = 0;
                //int scanCount = 0;
                //int translateCount = 0;
                for (int op = 0; op < operation_per_user; op++)
                {    
                    Array vWPSX = Enum.GetValues(typeof(WPSX_OP));
                    WPSX_OP operation = (WPSX_OP)vWPSX.GetValue(random.Next(vWPSX.Length));
                    string productURL = @"http://WPSX/Windows/v5.2.0/" + operation.ToString();
                    // 隨機語言
                    int fromLanguageIndex = random.Next(vInputLanguages.Length);
                    int toLanguageIndex = random.Next(vInputLanguages.Length);
                    if (toLanguageIndex == fromLanguageIndex)
                    {   // 避免重複
                        toLanguageIndex = (toLanguageIndex + 1) % vInputLanguages.Length;
                    }
                    string strFrom = vInputLanguages[fromLanguageIndex];
                    string strTo = vInputLanguages[toLanguageIndex];
                    switch (operation)
                    {
                        case WPSX_OP.EasyDict:
                        case WPSX_OP.Dictionary:                            
                            productURL += @"/From_" + strFrom + @"/To_" + strTo;
                            //dictCount++;
                            break;
                        case WPSX_OP.ScanText:
                            productURL += @"/" + strFrom;
                            //scanCount++;
                            break;
                        case WPSX_OP.Translate:
                            productURL += @"/From_" + strFrom + @"/To_" + strTo;
                            //translateCount++;
                            break;                        
                    }
                    PiwikTracker tracker = new PiwikTracker(siteID, piwikServerURL, true);                    
                    tracker.SetUrl(productURL);
                    tracker.SetUserId(userID);
                    string iPadUA = @"Mozilla/5.0 (iPad; CPU OS 11_2 like Mac OS X) AppleWebKit/602.4.6 (KHTML, like Gecko) Version/10.0 Mobile/14D27 Safari/602.1";
                    tracker.SetUserAgent(iPadUA);
                    tracker.SetBrowserLanguage(userLocale);                    
                    var respond = tracker.DoTrackPageView();

                    Console.WriteLine(respond.HttpStatusCode);
                    Thread.Sleep(20);
                }

                // 送交最愛的功能
                //WPSX_OP favoriteOperation;
                //string favoriteURL;
                //int max3 = Math.Max(dictCount, Math.Max(scanCount, translateCount));
                //if (max3 <= 0)
                //{
                //    favoriteURL = @"http://WPSX/v5.2.0/Favorite/None";
                //}
                //else if (max3 < 15)
                //{
                //    favoriteURL = @"http://WPSX/v5.2.0/Favorite/Mix";
                //}
                //else if (max3 == dictCount)
                //{
                //    favoriteOperation = WPSX_OP.Dictionary;
                //    favoriteURL = @"http://WPSX/v5.2.0/Favorite/" + favoriteOperation.ToString();
                //}
                //else if (max3 == scanCount)
                //{
                //    favoriteOperation = WPSX_OP.ScanText;
                //    favoriteURL = @"http://WPSX/v5.2.0/Favorite/" + favoriteOperation.ToString();
                //}
                //else
                //{
                //    favoriteOperation = WPSX_OP.Translate;
                //    favoriteURL = @"http://WPSX/v5.2.0/Favorite/" + favoriteOperation.ToString();
                //}
                //PiwikTracker favTracker = new PiwikTracker(siteID, piwikServerURL, true);
                //favTracker.SetUrl(favoriteURL);
                //favTracker.SetBrowserLanguage(userLocale);
                //favTracker.SetUserId(userID);
                //favTracker.SetUserAgent(UA);

                /*var respond2 = favTracker.DoTrackPageView();
                Console.Write(favoriteURL);
                Console.WriteLine("\t" + respond2.HttpStatusCode);*/
                Thread.Sleep(10);
            }
        }
    }
}
