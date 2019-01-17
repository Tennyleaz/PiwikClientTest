using System;
using System.Runtime.InteropServices;

namespace WPSXWrapper
{
    [Guid("02574906-D5DC-42F6-8091-8DB15F70EC36")]
    public interface WPSX_COM_Interface
    {
        [DispId(1)]
        bool Initialize(string serverURL, string version, string userID,
            string appLocale, int siteID, string appName, int customWidth, int customHeight, bool ignoreSSLWarning);

        [DispId(2)]
        bool SendScanRecord(string sourceLanguage);

        [DispId(3)]
        bool SendDictionaryRecord(string dictionaryType, string sourceLanguage, string destinationLanguage);

        [DispId(4)]
        bool SendEasyDictRecord(string dictionaryType, string sourceLanguage, string destinationLanguage);

        [DispId(5)]
        bool SendTranslateRecord(string engineType, string sourceLanguage, string destinationLanguage);
    }
}
