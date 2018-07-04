using Windows.Devices.Pwm;
using Windows.Devices.Gpio;

namespace RobotCode.Resources
{
    /// <summary>
    /// A Sign-Magnitude PWM connection uses a direction pin and a PWM pin.
    /// Helps to fix the issue of the "crawl" when at "0" of Antilock-phase PWM.
    /// 0-50 input is converted to 0-100 and a certain direction
    /// </summary>
    public class SMPWM
    {
        /// <summary>
        /// The PWM signal pin to use for percentage
        /// </summary>
        private PwmPin _pwmPin;
        /// <summary>
        /// The GPIO pin to use for direction
        /// </summary>
        private GpioPin _GPIOPin;
        public SMPWM(){  }
        /// <summary>
        /// Initialize the Pins with default values of low and 0
        /// </summary>
        /// <param name="GPIO_pin">The number of the GPIO pin to open</param>
        /// <param name="PWM_pin">The number of the PWM pin to open</param>
        /// <param name="gpioController">The gpio controller</param>
        /// <param name="pwmController">The PWM controller</param>
        public void Init(int GPIO_pin, int PWM_pin, GpioController gpioController, PwmController pwmController)
        {
            _pwmPin = pwmController.OpenPin(PWM_pin);
            _GPIOPin = gpioController.OpenPin(GPIO_pin);
            _GPIOPin.Write(GpioPinValue.Low);
            _GPIOPin.SetDriveMode(GpioPinDriveMode.Output);
            _pwmPin.SetActiveDutyCyclePercentage(0);
        }
        /// <summary>
        /// Start the PWM pin
        /// </summary>
        public void Start()
        {
            _pwmPin.Start();
        }
        /// <summary>
        /// Stop the PWM pin and set the pins with default values of low and 0
        /// </summary>
        public void Stop()
        {
            _pwmPin.SetActiveDutyCyclePercentage(0);
            _GPIOPin.Write(GpioPinValue.Low);
            _pwmPin.Stop();
        }
        /// <summary>
        /// Determinds if the PWM Pin is currently started
        /// </summary>
        /// <returns>True if started, false otherwise</returns>
        public bool IsStarted() { return _pwmPin.IsStarted; }
        /// <summary>
        /// Set the speed for the motor to move. above 0.5 sets the direction to high, below to low, and 0 to off
        /// </summary>
        /// <param name="percentage">The amount, in 0-1, to move the motors by</param>
        public void SetActiveDutyCyclePercentage(double percentage)
        {
            if (percentage == 0.5)
            {
                SetActiveDutyCyclePercentage(GpioPinValue.Low, 0.0);
            }
            else if(percentage > 0.5)
            {
                SetActiveDutyCyclePercentage(GpioPinValue.High, (percentage - 0.5) * 2);
            }
            else if (percentage < 0.5)
            {
                SetActiveDutyCyclePercentage(GpioPinValue.Low, (0.5 - percentage) * 2);
            }
        }
        /// <summary>
        /// Set the speed for the motor to move.
        /// </summary>
        /// <param name="value">High is x, Low is y</param>
        /// <param name="percentage">The percentage, from 0-1, for the magnitude of the given direction</param>
        public void SetActiveDutyCyclePercentage(GpioPinValue value, double percentage)
        {
            _GPIOPin.Write(value);
            _pwmPin.SetActiveDutyCyclePercentage(percentage);
        }
        public double GetActiveDutyCyclePercentage()
        {
            return _pwmPin.GetActiveDutyCyclePercentage();
        }
    }
}
