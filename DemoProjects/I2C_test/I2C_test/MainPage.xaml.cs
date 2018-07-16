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
using Windows.Devices;
using Windows.Devices.Adc;
using Windows.Devices.I2c;
using Windows.Devices.Gpio;
using Microsoft.IoT.Lightning;
using Microsoft.IoT.Lightning.Providers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace I2C_test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private I2cController _i2CController;
        private I2cConnectionSettings _i2CConnectionSettings;
        private I2cDevice _i2CDevice;
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += LoadUserCode;
        }

        private async void LoadUserCode(object sender, RoutedEventArgs e)
        {
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            }
            else
            {
                return;
            }
            _i2CController = await I2cController.GetDefaultAsync();
            //TODO:
        }
    }
}
