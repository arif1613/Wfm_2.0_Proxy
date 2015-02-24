using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File
{
    public class RuntimeInfoFile
    {
        public ulong lastFetchedID;

        public void Save()
        {
            using (TextWriter textWriter = new StreamWriter(Config.GetConfig().RuntimeInfoFileLocation + "RuntimeInfoFile.xml"))
            {
                var serializer = new XmlSerializer(typeof(RuntimeInfoFile));
                serializer.Serialize(textWriter, this);
            }
        }

        public static RuntimeInfoFile Load()
        {
            try
            {
                using (TextReader textReader = new StreamReader(Config.GetConfig().RuntimeInfoFileLocation + "RuntimeInfoFile.xml"))
                {
                    var serializer = new XmlSerializer(typeof(RuntimeInfoFile));
                    return serializer.Deserialize(textReader) as RuntimeInfoFile;
                }
            }
            catch
            {
                return new RuntimeInfoFile();
            }
        }
    }
}
