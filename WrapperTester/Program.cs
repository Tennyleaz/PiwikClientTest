using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPSXWrapper;

namespace WrapperTester
{
    class Program
    {
        static void Main(string[] args)
        {
            WPSXTracker_Net tracker = new WPSXTracker_Net();            
            if (tracker.Initialize("http://10.10.12.39", "v5.2.0", "Alice", "en-US", 2, "WPSX", 800, 600, false))
            {
                bool success = tracker.SendScanRecord("en");
                if (success)
                    Console.WriteLine("SendScanRecord success");
                else
                    Console.WriteLine("SendScanRecord fail");

                Console.ReadLine();

                success = tracker.SendDictionaryRecord("Basic", "fr", "en");
                if (success)
                    Console.WriteLine("SendDictionaryRecord success");
                else
                    Console.WriteLine("SendDictionaryRecord fail");

                Console.ReadLine();
                
                success = tracker.SendEasyDictRecord("Basic", "fr", "en");
                if (success)
                    Console.WriteLine("SendEasyDictRecord success");
                else
                    Console.WriteLine("SendEasyDictRecord fail");

                Console.ReadLine();

                success = tracker.SendTranslateRecord("Google", "fr", "en");
                if (success)
                    Console.WriteLine("SendTranslateRecord success");
                else
                    Console.WriteLine("SendTranslateRecord fail");
            }
            Console.ReadLine();
        }
    }
}
