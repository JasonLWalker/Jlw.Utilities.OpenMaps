using System.Net;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Jlw.Utilities.OpenMaps
{
    public class MapData : ImageInfo
    {
        public MapData(string source, HttpStatusCode statusCode = HttpStatusCode.Ambiguous, string status="", byte[] imageData = null) : base(source, statusCode, status, imageData)
        {
        }


    }
}