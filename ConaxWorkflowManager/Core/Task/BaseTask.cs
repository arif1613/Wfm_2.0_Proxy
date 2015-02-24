using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public abstract class BaseTask
    {
        private TaskConfig taskConfig;
        public ScheduleType Type { get; set; }
        public DateTime StartTime { get; set; }
        public int IntervalHours { get; set; }
        public int IntervalMinutes { get; set; }
        public bool Enabled { get; set; }
        public Scheduler Scheduler { get; set; }

        protected TaskConfig TaskConfig
        {
            get { return taskConfig; }
        }


        public abstract void DoExecute();

        internal void Init(TaskConfig taskConfig)
        {
            if (taskConfig.GetConfigParam("Enabled").ToLower().Equals("true"))
                Enabled = true;
            Type = Scheduler.getScheduleType(taskConfig.GetConfigParam("Type"));
            String dateStr = taskConfig.GetConfigParam("StartDate");
            String timeStr = taskConfig.GetConfigParam("StartTime");
            DateTime start = DateTime.Now;
            if (dateStr != null && dateStr.Length != 0)
            {
                start = DateTime.ParseExact(dateStr, "yyyy-MM-dd", new DateTimeFormatInfo());
            }
            if (timeStr != null && timeStr.Length != 0)
            {
                DateTime time = DateTime.ParseExact(timeStr, "HH:mm", new DateTimeFormatInfo());
                start = start.AddHours(time.Hour - start.Hour);
                start = start.AddMinutes(time.Minute - start.Minute);
                start = start.AddSeconds(-start.Second);
                if (start < DateTime.Now)
                    start = start.AddDays(1);
            }
            StartTime = start;

            String intervalStr = taskConfig.GetConfigParam("Interval");
            if (intervalStr != null && intervalStr.Length != 0)
            {
                DateTime interval = DateTime.ParseExact(intervalStr, "HH:mm", new DateTimeFormatInfo());
                IntervalHours = interval.Hour;
                IntervalMinutes = interval.Minute;
            }
            else
            {
                IntervalHours = 0;
                IntervalMinutes = 0;
            }
            this.taskConfig = taskConfig;
        }
    }
}
