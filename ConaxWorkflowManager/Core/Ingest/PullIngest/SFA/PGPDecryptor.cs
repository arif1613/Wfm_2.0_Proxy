using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.PullIngest.SFA
{
    public class PGPDecryptor
    {
        //public static void DecryptAllGPGInFolder(string folder)
        //{
        //    {
        //        System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("cmd.exe");
        //        psi.CreateNoWindow = true;
        //        psi.UseShellExecute = false;
        //        psi.RedirectStandardInput = true;
        //        psi.RedirectStandardOutput = true;
        //        psi.RedirectStandardError = true;
        //        psi.WorkingDirectory = folder;

        //        System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);

        //        var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "SFAnytime").SingleOrDefault();
        //        string password = systemConfig.GetConfigParam("PgpPassword");

        //        string sCommandLine = "echo " + password + "| gpg --batch --passphrase-fd 0 *.gpg";

        //        process.StandardInput.WriteLine(sCommandLine);
        //        process.StandardInput.Flush();
        //        process.StandardInput.Close();
        //        process.WaitForExit();
        //        process.Close();
        //    }
        //}

        public static void DecryptAllGPGInFolder(string folder)
        {
            DecryptFile(folder, "*.gpg");
        }

        public static void DecryptFile(string folder, string filename)
        {
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("cmd.exe");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.WorkingDirectory = folder;

            System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);

            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "SFAnytime").SingleOrDefault();
            string password = systemConfig.GetConfigParam("PgpPassword");

            string sCommandLine = "echo " + password + "| gpg --batch --passphrase-fd 0 " + filename;

            process.StandardInput.WriteLine(sCommandLine);
            process.StandardInput.Flush();
            process.StandardInput.Close();
            process.WaitForExit();
            process.Close();
        }
    }
}
