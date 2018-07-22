using Microsoft.IoT.Lightning.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace IR_test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private GpioPin TriggerPin;
        private int UpCountInt = 0;
        private int DownCountInt = 0;
        private int TotalCountInt = 0;
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
            if (_controller == null)
                return;

            TriggerPin = _controller.OpenPin(21);
            TriggerPin.Write(GpioPinValue.Low);
            TriggerPin.SetDriveMode(GpioPinDriveMode.Input);
            TriggerPin.ValueChanged += TriggerPin_ValueChanged;
        }

        private void TriggerPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            switch(args.Edge)
            {
                case GpioPinEdge.FallingEdge://low value
                    TotalCountInt--;
                    DownCountInt++;
                    break;
                case GpioPinEdge.RisingEdge://high value
                    TotalCountInt++;
                    UpCountInt++;
                    break;
            }
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                DistanceVal.Text = args.Edge.ToString();
                DownCount.Text = "Down: " + DownCountInt.ToString();
                UpCount.Text = "Up: " + UpCountInt.ToString();
                TotalCount.Text = "Total: " + TotalCountInt.ToString();
            });
        }
    }
}
