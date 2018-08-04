using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace RobotCode.Resources
{
    /// <summary>
    /// A simple approach to the IR reciever
    /// </summary>
    public class IRReciever
    {
        /// <summary>
        /// Flag for if the IR wall has been detected
        /// </summary>
        public bool WallDetected { get; private set; } = false;
        /// <summary>
        /// The Pin number of the GPIO pin
        /// </summary>
        public int PinNumber { get; private set; } = -1;//not a valid pin number for a reason
        /// <summary>
        /// The GPIO pin connected to the IR sensor
        /// </summary>
        private GpioPin _pin;
        /// <summary>
        /// The Number to Detections (in case signal is PWM and to prevent one immediate detection)
        /// </summary>
        private int NumDetections = 0;
        /// <summary>
        /// The number of falling edge detections untill the system considers a detection to be genuine
        /// </summary>
        private int DetectionThreshold = 0;
        /// <summary>
        /// Flag to help stop rouge falling edges, set with start and stop
        /// </summary>
        public bool Enabled { get; private set; } = false;
        /// <summary>
        /// Create an instance of the IR receiver object
        /// </summary>
        public IRReciever() { }
        /// <summary>
        /// Initaliise the IR sensor
        /// </summary>
        /// <param name="pinNumber">The GPIO pin number to use</param>
        /// <param name="detectionThreshold">The number of falling edges to count before considering the signal genuine</param>
        /// <param name="controller">The GpioController to open the pins on</param>
        /// <returns></returns>
        public bool InitSensor(int pinNumber, int detectionThreshold, GpioController controller)
        {
            //verify stuff
            if (controller == null)
                return false;
            if (pinNumber < 0)
                return false;
            //so it can be retrived
            PinNumber = pinNumber;
            DetectionThreshold = detectionThreshold;
            //try to use the controller
            try
            { _pin = controller.OpenPin(pinNumber); }
            catch
            { return false; }
            //default from IR sensor is a high value when nothing detected
            _pin.Write(GpioPinValue.High);
            _pin.SetDriveMode(GpioPinDriveMode.Input);
            return true;
        }
        /// <summary>
        /// Start listeing for falling edge changes
        /// </summary>
        public void Start()
        {
            _pin.ValueChanged += OnValueChanged;
            Enabled = true;
        }
        /// <summary>
        /// Stops listening for falling edge changes
        /// </summary>
        /// <param name="reset">If true, also reset the detection algorithim</param>
        public void Stop(bool reset)
        {
            Enabled = false;
            if(reset)
            {
                ResetDetection();
            }
        }
        /// <summary>
        /// Resets the internal detection algorithim
        /// </summary>
        public void ResetDetection()
        {
            WallDetected = false;
            NumDetections = 0;
            _pin.ValueChanged += OnValueChanged;
        }
        /// <summary>
        /// Clears the number of detections. Implied use is to remove noise by claering false detections
        /// </summary>
        public void ClearDetectionBuffer()
        {
            NumDetections = 0;
        }
        /// <summary>
        /// Event handler for when the GPIO pin value is changed
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="args">Event arguements</param>
        private void OnValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            //default is high, so check for lows
            //roomba IR works by pulsing the frequency
            //but we only need a few downs to detect the IR

            //but make sure we have started the sensor reading
            if (!Enabled)
                return;
            if (args.Edge == GpioPinEdge.FallingEdge)
            {
                if (NumDetections++ >= DetectionThreshold)
                {
                    WallDetected = true;
                    //do not fire any more events since we don't have to anymore
                    _pin.ValueChanged -= OnValueChanged;
                }
            }
        }
    }
}
