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
using Windows.Devices.Spi;

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
    public enum RobotStatus
    {
        Idle = 0,
        UnknownError = 5
    };
    /// <summary>
    /// A Utility class for important static methods
    /// </summary>
    public static class Utils
    {
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
        /// The port used for listening for robot events
        /// </summary>
        public const int RobotListenerPort = 42424;
        /// <summary>
        /// The port used for sending for robot events
        /// </summary>
        public const int RobotSenderPort = 24242;
        /// <summary>
        /// The background thread for listening for messages from the dashboard
        /// </summary>
        private static BackgroundWorker DashboardListener = null;
        private static bool DashboardConnected = false;
        private static GpioController Controller = null;
        public static GpioPin[] Pins = new GpioPin[5];
        public const int STATUS_PIN = 22;
        public const int DASHBOARD_CONNECTION_PIN = 4;
        /// <summary>
        /// Initializes the robot, network-wise
        /// </summary>
        public static void InitComms()
        {
            //get the devices IP address
            //max 2 network devices, 4 connections
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
            //listen for the dashboard
            bool listingForDashboard = true;

            RobotIPV6Address = V6NetworkInfos[0].IPAddress.ToString();
            RobotIPV4Address = V4NetworkInfos[0].IPAddress.ToString();
            //dashboard sends it's ip address
            RobotSenderIPEndPoint = new IPEndPoint(IPAddress.Parse(RobotIPV4Address), RobotSenderPort);
            RobotSenderClient = new UdpClient();
            RobotSenderClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            RobotSenderClient.Client.Bind(RobotSenderIPEndPoint);
            string result = Encoding.UTF8.GetString(RobotSenderClient.Receive(ref RobotSenderIPEndPoint));
            if (IPAddress.TryParse(result, out IPAddress address))
            {
                if(address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    ComputerIPV6Address = address.ToString();
                }
                else if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    ComputerIPV4Address = address.ToString();
                }
            }
            //comms established, setup receiver
            RobotRecieverIPEndPoint = new IPEndPoint(IPAddress.Parse(string.IsNullOrWhiteSpace(ComputerIPV6Address) ? ComputerIPV4Address : ComputerIPV6Address), RobotListenerPort);
            RobotListenerClient = new UdpClient();
            RobotListenerClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            RobotListenerClient.Connect(RobotRecieverIPEndPoint);
            RobotListenerClient.Send(Encoding.UTF8.GetBytes("comms established"), Encoding.UTF8.GetByteCount("comms established"));
            DashboardConnected = true;
            Pins[1] = Controller.OpenPin(DASHBOARD_CONNECTION_PIN);
            Pins[1].Write(GpioPinValue.High);
            Pins[1].SetDriveMode(GpioPinDriveMode.Output);
            //Thread t = new Thread(new ThreadStart(SendHeartbeats));
            //t.Start();
        }

        private static void SendHeartbeats()
        {
            int i = 0;
            while (true)
            {
                string test = "send number " + i++;
                //DEBUG
                RobotListenerClient.Send(Encoding.UTF8.GetBytes(test), Encoding.UTF8.GetByteCount(test));
                Thread.Sleep(500);
            }
        }

        public static bool InitGPIO()
        {
            Controller = GpioController.GetDefault();
            if (Controller == null)
                return false;
            Pins[0] = Controller.OpenPin(STATUS_PIN);
            Pins[0].Write(GpioPinValue.High);
            Pins[0].SetDriveMode(GpioPinDriveMode.Output);
            return true;
        }

        public static void LogNetwork(string StringToSend)
        {
            if (RobotListenerClient == null)
                return;
        }

        public static void InitSPI()
        {

        }
    }
}
