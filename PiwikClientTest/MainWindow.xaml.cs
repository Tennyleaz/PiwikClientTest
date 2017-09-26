using Piwik.Tracker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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

namespace PiwikClientTest
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        private string UA;// = "Mozilla/5.0 (Windows NT 10.0; WOW64; en-US;)"; //"Mozilla/5.0 (Windows NT 10.0; WOW64; rv:55.0)";
        private static readonly string PiwikBaseUrl = "http://10.10.12.73";
        private static int SiteId = 2;  //Piwik控制台裡面設定的site id號碼，對應不同產品

        private BackgroundWorker worker;
        private string workerException;
        private string uID;
        private string titleString;
        private string versionNumber;
        private string responceString;
        private string localeName;

        enum RecordType
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
            LotusImport,
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
            LotusExport,
            SaleforceExport,
            SaleforceLeadExport,
            WcxfExport,
            ExcelExport,
            CsvExport,
            VcardExport,
            JpegExport,
            DropboxExport,
            TxtExport
        }

        public MainWindow()
        {
            UA = "Mozilla/5.0 (Windows NT 10.0; WOW64; en-US;)";
            InitializeComponent();            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int osMajor = Environment.OSVersion.Version.Major;
            int osMinor = Environment.OSVersion.Version.Minor;
            UA = "Mozilla/5.0 (Windows NT " + osMajor + "." + osMinor + "; WOW64; en-US;)";
            CultureInfo ci = CultureInfo.CurrentCulture;
            localeName = ci.Name;
            lbOS.Content = "Windows NT " + osMajor + "." + osMinor + ", " + localeName;
            cbAppName.SelectedIndex = 0;
            //Guid guid = new Guid();
            //tbUserID.Text = guid.ToString();
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = false;
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
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
                MessageBox.Show(workerException);
                lbResult.Content = workerException;
            }
            else
                lbResult.Content = responceString;
            progressBar.Visibility = Visibility.Hidden;
            tabs.IsEnabled = true;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            worker.ReportProgress(1);
            object obj = e.Argument;
            string url = obj as string;
            if (!string.IsNullOrEmpty(url))
                RecordSimplePageViewWithCustomProperties(url);
        }
        #endregion

        private void cbAppName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbAppName.SelectedIndex == 0)  //WCT
            {
                btnNasSync.IsEnabled = true;
                btnOutlookSync.IsEnabled = true;
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
            }
            else  //WC8
            {
                btnNasSync.IsEnabled = false;
                btnOutlookSync.IsEnabled = false;
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
            }
        }

        #region Prepare URL data
        private void RecordSimplePageViewWithCustomProperties(string url)
        {
            workerException = null;

            var piwikTracker = new PiwikTracker(SiteId, PiwikBaseUrl);
            piwikTracker.SetUserAgent(UA);

            piwikTracker.SetResolution((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);

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
            piwikTracker.SetCustomTrackingParameter("dimension2", versionNumber);
            
            piwikTracker.SetBrowserLanguage(localeName);

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

        private string GenerateURL(RecordType recordType)
        {
            tabs.IsEnabled = false;

            uID = tbUserID.Text;
            titleString = tbTitle.Text;
            versionNumber = tbVersionNumber.Text;

            string url = "http://";
            if (cbAppName.SelectedIndex == 0) //WCT
            {
                url += "WorldCardTeam/";
                if (string.IsNullOrWhiteSpace(versionNumber))
                    versionNumber = "v1.1.0";
            }
            else  //WC8
            {
                url += "WorldCard8/";
                if (string.IsNullOrWhiteSpace(versionNumber))
                    versionNumber = "v8.5.6";
            }

            url += versionNumber;

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
                case RecordType.LotusImport:
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
                case RecordType.LotusExport:
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
            return url;
        }
        #endregion

        #region General Usage Buttons
        private void btnUse_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.Use);
            worker.RunWorkerAsync(url);
        }

        private void btnLinkedin_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.LinkedIn);
            worker.RunWorkerAsync(url);
        }

        private void btnFacebook_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.Facebook);
            worker.RunWorkerAsync(url);
        }

        private void btnWeibo_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.SinaWeibo);
            worker.RunWorkerAsync(url);
        }

        private void btnTwitter_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.Twitter);
            worker.RunWorkerAsync(url);
        }

        private void btnShowMap_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.ShowMap);
            worker.RunWorkerAsync(url);
        }

        private void btnRoute_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.Route);
            worker.RunWorkerAsync(url);
        }

        private void btnSkype_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.SkypeOut);
            worker.RunWorkerAsync(url);
        }

        private void btnSkypeSMS_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.SkypeSMS);
            worker.RunWorkerAsync(url);
        }

        private void btnImageView_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.ImageView);
            worker.RunWorkerAsync(url);
        }

        private void btnRecovery_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.Recovery);
            worker.RunWorkerAsync(url);
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.Print);
            worker.RunWorkerAsync(url);
        }

        private void btnEmail_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.SendMail);
            worker.RunWorkerAsync(url);
        }

        private void btnWebsite_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.OpenWebSite);
            worker.RunWorkerAsync(url);
        }

        private void btnEditCard_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.EditCard);
            worker.RunWorkerAsync(url);
        }

        private void btnFindDuplicate_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.FindDuplicate);
            worker.RunWorkerAsync(url);
        }

        private void btnFindSameName_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.FindTheSameName);
            worker.RunWorkerAsync(url);
        }

        private void btnSetCategory_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.SetCategory);
            worker.RunWorkerAsync(url);
        }

        private void btnAddCardCount_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.AddCardCount);
            worker.RunWorkerAsync(url);
        }

        private void btnManual_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.Manual);
            worker.RunWorkerAsync(url);
        }
        #endregion

        #region Sync Buttons
        private void btnGmailSync_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.GoogleSync);
            worker.RunWorkerAsync(url);
        }

        private void btnNasSync_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.NasSync);
            worker.RunWorkerAsync(url);
        }

        private void btnActSync_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.ActSync);
            worker.RunWorkerAsync(url);
        }

        private void btnOutlookSync_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.OutlookSync);
            worker.RunWorkerAsync(url);
        }

        private void btnLotusNoteSync_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.LotusNoteSync);
            worker.RunWorkerAsync(url);
        }

        private void btnSalesforceSync_Click(object sender, RoutedEventArgs e)
        {
            string url = GenerateURL(RecordType.SalesforceSync);
            worker.RunWorkerAsync(url);
        }
        #endregion
    }
}
