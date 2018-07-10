using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;

namespace RobotCode.Resources
{
    /// <summary>
    /// The differet codename enumerations for the differnt areas an indicator could get it's status count from
    /// </summary>
    public enum StatusFeed
    {
        /// <summary>
        /// The Robot status enumeration to use
        /// </summary>
        RobotStatus,
        /// <summary>
        /// The Signal battery voltage enumeration to use
        /// </summary>
        SignalBattery,
        /// <summary>
        /// The power battery voltage enumeration to use
        /// </summary>
        PowerBattery
    }
    /// <summary>
    /// A class to specify an LED as a status indicator system on top of a timer
    /// NOTE: timer is *not* a new thread
    /// </summary>
    public class StatusIndicator : DispatcherTimer
    {
        /// <summary>
        /// The counter for the itteration of where it is in it's status blinking
        /// </summary>
        public int TimeThrough;
        /// <summary>
        /// The number of itterations through the timer loop before reseting
        /// </summary>
        public int TimeToStop;
        /// <summary>
        /// The gpio pin object used for the status indiactor
        /// </summary>
        public GpioPin GpioPin;
        /// <summary>
        /// The index to specify how/where TimeToStop gets it's new updated value from
        /// see StatusFeed enumeration for what they are
        /// </summary>
        public int Index;
        /// <summary>
        /// Don't worry about this.
        /// </summary>
        public StatusIndicator()
        {
            
        }
    }
}
