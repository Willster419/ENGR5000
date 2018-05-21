using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.NetworkInformation;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;

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
            //setup and bind listenr
            LogConsole("Binding robot listener to address " + ComputerIPV6Address);
            RobotRecieverIPEndPoint = new IPEndPoint(IPAddress.Parse(ComputerIPV6Address), RobotListenerPort);
            RobotListenerClient = new UdpClient(AddressFamily.InterNetworkV6);
            RobotListenerClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            RobotListenerClient.Client.Bind(RobotRecieverIPEndPoint);
            //setup and bind sender
            LogConsole("Binding robot sender to address " + RobotIPV6Address);
            RobotSenderIPEndPoint = new IPEndPoint(IPAddress.Parse(RobotIPV6Address), RobotSenderPort);
            RobotSenderClient = new UdpClient(AddressFamily.InterNetworkV6);
            RobotSenderClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            RobotSenderClient.Send(Encoding.UTF8.GetBytes(ComputerIPV6Address), Encoding.UTF8.GetByteCount(ComputerIPV6Address), RobotIPV6Address, RobotSenderPort);
            using (RobotListener = new BackgroundWorker())
            {
                RobotListener.DoWork += ListenForEvents;
                RobotListener.ProgressChanged += OnRecievedData;
                RobotListener.RunWorkerAsync();
            }
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
            LogRobot((string)e.UserState);
        }
    }
}
