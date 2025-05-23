using Serilog;
using TmxReadAndWrite;

var log = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

//var tmxSvc = new TmxService(log);

//var tileset = tmxSvc.LoadTileset("C:\\Users\\justi\\Documents\\projects\\WeeRpg\\WeeRpg\\Content\\narfox.tsx");