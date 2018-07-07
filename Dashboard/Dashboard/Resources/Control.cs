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

        public static void StartControl(MainWindow mw)
        {
            if (MainWindowInstance == null)
                MainWindowInstance = mw;
            if (ControlTimer == null)
            {
                ControlTimer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(100),
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
            if(false)
            {
                //send values from joystick

            }
            else if (MainWindowInstance.KeyboardControl.IsFocused)
            {
                //send values from keyboard
                
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
