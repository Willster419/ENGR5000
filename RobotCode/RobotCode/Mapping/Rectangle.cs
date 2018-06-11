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
        public int Width, Height;
        public int LocationX, LocationY;
        public int Area
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
