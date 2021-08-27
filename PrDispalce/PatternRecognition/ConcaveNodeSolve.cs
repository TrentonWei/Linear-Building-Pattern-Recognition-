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
    /// 处理潜在凹点的类，包括潜在处理方法、选择处理方法等
    /// </summary>
    class ConcaveNodeSolve
    {
        #region 属性
        AxMapControl pMapControl = null;
        ComFunLib CFL = new ComFunLib();
        PrDispalce.PatternRecognition.PolygonPreprocess PP = new PolygonPreprocess();
        #endregion

        #region 构造函数
        public ConcaveNodeSolve()
        {
        }

        public ConcaveNodeSolve(AxMapControl CacheControl)
        {
            this.pMapControl = CacheControl;
        }
        #endregion

        /// <summary>
        /// 获取给定凹点基于三角网的所有潜在切割点
        /// </summary>
        /// <param name="ConcaveNode"></param>
        /// <param name="cdt"></param>
        /// <returns></returns>
        public List<Cut> GetCuts(TriNode ConcaveNode, ConsDelaunayTin cdt,List<PolygonObject> PoList)
        {
            List<Cut> AllCuts = new List<Cut>();
            List<Cut> ResultCuts = new List<Cut>();

            #region 获取所有凹点关联的Cut
            foreach (TriEdge tE in cdt.TriEdgeList)
            {
                if (Math.Abs(tE.startPoint.Y - ConcaveNode.Y) < 0.0000001 && Math.Abs(tE.startPoint.X - ConcaveNode.X) < 0.0000001)
                {
                    Cut pCut = new Cut(tE);
                    AllCuts.Add(pCut);
                }
                else if (Math.Abs(tE.endPoint.Y - ConcaveNode.X) < 0.0000001 && Math.Abs(tE.endPoint.X - ConcaveNode.X) < 0.0000001)
                {
                    Cut pCut = new Cut(tE);
                    pCut.StartLable = true;
                    AllCuts.Add(pCut);
                }
            }
            #endregion

            #region 排除在建筑物上的Cut（即Cut是建筑物上的边）
            foreach (Cut pCut in AllCuts)
            {
                TriNode StartNode = pCut.CutEdge.startPoint;
                TriNode EndNode = pCut.CutEdge.endPoint;

                foreach (PolygonObject Po in PoList)
                {
                    int sIndex = -1000; int eIndex = -1000;
                    if (Po.PointList.Contains(StartNode))
                    {
                        sIndex = Po.PointList.IndexOf(StartNode);
                    }
                    if (Po.PointList.Contains(EndNode))
                    {
                        eIndex = Po.PointList.IndexOf(EndNode);
                    }

                    if (Math.Abs(sIndex - eIndex) == 1 || Math.Abs(sIndex - eIndex) == (Po.PointList.Count - 1))
                    {
                        pCut.OnBoundary = true;
                    }
                }
            }

            foreach (Cut pCut in AllCuts)
            {
                if (!pCut.OnBoundary)
                {
                    ResultCuts.Add(pCut);
                }
            }
            #endregion

            return ResultCuts;
        }

        /// <summary>
        /// 对给定的所有潜在分割进行判断（Cut必须能解决当前的凹部）
        /// 该函数的执行必须在计算潜在Cut的属性后执行
        /// 判断条件：每一个Cut裁剪后，裁剪的起点和终点产生的四个角均为凸
        /// </summary>
        /// <param name="pCut"></param>
        /// <returns></returns>
        public List<Cut> CutsRefine(List<Cut> pCut, double OnlineT)
        {
            List<Cut> RefinedCut = new List<Cut>();

            foreach (Cut kCut in pCut)
            {
                #region 判断Cut的四个角
                double CutAngle11=kCut.CutAngle11;
                double CutAngle12=kCut.CutAngle12;
                double CutAngle21=kCut.CutAngle21;
                double CutAngle22=kCut.CutAngle22;

                CutAngle11 =  CutAngle11 * 180 / 3.1415926;
                CutAngle12 =  CutAngle12 * 180 / 3.1415926;
                CutAngle21 =  CutAngle21 * 180 / 3.1415926;
                CutAngle22 =  CutAngle22 * 180 / 3.1415926;

                if (CutAngle11 < 0)
                {
                    CutAngle11 = 360 + CutAngle11;
                }
                if (CutAngle12 < 0)
                {
                    CutAngle12 = 360 + CutAngle12;
                }
                if (CutAngle21 < 0)
                {
                    CutAngle21 = 360 + CutAngle21;
                }
                if (CutAngle22 < 0)
                {
                    CutAngle22 = 360 + CutAngle22;
                }
                #endregion

                if (CutAngle11 <= (180+OnlineT) && CutAngle12 <= (180+OnlineT) && 
                    CutAngle21 <= (180+OnlineT) && CutAngle22 <= (180+OnlineT))
                {
                    RefinedCut.Add(kCut);
                }
            }

            return RefinedCut;
        }

        /// <summary>
        /// 获得最佳Cut（不考虑直角的情况）
        /// </summary>
        /// <param name="CutList">所有带属性的潜在Cut</param>
        /// <returns></returns>NodeConsider表示是否考虑节点连接（True表示考虑；false表示不考虑）
        /// OrthConsider表示是否考虑Cut节点直角的保持（True表示考虑；false表示不考虑）
        public Cut GetBestCut(List<Cut> CutList,bool NodeConsider,bool OrthConsider)
        {
            Cut TargetCut = null;

            List<Cut> TwoConcaveCut = new List<Cut>();
            List<Cut> OneConcaveCut = new List<Cut>();

            #region 判断连接的两个点是否是凹点
            foreach (Cut kCut in CutList)
            {
                #region 获得Cut处两个节点的角度
                double StartAngle = kCut.StartAngle;
                double EndAngle = kCut.EndAngle;

                StartAngle = StartAngle * 180 / 3.1415926;
                EndAngle = EndAngle * 180 / 3.1415926;

                if (StartAngle < 0)
                {
                    StartAngle = 360 + StartAngle;
                }
                if (EndAngle < 0)
                {
                    EndAngle = 360 + EndAngle;
                }
                #endregion

                if (StartAngle > 180 & EndAngle > 180)
                {
                    TwoConcaveCut.Add(kCut);
                }
                else
                {
                    OneConcaveCut.Add(kCut);
                }
            }
            #endregion

            #region 判断Cut的长度
            List<Cut> CutForLengthDecision = new List<Cut>();
            if (TwoConcaveCut.Count > 0)
            {
                CutForLengthDecision = TwoConcaveCut;
            }
            else
            {
                CutForLengthDecision = OneConcaveCut;
            }

            #region 不考虑节点且不考虑直角
            if (!NodeConsider && !OrthConsider)
            {
                double MinLength = 100000000;
                foreach (Cut kCut in CutForLengthDecision)
                {
                    if (kCut.CutLength < MinLength)
                    {
                        MinLength = kCut.CutLength;
                        TargetCut = kCut; ;
                    }
                }
            }
            #endregion

            #region 考虑节点且不考虑直角
            if (NodeConsider && !OrthConsider)
            {
                double MinLength = 100000000;
                foreach (Cut kCut in CutForLengthDecision)
                {
                    if (kCut.NodeCount == 1)
                    {
                        if (kCut.CutLength < MinLength)
                        {
                            MinLength = kCut.CutLength;
                            TargetCut = kCut; ;
                        }
                    }
                    else
                    {
                        if (kCut.CutLength * 0.8 < MinLength)
                        {
                            MinLength = kCut.CutLength * 0.8;
                            TargetCut = kCut; ;
                        }
                    }
                }
            }
            #endregion

            #region 不考虑节点且考虑直角
            if (!NodeConsider && OrthConsider)
            {
                double MinLength = 100000000;
                foreach (Cut kCut in CutForLengthDecision)
                {
                    if (!kCut.RightAngleMaintain)
                    {
                        if (kCut.CutLength < MinLength)
                        {
                            MinLength = kCut.CutLength;
                            TargetCut = kCut; ;
                        }
                    }
                    else
                    {
                        if (kCut.CutLength * 0.8 < MinLength)
                        {
                            MinLength = kCut.CutLength * 0.8;
                            TargetCut = kCut; ;
                        }
                    }
                }
            }
            #endregion

            #region 考虑节点且考虑直角
            if (NodeConsider && OrthConsider)
            {
                double MinLength = 100000000;
                foreach (Cut kCut in CutForLengthDecision)
                {
                    if (kCut.NodeCount == 1 && !kCut.RightAngleMaintain)
                    {
                        if (kCut.CutLength < MinLength)
                        {
                            MinLength = kCut.CutLength;
                            TargetCut = kCut; ;
                        }
                    }
                    else if ((kCut.NodeCount == 2 && !kCut.RightAngleMaintain) || (kCut.NodeCount == 1 && kCut.RightAngleMaintain))
                    {
                        if (kCut.CutLength * 0.8 < MinLength)
                        {
                            MinLength = kCut.CutLength * 0.8;
                            TargetCut = kCut; ;
                        }
                    }
                    else if (kCut.NodeCount == 2 && kCut.RightAngleMaintain)
                    {
                        if (kCut.CutLength * 0.8 * 0.8 < MinLength)
                        {
                            MinLength = kCut.CutLength * 0.8 * 0.8;
                            TargetCut = kCut; ;
                        }
                    }
                }
            }
            #endregion
            #endregion

            return TargetCut;
        }

        /// <summary>
        /// 获得最佳Cut（考虑直角和节点增加的数量！！）【明天修改！！】
        /// </summary>
        /// <param name="CutList">所有带属性的潜在Cut</param>
        /// <returns></returns>NodeConsider表示是否考虑节点连接（True表示考虑；false表示不考虑）
        /// OrthConsider表示是否考虑Cut节点直角的保持（True表示考虑；false表示不考虑）
        /// a表示考虑节点的权重
        /// b表示考虑直角的权重
        public Cut GetBestCut2(List<Cut> CutList, bool NodeConsider, bool OrthConsider,double a,double b)
        {
            Cut TargetCut = null;

            List<Cut> TwoConcaveCut = new List<Cut>();
            List<Cut> OneConcaveCut = new List<Cut>();

            #region 判断连接的两个点是否是凹点
            foreach (Cut kCut in CutList)
            {
                #region 获得Cut处两个节点的角度
                double StartAngle = kCut.StartAngle;
                double EndAngle = kCut.EndAngle;

                StartAngle = StartAngle * 180 / 3.1415926;
                EndAngle = EndAngle * 180 / 3.1415926;

                if (StartAngle < 0)
                {
                    StartAngle = 360 + StartAngle;
                }
                if (EndAngle < 0)
                {
                    EndAngle = 360 + EndAngle;
                }
                #endregion

                if (StartAngle > 180.5 & EndAngle > 180.5) //防止直线点被误以为是多重节点！
                {
                    TwoConcaveCut.Add(kCut);
                }
                else
                {
                    OneConcaveCut.Add(kCut);
                }
            }
            #endregion

            #region 判断Cut的长度
            List<Cut> CutForLengthDecision = new List<Cut>();

            if (TwoConcaveCut.Count > 0)
            {
                CutForLengthDecision = TwoConcaveCut;
            }
            else
            {
                CutForLengthDecision = OneConcaveCut;
            }

            #region 可视化测试
            for (int i = 0; i < CutForLengthDecision.Count; i++)
            {
                Cut pCut = CutForLengthDecision[i];
                #region CutLine转换
                IPolyline CutLine = new PolylineClass();
                IPoint StartPoint = new PointClass(); IPoint EndPoint = new PointClass();
                StartPoint.X = pCut.CutEdge.startPoint.X; StartPoint.Y = pCut.CutEdge.startPoint.Y;
                EndPoint.X = pCut.CutEdge.endPoint.X; EndPoint.Y = pCut.CutEdge.endPoint.Y;
                CutLine.FromPoint = StartPoint; CutLine.ToPoint = EndPoint;
                #endregion

                #region 可视化显示测试
                PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
                object LineSb = Sb.LineSymbolization(2, 50, 50, 50, 0);
                pMapControl.DrawShape(CutLine, ref LineSb);
                pMapControl.Refresh();
                #endregion
            }
            #endregion

            #region 不考虑节点且不考虑直角
            if (!NodeConsider && !OrthConsider)
            {
                double MinLength = 100000000;
                foreach (Cut kCut in CutForLengthDecision)
                {
                    if (kCut.CutLength < MinLength)
                    {
                        MinLength = kCut.CutLength;
                        TargetCut = kCut; ;
                    }
                }
            }
            #endregion

            #region 考虑节点且不考虑直角
            else if (NodeConsider && !OrthConsider)
            {
                double MinLength = 100000000;
                foreach (Cut kCut in CutForLengthDecision)
                {
                    if (kCut.CutLength * (1 + kCut.RealNodeCount * a) < MinLength)
                    {
                        MinLength = kCut.CutLength * (1 + kCut.RealNodeCount * a);
                        TargetCut = kCut; ;
                    }
                }
            }
            #endregion

            #region 不考虑节点且考虑直角
            else if (!NodeConsider && OrthConsider)
            {
                double MinLength = 100000000;
                foreach (Cut kCut in CutForLengthDecision)
                {

                    if (kCut.CutLength * (1 - kCut.OrthCount * b) < MinLength)
                    {
                        MinLength = kCut.CutLength * (1 - kCut.OrthCount * b);
                        TargetCut = kCut; ;
                    }
                }
            }
            #endregion

            #region 考虑节点且考虑直角
            else if (NodeConsider && OrthConsider)
            {
                double MinLength = 100000000;
                foreach (Cut kCut in CutForLengthDecision)
                {
                    if (kCut.CutLength * (1 + kCut.RealNodeCount * a) * (1 - kCut.OrthCount * b) < MinLength)
                    {
                        MinLength = kCut.CutLength * (1 + kCut.RealNodeCount * a) * (1 - kCut.OrthCount * b);
                        TargetCut = kCut; ;
                    }
                }
            }
            #endregion
            #endregion

            return TargetCut;
        }
        /// <summary>
        /// 获得最佳Cut（不考虑直角的情况）
        /// </summary>
        /// <param name="CutList"></param>
        /// <returns></returns>NodeConsider true考虑节点；false不考虑节点
        /// StructNodeOutCut true考虑CutOut；false不考虑CutOut
        public Cut GetBestCutConsiderOrth(List<Cut> CutList,bool NodeConsider,int StructNodeOutCut)
        {
            Cut TargetCut = null;
            List<Cut> TwoConcaveCut = new List<Cut>();
            List<Cut> OneConcaveCut = new List<Cut>();

            #region 判断连接的两个点是否是凹点
            foreach (Cut kCut in CutList)
            {
                #region 获得Cut处两个节点的角度
                double StartAngle = kCut.StartAngle;
                double EndAngle = kCut.EndAngle;

                StartAngle = StartAngle * 180 / 3.1415926;
                EndAngle = EndAngle * 180 / 3.1415926;

                if (StartAngle < 0)
                {
                    StartAngle = 360 + StartAngle;
                }
                if (EndAngle < 0)
                {
                    EndAngle = 360 + EndAngle;
                }
                #endregion

                if (StartAngle > 180 & EndAngle > 180)
                {
                    TwoConcaveCut.Add(kCut);
                }
                else
                {
                    OneConcaveCut.Add(kCut);
                }
            }
            #endregion

            #region 判断Cut的长度
            List<Cut> CutForLengthDecision = new List<Cut>();
            #region 获得待判断的CutList(结构点不排除Cut)——相同的结构点
            if (StructNodeOutCut==1)
            {
                if (TwoConcaveCut.Count > 0)
                {
                    for (int i = 0; i < TwoConcaveCut.Count; i++)
                    {
                        if (!TwoConcaveCut[i].StructNodeDcrese)
                        {
                            CutForLengthDecision.Add(TwoConcaveCut[i]);
                        }
                    }

                    if (CutForLengthDecision.Count == 0)
                    {
                        CutForLengthDecision = TwoConcaveCut;
                    }
                }
                else
                {
                    for (int i = 0; i < OneConcaveCut.Count; i++)
                    {
                        if (!OneConcaveCut[i].StructNodeDcrese)
                        {
                            CutForLengthDecision.Add(OneConcaveCut[i]);
                        }
                    }

                    if (CutForLengthDecision.Count == 0)
                    {
                        CutForLengthDecision = OneConcaveCut;
                    }
                }
            }
            #endregion

            #region 获得待判断的CutList(结构点排除Cut)——相同的结构点
            else if(StructNodeOutCut==2)
            {
                if (TwoConcaveCut.Count > 0)
                {
                    for (int i = 0; i < TwoConcaveCut.Count; i++)
                    {
                        if (!TwoConcaveCut[i].StructNodeDecreseOutCut)
                        {
                            CutForLengthDecision.Add(TwoConcaveCut[i]);
                        }
                    }

                    if (CutForLengthDecision.Count == 0)
                    {
                        CutForLengthDecision = TwoConcaveCut;
                    }
                }
                else
                {
                    for (int i = 0; i < OneConcaveCut.Count; i++)
                    {
                        if (!OneConcaveCut[i].StructNodeDecreseOutCut)
                        {
                            CutForLengthDecision.Add(OneConcaveCut[i]);
                        }
                    }

                    if (CutForLengthDecision.Count == 0)
                    {
                        CutForLengthDecision = OneConcaveCut;
                    }
                }
            }
            #endregion

            #region 获得待判断的CutList——不管相同的结构点
            else if (StructNodeOutCut == 3)
            {
                if (TwoConcaveCut.Count > 0)
                {
                    for (int i = 0; i < TwoConcaveCut.Count; i++)
                    {
                        if (!TwoConcaveCut[i].sStructNodeDecese)
                        {
                            CutForLengthDecision.Add(TwoConcaveCut[i]);
                        }
                    }

                    if (CutForLengthDecision.Count == 0)
                    {
                        CutForLengthDecision = TwoConcaveCut;
                    }
                }
                else
                {
                    for (int i = 0; i < OneConcaveCut.Count; i++)
                    {
                        if (!OneConcaveCut[i].sStructNodeDecese)
                        {
                            CutForLengthDecision.Add(OneConcaveCut[i]);
                        }
                    }

                    if (CutForLengthDecision.Count == 0)
                    {
                        CutForLengthDecision = OneConcaveCut;
                    }
                }
            }
            #endregion

            #region 不考虑节点
            if (!NodeConsider)
            {
                double MinLength = 100000000;
                foreach (Cut kCut in CutForLengthDecision)
                {
                    if (kCut.CutLength < MinLength)
                    {
                        MinLength = kCut.CutLength;
                        TargetCut = kCut; ;
                    }
                }
            }
            #endregion

            #region CutLine转换
            for (int i = 0; i < CutForLengthDecision.Count; i++)
            {
                IPolyline CutLine = new PolylineClass();
                IPoint StartPoint = new PointClass(); IPoint EndPoint = new PointClass();
                StartPoint.X = CutForLengthDecision[i].CutEdge.startPoint.X; StartPoint.Y = CutForLengthDecision[i].CutEdge.startPoint.Y;
                EndPoint.X = CutForLengthDecision[i].CutEdge.endPoint.X; EndPoint.Y = CutForLengthDecision[i].CutEdge.endPoint.Y;
                CutLine.FromPoint = StartPoint; CutLine.ToPoint = EndPoint;

                #region 可视化显示测试
                PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
                object LineSb = Sb.LineSymbolization(2, 200, 200, 200, 0);
                pMapControl.DrawShape(CutLine, ref LineSb);
                pMapControl.Refresh();
            }
            #endregion

            #region 考虑节点
            if (NodeConsider)
            {
                double MinLength = 100000000;
                foreach (Cut kCut in CutForLengthDecision)
                {
                    if (kCut.NodeCount == 1)
                    {
                        if (kCut.CutLength < MinLength)
                        {
                            MinLength = kCut.CutLength;
                            TargetCut = kCut; ;
                        }
                    }
                    else
                    {
                        if (kCut.CutLength * 0.8 < MinLength)
                        {
                            MinLength = kCut.CutLength * 0.8;
                            TargetCut = kCut; ;
                        }
                    }
                }
            }
            #endregion
            #endregion
            #endregion

            return TargetCut;
        }

        /// <summary>
        /// 判断结构点是否增加
        /// </summary>
        /// <param name="BeforeNode"></param>
        /// <param name="AfterNode"></param>
        /// <returns></returns>true 结构点Node减少；false 结构点Node增加
        public bool NodeDecrese(List<TriNode> BeforeNode,List<TriNode> AfterNode)
        {
            bool NodeIncrese = false;

            for (int i = 0; i < BeforeNode.Count; i++)
            {
                bool CacheLabel=false;
                for (int j = 0; j < AfterNode.Count; j++)
                {
                    if (Math.Abs(AfterNode[j].X - BeforeNode[i].X) < 0.0000001 && Math.Abs(AfterNode[j].Y - BeforeNode[i].Y) < 0.0000001)
                    {
                        CacheLabel = true;
                        break;
                    }
                }
                if (!CacheLabel)
                {
                    NodeIncrese = true;
                    break;
                }
            }

            return NodeIncrese;
        }

        /// <summary>
        /// 判断结构点是否增加
        /// </summary>
        /// <param name="BeforeNode"></param>
        /// <param name="AfterNode"></param>
        /// <param name="pCut"></param>
        /// <returns></returns>true表示Node减少；false表示结构点增加
        public bool NodeDecreseOutCut(List<TriNode> BeforeNode,List<TriNode> AfterNode,Cut pCut)
        {
            bool NodeIncrese = false;

            TriNode pNode1 = pCut.CutEdge.startPoint;
            TriNode pNode2 = pCut.CutEdge.endPoint;

            for (int i = BeforeNode.Count - 1; i > 0; i--)
            {
                if((Math.Abs(pNode1.X-BeforeNode[i].X)<0.0000001&&Math.Abs(pNode1.Y-BeforeNode[i].Y)<0.0000001)
                    || (Math.Abs(pNode2.X - BeforeNode[i].X) < 0.0000001 && Math.Abs(pNode2.Y - BeforeNode[i].Y) < 0.0000001))
                {
                    BeforeNode.RemoveAt(i);
                }
            }

            for (int i = AfterNode.Count - 1; i >= 0; i--)
            {
                if ((Math.Abs(pNode1.X - AfterNode[i].X) < 0.0000001 && Math.Abs(pNode1.Y - AfterNode[i].Y) < 0.0000001)
                    || (Math.Abs(pNode2.X - AfterNode[i].X) < 0.0000001 && Math.Abs(pNode2.Y - AfterNode[i].Y) < 0.0000001))
                {
                    AfterNode.RemoveAt(i);
                }
            }

            NodeIncrese = this.NodeDecrese(BeforeNode, AfterNode);
            return NodeIncrese;
        }

        /// <summary>
        /// 计算给定节点所有Cut的属性，如角度、长度、连接的两节点属性等
        /// 考虑到洞的问题，所以是PoList
        /// </summary>
        /// <param name="ConcaveNode"></param>
        /// <param name="AllCut"></param>所有的Cut列表
        /// <param name="PoList"></param>建筑物（包括洞）
        /// <param name="PerAngle"></param>直角的约束角度
        ///<param name="OnLineT">表示是直角的约束角度
        public void GetCutProperty(TriNode ConcaveNode, List<Cut> AllCut, List<PolygonObject> PoList,double PerAngle,double OnLineT)
        {
            PP.pMapControl = pMapControl;

            #region 基础属性计算
            for (int i = 0; i < AllCut.Count; i++)
            {
                PolygonObject CachePo = this.CutInPolygon(AllCut[i], PoList);
                if (CachePo != null)
                {
                    this.GetCutLength(AllCut[i]);//计算Cut长度
                    this.GetCutBeforeAngle(AllCut[i], PoList);
                    this.GetCutAfterAngle(AllCut[i], PoList,OnLineT);

                    #region 获得分割后的结构点
                    //List<TriNode> StructNode = new List<TriNode>();
                    //PolygonObject Po = this.CutInPolygon(AllCut[i], PoList);
                    //List<TriNode> BeforeStructNode = PP.GetNodeStruct(Po, PerAngle, true);
                    //List<TriNode> AfterStructNode = this.GetStructNodeAfterCut(AllCut[i], PoList, PerAngle);
                    #endregion

                    #region 判断结构点是否减少
                    //for (int j = 0; j < AfterStructNode.Count; j++)
                    //{
                    //    IPoint CacheStartPoint = new PointClass();
                    //    CacheStartPoint.X = AfterStructNode[j].X; CacheStartPoint.Y = AfterStructNode[j].Y;
                    //    PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
                    //    object PointSb = Sb.PointSymbolization(100, 100, 100);
                    //    pMapControl.DrawShape(CacheStartPoint, ref PointSb);
                    //    pMapControl.Refresh();
                    //}

                    //if (AfterStructNode.Count < BeforeStructNode.Count)
                    //{
                    //    AllCut[i].sStructNodeDecese = true;
                    //}
                    //else
                    //{
                    //    AllCut[i].sStructNodeDecese = false;
                    //}

                    //AllCut[i].StructNodeDcrese = this.NodeDecrese(BeforeStructNode, AfterStructNode);
                    //AllCut[i].StructNodeDecreseOutCut = this.NodeDecreseOutCut(BeforeStructNode, AfterStructNode, AllCut[i]);
                    #endregion
                }
            }
            #endregion

            #region 删除无效的Cut
            bool ProcessLabel = true;
            while (ProcessLabel)
            {
                ProcessLabel = false;
                for (int i = 0; i < AllCut.Count; i++)
                {
                    if (AllCut[i].CutLength == 0)
                    {
                        AllCut.RemoveAt(i);
                        ProcessLabel = true;
                        break;
                    }
                }
            }
            #endregion

            #region 剖分属性计算
            for (int i = 0; i < AllCut.Count; i++)
            {
                #region 计算角度
                double StartAngle = AllCut[i].StartAngle;
                double EndAngle = AllCut[i].EndAngle;
                double CutAngle11 = AllCut[i].CutAngle11;
                double CutAngle12 = AllCut[i].CutAngle12;
                double CutAngle21 = AllCut[i].CutAngle21;
                double CutAngle22 = AllCut[i].CutAngle22;

                StartAngle = StartAngle * 180 / Math.PI;
                EndAngle = EndAngle * 180 / Math.PI;
                CutAngle11 = CutAngle11 * 180 / Math.PI;
                CutAngle12 = CutAngle12 * 180 / Math.PI;
                CutAngle21 = CutAngle21 * 180 / Math.PI;
                CutAngle22 = CutAngle22 * 180 / Math.PI;

                if (StartAngle < 0)
                {
                    StartAngle = 360 + StartAngle;
                }
                if (EndAngle < 0)
                {
                    EndAngle = 360 + EndAngle;
                }
                if (CutAngle11 < 0)
                {
                    CutAngle11 = 360 + CutAngle11;
                }
                if (CutAngle12 < 0)
                {
                    CutAngle12 = 360 + CutAngle12;
                }
                if (CutAngle21 < 0)
                {
                    CutAngle21 = 360 + CutAngle21;
                }
                if (CutAngle22 < 0)
                {
                    CutAngle22 = 360 + CutAngle22;
                }
                #endregion

                #region 计算Cut关联的节点个数
                int StartCutNodeCount = 0; int EndCutNodeCount = 0;
                if (Math.Abs(StartAngle - 180) > OnLineT && StartAngle!=0)
                {
                    AllCut[i].NodeCount++;
                    StartCutNodeCount++;
                }
                if (Math.Abs(EndAngle - 180) > OnLineT && EndAngle != 0)
                {
                    AllCut[i].NodeCount++;
                    StartCutNodeCount++;
                }
                if (Math.Abs(CutAngle11 - 180) > OnLineT && CutAngle11 != 0)
                {
                    EndCutNodeCount++;
                }
                if (Math.Abs(CutAngle12 - 180) > OnLineT && CutAngle12 != 0)
                {
                    EndCutNodeCount++;
                }
                if (Math.Abs(CutAngle21 - 180) > OnLineT && CutAngle21 != 0)
                {
                    EndCutNodeCount++;
                }
                if (Math.Abs(CutAngle22 - 180) > OnLineT && CutAngle22 != 0)
                {
                    EndCutNodeCount++;
                }
                AllCut[i].RealNodeCount = EndCutNodeCount - StartCutNodeCount;//获得增加的节点个数
                #endregion

                #region 计算凹点减少个数
                double StartNode = 0; double EndNode = 0;
                if (StartAngle > (180 + OnLineT))
                {
                    StartNode++;
                }
                if (EndAngle > (180 + OnLineT))
                {
                    StartNode++;
                }
                if (CutAngle11 > (180+OnLineT))
                {
                    EndNode++;
                }
                if (CutAngle12 > (180+OnLineT))
                {
                    EndNode++;
                }
                if (CutAngle21>(180+OnLineT))
                {
                    EndNode++;
                }
                if (CutAngle22 > (180 + OnLineT))
                {
                    EndNode++;
                }

                AllCut[i].RConP = StartNode - EndNode;
                #endregion

                #region 判断是否保持了直角
                bool StartLabel = false; bool EndLabel = false; int OrthBeforeCount = 0;
                bool Cut11Label = false; bool Cut12Label = false; int OrthAfterCount = 0;
                bool Cut21Label = false;bool Cut22Label = false;
                if (Math.Abs(StartAngle - 90) < PerAngle || Math.Abs(StartAngle - 270) < PerAngle)
                {
                    StartLabel = true;
                    OrthBeforeCount++;
                }
                if (Math.Abs(EndAngle - 90) < PerAngle || Math.Abs(EndAngle - 270) < PerAngle)
                {
                    EndLabel = true;
                    OrthBeforeCount++;
                }
                if (Math.Abs(CutAngle11 - 90) < PerAngle || Math.Abs(CutAngle11 - 270) < PerAngle)
                {
                    Cut11Label = true;
                    OrthAfterCount++;
                }
                if (Math.Abs(CutAngle12 - 90) < PerAngle || Math.Abs(CutAngle12 - 270) < PerAngle)
                {
                    Cut12Label = true;
                    OrthAfterCount++;
                }
                if (Math.Abs(CutAngle21 - 90) < PerAngle || Math.Abs(CutAngle21 - 270) < PerAngle)
                {
                    Cut21Label = true;
                    OrthAfterCount++;
                }
                if (Math.Abs(CutAngle22 - 90) < PerAngle || Math.Abs(CutAngle22 - 270) < PerAngle)
                {
                    Cut22Label = true;
                    OrthAfterCount++;
                }
                AllCut[i].OrthCount = OrthAfterCount - OrthBeforeCount;//计算直角增加的数量
                if (StartLabel && !Cut11Label && !Cut12Label)
                {
                    AllCut[i].RightAngleMaintain = false;
                }
                if (EndLabel && !Cut21Label && !Cut22Label)
                {
                    AllCut[i].RightAngleMaintain = false;
                }
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// 计算给定建筑物所有PotentialCuts之间的相交关系，凹点消除关系
        /// </summary>
        /// <param name="AllCut"></param>
        /// <param name="TargetPo"></param>
        /// <param name="PerAngle"></param>
        /// <param name="OnLineT"></param>
        public void GetCutProperty(List<Cut> AllCut, PolygonObject TargetPo, double OnLineT)
        {
            List<PolygonObject> PoList = new List<PolygonObject>();
            PoList.Add(TargetPo);
            
            for (int i = 0; i < AllCut.Count; i++)
            {
                IPolyline SourceCut = new PolylineClass();
                IPoint sStartPoint = new PointClass(); sStartPoint.X = AllCut[i].CutEdge.startPoint.X; sStartPoint.Y = AllCut[i].CutEdge.startPoint.Y;
                IPoint sEndPoint = new PointClass(); sEndPoint.X = AllCut[i].CutEdge.endPoint.X; sEndPoint.Y = AllCut[i].CutEdge.endPoint.Y;
                SourceCut.FromPoint = sStartPoint; SourceCut.ToPoint = sEndPoint;
                
                #region 判断相交关系
                for (int j = 0; j < AllCut.Count; j++)
                {
                    if (j != i)
                    {
                        IPolyline TargetCut = new PolylineClass();

                        IPoint tStartPoint = new PointClass(); tStartPoint.X = AllCut[j].CutEdge.startPoint.X; tStartPoint.Y = AllCut[j].CutEdge.startPoint.Y;
                        IPoint tEndPoint = new PointClass(); tEndPoint.X = AllCut[j].CutEdge.endPoint.X; tEndPoint.Y = AllCut[j].CutEdge.endPoint.Y;
                        TargetCut.FromPoint = tStartPoint; TargetCut.ToPoint = tEndPoint;

                        IRelationalOperator iRO = SourceCut as IRelationalOperator;
                        if (iRO.Crosses(TargetCut as IGeometry) && !iRO.Touches(TargetCut as IGeometry))
                        {
                            AllCut[i].IntersectCuts.Add(AllCut[j]);
                        }
                    }
                } 
                #endregion

                ///同时，这两个过程能获取Cut能消除的对应凹点
                this.GetCutBeforeAngle(AllCut[i], PoList);
                this.GetCutAfterAngle(AllCut[i], PoList, OnLineT);
            }
           
        }

        /// <summary>
        /// 获得分割后的结构点
        /// </summary>
        /// <returns></returns>
        public List<TriNode> GetStructNodeAfterCut(Cut pCut,List<PolygonObject> PoList,double PerAngle)
        {
            List<TriNode> StructNode = new List<TriNode>();
            PolygonObject CutInPolygon = this.CutInPolygon(pCut, PoList);//获得分割对应的建筑物
            Polygon pPolygon = this.PolygonObjectConvert(CutInPolygon) as Polygon;
            List<Polygon> CuttedPolygons = this.GetPolygonAfterCut(pPolygon, pCut);

            int testLabel = 0;
            for (int i = 0; i < CuttedPolygons.Count; i++)
            {
                PolygonObject CachePo = this.PolygonConvert(CuttedPolygons[i] as IPolygon);
                List<TriNode> NodeList = PP.GetNodeStruct(CachePo, PerAngle, true);
               
                for (int j = 0; j < NodeList.Count; j++)
                {
                    testLabel++;
                    bool Label1 = false;
                    TriNode PN1 = NodeList[j];
                    for (int m = 0; m < StructNode.Count; m++)
                    {
                        if (Math.Abs(StructNode[m].X - PN1.X) < 0.0000001 && Math.Abs(StructNode[m].Y - PN1.Y) < 0.0000001)
                        {
                            Label1 = true;
                            break;
                        }
                    }
                    if (!Label1)
                    {
                        StructNode.Add(PN1);
                    }
                }
            }

             return StructNode;
        }

        /// <summary>
        /// 计算Cut的长度
        /// </summary>
        /// <param name="tE"></param>
        /// <returns></returns>
        public void GetCutLength(Cut pCut)
        {
          double Length = Math.Sqrt((pCut.CutEdge.endPoint.Y - pCut.CutEdge.startPoint.Y) * (pCut.CutEdge.endPoint.Y - pCut.CutEdge.startPoint.Y) +
               (pCut.CutEdge.endPoint.X - pCut.CutEdge.startPoint.X) * (pCut.CutEdge.endPoint.X - pCut.CutEdge.startPoint.X));
          pCut.CutLength = Length;
        }

        /// <summary>
        /// 获得Cut对应处的角度;同时，添加Cut关联的凹点
        /// </summary>
        /// <param name="pCut"></param>
        /// <param name="PoList"></param>
        public void GetCutBeforeAngle(Cut pCut, List<PolygonObject> PoList)
        {
            PolygonObject CutInPolygon = this.CutInPolygon(pCut, PoList);
            TriNode StartNode = pCut.CutEdge.startPoint;
            TriNode EndNode = pCut.CutEdge.endPoint;
            CutInPolygon.GetBendAngle();

            for (int j = 0; j < CutInPolygon.PointList.Count; j++)
            {
                if (Math.Abs(CutInPolygon.PointList[j].X - StartNode.X) < 0.00001 && Math.Abs(CutInPolygon.PointList[j].Y - StartNode.Y) < 0.00001)
                {
                    pCut.StartAngle = CutInPolygon.BendAngle[j][1];

                    //添加Cut关联的凹点
                    if (pCut.StartAngle < 0)
                    {
                        pCut.ConcaveNodes.Add(CutInPolygon.PointList[j]);
                    }
                }

                //备注：若终点为图形
                if (Math.Abs(CutInPolygon.PointList[j].X - EndNode.X) < 0.00001 && Math.Abs(CutInPolygon.PointList[j].Y - EndNode.Y) < 0.00001)
                {
                    pCut.EndAngle = CutInPolygon.BendAngle[j][1];

                    //添加Cut关联的凹点
                    if (pCut.EndAngle < 0)
                    {
                        pCut.ConcaveNodes.Add(CutInPolygon.PointList[j]);
                    }
                }
            }
        }

        /// <summary>
        /// 计算每一个Cut后获取的4个角度
        /// </summary>
        /// <param name="pCut"></param>
        /// <param name="PoList">考虑了洞的情况，即PoList表示一个建筑物，PoList[0]为外轮廓；PoList[>0]为洞</param>
        public void GetCutAfterAngle(Cut pCut, List<PolygonObject> PoList,double OnLineAngle)
        {
            //获得Cut的两个节点
            TriNode StartNode = pCut.CutEdge.startPoint;
            TriNode EndNode = pCut.CutEdge.endPoint;
            PolygonObject CutInPolygon = this.CutInPolygon(pCut, PoList);

            #region 如果建筑物是外轮廓
            if (PoList.IndexOf(CutInPolygon) == 0)
            {
                for (int j = 0; j < CutInPolygon.PointList.Count; j++)
                {
                    #region 获得待判断节点
                    TriNode esNode = null; TriNode eeNode = null;
                    if (j == CutInPolygon.PointList.Count - 1)
                    {
                        esNode = CutInPolygon.PointList[j];
                        eeNode = CutInPolygon.PointList[0];
                    }
                    else
                    {
                        esNode = CutInPolygon.PointList[j];
                        eeNode = CutInPolygon.PointList[j + 1];
                    }
                    #endregion

                    ///判断点是否在建筑物的某条边上
                    int sOnline = this.PointOnLineBoundary(esNode, eeNode, StartNode);
                    int eOnline = this.PointOnLineBoundary(esNode, eeNode, EndNode);

                    #region 若StartNode在边上
                    if (sOnline > 0)
                    {
                        TriNode BeforeNode = null;
                        TriNode AfterNode = null;

                        if (sOnline == 1)//在起点
                        {
                            if (j == 0)
                            {
                                BeforeNode = CutInPolygon.PointList[CutInPolygon.PointList.Count - 1];
                            }
                            else
                            {
                                BeforeNode = CutInPolygon.PointList[j - 1];
                            }
                            AfterNode = eeNode;
                        }
                        else if (sOnline == 2)//在终点
                        {
                            BeforeNode = esNode;
                            if (j == CutInPolygon.PointList.Count - 1)
                            {
                                AfterNode = CutInPolygon.PointList[1];
                            }
                            else if (j == CutInPolygon.PointList.Count - 2)
                            {
                                AfterNode = CutInPolygon.PointList[0];
                            }
                            else
                            {
                                AfterNode = CutInPolygon.PointList[j + 2];
                            }
                        }
                        else if (sOnline == 3)//在边上
                        {
                            BeforeNode = esNode;
                            AfterNode = eeNode;
                        }

                        pCut.CutAngle11 = this.GetPointAngle(StartNode, BeforeNode, EndNode);
                        pCut.CutAngle12 = this.GetPointAngle(StartNode, EndNode, AfterNode);
                    }
                    #endregion

                    #region 若EndNode在边上
                    if (eOnline > 0)
                    {
                        TriNode BeforeNode = null;
                        TriNode AfterNode = null;

                        if (eOnline == 1)//在起点
                        {
                            if (j == 0)
                            {
                                BeforeNode = CutInPolygon.PointList[CutInPolygon.PointList.Count - 1];
                            }
                            else
                            {
                                BeforeNode = CutInPolygon.PointList[j - 1];
                            }
                            AfterNode = eeNode;
                        }
                        else if (eOnline == 2)//在终点
                        {
                            BeforeNode = esNode;
                            if (j == CutInPolygon.PointList.Count - 1)
                            {
                                AfterNode = CutInPolygon.PointList[1];
                            }
                            else if (j == CutInPolygon.PointList.Count - 2)
                            {
                                AfterNode = CutInPolygon.PointList[0];
                            }
                            else
                            {
                                AfterNode = CutInPolygon.PointList[j + 2];
                            }
                        }
                        else if (eOnline == 3)//在边上
                        {
                            BeforeNode = esNode;
                            AfterNode = eeNode;
                        }

                        pCut.CutAngle21 = this.GetPointAngle(EndNode, BeforeNode, StartNode);
                        pCut.CutAngle22 = this.GetPointAngle(EndNode, StartNode, AfterNode);

                        #region 移除未能移除凹点的Cut中的对应凹点
                        double CutAngle21 = pCut.CutAngle21;
                        double CutAngle22 = pCut.CutAngle22;
                        CutAngle21 = CutAngle21 * 180 / 3.1415926;
                        CutAngle22 = CutAngle22 * 180 / 3.1415926;
                        if (CutAngle21 < 0)
                        {
                            CutAngle21 = 360 + CutAngle21;
                        }
                        if (CutAngle22 < 0)
                        {
                            CutAngle22 = 360 + CutAngle22;
                        }

                        if (!(CutAngle21 <= (180 + OnLineAngle)) || !(CutAngle22 <= (180 + OnLineAngle)))
                        {
                            if (pCut.ConcaveNodes.Count == 2)
                            {
                                pCut.ConcaveNodes.RemoveAt(1);
                            }
                        }                    
                        #endregion
                    }
                    #endregion
                }
            }
            #endregion

            #region 如果建筑物是给定的洞
            if (PoList.IndexOf(CutInPolygon) > 0)
            {

                for (int j = 0; j < CutInPolygon.PointList.Count; j++)
                {
                    #region 获得待判断节点
                    TriNode esNode = null; TriNode eeNode = null;
                    if (j == CutInPolygon.PointList.Count - 1)
                    {
                        esNode = CutInPolygon.PointList[j];
                        eeNode = CutInPolygon.PointList[0];
                    }
                    else
                    {
                        esNode = CutInPolygon.PointList[j];
                        eeNode = CutInPolygon.PointList[j + 1];
                    }
                    #endregion

                    ///判断点是否在建筑物的某条边上
                    int sOnline = this.PointOnLineBoundary(esNode, eeNode, StartNode);
                    int eOnline = this.PointOnLineBoundary(esNode, eeNode, EndNode);

                    #region 若StartNode在边上
                    if (sOnline > 0)
                    {
                        TriNode BeforeNode = null;
                        TriNode AfterNode = null;

                        if (sOnline == 1)//在起点
                        {
                            if (j == 0)
                            {
                                BeforeNode = CutInPolygon.PointList[CutInPolygon.PointList.Count - 1];
                            }
                            else
                            {
                                BeforeNode = CutInPolygon.PointList[j - 1];
                            }
                            AfterNode = eeNode;
                        }
                        else if (sOnline == 2)//在终点
                        {
                            BeforeNode = esNode;
                            if (j == CutInPolygon.PointList.Count - 1)
                            {
                                AfterNode = CutInPolygon.PointList[1];
                            }
                            else if (j == CutInPolygon.PointList.Count - 2)
                            {
                                AfterNode = CutInPolygon.PointList[0];
                            }
                            else
                            {
                                AfterNode = CutInPolygon.PointList[j + 2];
                            }
                        }
                        else if (sOnline == 3)//在边上
                        {
                            BeforeNode = esNode;
                            AfterNode = eeNode;
                        }

                        #region 角度计算
                        #region 可视化测试用
                        Point CacheStartPoint = new PointClass();
                        Point CacheBeforePoint = new PointClass();
                        PointClass CacheEndPoint = new PointClass();
                        Point CacheAfterPoint = new PointClass();

                        CacheStartPoint.X = StartNode.X; CacheStartPoint.Y = StartNode.Y;
                        CacheBeforePoint.X = BeforeNode.X; CacheBeforePoint.Y = BeforeNode.Y;
                        CacheEndPoint.X = EndNode.X; CacheEndPoint.Y = EndNode.Y;
                        CacheAfterPoint.X = AfterNode.X; CacheAfterPoint.Y = AfterNode.Y;

                        PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
                        object PointSb = Sb.PointSymbolization(100, 100, 100);
                        pMapControl.DrawShape(CacheStartPoint, ref PointSb);
                        pMapControl.DrawShape(CacheBeforePoint, ref PointSb);
                        pMapControl.DrawShape(CacheEndPoint, ref PointSb);
                        pMapControl.DrawShape(CacheAfterPoint, ref PointSb);
                        #endregion

                        pCut.CutAngle11 = this.GetPointAngle(StartNode, BeforeNode, EndNode);
                        pCut.CutAngle12 = this.GetPointAngle(StartNode, EndNode, AfterNode);
                        #endregion
                    }
                    #endregion

                    #region 若EndNode在边上
                    if (eOnline > 0)
                    {
                        TriNode BeforeNode = null;
                        TriNode AfterNode = null;
                        if (eOnline == 1)//在起点
                        {
                            if (j == 0)
                            {
                                BeforeNode = CutInPolygon.PointList[CutInPolygon.PointList.Count - 1];
                            }
                            else
                            {
                                BeforeNode = CutInPolygon.PointList[j - 1];
                            }
                            AfterNode = eeNode;
                        }
                        else if (eOnline == 2)//在终点
                        {
                            BeforeNode = esNode;
                            if (j == CutInPolygon.PointList.Count - 1)
                            {
                                AfterNode = CutInPolygon.PointList[1];
                            }
                            else if (j == CutInPolygon.PointList.Count - 2)
                            {
                                AfterNode = CutInPolygon.PointList[0];
                            }
                            else
                            {
                                AfterNode = CutInPolygon.PointList[j + 2];
                            }
                        }
                        else if (eOnline == 3)//在边上
                        {
                            BeforeNode = esNode;
                            AfterNode = eeNode;
                        }

                        #region 角度计算
                        #region 可视化测试用
                        Point CacheStartPoint = new PointClass();
                        Point CacheBeforePoint = new PointClass();
                        PointClass CacheEndPoint = new PointClass();
                        Point CacheAfterPoint = new PointClass();

                        CacheStartPoint.X = StartNode.X; CacheStartPoint.Y = StartNode.Y;
                        CacheBeforePoint.X = BeforeNode.X; CacheBeforePoint.Y = BeforeNode.Y;
                        CacheEndPoint.X = EndNode.X; CacheEndPoint.Y = EndNode.Y;
                        CacheAfterPoint.X = AfterNode.X; CacheAfterPoint.Y = AfterNode.Y;

                        PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
                        object PointSb = Sb.PointSymbolization(100, 100, 100);
                        pMapControl.DrawShape(CacheStartPoint, ref PointSb);
                        pMapControl.DrawShape(CacheBeforePoint, ref PointSb);
                        pMapControl.DrawShape(CacheEndPoint, ref PointSb);
                        pMapControl.DrawShape(CacheAfterPoint, ref PointSb);
                        #endregion

                        pCut.CutAngle21 = this.GetPointAngle(EndNode, BeforeNode, StartNode);
                        pCut.CutAngle22 = this.GetPointAngle(EndNode, StartNode, AfterNode);

                        #region 移除未能移除凹点的Cut
                        if (pCut.CutAngle21 < (-5 * 3.1415926 / 180) || pCut.CutAngle22 < (-5 * 3.1415926 / 180))
                        {
                            if (pCut.ConcaveNodes.Count == 2)
                            {
                                pCut.ConcaveNodes.RemoveAt(1);
                            }
                        }
                        #endregion
                        #endregion
                    }
                    #endregion
                }
            }
            #endregion
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
            if (CosCur >= 1 || CosCur <= -1)
            {
                CosCur = Math.Ceiling(CosCur);
            }
            double Angle = Math.Acos(CosCur);

            return Angle;
        }

        /// <summary>
        /// 判断给定的TargetNode是否在直线中间
        /// </summary>
        /// <param name="StartNode"></param>
        /// <param name="EndNode"></param>
        /// <param name="TargetNode"></param>
        /// <returns></returns>Online=false不在线上；OnLine=true在线上；
        public bool PointOnLine(TriNode StartNode, TriNode EndNode, TriNode TargetNode)
        {
            bool OnLine = false;

            double k1 = (StartNode.Y - TargetNode.Y) / (StartNode.X - TargetNode.X);
            double k2 = (EndNode.Y - TargetNode.Y) / (EndNode.X - TargetNode.X);

            if (Math.Abs(k1 - k2) < 0.00001)
            {
                OnLine = true;
            }

            return OnLine;
        }

        /// <summary>
        /// 判断给定的TargetNode是否在直线端点
        /// </summary>
        /// <param name="StartNode"></param>
        /// <param name="EndNode"></param>
        /// <param name="TargetNode"></param>
        /// <returns></returns>=1,在StartNode；=2，在EndNode上;=3，在线上；=0表示不在线上
        public int PointOnLineBoundary(TriNode StartNode, TriNode EndNode, TriNode TargetNode)
        {
            int OnLabel = 0;
            if (Math.Abs(TargetNode.X-StartNode.X)<0.00001 && Math.Abs(TargetNode.Y - StartNode.Y)<0.00001)
            {
                OnLabel = 1;
            }
            else if (Math.Abs(TargetNode.X - EndNode.X)<0.00001 && Math.Abs(TargetNode.Y - EndNode.Y)<0.00001)
            {
                OnLabel = 2;
            }
            else
            {
                double k1 = (StartNode.Y - TargetNode.Y) / (StartNode.X - TargetNode.X);
                double k2 = (EndNode.Y - TargetNode.Y) / (EndNode.X - TargetNode.X);

                double MaxX = 0; double MinX = 0; double MaxY = 0; double MinY = 0;
                if (StartNode.X > EndNode.X)
                {
                    MaxX = StartNode.X; MinX = EndNode.X;
                }
                else
                {
                    MaxX = EndNode.X; MinX = StartNode.X;
                }
                if (StartNode.Y > EndNode.Y)
                {
                    MaxY = StartNode.Y; MinY = EndNode.Y;
                }
                else
                {
                    MaxY = EndNode.Y; MinY = StartNode.Y;
                }

                //确保点在线上，而不是线外共线
                if ((Math.Abs(k1 - k2) < 0.001) && (TargetNode.X < MaxX && TargetNode.X > MinX) && (TargetNode.Y < MaxY && TargetNode.Y > MinY))
                {
                    OnLabel = 3;
                }
            }
            return OnLabel;
        }

        /// <summary>
        /// 依据Cut获得Cut后的建筑物图形
        /// </summary>
        /// <param name="PoList"></param>
        /// <returns></returns>
        public List<Polygon> GetPolygonAfterCut(Polygon TargetPolygon,Cut TargetCut)
        {
            List<Polygon> PoList = new List<Polygon>();

            #region CutLine转换
            IPolyline CutLine = new PolylineClass();
            IPoint StartPoint = new PointClass(); IPoint EndPoint = new PointClass();
            StartPoint.X = TargetCut.CutEdge.startPoint.X; StartPoint.Y = TargetCut.CutEdge.startPoint.Y;
            EndPoint.X = TargetCut.CutEdge.endPoint.X; EndPoint.Y = TargetCut.CutEdge.endPoint.Y;
            CutLine.FromPoint = StartPoint; CutLine.ToPoint = EndPoint;

            #region 可视化显示测试
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
            object LineSb = Sb.LineSymbolization(2, 50, 50, 50, 0);
            pMapControl.DrawShape(CutLine, ref LineSb);
            pMapControl.Refresh();
            #endregion
            #endregion

            #region Cut过程
            ITopologicalOperator4 iTo = TargetPolygon as ITopologicalOperator4;
            IGeometryCollection CutResult = iTo.Cut2(CutLine);
            for (int i = 0; i < CutResult.GeometryCount; i++)
            {
                Polygon CutPolygon = CutResult.get_Geometry(i) as Polygon;
                PoList.Add(CutPolygon);
            }
            #endregion

            return PoList;
        }

        /// <summary>
        /// 依据Cut获得Cut后的建筑物图形
        /// </summary>
        /// <param name="PoList"></param>
        /// <returns></returns>
        public List<IPolygon> GetIPolygonAfterCut(Polygon TargetPolygon, Cut TargetCut)
        {
            List<IPolygon> PoList = new List<IPolygon>();

            #region CutLine转换
            IPolyline CutLine = new PolylineClass();
            IPoint StartPoint = new PointClass(); IPoint EndPoint = new PointClass();
            StartPoint.X = TargetCut.CutEdge.startPoint.X; StartPoint.Y = TargetCut.CutEdge.startPoint.Y;
            EndPoint.X = TargetCut.CutEdge.endPoint.X; EndPoint.Y = TargetCut.CutEdge.endPoint.Y;
            CutLine.FromPoint = StartPoint; CutLine.ToPoint = EndPoint;

            #region 可视化显示测试
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
            object LineSb = Sb.LineSymbolization(2, 50, 50, 50, 0);
            pMapControl.DrawShape(CutLine, ref LineSb);
            pMapControl.Refresh();
            #endregion
            #endregion

            #region Cut过程
            ITopologicalOperator4 iTo = TargetPolygon as ITopologicalOperator4;
            IGeometryCollection CutResult = iTo.Cut2(CutLine);
            for (int i = 0; i < CutResult.GeometryCount; i++)
            {
                Polygon CutPolygon = CutResult.get_Geometry(i) as Polygon;
                PoList.Add(CutPolygon as IPolygon);
            }
            #endregion

            return PoList;
        }

        /// <summary>
        /// 依据CutList获得Cut后的建筑物图形
        /// </summary>
        /// <param name="TargetPolygon"></param>
        /// <param name="TargetCuts"></param>
        /// <returns></returns>
        public List<IPolygon> GetIPolygonAfterCut(Polygon TargetPolygon,List<Cut> TargetCuts)
        {
            List<IPolygon> PoList = new List<IPolygon>();

            ITopologicalOperator4 iTo = TargetPolygon as ITopologicalOperator4;
            IGeometryCollection CutResult = null;
            for (int i = 0; i < TargetCuts.Count; i++)
            {
                #region CutLine转换
                IPolyline CutLine = new PolylineClass();
                IPoint StartPoint = new PointClass(); IPoint EndPoint = new PointClass();
                StartPoint.X = TargetCuts[i].CutEdge.startPoint.X; StartPoint.Y = TargetCuts[i].CutEdge.startPoint.Y;
                EndPoint.X = TargetCuts[i].CutEdge.endPoint.X; EndPoint.Y = TargetCuts[i].CutEdge.endPoint.Y;
                CutLine.FromPoint = StartPoint; CutLine.ToPoint = EndPoint;
                #endregion

                CutResult = iTo.Cut2(CutLine);
                iTo = CutResult as ITopologicalOperator4;
            }

            for (int i = 0; i < CutResult.GeometryCount; i++)
            {
                Polygon CutPolygon = CutResult.get_Geometry(i) as Polygon;
                PoList.Add(CutPolygon as IPolygon);
            }

            return PoList;
        }

        /// <summary>
        /// 依据CutList获得Cut后的建筑物图形 PoList
        /// </summary>
        /// <param name="TargetPolygon"></param>
        /// <param name="TargetCut"></param>
        /// <returns></returns>
        public List<PolygonObject> GetPoListAfterCut(Polygon TargetPolygon, List<Cut> TargetCuts)
        {
            List<IPolygon> PoList = new List<IPolygon>();
            PrDispalce.BuildingSim.PublicUtil Pu = new BuildingSim.PublicUtil();
            PoList.Add(TargetPolygon as IPolygon);
                 
            for (int i = 0; i < TargetCuts.Count; i++)
            {
                #region CutLine转换
                IPolyline CutLine = new PolylineClass();
                IPoint StartPoint = new PointClass(); IPoint EndPoint = new PointClass();
                StartPoint.X = TargetCuts[i].CutEdge.startPoint.X; StartPoint.Y = TargetCuts[i].CutEdge.startPoint.Y;
                EndPoint.X = TargetCuts[i].CutEdge.endPoint.X; EndPoint.Y = TargetCuts[i].CutEdge.endPoint.Y;
                CutLine.FromPoint = StartPoint; CutLine.ToPoint = EndPoint;
                #endregion

                #region 避免Polygon和PolygonObject转化时可能存在的疑问
                //IPoint CenterPoint=new PointClass();
                //CenterPoint.X=(StartPoint.X+EndPoint.X)/2;
                //CenterPoint.Y=(StartPoint.Y+EndPoint.Y)/2;
                //ITransform2D pTransform2D = CutLine as ITransform2D;
                //pTransform2D.Scale(CenterPoint, 0.5, 0.5);
                //CutLine = pTransform2D as IPolyline;
                #endregion

                for (int j = 0; j < PoList.Count; j++)
                {
                    IRelationalOperator iRo = PoList[j] as IRelationalOperator;
                    if (iRo.Contains(CutLine))
                    {
                        //pTransform2D.Scale(CenterPoint, 4, 4);
                        //CutLine = pTransform2D as IPolyline;

                        ITopologicalOperator4 iTo = PoList[i] as ITopologicalOperator4;  
                        IGeometryCollection CutResult=null;

                        #region 可视化显示
                        //PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
                        //object PolygonSb = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 100, 100, 100);
                        //object PolylineSb = Sb.LineSymbolization(1, 100, 100, 100, 0);

                        //pMapControl.DrawShape(PoList[i], ref PolygonSb);
                        //pMapControl.DrawShape(CutLine, ref PolylineSb);

                        //pMapControl.Refresh();
                        #endregion

                        #region 防止Cut失败
                        try
                        {
                            CutResult = iTo.Cut2(CutLine);
                            PoList.RemoveAt(j);

                            for (int g = 0; g < CutResult.GeometryCount; g++)
                            {
                                PoList.Add(CutResult.get_Geometry(g) as IPolygon);
                            }
                        }

                        catch
                        {
                        }
                        #endregion
                    }                   
                }
                
            }

            List<PolygonObject> outPoList=new List<PolygonObject>();
            for (int i = 0; i < PoList.Count; i++)
            {
                outPoList.Add(Pu.PolygonConvert(PoList[i]));
            }

            return outPoList;
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

        /// <summary>
        /// 判断Cut所在的建筑物
        /// </summary>
        /// <param name="pCut"></param>
        /// <param name="PoList"></param>
        /// <returns></returns>
        public PolygonObject CutInPolygon(Cut pCut, List<PolygonObject> PoList)
        {
            PolygonObject Po = null;
            IPoint StartPoint = new PointClass(); IPoint EndPoint = new PointClass();
            StartPoint.X=pCut.CutEdge.startPoint.X;
            StartPoint.Y = pCut.CutEdge.startPoint.Y;
            EndPoint.X = pCut.CutEdge.endPoint.X;
            EndPoint.Y = pCut.CutEdge.endPoint.Y;

            for (int i = 0; i < PoList.Count; i++)
            {
                IPolygon IPO = this.PolygonObjectConvert(PoList[i]);
                IRelationalOperator IRO = IPO as IRelationalOperator;
                bool Touch1 = IRO.Touches(StartPoint);
                bool Touch2 = IRO.Touches(EndPoint);

                if (Touch1 && Touch2)
                {
                    Po = PoList[i];
                }
            }

            return Po;
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
    }
}
