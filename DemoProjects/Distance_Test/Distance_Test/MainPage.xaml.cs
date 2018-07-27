using Microsoft.IoT.Lightning.Providers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Distance_Test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            Loaded += LoadUserCode;
        }
        //constnats (not testing)
        private const float SECONDS_TO_MICROSECONDS = 1000000;
        private readonly float TICKS_PER_MICROSECOND = Stopwatch.Frequency / SECONDS_TO_MICROSECONDS;
        private const float MICROSECONDS_TO_CM = 0.01715F;
        //statics in use (testing)
        private GpioPin TriggerPin;//not used, RPi can't keep up
        private GpioPin EchoPin;
        private float distance_in_cm = 0F;
        private float session_microseconds;
        private Task SendTriggers;
        private Stopwatch distanceTimer;

        //averaging
        private float avg_val = 0;
        private float itteration = 1;
        private float num_to_normalize_to = 10;

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
            if (_controller == null)
                return;
            //TriggerPin = _controller.OpenPin(25);
            //TriggerPin.Write(GpioPinValue.Low);
            //TriggerPin.SetDriveMode(GpioPinDriveMode.Output);
            EchoPin = _controller.OpenPin(7);
            EchoPin.Write(GpioPinValue.Low);
            EchoPin.SetDriveMode(GpioPinDriveMode.Input);
            //disable for now...
            EchoPin.ValueChanged += OnEchoResponse;
            distanceTimer = new Stopwatch();
            distanceTimer.Reset();
            SendTriggers = new Task(() => FakePWM());
            //SendTriggers.Start();
        }

        private void OnEchoResponse(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            switch (args.Edge)
            {
                case GpioPinEdge.FallingEdge:
                    /*
                     * we are given ticks
                     * we need microseconds
                     * we have frequency -> ticks/second
                     * dividing by frequency gives us time
                     * ticks per second / 1000 = ticks per millisecond
                     * /1000 = ticks per microsecond
                     * now have frequency in ticks/microsecond
                     * OUR ticks / new frequency = microseconds!
                     */
                    session_microseconds = distanceTimer.ElapsedTicks / TICKS_PER_MICROSECOND;
                    //distance_in_cm = session_microseconds * MICROSECONDS_TO_CM;
                    distance_in_cm = distanceTimer.ElapsedMilliseconds;
                    if(itteration++ >= 10)
                    {
                        float real_value = avg_val / num_to_normalize_to;
                        if (real_value > 20F)
                            real_value = 20F;
                        var task = this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                        {
                            DistanceVal.Text = real_value.ToString();
                        });
                        itteration = 1;
                        avg_val = 0;
                    }
                    else
                    {
                        if (distance_in_cm > 20F)
                            distance_in_cm = 20F;
                        avg_val += distance_in_cm;
                    }
                    distanceTimer.Reset();
                    break;
                case GpioPinEdge.RisingEdge:
                    //it's a response, log it!
                    distanceTimer.Start();
                    break;
            }
        }
        private async void FakePWM()
        {
            while (true)
            {
                //1 tick = 100 ns = 0.1us
                //need a 10us pulse
                //10 tick = 1000ns = 1us
                //100 tick = 10us
                //1000 tick = 100us
                TriggerPin.Write(GpioPinValue.High);
                await Task.Delay(TimeSpan.FromTicks(100));
                TriggerPin.Write(GpioPinValue.Low);
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }
        }
    }
}
