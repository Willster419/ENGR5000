using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.UI.Core;

namespace RobotCode.Sensors
{
    /// <summary>
    /// Represents a distnace sensor on the robot. Note that the trigger is currently provided by a TI MSP board
    /// </summary>
    public class DistanceSensor
    {
        private GpioPin EchoPin;
        private float Collected_Distance = 0F;
        public float Distance_in_cm { get; private set; } = 0F;
        private float Session_microseconds;
        private float Maximum_distance_width = 20F;
        private Stopwatch distanceTimer;
        //averaging
        private float Avg_Distance = 0;
        private float Total_avg_itterations = 10F;
        private float Current_avg_itteration = 1F;

        public DistanceSensor() { }

        public bool InitSensor(int gpio_pin, GpioController contorller, float max_distance_width, float distance_avg_itterations)
        {
            if (gpio_pin < 0)
                return false;
            if (contorller == null)
                return false;
            EchoPin = contorller.OpenPin(gpio_pin);
            if (EchoPin == null)
                return false;
            EchoPin.Write(GpioPinValue.Low);
            EchoPin.SetDriveMode(GpioPinDriveMode.Input);
            EchoPin.ValueChanged += OnEchoResponse;
            distanceTimer = new Stopwatch();
            distanceTimer.Reset();
            Maximum_distance_width = max_distance_width;
            Total_avg_itterations = distance_avg_itterations;
            return true;
        }

        private void OnEchoResponse(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            var task = RobotController.SystemDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                switch (args.Edge)
                {
                    case GpioPinEdge.FallingEdge:
                        //session_microseconds = distanceTimer.ElapsedTicks / TICKS_PER_MICROSECOND;
                        //distance_in_cm = session_microseconds * MICROSECONDS_TO_CM;
                        //just using elappsed ms for now
                        Collected_Distance = distanceTimer.ElapsedMilliseconds;
                        if (Current_avg_itteration++ >= Total_avg_itterations)
                        {
                            Distance_in_cm = Avg_Distance / Total_avg_itterations;
                            Current_avg_itteration = 1;
                            Avg_Distance = 0;
                        }
                        else
                        {
                            if (Collected_Distance > Maximum_distance_width)
                                Collected_Distance = Maximum_distance_width;
                            Avg_Distance += Collected_Distance;
                        }
                        distanceTimer.Reset();
                        break;
                    case GpioPinEdge.RisingEdge:
                        //it's a response, log it!
                        distanceTimer.Start();
                        break;
                }
            });
        }
    }
}
