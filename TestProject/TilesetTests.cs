using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TmxReadAndWrite.Models;

namespace TestProject;
public class TilesetTests
{
    [Fact]
    public void Serialize_ShouldSet()
    {
        var tileset = new Tileset();
        tileset.Name = "TestTileset";
        tileset.TileWidth = 32;
        tileset.TileHeight = 24;
        tileset.Spacing = 2;
        tileset.Margin = 1;
        tileset.Columns = 32;

        tileset.FirstGid = 1;

        tileset.Images = new TilesetImage[1]
        {
            new TilesetImage
            {
                Source = "test.png",
                Width = 512,
                Height = 128
            }
        };

        tileset.Tiles = new List<TilesetTile>
        {
            new TilesetTile
            {
                Id = 1,
                Type = "TestType",
            },
            new TilesetTile
            {
                Id = 2,
                Type = "TestType2",
                Objects = new ObjectGroup
                {
                    Name = "TestObjectGroup",
                    properties = new List<property>
                    {
                        new property
                        {
                            name = "TestProperty",
                            Type = "string",
                            value = "TestValue"
                        }
                    },
                }
            }
        };



        var serialized = tileset.Serialize();

        serialized.Contains("tilewidth=\"32\"").ShouldBeTrue();
        serialized.Contains("tileheight=\"24\"").ShouldBeTrue();
        serialized.Contains("name=\"TestTileset\"").ShouldBeTrue();
        serialized.Contains("spacing=\"2\"").ShouldBeTrue();
        serialized.Contains("margin=\"1\"").ShouldBeTrue();
        serialized.Contains("<image").ShouldBeTrue();
        serialized.Contains("width=\"512\"").ShouldBeTrue();
        serialized.Contains("height=\"128\"").ShouldBeTrue();

        serialized.Contains("name=\"TestProperty\"").ShouldBeTrue();
        serialized.Contains("value=\"TestValue\"").ShouldBeTrue();

    }
}
