using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPSX.Tracker;
using System.Runtime.InteropServices;

namespace WPSXWrapper
{
    [Guid("285F1871-EDB9-4AC4-8646-98EDE206F4E2"),
        ClassInterface(ClassInterfaceType.None),
        ComSourceInterfaces(typeof(WPSX_COM_Interface))]
    public class WPSXTracker_Net : WPSX_COM_Interface
    {
        private WPSXTracker tracker;

        public WPSXTracker_Net()
        {
            
        }

        private void Tracker_SendRecordCompleted(global::Piwik.Tracker.SendRecordCompleteEventArgs e)
        {
            //Console.WriteLine(e.HttpStatusCode);
        }

        public bool Initialize(string serverURL, string version, string userID,
            string appLocale, int siteID, string appName, int customWidth, int customHeight, bool ignoreSSLWarning)
        {
            if (tracker == null)
            {
                tracker = new WPSXTracker(serverURL, version, userID, appLocale, siteID, appName, customWidth, customHeight, ignoreSSLWarning);
                return true;
            }

            return false;
        }

        public bool SendDictionaryRecord(string dictionaryType, string sourceLanguage, string destinationLanguage)
        {
            //if (tracker != null)
            //    return tracker.SendDictionaryRecord(dictionaryType, sourceLanguage, destinationLanguage);
            //else
                return false;
        }

        public bool SendEasyDictRecord(string dictionaryType, string sourceLanguage, string destinationLanguage)
        {
            //if (tracker != null)
            //    return tracker.SendEasyDictRecord(dictionaryType, sourceLanguage, destinationLanguage);
            //else
                return false;
        }

        public bool SendScanRecord(string sourceLanguage)
        {
            //if (tracker != null)
            //    return tracker.SendScanRecord(sourceLanguage);
            //else
                return false;
        }

        public bool SendTranslateRecord(string engineType, string sourceLanguage, string destinationLanguage)
        {
            //if (tracker != null)
            //    return tracker.SendTranslateRecord(engineType, sourceLanguage, destinationLanguage);
            //else
                return false;
        }
    }
}
