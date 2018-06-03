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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if(!GPIO.InitGPIO())
            {
                //https://stackoverflow.com/questions/32677597/how-to-exit-or-close-an-uwp-app-programmatically-windows-10
                Application.Current.Exit();
            }
            //init the robot networking
            if(!NetworkUtils.InitComms())
            {
                Application.Current.Exit();
            }
            //DEBUG: wait for dashboard logging connection
            if(NetworkUtils.DEBUG_FORCE_DASHBOARD_CONNECT)
            {
                while(!NetworkUtils.DashboardConnected)
                {
                    System.Threading.Thread.Sleep(100);
                }
                NetworkUtils.LogNetwork("DEBUG: dashboard connected via force wait");
            }
            
            NetworkUtils.LogNetwork("Initializing SPI interface");
            GPIO.InitSPI();
            while (!GPIO.SPI_Initialized)
                System.Threading.Thread.Sleep(100);
            if(!GPIO.SPI_works)
            {
                NetworkUtils.LogNetwork("SPI failed to intialize");
                Application.Current.Exit();
            }
            NetworkUtils.LogNetwork("SPI Interface initialization complete, reading one value");
            while(true)
            {
                NetworkUtils.LogNetwork("" + GPIO.ReadVoltage(0));
                System.Threading.Thread.Sleep(1000);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            sw.Restart();
            Box.Text = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            sw.Stop();
            Box.Text = "" + sw.ElapsedMilliseconds;
        }
    }
}
