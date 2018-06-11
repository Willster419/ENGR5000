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
        }
        Stopwatch sw = new Stopwatch();

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if(!GPIO.InitGPIO())
            {
                //https://stackoverflow.com/questions/32677597/how-to-exit-or-close-an-uwp-app-programmatically-windows-10
                Application.Current.Exit();
            }
            //init the robot networking
            if(!NetworkUtils.InitComms())
            {
                GPIO.RobotStatus = RobotStatus.Error;
                Application.Current.Exit();
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
            //check battery status of both devices

            NetworkUtils.LogNetwork("Initializing SPI interface", NetworkUtils.MessageType.Info);
            //http://blog.stephencleary.com/2012/07/dont-block-on-async-code.html
            if (!await GPIO.InitSPI().ConfigureAwait(false))
            {
                NetworkUtils.LogNetwork("SPI failed to intialize", NetworkUtils.MessageType.Info);
                GPIO.RobotStatus = RobotStatus.Error;
                Application.Current.Exit();
            }
            NetworkUtils.LogNetwork("SPI Interface initialization complete, reading one value", NetworkUtils.MessageType.Info);
            string message = "";
            while(true)
            {
                message = string.Format("Signal Voltage: {0}V", (GPIO.ReadVoltage(GPIO.SIGNAL_VOLTAGE_MONITOR_CHANNEL) / 1000.0F));
                System.Threading.Thread.Sleep(250);
                message = string.Format("Power Voltage: {0}V", (GPIO.ReadVoltage(GPIO.POWER_VOLTAGE_MONITOR_CHANNEL) / 1000.0F));
                System.Threading.Thread.Sleep(250);
                message = string.Format("Tempature Voltage: {0}V", (GPIO.ReadVoltage(GPIO.TEMPATURE_CHANNEL) / 1000.0F));
                System.Threading.Thread.Sleep(250);
                message = string.Format("Water Voltage: {0}V", (GPIO.ReadVoltage(GPIO.WATER_LEVEL_CHANNEL) / 1000.0F));
                System.Threading.Thread.Sleep(250);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            sw.Restart();
            Box.Text = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            sw.Stop();
            Box.Text = "" + sw.ElapsedMilliseconds;
            GPIO.RobotStatus = RobotStatus.Exception;
        }
    }
}
