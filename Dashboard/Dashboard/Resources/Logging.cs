using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net.NetworkInformation;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dashboard
{
    
    /// <summary>
    /// A Utility class for important static methods and global fields
    /// </summary>
    public static class Logging
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
        private static object ConsoleLocker = new object();
        private static object RobotLocker = new object();
        private const string DASHBOARD_LOG_FILE = "Dashboard.log";
        private const string ROBOT_LOG_FILE = "Robot.log";
        
        /// <summary>
        /// Initializes the Logging functions of the application
        /// </summary>
        /// <param name="logOutput">The Textbox from the MainWindow</param>
        public static void InitLogging(MainWindow @MainWindow)
        {
            ConsoleLogOutput = MainWindow.ConsoleLogOutput;
            RobotLogOutput = MainWindow.RobotLogOutput;
            ConsoleLogStream = new FileStream(DASHBOARD_LOG_FILE, FileMode.Append, FileAccess.Write);
            RobotLogSteam = new FileStream(ROBOT_LOG_FILE, FileMode.Append, FileAccess.Write);
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
            lock(ConsoleLocker)
            {
                ConsoleLogOutput.AppendText(textToLog);
                ConsoleLogStream.Write(Encoding.UTF8.GetBytes(textToLog), 0, Encoding.UTF8.GetByteCount(textToLog));
                ConsoleLogStream.Flush();
            }
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
            lock(RobotLocker)
            {
                RobotLogOutput.AppendText(textToLog);
                RobotLogSteam.Write(Encoding.UTF8.GetBytes(textToLog), 0, Encoding.UTF8.GetByteCount(textToLog));
                RobotLogSteam.Flush();
            }
        }

        public static void ClearConsoleLogFile()
        {
            //close the stream, delete the file, and open it again
            ConsoleLogStream.Flush();
            ConsoleLogStream.Close();
            ConsoleLogStream.Dispose();
            File.Delete(DASHBOARD_LOG_FILE);
            ConsoleLogStream = new FileStream(DASHBOARD_LOG_FILE, FileMode.Append, FileAccess.Write);
            ConsoleLogOutput.AppendText("Log file cleared");
        }

        public static void ClearRobotLogFile()
        {
            RobotLogSteam.Flush();
            RobotLogSteam.Close();
            RobotLogSteam.Dispose();
            File.Delete(ROBOT_LOG_FILE);
            RobotLogSteam = new FileStream(ROBOT_LOG_FILE, FileMode.Append, FileAccess.Write);
            RobotLogOutput.AppendText("Log file cleared");
        }

    }
}
