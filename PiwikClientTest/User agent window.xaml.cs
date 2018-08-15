using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PiwikClientTest
{
    /// <summary>
    /// User_agent_window.xaml 的互動邏輯
    /// </summary>
    public partial class User_agent_window : Window
    {
        public string UA_String;
        public string UA_Display_String;
        public string Culture_String;        

        private static string iPhoneUA = @"Mozilla/5.0 (iPhone 8; CPU iPhone OS 11_2 like Mac OS X)";
        private static string iPadUA = @"Mozilla/5.0 (iPad; CPU OS 11_2 like Mac OS X) AppleWebKit/602.4.6 (KHTML, like Gecko) Version/10.0 Mobile/14D27 Safari/602.1";
        private static string AndroidUA = @"Mozilla/5.0 (Linux; Android 8.0; HTC U11)";
        private static string AndroidTabletUA = @"Mozilla/5.0 (Linux; Android 7.1.2; Asus TF701T Tablet) AppleWebKit/537.36 (KHTML, like Gecko) Safari/537.36";
        private static string WPUA = @"Mozilla/5.0 (Windows Phone 10.0; Microsoft; Lumia 950) AppleWebKit/537.36 (KHTML, like Gecko) Edge/13.1058";
        private static string SurfaceUA = @"Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; ARM; Trident/6.0; Touch; Microsoft Surface)";
        private static string XboxUA = "Mozilla/5.0 (Xbox; Xbox One) AppleWebKit/537.36 (KHTML, like Gecko)";

        private List<string> vCultureNames;

        public User_agent_window()
        {
            InitializeComponent();
            Loaded += User_agent_window_Loaded;
        }

        private void User_agent_window_Loaded(object sender, RoutedEventArgs e)
        {
            int index = 0;
            int selectedIndex = 0;
            string currentName = CultureInfo.CurrentCulture.Name;
            vCultureNames = new List<string>();
            CultureInfo[] cultureList = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
            foreach (CultureInfo culture in cultureList)
            {
                vCultureNames.Add(culture.Name);
                if (culture.Name == currentName)
                    selectedIndex = index;
                index++;
            }

            cbLocale.ItemsSource = vCultureNames;
            if (selectedIndex > 0)
                cbLocale.SelectedIndex = selectedIndex;

            lbDevice.Content = "普通桌上型電腦, Windows " + Environment.OSVersion.Version.ToString();
            cbUserAgents.SelectedIndex = 0;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void cbUserAgents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cbUserAgents.SelectedIndex)
            {
                case 0:
                default:
                    UA_String = string.Empty;
                    lbDevice.Content = "普通桌上型電腦, Windows " + Environment.OSVersion.Version.ToString();
                    break;
                case 1:
                    UA_String = AndroidUA;
                    lbDevice.Content = "HTC U11, Android 8.0";
                    UA_Display_String = "Android 8.0";
                    break;
                case 2:
                    UA_String = AndroidTabletUA;
                    lbDevice.Content = "Asus TF701T, Android 7.1.2";
                    UA_Display_String = "Android 7.1.2";
                    break;
                case 3:
                    UA_String = iPhoneUA;
                    lbDevice.Content = "iPhone, iOS 11.2";
                    UA_Display_String = "iOS 11.2";
                    break;
                case 4:
                    UA_String = iPadUA;
                    lbDevice.Content = "iPad, iOS 11.2";
                    UA_Display_String = "iOS 11.2";
                    break;
                case 5:
                    UA_String = WPUA;
                    lbDevice.Content = "Microsoft Lumia 950, Windows Phone 10.0";
                    UA_Display_String = "Windows Phone 10.0";
                    break;
                case 6:
                    UA_String = SurfaceUA;
                    lbDevice.Content = "Microsoft Surface, Windows 10.0";
                    UA_Display_String = "平板電腦, Windows 10.0";
                    break;
                case 7:
                    UA_String = XboxUA;
                    lbDevice.Content = "Xbox 主機";
                    UA_Display_String = "Xbox 主機";
                    break;
            }
        }

        private void cbLocale_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Culture_String = cbLocale.SelectedItem as string;
        }
    }
}
