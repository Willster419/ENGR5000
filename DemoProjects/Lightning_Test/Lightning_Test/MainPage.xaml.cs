using Microsoft.IoT.Lightning.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Lightning_Test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private PwmPin leftDrive;
        private PwmPin rightDrive;
        private PwmController pwmController = null;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += OnPageLoad;
            this.Unloaded += OnPageUnload;
        }

        private void OnPageUnload(object sender, RoutedEventArgs e)
        {
            if (leftDrive != null)
                leftDrive.Stop();
            if (rightDrive != null)
                rightDrive.Stop();
        }

        private async void OnPageLoad(object sender, RoutedEventArgs e)
        {
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            }

            var gpioController = await GpioController.GetDefaultAsync();
            if (gpioController == null)
            {
                return;
            }

            var pwmControllers = await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider());

            
            pwmController = pwmControllers[1];//hard code from examples to use index 1
            pwmController.SetDesiredFrequency(1000);//do *not* debug over this line, it will crash
            rightDrive = pwmController.OpenPin(13);
            rightDrive.SetActiveDutyCyclePercentage(0.5);
            rightDrive.Start();
            leftDrive = pwmController.OpenPin(12);
            leftDrive.SetActiveDutyCyclePercentage(0.5);
            leftDrive.Start();
        }
    }
}
