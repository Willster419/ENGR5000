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
        /// <summary>
        /// Locker object used to prevent multiple access to the console file stream
        /// </summary>
        private static readonly object ConsoleLocker = new object();
        /// <summary>
        /// Locker object used to prevent multiple access to the robot file stream
        /// </summary>
        private static readonly object RobotLocker = new object();
        /// <summary>
        /// The name of the dashboard log file
        /// </summary>
        private const string DASHBOARD_LOG_FILE = "Dashboard.log";
        /// <summary>
        /// The name of the robot log file
        /// </summary>
        private const string ROBOT_LOG_FILE = "Robot.log";
        private const string DATA_LOG_FOLDER_NAME = "log_files";
        private const string DATA_LOG_FILE_START = "robot_data_";
        private const string DATA_LOG_FILE_EXTENSION = ".csv";
        //https://stackoverflow.com/questions/938421/getting-the-applications-directory-from-a-wpf-application
        private static readonly string ApplicationPath = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string DATA_LOG_FOLDER_PATH = Path.Combine(ApplicationPath, DATA_LOG_FOLDER_NAME);
        private static string CurrentDataFilename = "";
        private static string CurrentDataFullFilePath = "";
        private const string DATA_SEP_CHAR = ",";
        private const string DATA_LINE_ENDING = "\r\n";
        private static readonly string[] DATA_HEADER = new string[]
        {
            "Date",
            "Time",
            "ControlMode",
            "RobotStatus",
            "RobotAutoControlStage",
            "Battery1VoltsRaw",
            "Battery1AmpsRaw",
            "Battery2VoltsRaw",
            "Battery2AmpsRaw",
            "WaterLevel",
            "TempatureRaw",
            "LeftDriveSign",
            "LeftDriveMag",
            "LeftDriveEncoder",
            "RightDriveSign",
            "RightDriveMag",
            "RightDriveEncoder",
            "Battery1Volts",
            "Battery1Amps",
            "Battery2Volts",
            "Battery2Amps",
            "AccelX",
            "AccelY",
            "AccelZ",
            "GyroX",
            "GyroY",
            "GyroZ"
        };
        #region Console Logging
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
        /// <summary>
        /// Clears the console log file by releasing filestreamrescources, deleting the file, and renewing the filestream
        /// </summary>
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
        /// <summary>
        /// Clears the robot log file by releasing filestreamrescources, deleting the file, and renewing the filestream
        /// </summary>
        public static void ClearRobotLogFile()
        {
            RobotLogSteam.Flush();
            RobotLogSteam.Close();
            RobotLogSteam.Dispose();
            File.Delete(ROBOT_LOG_FILE);
            RobotLogSteam = new FileStream(ROBOT_LOG_FILE, FileMode.Append, FileAccess.Write);
            RobotLogOutput.AppendText("Log file cleared");
        }
        #endregion

        #region Data logging
        public static void InitNewDataLogFile()
        {
            if (!Directory.Exists(DATA_LOG_FOLDER_PATH))
                Directory.CreateDirectory(DATA_LOG_FOLDER_PATH);
            string dateTimeFormat = string.Format("{0:yyyy-MM-dd:HH-mm-ss}", DateTime.Now);
            CurrentDataFilename = DATA_LOG_FILE_START + dateTimeFormat + DATA_LOG_FILE_EXTENSION;
            CurrentDataFullFilePath = Path.Combine(DATA_LOG_FOLDER_PATH, CurrentDataFilename);
            File.WriteAllText(CurrentDataFullFilePath, string.Join(DATA_SEP_CHAR, DATA_HEADER) + DATA_LINE_ENDING);
        }
        public static void WriteDataLogEntry(string[] data)
        {
            //format for date
            string dateFormat = string.Format("{0:yyyy-MM-dd}", DateTime.Now);
            //format for time
            string timeFormat = string.Format("{0:HH-mm-ss.fff}", DateTime.Now);
            //combine reslutls with data
            string[] dataToWrite = new string[data.Length + 2];
            dataToWrite[0] = dateFormat;
            dataToWrite[1] = timeFormat;
            data.CopyTo(dataToWrite, 2);
            File.WriteAllText(CurrentDataFullFilePath, string.Join(DATA_SEP_CHAR, dataToWrite)+DATA_LINE_ENDING);
        }
        #endregion
    }
}
