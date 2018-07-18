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
using System.Diagnostics;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Encoder_Event_Test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private int counter = 0;
        private int numCorrectFiredEvents = 0;
        private GpioPin CLK;
        private GpioPin DT;
        byte[] values = new byte[] { 0, 0, 0, 0 };

        private static readonly byte[] ccw1 = new byte[] { 0, 0, 0, 1 };
        private static readonly byte[] ccw2 = new byte[] { 0, 1, 1, 1 };
        private static readonly byte[] ccw4 = new byte[] { 1, 1, 1, 0 };
        private static readonly byte[] ccw3 = new byte[] { 1, 0, 0, 0 };

        private static readonly byte[] cw1 = new byte[] { 0, 0, 1, 0 };
        private static readonly byte[] cw3 = new byte[] { 1, 0, 1, 1 };
        private static readonly byte[] cw4 = new byte[] { 1, 1, 0, 1 };
        private static readonly byte[] cw2 = new byte[] { 0, 1, 0, 0 };

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
            DispatcherTimer timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
            //TIMING INFO
            //10,000 ticks = 1ms = 1000us
            //1,000 ticks = 0.1ms = 100us
            //100 ticks = 0.01ms = 10us
            //900 ticks = 0.09ms = 90us
        }

        private void Timer_Tick(object sender, object e)
        {
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                CounterVal.Text = counter.ToString();
                DTVal.Text = numCorrectFiredEvents.ToString();
            });
        }

        private void CLK_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            //https://hifiduino.wordpress.com/2010/10/20/rotaryencoder-hw-sw-no-debounce/
            //DT first, then CLK
            //0,1 are old, 2,3 are new
            //DT,CLK,DT,CLK
            values[0] = values[2];
            values[1] = values[3];
            if(sender == DT)
            {
                values[2] = (byte)args.Edge;//1 = rising, 0 = falling
                values[3] = (byte)CLK.Read();
            }
            else
            {
                values[3] = (byte)args.Edge;//1 = rising, 0 = falling
                values[2] = (byte)DT.Read();
            }
            //values[2] = (byte)DT.Read();
            //values[3] = (byte)CLK.Read();
            //string together = string.Join("", values);
            if//CCW
            (
                values.SequenceEqual(ccw1) ||
                values.SequenceEqual(ccw2) ||
                values.SequenceEqual(ccw3) ||
                values.SequenceEqual(ccw4)
            )
            {
                numCorrectFiredEvents++;
                counter++;
            }
            else if//CW
            (
                values.SequenceEqual(cw1) ||
                values.SequenceEqual(cw2) ||
                values.SequenceEqual(cw3) ||
                values.SequenceEqual(cw4)
            )
            {
                numCorrectFiredEvents++;
                counter--;
            }
        }
        private void CorrectTicks()
        {
            while(counter%4 != 0)
            {
                counter++;
            }
        }
    }
}
