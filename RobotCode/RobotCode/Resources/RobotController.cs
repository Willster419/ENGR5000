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
using Windows.UI.Core;

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
        /// The Side IR has detected the side wall while moving in mapping motion, wait until left encoder has moved back x value
        /// </summary>
        MapSideBackup,
        /// <summary>
        /// Performing calculations to create the map and send the XML map data to the dashboard
        /// </summary>
        Calculations,
        /// <summary>
        /// It has finished mapping, is back at start, and will now turn left to start cleaning of first line
        /// </summary>
        TurnToClean,
        /// <summary>
        /// Cleaning the area, in the up side
        /// </summary>
        CleanUp,
        /// <summary>
        /// Cleaning the area, in the down side
        /// </summary>
        CleanDown,
        /// <summary>
        /// Turning to clean the next line
        /// </summary>
        CleanTurn,
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
        private static RobotStatus _RobotStatus = RobotStatus.Idle;
        private static object _robotlocker = new object();
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
        private static int Manual_counter_1 = 0;
        private static int Manual_counter_2 = 0;
        private static int left_ir_count_reset = 0;
        private static int right_ir_count_reset = 0;
        public static CoreDispatcher SystemDispatcher;
        private static int NumSidesMapped = 0;
        //speed constants
        private const float SLOW_LEFT_FORWARD_MAP = 0.65F;
        private const float SLOW_RIGHT_FORWARD_MAP = 0.6F;
        private const float SLOW_FORWARD = 0.6F;
        private const float SLOW_REVERSE = 0.35F;
        private const float ALL_STOP = 0.5F;
        private static int num_lanes = 0;
        private const int ROBOT_TICKS_WIDTH = 70;
        //encoder constsnts
        private const int SIDE_WALL_CORRECTION = -5;
        private const int MAP_TURN_LEFT = -40;
        /// <summary>
        /// Initialize the controller subsystem
        /// </summary>
        /// <returns>true if initialization was sucessfull, false otherwise</returns>
        public static bool InitController(CoreDispatcher uiDispatcher)
        {
            SystemDispatcher = uiDispatcher;
            //first set the robot status
            _RobotStatus = RobotStatus.Idle;
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
            if (!Hardware.SideReciever.Enabled)
            {
                NetworkUtils.LogNetwork("Starting side reciever", MessageType.Debug);
                Hardware.SideReciever.Start();
            }
            if(!Hardware.FrontReciever.Enabled)
            {
                NetworkUtils.LogNetwork("Starting front reciever", MessageType.Debug);
                Hardware.FrontReciever.Start();
            }
            Manual_counter_1 = Manual_counter_2 = left_ir_count_reset = right_ir_count_reset = 0;
            Hardware.ResetI2CData(true, true);
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
                    Hardware.LeftDrive.SetActiveDutyCyclePercentage(ALL_STOP);
                    Hardware.RightDrive.SetActiveDutyCyclePercentage(ALL_STOP);
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
                Hardware.UpdateI2CData(0, 1);
                //SPI
                Hardware.UpdateSPIData();
                //IR
                if(left_ir_count_reset++ > 5)
                {
                    Hardware.SideReciever.ClearDetectionBuffer();
                    left_ir_count_reset = 0;
                }
                if(right_ir_count_reset++ > 5)
                {
                    Hardware.FrontReciever.ClearDetectionBuffer();
                    right_ir_count_reset = 0;
                }
                if(Hardware.SideReciever.WallDetected && Manual_counter_1++ > 40)
                {
                    Hardware.SideReciever.ResetDetection();
                    Manual_counter_1 = 0;
                }
                if(Hardware.FrontReciever.WallDetected && Manual_counter_2++ > 40)
                {
                    Hardware.FrontReciever.ResetDetection();
                    Manual_counter_2 = 0;
                }

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
                    Hardware.LeftDrive.SetActiveDutyCyclePercentage(ALL_STOP);
                    Hardware.RightDrive.SetActiveDutyCyclePercentage(ALL_STOP);
                    Hardware.Auger_pin.Write(GpioPinValue.High);
                    Hardware.Impeller_pin.Write(GpioPinValue.High);
                }
                System.Threading.Thread.Sleep(25);
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
            Hardware.ResetI2CData(true, true);
            Hardware.RightEncoder.ResetCounter();
            Hardware.LeftEncoder.ResetCounter();
            Manual_counter_1 = Manual_counter_2 = left_ir_count_reset = right_ir_count_reset = 0;
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
                    Hardware.UpdateI2CData(0, 1);
                    DelayI2CRead = -1;
                }
                DelayI2CRead++;
                //SPI
                Hardware.UpdateSPIData();
                //IR
                if (left_ir_count_reset++ > 25)
                {
                    Hardware.SideReciever.ClearDetectionBuffer();
                    left_ir_count_reset = 0;
                }
                if (right_ir_count_reset++ > 25)
                {
                    Hardware.FrontReciever.ClearDetectionBuffer();
                    right_ir_count_reset = 0;
                }
                //process battery conditions
                if (!IGNORE_LOW_SIGNAL_BATTERY_ACTION)
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
                //Hardware.WaterLevel > 1.5F && !ReachedWaterLimit
                if (false)//ignore water leverl for now
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
                        NumSidesMapped = 0;
                        NetworkUtils.LogNetwork("On robot init, autocontrol state none->TurnToMap", MessageType.Info);
                        SingleSetBool = false;
                        break;
                    case AutoControlState.TurnToMap:
                        //turn to right to get to first laser reading
                        //move encoders specific ammount
                        if(Hardware.SideReciever.WallDetected)//it makes it to the wall
                        {
                            if (SingleSetBool)
                            {
                                NetworkUtils.LogNetwork("Robot has found side wall, TurnToMap->MapSideBackup", MessageType.Info);
                                Hardware.LeftEncoder.ResetCounter();
                                Hardware.RightEncoder.ResetCounter();
                                Hardware.RightDrive.SetActiveDutyCyclePercentage(ALL_STOP);
                                Hardware.LeftDrive.SetActiveDutyCyclePercentage(ALL_STOP);
                                RobotAutoControlState = AutoControlState.MapSideBackup;
                                SingleSetBool = false;
                            }
                            
                        }
                        else if (!SingleSetBool)
                        {
                            Hardware.RightDrive.SetActiveDutyCyclePercentage(SLOW_REVERSE);
                            Hardware.LeftDrive.SetActiveDutyCyclePercentage(SLOW_LEFT_FORWARD_MAP);
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
                            NetworkUtils.LogNetwork("Front wall detected, MapOneSide->MapTurn", MessageType.Info);
                            RobotAutoControlState = AutoControlState.MapTurn;
                            SingleSetBool = false;
                        }
                        else if (Hardware.SideReciever.WallDetected)
                        {
                            NetworkUtils.LogNetwork("Side IR detected wall, need to turn small left, MapOneSide->MapSideBackup", MessageType.Info);
                            Hardware.LeftEncoder.ResetCounter();
                            RobotAutoControlState = AutoControlState.MapSideBackup;
                            SingleSetBool = false;
                        }
                        else if (!SingleSetBool)
                        {
                            Hardware.LeftDrive.SetActiveDutyCyclePercentage(SLOW_LEFT_FORWARD_MAP);
                            Hardware.RightDrive.SetActiveDutyCyclePercentage(SLOW_RIGHT_FORWARD_MAP);
                            SingleSetBool = true;
                        }
                        break;
                    case AutoControlState.MapSideBackup:
                        if(!SingleSetBool)
                        {
                            Hardware.RightDrive.SetActiveDutyCyclePercentage(ALL_STOP);
                            Hardware.LeftDrive.SetActiveDutyCyclePercentage(SLOW_REVERSE);
                            SingleSetBool = true;
                        }
                        else if (Hardware.LeftEncoder.Clicks <= SIDE_WALL_CORRECTION)
                        {
                            //go back to a slow turn to the right
                            NetworkUtils.LogNetwork("Left encoder has backed up enough, moving to slow right movement, MapSideBackup->MapOneSide", MessageType.Info);
                            Hardware.LeftEncoder.ResetCounter();
                            Hardware.SideReciever.ResetDetection();
                            Hardware.LeftDrive.SetActiveDutyCyclePercentage(ALL_STOP);
                            Hardware.RightDrive.SetActiveDutyCyclePercentage(ALL_STOP);
                            RobotAutoControlState = AutoControlState.MapOneSide;
                            SingleSetBool = false;
                        }
                        break;
                    case AutoControlState.MapTurn:
                        if (!SingleSetBool)
                        {
                            float encoder_value = Hardware.RightEncoder.Clicks;
                            Hardware.RightEncoder.ResetCounter();
                            //convert it to a normalized distance
                            float MPU_height = Hardware.PositionX;
                            //convert it to a normalized distance
                            //average it
                            Hardware.LeftEncoder.ResetCounter();
                            Hardware.RightDrive.SetActiveDutyCyclePercentage(SLOW_FORWARD);
                            Hardware.LeftDrive.SetActiveDutyCyclePercentage(SLOW_REVERSE);
                            switch(NumSidesMapped)
                            {
                                case 0:
                                    NetworkUtils.LogNetwork(string.Format("Side {0} mapped, using width encoder value of {1}, not averaging", NumSidesMapped, encoder_value), MessageType.Info);
                                    WorkArea.SetHeight(encoder_value, false);
                                    break;
                                case 1:
                                    NetworkUtils.LogNetwork(string.Format("Side {0} mapped, using height encoder value of {1}, not averaging", NumSidesMapped, encoder_value), MessageType.Info);
                                    WorkArea.SetWidth(encoder_value, false);
                                    break;
                                case 2:
                                    NetworkUtils.LogNetwork(string.Format("Side {0} mapped, using second width encoder value of {1}, averaging", NumSidesMapped, encoder_value), MessageType.Info);
                                    WorkArea.SetHeight(encoder_value, true);
                                    break;
                                case 3:
                                    NetworkUtils.LogNetwork(string.Format("Side {0} mapped, using second height encoder value of {1}, averaging", NumSidesMapped, encoder_value), MessageType.Info);
                                    WorkArea.SetWidth(encoder_value, true);
                                    break;
                                default:
                                    NetworkUtils.LogNetwork(string.Format("Sides value of {0} should not happen", NumSidesMapped), MessageType.Error);
                                    break;
                            }
                            NumSidesMapped++;
                            if(NumSidesMapped > 3)
                            {
                                NetworkUtils.LogNetwork("Mapping finished, moving to calculations, MapTurn->Calculations", MessageType.Info);
                                RobotAutoControlState = AutoControlState.Calculations;
                                SingleSetBool = false;
                            }
                            else
                            {
                                SingleSetBool = true;
                            }
                        }
                        else if (Hardware.LeftEncoder.Clicks <= MAP_TURN_LEFT)
                        {
                            NetworkUtils.LogNetwork("Left encoder has backed up enough for conter, moving to slow right movement on new wall, MapTurn->MapOneSide", MessageType.Info);
                            //reset the side reciever to let it continue
                            Hardware.SideReciever.ResetDetection();
                            Hardware.FrontReciever.ResetDetection();
                            //DEBUG:TEMP SET BACK TO MAKE A SQUARE
                            RobotAutoControlState = AutoControlState.MapOneSide;
                            SingleSetBool = false;
                        }
                        break;
                    case AutoControlState.Calculations:
                        if(!SingleSetBool)
                        {
                            Hardware.RightDrive.SetActiveDutyCyclePercentage(ALL_STOP);
                            Hardware.LeftDrive.SetActiveDutyCyclePercentage(ALL_STOP);
                            //set robot location
                            WorkArea.SetRobotLocation(0F, 0F);
                            NetworkUtils.LogNetwork("Sending mapping data...", MessageType.Info);
                            NetworkUtils.LogNetwork(WorkArea.XMLMap, MessageType.Mapping);
                            RobotAutoControlState = AutoControlState.TurnToClean;
                            NetworkUtils.LogNetwork("Finish calculations, Calculcations->TurnToClean",MessageType.Info);
                            SingleSetBool = false;
                        }
                        break;
                    case AutoControlState.TurnToClean:
                        if(!SingleSetBool)
                        {
                            Hardware.RightEncoder.ResetCounter();
                            Hardware.ResetI2CData(true, true);
                            Hardware.RightDrive.SetActiveDutyCyclePercentage(SLOW_RIGHT_FORWARD_MAP);
                            Hardware.LeftDrive.SetActiveDutyCyclePercentage(ALL_STOP);
                            //you would set the cleaning relays on here
                            SingleSetBool = true;
                        }
                        if(Hardware.RightEncoder.Clicks >=50 || Hardware.RotationX >= 800)
                        {
                            NetworkUtils.LogNetwork(string.Format("right encoder clicks={0}, rotationX={1}, TurnToClean->CleanUp", Hardware.RightEncoder.Clicks, Hardware.RotationX), MessageType.Info);
                            RobotAutoControlState = AutoControlState.CleanUp;
                            SingleSetBool = false;
                        }
                        break;
                    case AutoControlState.CleanUp:
                        if(!SingleSetBool)
                        {
                            Hardware.RightDrive.SetActiveDutyCyclePercentage(ALL_STOP);
                            Hardware.LeftDrive.SetActiveDutyCyclePercentage(ALL_STOP);
                            SingleSetBool = true;
                        }
                        break;
                    case AutoControlState.CleanTurn:

                        break;
                    case AutoControlState.CleanDown:

                        break;
                    case AutoControlState.CleanComplete:

                        break;
                    case AutoControlState.OnLowPowerBattery:

                        break;
                    case AutoControlState.OnLowSignalBattery:

                        break;
                    case AutoControlState.OnWaterLimit:

                        break;
                    case AutoControlState.OnObstructionWhenCleaning:
                        //don't have the hardware to do this...
                        break;
                    case AutoControlState.OnObstuctionWhenMapping:
                        //don't have the hardware to do this...
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
            _RobotStatus = RobotStatus.Error;
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
            try
            {
                ShutdownManager.CancelShutdown();
                NetworkUtils.LogNetwork("Shutdown/reboot canceled", MessageType.Warning);
            }
            catch
            {
                NetworkUtils.LogNetwork("Failed to cancel shutdown/reboot: no command exists", MessageType.Warning);
            }
        }
        /// <summary>
        /// Sets the robot status, as well as updates the value of the status LED indicator
        /// </summary>
        /// <param name="status">The new robot status value</param>
        public static void SetRobotStatus(RobotStatus status)
        {
            _RobotStatus = status;
            if (Hardware.Code_running_indicator != null)
                Hardware.Code_running_indicator.UpdateRuntimeValue((int)status);
        }
        /// <summary>
        /// Get the Robot Status
        /// </summary>
        /// <returns>The robot status</returns>
        public static RobotStatus GetRobotStatus()
        { return _RobotStatus; }
    }
}
