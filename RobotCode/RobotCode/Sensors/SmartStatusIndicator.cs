using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace RobotCode.Resources
{
    /// <summary>
    /// Represents an LED that shows diagnostic feedback based on the input source
    /// </summary>
    public class SmartStatusIndicator
    {
        /// <summary>
        /// counter to represents where the status indicator is in it's current itteration
        /// </summary>
        private int Time_thorugh_status = 0;
        /// <summary>
        /// Int represents the number of blinks to make on this irteratoin based on the status passed into it
        /// </summary>
        private int Time_to_stop = 0;
        /// <summary>
        /// The gpio pin object used for the status indiactor
        /// </summary>
        public int Pin
        {
            get
            {
                return _pin == null? -1: _pin.PinNumber;
            }
        }
        /// <summary>
        /// The delay used for determining the frequency of which the LED with flash the status indication and cycle through them
        /// </summary>
        public TimeSpan Delay { get; private set; }
        /// <summary>
        /// The token used for cancelation
        /// </summary>
        private CancellationToken CancelToken;
        /// <summary>
        /// The source of cancelToken
        /// </summary>
        private CancellationTokenSource CancelTokenSource;
        /// <summary>
        /// The pin used to toggle the status LED
        /// </summary>
        private GpioPin _pin;
        /// <summary>
        /// The Task that asyncronously runs on a seperate thread to keep the UI thread as free as possible
        /// </summary>
        private Task Pin_task;
        public SmartStatusIndicator() { }
        /// <summary>
        /// Initializes the Status Indicator
        /// </summary>
        /// <param name="contorller">The GPIO controller to open the pins on</param>
        /// <param name="pin_to_use">The GPIO pin number ot use</param>
        /// <param name="delay">The delay for determining the frequency of which the LED with flash the status indication</param>
        /// <param name="first_time_source">An initializaion value, determins how many times the LED will first blink before an official value as set later</param>
        /// <returns>True if initalization was sucessfull, false otherwise</returns>
        public bool InitIndicator(GpioController contorller, int pin_to_use, TimeSpan delay, int first_time_source)
        {
            if(contorller == null)
                return false;
            if (pin_to_use <= 0)
                return false;
            try
            {
                //if this fails then eithor the pin is in use or 
                _pin = contorller.OpenPin(pin_to_use);
            }
            catch
            {
                return false;
            }
            //outputs so default to show nothing
            _pin.Write(GpioPinValue.Low);
            //it's an output
            _pin.SetDriveMode(GpioPinDriveMode.Output);
            Delay = delay;
            //https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-cancel-a-task-and-its-children
            CancelTokenSource = new CancellationTokenSource();
            CancelToken = CancelTokenSource.Token;
            Pin_task = new Task(() => RunStatusTask(CancelToken), CancelToken);
            Time_to_stop = first_time_source;
            return true;
        }
        /// <summary>
        /// Start the Task to run asyncronously
        /// </summary>
        /// <returns>True if Task starting was sucessfull, false otherwise</returns>
        public bool Start()
        {
            if (Pin_task == null)
                return false;
            if (Pin_task.Status == TaskStatus.Running)
                return false;
            Pin_task.Start();
            return true;
        }
        /// <summary>
        /// Sends a cancel request to the Task so that it stopps running
        /// </summary>
        public void Stop()
        {
            CancelTokenSource.Cancel();
        }
        /// <summary>
        /// The method that the Task uses to run async
        /// </summary>
        /// <param name="ct">The Cancelation token send into the method to detect if the calling parent task/thread wants it to end</param>
        private async void RunStatusTask(CancellationToken ct)
        {
            int real_time_to_stop = Time_to_stop;
            while(true)
            {
                if (ct.IsCancellationRequested)
                {
                    //turn off the light and stop
                    _pin.Write(GpioPinValue.Low);
                    NetworkUtils.LogNetwork("task stop request gotten, stopping", MessageType.Debug);
                    return;
                }
                while(Time_thorugh_status < Time_to_stop)
                {
                    _pin.Write(GpioPinValue.High);
                    await Task.Delay(Delay);
                    _pin.Write(GpioPinValue.Low);
                    await Task.Delay(Delay);
                    Time_thorugh_status++;
                }
                Time_thorugh_status = 0;
                //Time_to_stop updates outside of this function
                real_time_to_stop = Time_to_stop;
                await Task.Delay(Delay + Delay + Delay);
            }
        }
        /// <summary>
        /// Called externally to update the number of blinks the LED will blink
        /// </summary>
        /// <param name="timeSource"></param>
        public void UpdateRuntimeValue(int timeSource)
        {
            Time_to_stop = timeSource;
        }
    }
}
