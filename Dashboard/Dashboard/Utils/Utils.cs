using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net.NetworkInformation;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dashboard
{
    /// <summary>
    /// Represents the compination of a network card and it's ip address. Can have multiple Network names if it has an IP V4 and V6
    /// </summary>
    public struct NetworkInformation
    {
        public string NetworkName;
        public IPAddress @IPAddress;
    }
    /// <summary>
    /// A Utility class for important static methods and global fields
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// A refrence from MainWindow, the log output for the console
        /// </summary>
        private static TextBox ConsoleLogOutput = null;
        /// <summary>
        /// The Log Filestream for writing console messages to disk
        /// </summary>
        private static FileStream ConsoleLogStream = null;
        /// <summary>
        /// A refrence from MainWindow, the log output for the robot
        /// </summary>
        private static TextBox RobotLogOutput = null;
        /// <summary>
        /// The Log Filestream for writing robot messages to disk
        /// </summary>
        private static FileStream RobotLogSteam = null;
        /// <summary>
        /// The IP address of the robot
        /// </summary>
        public static string RobotIPV6Address = "";
        public static string RobotIPV4Address = "";
        /// <summary>
        /// The IP address of the computer
        /// </summary>
        public static string ComputerIPV6Address = "";
        public static string ComputerIPV4Address = "";
        /// <summary>
        /// The IP endpoint of the robot listener
        /// </summary>
        private static IPEndPoint RobotRecieverIPEndPoint = null;
        /// <summary>
        /// The IP endpoint of the robot sender
        /// </summary>
        private static IPEndPoint RobotSenderIPEndPoint = null;
        /// <summary>
        /// The UDP listener client of the robot
        /// </summary>
        private static UdpClient RobotListenerClient = null;
        /// <summary>
        /// The UDP sender client to the robot
        /// </summary>
        private static UdpClient RobotSenderClient = null;
        /// <summary>
        /// The background thread for listening for messages from the robot
        /// </summary>
        private static BackgroundWorker RobotListener = null;
        /// <summary>
        /// The port used for listening for robot events
        /// </summary>
        public const int RobotListenerPort = 42424;
        /// <summary>
        /// The port used for sending for robot events
        /// </summary>
        public const int RobotSenderPort = 24242;
        private static object locker = new object();
        private static bool DashboardRecievedRobotData = false;
        private static Thread InitSenderThread = null;
        private static BackgroundWorker DNSGetter = null;
        /// <summary>
        /// Initializes the Logging functions of the application
        /// </summary>
        /// <param name="logOutput">The Textbox from the MainWindow</param>
        public static void InitLogging(MainWindow @MainWindow)
        {
            ConsoleLogOutput = MainWindow.ConsoleLogOutput;
            RobotLogOutput = MainWindow.RobotLogOutput;
            ConsoleLogStream = new FileStream("Dashboard.log", FileMode.Append, FileAccess.Write);
            RobotLogSteam = new FileStream("Robot.log", FileMode.Append, FileAccess.Write);
        }
        /// <summary>
        /// Logs a string value to the console log output in the log tab, and to the console log file
        /// </summary>
        /// <param name="textToLog">The text to log</param>
        /// <param name="debug">For use later, specify this to get a DEBUG header for that line</param>
        public static void LogConsole(string textToLog, bool debug = false)
        {
            string dateTimeFormat = string.Format("{0:yyyy-MM-dd:HH-mm-ss.fff}", DateTime.Now);
            if(debug)
            {
                textToLog = "DEBUG: " + textToLog;
            }
            //using \r\n allows for windows line endings
            textToLog = string.Format("{0}   {1}\r\n",dateTimeFormat,textToLog);
            ConsoleLogOutput.AppendText(textToLog);
            ConsoleLogStream.Write(Encoding.UTF8.GetBytes(textToLog), 0, Encoding.UTF8.GetByteCount(textToLog));
            ConsoleLogStream.Flush();
        }
        /// <summary>
        /// Logs a string value to the robot log output in the log tab, and to the robot log file
        /// </summary>
        /// <param name="textToLog">The text to log</param>
        /// <param name="debug">For use later, specify this to get a DEBUG header for that line</param>
        public static void LogRobot(string textToLog, bool debug = false)
        {
            string dateTimeFormat = string.Format("{0:yyyy-MM-dd:HH-mm-ss.fff}", DateTime.Now);
            if (debug)
            {
                textToLog = "DEBUG: " + textToLog;
            }
            textToLog = string.Format("{0}   {1}\r\n", dateTimeFormat, textToLog);
            RobotLogOutput.AppendText(textToLog);
            RobotLogSteam.Write(Encoding.UTF8.GetBytes(textToLog), 0, Encoding.UTF8.GetByteCount(textToLog));
            RobotLogSteam.Flush();
        }
        /// <summary>
        /// Starts the Listener for netowrk log packets from the robot
        /// </summary>
        public static void StartRobotNetworking()
        {
            LogConsole("Checking for any local internet connection...");
            //https://stackoverflow.com/questions/6803073/get-local-ip-address
            //check if we even have an online connectoin
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                LogConsole("ERROR: No valid network connectoins exist!");
                return;
            }
            else
            {
                LogConsole("At leaset one valid network connections exist!");
            }
            //select which ip address of this pc to use
            //https://stackoverflow.com/questions/6803073/get-local-ip-address
            //https://stackoverflow.com/questions/9855230/how-do-i-get-the-network-interface-and-its-right-ipv4-address
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            List<NetworkInformation> V6NetworkInfos = new List<NetworkInformation>();
            List<NetworkInformation> V4NetworkInfos = new List<NetworkInformation>();
            foreach (NetworkInterface ni in interfaces)
            {
                if ((ni.OperationalStatus == OperationalStatus.Up)
                    && (!ni.Description.Contains("VMware"))
                    && (!ni.Description.Contains("VirtualBox"))
                    && (!ni.Description.Contains("Tunnel"))
                    && (!ni.Description.Contains("Loopback")))
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            LogConsole(string.Format("Valid V6 network found: name={0}, address={1}", ni.Description, ip.Address.ToString()));
                            V6NetworkInfos.Add(new NetworkInformation { NetworkName = ni.Description, IPAddress = ip.Address });
                        }
                        else if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            LogConsole(string.Format("Valid V4 network found: name={0}, address={1}", ni.Description, ip.Address.ToString()));
                            V4NetworkInfos.Add(new NetworkInformation { NetworkName = ni.Description, IPAddress = ip.Address });
                        }
                    }
                }
            }
            LogConsole("Hard-code select index 0", true);
            ComputerIPV6Address = V6NetworkInfos[0].IPAddress.ToString();
            ComputerIPV4Address = V4NetworkInfos[0].IPAddress.ToString();
            LogConsole("Computer IPV6 address set to " + ComputerIPV6Address);
            LogConsole("Computer IPV4 address set to " + ComputerIPV4Address);
            LogConsole("Pinging robot hostname for ip address(s)");
            //ping the robot to check if it's on the network, if it is get it's ip address
            Ping p = new Ping();
            p.PingCompleted += OnPingCompleted;
            p.SendAsync("minwinpc", null);
            LogRobot("Dashboard: waiting for robot...");
        }
        /// <summary>
        /// Event hander for when the async ping completes
        /// </summary>
        /// <param name="sender">The ping sent</param>
        /// <param name="e">ping args</param>
        private static void OnPingCompleted(object sender, PingCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                LogConsole("ERROR, failed to get ip address of robot, (is it online?). The application cannot continue");
                LogConsole(e.Error.ToString());
                LogRobot("Dashboard: Robot not found");
                return;
            }
            else
            {
                LogConsole("Ping host name IP address ping SUCCESS, getting IP v4 and v6 addresses");
                using (DNSGetter = new BackgroundWorker() { WorkerReportsProgress = true })
                {
                    DNSGetter.RunWorkerCompleted += OnDNSParsed;
                    DNSGetter.DoWork += GetDNSName;
                    DNSGetter.ProgressChanged += OnDNSLog;
                    DNSGetter.RunWorkerAsync();
                }
            }
        }

        private static void OnDNSLog(object sender, ProgressChangedEventArgs e)
        {
            LogConsole((string)e.UserState);
        }

        private static void GetDNSName(object sender, DoWorkEventArgs e)
        {
            bool IPV6Parsed = false;
            bool IPV4Parsed = false;
            while (!IPV6Parsed && !IPV4Parsed)
            {
                foreach (IPAddress ip in Dns.GetHostAddresses("minwinpc"))
                {
                    if (ip.AddressFamily == AddressFamily.InterNetworkV6 && !IPV6Parsed)
                    {
                        //LogConsole("Parsed IPV6 address: " + ip.ToString());
                        DNSGetter.ReportProgress(0, "Parsed IPV6 address: " + ip.ToString());
                        RobotIPV6Address = ip.ToString();
                        IPV6Parsed = true;
                    }
                    else if (ip.AddressFamily == AddressFamily.InterNetwork && !IPV4Parsed)
                    {
                        //LogConsole("Parsed IPV4 address: " + ip.ToString());
                        DNSGetter.ReportProgress(0, "Parsed IPV4 address: " + ip.ToString());
                        RobotIPV4Address = ip.ToString();
                        IPV4Parsed = true;
                    }
                }
            }
        }

        private static void OnDNSParsed(object sender, RunWorkerCompletedEventArgs e)
        {
            //bind the robot socket and start the background listener
            LogRobot("Dashboard: Robot found, binding socket and listening for events");
            //setup and bind listenr
            LogConsole("Binding robot listener to address " + ComputerIPV4Address);
            RobotRecieverIPEndPoint = new IPEndPoint(IPAddress.Parse(ComputerIPV4Address), RobotListenerPort);
            RobotListenerClient = new UdpClient();
            RobotListenerClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            RobotListenerClient.Client.Bind(RobotRecieverIPEndPoint);
            using (RobotListener = new BackgroundWorker() { WorkerReportsProgress = true })
            {
                RobotListener.DoWork += ListenForEvents;
                RobotListener.ProgressChanged += OnRecievedData;
                RobotListener.RunWorkerAsync();
            }
            //setup and bind sender
            LogConsole("Binding robot sender to address " + RobotIPV4Address);
            RobotSenderIPEndPoint = new IPEndPoint(IPAddress.Parse(RobotIPV4Address), RobotSenderPort);
            RobotSenderClient = new UdpClient();
            RobotSenderClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //RobotSenderClient.Connect(RobotRecieverIPEndPoint);//DEBUG
            RobotSenderClient.Connect(RobotSenderIPEndPoint);
            LogConsole("Setting up dashboard send test, sends data recieved from reciever...");
            InitSenderThread = new Thread(new ThreadStart(InitRobotListener));
            InitSenderThread.Start();
        }

        private static void ListenForEvents(object sender, DoWorkEventArgs e)
        {
            while(true)
            {
                string result = Encoding.UTF8.GetString(RobotListenerClient.Receive(ref RobotRecieverIPEndPoint));
                RobotListener.ReportProgress(0, result);
            }
        }

        private static void OnRecievedData(object sender, ProgressChangedEventArgs e)
        {
            if (!DashboardRecievedRobotData)
            {
                LogConsole("Dashboard recieved first data from robot, comms link successfully established");
                InitSenderThread.Abort();
                DashboardRecievedRobotData = true;
            }
            else
            {
                LogRobot((string)e.UserState);
            }
        }

        private static void InitRobotListener()
        {
            //sends data to the robot ip address untill it responds
            while(true)
            {
                RobotSenderClient.Send(Encoding.UTF8.GetBytes(ComputerIPV4Address), Encoding.UTF8.GetByteCount(ComputerIPV4Address));
                Thread.Sleep(500);
            }
        }

        public static void AbortThreads()
        {
            if (InitSenderThread != null)
            {
                InitSenderThread.Abort();
                InitSenderThread = null;
            }
            if(RobotListener != null)
            {
                RobotListener.Dispose();
                RobotListener = null;
            }
            if(DNSGetter != null)
            {
                DNSGetter.Dispose();
                DNSGetter = null;
            }
        }
    }
}
