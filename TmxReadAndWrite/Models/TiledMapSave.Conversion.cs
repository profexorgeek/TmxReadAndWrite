using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Globalization;
using TmxReadAndWrite.IO;

namespace TmxReadAndWrite.Models;

#region FileReferenceType enum
public enum FileReferenceType
{
    NoDirectory,
    Absolute,
    Relative
}
#endregion

public partial class TiledMapSave
{
    #region Enums

    public enum CSVPropertyType { Tile, Layer, Map, Object };

    enum LessOrGreaterDesired
    {
        Less,
        Greater,
        NoChange
    }
    #endregion

    #region Fields

    public static LayerVisibleBehavior LayerVisibleBehaviorValue = LayerVisibleBehavior.Ignore;
    public static int MaxDegreeOfParallelism = 1;
    
    const string animationColumnName = "EmbeddedAnimation (List<FlatRedBall.Content.AnimationChain.AnimationFrameSave>)";


    private static Tuple<float, float, float> _offset = new Tuple<float, float, float>(0f, 0f, 0f);

    #endregion

    #region Properties

    public static Tuple<float, float, float> Offset
    {
        get { return _offset; }
        set { _offset = value; }
    }

    #endregion

    /// <summary>
    /// The FRB plugin uses the properties dictionary to create objects and assign their values.
    /// This moves the Type value to the properties so that it can be used later on to create entities.
    /// Technically this may cause problems if there is a custom property called Type, but we'll cross that
    /// in the future if it ever becomes a problem.
    /// </summary>
    public void MoveTypeToProperties()
    {
        foreach (var tileset in this.Tilesets)
        {
            var tilesWithTypes = tileset.Tiles.Where(item => !string.IsNullOrEmpty(item.Type));

            foreach (var tile in tilesWithTypes)
            {
                var dictionaryEntry = tileset.TileDictionary[(uint)tile.Id];

                dictionaryEntry.properties.Add(new property { name = "Type", value = tile.Type });
            }
        }

        foreach (var objectLayer in this.Objectgroup)
        {
            if (objectLayer.@object != null)
            {
                foreach (var item in objectLayer.@object)
                {
                    if (!string.IsNullOrEmpty(item.Type) && item.properties != null)
                    {
                        item.properties.Add(new property { name = "Type", Type = "string", value = item.Type });
                        item.PropertyDictionary["Type"] = item.Type;
                    }

                    if (item.gid != null)
                    {
                        var tileset = GetTilesetForGid(item.gid.Value);

                        if (tileset.TileDictionary.ContainsKey(item.gid.Value - tileset.FirstGid))
                        {
                            var properties = tileset.TileDictionary[item.gid.Value - tileset.FirstGid];
                            if (!string.IsNullOrEmpty(properties.Type))
                            {
                                if(item.PropertyDictionary.ContainsKey("Type")) {
                                    //If it already has a Type, it's overridden in tiled so we don't want the base tileset Type
                                } else {
                                    item.properties.Add(new property { name = "Type", Type = "string", value = properties.Type });
                                    item.PropertyDictionary["Type"] = properties.Type;
                                }
                            }
                        }

                    }
                }
            }
        }

    }

    public void NameUnnamedTilesetTiles()
    {
        foreach (var tileset in this.Tilesets)
        {
            foreach (var tileDictionary in tileset.TileDictionary)
            {
                var propertyList = tileDictionary.Value.properties;
                var nameProperty = propertyList.FirstOrDefault(item => item.StrippedNameLower == "name");

                if (nameProperty == null)
                {
                    // create a new property:
                    var newNameProperty = new property();
                    newNameProperty.name = "Name";
                    newNameProperty.value = tileset.Name + tileDictionary.Key + "_autoname";

                    propertyList.Add(newNameProperty);

                    tileDictionary.Value.ForceRebuildPropertyDictionary();
                }
            }
        }
    }

    public void NameUnnamedObjects()
    {
        int index = 0;
        foreach (var objectLayer in this.Objectgroup)
        {
            // Seems like this can be null, not sure why...
            if (objectLayer.@object != null)
            {

                foreach (var objectInstance in objectLayer.@object)
                {
                    bool hasName = string.IsNullOrEmpty(objectInstance.Name) == false;
                    bool hasNameProperty = objectInstance.properties.Any(item => item.StrippedNameLower == "name");

                    if (!hasName && !hasNameProperty)
                    {
                        objectInstance.Name = $"object{index}_autoname";
                        objectInstance.properties.Add(new property { name = "name", value = objectInstance.Name });
                        index++;

                    }
                    else if (hasName && !hasNameProperty)
                    {
                        objectInstance.properties.Add(new property { name = "name", value = objectInstance.Name });
                    }
                }
            }
        }
    }

    public string ToCSVString(CSVPropertyType type = CSVPropertyType.Tile, string layerName = null)
    {
        var sb = new StringBuilder();
        IEnumerable<string> columnsAsEnumerable = GetColumnNames(type, layerName);
        var columnList = columnsAsEnumerable as IList<string> ?? columnsAsEnumerable.ToList();
        WriteColumnHeader(sb, columnList);
        WriteColumnValues(sb, columnList, type, layerName);

        return sb.ToString();
    }

    private void WriteColumnValues(StringBuilder sb, IList<string> columnNames, CSVPropertyType type, string layerName)
    {
        columnNames = columnNames.Select(item => property.GetStrippedName(item)).ToList();


        // TODO: There is probably a good way to refactor this code
        switch (type)
        {
            case CSVPropertyType.Tile:
                WriteColumnValuesForTile(sb, columnNames);
                break;
            case CSVPropertyType.Layer:

                WriteColumnValuesForLayer(sb, columnNames, layerName);
                break;
            case CSVPropertyType.Map:
                WriteValuesFromDictionary(sb, null, PropertyDictionary, columnNames, null);
                break;
            case CSVPropertyType.Object:
                this.Objectgroup.Where(
                    og =>
                    layerName == null ||
                    (((AbstractMapLayer)og).Name != null && ((AbstractMapLayer)og).Name.Equals(layerName, StringComparison.OrdinalIgnoreCase)))
                    .SelectMany(o => o.@object, (o, c) => new { group = o, obj = c, X = c.x, Y = c.y })
                    .Where(o => o.obj.gid != null)
                    .ToList()
                    .ForEach(o => WriteValuesFromDictionary(sb, o.group.PropertyDictionary, o.obj.PropertyDictionary, columnNames, null));
                break;
        }
    }

    private void WriteColumnValuesForLayer(StringBuilder sb, IList<string> columnNames, string layerName)
    {
        var availableItems =
        this.Layers.Where(
            l =>
            layerName == null ||
            (l.Name != null && l.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase))).ToList();

        foreach (var l in availableItems)
        {
            WriteValuesFromDictionary(sb, null, l.PropertyDictionary, columnNames, null);
        }

    }

    private void WriteColumnValuesForTile(StringBuilder sb, IList<string> columnNames)
    {
        for (int i = 0; i < this.Tilesets.Count; i++)
        {
            Tileset tileSet = this.Tilesets[i];

            if (tileSet.Tiles != null)
            {
                Func<TilesetTile, bool> predicate =
                    t => t.PropertyDictionary.Count > 0 ||
                    (t.Animation != null && t.Animation.Frames != null && t.Animation.Frames.Count > 0);

                foreach (TilesetTile tile in tileSet.Tiles.Where(predicate))
                {
                    Dictionary<string, string> propertyDictionary = new Dictionary<string, string>(tile.PropertyDictionary);

                    bool needsName = propertyDictionary.Count != 0 ||
                        (tile.Animation != null && tile.Animation.Frames != null && tile.Animation.Frames.Count != 0);

                    if (needsName && propertyDictionary.Keys.Any(item => property.GetStrippedName(item).ToLowerInvariant() == "name") == false)
                    {
                        var globalId = tile.Id + tileSet.FirstGid;
                        // This has properties, but no name, so let's give it a name!
                        propertyDictionary.Add("Name (required, string)", "Unnamed" + globalId);
                    }
                    WriteValuesFromDictionary(sb, null, propertyDictionary, columnNames, tile.Animation, i);
                }
            }
        }
        foreach (var objectGroup in this.Objectgroup)
        {
            foreach (var @object in objectGroup.@object)
            {
                if (@object.gid != null)
                {
                    WriteValuesFromDictionary(sb, null, @object.PropertyDictionary, columnNames, null);
                }
            }
        }
    }

    static int numberOfUnnamedTiles = 0;

    private void WriteValuesFromDictionary(StringBuilder sb, IDictionary<string, string> pDictionary,
        IDictionary<string, string> iDictionary, IEnumerable<string> columnNames, TileAnimation animation, int tilesetIndex = 0)
    {


        ///////////////////// Early out //////////////////////

        if (tilesetIndex >= Tilesets.Count)
        {
            return;
        }
        ////////////////// End early out ////////////////////
        uint startGid = Tilesets[tilesetIndex].FirstGid;

        string nameValue = GetNameValue(iDictionary);

        List<string> row = new List<string>();
        row.Add(nameValue);

        int layerIndex = -1;



        uint endIdExclusive = uint.MaxValue;
        if (tilesetIndex < Tilesets.Count - 1)
        {
            endIdExclusive = Tilesets[tilesetIndex + 1].FirstGid;
        }


        for (int i = 0; i < Layers.Count; i++)
        {
            var layer = Layers[i];
            // see if any layers reference this tile:
            foreach (var data in layer.Data)
            {
                foreach (var tile in data.Tiles)
                {
                    if (tile >= startGid && tile < endIdExclusive)
                    {
                        layerIndex = i;
                        break;
                    }
                }

                if (layerIndex != -1)
                {
                    break;
                }
            }

            if (layerIndex != -1)
            {
                break;
            }
        }

        bool hasAnimation = columnNames.Contains("EmbeddedAnimation");

        if (hasAnimation)
        {
            AddAnimationFrameAtIndex(animation, row, 0, layerIndex, tilesetIndex);
        }
        AppendCustomProperties(pDictionary, iDictionary, columnNames, row, false);

        AppendRowToStringBuilder(sb, row);

        if (animation != null && animation.Frames != null)
        {

            for (int i = 1; i < animation.Frames.Count; i++)
            {
                row = new List<string>();
                row.Add(""); // Name column


                if (hasAnimation)
                {
                    AddAnimationFrameAtIndex(animation, row, i, layerIndex, tilesetIndex);
                }
                AppendCustomProperties(pDictionary, iDictionary, columnNames, row, true);
                AppendRowToStringBuilder(sb, row);
            }
        }
    }

    private void AddAnimationFrameAtIndex(TileAnimation animation, List<string> row, int animationIndex, int indexOfLayerReferencingTileset, int tilesetIndex)
    {
        if (animation != null && animation.Frames != null && animation.Frames.Count > animationIndex)
        {
            // public int TileId
            // public int Duration

            var frame = animation.Frames[animationIndex];

            int leftCoordinate = 0;
            int rightCoordinate = 16;
            int topCoordinate = 0;
            int bottomCoordinate = 16;

            var frameId = (uint)frame.TileId;
            // not sure why, but need to add 1:
            //frameId++;
            // Update - I know why, because the TileId
            // is relative to the Tileset.  I didn't try
            // this with multiple tilesets, and the first
            // tileset has a starting ID of 1. 
            frameId += this.Tilesets[tilesetIndex].FirstGid;

            GetPixelCoordinatesFromGid(frameId, this.Tilesets[tilesetIndex],
                out leftCoordinate, out topCoordinate, out rightCoordinate, out bottomCoordinate);

            row.Add(string.Format(
                "new FlatRedBall.Content.AnimationChain.AnimationFrameSave(TextureName={0}, " +
                "FrameLength={1}, LeftCoordinate={2}, RightCoordinate={3}, TopCoordinate={4}, BottomCoordinate={5})",
                indexOfLayerReferencingTileset,
                (frame.Duration / 1000.0f).ToString(CultureInfo.InvariantCulture),
                leftCoordinate.ToString(CultureInfo.InvariantCulture),
                rightCoordinate.ToString(CultureInfo.InvariantCulture),
                topCoordinate.ToString(CultureInfo.InvariantCulture),
                bottomCoordinate.ToString(CultureInfo.InvariantCulture)));
        }
        else
        {
            row.Add(null);
        }
    }

    private static void AppendRowToStringBuilder(StringBuilder sb, List<string> row)
    {
        bool isFirst = true;
        foreach (var originalValue in row)
        {
            string value = originalValue;

            if (!isFirst)
            {
                sb.Append(",");

            }
            if (value != null)
            {
                value = value.Replace("\"", "\"\"");
            }

            sb.AppendFormat("\"{0}\"", value);
            isFirst = false;
        }
        sb.AppendLine();

    }

    private static string GetNameValue(IDictionary<string, string> iDictionary)
    {
        string nameValue = null;

        bool doesDictionaryContainNameValue =
            iDictionary.Any(p => property.GetStrippedName(p.Key).Equals("name", StringComparison.CurrentCultureIgnoreCase));

        if (doesDictionaryContainNameValue)
        {
            nameValue = iDictionary.First(p => property.GetStrippedName(p.Key).Equals("name", StringComparison.CurrentCultureIgnoreCase)).Value;
        }
        else
        {
            nameValue = "UnnamedTile" + numberOfUnnamedTiles;
            numberOfUnnamedTiles++;
        }
        return nameValue;
    }

    private static void AppendCustomProperties(IDictionary<string, string> pDictionary, IDictionary<string, string> iDictionary, IEnumerable<string> columnNames, List<string> row, bool forceEmpty)
    {
        foreach (string columnName in columnNames)
        {
            string strippedColumnName = property.GetStrippedName(columnName);

            bool isAnimation =
                strippedColumnName.Equals("embeddedanimation", StringComparison.CurrentCultureIgnoreCase);

            bool isCustomProperty = !isAnimation &&
                !strippedColumnName.Equals("name", StringComparison.CurrentCultureIgnoreCase);



            if (isCustomProperty)
            {
                if (!forceEmpty && iDictionary.Any(p => property.GetStrippedName(p.Key).Equals(strippedColumnName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    var value =
                        iDictionary.First(p => property.GetStrippedName(p.Key).Equals(strippedColumnName, StringComparison.CurrentCultureIgnoreCase)).Value;

                    row.Add(value);
                }
                // Victor Chelaru
                // October 12, 2014
                // Not sure what pDictionary
                // is, but it looks like it's
                // only used for "object" CSVs.
                // My first question is - do we need
                // to use stripped names here?  Also, do
                // we even want to support object dictionaries
                // in the future?  How does this fit in with the
                // new "level" pattern.
                else if (!forceEmpty && pDictionary != null && pDictionary.Any(p => p.Key.Equals(strippedColumnName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    var value =
                        pDictionary.First(p => p.Key.Equals(strippedColumnName, StringComparison.CurrentCultureIgnoreCase)).Value;

                    row.Add(value);
                }
                else
                {
                    row.Add(null);
                }
            }
        }
    }

    private static void WriteColumnHeader(StringBuilder sb, IEnumerable<string> columnNames)
    {
        sb.Append("Name (required)");
        foreach (string columnName in columnNames)
        {
            string strippedName = property.GetStrippedName(columnName);


            bool isName = strippedName.Equals("name", StringComparison.CurrentCultureIgnoreCase);

            if (!isName)
            {
                // Update August 27, 2012
                // We can't just assume that
                // all of the column names are
                // going to be capitalized.  This
                // was likely done to force the Name
                // property to be capitalized, which we
                // want, but we don't want to do it for everything.
                //if (columnName.Length > 1)
                //{
                //    sb.AppendFormat(",{0}{1}", columnName.Substring(0, 1).ToUpper(), columnName.Substring(1));
                //}
                //else
                //{
                //    sb.AppendFormat(",{0}", columnName.ToUpper());
                //}
                sb.Append("," + columnName);
            }
        }
        sb.AppendLine();
    }

    /// <summary>
    /// Compares the stripped name of properties - removing the type
    /// </summary>
    public class CaseInsensitivePropertyEqualityComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {

            return property.GetStrippedName(x).Equals(property.GetStrippedName(y), StringComparison.CurrentCultureIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return property.GetStrippedName(obj).ToLowerInvariant().GetHashCode();
        }
    }

    private IEnumerable<string> GetColumnNames(CSVPropertyType type, string layerName)
    {
        var comparer = new CaseInsensitivePropertyEqualityComparer();

        var columnNames = new HashSet<string>();

        switch (type)
        {
            case CSVPropertyType.Tile:
                return GetColumnNamesForTile(comparer);
            case CSVPropertyType.Layer:
                return
                    this.Layers.Where(
                        l =>
                        layerName == null ||
                        (l.Name != null && l.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase)))
                        .SelectMany(l => l.PropertyDictionary)
                        .Select(d => d.Key)
                        .Distinct(comparer);
            case CSVPropertyType.Map:
                return this.PropertyDictionary.Select(d => d.Key).Distinct(comparer);
            case CSVPropertyType.Object:

                List<string> toReturn = new List<string>();

                toReturn.Add("X (int)");
                toReturn.Add("Y (int)");

                if (Objectgroup != null)
                {

                    var query1 =
                        Objectgroup.Where(l =>
                                                 layerName == null ||
                                                 (l.Name != null &&
                                                  l.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase)));
                    var query2 =
                        Objectgroup.Where(l =>
                                                 layerName == null ||
                                                 (l.Name != null &&
                                                  l.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase)));
                    return toReturn
                        .Union(query1
                                   .SelectMany(o => o.@object)
                                   .Where(o => o.gid != null) //November 2015 by Jesse Crafts-Finch: will ignore objects which are to be treated as sprites (they have a gid). 
                                   .SelectMany(o => o.PropertyDictionary)
                                   .Select(d => d.Key), comparer)
                        .Union(query2
                                   .SelectMany(o => o.PropertyDictionary)
                                   .Select(d => d.Key), comparer);
                }
                else
                {
                    return toReturn;
                }

        }
        return columnNames;
    }

    private IEnumerable<string> GetColumnNamesForTile(CaseInsensitivePropertyEqualityComparer comparer)
    {
        List<string> toReturn = new List<string>();

        // Name is required and always available
        toReturn.Add("Name (string, required)");

        // And animation is required too
        toReturn.Add(animationColumnName);

        toReturn.AddRange(this.Tilesets.SelectMany(t => t.Tiles)
                .SelectMany(tile => tile.PropertyDictionary)
                .Select(d => d.Key)
                //.Distinct(comparer)
                .ToList());

        foreach (var group in this.Objectgroup)
        {
            bool addedGroup = false;
            foreach (var @object in group.@object)
            {
                if (@object.gid != null)
                {
                    addedGroup = true;
                    toReturn.AddRange(@object.PropertyDictionary.Keys);
                }
            }
            if (addedGroup)
            {
                toReturn.AddRange(group.PropertyDictionary.Keys);
            }
        }

        return toReturn.Distinct(comparer);
    }


    private static bool IsName(string key)
    {
        return String.Equals(property.GetStrippedName(key), "name", StringComparison.OrdinalIgnoreCase);
    }


    public void CalculateWorldCoordinates(int layerIndex, int tileIndex, int tileWidth, int tileHeight, int layerWidth, out float x, out float y, out float z)
    {
        int tileXIndex = tileIndex % this.Width;
        int tileYIndex = tileIndex / this.Width;
        CalculateWorldCoordinates(layerIndex, tileXIndex, tileYIndex, tileWidth, tileHeight, layerWidth, out x, out y, out z);
    }

    public void CalculateWorldCoordinates(int layerIndex, float tileXIndex, float tileYIndex, int tileWidth, int tileHeight, int layerWidth, out float x, out float y, out float z)
    {
        if (this.Orientation == null || this.Orientation.Equals("orthogonal"))
        {
            x = (tileXIndex * this.TileWidth) + (this.TileWidth / 2.0f);
            x += (tileWidth - this.TileWidth) / 2.0f;
            y = -(tileYIndex * this.TileHeight) - (this.TileHeight / 2.0f);
            y += (tileHeight - this.TileHeight) / 2.0f;
            z = layerIndex;
        }
        else if (this.Orientation != null && this.Orientation.Equals("isometric"))
        {
            y = -((tileXIndex * this.TileWidth / 2.0f) + (tileYIndex * this.TileWidth / 2.0f)) / 2;
            y -= tileHeight / 2.0f;
            x = -((tileYIndex * this.TileWidth / 2.0f) - (tileXIndex * this.TileHeight / 2.0f) * 2);
            x += (tileWidth * this.Width) / 2.0f;
            z = ((tileYIndex * layerWidth + tileXIndex) * .000001f) + layerIndex;
        }
        else
        {
            throw new NotImplementedException("Unknown orientation type");
        }

        x += Offset.Item1;
        y += Offset.Item2;
        z += Offset.Item3;
    }

    public static void GetPixelCoordinatesFromGid(uint gid, Tileset tileSet,
        out int leftPixelCoord, out int topPixelCoord, out int rightPixelCoord, out int bottomPixelCoord)
    {
        int imageWidth = tileSet.Images[0].Width;
        int imageHeight = tileSet.Images[0].Height;
        int tileWidth = tileSet.TileWidth;
        int spacing = tileSet.Spacing;
        int tileHeight = tileSet.TileHeight;
        int margin = tileSet.Margin;


        var gidWithoutRotation = gid & 0x0fffffff;

        bool flipHorizontally, flipVertically, flipDiagonally;
        GetFlipBoolsFromGid(gid, out flipHorizontally, out flipVertically, out flipDiagonally);

        // Calculate pixel coordinates in the texture sheet
        leftPixelCoord = CalculateXCoordinate(gidWithoutRotation - tileSet.FirstGid, imageWidth, tileWidth, spacing, margin);
        topPixelCoord = CalculateYCoordinate(gidWithoutRotation - tileSet.FirstGid, imageWidth, tileWidth, tileHeight, spacing, margin);
        rightPixelCoord = leftPixelCoord + tileWidth;
        bottomPixelCoord = topPixelCoord + tileHeight;

        if ((flipHorizontally && flipDiagonally == false) ||
            (flipVertically && flipDiagonally))
        {
            var temp = rightPixelCoord;
            rightPixelCoord = leftPixelCoord;
            leftPixelCoord = temp;
        }

        if ((flipVertically && flipDiagonally == false) ||
            (flipHorizontally && flipDiagonally))
        {
            var temp = topPixelCoord;
            topPixelCoord = bottomPixelCoord;
            bottomPixelCoord = temp;

        }
    }

    public static void GetFlipBoolsFromGid(uint gid, out bool flipHorizontally, out bool flipVertically, out bool flipDiagonally)
    {
        const uint FlippedHorizontallyFlag = 0x80000000;
        const uint FlippedVerticallyFlag = 0x40000000;
        const uint FlippedDiagonallyFlag = 0x20000000;

        flipHorizontally = (gid & FlippedHorizontallyFlag) == FlippedHorizontallyFlag;
        flipVertically = (gid & FlippedVerticallyFlag) == FlippedVerticallyFlag;
        flipDiagonally = (gid & FlippedDiagonallyFlag) == FlippedDiagonallyFlag;
    }

    public Tileset GetTilesetForGid(uint gid, bool shouldRemoveFlipFlags = true)
    {
        var effectiveGid = gid;

        if (shouldRemoveFlipFlags)
        {
            effectiveGid = 0x0fffffff & gid;
        }

        // Assuming tilesets are sorted by the firstgid value...
        // Resort with LINQ if not
        if (Tilesets != null)
        {
            for (int i = Tilesets.Count - 1; i >= 0; --i)
            {
                Tileset tileSet = Tilesets[i];
                if (effectiveGid >= tileSet.FirstGid)
                {
                    return tileSet;
                }
            }
        }
        return null;
    }

    private static float GetTextureCoordinate(int pixelCoord, int dimension, LessOrGreaterDesired lessOrGreaterDesired)
    {
        float asFloat = pixelCoord / (float)dimension;

        //const float modValue = .000001f;
        const float modValue = .000002f;
        //const float modValue = .00001f;
        switch (lessOrGreaterDesired)
        {
            case LessOrGreaterDesired.Greater:
                return asFloat + modValue;
            case LessOrGreaterDesired.Less:
                return asFloat - modValue;
            default:
                return asFloat;
        }
    }

    public static int CalculateYCoordinate(uint gid, int imageWidth, int tileWidth, int tileHeight, int spacing, int margin)
    {

        int tilesWide = TilesetExtensionMethods.GetNumberOfTilesWide(
            imageWidth, margin, tileWidth, spacing);

        int normalizedy = (int)(gid / tilesWide);
        int pixely = normalizedy * (tileHeight + spacing) + margin;

        return pixely;
    }

    public static string RelativeDirectory { get; set;}

    public static int CalculateXCoordinate(uint gid, int imageWidth, int tileWidth, int spacing, int margin)
    {
        var tilesWide = TilesetExtensionMethods.GetNumberOfTilesWide(
            imageWidth, margin, tileWidth, spacing);


        int normalizedX = (int)(gid % tilesWide);
        int pixelX = normalizedX * (tileWidth + spacing) + margin;

        return pixelX;
    }

    public static TiledMapSave FromFile(string fileName)
    {
        // I believe the relative directory of the TMS must be its own directory so that
        // image references can be tracked on XML deserialization
        string oldRelativeDirectory = RelativeDirectory;
        try
        {
            RelativeDirectory = Path.GetDirectoryName(fileName);
        }
        catch
        {
        }
        TiledMapSave tms = null;

        try
        {
            if(fileName?.EndsWith(".json") == true)
            {
                throw new InvalidOperationException("Could not load TMX file with .json extension");
            }
            else
            {
                tms = FileManager.XmlDeserialize<TiledMapSave>(fileName);
                tms.FileName = fileName;
            }
        }
        finally
        {
            RelativeDirectory = oldRelativeDirectory;

        }




        foreach (MapLayer layer in tms.Layers)
        {
            if (!layer.PropertyDictionary.ContainsKey("VisibleBehavior"))
            {
                layer.VisibleBehavior = LayerVisibleBehaviorValue;
            }
            else
            {
                if (!Enum.TryParse(layer.PropertyDictionary["VisibleBehavior"], out layer.VisibleBehavior))
                {
                    layer.VisibleBehavior = LayerVisibleBehaviorValue;
                }
            }
        }
        return tms;
    }

    public string Serialize()
    {
        FileManager.XmlSerialize(typeof(TiledMapSave), this, out string serialized);

        return serialized;
    }

    public void Save(string fileName)
    {
        FileManager.XmlSerialize(this, fileName);

    }
}
