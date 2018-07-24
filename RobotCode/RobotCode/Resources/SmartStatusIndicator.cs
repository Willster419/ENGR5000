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
        public TimeSpan Delay { get; private set; }
        private CancellationToken CancelToken;
        private CancellationTokenSource CancelTokenSource;
        /// <summary>
        /// The pin used to toggle the status LED
        /// </summary>
        private GpioPin _pin;
        private Task Pin_task;
        public SmartStatusIndicator() { }
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
        public bool Start()
        {
            if (Pin_task == null)
                return false;
            if (Pin_task.Status == TaskStatus.Running)
                return false;
            Pin_task.Start();
            return true;
        }
        public void Stop()
        {
            CancelTokenSource.Cancel();
        }
        private async void RunStatusTask(CancellationToken ct)
        {
            int real_time_to_stop = Time_to_stop;
            while(true)
            {
                if (ct.IsCancellationRequested)
                {
                    //turn off the light and stop
                    _pin.Write(GpioPinValue.Low);
                    NetworkUtils.LogNetwork("task stop request gotten, stopping", NetworkUtils.MessageType.Debug);
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
        public void UpdateRuntimeValue(int timeSource)
        {
            Time_to_stop = timeSource;
        }
    }
}
