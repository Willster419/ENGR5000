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

namespace RobotCode
{

    public enum RobotStatus
    {
        Idle = 1,
        Error = 2,
        Exception = 3,
        UnknownError = 4
    };

    public static class GPIO
    {
        public static SpiDevice ADC = null;
        public static SpiConnectionSettings ADCSettings = null;
        public const int SPI_CLOCK_FREQUENCY = 1000000;
        private static GpioController Controller = null;
        public static GpioPin[] Pins = new GpioPin[5];
        public const int CODE_RUNNING_PIN = 17;
        public const int DASHBOARD_CONNECTED_PIN = 27;
        public const byte FORCE_ADC_CHANNEL_SINGLE = 0x80;
        public const float MVOLTS_PER_STEP = 5000.0F / 1024.0F;//5k mv range, 1024 digital steps
        private readonly static int TOTAL_STATUS_TYPES = Enum.GetNames(typeof(RobotStatus)).Count();
        private static DispatcherTimer StatusTimer = null;
        private static BackgroundWorker SensorThread = null;
        private static int TimeThrough = 0;
        private static int TimeToStop = 0;
        public static RobotStatus @RobotStatus = RobotStatus.Idle;
        public static bool FirstCycle = true;

        public static bool InitGPIO()
        {
            //init the GPIO controller
            Controller = GpioController.GetDefault();
            if (Controller == null)
                return false;
            Pins[0] = Controller.OpenPin(CODE_RUNNING_PIN);
            Pins[0].Write(GpioPinValue.High);
            Pins[0].SetDriveMode(GpioPinDriveMode.Output);
            TimeToStop = (int)RobotStatus * 2;
            //setup the status led
            /*
            SensorThread = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            SensorThread.DoWork += InitTimers;
            SensorThread.RunWorkerAsync();
            */
            StatusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            StatusTimer.Tick += OnStatusLEDTick;
            StatusTimer.Start();
            return true;
        }

        private static void OnStatusLEDTick(object sender, object e)
        {
            //two seconds of nothing
            //get the current status
            //flash appropriatly
            //(repeat the above)
            //also have check if it's the first tiem running this so that it actually counts correctly
            //since it's first run it's high
            if(FirstCycle)
            {
                Pins[0].Write(GpioPinValue.Low);
                TimeToStop = (int)RobotStatus * 2;
                //TimeThrough will already have been set to 0, no need to do it again...
                FirstCycle = false;
            }
            if (TimeThrough == 0)
            {
                TimeToStop = (int)RobotStatus * 2;//times 2 cause one cycle is on and one is off
            }
            if(TimeThrough == TimeToStop-1)
            {
                //turn off the status LED
                Pins[0].Write(GpioPinValue.Low);
                TimeThrough = -5;
            }
            if(TimeThrough >= 0)
            {
                if(Pins[0].Read() == GpioPinValue.High)
                {
                    Pins[0].Write(GpioPinValue.Low);
                }
                else
                {
                    Pins[0].Write(GpioPinValue.High);
                }
            }
            TimeThrough++;
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

        public static async Task<bool> InitSPI()
        {
            string SPIDevice = SpiDevice.GetDeviceSelector("SPI0");//apparently SPI0 is a rogue device
            IReadOnlyList<DeviceInformation> devices = await DeviceInformation.FindAllAsync(SPIDevice);
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
            ADC = await SpiDevice.FromIdAsync(devices[0].Id, ADCSettings);
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
             */
            hexChannel = (byte)(hexChannel|FORCE_ADC_CHANNEL_SINGLE);
            byte[] transmitBuffer = new byte[3] { 1, hexChannel, 0x00 };
            byte[] receiveBuffer = new byte[3];
            ADC.TransferFullDuplex(transmitBuffer, receiveBuffer);
            int result = ((receiveBuffer[1] & 3) << 8) + receiveBuffer[2];
            return result * MVOLTS_PER_STEP;
        }
    }
}
