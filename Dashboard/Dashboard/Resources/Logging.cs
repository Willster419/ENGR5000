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
        /// The Log Filestream for writing diagnostic robot data
        /// </summary>
        private static FileStream RobotDataStream = null;
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
        /// <summary>
        /// The directory where all robot log files are kept
        /// </summary>
        private const string DATA_LOG_FOLDER_NAME = "log_files";
        /// <summary>
        /// The Starting prefix for all robot data log files
        /// </summary>
        private const string DATA_LOG_FILE_START = "robot_data_";
        /// <summary>
        /// The file suffix extension for the robot data log files
        /// </summary>
        private const string DATA_LOG_FILE_EXTENSION = ".csv";
        //https://stackoverflow.com/questions/938421/getting-the-applications-directory-from-a-wpf-application
        /// <summary>
        /// The Startup path of the application
        /// </summary>
        private static readonly string ApplicationPath = AppDomain.CurrentDomain.BaseDirectory;
        /// <summary>
        /// The parsed absolute path to the data log folder
        /// </summary>
        private static readonly string DATA_LOG_FOLDER_PATH = Path.Combine(ApplicationPath, DATA_LOG_FOLDER_NAME);
        /// <summary>
        /// The name of the currently open data file
        /// </summary>
        private static string CurrentDataFilename = "";
        /// <summary>
        /// The absolute full path of the current open data file
        /// </summary>
        private static string CurrentDataFullFilePath = "";
        /// <summary>
        /// The character seperater for the csv file
        /// </summary>
        private const string DATA_SEP_CHAR = ",";
        /// <summary>
        /// The Line endings to use for each data entry
        /// </summary>
        private const string DATA_LINE_ENDING = "\r\n";
        /// <summary>
        /// The Header for the data log file. contains all header info for all logged data
        /// </summary>
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
            "CH6",
            "CH7",
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
            "GyroZ",
            "CollectionMotor1",
            "CollectionMotor2",
            "VelocityX",
            "VelocityY",
            "VelocityZ",
            "RotationX",
            "RotationY",
            "RotationZ",
            "Tempature_MPU",
            "SideWall",
            "FrontWall",
            "Proximity",
            "Positionx",
            "Positiony",
            "Positionz",
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
        /// <summary>
        /// Initialize a new data logfile by making a new name and applying the header infromation to the file
        /// </summary>
        public static void InitNewDataLogFile()
        {
            if (!Directory.Exists(DATA_LOG_FOLDER_PATH))
                Directory.CreateDirectory(DATA_LOG_FOLDER_PATH);
            string dateTimeFormat = string.Format("{0:yyyy_MM_dd-HH_mm_ss}", DateTime.Now);
            CurrentDataFilename = DATA_LOG_FILE_START + dateTimeFormat + DATA_LOG_FILE_EXTENSION;
            CurrentDataFullFilePath = Path.Combine(DATA_LOG_FOLDER_PATH, CurrentDataFilename);
            string toWrite = string.Join(DATA_SEP_CHAR, DATA_HEADER) + DATA_LINE_ENDING;
            RobotDataStream = new FileStream(CurrentDataFullFilePath, FileMode.Append, FileAccess.Write);
            RobotDataStream.Write(Encoding.UTF8.GetBytes(toWrite), 0, Encoding.UTF8.GetByteCount(toWrite));
            RobotDataStream.Flush();
            LogConsole("New data log file created");
        }
        /// <summary>
        /// Write a data entry to the currently open file, if an open file exists
        /// </summary>
        /// <param name="data"></param>
        public static void WriteDataLogEntry(string[] data)
        {
            if (string.IsNullOrEmpty(CurrentDataFullFilePath))
            {
                LogConsole("ERROR: WriteDataLogEntry called but CurrentDataFullFilePath is null");
                return;
            }
            if (!File.Exists(CurrentDataFullFilePath))
            {
                LogConsole("ERROR: WriteDataLogEntry called but file does not exist!");
                return;
            }
            //format for date
            string dateFormat = string.Format("{0:yyyy-MM-dd}", DateTime.Now);
            //format for time
            string timeFormat = string.Format("{0:HH-mm-ss.fff}", DateTime.Now);
            //combine reslutls with data
            string[] dataToWrite = new string[data.Length + 2];
            dataToWrite[0] = dateFormat;
            dataToWrite[1] = timeFormat;
            data.CopyTo(dataToWrite, 2);
            string toWrite = string.Join(DATA_SEP_CHAR, dataToWrite) + DATA_LINE_ENDING;
            RobotDataStream.Write(Encoding.UTF8.GetBytes(toWrite), 0, Encoding.UTF8.GetByteCount(toWrite));
            RobotDataStream.Flush();
        }
        #endregion
    }
}
