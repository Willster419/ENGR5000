using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.Devices.Gpio;
using RobotCode.Resources;
using Windows.System;

namespace RobotCode
{
    /// <summary>
    /// Status indicators for the various erorr
    /// </summary>
    public enum RobotStatus
    {
        /// <summary>
        /// No errors
        /// </summary>
        Idle = 1,
        /// <summary>
        /// Issues, but not critical, can be solved or ignored
        /// </summary>
        Warning = 2,
        /// <summary>
        /// Something that affects the system severly, but it can still has limited functionalatiy
        /// </summary>
        Error = 3,
        /// <summary>
        /// The robot has encountered an unhandled (should be handled) exception, the application may (should) unload
        /// </summary>
        Exception = 4,
        /// <summary>
        /// An Unknown error
        /// </summary>
        UnknownError = 5
    };
    /// <summary>
    /// Status indicators for various levels of the battery
    /// </summary>
    public enum BatteryStatus
    {
        /// <summary>
        /// unknown battery status
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// great battery
        /// </summary>
        Above75 = 5,
        /// <summary>
        /// good battery
        /// </summary>
        Between50And75 = 4,
        /// <summary>
        /// accetable, low battery, if signal circuit no change, if power circuit, go to charger
        /// </summary>
        Between25And50 = 3,
        /// <summary>
        /// warning low, if signal circuit, going back to charger
        /// may also happen upon robot start, means critical level of power circuit
        /// </summary>
        Below15Warning = 2,
        /// <summary>
        /// critical low, if signal circuit, immediate shutdown to prevent damage to components
        /// may also happen upon robot start, means critical level of signal circuit
        /// </summary>
        Below5Shutdown = 1
    }
    /// <summary>
    /// Differnt states that the robot could be in, defined from what task the robot could be working on
    /// </summary>
    public enum ControlStatus
    {
        None = 0,
        RequestManual = 1,
        Manual = 2,
        RelaseManual = 3,
        Mapping = 4,
        Cleaning = 5,
        Docking = 6,//*lennyface*
        LowPowerWait = 7
    }
    /// <summary>
    /// The Class responsible for controlling the robot and handling robot status information
    /// </summary>
    public static class RobotController
    {
        /*
         * Current timer setup
         * 0 = robot status
         * 1 = network status
         * 2 = battery status
         */
        public static StatusIndicator[] statusIndicators;
        public static RobotStatus @RobotStatus = RobotStatus.Idle;
        public static BatteryStatus SignalBatteryStatus = BatteryStatus.Unknown;//default for now
        public static BatteryStatus PowerBatteryStatus = BatteryStatus.Unknown;//default
        public static ControlStatus RobotControlStatus = ControlStatus.None;
        public static BackgroundWorker ControllerThread;
        public static bool SystemOnline = false;
        public static bool InitController()
        {
            //init the status indicators
            statusIndicators = new StatusIndicator[3];

            //robot status
            statusIndicators[0] = new StatusIndicator()
            {
                GpioPin = Hardware.Pins[0],
                Interval = TimeSpan.FromMilliseconds(250),
                TimeThrough = 0,
                Index = 0
            };
            statusIndicators[0].TimeToStop = (int)RobotStatus * 2;
            statusIndicators[0].Tick += OnStatusTick;
            statusIndicators[0].Start();

            //signal batteyr specific
            statusIndicators[1] = new StatusIndicator()
            {
                GpioPin = Hardware.Pins[2],
                Interval = TimeSpan.FromMilliseconds(200),
                TimeThrough = 0,
                Index = 1
            };
            statusIndicators[1].TimeToStop = (int)Hardware.UpdateSignalBatteryStatus() * 2;
            statusIndicators[1].Tick += OnStatusTick;
            statusIndicators[1].Start();

            //power batteyr specific
            statusIndicators[2] = new StatusIndicator()
            {
                GpioPin = Hardware.Pins[4],
                Interval = TimeSpan.FromMilliseconds(200),
                TimeThrough = 0,
                Index = 2
            };
            statusIndicators[2].TimeToStop = (int)Hardware.UpdatePowerBatteryStatus() * 2;
            statusIndicators[2].Tick += OnStatusTick;
            statusIndicators[2].Start();
            //TODO: status indicator for what the robot control is doing
            ControllerThread = new BackgroundWorker()
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };
            ControllerThread.RunWorkerCompleted += OnControlThreadExit;
            ControllerThread.ProgressChanged += ControllerLogProgress;
            ControllerThread.DoWork += ControlRobotAuto;
            ControllerThread.RunWorkerAsync();
            return true;
        }
        /// <summary>
        /// The tick event for the timer for diagnostic LEDs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnStatusTick(object sender, object e)
        {
            StatusIndicator SI = (StatusIndicator)sender;
            if (SI.TimeThrough == 0)
            {
                switch(SI.Index)
                {
                    case (int)StatusFeed.RobotStatus://robot status
                        SI.TimeToStop = (int)RobotStatus * 2;//times 2 cause one cycle is on and one is off
                        break;
                    case (int)StatusFeed.SignalBattery://signal battery
                        SI.TimeToStop = (int)Hardware.UpdateSignalBatteryStatus() * 2;
                        break;
                    case (int)StatusFeed.PowerBattery://power battery
                        SI.TimeToStop = (int)Hardware.UpdatePowerBatteryStatus() * 2;
                        break;
                }
                
            }
            else if (SI.TimeThrough == SI.TimeToStop)
            {
                //turn off the status LED
                SI.GpioPin.Write(GpioPinValue.Low);
                //set negative time so that it acts as a pause
                SI.TimeThrough = -4;
            }
            if (SI.TimeThrough >= 0)
            {
                //basicly a toggle. if high then low, if low then high.
                if (SI.GpioPin.Read() == GpioPinValue.High)
                {
                    SI.GpioPin.Write(GpioPinValue.Low);
                }
                else
                {
                    SI.GpioPin.Write(GpioPinValue.High);
                }
            }
            SI.TimeThrough++;
        }
        /// <summary>
        /// The method for the control thread to run when manual debugging control is requested
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ControlRobotManual(object sender, DoWorkEventArgs e)
        {
            //https://social.msdn.microsoft.com/Forums/vstudio/en-US/42e694a0-843a-4f7f-81bc-69e1ae662e9f/how-to-lower-the-thread-priority?forum=csharpgeneral
            if (System.Threading.Thread.CurrentThread.Priority != System.Threading.ThreadPriority.Highest)
            {
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            }
            NetworkUtils.LogNetwork("Manual control method starting", NetworkUtils.MessageType.Debug);
            while (true)
            {
                if(ControllerThread.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                if(!NetworkUtils.ConnectionLive)
                {
                    //stop moving
                    Hardware.leftDrive.SetActiveDutyCyclePercentage(0.5F);
                    Hardware.rightDrive.SetActiveDutyCyclePercentage(0.5F);
                    Hardware.Pins[3].Write(GpioPinValue.High);
                    System.Threading.Thread.Sleep(20);
                    continue;
                }
                //parse the write the commands
                string[] commands = NetworkUtils.ManualControlCommands.Split(',');
                //left (float), right (float), motor (bool)
                try
                {
                    Hardware.leftDrive.SetActiveDutyCyclePercentage(float.Parse(commands[0]));
                    Hardware.rightDrive.SetActiveDutyCyclePercentage(float.Parse(commands[1]));
                    Hardware.Pins[3].Write(bool.Parse(commands[2]) ? GpioPinValue.Low : GpioPinValue.High);
                }
                catch
                {
                    Hardware.leftDrive.SetActiveDutyCyclePercentage(0.5F);
                    Hardware.rightDrive.SetActiveDutyCyclePercentage(0.5F);
                    Hardware.Pins[3].Write(GpioPinValue.High);
                }
                System.Threading.Thread.Sleep(10);
            }
        }
        /// <summary>
        /// Default contorl method for when the robot is in regular cleaning mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ControlRobotAuto(object sender, DoWorkEventArgs e)
        {
            //https://social.msdn.microsoft.com/Forums/vstudio/en-US/42e694a0-843a-4f7f-81bc-69e1ae662e9f/how-to-lower-the-thread-priority?forum=csharpgeneral
            if (System.Threading.Thread.CurrentThread.Priority != System.Threading.ThreadPriority.Highest)
            {
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
            }
            while (true)
            {
                if (ControllerThread.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                System.Threading.Thread.Sleep(100);
            }
        }
        /// <summary>
        /// Hooks back into the UI thread upon progress from the Controller thread
        /// </summary>
        /// <param name="sender">The BackgroundWorker itself</param>
        /// <param name="e">The log message to report. Userstate is string message, percent is message type</param>
        private static void ControllerLogProgress(object sender, ProgressChangedEventArgs e)
        {
            //log here
            string message = (string)e.UserState;
            NetworkUtils.MessageType messageType = (NetworkUtils.MessageType)e.ProgressPercentage;
            NetworkUtils.LogNetwork(message, messageType);
        }
        /// <summary>
        /// Event method reasied when The control thread is exited. If cancled or error it will restart from this method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnControlThreadExit(object sender, RunWorkerCompletedEventArgs e)
        {
            NetworkUtils.LogNetwork("Control thread is down, determining what to do next", NetworkUtils.MessageType.Debug);
            //NOTE: this is on the UI thread
            if (e.Cancelled || e.Error != null)
            {
                if (e.Error != null)
                {
                    NetworkUtils.LogNetwork(e.Error.ToString(), NetworkUtils.MessageType.Error);
                }
                try
                { ControllerThread.DoWork -= ControlRobotAuto; }
                catch
                { }
                try
                { ControllerThread.DoWork -= ControlRobotManual; }
                catch
                { }
                //check for manual control
                switch (RobotControlStatus)
                {
                    case ControlStatus.RequestManual:
                        NetworkUtils.LogNetwork("Manual Control has been requested, sending ack and enabling manual control",NetworkUtils.MessageType.Debug);
                        NetworkUtils.LogNetwork("request_ack", NetworkUtils.MessageType.Control);
                        ControllerThread.DoWork += ControlRobotManual;
                        ControllerThread.RunWorkerAsync();
                        break;
                    case ControlStatus.Manual:
                        NetworkUtils.LogNetwork("Resuming manual control", NetworkUtils.MessageType.Debug);
                        ControllerThread.DoWork += ControlRobotManual;
                        ControllerThread.RunWorkerAsync();
                        break;
                    case ControlStatus.RelaseManual:
                        NetworkUtils.LogNetwork("Releasing manual control, restarting auto", NetworkUtils.MessageType.Debug);
                        NetworkUtils.LogNetwork("release_ack", NetworkUtils.MessageType.Control);
                        ControllerThread.DoWork += ControlRobotAuto;
                        ControllerThread.RunWorkerAsync();
                        break;
                }
            }
            else
            {
                NetworkUtils.LogNetwork("The controller thread has ended, is the application unloading?", NetworkUtils.MessageType.Debug);
                return;
            }
        }
        /// <summary>
        /// Stops all motor activity incase of emergency and sets the robot state.
        /// </summary>
        public static void EmergencyStop()
        {
            //everything must stop, a critical exception has occured
            RobotStatus = RobotStatus.Exception;
            //shut off any relays on
            Hardware.Pins[3].Write(GpioPinValue.Low);
            //shut off any PWM systems

        }
        /// <summary>
        /// Usually when the signal (RPi) battery is in a critical state. Shuts down the unit in the event of a critical
        /// system failure and sets the robot state.
        /// </summary>
        public static void EmergencyShutdown(TimeSpan delay)
        {
            NetworkUtils.LogNetwork(string.Format("CRITICAL SYSTEM MESSAGE: The device is shutting down in {0} seconds, use" +
                " ShutdownManager.CancelShutdown() to cancel", delay.TotalSeconds),NetworkUtils.MessageType.Warning);
            //completly shut down the robot
            ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, delay);
        }
        /// <summary>
        /// Requests a reboot of the system
        /// </summary>
        /// <param name="delay">The delay before the reboot takes place</param>
        public static void Reboot(TimeSpan delay)
        {
            NetworkUtils.LogNetwork(string.Format("NOTICE: Rebooting in {0} seconds", delay.TotalSeconds), NetworkUtils.MessageType.Warning);
            ShutdownManager.BeginShutdown(ShutdownKind.Restart, delay);
        }
        /// <summary>
        /// Reauests a poweroff of the system
        /// </summary>
        /// <param name="delay">The dealy before the poweroff takes place</param>
        public static void Poweroff(TimeSpan delay)
        {
            NetworkUtils.LogNetwork(string.Format("NOTICE: Shutting down in {0} seconds", delay.TotalSeconds), NetworkUtils.MessageType.Warning);
            ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, delay);
        }
        /// <summary>
        /// Cancels a pending shutdown or reboot, if one exists
        /// </summary>
        public static void CancelShutdown()
        {
            NetworkUtils.LogNetwork("WARNING: Canceling shutdown/reboot", NetworkUtils.MessageType.Warning);
            ShutdownManager.CancelShutdown();
        }
    }
}
