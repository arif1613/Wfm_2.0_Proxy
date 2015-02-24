using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup
{
    public class NPVRRecording
    {
        public NPVRRecording() {
            Sources = new List<NPVRRecordingSource>();
        }

        public Int32? EpgId { get; set; }
        public Int32? Id { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public List<NPVRRecordingSource> Sources { get; set; }        
        public String EPGExternalID { get; set; }
        public NPVRRecordStateInCubiware RecordState { get; set; }
    }
    
    public class NPVRRecordingSource {

        public NPVRRecordingSource() {
            Device = DeviceType.NotSpecified;
        }

        public DeviceType Device { get; set; }
        public String Url { get; set; }
    }
}
