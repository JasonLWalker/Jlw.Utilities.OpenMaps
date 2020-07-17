using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jlw.Utilities.OpenMaps.Tests.UnitTests.TileRendererTests
{
    [TestClass]
    public class FetchTileFixture
    {
        [TestMethod]
        [TilePointDataSource]
        public void ShouldMatchForMapnik(int x, int y, int zoom)
        {
            string engine = "Mapnik";
            string template = "https://tile.openstreetmap.org/{0}/{1}/{2}.png";
            string expected = String.Format(template, zoom, x, y);
            TileEngine sut = new TileEngine(engine);
            var result = sut.FetchTile(x, y, zoom);
            Assert.AreEqual(expected, result.Source);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.AreEqual("Image<Rgba32>: 256x256", result.ImageData.ToString());
        }
    }
}
