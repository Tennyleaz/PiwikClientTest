using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Report_Viewer_2
{
    internal class Updater
    {
        private const string REMOTE_FILE = @"\\10.10.10.3\Share\Tenny\Piwik-Matomo 文件與工具\Matomo 報告產生工具\build.txt";

        public static DateTime GetLocalBuildDate()
        {
            if (DateTime.TryParseExact(Build.Timestamp, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime localBuildDate))
            {
                return localBuildDate;
            }
            return DateTime.MinValue;
        }

        public static DateTime GetRemoteBuildDate()
        {
            try
            {
                string remoteString = System.IO.File.ReadAllText(REMOTE_FILE);
                if (DateTime.TryParseExact(remoteString, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime remoteBuildDate))
                {
                    return remoteBuildDate;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return DateTime.MinValue;
        }
    }
}
