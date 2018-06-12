using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;

namespace RobotCode.Resources
{
    public class StatusIndicator : DispatcherTimer
    {
        public int TimeThrough;
        public int TimeToStop;
        public GpioPin GpioPin;
        public int Index;
        public StatusIndicator()
        {
            
        }
    }
}
