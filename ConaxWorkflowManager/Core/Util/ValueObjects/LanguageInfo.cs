using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    public class LanguageInfo
    {
        public LanguageInfo() {
            Images = new List<Image>();
        }

        // 3 letters ISO CODE
        public String ISO { get; set; }        
        public String Title { get; set; }
        public String SortName { get; set; }
        public String ShortDescription { get; set; }
        public String LongDescription { get; set; }
        public List<Image> Images { get; set; }
        public String SubtitleURL { get; set; }
        //public String ResourceA { get; set; }
        //public String ResourceB { get; set; }
    }
}

