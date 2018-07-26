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
using System.Threading.Tasks;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Encoder_Event_Test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //both i guess
        public const int LEFT_CLK = 26;
        public const int LEFT_DT = 19;
        public const int RIGHT_CLK = 20;
        public const int RIGHT_DT = 16;
        //left
        private int LeftCounter = 0;//if this value goes to anything it means a click was missed
        private int LeftCLKFire = 0;
        private int LeftDTFire = 0;
        private int LeftNumCorrectFiredEvents = 0;
        private GpioPin LeftCLK;
        private GpioPin LeftDT;
        //right
        private int RightCounter = 0;
        private int RightCLKFire = 0;
        private int RightDTFire = 0;
        private int RightNumCorrectFiredEvents = 0;
        private GpioPin RightCLK;
        private GpioPin RightDT;
        //state filtering
        byte[] LeftValues = new byte[] { 0, 0, 0, 0 };
        byte[] RightValues = new byte[] { 0, 0, 0, 0 };
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
            InitializeComponent();
            Loaded += LoadUserCode;
        }

        private void LoadUserCode(object sender, RoutedEventArgs e)
        {
            //lighting driver stuff
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            }
            else
            {
                return;
            }
            //need a controller
            GpioController _controller = GpioController.GetDefault();
            //left pins
            LeftCLK = _controller.OpenPin(LEFT_CLK);
            LeftCLK.SetDriveMode(GpioPinDriveMode.Input);
            LeftCLK.ValueChanged += LeftValueChange;
            LeftDT = _controller.OpenPin(LEFT_DT);
            LeftDT.SetDriveMode(GpioPinDriveMode.Input);
            LeftDT.ValueChanged += LeftValueChange;
            //right pins
            RightCLK = _controller.OpenPin(RIGHT_CLK);
            RightCLK.SetDriveMode(GpioPinDriveMode.Input);
            RightCLK.ValueChanged += RightValueChange;
            RightDT = _controller.OpenPin(RIGHT_DT);
            RightDT.SetDriveMode(GpioPinDriveMode.Input);
            RightDT.ValueChanged += RightValueChange;
            //polling updates like in the robot
            DispatcherTimer timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(20)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
            //TIMING INFO
            //10,000 ticks = 1ms = 1000us
            //1,000 ticks = 0.1ms = 100us
            //100 ticks = 0.01ms = 10us
            //900 ticks = 0.09ms = 90us
        }

        private void RightValueChange(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (args.Edge == GpioPinEdge.FallingEdge)
            {
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    //https://hifiduino.wordpress.com/2010/10/20/rotaryencoder-hw-sw-no-debounce/
                    //DT first, then CLK
                    //0,1 are old, 2,3 are new
                    //DT,CLK,DT,CLK
                    RightValues[0] = RightValues[2];
                    RightValues[1] = RightValues[3];
                    if (sender == RightDT)
                    {
                        RightValues[2] = (byte)args.Edge;//1 = rising, 0 = falling
                        RightValues[3] = (byte)RightCLK.Read();
                        RightDTFire++;
                    }
                    else
                    {
                        RightValues[3] = (byte)args.Edge;//1 = rising, 0 = falling
                        RightValues[2] = (byte)RightDT.Read();
                        RightCLKFire++;
                    }
                    if//CCW
                    (
                        RightValues.SequenceEqual(ccw1) ||
                        RightValues.SequenceEqual(ccw2) ||
                        RightValues.SequenceEqual(ccw3) ||
                        RightValues.SequenceEqual(ccw4)
                    )
                    {
                        RightNumCorrectFiredEvents++;
                        RightCounter--;
                    }
                    else if//CW
                    (
                        RightValues.SequenceEqual(cw1) ||
                        RightValues.SequenceEqual(cw2) ||
                        RightValues.SequenceEqual(cw3) ||
                        RightValues.SequenceEqual(cw4)
                    )
                    {
                        RightNumCorrectFiredEvents++;
                        RightCounter++;
                    }
                });
            }
        }

        private void LeftValueChange(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (args.Edge == GpioPinEdge.FallingEdge)
            {
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    //https://hifiduino.wordpress.com/2010/10/20/rotaryencoder-hw-sw-no-debounce/
                    //DT first, then CLK
                    //0,1 are old, 2,3 are new
                    //DT,CLK,DT,CLK
                    LeftValues[0] = LeftValues[2];
                    LeftValues[1] = LeftValues[3];
                    if (sender == LeftDT)
                    {
                        LeftValues[2] = (byte)args.Edge;//1 = rising, 0 = falling
                        LeftValues[3] = (byte)LeftCLK.Read();
                        LeftDTFire++;
                    }
                    else
                    {
                        LeftValues[3] = (byte)args.Edge;//1 = rising, 0 = falling
                        LeftValues[2] = (byte)LeftDT.Read();
                        LeftCLKFire++;
                    }
                    if//CCW
                    (
                        LeftValues.SequenceEqual(ccw1) ||
                        LeftValues.SequenceEqual(ccw2) ||
                        LeftValues.SequenceEqual(ccw3) ||
                        LeftValues.SequenceEqual(ccw4)
                    )
                    {
                        LeftNumCorrectFiredEvents++;
                        LeftCounter++;
                    }
                    else if//CW
                    (
                        LeftValues.SequenceEqual(cw1) ||
                        LeftValues.SequenceEqual(cw2) ||
                        LeftValues.SequenceEqual(cw3) ||
                        LeftValues.SequenceEqual(cw4)
                    )
                    {
                        LeftNumCorrectFiredEvents++;
                        LeftCounter--;
                    }
                });
            }
        }
        //updating the UI thread via polling
        private void Timer_Tick(object sender, object e)
        {
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                LeftCounterVal.Text = LeftCounter.ToString();
                LeftDTVal.Text = LeftDTFire.ToString();
                LeftCLKVal.Text = LeftCLKFire.ToString();
                LeftCorrectEventsVal.Text = LeftNumCorrectFiredEvents.ToString();

                RightCounterVal.Text = RightCounter.ToString();
                RightDTVal.Text = RightDTFire.ToString();
                RightCLKVal.Text = RightCLKFire.ToString();
                RightCorrectEventsVal.Text = RightNumCorrectFiredEvents.ToString();
            });
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            LeftCounter = 0;
            LeftDTFire = 0;
            LeftCLKFire = 0;
            LeftNumCorrectFiredEvents = 0;
            RightCounter = 0;
            RightDTFire = 0;
            RightCLKFire = 0;
            RightNumCorrectFiredEvents = 0;
        }
        //Interesting idea
        private void CorrectTicks()
        {
           while(LeftCounter%4 != 0)
           {
               LeftCounter++;
           }
        }
    }
}