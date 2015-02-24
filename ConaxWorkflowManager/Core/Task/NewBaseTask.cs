using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    class NewBaseTask
    {
        private TaskConfig taskConfig;
        public DateTime StartTime { get; set; }

        public NewBaseTask(TaskConfig tc)
        {
            taskConfig.Task = tc.Task;
        }
    }
}
