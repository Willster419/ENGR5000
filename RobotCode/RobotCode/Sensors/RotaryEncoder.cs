using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;

namespace RobotCode.Resources
{
    /// <summary>
    /// Represents a Rotary Encoder piece of hardware on the robot
    /// </summary>
    public class RotaryEncoder
    {
        /// <summary>
        /// The GPIO pin for the CLK line
        /// </summary>
        public GpioPin CLKPin;
        /// <summary>
        /// The GPIO pin for the DT pin
        /// </summary>
        public GpioPin DTPin;
        /// <summary>
        /// The number of times the encoder has moved at each full cycle (however many ticks it has to make a full circle)
        /// </summary>
        public int Counter { get; private set; }
        /// <summary>
        /// The number of valid state changes detected
        /// </summary>
        public int Ticks { get; private set; }
        /// <summary>
        /// Switch to add or subtract values (cause they turn different ways)
        /// </summary>
        public bool NegateValues { get; private set; } = false;
        /// <summary>
        /// The array to keep track of the current and previous states of the encoder. A state is a read of both pins
        /// </summary>
        private byte[] Values = new byte[] { 0, 0, 0, 0 };
        /// <summary>
        /// Creates an instance of the object
        /// </summary>
        public RotaryEncoder() { }
        /// <summary>
        /// Initialize the encoder
        /// </summary>
        /// <param name="clkPinNumber">The GPIO pin number connected to the CLK pin</param>
        /// <param name="dtPinNumber">The GPIO pin number connected to the DT pin</param>
        /// <param name="controller">The GPIO controller</param>
        /// <param name="negateValues">Flag to negate the values (for other side of robot, for example)</param>
        /// <returns></returns>
        public bool InitEncoder(int clkPinNumber, int dtPinNumber, GpioController controller, bool negateValues)
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

            Counter = 0;
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
            //https://hifiduino.wordpress.com/2010/10/20/rotaryencoder-hw-sw-no-debounce/
            //DT first, then CLK
            //0,1 are old, 2,3 are new
            //DT,CLK,DT,CLK
            Values[0] = Values[2];
            Values[1] = Values[3];
            if (sender == DTPin)
            {
                Values[2] = (byte)args.Edge;//1 = rising, 0 = falling
                Values[3] = (byte)CLKPin.Read();
            }
            else
            {
                Values[3] = (byte)args.Edge;//1 = rising, 0 = falling
                Values[2] = (byte)DTPin.Read();
            }
            //CCW
            if (Values.SequenceEqual(Hardware.ccw1) || Values.SequenceEqual(Hardware.ccw2) || Values.SequenceEqual(Hardware.ccw3) || Values.SequenceEqual(Hardware.ccw4))
            {
                //Ticks++;
                Ticks = NegateValues ? Ticks - 1 : Ticks + 1;
                Counter = Ticks / 4;
            }
            //CW
            else if (Values.SequenceEqual(Hardware.cw1) || Values.SequenceEqual(Hardware.cw2) || Values.SequenceEqual(Hardware.cw3) || Values.SequenceEqual(Hardware.cw4))
            {
                //Ticks--;
                Ticks = NegateValues ? Ticks + 1 : Ticks - 1;
                Counter = Ticks / 4;
            }
        }
        /// <summary>
        /// Resets the Counter and ticks variables
        /// </summary>
        public void ResetCounter() { Counter = 0; Ticks = 0; }
        /// <summary>
        /// Corrects the Ticks value to be a multiple of 4
        /// </summary>
        public void CorrectTicks()
        {
            while(Ticks % 4 != 0)
            {
                Ticks++;
            }
            Counter = Ticks % 4;
        }

    }
}
