using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using Jlw.Standard.Utilities.Data.DbUtility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Jlw.Utilities.OpenMaps
{
    public class TileEngine
    {
        private string _tileTemplate;
        private string _connString;
        private IModularDbClient _dbClient;
        private bool _useCache = false;

        public TileEngine(string tileEngine = null, string connString = null, IModularDbClient dbClient = null )
        {
            _tileTemplate = TileSources.Sources.FirstOrDefault(kvp=> kvp.Key.Equals(tileEngine ?? "", StringComparison.InvariantCultureIgnoreCase)).Value;
            _connString = connString;
            _dbClient = dbClient ?? new ModularDbClient<NullDbConnection, NullDbCommand, NullDbParameter>();
            
        }

        public TileData FetchTile(int x, int y, int zoom)
        {
            return new TileData(x, y, zoom, _tileTemplate, _connString, _dbClient);
        }



    }
}
