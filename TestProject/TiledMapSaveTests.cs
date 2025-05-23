
using TmxReadAndWrite.Models;

namespace TestProject;

public class TiledMapSaveTests
{
    [Fact]
    public void Save_ShouldSaveTmx()
    {
        var tiledMapSave = new TiledMapSave();

        tiledMapSave.Height = 64;
        tiledMapSave.Width = 64;

        var serialized = tiledMapSave.Serialize("asdf.tmx");


    }
}