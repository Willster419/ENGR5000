using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Devices.Enumeration;
using Windows.Devices.Pwm;

namespace RobotCode
{

    public enum RobotStatus
    {
        Idle = 0,
        UnknownError = 5
    };
    public static class GPIO
    {
        public static SpiDevice ADC = null;
        public static SpiConnectionSettings ADCSettings = null;
        public const int SPI_CLOCK_FREQUENCY = 3600000;
        public static volatile bool SPI_Initialized = false;
        public static bool SPI_works = false;
        private static GpioController Controller = null;
        public static GpioPin[] Pins = new GpioPin[5];
        public const int CODE_RUNNING_PIN = 17;
        public const int DASHBOARD_CONNECTED_PIN = 27;

        public static bool InitGPIO()
        {
            Controller = GpioController.GetDefault();
            if (Controller == null)
                return false;
            Pins[0] = Controller.OpenPin(CODE_RUNNING_PIN);
            Pins[0].Write(GpioPinValue.High);
            Pins[0].SetDriveMode(GpioPinDriveMode.Output);
            return true;
        }

        public static void ToggleNetworkStatus(bool init)
        {
            if(init)
            {
                Pins[1] = Controller.OpenPin(DASHBOARD_CONNECTED_PIN);
            }
            Pins[1].Write(NetworkUtils.DashboardConnected? GpioPinValue.High: GpioPinValue.Low);
            if(init)
            {
                //SetDriveMode actually writes the value during init
                Pins[1].SetDriveMode(GpioPinDriveMode.Output);
            }
        }

        public static async void InitSPI()
        {
            string SPIDevice = SpiDevice.GetDeviceSelector();
            IReadOnlyList<DeviceInformation> devices = await DeviceInformation.FindAllAsync(SPIDevice);
            //0 is chip select line 0
            ADCSettings = new SpiConnectionSettings(0)
            {
                ClockFrequency = SPI_CLOCK_FREQUENCY,
                Mode = SpiMode.Mode0
            };
            //ADCSettings.ChipSelectLine = 0;
            ADC = await SpiDevice.FromIdAsync(devices[0].Id, ADCSettings);
            if(ADC == null)
            {
                SPI_works = false;
            }
            SPI_Initialized = true;
            SPI_works = true;
        }

        public static float ReadVoltage(UInt16 channel)
        {
            if(ADC == null)
            {
                return 0.0F;
            }
            var transmitBuffer = new byte[3] { 1, 0x80, 0x00 };
            var receiveBuffer = new byte[3];

            ADC.TransferFullDuplex(transmitBuffer, receiveBuffer);

            var result = ((receiveBuffer[1] & 3) << 8) + receiveBuffer[2];
            return result;
        }
    }
}
