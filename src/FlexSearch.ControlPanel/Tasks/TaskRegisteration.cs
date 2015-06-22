using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.ControlPanel.Tasks
{
    internal class TaskRegisteration
    {
        public static BindableCollection<TaskBase> Tasks { get; set; }
        static TaskRegisteration()
        {
            // We could have used MEF or any other similar tchnology to build this list. But this
            // is a very simple application which can do without that complexiety.


        }
    }
}
