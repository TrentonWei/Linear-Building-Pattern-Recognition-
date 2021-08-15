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

using AuxStructureLib;
using AuxStructureLib.IO;

namespace PrDispalce.典型化
{
    class SimilarityComputation
    {
        //参数
        PrDispalce.BuildingSim.PublicUtil Pu = new BuildingSim.PublicUtil();
        PrDispalce.BuildingSim.BuildingMeasures BM = new BuildingSim.BuildingMeasures();

        /// <summary>
        /// 计算两个建筑物的大小相似度(大面积建筑物与小面积建筑物面积的比值)[0,1]
        /// </summary>
        /// <returns></returns>
        public double SizeSimilarity(PolygonObject pObject1, PolygonObject pObject2)
        {
            double SizeSimi = -1;

            if (pObject1.Area < pObject2.Area)
            {
                SizeSimi = pObject1.Area / pObject2.Area;
            }

            else
            {
                SizeSimi = pObject2.Area / pObject1.Area;
            }
            return SizeSimi;
        }

        /// <summary>
        /// 计算两个建筑物的方向相似度
        /// Type=1[0,180];Type=2[0,90]
        /// </summary>
        /// <returns></returns>
        public double OrientationSimilarity(PolygonObject pObject1, PolygonObject pObject2, int Type)
        {
            double OrientationSimi = -1;

            IPolygon Po1 = Pu.PolygonObjectConvert(pObject1);
            IPolygon Po2 = Pu.PolygonObjectConvert(pObject2);
            double SBRO1 = BM.GetSMBROrientation(Po1);
            double SBRO2 = BM.GetSMBROrientation(Po2);

            #region 范围[0,180]
            if (Type == 1)
            {
                OrientationSimi = Math.Abs(SBRO1 - SBRO2);
            }
            #endregion

            #region 范围[0,90]
            if (Type == 2)
            {
                OrientationSimi = Math.Abs(SBRO1 - SBRO2);
                if (OrientationSimi > 90)
                {
                    OrientationSimi = 180 - OrientationSimi;
                }
            }
            #endregion

            return OrientationSimi;
        }

        /// <summary>
        /// 计算两个建筑物的形状相似度(ShapeIndex)
        /// [0,1]
        /// </summary>
        /// <returns></returns>
        public double ShapeSimilarity(PolygonObject pObject1, PolygonObject pObject2)
        {
            double ShapeSimi = -1;

            IPolygon Po1 = Pu.PolygonObjectConvert(pObject1);
            IPolygon Po2 = Pu.PolygonObjectConvert(pObject2);
            double ShapeIndex1 = BM.ShapeIndex(Po1);
            double ShapeIndex2 = BM.ShapeIndex(Po2);

            if (ShapeIndex1 > ShapeIndex2)
            {
                ShapeSimi = ShapeIndex2 / ShapeIndex1;
            }
            else
            {
                ShapeSimi = ShapeIndex1 / ShapeIndex2;
            }

            return ShapeSimi;
        }
    }
}
