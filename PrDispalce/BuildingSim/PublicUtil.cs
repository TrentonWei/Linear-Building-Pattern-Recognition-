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
using PrDispalce.建筑物聚合;

namespace PrDispalce.BuildingSim
{
    class PublicUtil
    {

        /// <summary>
        /// 获得图形上的所有结构（建筑物简化投稿用）
        /// (凸角2、凹角3、阶梯状结构4//要求两个角都是直角
        /// </summary>
        /// <param name="Curpo"></param>
        /// <param name="PerAngel"></param>
        /// <returns></returns>
        public List<BasicStruct> GetStructedNodes(PolygonObject Curpo, double PerAngle)
        {
            List<BasicStruct> StructedList = new List<BasicStruct>();

            double Pi = 3.1415926;//Pi的定义
            Curpo.GetBendAngle();//获得每一个节点的角度，区分正负号

            int p = -1;//结构编码序号

            #region 获得建筑物中的每一条边
            List<PolylineObject> EdgeList = new List<PolylineObject>();
            for (int i = 0; i < Curpo.PointList.Count; i++)
            {
                if (i != Curpo.PointList.Count - 1)
                {
                    PolylineObject CacheEdge = new PolylineObject();
                    List<TriNode> NodeList = new List<TriNode>();
                    NodeList.Add(Curpo.PointList[i]); NodeList.Add(Curpo.PointList[i + 1]);
                    CacheEdge.PointList = NodeList; CacheEdge.ID = i;
                    EdgeList.Add(CacheEdge);
                }

                else
                {
                    PolylineObject CacheEdge = new PolylineObject();
                    List<TriNode> NodeList = new List<TriNode>();
                    NodeList.Add(Curpo.PointList[i]); NodeList.Add(Curpo.PointList[0]);
                    CacheEdge.PointList = NodeList; CacheEdge.ID = i;
                    EdgeList.Add(CacheEdge);
                }
            }
            #endregion

            #region 其次，判断特殊结构（依次判断邻近两个转折）
            for (int i = 0; i < Curpo.BendAngle.Count; i++)
            {
                if (i == Curpo.BendAngle.Count - 1)
                {
                    double BendAngle1 = Curpo.BendAngle[i][1]; //获得两个bend的角度
                    double BendAngle2 = Curpo.BendAngle[0][1];

                    #region 两个角均为直角
                    if (Math.Abs(Math.Abs(BendAngle1) - Pi / 2) < PerAngle && Math.Abs(Math.Abs(BendAngle2) - Pi / 2) < PerAngle)
                    {
                        List<TriNode> NodeList = new List<TriNode>();
                        List<PolylineObject> CacheEdgeList = new List<PolylineObject>();
                        List<double> NodeAngle = new List<double>();

                        NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 2]);
                        NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 1]);
                        NodeList.Add(Curpo.PointList[0]);
                        NodeList.Add(Curpo.PointList[1]);

                        NodeAngle.Add(Curpo.BendAngle[Curpo.BendAngle.Count - 2][1]);
                        NodeAngle.Add(Curpo.BendAngle[Curpo.BendAngle.Count - 1][1]);
                        NodeAngle.Add(Curpo.BendAngle[0][1]);
                        NodeAngle.Add(Curpo.BendAngle[1][1]);

                        CacheEdgeList.Add(EdgeList[EdgeList.Count - 2]);
                        CacheEdgeList.Add(EdgeList[EdgeList.Count - 1]);
                        CacheEdgeList.Add(EdgeList[0]);

                        #region 是凸部或者凹部
                        if (NodeAngle[1] * NodeAngle[2] > 0)
                        {
                            //凸部
                            if (NodeAngle[1] > 0)
                            {
                                p++;
                                BasicStruct CacheStruct = new BasicStruct(p, 2, NodeList, CacheEdgeList, NodeAngle);
                                StructedList.Add(CacheStruct);
                            }

                            //凹部
                            else if (NodeAngle[1] <= 0)
                            {
                                p++;
                                BasicStruct CacheStruct = new BasicStruct(p, 3, NodeList, CacheEdgeList, NodeAngle);
                                StructedList.Add(CacheStruct);
                            }
                        }
                        #endregion

                        #region 阶梯状
                        else
                        {
                            p++;
                            BasicStruct CacheStruct = new BasicStruct(p, 4, NodeList, CacheEdgeList, NodeAngle);
                            StructedList.Add(CacheStruct);
                        }
                        #endregion
                    }
                    #endregion
                }

                else
                {
                    double BendAngle1 = Curpo.BendAngle[i][1]; //获得两个bend的角度
                    double BendAngle2 = Curpo.BendAngle[i + 1][1];

                    #region 两个角均为直角
                    if (Math.Abs(Math.Abs(BendAngle1) - Pi / 2) < PerAngle && Math.Abs(Math.Abs(BendAngle2) - Pi / 2) < PerAngle)
                    {
                        List<TriNode> NodeList = new List<TriNode>();
                        List<PolylineObject> CacheEdgeList = new List<PolylineObject>();
                        List<double> NodeAngle = new List<double>();

                        #region 如果是第一个点
                        if (i == 0)
                        {
                            NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 1]);
                            NodeList.Add(Curpo.PointList[0]);
                            NodeList.Add(Curpo.PointList[1]);
                            NodeList.Add(Curpo.PointList[2]);

                            NodeAngle.Add(Curpo.BendAngle[Curpo.BendAngle.Count - 1][1]);
                            NodeAngle.Add(Curpo.BendAngle[0][1]);
                            NodeAngle.Add(Curpo.BendAngle[1][1]);
                            NodeAngle.Add(Curpo.BendAngle[2][1]);

                            CacheEdgeList.Add(EdgeList[EdgeList.Count - 1]);
                            CacheEdgeList.Add(EdgeList[0]);
                            CacheEdgeList.Add(EdgeList[1]);
                        }
                        #endregion

                        #region 如果是倒数第二个点
                        else if (i == Curpo.PointList.Count - 2)
                        {
                            NodeList.Add(Curpo.PointList[i - 1]);
                            NodeList.Add(Curpo.PointList[i]);
                            NodeList.Add(Curpo.PointList[i + 1]);
                            NodeList.Add(Curpo.PointList[0]);

                            NodeAngle.Add(Curpo.BendAngle[i - 1][1]);
                            NodeAngle.Add(Curpo.BendAngle[i][1]);
                            NodeAngle.Add(Curpo.BendAngle[i + 1][1]);
                            NodeAngle.Add(Curpo.BendAngle[0][1]);

                            CacheEdgeList.Add(EdgeList[i - 1]);
                            CacheEdgeList.Add(EdgeList[i]);
                            CacheEdgeList.Add(EdgeList[i + 1]);
                        }
                        #endregion

                        #region 其它
                        else
                        {
                            NodeList.Add(Curpo.PointList[i - 1]);
                            NodeList.Add(Curpo.PointList[i]);
                            NodeList.Add(Curpo.PointList[i + 1]);
                            NodeList.Add(Curpo.PointList[i + 2]);

                            NodeAngle.Add(Curpo.BendAngle[i - 1][1]);
                            NodeAngle.Add(Curpo.BendAngle[i][1]);
                            NodeAngle.Add(Curpo.BendAngle[i + 1][1]);
                            NodeAngle.Add(Curpo.BendAngle[i + 2][1]);

                            CacheEdgeList.Add(EdgeList[i - 1]);
                            CacheEdgeList.Add(EdgeList[i]);
                            CacheEdgeList.Add(EdgeList[i + 1]);

                        }
                        #endregion

                        #region 是凸部或者凹部
                        if (NodeAngle[1] * NodeAngle[2] > 0)
                        {
                            //凸部
                            if (NodeAngle[1] > 0)
                            {
                                p++;
                                BasicStruct CacheStruct = new BasicStruct(p, 2, NodeList, CacheEdgeList, NodeAngle);
                                StructedList.Add(CacheStruct);
                            }

                            //凹部
                            else if (NodeAngle[1] <= 0)
                            {
                                p++;
                                BasicStruct CacheStruct = new BasicStruct(p, 3, NodeList, CacheEdgeList, NodeAngle);
                                StructedList.Add(CacheStruct);
                            }
                        }
                        #endregion

                        #region 阶梯状
                        else
                        {
                            p++;
                            BasicStruct CacheStruct = new BasicStruct(p, 4, NodeList, CacheEdgeList, NodeAngle);
                            StructedList.Add(CacheStruct);
                        }
                        #endregion
                    }
                    #endregion
                }
            }
            #endregion

            return StructedList;
        }

        /// <summary>
        /// 获取建筑物的凹点，同时，对节点的信息进行标记（已经有效考虑了洞的情况）
        /// 【即外轮廓大于180度，则为凹点；内轮廓小于180度，则为凹点——洞的节点编码顺序与外轮廓相反】
        /// </summary>
        /// <param name="Po"></param>
        /// <returns></returns>
        public List<TriNode> GetConcaveNode(PolygonObject Po, double OnlineT)
        {
            List<TriNode> ConvexNodeList = new List<TriNode>();

            for (int i = 0; i < Po.PointList.Count; i++)
            {
                #region 获取节点信息
                TriNode CurPoint1 = null; TriNode AfterPoint2 = null; TriNode BeforePoint3 = null;

                if (i == 0)
                {
                    CurPoint1 = Po.PointList[i];
                    AfterPoint2 = Po.PointList[i + 1];
                    BeforePoint3 = Po.PointList[Po.PointList.Count - 1];
                }

                else if (i == Po.PointList.Count - 1)
                {
                    CurPoint1 = Po.PointList[i];
                    AfterPoint2 = Po.PointList[0];
                    BeforePoint3 = Po.PointList[i - 1];
                }

                else
                {
                    CurPoint1 = Po.PointList[i];
                    AfterPoint2 = Po.PointList[i + 1];
                    BeforePoint3 = Po.PointList[i - 1];
                }
                #endregion

                double Angle = this.GetPointAngle(CurPoint1, BeforePoint3, AfterPoint2);//计算角度

                #region 判断凹凸性
                #region 角度计算
                Angle = Angle * 180 / 3.1415926;

                if (Angle < 0)
                {
                    Angle = 360 + Angle;
                }
                #endregion

                if (Angle > (180 + OnlineT))
                {
                    ConvexNodeList.Add(Po.PointList[i]);
                }
                #endregion
            }

            return ConvexNodeList;
        }

        /// <summary>
        /// 计算建筑物图形上某节点处的角度
        /// </summary>
        /// <param name="TriNode1"></param>
        /// <param name="TriNode2"></param>
        /// <param name="TriNode3"></param>
        /// <returns></returns>
        public double GetPointAngle(TriNode CurPoint, TriNode BeforePoint, TriNode AfterPoint)
        {
            #region 计算叉积信息
            double Vector1X = BeforePoint.X - CurPoint.X; double Vector1Y = BeforePoint.Y - CurPoint.Y;
            double Vector2X = AfterPoint.X - CurPoint.X; double Vector2Y = AfterPoint.Y - CurPoint.Y;

            double xMultiply = Vector1X * Vector2Y - Vector1Y * Vector2X;//获得叉积信息，用于判断顺逆时针
            #endregion

            #region 计算角度信息(顺时针角度为正；逆时针角度为负)
            double Angle = this.GetAngle(CurPoint, AfterPoint, BeforePoint);
            if (xMultiply < 0)
            {
                Angle = Angle * (-1);
            }
            #endregion

            return Angle;
        }

        /// <summary>
        /// 给定直线上两点，获取从给定点出发与该直线平行的平行线
        /// </summary>
        /// <param name="CurPoint"></param>
        /// <param name="ParaNode1"></param>
        /// <param name="ParaNode2"></param>
        /// <returns></returns>
        public IPolyline GetParaLine(TriNode CurPoint, TriNode ParaNode1, TriNode ParaNode2)
        {
            IPolyline ParaLine = new PolylineClass();

            double k = (ParaNode2.Y - ParaNode1.Y) / (ParaNode2.X - ParaNode1.X);

            if (ParaNode2.X - ParaNode1.X != 0)
            {
                double x1 = CurPoint.X - 1000; double y1 = CurPoint.Y - 1000 * k;
                double x2 = CurPoint.X + 1000; double y2 = CurPoint.Y + 1000 * k;
                IPoint FromPoint = new PointClass(); FromPoint.X = x1; FromPoint.Y = y1;
                IPoint ToPoint = new PointClass(); ToPoint.X = x2; ToPoint.Y = y2;
                ParaLine.FromPoint = FromPoint; ParaLine.ToPoint = ToPoint;
            }

            else
            {
                double x1 = CurPoint.X; double y1 = CurPoint.Y - 1000;
                double x2 = CurPoint.X; double y2 = CurPoint.Y + 1000;
                IPoint FromPoint = new PointClass(); FromPoint.X = x1; FromPoint.Y = y1;
                IPoint ToPoint = new PointClass(); ToPoint.X = x2; ToPoint.Y = y2;
                ParaLine.FromPoint = FromPoint; ParaLine.ToPoint = ToPoint;
            }

            return ParaLine;
        }

        /// <summary>
        /// 给定两点，获得CurPoint沿該条直线的延长线
        /// </summary>
        /// <param name="CurPoint"></param>
        /// <param name="EndPoint"></param>
        /// <returns></returns>
        public IPolyline GetExtendingLine(TriNode CurPoint, TriNode EndPoint)
        {
            IPolyline ExtendingLine = new PolylineClass();

            IPoint StartNode = new PointClass(); StartNode.X = CurPoint.X; StartNode.Y = CurPoint.Y;
            IPoint EndNode = new PointClass();
            double k = (EndPoint.Y - CurPoint.Y) / (EndPoint.X - CurPoint.X);
            if (CurPoint.X > EndPoint.X)
            {
                double X = CurPoint.X + 1000;
                double Y = CurPoint.Y + 1000 * k;
                EndNode.X = X; EndNode.Y = Y;
            }

            else if (CurPoint.X == EndPoint.X)
            {
                double X = CurPoint.X ;
                double Y = CurPoint.Y + 1000;
                EndNode.X = X; EndNode.Y = Y;
            }

            else
            {
                double X = CurPoint.X - 1000;
                double Y = CurPoint.Y - 1000 * k;
                EndNode.X = X; EndNode.Y = Y;
            }

            ExtendingLine.FromPoint = StartNode;
            ExtendingLine.ToPoint = EndNode;

            return ExtendingLine;
        }

        /// <summary>
        /// 给定三点，计算该点的角度值(0,PI)
        /// </summary>
        /// <param name="curNode"></param>
        /// <param name="TriNode1"></param> AfterPoint后节点！
        /// <param name="TriNode2"></param> BeforePoint前节点！
        /// <returns></returns>
        public double GetAngle(TriNode curNode, TriNode TriNode1, TriNode TriNode2)
        {
            double a = Math.Sqrt((curNode.X - TriNode1.X) * (curNode.X - TriNode1.X) + (curNode.Y - TriNode1.Y) * (curNode.Y - TriNode1.Y));
            double b = Math.Sqrt((curNode.X - TriNode2.X) * (curNode.X - TriNode2.X) + (curNode.Y - TriNode2.Y) * (curNode.Y - TriNode2.Y));
            double c = Math.Sqrt((TriNode1.X - TriNode2.X) * (TriNode1.X - TriNode2.X) + (TriNode1.Y - TriNode2.Y) * (TriNode1.Y - TriNode2.Y));

            double CosCur = (a * a + b * b - c * c) / (2 * a * b);
            double Angle = Math.Acos(CosCur);

            return Angle;
        }

        /// <summary>
        /// 给定三点，计算该点的角度值(0,PI)
        /// </summary>
        /// <param name="curNode"></param>
        /// <param name="TriNode1"></param> AfterPoint后节点！
        /// <param name="TriNode2"></param> BeforePoint前节点！
        /// <returns></returns>
        public double GetAngle(IPoint curNode, IPoint TriNode1, IPoint TriNode2)
        {
            TriNode tcurNode = new TriNode(curNode.X, curNode.Y);
            TriNode tTriNode1 = new TriNode(TriNode1.X, TriNode1.Y);
            TriNode tTriNode2 = new TriNode(TriNode2.X, TriNode2.Y);
            return this.GetAngle(tcurNode, tTriNode1, tTriNode2);
        }

        /// <summary>
        /// 将建筑物转化为IPolygon
        /// </summary>
        /// <param name="pPolygonObject"></param>
        /// <returns></returns>
        public IPolygon PolygonObjectConvert(PolygonObject pPolygonObject)
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

            curPoint = pPolygonObject.PointList[0];
            curResultPoint.PutCoords(curPoint.X, curPoint.Y);
            ring1.AddPoint(curResultPoint, ref missing, ref missing);

            IGeometryCollection pointPolygon = new PolygonClass();
            pointPolygon.AddGeometry(ring1 as IGeometry, ref missing, ref missing);
            IPolygon pPolygon = pointPolygon as IPolygon;

            //PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
            //object PolygonSymbol = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);

            //pMapControl.DrawShape(pPolygon, ref PolygonSymbol);
            //pMapControl.Map.RecalcFullExtent();

            pPolygon.SimplifyPreserveFromTo();
            return pPolygon;
        }

        /// <summary>
        /// polygon转换成polygonobject
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <returns></returns>
        public PolygonObject PolygonConvert(IPolygon pPolygon)
        {
            int ppID = 0;//（polygonobject自己的编号，应该无用）
            List<TriNode> trilist = new List<TriNode>();
            //Polygon的点集
            IPointCollection pointSet = pPolygon as IPointCollection;
            int count = pointSet.PointCount;
            double curX;
            double curY;
            //ArcGIS中，多边形的首尾点重复存储
            for (int i = 0; i < count - 1; i++)
            {
                curX = pointSet.get_Point(i).X;
                curY = pointSet.get_Point(i).Y;
                //初始化每个点对象
                TriNode tPoint = new TriNode(curX, curY, ppID, 1);
                trilist.Add(tPoint);
            }
            //生成自己写的多边形
            PolygonObject mPolygonObject = new PolygonObject(ppID, trilist);

            return mPolygonObject;
        }

        /// <summary>
        /// 获得旋转后的多边形
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <param name="Orientation"></param>
        /// <returns></returns>
        public IPolygon GetRotatedPolygon(IPolygon pPolygon, double Orientation)
        {
            IArea pArea = pPolygon as IArea;
            IPoint CenterPoint = pArea.Centroid;
            ITransform2D pTransform2D = pPolygon as ITransform2D;
            pTransform2D.Rotate(CenterPoint, Orientation);
            return pTransform2D as IPolygon;
        }

        /// <summary>
        /// 获得平移后的多边形
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <param name="pPoint"></param>
        /// <returns></returns>
        public IPolygon GetPannedPolygon(IPolygon pPolygon, IPoint pPoint)
        {
            IArea pArea = pPolygon as IArea;
            IPoint CenterPoint = pArea.Centroid;

            double Dx = pPoint.X - CenterPoint.X;
            double Dy = pPoint.Y - CenterPoint.Y;

            ITransform2D pTransform2D = pPolygon as ITransform2D;
            pTransform2D.Move(Dx, Dy);
            return pTransform2D as IPolygon;
        }

        /// <summary>
        /// 获得放大后的多边形
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <param name="EnlargeRate"></param>
        /// <returns></returns>
        public IPolygon GetEnlargedPolygon(IPolygon pPolygon, double EnlargeRate)
        {
            IArea pArea = pPolygon as IArea;
            IPoint CenterPoint = pArea.Centroid;

            ITransform2D pTransform2D = pPolygon as ITransform2D;
            pTransform2D.Scale(CenterPoint, EnlargeRate, EnlargeRate);
            return pTransform2D as IPolygon;
        }

        /// <summary>
        /// 将建筑物缩放至与TargetPo面积一致
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <param name="EnlargeRate"></param>
        /// <returns></returns>
        public IPolygon GetEnlargedPolygon(IPolygon pPolygon, IPolygon TargetPo)
        {
            IArea tArea = TargetPo as IArea;
            double tA = tArea.Area;

            IArea pArea = pPolygon as IArea;
            IPoint CenterPoint = pArea.Centroid;
            double pA=pArea.Area;

            double EnlargeRate = pA / tA;

            ITransform2D pTransform2D = pPolygon as ITransform2D;
            pTransform2D.Scale(CenterPoint, EnlargeRate, EnlargeRate);
            return pTransform2D as IPolygon;
        }

        /// <summary>
        /// 获取给定Feature的属性
        /// </summary>
        /// <param name="CurFeature"></param>
        /// <param name="FieldString"></param>
        /// <returns></returns>
        public double GetValue(IFeature curFeature, string FieldString)
        {
            double Value = 0;

            IFields pFields = curFeature.Fields;
            int field1 = pFields.FindField(FieldString);
            Value = Convert.ToDouble(curFeature.get_Value(field1));

            return Value;
        }
    }
}
