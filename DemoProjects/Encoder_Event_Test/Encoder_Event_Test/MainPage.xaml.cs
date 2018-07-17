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
        private int numCLKFire = 0;
        private int numDTFire = 0;
        private GpioPin CLK;
        private GpioPin DT;
        private DispatcherTimer _debounceTimout;
        private volatile bool acceptingNewValue = true;
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
            //CLK.DebounceTimeout = TimeSpan.FromMilliseconds(1000);//DOES NOT WORK, MICROSOFT ISSUE :<
            //DT.DebounceTimeout = TimeSpan.FromMilliseconds(1000);
            CLK.SetDriveMode(GpioPinDriveMode.Input);
            DT.SetDriveMode(GpioPinDriveMode.Input);
            //CLK.ValueChanged += CLK_ValueChanged;
            DT.ValueChanged += DT_ValueChanged;
            //GpioChangeReader reader = new GpioChangeReader(DT);
            _debounceTimout = new DispatcherTimer();
            _debounceTimout.Interval = TimeSpan.FromMilliseconds(50);
            _debounceTimout.Tick += (sender2, args) =>
            {
                _debounceTimout.Stop();
                acceptingNewValue = true;
            };
        }

        private void DT_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (args.Edge == GpioPinEdge.FallingEdge && CLK.Read() == GpioPinValue.High && acceptingNewValue)
            {
                acceptingNewValue = false;
                _debounceTimout.Start();
                counter++;
                numDTFire++;
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    DTVal.Text = numDTFire.ToString();
                    CounterVal.Text = counter.ToString();
                });
            }
        }

        private void CLK_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            //falling edge is what triggers the change = LED ON = now low value
            //rising edge = LED OFF = low to high = now high value
            if(args.Edge == GpioPinEdge.FallingEdge && DT.Read() == GpioPinValue.High && acceptingNewValue)
            {
                acceptingNewValue = false;
                _debounceTimout.Start();
                counter--;
                numCLKFire++;
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    CLKVal.Text = numCLKFire.ToString();
                    CounterVal.Text = counter.ToString();
                });
            }
        }
    }
}
