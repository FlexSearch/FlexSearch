using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.ControlPanel
{
    internal class Tasks
    {
        public static void InstallServerTask()
        {
            if (!Helpers.IsAdministrator())
            {
                Console.WriteLine("Installation requires admin permission.");
                return;
            }

            Console.WriteLine("Registering the FlexSearch service");
            Helpers.Exec("FlexSearch Server.exe", "install");
            Console.WriteLine("Installing the ETW manifest");
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

        public static void UnInstallServerTask()
        {
            if (!Helpers.IsAdministrator())
            {
                Console.WriteLine("Un-installation requires admin permission.");
                return;
            }

            Console.WriteLine("Unregistering the FlexSearch service");
            Helpers.Exec("FlexSearch Server.exe", "uninstall");
            Console.WriteLine("Un-installing the ETW manifest");
            var parameters = new StringBuilder();
            var args = parameters
                .Append("um ")
                .Append(Helpers.ToQuotedString(Path.Combine(Helpers.BasePath, "FlexSearch.Logging.FlexSearch.etwManifest.man")))
                .ToString();
            Helpers.Exec("wevtutil.exe", args);
        }

        public static void StartServerTask()
        {
            var service = Helpers.GetService("FlexSearch-Server");
            if (service != null)
            {
                if (service.Status != ServiceControllerStatus.Running)
                {
                    Console.WriteLine("Starting FlexSearch Server service.");
                    service.Start();
                }
                else
                {
                    Console.WriteLine("FlexSearch Server service is already running.");
                }
            }
            else
            {
                Console.WriteLine("FlexSearch Server service is not installed.");
            }
        }

        public static void StopServiceTask()
        {
            var service = Helpers.GetService("FlexSearch-Server");
            if (service != null)
            {
                if (service.Status != ServiceControllerStatus.Stopped)
                {
                    Console.WriteLine("Stopping FlexSearch Server service.");
                    service.Stop();
                }
                else
                {
                    Console.WriteLine("FlexSearch Server service is already stopped.");
                }
            }
            else
            {
                Console.WriteLine("FlexSearch Server service is not installed.");
            }
        }
    }
}
