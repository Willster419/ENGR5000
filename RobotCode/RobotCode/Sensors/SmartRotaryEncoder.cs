using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.UI.Core;
using System.ServiceModel;
using Windows.Devices;

namespace RobotCode.Sensors
{
    public class SmartRotaryEncoder
    {
        /// <summary>
        /// The GPIO pin for the CLK pin
        /// </summary>
        public GpioPin CLKPin;
        /// <summary>
        /// The GPIO pin for the DT pin
        /// </summary>
        public GpioPin DTPin;
        /// <summary>
        /// The number of times the encoder has moved at each full click
        /// </summary>
        public int Clicks { get; private set; }
        /// <summary>
        /// The number of valid state changes detected (4 ticks in a click)
        /// </summary>
        public int Ticks { get; private set; }
        /// <summary>
        /// Switch to add or subtract values (cause they turn different ways)
        /// </summary>
        public bool NegateValues { get; private set; } = false;
        //TODO: document error more?
        /// <summary>
        /// Switch to enable the periodic detection of error accumulation and add them to the current value
        /// </summary>
        public bool EnableErrorCorrection { get; private set; } = false;
        /// <summary>
        /// The array to keep track of the current and previous states of the encoder. A state is a read of both pins
        /// </summary>
        private byte[] StateValues = new byte[] { 0, 0, 0, 0 };
        //idea is that the value should be 0 after a click. If not, a value was skipped
        /// <summary>
        /// The state array to keep track of the states on a falling edge form the encoder. A state is read of both pins.
        /// </summary>
        private byte[] ErrorStateValues = new byte[] { 0, 0, 0, 0 };
        private int ErrorCounter = 0;
        public int ErrorThrshold { get; private set; } = 0;
        /// <summary>
        /// Creates an instance of the SmartRotaryEncoder object
        /// </summary>
        public SmartRotaryEncoder() { }
        /// <summary>
        /// Initialize the encoder
        /// </summary>
        /// <param name="clkPinNumber">The GPIO pin number connected to the CLK pin</param>
        /// <param name="dtPinNumber">The GPIO pin number connected to the DT pin</param>
        /// <param name="controller">The GPIO controller</param>
        /// <param name="negateValues">Flag to negate the values (for other side of robot, for example)</param>
        /// <param name="errorThreshold">The ammount of click error accumulation to wait untill applying error connectoin. Must be above 2. Set 0 to disable Erorr correction</param>
        /// <returns></returns>
        public bool InitEncoder(int clkPinNumber, int dtPinNumber, GpioController controller, bool negateValues, int errorThreshold)
        {
            CLKPin = controller.OpenPin(clkPinNumber);
            if (CLKPin == null)
                return false;
            CLKPin.SetDriveMode(GpioPinDriveMode.Input);
            CLKPin.ValueChanged += OnValueChange;

            DTPin = controller.OpenPin(dtPinNumber);
            if (DTPin == null)
                return false;
            DTPin.SetDriveMode(GpioPinDriveMode.Input);
            DTPin.ValueChanged += OnValueChange;

            NegateValues = negateValues;
            if(errorThreshold > 1)
            {
                EnableErrorCorrection = true;
                ErrorThrshold = errorThreshold;
            }
            else
            {
                EnableErrorCorrection = false;
                ErrorThrshold = 0;
            }

            Clicks = 0;
            Ticks = 0;
            return true;
        }
        /// <summary>
        /// Event for when a change in value is detected
        /// </summary>
        /// <param name="sender">The GPIO pin object</param>
        /// <param name="args">GPIO pin event arguements</param>
        private void OnValueChange(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (RobotController.SystemDispatcher == null)
            {
                NetworkUtils.LogNetwork("RobotController.SystemDispatcher is null", MessageType.Error);
                return;
            }
            var task = RobotController.SystemDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                //https://hifiduino.wordpress.com/2010/10/20/rotaryencoder-hw-sw-no-debounce/
                //DT first, then CLK
                //0,1 are old, 2,3 are new
                //DT,CLK,DT,CLK
                StateValues[0] = StateValues[2];
                StateValues[1] = StateValues[3];
                if (sender == DTPin)
                {
                    StateValues[2] = (byte)args.Edge;//1 = rising, 0 = falling
                    StateValues[3] = (byte)CLKPin.Read();
                }
                else
                {
                    StateValues[3] = (byte)args.Edge;//1 = rising, 0 = falling
                    StateValues[2] = (byte)DTPin.Read();
                }
                //CCW
                if (StateValues.SequenceEqual(Hardware.ccw1) || StateValues.SequenceEqual(Hardware.ccw2) || StateValues.SequenceEqual(Hardware.ccw3) || StateValues.SequenceEqual(Hardware.ccw4))
                {
                    //Ticks++;
                    Ticks = NegateValues ? Ticks - 1 : Ticks + 1;
                    Clicks = Ticks / 4;
                }
                //CW
                else if (StateValues.SequenceEqual(Hardware.cw1) || StateValues.SequenceEqual(Hardware.cw2) || StateValues.SequenceEqual(Hardware.cw3) || StateValues.SequenceEqual(Hardware.cw4))
                {
                    //Ticks--;
                    Ticks = NegateValues ? Ticks + 1 : Ticks - 1;
                    Clicks = Ticks / 4;
                }
                if (EnableErrorCorrection && args.Edge == GpioPinEdge.FallingEdge)
                {
                    ErrorStateValues[0] = ErrorStateValues[2];
                    ErrorStateValues[1] = ErrorStateValues[3];
                    ErrorStateValues[2] = StateValues[2];
                    ErrorStateValues[3] = StateValues[3];
                    if//CCW
                    (
                        ErrorStateValues.SequenceEqual(Hardware.ccw1) ||
                        ErrorStateValues.SequenceEqual(Hardware.ccw2) ||
                        ErrorStateValues.SequenceEqual(Hardware.ccw3) ||
                        ErrorStateValues.SequenceEqual(Hardware.ccw4)
                    )
                    {
                        ErrorCounter--;
                    }
                    else if//CW
                    (
                        ErrorStateValues.SequenceEqual(Hardware.cw1) ||
                        ErrorStateValues.SequenceEqual(Hardware.cw2) ||
                        ErrorStateValues.SequenceEqual(Hardware.cw3) ||
                        ErrorStateValues.SequenceEqual(Hardware.cw4)
                    )
                    {
                        ErrorCounter++;
                    }
                    if (ErrorCounter >= ErrorThrshold)
                        OnErrorAccumulation();
                }
            });
        }
        private void OnErrorAccumulation()
        {
            NetworkUtils.LogNetwork("Feature not yet implimented: SmartRotaryEncoder->OnErrorAccumulation()", MessageType.Warning);
            ErrorCounter = 0;
        }
        /// <summary>
        /// Resets the Counter and ticks variables
        /// </summary>
        public void ResetCounter() { Clicks = 0; Ticks = 0; }
    }
}
