using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public delegate void ProgressChangeDelegate(double Persentage, ref bool Cancel);

    public delegate void Completedelegate();

    internal class FileMoveProcess
    {
        public string SourceFilePath { get; set; }
        public string DestFilePath { get; set; }
        public event ProgressChangeDelegate OnProgressChanged;
        public event Completedelegate OnComplete;

        public FileMoveProcess(string Source, string Dest)
        {
            SourceFilePath = Source;
            DestFilePath = Dest;
            OnProgressChanged += delegate { };
            OnComplete += delegate { };
            filemover();
        }

        public void filemover()
        {
            System.Threading.Tasks.Task t = new System.Threading.Tasks.Task(Copy);
            t.Start();
            t.Wait();
        }

        public void Copy()
        {
            byte[] buffer = new byte[100 * 1024]; // 100 MB buffer
            bool cancelFlag = false;

            using (FileStream source = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read))
            {
                //    long fileLength = source.Length;
                //    using (FileStream dest = new FileStream(DestFilePath, FileMode.CreateNew, FileAccess.Write))
                //    {
                //        long totalBytes = 0;
                //        int currentBlockSize = 0;

                //        while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
                //        {
                //            totalBytes += currentBlockSize;
                //            double persentage = (double)totalBytes * 100.0 / fileLength;
                //            dest.Write(buffer, 0, currentBlockSize);
                //            cancelFlag = false;
                //            OnProgressChanged(persentage, ref cancelFlag);
                //        }
                //        dest.Close();
                //    }
                //source.Close();
                //}

                //OnComplete();
                if (File.Exists(DestFilePath))
                {
                    File.Delete(DestFilePath);
                }
                File.Move(SourceFilePath, DestFilePath);

            }

        }
    }
}
