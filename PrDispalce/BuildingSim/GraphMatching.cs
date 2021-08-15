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

namespace PrDispalce.BuildingSim
{
    /// <summary>
    /// 基于概率松弛对建筑物目标进行匹配
    /// </summary>
    class GraphMatching
    {
        PublicUtil Pu = new PublicUtil();

        double[,] initialSim;//匹配相似性矩阵
        double[,] SupportSim;//匹配支持度矩阵

        List<PolygonObject> sourcePos = new List<PolygonObject>();//源建筑物剖分结果
        List<PolygonObject> targetPos = new List<PolygonObject>();//目标建筑物剖分结果

        public List<IPolygon> sourcePolygons = new List<IPolygon>();//源建筑物剖分结果
        public List<IPolygon> targetPolygons = new List<IPolygon>();//目标建筑物剖分结果

        List<PolygonObject> MatchingsourcePos = new List<PolygonObject>();//匹配的源建筑物列表
        List<PolygonObject> MatchingtargetPos = new List<PolygonObject>();//匹配的目标建筑物列表

        public List<IPolygon> MatchingsourcePolygons = new List<IPolygon>();//匹配的源建筑物列表
        public List<IPolygon> MatchingtargetPolygons = new List<IPolygon>();//匹配的目标建筑物列表

        /// <summary>
        /// 构造函数1
        /// </summary>
        public GraphMatching()
        {

        }
        /// <summary>
        /// 构造函数2
        /// </summary>
        /// <param name="sourcePos"></param>
        /// <param name="targetPos"></param>
        public GraphMatching(List<PolygonObject> sourcePos,List<PolygonObject> targetPos)
        {
            this.sourcePos = sourcePos;
            this.targetPos = targetPos;
            initialSim = new double[sourcePos.Count, targetPos.Count];
            SupportSim = new double[sourcePos.Count, targetPos.Count];

            for (int i = 0; i < sourcePos.Count; i++)
            {
                this.sourcePolygons.Add(Pu.PolygonObjectConvert(sourcePos[i]));
            }

            for (int i = 0; i < targetPos.Count; i++)
            {
                this.targetPolygons.Add(Pu.PolygonObjectConvert(targetPos[i]));
            }
        }

        /// <summary>
        /// 依据建筑物间大小+形状相似度计算初始匹配矩阵
        /// </summary>
        public void initialSimCom()
        {
            BuildingPairSim BS = new BuildingPairSim();//计算建筑物匹配对的相似度

            for(int i=0;i<sourcePos.Count;i++)
            {
                for(int j=0;j<targetPos.Count;j++)
                {
                    double ShapeSim = BS.ShapeSimilarity(sourcePos[i], targetPos[j], 1);//基于转角函数计算两个图形的形状相似性[0,1]
                    double SizeSim = BS.SizeSimilarity(sourcePos[i], targetPos[j], 0);//计算两个建筑物的面积相似性[0,1]

                    initialSim[i, j] = ShapeSim * 0.5 + SizeSim * 0.5;
                }
            }
            
        }

        /// <summary>
        /// 依据建筑物邻近范围内建筑物的配对情况计算配对支持系数；后可据此进行更新
        /// </summary>
        public void SupportSimCom()
        {
            for (int i = 0; i < sourcePos.Count; i++)
            {
                for (int j = 0; j < targetPos.Count; j++)
                {
                    SupportSim[i, j] = this.pairSupportSim(sourcePos[i], targetPos[j]);
                }
            }
        }


        /// <summary>
        /// 计算给定的一对建筑物的兼容系数
        /// </summary>
        /// <param name="sourcePo"></param>
        /// <param name="TargetPo"></param>
        /// <returns></returns>
        public double pairSupportSim(PolygonObject sourcePo, PolygonObject targetPo)
        {
            double SupportSim = 0;

            #region 计算过程
            List<PolygonObject> sourceSurr = sourcePo.SurrPos;//sourcePo的邻近范围内建筑物
            List<PolygonObject> targetSurr = targetPo.SurrPos;//targetPo的邻近范围内建筑物
            ContextSim CS = new ContextSim();
            PublicUtil Pu = new PublicUtil();
            List<double> AllSupportSim = new List<double>();

            if (sourceSurr.Count == 0 || targetSurr.Count == 0)
            {
                return SupportSim;
            }

            else
            {
                for (int i = 0; i < sourceSurr.Count; i++)
                {
                    List<IPolygon> CachetPair = new List<IPolygon>();
                    CachetPair.Add(Pu.PolygonObjectConvert(sourcePo));
                    CachetPair.Add(Pu.PolygonObjectConvert(sourceSurr[i]));
                    for (int j = 0; j < targetSurr.Count; j++)
                    {
                        List<IPolygon> CachemPair = new List<IPolygon>();
                        CachemPair.Add(Pu.PolygonObjectConvert(targetPo));
                        CachemPair.Add(Pu.PolygonObjectConvert(targetSurr[j]));
                        double CacheSupportSim = CS.GetMatchingRelationSim(CachetPair, CachemPair);

                        int indexSource = sourcePos.IndexOf(sourceSurr[i]);
                        int indexTarget = targetPos.IndexOf(targetSurr[j]);
                        double MatchIndex = initialSim[indexSource, indexTarget];

                        AllSupportSim.Add(CacheSupportSim * MatchIndex);
                    }
                }
            }

            SupportSim = AllSupportSim.Max();///返回最大的Pair
            #endregion
         
            return SupportSim;
        }

        /// <summary>
        /// 获得SourcePos和TargetPos的邻域范围（存在Touch关系的建筑物为其邻域范围）
        /// </summary>
        public void PoSurr()
        {
            BuildingPairSim BS = new BuildingPairSim();

            #region SourcePos处理
            for (int i = 0; i < sourcePos.Count; i++)
            {
                for (int j = 0; j < sourcePos.Count; j++)
                {
                    if (i != j)
                    {
                        int TopoRelType=BS.TopoRelationComputation(sourcePos[i], sourcePos[j]);
                        if (TopoRelType >= 2)//若两建筑物相切或相交，则为建筑物的邻域范围
                        {
                            sourcePos[i].SurrPos.Add(sourcePos[j]);
                        }
                    }
                }
            }
            #endregion

            #region TargetPos处理
            for (int i = 0; i < targetPos.Count; i++)
            {
                for (int j = 0; j < targetPos.Count; j++)
                {
                    if (i != j)
                    {
                        int TopoRelType = BS.TopoRelationComputation(targetPos[i], targetPos[j]);
                        if (TopoRelType >= 2)//若两建筑物相切或相交，则为建筑物的邻域范围
                        {
                            targetPos[i].SurrPos.Add(targetPos[j]);
                        }
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// initialSim矩阵更新
        /// return StopLabel=false，下一步结束更新；=true，下一步继续更新
        /// </summary>
        public bool initialSimUpdate(double StopT)
        {
            bool StopLabel = false;
            double[,] CacheInitialSim = new double[sourcePos.Count, targetPos.Count];

            #region 更新值存入虚拟矩阵
            for (int i = 0; i < sourcePos.Count; i++)
            {
                #region 计算Cachei
                double Cachei=0;

                for(int j=0;j<targetPos.Count;j++)
                {
                    Cachei=Cachei+SupportSim[i,j];
                }
                #endregion

                #region 更新过程
                for (int j = 0; j < targetPos.Count; j++)
                {
                    CacheInitialSim[i, j] = (initialSim[i, j] + SupportSim[i, j]) / (1 + Cachei);
                }
                #endregion
            }
            #endregion

            #region 初始矩阵进行更新
            for (int i = 0; i < sourcePos.Count; i++)
            {
                for (int j = 0; j < targetPos.Count; j++)
                {
                    #region 判断更新是否结束
                    if (Math.Abs(CacheInitialSim[i, j] - initialSim[i, j]) > StopT)
                    {
                        StopLabel = true;
                    }
                    #endregion

                    initialSim[i, j] = CacheInitialSim[i, j];
                }
            }
            #endregion

            return StopLabel;
        }

        /// <summary>
        /// 概率松弛匹配过程
        /// </summary>
        public void GraphMatchingProcess(double StopT)
        {
            this.PoSurr();//获得建筑物的邻域范围
            this.initialSimCom();//计算初始匹配矩阵
            this.SupportSimCom();//计算支持矩阵

            bool Stop = true;
            while (Stop)
            {
                Stop = initialSimUpdate(StopT);//更新匹配矩阵
                this.SupportSimCom();//更新支持矩阵
            }

          this.GetMatchingPos();
        }
       
        /// <summary>
        /// 获得基于匹配矩阵的建筑物匹配关系
        /// </summary>
        public void GetMatchingPos()
        {
            double sourceCount = this.sourcePos.Count;
            double targetCount = this.targetPos.Count;
            double minCount = Math.Min(sourceCount, targetCount);

            double maxMatrix=0;
            do
            {
                maxMatrix = this.GetMaxMatrix();
                List<int> Listij = this.Getij(maxMatrix);

                if (!MatchingsourcePos.Contains(sourcePos[Listij[0]]) && !MatchingtargetPos.Contains(targetPos[Listij[1]]))
                {
                    MatchingsourcePos.Add(sourcePos[Listij[0]]);
                    MatchingtargetPos.Add(targetPos[Listij[1]]);

                    MatchingsourcePolygons.Add(sourcePolygons[Listij[0]]);
                    MatchingtargetPolygons.Add(targetPolygons[Listij[1]]);
                }

                initialSim[Listij[0], Listij[1]] = 0;
            } while (MatchingtargetPos.Count < minCount||maxMatrix<=0);

            //int testloca = 0;
        }

        /// <summary>
        /// 获取二维数组的最大值
        /// 备注：Max>0
        /// </summary>
        /// <returns></returns>
        public double GetMaxMatrix()
        {
            double Max = 0;

            for (int i = 0; i < sourcePos.Count; i++)
            {
                for (int j = 0; j < targetPos.Count; j++)
                {
                    if (initialSim[i, j] > Max)
                    {
                        Max = initialSim[i, j];
                    }
                }
            }

            return Max;
        }

        /// <summary>
        /// 获得给定值的ij编号 Max>0
        /// </summary>
        /// <param name="Max"></param>
        /// <returns></returns>
        public List<int> Getij(double Max)
        {
            List<int> Listij = new List<int>();

            for (int i = 0; i < sourcePos.Count; i++)
            {
                for (int j = 0; j < targetPos.Count; j++)
                {
                    if (initialSim[i, j] ==Max)
                    {
                        Listij.Add(i);
                        Listij.Add(j);
                    }
                }
            }

            return Listij;
        }
    }
}
