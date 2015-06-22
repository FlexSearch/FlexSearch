using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.ControlPanel.Tasks
{
    internal class OpenEventLogTask : TaskBase
    {
        const string Title = "Open Event Log";
        const string Description = "";
        public OpenEventLogTask(ILogService logService)
            : base(Title, Description, logService)
        {

        }

        public override void Execute()
        {
            Helpers.Exec("eventvwr.exe", String.Empty);
        }
    }
}
