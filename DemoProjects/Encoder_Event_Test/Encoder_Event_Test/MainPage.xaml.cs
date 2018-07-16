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
using Windows.Devices.Gpio;
using Microsoft.IoT.Lightning;
using Microsoft.IoT.Lightning.Providers;
using Windows.Devices;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Encoder_Event_Test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private int counter = 0;
        private GpioPin CLK;
        private GpioPin DT;
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += LoadUserCode;
        }

        private void LoadUserCode(object sender, RoutedEventArgs e)
        {
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            }
            else
            {
                return;
            }
            GpioController _controller = GpioController.GetDefault();
            CLK = _controller.OpenPin(20);
            DT = _controller.OpenPin(26);
            //TODO: maybe set debounce to 1ms test? i can't rotate the encoder at 1000 Hz, maybe 100...
            //CLK.DebounceTimeout = TimeSpan.FromTicks(500);
            //DT.DebounceTimeout = TimeSpan.FromTicks(500);
            CLK.SetDriveMode(GpioPinDriveMode.Input);
            DT.SetDriveMode(GpioPinDriveMode.Input);
            //CLK.ValueChanged += CLK_ValueChanged;
            DT.ValueChanged += DT_ValueChanged;
            DispatcherTimer _timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += _timer_Tick;
            _timer.Start();
            //GpioChangeReader reader = new GpioChangeReader(DT);
        }

        private void _timer_Tick(object sender, object e)
        {
            LogBox.Text = "Counter = " + counter.ToString();
        }

        private void DT_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            //LogBox.Text = "DT_ValueChanged: " + args.Edge.ToString() + "\r\n";
            if(args.Edge == GpioPinEdge.RisingEdge)
            {
                //LogBox.Text = CLK.Read().ToString();
                if(CLK.Read() == GpioPinValue.Low)
                {
                    counter++;
                    //LogBox.Text = "Counter = " + counter++.ToString();
                }
                else
                {
                    counter--;
                    //LogBox.Text = "Counter = " + counter--.ToString();
                }
            }
        }
    }
}
