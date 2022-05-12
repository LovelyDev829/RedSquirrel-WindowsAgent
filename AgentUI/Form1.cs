using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsAgent;
using System.Timers;
using System.Diagnostics;

namespace AgentUI
{
    public partial class Form1 : Form
    {
        private readonly System.Timers.Timer _timer;
        private static System.Timers.Timer aTimer;
        public Form1()
        {
            InitializeComponent();
            _timer = new System.Timers.Timer(500) { AutoReset = true };
            _timer.Elapsed += TimerElapsed;
        }
        private async void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (File.Exists(@"C:\ProgramData\Red_Squirrel\WindowsAgent_SetupDateTime.json"))
            {
                string date = File.ReadAllText(@"C:\ProgramData\Red_Squirrel\WindowsAgent_SetupDateTime.json");
                dateLabel.Text = date;
                _timer.Stop();
                await Task.Delay(30000);
                SetaTimer();
            }
        }
        private void SetaTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(500);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            switch (ServiceRunningCheck())
            {
                case "Running":
                    startBtn.Enabled = false;
                    stopBtn.Enabled = true;
                    uninstallBtn.Enabled = true;
                    break;
                case "Stopped":
                    startBtn.Enabled = true;
                    stopBtn.Enabled = false;
                    uninstallBtn.Enabled = true;
                    break;
                default:
                    startBtn.Enabled = true;
                    stopBtn.Enabled = false;
                    uninstallBtn.Enabled = false;
                    break;
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            _timer.Start();
            if (File.Exists(@"C:\ProgramData\Red_Squirrel\WindowsAgent_SetupDateTime.json"))
            {
                string date = File.ReadAllText(@"C:\ProgramData\Red_Squirrel\WindowsAgent_SetupDateTime.json");
                dateLabel.Text = date;
            }
            else
            {
                dateLabel.Text = "Not installed yet..";                
            }
            switch (ServiceRunningCheck())
            {
                case "Running":
                    startBtn.Enabled = false;
                    stopBtn.Enabled = true;
                    uninstallBtn.Enabled = true;
                    break;
                case "Stopped":
                    startBtn.Enabled = true;
                    stopBtn.Enabled = false;
                    uninstallBtn.Enabled = true;
                    break;
                default:
                    startBtn.Enabled = true;
                    stopBtn.Enabled = false;
                    uninstallBtn.Enabled = false;
                    break;
            }
        }
        private string ServiceRunningCheck()
        {
            ServiceController[] services = ServiceController.GetServices();
            string Status, ServiceName;
            foreach (ServiceController service in services)
            {
                Status = service.Status.ToString();
                ServiceName = service.DisplayName.ToString();
                if (ServiceName == "Red Squirrel Windows Version") return Status;
            }
            return "NotInstalled";
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if(ServiceRunningCheck() == "Stopped")
            {
                string cPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
                string cParams = "start";
                string filename = Path.Combine(cPath, "WindowsAgent.exe");
                Process.Start(filename, cParams);
            }
            else
            {
                string cPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
                string cParams = "install start";
                string filename = Path.Combine(cPath, "WindowsAgent.exe");
                Process.Start(filename, cParams);
            }
            startBtn.Enabled = false;
            stopBtn.Enabled = true;
            uninstallBtn.Enabled = true;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            string cPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            string cParams = "stop";
            string filename = Path.Combine(cPath, "WindowsAgent.exe");
            Process.Start(filename, cParams);

            startBtn.Enabled = true;
            stopBtn.Enabled = false;
            uninstallBtn.Enabled = true;
        }
        private void button3_Click(object sender, EventArgs e)
        {
            string cPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            string cParams = "uninstall";
            string filename = Path.Combine(cPath, "WindowsAgent.exe");
            Process.Start(filename, cParams);

            startBtn.Enabled = true;
            stopBtn.Enabled = false;
            uninstallBtn.Enabled = false;
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
