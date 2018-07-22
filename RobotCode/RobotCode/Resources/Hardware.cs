using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Devices.Enumeration;
using Windows.Devices.Pwm;
using Windows.Devices.I2c;
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
        #region GPIO
        public static GpioController GpioController = null;
        public static GpioPin[] Pins = new GpioPin[5];
        public const int CODE_RUNNING_PIN = 17;//index 0
        public const int DASHBOARD_CONNECTED_PIN = 27;//index 1
        public const int SIGNAL_BATTERY_STATUS_PIN = 23;//index 2
        public const int COLLECTION_RELAY = 22;//index 3
        public const int POWER_BATTERY_STATUS_PIN = 24;//index 4
        public static int Collection_1_output = (int)GpioPinValue.High;
        public static int Collection_2_output = (int)GpioPinValue.High;
        #endregion

        #region SPI/ADC
        /// <summary>
        /// The MCP3008 ADC device, SPI interface
        /// </summary>
        public static SpiDevice ADC = null;
        /// <summary>
        /// The SPI Controller on the Pi
        /// </summary>
        public static SpiController ADC_Control = null;
        /// <summary>
        /// The SPI connection settings for the MCP3008
        /// </summary>
        public static SpiConnectionSettings ADCSettings = null;
        /// <summary>
        /// The clock frequency to use on the MCP3008
        /// </summary>
        public const int SPI_CLOCK_FREQUENCY = 1000000;
        /// <summary>
        /// Constant to use with SPI reading to make channel selections easier (see ReadVoltage method)
        /// </summary>
        public const byte FORCE_ADC_CHANNEL_SINGLE = 0x80;
        /// <summary>
        /// The digital resultion of the MCP3008
        /// </summary>
        public const float MVOLTS_PER_STEP = 5000.0F / 1024.0F;//5k mv range, 1024 digital steps
        /// <summary>
        /// The analog channel of the signal voltage sensor
        /// </summary>
        public const byte SIGNAL_VOLTAGE_MONITOR_CHANNEL = 0x00;
        /// <summary>
        /// The analog channel of the signal current sensor
        /// </summary>
        public const byte SIGNAL_CURRENT_MONITOR_CHANEL = 0x10;
        /// <summary>
        /// The analog channel fo the power voltage sensor
        /// </summary>
        public const byte POWER_VOLTAGE_MONITOR_CHANNEL = 0x20;
        /// <summary>
        /// The analog channel of the power current sensor
        /// </summary>
        public const byte POWER_CURRENT_MONITOR_CHANNEL = 0x30;
        /// <summary>
        /// The analog channel of the tempature sensor
        /// </summary>
        public const byte TEMPATURE_CHANNEL = 0x40;
        /// <summary>
        /// The analog chanel of the water level sensor
        /// </summary>
        public const byte WATER_LEVEL_CHANNEL = 0x50;
        public const float POWER_VOLTAGE_MULTIPLIER = 4.75F;
        public const float SIGNAL_VOLTAGE_BASE_SUBRTACT = 2.5F;
        public const float SIGNAL_VOLTAGE_MULTIPLIER = 11.20F;
        public const float CURRENT_BASE_SUBTRACT = 2.5F;
        public const float POWER_CURRENT_MULTIPLIER = 12F;
        public const float SIGNAL_CURRENT_MULTIPLIER = 2F;
        /// <summary>
        /// The voltage of the signal battery
        /// </summary>
        public static float SignalVoltage { get; private set; }
        /// <summary>
        /// The raw analog voltage of the signal battery level (volts)
        /// </summary>
        public static float SignalVoltageRaw { get; private set; }
        /// <summary>
        /// The volatge of the power battery
        /// </summary>
        public static float PowerVoltage { get; private set; }
        /// <summary>
        /// The raw analog voltage of the power battery elvel (volts)
        /// </summary>
        public static float PowerVoltageRaw { get; private set; }
        /// <summary>
        /// The current flowing from the signal battery (amps)
        /// </summary>
        public static float SignalCurrent { get; private set; }
        /// <summary>
        /// The raw analog voltage of the current flowing from the signal battery
        /// </summary>
        public static float SignalCurrentRaw { get; private set; }
        /// <summary>
        /// The current frowing from the power battery (amps)
        /// </summary>
        public static float PowerCurrent { get; private set; }
        /// <summary>
        /// The raw analog voltage of the current flowing from the poawer battery
        /// </summary>
        public static float PowerCurrentRaw { get; private set; }
        /// <summary>
        /// The tempature of around the robot (celcius)
        /// </summary>
        public static float Tempature { get; private set; }
        /// <summary>
        /// The raw analog voltage of the tempature around the robot
        /// </summary>
        public static float TempatureRaw { get; private set; }
        /// <summary>
        /// The level detection of water in the collection
        /// </summary>
        public static float WaterLevel { get; private set; }
        #endregion

        #region PWM
        public static SMPWM LeftDrive;//channel 0
        public static SMPWM RightDrive;//channel 1
        public static PwmController driveControl;
        #endregion

        #region I2C
        public static I2cController I2C_Controller = null;
        public static I2cDevice I2C_Device = null;
        public static I2cConnectionSettings I2C_Connection_settings = null;
        private const byte ADDRESS = 0x68;
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
        public static float AccelerationX { get; private set; }
        public static float AccelerationY { get; private set; }
        public static float AccelerationZ { get; private set; }
        public static float GyroX { get; private set; }
        public static float GyroY { get; private set; }
        public static float GyroZ { get; private set; }
        #endregion

        #region Encoders
        public static RotaryEncoder LeftEncoder;
        public static RotaryEncoder RightEncoder;
        public const int ROTARY_LEFT_CLK = 0;
        public const int ROTARY_LEFT_DT = 0;
        public const int ROTARY_RIGHT_CLK = 0;
        public const int ROTARY_RIGHT_DT = 0;
        public static readonly byte[] ccw1 = new byte[] { 0, 0, 0, 1 };
        public static readonly byte[] ccw2 = new byte[] { 0, 1, 1, 1 };
        public static readonly byte[] ccw4 = new byte[] { 1, 1, 1, 0 };
        public static readonly byte[] ccw3 = new byte[] { 1, 0, 0, 0 };
        public static readonly byte[] cw1 = new byte[] { 0, 0, 1, 0 };
        public static readonly byte[] cw3 = new byte[] { 1, 0, 1, 1 };
        public static readonly byte[] cw4 = new byte[] { 1, 1, 0, 1 };
        public static readonly byte[] cw2 = new byte[] { 0, 1, 0, 0 };
        #endregion

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
            GpioController = GpioController.GetDefault();
            
            if (GpioController == null)
                return false;
            Pins[0] = GpioController.OpenPin(CODE_RUNNING_PIN);
            Pins[1] = GpioController.OpenPin(DASHBOARD_CONNECTED_PIN);
            Pins[2] = GpioController.OpenPin(SIGNAL_BATTERY_STATUS_PIN);
            Pins[3] = GpioController.OpenPin(COLLECTION_RELAY);
            Pins[4] = GpioController.OpenPin(POWER_BATTERY_STATUS_PIN);
            //loop for all the pins
            for (int i = 0; i < Pins.Count(); i++)
            {
                if (i != 0 && i != 3)//use this to set high for values that need it
                    Pins[i].Write(GpioPinValue.Low);
                else
                    Pins[i].Write(GpioPinValue.High);
                Pins[i].SetDriveMode(GpioPinDriveMode.Output);
            }
            return true;
        }
        /// <summary>
        /// Initializes the SPI contorller and set default sensor values attached to the ADC
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
            WaterLevel = 0;
            Tempature = 0;
            SignalCurrent = 0;
            SignalVoltage = 0;
            PowerCurrent = 0;
            PowerVoltage = 0;
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
            LeftDrive = new SMPWM();
            RightDrive = new SMPWM();
            LeftDrive.Init(5, 12, GpioController, driveControl);//PWM grey wire, m2
            RightDrive.Init(6, 13, GpioController, driveControl);//PWM purple wire, m1
            LeftDrive.Start();
            RightDrive.Start();
            return true;
        }
        /// <summary>
        /// Initializes the I2C controller and set default values of devices attached to I2C
        /// </summary>
        /// <returns>true if successfull init, false otherwise</returns>
        public static async Task<bool> InitI2C()
        {
            //https://github.com/Microsoft/Windows-universal-samples/blob/master/Samples/IoT-I2C/cs/Scenario1_ReadData.xaml.cs
            I2C_Controller = await I2cController.GetDefaultAsync();
            if (I2C_Controller == null)
                return false;
            //TODO: make constants for device names
            I2C_Connection_settings = new I2cConnectionSettings(ADDRESS)//MPU-6050 address
            {
                BusSpeed = I2cBusSpeed.FastMode,
            };
            I2C_Device = I2C_Controller.GetDevice(I2C_Connection_settings);
            if (I2C_Device == null)
                return false;
            await Task.Delay(3); // wait power up sequence
            try
            {
                I2C_WriteByte(PWR_MGMT_1, 0x80);// reset the device
            }
            catch
            {
                return false;
            }

            await Task.Delay(100);
            I2C_WriteByte(PWR_MGMT_1, 0x2);
            I2C_WriteByte(USER_CTRL, 0x04); //reset fifo

            I2C_WriteByte(PWR_MGMT_1, 1); // clock source = gyro x
            I2C_WriteByte(GYRO_CONFIG, 0); // +/- 250 degrees sec
            I2C_WriteByte(ACCEL_CONFIG, 0); // +/- 2g

            I2C_WriteByte(CONFIG, 1); // 184 Hz, 2ms delay
            I2C_WriteByte(SMPLRT_DIV, 19);  // set rate 50Hz
            I2C_WriteByte(FIFO_EN, 0x78); // enable accel and gyro to read into fifo
            I2C_WriteByte(USER_CTRL, 0x40); // reset and enable fifo
            I2C_WriteByte(INT_ENABLE, 0x1);
            AccelerationX = 0;
            AccelerationY = 0;
            AccelerationZ = 0;
            GyroX = 0;
            GyroY = 0;
            GyroZ = 0;
            return true;
        }
        /// <summary>
        /// Initialize the Encoders and set dafult values
        /// </summary>
        /// <returns></returns>
        public static bool InitEncoders()
        {
            LeftEncoder = new RotaryEncoder();
            RightEncoder = new RotaryEncoder();
            if (!LeftEncoder.InitEncoder(26, 19, GpioController))
                return false;
            if (!RightEncoder.InitEncoder(20, 16, GpioController))
                return false;
            return true;
        }
        #endregion

        #region GPIO methods
        public static void UpdateGPIOValues()
        {
            Collection_1_output = (int)Pins[3].Read();
            Collection_2_output = 1;
        }
        #endregion

        #region SPI/ADC methods
        /// <summary>
        /// Updated all sensors (except battery info) on the ADC
        /// Currently tempature and water level
        /// </summary>
        public static void UpdateSPIData()
        {
            WaterLevel = ReadVoltage(WATER_LEVEL_CHANNEL, true, 2);
            TempatureRaw = ReadVoltage(TEMPATURE_CHANNEL, true, 2);
        }
        /// <summary>
        /// Reads a raw digital voltage from one of the analog channels
        /// </summary>
        /// <param name="hexChannel">The channel, 0-7, in hex to read from (use the consants definded)</param>
        /// <param name="normalizeTo5">true if you want to voltage normalized to 5 volts (99% you do)</param>
        /// <param name="round">The number of places to round to (0 for whole number, -1 to disable rounding)</param>
        /// <returns>A floating point number of the voltage</returns>
        private static float ReadVoltage(byte hexChannel, bool normalizeTo5, int round)
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

        #region I2C methods
        /// <summary>
        /// Updates Acceleration and Gyro values
        /// </summary>
        /// <param name="round">The number of places to round to (0 for whole number, -1 to disable rounding)</param>
        public static void UpdateI2CData(int round)
        {
            int interruptStatus = I2C_ReadByte(INT_STATUS);
            if ((interruptStatus & 0x10) != 0)
            {
                I2C_WriteByte(USER_CTRL, 0x44); // reset and enable fifo
            }
            if ((interruptStatus & 0x1) == 0) return;
            int count = I2C_ReadWord(FIFO_COUNT);
            while (count >= SensorBytes)
            {
                var data = I2C_ReadBytes(FIFO_R_W, SensorBytes);
                count -= SensorBytes;

                var xa = (short)(data[0] << 8 | data[1]);
                var ya = (short)(data[2] << 8 | data[3]);
                var za = (short)(data[4] << 8 | data[5]);

                var xg = (short)(data[6] << 8 | data[7]);
                var yg = (short)(data[8] << 8 | data[9]);
                var zg = (short)(data[10] << 8 | data[11]);

                AccelerationX = xa / (float)16384;
                AccelerationY = ya / (float)16384;
                AccelerationZ = za / (float)16384;
                GyroX = xg / (float)131;
                GyroY = yg / (float)131;
                GyroZ = zg / (float)131;

                if (round >= 0)
                {
                    AccelerationX = MathF.Round(AccelerationX, round);
                    AccelerationY = MathF.Round(AccelerationY, round);
                    AccelerationZ = MathF.Round(AccelerationZ, round);
                    GyroX = MathF.Round(GyroX, round);
                    GyroY = MathF.Round(GyroY, round);
                    GyroZ = MathF.Round(GyroZ, round);
                }
            }
        }
        private static void I2C_WriteByte(byte regAddr, byte data)
        {
            byte[] buffer = new byte[2];
            buffer[0] = regAddr;
            buffer[1] = data;
            I2C_Device.Write(buffer);
        }

        private static byte I2C_ReadByte(byte regAddr)
        {
            byte[] buffer = new byte[1];
            buffer[0] = regAddr;
            byte[] value = new byte[1];
            I2C_Device.WriteRead(buffer, value);
            return value[0];
        }

        private static byte[] I2C_ReadBytes(byte regAddr, int length)
        {
            byte[] values = new byte[length];
            byte[] buffer = new byte[1];
            buffer[0] = regAddr;
            I2C_Device.WriteRead(buffer, values);
            return values;
        }

        private static ushort I2C_ReadWord(byte address)
        {
            byte[] buffer = I2C_ReadBytes(FIFO_COUNT, 2);
            return (ushort)(((int)buffer[0] << 8) | (int)buffer[1]);
        }
        #endregion

        #region Battery Methods
        /// <summary>
        /// Updates the Voltage and Current values of the signal battery
        /// </summary>
        public static void UpdateSignalBattery()
        {
            SignalVoltageRaw = ReadVoltage(SIGNAL_VOLTAGE_MONITOR_CHANNEL, true, 2);
            SignalCurrentRaw = ReadVoltage(SIGNAL_CURRENT_MONITOR_CHANEL, true, 2);
            /*
             * Notes on signal voltage monitor:
             * - Middle value is 2.5 volts
             * - ONLY reads from 0.5 to 4.5, NOT 0-5!
             * - Range is 2.5V +/- 2 volts
             * - Device scales form -30 to 30V
             * - +/-4v signal = +/-30v actual
            */
            //idea to avoid negatives:
            //subtract 0.5 for the part that isn't used
            float voltage_part_1 = SignalVoltageRaw-0.5F;
            //make signal 0 - 4 = correspond to 0 - 60 actual
            float voltage_part_2 = voltage_part_1 * 15F;
            //subtract 30 to normalize back
            SignalVoltage = MathF.Round(voltage_part_2 - 33.8F, 2);
            SignalCurrent = MathF.Round(MathF.Abs(SignalCurrentRaw - CURRENT_BASE_SUBTRACT) * SIGNAL_CURRENT_MULTIPLIER, 2);
        }
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
            if (SignalVoltage > 9.99F)
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
        /// Updates the current and voltrage of the power battery
        /// </summary>
        public static void UpdatePowerBattery()
        {
            PowerVoltageRaw = ReadVoltage(POWER_VOLTAGE_MONITOR_CHANNEL, true, 2);
            PowerCurrentRaw = ReadVoltage(POWER_CURRENT_MONITOR_CHANNEL, true, 2);
            PowerVoltage = MathF.Round(PowerVoltageRaw * POWER_VOLTAGE_MULTIPLIER, 2);
            PowerCurrent = MathF.Round(MathF.Abs(PowerCurrentRaw - CURRENT_BASE_SUBTRACT) * POWER_CURRENT_MULTIPLIER, 2);
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
