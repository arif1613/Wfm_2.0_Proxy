using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Envivio;
using System.ServiceModel;
using log4net;
using System.Reflection;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication
{
    /// <summary>
    /// This wrapper helps with the communication towards Envivio 4Balencer
    /// </summary>
    public class Envivio4BalancerServicesWrapper
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        ServicePortTypeClient client = new ServicePortTypeClient();

        public Envivio4BalancerServicesWrapper()
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "EnvivioEncoder").SingleOrDefault();
            String endpoint = systemConfig.GetConfigParam("Endpoint");
            log.Debug("<------------------------------------------------------------------>");
            log.Debug("Setting endpoint for envivio encoder to " + endpoint + ", previous endpoint was " + client.Endpoint.Address.Uri);
            client.Endpoint.Address = new EndpointAddress(new Uri(endpoint), client.Endpoint.Address.Identity, client.Endpoint.Address.Headers);
        }

        public bool UploadMezzanineFiles()
        {
            throw new Exception("Not Implemented");
        }

        /// <summary>
        /// This starts an encoding job.
        /// </summary>
        /// <param name="presetID">This states what preset that should be used</param>
        /// <param name="jobParameters">A list of job parameters</param>
        /// <param name="jobName">The name of the job, for example content name</param>
        /// <returns>If successful the ID of the started job is returned, othervise "" is returned.</returns>
        public String LaunchEncodingJob(String presetID, List<JobParameter> jobParameters, String jobName)
        {

            Envivio.param[] parameters = new Envivio.param[jobParameters.Count];
            int i = 0;
            foreach (JobParameter p in jobParameters)
            {
                Envivio.param parameter = new Envivio.param();
                parameter.name = p.Name;
                parameter.value = p.Value;
                parameters[i] = parameter;
                i++;
            }
            
            String jobID = client.launchJob(presetID, parameters, jobName);

            return jobID;
        }

        /// <summary>
        /// This method checks the status of a job.
        /// </summary>
        /// <param name="jobID">The ID of the job to check status for</param>
        /// <returns>A EncodingJobStatus object containing the status of the job and for some status it also contains info.</returns>
        public EncodingJobStatus GetJobStatus(String jobID)
        {
            jobstatusinfo[] infos;
            String reply = client.getJobStatus(jobID, out infos);
            EncodingJobStatus jobStatus = new EncodingJobStatus();
            jobStatus.JobStatus = (JobStatus)Enum.Parse(typeof(JobStatus), reply);
            jobStatus.Infos = new List<JobInfo>();
            foreach (jobstatusinfo i in infos)
            {
                jobStatus.Infos.Add(new JobInfo() { Name = i.name, Value = i.value });
            }
            return jobStatus;
        }

        /// <summary>
        /// This method cancels an encoding job.
        /// </summary>
        /// <param name="jobID">The ID of the job to cancel</param>
        /// <returns></returns>
        public void CancelEncodingJob(String jobID)
        {
            client.cancelJob(jobID);
        }
    }

    public class JobParameter
    {

        public String Name { get; set; }

        public String Value { get; set; }

    }

    public class EncodingJobStatus
    {
        public JobStatus JobStatus { get; set; }

        public List<JobInfo> Infos { get; set; }
    }

    public enum JobStatus
    {
        created,
        initializing,
        evaluating,
        evaluated,
        dispatching,
        dispatched,
        queued,
        running,
        canceling,
        canceled,
        success,
        error,
        retrying,
        delayed
    }

    public class JobInfo
    {
        public String Name { get; set; }

        public String Value { get; set; }
    }
}
