using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using AuxStructureLib;

namespace PrDispalce.典型化
{
    /// <summary>
    /// 计算建筑物的特征参数
    /// </summary>
    class Measures
    {
        /// <summary>
        /// 计算建筑物的大小参数
        /// </summary>
        /// <returns></returns>
        public double SizeIndex(PolygonObject pObject)
        {
            double SizeIndex = -1;
            IPolygon pPolygon = PolygonObjectConvert(pObject);
            IArea pArea = pPolygon as IArea;
            SizeIndex = pArea.Area;
            return SizeIndex;
        }

        /// <summary>
        /// 计算建筑物形状参数
        /// </summary>
        /// <returns></returns>
        public double ShapeIndex(PolygonObject pObject)
        {
            double ShapeIndex = -1; double Pi = 3.1415926;

            IPolygon pPolygon = PolygonObjectConvert(pObject);
            double length1 = pPolygon.Length;

            IArea pArea = (IArea)pPolygon;
            double area1 = pArea.Area;

            ShapeIndex = 4 * Pi * area1 / (length1 * length1);
            return ShapeIndex;
        }

        /// <summary>
        /// 计算建筑物方向参数
        /// </summary>
        /// <returns></returns>
        public double OrientationIndex(PolygonObject pObject)
        {
            double OrientationIndex = -1;
            PrDispalce.工具类.ParameterCompute parametercompute = new 工具类.ParameterCompute();
            IPolygon pPolygon = PolygonObjectConvert(pObject);
            IPolygon SMBR = parametercompute.GetSMBR(pPolygon);

            #region 计算SMBR的方向
            IArea sArea = SMBR as IArea;
            IPoint CenterPoint = sArea.Centroid;

            IPointCollection sPointCollection = SMBR as IPointCollection;
            IPoint Point1 = sPointCollection.get_Point(1);
            IPoint Point2 = sPointCollection.get_Point(2);
            IPoint Point3 = sPointCollection.get_Point(3);
            IPoint Point4 = sPointCollection.get_Point(4);

            ILine Line1 = new LineClass(); Line1.FromPoint = Point1; Line1.ToPoint = Point2;
            ILine Line2 = new LineClass(); Line2.FromPoint = Point2; Line2.ToPoint = Point3;

            double Length1 = Line1.Length; double Length2 = Line2.Length;

            double LLength = 0; double SLength = 0;

            IPoint oPoint1 = new PointClass(); IPoint oPoint2 = new PointClass();
            IPoint oPoint3 = new PointClass(); IPoint oPoint4 = new PointClass();

            if (Length1 > Length2)
            {
                OrientationIndex = Line1.Angle;
                LLength = Length1; SLength = Length2;
            }

            else
            {
                OrientationIndex = Line2.Angle;
                LLength = Length2; SLength = Length1;
            }
            #endregion

            return OrientationIndex;
        }

        /// <summary>
        /// 将建筑物转化为Polygonobject
        /// </summary>
        /// <param name="pPolygonObject"></param>
        /// <returns></returns>
        IPolygon PolygonObjectConvert(PolygonObject pPolygonObject)
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
    }
}
