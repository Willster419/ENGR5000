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
using Windows.Devices.Pwm.Provider;

namespace RobotCode
{
    /// <summary>
    /// Contains all Sensors and Actuators on the robot, as well as all pin mappings.
    /// </summary>
    public static class Hardware
    {
        #region GPIO
        /// <summary>
        /// The GPIO Controller for all IO pins
        /// </summary>
        public static GpioController GpioController = null;
        /// <summary>
        /// The GPIO pin number of the LED comms pin. Index 1 in pin array
        /// </summary>
        public const int DASHBOARD_CONNECTED_INDICATOR_PIN = 27;
        public static GpioPin Dashboard_connected_pin;
        /// <summary>
        /// THE GPIO pin number of the Augar pin. Index 3 in array
        /// </summary>
        public const int AUGAR_PIN = 22;
        public static GpioPin Auger_pin;
        public const int IMPELLER_PIN = 18;//TODO:check
        public static GpioPin Impeller_pin;
        /// <summary>
        /// The current logical representation of the realy output for the auger. 1 is no output, 0 is output
        /// </summary>
        public static int Augar_Output { get; private set; } = (int)GpioPinValue.High;
        /// <summary>
        /// The current logical representation of the relay output for the impeller. 1 is no output, 0 is output
        /// </summary>
        public static int Impeller_Output { get; private set; } = (int)GpioPinValue.High;
        #endregion

        #region IR
        /// <summary>
        /// The GPIO pin used for the IR sensor on the side of the plow
        /// </summary>
        public const int SIDE_IR_DETECT_PIN = 21;
        /// <summary>
        /// The GPIO pin used of the IR sensor on the top of the plow
        /// </summary>
        public const int FRONT_IR_DETECT_PIN = 4;
        /// <summary>
        /// The Infared reciever on the right side of the plow, detects the side wall (current wall)
        /// </summary>
        public static IRReciever SideReciever;
        /// <summary>
        /// The Infared reciever on the top of the plow, detects the front wall (end of current wall)
        /// </summary>
        public static IRReciever FrontReciever;
        #endregion

        #region StatusIndicators
        /// <summary>
        /// The GPIO pin number of the LED code running pin. Index 0 in pin array
        /// </summary>
        public const int CODE_RUNNING_INDICATOR_PIN = 17;
        public static SmartStatusIndicator Code_running_indicator;
        /// <summary>
        /// The GPIO pin number of the signal battery status pin. White(?) LED, index 2 in pin array
        /// </summary>
        public const int SIGNAL_BATTERY_STATUS_INDICATOR_PIN = 23;
        public static SmartStatusIndicator Signal_battery_status_indicator;
        /// <summary>
        /// The GPIO pin number of the power battery status pin. Green(?) LED, index 4 of pin array
        /// </summary>
        public const int POWER_BATTERY_STATUS_INDICATOR_PIN = 24;
        public static SmartStatusIndicator Power_battery_status_indicator;
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
        public static float SignalVoltage { get; private set; } = 0F;
        /// <summary>
        /// The raw analog voltage of the signal battery level (volts)
        /// </summary>
        public static float SignalVoltageRaw { get; private set; } = 0F;
        /// <summary>
        /// The volatge of the power battery
        /// </summary>
        public static float PowerVoltage { get; private set; } = 0F;
        /// <summary>
        /// The raw analog voltage of the power battery elvel (volts)
        /// </summary>
        public static float PowerVoltageRaw { get; private set; } = 0F;
        /// <summary>
        /// The current flowing from the signal battery (amps)
        /// </summary>
        public static float SignalCurrent { get; private set; } = 0F;
        /// <summary>
        /// The raw analog voltage of the current flowing from the signal battery
        /// </summary>
        public static float SignalCurrentRaw { get; private set; } = 0F;
        /// <summary>
        /// The current frowing from the power battery (amps)
        /// </summary>
        public static float PowerCurrent { get; private set; } = 0F;
        /// <summary>
        /// The raw analog voltage of the current flowing from the poawer battery
        /// </summary>
        public static float PowerCurrentRaw { get; private set; } = 0F;
        /// <summary>
        /// The tempature of around the robot (celcius)
        /// </summary>
        public static float Tempature { get; private set; } = 0F;
        /// <summary>
        /// The raw analog voltage of the tempature around the robot
        /// </summary>
        public static float TempatureRaw { get; private set; } = 0F;
        /// <summary>
        /// The level detection of water in the collection
        /// </summary>
        public static float WaterLevel { get; private set; } = 0F;
        #endregion

        #region PWM
        /// <summary>
        /// The Left drive PWM output. Channel 0 from the Pi
        /// </summary>
        public static SMPWM LeftDrive;//channel 0
        /// <summary>
        /// The Right drive PWM output. Channel 1 from the Pi
        /// </summary>
        public static SMPWM RightDrive;//channel 1
        /// <summary>
        /// The Controller of PWM hardware on the Pi
        /// </summary>
        public static PwmController driveControl;
        #endregion

        #region I2C
        /// <summary>
        /// The hardware I2C controller on the Pi
        /// </summary>
        public static I2cController I2C_Controller = null;
        /// <summary>
        /// The GY-521 module
        /// </summary>
        public static I2cDevice MPU6050 = null;
        /// <summary>
        /// The Connection settings for the GY-521 module
        /// </summary>
        public static I2cConnectionSettings I2C_Connection_settings = null;
        /// <summary>
        /// The init address of the GY-521 module
        /// </summary>
        private const byte ADDRESS = 0x68;
        /// <summary>
        /// Register address that specifies the frequency of new data updates
        /// </summary>
        private const byte SMPLRT_DIV = 0x19;
        /// <summary>
        /// Address of Frame Syncronization (FSYNC) and Digital Low Pass Filter (DLPF)
        /// Look at the register map datasheet for more informatoin
        /// </summary>
        private const byte CONFIG = 0x1A;
        /// <summary>
        /// Gyro Config register that specifies sensitivitry and full scale range form 250deg/sec to 2000deg/s
        /// </summary>
        private const byte GYRO_CONFIG = 0x1B;
        /// <summary>
        /// Accel config register that specifies snesitifit and full scale range from 2g to 16g
        /// </summary>
        private const byte ACCEL_CONFIG = 0x1C;
        /// <summary>
        /// Configuratoin that toggles the fifo enabled
        /// </summary>
        private const byte FIFO_EN = 0x23;
        /// <summary>
        /// Register tha tholds configuration data that specifies which enables to use
        /// </summary>
        private const byte INT_ENABLE = 0x38;
        /// <summary>
        /// Read only register that goes high for different bits when different inturrupts are ready (if INT_ENABLE is set for it)
        /// </summary>
        private const byte INT_STATUS = 0x3A;
        /// <summary>
        /// Acceleration x,y,z data. High is MSB bits 15-8, LSB bits low are 7-0
        /// </summary>
        private const byte ACCEL_XOUT_H = 0x3B;
        /// <summary>
        /// Acceleration x,y,z data. High is MSB bits 15-8, LSB bits low are 7-0
        /// </summary>
        private const byte ACCEL_XOUT_L = 0x3C;
        /// <summary>
        /// Acceleration x,y,z data. High is MSB bits 15-8, LSB bits low are 7-0
        /// </summary>
        private const byte ACCEL_YOUT_H = 0x3D;
        /// <summary>
        /// Acceleration x,y,z data. High is MSB bits 15-8, LSB bits low are 7-0
        /// </summary>
        private const byte ACCEL_YOUT_L = 0x3E;
        /// <summary>
        /// Acceleration x,y,z data. High is MSB bits 15-8, LSB bits low are 7-0
        /// </summary>
        private const byte ACCEL_ZOUT_H = 0x3F;
        /// <summary>
        /// Acceleration x,y,z data. High is MSB bits 15-8, LSB bits low are 7-0
        /// </summary>
        private const byte ACCEL_ZOUT_L = 0x40;
        /// <summary>
        /// Tempature data. High is MSB bits 15-8, LSB bits low are 7-0
        /// </summary>
        private const byte TEMP_OUT_H = 0x41;
        /// <summary>
        /// Tempature data. High is MSB bits 15-8, LSB bits low are 7-0
        /// </summary>
        private const byte TEMP_OUT_L = 0x42;
        /// <summary>
        /// Gyro x,y,z data. High is MSB bits 15-8, LSB bits low are 7-0
        /// </summary>
        private const byte GYRO_XOUT_H = 0x43;
        /// <summary>
        /// Gyro x,y,z data. High is MSB bits 15-8, LSB bits low are 7-0
        /// </summary>
        private const byte GYRO_XOUT_L = 0x44;
        /// <summary>
        /// Gyro x,y,z data. High is MSB bits 15-8, LSB bits low are 7-0
        /// </summary>
        private const byte GYRO_YOUT_H = 0x45;
        /// <summary>
        /// Gyro x,y,z data. High is MSB bits 15-8, LSB bits low are 7-0
        /// </summary>
        private const byte GYRO_YOUT_L = 0x46;
        /// <summary>
        /// Gyro x,y,z data. High is MSB bits 15-8, LSB bits low are 7-0
        /// </summary>
        private const byte GYRO_ZOUT_H = 0x47;
        /// <summary>
        /// Gyro x,y,z data. High is MSB bits 15-8, LSB bits low are 7-0
        /// </summary>
        private const byte GYRO_ZOUT_L = 0x48;
        /// <summary>
        /// Controls resetting of FIFO, FIFO enable, and Signal conditioning reset
        /// </summary>
        private const byte USER_CTRL = 0x6A;
        /// <summary>
        /// Allows confiuration of power mode and clock source and reset and temp disble
        /// </summary>
        private const byte PWR_MGMT_1 = 0x6B;
        /// <summary>
        /// Read only register that shows the address currently used
        /// </summary>
        private const byte WHO_AM_I = 0x75;
        /// <summary>
        /// Acceleration x,y,z data
        /// </summary>
        public static float AccelerationX { get; private set; } = 0F;
        /// <summary>
        /// Acceleration x,y,z data
        /// </summary>
        public static float AccelerationY { get; private set; } = 0F;
        /// <summary>
        /// Acceleration x,y,z data
        /// </summary>
        public static float AccelerationZ { get; private set; } = 0F;
        /// <summary>
        /// Gyro x,y,z data
        /// </summary>
        public static float GyroX { get; private set; } = 0F;
        /// <summary>
        /// Gyro x,y,z data
        /// </summary>
        public static float GyroY { get; private set; } = 0F;
        /// <summary>
        /// Gyro x,y,z data
        /// </summary>
        public static float GyroZ { get; private set; } = 0F;
        /// <summary>
        /// MPU tempature data
        /// </summary>
        public static float Tempature_2 { get; private set; } = 0F;
        /// <summary>
        /// Velocity x,y,z data
        /// </summary>
        public static float VelocityX { get; private set; } = 0F;
        /// <summary>
        /// Velocity x,y,z data
        /// </summary>
        public static float VelocityY { get; private set; } = 0F;
        /// <summary>
        /// Velocity x,y,z data
        /// </summary>
        public static float VelocityZ { get; private set; } = 0F;
        /// <summary>
        /// Rotation x,y,z data
        /// </summary>
        public static float RotationX { get; private set; } = 0F;
        /// <summary>
        /// Rotation x,y,z data
        /// </summary>
        public static float RotationY { get; private set; } = 0F;
        /// <summary>
        /// Rotation x,y,z data
        /// </summary>
        public static float RotationZ { get; private set; } = 0F;
        /// <summary>
        /// Position x,y,z data
        /// </summary>
        public static float PositionX { get; private set; } = 0F;
        /// <summary>
        /// Position x,y,z data
        /// </summary>
        public static float PositionY { get; private set; } = 0F;
        /// <summary>
        /// Position x,y,z data
        /// </summary>
        public static float PositionZ { get; private set; } = 0F;
        /// <summary>
        /// The first values read on init, so that they are subtracted and not counted when getting real values
        /// </summary>
        private static float GyroX_Offset = 0F;
        /// <summary>
        /// The first values read on init, so that they are subtracted and not counted when getting real values
        /// </summary>
        private static float GyroY_Offset = 0F;
        /// <summary>
        /// The first values read on init, so that they are subtracted and not counted when getting real values
        /// </summary>
        private static float GyroZ_Offset = 0F;
        /// <summary>
        /// The first values read on init, so that they are subtracted and not counted when getting real values
        /// </summary>
        private static float AccelerationX_Offset = 0F;
        /// <summary>
        /// The first values read on init, so that they are subtracted and not counted when getting real values
        /// </summary>
        private static float AccelerationY_Offset = 0F;
        /// <summary>
        /// The first values read on init, so that they are subtracted and not counted when getting real values
        /// </summary>
        private static float AccelerationZ_Offset = 0F;
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

            //init generic pins
            Dashboard_connected_pin = GpioController.OpenPin(DASHBOARD_CONNECTED_INDICATOR_PIN);
            Auger_pin = GpioController.OpenPin(AUGAR_PIN);
            Impeller_pin = GpioController.OpenPin(IMPELLER_PIN);
            //dashboard pin to low, relay pins to high
            Dashboard_connected_pin.Write(GpioPinValue.Low);
            Dashboard_connected_pin.SetDriveMode(GpioPinDriveMode.Output);
            Auger_pin.Write(GpioPinValue.High);
            Auger_pin.SetDriveMode(GpioPinDriveMode.Output);
            Impeller_pin.Write(GpioPinValue.High);
            Impeller_pin.SetDriveMode(GpioPinDriveMode.Output);
            
            //init IR recievers
            SideReciever = new IRReciever();
            FrontReciever = new IRReciever();
            if (!SideReciever.InitSensor(SIDE_IR_DETECT_PIN, 2, GpioController))
                return false;
            if (!FrontReciever.InitSensor(FRONT_IR_DETECT_PIN, 2, GpioController))
                return false;

            //init status indicators
            Code_running_indicator = new SmartStatusIndicator();
            Signal_battery_status_indicator = new SmartStatusIndicator();
            Power_battery_status_indicator = new SmartStatusIndicator();
            if (!Code_running_indicator.InitIndicator(GpioController, CODE_RUNNING_INDICATOR_PIN, TimeSpan.FromMilliseconds(250), (int)RobotStatus.Idle))
                return false;
            if (!Signal_battery_status_indicator.InitIndicator(GpioController, SIGNAL_BATTERY_STATUS_INDICATOR_PIN,
                TimeSpan.FromMilliseconds(250), (int)BatteryStatus.Unknown))
                return false;
            if (!Power_battery_status_indicator.InitIndicator(GpioController, POWER_BATTERY_STATUS_INDICATOR_PIN,
                TimeSpan.FromMilliseconds(250), (int)BatteryStatus.Unknown))
                return false;
            //start the code running one
            Code_running_indicator.Start();

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
            if (driveControl == null)
                return false;
            //can't step through the below line because reasons
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
            I2C_Connection_settings = new I2cConnectionSettings(ADDRESS)//MPU-6050 address
            {
                BusSpeed = I2cBusSpeed.FastMode,
            };
            MPU6050 = I2C_Controller.GetDevice(I2C_Connection_settings);
            if (MPU6050 == null)
                return false;
            await Task.Delay(3); // wait power up sequence
            //device powers up in sleep mode, need to take it out of sleep mode
            //0x80 sets the device to reset so we have a working state to use
            try
            {
                I2C_WriteByte(PWR_MGMT_1, 0x80);
                NetworkUtils.LogNetwork("MPU connection sucessfull, verified with reset bit send", NetworkUtils.MessageType.Debug);
            }
            catch
            {
                return false;
            }
            //allow for the reset to occur
            await Task.Delay(100);

            //test to make sure the device is on the correct bus
            byte address_test = I2C_ReadByte(WHO_AM_I);
            NetworkUtils.LogNetwork(string.Format("MPU reports that device is on address {0}...", address_test),NetworkUtils.MessageType.Debug);
            if (!address_test.Equals(ADDRESS))
                return false;

            //"Upon power up, the MPU-60X0 clock source defaults to the internal oscillator. However, it is highly
            //recommended that the device be configured to use one of the gyroscopes(or an external clock
            //source) as the clock reference for improved stability"
            //using x axis gyroscope
            I2C_WriteByte(PWR_MGMT_1, 0x1);
            NetworkUtils.LogNetwork("MPU internal clock set to use X axis gyroscope", NetworkUtils.MessageType.Debug);

            //reset and disable the FIFO (don't need it)
            NetworkUtils.LogNetwork("MPU FIFO reset and disable and signal cond clear reset",NetworkUtils.MessageType.Debug);
            //disable all sensors from writing to the FIFO
            I2C_WriteByte(FIFO_EN, 0x00);
            //reset and complely disable FIFO, and reset signal conditons while clearing signal registers. nice.
            I2C_WriteByte(USER_CTRL, 0x05);

            NetworkUtils.LogNetwork("Config gyro and accel sensitivity",NetworkUtils.MessageType.Debug);
            //config gyro to be +/- 250 degrees/sec
            I2C_WriteByte(GYRO_CONFIG, 0);
            //config accel to be +/- 2g
            I2C_WriteByte(ACCEL_CONFIG, 0);

            NetworkUtils.LogNetwork("Config gyro and accel sample rate and filter rate", NetworkUtils.MessageType.Debug);
            //config DLPF to be 10/10 HZ, 13.8/13.4 ms delay (secondmost maximum filtering)
            //TODO: determine if this is too much filtering?
            //https://www.youtube.com/watch?v=Bv5ajMgdsno
            I2C_WriteByte(CONFIG, 0x06);
            //use a 50Hz sample rate
            I2C_WriteByte(SMPLRT_DIV, 19);

            NetworkUtils.LogNetwork("Disable inturrupts and clear accel and gyro values",NetworkUtils.MessageType.Debug);
            I2C_WriteByte(INT_ENABLE, 0x00);

            //wait for process to take place...
            await Task.Delay(100);
            //...and get normalization values
            NormalizeI2CData();

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
            Augar_Output = (int)Auger_pin.Read();
            Impeller_Output = (int)Impeller_pin.Read();
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
            //Get the values
            short xa = I2C_ReadShort(ACCEL_XOUT_H, ACCEL_XOUT_L);
            short ya = I2C_ReadShort(ACCEL_YOUT_H, ACCEL_YOUT_L);
            short za = I2C_ReadShort(ACCEL_ZOUT_H, ACCEL_ZOUT_L);
            short xg = I2C_ReadShort(GYRO_XOUT_H, GYRO_XOUT_L);
            short yg = I2C_ReadShort(GYRO_YOUT_H, GYRO_YOUT_L);
            short zg = I2C_ReadShort(GYRO_ZOUT_H, GYRO_ZOUT_L);
            short te = I2C_ReadShort(TEMP_OUT_H, TEMP_OUT_L);

            //data conversion
            AccelerationX = xa / (float)16384;
            AccelerationY = ya / (float)16384;
            AccelerationZ = za / (float)16384;
            GyroX = xg / (float)131;
            GyroY = yg / (float)131;
            GyroZ = zg / (float)131;
            Tempature_2 = te / (float)16384;

            //offset normalization
            AccelerationX -= AccelerationX_Offset;
            AccelerationY -= AccelerationY_Offset;
            AccelerationZ -= AccelerationZ_Offset;
            GyroX -= GyroX_Offset;
            GyroY -= GyroY_Offset;
            GyroZ -= GyroZ_Offset;

            //rounding
            if (round >= 0)
            {
                AccelerationX = MathF.Round(AccelerationX, round);
                AccelerationY = MathF.Round(AccelerationY, round);
                AccelerationZ = MathF.Round(AccelerationZ, round);
                GyroX = MathF.Round(GyroX, round);
                GyroY = MathF.Round(GyroY, round);
                GyroZ = MathF.Round(GyroZ, round);
                Tempature_2 = MathF.Round(Tempature_2, round);
            }

            //integration
            VelocityX += AccelerationX;
            VelocityY += AccelerationY;
            VelocityZ += AccelerationZ;
            RotationX += GyroX;
            RotationY += GyroY;
            RotationZ += GyroZ;

            //more integration
            PositionX += VelocityX;
            PositionY += VelocityY;
            PositionZ += VelocityZ;
        }
        /// <summary>
        /// Gets the current values and uses them as normalization values to subtract when getting actual values later. Calibration, if you will
        /// </summary>
        public static void NormalizeI2CData()
        {
            //Get the values
            short xa = I2C_ReadShort(ACCEL_XOUT_H, ACCEL_XOUT_L);
            short ya = I2C_ReadShort(ACCEL_YOUT_H, ACCEL_YOUT_L);
            short za = I2C_ReadShort(ACCEL_ZOUT_H, ACCEL_ZOUT_L);
            short xg = I2C_ReadShort(GYRO_XOUT_H, GYRO_XOUT_L);
            short yg = I2C_ReadShort(GYRO_YOUT_H, GYRO_YOUT_L);
            short zg = I2C_ReadShort(GYRO_ZOUT_H, GYRO_ZOUT_L);

            //data conversion
            AccelerationX_Offset = xa / (float)16384;
            AccelerationY_Offset = ya / (float)16384;
            AccelerationZ_Offset = za / (float)16384;
            GyroX_Offset = xg / (float)131;
            GyroY_Offset = yg / (float)131;
            GyroZ_Offset = zg / (float)131;
        }
        /// <summary>
        /// Writes a byte of data to a specified byte address
        /// </summary>
        /// <param name="regAddr">The hex address of the register to write to</param>
        /// <param name="data">The data to write</param>
        private static void I2C_WriteByte(byte regAddr, byte data)
        {
            byte[] buffer = new byte[2];
            buffer[0] = regAddr;
            buffer[1] = data;
            MPU6050.Write(buffer);
        }
        /// <summary>
        /// Reads a byte of data from a specified byte register address
        /// </summary>
        /// <param name="regAddr">The address of the register to read form</param>
        /// <returns>A byte of data</returns>
        private static byte I2C_ReadByte(byte regAddr)
        {
            byte[] buffer = new byte[1];
            buffer[0] = regAddr;
            byte[] value = new byte[1];
            MPU6050.WriteRead(buffer, value);
            return value[0];
        }
        /// <summary>
        /// Reads two bytes of data and converts them to a short, to make reading a 16bit data value
        /// </summary>
        /// <param name="MSBRegAddr">The address of the MSB (15-8) bits</param>
        /// <param name="LSBRegAddr">The address of the LSB (7-0) bits</param>
        /// <returns>A short of 16bit data</returns>
        private static short I2C_ReadShort(byte MSBRegAddr, byte LSBRegAddr)
        {
            byte[] MSBbuffer = new byte[1];
            MSBbuffer[0] = MSBRegAddr;
            byte[] LSBbuffer = new byte[1];
            LSBbuffer[0] = LSBRegAddr;
            byte[] MSBValue = new byte[1];
            byte[] LSBValue = new byte[1];
            //get the 15-8 register
            MPU6050.WriteRead(MSBbuffer, MSBValue);
            //gets the 7-0 register
            MPU6050.WriteRead(LSBbuffer, LSBValue);
            //https://stackoverflow.com/questions/31654634/combine-two-bytes-to-short-using-left-shift
            return (short)(MSBValue[0] << 8 | LSBValue[0]);
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
