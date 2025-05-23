using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace TmxReadAndWrite.Models;


[XmlType(AnonymousType = true)]
public partial class TilesetTile
{
    private IDictionary<string, string> propertyDictionaryField = null;

    [XmlIgnore]
    public IDictionary<string, string> PropertyDictionary
    {
        get
        {
            lock (this)
            {
                if (propertyDictionaryField == null)
                {
                    ForceRebuildPropertyDictionary();
                }
                return propertyDictionaryField;
            }
        }
    }



    List<property> mProperties = new List<property>();

    public List<property> properties
    {
        get { return mProperties; }
        set
        {
            mProperties = value;
        }
    }

    public bool ShouldSerializeproperties() => mProperties?.Count > 0;

    // Vic asks - shouldn't this be a uint?
    /// <remarks/>
    [XmlAttribute("id")]
    public int Id
    {
        get;
        set;
    }

    [XmlAttribute("type")]
    public string Type { get; set; }



    [XmlAttribute("class")]
    public string Class
    {
        get => Type;
        set => Type = value;
    }

    [XmlElement("animation")]
    public TileAnimation Animation
    {
        get;
        set;
    }

    [XmlElement("objectgroup")]
    public ObjectGroup Objects { get; set; }

    [XmlAttribute("probability")]
    public double Probability
    {
        get;
        set;
    } = 1;

    public TilesetTile()
        {
        }


    public override string ToString()
    {
        string toReturn = Id.ToString();

        if(!string.IsNullOrEmpty(Type))
        {
            toReturn += $" {Type} ";
        }

        if(PropertyDictionary.Count != 0)
        {
            toReturn += " (";

            foreach (var kvp in PropertyDictionary)
            {
                toReturn += "(" + kvp.Key + "," + kvp.Value + ")";
            }


            toReturn += ")";
        }
        return toReturn;
    }

    public void ForceRebuildPropertyDictionary()
    {
        propertyDictionaryField = TiledMapSave.BuildPropertyDictionaryConcurrently(properties);
    }
}
