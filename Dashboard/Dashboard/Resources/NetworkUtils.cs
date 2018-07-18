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
using System.Timers;

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
            Control = 8,
            DiagnosticData = 9
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
        public static string DashboardIPV6Address = "";
        /// <summary>
        /// The IPV4 address of the dashboard
        /// </summary>
        public static string DashboardIPV4Address = "";
        /// <summary>
        /// The IP endpoint of the robot listener (dashboard->robot)
        /// </summary>
        private static IPEndPoint RobotRecieverIPEndPoint = null;
        /// <summary>
        /// The IP endpoint of the robot sender (robot->dashboard)
        /// </summary>
        private static IPEndPoint RobotSenderIPEndPoint = null;
        /// <summary>
        /// The UDP reciever of robot to dashboard
        /// </summary>
        private static UdpClient RobotRecieverUDPClient = null;
        /// <summary>
        /// The TCP reciever of robot to dashboard
        /// </summary>
        private static TcpListener RobotReceiverTCPListener = null;
        /// <summary>
        /// The TCP client from reciver
        /// </summary>
        private static TcpClient RobotRecieverTCPClient = null;
        /// <summary>
        /// The UDP sender of dashboard to robot
        /// </summary>
        private static UdpClient RobotSenderUDPClient = null;
        /// <summary>
        /// The TCP sender of dashboard to robot
        /// </summary>
        private static TcpClient RobotSenderTCPClient = null;
        /// <summary>
        /// The TCP network stream of client
        /// </summary>
        private static NetworkStream RobotSenderTCPStream = null;
        /// <summary>
        /// The port used for listening for robot events (dashboard POV)
        /// </summary>
        public const int RobotRecieverPort = 42424;
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
        public static bool ConnectionLive { get; private set; } = false;
        /// <summary>
        /// The timer to send the heartbeats at 1 second invervals to the robot
        /// </summary>
        private static System.Timers.Timer HeartbeatTimer = null;
        /// <summary>
        /// The Netwokring thread for all initialization and revieving of network data
        /// </summary>
        public static BackgroundWorker ConnectionManager = null;
        /// <summary>
        /// Number of heartbeats that have been sent since the connectino has been alive
        /// </summary>
        private static UInt64 NumHeartbeatsSent = 0;
        /// <summary>
        /// Arbitrary object to lock the sender client to prevent mulitple threads from accesing the sender client at the same time
        /// </summary>
        private static readonly object NetworkSenderLocker = new object();
        /// <summary>
        /// Ignore the timeout from the networking (for example, from a step by step debug session)
        /// </summary>
        private static bool DEBUG_IGNORE_TIMEOUT = true;
        /// <summary>
        /// Toggle TCP connection mode
        /// </summary>
        private static bool DEBUG_TCP_TEST = false;
        /// <summary>
        /// Starts the Listener for netowrk log packets from the robot
        /// </summary>
        private static MainWindow mainWindowInstance;
        /// <summary>
        /// Iinitalize the communications system
        /// </summary>
        /// <param name="mw">The instance of the MainWindow so we can access the UI fields ot add/remove data</param>
        public static void InitComms(MainWindow mw)
        {
            if (mw != null)
                mainWindowInstance = mw;
            Logging.LogConsole("Checking for any local internet connection...");
            //https://stackoverflow.com/questions/6803073/get-local-ip-address
            //check if we even have an online connectoin
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                Logging.LogConsole("ERROR: No valid network connectoins exist!");
                return;
            }
            Logging.LogConsole("At leaset one valid network connections exist!");
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
            if(V6NetworkInfos.Count > 0)
                DashboardIPV6Address = V6NetworkInfos[0].IPAddress.ToString();
            DashboardIPV4Address = V4NetworkInfos[0].IPAddress.ToString();
            Logging.LogConsole("Computer IPV6 address set to " + DashboardIPV6Address);
            Logging.LogConsole("Computer IPV4 address set to " + DashboardIPV4Address);
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
                bool IPV6Parsed = false;
                bool IPV4Parsed = false;
                while (!IPV6Parsed && !IPV4Parsed)
                {
                    foreach (IPAddress ip in Dns.GetHostAddresses(RobotNetworkName))
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetworkV6 && !IPV6Parsed)
                        {
                            Logging.LogConsole("Parsed IPV6 address: " + ip.ToString());
                            RobotIPV6Address = ip.ToString();
                            IPV6Parsed = true;
                        }
                        else if (ip.AddressFamily == AddressFamily.InterNetwork && !IPV4Parsed)
                        {
                            Logging.LogConsole("Parsed IPV4 address: " + ip.ToString());
                            RobotIPV4Address = ip.ToString();
                            IPV4Parsed = true;
                        }
                    }
                }
                //setup the timer (but don't start it yet)
                //NOTE: is is on the UI thread
                if (HeartbeatTimer == null)
                {
                    HeartbeatTimer = new System.Timers.Timer()
                    {
                        AutoReset = true,
                        Enabled = true,
                        Interval = 1000
                    };
                    HeartbeatTimer.Elapsed += OnHeartbeatTick;
                }
                HeartbeatTimer.Stop();
                //create the backround thread for networking. Allows for blocking calls for recieve()
                if (ConnectionManager == null)
                {
                    ConnectionManager = new BackgroundWorker()
                    {
                        WorkerReportsProgress = true,
                        WorkerSupportsCancellation = true
                    };
                    if (DEBUG_TCP_TEST)
                    {
                        ConnectionManager.DoWork += ManageConnections_tcp;
                    }
                    else
                    {
                        ConnectionManager.DoWork += ManageConnections;
                    }
                    ConnectionManager.ProgressChanged += ConnectionManagerLog;
                    ConnectionManager.RunWorkerCompleted += OnWorkComplete;
                }
                ConnectionManager.RunWorkerAsync();
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
            switch (e.ProgressPercentage)
            {
                case 1://log to console
                    Logging.LogConsole((string)e.UserState);
                    break;
                case 2://robot data
                    Logging.LogRobot((string)e.UserState);
                    break;
                case 3://diagnostic data
                    string data = (string)e.UserState;
                    string[] diagnosticData = data.Split(',');
                    mainWindowInstance.OnDiagnosticData(diagnosticData);
                    break;
            }
        }
        /// <summary>
        /// When the network thread is exiting
        /// </summary>
        /// <param name="sender">The BackgroundWorker object</param>
        /// <param name="e">The event args</param>
        private static void OnWorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            ConnectionLive = false;
            //NOTE: this is back on the UI threads
            if(e.Error != null)
            {
                //error occured, should be restarted
                Disconnect();
                InitComms(null);
            }
            else if (e.Cancelled)
            {
                //cancel occured, user is resetting the network connections
                Disconnect();
                InitComms(null);
            }
            else
            {
                //just let it die
                if(ConnectionManager != null)
                {
                    ConnectionManager.Dispose();
                    ConnectionManager = null;
                }
            }
        }
        /// <summary>
        /// The main worker method for the networking thread, UDP method
        /// </summary>
        /// <param name="sender">The BackgroundWorker object</param>
        /// <param name="e">The event args</param>
        private static void ManageConnections(object sender, DoWorkEventArgs e)
        {
            if (ConnectionLive)
                ConnectionLive = false;
            while (true)
            {
                if (ConnectionManager.CancellationPending)
                {
                    e.Cancel = true;
                    HeartbeatTimer.Stop();
                    return;
                }
                HeartbeatTimer.Stop();
                //we now have all data we need to start sending heartbeats
                //bind the robot socket and start the background listener
                ConnectionManager.ReportProgress(2, "Dashboard: Robot found, binding socket and listening for events");
                //setup and bind listenr
                ConnectionManager.ReportProgress(1, "Binding robot listener to address " + DashboardIPV4Address);
                RobotRecieverIPEndPoint = new IPEndPoint(IPAddress.Parse(DashboardIPV4Address), RobotRecieverPort);
                RobotRecieverUDPClient = new UdpClient();
                RobotRecieverUDPClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                RobotRecieverUDPClient.Client.SendTimeout = 5000;
                RobotRecieverUDPClient.Client.ReceiveTimeout = 5000;
                RobotRecieverUDPClient.Client.Bind(RobotRecieverIPEndPoint);
                //setup and bind sender
                ConnectionManager.ReportProgress(1, "Binding robot sender to address " + RobotIPV4Address);
                RobotSenderIPEndPoint = new IPEndPoint(IPAddress.Parse(RobotIPV4Address), RobotSenderPort);
                RobotSenderUDPClient = new UdpClient();
                RobotSenderUDPClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                RobotSenderUDPClient.Client.ReceiveTimeout = 5000;
                RobotSenderUDPClient.Client.SendTimeout = 5000;
                ConnectionManager.ReportProgress(1, "Connecting to robot...");
                bool robotConnected = false;
                while(!robotConnected)
                {
                    if (ConnectionManager.CancellationPending)
                    {
                        e.Cancel = true;
                        HeartbeatTimer.Stop();
                        return;
                    }
                    try
                    {
                        RobotSenderUDPClient.Connect(RobotSenderIPEndPoint);
                        robotConnected = true;
                    }
                    catch(Exception)
                    {

                    }
                }
                //wait for the robot to respond
                ConnectionManager.ReportProgress(1, "Sending IP address of dashboard to robot and waiting for response...");
                bool robotResponded = false;
                string result = null;
                while (!robotResponded)
                {
                    if (ConnectionManager.CancellationPending)
                    {
                        e.Cancel = true;
                        HeartbeatTimer.Stop();
                        return;
                    }
                    try
                    {
                        RobotSenderUDPClient.Send(Encoding.UTF8.GetBytes(DashboardIPV4Address), Encoding.UTF8.GetByteCount(DashboardIPV4Address));
                        result = Encoding.UTF8.GetString(RobotRecieverUDPClient.Receive(ref RobotRecieverIPEndPoint));//should be "ack"
                        robotResponded = true;
                    }
                    catch (Exception)
                    {

                    }
                }
                ConnectionManager.ReportProgress(1, "Robot Connected, comms established");
                NumHeartbeatsSent = 0;
                HeartbeatTimer.Start();
                ConnectionLive = true;
                //initialize the robot data logging system
                Logging.InitNewDataLogFile();
                //netwokr setup is complete, now for as long as the connection is alive,
                //use blokcing call to wait for network events
                while (ConnectionLive)
                {
                    if (ConnectionManager.CancellationPending)
                    {
                        e.Cancel = true;
                        HeartbeatTimer.Stop();
                        return;
                    }
                    try
                    {
                        result = Encoding.UTF8.GetString(RobotRecieverUDPClient.Receive(ref RobotRecieverIPEndPoint));
                    }
                    catch (SocketException)
                    {
                        if (DEBUG_IGNORE_TIMEOUT && !DEBUG_TCP_TEST)
                            continue;
                        //robot has disconnected!
                        ConnectionLive = false;
                        ConnectionManager.ReportProgress(1, "Robot Disconnected, trying to reconnect...");
                        ConnectionManager.ReportProgress(2, "Robot Disconnected");
                        e.Cancel = true;
                        HeartbeatTimer.Stop();
                        return;
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
                                ConnectionManager.ReportProgress(2, "DEBUG: " + result.Substring(messageTypeString.Count() + 1));
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
                            case MessageType.Exception:
                                ConnectionManager.ReportProgress(2, "EXCEPTION: " + result.Substring(messageTypeString.Count() + 1));
                                break;
                            case MessageType.DiagnosticData:
                                ConnectionManager.ReportProgress(3, result.Substring(messageTypeString.Count() + 1));
                                break;
                        }
                    }

                }
            }
        }
        /// <summary>
        /// The main worker method for the networking thread, TCP method
        /// </summary>
        /// <param name="sender">The BackgroundWorker object</param>
        /// <param name="e">The event args</param>
        private static void ManageConnections_tcp(object sender, DoWorkEventArgs e)
        {
            ConnectionLive = false;
            while (true)
            {
                if (ConnectionManager.CancellationPending)
                {
                    e.Cancel = true;
                    HeartbeatTimer.Stop();
                    return;
                }
                //stop the timer
                HeartbeatTimer.Stop();
                //setup and bind listenr
                ConnectionManager.ReportProgress(2, "Dashboard: Robot found, binding socket and listening for events");
                ConnectionManager.ReportProgress(1, string.Format("Binding robot listener to address {0}, port {1}", DashboardIPV4Address, RobotRecieverPort));
                RobotRecieverIPEndPoint = new IPEndPoint(IPAddress.Parse(DashboardIPV4Address), RobotRecieverPort);
                RobotReceiverTCPListener = new TcpListener(RobotRecieverIPEndPoint);
                //ALWAYS set socket options to reuse before binding/connecting!!
                RobotReceiverTCPListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                //start() connects to the port, DOES NOT connect the two client together
                //connects to it's ip address on the computer, so it should not block or throw exception
                //also from example, server = receiver
                //cannot accept yet, because accept is blocking and it needs to send it's ip address
                RobotReceiverTCPListener.Start();
                //setup and bind sender
                ConnectionManager.ReportProgress(1, string.Format("Binding robot sender to address {0}, port {1}", RobotIPV4Address, RobotSenderPort));
                RobotSenderIPEndPoint = new IPEndPoint(IPAddress.Parse(RobotIPV4Address), RobotSenderPort);
                RobotSenderTCPClient = new TcpClient();
                RobotSenderTCPClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                RobotSenderTCPClient.Client.ReceiveTimeout = 5000;
                RobotSenderTCPClient.Client.SendTimeout = 5000;
                ConnectionManager.ReportProgress(1, "Sender connecting to robot...");
                bool senderConnected = false;
                while(!senderConnected)
                {
                    if(ConnectionManager.CancellationPending)
                    {
                        e.Cancel = true;
                        HeartbeatTimer.Stop();
                        return;
                    }
                    try
                    {
                        RobotSenderTCPClient.Connect(RobotSenderIPEndPoint);
                        ConnectionManager.ReportProgress(1, "Sender connected!");
                        senderConnected = true;
                    }
                    catch (SocketException)
                    {
                        //try again
                    }
                }
                ConnectionManager.ReportProgress(1, "Sending IP address of dashboard to robot...");
                RobotSenderTCPStream = RobotSenderTCPClient.GetStream();
                TCPSend(RobotSenderTCPStream, DashboardIPV4Address);
                if (!DEBUG_IGNORE_TIMEOUT)
                {
                    RobotReceiverTCPListener.Server.SendTimeout = 5000;
                    RobotReceiverTCPListener.Server.ReceiveTimeout = 5000;
                }
                //now wait for ack from robot that it is ready for comms...(this is blocking)
                bool recieverConnected = false;
                ConnectionManager.ReportProgress(1, "IP address sent, waiting for response...");
                while(!recieverConnected)
                {
                    if (ConnectionManager.CancellationPending)
                    {
                        e.Cancel = true;
                        HeartbeatTimer.Stop();
                        return;
                    }
                    try
                    {
                        RobotRecieverTCPClient = RobotReceiverTCPListener.AcceptTcpClient();
                        string temp = TCPRecieve(RobotRecieverTCPClient);//should be ack
                        ConnectionManager.ReportProgress(1, "Reciever connected!");
                        if (RobotRecieverTCPClient.Client.ReceiveTimeout != 5000)
                            RobotRecieverTCPClient.Client.ReceiveTimeout = 5000;
                        if (RobotRecieverTCPClient.Client.SendTimeout != 5000)
                            RobotRecieverTCPClient.Client.SendTimeout = 5000;
                        if(RobotRecieverTCPClient.GetStream().CanTimeout)
                        {
                            if (RobotRecieverTCPClient.GetStream().ReadTimeout != 5000)
                                RobotRecieverTCPClient.GetStream().ReadTimeout = 5000;
                            if (RobotRecieverTCPClient.GetStream().WriteTimeout != 5000)
                                RobotRecieverTCPClient.GetStream().WriteTimeout = 5000;
                        }
                        recieverConnected = true;
                    }
                    catch (Exception)
                    {

                    }
                }
                ConnectionManager.ReportProgress(1, "Robot Connected, comms established");
                ConnectionLive = true;
                //initialize the robot data logging system
                Logging.InitNewDataLogFile();
                NumHeartbeatsSent = 0;
                HeartbeatTimer.Start();
                //netwokr setup is complete
                string result = null;
                while (ConnectionLive)
                {
                    if (ConnectionManager.CancellationPending)
                    {
                        e.Cancel = true;
                        HeartbeatTimer.Stop();
                        return;
                    }
                    result = TCPRecieve(RobotRecieverTCPClient);
                    if (string.IsNullOrWhiteSpace(result))
                    {
                        if (DEBUG_IGNORE_TIMEOUT && DEBUG_TCP_TEST)
                        {
                            Thread.Sleep(1000);
                            continue;
                        }
                        //robot has disconnected!
                        ConnectionLive = false;
                        ConnectionManager.ReportProgress(1, "Robot Disconnected, trying to reconnect...");
                        ConnectionManager.ReportProgress(2, "Robot Disconnected");
                        e.Cancel = true;
                        return;
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
                                ConnectionManager.ReportProgress(2, "DEBUG: " + result.Substring(messageTypeString.Count() + 1));
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
                            case MessageType.Exception:
                                ConnectionManager.ReportProgress(2, "EXCEPTION: " + result.Substring(messageTypeString.Count() + 1));
                                break;
                            case MessageType.DiagnosticData:
                                ConnectionManager.ReportProgress(3, result.Substring(messageTypeString.Count() + 1));
                                break;
                        }
                    }

                }
            }
        }
        /// <summary>
        /// Event to fire when the heartbeat tick happends every second
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnHeartbeatTick(object sender, ElapsedEventArgs e)
        {
            string heartbeat = (int)MessageType.Heartbeat + "," + NumHeartbeatsSent++;
            if (DEBUG_TCP_TEST)
            {
                if (RobotSenderTCPClient != null)
                {
                    TCPSend(RobotSenderTCPStream, heartbeat);
                }
            }
            else
            {
                if (RobotSenderUDPClient != null)
                {
                    lock (NetworkSenderLocker)
                    {
                        RobotSenderUDPClient.Send(Encoding.UTF8.GetBytes(heartbeat), Encoding.UTF8.GetByteCount(heartbeat));
                    }
                }
            }
        }
        /// <summary>
        /// Send a message to the robot
        /// </summary>
        /// <param name="messageType">The type of message to send (see MessageType enumeration for types)</param>
        /// <param name="message">The actual string message to send</param>
        /// <returns></returns>
        public static bool SendRobotMesage(MessageType messageType, string message)
        {
            if (!ConnectionLive)
                return false;
            string messageSend = (int)messageType + "," + message;
            if (DEBUG_TCP_TEST)
            {
                if (RobotSenderTCPClient == null)
                    return false;
                if(!TCPSend(RobotSenderTCPStream, messageSend))
                    return false;
            }
            else
            {
                if (RobotSenderUDPClient == null)
                    return false;
                lock (NetworkSenderLocker)
                {
                    try
                    {
                        RobotSenderUDPClient.Send(Encoding.UTF8.GetBytes(messageSend), Encoding.UTF8.GetByteCount(messageSend));
                    }
                    catch(Exception)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// Closes all network connections and releases all rescources used by the system
        /// </summary>
        public static void Disconnect()
        {
            if (DEBUG_TCP_TEST)
            {
                Logging.LogConsole("Closing and disposing sender networkStream",true);
                if (RobotSenderTCPStream != null)
                {
                    //close socket
                    if(RobotSenderTCPClient.Connected)
                        RobotSenderTCPStream.Close();
                    //dispose
                    RobotSenderTCPStream.Dispose();
                    RobotSenderTCPStream = null;
                }
                Logging.LogConsole("Closing and disposing sender client");
                if (RobotSenderTCPClient != null)
                {
                    //close client
                    RobotSenderTCPClient.Close();
                    //dispose
                    RobotSenderTCPClient.Dispose();
                    RobotSenderTCPClient = null;
                }
                Logging.LogConsole("Closing Reciever stream and socket", true);
                if (RobotRecieverTCPClient != null)
                {
                    if (RobotRecieverTCPClient.Connected)
                    {
                        RobotRecieverTCPClient.GetStream().Close();
                    }
                    RobotRecieverTCPClient.Client.Close();
                }
                Logging.LogConsole("Closing Reciever server", true);
                if (RobotReceiverTCPListener != null)
                {
                    if (RobotReceiverTCPListener.Server.Connected)
                    {
                        Logging.LogConsole("tcpServer was open (again?), closing", true);
                        RobotReceiverTCPListener.Server.Disconnect(true);
                    }
                    RobotReceiverTCPListener.Stop();
                }
                Logging.LogConsole("Disposing Reciever client");
                if(RobotRecieverTCPClient != null)
                {
                    RobotRecieverTCPClient.Dispose();
                    RobotRecieverTCPClient = null;
                }
                Logging.LogConsole("Disposing Reciever server");
                if(RobotReceiverTCPListener != null)
                {
                    RobotReceiverTCPListener = null;
                }
            }
            else
            {
                Logging.LogConsole("Closing reciever");
                if(RobotRecieverUDPClient != null)
                {
                    if (RobotRecieverUDPClient.Client.Connected)
                        RobotRecieverUDPClient.Client.Disconnect(true);
                    RobotRecieverUDPClient.Client.Dispose();
                    RobotRecieverUDPClient.Close();
                    RobotRecieverUDPClient.Dispose();
                    RobotRecieverUDPClient = null;
                }
                Logging.LogConsole("Closing Sender");
                if(RobotSenderUDPClient != null)
                {
                    RobotSenderUDPClient.Client.Dispose();
                    RobotSenderUDPClient.Close();
                    RobotSenderUDPClient.Dispose();
                    RobotSenderUDPClient = null;
                }
            }
        }
        /// <summary>
        /// Sends a string of data across a NetworkStream. A return of false means the connection is error
        /// </summary>
        /// <param name="ns">The NetworkSteam object</param>
        /// <param name="s">The string to send</param>
        /// <returns></returns>
        public static bool TCPSend(NetworkStream ns, string s)
        {
            lock (NetworkSenderLocker)
            {
                Byte[] data = Encoding.UTF8.GetBytes(s);
                try
                {
                    ns.Write(data, 0, data.Length);
                    //send was sucessfull
                    return true;
                }
                catch
                {
                    //MAYBE put disconnected here?
                    return false;
                }
            }
        }
        /// <summary>
        /// Recieve a string of data from a TCPClient object. A null result indicates a disconnected state
        /// </summary>
        /// <param name="clinet">The client object</param>
        /// <returns></returns>
        public static string TCPRecieve(TcpClient clinet)
        {
            Byte[] data = new Byte[4096];
            try
            {
                return Encoding.UTF8.GetString(data, 0, clinet.GetStream().Read(data, 0, data.Length));
            }
            //return null in case of a failed connection
            catch (Exception)
            {
                return null;
            }
        }
    }
}
