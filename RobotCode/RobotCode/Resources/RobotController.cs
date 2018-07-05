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
    /// Status indicators for the various states the robot could be in
    /// </summary>
    public enum RobotStatus
    {
        Idle = 1,
        Error = 2,
        Exception = 3,
        UnknownError = 4
    };
    /// <summary>
    /// Status indicators for various levels of the battery
    /// </summary>
    public enum BatteryStatus
    {
        //unknown battery status
        Unknown = 0,
        //good battery, no issues here
        Above75 = 5,
        //stil la good battery
        Between50And75 = 4,
        //low battery, if signal circuit no change, if power circuit, go to charger (same for all below)
        Between25And50 = 3,
        //warning low, if signal circuit, going back to charger
        //may also happen upon robot start, means critical level of power circuit
        Below15Warning = 2,
        //critical low, if signal circuit, immediate shutdown to prevent damage to components
        //may also happen upon robot start, means critical level of signal circuit
        Below5Shutdown = 1
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
        private static BackgroundWorker ControllerThread;
        public static bool InitController()
        {
            //init the status indicators
            statusIndicators = new StatusIndicator[3];

            //robot status
            statusIndicators[0] = new StatusIndicator()
            {
                GpioPin = GPIO.Pins[0],
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
                GpioPin = GPIO.Pins[2],
                Interval = TimeSpan.FromMilliseconds(200),
                TimeThrough = 0,
                Index = 1
            };
            statusIndicators[1].TimeToStop = (int)GPIO.UpdateSignalBatteryStatus() * 2;
            statusIndicators[1].Tick += OnStatusTick;
            statusIndicators[1].Start();

            //power batteyr specific
            statusIndicators[2] = new StatusIndicator()
            {
                GpioPin = GPIO.Pins[4],
                Interval = TimeSpan.FromMilliseconds(200),
                TimeThrough = 0,
                Index = 2
            };
            statusIndicators[2].TimeToStop = (int)GPIO.UpdatePowerBatteryStatus() * 2;
            statusIndicators[2].Tick += OnStatusTick;
            statusIndicators[2].Start();

            ControllerThread = new BackgroundWorker()
            {
                WorkerSupportsCancellation = false,
                WorkerReportsProgress = true
            };
            ControllerThread.RunWorkerCompleted += OnWorkCompleted;
            ControllerThread.ProgressChanged += ControllerLogProgress;
            ControllerThread.DoWork += ControlRobot;
            ControllerThread.RunWorkerAsync();
            return true;
        }

        

        private static void OnStatusTick(object sender, object e)
        {
            StatusIndicator SI = (StatusIndicator)sender;
            if (SI.TimeThrough == 0)
            {
                switch(SI.Index)
                {
                    case 0://robot status
                        SI.TimeToStop = (int)RobotStatus * 2;//times 2 cause one cycle is on and one is off
                        break;
                    case 1://signal battery
                        SI.TimeToStop = (int)GPIO.UpdateSignalBatteryStatus() * 2;
                        break;
                    case 2://power battery
                        SI.TimeToStop = (int)GPIO.UpdatePowerBatteryStatus() * 2;
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

        private static void ControlRobot(object sender, DoWorkEventArgs e)
        {
            string message = "";
            while (true)
            {
                //using report progress allows two things:
                //1: allows reporting to be async (not blocking controller)
                //2: allows reporting to be on the UI thread (where it should be)
                /*
                message = string.Format("Signal Voltage: {0}V", (GPIO.ReadVoltage(GPIO.SIGNAL_VOLTAGE_MONITOR_CHANNEL) / 1000.0F));
                ControllerThread.ReportProgress((int)NetworkUtils.MessageType.Debug, message);
                System.Threading.Thread.Sleep(250);
                
                message = string.Format("Power Voltage: {0}V", (GPIO.ReadVoltage(GPIO.POWER_VOLTAGE_MONITOR_CHANNEL) / 1000.0F));
                ControllerThread.ReportProgress((int)NetworkUtils.MessageType.Debug, message);
                System.Threading.Thread.Sleep(250);
                message = string.Format("Tempature Voltage: {0}V", (GPIO.ReadVoltage(GPIO.TEMPATURE_CHANNEL) / 1000.0F));
                ControllerThread.ReportProgress((int)NetworkUtils.MessageType.Debug, message);
                System.Threading.Thread.Sleep(250);
                message = string.Format("Water Voltage: {0}V", (GPIO.ReadVoltage(GPIO.WATER_LEVEL_CHANNEL) / 1000.0F));
                ControllerThread.ReportProgress((int)NetworkUtils.MessageType.Debug, message);
                System.Threading.Thread.Sleep(250);
                */
                GPIO.leftDrive.SetActiveDutyCyclePercentage(0.4);
                GPIO.rightDrive.SetActiveDutyCyclePercentage(0.4);
                System.Threading.Thread.Sleep(1000);
                GPIO.leftDrive.SetActiveDutyCyclePercentage(0.6);
                GPIO.rightDrive.SetActiveDutyCyclePercentage(0.6);
                System.Threading.Thread.Sleep(1000);
                GPIO.leftDrive.SetActiveDutyCyclePercentage(0.5);
                GPIO.rightDrive.SetActiveDutyCyclePercentage(0.5);
                System.Threading.Thread.Sleep(500);
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

        private static void OnWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Cancelled)
            {
                NetworkUtils.LogNetwork("Controller thread was Cancelled! This should never happen!!", NetworkUtils.MessageType.Error);
                EmergencyShutdown(TimeSpan.FromSeconds(60));
            }
            else if (e.Error != null)
            {
                NetworkUtils.LogNetwork(e.Error.ToString(), NetworkUtils.MessageType.Error);
                EmergencyShutdown(TimeSpan.FromSeconds(60));
            }
            else
            {
                NetworkUtils.LogNetwork("The controller thread has ended, is the application unloading?", NetworkUtils.MessageType.Debug);
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
            GPIO.Pins[3].Write(GpioPinValue.Low);
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
    }
}
