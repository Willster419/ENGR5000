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
    /// The status of the mapping progress
    /// </summary>
    public enum MapProgress
    {
        /// <summary>
        /// Has not started or not engaged in mapping
        /// </summary>
        None = 0,
        /// <summary>
        /// The physical work area is currently being mapped
        /// </summary>
        MappingWorkArea = 1,
        /// <summary>
        /// An obstruction is being mapped
        /// </summary>
        MappingObstruction = 2,
        /// <summary>
        /// Mapping is complege
        /// </summary>
        MappingComplete = 3
    }
    public struct Location
    {
        public float X_Cordinate;
        public float Y_Cordinate;
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
        /// The current status of the Map
        /// </summary>
        public MapProgress @CurrentMapProgress;
        /// <summary>
        /// The Main Document for the mapping to send via network
        /// </summary>
        private XmlDocument MapDocument;
        private XmlElement XmlMap;
        private XmlAttribute XmlWidth;
        private XmlAttribute XmlHeight;
        private XmlAttribute XmlRobotPositionX;
        private XmlAttribute XmlRobotPositionY;
        private XmlElement XmlObstructionsHolder;
        public Location RobotLocation;
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
        public string XMLMap
        {
            get
            {
                return MapDocument.ToString();
            }
        }
        public float Width
        {
            get { return Width; }
            set { Width = value; }
        }
        public float Height
        {
            get { return Height; }
            set { Height = value; }
        }
        public float Area
        {
            get { return Width * Height; }
        }

        public Map()
        {
            //init the map stuff, including the xml stuff
            CurrentMapProgress = MapProgress.None;
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
            Obstructions = new List<Obstruction>();
            RobotLocation = new Location()
            {
                X_Cordinate = 0F,
                Y_Cordinate = 0F
            };
            //declaration
            //https://msdn.microsoft.com/en-us/library/system.xml.xmldocument.createxmldeclaration(v=vs.110).aspx
            XmlDeclaration declaration = MapDocument.CreateXmlDeclaration("1.0", Encoding.UTF8.ToString(), true.ToString());
            MapDocument.InsertBefore(declaration, MapDocument.DocumentElement);
        }

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
        }
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
        }
        public void AddObstruction(float width, float height, float LocationX, float LocationY)
        {
            Obstructions.Append(new Obstruction(width, height, Obstructions.Count + 1, MapDocument, XmlObstructionsHolder));
        }
    }
}
