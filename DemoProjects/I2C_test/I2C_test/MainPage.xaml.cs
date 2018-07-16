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
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace I2C_test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Constants
        public const byte ADDRESS = 0x68;
        private const byte PWR_MGMT_1 = 0x6B;
        private const byte SMPLRT_DIV = 0x19;
        private const byte CONFIG = 0x1A;
        private const byte GYRO_CONFIG = 0x1B;
        private const byte ACCEL_CONFIG = 0x1C;
        private const byte FIFO_EN = 0x23;
        private const byte INT_ENABLE = 0x38;
        private const byte INT_STATUS = 0x3A;
        private const byte USER_CTRL = 0x6A;
        private const byte FIFO_COUNT = 0x72;
        private const byte FIFO_R_W = 0x74;

        private const int SensorBytes = 12;
        #endregion
        private const Int32 INTERRUPT_PIN = 21;
        private GpioController _gpioController;
        private GpioPin _intPin;
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
            //https://github.com/Microsoft/Windows-universal-samples/blob/master/Samples/IoT-I2C/cs/Scenario1_ReadData.xaml.cs
            _gpioController = GpioController.GetDefault();
            _intPin = _gpioController.OpenPin(INTERRUPT_PIN);
            _intPin.Write(GpioPinValue.Low);
            _intPin.SetDriveMode(GpioPinDriveMode.Input);
            _intPin.ValueChanged += _dispatcherTimer_Tick;
            _i2CController = await I2cController.GetDefaultAsync();
            if (_i2CController == null)
                return;
            //TODO: make constants for device names
            _i2CConnectionSettings = new I2cConnectionSettings(0x68)//MPU-6050 address
            {
                BusSpeed = I2cBusSpeed.FastMode,
            };
            _i2CDevice = _i2CController.GetDevice(_i2CConnectionSettings);
            if (_i2CDevice == null)
                return;
            await Task.Delay(3); // wait power up sequence

            WriteByte(PWR_MGMT_1, 0x80);// reset the device
            await Task.Delay(100);
            WriteByte(PWR_MGMT_1, 0x2);
            WriteByte(USER_CTRL, 0x04); //reset fifo

            WriteByte(PWR_MGMT_1, 1); // clock source = gyro x
            WriteByte(GYRO_CONFIG, 0); // +/- 250 degrees sec
            WriteByte(ACCEL_CONFIG, 0); // +/- 2g

            WriteByte(CONFIG, 1); // 184 Hz, 2ms delay
            WriteByte(SMPLRT_DIV, 19);  // set rate 50Hz
            WriteByte(FIFO_EN, 0x78); // enable accel and gyro to read into fifo
            WriteByte(USER_CTRL, 0x40); // reset and enable fifo
            WriteByte(INT_ENABLE, 0x1);
        }

        private void _dispatcherTimer_Tick(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            int interruptStatus = ReadByte(INT_STATUS);
            if ((interruptStatus & 0x10) != 0)
            {
                WriteByte(USER_CTRL, 0x44); // reset and enable fifo
            }
            if ((interruptStatus & 0x1) == 0) return;
            int count = ReadWord(FIFO_COUNT);
            while (count >= SensorBytes)
            {
                var data = ReadBytes(FIFO_R_W, SensorBytes);
                count -= SensorBytes;

                var xa = (short)(data[0] << 8 | data[1]);
                var ya = (short)(data[2] << 8 | data[3]);
                var za = (short)(data[4] << 8 | data[5]);

                var xg = (short)(data[6] << 8 | data[7]);
                var yg = (short)(data[8] << 8 | data[9]);
                var zg = (short)(data[10] << 8 | data[11]);


                float AccelerationX = xa / (float)16384;
                float AccelerationY = ya / (float)16384;
                float AccelerationZ = za / (float)16384;
                float GyroX = xg / (float)131;
                float GyroY = yg / (float)131;
                float GyroZ = zg / (float)131;


                AccelDataX.Text = string.Format("AccelX = {0}", AccelerationX);
                AccelDataY.Text = string.Format("AccelY = {0}", AccelerationY);
                AccelDataZ.Text = string.Format("AccelZ = {0}", AccelerationZ);
                GyroDataX.Text = string.Format("GyroX = {0}", GyroX);
                GyroDataY.Text = string.Format("GyroY = {0}", GyroY);
                GyroDataZ.Text = string.Format("GryoZ = {0}", GyroZ);
            }
        }
        //https://www.hackster.io/graham_chow/mpu-6050-for-windows-iot-d67793
        void WriteByte(byte regAddr, byte data)
        {
            byte[] buffer = new byte[2];
            buffer[0] = regAddr;
            buffer[1] = data;
            _i2CDevice.Write(buffer);
        }

        private byte ReadByte(byte regAddr)
        {
            byte[] buffer = new byte[1];
            buffer[0] = regAddr;
            byte[] value = new byte[1];
            _i2CDevice.WriteRead(buffer, value);
            return value[0];
        }

        private byte[] ReadBytes(byte regAddr, int length)
        {
            byte[] values = new byte[length];
            byte[] buffer = new byte[1];
            buffer[0] = regAddr;
            _i2CDevice.WriteRead(buffer, values);
            return values;
        }

        public ushort ReadWord(byte address)
        {
            byte[] buffer = ReadBytes(FIFO_COUNT, 2);
            return (ushort)(((int)buffer[0] << 8) | (int)buffer[1]);
        }
    }
}
