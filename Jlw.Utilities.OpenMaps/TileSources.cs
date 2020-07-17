using System.Collections.Generic;

namespace Jlw.Utilities.OpenMaps
{
    public class TileSources
    {
        private static readonly Dictionary<string, string> _tileSources = new Dictionary<string, string>()
        {
            {"osm", "https://tile.openstreetmap.org/{Z}/{X}/{Y}.png"},
            {"mapnik", "https://tile.openstreetmap.org/{Z}/{X}/{Y}.png"},
            {"cycle", "http://a.tile.opencyclemap.org/cycle/{Z}/{X}/{Y}.png"},
            {"wikimedia","https://maps.wikimedia.org/osm-intl/{Z}/{X}/{Y}.png"}
        };

        public static IEnumerable<KeyValuePair<string, string>> Sources => _tileSources;
    }
}
