using System;
using System.Globalization;
using System.Windows;
using Piwik.Tracker;

namespace WDUSB.Tracker
{
    /// <summary>
    /// 可紀錄的操作
    /// </summary>
    public enum WDUSB_OP
    {
        Dictionary,
        EasyDict,
        Capture,
        AddToBookmark
    }

    /// <summary>
    /// 當傳送完成或失敗會發生
    /// </summary>
    public class TrackerResult
    {
        public short StatusCode;
        public string Message;
        public TrackerExcptionType ExcptionType;
    }

    public enum TrackerExcptionType
    {
        /// <summary>
        /// 當 http request 成功執行 (但是不保證結果是 OK)
        /// </summary>
        Success = 0,
        /// <summary>
        /// constructor 或是 method 參數沒有填對
        /// </summary>
        ArgumentExcption,
        /// <summary>
        /// ignoreSSLWarning = false 時候無法驗證伺服器憑證
        /// </summary>
        SSLException,
        /// <summary>
        /// 其他 http 例外
        /// </summary>
        WebException,
        /// <summary>
        /// 其他例外
        /// </summary>
        OtherException
    }

    public class WDUSBTracker
    {
        #region Private members        
        private static string PiwikBaseUrl = "http://matomo.penpower.net";
        private static int SiteId = 1;  //Piwik控制台裡面設定的site id號碼，對應不同產品
        private static readonly int TIMEOUT_SECOUND = 30;
        private static string referrerURL = null;

        private string _userAgent;  // = "Mozilla/5.0 (Windows NT 10.0; WOW64; en-US;)"; //"Mozilla/5.0 (Windows NT 10.0; WOW64; rv:55.0)";
        private string _userID;
        private string _title;
        private string _appName;
        private string _appLocale;
        private string _version;
        private int _width, _height;
        private int _dimentionIndex;
        private string _dimentionValue;
        private bool _ignoreSSLWarning;
        private TrackerExcptionType _excptionType;
        private string _excptionMessage;

        private static readonly TrackerResult ArgumentExcptionResult = new TrackerResult()
        {
            ExcptionType = TrackerExcptionType.ArgumentExcption,
            StatusCode = 0,
            Message = "Argument sourceLanguage is null or empty. ",
        };
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="siteID">Matomo 伺服器網頁上定義的每個網站 ID</param>
        /// <param name="serverURL">追蹤伺服器位址</param>
        /// <param name="appName">複寫預設的程式名，例如 WDUSB</param>
        /// <param name="version">以v開頭為佳，例如 v1.2.1</param>
        /// <param name="userID">辨識獨立的使用者代號，例如 Alice</param>
        /// <param name="customWidth">複寫系統螢幕寬度</param>
        /// <param name="customHeight">複寫系統螢幕高度</param>
        /// <param name="ignoreSSLWarning">忽略沒有有效簽章的 server 產生的 ssl 警告</param>
        /// <param name="appLocale">複寫使用者的預設 Windows 文化/地區，遵循 RFC 4646 的文化特性格式，例如 en-US</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public WDUSBTracker(string serverURL, string version, string userID,
            string appLocale = null, int siteID = 4, string appName = "WDUSB", int? customWidth = null, int? customHeight = null, bool ignoreSSLWarning = false)
        {
            _excptionType = TrackerExcptionType.Success;
            _excptionMessage = string.Empty;

            #region 檢查用
            if (string.IsNullOrEmpty(appName))
            {
                //throw new ArgumentNullException(appName, "App name must not be null or empty. ");
                _excptionMessage = "App name must not be null or empty. ";
                _excptionType = TrackerExcptionType.ArgumentExcption;
            }
            if (string.IsNullOrEmpty(serverURL))
            {
                //throw new ArgumentNullException(version, "Server URL must not be null or empty. ");
                _excptionMessage += "Server URL must not be null or empty. ";
                _excptionType = TrackerExcptionType.ArgumentExcption;
            }
            if (string.IsNullOrEmpty(version))
            {
                //throw new ArgumentNullException(serverURL, "Version must not be null or empty.");
                _excptionMessage += "Version must not be null or empty. ";
                _excptionType = TrackerExcptionType.ArgumentExcption;
            }
            if (siteID <= 0)
            {
                //throw new ArgumentException("siteID must > 0.", "siteID");
                _excptionMessage += "siteID must > 0.";
                _excptionType = TrackerExcptionType.ArgumentExcption;
            }
            #endregion

            if (!string.IsNullOrEmpty(serverURL))
                PiwikBaseUrl = serverURL;

            // os version
            int osMajor = Environment.OSVersion.Version.Major;
            int osMinor = Environment.OSVersion.Version.Minor;
            string architecture = Environment.Is64BitOperatingSystem ? "; x64" : "; x86";
            _userAgent = "Mozilla/5.0 (Windows NT " + osMajor + "." + osMinor + architecture + ")";

            // locale of user environment            
            if (!string.IsNullOrEmpty(appLocale))
                _appLocale = appLocale;
            else
            {
                CultureInfo ci = CultureInfo.CurrentCulture;
                _appLocale = ci.Name;
            }

            _width = customWidth.HasValue ? customWidth.Value : (int)SystemParameters.PrimaryScreenWidth;
            _height = customHeight.HasValue ? customHeight.Value : (int)SystemParameters.PrimaryScreenHeight;

            _appName = appName;
            _version = version;
            _userID = userID;
            SiteId = siteID;
            _ignoreSSLWarning = ignoreSSLWarning;
        }

        /// <summary>
        /// 送出一筆拍照紀錄。
        /// </summary>
        public TrackerResult SendCaptureRecord()
        {
            TrackerResult result = new TrackerResult();
            if (_excptionType != TrackerExcptionType.ArgumentExcption)
            {
                WDUSB_OP recordType = WDUSB_OP.Capture;
                string url = "http://" + _appName + "/Windows/" + _version + "/" + recordType.ToString();
                result = DoTrackPage(url, _title);
            }
            else
            {
                // argument exception, don't do anything
                result.StatusCode = 0;
                result.Message = _excptionMessage;
            }
            return result;
        }

        /// <summary>
        /// 送出一筆查字典紀錄。
        /// </summary>
        /// <param name="dictionaryType">字典的種類</param>
        /// <param name="sourceLanguage">翻譯來源語言</param>
        /// <param name="destinationLanguage">翻譯目標語言</param>
        public TrackerResult SendDictionaryRecord(string dictionaryType, string sourceLanguage, string destinationLanguage)
        {
            TrackerResult result = new TrackerResult();
            if (_excptionType != TrackerExcptionType.ArgumentExcption)
            {
                // check arguments
                if (string.IsNullOrEmpty(dictionaryType) || string.IsNullOrEmpty(sourceLanguage) || string.IsNullOrEmpty(destinationLanguage))
                {
                    return ArgumentExcptionResult;
                }

                WDUSB_OP recordType = WDUSB_OP.Dictionary;
                string url = "http://" + _appName + "/Windows/" + _version + "/" + recordType.ToString()
                    + "/" + dictionaryType
                    + "/From_" + sourceLanguage
                    + "/To_" + destinationLanguage;
                result = DoTrackPage(url, _title);
            }
            else
            {
                // argument exception, don't do anything
                result.StatusCode = 0;
                result.Message = _excptionMessage;
            }
            return result;
        }

        /// <summary>
        /// 送出一筆簡易查字典紀錄。
        /// </summary>
        /// <param name="dictionaryType">字典的種類</param>
        /// <param name="sourceLanguage">翻譯來源語言</param>
        /// <param name="destinationLanguage">翻譯目標語言</param>
        /// <returns>true=開始傳送 false=忙碌中</returns>
        public TrackerResult SendEasyDictRecord(string dictionaryType, string sourceLanguage, string destinationLanguage)
        {
            TrackerResult result = new TrackerResult();
            if (_excptionType != TrackerExcptionType.ArgumentExcption)
            {
                // check arguments
                if (string.IsNullOrEmpty(dictionaryType) || string.IsNullOrEmpty(sourceLanguage) || string.IsNullOrEmpty(destinationLanguage))
                {
                    return ArgumentExcptionResult;
                }

                WDUSB_OP recordType = WDUSB_OP.EasyDict;
                string url = "http://" + _appName + "/Windows/" + _version + "/" + recordType.ToString()
                    + "/" + dictionaryType
                    + "/From_" + sourceLanguage
                    + "/To_" + destinationLanguage;
                result = DoTrackPage(url, _title);
            }
            else
            {
                // argument exception, don't do anything
                result.StatusCode = 0;
                result.Message = _excptionMessage;
            }
            return result;
        }

        /// <summary>
        /// 送出一筆翻譯紀錄。
        /// </summary>
        /// <param name="engineType">翻譯/搜尋引擎</param>
        /// <param name="sourceLanguage">翻譯來源語言</param>
        /// <param name="destinationLanguage">翻譯目標語言</param>
        /// <returns>true=開始傳送 false=忙碌中</returns>
        public TrackerResult SendBookmarkRecord()
        {
            TrackerResult result = new TrackerResult();
            if (_excptionType != TrackerExcptionType.ArgumentExcption)
            {
                WDUSB_OP recordType = WDUSB_OP.AddToBookmark;
                string url = "http://" + _appName + "/Windows/" + _version + "/" + recordType.ToString();
                result = DoTrackPage(url, _title);
            }
            else
            {
                // argument exception, don't do anything
                result.StatusCode = 0;
                result.Message = _excptionMessage;
            }
            return result;
        }

        public TrackerResult CheckServerStatus()
        {
            TrackerResult result = new TrackerResult();
            if (_excptionType != TrackerExcptionType.ArgumentExcption)
            {
                try
                {
                    PiwikTracker tracker = new PiwikTracker(SiteId, PiwikBaseUrl, _ignoreSSLWarning);
                    tracker.DisableCookieSupport();

                    TrackingResponse response = tracker.DoPing();
                    result.ExcptionType = TrackerExcptionType.Success;
                    result.StatusCode = (short)response.HttpStatusCode;
                    result.Message = response.HttpStatusCode.ToString();
                }
                catch (System.Net.WebException ex)
                {
                    var response = ex.Response as System.Net.HttpWebResponse;
                    if (ex.Status == System.Net.WebExceptionStatus.TrustFailure && !_ignoreSSLWarning)
                    {
                        // tell user set ignoreSSLWarning to true
                        result.ExcptionType = TrackerExcptionType.SSLException;
                        result.StatusCode = (short)response?.StatusCode;
                        result.Message = "Something wrong with the certificate, try set ignoreSSLWarning to true. ";
                    }
                    else
                    {
                        // If exception thrown, no status code is provided. We will make one.
                        result.ExcptionType = TrackerExcptionType.WebException;
                        result.StatusCode = (short)response?.StatusCode;
                        result.Message = ex.ToString();
                    }
                }
                catch (Exception ex)
                {
                    // If exception thrown, no status code is provided. We will make one.
                    result.ExcptionType = TrackerExcptionType.OtherException;
                    result.StatusCode = 0;
                    result.Message = ex.ToString();
                }
            }
            else
            {
                // argument exception, don't do anything
                result.StatusCode = 0;
                result.Message = _excptionMessage;
            }
            return result;
        }

        private TrackerResult DoTrackPage(string url, string title = null)
        {
            TrackerResult result = new TrackerResult();
            try
            {
                PiwikTracker tracker = new PiwikTracker(SiteId, PiwikBaseUrl, _ignoreSSLWarning);
                tracker.DisableCookieSupport();
                tracker.SetUrl(url);
                tracker.SetUrlReferrer(referrerURL);
                referrerURL = url;
                GenerateAdditionalProperties(tracker);

                TrackingResponse response = tracker.DoTrackPageView(title);
                result.ExcptionType = TrackerExcptionType.Success;
                result.StatusCode = (short)response.HttpStatusCode;
                result.Message = response.HttpStatusCode.ToString();
            }
            catch (System.Net.WebException ex)
            {
                var response = ex.Response as System.Net.HttpWebResponse;
                if (ex.Status == System.Net.WebExceptionStatus.TrustFailure && !_ignoreSSLWarning)
                {
                    // tell user set ignoreSSLWarning to true
                    result.ExcptionType = TrackerExcptionType.SSLException;
                    result.StatusCode = (short)response?.StatusCode;
                    result.Message = "Something wrong with the certificate, try set ignoreSSLWarning to true. ";
                }
                else
                {
                    // If exception thrown, no status code is provided. We will make one.
                    result.ExcptionType = TrackerExcptionType.WebException;
                    result.StatusCode = (short)response?.StatusCode;
                    result.Message = ex.ToString();
                }
            }
            catch (Exception ex)
            {
                // If exception thrown, no status code is provided. We will make one.
                result.ExcptionType = TrackerExcptionType.OtherException;
                result.StatusCode = 0;
                result.Message = ex.ToString();
            }
            return result;
        }

        /*
        /// <summary>
        /// 設定自訂維度。存活於每個 WPSXTracker 實例的生命週期。
        /// </summary>
        /// <param name="index">伺服器上自訂維度的索引號</param>
        /// <param name="value">自訂維度值</param>
        public void SetDimention(int index, string value)
        {
            _dimentionIndex = index;
            _dimentionValue = value;
        }

        /// <summary>
        /// 設定自訂(網頁)標題。存活於每個 WPSXTracker 實例的生命週期。
        /// </summary>
        /// <param name="title"></param>
        public void SetCustomTitle(string title)
        {
            _title = title;
        }

        public void ClearUrlReferrer()
        {
            referrerURL = null;
        }

        /// <summary>
        /// 複寫 Constructor 裡面指定的 user id
        /// </summary>
        /// <param name="userID"></param>
        public void SetUserID(string userID)
        {
            _userID = userID;
        }
        */

        private string GenerateTrackingURL(WDUSB_OP recordType, string sourceLanguage)
        {
            string url = "http://" + _appName + "/Windows/" + _version + "/" + recordType.ToString() + "/" + sourceLanguage;
            return url;
        }

        private string GenerateTrackingURL<T>(WDUSB_OP recordType, T toolType, string sourceLanguage, string destinationLanguage)
        {
            string url = "http://" + _appName + "/Windows/" + _version + "/" + recordType.ToString()
                + "/" + toolType.ToString()
                + "/From_" + sourceLanguage
                + "/To_" + destinationLanguage;
            return url;
        }

        /// <summary>
        /// 設定額外的 UA、地區、ID、解析度、timeout
        /// </summary>
        /// <param name="piwikTracker"></param>
        private void GenerateAdditionalProperties(PiwikTracker piwikTracker)
        {
            if (!string.IsNullOrEmpty(_userAgent))
                piwikTracker.SetUserAgent(_userAgent);
            if (!string.IsNullOrEmpty(_appLocale))
                piwikTracker.SetBrowserLanguage(_appLocale);
            if (!string.IsNullOrEmpty(_userID))
                piwikTracker.SetUserId(_userID);
            if (_width > 0 && _height > 0)
                piwikTracker.SetResolution(_width, _height);
            piwikTracker.RequestTimeout = new TimeSpan(0, 0, TIMEOUT_SECOUND);
        }
    }
}
