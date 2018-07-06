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
        /// The UDP sender from robot to dashboard
        /// </summary>
        private static UdpClient RobotSenderClient = null;
        //https://msdn.microsoft.com/en-us/library/system.net.sockets.tcpclient(v=vs.110).aspx
        private static TcpClient RobotSenderClient_tcp = null;
        private static NetworkStream RobotSenderStream = null;
        /// <summary>
        /// The UDP receiver from dashboard to robot
        /// </summary>
        private static UdpClient RobotRecieverClient = null;
        private static TcpListener RobotRecieverClient_tcp2 = null;
        private static TcpClient RobotReceiverTCPClient = null;
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
        private static DispatcherTimer HeartbeatTimer = null;
        public static bool DashboardConnected = false;
        private static UInt64 NumHeartbeatsSent = 0;
        private static object NetworkSenderLocker = new object();
        //bools for debug stuff
        public static bool DEBUG_IGNORE_TIMEOUT = false;
        public static bool DEBUG_FORCE_DASHBOARD_CONNECT = true;//set to false when testing without dashboard
        private static bool DEBUG_TCP_TEST = true;
        private static volatile bool sendHeartbeats = false;
        private static GpioPin NetworkPin;
        private static Exception RecoveredException = null;
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
                if ((ni.OperationalStatus == OperationalStatus.Up) && (!ni.Description.Contains("Loopback")) && (!ni.Description.Contains("Virtual")))
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
                if(DEBUG_TCP_TEST)
                {
                    DashboardListener.DoWork += EstablishComms_tcp;
                }
                else
                {
                    DashboardListener.DoWork += EstablishComms;
                }
                DashboardListener.RunWorkerCompleted += OnListenerException;
            }
            DashboardListener.RunWorkerAsync();
            HeartbeatTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            HeartbeatTimer.Tick += OnHeartbeatTick;
            HeartbeatTimer.Start();
            NetworkPin = GPIO.Pins[1];
            NetworkPin.Write(DashboardConnected ? GpioPinValue.High : GpioPinValue.Low);
            return true;
        }

        private static void OnHeartbeatTick(object sender, object e)
        {
            if(sendHeartbeats)
            {
                string heartbeat = (int)MessageType.Heartbeat + "," + NumHeartbeatsSent++;
                if(DEBUG_TCP_TEST)
                {
                    if(RobotSenderClient_tcp != null)
                    {
                        TCPSend(RobotSenderStream, heartbeat);
                    }
                }
                else
                {
                    if(RobotSenderClient != null)
                    {
                        lock (NetworkSenderLocker)
                        {

                        }
                    }
                }
            }
        }

        private static void OnListenerException(object sender, RunWorkerCompletedEventArgs e)
        {
            //turn off coms and restart the thread
            if (e.Error != null)
            {
                DashboardConnected = false;
                NetworkPin.Write(DashboardConnected ? GpioPinValue.High : GpioPinValue.Low);
                RecoveredException = e.Error;
                DashboardListener = new BackgroundWorker()
                {
                    WorkerReportsProgress = true
                };
                DashboardListener.DoWork += EstablishComms;
                DashboardListener.RunWorkerCompleted += OnListenerException;
                DashboardListener.RunWorkerAsync();
            }
        }

        public static void EstablishComms(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                //verify stop the timer
                sendHeartbeats = false;
                //setup the receiver client
                RobotRecieverIPEndPoint = new IPEndPoint(IPAddress.Parse(RobotIPV4Address), RobotRecieverPort);
                RobotRecieverClient = new UdpClient();
                //RobotRecieverClient = new TcpClient();
                RobotRecieverClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                RobotRecieverClient.Client.Bind(RobotRecieverIPEndPoint);
                //RobotRecieverClient.Connect(RobotRecieverIPEndPoint);//ONLY TCP CONNECTS FOR RECEIVER!!1
                //RobotRecieverStream = RobotRecieverClient.GetStream();
                //wait for receiver client to get the ip address of the dashboard
                //TCP recieve method here
                string result = Encoding.UTF8.GetString(RobotRecieverClient.Receive(ref RobotRecieverIPEndPoint));
                //string result = TCPRecieve(RobotRecieverStream);
                //parse the ip address sent by the dashboard
                IPAddress address;
                try
                {
                    address = IPAddress.Parse(result);
                }
                catch (Exception)
                {
                    RobotController.RobotStatus = RobotStatus.Exception;
                    return;
                }
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
                //RobotSenderClient = new TcpClient();
                RobotSenderClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                RobotSenderClient.Client.SendTimeout = 5000;//value is miliseconds
                RobotSenderClient.Client.ReceiveTimeout = 5000;
                RobotSenderClient.Connect(RobotSenderIPEndPoint);
                //RobotSenderStream = RobotSenderClient.GetStream();
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
                NetworkPin.Write(DashboardConnected ? GpioPinValue.High : GpioPinValue.Low);
                if(RecoveredException != null)
                {
                    LogNetwork("The network thread just recovered from an exception level event\n" + RecoveredException.ToString(), MessageType.Exception);
                    RecoveredException = null;
                }
                //listen for dashboard events
                while (DashboardConnected)
                {
                    try
                    {
                        result = Encoding.UTF8.GetString(RobotRecieverClient.Receive(ref RobotRecieverIPEndPoint));
                        //result = TCPRecieve(RobotRecieverStream);
                    }
                    catch (SocketException)
                    {
                        //the dashboard has disocnnected!
                        DashboardConnected = false;
                        NetworkPin.Write(DashboardConnected ? GpioPinValue.High : GpioPinValue.Low);
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

        public static void EstablishComms_tcp(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                //verify stop the timer
                sendHeartbeats = false;

                //setup and bind listener
                RobotRecieverIPEndPoint = new IPEndPoint(IPAddress.Parse(RobotIPV4Address), RobotRecieverPort);
                RobotRecieverClient_tcp2 = new TcpListener(RobotRecieverIPEndPoint);
                //set socket before binding!
                RobotRecieverClient_tcp2.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                RobotRecieverClient_tcp2.Start();//now it's bound
                //wait for receiver client to get the ip address of the dashboard
                IPAddress address = null;
                bool recieverConnected = false;
                while(!recieverConnected)
                {
                    try
                    {
                        //try to accept the tcp connection and parse the ip address
                        RobotReceiverTCPClient = RobotRecieverClient_tcp2.AcceptTcpClient();
                        address = IPAddress.Parse(TCPRecieve(RobotReceiverTCPClient));
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
                    ComputerIPV6Address = address.ToString();
                }
                else if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    ComputerIPV4Address = address.ToString();
                }
                //for now we'er just force using the IPV4 stuff

                //setup and bind sender to send ack
                RobotSenderIPEndPoint = new IPEndPoint(IPAddress.Parse(string.IsNullOrWhiteSpace(ComputerIPV6Address) ? ComputerIPV4Address : ComputerIPV6Address), RobotSenderPort);
                RobotSenderClient_tcp = new TcpClient();
                RobotSenderClient_tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                RobotSenderClient_tcp.Client.SendTimeout = 5000;//value is miliseconds
                RobotSenderClient_tcp.Client.ReceiveTimeout = 5000;
                bool senderConnected = false;
                while(!senderConnected)
                {
                    try
                    {
                        RobotSenderClient_tcp.Connect(RobotSenderIPEndPoint);
                        senderConnected = true;
                    }
                    catch (SocketException)
                    {
                        //keep trying
                    }
                }
                RobotSenderStream = RobotSenderClient_tcp.GetStream();
                //set new timeout settings for the robot receiver
                if (!DEBUG_IGNORE_TIMEOUT)
                {
                    RobotRecieverClient_tcp2.Server.SendTimeout = 5000;//value is miliseconds
                    RobotRecieverClient_tcp2.Server.ReceiveTimeout = 5000;
                }

                //send ack to dashboard
                if (!TCPSend(RobotSenderStream, "ack"))
                    continue;

                //start to send heartbeats
                NumHeartbeatsSent = 0;
                sendHeartbeats = true;

                DashboardConnected = true;
                NetworkPin.Write(DashboardConnected ? GpioPinValue.High : GpioPinValue.Low);
                if (RecoveredException != null)
                {
                    LogNetwork("The network thread just recovered from an exception level event\n" + RecoveredException.ToString(), MessageType.Exception);
                    RecoveredException = null;
                }
                //listen for dashboard events
                while (DashboardConnected)
                {
                    string result = null;
                    result = TCPRecieve(RobotReceiverTCPClient);
                    if (result == null)
                    {
                        //the dashboard has disocnnected!
                        DashboardConnected = false;
                        NetworkPin.Write(DashboardConnected ? GpioPinValue.High : GpioPinValue.Low);
                        Disconnect();
                        break;
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
            LogNetwork(NumHeartbeatsSent++.ToString(),MessageType.Heartbeat);
        }

        public static void LogNetwork(string StringToSend, MessageType messageType)
        {
            if (RobotSenderClient == null && !DEBUG_TCP_TEST)
                return;
            if (RobotSenderClient_tcp == null && DEBUG_TCP_TEST)
                return;
            if (!DashboardConnected)
                return;
            StringToSend = (int)messageType + "," + StringToSend;
            lock(NetworkSenderLocker)
            {
                if(DEBUG_TCP_TEST)
                {
                    TCPSend(RobotSenderStream, StringToSend);
                }
                else
                {
                    RobotSenderClient.Send(Encoding.UTF8.GetBytes(StringToSend), Encoding.UTF8.GetByteCount(StringToSend));
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
            lock(NetworkSenderLocker)
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
            Byte[] data = new Byte[256];
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

        public static void Disconnect()
        {
            if (DEBUG_TCP_TEST)
            {
                if (RobotSenderStream != null)
                {
                    RobotSenderStream.Close();
                    //RobotSenderStream.Dispose();
                }
                if (RobotSenderClient_tcp != null)
                {
                    //RobotSenderClient_tcp.Client.Disconnect(true);
                    RobotSenderClient_tcp.Close();
                    //RobotSenderClient_tcp.Dispose();
                }
                if (RobotReceiverTCPClient != null)
                {
                    RobotReceiverTCPClient.GetStream().Close();
                    //RobotReceiverTCPClient.Client.Disconnect(true);
                    RobotReceiverTCPClient.Client.Close();
                    //RobotReceiverTCPClient.Dispose();
                }
            }
            else
            {

            }
        }
    }
}
