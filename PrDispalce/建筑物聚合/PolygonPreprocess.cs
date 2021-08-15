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
    //建筑物的预处理类
    class PolygonPreprocess
    {
        public AxMapControl pMapControl;

        #region 构造函数
        public PolygonPreprocess()
        {
        }

        public PolygonPreprocess(AxMapControl CacheControl)
        {
            this.pMapControl = CacheControl;
        }
        #endregion

        /// <summary>
        /// 删除共线的点
        /// </summary>
        /// <param name="CurPo"></param>
        /// <param name="MinOrientation"></param>
        public void DeleteOnLinePoint(PolygonObject CurPo,double MinOrientation)
        {
            //起点和终点在多边形点集中只存了一个点
            bool ProcessLable = true;

            while (ProcessLable)
            {
                ProcessLable = false;
                for (int i = 0; i < CurPo.PointList.Count; i++)
                {
                    #region 获得三个节点
                    TriNode TriNode1 = null;
                    TriNode TriNode2 = null;
                    TriNode CurNode = null;

                    CurNode = CurPo.PointList[i];
                    if (i == 0)
                    {
                        TriNode1 = CurPo.PointList[CurPo.PointList.Count - 1];
                        TriNode2 = CurPo.PointList[i + 1];
                    }

                    else if (i == CurPo.PointList.Count - 1)
                    {
                        TriNode1 = CurPo.PointList[i - 1];
                        TriNode2 = CurPo.PointList[0];
                    }

                    else
                    {
                        TriNode1 = CurPo.PointList[i - 1];
                        TriNode2 = CurPo.PointList[i + 1];
                    }
                    #endregion

                    #region 如果满足条件就删除该节点
                    double Angle = GetAngle(CurNode, TriNode1, TriNode2);
                    if (Math.Abs((Angle - 3.1415926)) < MinOrientation)
                    {
                        CurPo.PointList.RemoveAt(i);
                        ProcessLable = true;
                        break;
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// 删除共线的点
        /// </summary>
        /// <param name="CurPo"></param>
        /// <param name="MinOrientation"></param>
        public PolygonObject DeleteOnLinePoint2(PolygonObject CurPo, double MinOrientation)
        {
            //起点和终点在多边形点集中只存了一个点
            bool ProcessLable = true;
            PolygonObject Po = new PolygonObject(0, CurPo.PointList);

            while (ProcessLable)
            {
                ProcessLable = false;
                for (int i = 0; i < Po.PointList.Count; i++)
                {
                    #region 获得三个节点
                    TriNode TriNode1 = null;
                    TriNode TriNode2 = null;
                    TriNode CurNode = null;

                    CurNode = Po.PointList[i];
                    if (i == 0)
                    {
                        TriNode1 = Po.PointList[Po.PointList.Count - 1];
                        TriNode2 = Po.PointList[i + 1];
                    }

                    else if (i == Po.PointList.Count - 1)
                    {
                        TriNode1 = Po.PointList[i - 1];
                        TriNode2 = Po.PointList[0];
                    }

                    else
                    {
                        TriNode1 = Po.PointList[i - 1];
                        TriNode2 = Po.PointList[i + 1];
                    }
                    #endregion

                    #region 如果满足条件就删除该节点
                    double Angle = GetAngle(CurNode, TriNode1, TriNode2);
                    if (Math.Abs((Angle - 3.1415926)) < MinOrientation)
                    {
                        Po.PointList.RemoveAt(i);
                        ProcessLable = true;
                        break;
                    }
                    #endregion
                }
            }

            return Po;
        }

        /// <summary>
        /// 删除重复的节点
        /// </summary>
        /// <param name="CurPo"></param>
        /// <param name="MinDis"></param>
        public void DeleteSamePoint(PolygonObject CurPo, double MinDis)
        {
            bool ProcessLabel = true;

            while (ProcessLabel)
            {
                ProcessLabel = false;
                for (int i = 0; i < CurPo.PointList.Count - 1; i++)
                {
                    for (int j = i + 1; j < CurPo.PointList.Count; j++)
                    {
                        if (Math.Abs(CurPo.PointList[i].X - CurPo.PointList[j].X) < MinDis && Math.Abs(CurPo.PointList[i].Y - CurPo.PointList[j].Y) < MinDis)
                        {
                            //double ax = CurPo.PointList[i].X; double ay = CurPo.PointList[i].Y;
                            //double bx = CurPo.PointList[j].X; double by = CurPo.PointList[j].Y;
                            //double abx = Math.Abs(bx - ax);
                            //double aby = Math.Abs(by - ay);

                            //double Test11 = CurPo.PointList[i].X - CurPo.PointList[j].X;
                            //double Test22 = CurPo.PointList[i].Y - CurPo.PointList[j].Y;
                            //double Test1 = Math.Abs(CurPo.PointList[i].X - CurPo.PointList[j].X);
                            //double Test2 = Math.Abs(CurPo.PointList[i].Y - CurPo.PointList[j].Y);
                            CurPo.PointList.RemoveAt(i);

                            //double Test111 = Math.Abs(CurPo.PointList[i].X - CurPo.PointList[j].X);
                            //double Test222 = Math.Abs(CurPo.PointList[i].Y - CurPo.PointList[j].Y);
                            ProcessLabel = true;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 删除重复节点
        /// </summary>
        /// <param name="CurPo"></param>
        /// <param name="MinDis"></param>
        /// <returns></returns>
        public PolygonObject DeleteSamePoint2(PolygonObject CurPo, double MinDis)
        {
            PolygonObject Po = new PolygonObject(0,CurPo.PointList);
            bool ProcessLabel = true;

            while (ProcessLabel)
            {
                ProcessLabel = false;
                for (int i = 0; i < Po.PointList.Count - 1; i++)
                {
                    for (int j = i + 1; j < Po.PointList.Count; j++)
                    {
                        if (Math.Abs(Po.PointList[i].X - Po.PointList[j].X) < MinDis && Math.Abs(Po.PointList[i].Y - Po.PointList[j].Y) < MinDis)
                        {
                            //double ax = CurPo.PointList[i].X; double ay = CurPo.PointList[i].Y;
                            //double bx = CurPo.PointList[j].X; double by = CurPo.PointList[j].Y;
                            //double abx = Math.Abs(bx - ax);
                            //double aby = Math.Abs(by - ay);

                            //double Test11 = CurPo.PointList[i].X - CurPo.PointList[j].X;
                            //double Test22 = CurPo.PointList[i].Y - CurPo.PointList[j].Y;
                            //double Test1 = Math.Abs(CurPo.PointList[i].X - CurPo.PointList[j].X);
                            //double Test2 = Math.Abs(CurPo.PointList[i].Y - CurPo.PointList[j].Y);
                            Po.PointList.RemoveAt(i);

                            //double Test111 = Math.Abs(CurPo.PointList[i].X - CurPo.PointList[j].X);
                            //double Test222 = Math.Abs(CurPo.PointList[i].Y - CurPo.PointList[j].Y);
                            ProcessLabel = true;
                            break;
                        }
                    }
                }
            }

            return Po;
        }

        /// <summary>
        /// 删除较小角度的点
        /// </summary>
        /// <param name="CurPo"></param>
        /// <param name="MinOrientation"></param>
        public void DeleteSmallAngle(PolygonObject CurPo, double MinOrientation)
        {
            //起点和终点在多边形点集中只存了一个点
            bool ProcessLable = true;

            while (ProcessLable)
            {
                ProcessLable = false;
                for (int i = 0; i < CurPo.PointList.Count; i++)
                {
                    #region 获得三个节点
                    TriNode TriNode1 = null;
                    TriNode TriNode2 = null;
                    TriNode CurNode = null;

                    CurNode = CurPo.PointList[i];
                    if (i == 0)
                    {
                        TriNode1 = CurPo.PointList[CurPo.PointList.Count - 1];
                        TriNode2 = CurPo.PointList[i + 1];
                    }

                    else if (i == CurPo.PointList.Count - 1)
                    {
                        TriNode1 = CurPo.PointList[i - 1];
                        TriNode2 = CurPo.PointList[0];
                    }

                    else
                    {
                        TriNode1 = CurPo.PointList[i - 1];
                        TriNode2 = CurPo.PointList[i + 1];
                    }
                    #endregion

                    #region 如果满足条件就删除该节点
                    double Angle = GetAngle(CurNode, TriNode1, TriNode2);
                    if (Math.Abs(Angle) < MinOrientation||Math.Abs(Angle+180)<MinOrientation)
                    {
                        CurPo.PointList.RemoveAt(i);
                        ProcessLable = true;
                        break;
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// 删除较小角度的点
        /// </summary>
        /// <param name="CurPo"></param>
        /// <param name="MinOrientation"></param>
        public PolygonObject DeleteSmallAngle2(PolygonObject CurPo, double MinOrientation)
        {
            //起点和终点在多边形点集中只存了一个点
            bool ProcessLable = true;
            PolygonObject Po = new PolygonObject(0, CurPo.PointList);

            while (ProcessLable)
            {
                ProcessLable = false;
                for (int i = 0; i < Po.PointList.Count; i++)
                {
                    #region 获得三个节点
                    TriNode TriNode1 = null;
                    TriNode TriNode2 = null;
                    TriNode CurNode = null;

                    CurNode = Po.PointList[i];
                    if (i == 0)
                    {
                        TriNode1 = Po.PointList[Po.PointList.Count - 1];
                        TriNode2 = Po.PointList[i + 1];
                    }

                    else if (i == Po.PointList.Count - 1)
                    {
                        TriNode1 = Po.PointList[i - 1];
                        TriNode2 = Po.PointList[0];
                    }

                    else
                    {
                        TriNode1 = Po.PointList[i - 1];
                        TriNode2 = Po.PointList[i + 1];
                    }
                    #endregion

                    #region 如果满足条件就删除该节点
                    double Angle = GetAngle(CurNode, TriNode1, TriNode2);
                    if (Math.Abs(Angle) < MinOrientation || Math.Abs(Angle + 180) < MinOrientation)
                    {
                        Po.PointList.RemoveAt(i);
                        ProcessLable = true;
                        break;
                    }
                    #endregion
                }
            }

            return Po;
        }

        /// <summary>
        /// 给定三点，计算该点的角度值
        /// </summary>
        /// <param name="curNode"></param>
        /// <param name="TriNode1"></param>
        /// <param name="TriNode2"></param>
        /// <returns></returns>
        public double GetAngle(TriNode curNode, TriNode TriNode1, TriNode TriNode2)
        {
            double Angle = 0;
            double a = Math.Sqrt((curNode.X - TriNode1.X) * (curNode.X - TriNode1.X) + (curNode.Y - TriNode1.Y) * (curNode.Y - TriNode1.Y));
            double b = Math.Sqrt((curNode.X - TriNode2.X) * (curNode.X - TriNode2.X) + (curNode.Y - TriNode2.Y) * (curNode.Y - TriNode2.Y));
            double c = Math.Sqrt((TriNode1.X - TriNode2.X) * (TriNode1.X - TriNode2.X) + (TriNode1.Y - TriNode2.Y) * (TriNode1.Y - TriNode2.Y));

            double CosCur = (a * a + b * b - c * c) / (2 * a * b);
            if (Math.Abs(CosCur + 1) < 0.00001)
            {
                Angle = 3.1415926;
            }
            else
            {
                Angle = Math.Acos(CosCur);
            }

            return Angle;
        }

        /// <summary>
        /// 获得最短的边上的两个节点
        /// </summary>
        /// <param name="CurPo"></param>
        /// <returns></returns>返回最短边的两个节点和最短边的长度
        public List<TriNode> GetShortestEdge(PolygonObject CurPo, out double ShortDis)
        {
            List<TriNode> ShortestEdgeNodes = new List<TriNode>();
            ShortDis = 100000000;

            #region 判断过程
            for (int i = 0; i < CurPo.PointList.Count; i++)
            {
                if (i == CurPo.PointList.Count-1)
                {
                    double Dis = Math.Sqrt((CurPo.PointList[0].X - CurPo.PointList[CurPo.PointList.Count - 1].X) * (CurPo.PointList[0].X - CurPo.PointList[CurPo.PointList.Count - 1].X)
                        + (CurPo.PointList[0].Y - CurPo.PointList[CurPo.PointList.Count - 1].Y) * (CurPo.PointList[0].Y - CurPo.PointList[CurPo.PointList.Count - 1].Y));

                    if (Dis < ShortDis)
                    {
                        ShortDis = Dis;

                        ShortestEdgeNodes.Clear();
                        ShortestEdgeNodes.Add(CurPo.PointList[0]);
                        ShortestEdgeNodes.Add(CurPo.PointList[CurPo.PointList.Count - 1]);
                    }
                }

                else
                {
                    double Dis = Math.Sqrt((CurPo.PointList[i].X - CurPo.PointList[i+1].X) * (CurPo.PointList[i].X - CurPo.PointList[i+1].X)
                       + (CurPo.PointList[i].Y - CurPo.PointList[i+1].Y) * (CurPo.PointList[i].Y - CurPo.PointList[i+1].Y));

                    if (Dis < ShortDis)
                    {
                        ShortDis = Dis;

                        ShortestEdgeNodes.Clear();
                        ShortestEdgeNodes.Add(CurPo.PointList[i]);
                        ShortestEdgeNodes.Add(CurPo.PointList[i + 1]);
                    }
                }
            }
            #endregion

            return ShortestEdgeNodes;
        }

        /// <summary>
        /// 获得最短边关联的结构
        /// </summary>
        /// <param name="ShortestEdge"></param>
        /// <param name="StructList"></param>
        /// <returns></returns>
        public List<BasicStruct> GetStructForEdge(List<TriNode> ShortestEdge, List<BasicStruct> StructList)
        {
            List<BasicStruct> InvovedStructs = new List<BasicStruct>();

            for (int i = 0; i < StructList.Count; i++)
            {
                for (int j = 0; j < StructList[i].PolylineList.Count; j++)
                {
                    if (StructList[i].PolylineList[j].PointList.Contains(ShortestEdge[0]) && StructList[i].PolylineList[j].PointList.Contains(ShortestEdge[1]))
                    {
                        InvovedStructs.Add(StructList[i]);
                    }
                }
            }

            return InvovedStructs;
        }

        /// <summary>
        /// 获得节点关联的结构
        /// </summary>
        /// <param name="CandidateNode"></param>
        /// <param name="StructList"></param>
        /// <returns></returns>
        public List<BasicStruct> GetStructForNode(TriNode CandidateNode, List<BasicStruct> StructList)
        {
            List<BasicStruct> InvovedStructs = new List<BasicStruct>();

            for (int i = 0; i < StructList.Count; i++)
            {
                if (this.NodesInStruct(CandidateNode, StructList[i]))
                {
                    InvovedStructs.Add(StructList[i]);
                }
            }

            return InvovedStructs;
        }

        /// <summary>
        /// 判断节点是否在基础结构上
        /// </summary>
        /// <param name="CandidateNode"></param>
        /// <param name="BS"></param>
        /// <returns></returns>
        public bool NodesInStruct(TriNode CandidateNode, BasicStruct BS)
        {
            bool NodesInStruct = false;
            ComFunLib CFL = new ComFunLib();

            for (int i = 0; i < BS.PolylineList.Count; i++)
            {
                double Dis = CFL.CalMinDisPoint2LineN(CandidateNode, BS.PolylineList[i].PointList[0], BS.PolylineList[i].PointList[BS.PolylineList[i].PointList.Count - 1]);
                {
                    if (Dis < 0.001)
                    {
                        NodesInStruct = true;
                        break;
                    }
                }
            }

             return NodesInStruct;
        }

        /// <summary>
        /// 获得一个建筑物中所有的基础结构(凸角、凹角、阶梯状结构、非直角)
        /// </summary>
        /// <param name="Curpo">PerAngle表示是直角的阈值</param>
        /// <returns></returns>
        public List<BasicStruct> GetStructedNodes(PolygonObject Curpo,double PerAngle)
        {
            List<BasicStruct> StructedList = new List<BasicStruct>();

            double Pi=3.1415926;//Pi的定义
            Curpo.GetBendAngle();//获得每一个节点的角度，区分正负号
            
            #region 获得建筑物中的每一条边
            List<PolylineObject> EdgeList=new List<PolylineObject>();
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

            #region 根据角度对结构进行判断（前后两个相邻的角是直角，则其是一个结构）
            for (int i = 0; i < Curpo.BendAngle.Count; i++)
            {
                #region 判断第一个角是否是直角
                if (Math.Abs(Math.Abs(Curpo.BendAngle[i][1]) - Pi / 2) < PerAngle)
                {
                    #region 如果不是最后一个点
                    if (i != Curpo.PointList.Count - 1)
                    {
                        #region 若第二个角也是直角
                        if (Math.Abs(Math.Abs(Curpo.BendAngle[i + 1][1]) - Pi / 2) < PerAngle)
                        {
                            #region 如果是第一个点
                            if (i == 0)
                            {
                                List<TriNode> NodeList = new List<TriNode>();
                                NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 1]);
                                NodeList.Add(Curpo.PointList[0]);
                                NodeList.Add(Curpo.PointList[1]);
                                NodeList.Add(Curpo.PointList[2]);

                                List<PolylineObject> CacheEdgeList = new List<PolylineObject>();
                                CacheEdgeList.Add(EdgeList[EdgeList.Count - 1]);
                                CacheEdgeList.Add(EdgeList[0]);
                                CacheEdgeList.Add(EdgeList[1]);

                                #region 是凸部或者凹部
                                if (Curpo.BendAngle[i][1] * Curpo.BendAngle[i + 1][1] > 0)
                                {
                                    //凸部
                                    if (Curpo.BendAngle[i][1] > 0)
                                    {
                                        BasicStruct CacheStruct = new BasicStruct(i, 2, NodeList, CacheEdgeList);
                                        StructedList.Add(CacheStruct);
                                    }

                                    //凹部
                                    else if (Curpo.BendAngle[i][1] <= 0)
                                    {
                                        BasicStruct CacheStruct = new BasicStruct(i, 3, NodeList, CacheEdgeList);
                                        StructedList.Add(CacheStruct);
                                    }

                                }
                                #endregion

                                #region 阶梯状
                                else
                                {
                                    BasicStruct CacheStruct = new BasicStruct(i, 4, NodeList, CacheEdgeList);
                                    StructedList.Add(CacheStruct);
                                }
                                #endregion
                            }
                            #endregion

                            #region 如果不是第一个点
                            else
                            {
                                List<TriNode> NodeList = new List<TriNode>();
                                List<PolylineObject> CacheEdgeList = new List<PolylineObject>();

                                if (i == Curpo.PointList.Count - 2)
                                {
                                    NodeList.Add(Curpo.PointList[i - 1]);
                                    NodeList.Add(Curpo.PointList[i]);
                                    NodeList.Add(Curpo.PointList[i + 1]);
                                    NodeList.Add(Curpo.PointList[0]);
                                }

                                else
                                {
                                    NodeList.Add(Curpo.PointList[i - 1]);
                                    NodeList.Add(Curpo.PointList[i]);
                                    NodeList.Add(Curpo.PointList[i + 1]);
                                    NodeList.Add(Curpo.PointList[i + 2]);
                                }

                                CacheEdgeList.Add(EdgeList[i - 1]);
                                CacheEdgeList.Add(EdgeList[i]);
                                CacheEdgeList.Add(EdgeList[i + 1]);

                                #region 是凸部或者凹部
                                if (Curpo.BendAngle[i][1] * Curpo.BendAngle[i + 1][1] > 0)
                                {
                                    //凸部
                                    if (Curpo.BendAngle[i][1] > 0)
                                    {
                                        BasicStruct CacheStruct = new BasicStruct(i, 2, NodeList, CacheEdgeList);
                                        StructedList.Add(CacheStruct);
                                    }

                                    //凹部
                                    else if (Curpo.BendAngle[i][1] <= 0)
                                    {
                                        BasicStruct CacheStruct = new BasicStruct(i, 3, NodeList, CacheEdgeList);
                                        StructedList.Add(CacheStruct);
                                    }

                                }
                                #endregion

                                #region 阶梯状
                                else
                                {
                                    BasicStruct CacheStruct = new BasicStruct(i, 4, NodeList, CacheEdgeList);
                                    StructedList.Add(CacheStruct);
                                }
                                #endregion
                            }
                            #endregion
                        }
                        #endregion

                        #region 若第二个角不是直角；判断前一个角是否是直角
                        else
                        {
                            #region 如果是第一个点
                            if (i == 0)
                            {
                                #region 如果前一个角也不是直角,则是一个单独的直角结构，即非特殊结构
                                if ((Curpo.BendAngle[Curpo.PointList.Count - 1][1] - Pi / 2) > PerAngle)
                                {
                                    List<TriNode> NodeList = new List<TriNode>();
                                    NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 1]);
                                    NodeList.Add(Curpo.PointList[0]);
                                    NodeList.Add(Curpo.PointList[1]);

                                    List<PolylineObject> CacheEdgeList = new List<PolylineObject>();
                                    CacheEdgeList.Add(EdgeList[EdgeList.Count - 1]);
                                    CacheEdgeList.Add(EdgeList[0]);

                                    BasicStruct CacheStruct = new BasicStruct(i, 1, NodeList, CacheEdgeList);
                                    StructedList.Add(CacheStruct);
                                }
                                #endregion
                            }
                            #endregion

                            #region 如果不是第一个点
                            else
                            {
                                #region 如果前一个角也不是直角，则是一个单独的直角结构，即非特殊结构
                                if ((Curpo.BendAngle[i - 1][1] - Pi / 2) > PerAngle)
                                {
                                    List<TriNode> NodeList = new List<TriNode>();
                                    NodeList.Add(Curpo.PointList[i - 1]);
                                    NodeList.Add(Curpo.PointList[i]);
                                    NodeList.Add(Curpo.PointList[i + 1]);

                                    List<PolylineObject> CacheEdgeList = new List<PolylineObject>();
                                    CacheEdgeList.Add(EdgeList[i - 1]);
                                    CacheEdgeList.Add(EdgeList[i]);

                                    BasicStruct CacheStruct = new BasicStruct(i, 1, NodeList, CacheEdgeList);
                                    StructedList.Add(CacheStruct);
                                }
                                #endregion
                            }
                            #endregion
                        }
                        #endregion
                    }
                    #endregion

                    #region 如果是最后一个点
                    else
                    {
                        #region 若第二个角也是直角
                        if (Math.Abs(Math.Abs(Curpo.BendAngle[0][1]) - Pi / 2) < PerAngle)
                        {
                            List<TriNode> NodeList = new List<TriNode>();
                            NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 2]);
                            NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 1]);
                            NodeList.Add(Curpo.PointList[0]);
                            NodeList.Add(Curpo.PointList[1]);

                            List<PolylineObject> CacheEdgeList = new List<PolylineObject>();
                            CacheEdgeList.Add(EdgeList[EdgeList.Count - 2]);
                            CacheEdgeList.Add(EdgeList[EdgeList.Count - 1]);
                            CacheEdgeList.Add(EdgeList[0]);

                            #region 是凸部或者凹部
                            if (Curpo.BendAngle[i][1] * Curpo.BendAngle[0][1] > 0)
                            {
                                //凸部
                                if (Curpo.BendAngle[i][1] > 0)
                                {
                                    BasicStruct CacheStruct = new BasicStruct(i, 2, NodeList, CacheEdgeList);
                                    StructedList.Add(CacheStruct);
                                }

                                //凹部
                                else if (Curpo.BendAngle[i][1] <= 0)
                                {
                                    BasicStruct CacheStruct = new BasicStruct(i, 3, NodeList, CacheEdgeList);
                                    StructedList.Add(CacheStruct);
                                }

                            }
                            #endregion

                            #region 阶梯状
                            else
                            {
                                BasicStruct CacheStruct = new BasicStruct(i, 4, NodeList, CacheEdgeList);
                                StructedList.Add(CacheStruct);
                            }
                            #endregion
                        }
                        #endregion

                        #region 若第二个角不是直角；判断前一个角是否是直角
                        else
                        {
                            if (Math.Abs(Math.Abs(Curpo.BendAngle[i - 1][1]) - Pi / 2) > PerAngle)
                            {
                                List<TriNode> NodeList = new List<TriNode>();
                                NodeList.Add(Curpo.PointList[i - 1]);
                                NodeList.Add(Curpo.PointList[i]);
                                NodeList.Add(Curpo.PointList[0]);

                                List<PolylineObject> CacheEdgeList = new List<PolylineObject>();
                                CacheEdgeList.Add(EdgeList[i - 1]);
                                CacheEdgeList.Add(EdgeList[i]);

                                BasicStruct CacheStruct = new BasicStruct(i, 1, NodeList, CacheEdgeList);
                                StructedList.Add(CacheStruct);
                            }
                        }
                        #endregion
                    }
                    #endregion
                }
                #endregion

                #region 若该角不是直角
                else
                {
                    List<TriNode> NodeList = new List<TriNode>();
                    List<PolylineObject> CacheEdgeList = new List<PolylineObject>();

                    if (i == 0)
                    {
                        NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 1]);
                        NodeList.Add(Curpo.PointList[0]);
                        NodeList.Add(Curpo.PointList[1]);

                        CacheEdgeList.Add(EdgeList[EdgeList.Count - 1]);
                        CacheEdgeList.Add(EdgeList[0]);
                    }

                    else if (i == Curpo.PointList.Count - 1)
                    {
                        NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 2]);
                        NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 1]);
                        NodeList.Add(Curpo.PointList[0]);

                        CacheEdgeList.Add(EdgeList[EdgeList.Count-2]);
                        CacheEdgeList.Add(EdgeList[EdgeList.Count - 1]);

                    }

                    else
                    {
                        NodeList.Add(Curpo.PointList[i-1]);
                        NodeList.Add(Curpo.PointList[i]);
                        NodeList.Add(Curpo.PointList[i+1]);

                        CacheEdgeList.Add(EdgeList[i-1]);
                        CacheEdgeList.Add(EdgeList[i]);
                    }

                    BasicStruct CacheStruct = new BasicStruct(i, 1, NodeList, CacheEdgeList);
                    StructedList.Add(CacheStruct);
                }
                #endregion
            }
            #endregion

            return StructedList;
        }

        /// <summary>
        /// 获得图形上的所有结构（建筑物简化投稿用）
        /// (凸角、凹角、阶梯状结构、非直角、Corner)//不要求两个角都是直角（至少有一个角是直角）
        /// </summary>
        /// <param name="Curpo"></param>
        /// <param name="PerAngel"></param>
        /// <returns></returns>
        public List<BasicStruct> GetStructedNodes2(PolygonObject Curpo, double PerAngle)
        {
            #region 符号化测试
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
            object PolygonSymbol = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);
            #endregion

            List<BasicStruct> StructedList = new List<BasicStruct>();

            double Pi = 3.1415926;//Pi的定义
            Curpo.GetBendAngle();//获得每一个节点的角度，区分正负号

            #region 测试
            //for (int i = 0; i < Curpo.BendAngle.Count; i++)
            //{
            //    double CacheAngle = Curpo.BendAngle[i][1] * 180 / 3.1415926;

            //    if (CacheAngle < 0)
            //    {
            //        CacheAngle = 360 + CacheAngle;
            //    }

            //    Console.WriteLine(CacheAngle.ToString());
            //}
            #endregion

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

            #region 首先获得所有转折（依次判断一个转折）
            for (int i = 0; i < Curpo.BendAngle.Count; i++)
            {
                List<TriNode> NodeList = new List<TriNode>();
                List<PolylineObject> CacheEdgeList = new List<PolylineObject>();
                List<double> NodeAngle = new List<double>();

                if (i == 0)
                {
                    NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 1]);
                    NodeList.Add(Curpo.PointList[0]);
                    NodeList.Add(Curpo.PointList[1]);

                    NodeAngle.Add(Curpo.BendAngle[Curpo.BendAngle.Count - 1][1]);
                    NodeAngle.Add(Curpo.BendAngle[0][1]);
                    NodeAngle.Add(Curpo.BendAngle[1][1]);

                    CacheEdgeList.Add(EdgeList[EdgeList.Count - 1]);
                    CacheEdgeList.Add(EdgeList[0]);
                }

                else if (i == Curpo.BendAngle.Count - 1)
                {
                    NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 2]);
                    NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 1]);
                    NodeList.Add(Curpo.PointList[0]);

                    NodeAngle.Add(Curpo.BendAngle[Curpo.BendAngle.Count - 2][1]);
                    NodeAngle.Add(Curpo.BendAngle[Curpo.BendAngle.Count - 2][1]);
                    NodeAngle.Add(Curpo.BendAngle[0][1]);

                    CacheEdgeList.Add(EdgeList[EdgeList.Count - 2]);
                    CacheEdgeList.Add(EdgeList[EdgeList.Count - 1]);
                }

                else
                {
                    NodeList.Add(Curpo.PointList[i - 1]);
                    NodeList.Add(Curpo.PointList[i]);
                    NodeList.Add(Curpo.PointList[i + 1]);

                    NodeAngle.Add(Curpo.BendAngle[i-1][1]);
                    NodeAngle.Add(Curpo.BendAngle[i][1]);
                    NodeAngle.Add(Curpo.BendAngle[i+1][1]);

                    CacheEdgeList.Add(EdgeList[i - 1]);
                    CacheEdgeList.Add(EdgeList[i]);
                }

                p++;
                BasicStruct CacheStruct = new BasicStruct(p, 1, NodeList, CacheEdgeList,NodeAngle);
                StructedList.Add(CacheStruct);
            }
            #endregion

            #region 其次，判断特殊结构（依次判断邻近两个转折）
            for (int i = 0; i < Curpo.BendAngle.Count; i++)
            {
                if (i == Curpo.BendAngle.Count - 1)
                {
                    double BendAngle1 = Curpo.BendAngle[i][1]; //获得两个bend的角度
                    double BendAngle2 = Curpo.BendAngle[0][1];

                    #region 至少有一个直角
                    if (Math.Abs(Math.Abs(BendAngle1) - Pi / 2) < PerAngle || Math.Abs(Math.Abs(BendAngle2) - Pi / 2) < PerAngle)
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

                    #region 没有直角，判断是否存在corner
                    else
                    {
                        PolylineObject Line1=EdgeList[EdgeList.Count - 2];
                        PolylineObject Line2=EdgeList[0];

                        TriNode TriNode1 = new TriNode(Line1.PointList[0].X, Line1.PointList[0].Y); 
                        TriNode TriNode2 = Line1.PointList[Line1.PointList.Count - 1];
                        TriNode TriNode3 = Line2.PointList[0]; TriNode TriNode4 = Line2.PointList[Line2.PointList.Count - 1];

                        double dx = TriNode3.X - TriNode2.X; double dy = TriNode3.Y - TriNode2.Y;
                        TriNode1.X = TriNode1.X + dx; TriNode1.Y = TriNode1.Y + dy;

                        double Angle = Curpo.GetAngle(TriNode1, TriNode3, TriNode4);
                        if (Math.Abs(Angle - Pi / 2) < PerAngle)
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

                            p++;
                            BasicStruct CacheStruct = new BasicStruct(p, 5, NodeList, CacheEdgeList, NodeAngle);//corner
                            StructedList.Add(CacheStruct);
                        }
                    }
                    #endregion
                }

                else
                {
                    double BendAngle1 = Curpo.BendAngle[i][1]; //获得两个bend的角度
                    double BendAngle2 = Curpo.BendAngle[i+1][1];


                    //测试
                    //IPolygon CacheCurPo = this.PolygonObjectConvert(Curpo);
                    //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
                    //pMapControl.Map.RecalcFullExtent();

                    #region 至少有一个直角
                    if (Math.Abs(Math.Abs(BendAngle1) - Pi / 2) < PerAngle || Math.Abs(Math.Abs(BendAngle2) - Pi / 2) < PerAngle)
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

                    #region 判断是否有corner
                    if (!(Math.Abs(Math.Abs(BendAngle1) - Pi / 2) < PerAngle || Math.Abs(Math.Abs(BendAngle2) - Pi / 2) < PerAngle))
                    {
                        #region 若是第一个点
                        if (i == 0)
                        {
                           PolylineObject Line1=EdgeList[EdgeList.Count - 1];
                           PolylineObject Line2=EdgeList[1];

                           TriNode TriNode1 = new TriNode(Line1.PointList[0].X, Line1.PointList[0].Y); 
                           TriNode TriNode2 = Line1.PointList[Line1.PointList.Count - 1];
                           TriNode TriNode3 = Line2.PointList[0]; TriNode TriNode4 = Line2.PointList[Line2.PointList.Count - 1];

                           double dx = TriNode3.X - TriNode2.X; double dy = TriNode3.Y - TriNode2.Y;
                           TriNode1.X = TriNode1.X + dx; TriNode1.Y = TriNode1.Y + dy;

                           double Angle = Curpo.GetAngle(TriNode1, TriNode3, TriNode4);

                           if (Math.Abs(Angle - Pi / 2) < PerAngle)
                           {
                               List<TriNode> NodeList = new List<TriNode>();
                               List<PolylineObject> CacheEdgeList = new List<PolylineObject>();
                               List<double> NodeAngle = new List<double>();

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

                               p++;
                               BasicStruct CacheStruct = new BasicStruct(p, 5, NodeList, CacheEdgeList, NodeAngle);//corner
                               StructedList.Add(CacheStruct);
                           }
                        }
                        #endregion

                        #region 若是倒数第二个点
                        else if (i == Curpo.BendAngle.Count - 2)
                        {
                            PolylineObject Line1 = EdgeList[i - 1];
                            PolylineObject Line2 = EdgeList[i + 1];

                            TriNode TriNode1 = new TriNode(Line1.PointList[0].X, Line1.PointList[0].Y); 
                            TriNode TriNode2 = Line1.PointList[Line1.PointList.Count - 1];
                            TriNode TriNode3 = Line2.PointList[0]; TriNode TriNode4 = Line2.PointList[Line2.PointList.Count - 1];

                            double dx = TriNode3.X - TriNode2.X; double dy = TriNode3.Y - TriNode2.Y;
                            TriNode1.X = TriNode1.X + dx; TriNode1.Y = TriNode1.Y + dy;

                            double Angle = Curpo.GetAngle(TriNode1, TriNode3, TriNode4);

                            if (Math.Abs(Angle - Pi / 2) < PerAngle)
                            {
                                List<TriNode> NodeList = new List<TriNode>();
                                List<PolylineObject> CacheEdgeList = new List<PolylineObject>();
                                List<double> NodeAngle = new List<double>();

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

                                p++;
                                BasicStruct CacheStruct = new BasicStruct(p, 5, NodeList, CacheEdgeList, NodeAngle);//corner
                                StructedList.Add(CacheStruct);
                            }
                        }
                        #endregion

                        #region 其它
                        else
                        {
                            PolylineObject Line1 = EdgeList[i - 1];
                            PolylineObject Line2 = EdgeList[i + 1];

                            TriNode TriNode1 = new TriNode(Line1.PointList[0].X, Line1.PointList[0].Y); 
                            TriNode TriNode2 = Line1.PointList[Line1.PointList.Count - 1];
                            TriNode TriNode3 = Line2.PointList[0]; TriNode TriNode4 = Line2.PointList[Line2.PointList.Count - 1];

                            double dx = TriNode3.X - TriNode2.X; double dy = TriNode3.Y - TriNode2.Y;
                            TriNode1.X = TriNode1.X + dx; TriNode1.Y = TriNode1.Y + dy;

                            double Angle = Curpo.GetAngle(TriNode1, TriNode3, TriNode4);

                            if (Math.Abs(Angle - Pi / 2) < PerAngle)
                            {
                                List<TriNode> NodeList = new List<TriNode>();
                                List<PolylineObject> CacheEdgeList = new List<PolylineObject>();
                                List<double> NodeAngle = new List<double>();

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

                                p++;
                                BasicStruct CacheStruct = new BasicStruct(p, 5, NodeList, CacheEdgeList, NodeAngle);//corner
                                StructedList.Add(CacheStruct);
                            }

                            //测试
                            //CacheCurPo = this.PolygonObjectConvert(Curpo);
                            //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
                            //pMapControl.Map.RecalcFullExtent();
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
        /// 获得图形上的所有结构（建筑物简化投稿用）
        /// (凸角、凹角、阶梯状结构、非直角、Corner)//要求两个角都是直角（至少有一个角是直角）
        /// </summary>
        /// <param name="Curpo"></param>
        /// <param name="PerAngel"></param>
        /// <returns></returns>
        public List<BasicStruct> GetStructedNodes3(PolygonObject Curpo, double PerAngle)
        {
            #region 符号化测试
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
            object PolygonSymbol = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);
            #endregion

            List<BasicStruct> StructedList = new List<BasicStruct>();

            double Pi = 3.1415926;//Pi的定义
            Curpo.GetBendAngle();//获得每一个节点的角度，区分正负号

            #region 测试
            //for (int i = 0; i < Curpo.BendAngle.Count; i++)
            //{
            //    double CacheAngle = Curpo.BendAngle[i][1] * 180 / 3.1415926;

            //    if (CacheAngle < 0)
            //    {
            //        CacheAngle = 360 + CacheAngle;
            //    }

            //    Console.WriteLine(CacheAngle.ToString());
            //}
            #endregion

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

            #region 首先获得所有转折（依次判断一个转折）
            for (int i = 0; i < Curpo.BendAngle.Count; i++)
            {
                List<TriNode> NodeList = new List<TriNode>();
                List<PolylineObject> CacheEdgeList = new List<PolylineObject>();
                List<double> NodeAngle = new List<double>();

                if (i == 0)
                {
                    NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 1]);
                    NodeList.Add(Curpo.PointList[0]);
                    NodeList.Add(Curpo.PointList[1]);

                    NodeAngle.Add(Curpo.BendAngle[Curpo.BendAngle.Count - 1][1]);
                    NodeAngle.Add(Curpo.BendAngle[0][1]);
                    NodeAngle.Add(Curpo.BendAngle[1][1]);

                    CacheEdgeList.Add(EdgeList[EdgeList.Count - 1]);
                    CacheEdgeList.Add(EdgeList[0]);
                }

                else if (i == Curpo.BendAngle.Count - 1)
                {
                    NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 2]);
                    NodeList.Add(Curpo.PointList[Curpo.PointList.Count - 1]);
                    NodeList.Add(Curpo.PointList[0]);

                    NodeAngle.Add(Curpo.BendAngle[Curpo.BendAngle.Count - 2][1]);
                    NodeAngle.Add(Curpo.BendAngle[Curpo.BendAngle.Count - 2][1]);
                    NodeAngle.Add(Curpo.BendAngle[0][1]);

                    CacheEdgeList.Add(EdgeList[EdgeList.Count - 2]);
                    CacheEdgeList.Add(EdgeList[EdgeList.Count - 1]);
                }

                else
                {
                    NodeList.Add(Curpo.PointList[i - 1]);
                    NodeList.Add(Curpo.PointList[i]);
                    NodeList.Add(Curpo.PointList[i + 1]);

                    NodeAngle.Add(Curpo.BendAngle[i - 1][1]);
                    NodeAngle.Add(Curpo.BendAngle[i][1]);
                    NodeAngle.Add(Curpo.BendAngle[i + 1][1]);

                    CacheEdgeList.Add(EdgeList[i - 1]);
                    CacheEdgeList.Add(EdgeList[i]);
                }

                p++;
                BasicStruct CacheStruct = new BasicStruct(p, 1, NodeList, CacheEdgeList, NodeAngle);
                StructedList.Add(CacheStruct);
            }
            #endregion

            #region 其次，判断特殊结构（依次判断邻近两个转折）
            for (int i = 0; i < Curpo.BendAngle.Count; i++)
            {
                if (i == Curpo.BendAngle.Count - 1)
                {
                    double BendAngle1 = Curpo.BendAngle[i][1]; //获得两个bend的角度
                    double BendAngle2 = Curpo.BendAngle[0][1];

                    #region 至少有一个直角
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

                    #region 没有直角，判断是否存在corner
                    if(!(Math.Abs(Math.Abs(BendAngle1) - Pi / 2) < PerAngle || Math.Abs(Math.Abs(BendAngle2) - Pi / 2) < PerAngle))
                    {
                        PolylineObject Line1 = EdgeList[EdgeList.Count - 2];
                        PolylineObject Line2 = EdgeList[0];

                        TriNode TriNode1 = new TriNode(Line1.PointList[0].X, Line1.PointList[0].Y);
                        TriNode TriNode2 = Line1.PointList[Line1.PointList.Count - 1];
                        TriNode TriNode3 = Line2.PointList[0]; TriNode TriNode4 = Line2.PointList[Line2.PointList.Count - 1];

                        double dx = TriNode3.X - TriNode2.X; double dy = TriNode3.Y - TriNode2.Y;
                        TriNode1.X = TriNode1.X + dx; TriNode1.Y = TriNode1.Y + dy;

                        double Angle = Curpo.GetAngle(TriNode3, TriNode1, TriNode4);
                        if (Math.Abs(Angle - Pi / 2) < PerAngle)
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

                            p++;
                            BasicStruct CacheStruct = new BasicStruct(p, 5, NodeList, CacheEdgeList, NodeAngle);//corner
                            StructedList.Add(CacheStruct);
                        }
                    }
                    #endregion
                }

                else
                {
                    double BendAngle1 = Curpo.BendAngle[i][1]; //获得两个bend的角度
                    double BendAngle2 = Curpo.BendAngle[i + 1][1];


                    //测试
                    //IPolygon CacheCurPo = this.PolygonObjectConvert(Curpo);
                    //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
                    //pMapControl.Map.RecalcFullExtent();

                    #region 至少有一个直角
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

                    #region 判断是否有corner
                    if (!(Math.Abs(Math.Abs(BendAngle1) - Pi / 2) < PerAngle || Math.Abs(Math.Abs(BendAngle2) - Pi / 2) < PerAngle))
                    {
                        #region 若是第一个点
                        if (i == 0)
                        {
                            PolylineObject Line1 = EdgeList[EdgeList.Count - 1];
                            PolylineObject Line2 = EdgeList[1];

                            TriNode TriNode1 = new TriNode(Line1.PointList[0].X, Line1.PointList[0].Y);
                            TriNode TriNode2 = Line1.PointList[Line1.PointList.Count - 1];
                            TriNode TriNode3 = Line2.PointList[0]; TriNode TriNode4 = Line2.PointList[Line2.PointList.Count - 1];

                            double dx = TriNode3.X - TriNode2.X; double dy = TriNode3.Y - TriNode2.Y;
                            TriNode1.X = TriNode1.X + dx; TriNode1.Y = TriNode1.Y + dy;

                            double Angle = Curpo.GetAngle(TriNode3, TriNode1, TriNode4);

                            if (Math.Abs(Angle - Pi / 2) < PerAngle)
                            {
                                List<TriNode> NodeList = new List<TriNode>();
                                List<PolylineObject> CacheEdgeList = new List<PolylineObject>();
                                List<double> NodeAngle = new List<double>();

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

                                p++;
                                BasicStruct CacheStruct = new BasicStruct(p, 5, NodeList, CacheEdgeList, NodeAngle);//corner
                                StructedList.Add(CacheStruct);
                            }
                        }
                        #endregion

                        #region 若是倒数第二个点
                        else if (i == Curpo.BendAngle.Count - 2)
                        {
                            PolylineObject Line1 = EdgeList[i - 1];
                            PolylineObject Line2 = EdgeList[i + 1];

                            TriNode TriNode1 = new TriNode(Line1.PointList[0].X, Line1.PointList[0].Y);
                            TriNode TriNode2 = Line1.PointList[Line1.PointList.Count - 1];
                            TriNode TriNode3 = Line2.PointList[0]; TriNode TriNode4 = Line2.PointList[Line2.PointList.Count - 1];

                            double dx = TriNode3.X - TriNode2.X; double dy = TriNode3.Y - TriNode2.Y;
                            TriNode1.X = TriNode1.X + dx; TriNode1.Y = TriNode1.Y + dy;

                            double Angle = Curpo.GetAngle(TriNode3, TriNode1, TriNode4);

                            if (Math.Abs(Angle - Pi / 2) < PerAngle)
                            {
                                List<TriNode> NodeList = new List<TriNode>();
                                List<PolylineObject> CacheEdgeList = new List<PolylineObject>();
                                List<double> NodeAngle = new List<double>();

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

                                p++;
                                BasicStruct CacheStruct = new BasicStruct(p, 5, NodeList, CacheEdgeList, NodeAngle);//corner
                                StructedList.Add(CacheStruct);
                            }
                        }
                        #endregion

                        #region 其它
                        else
                        {
                            PolylineObject Line1 = EdgeList[i - 1];
                            PolylineObject Line2 = EdgeList[i + 1];

                            TriNode TriNode1 = new TriNode(Line1.PointList[0].X, Line1.PointList[0].Y);
                            TriNode TriNode2 = Line1.PointList[Line1.PointList.Count - 1];
                            TriNode TriNode3 = Line2.PointList[0]; TriNode TriNode4 = Line2.PointList[Line2.PointList.Count - 1];
                            TriNode TriNode5 = new TriNode(Line1.PointList[0].X, Line1.PointList[0].Y);

                            double dx = TriNode3.X - TriNode2.X; double dy = TriNode3.Y - TriNode2.Y;
                            TriNode1.X = TriNode1.X + dx; TriNode1.Y = TriNode1.Y + dy;

                            double Angle = Curpo.GetAngle(TriNode3, TriNode1, TriNode4);                     

                            if (Math.Abs(Angle - Pi / 2) < PerAngle)
                            {
                                List<TriNode> NodeList = new List<TriNode>();
                                List<PolylineObject> CacheEdgeList = new List<PolylineObject>();
                                List<double> NodeAngle = new List<double>();

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

                                IPoint CacheStartPoint1 = new PointClass();
                                IPoint CacheStartPoint2 = new PointClass();
                                IPoint CacheStartPoint3 = new PointClass();
                                IPoint CacheStartPoint4 = new PointClass();
                                IPoint CacheStartPoint5 = new PointClass();
                                CacheStartPoint1.X = TriNode1.X; CacheStartPoint1.Y = TriNode1.Y;
                                CacheStartPoint2.X = TriNode3.X; CacheStartPoint2.Y = TriNode3.Y;
                                CacheStartPoint3.X = TriNode4.X; CacheStartPoint3.Y = TriNode4.Y;
                                CacheStartPoint4.X = TriNode2.X; CacheStartPoint4.Y = TriNode2.Y;
                                CacheStartPoint5.X = TriNode5.X; CacheStartPoint5.Y = TriNode5.Y;

                                object PointSb1 = Sb.PointSymbolization(255, 23, 140);
                                pMapControl.DrawShape(CacheStartPoint2, ref PointSb1);
                                pMapControl.DrawShape(CacheStartPoint3, ref PointSb1);
                                pMapControl.DrawShape(CacheStartPoint4, ref PointSb1);
                                pMapControl.DrawShape(CacheStartPoint5, ref PointSb1);

                                object PointSb = Sb.PointSymbolization(200, 200, 200);
                                pMapControl.DrawShape(CacheStartPoint1, ref PointSb);
                                pMapControl.DrawShape(CacheStartPoint2, ref PointSb);
                                pMapControl.DrawShape(CacheStartPoint3, ref PointSb);

                                
                                IActiveView pActiveView = pMapControl.ActiveView;
                                pActiveView.Refresh();

                                p++;
                                BasicStruct CacheStruct = new BasicStruct(p, 5, NodeList, CacheEdgeList, NodeAngle);//corner
                                StructedList.Add(CacheStruct);
                            }

                            //测试
                            //CacheCurPo = this.PolygonObjectConvert(Curpo);
                            //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
                            //pMapControl.Map.RecalcFullExtent();
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
        /// 获得建筑物的结构点
        /// </summary>
        /// <param name="Curpo"></param>
        /// <param name="PerAngle"></param>
        /// PreProcess表示是否预处理，如去重、去尖角、去共线点
        /// <returns></returns>
        public List<TriNode> GetNodeStruct(PolygonObject Curpo, double PerAngle,bool PreProcess)
        {
            List<TriNode> NodeList = new List<TriNode>();
            PolygonObject CachePo = null;

            #region 预处理，消除共线点、尖锐点、重复点
            if (PreProcess)
            {
                CachePo=this.DeleteSamePoint2(Curpo, 0.0000001);
                this.DeleteOnLinePoint(CachePo, Math.PI / 36);
                this.DeleteSmallAngle(CachePo, Math.PI / 36);
            }
            #endregion

            List<BasicStruct> AllStruct = new List<BasicStruct>();
            AllStruct=this.GetStructedNodes3(CachePo, PerAngle);
            for (int i = 0; i < AllStruct.Count; i++)
            {
                if (AllStruct[i].Type != 1)
                {
                    TriNode PN1 = AllStruct[i].NodeList[1];
                    bool Lable1 = false;
                    TriNode PN2 = AllStruct[i].NodeList[2];
                    bool Lable2 = false;
                    for (int j = 0; j < NodeList.Count; j++)
                    {
                        if (Math.Abs(NodeList[j].X - PN1.X) < 0.0000001 && Math.Abs(NodeList[j].Y - PN1.Y) < 0.0000001)
                        {
                            Lable1 = true;
                        }
                        if (Math.Abs(NodeList[j].X - PN2.X) < 0.0000001 && Math.Abs(NodeList[j].Y - PN2.Y) < 0.0000001)
                        {
                            Lable2 = true;
                        }
                    }
                    if (!Lable1)
                    {
                        NodeList.Add(PN1);
                    }

                    if (!Lable2)
                    {
                        NodeList.Add(PN2);
                    }
                }
            }

             return NodeList;
        }

        /// <summary>
        /// 将建筑物转化为IPolygon
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
    }
}
