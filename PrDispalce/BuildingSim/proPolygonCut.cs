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

namespace PrDispalce.BuildingSim
{
    /// <summary>
    /// 依据实际应用需要渐进式分割建筑物图形为Convex
    /// </summary>
    class proPolygonCut
    {
        // 参数
        IPolygon TargetPolygon;//目标建筑物
        IPolygon MatchingPolygon;//匹配建筑物
        PublicUtil Pu = new PublicUtil();
        ContextSim CS = new ContextSim();
        AxMapControl pMapControl;

        #region 构造函数
        public proPolygonCut()
        {

        }

        public proPolygonCut(AxMapControl MapControl)
        {
            this.pMapControl = MapControl;
        }

        public proPolygonCut(IPolygon TargetPo, IPolygon MatchingPo)
        {
            this.TargetPolygon = TargetPo;
            this.MatchingPolygon = MatchingPo;
        }

        public proPolygonCut(IPolygon TargetPo, IPolygon MatchingPo, AxMapControl MapControl)
        {
            this.TargetPolygon = TargetPo;
            this.MatchingPolygon = MatchingPo;
            this.pMapControl = MapControl;
        }
        #endregion

        /// <summary>
        /// 返回当前两建筑物可能存在匹配关系的两个建筑物集合
        /// [0]表示TargetCuttedPolygons；[1]表示MatchingCuttedPolygons
        /// 备注：存在匹配关系的加入CuttedPolygon的MatchedPolygons集合；
        /// </summary>
        /// <param name="TargetCuttedPoly"></param>
        /// <param name="MatchingCuttedPoly"></param>
        /// <param name="InterN"></param>
        /// <param name="ConvexAngle"></param>
        /// <param name="OnLineAngle"></param>
        /// <returns></returns>
        public List<List<CuttedPolygon>> GetStepCuttedPolygons(CuttedPolygon TargetCuttedPoly, CuttedPolygon MatchingCuttedPoly, int InterN, double ConvexAngle, double OnLineAngle)
        {
            List<List<CuttedPolygon>> StepCuttedPolys = new List<List<CuttedPolygon>>();

            #region 获得所有可能的潜在匹配对
            for (int i = 0; i < TargetCuttedPoly.MatchedPolygons.Count; i++)
            {
                IPolygon TargetPo = TargetCuttedPoly.MatchedPolygons[i];
                IPolygon MatchingPo = MatchingCuttedPoly.MatchedPolygons[i];

                #region 判断需要剖分，则剖分；否则，不剖分
                if (this.CutLabel(TargetPo, MatchingPo, ConvexAngle))
                {
                    List<List<IPolygon>> CuttedPolys = this.GetCuttedPolygons(TargetPo, MatchingPo, InterN, ConvexAngle, OnLineAngle);
                    List<IPolygon> TargetCut = CuttedPolys[0];
                    List<IPolygon> MatchingCut = CuttedPolys[1];

                    #region 添加潜在的匹配对
                    #region TargetCut无剖分，MatchingCut剖分
                    if (TargetCut.Count == 1 && MatchingCut.Count > 1)
                    {
                        for (int j = 0; j < MatchingCut.Count / 2; j++)
                        {
                            #region 正序
                            CuttedPolygon CacheMatchingPo1 = new CuttedPolygon(MatchingCuttedPoly.OriginalPolygon, MatchingCuttedPoly.CuttedPolygons, MatchingCuttedPoly.MatchedPolygons);

                            CacheMatchingPo1.CuttedPolygons.Remove(MatchingPo);
                            CacheMatchingPo1.MatchedPolygons.Remove(MatchingPo);

                            CacheMatchingPo1.CuttedPolygons.Add(MatchingCut[j * 2 + 0]);
                            CacheMatchingPo1.CuttedPolygons.Add(MatchingCut[j * 2 + 1]);

                            CacheMatchingPo1.MatchedPolygons.Add(MatchingCut[j * 2 + 0]);
                            #endregion

                            #region 逆序
                            CuttedPolygon CacheMatchingPo2 = new CuttedPolygon(MatchingCuttedPoly.OriginalPolygon, MatchingCuttedPoly.CuttedPolygons, MatchingCuttedPoly.MatchedPolygons);

                            CacheMatchingPo2.CuttedPolygons.Remove(MatchingPo);
                            CacheMatchingPo2.MatchedPolygons.Remove(MatchingPo);

                            CacheMatchingPo2.CuttedPolygons.Add(MatchingCut[j * 2 + 0]);
                            CacheMatchingPo2.CuttedPolygons.Add(MatchingCut[j * 2 + 1]);

                            CacheMatchingPo2.MatchedPolygons.Add(MatchingCut[j * 2 + 1]);
                            #endregion

                            #region 添加
                            List<CuttedPolygon> CachePolyList1 = new List<CuttedPolygon>();
                            List<CuttedPolygon> CachePolyList2 = new List<CuttedPolygon>();

                            CuttedPolygon CacheTargetPolygon = new CuttedPolygon(TargetCuttedPoly.OriginalPolygon, TargetCuttedPoly.CuttedPolygons, TargetCuttedPoly.MatchedPolygons);
                            CachePolyList1.Add(CacheTargetPolygon); CachePolyList1.Add(CacheMatchingPo1);
                            CachePolyList2.Add(CacheTargetPolygon); CachePolyList2.Add(CacheMatchingPo2);
                            StepCuttedPolys.Add(CachePolyList1); StepCuttedPolys.Add(CachePolyList2);
                            #endregion
                        }
                    }
                    #endregion

                    #region MatchingCut剖分，TargteCut无剖分
                    else if (MatchingCut.Count == 1 && TargetCut.Count > 1)
                    {
                        for (int j = 0; j < TargetCut.Count / 2; j++)
                        {
                            #region 正序
                            CuttedPolygon CacheTargetPo1 = new CuttedPolygon(TargetCuttedPoly.OriginalPolygon, TargetCuttedPoly.CuttedPolygons, TargetCuttedPoly.MatchedPolygons);

                            CacheTargetPo1.CuttedPolygons.Remove(TargetPo);
                            CacheTargetPo1.MatchedPolygons.Remove(TargetPo);

                            CacheTargetPo1.CuttedPolygons.Add(TargetCut[j * 2 + 0]);
                            CacheTargetPo1.CuttedPolygons.Add(TargetCut[j * 2 + 1]);

                            CacheTargetPo1.MatchedPolygons.Add(TargetCut[j * 2 + 0]);
                            #endregion

                            #region 逆序
                            CuttedPolygon CacheTargetPo2 = new CuttedPolygon(TargetCuttedPoly.OriginalPolygon, TargetCuttedPoly.CuttedPolygons, TargetCuttedPoly.MatchedPolygons);

                            CacheTargetPo2.CuttedPolygons.Remove(TargetPo);
                            CacheTargetPo2.MatchedPolygons.Remove(TargetPo);

                            CacheTargetPo2.CuttedPolygons.Add(TargetCut[j * 2 + 0]);
                            CacheTargetPo2.CuttedPolygons.Add(TargetCut[j * 2 + 1]);

                            CacheTargetPo2.MatchedPolygons.Add(TargetCut[j * 2 + 1]);
                            #endregion

                            #region 添加
                            List<CuttedPolygon> CachePolyList1 = new List<CuttedPolygon>();
                            List<CuttedPolygon> CachePolyList2 = new List<CuttedPolygon>();

                            CuttedPolygon CacheMatchingPolygon = new CuttedPolygon(MatchingCuttedPoly.OriginalPolygon, MatchingCuttedPoly.CuttedPolygons, MatchingCuttedPoly.MatchedPolygons);
                            CachePolyList1.Add(CacheTargetPo1); CachePolyList1.Add(CacheMatchingPolygon);
                            CachePolyList2.Add(CacheTargetPo2); CachePolyList2.Add(CacheMatchingPolygon);
                            StepCuttedPolys.Add(CachePolyList1); StepCuttedPolys.Add(CachePolyList2);
                            #endregion
                        }
                    }
                    #endregion

                    #region TargetCut和MatchingCut均剖分
                    else
                    {
                        for (int j = 0; j < MatchingCut.Count / 2; j++)
                        {
                            #region 正序
                            CuttedPolygon CacheMatchingPo1 = new CuttedPolygon(MatchingCuttedPoly.OriginalPolygon, MatchingCuttedPoly.CuttedPolygons, MatchingCuttedPoly.MatchedPolygons);

                            CacheMatchingPo1.CuttedPolygons.Remove(MatchingPo);
                            CacheMatchingPo1.MatchedPolygons.Remove(MatchingPo);

                            CacheMatchingPo1.CuttedPolygons.Add(MatchingCut[j * 2 + 0]);
                            CacheMatchingPo1.CuttedPolygons.Add(MatchingCut[j * 2 + 1]);

                            CacheMatchingPo1.MatchedPolygons.Add(MatchingCut[j * 2 + 0]);
                            CacheMatchingPo1.MatchedPolygons.Add(MatchingCut[j * 2 + 1]);
                            #endregion

                            #region 逆序
                            CuttedPolygon CacheMatchingPo2 = new CuttedPolygon(MatchingCuttedPoly.OriginalPolygon, MatchingCuttedPoly.CuttedPolygons, MatchingCuttedPoly.MatchedPolygons);

                            CacheMatchingPo2.CuttedPolygons.Remove(MatchingPo);
                            CacheMatchingPo2.MatchedPolygons.Remove(MatchingPo);

                            CacheMatchingPo2.CuttedPolygons.Add(MatchingCut[j * 2 + 0]);
                            CacheMatchingPo2.CuttedPolygons.Add(MatchingCut[j * 2 + 1]);

                            CacheMatchingPo2.MatchedPolygons.Add(MatchingCut[j * 2 + 1]);
                            CacheMatchingPo2.MatchedPolygons.Add(MatchingCut[j * 2 + 0]);
                            #endregion

                            #region Matching建筑物
                            for (int m = 0; m < TargetCut.Count / 2; m++)
                            {
                                #region 正序
                                CuttedPolygon CacheTargetPo1 = new CuttedPolygon(TargetCuttedPoly.OriginalPolygon, TargetCuttedPoly.CuttedPolygons, TargetCuttedPoly.MatchedPolygons);

                                CacheTargetPo1.CuttedPolygons.Remove(TargetPo);
                                CacheTargetPo1.MatchedPolygons.Remove(TargetPo);

                                CacheTargetPo1.CuttedPolygons.Add(TargetCut[m * 2 + 0]);
                                CacheTargetPo1.CuttedPolygons.Add(TargetCut[m * 2 + 1]);

                                CacheTargetPo1.MatchedPolygons.Add(TargetCut[m * 2 + 0]);
                                CacheTargetPo1.MatchedPolygons.Add(TargetCut[m * 2 + 1]);
                                #endregion

                                #region 添加
                                List<CuttedPolygon> CachePolyList1 = new List<CuttedPolygon>();
                                List<CuttedPolygon> CachePolyList2 = new List<CuttedPolygon>();

                                CachePolyList1.Add(CacheTargetPo1); CachePolyList1.Add(CacheMatchingPo1);
                                CachePolyList2.Add(CacheTargetPo1); CachePolyList2.Add(CacheMatchingPo2);
                                StepCuttedPolys.Add(CachePolyList1); StepCuttedPolys.Add(CachePolyList2);
                                #endregion
                            }
                            #endregion

                            int testlocation = 0;
                        }
                    }
                    #endregion
                    #endregion
                }
                #endregion
            }
            #endregion

            return StepCuttedPolys;
        }

        /// <summary>
        /// 获得给定建筑物基于三角网的所有潜在分割
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <param name="InterN">三角网内插的倍率</param>
        /// <param name="ConvexAngle">Convex的阈值</param>
        /// <param name="OnLineAngle">OnLine的阈值</param>
        /// <returns></returns>
        public List<Cut> GetConcandidateCuts(IPolygon pPolygon, int InterN, double ConvexAngle, double OnLineAngle)
        {
            List<Cut> CandidateCuts = new List<Cut>();

            #region 处理类
            PrDispalce.PatternRecognition.BendProcess BP = new PatternRecognition.BendProcess();//弯曲处理类
            PrDispalce.PatternRecognition.ConcaveNodeSolve CNS = new PatternRecognition.ConcaveNodeSolve();//凹点处理类
            #endregion

            List<TriNode> CacheConcaveNodes = BP.GetConcaveNode(pPolygon as Polygon, ConvexAngle);//返回建筑物中的凹点

            if (CacheConcaveNodes.Count > 0)
            {
                #region 建筑物读取
                SMap map = new SMap();
                map.ReadDataFrmGivenPolygonObject(pPolygon as Polygon);
                map.InterpretatePoint2(InterN);

                SMap map2 = new SMap();
                map2.ReadDataFrmGivenPolygonObject(pPolygon as Polygon);
                #endregion

                #region 生成约束性三角网
                DelaunayTin dt = new DelaunayTin(map.TriNodeList);
                dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

                ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
                cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);

                Triangle.WriteID(dt.TriangleList);
                TriEdge.WriteID(dt.TriEdgeList);
                #endregion

                #region 获得潜在的Cut
                for (int i = 0; i < CacheConcaveNodes.Count; i++)
                {
                    TriNode TargetNode = CacheConcaveNodes[i];
                    List<Cut> AllCut = CNS.GetCuts(TargetNode, cdt, map.PolygonList);
                    CNS.GetCutProperty(TargetNode, AllCut, map2.PolygonList, ConvexAngle, OnLineAngle);//注意，这里需要用未加密的建筑物图形
                    List<Cut> RefinedCut = CNS.CutsRefine(AllCut, OnLineAngle);

                    for (int j = 0; j < RefinedCut.Count; j++)
                    {
                        if (!CandidateCuts.Contains(RefinedCut[j]))
                        {
                            CandidateCuts.Add(RefinedCut[j]);
                        }
                    }
                }
                #endregion
            }

            return CandidateCuts;
        }

        /// <summary>
        /// 给定两个建筑物，依据可能的Cut获取Cut后的不同建筑物
        /// </summary>
        /// <param name="TargetPo"></param>
        /// <param name="MatchingPo"></param>
        /// <param name="InterN"></param>
        /// <param name="ConvexAngle"></param>
        /// <param name="OnLineAngle"></param>
        /// <returns></returns>resPolys=依据不同的Cut获取相应的匹配关系resPolys[0]表示TargetPolys依据Cut获取的建筑物[0][1]表示第一个Cut获取的建筑物；[2][3]表示第二个Cut获取的建筑物（偶数）
        /// resPolys[1]表示MatchingPolys依据Cut获取的建筑物[0][1]表示第一个Cut获取的建筑物；[2][3]表示第二个Cut获取的建筑物（偶数）
        /// 备注：若TargetPo或MatchingPo没有Cut，则[0]表示TargetPo或MatchingPo（奇数）
        public List<List<IPolygon>> GetCuttedPolygons(IPolygon TargetPo, IPolygon MatchingPo, int InterN, double ConvexAngle, double OnLineAngle)
        {
            List<List<IPolygon>> resPolys = new List<List<IPolygon>>();

            #region 获取潜在Cuts
            List<Cut> TargetCuts=new List<Cut>();
            List<Cut> MatchingCuts=new List<Cut>();

            if (this.CutLabel(TargetPo, ConvexAngle))
            {
                TargetCuts = this.GetConcandidateCuts(TargetPo, InterN, ConvexAngle, OnLineAngle);
            }

            if (this.CutLabel(MatchingPo, ConvexAngle))
            {
                MatchingCuts = this.GetConcandidateCuts(MatchingPo, InterN, ConvexAngle, OnLineAngle);
            }
            #endregion

            #region 获取Cut后的建筑物
            ConcaveNodeSolve CNS = new ConcaveNodeSolve(pMapControl);
            List<IPolygon> TargetCutPolys = new List<IPolygon>();
            List<IPolygon> MatchingCutPolys = new List<IPolygon>();

            if (TargetCuts.Count == 0)
            {
                TargetCutPolys.Add(TargetPo);
            }
            for (int i = 0; i < TargetCuts.Count; i++)
            {
                List<IPolygon> tCuttedPolygons = CNS.GetIPolygonAfterCut(TargetPo as Polygon, TargetCuts[i]);
                TargetCutPolys.Add(tCuttedPolygons[0]);
                TargetCutPolys.Add(tCuttedPolygons[1]);
            }

            if (MatchingCuts.Count == 0)
            {
                MatchingCutPolys.Add(MatchingPo);
            }
            for (int i = 0; i < MatchingCuts.Count; i++)
            {
                List<IPolygon> mCuttedPolygons = CNS.GetIPolygonAfterCut(MatchingPo as Polygon, MatchingCuts[i]);
                MatchingCutPolys.Add(mCuttedPolygons[0]);
                MatchingCutPolys.Add(mCuttedPolygons[1]);
            }
            #endregion

            resPolys.Add(TargetCutPolys);
            resPolys.Add(MatchingCutPolys);
            return resPolys;
        }

        /// <summary>
        /// 判断给定的两个建筑物是否需要剖分
        /// </summary>
        /// <param name="TargetPo"></param>
        /// <param name="MatchingPo"></param>
        /// <returns></returns> =false表示不需要继续剖分；=true表示需要继续剖分
        public bool CutLabel(IPolygon TargetPo, IPolygon MatchingPo, double ConvexAngle)
        {
            PrDispalce.PatternRecognition.BendProcess BP = new PatternRecognition.BendProcess();//弯曲处理类
            List<TriNode> tCacheConcaveNodes = BP.GetConcaveNode(TargetPo as Polygon, ConvexAngle);//返回建筑物中的凹点
            List<TriNode> mCacheConcaveNodes = BP.GetConcaveNode(MatchingPo as Polygon, ConvexAngle);//返回建筑物中的凹点

            if (tCacheConcaveNodes.Count == 0 && mCacheConcaveNodes.Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 判断给定的建筑物是否需要剖分
        /// </summary>
        /// <param name="TargetPo"></param>
        /// <param name="MatchingPo"></param>
        /// <returns></returns> =false表示不需要继续剖分；=true表示需要继续剖分
        public bool CutLabel(IPolygon TargetPo, double ConvexAngle)
        {
            PrDispalce.PatternRecognition.BendProcess BP = new PatternRecognition.BendProcess();//弯曲处理类
            List<TriNode> tCacheConcaveNodes = BP.GetConcaveNode(TargetPo as Polygon, ConvexAngle);//返回建筑物中的凹点

            if (tCacheConcaveNodes.Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 判断当前过程是否需要继续剖分
        /// </summary>
        /// <param name="TargetPo"></param>
        /// <param name="MatchingPo"></param>
        /// <param name="ConvexAngle"></param>
        /// <returns></returns>
        public bool CutOver(List<IPolygon> TargetPoList, List<IPolygon> MatchingPoList, double ConvexAngle)
        {
            bool CutOverLabel = false;
            PrDispalce.PatternRecognition.BendProcess BP = new PatternRecognition.BendProcess();//弯曲处理类
            
            for (int i = 0; i < TargetPoList.Count; i++)
            {
                List<TriNode> tCacheConcaveNodes = BP.GetConcaveNode(TargetPoList[i] as Polygon, ConvexAngle);//返回建筑物中的凹点
                if (tCacheConcaveNodes.Count > 0)
                {
                    CutOverLabel = true;
                }
            }

            for (int i = 0; i < MatchingPoList.Count; i++)
            {
                List<TriNode> mCacheConcaveNodes = BP.GetConcaveNode(MatchingPoList[i] as Polygon, ConvexAngle);//返回建筑物中的凹点
                if (mCacheConcaveNodes.Count > 0)
                {
                    CutOverLabel = true;
                }
            }

            return CutOverLabel;
        }
    }
}
