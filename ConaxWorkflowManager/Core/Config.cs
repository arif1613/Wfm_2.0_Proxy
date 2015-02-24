using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using log4net;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core
{
    public class Config
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static Config config;
        private List<TaskConfig> taskConfigs = new List<TaskConfig>();
        private List<SystemConfig> systemConfigs = new List<SystemConfig>();
        private List<CustomConfig> customConfigs = new List<CustomConfig>();
        private List<WorkFlowConfig> workFlowConfigs = new List<WorkFlowConfig>();
        private List<IngestXMLConfig> ingestXMLConfigs = new List<IngestXMLConfig>();
        private List<ServiceConfig> serviceConfigs = new List<ServiceConfig>();

        //private String dbsource;

        public List<TaskConfig> TaskConfigs
        {
            get { return taskConfigs; }
        }

        public List<SystemConfig> SystemConfigs
        {
            get { return systemConfigs; }
        }

        public List<CustomConfig> CustomConfigs
        {
            get { return customConfigs; }
        }

        public List<WorkFlowConfig> WorkFlowConfigs
        {
            get { return workFlowConfigs; }
        }

        public List<IngestXMLConfig> IngestXMLConfigs
        {
            get { return ingestXMLConfigs; }
        }

        public List<ServiceConfig> ServiceConfigs
        {
            get { return serviceConfigs; }
        }


        public static void Init(XmlDocument d)
        //here d=ConaxWorkFlowManagerConfig.xml
        {
            config = new Config();
            try
            {
                foreach (XmlNode module in d.SelectNodes("CWMConfig/SystemConfigurations/SystemConfiguration"))
                {
                    SystemConfig systemConfig = null;
                    switch (module.Attributes["name"].Value)
                    {
                        case SystemConfigNames.ConaxWorkflowManager:
                            systemConfig = new ConaxWorkflowManagerConfig(module);
                            break;
                        case SystemConfigNames.MPP:
                            systemConfig = new MPPConfig(module);
                            break;
                        case SystemConfigNames.ElementalEncoder:
                            systemConfig = new ElementalEncoderConfig(module);
                            break;
                        case SystemConfigNames.MPP5:
                            systemConfig = new Mpp5Configuration(module);
                            break;
                        default:
                            systemConfig = new SystemConfig(module);
                            break;
                    }
                    config.systemConfigs.Add(systemConfig);
                }
               

                String Log4netConfig = config.SystemConfigs.SingleOrDefault(c => c.SystemName == "ConaxWorkflowManager").GetConfigParam("Log4NetConfig");
                var log4NetFile = new FileInfo(Log4netConfig);
                if (!log4NetFile.Exists)
                {
                    var path = new Uri(
                        System.IO.Path.GetDirectoryName(
                            System.Reflection.Assembly.GetExecutingAssembly().CodeBase)
                        ).LocalPath;
                    log4NetFile = new FileInfo(Path.Combine(path, "log4net.config"));
                }
                if (log4NetFile.Exists)
                    log4net.Config.XmlConfigurator.Configure(log4NetFile);


                foreach (XmlNode module in d.SelectNodes("CWMConfig/Tasks/Task"))
                {
                    var taskCfg = new TaskConfig(module);
                    config.taskConfigs.Add(taskCfg);
                }

                foreach (XmlNode module in d.SelectNodes("CWMConfig/CustomConfigurations/CustomConfiguration"))
                {
                    var customConfig = new CustomConfig(module);
                    config.customConfigs.Add(customConfig);
                }

                foreach (XmlNode module in d.SelectNodes("CWMConfig/WorkFlowConfigurations/WorkFlowConfiguration"))
                {
                    var workFlowConfig = new WorkFlowConfig(module);
                    config.workFlowConfigs.Add(workFlowConfig);
                }

                foreach (XmlNode module in d.SelectNodes("CWMConfig/IngestXMLConfigurations/IngestXMLConfiguration"))
                {
                    var ingestXMLConfig = new IngestXMLConfig(module);
                    config.ingestXMLConfigs.Add(ingestXMLConfig);
                }

                foreach (XmlNode module in d.SelectNodes("CWMConfig/ServiceConfigurations/ServiceConfiguration"))
                {
                    var serviceConfig = new ServiceConfig(module);
                    config.serviceConfigs.Add(serviceConfig);
                }

                AssureTimeZonelist();
            }
            catch (Exception ex)
            {
                String sSource = "ConaxWorkflowManager";
                if (!EventLog.SourceExists(sSource))
                    EventLog.CreateEventSource(sSource, "Application");

                EventLog.WriteEntry(sSource, "Failed to load config xml: " + ex.Message);
                throw;
            }
        }

        private static void AssureTimeZonelist()
        {

            String SystemTimeZoneList = "";
            try
            {
                var workflowConfig = Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == "ConaxWorkflowManager");
                SystemTimeZoneList = workflowConfig.GetConfigParam("SystemTimeZoneList");
            }
            catch (Exception ex)
            {
                return;
            }
            try
            {
                if (File.Exists(SystemTimeZoneList))
                    return;
                log.Debug("AssureTimeZonelist " + SystemTimeZoneList);

                String timezonexml = "<TimeZones>";
                foreach (TimeZoneInfo timezoneinfo in TimeZoneInfo.GetSystemTimeZones())
                {

                    timezonexml += "<TimeZone>";
                    timezonexml += "<ID>" + timezoneinfo.Id + "</ID>";
                    timezonexml += "<Name><![CDATA[" + timezoneinfo.DisplayName + "]]></Name>";
                    timezonexml += "</TimeZone>";
                }
                timezonexml += "</TimeZones>";
                XmlDocument timezoneDoc = new XmlDocument();
                timezoneDoc.LoadXml(timezonexml);

                XmlTextWriter writer = new XmlTextWriter(SystemTimeZoneList, null);
                writer.Formatting = Formatting.Indented;
                timezoneDoc.Save(writer);

            }
            catch (Exception ex)
            {
                log.Warn("Failed to AssureTimeZonelist.", ex);
            }
        }

        public static Config GetConfig()
        {
            if (config == null)
            {
                throw new ApplicationException("Config not initialized, call Init()");
            }
            return config;
        }

        public static ConaxWorkflowManagerConfig GetConaxWorkflowManagerConfig()
        {
            return (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.First(con => con.SystemName == SystemConfigNames.ConaxWorkflowManager);
        }
    }
}
