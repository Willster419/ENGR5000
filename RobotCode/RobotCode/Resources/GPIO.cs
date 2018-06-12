using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Devices.Enumeration;
using Windows.Devices.Pwm;
using Windows.UI.Xaml;
using Windows.ApplicationModel.Background;
using System.ComponentModel;
using Windows.Foundation;

namespace RobotCode
{
    public static class GPIO
    {
        public static SpiDevice ADC = null;
        public static SpiConnectionSettings ADCSettings = null;
        public const int SPI_CLOCK_FREQUENCY = 1000000;
        private static GpioController Controller = null;
        /*
         * Current pins setup:
         * 0 is robot status
         * 1 is networking status
         * 2 is battery status
         * 3 is relay output
         * 4 is power battery status
         */
        public static GpioPin[] Pins = new GpioPin[5];
        public const int CODE_RUNNING_PIN = 17;//index 0
        public const int DASHBOARD_CONNECTED_PIN = 27;//index 1
        public const int SIGNAL_BATTERY_STATUS_PIN = 23;//index 2
        public const int POWER_BATTERY_STATUS_PIN = 24;//index 4
        public const byte FORCE_ADC_CHANNEL_SINGLE = 0x80;
        public const float MVOLTS_PER_STEP = 5000.0F / 1024.0F;//5k mv range, 1024 digital steps
        public const byte SIGNAL_VOLTAGE_MONITOR_CHANNEL = 0x00;
        public const byte SIGNAL_CURRENT_MONITOR_CHANEL = 0x10;
        public const byte POWER_VOLTAGE_MONITOR_CHANNEL = 0x20;
        public const byte POWER_CURRENT_MONITOR_CHANNEL = 0x30;
        public const byte TEMPATURE_CHANNEL = 0x40;
        public const byte WATER_LEVEL_CHANNEL = 0x50;
        public const byte ACCEL_CHANNEL = 0x60;
        public const byte GYRO_CHANNEL = 0x70;
        public const int COLLECTION_RELAY = 22;//index 3
        /*
        * voltage notes:
        *  signal: 9.8  = 1.882
        *  power:  9.05 = 3.369
        */
        public static float SignalBatteryVoltage = 0.0F;
        public static float SignalPowerVoltage = 0.0F;

        public static bool InitGPIO()
        {
            //init the GPIO controller
            Controller = GpioController.GetDefault();
            if (Controller == null)
                return false;
            Pins[0] = Controller.OpenPin(CODE_RUNNING_PIN);
            Pins[1] = Controller.OpenPin(DASHBOARD_CONNECTED_PIN);
            Pins[2] = Controller.OpenPin(SIGNAL_BATTERY_STATUS_PIN);
            Pins[3] = Controller.OpenPin(COLLECTION_RELAY);
            Pins[4] = Controller.OpenPin(POWER_BATTERY_STATUS_PIN);
            for (int i = 0; i < Pins.Count(); i++)
            {
                Pins[i].Write(GpioPinValue.Low);
                Pins[i].SetDriveMode(GpioPinDriveMode.Output);
            }
            Pins[0].Write(GpioPinValue.High);
            return true;
        }

        public static bool InitSPI()
        {
            string SPIDevice = SpiDevice.GetDeviceSelector("SPI0");//apparently SPI0 is a rogue device
            IAsyncOperation<DeviceInformationCollection> t = DeviceInformation.FindAllAsync(SPIDevice);
            while (!(t.Status == AsyncStatus.Completed))
            {
                System.Threading.Thread.Sleep(10);
            }
            IReadOnlyList<DeviceInformation> devices = t.GetResults();
            if(devices == null)
                return false;
            if(devices.Count == 0 )
                return false;
            ADCSettings = new SpiConnectionSettings(0)
            {
                ClockFrequency = SPI_CLOCK_FREQUENCY,
                Mode = SpiMode.Mode0//,
                //ChipSelectLine = 0
            };
            IAsyncOperation<SpiDevice> spiDevice = SpiDevice.FromIdAsync(devices[0].Id, ADCSettings);
            while (!(spiDevice.Status == AsyncStatus.Completed))
            {
                System.Threading.Thread.Sleep(10);
            }
            ADC = spiDevice.GetResults();
            if (ADC == null)
                return false;
            return true;
        }

        public static float ReadVoltage(byte hexChannel)
        {
            //unsigned char in C++ is byte in C#
            if(ADC == null)
            {
                return 0.0F;
            }
            /*structure like this:
             *  channel_config channel_bit_1 channel_bit_2 channel_bit_3 -> use channel config 1 for single end voltage (compares voltage @ channel to Vref)
             *  |      1      |      0      |      0      |      0      | -> single voltrage mode get value from channel 0
             *  use a bitwise or to verify that the channel is set for intented use
             *  NOTE that the above is the FIRST of TWO hex bits
             *  THEREFORE channel 0 is 0x80, 1 is 0x90
             */
            hexChannel = (byte)(hexChannel|FORCE_ADC_CHANNEL_SINGLE);
            byte[] transmitBuffer = new byte[3] { 1, hexChannel, 0x00 };
            byte[] receiveBuffer = new byte[3];
            ADC.TransferFullDuplex(transmitBuffer, receiveBuffer);
            int result = ((receiveBuffer[1] & 3) << 8) + receiveBuffer[2];
            return result * MVOLTS_PER_STEP;
        }

        public static BatteryStatus GetSignalBatteryStatus()
        {
            if (ADC == null)
                return BatteryStatus.Unknown;
            return BatteryStatus.Between50And75;
        }

        public static BatteryStatus GetPowerBatteryStatus()
        {
            if (ADC == null)
                return BatteryStatus.Unknown;
            return BatteryStatus.Between50And75;
        }
    }
}
