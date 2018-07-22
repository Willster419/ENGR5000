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
        private const float SECONDS_TO_MICROSECONDS = 1000000;
        private readonly float TICKS_PER_MICROSECOND = Stopwatch.Frequency / SECONDS_TO_MICROSECONDS;
        private const float MICROSECONDS_TO_CM = 0.01715F;
        private GpioPin TriggerPin;
        private GpioPin EchoPin;
        private Task SendTriggers;
        private float distance_in_cm = 0F;
        private float session_microseconds;
        private bool enable = false;
        private Stopwatch distanceTimer;
        private System.Timers.Timer debounceTimer;
        private DispatcherTimer _timer = new DispatcherTimer();
        int lastTicks = 0;
        private GpioPinEdge lastEdge;

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

            TriggerPin = _controller.OpenPin(21);
            TriggerPin.Write(GpioPinValue.Low);
            TriggerPin.SetDriveMode(GpioPinDriveMode.Output);
            EchoPin = _controller.OpenPin(4);
            EchoPin.Write(GpioPinValue.Low);
            //EchoPin.DebounceTimeout = TimeSpan.FromMilliseconds(0.01);
            EchoPin.SetDriveMode(GpioPinDriveMode.Input);
            //EchoPin.DebounceTimeout = TimeSpan.FromTicks(1000);
            EchoPin.ValueChanged += OnEchoResponse;
            enable = true;
            distanceTimer = new Stopwatch();
            distanceTimer.Reset();
            debounceTimer = new System.Timers.Timer();
            debounceTimer.Interval = 0.01;
            debounceTimer.AutoReset = false;
            debounceTimer.Elapsed += OnDebounceReset;
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += _timer_Tick;
            _timer.Start();
            //https://www.c-sharpcorner.com/article/ultrasonic-proximity-sensors-in-iot-context-raspberry-pi/
            SendTriggers = new Task(() =>
           {
               while (enable)
               {
                   TriggerPin.Write(GpioPinValue.High);
                   Task.Delay(TimeSpan.FromMilliseconds(0.01)).Wait();
                   TriggerPin.Write(GpioPinValue.Low);
                   Task.Delay(TimeSpan.FromMilliseconds(200)).Wait();
               }
           });
            SendTriggers.Start();
        }

        private void _timer_Tick(object sender, object e)
        {
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                DistanceVal.Text = "Time: " + lastTicks;
            });
        }

        private void OnDebounceReset(object sender, System.Timers.ElapsedEventArgs e)
        {
            EchoPin.ValueChanged += OnEchoResponse;
            debounceTimer.Stop();
        }

        private void OnEchoResponse(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            //EchoPin.ValueChanged -= OnEchoResponse;
            //debounceTimer.Start();
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
                     if(lastEdge == GpioPinEdge.FallingEdge)
                    {

                    }
                    lastTicks = (int)distanceTimer.ElapsedTicks;
                    //session_microseconds = distanceTimer.ElapsedTicks / TICKS_PER_MICROSECOND;
                    //distance_in_cm = session_microseconds * MICROSECONDS_TO_CM;
                    distanceTimer.Reset();
                    break;
                case GpioPinEdge.RisingEdge:
                    //it's a response, log it!
                    distanceTimer.Start();
                    break;
            }
            lastEdge = args.Edge;
        }
    }
}
