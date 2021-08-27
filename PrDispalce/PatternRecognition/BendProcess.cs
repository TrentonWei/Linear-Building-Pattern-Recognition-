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

namespace PrDispalce.PatternRecognition
{
    /// <summary>
    /// 处理弯曲的类，获取弯曲、获取弯曲的skeleton、获取弯曲的长度等（未考虑洞的情况！！！）
    /// </summary>
    class BendProcess
    {
        /// <summary>
        /// 获得给定凹点在凹部的最小可视距离
        /// </summary>
        /// <param name="Po"></param>
        /// <param name="ConvexNode"></param>
        /// <returns></returns>
        public double VisualDis(PolygonObject Po, TriNode ConvexNode)
        {
            double VisualDis = 0;
            List<TriNode> ConvexPart = this.GetConvexPart(Po, ConvexNode);
            ComFunLib CFL = new ComFunLib();
            VisualDis = CFL.CalMinDisPoint2LineN(ConvexNode, ConvexPart[0], ConvexPart[ConvexPart.Count - 1]);
            return VisualDis;
        }

        /// <summary>
        /// 对ske进行优化，删除起点不是建筑物节点的部分骨架
        /// refine规则：骨架线存在节点是建筑物内插后的某节点，但是该节点不是建筑物的原始节点，则删除该骨架
        /// </summary>
        /// ske=待refine的骨架；Po=原始建筑物；InterPo=内插后的建筑物
        public Skeleton SkeRefine(Skeleton ske,PolygonObject Po, PolygonObject InterPo)
        {
            for (int i = ske.Skeleton_ArcList.Count-1; i>-1; i--)
            {
                TriNode StartNode = ske.Skeleton_ArcList[i].PointList[0];
                TriNode EndNode = ske.Skeleton_ArcList[i].PointList[ske.Skeleton_ArcList[i].PointList.Count - 1];

                int PLabel = -1; int IPLabel = -1;
                for (int j = 0; j < InterPo.PointList.Count; j++)
                {
                    TriNode CacheNode = InterPo.PointList[j];
                    if((Math.Abs(CacheNode.X-StartNode.X)<0.0000001&&Math.Abs(CacheNode.Y-StartNode.Y)<0.0000001)||
                        (Math.Abs(CacheNode.X-EndNode.X)<0.0000001&&Math.Abs(CacheNode.Y-EndNode.Y)<0.0000001)||
                        (Math.Abs(CacheNode.X-(StartNode.X+EndNode.X)/2)<0.00001&&Math.Abs(CacheNode.Y-(StartNode.Y+EndNode.Y)/2)<0.00001))
                    {
                        IPLabel=1;
                    }
                }

                if(IPLabel==1)
                {
                    for (int j = 0; j < Po.PointList.Count; j++)
                    {
                        TriNode CacheNode = Po.PointList[j];
                        if ((Math.Abs(CacheNode.X - StartNode.X) < 0.0000001 && Math.Abs(CacheNode.Y - StartNode.Y) < 0.0000001) ||
                            (Math.Abs(CacheNode.X - EndNode.X) < 0.0000001 && Math.Abs(CacheNode.Y - EndNode.Y) < 0.0000001))
                        {
                            PLabel = 1;
                        }
                    }
                }

                if (IPLabel * PLabel < 0)
                {
                    ske.Skeleton_ArcList.RemoveAt(i);
                }
            }

            return ske;
        }

        /// <summary>
        /// 对ske进行优化，删除起点不是建筑物节点的部分骨架
        /// refine规则：骨架线存在节点是建筑物某条边上的点，但是该节点不是建筑物的原始节点，则删除该骨架
        /// </summary>
        /// ske=待refine的骨架；Po=原始建筑物；InterPo=内插后的建筑物
        public Skeleton SkeRefine2(Skeleton ske, PolygonObject Po, PolygonObject InterPo)
        {
            for (int i = ske.Skeleton_ArcList.Count - 1; i > -1; i--)
            {
                TriNode StartNode = ske.Skeleton_ArcList[i].PointList[0];
                TriNode EndNode = ske.Skeleton_ArcList[i].PointList[ske.Skeleton_ArcList[i].PointList.Count - 1];

                int PLabel = -1; int IPLabel = -1;
                for (int j = 0; j < InterPo.PointList.Count; j++)
                {
                    #region 获得判断的节点
                    TriNode CacheNode1 = InterPo.PointList[j];
                    TriNode CacheNode2 = null;
                    if (j == InterPo.PointList.Count - 1)
                    {
                        CacheNode2 = InterPo.PointList[0];
                    }
                    else
                    {
                        CacheNode2 = InterPo.PointList[j + 1];
                    }
                    #endregion

                    ComFunLib CFL = new ComFunLib();
                    double Dis1 = CFL.CalMinDisPoint2LineN(StartNode, CacheNode1, CacheNode2);
                    double Dis2 = CFL.CalMinDisPoint2LineN(EndNode, CacheNode1, CacheNode2);

                    if (Dis1 < 0.0000001 || Dis2 < 0.0000001)
                    {
                        IPLabel = 1;
                    }
                }

                if (IPLabel == 1)
                {
                    for (int j = 0; j < Po.PointList.Count; j++)
                    {
                        TriNode CacheNode = Po.PointList[j];
                        if ((Math.Abs(CacheNode.X - StartNode.X) < 0.0000001 && Math.Abs(CacheNode.Y - StartNode.Y) < 0.0000001) ||
                            (Math.Abs(CacheNode.X - EndNode.X) < 0.0000001 && Math.Abs(CacheNode.Y - EndNode.Y) < 0.0000001))
                        {
                            PLabel = 1;
                        }
                    }
                }

                if (IPLabel * PLabel < 0)
                {
                    ske.Skeleton_ArcList.RemoveAt(i);
                }
            }

            return ske;
        }

        /// <summary>
        /// 获得给定凹点所处的凹部
        /// </summary>
        /// <param name="Po"></param>
        /// <param name="ConvexNode"></param>
        /// <returns></returns>
        public List<TriNode> GetConvexPart(PolygonObject Po, TriNode ConvexNode)
        {
            List<TriNode> ConvexPart = new List<TriNode>();
            List<List<TriNode>> ConvexParts = this.GetConvexParts(Po);

            for (int i = 0; i < ConvexParts.Count; i++)
            {
                for (int j = 0; j < ConvexParts[i].Count; j++)
                {
                    if (Math.Abs(ConvexParts[i][j].X - ConvexNode.X) < 0.0000001 &&
                        Math.Abs(ConvexParts[i][j].Y - ConvexNode.Y) < 0.0000001)
                    {
                        ConvexPart = ConvexParts[i];
                        return ConvexPart;
                    }
                }
            }

            return ConvexPart;
        }

        /// <summary>
        /// 获得建筑物图形的所有凹部
        /// </summary>
        /// <param name="Po"></param>
        /// <returns></returns>
        public List<List<TriNode>> GetConvexParts(PolygonObject Po)
        {
            List<List<TriNode>> ConvexParts = new List<List<TriNode>>();

            #region 计算给定建筑物图形的凸包
            ConvexNull cn = new ConvexNull(Po.PointList);
            cn.CreateConvexNull();
            #endregion

            #region 获得凸包在建筑物图形上的对应点
            List<int> ConvexNullIndex = new List<int>();
            for (int i = 0; i < cn.ConvexVertexSet.Count-1; i++)
            {
                for (int j = 0; j < Po.PointList.Count; j++)
                {
                    if (Math.Abs(cn.ConvexVertexSet[i].X - Po.PointList[j].X )< 0.0000001 &&
                        Math.Abs(cn.ConvexVertexSet[i].Y - Po.PointList[j].Y)< 0.0000001)
                    {
                        ConvexNullIndex.Add(j);
                    }
                }
            }
            #endregion

            #region 获得ConvexPart
            ConvexNullIndex.Sort();
            #region 第一个点
            if (ConvexNullIndex[0] != 0 || ConvexNullIndex[ConvexNullIndex.Count - 1] != (Po.PointList.Count - 1))
            {
                List<TriNode> ConvexPart = new List<TriNode>();
                for (int p = ConvexNullIndex[ConvexNullIndex.Count - 1]; p < Po.PointList.Count; p++)
                {
                    ConvexPart.Add(Po.PointList[p]);
                }
                for (int p = 0; p < ConvexNullIndex[0] + 1; p++)
                {
                    ConvexPart.Add(Po.PointList[p]);
                }

                ConvexParts.Add(ConvexPart);
            }
            #endregion
            #region 后续点
            for (int i = 0; i < ConvexNullIndex.Count-1; i++)
            {
                if (ConvexNullIndex[i + 1] - ConvexNullIndex[i] > 1)
                {
                    List<TriNode> ConvexPart = new List<TriNode>();
                    for (int p = ConvexNullIndex[i]; p < ConvexNullIndex[i + 1] + 1; p++)
                    {
                        ConvexPart.Add(Po.PointList[p]);
                    }

                    ConvexParts.Add(ConvexPart);
                }
            }
            #endregion
            #endregion

            return ConvexParts;
        }

        /// <summary>
        /// 获取建筑物的凹点，同时，对节点的信息进行标记（已经有效考虑了洞的情况）
        /// 【即外轮廓大于180度，则为凹点；内轮廓小于180度，则为凹点——洞的节点编码顺序与外轮廓相反】
        /// </summary>
        /// <param name="Po"></param>
        /// <returns></returns>
        public List<TriNode> GetConcaveNode(PolygonObject Po,double OnlineT)
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
        /// 获取建筑物的凹点，同时，对节点的信息进行标记（已经有效考虑了洞的情况）
        /// 【即外轮廓大于180度，则为凹点；内轮廓小于180度，则为凹点——洞的节点编码顺序与外轮廓相反】
        /// </summary>
        /// <param name="Po"></param>
        /// <returns></returns>
        public List<TriNode> GetConcaveNode(Polygon Po,double OnlineT)
        {
            List<TriNode> ConvexNodeList = new List<TriNode>();
            SMap map2 = new SMap();
            map2.ReadDataFrmGivenPolygonObject(Po);
            ConvexNodeList = this.GetConcaveNode(map2.PolygonList,OnlineT);
            return ConvexNodeList;
        }

        /// <summary>
        /// 获取建筑物的凹点，同时，对节点的信息进行标记（已经有效考虑了洞的情况）
        /// 【即外轮廓大于180度，则为凹点；内轮廓小于180度，则为凹点——洞的节点编码顺序与外轮廓相反】
        /// </summary>
        /// <param name="Po"></param>
        /// <returns></returns>
        public List<TriNode> GetConcaveNode(List<PolygonObject> PoList,double OnLineT)
        {
            List<TriNode> ConcaveNodeList = new List<TriNode>();

            for (int i = 0; i < PoList.Count; i++)
            {
                List<TriNode> CacheNodeList = this.GetConcaveNode(PoList[i],OnLineT);
                ConcaveNodeList = ConcaveNodeList.Concat(CacheNodeList).ToList();
            }

            return ConcaveNodeList;
        }
        
        /// <summary>
        /// 计算建筑物图形上某节点出的角度
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
            double Angle = GetAngle(CurPoint, AfterPoint, BeforePoint);
            if (xMultiply < 0)
            {
                Angle = Angle * (-1);
            }
            #endregion

            return Angle;
        }

        /// <summary>
        /// 给定三点，计算该点的角度值
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
        /// 获取给定节点的弯曲深度（外环的深度）
        /// this.GetOutSkeArcLevel(ske, Po);执行此程序之前需要获取到每一个Arc的Level
        /// </summary>
        /// <param name="ConvexNode"></param>
        /// <param name="ske"></param>
        /// <returns></returns>
        public List<Skeleton_Arc> GetOutBendRoadForNodes(TriNode ConvexNode,Skeleton ske,PolygonObject Po)
        {           
            List<Skeleton_Arc> BendRoad = new List<Skeleton_Arc>();
            Skeleton_Arc StartSkeleton_Arc = this.GetOutStartSkeletonForNodes(ConvexNode, ske);
            BendRoad.Add(StartSkeleton_Arc);
            ske.Skeleton_ArcList.Remove(StartSkeleton_Arc);

            if (StartSkeleton_Arc != null)
            {
                bool Label = true;
                do
                {
                    Label = false;
                    foreach (Skeleton_Arc SA in ske.Skeleton_ArcList)
                    {
                        List<TriNode> IntersectedNodes = SA.PointList.Intersect(StartSkeleton_Arc.PointList).ToList();
                        if (IntersectedNodes.Count > 0 && (StartSkeleton_Arc.ArcLevel - SA.ArcLevel) == 1)
                        {
                            BendRoad.Add(SA);
                            StartSkeleton_Arc = SA;
                            ske.Skeleton_ArcList.Remove(SA);
                            Label = true;
                            break;
                        }
                    }

                } while (Label);
            }

            else
            {
                BendRoad = null;
            }
            return BendRoad;
        }

        /// <summary>
        /// 获得弯曲深度的长度
        /// </summary>
        /// <param name="Road"></param>
        /// <returns></returns>
        public double GetRoadLength(List<Skeleton_Arc> Road)
        {
            double RoadLength = 0;

            if (Road != null)
            {
                for (int i = 0; i < Road.Count; i++)
                {
                    RoadLength = RoadLength + Road[i].Length;
                }
            }

            else
            {
                RoadLength = 0;
            }

            return RoadLength;
        }

        /// <summary>
        ///获得凹点上对应外部弯曲的skeleton起点 
        /// </summary>
        /// <param name="ConvexNode"></param>
        /// <param name="ske"></param>
        /// <returns></returns>
        public Skeleton_Arc GetOutStartSkeletonForNodes(TriNode ConvexNode, Skeleton ske)
        {
            Skeleton_Arc OutStartSkeleton = null;

            for (int i = 0; i < ske.Skeleton_ArcList.Count; i++)
            {
                if (ske.Skeleton_ArcList[i].ArcOutIn == 1)
                {
                    if(ske.Skeleton_ArcList[i].PointList.Contains(ConvexNode))
                    {
                        OutStartSkeleton = ske.Skeleton_ArcList[i];
                        break;
                    }
                }
            }

            return OutStartSkeleton;
        }

        /// <summary>
        /// 标记ske中不同弧段的层次(执行此程序之前需要执行此过程)
        ///PrDispalce.建筑物聚合.TriangleProcess TP = new 建筑物聚合.TriangleProcess();
        ///TP.LabelInOutType(ske.CDT.DT.TriangleList, Po);//标记是内或外三角形
        ///TP.CommonEdgeTriangleLabel(ske.CDT.DT.TriangleList);//标记共边三角形
        ///TP.LabelBoundaryBuilding(ske.CDT.DT.TriangleList);//标记边缘三角形
        /// </summary>
        /// <param name="ske"></param>
        public void GetOutSkeArcLevel(Skeleton ske,PolygonObject Po)
        {
            List<Skeleton_Arc> OutSkeleton_Arc = this.GetOutSkeleton_Arc(ske, Po);
            List<Skeleton_Arc> FirstOutSkeleton_Arc = this.GetOutFirstSkeleton_Arc(ske);

            #region 移除第一层次Arc
            for (int i = 0; i < FirstOutSkeleton_Arc.Count; i++)
            {
                OutSkeleton_Arc.Remove(FirstOutSkeleton_Arc[i]);
            }
            #endregion

            int ArcLevel=1;
 
            do
            {
                ArcLevel++;
                List<Skeleton_Arc> CacheRemoveArc = new List<Skeleton_Arc>();
                #region 对ArcLevel编号
                for (int i = 0; i < OutSkeleton_Arc.Count; i++)
                {
                    for (int j = 0; j < FirstOutSkeleton_Arc.Count; j++)
                    {
                        List<TriNode> IntersectNode = OutSkeleton_Arc[i].PointList.Intersect(FirstOutSkeleton_Arc[j].PointList).ToList();
                        if (IntersectNode.Count > 0)
                        {
                            OutSkeleton_Arc[i].ArcLevel = ArcLevel;
                            CacheRemoveArc.Add(OutSkeleton_Arc[i]);                      
                        }
                    }
                }
                #endregion

                #region 移除上一层次编号
                for (int i = 0; i < CacheRemoveArc.Count; i++)
                {
                    OutSkeleton_Arc.Remove(CacheRemoveArc[i]);
                }
                FirstOutSkeleton_Arc = CacheRemoveArc;
                #endregion

            } while (OutSkeleton_Arc.Count > 0);
        }

        /// <summary>
        /// 获得建筑物图形外的Skleton_Arc
        /// </summary>
        /// <param name="ske"></param>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public List<Skeleton_Arc> GetOutSkeleton_Arc(Skeleton ske, PolygonObject Po)
        {
            List<Skeleton_Arc> OutSkeleton_Arcs = new List<Skeleton_Arc>();

            PrDispalce.PatternRecognition.TriangleProcess TP = new TriangleProcess();//TriangleProcess
            TP.LabelInOutType(ske.CDT.DT.TriangleList, Po);//标记是内或外三角形
            TP.CommonEdgeTriangleLabel(ske.CDT.DT.TriangleList);//标记共边三角形
            TP.LabelBoundaryBuilding(ske.CDT.DT.TriangleList);//标记边缘三角形

            foreach (Skeleton_Arc SA in ske.Skeleton_ArcList)
            {
                if (SA.TriangleList[0].InOutType == 0)
                {
                    OutSkeleton_Arcs.Add(SA);
                    SA.ArcOutIn = 1;
                }
            }

            return OutSkeleton_Arcs;
        }

        /// <summary>
        /// 返回建筑物内的Skeleton_Arcs
        /// </summary>
        /// <param name="ske"></param>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public List<Skeleton_Arc> GetInSkeleton_Arc(Skeleton ske, PolygonObject Po)
        {
            List<Skeleton_Arc> InSkeleton_Arcs = new List<Skeleton_Arc>();

            PrDispalce.PatternRecognition.TriangleProcess TP = new TriangleProcess();//TriangleProcess
            TP.LabelInOutType(ske.CDT.DT.TriangleList, Po);//标记是内或外三角形
            TP.CommonEdgeTriangleLabel(ske.CDT.DT.TriangleList);//标记共边三角形
            TP.LabelBoundaryBuilding(ske.CDT.DT.TriangleList);//标记边缘三角形

            foreach (Skeleton_Arc SA in ske.Skeleton_ArcList)
            {
                if (SA.TriangleList[0].InOutType == 1)
                {
                    InSkeleton_Arcs.Add(SA);
                    SA.ArcOutIn = 0;
                }
            }

            return InSkeleton_Arcs;
        }

        /// <summary>
        /// 返回最新的Skeleton_Arc
        /// </summary>
        /// <param name="ske"></param>
        /// <returns></returns>
        public List<Skeleton_Arc> GetOutFirstSkeleton_Arc(Skeleton ske)
        {
            List<Skeleton_Arc> FirstLevelSke_Arc = new List<Skeleton_Arc>();

            foreach (Skeleton_Arc SA in ske.Skeleton_ArcList)
            {
                foreach (Triangle Tri in SA.TriangleList)
                {
                    if (Tri.OutBoundary == 1)
                    {
                        FirstLevelSke_Arc.Add(SA);
                        SA.ArcLevel = 1;//表示是第一层次的ArcLevel
                        break;
                    }
                }
            }

            return FirstLevelSke_Arc;
        }

        /// <summary>
        /// 获得完全深度最深对应的节点
        /// </summary>
        /// <param name="BendLength">每一个节点对应的完全深度</param>
        /// <returns></returns>
        public TriNode GetDeepestNode(Dictionary<TriNode, double> BendLength)
        {
            double MaxLength = -1; TriNode TargetNode = null;
            foreach (KeyValuePair<TriNode, double> kv in BendLength)
            {
                if (kv.Value > MaxLength)
                {
                    MaxLength = kv.Value;
                    TargetNode = kv.Key;
                }
            }
            return TargetNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="BendLength"></param>
        /// <returns></returns>
        public double GetDeepestLength(Dictionary<TriNode, double> BendLength)
        {
            double MaxLength = -1; 
            foreach (KeyValuePair<TriNode, double> kv in BendLength)
            {
                if (kv.Value > MaxLength)
                {
                    MaxLength = kv.Value;
                }
            }
            return MaxLength;
        }
    }
}
