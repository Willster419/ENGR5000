using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;
using System.Net.NetworkInformation;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;

namespace RobotCode
{
    /// <summary>
    /// Represents the compination of a network card and it's ip address. Can have multiple Network names if it has an IP V4 and V6
    /// </summary>
    public struct NetworkInformation
    {
        /// <summary>
        /// The name of the network (card) interface
        /// </summary>
        public string NetworkName;
        /// <summary>
        /// An IP address that the network interface currently has
        /// </summary>
        public IPAddress @IPAddress;
    }
    /// <summary>
    /// A Network Utility class for all things relevent to networking
    /// </summary>
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
        /// The IP endpoint of the robot sender (robot->dashboard)
        /// </summary>
        private static IPEndPoint RobotRecieverIPEndPoint = null;
        /// <summary>
        /// The IP endpoint of the robot listener (dashboard->robot)
        /// </summary>
        private static IPEndPoint RobotSenderIPEndPoint = null;
        /// <summary>
        /// The UDP receiver from dashboard to robot
        /// </summary>
        private static UdpClient RobotRecieverUDPClient = null;
        /// <summary>
        /// The TCP listener of dashboard to robot
        /// </summary>
        private static TcpListener RobotReceiverTCPListener = null;
        /// <summary>
        /// The TCP client from reciver
        /// </summary>
        private static TcpClient RobotRecieverTCPClient = null;
        /// <summary>
        /// The UDP sender of robot to dashboard
        /// </summary>
        private static UdpClient RobotSenderUDPClient = null;
        //https://msdn.microsoft.com/en-us/library/system.net.sockets.tcpclient(v=vs.110).aspx
        /// <summary>
        /// The TCP sender of robot to dashboard
        /// </summary>
        private static TcpClient RobotSenderTCPClient = null;
        /// <summary>
        /// The TCP neteork stream of client
        /// </summary>
        private static NetworkStream RobotSenderTCPStream = null;
        /// <summary>
        /// The port used for sending dashboard events (robot POV)
        /// </summary>
        public const int RobotSenderPort = 42424;
        /// <summary>
        /// The port used for recieving dashboard commands (robot POV)
        /// </summary>
        public const int RobotRecieverPort = 24242;
        /// <summary>
        /// Flag used to determine if the robot is connected for the networking thread
        /// </summary>
        public static bool ConnectionLive { get; private set; } = false;
        /// <summary>
        /// The timer to send the heartbeats at 1 second invervals to the dashboard
        /// </summary>
        private static System.Timers.Timer HeartbeatTimer = null;
        /// <summary>
        /// The timer to send diagnostic robot data
        /// </summary>
        private static System.Timers.Timer DiagnosticTimer = null;
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
        private static bool DEBUG_IGNORE_TIMEOUT = false;
        /// <summary>
        /// Toggle TCP connection mode
        /// </summary>
        private static bool DEBUG_TCP_TEST = false;
        /// <summary>
        /// Force the robot to wait on load until the dashboard is connected
        /// set to false when testing without dashboard
        /// </summary>
        public static bool DEBUG_FORCE_DASHBOARD_CONNECT = true;
        /// <summary>
        /// The Pin for setting the network status LED
        /// </summary>
        private static GpioPin NetworkPin;
        /// <summary>
        /// If an exception occures on the network thread, show it when it comes back
        /// </summary>
        private static Exception RecoveredException = null;
        /// <summary>
        /// Initializes the robot, network-wise
        /// </summary>
        public static string ManualControlCommands = "";
        /// <summary>
        /// Initialises the communication system
        /// </summary>
        /// <returns>True if successfull initaliization, false otherwise</returns>
        public static bool InitComms()
        {
            //get the devices IP address
            //max 2 network devices, 4 connections
            if(!NetworkInterface.GetIsNetworkAvailable())
            {
                return false;
            }
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
                    foreach(UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if(ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            V6NetworkInfos.Add(new NetworkInformation { NetworkName = ni.Description, IPAddress = ip.Address });
                        }
                        else if(ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            V4NetworkInfos.Add(new NetworkInformation { NetworkName = ni.Description, IPAddress = ip.Address });
                        }
                    }
                }
            }
            if (V6NetworkInfos.Count == 0 && V4NetworkInfos.Count == 0)
                return false;
            //TODO: figure out a better way to do this
            if(V6NetworkInfos.Count > 0)
                RobotIPV6Address = V6NetworkInfos[0].IPAddress.ToString();
            RobotIPV4Address = V4NetworkInfos[0].IPAddress.ToString();
            //setup the heartbeat timer
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
            if (DiagnosticTimer == null)
            {
                DiagnosticTimer = new System.Timers.Timer()
                {
                    AutoReset = true,
                    Enabled = true,
                    Interval = 20
                };
                DiagnosticTimer.Elapsed += SendDiagnosticData;
            }
            DiagnosticTimer.Stop();
            if (ConnectionManager == null)
            {
                ConnectionManager = new BackgroundWorker()
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true
                };
                if(DEBUG_TCP_TEST)
                {
                    ConnectionManager.DoWork += ManageConnections_tcp;
                }
                else
                {
                    ConnectionManager.DoWork += ManageConnections;
                }
                //ConnectionManager.ProgressChanged += ConnectionManagerLog;
                ConnectionManager.RunWorkerCompleted += OnWorkComplete;
            }
            ConnectionManager.RunWorkerAsync();
            //Robot only
            NetworkPin = Hardware.Pins[1];
            NetworkPin.Write(ConnectionLive ? GpioPinValue.High : GpioPinValue.Low);
            return true;
        }
        /// <summary>
        /// When the network thread is exiting
        /// </summary>
        /// <param name="sender">The BackgroundWorker object</param>
        /// <param name="e">The event args</param>
        private static void OnWorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            ConnectionLive = false;
            //turn off coms and restart the thread
            if (e.Error != null)
            {
                NetworkPin.Write(ConnectionLive ? GpioPinValue.High : GpioPinValue.Low);
                RecoveredException = e.Error;
                Disconnect();
                InitComms();
            }
            else if (e.Cancelled)
            {
                //cancel occured, user is resetting the network connections
                Disconnect();
                InitComms();
            }
            else
            {
                //just let it die
                if (ConnectionManager != null)
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
        public static void ManageConnections(object sender, DoWorkEventArgs e)
        {
            if(ConnectionLive)
                ConnectionLive = false;
            NetworkPin.Write(ConnectionLive ? GpioPinValue.High : GpioPinValue.Low);
            while (true)
            {
                if (ConnectionManager.CancellationPending)
                {
                    e.Cancel = true;
                    HeartbeatTimer.Stop();
                    DiagnosticTimer.Stop();
                    return;
                }
                //verify stop the timer
                HeartbeatTimer.Stop();
                DiagnosticTimer.Stop();
                //setup and bind listener
                RobotRecieverIPEndPoint = new IPEndPoint(IPAddress.Parse(RobotIPV4Address), RobotRecieverPort);
                RobotRecieverUDPClient = new UdpClient();
                RobotRecieverUDPClient.Client.SendTimeout = 5000;//value is miliseconds
                RobotRecieverUDPClient.Client.ReceiveTimeout = 5000;
                RobotRecieverUDPClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                RobotRecieverUDPClient.Client.Bind(RobotRecieverIPEndPoint);
                //wait for receiver client to get the ip address of the dashboard
                bool receivedIP = false;
                string result = null;
                while (!receivedIP)
                {
                    if (ConnectionManager.CancellationPending)
                    {
                        e.Cancel = true;
                        HeartbeatTimer.Stop();
                        DiagnosticTimer.Stop();
                        return;
                    }
                    try
                    {
                        result = Encoding.UTF8.GetString(RobotRecieverUDPClient.Receive(ref RobotRecieverIPEndPoint));
                        receivedIP = true;
                    }
                    catch (Exception)
                    {

                    }
                }
                //parse the ip address sent by the dashboard
                IPAddress address;
                try
                {
                    address = IPAddress.Parse(result);
                }
                catch (Exception)
                {
                    RobotController.RobotStatus = RobotStatus.Exception;
                    e.Cancel = true;
                    return;
                }
                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    DashboardIPV6Address = address.ToString();
                }
                else if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    DashboardIPV4Address = address.ToString();
                }
                //setup and connect sender
                RobotSenderIPEndPoint = new IPEndPoint(IPAddress.Parse(string.IsNullOrWhiteSpace(DashboardIPV6Address) ? DashboardIPV4Address : DashboardIPV6Address), RobotSenderPort);
                RobotSenderUDPClient = new UdpClient();
                RobotSenderUDPClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                RobotSenderUDPClient.Client.SendTimeout = 5000;//value is miliseconds
                RobotSenderUDPClient.Client.ReceiveTimeout = 5000;
                bool senderConnected = false;
                while(!senderConnected)
                {
                    if (ConnectionManager.CancellationPending)
                    {
                        e.Cancel = true;
                        HeartbeatTimer.Stop();
                        DiagnosticTimer.Stop();
                        return;
                    }
                    try
                    {
                        RobotSenderUDPClient.Connect(RobotSenderIPEndPoint);
                        senderConnected = true;
                    }
                    catch(Exception)
                    {

                    }
                }
                //send ack
                RobotSenderUDPClient.Send(Encoding.UTF8.GetBytes("ack"), Encoding.UTF8.GetByteCount("ack"));
                //start to send heartbeats
                NumHeartbeatsSent = 0;
                HeartbeatTimer.Start();
                DiagnosticTimer.Start();
                ConnectionLive = true;
                NetworkPin.Write(ConnectionLive ? GpioPinValue.High : GpioPinValue.Low);
                if(RecoveredException != null)
                {
                    LogNetwork("The network thread just recovered from an exception level event\n" + RecoveredException.ToString(), MessageType.Exception);
                    RecoveredException = null;
                }
                //listen for dashboard events
                while (ConnectionLive)
                {
                    if (ConnectionManager.CancellationPending)
                    {
                        e.Cancel = true;
                        HeartbeatTimer.Stop();
                        DiagnosticTimer.Stop();
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
                        //the dashboard has disocnnected!
                        ConnectionLive = false;
                        NetworkPin.Write(ConnectionLive ? GpioPinValue.High : GpioPinValue.Low);
                        e.Cancel = true;
                        HeartbeatTimer.Stop();
                        DiagnosticTimer.Stop();
                        return;
                    }
                    int messageTypeInt = -1;
                    string messageTypeString = result.Split(',')[0];
                    if (int.TryParse(messageTypeString, out messageTypeInt))
                    {
                        MessageType messageType = (MessageType)messageTypeInt;
                        switch (messageType)
                        {
                            case MessageType.Heartbeat:
                                //do nothing
                                break;
                            case MessageType.Control:
                                string controlMessage = result.Substring(messageTypeString.Count() + 1);
                                if (!controlMessage.Equals(ManualControlCommands))
                                {
                                    //assuming thread access is atomic...
                                    ManualControlCommands = controlMessage;
                                    if(ManualControlCommands.Equals("Start"))
                                    {
                                        RobotController.RobotControlStatus = ControlStatus.RequestManual;
                                        RobotController.ControllerThread.CancelAsync();
                                    }
                                    else if(ManualControlCommands.Equals("Stop"))
                                    {
                                        RobotController.RobotControlStatus = ControlStatus.RelaseManual;
                                        RobotController.ControllerThread.CancelAsync();
                                    }
                                    else if (ManualControlCommands.Split(',')[0].Equals("Shutdown"))
                                    {
                                        RobotController.Poweroff(TimeSpan.FromSeconds(int.Parse(ManualControlCommands.Split(',')[1])));
                                    }
                                    else if (ManualControlCommands.Split(',')[0].Equals("Reboot"))
                                    {
                                        RobotController.Reboot(TimeSpan.FromSeconds(int.Parse(ManualControlCommands.Split(',')[1])));
                                    }
                                    else if (ManualControlCommands.Split(',')[0].Equals("Cancel_Shutdown"))
                                    {
                                        RobotController.CancelShutdown();
                                    }
                                }
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
        public static void ManageConnections_tcp(object sender, DoWorkEventArgs e)
        {
            ConnectionLive = false;
            //Robot only
            NetworkPin.Write(ConnectionLive ? GpioPinValue.High : GpioPinValue.Low);
            while (true)
            {
                if (ConnectionManager.CancellationPending)
                {
                    e.Cancel = true;
                    HeartbeatTimer.Stop();
                    DiagnosticTimer.Stop();
                    return;
                }
                //verify stop the timer
                HeartbeatTimer.Stop();
                DiagnosticTimer.Stop();
                //setup and bind listener
                RobotRecieverIPEndPoint = new IPEndPoint(IPAddress.Parse(RobotIPV4Address), RobotRecieverPort);
                RobotReceiverTCPListener = new TcpListener(RobotRecieverIPEndPoint);
                RobotReceiverTCPListener.Server.SendTimeout = 5000;//value is miliseconds
                RobotReceiverTCPListener.Server.ReceiveTimeout = 5000;
                //set socket before binding!
                RobotReceiverTCPListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                RobotReceiverTCPListener.Start();//now it's bound
                //wait for receiver client to get the ip address of the dashboard
                IPAddress address = null;
                bool recieverConnected = false;
                while(!recieverConnected)
                {
                    if (ConnectionManager.CancellationPending)
                    {
                        e.Cancel = true;
                        HeartbeatTimer.Stop();
                        DiagnosticTimer.Stop();
                        return;
                    }
                    try
                    {
                        //try to accept the tcp connection and parse the ip address
                        RobotRecieverTCPClient = RobotReceiverTCPListener.AcceptTcpClient();
                        address = IPAddress.Parse(TCPRecieve(RobotRecieverTCPClient));
                        recieverConnected = true;
                    }
                    catch (Exception)
                    {
                        //RobotController.RobotStatus = RobotStatus.Exception;
                        //keep trying
                    }
                }
                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    DashboardIPV6Address = address.ToString();
                }
                else if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    DashboardIPV4Address = address.ToString();
                }
                //for now we'er just force using the IPV4 stuff
                //setup and bind sender to send ack
                RobotSenderIPEndPoint = new IPEndPoint(IPAddress.Parse(string.IsNullOrWhiteSpace(DashboardIPV6Address) ? DashboardIPV4Address : DashboardIPV6Address), RobotSenderPort);
                RobotSenderTCPClient = new TcpClient();
                RobotSenderTCPClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                RobotSenderTCPClient.Client.SendTimeout = 5000;//value is miliseconds
                RobotSenderTCPClient.Client.ReceiveTimeout = 5000;
                bool senderConnected = false;
                while(!senderConnected)
                {
                    if (ConnectionManager.CancellationPending)
                    {
                        e.Cancel = true;
                        HeartbeatTimer.Stop();
                        DiagnosticTimer.Stop();
                        return;
                    }
                    try
                    {
                        RobotSenderTCPClient.Connect(RobotSenderIPEndPoint);
                        senderConnected = true;
                    }
                    catch (SocketException)
                    {
                        //keep trying
                    }
                }
                RobotSenderTCPStream = RobotSenderTCPClient.GetStream();
                //set new timeout settings for the robot receiver
                if (RobotRecieverTCPClient.Client.ReceiveTimeout != 5000)
                    RobotRecieverTCPClient.Client.ReceiveTimeout = 5000;
                if (RobotRecieverTCPClient.Client.SendTimeout != 5000)
                    RobotRecieverTCPClient.Client.SendTimeout = 5000;
                if (RobotRecieverTCPClient.GetStream().CanTimeout)
                {
                    if (RobotRecieverTCPClient.GetStream().ReadTimeout != 5000)
                        RobotRecieverTCPClient.GetStream().ReadTimeout = 5000;
                    if (RobotRecieverTCPClient.GetStream().WriteTimeout != 5000)
                        RobotRecieverTCPClient.GetStream().WriteTimeout = 5000;
                }
                //send ack to dashboard
                if (!TCPSend(RobotSenderTCPStream, "ack"))
                    continue;
                //start to send heartbeats
                ConnectionLive = true;
                NetworkPin.Write(ConnectionLive ? GpioPinValue.High : GpioPinValue.Low);
                NumHeartbeatsSent = 0;
                HeartbeatTimer.Start();
                DiagnosticTimer.Start();
                if (RecoveredException != null)
                {
                    LogNetwork("The network thread just recovered from an exception level event\n" + RecoveredException.ToString(), MessageType.Exception);
                    RecoveredException = null;
                }
                //listen for dashboard events
                string result = null;
                while (ConnectionLive)
                {
                    if (ConnectionManager.CancellationPending)
                    {
                        e.Cancel = true;
                        HeartbeatTimer.Stop();
                        DiagnosticTimer.Stop();
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
                        //the dashboard has disocnnected!
                        ConnectionLive = false;
                        NetworkPin.Write(ConnectionLive ? GpioPinValue.High : GpioPinValue.Low);
                        Disconnect();
                        break;
                    }
                    int messageTypeInt = -1;
                    string messageTypeString = result.Split(',')[0];
                    if (int.TryParse(messageTypeString, out messageTypeInt))
                    {
                        MessageType messageType = (MessageType)messageTypeInt;
                        switch (messageType)
                        {
                            case MessageType.Heartbeat:
                                //do nothing
                                break;
                            case MessageType.Control:
                                string controlMessage = result.Substring(messageTypeString.Count() + 1);
                                if (!controlMessage.Equals(ManualControlCommands))
                                {
                                    //assuming thread access is atomic...
                                    ManualControlCommands = controlMessage;
                                    if (ManualControlCommands.Equals("Start"))
                                    {
                                        RobotController.RobotControlStatus = ControlStatus.RequestManual;
                                        RobotController.ControllerThread.CancelAsync();
                                    }
                                    else if (ManualControlCommands.Equals("Stop"))
                                    {
                                        RobotController.RobotControlStatus = ControlStatus.RelaseManual;
                                        RobotController.ControllerThread.CancelAsync();
                                    }
                                    else if (ManualControlCommands.Split(',')[0].Equals("Shutdown"))
                                    {
                                        RobotController.Poweroff(TimeSpan.FromSeconds(int.Parse(ManualControlCommands.Split(',')[1])));
                                    }
                                    else if (ManualControlCommands.Split(',')[0].Equals("Reboot"))
                                    {
                                        RobotController.Reboot(TimeSpan.FromSeconds(int.Parse(ManualControlCommands.Split(',')[1])));
                                    }
                                    else if (ManualControlCommands.Split(',')[0].Equals("Cancel_Shutdown"))
                                    {
                                        RobotController.CancelShutdown();
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Method for the 1 second tick of the heartbeat timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnHeartbeatTick(object sender, object e)
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
        /// Sends diagnostic data to the dashboard
        /// </summary>
        /// <param name="sender">The timer</param>
        /// <param name="e">The event args</param>
        private static void SendDiagnosticData(object sender, System.Timers.ElapsedEventArgs e)
        {
            //collect and send diagnostic robot data to the dashboard
            if (!ConnectionLive || !RobotController.SystemOnline)
                return;
            string[] diagnosticData = new string[]
            {
                RobotController.RobotControlStatus.ToString(),
                RobotController.RobotStatus.ToString(),
                Hardware.SignalVoltageRaw.ToString(),//raw signal voltage
                Hardware.SignalCurrentRaw.ToString(),//raw signal current
                Hardware.PowerVoltageRaw.ToString(),//raw power voltage
                Hardware.PowerVoltage.ToString(),//raw power current
                Hardware.WaterLevel.ToString(),//water level
                Hardware.TempatureRaw.ToString(),//raw tempature
                "",//CH6 (unused)
                "",//CH7 (unused)
                Hardware.LeftDrive.GetSignInt().ToString(),//sign
                Math.Round(Hardware.LeftDrive.GetActiveDutyCyclePercentage(),2).ToString(),//mag
                Hardware.LeftEncoder.Counter.ToString(),//encoder
                Hardware.RightDrive.GetSignInt().ToString(),//sign
                Math.Round(Hardware.RightDrive.GetActiveDutyCyclePercentage(),2).ToString(),//mag
                Hardware.RightEncoder.Counter.ToString(),//encoder
                Hardware.SignalVoltage.ToString(),//signal voltage
                Hardware.SignalCurrent.ToString(),//signal current
                Hardware.PowerVoltage.ToString(),//power voltage
                Hardware.PowerCurrent.ToString(),//power current
                Hardware.AccelerationX.ToString(), //accel X
                Hardware.AccelerationY.ToString(), //accel Y
                Hardware.AccelerationZ.ToString(), //accel Z
                Hardware.GyroX.ToString(), //gyro X
                Hardware.GyroY.ToString(), //gyro Y
                Hardware.GyroZ.ToString() //gyro Z
            };
            LogNetwork(string.Join(',', diagnosticData), MessageType.DiagnosticData);
        }
        /// <summary>
        /// Send a string of information over the network to the dashboard
        /// </summary>
        /// <param name="StringToSend">The string message to send</param>
        /// <param name="messageType">The type of message</param>
        public static void LogNetwork(string StringToSend, MessageType messageType)
        {
            if (RobotSenderUDPClient == null && !DEBUG_TCP_TEST)
                return;
            if (RobotSenderTCPClient == null && DEBUG_TCP_TEST)
                return;
            if (!ConnectionLive)
                return;
            StringToSend = (int)messageType + "," + StringToSend;
            if(DEBUG_TCP_TEST)
            {
                TCPSend(RobotSenderTCPStream, StringToSend);
            }
            else
            {
                lock (NetworkSenderLocker)
                {
                    RobotSenderUDPClient.Send(Encoding.UTF8.GetBytes(StringToSend), Encoding.UTF8.GetByteCount(StringToSend));
                }
            }
        }
        /// <summary>
        /// Closes all network connections and releases all rescources used by the system
        /// </summary>
        public static void Disconnect()
        {
            if (DEBUG_TCP_TEST)
            {
                if (RobotSenderTCPStream != null)
                {
                    //close socket
                    if (RobotSenderTCPClient.Connected)
                        RobotSenderTCPStream.Close();
                    //dispose
                    RobotSenderTCPStream.Dispose();
                    RobotSenderTCPStream = null;
                }
                if (RobotSenderTCPClient != null)
                {
                    //close client
                    RobotSenderTCPClient.Close();
                    //dispose
                    RobotSenderTCPClient.Dispose();
                    RobotSenderTCPClient = null;
                }
                if (RobotRecieverTCPClient != null)
                {
                    if (RobotRecieverTCPClient.Connected)
                    {
                        RobotRecieverTCPClient.GetStream().Close();
                    }
                    RobotRecieverTCPClient.Client.Close();
                }
                if (RobotReceiverTCPListener != null)
                {
                    if (RobotReceiverTCPListener.Server.Connected)
                    {
                        RobotReceiverTCPListener.Server.Disconnect(true);
                    }
                    RobotReceiverTCPListener.Stop();
                }
                if (RobotRecieverTCPClient != null)
                {
                    RobotRecieverTCPClient.Dispose();
                    RobotRecieverTCPClient = null;
                }
                if (RobotReceiverTCPListener != null)
                {
                    RobotReceiverTCPListener = null;
                }
            }
            else
            {
                if (RobotRecieverUDPClient != null)
                {
                    if (RobotRecieverUDPClient.Client.Connected)
                        RobotRecieverUDPClient.Client.Disconnect(true);
                    RobotRecieverUDPClient.Client.Dispose();
                    RobotRecieverUDPClient.Close();
                    RobotRecieverUDPClient.Dispose();
                    RobotRecieverUDPClient = null;
                }
                if (RobotSenderUDPClient != null)
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
                catch (Exception e)
                {
                    //MAYBE put disconnected here?
                    RecoveredException = e;
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
            catch (Exception e)
            {
                RecoveredException = e;
                return null;
            }
        }
    }
}
