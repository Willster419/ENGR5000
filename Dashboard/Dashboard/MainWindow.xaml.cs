using System;
using System.Windows;
using System.Reflection;
using System.Windows.Controls;
using Dashboard.Resources;

namespace Dashboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        /// <summary>
        ///  Gets the version of the Assembely information for the Appliation
        /// </summary>
        private string GetApplicationVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
        /// <summary>
        /// Gets the time of which the application was compiled
        /// </summary>
        /// <returns></returns>
        private string GetCompileTime()
        {
            return CiInfo.BuildTag;
        }
        /// <summary>
        /// When the application is loaded for the first time
        /// </summary>
        /// <param name="sender">Stuff</param>
        /// <param name="e">More stuff</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Logging.InitLogging(this);
            Logging.LogConsole("/----------------------------------------------------------------------------------------------------------------------------------\\");
            Logging.LogConsole(string.Format("Dashboard Version {0}", GetApplicationVersion()));
            Logging.LogConsole("Built on " + GetCompileTime());
            Logging.LogConsole("Initializing network connections");
            Logging.LogRobot("/----------------------------------------------------------------------------------------------------------------------------------\\");
            NetworkUtils.InitComms(this);
        }
        /// <summary>
        /// When The application is closed
        /// </summary>
        /// <param name="sender">Stuff</param>
        /// <param name="e">Even more stuff</param>
        private void Window_Closed(object sender, EventArgs e)
        {
            Logging.LogConsole("Application Closing");
            Logging.LogConsole("\\----------------------------------------------------------------------------------------------------------------------------------/");
            Logging.LogRobot("\\----------------------------------------------------------------------------------------------------------------------------------/");
        }
        /// <summary>
        /// When the Robot Log output text is changed. Event used to scrolling to see the last entry in the log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RobotLogOutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            RobotLogOutput.ScrollToEnd();
        }
        /// <summary>
        /// When the Robot Log output text is changed. Event used to scrolling to see the last entry in the log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConsoleLogOutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            ConsoleLogOutput.ScrollToEnd();
        }
        /// <summary>
        /// On click of the button to clear the dashboard log output
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearDashboardLogOutput_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLogOutput.Clear();
            Logging.LogConsole("Console log cleared");
        }
        /// <summary>
        /// On lcick of button to clear dashboard log text file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteDashboardLogFile_Click(object sender, RoutedEventArgs e)
        {
            Logging.ClearConsoleLogFile();
        }
        /// <summary>
        /// On click of button to clear the robot log output
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearRobotLogOutput_Click(object sender, RoutedEventArgs e)
        {
            RobotLogOutput.Clear();
            Logging.LogRobot("Robot log cleared");
        }
        /// <summary>
        /// On click of button to clear the robot log text file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteRobotLogFile_Click(object sender, RoutedEventArgs e)
        {
            Logging.ClearRobotLogFile();
        }
        /// <summary>
        /// On click of button to start the process of resetting the network communication
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetNetworkConnection_Click(object sender, RoutedEventArgs e)
        {
            Logging.LogConsole("Resetting network connections...");
            NetworkUtils.ConnectionManager.CancelAsync();
        }
        /// <summary>
        /// On putton press to acivate the button press events of clearing both log displays
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearBothlogDisplays_Click(object sender, RoutedEventArgs e)
        {
            ClearRobotLogOutput_Click(null, null);
            ClearDashboardLogOutput_Click(null, null);
        }
        /// <summary>
        /// Used for when the network system has parsed diagnostic data from the robot
        /// </summary>
        /// <param name="data">The diagnostic data send from the robot</param>
        public void OnDiagnosticData(string[] data)
        {
            int i = 0;
            Channel0Data.Text = data[i++];
            Channel1Data.Text = data[i++];
            Channel2Data.Text = data[i++];
            Channel3Data.Text = data[i++];
            Channel4Data.Text = data[i++];
            Channel5Data.Text = data[i++];
            Channel6Data.Text = data[i++];
            Channel7Data.Text = data[i++];
            LeftDriveSign.Text = data[i++];
            LeftDriveMag.Text = data[i++];
            LeftDriveEncoder.Text = data[i++];
            RightDriveSign.Text = data[i++];
            RightDriveMag.Text = data[i++];
            RightDriveEncoder.Text = data[i++];
            Battery1Volts.Text = data[i++];
            Battery1Amps.Text = data[i++];
            Battery2Volts.Text = data[i++];
            Battery2Amps.Text = data[i++];
        }
        /// <summary>
        /// On click on checkbox when the user wants to request manual debug control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManualControlToggle_Checked(object sender, RoutedEventArgs e)
        {
            Logging.LogConsole("Starting Manual Control");
            ControlSystem.StartControl(this);
        }
        /// <summary>
        /// On click on checkbox when user wants to release manual debug contorl, back to automated system
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManualControlToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            //send stop of manual control
            ControlSystem.StopControl();
            ControlSystem.FirstJoystickMoveMent = true;
            OutputLeft.Text = "";
            OutputRight.Text = "";
            OutputMotor.Text = "";
        }
        /// <summary>
        /// On selection change from the combox for selecting which joystick to use
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Joysticks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Joysticks.SelectedIndex == -1)
                return;
            Logging.LogConsole("Init joystick index " + Joysticks.SelectedIndex);
            ControlSystem.EnableManualJoystickControl(Joysticks.SelectedIndex);
        }
        /// <summary>
        /// On enable click (check) of the checkbox to enable joystick control (over-ride the button or keyboard contorls)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JoystickToggle_Checked(object sender, RoutedEventArgs e)
        {
            //clear the list
            Joysticks.Items.Clear();
            ControlSystem.joystickDriveneable = true;
            ControlSystem.InitManualJoystickControl(this);
        }
        /// <summary>
        /// On disable click (uncheck) of the checkbox to disable jostick control (back to keyboard or buttons)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JoystickToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            Joysticks.SelectedIndex = -1;
            ControlSystem.joystickDriveneable = false;
            ControlSystem.StopJoystickControl();
        }
    }
}
