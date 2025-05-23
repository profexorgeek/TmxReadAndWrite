using System.Collections.Generic;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using TmxReadAndWrite.IO;

namespace TmxReadAndWrite.Models;

/// <remarks/>
[XmlType(AnonymousType = true)]
// ReSharper disable InconsistentNaming
[XmlRoot("mapTileset")]
public class Tileset
// ReSharper restore InconsistentNaming
{
    #region Fields/Properties

    /// <remarks/>
    [XmlElement("image")]
    public TilesetImage[] Images
    {
        get
        {
            return this._imageField;
        }
        set
        {
            if (this._imageField == null || this._imageField.Length == 0)
            {
                this._imageField = value;
            }
        }
    }
    public bool ShouldSerializeImage()
    {
        return string.IsNullOrEmpty(this.Source);
    }



    private TilesetImage[] _imageField;

    private mapTilesetTileOffset[] _tileOffsetField;

    [XmlAttribute("version")]
    public string Version { get; set; }

    [XmlAttribute("tiledversion")]
    public string TiledVersion { get; set; }

    [XmlAttribute("tilecount")]
    public int TileCount { get; set; }


    [XmlAttribute("columns")]
    public int Columns { get; set; }

    private string _sourceField;

    [XmlIgnore]
    public string SourceDirectory
    {
        get
        {
            if (_sourceField != null && _sourceField.Contains("\\"))
            {
                // add 1 to include the ending directory
                return _sourceField.Substring(0, _sourceField.LastIndexOf('\\') + 1);
            }
            else
            {
                return ".";
            }
        }
    }

    public static bool ShouldLoadValuesFromSource = true;

    [XmlAttribute("source")]
    public string Source
    {
        get
        {
            return _sourceField;
        }
        set
        {
            this._sourceField = value;

            if (ShouldLoadValuesFromSource)
            {
                LoadValuesFromSource();
            }
        }
    }

    [XmlIgnore]
    public bool IsShared
    {
        get
        {
            return !string.IsNullOrEmpty(Source);
        }
    }


    /// <remarks/>
    [XmlElement("tileoffset")]
    public mapTilesetTileOffset[] Tileoffset
    {
        get
        {
            return this._tileOffsetField;
        }
        set
        {
            if (this._tileOffsetField == null || this._tileOffsetField.Length == 0)
            {
                this._tileOffsetField = value;
            }
        }
    }


    [XmlArray("terraintypes")]
    public List<TilesetTerrain> Terraintypes = new List<TilesetTerrain>();

    public bool ShouldSerializeterraintypes()
    {
        return string.IsNullOrEmpty(this.Source);
    }

    [XmlElement("tile")]
    public List<TilesetTile> Tiles = new List<TilesetTile>();
    public bool ShouldSerializeTiles()
    {
        return string.IsNullOrEmpty(this.Source);
    }
    //{
    //    get
    //    {
    //        return this.tileField;
    //    }
    //    set
    //    {
    //        if (this.tileField != null && this.tileField.Length > 0)
    //        {
    //            return;
    //        }
    //        else
    //        {
    //            this.tileField = value;
    //        }
    //    }
    //}

    public void RefreshTileDictionary()
    {
        _tileDictionaryField = null;
    }


    private IDictionary<uint, TilesetTile> _tileDictionaryField;

    /// <summary>
    /// Dictionary containing the tiles where the key is the tile.id. This makes searching
    /// for tiles by id faster than a .Where call.
    /// </summary>
    [XmlIgnore]
    public IDictionary<uint, TilesetTile> TileDictionary
    {
        get
        {
            lock (this)
            {
                if (_tileDictionaryField == null)
                {
                    _tileDictionaryField = new ConcurrentDictionary<uint, TilesetTile>();

                    if (Tiles != null)
                    {
                        //Parallel.ForEach(tile, (t) =>
                        //            {
                        //                if (t != null && !tileDictionaryField.ContainsKey((uint)t.id + 1))
                        //                {
                        //                    tileDictionaryField.Add((uint)t.id + 1, t);
                        //                }
                        //            });

                        foreach (var t in Tiles)
                        {

                            // November 11, 2017 - why is this "+1"?
                            // That's confusing. Is it because in the old days 
                            // it was hardcoded to be the ID of the tile including the offset?
                            // for multiple tilesets the offset won't be 1...
                            //uint key = (uint)t.id + 1;
                            uint key = (uint)t.Id;
                            if (!_tileDictionaryField.ContainsKey(key))
                            {
                                _tileDictionaryField.Add(key, t);
                            }
                        }
                    }

                    return _tileDictionaryField;

                }
                else
                {
                    return _tileDictionaryField;
                }
            }

        }
    }



    /// <remarks/>
    [XmlAttribute("firstgid")]
    public uint FirstGid
    {
        get;
        set;
    }

    /// <remarks/>
    [XmlAttribute("name")]
    public string Name
    {
        get;
        set;
    }

    public bool ShouldSerializeName()
    {
        return string.IsNullOrEmpty(this.Source);
    }

    /// <remarks/>
    [XmlAttribute("tilewidth")]
    public int TileWidth
    {
        get;
        set;
    }

    public bool ShouldSerializeTilewidth()
    {
        return string.IsNullOrEmpty(this.Source);
    }

    /// <remarks/>
    [XmlAttribute("tileheight")]
    public int TileHeight
    {
        get;
        set;
    }

    public bool ShouldSerializeTileheight()
    {
        return string.IsNullOrEmpty(this.Source);
    }

    /// <remarks/>
    [XmlAttribute("spacing")]
    public int Spacing
    {
        get;
        set;
    }

    public bool ShouldSerializeSpacing()
    {
        return string.IsNullOrEmpty(this.Source);
    }

    /// <remarks/>
    [XmlAttribute("margin")]
    public int Margin
    {
        get;
        set;
    }

    public bool ShouldSerializeMargin()
    {
        return string.IsNullOrEmpty(this.Source);
    }

    [XmlArray("wangsets")]
    public List<wangset> Wangsets { get; set; } = new List<wangset>();


    #endregion

    private void LoadValuesFromSource()
    {
        if (!string.IsNullOrEmpty(this._sourceField))
        {
            _sourceField = _sourceField.Replace("/", "\\");

            tileset xts = null;

            if(_sourceField.EndsWith(".json"))
            {
                throw new System.InvalidOperationException(
                    $"Could not load tileset {_sourceField} because it uses the .json format which is currently not supported. Try saving your tileset as tsx instead of json.");
            }
            string fileAttemptedToLoad = _sourceField;
            //if (FileManager.IsRelative(_sourceField))
            //{
            //    fileAttemptedToLoad = FileManager.RemoveDotDotSlash( FileManager.RelativeDirectory + _sourceField);
            //}

            try
            {

                xts = FileManager.XmlDeserialize<tileset>(fileAttemptedToLoad);
            }
            catch (FileNotFoundException)
            {

                string message = "Could not find the shared tsx file \n" + fileAttemptedToLoad + 
                    "\nIf this is a relative file name, then the loader will use " +
                    "the FileManager's RelativeDirectory to make the file absolute.  Therefore, be sure to set the FileManger's RelativeDirectory to the file represented by " +
                    "this fileset before setting this property if setting this property manually.\n\nIf you are loading this TMX in a tool and you do not want to recursively" +
                    "load all content, then set Tileset.ShouldLoadValuesFromSource to false prior to loading.";


                throw new FileNotFoundException(message);
            }

            if (xts.image != null)
            {

                Images = new TilesetImage[xts.image.Length];

                Parallel.For(0, xts.image.Length, count =>
                {
                    this.Images[count] = new TilesetImage
                    {
                        Source = xts.image[count].source,
                        Height = xts.image[count].height != 0 ? xts.image[count].height : xts.tileheight,
                        Width = xts.image[count].width != 0 ? xts.image[count].width : xts.tilewidth
                    };
                });
            }
            this.Name = xts.name;
            this.Margin = xts.margin;
            this.Spacing = xts.spacing;
            this.TileHeight = xts.tileheight;
            this.TileWidth = xts.tilewidth;
            this.Tiles = xts.tile;

            this.Version = xts.Version;
            this.TiledVersion = xts.TiledVersion;
            this.Columns = xts.Columns;
            this.TileCount = xts.TileCount;

            this.Wangsets = xts.wangsets;
        }
    }

    public override string ToString()
    {
        string toReturn = this.Name;

        //if (!string.IsNullOrEmpty(Source))
        //{
        //    string sourceWithoutPath = FileManager.RemovePath(Source);
        //    toReturn += " (" + sourceWithoutPath + ")";
        //}

        return toReturn;
    }

    public string Serialize()
    {
        FileManager.XmlSerialize(typeof(Tileset), this, out string serialized);

        return serialized;
    }

}



public class TilesetTerrain
{
    [XmlElement("name")]
    public string Name { get; set; }
    [XmlElement("tile")]
    public int Tile { get; set; }
}
