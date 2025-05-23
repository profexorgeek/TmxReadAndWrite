using System;
using System.Xml.Serialization;

namespace TmxReadAndWrite.Models
{
    [Serializable]
    [XmlInclude(typeof (MapLayer))]
    [XmlInclude(typeof  (MapImageLayer))]
    [XmlInclude(typeof (ObjectGroup))]
    public abstract class AbstractMapLayer
    {
        private float _offsetX;
        private float _offsetY;

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("id")]
        public int Id { get; set; }


        [XmlAttribute("tintcolor")]
        public string TintColor { get; set; } = "#ffffff";


        [XmlAttribute("opacity")]
        public float Opacity
        {
            get; set;
        } = 1.0f;

        private int? visibleField;
        [XmlAttribute("visible")]
        public int Visible
        {
            get
            {
                return this.visibleField.HasValue ? this.visibleField.Value : 1;
            }
            set
            {
                this.visibleField = value;
            }
        }

        public bool IsVisible
        {
            get
            {
                return visibleField == null || visibleField == 1;
            }
            set
            {
                if (value)
                {
                    visibleField = 1;
                }
                else
                {
                    visibleField = 0;
                }
            }
        }

        private float? parallaxxField;
        [XmlAttribute("parallaxx")]
        public float ParallaxX
        {
            get
            {
                return this.parallaxxField.HasValue ? this.parallaxxField.Value : 1f;
            }
            set
            {
                this.parallaxxField = value;
            }
        }

        private float? parallaxyField;
        [XmlAttribute("parallaxy")]
        public float ParallaxY
        {
            get
            {
                return this.parallaxyField.HasValue ? this.parallaxyField.Value : 1f;
            }
            set
            {
                this.parallaxyField = value;
            }
        }

        [XmlAttribute("offsetx")]
        public float OffsetX
        {
            get { return _offsetX; }
            set { _offsetX = value; }
        }

        [XmlAttribute("offsety")]
        public float OffsetY
        {
            get { return _offsetY; }
            set { _offsetY = value; }
        }
    }
}
