using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Timers;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using Timer = System.Timers.Timer;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core
{
    public class Runner
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scheduler Scheduler { get; set; }
        private XmlDocument d = null;
        private String lastState { get; set; }
        private UInt32 lastStateCounter { get; set; }

        public void Run()
        {            
            Scheduler = new Scheduler();

            try
            {
                XmlTextReader r = new XmlTextReader(ConfigurationSettings.AppSettings["configFilename"]);
                d = new XmlDocument();
                d.Load(r);

                lastStateCounter = 0;
                lastState = "";
                Config.Init(d);
                // sleep for 3sec
                //Int32 pollTime = 3000;
                //System.Threading.Thread.Sleep(pollTime);
                CheckState();
                //WriteSateFile();
                Timer timer = new Timer(10000);
                timer.Elapsed += new ElapsedEventHandler(onElapsedTime);
                timer.Enabled = true;
                timer.AutoReset = true;

                foreach (TaskConfig taskConfig in Config.GetConfig().TaskConfigs)
                    Scheduler.AddTask(taskConfig);
            }
            catch (Exception exc)
            {
                log.Error("Error starting up", exc);
                throw;
            }
        }

        private void onElapsedTime(object source, ElapsedEventArgs e)
        {
            (source as Timer).Stop();
            // write state file
            CheckState();
            //WriteSateFile();

            if (source != null)
                (source as Timer).Start();
        }

        private void CheckState() {

            try
            {
                String stateFilePath = this.StateFilePath;
                if (!File.Exists(stateFilePath))
                {   // create wfm sta file
                    WriteSateFile();
                }

                // read value
                String statefile = "";
                try
                {
                    using (StreamReader sr = new StreamReader(stateFilePath))
                    {
                        statefile = sr.ReadToEnd();
                    }
                }
                catch (Exception ex) {
                    log.Warn("Failed to read data from " + stateFilePath, ex);
                }

                //log.Debug("statefile " + statefile + " " + CommonUtil.GetMyID());
                String[] data = statefile.Split('|');

                if (CommonUtil.GetMyID().Equals(data[0]))
                {   // I am still master
                    Scheduler.IsMaster = true;
                    lastStateCounter = 0;
                    WriteSateFile(); // update my stamp
                }
                else {
                    Scheduler.IsMaster = false;
                    // check if it's still the same as the last state
                    // add a coutner
                    if (lastState.Equals(statefile))
                        lastStateCounter++;
                     else
                        lastStateCounter = 0;

                    lastState = statefile;
                    // if counter is 3+, then the time stamp hasn't been changged for a while, 
                    // the master instance might have gone down.
                    // write down my stamp and take it over in next round.
                    if (lastStateCounter >= 3) {
                        WriteSateFile(); // write my stamp
                        lastStateCounter = 0;
                        ThreadContext.Properties["MyID"] = CommonUtil.GetMyID();
                        log.Debug(CommonUtil.GetMyID() + " is taking over.");
                    }
                }
            }
            catch (Exception ex) {

                Scheduler.IsMaster = false;
                String sSource = "XtendWorkflowManager";
                if (!EventLog.SourceExists(sSource))
                    EventLog.CreateEventSource(sSource, "Application");

                EventLog.WriteEntry(sSource, "Verify XtendWorkflowManager state, " + ex.Message + ex.StackTrace, EventLogEntryType.Error);
            }
        }

        private void WriteSateFile() { 
            String stateFilePath = this.StateFilePath;

            String data = CommonUtil.GetMyID() + "|" + DateTime.UtcNow.Ticks;
            Int32 wrtieAtempt = 0;
            do
            {            
                try
                {
                    System.IO.File.WriteAllText(stateFilePath, data);
                    return;
                }
                catch (Exception ex)
                {
                    wrtieAtempt++;
                    if (wrtieAtempt ==3)
                        log.Warn("Failed to write to " + stateFilePath + " with " + data, ex);
                }
                Thread.Sleep(1000);
            } while (wrtieAtempt < 3);
        }

        private String _stateFilePath;
        private String StateFilePath {
            get {
                if (string.IsNullOrWhiteSpace(_stateFilePath))
                {
                    XmlNode sysconNode = d.SelectSingleNode("CWMConfig/SystemConfigurations/SystemConfiguration[@name='ConaxWorkflowManager']");
                    XmlElement DBSourcenode = (XmlElement)sysconNode.SelectSingleNode("ConfigParam[@key='DBSource']");

                    String dirPath = Path.GetDirectoryName(DBSourcenode.GetAttribute("value"));
                    _stateFilePath = Path.Combine(dirPath, "wfm.sta");
                }
                return _stateFilePath;
            }
        }


        //private String _myIP;
        //private String MyIP {
        //    get {

        //        if (String.IsNullOrWhiteSpace(_myIP))
        //        {
        //            var workflowConfig = Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == "ConaxWorkflowManager");
        //            if (workflowConfig.ConfigParams.ContainsKey("MyID"))
        //            {
        //                _myIP = workflowConfig.GetConfigParam("MyID");
        //            }
        //            else
        //            {
        //                var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
        //                var ip = (
        //                           from addr in hostEntry.AddressList
        //                           where addr.AddressFamily == AddressFamily.InterNetwork
        //                           select addr.ToString()
        //                    ).FirstOrDefault();
        //                _myIP = ip;
        //            }
        //        }
        //        return _myIP;
        //    }
        //}
    }
}
