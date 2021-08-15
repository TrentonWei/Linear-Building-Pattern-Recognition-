using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AuxStructureLib;
using AuxStructureLib.IO;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;

namespace PrDispalce.建筑物聚合
{
    /// <summary>
    /// 对找到的所有凸多边形进行处理
    /// </summary>
    class ConvexPolygonProcess
    {
        /// <summary>
        /// 获得最小的可处理单元
        /// </summary>OperatingConvexPolygon 操作的凸区域
        /// <returns></returns>
        public List<PolygonObject> GetMinOperatingPolygon(ProxiEdge MinEdge,List<PolygonObject> OperatingConvexPolygon,PolygonObject curPolygon)
        {
            List<PolygonObject> MinOperatingPolygon = new List<PolygonObject>();

            return MinOperatingPolygon;
        }
    }
}
