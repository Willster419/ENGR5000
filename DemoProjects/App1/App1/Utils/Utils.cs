using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;
using System.Net.NetworkInformation;

namespace App1.Utils
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
    /// A Utility class for important static methods
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// The IP address of the robot
        /// </summary>
        public static string RobotIPV6Address = "";
        /// <summary>
        /// The IP address of the computer
        /// </summary>
        public static string ComputerIPV6Address = "";
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
        /// <summary>
        /// Initializes the robot, network-wise
        /// </summary>
        public static void InitComms()
        {
            //get the devices IP address
            //max 2 network devices, 4 connections
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            List<NetworkInformation> NetworkInfos = new List<NetworkInformation>();
            foreach (NetworkInterface ni in interfaces)
            {
                if((ni.OperationalStatus == OperationalStatus.Up) && (!ni.Description.Contains("Loopback")))
                {
                    foreach(UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if(ip.Address.IsIPv6LinkLocal)
                        {
                            NetworkInfos.Add(new NetworkInformation { NetworkName = ni.Description, IPAddress = ip.Address });
                        }
                    }
                }
            }
            //listen for the dashboard
            bool listingForDashboard = true;

            RobotIPV6Address = NetworkInfos[0].IPAddress.ToString();
            //dashboard sends it's ip address
            RobotSenderIPEndPoint = new IPEndPoint(IPAddress.Parse(RobotIPV6Address), RobotSenderPort);
            RobotSenderClient = new UdpClient(AddressFamily.InterNetworkV6);
            RobotSenderClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            RobotSenderClient.Client.Bind(RobotSenderIPEndPoint);
            string result = Encoding.UTF8.GetString(RobotSenderClient.Receive(ref RobotSenderIPEndPoint));
            if (IPAddress.TryParse(result, out IPAddress address))
            {
                ComputerIPV6Address = address.ToString();
            }
        }
    }
}
