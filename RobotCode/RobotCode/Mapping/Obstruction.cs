using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RobotCode.Mapping
{
    /// <summary>
    /// Represents a physical object in the mapping area
    /// </summary>
    public class Obstruction
    {
        //xml elements
        public XmlElement ObstructionElement { get; private set; }
        private XmlAttribute XmlID;
        private XmlAttribute XmlWidth;
        private XmlAttribute XmlHeight;
        private XmlAttribute XmlLocationX;
        private XmlAttribute XmlLocationY;
        //regular elemtns
        public int ID
        {
            get { return ID; }
            set
            {
                ID = value;
                if(XmlID != null)
                {
                    XmlID.Value = ID.ToString();
                }
            }
        }
        public float Width
        {
            get { return Width; }
            set
            {
                Width = value;
                if(XmlWidth != null)
                {
                    XmlWidth.Value = Width.ToString();
                }
            }
        }
        public float Height
        {
            get { return Height; }
            set
            {
                Height = value;
                if(XmlHeight != null)
                {
                    XmlHeight.Value = Height.ToString();
                }
            }
        }
        //TODO: use sometime
        public int XPosition { get; private set; }
        public int YPosition { get; private set; }
        public float Area
        {
            get
            {
                return Width * Height;
            }
        }
        public Obstruction(float width, float height, int ID, XmlDocument ParentDocument, XmlElement ObstructionsHolder)
        {
            //init the xml stuff
            ObstructionElement = ParentDocument.CreateElement("Obstructions");
            XmlHeight = ParentDocument.CreateAttribute("Height");
            XmlWidth = ParentDocument.CreateAttribute("Width");
            XmlID = ParentDocument.CreateAttribute("ID");
            //attache them
            ObstructionElement.Attributes.Append(XmlHeight);
            ObstructionElement.Attributes.Append(XmlWidth);
            ObstructionElement.Attributes.Append(XmlID);
            ObstructionsHolder.AppendChild(ObstructionElement);
            //NOW actually set the properties
            Width = width;
            Height = height;
            this.ID = ID;
        }
    }
}
