namespace FlexSearch.ControlPanel {
    using Caliburn.Micro;
    using System;
    using System.ServiceProcess;
    using System.Linq;

    public class ShellViewModel : PropertyChangedBase, IShell {
        private string log;
        public string Log {
            get { return log; }
            set {
                log = value;
                NotifyOfPropertyChange(() => Log);
            }
        }

        void AppendToLog(string msg) {
            Log += String.Format("\n{0}", msg);
        }
        public ShellViewModel(){
            AppendToLog("Starting FlexSearch Control Panel");
            AppendToLog("FlexSearch Version: 0.2.1");
            AppendToLog("FlexSearch Server Service installed: " + DoesServiceExist("FlexSearchServer", ".").ToString());
        }

        bool DoesServiceExist(string serviceName, string machineName)
        {
            ServiceController[] services = ServiceController.GetServices(machineName);
            var service = services.FirstOrDefault(s => s.ServiceName == serviceName);
            return service != null;
        }
    }
}