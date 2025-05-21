using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WC8.Tracker;

namespace WC8.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            string testServer = "http://10.10.15.65";
            string realServer = "https://matomo.penpower.net";
            WCRetailTracker tracker = new WCRetailTracker(
                realServer,
                "v8.8.2",
                "Tennytest 2",
                null,
                5
                );

            if (tracker.CheckServerStatus().ExcptionType == TrackerExcptionType.Success)
            {
                Console.WriteLine("send AD...");
                PrintResult(tracker.SendAdView("Scan Wizard"));

                Console.WriteLine("send SalesforceSync...");
                PrintResult(tracker.SendOperation(WCR_SYNC_OP.SalesforceSync));

                Console.WriteLine("send WcxfImport...");
                PrintResult(tracker.SendOperation(WCR_Import_OP.WcxfImport));

                Console.WriteLine("send JpegExport...");
                PrintResult(tracker.SendOperation(WCR_Export_OP.JpegExport));

                Console.WriteLine("send Error Log...");
                PrintResult(tracker.SendErrorLog("ImageView", "Null reference exception at line 123."));

                Console.WriteLine("send AddCard...");
                PrintResult(tracker.SendOperation(WCR_OP.AddCard));
                PrintResult(tracker.SendAddCardCountEvent(ADD_CARD_SOURCE.ManualAdd, 2));

                Console.WriteLine("send AddCard...");
                PrintResult(tracker.SendOperation(WCR_OP.AddCard));
                PrintResult(tracker.SendAddCardCountEvent(ADD_CARD_SOURCE.ScanADF, 10, "AV176U"));

                Console.WriteLine("send error report...");
                PrintResult(tracker.SendErrorLog("MainWindow", "LL_SERIOUS_ERROR/exception at some point?", "Additional Error Title"));
            }
            else
                Console.WriteLine("CheckServerStatus failed!");

            // wait for exit
            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        private static void PrintResult(TrackerResult result)
        {
            Console.WriteLine("result  = " + result.ExcptionType);            
            Console.WriteLine("code    = " + result.StatusCode);
            Console.WriteLine("message = " + result.Message);
            Console.WriteLine();
        }
    }
}
