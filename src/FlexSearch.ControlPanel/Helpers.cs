using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.ControlPanel
{
    internal class Helpers
    {
        public static string BasePath = AppDomain.CurrentDomain.BaseDirectory;
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

        public static bool Exec(string path, string argument)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = path;
            psi.Arguments = argument;
            psi.WorkingDirectory = BasePath;
            psi.RedirectStandardOutput = false;
            psi.UseShellExecute = false;
            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
                if (p.ExitCode != 0 && (psi.FileName != "netsh.exe" && p.ExitCode != 183))
                {
                    return true;
                }
                return false;
            }
        }
    }
}
