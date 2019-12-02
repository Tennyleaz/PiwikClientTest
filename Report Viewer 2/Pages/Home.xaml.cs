using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using FirstFloor.ModernUI.Windows.Controls;

namespace Report_Viewer_2.Pages
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl
    {
        private const string REMOTE_DIR = @"\\10.10.10.3\Share\Tenny\Piwik-Matomo 文件與工具\Matomo 報告產生工具\";

        public Home()
        {
            InitializeComponent();
            welcomeText.Text = "請選擇上方的功能連結...";
            labelVersion.Content = "建置版本：" + Build.Timestamp;
        }

        private void BtnUpdate_OnClick(object sender, RoutedEventArgs e)
        {
            DateTime localDate = Updater.GetLocalBuildDate();
            DateTime remoteDate = Updater.GetRemoteBuildDate();
            if (remoteDate == DateTime.MinValue)
            {
                string str = "無法連線至\n" + REMOTE_DIR;
                ModernDialog.ShowMessage(str, "更新", MessageBoxButton.OK);
            }
            else
            {
                if (remoteDate > localDate)
                {
                    string str = "新版本：\n建置時間 " 
                                 + remoteDate.ToString() 
                                 + "\n位於：\n" 
                                 + REMOTE_DIR;
                    ModernDialog.ShowMessage(str, "更新", MessageBoxButton.OK);
                }
                else
                    ModernDialog.ShowMessage("目前找不到更新。          ", "更新", MessageBoxButton.OK);
            }
        }

        private void BtnRemoteFolder_OnClick(object sender, RoutedEventArgs e)
        {
            if (System.IO.Directory.Exists(REMOTE_DIR))
                System.Diagnostics.Process.Start("explorer.exe", REMOTE_DIR);
            else
            {
                string str = "無法開啟建置資料夾：\n"
                             + REMOTE_DIR;
                ModernDialog.ShowMessage(str, "更新", MessageBoxButton.OK);
            }
        }
    }
}
