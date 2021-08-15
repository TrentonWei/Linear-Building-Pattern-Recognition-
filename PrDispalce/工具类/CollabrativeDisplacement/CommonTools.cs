using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Data;
using AuxStructureLib.IO;
using AuxStructureLib.ConflictLib;
using System.Windows.Forms;

namespace PrDispalce.工具类.CollabrativeDisplacement
{
    class CommonTools
    {
        /// <summary>
        /// 将polygonobject转换成ipolygon
        /// </summary>
        /// <param name="pPolygonObject"></param>
        /// <returns></returns>
        public IPolygon ObjectConvert(PolygonObject pPolygonObject)
        {
            Ring ring1 = new RingClass();
            object missing = Type.Missing;

            IPoint curResultPoint = new PointClass();
            TriNode curPoint = null;
            if (pPolygonObject != null)
            {
                for (int i = 0; i < pPolygonObject.PointList.Count; i++)
                {
                    curPoint = pPolygonObject.PointList[i];
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    ring1.AddPoint(curResultPoint, ref missing, ref missing);
                }
            }

            IGeometryCollection pointPolygon = new PolygonClass();
            pointPolygon.AddGeometry(ring1 as IGeometry, ref missing, ref missing);
            IPolygon pPolygon = pointPolygon as IPolygon;
            pPolygon.SimplifyPreserveFromTo();
            return pPolygon;
        }

        /// <summary>
        /// 将polygon转换为polygonobject
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <returns></returns>
        public PolygonObject PolygonConvert(IPolygon pPolygon)
        {          
            int ppID = 0;//（polygonobject自己的编号，应该无用）
            //int pID = 0;//重心编号（应该无用，故没用编号）
            int vextID = 0;
            List<TriNode> trilist = new List<TriNode>();
            //Polygon的点集
            IPointCollection pointSet = pPolygon as IPointCollection;
            int count = pointSet.PointCount;
            double curX;
            double curY;
            //ArcGIS中，多边形的首尾点重复存储
            for (int i = 0; i < count; i++)
            {
                curX = pointSet.get_Point(i).X;
                curY = pointSet.get_Point(i).Y;
                //初始化每个点对象
                TriNode tPoint = new TriNode(curX, curY,vextID, ppID, FeatureType.PolygonType);
                trilist.Add(tPoint);
            }
            //生成自己写的多边形
            PolygonObject mPolygonObject = new PolygonObject(ppID, trilist);

            return mPolygonObject;
        }       
    }
}
