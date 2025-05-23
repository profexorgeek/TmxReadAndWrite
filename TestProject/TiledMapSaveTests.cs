
using Shouldly;
using TmxReadAndWrite.Models;

namespace TestProject;

public class TiledMapSaveTests
{
    [Fact]
    public void Serialize_ShouldSetDimensions()
    {
        var tiledMapSave = new TiledMapSave();

        tiledMapSave.Height = 32;
        tiledMapSave.Width = 64;
        tiledMapSave.TileWidth = 12;
        tiledMapSave.TileHeight = 16;

        var serialized = tiledMapSave.Serialize();

        serialized.Contains("height=\"32\"").ShouldBeTrue();
        serialized.Contains("width=\"64\"").ShouldBeTrue();
        serialized.Contains("tilewidth=\"12\"").ShouldBeTrue();
        serialized.Contains("tileheight=\"16\"").ShouldBeTrue();


    }

    [Fact]
    public void Serialize_ShouldSaveLayers()
    {
        var tiledMapSave = new TiledMapSave();

        var layer = new MapLayer
        {
            Name = "TestLayer",
            Width = 10,
            Height = 10,
            Opacity = 1.0f,
            IsVisible = true
        };

        layer.Data = new MapLayerData[1]
        {
            new MapLayerData
            {
                Length = 100,
                Encoding = "csv",
                Compression = "zlib"
            }
        };

        var tileInts = new uint[64 * 64];

        for(int i = 0; i < tileInts.Length; i++)
        {
            tileInts[i] = (uint)(i % 3);
        }

        layer.Data[0].SetTileData(tileInts, "", "gzip");



        tiledMapSave.MapLayers.Add(layer);

        var serialized = tiledMapSave.Serialize();

        serialized.Contains("length=\"4096\"").ShouldBeTrue();

    }

    [Fact]
    public void Serialize_ShouldSaveTileset()
    {
        var tiledMapSave = new TiledMapSave();

        Tileset.ShouldLoadValuesFromSource = false;
        var tileset = new Tileset
        {
            Source = "test.tsx",
            FirstGid = 1
        };

        tiledMapSave.Tilesets.Add(tileset);

        var serialized = tiledMapSave.Serialize();

        serialized.Contains("source=\"test.tsx\"").ShouldBeTrue();
        serialized.Contains("firstgid=\"1\"").ShouldBeTrue();
    }
}