using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW
{
    public interface IMiddleWareRestApiCaller
    {
        CallStatus MakeGetCall(String objectToHandle, String id);

        CallStatus MakeAddCall(String objectToHandle, XmlDocument data);

        CallStatus MakeAddCall(String objectToHandle, String xmlString);

        CallStatus MakeUpdateCall(String objectToHandle, String id, XmlDocument data);

        CallStatus MakeUpdateCall(String objectToHandle, String id, String xmlString);

        CallStatus MakeDeleteCall(String objectToHandle, String id);
    }
}
