using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
    /// <summary>
    /// The actual map that will be created in memory
    /// </summary>
    public class Map
    {
        /// <summary>
        /// The physical work area when mappingthe area
        /// </summary>
        public Rectangle WorkArea;
        /// <summary>
        /// List of Physical obstructions inside the work area
        /// </summary>
        public List<Rectangle> Obstructions;
        /// <summary>
        /// The current status of the Map
        /// </summary>
        public MapProgress @CurrentMapProgress;
        private XmlDocument XmlMap;
        private List<XmlElement> XmlObstructions;
        private XmlElement XmlWorkArea;
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
                return XmlMap.ToString();
            }
        }
        public Map()
        {
            //init the map stuff, including the xml stuff
            CurrentMapProgress = MapProgress.None;
            XmlMap = new XmlDocument();
            Obstructions = new List<Rectangle>();
            XmlObstructions = new List<XmlElement>();
            WorkArea = new Rectangle();
            XmlDeclaration declaration = XmlMap.CreateXmlDeclaration("1.0", Encoding.UTF8.ToString(), true.ToString());

            XmlMap.InsertBefore(declaration, XmlMap.DocumentElement);
        }
    }
}
