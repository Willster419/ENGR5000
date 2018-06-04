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

namespace RobotCode
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
            Control = 8
        }
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
        private static IPEndPoint RobotSenderIPEndPoint = null;
        /// <summary>
        /// The IP endpoint of the robot sender
        /// </summary>
        private static IPEndPoint RobotRecieverIPEndPoint = null;
        /// <summary>
        /// The UDP listener client of the robot
        /// </summary>
        //https://msdn.microsoft.com/en-us/library/system.net.sockets.tcpclient(v=vs.110).aspx
        //private static TcpClient RobotSenderClient = null;
        private static UdpClient RobotSenderClient = null;
        private static NetworkStream RobotNetworkStream = null;
        /// <summary>
        /// The UDP sender client to the robot
        /// </summary>
        //private static TcpClient RobotRecieverClient = null;
        private static UdpClient RobotRecieverClient = null;
        private static NetworkStream RobotRecieverStream = null;
        /// <summary>
        /// The port used for sending dashboard events (robot POV)
        /// </summary>
        public const int RobotSenderPort = 42424;
        /// <summary>
        /// The port used for recieving dashboard commands (robot POV)
        /// </summary>
        public const int RobotRecieverPort = 24242;
        /// <summary>
        /// The background thread for managing the listener and heartbeat thread
        /// </summary>
        private static BackgroundWorker DashboardListener = null;
        private static Thread HeartbeatThread = null;
        public static bool DashboardConnected = false;
        private static UInt64 NumHeartbeatsSent = 0;
        private static object NetworkSenderLocker = new object();
        public const bool DEBUG_IGNORE_TIMEOUT = false;
        public const bool DEBUG_FORCE_DASHBOARD_CONNECT = true;
        private static volatile bool sendHeartbeats = false;
        /// <summary>
        /// Initializes the robot, network-wise
        /// </summary>
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
                if((ni.OperationalStatus == OperationalStatus.Up) && (!ni.Description.Contains("Loopback")))
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
            RobotIPV6Address = V6NetworkInfos[0].IPAddress.ToString();
            RobotIPV4Address = V4NetworkInfos[0].IPAddress.ToString();
            if(DashboardListener == null)
            {
                DashboardListener = new BackgroundWorker()
                {
                    WorkerReportsProgress = true
                };
                DashboardListener.DoWork += EstablishComms;
            }
            DashboardListener.RunWorkerAsync();
            HeartbeatThread = new Thread(new ThreadStart(SendHeartBeats));
            HeartbeatThread.Start();
            sendHeartbeats = true;
            GPIO.ToggleNetworkStatus(true);
            return true;
        }

        public static void EstablishComms(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                sendHeartbeats = false;
                //setup the receiver client
                RobotRecieverIPEndPoint = new IPEndPoint(IPAddress.Parse(RobotIPV4Address), RobotRecieverPort);
                RobotRecieverClient = new UdpClient();
                RobotRecieverClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                RobotRecieverClient.Client.Bind(RobotRecieverIPEndPoint);
                //RobotRecieverStream = RobotRecieverClient.GetStream();
                //wait for receiver client to get the ip address of the dashboard
                //TCP recieve method here
                string result = Encoding.UTF8.GetString(RobotRecieverClient.Receive(ref RobotRecieverIPEndPoint));
                //parse the ip address sent by the dashboard
                IPAddress address = IPAddress.Parse(result);
                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    ComputerIPV6Address = address.ToString();
                }
                else if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    ComputerIPV4Address = address.ToString();
                }
                //setup sender
                RobotSenderIPEndPoint = new IPEndPoint(IPAddress.Parse(string.IsNullOrWhiteSpace(ComputerIPV6Address) ? ComputerIPV4Address : ComputerIPV6Address), RobotSenderPort);
                RobotSenderClient = new UdpClient();
                RobotSenderClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                RobotSenderClient.Client.SendTimeout = 5000;//value is miliseconds
                RobotSenderClient.Client.ReceiveTimeout = 5000;
                RobotSenderClient.Connect(RobotSenderIPEndPoint);
                //set new timeout settings for the robot receiver
                if(!DEBUG_IGNORE_TIMEOUT)
                {
                    RobotRecieverClient.Client.SendTimeout = 5000;//value is miliseconds
                    RobotRecieverClient.Client.ReceiveTimeout = 5000;
                }
                //start to send heartbeats
                NumHeartbeatsSent = 0;
                sendHeartbeats = true;
                DashboardConnected = true;
                GPIO.ToggleNetworkStatus(false);
                //listen for dashboard events
                while (DashboardConnected)
                {
                    try
                    {
                        result = Encoding.UTF8.GetString(RobotRecieverClient.Receive(ref RobotRecieverIPEndPoint));
                    }
                    catch (SocketException)
                    {
                        //the dashboard has disocnnected!
                        DashboardConnected = false;
                        GPIO.ToggleNetworkStatus(false);
                        RobotRecieverClient.Close();
                        RobotRecieverClient.Dispose();
                        RobotRecieverClient = null;
                        RobotSenderClient.Close();
                        RobotSenderClient.Dispose();
                        RobotSenderClient = null;
                    }
                    string messageTypeString = result.Split(',')[0];
                    int messageTypeInt = -1;
                    if (int.TryParse(messageTypeString, out messageTypeInt))
                    {
                        MessageType messageType = (MessageType)messageTypeInt;
                        switch (messageType)
                        {
                            case MessageType.Heartbeat:
                                //do nothing
                                break;
                            case MessageType.Control:
                                //TODO: manually control the robot
                                break;
                        }
                    }
                }
            }
        }

        private static void SendHeartBeats()
        {
            while(true)
            {
                if(sendHeartbeats)
                {
                    LogNetwork(NumHeartbeatsSent++.ToString(),MessageType.Heartbeat);
                }
                Thread.Sleep(1000);
            }
        }

        public static void LogNetwork(string StringToSend, MessageType messageType)
        {
            if (RobotSenderClient == null)
                return;
            if (!DashboardConnected)
                return;
            StringToSend = (int)messageType + "," + StringToSend;
            lock(NetworkSenderLocker)
            {
                RobotSenderClient.Send(Encoding.UTF8.GetBytes(StringToSend), Encoding.UTF8.GetByteCount(StringToSend));
            }
        }

        public static void NetworkSend(NetworkStream ns, string s)
        {
            Byte[] data = Encoding.UTF8.GetBytes(s);
            ns.Write(data, 0, data.Length);
        }

        public static string NetworkRecieve(NetworkStream ns)
        {
            Byte[] data = new Byte[256];
            Int32 bytes = ns.Read(data, 0, data.Length);
            return Encoding.UTF8.GetString(data, 0, bytes);
        }
    }
}
