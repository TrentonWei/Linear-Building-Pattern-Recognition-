using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using PrDispalce.PatternRecognition;

using AuxStructureLib;
using AuxStructureLib.IO;

namespace PrDispalce.BuildingSim
{
    class CuttedPolygon
    {
        public IPolygon OriginalPolygon;//原始建筑物
        public List<IPolygon> CuttedPolygons=new List<IPolygon>();//剖分后的建筑物列表
        public List<IPolygon> MatchedPolygons=new List<IPolygon>();//剖分后存在匹配关系的建筑物

        #region 构造函数
        public CuttedPolygon(IPolygon Po,List<IPolygon> CutPoList,List<IPolygon> MatchedPoList)
        {
            this.OriginalPolygon = Po;
            this.CuttedPolygons = CutPoList.ToList();
            this.MatchedPolygons = MatchedPoList.ToList();
        }
        /// <summary>
        /// 表示对一个给定的建筑物初始化为一个CuttedPolygon
        /// </summary>
        /// <param name="Po"></param>
        public CuttedPolygon(IPolygon Po)
        {
            this.OriginalPolygon = Po;
            this.CuttedPolygons.Add(Po);
            this.MatchedPolygons.Add(Po);
        }

        public CuttedPolygon()
        {
        }
        #endregion
    }
}
