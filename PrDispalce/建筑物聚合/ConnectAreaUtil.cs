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
    class ConnectAreaUtil
    {

        /// <summary>
        /// 获得任意两个多边形的连接区域
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <param name="cdt"></param>
        /// <returns></returns>
        Dictionary<List<int>,List<Triangle>> GetConnectArea(List<PolygonObject> PolygonList,ConsDelaunayTin cdt)
        {
            Dictionary<List<int>, List<Triangle>> ConnectAreaList = new Dictionary<List<int>, List<Triangle>>();

            for (int i = 0; i < cdt.DT.TriangleList.Count; i++)
            {
                List<int> TagIDList = new List<int>();
                if (cdt.DT.TriangleList[i].PolygonConnectType == 2)
                {
                    TagIDList.Add(cdt.DT.TriangleList[i].point1.TagValue);
                    TagIDList.Add(cdt.DT.TriangleList[i].point2.TagValue);
                    TagIDList.Add(cdt.DT.TriangleList[i].point2.TagValue);
                }

                if (TagIDList.Count > 0)
                {
                    TagIDList.Distinct();
                    TagIDList.Sort();

                    if (ConnectAreaList.ContainsKey(TagIDList))
                    {
                        ConnectAreaList[TagIDList].Add(cdt.DT.TriangleList[i]);
                    }

                    else
                    {
                        List<Triangle> TriangleList = new List<Triangle>();
                        TriangleList.Add(cdt.DT.TriangleList[i]);
                        ConnectAreaList.Add(TagIDList, TriangleList);
                    }
                }
            }

            return ConnectAreaList;
        }
    }
}
