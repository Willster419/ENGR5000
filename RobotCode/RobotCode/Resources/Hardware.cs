using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Devices.Enumeration;
using Windows.Devices.Pwm;
using Microsoft.IoT.Lightning;
using Microsoft.IoT.Lightning.Providers;
using Windows.UI.Xaml;
using Windows.ApplicationModel.Background;
using System.ComponentModel;
using Windows.Foundation;
using Windows.Devices;
using RobotCode.Resources;

namespace RobotCode
{
    /// <summary>
    /// Contains all Sensors and Actuators on the robot, as well as all pin mappings.
    /// </summary>
    public static class Hardware
    {
        //GPIO
        private static GpioController Controller = null;

        //SPI
        public static SpiDevice ADC = null;
        public static SpiController ADC_Control = null;
        public static SpiConnectionSettings ADCSettings = null;
        public const int SPI_CLOCK_FREQUENCY = 1000000;

        //PWM pins
        public static SMPWM leftDrive;//channel 0
        public static SMPWM rightDrive;//channel 1
        public static PwmController driveControl;

        //I2C (TODO)

        //Encoders (TODO)

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
        public const float SIGNAL_VOLTAGE_MULTIPLIER = 4.91F;
        public const float POWER_VOLTAGE_BASE_SUBRTACT = 2.5F;
        public const float POWER_VOLTAGE_MULTIPLIER = 11F;
        public static float SignalVoltage = 0.0F;
        public static float PowerVoltage = 0.0F;
        public const float CURRENT_BASE_SUBTRACT = 2.5F;
        public const float POWER_CURRENT_MULTIPLIER = 12F;
        public const float SIGNAL_CURRENT_MULTIPLIER = 2F;
        public static float SignalCurrent = 0.0F;
        public static float PowerCurrent = 0.0F;
        #region Init methods
        /// <summary>
        /// Initializes the GPIO controller
        /// </summary>
        /// <returns>True if succussfull init, false otherwise</returns>
        public static bool InitGPIO()
        {
            //init the lightning provivders
            if(LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            }
            else
            {
                return false;
            }
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
        /// <summary>
        /// Initializes the SPI contorller
        /// </summary>
        /// <returns>true if the contorl init success, false otherwise</returns>
        public static async Task<bool> InitSPI()
        {
            ADC_Control = await SpiController.GetDefaultAsync();
            ADCSettings = new SpiConnectionSettings(0)
            {
                ClockFrequency = SPI_CLOCK_FREQUENCY,
                Mode = SpiMode.Mode0
            };
            ADC = ADC_Control.GetDevice(ADCSettings);
            if (ADC == null)
                return false;
            return true;
        }
        /// <summary>
        /// Initializes the PWM controller
        /// </summary>
        /// <returns>true if successfull init, false otherwise</returns>
        public static async Task<bool> InitPWM()
        {
            // PWM Pins http://raspberrypi.stackexchange.com/questions/40812/raspberry-pi-2-b-gpio-pwm-and-interrupt-pins
            var controllers = await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider());
            if (controllers.Count <= 1)
                return false;
            driveControl = controllers[1];
            try
            {
                driveControl.SetDesiredFrequency(1000);
            }
            catch
            {
                return false;
            }
            leftDrive = new SMPWM();
            rightDrive = new SMPWM();
            leftDrive.Init(5, 12, Controller, driveControl);//PWM grey wire, m2
            rightDrive.Init(6, 13, Controller, driveControl);//PWM purple wire, m1
            leftDrive.Start();
            rightDrive.Start();
            return true;
        }
        /// <summary>
        /// Initializes the I2C controller
        /// </summary>
        /// <returns>true if successfull init, false otherwise</returns>
        public static async Task<bool> InitI2C()
        {
            return true;
        }
        #endregion
        #region SPI methods
        /// <summary>
        /// Reads a raw digital voltage from one of the analog channels
        /// </summary>
        /// <param name="hexChannel">The channel, 0-7, in hex to read from (use the consants definded)</param>
        /// <param name="normalizeTo5">true if you want to voltage normalized to 5 volts (99% you do)</param>
        /// <param name="round">The number of places to round to (0 for whole number, -1 to disable rounding)</param>
        /// <returns>A floating point number of the voltage</returns>
        public static float ReadVoltage(byte hexChannel, bool normalizeTo5, int round)
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
            float resultFloat = result * MVOLTS_PER_STEP;
            if (normalizeTo5)
                resultFloat = resultFloat / 1000F;
            if (round >= 0)
                resultFloat = MathF.Round(resultFloat, round);
            return resultFloat;
        }
        #endregion
        #region Battery Methods
        /// <summary>
        /// Reads the voltage of the battery for the signal system
        /// </summary>
        /// <returns>The BatteryStatus Enumeration that corresponds to the value read</returns>
        public static BatteryStatus UpdateSignalBatteryStatus()
        {
            if (ADC == null)
                return BatteryStatus.Unknown;
            if(!RobotController.SystemOnline)
                return BatteryStatus.Unknown;
            if(!NetworkUtils.ConnectionLive)
            {
                //update the voltage value since the network thread is not
                SignalVoltage = ReadVoltage(SIGNAL_VOLTAGE_MONITOR_CHANNEL, true, 2) * SIGNAL_VOLTAGE_MULTIPLIER;
            }
            if(SignalVoltage > 9.99F)
            {
                return BatteryStatus.Above75;
            }
            else if (SignalVoltage > 9.59F)
            {
                return BatteryStatus.Between50And75;
            }
            else if (SignalVoltage > 9.19F)
            {
                return BatteryStatus.Between25And50;
            }
            else if (SignalVoltage > 8.59F)
            {
                return BatteryStatus.Below15Warning;
            }
            else
            {
                return BatteryStatus.Below5Shutdown;
            }
        }
        /// <summary>
        /// Reads the voltage of the battery for the power system
        /// </summary>
        /// <returns>The BatteryStatus Enumeration that corresponds to the value read</returns>
        public static BatteryStatus UpdatePowerBatteryStatus()
        {
            if (ADC == null)
                return BatteryStatus.Unknown;
            if (!RobotController.SystemOnline)
                return BatteryStatus.Unknown;
            if (!NetworkUtils.ConnectionLive)
            {
                //update the voltage value since the network thread is not
                PowerVoltage = ReadVoltage(POWER_VOLTAGE_MONITOR_CHANNEL, true, 2) * POWER_VOLTAGE_MULTIPLIER;
            }
            if (PowerVoltage > 8.99F)
            {
                return BatteryStatus.Above75;
            }
            else if (PowerVoltage > 7.99F)
            {
                return BatteryStatus.Between50And75;
            }
            else if (PowerVoltage > 7.19F)
            {
                return BatteryStatus.Between25And50;
            }
            else if (PowerVoltage > 6.49F)
            {
                return BatteryStatus.Below15Warning;
            }
            else
            {
                return BatteryStatus.Below5Shutdown;
            }
        }
        #endregion
    }
}
