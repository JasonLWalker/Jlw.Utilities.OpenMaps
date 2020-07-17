using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jlw.Utilities.OpenMaps.Tests
{
    public class TilePointDataSourceAttribute : Attribute, ITestDataSource
    {
        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            foreach (var tuple in DataSourceValues.TilePointData)
            {
                //var value = tuple.Item1;
                //var expectedValue = tuple.ExpectedValue ?? false;

                //var desc = tuple.Description;
                yield return new object[] {tuple.Item1, tuple.Item2, tuple.Item3};
            }
        }

        
        public string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            if (data != null)
                return string.Format(CultureInfo.CurrentCulture, "Should match for Tile({0}, {1}), Zoom: {2}", data[0], data[1], data[2]);

            return null;
        }
        


    }
}
