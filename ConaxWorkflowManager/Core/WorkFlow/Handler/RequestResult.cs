using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class RequestResult
    {
        public RequestResult() { }

        public RequestResult(RequestResultState state)
            : this(state, "", null)
        {
        }
        public RequestResult(RequestResultState state, Exception ex) : this(state, ex.Message, ex)
        {
            
        }

        public RequestResult(RequestResultState state, String message,  Exception ex)
        {
            State = state;
            Ex = ex;
            Message = message;
        }

        public RequestResult(RequestResultState state, String message) : this(state, message, null)
        {
        }

        public RequestResultState State { get; set; }
        public String Message { get; set; }
        public Exception Ex { get; set; }
    }
}
