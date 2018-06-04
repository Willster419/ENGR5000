using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using RelhaxModpack;

namespace Dashboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Boring Stuff
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
            NetworkUtils.StartRobotNetworking();
        }
        /// <summary>
        /// When The application is closed
        /// </summary>
        /// <param name="sender">Stuff</param>
        /// <param name="e">Even more stuff</param>
        private void Window_Closed(object sender, EventArgs e)
        {
            Logging.LogConsole("Application Closing");
            NetworkUtils.AbortNetworkThreads();
            Logging.LogConsole("\\----------------------------------------------------------------------------------------------------------------------------------/");
            Logging.LogRobot("\\----------------------------------------------------------------------------------------------------------------------------------/");
        }
        #endregion

        private void RobotLogOutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            RobotLogOutput.ScrollToEnd();
        }

        private void ConsoleLogOutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            ConsoleLogOutput.ScrollToEnd();
        }
    }
}
