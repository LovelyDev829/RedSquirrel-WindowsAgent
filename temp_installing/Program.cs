using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace temp_installing
{
    class Program
    {
        static void Main(string[] args)
        {
            // string cPath = @"E:\ME\NET_WORK\WorkinG\Deldric_agent-agentless_2000$\Project\WindowsAgent\temp_installing\bin\Debug";
            string cPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            string cParams = "install start";
            string filename = Path.Combine(cPath, "WindowsAgent.exe");
            Process.Start(filename, cParams);
        }
    }
}
