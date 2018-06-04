using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

    public static class NetworkUtils
    {
        public enum MessageType
        {
            Heartbeat = 0,
            Debug = 1,
            Info = 2,
            Warning = 3,
            Error = 4,
            Exception = 5,
            Mapping = 6,
            XML = 7,
            Control = 8
        }
        /// <summary>
        /// The IPV6 address of the robot
        /// </summary>
        public static string RobotIPV6Address = "";
        /// <summary>
        /// The IPV4 Address of the robot
        /// </summary>
        public static string RobotIPV4Address = "";
        /// <summary>
        /// The IPV6 address of the dashboard
        /// </summary>
        public static string ComputerIPV6Address = "";
        /// <summary>
        /// The IPV4 address of the dashboard
        /// </summary>
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
        private static UdpClient RobotRecieverClient = null;
        /// <summary>
        /// The UDP sender client to the robot
        /// </summary>
        private static UdpClient RobotSenderClient = null;
        /// <summary>
        /// The port used for listening for robot events (dashboard POV)
        /// </summary>
        public const int RobotListenerPort = 42424;
        /// <summary>
        /// The port used for sending for robot events (dashboard POV)
        /// </summary>
        public const int RobotSenderPort = 24242;
        /// <summary>
        /// The computer/network name of the robot
        /// </summary>
        public const string RobotNetworkName = "minwinpc";
        /// <summary>
        /// Flag used to determine if the robot is connected for the networking thread
        /// </summary>
        private static bool RobotConnected = false;
        /// <summary>
        /// The thread to send heartbeats to the robot. TODO: determine if this can be done via dispatcher event
        /// </summary>
        private static Thread InitSenderHeartbeatThread = null;
        /// <summary>
        /// The Netwokring thread for all initialization and revieving of network data
        /// </summary>
        private static BackgroundWorker ConnectionManager = null;
        /// <summary>
        /// Number of heartbeats that have been sent since the connectino has been alive
        /// </summary>
        private static UInt64 NumHeartbeatsSent = 0;
        /// <summary>
        /// Arbitrary object to lock the sender client to prevent mulitple threads from accesing the sender client at the same time
        /// </summary>
        private static object SenderLocker = new object();
        private const bool DEBUG_IGNORE_TIMEOUT = false;
        /// <summary>
        /// modes for the sender. It could be sending the IP address (0) for the initial (re)connection, or heartbeats(1)
        /// </summary>
        private static int threadMode = 0;
        /// <summary>
        /// Starts the Listener for netowrk log packets from the robot
        /// </summary>
        public static void StartRobotNetworking()
        {
            Logging.LogConsole("Checking for any local internet connection...");
            //https://stackoverflow.com/questions/6803073/get-local-ip-address
            //check if we even have an online connectoin
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                Logging.LogConsole("ERROR: No valid network connectoins exist!");
                return;
            }
            else
            {
                Logging.LogConsole("At leaset one valid network connections exist!");
            }
            //select which ip address of this pc to use
            //https://stackoverflow.com/questions/6803073/get-local-ip-address
            //https://stackoverflow.com/questions/9855230/how-do-i-get-the-network-interface-and-its-right-ipv4-address
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            List<NetworkInformation> V6NetworkInfos = new List<NetworkInformation>();
            List<NetworkInformation> V4NetworkInfos = new List<NetworkInformation>();
            foreach (NetworkInterface ni in interfaces)
            {
                //filter out useless connections (like vmware and virtualbox vm connections, as well as loopback)
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
                            Logging.LogConsole(string.Format("Valid V6 network found: name={0}, address={1}", ni.Description, ip.Address.ToString()));
                            V6NetworkInfos.Add(new NetworkInformation { NetworkName = ni.Description, IPAddress = ip.Address });
                        }
                        else if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            Logging.LogConsole(string.Format("Valid V4 network found: name={0}, address={1}", ni.Description, ip.Address.ToString()));
                            V4NetworkInfos.Add(new NetworkInformation { NetworkName = ni.Description, IPAddress = ip.Address });
                        }
                    }
                }
            }
            //TODO: figure out a better way to do this
            Logging.LogConsole("Hard-code select index 0", true);
            ComputerIPV6Address = V6NetworkInfos[0].IPAddress.ToString();
            ComputerIPV4Address = V4NetworkInfos[0].IPAddress.ToString();
            Logging.LogConsole("Computer IPV6 address set to " + ComputerIPV6Address);
            Logging.LogConsole("Computer IPV4 address set to " + ComputerIPV4Address);
            Logging.LogConsole("Pinging robot hostname for aliveness");
            //ping the robot to check if it's on the network, if it is get it's ip address
            Ping p = new Ping();
            p.PingCompleted += OnPingCompleted;
            p.SendAsync(RobotNetworkName, null);
            Logging.LogRobot("Dashboard: Ping sent, waiting for ping respone from robot...");
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
                Logging.LogConsole("ERROR, failed to get ip address of robot, (is it online?). The application cannot continue");
                Logging.LogConsole(e.Error.ToString());
                Logging.LogRobot("Dashboard: Robot not found");
                return;
            }
            else
            {
                Logging.LogConsole("Ping SUCCESS, getting IP v4 and v6 addresses");
                using (ConnectionManager = new BackgroundWorker() { WorkerReportsProgress = true })
                {
                    ConnectionManager.DoWork += ManageConnections;
                    ConnectionManager.ProgressChanged += ConnectionManagerLog;
                    ConnectionManager.RunWorkerAsync();
                }
            }
        }
        /// <summary>
        /// Method executed on the UI thead, raised from the Networking thread when reporting progress.
        /// </summary>
        /// <param name="sender">The BackgroundWorker object</param>
        /// <param name="e">EventArgs (like percent complete and custom user object)</param>
        private static void ConnectionManagerLog(object sender, ProgressChangedEventArgs e)
        {
            //use the progressPercentage to determine which console to output to
            switch(e.ProgressPercentage)
            {
                case 1://log to console
                    Logging.LogConsole((string)e.UserState);
                    break;
                case 2://robot data
                    Logging.LogRobot((string)e.UserState);
                    break;
            }
        }

        private static void ManageConnections(object sender, DoWorkEventArgs e)
        {
            //initial worker state:
            //set the connection status false
            //start the sender thread
            RobotConnected = false;
            InitSenderHeartbeatThread = new Thread(new ThreadStart(InitRobotListener));
            InitSenderHeartbeatThread.Start();
            while (true)
            {
                bool IPV6Parsed = false;
                bool IPV4Parsed = false;
                while (!IPV6Parsed && !IPV4Parsed)
                {
                    foreach (IPAddress ip in Dns.GetHostAddresses(RobotNetworkName))
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetworkV6 && !IPV6Parsed)
                        {
                            ConnectionManager.ReportProgress(1, "Parsed IPV6 address: " + ip.ToString());
                            RobotIPV6Address = ip.ToString();
                            IPV6Parsed = true;
                        }
                        else if (ip.AddressFamily == AddressFamily.InterNetwork && !IPV4Parsed)
                        {
                            ConnectionManager.ReportProgress(1, "Parsed IPV4 address: " + ip.ToString());
                            RobotIPV4Address = ip.ToString();
                            IPV4Parsed = true;
                        }
                    }
                }
                //we now have all data we need to start sending heartbeats
                //bind the robot socket and start the background listener
                ConnectionManager.ReportProgress(2, "Dashboard: Robot found, binding socket and listening for events");
                //setup and bind listenr
                ConnectionManager.ReportProgress(1,"Binding robot listener to address " + ComputerIPV4Address);
                RobotRecieverIPEndPoint = new IPEndPoint(IPAddress.Parse(ComputerIPV4Address), RobotListenerPort);
                RobotRecieverClient = new UdpClient();
                RobotRecieverClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                RobotRecieverClient.Client.Bind(RobotRecieverIPEndPoint);
                //setup and bind sender
                ConnectionManager.ReportProgress(1, "Binding robot sender to address " + RobotIPV4Address);
                RobotSenderIPEndPoint = new IPEndPoint(IPAddress.Parse(RobotIPV4Address), RobotSenderPort);
                RobotSenderClient = new UdpClient();
                RobotSenderClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                RobotSenderClient.Client.ReceiveTimeout = 5000;
                RobotSenderClient.Client.SendTimeout = 5000;
                RobotSenderClient.Connect(RobotSenderIPEndPoint);
                ConnectionManager.ReportProgress(1, "Sending IP address of dashboard to robot...");
                threadMode = 1;//send init IP
                //wait for the robot to respond
                string result = "";
                result = Encoding.UTF8.GetString(RobotRecieverClient.Receive(ref RobotRecieverIPEndPoint));
                ConnectionManager.ReportProgress(1, "Robot Connected, comms established");
                RobotConnected = true;
                if(!DEBUG_IGNORE_TIMEOUT)
                {
                    RobotRecieverClient.Client.SendTimeout = 5000;
                    RobotRecieverClient.Client.ReceiveTimeout = 5000;
                }
                NumHeartbeatsSent = 0;
                threadMode = 2;//send heartbeats
                //netwokr setup is complete, now for as long as the connection is alive,
                //use blokcing call to wait for network events
                //TODO: see if TCP will provide more reliability for packet delivery
                while (RobotConnected)
                {
                    if (e.Cancel)
                        return;
                    try
                    {
                        result = Encoding.UTF8.GetString(RobotRecieverClient.Receive(ref RobotRecieverIPEndPoint));
                    }
                    catch (SocketException)
                    {
                        //robot has disconnected!
                        RobotConnected = false;
                        ConnectionManager.ReportProgress(1,"Robot Disconnected, trying to reconnect...");
                        ConnectionManager.ReportProgress(2,"Robot Disconnected");
                        RobotRecieverClient.Close();
                        RobotRecieverClient.Dispose();
                        RobotRecieverClient = null;
                        RobotSenderClient.Close();
                        RobotSenderClient.Dispose();
                        RobotSenderClient = null;
                    }
                    int messageTypeInt = -1;
                    //first part of the message will always be a number describing the type of the message
                    string messageTypeString = result.Split(',')[0];
                    if (int.TryParse(messageTypeString, out messageTypeInt))
                    {
                        MessageType messageType = (MessageType)messageTypeInt;
                        switch (messageType)
                        {
                            case MessageType.Heartbeat:
                                //do nothing, ignore
                                break;
                            case MessageType.Debug:
                                ConnectionManager.ReportProgress(2, "DEBUG: " + result.Substring(messageTypeString.Count()+1));
                                break;
                            case MessageType.Info:
                                ConnectionManager.ReportProgress(2, "INFO: " + result.Substring(messageTypeString.Count() + 1));
                                break;
                            case MessageType.Warning:
                                ConnectionManager.ReportProgress(2, "WARNING: " + result.Substring(messageTypeString.Count() + 1));
                                break;
                            case MessageType.Error:
                                ConnectionManager.ReportProgress(2, "ERROR: " + result.Substring(messageTypeString.Count() + 1));
                                break;
                        }
                    }
                    
                }
            }
        }

        private static void InitRobotListener()
        {
            //sends data to the robot ip address untill it responds
            while (true)
            {
                string heartbeat = (int)MessageType.Heartbeat + "," + NumHeartbeatsSent++;
                if(RobotSenderClient != null)
                {
                    lock (SenderLocker)
                    {
                        //note that threadmode can be set to 0 (or any other number than 1/2) to avoid sending data
                        //TODO: use dispatchers rather than sleep?
                        switch (threadMode)
                        {
                            case 1://IPAddress
                                RobotSenderClient.Send(Encoding.UTF8.GetBytes(ComputerIPV4Address), Encoding.UTF8.GetByteCount(ComputerIPV4Address));
                                break;
                            case 2://heartbeats
                                RobotSenderClient.Send(Encoding.UTF8.GetBytes(heartbeat), Encoding.UTF8.GetByteCount(heartbeat));
                                break;
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        }

        public static void AbortNetworkThreads()
        {
            //stop all threads to allow the application to exit
            if (InitSenderHeartbeatThread != null)
            {
                InitSenderHeartbeatThread.Abort();
                InitSenderHeartbeatThread = null;
            }
            if (ConnectionManager != null)
            {
                ConnectionManager.Dispose();
                ConnectionManager = null;
            }
        }
    }
}
