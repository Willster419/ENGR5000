using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.Devices.Gpio;
using RobotCode.Resources;

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
        Above75 = 1,
        //stil la good battery
        Between50And75 = 2,
        //low battery, if signal circuit no change, if power circuit, go to charger (same for all below)
        Between25And50 = 3,
        //warning low, if signal circuit, going back to charger
        //may also happen upon robot start, means critical level of power circuit
        Below15Warning = 4,
        //critical low, if signal circuit, immediate shutdown to prevent damage to components
        //may also happen upon robot start, means critical level of signal circuit
        Below5Shutdown = 5
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
        public static BatteryStatus SignalBatteryStatus = BatteryStatus.Above75;//default for now
        public static BatteryStatus PowerBatteryStatus = BatteryStatus.Above75;//default
        private static BackgroundWorker ControllerThread;
        public static bool InitController()
        {
            /*
            TimeToStop = (int)RobotStatus * 2;
            RobotStatusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            RobotStatusTimer.Tick += OnStatusLEDTick;
            RobotStatusTimer.Start();
            */

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
            statusIndicators[1].TimeToStop = (int)GPIO.GetSignalBatteryStatus() * 2;
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
            statusIndicators[2].TimeToStop = (int)GPIO.GetPowerBatteryStatus() * 2;
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
                        SI.TimeToStop = (int)GPIO.GetSignalBatteryStatus() * 2;
                        break;
                    case 2://power battery
                        SI.TimeToStop = (int)GPIO.GetPowerBatteryStatus() * 2;
                        break;
                }
                
            }
            else if (SI.TimeThrough == SI.TimeToStop)
            {
                //turn off the status LED
                SI.GpioPin.Write(GpioPinValue.Low);
                SI.TimeThrough = -4;
            }
            if (SI.TimeThrough >= 0)
            {
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
                message = string.Format("Signal Voltage: {0}V", (GPIO.ReadVoltage(GPIO.SIGNAL_VOLTAGE_MONITOR_CHANNEL) / 1000.0F));
                NetworkUtils.LogNetwork(message, NetworkUtils.MessageType.Info);
                System.Threading.Thread.Sleep(250);
                message = string.Format("Power Voltage: {0}V", (GPIO.ReadVoltage(GPIO.POWER_VOLTAGE_MONITOR_CHANNEL) / 1000.0F));
                NetworkUtils.LogNetwork(message, NetworkUtils.MessageType.Info);
                System.Threading.Thread.Sleep(250);
                message = string.Format("Tempature Voltage: {0}V", (GPIO.ReadVoltage(GPIO.TEMPATURE_CHANNEL) / 1000.0F));
                NetworkUtils.LogNetwork(message, NetworkUtils.MessageType.Info);
                System.Threading.Thread.Sleep(250);
                message = string.Format("Water Voltage: {0}V", (GPIO.ReadVoltage(GPIO.WATER_LEVEL_CHANNEL) / 1000.0F));
                NetworkUtils.LogNetwork(message, NetworkUtils.MessageType.Info);
                System.Threading.Thread.Sleep(250);
            }
        }

        private static void ControllerLogProgress(object sender, ProgressChangedEventArgs e)
        {
            //log here

        }

        private static void OnWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Cancelled)
            {
                NetworkUtils.LogNetwork("Controller thread was Cancelled! This should never happen!!", NetworkUtils.MessageType.Exception);
                EmergencyShutdown();
            }
            else if (e.Error != null)
            {
                NetworkUtils.LogNetwork(e.Error.ToString(), NetworkUtils.MessageType.Exception);
                EmergencyShutdown();
            }
            else
            {
                NetworkUtils.LogNetwork("The controller thread has ended, is the application unloading?", NetworkUtils.MessageType.Debug);
            }
        }

        public static void EmergencyShutdown()
        {
            //everything must stop, a critical exception has occured
            RobotStatus = RobotStatus.Exception;
            //shut off any relays on
            GPIO.Pins[3].Write(GpioPinValue.Low);
            //shut off any PWM systems

        }
    }
}
