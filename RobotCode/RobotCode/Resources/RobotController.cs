using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace RobotCode
{
    /// <summary>
    /// The Class responsible for controlling the robot
    /// </summary>
    public static class RobotController
    {
        private static BackgroundWorker ControllerThread;
        public static void InitController()
        {
            ControllerThread = new BackgroundWorker()
            {
                WorkerSupportsCancellation = false,
                WorkerReportsProgress = true
            };
            ControllerThread.RunWorkerCompleted += OnWorkCompleted;
            ControllerThread.ProgressChanged += ControllerLogProgress;
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
            GPIO.RobotStatus = RobotStatus.Exception;
            //shut off any relays on
            GPIO.Pins[3].Write(Windows.Devices.Gpio.GpioPinValue.Low);
            //shut off any PWM systems

        }
    }
}
