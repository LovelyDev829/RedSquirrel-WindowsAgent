using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Topshelf;

namespace WindowsAgent
{
    class Program
    {
        public static void Main(string[] args)
        {
            var exitCode = HostFactory.Run(x =>
            {
                x.Service<WindowsAgent>(s => {
                    s.ConstructUsing(windowsagent => new WindowsAgent());
                    s.WhenStarted(windowsagent => windowsagent.Start());
                    s.WhenStopped(windowsagent => windowsagent.Stop());
                });
                x.RunAsLocalSystem();

                x.SetServiceName("RedSquirrel");
                x.SetDisplayName("Red Squirrel Windows Version");
                x.SetDescription("This is a windows agent developed by Dmitriy for Deldric in April 2022." +
                    "It gets System information and event logs of this personal computer and sends them to a certain network server.");
            });

            int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
            Environment.ExitCode = exitCodeValue;
            //StartAgentClass.StartAgent();
        }
    }
}
