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
        private GpioPin SideIR;
        private GpioPin FrontIR;
        private int SideUpCountInt = 0;
        private int SideDownCountInt = 0;
        private int SideTotalCount = 0;
        private int FrontUpCountInt = 0;
        private int FrontDownCountInt = 0;
        private int FrontTotalCountInt = 0;
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

            SideIR = _controller.OpenPin(21);
            SideIR.Write(GpioPinValue.Low);
            SideIR.SetDriveMode(GpioPinDriveMode.Input);
            SideIR.ValueChanged += TriggerPin_ValueChanged;

            FrontIR = _controller.OpenPin(4);
            FrontIR.Write(GpioPinValue.Low);
            FrontIR.SetDriveMode(GpioPinDriveMode.Input);
            FrontIR.ValueChanged += OnFrontIRChange;
        }

        private void OnFrontIRChange(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            switch (args.Edge)
            {
                case GpioPinEdge.FallingEdge://low value
                    FrontTotalCountInt--;
                    FrontDownCountInt++;
                    break;
                case GpioPinEdge.RisingEdge://high value
                    FrontTotalCountInt++;
                    FrontUpCountInt++;
                    break;
            }
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                FrontEdge.Text = args.Edge.ToString();
                FrontDownCount.Text = "Down: " + FrontDownCountInt.ToString();
                FrontUpCount.Text = "Up: " + FrontUpCountInt.ToString();
                FrontTotalCount.Text = "Total: " + FrontTotalCountInt.ToString();
            });
        }

        private void TriggerPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            switch(args.Edge)
            {
                case GpioPinEdge.FallingEdge://low value
                    SideTotalCount--;
                    SideDownCountInt++;
                    break;
                case GpioPinEdge.RisingEdge://high value
                    SideTotalCount++;
                    SideUpCountInt++;
                    break;
            }
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                SideEdge.Text = args.Edge.ToString();
                SideDownCount.Text = "Down: " + SideDownCountInt.ToString();
                SideUpCout.Text = "Up: " + SideUpCountInt.ToString();
                SideTotalCout.Text = "Total: " + SideTotalCount.ToString();
            });
        }
    }
}
