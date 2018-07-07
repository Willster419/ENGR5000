using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SharpDX;
using SharpDX.DirectInput;
using System.Windows.Threading;


namespace Dashboard.Resources
{
    public static class Control
    {
        public static DirectInput @DirectInput;
        private static Guid JoystickGUID = Guid.Empty;
        private static Joystick @Joystick;
        public static DispatcherTimer ControlTimer;
        private static MainWindow MainWindowInstance;

        private static float LeftDrive = 0.0F;
        private static float RightDrive = 0.0F;
        private static bool Motor = false;

        private const float MAX_AXIS_VALUE = 65535;
        private const float MIDDLE_AXIS_VALUE = MAX_AXIS_VALUE / 2;
        private const float AXIS_DEADZONE = 1000;
        private const float MAX_DEADZONE_AXIS_VALUE = MAX_AXIS_VALUE - AXIS_DEADZONE;
        private const float MIDDLE_DEADZONE_AXIS_VALUE = MAX_DEADZONE_AXIS_VALUE / 2;

        public static void StartControl(MainWindow mw)
        {
            if (MainWindowInstance == null)
                MainWindowInstance = mw;
            if (ControlTimer == null)
            {
                ControlTimer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(50),
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
            if(Joystick != null)
            {
                //send values from joystick
                Joystick.Poll();
                JoystickUpdate[] updates = Joystick.GetBufferedData();
                float xValue = 0;
                float yValue = 0;
                foreach(JoystickUpdate update in updates)
                {
                    switch (update.Offset)
                    {
                        case JoystickOffset.X:
                            xValue = update.Value;
                            break;
                        case JoystickOffset.Y:
                            yValue = update.Value;
                            break;
                        case JoystickOffset.Buttons0:
                            Motor = update.Value == 128? true: false;
                            break;
                    }
                }
                //now have latest motor and drive values
                //scale the x and y values
                xValue = xValue / MAX_AXIS_VALUE;
                yValue = yValue / MAX_AXIS_VALUE;

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
            if (!NetworkUtils.SendRobotMesage(NetworkUtils.MessageType.Control, string.Join(",", controlMessage)))
            {
                Logging.LogConsole("Failed to send control data, stopping");
                StopControl();
            }
        }
        public static void EnableManualJoystickControl(DeviceInstance di)
        {
            //https://stackoverflow.com/questions/3929764/taking-input-from-a-joystick-with-c-sharp-net
            //https://github.com/sharpdx/SharpDX-Samples/blob/master/Desktop/DirectInput/JoystickApp/Program.cs
            //http://sharpdx.org/wiki/class-library-api/directinput/
            Joystick = new Joystick(DirectInput, di.InstanceGuid);
            Joystick.Properties.BufferSize = 128;
            Joystick.Properties.AxisMode = DeviceAxisMode.Absolute;
            Joystick.Properties.DeadZone = 1500;
            Joystick.Acquire();
            Logging.LogConsole("Joystick initaliized");
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
    }
}
