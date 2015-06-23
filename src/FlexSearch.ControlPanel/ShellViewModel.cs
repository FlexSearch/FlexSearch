namespace FlexSearch.ControlPanel
{
    using Caliburn.Micro;
    using System;
    using System.ServiceProcess;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.IO;

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

        private bool notProcessing = true;
        public bool NotProcessing
        {
            get { return notProcessing; }
            set
            {
                notProcessing = value;
                NotifyOfPropertyChange(() => NotProcessing);
                this.IsBusy = !notProcessing;
            }
        }

        private bool isBusy = false;
        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                NotifyOfPropertyChange(() => IsBusy);
            }
        }

        public int ActionsSelectedIndex { get; set; }
        private BindableCollection<Tuple<string, System.Action>> actions = new BindableCollection<Tuple<string, System.Action>>();
        public BindableCollection<Tuple<string, System.Action>> Actions
        {
            get { return actions; }
            set { NotifyOfPropertyChange(() => Actions); }
        }

        void AppendToLog(string msg)
        {
            Console.WriteLine(msg);
        }

        private async void ExecuteAction(Tuple<string, System.Action> action)
        {
            this.IsBusy = true;
            await Task.Run(() =>
            {
                try
                {
                    Helpers.PrintSeparator();
                    Console.WriteLine(String.Format("Invoke action: {0}", action.Item1));
                    action.Item2.Invoke();
                    Console.WriteLine(String.Format("Action: {0} invoked successfully.", action.Item1));
                }
                catch (Exception e)
                {
                    Console.WriteLine(String.Format("Unable to invoke the action: {0} successfully.", action.Item1));
                    Helpers.PrintSeparator();
                    Console.WriteLine(e.ToString());
                }
                Helpers.PrintSeparator();
                this.IsBusy = false;
            });
        }

        public void InvokeAction(string caller)
        {
            var action = this.Actions.FirstOrDefault(x => String.Equals(x.Item1, caller, StringComparison.OrdinalIgnoreCase));
            if (action != null)
            {
                ExecuteAction(action);
            }
        }

        public void InvokeAction()
        {
            ExecuteAction(this.Actions[this.ActionsSelectedIndex]);
        }

        private void TaskRegisteration()
        {
            this.Actions.Add(new Tuple<string, System.Action>("Install Windows Service", Tasks.InstallServerTask));
            this.Actions.Add(new Tuple<string, System.Action>("Un-install Windows Service", Tasks.UnInstallServerTask));
            this.Actions.Add(new Tuple<string, System.Action>("Start Service", Tasks.StartServerTask));
            this.Actions.Add(new Tuple<string, System.Action>("Stop Service", Tasks.StopServiceTask));
            this.Actions.Add(new Tuple<string, System.Action>("Open Event Viewer", () => Helpers.Exec("eventvwr.exe", "")));
            this.Actions.Add(new Tuple<string, System.Action>("Open Services", () => Helpers.Exec("mmc.exe", "services.msc")));
            this.Actions.Add(new Tuple<string, System.Action>("Explore", () => Helpers.Exec("explorer.exe", Helpers.BasePath)));
            this.Actions.Add(new Tuple<string, System.Action>("Settings", () => Helpers.Exec("explorer.exe", Path.Combine(Helpers.BasePath, "Conf"))));
            this.Actions.Add(new Tuple<string, System.Action>("About", () => Helpers.Exec("http://flexsearch.net", "", true)));
            this.Actions.Add(new Tuple<string, System.Action>("Exit", () => Application.Current.Shutdown()));
        }

        public ShellViewModel()
        {
            TaskRegisteration();
            DetectPrimaryAction();
        }

        private void DetectPrimaryAction()
        {
            // Check if FlexSearch is installed as a service
            if (!Helpers.DoesServiceExist("FlexSearch-Server"))
            {
                this.MainMessage = NotService;
                this.ActionsSelectedIndex = 0;
                return;
            }

            this.MainMessage += "FlexSearch Server is installed as a service.";
            var service = Helpers.GetService("FlexSearch-Server");
            if (service.Status != ServiceControllerStatus.Running)
            {
                this.MainMessage += " FlexSearch Server service is not running.";
                this.ActionsSelectedIndex = 2;
            }
        }
    }
}