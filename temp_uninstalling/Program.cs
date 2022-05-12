using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace temp_uninstalling
{
    class Program
    {
        static void Main(string[] args)
        {
            //ServiceController[] services = ServiceController.GetServices();
            //string Status, ServiceName;
            //foreach (ServiceController service in services)
            //{
            //    Status = service.Status.ToString();
            //    ServiceName = service.DisplayName.ToString();
            //    if (ServiceName == "Red Squirrel Windows Version")
            //    {
            //        service.Close();
            //    }
            //}

            string exe = @"rd /s /q C:\ProgramData\Red_Squirrel";
            var psi = new ProcessStartInfo();
            psi.CreateNoWindow = true; //This hides the dos-style black window that the command prompt usually shows
            psi.FileName = @"cmd.exe";
            psi.Verb = "runas"; //This is what actually runs the command as administrator
            psi.Arguments = "/C " + exe;
            try
            {
                var process = new Process();
                process.StartInfo = psi;
                process.Start();
                process.WaitForExit();
            }
            catch (Exception)
            {
                //If you are here the user clicked decline to grant admin privileges (or he's not administrator)
            }
        }
    }
}
