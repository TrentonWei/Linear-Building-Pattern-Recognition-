using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Windows.Forms;

using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Display;

using AuxStructureLib;
using AuxStructureLib.IO;
using PrDispalce;

namespace PrDispalce.工具类
{
    class ParameterCompute
    {
        double pi = 3.1415926;
        PrDispalce.工具类.Symbolization SB = new 工具类.Symbolization();//测试可视化用

        #region 圆类
        public class Circle
        {
            public double r;
            public IPoint Center;
            public IPolygon Polygon;
        }
        #endregion

        #region 提取点集
        #region 获取Feature点集（去掉首末点中一个点后的点集）
        public IPointArray GetPoints(IFeature pFeature)
        {
            IPointArray PointArray = new PointArrayClass();

            IPolygon pPolygon = (IPolygon)pFeature.Shape;

            IPointCollection tPointCollection = pPolygon as IPointCollection;
            for (int k = 0; k < tPointCollection.PointCount - 1; k++)
            {
                IPoint tPoint = tPointCollection.get_Point(k);
                PointArray.Add(tPoint);
            }


            return PointArray;
        }
        #endregion

        #region 获取polygon点集
        public IPointArray GetPoints(IPolygon pPolygon)
        {
            IPointArray PointArray = new PointArrayClass();

            IPointCollection tPointCollection = pPolygon as IPointCollection;
            for (int k = 0; k < tPointCollection.PointCount - 1; k++)
            {
                IPoint tPoint = tPointCollection.get_Point(k);
                PointArray.Add(tPoint);
            }

            return PointArray;
        }
        #endregion
        #endregion

        /// <summary>
        /// 计算建筑物面积
        /// </summary>
        /// <param name="pFeature"></param>
        /// <returns></returns>
        public double GetArea(IPolygon pPolygon)
        {
            IArea pArea = (IArea)pPolygon;
            double area1 = pArea.Area;
            return area1;
        }

        /// <summary>
        /// 计算IPQCom
        /// </summary>
        /// <param name="pFeature"></param>
        /// <returns></returns>
        public double GetIPQCom(IPolygon pPolygon)
        {
            double length1 = pPolygon.Length;

            IArea pArea = (IArea)pPolygon;
            double area1 = pArea.Area;

            double MillerMeasure = 4 * pi * area1 / (length1 * length1);
            return MillerMeasure;
        }

        /// <summary>
        /// 计算IPQCom
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <returns></returns>
        public double GetIPQCom(PolygonObject pPolygon)
        {
            double MillerMeasure = 4 * pi * pPolygon.Area/ (pPolygon.Perimeter * pPolygon.Perimeter);
            return MillerMeasure;
        }

       /// <summary>
       /// 计算最小凸包
       /// </summary>
       /// <param name="iPolygon"></param>
       /// <returns></returns>
        public IPolygon GetConvexHull(IPolygon iPolygon)
        {
            ITopologicalOperator iTopo = iPolygon as ITopologicalOperator;
            IGeometry pConvexHull;
            pConvexHull = iTopo.ConvexHull();

            IPolygon Polygon = pConvexHull as IPolygon;

            return Polygon;
        }

        /// <summary>
        /// 计算最小绑定矩形
        /// </summary>
        /// <param name="iPolygon"></param>
        /// <returns></returns>
        public IPolygon GetSMBR(IPolygon iPolygon)
        {
            IPolygon SMBR = new PolygonClass();
            IPolygon polygon = GetConvexHull(iPolygon);

            IPointArray polyPointArray = GetPoints(polygon);
            IPointArray pPointArray = GetPoints(iPolygon);

            #region 获取建筑物重心
            IArea pArea = iPolygon as IArea;
            IPoint Center = pArea.Centroid;
            #endregion

            #region 获取多边形每个点相对于重心的坐标
            IPointArray TransPointArray = new PointArrayClass();

            int pPointCount = pPointArray.Count;

            for (int j = 0; j < pPointCount; j++)
            {
                IPoint Point = (IPoint)pPointArray.get_Element(j);
                IPoint TransPoint = new PointClass();

                TransPoint.X = Point.X - Center.X;
                TransPoint.Y = Point.Y - Center.Y;

                TransPointArray.Add(TransPoint);

            }
            #endregion

            int polyPointCount = polyPointArray.Count;
            double MinArea = 10000000000;
            double MinOrientation = 0;


            for (int i = 0; i < polyPointCount; i++)
            {
                #region 获取每条凸包边的方向
                IPoint StartPoint = (IPoint)polyPointArray.get_Element(i);
                IPoint EndPoint;

                if (i + 1 == polyPointCount)
                {
                    EndPoint = (IPoint)polyPointArray.get_Element(0);
                }

                else
                {
                    EndPoint = (IPoint)polyPointArray.get_Element(i + 1);
                }

                ILine Line = new LineClass();
                Line.FromPoint = StartPoint;
                Line.ToPoint = EndPoint;

                double angle = Line.Angle;
                #endregion

                #region 获取旋转后绑定矩形
                double MinX, MaxX, MinY, MaxY;
                MinX = MinY = MaxX = MaxY = 0;

                for (int m = 0; m < pPointCount; m++)
                {
                    IPoint Point1;

                    Point1 = (IPoint)TransPointArray.get_Element(m);

                    double X = Point1.X * Math.Cos(angle) + Point1.Y * Math.Sin(angle);
                    double Y = Point1.X * (-1) * Math.Sin(angle) + Point1.Y * Math.Cos(angle);


                    if (MinX > X)
                    {
                        MinX = X;
                    }

                    if (MaxX < X)
                    {
                        MaxX = X;
                    }


                    if (MinY > Y)
                    {
                        MinY = Y;
                    }

                    if (MaxY < Y)
                    {
                        MaxY = Y;

                    }
                }
                #endregion

                #region 将点集转化为polygon
                Ring ring = new RingClass();
                object missing = Type.Missing;

                //IMultipoint pSourceMultipoint = new MultipointClass();
                //IPointCollection4 PointCollection = new PolylineClass();
                //object missing = Type.Missing;

                IPoint mPoint1 = new PointClass();
                IPoint mPoint2 = new PointClass();
                IPoint mPoint3 = new PointClass();
                IPoint mPoint4 = new PointClass();

                angle = 6.2831852 - angle;
                mPoint1.X = MaxX * Math.Cos(angle) + MaxY * Math.Sin(angle)+Center.X;
                mPoint1.Y = MaxX * (-1) * Math.Sin(angle) + MaxY * Math.Cos(angle)+Center.Y;
                ring.AddPoint(mPoint1, ref missing, ref missing);
                //PointCollection.AddPoint(mPoint1);

                mPoint2.X = MinX * Math.Cos(angle) + MaxY * Math.Sin(angle)+Center.X;
                mPoint2.Y = MinX * (-1) * Math.Sin(angle) + MaxY * Math.Cos(angle)+Center.Y;
                ring.AddPoint(mPoint2, ref missing, ref missing);
                //PointCollection.AddPoint(mPoint2);

                mPoint3.X = MinX * Math.Cos(angle) + MinY * Math.Sin(angle)+Center.X;
                mPoint3.Y = MinX * (-1) * Math.Sin(angle) + MinY * Math.Cos(angle)+Center.Y;
                ring.AddPoint(mPoint3, ref missing, ref missing);
                //PointCollection.AddPoint(mPoint3);

                mPoint4.X = MaxX * Math.Cos(angle) + MinY * Math.Sin(angle)+Center.X;
                mPoint4.Y = MaxX * (-1) * Math.Sin(angle) + MinY * Math.Cos(angle)+Center.Y;
                ring.AddPoint(mPoint4, ref missing, ref missing);
                ring.AddPoint(mPoint1, ref missing, ref missing);

                //PointCollection.AddPoint(mPoint4);
                //PointCollection.AddPoint(mPoint1);

                IGeometryCollection pointPolygon = new PolygonClass();
                pointPolygon.AddGeometry(ring as IGeometry, ref missing, ref missing);
                IPolygon MBR = pointPolygon as IPolygon;
                MBR.SimplifyPreserveFromTo();

                /*IGeometryCollection gPointCollection = PointCollection as IGeometryCollection;
                ISegmentCollection pRing = new RingClass();
                pRing.AddSegmentCollection(gPointCollection as ISegmentCollection);
                IGeometryCollection nPolygon = new PolygonClass();
                nPolygon.AddGeometry(pRing as IGeometry, ref missing, ref missing);
                IPolygon MBR =nPolygon as IPolygon;
                MBR.SimplifyPreserveFromTo();*/

                #endregion

                #region 计算绑定矩形面积
                double Area;
                Area = (MaxX - MinX) * (MaxY - MinY);
                #endregion

                #region 比较得到最小绑定矩形
                if (MinArea > Area)
                {                   
                    MinArea = Area;
                    MinOrientation = angle;
                    SMBR = MBR;
                }
                #endregion
            }

            return SMBR;
        }  

        /// <summary>
        /// 计算两点间距离
        /// </summary>
        /// <param name="Point1"></param>
        /// <param name="Point2"></param>
        /// <returns></returns>
        public double DistanceCompute(IPoint Point1, IPoint Point2)
        {
            double distance;

            distance = Math.Sqrt((Point2.X - Point1.X) * (Point2.X - Point1.X) + (Point2.Y - Point1.Y) * (Point2.Y - Point1.Y));

            return distance;
        }

        /// <summary>
        /// 计算多边形最长弦
        /// </summary>
        /// <param name="pFeature"></param>
        /// <returns></returns>
        public ILine GetTheLongestChord(IFeature pFeature)
        {
            IPointArray PointArray = GetPointsofDivide(pFeature);

            int NewPointArrayCount = PointArray.Count;
            double MaxDistance = 0;
            ILine LongestChord = new LineClass();

            #region 比较得到最长弦
            for (int i = 0; i < NewPointArrayCount; i++)
            {
                IPoint Point1 = PointArray.get_Element(i);

                for (int j = 0; j < NewPointArrayCount; j++)
                {

                    IPoint Point2 = PointArray.get_Element(j);

                    double distance = DistanceCompute(Point1, Point2);

                    if (MaxDistance < distance)
                    {
                        MaxDistance = distance;
                        LongestChord.FromPoint = Point1;
                        LongestChord.ToPoint = Point2;
                    }

                }
            }
            #endregion

            return LongestChord;
        }

        /// <summary>
        /// 计算最长弦
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <returns></returns>
        public ILine GetThelongestChord(IPolygon pPolygon)
        {
            IPointArray PointArray = GetPointsofDivide(pPolygon);

            int NewPointArrayCount = PointArray.Count;
            double MaxDistance = 0;
            ILine LongestChord = new LineClass();

            #region 比较得到最长弦
            for (int i = 0; i < NewPointArrayCount; i++)
            {
                IPoint Point1 = PointArray.get_Element(i);

                for (int j = 0; j < NewPointArrayCount; j++)
                {

                    IPoint Point2 = PointArray.get_Element(j);

                    double distance = DistanceCompute(Point1, Point2);

                    if (MaxDistance < distance)
                    {
                        MaxDistance = distance;
                        LongestChord.FromPoint = Point1;
                        LongestChord.ToPoint = Point2;
                    }

                }
            }
            #endregion

            return LongestChord;
        }

        #region 点分割（每条线段等距离分割成20个点）
        public IPointArray GetPointsofDivide(IFeature Feature)
        {
            IPointArray PointArray = GetPoints(Feature);

            int PointArrayCount = PointArray.Count;

            for (int i = 0; i < PointArrayCount; i++)
            {
                IPoint Point1 = new PointClass();
                IPoint Point2 = new PointClass();

                Point1 = PointArray.get_Element(i);

                if (i + 1 == PointArrayCount)
                {
                    Point2 = PointArray.get_Element(i + 1);
                }

                else
                {
                    Point2 = PointArray.get_Element(0);
                }

                double X1, X2;
                double Y1, Y2;

                X1 = Point1.X; X2 = Point2.X;
                Y1 = Point1.Y; Y2 = Point2.Y;

                IPoint Point3 = new PointClass();

                for (int j = 1; j < 20; j++)
                {
                    Point3.X = X1 * (20 - j) / 20 + X2 * j / 20;
                    Point3.Y = Y1 * (20 - j) / 20 + Y2 * j / 20;

                    PointArray.Add(Point3);
                }
            }

            return PointArray;
        }

        public IPointArray GetPointsofDivide(IPolygon pPolygon)
        {
            IPointArray PointArray = this.GetPoints(pPolygon);

            int PointArrayCount = PointArray.Count;

            for (int i = 0; i < PointArrayCount; i++)
            {
                IPoint Point1 = new PointClass();
                IPoint Point2 = new PointClass();

                Point1 = PointArray.get_Element(i);

                if (i + 1 == PointArrayCount)
                {
                    Point2 = PointArray.get_Element(i + 1);
                }

                else
                {
                    Point2 = PointArray.get_Element(0);
                }

                double X1, X2;
                double Y1, Y2;

                X1 = Point1.X; X2 = Point2.X;
                Y1 = Point1.Y; Y2 = Point2.Y;

                IPoint Point3 = new PointClass();

                for (int j = 1; j < 20; j++)
                {
                    Point3.X = X1 * (20 - j) / 20 + X2 * j / 20;
                    Point3.Y = Y1 * (20 - j) / 20 + Y2 * j / 20;

                    PointArray.Add(Point3);
                }
            }

            return PointArray;
        }
        #endregion

        #region 获得同等面积的圆目标(计算bottom measure)
        public IPolygon AreaToCircle(double area, IPoint Point, bool isCCW)
        {
            double pi = 3.1415926;
            ICircularArc circularArc = new CircularArcClass();
            IConstructCircularArc construtionCircularArc = circularArc as IConstructCircularArc;

            double r = Math.Sqrt(area / pi);

            construtionCircularArc.ConstructCircle(Point, r, isCCW);

            object missing = Type.Missing;
            ISegmentCollection pSegmentColl = new RingClass();
            pSegmentColl.AddSegment((ISegment)construtionCircularArc, ref missing, ref missing);
            IRing pRing = (IRing)pSegmentColl;
            pRing.Close(); //得到闭合的环
            IGeometryCollection pGeometryCollection = new PolygonClass();
            pGeometryCollection.AddGeometry(pRing, ref missing, ref missing); //环转面
            IPolygon pPolygon = (IPolygon)pGeometryCollection;

            return pPolygon;
        }
        #endregion

        #region 计算多边形最小外接圆用
        #region 计算多边形相距最远的两个顶点
        public ILine GetTheLongesLine(IFeature pFeature)
        {
            IPointArray PointArray = GetPoints(pFeature);
            ILine TheLongestLine = new LineClass();
            double MaxDistance = 0;

            int PointArrayCount = PointArray.Count;

            #region 比较得到最长弦
            for (int i = 0; i < PointArrayCount; i++)
            {
                IPoint Point1 = PointArray.get_Element(i);

                for (int j = 0; j < PointArrayCount; j++)
                {

                    IPoint Point2 = PointArray.get_Element(j);

                    double distance = DistanceCompute(Point1, Point2);

                    if (MaxDistance < distance)
                    {
                        MaxDistance = distance;
                        TheLongestLine.FromPoint = Point1;
                        TheLongestLine.ToPoint = Point2;
                    }

                }
            }
            #endregion

            return TheLongestLine;
        }
        #endregion

        #region 计算点到直线的距离
        public double GetTheDistanceFromPointToLine(IPoint Point, ILine Line)
        {
            IPoint Point1 = new PointClass();
            IPoint Point2 = new PointClass();

            Point1 = Line.FromPoint;
            Point2 = Line.ToPoint;

            double k = (Point1.Y - Point2.Y) / (Point1.X - Point2.X);
            double c = (Point2.X * Point1.Y - Point1.X * Point2.Y) / (Point2.X - Point1.X);

            double distance = Math.Abs(k * Point.X - Point.Y + c) / Math.Sqrt(1 + k * k);

            return distance;
        }
        #endregion

        /*#region 比较得到距直径最远的点
        public IPoint GetTheFarestPoint(IFeature Feature,ILine Line)
        {
            IPointArray PointArray = GetPoints(Feature);
            IPoint Point=new PointClass();
            //IPoint Point1 = Line.FromPoint;
            //IPoint Point2 = Line.ToPoint;

            //IPoint MidPoint = new PointClass();

            //MidPoint.X = (Point1.X + Point2.X) / 2;
            //MidPoint.Y = (Point1.Y + Point2.Y) / 2;

            int PointCount = PointArray.Count;
            double MaxDistance = 0;

            for (int i = 0; i < PointCount; i++)
            {
                IPoint Point1=PointArray.get_Element(i);
                double distance = GetTheDistanceFromPointToLine(Point1,Line);

                if (MaxDistance < distance)
                {
                    MaxDistance = distance;
                    Point = Point1;
                }
            }

            return Point;
        }
        #endregion*/

        #region 比较得到距长轴中点最远的点
        public IPoint GetTheFarestPoint1(IFeature Feature, ILine Line)
        {
            IPointArray PointArray = GetPoints(Feature);
            IPoint Point = new PointClass();
            IPoint Point1 = Line.FromPoint;
            IPoint Point2 = Line.ToPoint;

            IPoint MidPoint = new PointClass();

            MidPoint.X = (Point1.X + Point2.X) / 2;
            MidPoint.Y = (Point1.Y + Point2.Y) / 2;

            int PointCount = PointArray.Count;
            double MaxDistance = 0;

            for (int i = 0; i < PointCount; i++)
            {
                IPoint pPoint1 = PointArray.get_Element(i);

                double distance = DistanceCompute(pPoint1, MidPoint);

                if (pPoint1.X == Point1.X && pPoint1.Y == Point1.Y)
                {
                    distance = 0;
                }

                if (pPoint1.X == Point2.X && pPoint1.Y == Point2.Y)
                {
                    distance = 0;
                }


                if (MaxDistance < distance)
                {
                    MaxDistance = distance;
                    Point = pPoint1;
                }
            }

            return Point;
        }
        #endregion

        #region 计算两条直线的交点（无用）
        public IPoint GetTheCommonPointOfTwoLine(double k1, IPoint Point1, double k2, IPoint Point2)
        {
            double x = (Point1.Y - Point2.Y + k2 * Point2.X - k1 * Point1.X) / (k2 - k1);
            double y = (k2 * Point1.Y - k1 * Point2.Y + k1 * k2 * Point2.X - k1 * k2 * Point1.X) / (k2 - k1);

            IPoint CommomPoint = new PointClass();

            CommomPoint.X = x;
            CommomPoint.Y = y;

            return CommomPoint;
        }
        #endregion

        #region 计算三角形最小外接圆
        public Circle GetTheSmallestCircumcircleOfTriangle(IPoint Point, IPoint Point1, IPoint Point2)
        {
            IPoint CenterPoint = new PointClass();

            /*double k1 = (Point1.Y - Point2.Y) / (Point1.X - Point2.X);
            double k11 = 1 / k1 * (-1);
            IPoint MidPoint1 = new PointClass();
            MidPoint1.X = (Point1.X + Point2.X) / 2;
            MidPoint1.Y = (Point1.Y + Point2.Y) / 2;

            double k2 = (Point.Y - Point1.Y) / (Point.X - Point1.Y);
            double k22 = 1 / k2 * (-1);
            IPoint MidPoint2 = new PointClass();
            MidPoint2.X = (Point.X + Point1.X) / 2;
            MidPoint2.Y = (Point.Y + Point1.Y) / 2;

            CenterPoint = GetTheCommonPointOfTwoLine(k11, MidPoint1, k22, MidPoint2);*/

            CenterPoint.X = ((Point.X * Point.X - Point1.X * Point1.X + Point.Y * Point.Y - Point1.Y * Point1.Y) * (Point.Y - Point2.Y) - (Point.X * Point.X - Point2.X * Point2.X + Point.Y * Point.Y - Point2.Y * Point2.Y) * (Point.Y - Point1.Y)) / (2 * (Point.Y - Point2.Y) * (Point.X - Point1.X) - 2 * (Point.Y - Point1.Y) * (Point.X - Point2.X));
            CenterPoint.Y = ((Point.X * Point.X - Point1.X * Point1.X + Point.Y * Point.Y - Point1.Y * Point1.Y) * (Point.X - Point2.X) - (Point.X * Point.X - Point2.X * Point2.X + Point.Y * Point.Y - Point2.Y * Point2.Y) * (Point.X - Point1.X)) / (2 * (Point.Y - Point1.Y) * (Point.X - Point2.X) - 2 * (Point.Y - Point2.Y) * (Point.X - Point1.X));

            double r1 = Math.Sqrt((CenterPoint.Y - Point1.Y) * (CenterPoint.Y - Point1.Y) + (CenterPoint.X - Point1.X) * (CenterPoint.X - Point1.X));

            ICircularArc circularArc = new CircularArcClass();
            IConstructCircularArc construtionCircularArc = circularArc as IConstructCircularArc;

            construtionCircularArc.ConstructCircle(CenterPoint, r1, true);

            object missing = Type.Missing;
            ISegmentCollection pSegmentColl = new RingClass();
            pSegmentColl.AddSegment((ISegment)construtionCircularArc, ref missing, ref missing);
            IRing pRing = (IRing)pSegmentColl;
            pRing.Close(); //得到闭合的环
            IGeometryCollection pGeometryCollection = new PolygonClass();
            pGeometryCollection.AddGeometry(pRing, ref missing, ref missing); //环转面
            IPolygon pPolygon = (IPolygon)pGeometryCollection;

            Circle Circle1 = new Circle();

            Circle1.r = r1;
            Circle1.Center = CenterPoint;
            Circle1.Polygon = pPolygon;

            return Circle1;
        }
        #endregion

        #region 比较得到距圆心最远的点
        public IPoint GetTheFarestPointToTheCenter(Circle circle1, IFeature Feature)
        {
            IPointArray PointArray = GetPoints(Feature);
            IPoint Center = circle1.Center;
            int PointCount = PointArray.Count;

            IPoint FarestPoint = new PointClass();
            double MaxDistance = 0;

            for (int i = 0; i < PointCount; i++)
            {
                IPoint Point1 = PointArray.get_Element(i);

                double distance = DistanceCompute(Center, Point1);

                if (MaxDistance < distance)
                {
                    MaxDistance = distance;
                    FarestPoint = Point1;
                }
            }

            return FarestPoint;
        }
        #endregion

        #region 获得距离三角形顶点最近的顶点
        public IPointArray GetTheClosePoint(IPoint Point, IPoint Point1, IPoint Point2, IPoint Point3)
        {
            double distance1 = DistanceCompute(Point, Point1);
            double distance2 = DistanceCompute(Point, Point2);
            double distance3 = DistanceCompute(Point, Point3);

            double MinDistance = 1000000;
            IPoint tPoint = new PointClass();
            IPointArray PointArray = new PointArrayClass();
            PointArray.Add(Point1);
            PointArray.Add(Point2);
            PointArray.Add(Point3);

            if (distance1 < MinDistance)
            {
                MinDistance = distance1;
                tPoint = Point1;
            }

            if (distance2 < MinDistance)
            {
                MinDistance = distance2;
                tPoint = Point2;
            }

            if (distance3 < MinDistance)
            {
                MinDistance = distance3;
                tPoint = Point3;
            }

            for (int i = 0; i < 3; i++)
            {
                IPoint dPoint = PointArray.get_Element(i);

                if (tPoint == dPoint)
                {
                    PointArray.Remove(i);
                }
            }

            PointArray.Add(tPoint);

            return PointArray;
        }
        #endregion
        #endregion

        #region 计算boyceClarkMeasure
        #region 知道一点和斜率，计算直线
        public IPolyline LineCompute(IPoint Point, double Angle)
        {
            double x = Point.X;
            double y = Point.Y;

            double k = Math.Tan(Angle);

            IPoint Point1 = new PointClass();
            IPoint Point2 = new PointClass();

            Point1.X = x + 3000; Point1.Y = y + 3000 * k;
            Point2.X = x - 3000; Point2.Y = y - 3000 * k;

            IPolyline PolyLine = new PolylineClass();
            PolyLine.FromPoint = Point1;
            PolyLine.ToPoint = Point2;

            return PolyLine;
        }
        #endregion

        #region 提取polygon中的直线（变为Line）
        public List<ILine> GetTheLinesOfPolygon(IPolygon pPolygon)
        {
            IPointCollection tPointCollection = pPolygon as IPointCollection;
            List<ILine> LineList = new List<ILine>();

            for (int k = 0; k < tPointCollection.PointCount - 1; k++)
            {
                IPoint tPoint1 = tPointCollection.get_Point(k);
                IPoint tPoint2 = tPointCollection.get_Point(k + 1);

                ILine Line = new LineClass();
                Line.FromPoint = tPoint1;
                Line.ToPoint = tPoint2;

                LineList.Add(Line);
            }

            return LineList;
        }
        #endregion

        #region 公式计算
        public double GetTheBoyceClarkMeasure(List<IPolyline> PolylineList)
        {
            int PolyLineListCount = PolylineList.Count;
            double SumofRadius = 0;

            for (int i = 0; i < PolyLineListCount; i++)
            {
                IPolyline Polyline = PolylineList[i];
                double Length = Polyline.Length;
                SumofRadius = SumofRadius + Length;
            }

            double mSum = 0;
            for (int i = 0; i < PolyLineListCount; i++)
            {
                IPolyline Polyline = PolylineList[i];
                double Length = Polyline.Length;
                double m = Math.Abs(Length * 100 / SumofRadius - 100 / 60);
                mSum = mSum + m;
            }

            double BoyceClarkMeasure = 1 - mSum / 200;
            return BoyceClarkMeasure;
        }
        #endregion
        #endregion

        /// <summary>
        /// 计算最小绑定矩形方向(0,180)
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <returns></returns>
        public double GetSMBROrientation(IPolygon pPolygon)
        {
            IPolygon SMBR = this.GetSMBR(pPolygon);
            IPointArray SMBRPoints = GetPoints(SMBR);
            IPoint Point1 = SMBRPoints.get_Element(1);
            IPoint Point2 = SMBRPoints.get_Element(2);
            IPoint Point3 = SMBRPoints.get_Element(3);

            ILine Line1 = new LineClass(); Line1.FromPoint = Point1; Line1.ToPoint = Point2;
            ILine Line2 = new LineClass(); Line2.FromPoint = Point2; Line2.ToPoint = Point3;

            double Length1 = Line1.Length; double Length2 = Line2.Length; double tAngle = 0;

            if (Length1 > Length2)
            {
                double Angle = Line1.Angle;

                if (Angle < 0)
                {
                    Angle = Angle + 3.1415926;
                }

                tAngle = Angle / 3.1415926 * 180;
            }

            else
            {
                double Angle = Line2.Angle;

                if (Angle < 0)
                {
                    Angle = Angle + 3.1415926;
                }

                tAngle = Angle / 3.1415926 * 180;
            }

            return tAngle;
        }

        /// <summary>
        /// 计算最小绑定矩形方法
        /// </summary>
        /// <param name="PolygonObject"></param>
        /// <returns></returns>
        public double GetSMBROrientation(PolygonObject Po)
        {
            #region 将Po转换为pPolygon
            IPolygon pPolygon = PolygonObjectConvert(Po);
            #endregion

            #region 计算绑定矩形方向
            IPolygon SMBR = this.GetSMBR(pPolygon);
            IPointArray SMBRPoints = GetPoints(SMBR);
            IPoint Point1 = SMBRPoints.get_Element(1);
            IPoint Point2 = SMBRPoints.get_Element(2);
            IPoint Point3 = SMBRPoints.get_Element(3);

            ILine Line1 = new LineClass(); Line1.FromPoint = Point1; Line1.ToPoint = Point2;
            ILine Line2 = new LineClass(); Line2.FromPoint = Point2; Line2.ToPoint = Point3;

            double Length1 = Line1.Length; double Length2 = Line2.Length; double tAngle = 0;

            if (Length1 > Length2)
            {
                double Angle = Line1.Angle;

                if (Angle < 0)
                {
                    Angle = Angle + 3.1415926;
                }

                tAngle = Angle / 3.1415926 * 180;
            }

            else
            {
                double Angle = Line2.Angle;

                if (Angle < 0)
                {
                    Angle = Angle + 3.1415926;
                }

                tAngle = Angle / 3.1415926 * 180;
            }
            #endregion

            return tAngle;
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

        /// <summary>
        /// 将polygonobject转换成polygon
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
        /// 获得两个建筑物的正对面积比
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <returns></returns>
        public double GetFaceRatio(PolygonObject Po1, PolygonObject Po2)
        {
            double FaceRatio = 0;

            #region 获得绑定矩形
            IPolygon IPo1 = this.ObjectConvert(Po1);
            IPolygon IPo2 = this.ObjectConvert(Po2);
            IPolygon smbr1 = this.GetSMBR(IPo1);
            IPolygon smbr2 = this.GetSMBR(IPo2);
            #endregion

            #region 获得两建筑物smbr的四条边
            IPointArray SMBRPoints = GetPoints(smbr1);
            IPoint Point1 = SMBRPoints.get_Element(1);
            IPoint Point2 = SMBRPoints.get_Element(2);
            IPoint Point3 = SMBRPoints.get_Element(3);

            IPolyline Line1 = new PolylineClass(); Line1.FromPoint = Point1; Line1.ToPoint = Point2;
            IPolyline Line2 = new PolylineClass(); Line2.FromPoint = Point2; Line2.ToPoint = Point3;

            IPointArray SMBRPoints2 = GetPoints(smbr2);
            IPoint Point21 = SMBRPoints2.get_Element(1);
            IPoint Point22 = SMBRPoints2.get_Element(2);
            IPoint Point23 = SMBRPoints2.get_Element(3);

            IPolyline Line21 = new PolylineClass(); Line21.FromPoint = Point21; Line21.ToPoint = Point22;
            IPolyline Line22 = new PolylineClass(); Line22.FromPoint = Point22; Line22.ToPoint = Point23;
            #endregion

            #region 转化成TriNode
            TriNode P1 = new TriNode(Point1.X, Point2.Y);
            TriNode P2 = new TriNode(Point2.X, Point2.Y);
            TriNode P3 = new TriNode(Point3.X, Point3.Y);

            TriNode P21 = new TriNode(Point21.X, Point22.Y);
            TriNode P22 = new TriNode(Point22.X, Point22.Y);
            TriNode P23 = new TriNode(Point23.X, Point23.Y);
            #endregion

            #region 获得最大的faceratio
            double FaceRatio1 = this.EdgeRatio(P1, P2, P21, P22);
            double FaceRatio2 = this.EdgeRatio(P1, P2, P22, P23);
            double FaceRatio3 = this.EdgeRatio(P2, P3, P21, P22);
            double FaceRatio4 = this.EdgeRatio(P2, P3, P22, P23);

            FaceRatio = FaceRatio1;
            if (FaceRatio < FaceRatio2)
            {
                FaceRatio = FaceRatio2;
            }
            if (FaceRatio < FaceRatio3)
            {
                FaceRatio = FaceRatio3;
            }
            if (FaceRatio < FaceRatio4)
            {
                FaceRatio = FaceRatio4;
            }
            #endregion

            return FaceRatio;
        }

        /// <summary>
        /// 计算给定两边的正对比
        /// </summary>
        /// <param name="P1"></param>
        /// <param name="p2"></param>
        /// <param name="sp"></param>
        /// <param name="ep"></param>
        /// <returns></returns>
        public double EdgeRatio(TriNode P1,TriNode P2,TriNode sp,TriNode ep)
        {
            double FaceRatio = 0;

            #region 计算垂足
            TriNode perP1 = this.CalMinDisPoint2Line(P1, sp, ep);
            TriNode perP2 = this.CalMinDisPoint2Line(P2, sp, ep);
            #endregion

            #region 判断垂足与线段关系
            double MaxX = ep.X; double MinX = sp.X; TriNode Lowp = sp; TriNode Highp = ep;
            if (ep.X < sp.X)
            {
                MaxX = sp.X; MinX = ep.X;
                Lowp = ep; Highp = sp;
            }

            int P1Lable = 0; int P2Lable = 0;//1表示在线段内；2表示在线段下方；3表示在选段上方
            if (perP1.X >= MinX && perP1.X <= MaxX)
            {
                P1Lable = 1;
            }

            else if (perP1.X < MinX)
            {
                P1Lable = 2;
            }

            else if (perP1.X > MaxX)
            {
                P1Lable = 3;
            }

            if (perP2.X >= MinX && perP2.X <= MaxX)
            {
                P2Lable = 1;
            }

            else if (perP2.X < MinX)
            {
                P2Lable = 2;
            }

            else if (perP2.X > MaxX)
            {
                P2Lable = 3;
            }
            #endregion

            #region 计算FaceRatio
            if ((P1Lable == 2 && P2Lable == 2) || (P1Lable == 3 && P2Lable == 3))//两个点均不在线段内；且在同一侧
            {
                FaceRatio = 0;
            }

            else if (P1Lable == 1 && P2Lable == 1)//两个点均在线段内
            {
                double Distance1 = Math.Sqrt((perP1.X - perP2.X) * (perP1.X - perP2.X) + (perP1.Y - perP2.Y) * (perP1.Y - perP2.Y));
                double Distance2 = Math.Sqrt((Lowp.X - Highp.X) * (Lowp.X - Highp.X) + (Lowp.Y - Highp.Y) * (Lowp.Y - Highp.Y));
                FaceRatio = Distance1 / Distance2;
            }

            else if ((P1Lable == 2 && P2Lable == 3) || (P1Lable == 3 && P2Lable == 2))//线段在垂足表示的线段内（两个点均不在线段内）
            {

                double Distance1 = Math.Sqrt((perP1.X - perP2.X) * (perP1.X - perP2.X) + (perP1.Y - perP2.Y) * (perP1.Y - perP2.Y));
                double Distance2 = Math.Sqrt((Lowp.X - Highp.X) * (Lowp.X - Highp.X) + (Lowp.Y - Highp.Y) * (Lowp.Y - Highp.Y));
                FaceRatio = Distance2 / Distance1;
            }

            else if (P1Lable == 1 && P2Lable == 2)
            {
                double Distance1 = Math.Sqrt((Lowp.X - perP1.X) * (Lowp.X - perP1.X) + (Lowp.Y - perP1.Y) * (Lowp.Y - perP1.Y));
                double Distance2 = Math.Sqrt((Highp.X - perP2.X) * (Highp.X - perP2.X) + (Highp.Y - perP2.Y) * (Highp.Y - perP2.Y));
                FaceRatio = Distance1 / Distance2;
            }

            else if (P1Lable == 1 && P2Lable == 3)
            {
                double Distance1 = Math.Sqrt((Highp.X - perP1.X) * (Highp.X - perP1.X) + (Highp.Y - perP1.Y) * (Highp.Y - perP1.Y));
                double Distance2 = Math.Sqrt((Lowp.X - perP2.X) * (Lowp.X - perP2.X) + (Lowp.Y - perP2.Y) * (Lowp.Y - perP2.Y));
                FaceRatio = Distance1 / Distance2;
            }

            else if (P2Lable == 1 && P1Lable == 2)
            {
                double Distance1 = Math.Sqrt((Lowp.X - perP2.X) * (Lowp.X - perP2.X) + (Lowp.Y - perP2.Y) * (Lowp.Y - perP2.Y));
                double Distance2 = Math.Sqrt((Highp.X - perP1.X) * (Highp.X - perP1.X) + (Highp.Y - perP1.Y) * (Highp.Y - perP1.Y));
                FaceRatio = Distance1 / Distance2;
            }

            else if (P2Lable == 1 && P1Lable == 3)
            {
                double Distance1 = Math.Sqrt((Highp.X - perP2.X) * (Highp.X - perP2.X) + (Highp.Y - perP2.Y) * (Highp.Y - perP2.Y));
                double Distance2 = Math.Sqrt((Lowp.X - perP1.X) * (Lowp.X - perP1.X) + (Lowp.Y - perP1.Y) * (Lowp.Y - perP1.Y));
                FaceRatio = Distance1 / Distance2;
            }
            #endregion

            return FaceRatio;
        }

        /// <summary>
        /// 求点到某两点的垂足;并输出点在线段内还是线段外;true=在内；false=在外
        /// /// </summary>
        /// <param name="v1">顶点</param>
        /// <param name="v2">底边顶点1</param>
        /// <param name="v3">底边顶点2</param>
        /// <param name="nearestPoint">最近距离</param>
        /// <param name="v4">最近距离对应的点</param>
        /// <param name="isPerpendicular">最近距离是否是沿着垂线上</param>
        /// <returns>最小距离</returns>
        public TriNode CalMinDisPoint2Line(TriNode p, TriNode s, TriNode e)
        {
            double x = 0, y = 0; 
            if ((e.X - s.X) == 0 && (e.Y - s.Y) != 0)//平行于y轴
            {
                x = e.X;
                y = p.Y;
            }
            else if ((e.Y - s.Y) == 0 && (e.X - s.X) != 0)//平行于X轴
            {
                x = p.X;
                y = e.Y;
            }
            else if ((e.Y - s.Y) == 0 && (e.X - s.X) == 0)
            {
                x = e.X;
                y = e.Y;
            }
            else if ((e.Y - s.Y) != 0 && (e.X - s.X) != 0)
            {
                double k = (e.Y - s.Y) / (e.X - s.X);
                x = (k * p.Y - k * s.Y + p.X + k * k * s.X) / (1 + k * k);
                y = (k * p.X - k * s.X + s.Y + k * k * p.Y) / (1 + k * k);
            }

            return new TriNode(x, y);
        }
    }
}
