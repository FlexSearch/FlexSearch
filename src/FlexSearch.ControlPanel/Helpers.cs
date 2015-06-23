using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.ControlPanel
{
    internal class Helpers
    {
        public static string BasePath = AppDomain.CurrentDomain.BaseDirectory;

        public static void PrintSeparator()
        {
            Console.WriteLine("-----------------------------------------------");
        }

        public static bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                .IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static string ToQuotedString(string s)
        {
            if (String.IsNullOrEmpty(s))
            {
                return s;
            }
            else if (s[0] == '"')
            {
                return s;
            }
            else
            {
                return "\"" + s + "\"";
            }
        }

        public static void Exec(string path, string argument, bool useShellExecute = false)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = path;
            psi.Arguments = argument;
            psi.WorkingDirectory = BasePath;
            //  Set the options.
            psi.UseShellExecute = useShellExecute;

            if (!useShellExecute)
            {
                psi.ErrorDialog = false;
                psi.CreateNoWindow = true;

                //  Specify redirection.
                psi.RedirectStandardError = true;
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
            }
            Console.WriteLine("Executing process: " + path);
            Console.WriteLine("Process Arguments: " + argument);
            using (var p = Process.Start(psi))
            {
                // Process can be null in case of Shell Execute
                if (p != null)
                {
                    p.WaitForExit();
                    if (p.StandardOutput != null)
                    {
                        Console.WriteLine(p.StandardOutput.ReadToEnd());
                    }
                    Console.WriteLine("Process Exit Code: " + p.ExitCode);
                }
            }
        }

        public static bool DoesServiceExist(string serviceName)
        {
            ServiceController[] services = ServiceController.GetServices("127.0.0.1");
            var service = services.FirstOrDefault(s => s.ServiceName == serviceName);
            return service != null;
        }

        public static ServiceController GetService(string serviceName)
        {
            ServiceController[] services = ServiceController.GetServices("127.0.0.1");
            var service = services.FirstOrDefault(s => s.ServiceName == serviceName);
            return service;
        }
    }
}
