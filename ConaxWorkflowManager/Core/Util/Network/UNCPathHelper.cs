using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using log4net;
using System.Reflection;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Network
{
    /// <summary>
    /// This class helps with managing UNC paths, for example open up UNC paths that needs a username and password and are not part of an domain
    /// </summary>
    public class UNCPathHelper
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private String UserName = "";

        private String PassWord = "";

        private String UNCPath = "";

        private Process p = null;

        public UNCPathHelper(String userName, String passWord)
        {
            UserName = userName;
            PassWord = passWord;
        }

        public void UnlockUNCPath(String UNCPathToUnlock)
        {
            try
            {
                log.Debug("Opening path");
                log.Debug(@"use \\" + UNCPathToUnlock + @" password /USER:" + UserName + @"\" + PassWord);
               // p = Process.Start("net.exe", "use " + UNCPathToUnlock + @" password /USER:" + UserName + @"\" + PassWord);
                Process p = new Process();
                p.StartInfo.FileName = "net.exe";
                p.StartInfo.Arguments = @"use " + UNCPathToUnlock + @" password /USER:" + UserName + @"\" + PassWord;
                //  p.WaitForExit();
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.UseShellExecute = false;
                p.Start();
                string stdoutx = p.StandardOutput.ReadToEnd();
                string stderrx = p.StandardError.ReadToEnd();
                log.Debug("message from process: " + stdoutx);
                log.Debug("message from process: " + stderrx);
                log.Debug("Path opened");
            }
            catch (Exception exc)
            {
                log.Error("Error when opening UNCPath", exc);
                throw;
            }
        }

        public void CloseUNCPath()
        {
            try
            {
                if (p != null)
                {
                    p.Close();
                    log.Debug("Path closed");
                }
            }
            catch (Exception exc)
            {
                log.Error("Error when closing UNCPath", exc);
                throw;
            }
        }
    }
}
