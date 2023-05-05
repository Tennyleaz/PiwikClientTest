using System;
using Phone2Pc.Tracker;

namespace Phone2Pc.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            string testServerURL = "http://10.10.15.65";
            string realServerURL = "http://matomo.penpower.net";  // 正式站台使用的追蹤網址，僅能透過API溝通不能瀏覽
            int testSiteId = 8;   // 測試站台的專案號碼，Phone2PC必須是8
            int realSiteId = 10;  // 正式站台的專案號碼，Phone2PC必須是10
            string appVersion = "v1.1.0";
            string userId = "Tenny";  // 獨立的使用者ID，每一台電腦可以分辨
            Phone2PcTracker tracker = new Phone2PcTracker(testServerURL, appVersion, userId, testSiteId);

            // test action
            var result = tracker.SendAction("Launch");
            Console.WriteLine(result.ExcptionType);

            // test action with sub-action
            result = tracker.SendAction("Category1/Category2/Category3");
            Console.WriteLine(result.ExcptionType);

            // test event
            string eventCategory = "未分類";  // 類別
            string eventAction = "手寫";      // 動作
            string eventName = "簽名";        // 標籤
            int count = 10;
            result = tracker.SendEvent(eventCategory, eventAction, eventName, count.ToString());
            Console.WriteLine(result.ExcptionType);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
