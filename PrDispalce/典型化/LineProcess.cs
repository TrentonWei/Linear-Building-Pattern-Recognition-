using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using AuxStructureLib;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Controls;

namespace PrDispalce.典型化
{
    class LineProcess
    {
        /// <summary>
        /// 给两条直线插点(相交或者需要相交的是直线插点)
        /// </summary>
        /// <param name="Line1"></param>
        /// <param name="Line2"></param>
        /// <returns></returns>
        public void InsertPointComputation(Line Line1,Line Line2,double Distance,double OrientationConstraint,AxMapControl mMapControl)
        {
            int RelationLabel=RelationComputation(Line1, Line2, Distance, OrientationConstraint,mMapControl);
            
            #region 相交关系 
            if (RelationLabel == 1)
            {
                #region 重新创建两条新线段
                IPolyline pLine1 = new PolylineClass();
                IPoint fPoint1 = new PointClass(); fPoint1.X = Line1.BoundaryPointList[0].X; fPoint1.Y = Line1.BoundaryPointList[0].Y;
                IPoint tPoint1 = new PointClass(); tPoint1.X = Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].X; tPoint1.Y = Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].Y;
                pLine1.FromPoint = fPoint1; pLine1.ToPoint = tPoint1;

                IPolyline pLine2 = new PolylineClass();
                IPoint fPoint2 = new PointClass(); fPoint2.X = Line2.BoundaryPointList[0].X; fPoint2.Y = Line2.BoundaryPointList[0].Y;
                IPoint tPoint2 = new PointClass(); tPoint2.X = Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].X; tPoint2.Y = Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].Y;
                pLine2.FromPoint = fPoint2; pLine2.ToPoint = tPoint2;
                #endregion

                ITopologicalOperator iT1 = pLine1 as ITopologicalOperator;
                IGeometry IntersectGeometry = iT1.Intersect(pLine2,esriGeometryDimension.esriGeometry0Dimension);
                IPointCollection Pc = IntersectGeometry as IPointCollection;
                IPoint IntersectPoint = Pc.get_Point(0);

                Point IPoint = new Point(IntersectPoint.X, IntersectPoint.Y);

                Line1.IntersectPointList.Add(IPoint);
                Line2.IntersectPointList.Add(IPoint);
            }
            #endregion

            #region 非相交，需要连接
            else if (RelationLabel == 3)
            {
                #region 重新创建两条新线段
                ILine pLine1 = new LineClass();
                IPoint fPoint1 = new PointClass(); fPoint1.X = Line1.BoundaryPointList[0].X; fPoint1.Y = Line1.BoundaryPointList[0].Y;
                IPoint tPoint1 = new PointClass(); tPoint1.X = Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].X; tPoint1.Y = Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].Y;
                pLine1.FromPoint = fPoint1; pLine1.ToPoint = tPoint1;

                ILine pLine2 = new LineClass();
                IPoint fPoint2 = new PointClass(); fPoint2.X = Line2.BoundaryPointList[0].X; fPoint2.Y = Line2.BoundaryPointList[0].Y;
                IPoint tPoint2 = new PointClass(); tPoint2.X = Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].X; tPoint2.Y = Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].Y;
                pLine2.FromPoint = fPoint2; pLine2.ToPoint = tPoint2;

                #region 符号化测试
                ESRI.ArcGIS.Display.ISimpleLineSymbol simpleLineSymbol = new ESRI.ArcGIS.Display.SimpleLineSymbolClass();
                simpleLineSymbol.Width = 5;

                IRgbColor rgbColor1 = new RgbColorClass();
                rgbColor1.Red = 255;
                rgbColor1.Green = 215;
                rgbColor1.Blue = 0;
                simpleLineSymbol.Color = rgbColor1;

                simpleLineSymbol.Style = 0;

                object SimpleLillSymbol = simpleLineSymbol;

                IPolyline ppLine1 = new PolylineClass();
                IPolyline ppLine2 = new PolylineClass();

                ppLine1.FromPoint = pLine1.FromPoint;
                ppLine1.ToPoint = pLine1.ToPoint;

                ppLine2.FromPoint = pLine2.FromPoint;
                ppLine2.ToPoint = pLine2.ToPoint;

                mMapControl.DrawShape(ppLine1, ref SimpleLillSymbol);
                mMapControl.DrawShape(ppLine2, ref SimpleLillSymbol);
                #endregion
                #endregion

                #region 求四点的垂足
                Point cPoint1 = Chuizu(Line1.BoundaryPointList[0], Line2); IPoint Point1 = new PointClass(); Point1.X = cPoint1.X; Point1.Y = cPoint1.Y;
                Point cPoint2 = Chuizu(Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1], Line2); IPoint Point2 = new PointClass(); Point2.X = cPoint2.X; Point2.Y = cPoint2.Y;
                Point cPoint3 = Chuizu(Line2.BoundaryPointList[0], Line1); IPoint Point3 = new PointClass(); Point3.X = cPoint3.X; Point3.Y = cPoint3.Y;
                Point cPoint4 = Chuizu(Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1], Line1); IPoint Point4 = new PointClass(); Point4.X = cPoint4.X; Point4.Y = cPoint4.Y;

                #region 符号化测试
                ISimpleMarkerSymbol pMarkerSymbol = new SimpleMarkerSymbolClass();
                esriSimpleMarkerStyle eSMS = (esriSimpleMarkerStyle)0;
                pMarkerSymbol.Style = eSMS;

                IRgbColor rgbColor = new RgbColorClass();
                rgbColor.Red = 100;
                rgbColor.Green = 100;
                rgbColor.Blue = 100;

                pMarkerSymbol.Color = rgbColor;
                object oMarkerSymbol = pMarkerSymbol;
                mMapControl.DrawShape(Point1, ref oMarkerSymbol);
                mMapControl.DrawShape(Point2, ref oMarkerSymbol);
                mMapControl.DrawShape(Point3, ref oMarkerSymbol);
                mMapControl.DrawShape(Point4, ref oMarkerSymbol);
                #endregion
                #endregion

                IProximityOperator ip1 = pLine1 as IProximityOperator;
                IProximityOperator ip2 = pLine2 as IProximityOperator;

                double distance1 = ip2.ReturnDistance(Point1);
                double distance2 = ip2.ReturnDistance(Point2);
                double distance3 = ip1.ReturnDistance(Point3);
                double distance4 = ip1.ReturnDistance(Point4);

                #region 判断在线段上的垂足
                int ChuizuCount1 = 0; int ChuizuCount2 = 0;
                bool Label1 = false; bool Label2 = false; bool Label3 = false; bool Label4 = false;
                //标识四个垂足点是否在直线上，false表示不在；true表示在
                if (distance1 < 0.0000001)
                {
                    ChuizuCount1 = ChuizuCount1 + 1;
                    Label1 = true;
                }

                if (distance2 < 0.0000001)
                {
                    ChuizuCount1 = ChuizuCount1 + 1;
                    Label2 = true;
                }

                if (distance3 < 0.0000001)
                {
                    ChuizuCount2 = ChuizuCount2 + 1;
                    Label3 = true;
                }

                if (distance4 < 0.0000001)
                {
                    ChuizuCount2 = ChuizuCount2 + 1;
                    Label4 = true;
                }
                #endregion

                #region 如果直线1两个垂足都在直线2上，将直线1上较短的垂足连接在直线2上;同理，对直线2亦如此；
                if (ChuizuCount1 == 2)
                {
                    double dDistance1 = ip2.ReturnDistance(fPoint1);
                    double dDistance2 = ip2.ReturnDistance(tPoint1);

                    if (dDistance1 < dDistance2)
                    {
                        Line1.TemporaryPointList.Add(cPoint1);
                        Line2.IntersectPointList.Add(cPoint1);
                    }

                    else
                    {
                        Line1.TemporaryPointList.Add(cPoint2);
                        Line2.IntersectPointList.Add(cPoint2);
                    }
                }

                else if (ChuizuCount2 == 2)
                {
                    double dDistance1 = ip1.ReturnDistance(fPoint2);
                    double dDistance2 = ip1.ReturnDistance(tPoint2);

                    if (dDistance1 < dDistance2)
                    {
                        Line2.TemporaryPointList.Add(cPoint3);
                        Line1.IntersectPointList.Add(cPoint3);
                    }

                    else 
                    {
                        Line2.TemporaryPointList.Add(cPoint4);
                        Line1.IntersectPointList.Add(cPoint4);
                    }
                }
                #endregion

                #region 如果直线1有一个垂足在直线2上，且直线2有一个垂足在直线1上
                else if (ChuizuCount1 == 1 && ChuizuCount2 == 1)
                {
                    double dDistance1 = 0; double dDistance2 = 0;
                    if (Label1)
                    {
                        dDistance1 = ip2.ReturnDistance(fPoint1);
                    }

                    if (Label2)
                    {
                        dDistance1 = ip2.ReturnDistance(tPoint1);
                    }

                    if (Label3)
                    {
                        dDistance2 = ip1.ReturnDistance(fPoint2);
                    }

                    if (Label4)
                    {
                        dDistance2 = ip1.ReturnDistance(tPoint2);
                    }

                    if (dDistance1 < dDistance2)
                    {
                        if (Label1)
                        {
                            Line1.TemporaryPointList.Add(cPoint1);
                            Line2.IntersectPointList.Add(cPoint1);
                        }

                        if (Label2)
                        {

                            Line1.TemporaryPointList.Add(cPoint2);
                            Line2.IntersectPointList.Add(cPoint2);
                        }
                    }

                    else
                    {

                        if (Label3)
                        {
                            Line2.TemporaryPointList.Add(cPoint3);
                            Line1.IntersectPointList.Add(cPoint3);
                        }

                        if (Label4)
                        {

                            Line2.TemporaryPointList.Add(cPoint4);
                            Line1.IntersectPointList.Add(cPoint4);
                        }
                    }
                }
                #endregion

                #region 只有一个点在垂足上
                else if (ChuizuCount1 == 1 && ChuizuCount2 == 0)
                {
                    if (Label1)
                    {
                        Line1.TemporaryPointList.Add(cPoint1);
                        Line2.IntersectPointList.Add(cPoint1);
                    }

                    if (Label2)
                    {

                        Line1.TemporaryPointList.Add(cPoint2);
                        Line2.IntersectPointList.Add(cPoint2);
                    }
                }

                else if (ChuizuCount1 == 0 && ChuizuCount2 == 1)
                {
                    if (Label3)
                    {
                        Line2.TemporaryPointList.Add(cPoint3);
                        Line1.IntersectPointList.Add(cPoint3);
                    }

                    if (Label4)
                    {

                        Line2.TemporaryPointList.Add(cPoint4);
                        Line1.IntersectPointList.Add(cPoint4);
                    }
                }
                #endregion

                #region 没有垂足
                else if (ChuizuCount1 == 0 || ChuizuCount2 == 0)
                {
                    double LDistance1 = Math.Sqrt((Line1.BoundaryPointList[0].X - Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].X) * (Line1.BoundaryPointList[0].X - Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].X)
                           + (Line1.BoundaryPointList[0].Y - Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].Y) * (Line1.BoundaryPointList[0].Y - Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].Y));
                    double LDistance2 = Math.Sqrt((Line2.BoundaryPointList[0].X - Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].X) * (Line2.BoundaryPointList[0].X - Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].X)
                           + (Line2.BoundaryPointList[0].Y - Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].Y) * (Line2.BoundaryPointList[0].Y - Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].Y));

                    #region 找到两条线段最近的两个端点
                    if (LDistance1 > LDistance2)//线段1比线段2长
                    {
                        IPoint LPoint1 = new PointClass();
                        LPoint1.X = Line2.BoundaryPointList[0].X; LPoint1.Y = Line2.BoundaryPointList[0].Y;
                        IPoint LPoint2 = new PointClass();
                        LPoint2.X = Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].X; LPoint2.Y = Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].Y;

                        double kDistance1 = ip1.ReturnDistance(LPoint1);
                        double kDistance2 = ip1.ReturnDistance(LPoint2);

                        #region 端点1更近
                        if (kDistance1 < kDistance2)
                        {
                            double mDistance1 = Math.Sqrt((Line2.BoundaryPointList[0].X - Line1.BoundaryPointList[0].X) * (Line2.BoundaryPointList[0].X - Line1.BoundaryPointList[0].X)
                                + (Line2.BoundaryPointList[0].Y - Line1.BoundaryPointList[0].Y) * (Line2.BoundaryPointList[0].Y - Line1.BoundaryPointList[0].Y));
                            double mDistance2 = Math.Sqrt((Line2.BoundaryPointList[0].X - Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].X) * (Line2.BoundaryPointList[0].X - Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].X)
                            + (Line2.BoundaryPointList[0].Y - Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].Y) * (Line2.BoundaryPointList[0].Y - Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].Y));

                            if (mDistance1 < mDistance2)
                            {
                                Line2.TemporaryPointList.Add(Line1.BoundaryPointList[0]);
                            }

                            else
                            {
                                Line2.TemporaryPointList.Add(Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1]);
                            }

                        }
                        #endregion

                        #region 端点2更近
                        else
                        {
                            double mDistance1 = Math.Sqrt((Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].X - Line1.BoundaryPointList[0].X) * (Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].X - Line1.BoundaryPointList[0].X)
                                + (Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].Y - Line1.BoundaryPointList[0].Y) * (Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].Y - Line1.BoundaryPointList[0].Y));
                            double mDistance2 = Math.Sqrt((Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].X - Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].X) * (Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].X - Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].X)
                            + (Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].Y - Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].Y) * (Line2.BoundaryPointList[Line2.BoundaryPointList.Count-1].Y - Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].Y));

                            if (mDistance1 < mDistance2)
                            {
                                Line2.TemporaryPointList.Add(Line1.BoundaryPointList[0]);
                            }

                            else
                            {
                                Line2.TemporaryPointList.Add(Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1]);
                            }
                        }
                        #endregion
                    }

                    else//线段1比线段2短
                    {
                        IPoint LPoint1 = new PointClass();
                        LPoint1.X = Line1.BoundaryPointList[0].X; LPoint1.Y = Line1.BoundaryPointList[0].Y;
                        IPoint LPoint2 = new PointClass();
                        LPoint2.X = Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].X; LPoint2.Y = Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].Y;

                        double kDistance1 = ip2.ReturnDistance(LPoint1);
                        double kDistance2 = ip2.ReturnDistance(LPoint2);

                        #region 端点1更近
                        if (kDistance1 < kDistance2)
                        {
                            double mDistance1 = Math.Sqrt((Line1.BoundaryPointList[0].X - Line2.BoundaryPointList[0].X) * (Line1.BoundaryPointList[0].X - Line2.BoundaryPointList[0].X)
                                + (Line1.BoundaryPointList[0].Y - Line2.BoundaryPointList[0].Y) * (Line1.BoundaryPointList[0].Y - Line2.BoundaryPointList[0].Y));
                            double mDistance2 = Math.Sqrt((Line1.BoundaryPointList[0].X - Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].X) * (Line1.BoundaryPointList[0].X - Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].X)
                            + (Line1.BoundaryPointList[0].Y - Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].Y) * (Line1.BoundaryPointList[0].Y - Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].Y));

                            if (mDistance1 < mDistance2)
                            {
                                Line1.TemporaryPointList.Add(Line2.BoundaryPointList[0]);
                            }

                            else
                            {
                                Line1.TemporaryPointList.Add(Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1]);
                            }

                        }
                        #endregion

                        #region 端点2更近
                        else
                        {
                            double mDistance1 = Math.Sqrt((Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].X - Line2.BoundaryPointList[0].X) * (Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].X - Line2.BoundaryPointList[0].X)
                               + (Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].Y - Line2.BoundaryPointList[0].Y) * (Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].Y - Line2.BoundaryPointList[0].Y));
                            double mDistance2 = Math.Sqrt((Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].X - Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].X) * (Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].X - Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].X)
                            + (Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].Y - Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].Y) * (Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].Y - Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].Y));

                            if (mDistance1 < mDistance2)
                            {
                                Line1.TemporaryPointList.Add(Line2.BoundaryPointList[0]);
                            }

                            else
                            {
                                Line1.TemporaryPointList.Add(Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1]);
                            }
                        }
                        #endregion
                    }
                    #endregion
                }
                #endregion
            }
            #endregion

            #region 平行关系 短的线向长的线做垂足，取垂足中最远的两个点当做新的BoundaryPoint
            if (RelationLabel == 2)
            {

            }
            #endregion
        }

        /// <summary>
        /// 计算两条直线的关系
        /// </summary>
        /// <param name="Line1"></param>
        /// <param name="Line2"></param>
        /// <returns></returns>
        /// 1、表示相交；2、表示平行；3、表示需要相交；4、表示无需处理
        public int RelationComputation(Line Line1,Line Line2,double Distance,double OrientationConstraint,AxMapControl mMapControl)
        {
            #region 重新创建两条新线段
            ILine pLine1 = new LineClass();
            IPoint fPoint1 = new PointClass(); fPoint1.X = Line1.BoundaryPointList[0].X; fPoint1.Y = Line1.BoundaryPointList[0].Y;
            IPoint tPoint1 = new PointClass(); tPoint1.X = Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].X; tPoint1.Y = Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].Y;
            pLine1.FromPoint = fPoint1; pLine1.ToPoint = tPoint1;

            ILine pLine2 = new LineClass();
            IPoint fPoint2 = new PointClass(); fPoint2.X = Line2.BoundaryPointList[0].X; fPoint2.Y = Line2.BoundaryPointList[0].Y;
            IPoint tPoint2 = new PointClass(); tPoint2.X = Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].X; tPoint2.Y = Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1].Y;
            pLine2.FromPoint = fPoint2; pLine2.ToPoint = tPoint2;
            #endregion

            IProximityOperator ip1 = pLine1 as IProximityOperator;
            IProximityOperator ip2 = pLine2 as IProximityOperator;
            double distance = ip1.ReturnDistance(pLine2);

            #region 如果线段距离等于0，则表示两条线段相交
            if (distance == 0)
            {
                return 1;
            }
            #endregion

            #region 如果不相交，则判断是否平行；是否需要合并
            else
            {
                #region 距离小
                if (distance < Distance)
                {
                    #region 计算两条直线的夹角
                    double angle1 = pLine1.Angle;
                    double angle2 = pLine2.Angle;

                    #region 将angle装换到0-180
                    double Pi = 4 * Math.Atan(1);
                    double dAngle1Degree = (180 * angle1) / Pi;
                    double dAngle2Degree = (180 * angle2) / Pi;

                    if (dAngle1Degree < 0)
                    {
                        dAngle1Degree = dAngle1Degree + 180;
                    }

                    if (dAngle2Degree < 0)
                    {
                        dAngle2Degree = dAngle2Degree + 180;
                    }
                    #endregion
                    #endregion

                    #region 角度上平行
                    if ((Math.Abs(dAngle1Degree - dAngle2Degree) < 90 && Math.Abs(dAngle1Degree - dAngle2Degree) < OrientationConstraint)
                        || (Math.Abs(dAngle1Degree - dAngle2Degree) > 90 && Math.Abs(180 - Math.Abs(dAngle1Degree - dAngle2Degree)) < OrientationConstraint))
                    {
                        #region 求角平分线和垂足
                        double avAngle = 0; double k = 0;
                        if (Math.Abs(dAngle1Degree - dAngle2Degree) < 90)
                        {
                            avAngle = (dAngle1Degree + dAngle2Degree) / 2;
                            k = Math.Tan(avAngle);
                        }

                        else
                        {
                            avAngle = 180-(dAngle1Degree + dAngle2Degree) / 2;
                            k = Math.Tan(avAngle);
                        }

                        IPoint ddPoint = new PointClass();
                        ddPoint.X = tPoint1.X; ddPoint.Y = k * ddPoint.X + fPoint1.Y - k * fPoint1.X;

                        double ck = (ddPoint.Y - fPoint1.Y) / (ddPoint.X - fPoint1.X);
                        Point nnPoint=new Point(ddPoint.X,ddPoint.Y);

                        #region 符号化测试
                        IPolyline ddPolyline = new PolylineClass();
                        ILine cLine=new LineClass();
                        cLine.FromPoint=fPoint1;cLine.ToPoint=ddPoint;
                        ddPolyline.FromPoint = fPoint1;
                        ddPolyline.ToPoint = ddPoint;

                        ESRI.ArcGIS.Display.ISimpleLineSymbol simpleLineSymbol = new ESRI.ArcGIS.Display.SimpleLineSymbolClass();
                        simpleLineSymbol.Width = 5;

                        IRgbColor rgbColor1 = new RgbColorClass();
                        rgbColor1.Red = 255;
                        rgbColor1.Green = 215;
                        rgbColor1.Blue = 0;
                        simpleLineSymbol.Color = rgbColor1;

                        simpleLineSymbol.Style = 0;

                        object SimpleLillSymbol = simpleLineSymbol;
                        mMapControl.DrawShape(ddPolyline, ref SimpleLillSymbol);
                        double cAngle = cLine.Angle;

                        double cAngle1Degree = (180 * cAngle) / Pi;

                        if (cAngle1Degree < 0)
                        {
                            cAngle1Degree = dAngle1Degree + 180;
                        }
                        #endregion

                        Point chuzuPoint1 = Chuizu(Line1.BoundaryPointList[0], Line1.BoundaryPointList[0],nnPoint);
                        Point chuzuPoint2 = Chuizu(Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1], Line1.BoundaryPointList[0], nnPoint);
                        Point chuzuPoint3 = Chuizu(Line2.BoundaryPointList[0], Line1.BoundaryPointList[0], nnPoint);
                        Point chuzuPoint4 = Chuizu(Line2.BoundaryPointList[Line2.BoundaryPointList.Count - 1], Line1.BoundaryPointList[0], nnPoint);
                        #endregion

                        #region 投影重叠距离
                        IPolyline mLine1 = new PolylineClass();
                        IPoint mPoint1 = new PointClass(); IPoint mPoint2 = new PointClass();
                        mPoint1.X = chuzuPoint1.X; mPoint1.Y = chuzuPoint1.Y;
                        mPoint2.X = chuzuPoint2.X; mPoint2.Y = chuzuPoint2.Y;
                        mLine1.FromPoint = mPoint1; mLine1.ToPoint = mPoint2;

                        IPolyline mLine2 = new PolylineClass();
                        IPoint mPoint3 = new PointClass(); IPoint mPoint4 = new PointClass();
                        mPoint3.X = chuzuPoint3.X; mPoint3.Y = chuzuPoint3.Y;
                        mPoint4.X = chuzuPoint4.X; mPoint4.Y = chuzuPoint4.Y;
                        mLine2.FromPoint = mPoint3; mLine2.ToPoint = mPoint4;

                        #region 符号化测试
                        ISimpleMarkerSymbol pMarkerSymbol = new SimpleMarkerSymbolClass();
                        esriSimpleMarkerStyle eSMS = (esriSimpleMarkerStyle)0;
                        pMarkerSymbol.Style = eSMS;

                        IRgbColor rgbColor = new RgbColorClass();
                        rgbColor.Red = 100;
                        rgbColor.Green = 100;
                        rgbColor.Blue = 100;

                        pMarkerSymbol.Color = rgbColor;
                        object oMarkerSymbol = pMarkerSymbol;
                        mMapControl.DrawShape(mPoint1, ref oMarkerSymbol);
                        mMapControl.DrawShape(mPoint2, ref oMarkerSymbol);
                        mMapControl.DrawShape(mPoint3, ref oMarkerSymbol);
                        mMapControl.DrawShape(mPoint4, ref oMarkerSymbol);
                        #endregion

                        ITopologicalOperator mtop = mLine1 as ITopologicalOperator;
                        IGeometry pGeometry = mtop.Intersect(mLine2, esriGeometryDimension.esriGeometry1Dimension);                       
                        #endregion

                        if (pGeometry != null)
                        {
                            IPolyline gLine = pGeometry as IPolyline;
                            double Length = gLine.Length;

                            #region 符号化测试
                            mMapControl.DrawShape(gLine, ref SimpleLillSymbol);
                            #endregion

                            //投影距离较长
                            if (Length > Distance)
                            {
                                return 2;//
                            }

                            //投影距离较短
                            else
                            {
                                return 3;//投影距离不够长
                            }
                        }

                        else
                        {
                            return 3;
                        }
                    }
                    #endregion

                    #region 角度上不平行；判断距离是否小于阈值
                    else
                    {
                        return 3;//需要做相交处理
                    }
                    #endregion
                }
                #endregion

                #region 距离大
                else
                {
                    return 4;//表示不需要处理
                }
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// 求点到直线的垂足
        /// </summary>
        /// <param name="Point1"></param>
        /// <param name="Line1"></param>
        /// <returns></returns>
        public Point Chuizu(Point Point1, Line Line1)
        {
            TriNode s = new TriNode(Line1.BoundaryPointList[0].X, Line1.BoundaryPointList[0].Y);
            TriNode e = new TriNode(Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].X, Line1.BoundaryPointList[Line1.BoundaryPointList.Count - 1].Y);
            TriNode p = new TriNode(Point1.X, Point1.Y);

            ComFunLib cfl = new ComFunLib();
            TriNode td = cfl.ComChuizu(s, e, p);
            Point tdp = new Point(td.X, td.Y);
            return tdp;
        }

        /// <summary>
        /// 求点到直线的垂足
        /// </summary>
        /// <param name="Point1"></param>
        /// <param name="Line1"></param>
        /// <returns></returns>
        public Point Chuizu(Point Point1, double k)
        {
            TriNode s = new TriNode(0,0);
            TriNode e = new TriNode(10000000, 10000000 * k);
            TriNode p = new TriNode(Point1.X, Point1.Y);

            ComFunLib cfl = new ComFunLib();
            TriNode td = cfl.ComChuizu(s, e, p);
            Point tdp = new Point(td.X, td.Y);
            return tdp;
        }

        /// <summary>
        /// 求直线的垂足
        /// </summary>
        /// <param name="Point1"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public Point Chuizu(Point Point1,Point Point2,Point Point3)
        {
            TriNode s = new TriNode(Point2.X, Point2.Y);
            TriNode e = new TriNode(Point3.X, Point3.Y);
            TriNode p = new TriNode(Point1.X, Point1.Y);

            ComFunLib cfl = new ComFunLib();
            TriNode td = cfl.ComChuizu(s, e, p);
            Point tdp = new Point(td.X, td.Y);
            return tdp;
        }
    }
}
