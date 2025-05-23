using System.Xml.Serialization;

namespace TmxReadAndWrite;

[XmlRoot("tileset")]
public class Tileset
{
    [XmlAttribute("name")] public string Name;
    [XmlAttribute("tilewidth")] public int TileWidth;
    [XmlAttribute("tileheight")] public int TileHeight;
    [XmlAttribute("tilecount")] public int TileCount;
    [XmlAttribute("columns")] public int Columns;

    [XmlElement("image")] public TsxImage Image;
    [XmlElement("tile")] public List<TsxTile> Tiles;
    [XmlArray("wangsets")]
    [XmlArrayItem("wangset")] public List<TsxWangSet> WangSets;
}

public class TsxImage
{
    [XmlAttribute("source")] public string Source;
    [XmlAttribute("width")] public int Width;
    [XmlAttribute("height")] public int Height;
}

public class TsxTile
{
    [XmlAttribute("id")] public int Id;
    [XmlAttribute("type")] public string Type;
    [XmlAttribute("probability")] public float Probability;

    [XmlElement("animation")] public TsxAnimation Animation;
}

public class TsxAnimation
{
    [XmlElement("frame")] public List<TsxFrame> Frames;
}

public class TsxFrame
{
    [XmlAttribute("tileid")] public int TileId;
    [XmlAttribute("duration")] public int Duration;
}

public class TsxWangSet
{
    [XmlAttribute("name")] public string Name;
    [XmlAttribute("type")] public string Type;
    [XmlAttribute("tile")] public int Tile;

    [XmlElement("wangcolor")] public List<TsxWangColor> WangColors;
    [XmlElement("wangtile")] public List<TsxWangTile> WangTiles;
}

public class TsxWangColor
{
    [XmlAttribute("name")] public string Name;
    [XmlAttribute("color")] public string Color;
    [XmlAttribute("tile")] public int Tile;
    [XmlAttribute("probability")] public float Probability;
}

public class TsxWangTile
{
    [XmlAttribute("tileid")] public int TileId;
    [XmlAttribute("wangid")] public string WangId;
}
