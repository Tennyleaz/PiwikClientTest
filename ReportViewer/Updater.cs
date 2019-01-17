using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportViewer
{
    class Updater
    {
        private static readonly string REMOTE_FILE = @"\\10.10.10.3\Share\Tenny\Piwik Testing\Test App\build.txt";

        public static DateTime GetLocalBuildDate()
        {
            DateTime localBuildDate;
            if (DateTime.TryParseExact(Build.Timestamp, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out localBuildDate))
            {
                return localBuildDate;
            }
            else
                return DateTime.MinValue;
        }

        public static DateTime GetRemoteBuildDate()
        {
            try
            {
                string remoteString = System.IO.File.ReadAllText(REMOTE_FILE);
                DateTime remoteBuildDate;
                if (DateTime.TryParseExact(remoteString, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out remoteBuildDate))
                {
                    return remoteBuildDate;
                }
                else
                    return DateTime.MinValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return DateTime.MinValue;
            }
        }
    }
}
