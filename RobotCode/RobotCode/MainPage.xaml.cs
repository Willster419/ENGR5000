using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using System.Threading.Tasks;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RobotCode
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }
        Stopwatch sw = new Stopwatch();

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if(!GPIO.InitGPIO())
            {
                //https://stackoverflow.com/questions/32677597/how-to-exit-or-close-an-uwp-app-programmatically-windows-10
                Application.Current.Exit();
                return;
            }
            //init the robot networking
            if(!NetworkUtils.InitComms())
            {
                RobotController.RobotStatus = RobotStatus.Error;
                Application.Current.Exit();
                return;
            }
            //DEBUG: wait for dashboard logging connection
            if(NetworkUtils.DEBUG_FORCE_DASHBOARD_CONNECT)
            {
                while(!NetworkUtils.DashboardConnected)
                {
                    System.Threading.Thread.Sleep(100);
                }
                NetworkUtils.LogNetwork("dashboard connected via force wait", NetworkUtils.MessageType.Debug);
                System.Threading.Thread.Sleep(100);//delay to show it...
            }
            NetworkUtils.LogNetwork("Initializing SPI interface", NetworkUtils.MessageType.Info);
            //http://blog.stephencleary.com/2012/07/dont-block-on-async-code.html
            //https://docs.microsoft.com/en-us/uwp/api/windows.devices.enumeration.deviceinformation.findallasync
            //https://stackoverflow.com/questions/33587832/prevent-winforms-ui-block-when-using-async-await
            if (!GPIO.InitSPI())
            {
                NetworkUtils.LogNetwork("SPI failed to intialize", NetworkUtils.MessageType.Error);
                RobotController.RobotStatus = RobotStatus.Error;
                Application.Current.Exit();
                return;
            }
            NetworkUtils.LogNetwork("SPI Interface initialization complete, loading PWM", NetworkUtils.MessageType.Info);
            //check battery status of both devices

            //init pwm
            if (!GPIO.InitPWM())
            {
                NetworkUtils.LogNetwork("PWM failed to intialize", NetworkUtils.MessageType.Error);
                RobotController.RobotStatus = RobotStatus.Error;
                Application.Current.Exit();
                return;
            }
            NetworkUtils.LogNetwork("PWM loading complete, Initializing Main Robot controller", NetworkUtils.MessageType.Info);
            if (!RobotController.InitController())
            {
                NetworkUtils.LogNetwork("Controller failed to intialize", NetworkUtils.MessageType.Error);
                RobotController.RobotStatus = RobotStatus.Error;
                Application.Current.Exit();
                return;
            }
            NetworkUtils.LogNetwork("Controller initialized, system online", NetworkUtils.MessageType.Info);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            sw.Restart();
            Box.Text = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            sw.Stop();
            Box.Text = "" + sw.ElapsedMilliseconds;
            RobotController.RobotStatus = RobotStatus.Exception;
        }
    }
}
