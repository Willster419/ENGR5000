using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotCode.Mapping
{
    /// <summary>
    /// Represents a physical object in the mapping area
    /// </summary>
    public class Rectangle
    {
        public float Width, Height;
        public float Area
        {
            get
            {
                return Width * Height;
            }
        }
        public Rectangle()
        {

        }
    }
}
