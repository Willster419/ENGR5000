using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SharpDX;
using SharpDX.DirectInput;
using System.Windows.Threading;
using System.ComponentModel;


namespace Dashboard.Resources
{
    public static class ControlSystem
    {
        public static DirectInput @DirectInput;
        private static Guid JoystickGUID = Guid.Empty;
        private static Joystick @Joystick;
        public static DispatcherTimer ControlTimer;
        private static BackgroundWorker joystickWorker;
        private static MainWindow MainWindowInstance;
        public static List<DeviceInstance> DeviceInstances = new List<DeviceInstance>();

        private static float LeftDrive = 0.0F;
        private static float RightDrive = 0.0F;
        private static bool Motor = false;

        private static float JoystickXValue = 0;
        private static float JoystickYValue = 0;
        private static bool JoystickMotor = false;
        public static bool FirstJoystickMoveMent = true;
        private static volatile int joystickIndex = -1;
        public static volatile bool joystickDriveneable = false;

        private const float MAX_AXIS_VALUE = 65535;
        private const float MIDDLE_AXIS_VALUE = MAX_AXIS_VALUE / 2;
        private const float AXIS_DEADZONE = 1000;
        private const float MAX_DEADZONE_AXIS_VALUE = MAX_AXIS_VALUE - AXIS_DEADZONE;
        private const float MIDDLE_DEADZONE_AXIS_VALUE = MAX_DEADZONE_AXIS_VALUE / 2;

        private static int NetworkSendTimer = 0;

        public static void InitManualJoystickControl(MainWindow mw)
        {
            if (MainWindowInstance == null)
                MainWindowInstance = mw;
            if(joystickWorker == null)
            {
                joystickWorker = new BackgroundWorker()
                {
                    WorkerReportsProgress = false,//operations are atomic, so don't need to worry about threading on the ints
                    WorkerSupportsCancellation = true
                };
                joystickWorker.DoWork += GetJoystickValues;
                joystickWorker.RunWorkerCompleted += OnWorkerStop;
            }
            Logging.LogConsole("Getting all joystick instances");
            if (DirectInput == null)
                DirectInput = new DirectInput();
            //get all valid instances
            DeviceInstances.Clear();
            DeviceInstances = (List<DeviceInstance>)DirectInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices);
            foreach (DeviceInstance deviceInstance in DeviceInstances)
            {
                MainWindowInstance.Joysticks.Items.Add(deviceInstance.ProductName);
                Logging.LogConsole("Found joystick instance: " + deviceInstance.ProductName);
            }
        }

        private static void OnWorkerStop(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Cancelled)
            {
                Logging.LogConsole("Joystick thread was stopped", true);
            }
            Joystick.Dispose();
            Joystick = null;
        }

        private static void GetJoystickValues(object sender, DoWorkEventArgs e)
        {
            if (System.Threading.Thread.CurrentThread.Priority != System.Threading.ThreadPriority.BelowNormal)
            {
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.BelowNormal;
            }
            //https://stackoverflow.com/questions/3929764/taking-input-from-a-joystick-with-c-sharp-net
            //https://github.com/sharpdx/SharpDX-Samples/blob/master/Desktop/DirectInput/JoystickApp/Program.cs
            //http://sharpdx.org/wiki/class-library-api/directinput/

            Joystick = new Joystick(DirectInput, DeviceInstances[joystickIndex].InstanceGuid);
            Joystick.Properties.BufferSize = 128;
            Joystick.Properties.AxisMode = DeviceAxisMode.Absolute;
            Joystick.Properties.DeadZone = 1500;
            Joystick.Acquire();
            while (true)
            {
                if (joystickWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                if (Joystick != null && joystickDriveneable)
                {
                    //send values from joystick
                    Joystick.Poll();
                    JoystickUpdate[] updates = Joystick.GetBufferedData();
                    float xValue = 0;
                    float yValue = 0;
                    bool buttonTemp = false;
                    //reverse the list to get the latest version of each
                    //updates.Reverse();
                    bool xUpdated = false;
                    bool yUpdated = false;
                    bool buttonUpdated = false;
                    if (FirstJoystickMoveMent)
                    {
                        xUpdated = true;
                        yUpdated = true;
                        buttonUpdated = true;
                        xValue = 0.5F;
                        yValue = 0.5F;
                        buttonTemp = false;
                        FirstJoystickMoveMent = false;
                    }
                    foreach (JoystickUpdate update in updates)
                    {
                        //scale the x and y values to between 0 and 1
                        if (!xUpdated && update.Offset == JoystickOffset.X)
                        {
                            xValue = update.Value / MAX_AXIS_VALUE;
                            xUpdated = true;
                        }
                        else if (!yUpdated && update.Offset == JoystickOffset.Y)
                        {
                            yValue = update.Value / MAX_AXIS_VALUE;
                            yUpdated = true;
                        }
                        else if (!buttonUpdated && update.Offset == JoystickOffset.Buttons0)
                        {
                            buttonTemp = update.Value == 128 ? true : false;
                            buttonUpdated = true;
                        }
                    }
                    //subtract to make the -0.5 to 0.5, 0 being center
                    if (xUpdated)
                        JoystickXValue = xValue - 0.5F;
                    if (yUpdated)
                        JoystickYValue = yValue - 0.5F;
                    if (buttonUpdated)
                        Motor = buttonTemp;
                    //round them as well to 3 decimal places
                    JoystickXValue = (float)Math.Round(JoystickXValue, 3);
                    JoystickYValue = (float)Math.Round(JoystickYValue, 3);
                }
            }
        }

        public static void EnableManualJoystickControl(int index)
        {
            joystickIndex = index;
            if (joystickWorker != null)
            {
                joystickWorker.RunWorkerAsync();
            }
            else
            {
                Logging.LogConsole("Joystick thread failed to initalize");
            }
            Logging.LogConsole("Joystick initaliized");
        }

        public static void StartControl(MainWindow mw)
        {
            if (MainWindowInstance == null)
                MainWindowInstance = mw;
            if (ControlTimer == null)
            {
                ControlTimer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(5),//5ms polling, 10ms network sending
                    IsEnabled = false
                };
                ControlTimer.Tick += OnControlTimerTick;
            }
            //send init control message
            if(!NetworkUtils.SendRobotMesage(NetworkUtils.MessageType.Control,"Start"))
            {
                Logging.LogConsole("Failed to start manual control");
                return;
            }
            Logging.LogConsole("Manual control Started");
            ControlTimer.Start();
        }

        private static void OnControlTimerTick(object sender, EventArgs e)
        {
            if(Joystick != null && MainWindowInstance != null && (bool)MainWindowInstance.JoystickToggle.IsChecked)
            {
                MainWindowInstance.JoystickXValue.Text = JoystickXValue.ToString();
                MainWindowInstance.JoystickYValue.Text = JoystickYValue.ToString();
                //x value affects the left and right drives directly
                //y value affects the left and rights drives inversly
                LeftDrive = 0.5F;
                RightDrive = 0.5F;
                //y factor
                LeftDrive += (JoystickYValue * -1);
                RightDrive += (JoystickYValue * -1);
                //x factor (lol)
                LeftDrive -= (JoystickXValue * -1);
                RightDrive += (JoystickXValue * -1);
                //just to be safe
                if (LeftDrive > 1.0F)
                    LeftDrive = 1.0F;
                if (RightDrive > 1.0F)
                    RightDrive = 1.0F;
                if (LeftDrive < 0.0F)
                    LeftDrive = 0.0F;
                if (RightDrive < 0.0F)
                    RightDrive = 0.0F;
            }
            else if (MainWindowInstance.KeyboardControl.IsFocused)
            {
                LeftDrive = 0.5F;
                RightDrive = 0.5F;
                //send values from keyboard
                if(System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Up))
                {
                    LeftDrive += 0.2F;
                    RightDrive += 0.2F;
                }
                if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Down))
                {
                    LeftDrive -= 0.2F;
                    RightDrive -= 0.2F;
                }
                if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Left))
                {
                    LeftDrive -= 0.2F;
                    RightDrive += 0.2F;
                }
                if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Right))
                {
                    LeftDrive += 0.2F;
                    RightDrive -= 0.2F;
                }
                //just to be safe
                if (LeftDrive > 1.0F)
                    LeftDrive = 1.0F;
                if (RightDrive > 1.0F)
                    RightDrive = 1.0F;
                if (LeftDrive < 0.0F)
                    LeftDrive = 0.0F;
                if (RightDrive < 0.0F)
                    RightDrive = 0.0F;
                Motor = System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) ? true : false;
            }
            else
            {
                //send values from buttons
                if(MainWindowInstance.DriveUpButton.IsPressed)
                {
                    LeftDrive = 0.7F;
                    RightDrive = 0.7F;
                    Motor = false;
                }
                else if(MainWindowInstance.DriveDownButton.IsPressed)
                {
                    LeftDrive = 0.3F;
                    RightDrive = 0.3F;
                    Motor = false;
                }
                else if(MainWindowInstance.DriveLeftButton.IsPressed)
                {
                    LeftDrive = 0.3F;
                    RightDrive = 0.7F;
                    Motor = false;
                }
                else if(MainWindowInstance.DriveRightButton.IsPressed)
                {
                    LeftDrive = 0.7F;
                    RightDrive = 0.3F;
                    Motor = false;
                }
                else if(MainWindowInstance.DriveMotorButton.IsPressed)
                {
                    LeftDrive = 0.5F;
                    RightDrive = 0.5F;
                    Motor = true;
                }
                else
                {
                    LeftDrive = 0.5F;
                    RightDrive = 0.5F;
                    Motor = false;
                }
            }
            MainWindowInstance.OutputLeft.Text = LeftDrive.ToString();
            MainWindowInstance.OutputRight.Text = RightDrive.ToString();
            MainWindowInstance.OutputMotor.Text = Motor ? "1" : "0";
            //send pwm values and motor value
            string[] controlMessage = new string[]
            {
                LeftDrive.ToString(),
                RightDrive.ToString(),
                Motor.ToString()
            };
            //part 2: send values over network
            NetworkSendTimer++;
            if (NetworkSendTimer >= 2)
            {
                if (!NetworkUtils.SendRobotMesage(NetworkUtils.MessageType.Control, string.Join(",", controlMessage)))
                {
                    Logging.LogConsole("Failed to send control data, stopping");
                    StopControl();
                }
                NetworkSendTimer = 0;
            }
        }
        
        public static void StopControl()
        {
            //stop timer
            ControlTimer.Stop();
            MainWindowInstance.ManualControlToggle.IsChecked = false;
            //send stop signal
            if (!NetworkUtils.SendRobotMesage(NetworkUtils.MessageType.Control, "Stop"))
            {
                Logging.LogConsole("Failed to stop manual control");
                return;
            }
        }

        public static void StopJoystickControl()
        {
            if(!joystickWorker.CancellationPending)
            {
                joystickWorker.CancelAsync();
            }
        }
    }
}
