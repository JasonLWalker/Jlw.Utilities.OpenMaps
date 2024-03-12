using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Jlw.Utilities.Data;
using Jlw.Utilities.Data.DbUtility;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jlw.Utilities.OpenMaps
{
    public class MapEngine
    {
        private string _tileTemplate;
        private int _tileSize = 256;
        //private int _maxWidth = 1024;
        //private int _maxHeight = 1024;
        private string _connString;
        //private bool _useCache = false;
        protected static Regex rxMarkers = new Regex(@"^\s*(-?[0-9]+[\\.]?[0-9]*)\s*,\s*(-?[0-9]+[\\.]?[0-9]*)\s*[,]?([a-z0-9\\-]*)", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
        protected static Regex rxSize = new Regex(@"^([0-9]{1,4})x([0-9]{1,4})$", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
        protected static IModularDbClient _dbClient;

        public MapEngine(string tileEngine = null, string connString=null, IModularDbClient dbClient=null)
        {
            _tileTemplate = TileSources.Sources.FirstOrDefault(kvp=> kvp.Key.Equals(tileEngine ?? "", StringComparison.InvariantCultureIgnoreCase)).Value;
            _connString = connString;
            _dbClient = dbClient ?? new ModularDbClient<NullDbConnection, NullDbCommand, NullDbParameter>();
        }

        protected double LonToTile(double lon, int zoom)
        {
            return (((lon + 180) / 360.0) * Math.Pow(2, zoom));
        }

        protected double LatToTile(double lat, int zoom)
        {
            return (1 - Math.Log(Math.Tan(lat * Math.PI / 180) + 1 / Math.Cos(lat * Math.PI / 180)) / Math.PI) / 2 * Math.Pow(2, zoom);
        }

        public ImageInfo FetchMapImage(double lat, double lon, int zoom, int width, int height)
        {
            Image<Rgba32> mapData = FetchMapImageFromCache(lat, lon, zoom, width, height);

            ImageInfo result = new ImageInfo("");
            if (mapData != null)
            {
                result.SetImageData(mapData);
                return result;

            }

            mapData = new Image<Rgba32>(width, height);

            double centerX = LonToTile(lon, zoom);
            double centerY = LatToTile(lat, zoom);

            int startX = DataUtility.ParseInt(Math.Floor(centerX - ( (double)width / _tileSize ) / 2 ) );
            int startY = DataUtility.ParseInt(Math.Floor(centerY - ( (double)height / _tileSize ) / 2 ) );

            int endX = DataUtility.ParseInt(Math.Ceiling(centerX + ( (double)width / _tileSize ) / 2 ) );
            int endY = DataUtility.ParseInt(Math.Ceiling(centerY + ( (double)height / _tileSize ) / 2 ) );


            double offsetX = -Math.Floor((centerX - Math.Floor(centerX)) * _tileSize);
            double offsetY = -Math.Floor((centerY - Math.Floor(centerY)) * _tileSize);

            offsetX += Math.Floor((double)width / 2);
            offsetY += Math.Floor((double)height / 2);

            offsetX += Math.Floor(startX - Math.Floor(centerX)) * _tileSize;
            offsetY += Math.Floor(startY - Math.Floor(centerY)) * _tileSize;

            int max = DataUtility.ParseInt(Math.Pow(2, zoom));

            var renderer = new TileEngine(_tileTemplate, _connString);
            var opts = new GraphicsOptions(); //GraphicsOptions.Default;
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    var xWrap = ((x % max) + max) % max;
                    var yWrap = ((y % max) + max) % max;

                    TileData tileData = renderer.FetchTile(xWrap, yWrap, zoom);
                    Image<Rgba32> img;
                    if (tileData.ImageData.Height == _tileSize && tileData.ImageData.Width == _tileSize)
                    {
                        img = tileData.ImageData;
                    }
                    else
                    {
                        img = ImageInfo.GetErrorTile(xWrap, yWrap, zoom);

                    }
                    int destX = DataUtility.ParseInt((x - startX) * _tileSize + offsetX);
                    int destY = DataUtility.ParseInt((y - startY) * _tileSize + offsetY);
                    mapData.Mutate(ctx => ctx.DrawImage(img, new Point(destX, destY), opts ));
                }
            }
            mapData.Mutate(ctx=>
            {
                int rX = 165;
                int rXOffset = 8;
                int rY = 20;
                int fontSize = 8;
                ctx.FillPolygon(new DrawingOptions(), new SolidBrush(Color.FromRgba(0xFF, 0xFF, 0xFF, 0x88)), new PointF[]{new PointF(width - rX,height -rY), new PointF(width,height - rY), new PointF(width,height), new PointF(width - rX,height)});
                ctx.DrawText($"Copyright © OpenStreetMap Contributors", SystemFonts.CreateFont("Arial", 8, FontStyle.Regular), Color.Black, new PointF(width - rX + rXOffset, height - (rY/2) - (fontSize /2)));
                
            });

            result = new ImageInfo("");
            result.SetImageData(mapData);
            SaveMapImageToCache(lat, lon, zoom, width, height, result.GetAsPng());
            return result;
        }

        public void SaveMapImageToCache(double lat, double lon, int zoom, int width, int height, byte[] imagedata)
        {
            if (string.IsNullOrWhiteSpace(_connString))
                return;

            using (var conn = _dbClient.GetConnection(_connString))
            {
                conn.Open();
                using (var cmd = _dbClient.GetCommand("sp_SaveMapImageToCache", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    _dbClient.AddParameterWithValue("lat", lat, cmd);
                    _dbClient.AddParameterWithValue("lon", lon, cmd);
                    _dbClient.AddParameterWithValue("zoom", zoom, cmd);
                    _dbClient.AddParameterWithValue("width", width, cmd);
                    _dbClient.AddParameterWithValue("height", height, cmd);
                    _dbClient.AddParameterWithValue("imagedata", imagedata, cmd);

                    cmd.ExecuteNonQuery();
                }
            }
        }


        internal Image<Rgba32> FetchMapImageFromCache(double lat, double lon, int zoom, int width, int height)
        {
            Image<Rgba32> result = null;
            
            if (string.IsNullOrWhiteSpace(_connString))
                return null;
            
            using (var conn = _dbClient.GetConnection(_connString))
            {
                conn.Open();
                using (var cmd = _dbClient.GetCommand("sp_GetCachedMapImage", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    _dbClient.AddParameterWithValue("lat", lat, cmd);
                    _dbClient.AddParameterWithValue("lon", lon, cmd);
                    _dbClient.AddParameterWithValue("zoom", zoom, cmd);
                    _dbClient.AddParameterWithValue("width", width, cmd);
                    _dbClient.AddParameterWithValue("height", height, cmd);

                    using (var sqlResults = cmd.ExecuteReader())
                    {
                        while (sqlResults.Read())
                        {
                            result = Image.Load<Rgba32>((byte[]) sqlResults["ImageData"]);
                        }
                    }
                }
            }

            return result;
        }

        public void OverlayMarkers(ImageInfo imageInfo, double lat, double lon, int zoom, string markers)
        {
            double width = imageInfo?.ImageData?.Width ?? 0;
            double height = imageInfo?.ImageData?.Height ?? 0;
//            Point p = new Point();

            Point markerOffset = new Point(-12, -32);
            Point shadowOffset = new Point(-1, -13);

            imageInfo?.ImageData?.Mutate(ctx =>
            {
                var marker = new MapMarker(markers);
                var opts = new GraphicsOptions();
                if (marker.ImageData != null)
                {
                    var mW = marker?.ImageData?.Width ?? 0;
                    var mH = marker?.ImageData?.Height ?? 0;
                    double centerX = LonToTile(lon, zoom);
                    double centerY = LatToTile(lat, zoom);

                    var destX = DataUtility.ParseInt(Math.Floor((width / 2) - _tileSize * (centerX - LonToTile(marker.X, zoom))));
                    var destY = DataUtility.ParseInt(Math.Floor((height / 2) - _tileSize * (centerY - LatToTile(marker.Y, zoom))));
                    ctx.DrawImage(marker.ShadowData, new Point(destX + shadowOffset.X, destY + shadowOffset.Y), opts);
                    ctx.DrawImage(marker.ImageData, new Point(destX + markerOffset.X, destY + markerOffset.Y), opts);
                }
                    
            });
        }

        

        public static PointF ParsePointF(string val)
        {
            PointF point = new PointF();
            
            if (rxMarkers.IsMatch(val))
            {
                var matches = rxMarkers.Match(val);
                point.Y = DataUtility.ParseFloat(matches.Groups[1].Value);
                point.X = DataUtility.ParseFloat(matches.Groups[2].Value);
            }

            return point;
        }

        public static Size ParseSize(string val)
        {
            Size size = new Size();
            if (rxSize.IsMatch(val))
            {
                var matches = rxSize.Match(val);
                size.Width = DataUtility.ParseInt(matches.Groups[1].Value);
                size.Height = DataUtility.ParseInt(matches.Groups[2].Value);
            }

            return size;
        }

    }
}
