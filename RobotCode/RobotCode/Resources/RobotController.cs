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
using RobotCode.Mapping;

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
    /// The various different fine control states that the robot could be in
    /// </summary>
    public enum AutoControlState
    {
        /// <summary>
        /// Starting point for all auto control. Starts from the dock, if you will.
        /// </summary>
        None,
        /// <summary>
        /// When the robot is leaving the dock and onto the field (of battle)
        /// </summary>
        LeavingDock,
        /// <summary>
        /// Is is now inside the area of work and turns to the right to begin mapping
        /// </summary>
        TurnToMap,
        /// <summary>
        /// Mapping the first side of the rectangle
        /// </summary>
        MapOneSide,
        /// <summary>
        /// The front and side IR sensors have both detected walls, it is at a corner.
        /// Need to turn and map the next part of the rectnagle. Applies for all corners
        /// </summary>
        MapTurn,
        /// <summary>
        /// Mapping the second side ofthe rectangle
        /// </summary>
        MapTwoSide,
        /// <summary>
        /// Mapping the third side of the rectnagle
        /// </summary>
        MapThreeSide,
        /// <summary>
        /// Mapping the fourther side of the rectangle
        /// </summary>
        MapFourSide,
        /// <summary>
        /// Performing calculations to create the map and send the XML map data to the dashboard
        /// </summary>
        Calculations,
        /// <summary>
        /// Cleaning the area, in the up side
        /// </summary>
        CleanUp,
        /// <summary>
        /// Cleaning the area, in the down side
        /// </summary>
        CleanDown,
        /// <summary>
        /// On cleaning complete, going back to base to charge or something
        /// </summary>
        CleanComplete,
        /// <summary>
        /// When an obstruction is found when the robot is maping the work area
        /// </summary>
        OnObstuctionWhenMapping,
        /// <summary>
        /// When an obstruction is found when the robot is cleaning the work area
        /// </summary>
        OnObstructionWhenCleaning,
        /// <summary>
        /// When the signal battey is at a low level
        /// </summary>
        OnLowSignalBattery,
        /// <summary>
        /// When the power battery is at a low level
        /// </summary>
        OnLowPowerBattery,
        /// <summary>
        /// When the water tank has reached a level where it needs to be dumped
        /// </summary>
        OnWaterLimit
    }
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
        /// <summary>
        /// No control data
        /// </summary>
        None = 0,
        /// <summary>
        /// Manual control has been requested from the dashboard. async inturrupt is sent to the thread
        /// </summary>
        RequestManual = 1,
        /// <summary>
        /// Manual contorl mode form the dashboard
        /// </summary>
        Manual = 2,
        /// <summary>
        /// Manual control has been requestedto be relased form the dashboard. async inturrupt is send to the contorl thread
        /// </summary>
        RelaseManual = 3,
        /// <summary>
        /// Auto contorl hsa been requested form the dashboard?
        /// TODO: is this neded
        /// </summary>
        RequestAuto = 4,
        /// <summary>
        /// Auto contorl mode, default status
        /// </summary>
        Auto = 5,
        /// <summary>
        /// Auto contorl mode is requesting to be released form the contorl thrad.
        /// TODO: is this needed?
        /// </summary>
        ReleaseAuto = 6,
        /// <summary>
        /// TODO: what is this for?
        /// </summary>
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
        public static RobotStatus @RobotStatus
        {
            get
            {
                return @RobotStatus;
            }
            set
            {
                @RobotStatus = value;
                if (Hardware.Code_running_indicator != null)
                {
                    Hardware.Code_running_indicator.UpdateRuntimeValue((int)value);
                }
            }
        }
        public static BatteryStatus SignalBatteryStatus = BatteryStatus.Unknown;//default for now
        public static BatteryStatus PowerBatteryStatus = BatteryStatus.Unknown;//default
        public static ControlStatus @ControlStatus = ControlStatus.None;
        public static AutoControlState RobotAutoControlState { get; private set; } = AutoControlState.None;
        public static BackgroundWorker ControllerThread;
        public static bool SystemOnline = false;
        private const bool IGNORE_LOW_SIGNAL_BATTERY_ACTION = true;
        private const bool IGNORE_LOW_POWER_BATTERY_ACTION = true;
        private static BatteryStatus LastSignalBattStatus = BatteryStatus.Unknown;
        private static BatteryStatus LastPowerBattStatus = BatteryStatus.Unknown;
        private static int DelayI2CRead = 0;
        private static bool SingleSetBool = false;
        private static bool ReachedWaterLimit = false;
        private static Map WorkArea;
        /// <summary>
        /// Initialize the controller subsystem
        /// </summary>
        /// <returns>true if initialization was sucessfull, false otherwise</returns>
        public static bool InitController()
        {
            //first set the robot status
            @RobotStatus = RobotStatus.Idle;
            //battery
            Hardware.UpdateSignalBattery();
            SignalBatteryStatus = Hardware.UpdateSignalBatteryStatus();
            Hardware.Signal_battery_status_indicator.UpdateRuntimeValue((int)SignalBatteryStatus);
            Hardware.UpdatePowerBattery();
            PowerBatteryStatus = Hardware.UpdatePowerBatteryStatus();
            Hardware.Power_battery_status_indicator.UpdateRuntimeValue((int)PowerBatteryStatus);
            //and start them
            Hardware.Power_battery_status_indicator.Start();
            Hardware.Signal_battery_status_indicator.Start();
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
            if (ControlStatus != ControlStatus.Manual)
                ControlStatus = ControlStatus.Manual;
            NetworkUtils.LogNetwork("Manual control method starting", MessageType.Debug);
            while (true)
            {
                if(ControllerThread.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                //if there is no network connection
                if(!NetworkUtils.ConnectionLive)
                {
                    //stop moving
                    Hardware.LeftDrive.SetActiveDutyCyclePercentage(0.5F);
                    Hardware.RightDrive.SetActiveDutyCyclePercentage(0.5F);
                    Hardware.Auger_pin.Write(GpioPinValue.High);
                    Hardware.Impeller_pin.Write(GpioPinValue.High);
                    System.Threading.Thread.Sleep(20);
                    continue;
                }
                //update sensor values
                //battery
                Hardware.UpdateSignalBattery();
                SignalBatteryStatus = Hardware.UpdateSignalBatteryStatus();
                Hardware.Signal_battery_status_indicator.UpdateRuntimeValue((int)SignalBatteryStatus);
                Hardware.UpdatePowerBattery();
                PowerBatteryStatus = Hardware.UpdatePowerBatteryStatus();
                Hardware.Power_battery_status_indicator.UpdateRuntimeValue((int)PowerBatteryStatus);
                //GPIO
                Hardware.UpdateGPIOValues();
                //I2C
                Hardware.UpdateI2CData(2);
                //SPI
                Hardware.UpdateSPIData();

                //parse the write the commands
                string[] commands = NetworkUtils.ManualControlCommands.Split(',');
                //left (float), right (float), motor (bool)
                try
                {
                    Hardware.LeftDrive.SetActiveDutyCyclePercentage(float.Parse(commands[0]));
                    Hardware.RightDrive.SetActiveDutyCyclePercentage(float.Parse(commands[1]));
                    Hardware.Auger_pin.Write(bool.Parse(commands[2]) ? GpioPinValue.Low : GpioPinValue.High);
                    Hardware.Impeller_pin.Write(bool.Parse(commands[2]) ? GpioPinValue.Low : GpioPinValue.High);
                }
                catch
                {
                    Hardware.LeftDrive.SetActiveDutyCyclePercentage(0.5F);
                    Hardware.RightDrive.SetActiveDutyCyclePercentage(0.5F);
                    Hardware.Auger_pin.Write(GpioPinValue.High);
                    Hardware.Impeller_pin.Write(GpioPinValue.High);
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
            if (ControlStatus != ControlStatus.Auto)
                ControlStatus = ControlStatus.Auto;
            RobotAutoControlState = AutoControlState.None;
            while (true)
            {
                //check for cancel/abort
                if (ControllerThread.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                //update sensor values
                //battery
                Hardware.UpdateSignalBattery();
                SignalBatteryStatus = Hardware.UpdateSignalBatteryStatus();
                Hardware.Signal_battery_status_indicator.UpdateRuntimeValue((int)SignalBatteryStatus);
                Hardware.UpdatePowerBattery();
                PowerBatteryStatus = Hardware.UpdatePowerBatteryStatus();
                Hardware.Power_battery_status_indicator.UpdateRuntimeValue((int)PowerBatteryStatus);
                //GPIO
                Hardware.UpdateGPIOValues();
                //I2C
                if (DelayI2CRead >= 2)
                {
                    Hardware.UpdateI2CData(2);
                    DelayI2CRead = -1;
                }
                DelayI2CRead++;
                //SPI
                Hardware.UpdateSPIData();

                //process battery conditions
                if(!IGNORE_LOW_SIGNAL_BATTERY_ACTION)
                {
                    switch(SignalBatteryStatus)
                    {
                        case BatteryStatus.Below5Shutdown:
                            if(LastSignalBattStatus != SignalBatteryStatus)
                            {
                                LastSignalBattStatus = SignalBatteryStatus;
                                NetworkUtils.LogNetwork("Robot signal voltage has reached critical level and must shut down, Setting shutdown for 60s", MessageType.Error);
                                EmergencyShutdown(TimeSpan.FromSeconds(60));
                            }
                            break;
                        case BatteryStatus.Below15Warning:
                            if(LastSignalBattStatus != SignalBatteryStatus)
                            {
                                LastSignalBattStatus = SignalBatteryStatus;
                                NetworkUtils.LogNetwork("Robot signal voltage has reached warning level, needs to go back to base", MessageType.Warning);
                            }
                            break;
                    }
                }
                if(!IGNORE_LOW_POWER_BATTERY_ACTION)
                {
                    if(LastPowerBattStatus != PowerBatteryStatus)
                    {
                        LastPowerBattStatus = PowerBatteryStatus;
                        switch(PowerBatteryStatus)
                        {
                            case BatteryStatus.Below5Shutdown:
                                NetworkUtils.LogNetwork("Robot power voltage has reached critical level, needs to go back to base", MessageType.Warning);
                                break;
                            case BatteryStatus.Below15Warning:
                                NetworkUtils.LogNetwork("Robot power voltage has reached warning level, needs to go back to base", MessageType.Warning);
                                break;
                        }
                    }
                }

                //check water level
                if(Hardware.WaterLevel > 1.5F && !ReachedWaterLimit)//ignore water leverl for now
                {
                    NetworkUtils.LogNetwork("Robot water level is at level, needs to dump", MessageType.Warning);
                    RobotAutoControlState = AutoControlState.OnWaterLimit;
                    ReachedWaterLimit = true;//will be set to false later
                }

                switch (RobotAutoControlState)
                {
                    case AutoControlState.None:
                        //getting into here means that the robot has not started, has good batteries, and is not water level full
                        Hardware.SideReciever.Start();
                        Hardware.FrontReciever.Start();
                        RobotAutoControlState = AutoControlState.TurnToMap;
                        if (WorkArea == null)
                            WorkArea = new Map();
                        SingleSetBool = false;
                        break;
                    case AutoControlState.TurnToMap:
                        //turn to right to get to first laser reading
                        //move encoders specific ammount
                        if(Hardware.SideReciever.WallDetected)//it makes it to the wall
                        {
                            NetworkUtils.LogNetwork("Robot has found wall, moving to map", MessageType.Info);
                            Hardware.LeftEncoder.ResetCounter();
                            Hardware.RightEncoder.ResetCounter();
                            Hardware.RightDrive.SetActiveDutyCyclePercentage(0.5);
                            Hardware.LeftDrive.SetActiveDutyCyclePercentage(0.5);
                            Hardware.SideReciever.ResetDetection();
                            RobotAutoControlState = AutoControlState.MapOneSide;
                            SingleSetBool = false;
                        }
                        else if (!SingleSetBool)
                        {
                            Hardware.RightDrive.SetActiveDutyCyclePercentage(0.5);
                            Hardware.LeftDrive.SetActiveDutyCyclePercentage(0.6);
                            SingleSetBool = true;
                        }
                        break;

                    case AutoControlState.MapOneSide:
                        //if front IR sensor
                        //  save counter rotations
                        //  move to turn to map phase
                        //if wall IR sensor
                        //  turn x rotations
                        //else if not wall ir sensor and counter for turning
                        //  set back to slow right turn
                        if(Hardware.FrontReciever.WallDetected)
                        {
                            NetworkUtils.LogNetwork("Front wall detected, saving average of position and encoder data", MessageType.Info);
                            Hardware.RightEncoder.CorrectTicks();
                            float encoder_height = Hardware.RightEncoder.Counter;
                            //convert it to a normalized distance
                            float MPU_height = Hardware.PositionX;
                            //convert it to a normalized distance
                            //average it
                            RobotAutoControlState = AutoControlState.MapTurn;
                            SingleSetBool = false;
                        }
                        else if (Hardware.SideReciever.WallDetected)
                        {
                            //reset left drive ticks
                            if (!SingleSetBool)
                            {
                                Hardware.LeftEncoder.ResetCounter();
                                Hardware.RightDrive.SetActiveDutyCyclePercentage(0.5);
                                Hardware.LeftDrive.SetActiveDutyCyclePercentage(0.4);
                                SingleSetBool = true;
                            }
                            else if(Hardware.LeftEncoder.Counter >= 5)
                            {
                                //reset the side reciever to let it continue
                                Hardware.SideReciever.ResetDetection();
                                SingleSetBool = false;
                            }
                        }
                        break;
                    case AutoControlState.MapTurn:
                        if (!SingleSetBool)
                        {
                            Hardware.LeftEncoder.ResetCounter();
                            Hardware.RightDrive.SetActiveDutyCyclePercentage(0.5);
                            Hardware.LeftDrive.SetActiveDutyCyclePercentage(0.4);
                            SingleSetBool = true;
                        }
                        else if (Hardware.LeftEncoder.Counter >= 5)
                        {
                            //reset the side reciever to let it continue
                            Hardware.SideReciever.ResetDetection();
                            Hardware.FrontReciever.ResetDetection();
                            //DEBUG:TEMP SET BACK TO MAKE A SQUARE
                            RobotAutoControlState = AutoControlState.MapOneSide;
                            SingleSetBool = false;
                        }
                        break;
                    case AutoControlState.OnLowPowerBattery:

                        break;
                    case AutoControlState.OnLowSignalBattery:

                        break;
                    case AutoControlState.OnWaterLimit:

                        break;
                    case AutoControlState.OnObstructionWhenCleaning:
                        
                        break;
                    case AutoControlState.OnObstuctionWhenMapping:

                        break;
                }
                System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(5));
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
            MessageType messageType = (MessageType)e.ProgressPercentage;
            NetworkUtils.LogNetwork(message, messageType);
        }
        /// <summary>
        /// Event method reasied when The control thread is exited. If cancled or error it will restart from this method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnControlThreadExit(object sender, RunWorkerCompletedEventArgs e)
        {
            NetworkUtils.LogNetwork("Control thread is down, determining what to do next", MessageType.Debug);
            //NOTE: this is on the UI thread
            if (e.Cancelled || e.Error != null)
            {
                if (e.Error != null)
                {
                    NetworkUtils.LogNetwork(e.Error.ToString(), MessageType.Error);
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
                switch (ControlStatus)
                {
                    case ControlStatus.RequestManual:
                        NetworkUtils.LogNetwork("Manual Control has been requested, sending ack and enabling manual control",MessageType.Debug);
                        NetworkUtils.LogNetwork("request_ack", MessageType.Control);
                        ControllerThread.DoWork += ControlRobotManual;
                        ControllerThread.RunWorkerAsync();
                        break;
                    case ControlStatus.Manual:
                        NetworkUtils.LogNetwork("Resuming manual control", MessageType.Debug);
                        ControllerThread.DoWork += ControlRobotManual;
                        ControllerThread.RunWorkerAsync();
                        break;
                    case ControlStatus.RelaseManual:
                        NetworkUtils.LogNetwork("Releasing manual control, restarting auto", MessageType.Debug);
                        NetworkUtils.LogNetwork("release_ack", MessageType.Control);
                        ControllerThread.DoWork += ControlRobotAuto;
                        ControllerThread.RunWorkerAsync();
                        break;
                }
            }
            else
            {
                NetworkUtils.LogNetwork("The controller thread has ended, is the application unloading?", MessageType.Debug);
                return;
            }
        }
        /// <summary>
        /// Stops all motor activity incase of emergency and sets the robot state.
        /// </summary>
        public static void EmergencyStop()
        {
            //everything must stop, a critical exception has occured
            RobotStatus = RobotStatus.Error;
            //shut off any relays on
            if(Hardware.GpioController != null && Hardware.Auger_pin != null)
                Hardware.Auger_pin.Write(GpioPinValue.High);
            if (Hardware.GpioController != null && Hardware.Impeller_pin != null)
                Hardware.Impeller_pin.Write(GpioPinValue.High);
            //shut off any PWM systems
            if (Hardware.driveControl != null)
            {
                if (Hardware.LeftDrive != null)
                    Hardware.LeftDrive.Stop();
                if (Hardware.RightDrive != null)
                    Hardware.RightDrive.Stop();
            }
        }
        /// <summary>
        /// Usually when the signal (RPi) battery is in a critical state. Shuts down the unit in the event of a critical
        /// system failure and sets the robot state.
        /// </summary>
        public static void EmergencyShutdown(TimeSpan delay)
        {
            NetworkUtils.LogNetwork(string.Format("CRITICAL SYSTEM MESSAGE: The device is shutting down in {0} seconds, use" +
                " ShutdownManager.CancelShutdown() to cancel", delay.TotalSeconds),MessageType.Warning);
            //completly shut down the robot
            ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, delay);
        }
        /// <summary>
        /// Requests a reboot of the system
        /// </summary>
        /// <param name="delay">The delay before the reboot takes place</param>
        public static void Reboot(TimeSpan delay)
        {
            NetworkUtils.LogNetwork(string.Format("Rebooting in {0} seconds", delay.TotalSeconds), MessageType.Warning);
            ShutdownManager.BeginShutdown(ShutdownKind.Restart, delay);
        }
        /// <summary>
        /// Reauests a poweroff of the system
        /// </summary>
        /// <param name="delay">The dealy before the poweroff takes place</param>
        public static void Poweroff(TimeSpan delay)
        {
            NetworkUtils.LogNetwork(string.Format("Shutting down in {0} seconds", delay.TotalSeconds), MessageType.Warning);
            ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, delay);
        }
        /// <summary>
        /// Cancels a pending shutdown or reboot, if one exists
        /// </summary>
        public static void CancelShutdown()
        {
            NetworkUtils.LogNetwork("Canceling shutdown/reboot", MessageType.Warning);
            ShutdownManager.CancelShutdown();
        }
    }
}
