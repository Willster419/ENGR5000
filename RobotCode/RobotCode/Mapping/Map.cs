using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Windows;

namespace RobotCode.Mapping
{
    /// <summary>
    /// A representation of showing where the robot is on the Mapped work area
    /// </summary>
    public struct Location
    {
        /// <summary>
        /// The x cordinate of the robot location
        /// </summary>
        public float X_Cordinate;
        /// <summary>
        /// The y cordinate of the robot location
        /// </summary>
        public float Y_Cordinate;
        /// <summary>
        /// Sets the location for the X_Cordinate and Y_Cordinate
        /// </summary>
        /// <param name="x">The x cordinate of the robot</param>
        /// <param name="y">The y cordinate of the robot</param>
        public void SetLocation(float x, float y)
        {
            X_Cordinate = x;
            Y_Cordinate = y;
        }
    }
    /// <summary>
    /// The actual map that will be created in memory
    /// </summary>
    public class Map
    {
        /// <summary>
        /// List of Physical obstructions inside the work area
        /// </summary>
        public List<Obstruction> Obstructions;
        /// <summary>
        /// The Main Document for the mapping to send via network. Includes XML declaration
        /// </summary>
        private XmlDocument MapDocument;
        /// <summary>
        /// The Map element to hold all attributes
        /// </summary>
        private XmlElement XmlMap;
        /// <summary>
        /// The map width xml attribute
        /// </summary>
        private XmlAttribute XmlWidth;
        /// <summary>
        /// the map height xml attribute
        /// </summary>
        private XmlAttribute XmlHeight;
        /// <summary>
        /// xml attribute of x cordinate where the robot is on the map
        /// </summary>
        private XmlAttribute XmlRobotPositionX;
        /// <summary>
        /// xml attribute of y cordinate where the robot is on the map
        /// </summary>
        private XmlAttribute XmlRobotPositionY;
        /// <summary>
        /// The xml element to act as holder for all obstructions shown in the map
        /// </summary>
        private XmlElement XmlObstructionsHolder;
        /// <summary>
        /// struct instance of the robot location
        /// </summary>
        public Location RobotLocation { get; private set; }
        /// <summary>
        /// Get the total number of obstructions in this map
        /// </summary>
        public int TotalObstructions
        {
            get
            {
                return Obstructions.Count;
            }
        }
        /// <summary>
        /// Gets the xml representation of the mapping data
        /// </summary>
        public string XMLMap
        {
            get
            {
                return MapDocument.OuterXml;
            }
        }
        /// <summary>
        /// The Width of the map
        /// </summary>
        private float Width;
        /// <summary>
        /// The Height of the map
        /// </summary>
        private float Height;
        /// <summary>
        /// The total area of the map
        /// </summary>
        public float Area
        {
            get { return Width * Height; }
        }
        /// <summary>
        /// Creates an instance of the map, with initialization and linking of all xml nodes
        /// </summary>
        public Map()
        {
            //init the map stuff, including the xml stuff
            MapDocument = new XmlDocument();
            XmlMap = MapDocument.CreateElement("Map");
            XmlObstructionsHolder = MapDocument.CreateElement("Obstructions");
            XmlHeight = MapDocument.CreateAttribute("Height");
            XmlWidth = MapDocument.CreateAttribute("Width");
            XmlRobotPositionX = MapDocument.CreateAttribute("RobotPositionX");
            XmlRobotPositionY = MapDocument.CreateAttribute("RobotPositionY");
            XmlMap.Attributes.Append(XmlHeight);
            XmlMap.Attributes.Append(XmlWidth);
            XmlMap.Attributes.Append(XmlRobotPositionX);
            XmlMap.Attributes.Append(XmlRobotPositionY);
            XmlMap.AppendChild(XmlObstructionsHolder);
            MapDocument.AppendChild(XmlMap);
            Obstructions = new List<Obstruction>();
            RobotLocation = new Location()
            {
                X_Cordinate = 0F,
                Y_Cordinate = 0F
            };
            //declaration
            //https://msdn.microsoft.com/en-us/library/system.xml.xmldocument.createxmldeclaration(v=vs.110).aspx
            XmlDeclaration declaration = MapDocument.CreateXmlDeclaration("1.0", Encoding.UTF8.ToString(), "yes");
            MapDocument.InsertBefore(declaration, MapDocument.DocumentElement);
        }
        /// <summary>
        /// Sets the width of the map, as well as updating the xml width attribute
        /// </summary>
        /// <param name="width">The width of the map</param>
        /// <param name="avg">set true to take an average of the first and second pass of getting the width</param>
        public void SetWidth(float width, bool avg)
        {
            if(avg)
            {
                this.Width = (this.Width + width) / 2;
            }
            else
            {
                this.Width = width;

            }
            XmlWidth.Value = this.Width.ToString();
        }
        /// <summary>
        /// Sets the height of the map, as well as updating the xml width attribute
        /// </summary>
        /// <param name="height">The height of the map</param>
        /// <param name="avg">set true to take an average of the first and second pass of getting the height</param>
        public void SetHeight(float height, bool avg)
        {
            if (avg)
            {
                this.Height = (this.Height + height) / 2;
            }
            else
            {
                this.Height = height;
            }
            XmlHeight.Value = this.Height.ToString();
        }
        /// <summary>
        /// Sets the location of the robot on the map
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetRobotLocation(float x, float y)
        {
            RobotLocation.SetLocation(x, y);
            XmlRobotPositionX.Value = x.ToString();
            XmlRobotPositionY.Value = y.ToString();
        }
        /// <summary>
        /// Add an obstruction (like a car) rectange to the map. on the UI it will be shown in red
        /// </summary>
        /// <param name="width">The width of the obstruction</param>
        /// <param name="height">The height of the obstruction</param>
        /// <param name="LocationX">The x cordinate location of the top left point</param>
        /// <param name="LocationY">The y cordinate location of the top left point</param>
        public void AddObstruction(float width, float height, float LocationX, float LocationY)
        {
            Obstructions.Append(new Obstruction(width, height, Obstructions.Count + 1, MapDocument, XmlObstructionsHolder));
        }
    }
}
