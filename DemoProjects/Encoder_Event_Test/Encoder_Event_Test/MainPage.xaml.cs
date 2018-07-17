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
using Windows.UI.Core;


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
        private DispatcherTimer _debounceTimout;
        byte[] values = new byte[] { 0, 0, 0, 0 };
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
            CLK.SetDriveMode(GpioPinDriveMode.Input);
            DT.SetDriveMode(GpioPinDriveMode.Input);
            CLK.ValueChanged += CLK_ValueChanged;
            DT.ValueChanged += CLK_ValueChanged;
            //GpioChangeReader reader = new GpioChangeReader(DT);
            _debounceTimout = new DispatcherTimer();
            _debounceTimout.Interval = TimeSpan.FromMilliseconds(1);
            _debounceTimout.Tick += _debounceTimout_Tick;
            //_debounceTimout.Start();
            //try polling at 1ms rate...
        }

        private void CLK_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            //https://hifiduino.wordpress.com/2010/10/20/rotaryencoder-hw-sw-no-debounce/
            //DT first, then CLK
            //0,1 are old, 2,3 are new
            //DT,CLK,DT,CLK
            values[0] = values[2];
            values[1] = values[3];
            values[2] = (byte)DT.Read();
            values[3] = (byte)CLK.Read();
            string together = string.Join("", values);
            if//CCW
            (
                together.Equals("0001") ||
                together.Equals("0111") ||
                together.Equals("1000") ||
                together.Equals("1110")
            )
            {
                counter++;
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    CounterVal.Text = counter.ToString();
                });
            }
            else if//CW
            (
                together.Equals("0010") ||
                together.Equals("0100") ||
                together.Equals("1011") ||
                together.Equals("1101")
            )
            {
                counter--;
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    CounterVal.Text = counter.ToString();
                });
            }
        }

        private void _debounceTimout_Tick(object sender, object e)
        {
            //DT first, then CLK
            //0,1 are old, 2,3 are new
            //DT,CLK,DT,CLK
            values[0] = values[2];
            values[1] = values[3];
            values[2] = (byte)DT.Read();
            values[3] = (byte)CLK.Read();
            string together = string.Join("", values);
            if//CCW
            (
                together.Equals("0001") ||
                together.Equals("0111") ||
                together.Equals("1000") ||
                together.Equals("1110")
            )
            {
                counter++;
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    CounterVal.Text = counter.ToString();
                });
            }
            else if//CW
            (
                together.Equals("0010") ||
                together.Equals("0100") ||
                together.Equals("1011") ||
                together.Equals("1101")
            )
            {
                counter--;
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    CounterVal.Text = counter.ToString();
                });
            }
        }
    }
}
