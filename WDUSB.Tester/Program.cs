using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDUSB.Tracker;

namespace WDUSB.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            WDUSBTracker tracker = new WDUSBTracker(
                "http://10.10.15.65",
                "v1.2.1",
                "Tennytest 2",
                null,
                3
                );

            if (tracker.CheckServerStatus().ExcptionType == TrackerExcptionType.Success)
            {
                Console.WriteLine("send bookmark...");
                PrintResult(tracker.SendBookmarkRecord());
                Console.WriteLine("send capture...");
                PrintResult(tracker.SendCaptureRecord());
                Console.WriteLine("send bookmark...");
                PrintResult(tracker.SendDictionaryRecord("Basic", "en", "ja"));
                Console.WriteLine("send easy dict...");
                PrintResult(tracker.SendEasyDictRecord("StarDict", "fr", "es"));
            }
            else
                Console.WriteLine("CheckServerStatus failed");

            Console.ReadLine();
        }

        static void PrintResult(TrackerResult result)
        {
            Console.WriteLine("result  = " + result.ExcptionType);
            Console.WriteLine("message = " + result.Message);
            Console.WriteLine();
        }
    }
}
