using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using log4net;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core
{
    public class Scheduler
    {        
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        Dictionary<Timer, BaseTask> tasks = new Dictionary<Timer, BaseTask>();

        public Boolean IsMaster { get; set; }

        public Scheduler() {
            this.IsMaster = false;
        }

        public void AddTask(TaskConfig taskConfig)
        {
            ThreadContext.Properties["MyID"] = CommonUtil.GetMyID();
            BaseTask task = Activator.CreateInstance(System.Type.GetType(taskConfig.Task)) as BaseTask;
            task.Init(taskConfig);
            task.Scheduler = this;
            if (!task.Enabled)
                return;

            log.Debug("Starting " + taskConfig.Task + " at " + task.StartTime);

            TimeSpan ts = task.StartTime - DateTime.Now;
            Timer timer;
            if (ts.TotalMilliseconds > 0)
            {
                log.Debug("Starting " + taskConfig.Task + " in " + ts);
                timer = new Timer(ts.TotalMilliseconds);
            }
            else
            {
                log.Debug("Starting " + taskConfig.Task + " now!");
                timer = new Timer(20);
            }
            timer.Elapsed += new ElapsedEventHandler(onElapsedTime);
            tasks.Add(timer, task);
            timer.Enabled = true;
            timer.AutoReset = true;
        }
        
        private void onElapsedTime(object source, ElapsedEventArgs e)
        {
            (source as Timer).Stop();

            BaseTask task = tasks[source as Timer];


            ThreadContext.Properties["MyID"] = CommonUtil.GetMyID();
            if (this.IsMaster)
                log.Debug("I am Master run " + task.GetType().Name);
            else
                log.Debug("I am Slave skip " + task.GetType().Name);

            if (task.Type == ScheduleType.Once)
            {
                // last execution, remove task when it's done
                //tasks.Remove(source as Timer);
                source = null;
            }
            else if (task.Type == ScheduleType.Interval)
            {
                DateTime dt = DateTime.Now.AddHours(task.IntervalHours);
                dt = dt.AddMinutes(task.IntervalMinutes);
                (source as Timer).Interval = ((TimeSpan)(dt - DateTime.Now)).TotalMilliseconds;
            }
            else if (task.Type == ScheduleType.Daily)
            {
                DateTime dt = DateTime.Now.AddDays(1);
                (source as Timer).Interval = ((TimeSpan)(dt - DateTime.Now)).TotalMilliseconds;
            }
            else if (task.Type == ScheduleType.Weekly)
            {
                DateTime dt = DateTime.Now.AddDays(7);
                (source as Timer).Interval = ((TimeSpan)(dt - DateTime.Now)).TotalMilliseconds;
            }
            else if (task.Type == ScheduleType.Monthly)
            {
                DateTime dt = DateTime.Now.AddMonths(1);
                (source as Timer).Interval = ((TimeSpan)(dt - DateTime.Now)).TotalMilliseconds;
            }

            if (this.IsMaster)
            {   // If I am master, run the tasks, otherwise skip executing them
                try
                {
                    ThreadContext.Properties["TaskName"] = task.GetType().Name;                    
                    task.DoExecute();
                }
                catch (Exception ex)
                {
                    log.Error("Failed to execute " + task.GetType().Name, ex);
                }
            }
            if (source != null)
                (source as Timer).Start();
        }

        public static ScheduleType getScheduleType(string typeStr)
        {
            if (ScheduleType.Daily.ToString().Equals(typeStr))
                return ScheduleType.Daily;
            if (ScheduleType.Interval.ToString().Equals(typeStr))
                return ScheduleType.Interval;
            if (ScheduleType.Monthly.ToString().Equals(typeStr))
                return ScheduleType.Monthly;
            if (ScheduleType.Once.ToString().Equals(typeStr))
                return ScheduleType.Once;
            if (ScheduleType.Weekly.ToString().Equals(typeStr))
                return ScheduleType.Weekly;
            throw new Exception("ScheduleType not found!");
        }
    }
}
