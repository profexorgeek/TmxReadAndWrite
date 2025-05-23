using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace TmxReadAndWrite.Models;

[Serializable]
public partial class MapLayer : AbstractMapLayer
{
    private IDictionary<string, string> propertyDictionaryField = null;


    List<property> mProperties = new List<property>();

    public List<property> properties
    {
        get { return mProperties; }
        set
        {
            mProperties = value;
        }
    }


    private MapLayerData[] dataField;

    private int widthField;

    private int heightField;

    [XmlIgnore]
    public IDictionary<string, string> PropertyDictionary
    {
        get
        {
            lock (this)
            {
                if (propertyDictionaryField == null)
                {
                    propertyDictionaryField = TiledMapSave.BuildPropertyDictionaryConcurrently(properties);
                }
                if (!propertyDictionaryField.Any(p => p.Key.Equals("name", StringComparison.OrdinalIgnoreCase)))
                {
                    propertyDictionaryField.Add("name", this.Name);
                }
                return propertyDictionaryField;
            }
        }
    }

    /// <remarks/>
    [XmlElement("data", Form = System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable = true)]
    public MapLayerData[] Data
    {
        get
        {
            return this.dataField;
        }
        set
        {
            this.dataField = value;
            if (dataField != null)
            {
                foreach (MapLayerData layerData in dataField)
                {
                    layerData.Length = Width * Height;
                }
            }
        }
    }

    /// <remarks/>
    [XmlAttribute("width")]
    public int Width
    {
        get
        {
            return this.widthField;
        }
        set
        {
            this.widthField = value;
            if (this.Data != null)
            {
                foreach (MapLayerData layerData in Data)
                {
                    layerData.Length = Width * Height;
                }
            }
        }
    }

    /// <remarks/>
    [XmlAttribute("height")]
    public int Height
    {
        get
        {
            return this.heightField;
        }
        set
        {
            this.heightField = value;
            if (this.Data != null)
            {
                foreach (MapLayerData layerData in Data)
                {
                    layerData.Length = Width * Height;
                }
            }
        }
    }
    
    [XmlIgnore]
    public TiledMapSave.LayerVisibleBehavior VisibleBehavior = TiledMapSave.LayerVisibleBehavior.Ignore;

    public override string ToString()
    {
        return Name;
    }

}
