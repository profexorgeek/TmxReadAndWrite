using Serilog;
using System.Xml.Serialization;

namespace TmxReadAndWrite;

public class TmxService
{
    ILogger _log;
    XmlSerializer _tsxSerializer;

    public TmxService(ILogger log)
    {
        _log = log;
        _tsxSerializer = new XmlSerializer(typeof(Tileset));
    }

    public Tileset? LoadTileset(string path)
    {
        Tileset? tileset = null;

        if(File.Exists(path))
        {
            using var reader = new StreamReader(path);
            tileset = (Tileset?)_tsxSerializer.Deserialize(reader);
        }
        else
        {
            _log.Error($"Tileset path doesn't exist: {path}");
        }

        return tileset;
    }
}
