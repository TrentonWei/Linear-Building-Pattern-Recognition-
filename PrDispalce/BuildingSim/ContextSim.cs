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

using AuxStructureLib;
using AuxStructureLib.IO;

///similarity computation for buildings
namespace PrDispalce.BuildingSim
{
    class ContextSim
    {
        #region 属性
        AxMapControl pMapControl = null;
        ComFunLib CFL = new ComFunLib();
        PrDispalce.PatternRecognition.PolygonPreprocess PP = new PatternRecognition.PolygonPreprocess();
        PrDispalce.工具类.ParameterCompute PC = new 工具类.ParameterCompute();
        PublicUtil Pu = new PublicUtil();
        double PI = 3.1415926;
        BuildingPairSim BS = new BuildingPairSim();
        #endregion

        #region 构造函数
        public ContextSim()
        {

        }

        public ContextSim(AxMapControl CacheControl)
        {
            this.pMapControl = CacheControl;
        }
        #endregion

        /// <summary>
        /// 计算两个建筑物空间方向矩阵关系的相似性
        /// 相似性=空间方向矩阵的重叠度
        /// 越大表示越相似
        /// </summary>
        /// <param name="TargetPoList"></param>只有两个建筑物
        /// <param name="MatchingPoList"></param>只有两个建筑物
        /// Type表示相似度计算的形式：0表示计算相互相似性的平均值；1表示计算相互相似性中的较大值；2表示计算相互相似性的较小值
        /// 匹配关系是确定的：[0,0;1,1]，按照List顺序确定
        /// <returns></returns>
        public double rOriRelationSim(List<IPolygon> TargetPoList, List<IPolygon> MatchingPoList, int Type)
        {
            double OriRelationSim = 0;

            #region 获得用于匹配的建筑物
            PolygonObject TargetPo1 = Pu.PolygonConvert(TargetPoList[0]);
            PolygonObject TargetPo2 = Pu.PolygonConvert(TargetPoList[1]);
            PolygonObject MatchPo1 = Pu.PolygonConvert(MatchingPoList[0]);
            PolygonObject MatchPo2 = Pu.PolygonConvert(MatchingPoList[1]);
            #endregion

            #region 计算方向矩阵
            double[,] TarMatriX1 = BS.OritationComputation(TargetPo1, TargetPo2);
            double[,] TarMatriX2 = BS.OritationComputation(TargetPo2, TargetPo1);
            double[,] MatchMatriX1 = BS.OritationComputation(MatchPo1, MatchPo2);
            double[,] MatchMatriX2 = BS.OritationComputation(MatchPo2, MatchPo1);
            #endregion

            #region 获取配对关系方向相似性
            double MatchSim1 = 0;//正向空间方向相似性
            double MatchSim2 = 0;//逆向空间方向相似性

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    MatchSim1 = MatchSim1 + Math.Min(TarMatriX1[i, j], MatchMatriX1[i, j]);
                    MatchSim2 = MatchSim2 + Math.Min(TarMatriX2[i, j], MatchMatriX2[i, j]);
                }
            }
            #endregion

            if (Type == 0)
            {
                OriRelationSim = (MatchSim1 + MatchSim2) / 2;
            }
            else if (Type == 1)
            {
                OriRelationSim = Math.Max(MatchSim1, MatchSim2);
            }
            else if (Type == 2)
            {
                OriRelationSim = Math.Min(MatchSim1, MatchSim2);
            }

            return OriRelationSim;
        }

        /// <summary>
        /// 计算两个建筑物大小关系的相似性(两个建筑物的大小相似性在[0,1]，参考张翔 2014)
        /// </summary>越大表示越相似
        /// <param name="TargetPoList"></param>//只有两个建筑物
        /// <param name="MatchingPoList"></param>//只有两个建筑物
        /// <returns></returns>
        public double SizeRelationSim(List<IPolygon> TargetPoList, List<IPolygon> MatchingPoList)
        {
            double SizeRe=0;

            double SizeRe1 = BS.SizeSimilarity(Pu.PolygonConvert(TargetPoList[0]), Pu.PolygonConvert(TargetPoList[1]), 1);//面积第一个比上第二个
            double SizeRe2 = BS.SizeSimilarity(Pu.PolygonConvert(MatchingPoList[0]), Pu.PolygonConvert(MatchingPoList[1]), 1);//面积第一个比上第二个

            if (SizeRe1 < 1)
            {
                SizeRe = Math.Abs(SizeRe1 - SizeRe2);
            }
            else
            {
                SizeRe1 = 1 / SizeRe1;
                SizeRe2 = 1 / SizeRe2;

                SizeRe = Math.Abs(SizeRe1 - SizeRe2);
            }

            SizeRe = 1 / (1 + SizeRe * SizeRe);
            return SizeRe;
        }

        /// <summary>
        /// 计算两个建筑物空间方向相对关系的相似性(参考张翔 2014)
        /// </summary>越大表示越相似
        /// <param name="TargetPoList"></param>//只有两个建筑物
        /// <param name="MatchingPoList"></param>//只有两个建筑物
        /// <returns></returns>
        public double OriRelationSim(List<IPolygon> TargetPoList, List<IPolygon> MatchingPoList)
        {
            double rOriRe = 0;

            double OriRe1 = BS.OrientationSimilarity(Pu.PolygonConvert(TargetPoList[0]), Pu.PolygonConvert(TargetPoList[1]), 0);
            double OriRe2 = BS.OrientationSimilarity(Pu.PolygonConvert(MatchingPoList[0]), Pu.PolygonConvert(MatchingPoList[1]), 0);
            rOriRe = Math.Abs(OriRe1 - OriRe2);
            if (rOriRe > 90)
            {
                rOriRe = 180 - rOriRe;
            }

            rOriRe = 1 - rOriRe / 90;
            return rOriRe;
        }

        /// <summary>
        /// 利用ShapeIndex度量建筑物匹配对的形状相似性（参考张翔 2014）
        /// </summary>越大越相似
        /// <param name="TargetPoList"></param>
        /// <param name="MatchingPoList"></param>
        /// <returns></returns>
        /// Type=0 表示利用ShapeIndex的相似度；Type=1表示利用转角函数的相似度；Type=3表示利用重叠比的相似度
        public double ShapeRelationSim(List<IPolygon> TargetPoList, List<IPolygon> MatchingPoList, int Type)
        {
            double rShapeRe = 0;

            #region shapeIndex相似性
            if (Type == 0)
            {
                PolygonObject tPo1=Pu.PolygonConvert(TargetPoList[0]);
                PolygonObject tPo2=Pu.PolygonConvert(TargetPoList[1]);

                double ShapeRe1 = BS.ShapeSimilarity(tPo1, tPo2, 0);//ShapeIndex第一个除以第二个

                PolygonObject mPo1 = Pu.PolygonConvert(MatchingPoList[0]);
                PolygonObject mPo2 = Pu.PolygonConvert(MatchingPoList[1]);
                double ShapeRe2 = BS.ShapeSimilarity(mPo1, mPo2,0);//ShapeIndex第一个除以第二个

                if (ShapeRe1 < 1)
                {
                    rShapeRe = Math.Abs(ShapeRe1 - ShapeRe2);
                }
                else
                {
                    ShapeRe1 = 1 / ShapeRe1;
                    ShapeRe2 = 1 / ShapeRe2;
                    rShapeRe = Math.Abs(ShapeRe1 - ShapeRe2);
                }

                rShapeRe = 1 / (1 + rShapeRe * rShapeRe);
            }
            #endregion

            #region 转角函数相似性
            if (Type == 1)
            {
                double ShapeRe1 = BS.TurningAngeSim(Pu.PolygonConvert(TargetPoList[0]), Pu.PolygonConvert(TargetPoList[1]));
                double ShapeRe2 = BS.TurningAngeSim(Pu.PolygonConvert(MatchingPoList[0]), Pu.PolygonConvert(MatchingPoList[1]));

                rShapeRe = 1 / (1 + Math.Abs(ShapeRe1 - ShapeRe2) * Math.Abs(ShapeRe1 - ShapeRe2));
            }
            #endregion

            #region 基于重叠面积比的相似性
            if (Type == 2)
            {
                double ShapeRe1 = BS.MDComputation(TargetPoList[0], TargetPoList[1]);
                double ShapeRe2 = BS.MDComputation(MatchingPoList[0], MatchingPoList[1]);

                rShapeRe = 1 / (1 + Math.Abs(ShapeRe1 - ShapeRe2) * Math.Abs(ShapeRe1 - ShapeRe2));
            }
            #endregion

            return rShapeRe;
        }

        /// <summary>
        /// 获得给定两个匹配关系的所有匹配对的实体匹配的综合相似度（复合相似度是按照一定权重加权）
        /// </summary>
        /// <param name="TargetPoList">目标建筑物集合</param>
        /// <param name="MatchingPoList">匹配建筑物集合</param>
        /// <param name="Type">类型=0表示计算平均值；=1表示获取最大值；=2表示获取最小值;=3按照面积加权(综合考虑匹配对的面积和)返回</param>
        /// 约束条件：ShapeWeight+SizeWeight=1
        /// <returns></returns>
        public List<double> GetMatchingSim(List<IPolygon> TargetPoList, List<IPolygon> MatchingPoList,double ShapeWeight,double SizeWeight)
        {
            List<double> MatchSimList = new List<double>();

            #region 计算相似度 
            for (int i = 0; i < TargetPoList.Count; i++)
            {
                double ShapePairSim = BS.ShapeIndexSim(Pu.PolygonConvert(TargetPoList[i]), Pu.PolygonConvert(MatchingPoList[i]),1);
                double SizePairSim = BS.SizeSimilarity(Pu.PolygonConvert(TargetPoList[i]), Pu.PolygonConvert(MatchingPoList[i]), 0);

                double MixMatchSim = ShapeWeight * ShapePairSim + SizeWeight * SizePairSim;
                MatchSimList.Add(MixMatchSim);
            }
            #endregion

            return MatchSimList;
        }

        /// <summary>
        /// 获得给定两个匹配关系的所有匹配对的实体匹配的综合相似度
        /// 复合相似度是两个相似度相乘
        /// </summary>
        /// <param name="TargetPoList"></param>
        /// <param name="MatchingPoList"></param>
        /// <returns></returns>
        public List<double> GetMatchingSim(List<IPolygon> TargetPoList, List<IPolygon> MatchingPoList)
        {
            List<double> MatchSimList = new List<double>();

            #region 计算相似度
            for (int i = 0; i < TargetPoList.Count; i++)
            {
                //IArea tArea = TargetPoList[i] as IArea;
                //double TesttArea = tArea.Area;
                //IArea mArea = MatchingPoList[i] as IArea;
                //double TestmArea = mArea.Area;

                double ShapePairSim = BS.ShapeIndexSim(Pu.PolygonConvert(TargetPoList[i]), Pu.PolygonConvert(MatchingPoList[i]), 1);
                double SizePairSim = BS.SizeSimilarity(Pu.PolygonConvert(TargetPoList[i]), Pu.PolygonConvert(MatchingPoList[i]), 0);

                double MixMatchSim = Math.Sqrt(ShapePairSim * SizePairSim);
                MatchSimList.Add(MixMatchSim);
            }
            #endregion

            return MatchSimList;
        }

        /// <summary>
        /// 计算给定的两对匹配对关系的空间关系的相似性（匹配关系按列表顺序匹配）
        /// 复合相似度为各相似度按权重加权
        /// </summary>
        /// <param name="TargetPoList"></param>
        /// <param name="MatchingPoList"></param>
        /// <param name="Type"></param>
        /// <param name="ShapeWeight"></param>
        /// <param name="SizeWeight"></param>
        /// <returns></returns>
        public double GetMatchingRelationSim(List<IPolygon> TargetPoList, List<IPolygon> MatchingPoList, double ShapeWeight, double SizeWeight,double OriWeight,double OriReWeigth)
        {
            double MixMatchSim = 0;

            #region 计算相似度
            double ShapeReSim = this.ShapeRelationSim(TargetPoList, MatchingPoList, 0);
            double rOriReSim = this.rOriRelationSim(TargetPoList, MatchingPoList, 0);
            double SizeReSim = this.SizeRelationSim(TargetPoList, MatchingPoList);
            double OriReSim = this.OriRelationSim(TargetPoList, MatchingPoList);

            MixMatchSim = ShapeWeight * ShapeReSim + SizeWeight * SizeReSim + OriReWeigth * OriReSim + OriReWeigth * rOriReSim;           
            #endregion

            return MixMatchSim;
        }

        /// <summary>
        /// 计算给定的两对匹配对关系的空间关系的相似性
        /// </summary>复合相似度为各权重相乘
        /// <param name="TargetPoList"></param>
        /// <param name="MatchingPoList"></param>
        /// <returns></returns>
        public double GetMatchingRelationSim(List<IPolygon> TargetPoList, List<IPolygon> MatchingPoList)
        {
            double MixMatchSim = 0;

            #region 计算相似度                    
            double ShapeReSim = this.ShapeRelationSim(TargetPoList, MatchingPoList, 0);
            double rOriReSim = this.rOriRelationSim(TargetPoList, MatchingPoList, 0);
            double SizeReSim = this.SizeRelationSim(TargetPoList, MatchingPoList);
            double OriReSim = this.OriRelationSim(TargetPoList, MatchingPoList);

            MixMatchSim = Math.Pow(ShapeReSim * SizeReSim * OriReSim * rOriReSim,0.25);
            #endregion

            return MixMatchSim;
        }

        /// <summary>
        /// 获得给定建筑物的邻近关系，并返回匹配对
        /// </summary>
        /// <param name="CP"></param>
        /// <returns></returns>
        public List<List<IPolygon>> GetAdjacentPair(List<IPolygon> PoList)
        {
            List<List<IPolygon>> MatchedPairs = new List<List<IPolygon>>();

            #region 邻近关系构建
            SMap map = new SMap();
            map.ReadDataFrmGivenPolygonList(PoList);
            map.InterpretatePoint(2);

            DelaunayTin dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(map.PolylineList, map.PolygonList);
            //cdt.RemoveTriangleInPolygon(map.PolygonList);

            Triangle.WriteID(dt.TriangleList);
            TriEdge.WriteID(dt.TriEdgeList);

            AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
            ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
            ske.TranverseSkeleton_Segment_PLP_NONull();

            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGraphfrmSkeletonBuildings(map, ske);
            pg.DeleteRepeatedEdge(pg.EdgeList);
            #endregion

            #region 返回邻近的匹配对
            for (int i = 0; i < pg.EdgeList.Count; i++)
            {
                List<IPolygon> PolygonPair = new List<IPolygon>();
                IPolygon CachePo1 = PoList[pg.EdgeList[i].Node1.TagID];
                IPolygon CachePo2 = PoList[pg.EdgeList[i].Node2.TagID];
                PolygonPair.Add(CachePo1); PolygonPair.Add(CachePo2);
                MatchedPairs.Add(PolygonPair);
            }
            #endregion

            return MatchedPairs;
        }

        /// <summary>
        /// 计算建筑物List的总面积
        /// </summary>
        /// <param name="PoList"></param>
        /// <returns></returns>
        public double AreaSum(List<List<IPolygon>> PoList)
        {
            double AreaSum=0;
            for (int i = 0; i < PoList.Count; i++)
            {
                AreaSum = AreaSum + this.AreaSum(PoList[i]);
            }

            return AreaSum;
        }

        /// <summary>
        /// 计算建筑物List的总面积
        /// </summary>
        /// <param name="PoList"></param>
        /// <returns></returns>
        public double AreaSum(List<IPolygon> PoList)
        {
            double AreaSum = 0;

            for (int i = 0; i < PoList.Count; i++)
            {
                IArea pArea = PoList[i] as IArea;
                AreaSum = AreaSum + pArea.Area;
            }

            return AreaSum;
        }

        /// <summary>
        /// 计算两个给定的Cuttpolygon的相似程度
        /// </summary>
        /// <param name="TargetPo">目标CutPolygon</param>
        /// <param name="MatchingPo">匹配CutPolygon</param>
        /// <param name="ShapeWeight">实体匹配形状权重</param>
        /// <param name="SizeWeight">实体匹配面积权重</param>
        /// <param name="ReShapeWeigth">关系匹配形状权重</param>
        /// <param name="ReSizeWeigth">关系匹配面积权重</param>
        /// <param name="ReOriWeigth">关系匹配方向权重</param>
        /// <param name="RegOriWeigth"><关系匹配空间方向矩阵关系权重/param>
        /// <param name="Type">=0基于面积加权，计算相似度</param>
        /// <param name="RelationWeight">考虑关系的权重</param>
        /// <returns></returns>
        public double CuttedPolygonSim(CuttedPolygon TargetPo, CuttedPolygon MatchingPo, double ShapeWeight, double SizeWeight,
            double ReShapeWeigth,double ReSizeWeigth,double ReOriWeigth,double RegOriWeigth,int Type,double RelationWeight)
        {
            double outgSim = 0;

            #region 计算实体匹配对相似度
            double pPolyMatchSim = 0;
            double tCutPolysAreaSum = this.AreaSum(TargetPo.CuttedPolygons);
            double mCutPolyAreaSum = this.AreaSum(MatchingPo.CuttedPolygons);
            double PolysAreaSum = tCutPolysAreaSum + mCutPolyAreaSum;
            List<double> PolyMatchSim = this.GetMatchingSim(TargetPo.MatchedPolygons, MatchingPo.MatchedPolygons,ShapeWeight,SizeWeight);
            #region 按照面积进行加权
            if (Type == 0)
            {
                for (int i = 0; i < PolyMatchSim.Count; i++)
                {
                    List<IPolygon> CacheAreaComList = new List<IPolygon>();
                    CacheAreaComList.Add(TargetPo.MatchedPolygons[i]);
                    CacheAreaComList.Add(MatchingPo.MatchedPolygons[i]);
                    double CacheArea = this.AreaSum(CacheAreaComList);

                    pPolyMatchSim = pPolyMatchSim + PolyMatchSim[i] * CacheArea / (PolysAreaSum);
                }
            }
            #endregion
            #endregion

            #region 计算关系匹配度相似度
            double PolyMatchReSim = 0;

            List<List<IPolygon>> aTargetPair = this.GetAdjacentPair(TargetPo.CuttedPolygons);
            double aTargetAreaSum = this.AreaSum(aTargetPair);
            List<List<IPolygon>> aMatchingPair = this.GetAdjacentPair(MatchingPo.CuttedPolygons);
            double aMatchingAreaSum = this.AreaSum(aMatchingPair);
            List<List<IPolygon>> mTargetPair = this.GetAdjacentPair(TargetPo.MatchedPolygons);
            List<List<IPolygon>> mMatchingPair = this.GetAdjacentPair(MatchingPo.MatchedPolygons);

            for (int i = 0; i < mTargetPair.Count; i++)
            {
                int tID1 = TargetPo.MatchedPolygons.IndexOf(mTargetPair[i][0]);
                int tID2 = TargetPo.MatchedPolygons.IndexOf(mTargetPair[i][1]);
                List<IPolygon> CacheTargetPoList = new List<IPolygon>();
                CacheTargetPoList.Add(mTargetPair[i][0]); CacheTargetPoList.Add(mTargetPair[i][1]);
                double CacheTargetPoListArea = this.AreaSum(CacheTargetPoList);
                double SumArea=aTargetAreaSum+aMatchingAreaSum;

                for (int j = 0; j < mMatchingPair.Count; j++)
                {
                    #region 如果是匹配对，计算匹配对相似度
                    int mID1 = MatchingPo.MatchedPolygons.IndexOf(mMatchingPair[j][0]);
                    int mID2 = MatchingPo.MatchedPolygons.IndexOf(mMatchingPair[j][1]);

                    List<IPolygon> CacheMatchPoList = new List<IPolygon>();
                    if (tID1 == mID1 && tID2 == mID2)
                    {
                        CacheMatchPoList.Add(mMatchingPair[j][0]);
                        CacheMatchPoList.Add(mMatchingPair[j][1]);

                        double ReSim = this.GetMatchingRelationSim(CacheTargetPoList, CacheMatchPoList, ReShapeWeigth, ReSizeWeigth, ReOriWeigth, RegOriWeigth);
                        double CacheMatchingPoListArea = this.AreaSum(CacheMatchPoList);

                        #region 面积加权
                        if (Type == 0)
                        {
                            PolyMatchReSim = (CacheTargetPoListArea + CacheMatchingPoListArea) / SumArea * ReSim;
                        }
                        #endregion
                    }
                    if (tID1 == mID2 && tID2 == mID1)
                    {
                        CacheMatchPoList.Add(mMatchingPair[j][1]);
                        CacheMatchPoList.Add(mMatchingPair[j][0]);

                        double ReSim = this.GetMatchingRelationSim(CacheTargetPoList, CacheMatchPoList, ReShapeWeigth, ReSizeWeigth, ReOriWeigth, RegOriWeigth);
                        double CacheMatchingPoListArea = this.AreaSum(CacheMatchPoList);
                        #region 面积加权
                        if (Type == 0)
                        {
                            PolyMatchReSim = (CacheTargetPoListArea + CacheMatchingPoListArea) / SumArea * ReSim;
                        }
                        #endregion
                    }
                    #endregion
                }
            }
            #endregion

            #region 综合计算两者的相似度
            outgSim = (pPolyMatchSim + PolyMatchReSim * RelationWeight) / (1 + RelationWeight);
            #endregion

            return outgSim;
        }

        /// <summary>
        /// 计算两个给定的Cuttpolygon的相似程度
        /// 复合的匹配对相似度和空间关系相似度为各相似度相乘（两个匹配对，还是以一定权重相乘）
        /// </summary>
        /// <param name="TargetPo"></param>
        /// <param name="MatchingPo"></param>
        /// <param name="Type"></param>
        /// <param name="RelationWeight"></param>
        /// <returns></returns>
        public double CuttedPolygonSim(CuttedPolygon TargetPo, CuttedPolygon MatchingPo, int Type, double RelationWeight)
        {
            double outgSim = 0;

            #region 计算实体匹配对相似度
            double pPolyMatchSim = 0;
            double tCutPolysAreaSum = this.AreaSum(TargetPo.CuttedPolygons);
            double mCutPolyAreaSum = this.AreaSum(MatchingPo.CuttedPolygons);
            double PolysAreaSum = tCutPolysAreaSum + mCutPolyAreaSum;
            List<double> PolyMatchSim = this.GetMatchingSim(TargetPo.MatchedPolygons, MatchingPo.MatchedPolygons);
            #region 按照面积进行加权
            if (Type == 0)
            {
                for (int i = 0; i < PolyMatchSim.Count; i++)
                {
                    List<IPolygon> CacheAreaComList = new List<IPolygon>();
                    CacheAreaComList.Add(TargetPo.MatchedPolygons[i]);
                    CacheAreaComList.Add(MatchingPo.MatchedPolygons[i]);
                    double CacheArea = this.AreaSum(CacheAreaComList);

                    pPolyMatchSim = pPolyMatchSim + PolyMatchSim[i] * CacheArea / (PolysAreaSum);
                }
            }
            #endregion
            #endregion

            #region 计算关系匹配相似度
            double PolyMatchReSim = 0;

            List<List<IPolygon>> aTargetPair = this.GetAdjacentPair(TargetPo.CuttedPolygons);//获取给定建筑物的邻近关系
            double aTargetAreaSum = this.AreaSum(aTargetPair);//边对应建筑物总面积
            List<List<IPolygon>> aMatchingPair = this.GetAdjacentPair(MatchingPo.CuttedPolygons);//获取给定建筑物的邻近关系
            double aMatchingAreaSum = this.AreaSum(aMatchingPair);//边对应建筑物总面积
            double SumArea = aTargetAreaSum + aMatchingAreaSum;

            List<List<IPolygon>> mTargetPair = this.GetAdjacentPair(TargetPo.MatchedPolygons);//获取给定的匹配建筑物的邻近关系
            List<List<IPolygon>> mMatchingPair = this.GetAdjacentPair(MatchingPo.MatchedPolygons);//获取给定的匹配建筑物的邻近关系

            for (int i = 0; i < mTargetPair.Count; i++)
            {
                int tID1 = TargetPo.MatchedPolygons.IndexOf(mTargetPair[i][0]);
                int tID2 = TargetPo.MatchedPolygons.IndexOf(mTargetPair[i][1]);
                List<IPolygon> CacheTargetPoList = new List<IPolygon>();
                CacheTargetPoList.Add(mTargetPair[i][0]); CacheTargetPoList.Add(mTargetPair[i][1]);
                double CacheTargetPoListArea = this.AreaSum(CacheTargetPoList);
                
                for (int j = 0; j < mMatchingPair.Count; j++)
                {
                    #region 如果是匹配对，计算匹配对相似度
                    int mID1 = MatchingPo.MatchedPolygons.IndexOf(mMatchingPair[j][0]);
                    int mID2 = MatchingPo.MatchedPolygons.IndexOf(mMatchingPair[j][1]);

                    List<IPolygon> CacheMatchPoList = new List<IPolygon>();
                    if (tID1 == mID1 && tID2 == mID2)
                    {
                        CacheMatchPoList.Add(mMatchingPair[j][0]);
                        CacheMatchPoList.Add(mMatchingPair[j][1]);

                        double ReSim = this.GetMatchingRelationSim(CacheTargetPoList, CacheMatchPoList);
                        double CacheMatchingPoListArea = this.AreaSum(CacheMatchPoList);

                        #region 面积加权
                        if (Type == 0)
                        {
                           PolyMatchReSim = (CacheTargetPoListArea + CacheMatchingPoListArea) / SumArea * ReSim;
                        }
                        #endregion
                    }
                    if (tID1 == mID2 && tID2 == mID1)
                    {
                        CacheMatchPoList.Add(mMatchingPair[j][1]);
                        CacheMatchPoList.Add(mMatchingPair[j][0]);

                        double ReSim = this.GetMatchingRelationSim(CacheTargetPoList, CacheMatchPoList);
                        double CacheMatchingPoListArea = this.AreaSum(CacheMatchPoList);
                        #region 面积加权
                        if (Type == 0)
                        {
                            PolyMatchReSim = (CacheTargetPoListArea + CacheMatchingPoListArea) / SumArea * ReSim;
                        }
                        #endregion
                    }
                    #endregion
                }
            }
            #endregion

            #region 综合计算两者的相似度
            outgSim = (pPolyMatchSim + PolyMatchReSim * RelationWeight) / (1 + RelationWeight);
            #endregion

            return outgSim;
        }
    }
}
