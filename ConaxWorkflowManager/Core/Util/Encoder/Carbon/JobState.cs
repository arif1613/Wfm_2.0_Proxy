using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Encoder;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Carbon
{
    /// <summary>
    /// This class contains the state of a encoding job, i.e. the ID of the Selected workflow, the Guid of the current job and the Guid of the Template that the current job used.
    /// </summary>
    public class JobState
    {
        /// <summary>
        /// The ID of the workflow that should be used for this encoding job.
        /// </summary>
        public String WorkFlowID;

        /// <summary>
        /// The Guid of the current Job that is running-
        /// </summary>
        public String CurrentJobGuid;

        /// <summary>
        /// The Guid of the template currently used in the workflow.
        /// </summary>
        public String CurrentTemplateGuidInWorkFlow;

        /// <summary>
        /// A list of templates already used in flow.
        /// </summary>
        public List<String> ListOfAlreadyUsedTemplates = new List<string>();

        /// <summary>
        /// Name of the previous job
        /// </summary>
        public String PreviousJobName;

        public IncludedWorkFlow PreviousWorkflow;

        /// <summary>
        /// TemplateEx Used For CurrentJob
        /// </summary>
        public String TemplateEx;

    }
}
