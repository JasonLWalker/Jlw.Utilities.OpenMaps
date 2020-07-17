using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jlw.Utilities.OpenMaps.Tests.UnitTests.TileRendererTests
{
    [TestClass]
    public class GetTileUrlFixture
    {
        [TestMethod]
        [TilePointDataSource]
        public void ShouldMatchForMapnik(int x, int y, int zoom)
        {
            string engine = "Mapnik";
            string template = "https://tile.openstreetmap.org/{0}/{1}/{2}.png";
            string expected = String.Format(template, zoom, x, y);
            string actual = TileData.GetTileUrl(x, y, zoom, engine);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [TilePointDataSource]
        public void ShouldMatchForOsm(int x, int y, int zoom)
        {
            string engine = "OSM";
            string template = "https://tile.openstreetmap.org/{0}/{1}/{2}.png";
            string expected = String.Format(template, zoom, x, y);
            string actual = TileData.GetTileUrl(x, y, zoom, engine);
            Assert.AreEqual(expected, actual);
        }

    }
}
