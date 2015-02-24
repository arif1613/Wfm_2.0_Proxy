using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    public enum MaturityRating
    {
        /// <summary>
        /// General Audience
        /// </summary>
        G,
        /// <summary>
        /// Parental Guidance
        /// </summary>
        PG, 
        /// <summary>
        /// Over 18
        /// </summary>
        A, 
        /// <summary>
        /// Erotic
        /// </summary>
        X,
        /// <summary>
        /// Erotic, PIN always required
        /// </summary>
        XXX
    }
}
