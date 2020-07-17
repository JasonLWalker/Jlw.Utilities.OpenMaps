using System;
using System.Collections.Generic;

namespace Jlw.Utilities.OpenMaps.Tests
{
    public partial class DataSourceValues
    {
        public static readonly List<Tuple<int, int, int>> TilePointData = new List<Tuple<int, int, int>>();


        static DataSourceValues()
        {
            Init();
        }


        protected static void Init()
        {
            TilePointData.Add(new Tuple<int, int, int>(0,0,0));
            TilePointData.Add(new Tuple<int, int, int>(1,2,3));
            TilePointData.Add(new Tuple<int, int, int>(4,6,8));
            TilePointData.Add(new Tuple<int, int, int>(5,7,9));
        }

    }
}