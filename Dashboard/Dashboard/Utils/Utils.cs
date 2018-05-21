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
        /// The Ip address of the robot
        /// </summary>
        public static string RobotIPV6Address = "";
        public static string ComputerIPV6Address = "";
        private static IPEndPoint ipep = null;
        private static UdpClient client = null;
        /// <summary>
        /// Initializes the Logging functions of the application
        /// </summary>
        /// <param name="logOutput">The Textbox from the MainWindow</param>
        public static void InitUtils(MainWindow @MainWindow)
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
            string dateTimeFormat = string.Format("{0:yyyy-MM-dd:HH-mm-ss}", DateTime.Now);
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
            string dateTimeFormat = string.Format("{0:yyyy-MM-dd:HH-mm-ss}", DateTime.Now);
            if (debug)
            {
                textToLog = "DEBUG: " + textToLog;
            }
            textToLog = string.Format("{0}   {1}\r\n", dateTimeFormat, textToLog);
            RobotLogOutput.AppendText(textToLog);
            RobotLogSteam.Write(Encoding.UTF8.GetBytes(textToLog), 0, Encoding.UTF8.GetByteCount(textToLog));
            RobotLogSteam.Flush();
        }

        public static void StartRobotLister(string addressToBind, int portToBind)
        {
            //ip objects
            //TODO: get current computer ip address
            ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 42424);
            client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(ipep);
            using (BackgroundWorker worker = new BackgroundWorker())
            {
                worker.DoWork += ListenForEvents;
                worker.ProgressChanged += OnRecievedData;
                worker.RunWorkerAsync();
            }
        }

        private static void ListenForEvents(object sender, DoWorkEventArgs e)
        {
            while(true)
            {
                //string result = Encoding.ASCII.GetString(client.Receive(ref ipep));
            }
        }

        private static void OnRecievedData(object sender, ProgressChangedEventArgs e)
        {
            
        }
    }
}
