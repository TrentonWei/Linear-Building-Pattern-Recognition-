using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AuxStructureLib;
using AuxStructureLib.IO;
using ESRI.ArcGIS.Geometry;

using ESRI.ArcGIS.Controls;

namespace PrDispalce.典型化
{
    public class Symbolization
    {
        #region 参数
        PrDispalce.工具类.Symbolization SB = new 工具类.Symbolization();//测试可视化用
        #endregion

        /// <summary>
        /// 返回符号化的建筑物
        /// </summary>
        /// <param name="Polygon"></param>
        /// <param name="Scale"></param>
        /// Label true表示符号化；false表示不符号化
        /// <returns></returns>
        public PolygonObject SymbolizedPolygon(PolygonObject Polygon, double Scale, double lLength, double sLength,out bool Label)
        {
            Label = false;

            PrDispalce.工具类.ParameterCompute parametercompute = new 工具类.ParameterCompute();
            //MultipleLevelDisplace Md = new MultipleLevelDisplace();

            IPolygon pPolygon = PolygonObjectConvert(Polygon);
            IPolygon SMBR = parametercompute.GetSMBR(pPolygon);
            //pMapControl.DrawShape(SMBR, ref PolygonSymbol);
            //pMapControl.DrawShape(pPolygon, ref PolygonSymbol);

            #region 计算SMBR的方向
            IArea pArea = pPolygon as IArea;
            IPoint pCenterPoint = pArea.Centroid;
            IArea sArea = SMBR as IArea;
            IPoint CenterPoint = sArea.Centroid;

            IPointCollection sPointCollection = SMBR as IPointCollection;
            IPoint Point1 = sPointCollection.get_Point(1);
            IPoint Point2 = sPointCollection.get_Point(2);
            IPoint Point3 = sPointCollection.get_Point(3);
            IPoint Point4 = sPointCollection.get_Point(4);

            ILine Line1 = new LineClass(); Line1.FromPoint = Point1; Line1.ToPoint = Point2;
            ILine Line2 = new LineClass(); Line2.FromPoint = Point2; Line2.ToPoint = Point3;

            double Length1 = Line1.Length; double Length2 = Line2.Length; double Angle = 0;

            double LLength = 0; double SLength = 0;

            IPoint oPoint1 = new PointClass(); IPoint oPoint2 = new PointClass();
            IPoint oPoint3 = new PointClass(); IPoint oPoint4 = new PointClass();

            if (Length1 > Length2)
            {
                Angle = Line1.Angle;
                LLength = Length1; SLength = Length2;
            }

            else
            {
                Angle = Line2.Angle;
                LLength = Length2; SLength = Length1;
            }
            #endregion

            #region 对不能依比例尺符号化的SMBR旋转后符号化
            if (LLength < lLength * Scale / 1000 || SLength < sLength * Scale / 1000)
            {
                if (LLength < lLength * Scale / 1000)
                {
                    LLength = lLength * Scale / 1000;
                }

                if (SLength < sLength * Scale / 1000)
                {
                    SLength = sLength * Scale / 1000;
                }

                IEnvelope pEnvelope = new EnvelopeClass();
                pEnvelope.XMin = CenterPoint.X - LLength / 2;
                pEnvelope.YMin = CenterPoint.Y - SLength / 2;
                pEnvelope.Width = LLength; pEnvelope.Height = SLength;

                #region 将SMBR旋转回来
                TriNode pNode1 = new TriNode(); TriNode pNode2 = new TriNode();
                TriNode pNode3 = new TriNode(); TriNode pNode4 = new TriNode();

                pNode1.X = (pEnvelope.XMin - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMin - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode1.Y = (pEnvelope.XMin - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMin - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                pNode2.X = (pEnvelope.XMax - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMin - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode2.Y = (pEnvelope.XMax - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMin - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                pNode3.X = (pEnvelope.XMax - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMax - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode3.Y = (pEnvelope.XMax - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMax - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                pNode4.X = (pEnvelope.XMin - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMax - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode4.Y = (pEnvelope.XMin - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMax - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                List<TriNode> TList = new List<TriNode>();
                TList.Add(pNode1); TList.Add(pNode2); TList.Add(pNode3); TList.Add(pNode4); //TList.Add(pNode1);
                PolygonObject pPolygonObject = new PolygonObject(Polygon.ID, TList);//ID标识建筑物的标号
                #endregion

                #region 新建筑物属性赋值
                pPolygonObject.ClassID = Polygon.ClassID;
                pPolygonObject.ID = Polygon.ID;
                pPolygonObject.TagID = Polygon.TagID;
                pPolygonObject.TypeID = Polygon.TypeID;
                #endregion

                #region 将建筑物移到原建筑物中心
                double dx = pCenterPoint.X - pPolygonObject.CalProxiNode().X;
                double dy = pCenterPoint.Y - pPolygonObject.CalProxiNode().Y;

                for (int i = 0; i < pPolygonObject.PointList.Count; i++)
                {
                    pPolygonObject.PointList[i].X = pPolygonObject.PointList[i].X + dx;
                    pPolygonObject.PointList[i].Y = pPolygonObject.PointList[i].Y + dy;
                }
                #endregion

                Label = true;
                pPolygonObject.SourceTemp = 1;//1表示是符号化
                return pPolygonObject;
            }
            #endregion

            else
            {
                return Polygon;
            }
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
        /// 对一个群进行符号化,即对建筑物的方向进行调整
        /// </summary>
        /// <param name="map"></param>
        /// <param name="Polygon"></param>
        /// <param name="Scale"></param>
        /// <param name="lLength"></param>
        /// <param name="sLength"></param>
        /// <param name="LocationLabel">=1：建筑物的原位置；=2多个建筑物的平均位置</param>
        /// <returns></returns>
        public PolygonObject SymbolizedPolygon(AxMapControl pMapControl, SMap map, PolygonObject Polygon, double Scale, double lLength, double sLength, int LocationLabel, out IPolygon ConvexPolygon)
        {
            ConvexPolygon = new PolygonClass();
            PrDispalce.工具类.ParameterCompute parametercompute = new 工具类.ParameterCompute();
            //MultipleLevelDisplace Md = new MultipleLevelDisplace();

            #region 如果是一个建筑物
            if (Polygon.TyList.Count == 0 || Polygon.PatternID>0)
            {
                IPolygon pPolygon = PolygonObjectConvert(Polygon);
                IPolygon SMBR = parametercompute.GetSMBR(pPolygon);
                //pMapControl.DrawShape(SMBR, ref PolygonSymbol);
                //pMapControl.DrawShape(pPolygon, ref PolygonSymbol);

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

                double Length1 = Line1.Length; double Length2 = Line2.Length; double Angle = 0;

                double LLength = 0; double SLength = 0;

                IPoint oPoint1 = new PointClass(); IPoint oPoint2 = new PointClass();
                IPoint oPoint3 = new PointClass(); IPoint oPoint4 = new PointClass();

                if (Length1 > Length2)
                {
                    Angle = Line1.Angle;
                    LLength = Length1; SLength = Length2;
                }

                else
                {
                    Angle = Line2.Angle;
                    LLength = Length2; SLength = Length1;
                }
                #endregion

                #region 对不能依比例尺符号化的SMBR旋转后符号化
                if (LLength < lLength * Scale / 1000 || SLength < sLength * Scale / 1000)
                {
                    if (LLength < lLength * Scale / 1000)
                    {
                        LLength = lLength * Scale / 1000;
                    }

                    if (SLength < sLength * Scale / 1000)
                    {
                        SLength = sLength * Scale / 1000;
                    }

                    IEnvelope pEnvelope = new EnvelopeClass();
                    pEnvelope.XMin = CenterPoint.X - LLength / 2;
                    pEnvelope.YMin = CenterPoint.Y - SLength / 2;
                    pEnvelope.Width = LLength; pEnvelope.Height = SLength;

                    #region 将SMBR旋转回来
                    TriNode pNode1 = new TriNode(); TriNode pNode2 = new TriNode();
                    TriNode pNode3 = new TriNode(); TriNode pNode4 = new TriNode();

                    pNode1.X = (pEnvelope.XMin - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMin - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                    pNode1.Y = (pEnvelope.XMin - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMin - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                    pNode2.X = (pEnvelope.XMax - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMin - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                    pNode2.Y = (pEnvelope.XMax - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMin - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                    pNode3.X = (pEnvelope.XMax - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMax - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                    pNode3.Y = (pEnvelope.XMax - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMax - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                    pNode4.X = (pEnvelope.XMin - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMax - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                    pNode4.Y = (pEnvelope.XMin - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMax - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                    List<TriNode> TList = new List<TriNode>();
                    TList.Add(pNode1); TList.Add(pNode2); TList.Add(pNode3); TList.Add(pNode4); //TList.Add(pNode1);
                    PolygonObject pPolygonObject = new PolygonObject(Polygon.ID, TList);//ID标识建筑物的标号
                    #endregion

                    #region 新建筑物属性赋值
                    pPolygonObject.ClassID = Polygon.ClassID;
                    pPolygonObject.ID = Polygon.ID;
                    pPolygonObject.TagID = Polygon.TagID;
                    pPolygonObject.TypeID = Polygon.TypeID;
                    #endregion

                    return pPolygonObject;
                }
                #endregion

                else
                {
                    return Polygon;
                }
            }
            #endregion

            #region 如果是多个建筑物
            else
            {
                #region 求多个建筑物的凸包
                IPointCollection pPointCollection = new MultipointClass();

                PolygonObject nPolygonObject = map.GetObjectbyID(Polygon.ID, FeatureType.PolygonType) as PolygonObject;
                for (int i = 0; i < nPolygonObject.PointList.Count; i++)
                {
                    IPoint Point = new PointClass();
                    Point.X = nPolygonObject.PointList[i].X;
                    Point.Y = nPolygonObject.PointList[i].Y;

                    pPointCollection.AddPoint(Point);
                }

                for (int i = 0; i < Polygon.TyList.Count; i++)
                {
                    PolygonObject pPolygonObject = map.GetObjectbyID(Polygon.TyList[i], FeatureType.PolygonType) as PolygonObject;

                    if (pPolygonObject != null)
                    {
                        for (int j = 0; j < pPolygonObject.PointList.Count; j++)
                        {
                            IPoint Point = new PointClass();
                            Point.X = pPolygonObject.PointList[j].X;
                            Point.Y = pPolygonObject.PointList[j].Y;

                            pPointCollection.AddPoint(Point);
                        }
                    }
                }

                ITopologicalOperator iTopo = pPointCollection as ITopologicalOperator;
                IGeometry pConvexHull = iTopo.ConvexHull();
                ConvexPolygon = pConvexHull as IPolygon;

                IPolygon cPolygon = pConvexHull as IPolygon;
                object PolygonSymbol = SB.PolygonSymbolization(3, 100, 100, 100, 0, 0, 0, 0);
                pMapControl.DrawShape(cPolygon, ref PolygonSymbol);
                #endregion

                IPolygon SMBR = parametercompute.GetSMBR(cPolygon);

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

                double Length1 = Line1.Length; double Length2 = Line2.Length; double Angle = 0;

                double LLength = 0; double SLength = 0;

                IPoint oPoint1 = new PointClass(); IPoint oPoint2 = new PointClass();
                IPoint oPoint3 = new PointClass(); IPoint oPoint4 = new PointClass();

                if (Length1 > Length2)
                {
                    Angle = Line1.Angle;
                    LLength = Length1; SLength = Length2;
                }

                else
                {
                    Angle = Line2.Angle;
                    LLength = Length2; SLength = Length1;
                }
                #endregion

                #region 对不能依比例尺符号化的SMBR旋转后符号化;并调整位置(多个建筑物的重心位置)          
                if (LocationLabel==2)
                {              
                    IEnvelope pEnvelope = new EnvelopeClass();
                    pEnvelope.XMin = CenterPoint.X - lLength * Scale / 2000;
                    pEnvelope.YMin = CenterPoint.Y - sLength * Scale / 2000;
                    pEnvelope.Width = lLength * Scale / 1000; pEnvelope.Height = sLength * Scale / 1000;

                    #region 将SMBR旋转回来
                    TriNode pNode1 = new TriNode(); TriNode pNode2 = new TriNode();
                    TriNode pNode3 = new TriNode(); TriNode pNode4 = new TriNode();

                    pNode1.X = (pEnvelope.XMin - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMin - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                    pNode1.Y = (pEnvelope.XMin - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMin - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                    pNode2.X = (pEnvelope.XMax - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMin - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                    pNode2.Y = (pEnvelope.XMax - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMin - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                    pNode3.X = (pEnvelope.XMax - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMax - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                    pNode3.Y = (pEnvelope.XMax - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMax - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                    pNode4.X = (pEnvelope.XMin - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMax - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                    pNode4.Y = (pEnvelope.XMin - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMax - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                    List<TriNode> TList = new List<TriNode>();
                    TList.Add(pNode1); TList.Add(pNode2); TList.Add(pNode3); TList.Add(pNode4); //TList.Add(pNode1);
                    PolygonObject pPolygonObject = new PolygonObject(Polygon.ID, TList);//ID标识建筑物的标号
                    #endregion

                    #region 新建筑物属性赋值
                    pPolygonObject.ClassID = Polygon.ClassID;
                    pPolygonObject.ID = Polygon.ID;
                    pPolygonObject.TagID = Polygon.TagID;
                    pPolygonObject.TypeID = Polygon.TypeID;
                    #endregion

                    return pPolygonObject;
                }


                #endregion

                #region 对不能依比例尺符号化的SMBR旋转后符号化;并调整位置(给定建筑物的重心位置)
                else if (LocationLabel == 1)
                {
                    IEnvelope pEnvelope = new EnvelopeClass();
                    pEnvelope.XMin = Polygon.CalProxiNode().X - lLength * Scale / 2000;
                    pEnvelope.YMin = Polygon.CalProxiNode().Y - sLength * Scale / 2000;
                    pEnvelope.Width = lLength * Scale / 1000; pEnvelope.Height = sLength*Scale/1000;

                    #region 将SMBR旋转回来
                    TriNode pNode1 = new TriNode(); TriNode pNode2 = new TriNode();
                    TriNode pNode3 = new TriNode(); TriNode pNode4 = new TriNode();

                    pNode1.X = (pEnvelope.XMin - Polygon.CalProxiNode().X) * Math.Cos(Angle) - (pEnvelope.YMin - Polygon.CalProxiNode().Y) * Math.Sin(Angle) + Polygon.CalProxiNode().X;
                    pNode1.Y = (pEnvelope.XMin - Polygon.CalProxiNode().X) * Math.Sin(Angle) + (pEnvelope.YMin - Polygon.CalProxiNode().Y) * Math.Cos(Angle) + Polygon.CalProxiNode().Y;

                    pNode2.X = (pEnvelope.XMax - Polygon.CalProxiNode().X) * Math.Cos(Angle) - (pEnvelope.YMin - Polygon.CalProxiNode().Y) * Math.Sin(Angle) + Polygon.CalProxiNode().X;
                    pNode2.Y = (pEnvelope.XMax - Polygon.CalProxiNode().X) * Math.Sin(Angle) + (pEnvelope.YMin - Polygon.CalProxiNode().Y) * Math.Cos(Angle) + Polygon.CalProxiNode().Y;

                    pNode3.X = (pEnvelope.XMax - Polygon.CalProxiNode().X) * Math.Cos(Angle) - (pEnvelope.YMax - Polygon.CalProxiNode().Y) * Math.Sin(Angle) + Polygon.CalProxiNode().X;
                    pNode3.Y = (pEnvelope.XMax - Polygon.CalProxiNode().X) * Math.Sin(Angle) + (pEnvelope.YMax - Polygon.CalProxiNode().Y) * Math.Cos(Angle) + Polygon.CalProxiNode().Y;

                    pNode4.X = (pEnvelope.XMin - Polygon.CalProxiNode().X) * Math.Cos(Angle) - (pEnvelope.YMax - Polygon.CalProxiNode().Y) * Math.Sin(Angle) + Polygon.CalProxiNode().X;
                    pNode4.Y = (pEnvelope.XMin - Polygon.CalProxiNode().X) * Math.Sin(Angle) + (pEnvelope.YMax - Polygon.CalProxiNode().Y) * Math.Cos(Angle) + Polygon.CalProxiNode().Y;

                    List<TriNode> TList = new List<TriNode>();
                    TList.Add(pNode1); TList.Add(pNode2); TList.Add(pNode3); TList.Add(pNode4); //TList.Add(pNode1);
                    PolygonObject pPolygonObject = new PolygonObject(Polygon.ID, TList);//ID标识建筑物的标号
                    #endregion

                    #region 新建筑物属性赋值
                    pPolygonObject.ClassID = Polygon.ClassID;
                    pPolygonObject.ID = Polygon.ID;
                    pPolygonObject.TagID = Polygon.TagID;
                    pPolygonObject.TypeID = Polygon.TypeID;
                    #endregion

                    return pPolygonObject;
                }
                #endregion

                else
                {
                    return Polygon;
                }
            }
            #endregion
        }
    }
}
