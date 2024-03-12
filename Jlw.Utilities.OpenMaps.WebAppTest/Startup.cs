using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jlw.Utilities.Data;
using Jlw.Utilities.Data.DbUtility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace Jlw.Utilities.OpenMaps.WebAppTest
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IModularDbClient>(new ModularDbClient<SqlConnection, SqlCommand, SqlParameter>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    string headJs = "<script defer src=\"https://use.fontawesome.com/releases/v5.11.2/js/all.js\"  crossorigin=\"anonymous\"></script><script src=\"https://unpkg.com/leaflet@1.5.1/dist/leaflet.js\" integrity=\"sha512-GffPMF3RvMeYyc1LWMHtK8EbPv0iNZ8/oTtHPx9/cc2ILxQ+u905qIwdpULaqDkyBKgOaB57QTMg7ztg8Jm2Og==\" crossorigin=\"\"></script>";
                    string headCss = "<link rel=\"stylesheet\" href=\"https://unpkg.com/leaflet@1.5.1/dist/leaflet.css\"\r\n   integrity=\"sha512-xwE/Az9zrjBIphAcBb3F6JVqxf46+CDLwfLMHloNu6KEQCAWi6HcDUbeOfBIptF7tcCzusKFjFw2yuvEpDL9wQ==\"\r\n   crossorigin=\"\"/>";
                    string js = "var mymap = L.map('mapid').setView([37.20980175586231,-93.27173709668480], 13);";
                    js += "L.tileLayer('https://localhost:44364/v1/{id}/{z}/{x}/{y}/tile.png', {\r\n    attribution: 'Map data &copy; <a href=\"https://www.openstreetmap.org/\">OpenStreetMap</a> contributors, <a href=\"https://creativecommons.org/licenses/by-sa/2.0/\">CC-BY-SA</a>',\r\n    maxZoom: 18,\r\n    id: 'osm',\r\n    accessToken: ''\r\n}).addTo(mymap);";
                    js +="var myIcon = L.divIcon({iconSize: [35,80],className:'', html:'<div class=\"fa-3x\"><span class=\"fa-layers fa-fw\"><i class=\"fas fa-map-marker\" style=\"color:red;\"></i><span class=\"fa-layers-text fa-inverse\" data-fa-transform=\"shrink-11.5 up-2\" style=\"font-weight:900;z-index:500;\">JW</span></span></div>'});\r\nL.marker([37.20980175586231,-93.27173709668480], {icon: myIcon}).addTo(mymap);";

                    await context.Response.WriteAsync("<!doctype html><html><head>" + headCss + headJs + "<style>body{margin:0;padding:0;} #mapid {height: 100vh; width: 80vw;}</style></head><body><div id=\"mapid\"></div><script>" + js + "</script>" + 
                                                      //"<script defer src=\"https://use.fontawesome.com/releases/v5.0.8/js/solid.js\" integrity=\"sha384-+Ga2s7YBbhOD6nie0DzrZpJes+b2K1xkpKxTFFcx59QmVPaSA8c7pycsNaFwUK6l\" crossorigin=\"anonymous\"></script>\r\n<script defer src=\"https://use.fontawesome.com/releases/v5.0.8/js/fontawesome.js\" integrity=\"sha384-7ox8Q2yzO/uWircfojVuCQOZl+ZZBg2D2J5nkpLqzH1HY0C1dHlTKIbpRz/LG23c\" crossorigin=\"anonymous\"></script>" +
                                                      "</body></html>");
                });
                endpoints.MapGet("/v1/{engine}/{zoom?}/{x?}/{y?}/tile.png", async context =>
                {
                    string engine;
                    switch (context.GetRouteValue("engine")?.ToString()?.ToLower())
                    {
                        case "cycle":
                            engine = "cycle";
                            break;
                        case "wikimedia":
                            engine = "wikimedia";
                            break;
                        default:
                            engine = "osm";
                            break;
                    }
                    var renderer = new TileEngine(engine, "data source=SPSDB4.sps.org;Integrated Security=SSPI;Initial Catalog=SPS_WEB_UTILITIES;", app.ApplicationServices.GetRequiredService<IModularDbClient>());
                    int x = DataUtility.ParseInt(context.GetRouteValue("x"));
                    int y = DataUtility.ParseInt(context.GetRouteValue("y"));
                    int zoom = DataUtility.ParseInt(context.GetRouteValue("zoom"));
                    zoom = Math.Min(Math.Max(zoom, 1), 18);
                    int max = DataUtility.ParseInt(Math.Pow(2, zoom));
                    x = ((x % max) + max) % max;
                    y = ((y % max) + max) % max;

                    var tile = renderer.FetchTile(x, y, zoom);
                    context.Response.ContentType = "image/png";
                    await context.Response.Body.WriteAsync(tile.GetAsPng());

                });
                endpoints.MapGet("/staticmap.php", async context =>
                {
                    int zoom = DataUtility.ParseInt(context.Request.Query.FirstOrDefault(kvp => kvp.Key.Equals("zoom", StringComparison.InvariantCultureIgnoreCase)).Value.ToString() ?? "");
                    zoom = Math.Min(Math.Max(zoom, 0), 15);
                    PointF center = MapEngine.ParsePointF(context.Request.Query.FirstOrDefault(kvp => kvp.Key.Equals("center", StringComparison.InvariantCultureIgnoreCase)).Value.ToString() ?? "");

                    center.Y = DataUtility.ParseFloat(Math.Max(Math.Min(center.Y, 37.3122615),37.085441 ));
                    center.X = DataUtility.ParseFloat(Math.Max(Math.Min(center.X, -93.113170), -93.5115452));
                    Size size = MapEngine.ParseSize(context.Request.Query.FirstOrDefault(kvp => kvp.Key.Equals("size", StringComparison.InvariantCultureIgnoreCase)).Value.ToString() ?? "");
                    size.Height = Math.Max(Math.Min(1024, size.Height), 1);
                    size.Width = Math.Max(Math.Min(1024, size.Width), 1);
                    
                    var renderer = new MapEngine("wikimedia", "data source=SPSDB4.sps.org;Integrated Security=SSPI;Initial Catalog=SPS_WEB_UTILITIES;", app.ApplicationServices.GetRequiredService<IModularDbClient>());
                    var map = renderer.FetchMapImage(center.Y, center.X, zoom, size.Width, size.Height);

                    string sMarkers = context.Request.Query.FirstOrDefault(kvp => kvp.Key.Equals("markers", StringComparison.InvariantCultureIgnoreCase)).Value.ToString() ?? ""; //"37.20980175586231,-93.27173709668480,fa-marker-red|37.20980175586231,-93.27173709668480,fa-marker-blue";

                    try
                    {
                        renderer.OverlayMarkers(map, center.Y, center.X, zoom, sMarkers);
                    }
                    catch
                    {
                        // Do nothing
                    }

//                    var marker = new MapMarker(sMarkers);
                    //await context.Response.WriteAsync($"x: {p.X}, y:{p.Y}\n");
                    context.Response.ContentType = "image/png";
                    await context.Response.Body.WriteAsync(map.GetAsPng());

                });
            });
        }
    }
}
