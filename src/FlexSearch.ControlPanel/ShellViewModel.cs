namespace FlexSearch.ControlPanel
{
    using Caliburn.Micro;
    using System;
    using System.ServiceProcess;
    using System.Linq;
    using FlexSearch.ControlPanel.Tasks;

    public class ShellViewModel : PropertyChangedBase, IShell
    {
        private const string NotService = "FlexSearch is not installed as a service. We highly recommend installing FlexSearch as a Windows Service in production.";
        
        private string mainMessage;
        public string MainMessage
        {
            get { return mainMessage; }
            set
            {
                mainMessage = value;
                NotifyOfPropertyChange(() => MainMessage);
            }
        }

        private string log;
        public string Log
        {
            get { return log; }
            set
            {
                log = value;
                NotifyOfPropertyChange(() => Log);
            }
        }

        private bool notProcessing = true;
        public bool NotProcessing {
            get { return notProcessing; }
            set
            {
                notProcessing = value;
                NotifyOfPropertyChange(() => NotProcessing);
            }
        }

        public BindableCollection<TaskBase> Actions
        {
            get { return TaskRegisteration.Tasks; }
        }

        void AppendToLog(string msg)
        {
            Log += String.Format("\n{0}", msg);
        }
        
        public ShellViewModel()
        {
            this.MainMessage = NotService;
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