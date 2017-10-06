using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace Piwik.Tracker
{
    public class PPPiwikClient
    {
        #region Private members
        private string UA;// = "Mozilla/5.0 (Windows NT 10.0; WOW64; en-US;)"; //"Mozilla/5.0 (Windows NT 10.0; WOW64; rv:55.0)";
        private static readonly string PiwikBaseUrl = "http://13.94.36.109";
        private static int SiteId = 2;  //Piwik控制台裡面設定的site id號碼，對應不同產品

        private string workerException;
        private string uID;
        private string titleString;
        private string applicationName;
        private string versionNumber;
        private string responceString;
        //private string localeName;
        //private string searchKey;
        private int width, height;
        private BackgroundWorker worker;
        #endregion

        #region Send result event definition
        public delegate void SendRecordEventHandler(object sender, SendRecordCompleteEventArgs e);
        /// <summary>
        /// 當傳送完成或失敗會發生
        /// </summary>
        public event SendRecordEventHandler SendRecordCompleted;

        /// <summary>
        /// 傳送的結果會觸發這個event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnSendRecordCompleted(SendRecordCompleteEventArgs e)
        {
            SendRecordCompleted?.Invoke(this, e);
        }
        #endregion

        /// <summary>
        /// Chung定義的操作分類
        /// </summary>
        public enum RecordType
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
        }

        /// <summary>
        /// 基本資料在Constructor時候傳入。之後要傳送一筆資料，只要呼叫它的SendRecord()
        /// </summary>
        /// <param name="siteID">Piwik伺服器網頁上定義的每個網站ID</param>
        /// <param name="appName">例如"WorldCardTeam"</param>
        /// <param name="version">例如"v1.0.0"</param>
        /// <param name="userID">辨識獨立的使用者代號</param>
        /// <param name="customWidth">複寫系統螢幕寬度</param>
        /// <param name="customHeight">複寫系統螢幕高度</param>
        public PPPiwikClient(int siteID, string appName, string version, string userID, int? customWidth = null, int? customHeight = null)
        {
            #region 檢查用
            if (string.IsNullOrEmpty(appName))
                throw new ArgumentNullException(appName, "App name must be not null or empty.");
            if (string.IsNullOrEmpty(userID))
                throw new ArgumentNullException(userID, "User ID must be not null or empty.");
            if (string.IsNullOrEmpty(version))
                throw new ArgumentNullException(version, "Version must be not null or empty.");
            #endregion

            int osMajor = Environment.OSVersion.Version.Major;
            int osMinor = Environment.OSVersion.Version.Minor;
            UA = "Mozilla/5.0 (Windows NT " + osMajor + "." + osMinor + "; WOW64)";

            width = customWidth.HasValue ? customWidth.Value : (int)SystemParameters.PrimaryScreenWidth;
            height = customHeight.HasValue ? customHeight.Value : (int)SystemParameters.PrimaryScreenHeight;

            applicationName = appName;
            versionNumber = version;
            uID = userID;
            SiteId = siteID;

            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = false;
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }        

        #region Background workers
        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SendRecordCompleteEventArgs ea = new SendRecordCompleteEventArgs();
            ea.ExceptionMessage = workerException;
            ea.SendResult = responceString;
            OnSendRecordCompleted(ea);
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            object argument = e.Argument;
            PiwikTracker pTracker = argument as PiwikTracker;
            if (pTracker != null)
            {
                try
                {
                    TrackingResponse response = pTracker.DoTrackPageView(titleString);
                    responceString = response.HttpStatusCode.ToString();
                    workerException = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(e);
                    workerException = ex.Message;
                    responceString = null;
                }
            }
        }
        #endregion

        private string GenerateTrackingURL(RecordType recordType)
        {
            /*uID = tbUserID.Text;
            titleString = tbTitle.Text;
            versionNumber = tbVersionNumber.Text;
            lbResult.Content = "";

            int.TryParse(tbWidth.Text, out width);
            int.TryParse(tbHeight.Text, out height);*/

            string url = "http://" + applicationName + "/" + versionNumber;
            /*if (cbAppName.SelectedIndex == 0) //WCT
            {
                url += "WorldCardTeam/";
                if (string.IsNullOrWhiteSpace(versionNumber) || !versionNumber.StartsWith("v"))
                    versionNumber = "v1.0.0";
            }
            else  //WC8
            {
                url += "WorldCard8/";
                if (string.IsNullOrWhiteSpace(versionNumber) || !versionNumber.StartsWith("v"))
                    versionNumber = "v8.5.6";
            }

            url += versionNumber;*/

            switch (recordType)
            {
                case RecordType.Use:
                case RecordType.Use_Daily:
                case RecordType.Manual:
                case RecordType.ShowMap:
                case RecordType.Route:
                case RecordType.SkypeOut:
                case RecordType.SkypeSMS:
                case RecordType.ImageView:
                case RecordType.Recovery:
                case RecordType.Print:
                case RecordType.SendMail:
                case RecordType.OpenWebSite:
                case RecordType.EditCard:
                case RecordType.FindDuplicate:
                case RecordType.FindTheSameName:
                case RecordType.SetCategory:
                case RecordType.AddCardCount:
                case RecordType.Search:
                    url += "/General/" + recordType.ToString();
                    break;
                case RecordType.Facebook:
                case RecordType.LinkedIn:
                case RecordType.SinaWeibo:
                case RecordType.Twitter:
                    url += "/SocialNetwork/" + recordType.ToString();
                    break;
                case RecordType.GoogleSync:
                case RecordType.NasSync:
                case RecordType.ActSync:
                case RecordType.OutlookSync:
                case RecordType.LotusNoteSync:
                case RecordType.SalesforceSync:
                    url += "/Sync/" + recordType.ToString();
                    break;
                case RecordType.DBankImport:
                case RecordType.OutlookImport:
                case RecordType.ActImport:
                case RecordType.LotusNoteImport:
                case RecordType.SaleforceImport:
                case RecordType.WcxfImport:
                case RecordType.CsvImport:
                case RecordType.VcardImport:
                case RecordType.JpegImport:
                case RecordType.DropboxImport:
                case RecordType.WcfImport:
                case RecordType.WorldCardv8DBImport:
                    url += "/Import/" + recordType.ToString();
                    break;
                case RecordType.DBankExport:
                case RecordType.OutlookExport:
                case RecordType.ActExport:
                case RecordType.LotusNotesExport:
                case RecordType.SaleforceExport:
                case RecordType.SaleforceLeadExport:
                case RecordType.WcxfExport:
                case RecordType.ExcelExport:
                case RecordType.CsvExport:
                case RecordType.VcardExport:
                case RecordType.JpegExport:
                case RecordType.DropboxExport:
                case RecordType.TxtExport:
                    url += "/Export/" + recordType.ToString();
                    break;
                default:
                    url += "/Undefined/" + recordType.ToString();
                    break;
            }
            Console.Write(url);
            return url;
        }

        private void GenerateAdditionalProperties(ref PiwikTracker tracker, string appLanguage, string searchKey = null)
        {
            // arguments
            if (!string.IsNullOrEmpty(appLanguage))
            {
                tracker.SetBrowserLanguage(appLanguage);
            }
            if (!string.IsNullOrEmpty(searchKey))
            {
                tracker.DoTrackSiteSearch(searchKey);
            }
            // private variables
            tracker.SetResolution(width, height);
            tracker.SetUserAgent(UA);
            tracker.SetUserId(uID);
        }

        /// <summary>
        /// 製作record url和添加自訂資訊，並傳送給piwik server。
        /// 如果searchKey有填值，就會自動增加一個搜尋的紀錄。
        /// </summary>
        /// <param name="recordType"></param>
        /// <param name="customTitle">可以自訂的標題</param>
        /// <param name="appLanguage">例如"zh-TW"</param>
        /// <param name="searchKey">搜尋關鍵字</param>
        /// <returns>true=開始傳送 false=忙碌中</returns>
        public bool SendRecord(RecordType recordType, string customTitle, string appLanguage, string searchKey = null)
        {
            titleString = customTitle;
            string url = GenerateTrackingURL(recordType);
            PiwikTracker piwikTracker = new PiwikTracker(SiteId, PiwikBaseUrl);
            piwikTracker.SetUrl(url);
            GenerateAdditionalProperties(ref piwikTracker, appLanguage, searchKey);

            // send in background
            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync(piwikTracker);
                return true;
            }
            else
                return false;
        }
    }

    /// <summary>
    /// 提供傳送結果的資料
    /// </summary>
    public class SendRecordCompleteEventArgs : EventArgs
    {
        public string SendResult { get; set; }
        public string ExceptionMessage { get; set; }        
    }
}
