using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Jlw.Standard.Utilities.Data;
using Jlw.Standard.Utilities.Data.DbUtility;
using SixLabors.ImageSharp;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace Jlw.Utilities.OpenMaps
{
    public class TileData : ImageInfo
    {
        protected int _x;
        protected int _y;
        protected int _zoom;

        public int X => _x;
        public int Y => _y;
        public int Zoom => _zoom;

        protected string _connString;
        protected static IModularDbClient _dbClient = new ModularSqlClient();
        protected bool _useCache = false;


        public TileData(string source, HttpStatusCode statusCode = HttpStatusCode.Ambiguous, string status="", byte[] imageData = null)  : base(source, statusCode, status, imageData)
        {
        }

        public TileData(int x, int y, int zoom, string engine = "Osm", string connString=null) : base("")
        {
            _connString = connString;
            Source = GetTileUrl(x, y, zoom, engine);
            FetchTileDataByUrl(x, y, zoom, Source);
        }

        public TileData(IDataRecord o) : base("")
        {
            if (o == null)
                return;

            Source = DataUtility.ParseString(o, "SourceUrl");
            StatusCode = string.IsNullOrWhiteSpace(Source) ? HttpStatusCode.NotFound : HttpStatusCode.OK;
            Status = "";
            _x = DataUtility.ParseInt(o, "CoordX");
            _y = DataUtility.ParseInt(o, "CoordY");
            _zoom = DataUtility.ParseInt(o, "Zoom");
            SetImageData((byte[])o["ImageData"]);
        }

        internal void FetchTileDataByUrl(int x, int y, int zoom, string url)
        {
            if (FetchTileFromCache(Source))
                return;
            
            _x = x;
            _y = y;
            _zoom = zoom;

            WebRequest request = WebRequest.Create(url);
            request.Headers.Add(HttpRequestHeader.UserAgent, "JlwUtilitiesOpenMaps");
            try
            {
                //var response = request.GetResponse();
                using (var response = request.GetResponse())
                {
                    //data = new TileData(response.ResponseUri.AbsoluteUri, ((HttpWebResponse) response).StatusCode, ((HttpWebResponse) response).StatusDescription);
                    Source = response.ResponseUri.AbsoluteUri;
                    StatusCode = ((HttpWebResponse) response).StatusCode;
                    Status = ((HttpWebResponse) response).StatusDescription;
                    ImageData = null;
                    using (Stream dataStream = response.GetResponseStream())
                    {
                        using (Image<Rgba32> img = Image.Load<Rgba32>(dataStream))
                        {
                            SetImageData(img);
                            if (StatusCode == HttpStatusCode.OK)
                                SaveTileToCache();
                        }
                    }
                }
                //response.Close();
            }
            catch (WebException wex)
            {
                WebExceptionStatus status = wex.Status;
                //data = new TileData(((HttpWebResponse)wex.Response)?.ResponseUri?.AbsoluteUri ?? request.RequestUri.AbsoluteUri, ((HttpWebResponse)wex.Response)?.StatusCode ?? HttpStatusCode.Ambiguous, ((HttpWebResponse)wex.Response)?.StatusDescription ?? "");
                Source = ((HttpWebResponse)wex.Response)?.ResponseUri?.AbsoluteUri ?? request.RequestUri.AbsoluteUri;
                StatusCode = ((HttpWebResponse)wex.Response)?.StatusCode ?? HttpStatusCode.Ambiguous;
                Status = ((HttpWebResponse) wex.Response)?.StatusDescription ?? "";
                SetImageData(GetErrorTile(x, y, zoom));
            }
            catch (Exception ex)
            {
                Source = request.RequestUri.AbsoluteUri;
                StatusCode = HttpStatusCode.Ambiguous;
                Status = "";
                SetImageData(TileData.GetErrorTile(x, y, zoom));
            }
        }

        internal bool FetchTileFromCache(string url)
        {
            TileData result = null;

            StatusCode = HttpStatusCode.Ambiguous;
            Status = "";
            ImageData = null;

            if (string.IsNullOrWhiteSpace(_connString))
                return false;
            
            using (var conn = _dbClient.GetConnection(_connString))
            {
                conn.Open();
                using (var cmd = _dbClient.GetCommand("sp_GetCachedMapTile", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    _dbClient.AddParameterWithValue("url", url, cmd);

                    using (var sqlResults = cmd.ExecuteReader())
                    {
                        while (sqlResults.Read())
                        {
                            Source = DataUtility.ParseString(sqlResults, "SourceUrl");
                            StatusCode = string.IsNullOrWhiteSpace(Source) ? HttpStatusCode.NotFound : HttpStatusCode.OK;
                            Status = "";
                            _x = DataUtility.ParseInt(sqlResults, "CoordX");
                            _y = DataUtility.ParseInt(sqlResults, "CoordY");
                            _zoom = DataUtility.ParseInt(sqlResults, "Zoom");
                            SetImageData((byte[])sqlResults["ImageData"]);
                        }
                    }
                }
            }

            if (StatusCode == HttpStatusCode.Ambiguous)
                return false;

            return true;
        }

        public void SaveTileToCache()
        {
            if (string.IsNullOrWhiteSpace(_connString))
                return;

            using (var conn = _dbClient.GetConnection(_connString))
            {
                conn.Open();
                using (var cmd = _dbClient.GetCommand("sp_SaveMapTileToCache", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    _dbClient.AddParameterWithValue("url", Source, cmd);
                    _dbClient.AddParameterWithValue("x", X, cmd);
                    _dbClient.AddParameterWithValue("y", Y, cmd);
                    _dbClient.AddParameterWithValue("zoom", Zoom, cmd);
                    _dbClient.AddParameterWithValue("imagedata", GetAsPng(), cmd);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static string GetTileUrl(int x, int y, int zoom, string engine = "Mapnik") 
        {
            string tileTemplate = TileSources.Sources.FirstOrDefault(kvp=> kvp.Key.Equals(engine ?? "Mapnik", StringComparison.InvariantCultureIgnoreCase)).Value;
            if (string.IsNullOrWhiteSpace(tileTemplate) && engine.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                tileTemplate = engine;

            string sZoom = zoom.ToString();
            string sX = x.ToString();
            string sY = y.ToString();
            return tileTemplate?.Replace("{Z}", sZoom).Replace("{X}", sX).Replace("{Y}", sY) ?? "";
        }


    }
}