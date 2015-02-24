using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.CarbonEncoder;
using System.ServiceModel;
using System.Reflection;
using log4net;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Encoder;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication
{
    
    public class CarbonVodEncoderWrapper
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private WfcJmServicesClient client = null;

        public CarbonVodEncoderWrapper()
        {
            client = new WfcJmServicesClient("JmHttpEndpoint");
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "CarbonEncoder").SingleOrDefault();
            String endpoint = "";
            if (systemConfig.ConfigParams.ContainsKey("Endpoint"))
            {
                endpoint = systemConfig.GetConfigParam("Endpoint");
            }
            if (!String.IsNullOrEmpty(endpoint))
                client.Endpoint.Address = new EndpointAddress(new Uri(endpoint), client.Endpoint.Address.Identity, client.Endpoint.Address.Headers);
        }

        /// <summary>
        /// This method starts a job using the supplied templateGuid, inputfile and outputfolder.
        /// </summary>
        /// <param name="profileGuid">The guid of the profile</param>
        /// <param name="inputFile">The source file</param>
        /// <param name="outPutFolder">The folder that the encoded files should end up.</param>
        /// <returns>The queued job</returns>
        public Job GenerateJob(String profileGuid, String inputFile, String outPutFolder)
        {
            if (!outPutFolder.EndsWith(@"\"))
                outPutFolder += @"\";
            Job encoderJob = null;
            try
            {
                
                encoderJob = client.GenerateJobByWorkflowGuidAndType(new Guid(profileGuid), inputFile, outPutFolder, SourceFileType.SingleFile);
                Job queuedJob = client.QueueJob(encoderJob);
            }
            catch (Exception ex)
            {
                log.Error("Error Starting job", ex);
            }

            return encoderJob;
        }

        /// <summary>
        /// This method starts a job using the supplied templateguid and templateEx
        /// </summary>
        /// <param name="profileGuid">The guid of the profile</param>
        /// <param name="templateEx">The xml containing the information needed for the job, for example inputfile, id for encryption</param>
        /// <param name="outputFolder">The outputFolder</param>
        /// <returns>The queued job</returns>
        public Job GenerateJob(IncludedWorkFlow workFlow, String templateEx, String outputFolder, bool pushToOrigin, bool isLastJob)
        {
            Job encoderJob = null;
            try
            {
                log.Debug("Fetching template for guid = " + workFlow.WorkFlowGuid);
                WorkflowTemplateObject template = client.GetWorkflowTemplateById(new Guid(workFlow.WorkFlowGuid));
                String templateConfig = template.Config;

                if (workFlow.UseTempFolderForOutput)
                    outputFolder = workFlow.TempFolder;
                if (isLastJob && pushToOrigin)
                {
                    log.Debug("is last job and pushtoharmonic, not setting outputfolder");
                }
                else
                {
                    log.Debug("Setting outputFolder");
                    templateConfig = SetOutputFolder(templateConfig, outputFolder);
                }
                log.Debug(Environment.NewLine);
                log.Debug("<---------------------------------------------------------------->");
                log.Debug("Template fetched, config= " + template.Config);
                log.Debug("<---------------------------------------------------------------->");
                encoderJob = client.GenerateTemplateExJobByWorkflow(templateEx, templateConfig);
                Job queuedJob = client.QueueJob(encoderJob);
            }
            catch (Exception ex)
            {
                log.Error("Error Starting job", ex);
                throw;
            }

            return encoderJob;
        }

        private String SetOutputFolder(String templateConfigString, String outputFolder)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(templateConfigString);
            XmlNodeList outputFolderNodes = doc.SelectNodes("WorkflowTasks/TransformTaskSet/TranscodeTargetSet/Target/Path");
            foreach (XmlNode pathNode in outputFolderNodes)
            {
                pathNode.InnerText = outputFolder;
            }
            XmlNodeList packageNodes = doc.SelectNodes("WorkflowTasks/TransformTaskSet/PackageTargetSet/PackageTarget/Path");
            foreach (XmlNode node in packageNodes)
                node.InnerText = outputFolder;

            XmlNode reportNode = doc.SelectSingleNode("WorkflowTasks/JobEndTaskSet/CompletionTaskSet/ReportTask/ReportOutputPath");
            if (reportNode != null)
                reportNode.InnerText = outputFolder;

            return doc.OuterXml;
        }

       
        public Job GetJob(String jobGuid)
        {
            Job job = null;
            try
            {

                job = client.GetJob(new Guid(jobGuid), false);
            }
            catch (Exception ex)
            {
                log.Error("Error Starting job", ex);
            }

            return job;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobGui"></param>
        /// <returns></returns>
        public CarbonEncoder.Job CheckJobStatus(Guid jobGui)
        {
            Job job = client.GetJob(jobGui, false);
            return job;
        }
    }
}
