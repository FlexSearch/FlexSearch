using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FlexSearch.ControlPanel.Tasks
{
    internal class InstallServerTask : TaskBase
    {
        const string Title = "Install Windows Service";
        const string Description = "";
        public InstallServerTask(ILogService logService)
            : base(Title, Description, logService)
        {

        }

        public override void Execute()
        {
            if (!Helpers.IsAdministrator())
            {
                LogService.Log(String.Format("Task '{0}' requires admin permission.", Title));
            }

            LogService.Log("Registering the FlexSearch service");
            Helpers.Exec("FlexSearch Server.exe", "install");
            LogService.Log("Installing the ETW manifest");
            var parameters = new StringBuilder();
            var args = parameters
                .Append("im ")
                .Append(Helpers.ToQuotedString(Path.Combine(Helpers.BasePath, "FlexSearch.Logging.FlexSearch.etwManifest.man")))
                .Append(" /rf:")
                .Append(Helpers.ToQuotedString(Path.Combine(Helpers.BasePath, "FlexSearch.Logging.FlexSearch.etwManifest.dll")))
                .Append(" /mf:")
                .Append(Helpers.ToQuotedString(Path.Combine(Helpers.BasePath, "FlexSearch.Logging.FlexSearch.etwManifest.dll")))
                .ToString();
            Helpers.Exec("wevtutil.exe", args);
            Helpers.Exec("netsh.exe", "http add urlacl url=http://+:9800/ user=everyone listen=yes");
        }
    }
}
