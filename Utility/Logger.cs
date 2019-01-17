using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Utility
{
    public class Logger
    {
        private const string LOGPATH = @"\Penpower\MatomoTracker\";
        private static object m_sLockFlag = new object();
        private static int _log_level = 0;

        public static void WriteLog(string FileName, LOG_LEVEL llLogLevel, string LogStr)
        {
            //時間、iLevel、字串，符合Level設定範圍的就寫入log

            string strExt = Path.GetExtension(FileName);
            if (strExt == null || strExt.Length == 0)
                strExt = ".log";

            string strFile = Path.GetFileNameWithoutExtension(FileName);

            string LogPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + LOGPATH;
            //string dateToday = DateTime.Now.ToString("yyyy") + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd");
            string dateToday = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string FilePath = LogPath + strFile + dateToday + strExt;

            if (IsNeedWritelog(llLogLevel))
            {
                if (!Directory.Exists(LogPath))
                {
                    Directory.CreateDirectory(LogPath);
                }

                lock (m_sLockFlag)
                {
                    try
                    {
                        StreamWriter sw = null;
                        sw = File.AppendText(FilePath);

                        string LevelStr = "(str)";
                        LevelStr = LevelStr.Replace("str", llLogLevel.ToString());
                        dateToday = DateTime.Now.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
                        string timeNow = "[" + dateToday + " " + DateTime.Now.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture) + "]";
                        sw.WriteLine("{0} {1} {2}",                                 //[時間]  Level  ErrMsg
                            timeNow,
                            LevelStr,
                            LogStr);
                        sw.Flush();
                        if (sw != null) sw.Close();
                    }
                    catch (Exception e)
                    {
                        e.Message.ToString();
                    }
                }
            }
        }

        private static bool IsNeedWritelog(LOG_LEVEL iLevel)
        {
            // 如果是0，就先讀一次registry決定
            if (_log_level == 0)
                _log_level = GetLogLevel();

            if ((int)iLevel > _log_level)
                return false;
            else
                return true;
        }

        /// <summary>
        /// 取得註冊表中debug Log的Level，預設是LL_SUB_FUNC=2
        /// </summary>
        /// <returns></returns>
        private static int GetLogLevel()
        {
            //return 4;
            int llValue = 2;
            string subKeyPath = @"SOFTWARE\Penpower\MatomoTracker";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(subKeyPath);
            if (key != null)
            {
                object objValue = key.GetValue("LogLevel");
                if (objValue != null)
                {
                    if (key.GetValueKind("LogLevel") != RegistryValueKind.DWord)
                    {
                        //key.DeleteValue("LogLevel");
                        llValue = 2;
                    }
                    else
                    {
                        llValue = Convert.ToInt32(objValue);
                    }
                }
                else
                {
                    llValue = 2;
                }
                key.Close();
            }
            return llValue;
        }
    }

    public enum LOG_LEVEL
    {
        LL_SERIOUS_ERROR = 1,
        LL_SUB_FUNC = 2,
        LL_NORMAL_LOG = 3,
        LL_TRACE_LOG = 4
    }
}
