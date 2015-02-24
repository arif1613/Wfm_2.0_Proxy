using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.TestData.Services.Cubi
{
    public class FakeMiddleWareRestApiCaller : IMiddleWareRestApiCaller
    {
        public Dictionary<String, String>  DB = new Dictionary<String, String>();

        public void ClearMemory()
        {
            DB = new Dictionary<String, String>();
        }

        public CallStatus MakeGetCall(string objectToHandle, string id)
        {
            String res = GetTestData(objectToHandle + "_" + id + ".xml");
            CallStatus call = new CallStatus();
            call.Data = res;
            call.Success = true;
            return call;
        }

        public ConaxWorkflowManager.Core.Communication.CallStatus MakeAddCall(string objectToHandle, System.Xml.XmlDocument data)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Communication.CallStatus MakeAddCall(string objectToHandle, string xmlString)
        {
            throw new NotImplementedException();
        }

        public CallStatus MakeUpdateCall(String objectToHandle, String id, XmlDocument data)
        {
            String key = objectToHandle + "/" + id;
            if (DB.ContainsKey(key))
                DB[key] = data.InnerXml;
            else
                DB.Add(key, data.InnerXml);

            CallStatus res = new CallStatus();
            res.Success = true;

            return res;
        }

        public ConaxWorkflowManager.Core.Communication.CallStatus MakeUpdateCall(string objectToHandle, string id, string xmlString)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Communication.CallStatus MakeDeleteCall(string objectToHandle, string id)
        {
            throw new NotImplementedException();
        }

        private String GetTestData(String fileName)
        {
            fileName = GetFileName(fileName);

            String appPath;
            appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            appPath = appPath.Replace(@"bin\Debug", @"Core\TestData\Services\Cubi\Data").Replace(@"file:\", "");

            String dataPath = Path.Combine(appPath, fileName);
            if (!File.Exists(dataPath))
                return "";

            XmlDocument confDoc = new XmlDocument();
            confDoc.Load(dataPath);


            return confDoc.InnerXml;
        }


        private String GetFileName(String fileName)
        {
            if (fileName.StartsWith("npvr_recordings")) {

                Match match = Regex.Match(fileName,
                                    @"page=\d+",
                                    RegexOptions.IgnoreCase);
                
                return "npvr_recordings_" + match.Value.Split('=')[1] + ".xml";
            }
            return fileName;
        }
    }
}
