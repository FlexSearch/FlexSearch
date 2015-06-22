using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.ControlPanel.Tasks
{
    public abstract class TaskBase
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public ILogService LogService { get; private set; }
        protected TaskBase(string title, string description, ILogService logService)
        {
            this.Title = title;
            this.Description = description;
            this.LogService = logService;
        }
        public abstract void Execute();
    }
}
