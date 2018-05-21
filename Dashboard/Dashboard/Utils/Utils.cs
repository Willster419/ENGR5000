using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.NetworkInformation;

namespace Dashboard
{
    public static class Utils
    {

        /// <summary>
        /// A refrence from MainWindow, the LogOutput TextBox from the MainWindow
        /// </summary>
        private static TextBox LogOutput = null;
        /// <summary>
        /// The Log Filestream for logging everything happening in the application
        /// </summary>
        private static FileStream LogStream = null;

        /// <summary>
        /// The Ip address of the robot
        /// </summary>
        private static string RobotIPV6Address = "";

        /// <summary>
        /// Initializes the Logging functions of the application
        /// </summary>
        /// <param name="logOutput">The Textbox from the MainWindow</param>
        public static void InitUtils(TextBox logOutput)
        {
            LogOutput = logOutput;
            LogStream = new FileStream("Dashboard.log", FileMode.Append, FileAccess.Write);
        }

        /// <summary>
        /// Logs a string value to the log output in the log tab, and to the log file
        /// </summary>
        /// <param name="textToLog">The text to log</param>
        /// <param name="debug">For use later, specify this to get a DEBUG header for that line</param>
        public static void Log(string textToLog, bool debug = false)
        {
            string dateTimeFormat = string.Format("{0:yyyy-MM-dd:HH-mm-ss}", DateTime.Now);
            if(debug)
            {
                textToLog = "DEBUG: " + textToLog;
            }
            textToLog = string.Format("{0}   DASHBOARD: {1}\r\n",dateTimeFormat,textToLog);
            LogOutput.AppendText(textToLog);
            LogStream.WriteAsync(Encoding.UTF8.GetBytes(textToLog), 0, Encoding.UTF8.GetByteCount(textToLog));
        }
        /// <summary>
        /// Gets the Robot's IP address
        /// </summary>
        /// <param name="hostname">The hostname of the Pi on the robot</param>
        public static void GetRobotIPAddress(string hostname)
        {
            Log("Getting Robot Ip address...");
            //ping the hostname, get the ip address in reply
            Ping p = new Ping();
            //p.SendAsync("minwinpc", null);
            PingReply reply = p.Send(hostname);
            if(reply.Status == IPStatus.Success)
            {
                Log("Ip address ping SUCCESS, address is " + reply.Address);
                RobotIPV6Address = reply.Address.ToString();
            }
            else
            {
                Log("ERROR, failed to get ip address of robot, (is it online?)");
            }
        }
    }
}
