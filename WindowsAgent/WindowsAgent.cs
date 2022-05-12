using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;
using System.Security.Principal;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using Newtonsoft.Json.Linq;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using Microsoft.Win32;
using NetFwTypeLib;
using System.Collections.Generic;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;

namespace WindowsAgent
{
    class WindowsAgent
    {
        // private static IntPtr accountToken;
        private readonly Timer _timerOne;
        private readonly Timer _timerTwo;
        private static dynamic message = new JObject();
        private bool timerOneFlag = true;
        private static int customLineCount = 0;
        private static void GetUIDInfo()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("|-----------------UIDInformation-------------------|");
            Console.ForegroundColor = ConsoleColor.White;
            //---------------------------------HostName--------------------------------//
            string HostName = Dns.GetHostName();
            Console.WriteLine("Host Name of machine =" + HostName);
            IPAddress[] ipaddress = Dns.GetHostAddresses(HostName);
            message.UIDInfo = new JObject();
            message.UIDInfo.HostName = HostName;
            message.UIDInfo.IPv4 = new JObject();
            message.UIDInfo.IPv4.List = new JArray();
            message.UIDInfo.IPv6 = new JObject();
            message.UIDInfo.IPv6.List = new JArray();
            //---------------------------------IPaddress--------------------------------//
            Console.WriteLine("IPv4 of Machine : ");
            int i = 0;
            foreach (IPAddress ip4 in ipaddress.Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
            {
                i++;
                Console.WriteLine("  " + ip4.ToString());
                message.UIDInfo.IPv4.List.Add(ip4.ToString());
            }
            message.UIDInfo.IPv4.Count = i;
            i = 0;
            Console.WriteLine("IPv6 of Machine : ");
            foreach (IPAddress ip6 in ipaddress.Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6))
            {
                i++;
                Console.WriteLine("  " + ip6.ToString());
                message.UIDInfo.IPv6.List.Add(ip6.ToString());
            }
            message.UIDInfo.IPv6.Count = i;
            //---------------------------------OS-Version--------------------------------//
            var OsVersionInfo = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().Cast<ManagementObject>()
                        select x.GetPropertyValue("Caption")).FirstOrDefault();
            Console.WriteLine("OS version is "+ OsVersionInfo);
            message.UIDInfo.OSVersion = OsVersionInfo;
            //------------------------------PhysicalSystem--------------------------------//
            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            message.UIDInfo.PhysicalSystemInfo = new JObject();
            message.UIDInfo.PhysicalSystemInfo.List = new JArray();
            dynamic tempUIDPhysicalSystemInfo = new JObject();
            Console.WriteLine("Interface information for {0}.{1}     ",
                    computerProperties.HostName, computerProperties.DomainName);
            if (nics == null || nics.Length < 1)
            {
                Console.WriteLine("  No network interfaces found.");
                return;
            }
            message.UIDInfo.PhysicalSystemInfo.Count = nics.Length;
            Console.WriteLine("  Number of interfaces .................... : {0}", nics.Length);
            foreach (NetworkInterface adapter in nics)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties(); //  .GetIPInterfaceProperties();
                Console.WriteLine();
                Console.WriteLine(adapter.Description);
                tempUIDPhysicalSystemInfo.Description = adapter.Description;
                Console.WriteLine(String.Empty.PadLeft(adapter.Description.Length, '='));
                Console.WriteLine("  Interface type .......................... : {0}", adapter.NetworkInterfaceType);
                tempUIDPhysicalSystemInfo.InterfaceType = adapter.NetworkInterfaceType.ToString();
                tempUIDPhysicalSystemInfo.PhysicalAddress = "";
                Console.Write("  Physical address ........................ : ");
                PhysicalAddress address = adapter.GetPhysicalAddress();
                byte[] bytes = address.GetAddressBytes();
                for (i = 0; i < bytes.Length; i++)
                {
                    tempUIDPhysicalSystemInfo.PhysicalAddress += bytes[i].ToString("X2");
                    // Display the physical address in hexadecimal.
                    Console.Write("{0}", bytes[i].ToString("X2"));
                    // Insert a hyphen after each byte, unless we are at the end of the
                    // address.
                    if (i != bytes.Length - 1)
                    {
                        Console.Write("-");
                        tempUIDPhysicalSystemInfo.PhysicalAddress += "-";
                    }
                }
                message.UIDInfo.PhysicalSystemInfo.List.Add(tempUIDPhysicalSystemInfo);
                Console.WriteLine();
            }
            //-------------------------------VirtualSystem--------------------------------//
            
        }
        private static void GetBiosInformation()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("|-----------------BiosInformation------------------|");
            Console.ForegroundColor = ConsoleColor.White;
            if (OperatingSystem2.IsWindows)
            {
                message.BiosInfo = new JObject();
                SelectQuery query = new SelectQuery(@"Select * from Win32_ComputerSystem");

                //initialize the searcher with the query it is supposed to execute
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    //execute the query
                    foreach (ManagementObject process in searcher.Get())
                    {
                        //print system info
                        process.Get();
                        Console.WriteLine("{0}{1}", "System Manufacturer:", process["Manufacturer"]);
                        Console.WriteLine("{0}{1}", " System Model:", process["Model"]);
                        message.BiosInfo.SystemManufacturer = process["Manufacturer"];
                        message.BiosInfo.SystemModel = process["Model"];
                    }
                }
                ManagementObjectSearcher searcher1 = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
                ManagementObjectCollection collection = searcher1.Get();
                foreach (ManagementObject obj in collection)
                {
                    if (((string[])obj["BIOSVersion"]).Length > 1)
                    {
                        Console.WriteLine("BIOS VERSION: " + ((string[])obj["BIOSVersion"])[0] + " - " + ((string[])obj["BIOSVersion"])[1]);
                        message.BiosInfo.BiosVersion = ((string[])obj["BIOSVersion"])[0] + " - " + ((string[])obj["BIOSVersion"])[1];
                    }
                    else
                    {
                        Console.WriteLine("BIOS VERSION: " + ((string[])obj["BIOSVersion"])[0]);
                        message.BiosInfo.BiosVersion = ((string[])obj["BIOSVersion"])[0];
                    }
                }
            }
            else Console.WriteLine("Not supported on this OS...");
        }
        private static void GetDNSServerSearchOrder()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("|-----------------DNSServerSearchOrder-------------|");
            Console.ForegroundColor = ConsoleColor.White;
            if (OperatingSystem2.IsWindows)
            {
                ManagementScope oMs = new ManagementScope("\\\\localhost\\root\\cimv2");
                string strQuery = "select DNSServerSearchOrder from Win32_NetworkAdapterConfiguration where IPEnabled='true'";
                ObjectQuery oQ = new ObjectQuery(strQuery);
                ManagementObjectSearcher oS = new ManagementObjectSearcher(oMs, oQ);
                ManagementObjectCollection oRc = oS.Get();

                message.DNSServerSearchOrder = new JObject();
                message.DNSServerSearchOrder.DNSServerSearchList = new JArray();
                dynamic tempDNSServerInfo = new JObject();
                int i = 0;
                foreach (ManagementObject oR in oRc)
                {
                    i++;
                    Console.WriteLine(oR.Properties);
                    tempDNSServerInfo = oR.Properties.ToString();
                    message.FirewallInfo.FirewallList.Add(tempDNSServerInfo);
                }
                message.DNSServerSearchOrder.DNSServerSearchCount = i;
            }
            else Console.WriteLine("Not supported on this OS...");
        }
        private static IPAddress GetDefaultGateway()
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties().GatewayAddresses)
                .Select(g => g?.Address)
                .Where(a => a != null)
                // .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                // .Where(a => Array.FindIndex(a.GetAddressBytes(), b => b != 0) >= 0)
                .FirstOrDefault();
        }
        private static void DispDefaultIPGateway()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("|-----------------DefaultGateway-------------------|");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(GetDefaultGateway());
            message.DefaultIPGateway = GetDefaultGateway().ToString();
        }
        private static void GetServiceList()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("|-----------------ServiceList----------------------|");
            Console.ForegroundColor = ConsoleColor.White;
            ServiceController[] services = ServiceController.GetServices();
            message.ServiceInfo = new JObject();
            message.ServiceInfo.ServiceList = new JArray();
            message.ServiceInfo.ServiceCount = services.Length;
            dynamic tempServiceInfo = new JObject();
            foreach (ServiceController service in services)
            {
                //Console.WriteLine("Service Status : " + service.Status + " Service Name : " + service.ServiceName);
                tempServiceInfo.Status = service.Status.ToString();
                tempServiceInfo.ServiceName = service.DisplayName.ToString();
                message.ServiceInfo.ServiceList.Add(tempServiceInfo);
            }
            Console.WriteLine("Service Count : " + services.Length);

        }
        public static void GetActiveTcpConnections()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("|-----------------ActiveTCPConnections-------------|");
            Console.ForegroundColor = ConsoleColor.White;
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            message.ActiveTcpInfo = new JObject();
            message.ActiveTcpInfo.ActiveTcpList = new JArray();
            message.ActiveTcpInfo.ActiveTcpCount = connections.Length;
            dynamic tempActiveTcpInfo = new JObject();
            foreach (TcpConnectionInformation connection in connections)
            {
                //Console.WriteLine("{0} : {1} <==> {2}", connection.State.ToString() ,connection.LocalEndPoint.ToString(), connection.RemoteEndPoint.ToString());
                tempActiveTcpInfo.LocalEndPoint = connection.LocalEndPoint.ToString();
                tempActiveTcpInfo.RemoteEndPoint = connection.RemoteEndPoint.ToString();
                tempActiveTcpInfo.State = connection.State.ToString();
                message.ActiveTcpInfo.ActiveTcpList.Add(tempActiveTcpInfo);
            }
            Console.WriteLine("ActiveTcpConnections Count : " + connections.Length);
        }
        private static void GetProcessInformation()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("|-----------------ProcessInformation---------------|");
            Console.ForegroundColor = ConsoleColor.White;
            Process[] processes = Process.GetProcesses();
            message.ProcessListInfo = new JObject();
            message.ProcessListInfo.ProcessList = new JArray();
            message.ProcessListInfo.ProcessCount = processes.Length;
            dynamic tempProcessInfo = new JObject();
            Array.ForEach(processes, (process) => {
                // if(process.ProcessName!="svchost")
                //Console.WriteLine("Process: {0} Id: {1}", process.ProcessName, process.Id);
                tempProcessInfo.ProcessName = process.ProcessName.ToString();
                tempProcessInfo.ProcessId = process.Id.ToString();
                message.ProcessListInfo.ProcessList.Add(tempProcessInfo);
            });
            Console.WriteLine("Process Count : " + processes.Length);
        }
        private static void GetUserAccountsInfo()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("|-----------------UserAccountsInfo-----------------|");
            Console.ForegroundColor = ConsoleColor.White;
            ManagementObjectSearcher usersSearcher = new ManagementObjectSearcher(@"SELECT * FROM Win32_UserAccount");
            ManagementObjectCollection users = usersSearcher.Get();

            var localUsers = users.Cast<ManagementObject>().Where(
                u => (bool)u["LocalAccount"] == true &&
                     (bool)u["Disabled"] == false &&
                     (bool)u["Lockout"] == false &&
                     int.Parse(u["SIDType"].ToString()) == 1 &&
                     u["Name"].ToString() != "HomeGroupUser$");
            message.UserAccountsInfo = new JObject();
            message.UserAccountsInfo.UserAccountList = new JArray();            
            dynamic tempAccountInfo = new JObject();
            int i = 0;
            foreach (ManagementObject user in localUsers)
            // foreach (ManagementObject user in users)
            {
                i++;
                Console.WriteLine("Account Type: " + user["AccountType"].ToString());
                Console.WriteLine("Caption: " + user["Caption"].ToString());
                Console.WriteLine("Description: " + user["Description"].ToString());
                Console.WriteLine("Disabled: " + user["Disabled"].ToString());
                Console.WriteLine("Domain: " + user["Domain"].ToString());
                Console.WriteLine("Full Name: " + user["FullName"].ToString());
                Console.WriteLine("Local Account: " + user["LocalAccount"].ToString());
                Console.WriteLine("Lockout: " + user["Lockout"].ToString());
                Console.WriteLine("Name: " + user["Name"].ToString());
                Console.WriteLine("Password Changeable: " + user["PasswordChangeable"].ToString());
                Console.WriteLine("Password Expires: " + user["PasswordExpires"].ToString());
                Console.WriteLine("Password Required: " + user["PasswordRequired"].ToString());
                Console.WriteLine("SID: " + user["SID"].ToString());
                Console.WriteLine("SID Type: " + user["SIDType"].ToString());
                Console.WriteLine("Status: " + user["Status"].ToString());

                tempAccountInfo.AccountType = user["AccountType"].ToString();
                tempAccountInfo.Caption = user["Caption"].ToString();
                tempAccountInfo.Description = user["Description"].ToString();
                tempAccountInfo.Disabled = user["Disabled"].ToString();
                tempAccountInfo.Domain = user["Domain"].ToString();
                tempAccountInfo.FullName = user["FullName"].ToString();
                tempAccountInfo.LocalAccount = user["LocalAccount"].ToString();
                tempAccountInfo.Lockout = user["Lockout"].ToString();
                tempAccountInfo.Name = user["Name"].ToString();
                tempAccountInfo.PasswordChangeable = user["PasswordChangeable"].ToString();
                tempAccountInfo.PasswordExpires = user["PasswordExpires"].ToString();
                tempAccountInfo.PasswordRequired = user["PasswordRequired"].ToString();
                tempAccountInfo.SID = user["SID"].ToString();
                tempAccountInfo.SIDType = user["SIDType"].ToString();
                tempAccountInfo.Status = user["Status"].ToString();

                message.UserAccountsInfo.UserAccountList.Add(tempAccountInfo);
            }
            message.UserAccountsInfo.UserAccountCount = i;
        }
        private static void GetInstalledSoftInfo()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("|-----------------InstalledSoftInfo----------------|");
            Console.ForegroundColor = ConsoleColor.White;
            string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            message.InstalledSoftInfo = new JObject();
            message.InstalledSoftInfo.InstalledSoftList = new JArray();
            dynamic temSoftInfo = new JObject();
            int i = 0;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key))
            {
                foreach (string subkey_name in key.GetSubKeyNames())
                {
                    using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                    {
                        i++;
                        //Console.WriteLine("DisplayName: " + subkey.GetValue("DisplayName"));
                        //Console.WriteLine("Caption: " + subkey.GetValue("Caption"));
                        //Console.WriteLine("CSName: " + subkey.GetValue("CSName"));
                        //Console.WriteLine("Description: " + subkey.GetValue("Description"));
                        //Console.WriteLine("FixComments: " + subkey.GetValue("FixComments"));
                        //Console.WriteLine("HotFixID: " + subkey.GetValue("HotFixID"));
                        //Console.WriteLine("InstallDate: " + subkey.GetValue("InstallDate"));
                        //Console.WriteLine("InstalledBy: " + subkey.GetValue("InstalledBy"));
                        //Console.WriteLine("InstalledOn: " + subkey.GetValue("InstalledOn"));
                        //Console.WriteLine("Name: " + subkey.GetValue("Name"));
                        //Console.WriteLine("ServicePackInEffect: " + subkey.GetValue("ServicePackInEffect"));
                        //Console.WriteLine("Status: " + subkey.GetValue("Status"));

                        temSoftInfo.DisplayName = subkey.GetValue("DisplayName");
                        temSoftInfo.Caption = subkey.GetValue("Caption");
                        temSoftInfo.CSName = subkey.GetValue("CSName");
                        temSoftInfo.Description = subkey.GetValue("Description");
                        temSoftInfo.FixComments = subkey.GetValue("FixComments");
                        temSoftInfo.HotFixID = subkey.GetValue("HotFixID");
                        temSoftInfo.InstallDate = subkey.GetValue("InstallDate");
                        temSoftInfo.InstalledBy = subkey.GetValue("InstalledBy");
                        temSoftInfo.InstalledOn = subkey.GetValue("InstalledOn");
                        temSoftInfo.Name = subkey.GetValue("Name");
                        temSoftInfo.ServicePackInEffect = subkey.GetValue("ServicePackInEffect");
                        temSoftInfo.Status = subkey.GetValue("Status");

                        message.InstalledSoftInfo.InstalledSoftList.Add(temSoftInfo);
                    }
                }
            }
            message.InstalledSoftInfo.InstalledSoftCount = i;
            Console.WriteLine("InstalledSoftware Count : " + 1);
        }
        private static void GetFirewallInfo()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("|-----------------FirewallInfo---------------------|");
            Console.ForegroundColor = ConsoleColor.White;
            try
            {
                Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);
                var currentProfiles = fwPolicy2.CurrentProfileTypes;

                message.FirewallInfo = new JObject();
                message.FirewallInfo.FirewallList = new JArray();
                dynamic tempFirewallInfo = new JObject();
                int i = 0;
                foreach (INetFwRule rule in fwPolicy2.Rules)
                {
                    i++;
                    //Console.WriteLine("Rule Name: " + rule.Name);
                    //Console.WriteLine("Rule Action: " + rule.Action);
                    //Console.WriteLine("Rule Direction: " + rule.Direction);
                    //Console.WriteLine("Rule Protocol: " + rule.Protocol);
                    //Console.WriteLine("Rule Profiles: " + rule.Profiles);
                    //Console.WriteLine("Rule ApplicationName: " + rule.ApplicationName);
                    //Console.WriteLine("Rule Description: " + rule.Description);
                    //Console.WriteLine("Rule Enabled: " + rule.Enabled);

                    tempFirewallInfo.Name = rule.Name;
                    tempFirewallInfo.Action = rule.Action;
                    tempFirewallInfo.Direction = rule.Direction;
                    tempFirewallInfo.Protocol = rule.Protocol;
                    tempFirewallInfo.Profiles = rule.Profiles;
                    tempFirewallInfo.ApplicationName = rule.ApplicationName;
                    tempFirewallInfo.Description = rule.Description;
                    tempFirewallInfo.Enabled = rule.Enabled;

                    message.FirewallInfo.FirewallList.Add(tempFirewallInfo);
                }
                message.FirewallInfo.FirewallCount = i;
                Console.WriteLine("Firewall Count : " + 1);
            }
            catch (Exception r)
            {
                Console.WriteLine("Error deleting a Firewall rule" + r);
            }
        }
        private static void GetEventLogs()
        {
            Console.WriteLine("..................EventLogs..................");
            EventLog[] remoteEventLogs;

            remoteEventLogs = EventLog.GetEventLogs();
            message.EventLogs = new JObject();
            message.EventLogs.Application = new JObject();
            message.EventLogs.Application.List = new JArray();
            message.EventLogs.Security = new JObject();
            message.EventLogs.Security.List = new JArray();

            Console.WriteLine("Number of logs on computer: " + remoteEventLogs.Length);
            foreach (EventLog log in remoteEventLogs)
            {
                //Console.WriteLine("Log: " + log.Log + " ------------------------------------------------------ ");
                if (log.Log == "Application")
                {
                    dynamic tempEventLogsInfo = new JObject();
                    Console.WriteLine("Log: " + log.Log + " ------------------------------------------------------ ");
                    int i = 0;
                    foreach (EventLogEntry entry in log.Entries)
                    {
                        i++;
                        //Console.WriteLine("(UserName: " + entry.UserName + ")(CategoryNumber: " + entry.CategoryNumber + ")(TimeWritten: " + entry.TimeWritten
                        //    + ")(EventID: " + entry.EventID + ")(Category: " + entry.Category + ")(MachineName: " + entry.MachineName + ")");
                        tempEventLogsInfo.EventID = entry.EventID;
                        tempEventLogsInfo.TimeWritten = entry.TimeWritten;
                        tempEventLogsInfo.CategoryNumber = entry.CategoryNumber;
                        tempEventLogsInfo.Category = entry.Category;
                        tempEventLogsInfo.UserName = entry.UserName;
                        tempEventLogsInfo.MachineName = entry.MachineName;

                        message.EventLogs.Application.List.Add(tempEventLogsInfo);
                    }
                    message.EventLogs.Application.Count = i;
                    Console.WriteLine("ApplicationEvenLogs : " + i);
                }
                
            }
        }
        private void GetSystemInfo()
        {
            if (timerOneFlag)
            {
                timerOneFlag = false;
                _timerOne.Stop();
                _timerOne.Interval = Convert.ToInt32(360000); //1hour//
                _timerOne.Start();
            }
            GetUIDInfo();
            GetBiosInformation();
            /////// GetDNSServerSearchOrder();
            DispDefaultIPGateway();
            GetServiceList();
            GetActiveTcpConnections();
            GetProcessInformation();
            GetUserAccountsInfo();
            GetInstalledSoftInfo();
            GetFirewallInfo();
            //////GetEventLogs();
        }
        private void TimerOneElapsed(object sender, ElapsedEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("-----------------------------------------------------------------|");
            Console.ForegroundColor = ConsoleColor.White;
            GetSystemInfo();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("-----------------------------------------------------------------|");
            Console.ForegroundColor = ConsoleColor.White;

            string[] lines = new string[] { DateTime.Now.ToString() +"\n"+ message};
            File.AppendAllLines(@"C:\ProgramData\Red_Squirrel\WindowsAgent_PC_Info.json", lines);
        }
        private static void GetEventLog(string eventName)
        {
            EventLog appLogs = new EventLog(eventName);
            dynamic syslogInfo = new JObject();
            syslogInfo.UIDInfo = message.UIDInfo;
            syslogInfo.EventLogList = new JArray();
            var entries = appLogs.Entries.Cast<EventLogEntry>().Where(x => x.TimeWritten >= DateTime.Now.AddSeconds(-6)).ToList();
            dynamic tempSyslogInfo = new JObject();
            int i = 0;
            foreach (EventLogEntry entry in entries)
            {
                i++;
                if(eventName == "Application")    Console.ForegroundColor = ConsoleColor.Green;
                else if (eventName == "Security") Console.ForegroundColor = ConsoleColor.Red;
                else if (eventName == "System")   Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("{" + eventName + "}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(
                    "(UserName: " + entry.UserName + ")"
                    + "(CategoryNumber: " + entry.CategoryNumber + ")"
                    + "(TimeWritten: " + entry.TimeWritten + ")\n\t"
                    + "(EventID: " + entry.EventID + ")"
                    + "(Category: " + entry.Category + ")"
                    + "(MachineName: " + entry.MachineName + ")\n\t"
                    + "(TotalMessage: " + entry.Message + ")");

                tempSyslogInfo.EventID = entry.EventID;
                tempSyslogInfo.TimeWritten = entry.TimeWritten;
                tempSyslogInfo.CategoryNumber = entry.CategoryNumber;
                tempSyslogInfo.Category = entry.Category;
                tempSyslogInfo.UserName = entry.UserName;
                tempSyslogInfo.MachineName = entry.MachineName;
                tempSyslogInfo.TotalMessage = entry.Message;

                syslogInfo.EventLogList.Add(tempSyslogInfo);
            }
            syslogInfo.EventLogCount = i;
            
            if (i > 0)
            {
                //Console.ForegroundColor = ConsoleColor.Green;
                //Console.WriteLine("Output");
                //Console.ForegroundColor = ConsoleColor.White;
                string[] lines = new string[] { DateTime.Now.ToString() + "\n" + syslogInfo };
                File.AppendAllLines($@"C:\ProgramData\Red_Squirrel\WindowsAgent_EventLogs_{eventName}.json", lines);
            }
        }
        private static void GetCustomLog(string logFileLocation)
        {
            dynamic customLogInfo = new JObject();
            customLogInfo.UIDInfo = message.UIDInfo;
            customLogInfo.EventLogList = new JArray();
            
            //DateTime d = DateTime.Now;
            //var format = "MM-dd-yy HH:mm:ss";
            //var fileDates = File.ReadAllLines(logFileLocation)
            //                .Where(l => l.Length >= format.Length
            //                        && DateTime.TryParseExact(l.Substring(0, format.Length)
            //                                                , format
            //                                                , CultureInfo.InvariantCulture
            //                                                , DateTimeStyles.None
            //                                                , out d)
            //                )
            //                .Select(l => d)
            //                .OrderBy(dt => dt);
            //if (fileDates.Any())
            //{
            //    DateTime firstDate = fileDates.First();  // 2011-11-17 23:05:17,266
            //    DateTime lastDate = fileDates.Last();   // 2011-11-17 23:17:08,862
            //    //Console.WriteLine(firstDate + "  :::  " + lastDate);
            //}

            string[] lines = File.ReadAllLines(logFileLocation);
            int lineLength = lines.Length;
            if (lineLength > customLineCount)
            {
                for (int i = customLineCount; i < lineLength; i++)
                {
                    dynamic tempCustomLogInfo = new JObject();
                    // tempCustomLogInfo.time = DateTime.Now.ToString(); //<===================
                    // Console.WriteLine("+" + lines[i]);
                    // Console.WriteLine("+" + fileDates);
                    int start1 = lines[i].IndexOf('<');
                    int end1 = lines[i].IndexOf('>');
                    tempCustomLogInfo.totalMessage = lines[i];
                    if (start1 > 0 && end1 > 0)
                    {
                        //Console.WriteLine("++" + lines[i].Substring(start1 + 1, end1 - start1 - 1) + " || " + lines[i].Substring(end1 + 1));
                        tempCustomLogInfo.firstItem = lines[i].Substring(start1 + 1, end1 - start1 - 1);
                        string tempLine2 = lines[i].Substring(end1 + 1);
                        int start2 = tempLine2.IndexOf('<');
                        int end2 = tempLine2.IndexOf('>');
                        if(start2 > 0 && end2 > 0)
                        {
                            //Console.WriteLine("+++" + tempLine2.Substring(start2 + 1, end2 - start2 - 1) + " || " + tempLine2.Substring(end2 + 3));
                            tempCustomLogInfo.secondItem = tempLine2.Substring(start2 + 1, end2 - start2 - 1);
                            string tempLine3 = tempLine2.Substring(end2 + 3);
                            tempCustomLogInfo.thirdItem = tempLine3;
                        }
                        else
                        {
                            //Console.WriteLine("@@@@@@" + tempLine2.Substring(0));
                            tempCustomLogInfo.message2 = tempLine2.Substring(0);
                        }
                    }
                    else
                    {
                        //Console.WriteLine(lines[i].Substring(19));
                        tempCustomLogInfo.message = lines[i].Substring(19);
                    }
                    //Console.WriteLine(start1 + " : " + end1);
                    customLogInfo.EventLogList.Add(tempCustomLogInfo);
                }
                customLogInfo.EventLogCount = lineLength - customLineCount;
                string[] liness = new string[] { DateTime.Now.ToString() + "\n" + customLogInfo };
                File.AppendAllLines($@"C:\ProgramData\Red_Squirrel\WindowsAgent_CustomLog.json", liness);
                customLineCount = lineLength;
            }
        }
        private void TimerTwoElapsed(object sender, ElapsedEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[TimeStamp]");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" [" + DateTime.Now.ToString() + "]");

            Parallel.Invoke(
                () => GetEventLog("Application"),
                () => GetEventLog("Security"),
                () => GetEventLog("System"),
                () => GetCustomLog(@"C:\custom.log"));

            //GetEventLog("Application");
            //GetEventLog("Security");
        }
        public WindowsAgent()
        {
            _timerOne = new Timer(1000) { AutoReset = true };
            _timerOne.Elapsed += TimerOneElapsed;
            _timerTwo = new Timer(5000) { AutoReset = true };
            _timerTwo.Elapsed += TimerTwoElapsed;
        }
        public void Start()
        {
            string dir = @"C:\ProgramData\Red_Squirrel";
            // If directory does not exist, create it
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            if (!File.Exists(@"C:\ProgramData\Red_Squirrel\WindowsAgent_SetupDateTime.json"))
            {
                string[] lines = new string[] { DateTime.Now.ToString() };
                File.AppendAllLines(@"C:\ProgramData\Red_Squirrel\WindowsAgent_SetupDateTime.json", lines);
            }
            Console.WriteLine("Starting Agent...");
            _timerOne.Start();
            _timerTwo.Start();
        }
        public void Stop()
        {
            _timerOne.Stop();
            _timerTwo.Stop();
            //if (File.Exists(@"C:\WindowsAgent\WindowsAgent_SetupDateTime.json"))
            //{
            //    File.Delete(@"C:\WindowsAgent\WindowsAgent_SetupDateTime.json");
            //}
            
        }
    }
}
