using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.ControlPanel
{
    internal class LogService : ILogService
    {
        private Action<string> logger;
        public LogService(Action<string> logger)
        {
            this.logger = logger;
        }
        public void Log(string message)
        {
            logger.Invoke(message);
        }
    }
}
