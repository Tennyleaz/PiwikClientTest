using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportViewer
{
    internal class RemoteManager : IDisposable
    {
        private SshClient _sshClient;        

        public RemoteManager()
        {
            
        }

        public bool Init(string host)
        {
            try
            {
                _sshClient = new SshClient(host, "webmaster", "penpower");
                _sshClient.Connect();
                if (_sshClient.IsConnected)
                    return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return false;
        }


        public void Dispose()
        {            
            _sshClient?.Disconnect();
            _sshClient?.Dispose();
            _sshClient = null;
        }

        public bool RaiseActionDisplayLimit(int limeit)
        {
            if (_sshClient != null && _sshClient.IsConnected)
            {
                string command = @"~/printpass.sh | sudo -S sed -i -- 's/visitor_log_maximum_actions_per_visit =.*/visitor_log_maximum_actions_per_visit = " + limeit + @"/' /var/www/matomo/public_html/config/global.ini.php";
                var cmd = _sshClient.RunCommand(command);
                string output = cmd.Result;
                Console.WriteLine(output);
                return true;
            }
            return false;
        }

        public bool ResetActionDisplayLimit()
        {
            if (_sshClient != null && _sshClient.IsConnected)
            {
                string command = @"~/printpass.sh | sudo -S sed -i -- 's/visitor_log_maximum_actions_per_visit =.*/visitor_log_maximum_actions_per_visit = 500/' /var/www/matomo/public_html/config/global.ini.php";
                var cmd = _sshClient.RunCommand(command);
                string output = cmd.Result;
                Console.WriteLine(output);
                cmd = _sshClient.RunCommand("exit");
                output = cmd.Result;
                Console.WriteLine(output);
                return true;
            }
            return false;
        }
    }
}
