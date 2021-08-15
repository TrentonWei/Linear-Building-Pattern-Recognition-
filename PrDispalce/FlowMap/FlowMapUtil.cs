using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.Geoprocessing;
using AuxStructureLib;
using AuxStructureLib.IO;

namespace PrDispalce.FlowMap
{
    /// <summary>
    /// UtilForFlowMap
    /// </summary>
    class FlowMapUtil
    {
        PrDispalce.BuildingSim.PublicUtil Pu = new BuildingSim.PublicUtil();
        FlowSup Fs = new FlowSup();

        /// <summary>
        /// 获得图层的范围
        /// </summary>
        /// <param name="pFeatureLayer"></param>
        /// <returns></returns>
        public double[] GetExtend(IFeatureLayer pFeatureLayer)
        {
            double[] ExtendValue = new double[4];
            IGeoDataset pGeoDataset = pFeatureLayer as IGeoDataset;
            IEnvelope Extend = pGeoDataset.Extent;
            ExtendValue[0] = Extend.XMin; ExtendValue[1] = Extend.YMin; ExtendValue[2] = Extend.XMax; ExtendValue[3] = Extend.YMax;

            return ExtendValue;
        }

        /// <summary>
        /// 获取给定的ODPoints
        /// </summary>
        /// <param name="pFeatureClass"></param>
        /// <param name="OriginPoint"></param>起点
        /// <param name="DesPoints"></param>终点
        /// <param name="AllPoints"></param>所有点
        /// <param name="PointFlow"></param>各点（des）流量统计
        public void GetOD(IFeatureClass pFeatureClass, IPoint OriginPoint,List<IPoint> DesPoints,List<IPoint> AllPoints,Dictionary<IPoint, double> PointFlow)
        {
            IFeatureCursor pFeatureCursor = pFeatureClass.Update(null, true);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                double Loc = Pu.GetValue(pFeature, "ID");
                IPoint nPoint = pFeature.Shape as IPoint;

                if (Pu.GetValue(pFeature, "ID") == 0)
                {
                    double Flow = Pu.GetValue(pFeature, "FlowOut");
                    OriginPoint.X = nPoint.X;///pfeatureCursor本身可能存在问题
                    OriginPoint.Y = nPoint.Y;
                    AllPoints.Add(OriginPoint);
                    PointFlow.Add(OriginPoint, Flow);
                }

                else
                {
                    double Flow = Pu.GetValue(pFeature, "FlowOut");
                    IPoint rPoint = new PointClass();///pfeatureCursor本身可能存在问题
                    rPoint.X = nPoint.X;
                    rPoint.Y = nPoint.Y;
                    DesPoints.Add(rPoint);
                    AllPoints.Add(rPoint);
                    PointFlow.Add(rPoint, Flow);
                }

                pFeature = pFeatureCursor.NextFeature();
            }
        }

        /// <summary>
        /// 获取给定图层的Features
        /// </summary>
        /// <param name="pFeatureLayers"></param>
        /// <returns></returns>
        public List<Tuple<IGeometry, esriGeometryType>> GetFeatures(List<IFeatureLayer> pFeatureLayers)
        {
            List<Tuple<IGeometry, esriGeometryType>> Features = new List<Tuple<IGeometry, esriGeometryType>>();

            #region 获取Featuers
            for (int i = 0; i < pFeatureLayers.Count; i++)
            {
                IFeatureClass pFeatureClass = pFeatureLayers[i].FeatureClass;
                for (int j = 0; j < pFeatureClass.FeatureCount(null); j++)
                {
                    IFeature pFeature = pFeatureClass.GetFeature(j);

                    IGeometry pGeometry = pFeature.Shape;
                    Tuple<IGeometry, esriGeometryType> CacheTuple = new Tuple<IGeometry, esriGeometryType>(pGeometry, pFeatureLayers[i].FeatureClass.ShapeType);
                    Features.Add(CacheTuple);
                }
            }
            #endregion

            return Features;
        }

        /// <summary>
        /// 获取总流量各点（des）流量统计
        /// </summary>
        /// <param name="PointFlow"></param>
        /// <returns></returns>
        public double GetAllVolume(Dictionary<IPoint, double> PointFlow)
        {
            return PointFlow.Values.Sum();
        }

        /// <summary>
        /// 获取最大流量各点（des）流量统计
        /// </summary>
        /// <param name="PointFlow"></param>
        /// <returns></returns>
        public double GetMaxVolume(Dictionary<IPoint, double> PointFlow)
        {
            return PointFlow.Values.Max();
        }

        /// <summary>
        /// 获取最小流量
        /// </summary>
        /// <param name="PointFlow"></param>各点（des）流量统计
        /// <returns></returns>
        public double GetMinVolume(Dictionary<IPoint, double> PointFlow)
        {
            return PointFlow.Values.Min();
        }

        /// <summary>
        /// 判断是否满足角度约束条件
        /// </summary>
        /// <param name="CacheShortPath"></param>待判断路径
        /// <param name="Path"></param>路径连接的主流路径
        /// <param name="Grids"></param>网格编码
        /// <returns></returns>true表示不满足角度约束限制
        /// false表示满足角度约束显示
        public bool AngleContraint(List<Tuple<int, int>> CacheShortPath, Path Path, Dictionary<Tuple<int, int>, List<double>> Grids)
        {
            Tuple<int, int> FromGrid = null;
            Tuple<int, int> MidGrid = null;
            Tuple<int, int> ToGrid = null;
            double CacheAngle = 0;
            if (CacheShortPath != null && CacheShortPath.Count >= 2 && Path.ePath.Count >= 2)
            {
                FromGrid = CacheShortPath[1];
                MidGrid = CacheShortPath[0];
                ToGrid = Path.ePath[Path.ePath.Count - 2];

                TriNode FromPoint = new TriNode();
                TriNode MidPoint = new TriNode();
                TriNode ToPoint = new TriNode();

                FromPoint.X = (Grids[FromGrid][0] + Grids[FromGrid][2]) / 2;
                FromPoint.Y = (Grids[FromGrid][1] + Grids[FromGrid][3]) / 2;

                MidPoint.X = (Grids[MidGrid][0] + Grids[MidGrid][2]) / 2;
                MidPoint.Y = (Grids[MidGrid][1] + Grids[MidGrid][3]) / 2;

                ToPoint.X = (Grids[ToGrid][0] + Grids[ToGrid][2]) / 2;
                ToPoint.Y = (Grids[ToGrid][1] + Grids[ToGrid][3]) / 2;

                CacheAngle = Pu.GetAngle(MidPoint, ToPoint, FromPoint);

                //if (CacheAngle > 3.1415927 / 3 && CacheAngle < 2 * 3.1415926 / 3)
                if (CacheAngle < 2 * 3.1415926 / 3 && CacheAngle > 3.1415926 / 72)
                {
                    return true;
                }
            }

            else
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static object Clone(object obj)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, obj);
            memoryStream.Position = 0;
            return formatter.Deserialize(memoryStream);
        }

        /// <summary>
        /// 添加FlowPath与destination重叠的约束条件
        /// </summary>
        /// <param name="desGrids">desPoint的格网</param>
        /// <param name="WeighGrids">权重格网</param>
        /// <param name="k">删除的限制数量</param>K=1表示自身；=2表示2阶邻近
        /// <param name="j">当前格网编码</param>
        public void FlowOverLayContraint(List<Tuple<int, int>> desGrids,Dictionary<Tuple<int, int>, double> WeighGrids, int k, Tuple<int,int> TaretDes)
        {
            for (int n = 0; n < desGrids.Count; n++)
            {
                if (desGrids[n].Item1 != TaretDes.Item1 || desGrids[n].Item2 != TaretDes.Item2)
                {
                    List<Tuple<int, int>> NearGrids = Fs.GetNearGrids(desGrids[n], WeighGrids.Keys.ToList(), k);

                    foreach (Tuple<int, int> Grid in NearGrids)
                    {
                        WeighGrids.Remove(Grid);
                    }
                }
            }
        }

        /// <summary>
        /// 添加FlowPath与destination重叠的约束条件
        /// </summary>
        /// <param name="desGrids">desPoint的格网</param>
        /// <param name="WeighGrids">权重格网</param>
        /// <param name="k">删除的限制数量</param>K=1表示自身；=2表示2阶邻近
        /// <param name="j">当前格网编码</param>
        public void FlowCrosssingContraint(List<Tuple<int, int>> desGrids, Dictionary<Tuple<int, int>, double> WeighGrids, int k, Tuple<int, int> TaretDes,Tuple<int,int> OriginGrid,List<Tuple<int,int>> PathGrids)
        {
            #region 移除Desgrids
            for (int n = 0; n < desGrids.Count; n++)
            {
                if (desGrids[n].Item1 != TaretDes.Item1 || desGrids[n].Item2 != TaretDes.Item2)
                {
                    List<Tuple<int, int>> NearGrids = Fs.GetNearGrids(desGrids[n], WeighGrids.Keys.ToList(), k);

                    foreach (Tuple<int, int> Grid in NearGrids)
                    {
                        if (WeighGrids.ContainsKey(Grid))
                        {
                            WeighGrids.Remove(Grid);
                        }
                    }
                }
            }
            #endregion

            #region 移除PathGrids
            for (int n = 0; n < PathGrids.Count; n++)
            {
                if (PathGrids[n].Item1 != OriginGrid.Item1 || PathGrids[n].Item2 != OriginGrid.Item2)
                {
                    List<Tuple<int, int>> NearGrids = Fs.GetNearGrids(PathGrids[n], WeighGrids.Keys.ToList(), 0);

                    foreach (Tuple<int, int> Grid in NearGrids)
                    {
                        if (WeighGrids.ContainsKey(Grid))
                        {
                            WeighGrids.Remove(Grid);
                        }
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// 获得给定的所有des节点在不同方向下的搜索图
        /// </summary>
        /// <param name="pWeighGrids"></param>
        /// <param name="desGrids"></param>
        /// <returns></returns>
        public Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> GetDesDirPt(Dictionary<Tuple<int, int>, double> pWeighGrids, List<Tuple<int, int>> desGrids)
        {
            Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> DesDirPt = new Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>>();

            foreach (Tuple<int, int> Grid in desGrids)
            {
                Dictionary<int, PathTrace> CacheDirPt = this.GetDirPt(pWeighGrids, Grid, desGrids, Grid);
                DesDirPt.Add(Grid, CacheDirPt);
            }


            return DesDirPt;
        }


        /// <summary>
        /// 获得给定的所有des节点在不同方向下的搜索图
        /// </summary>
        /// <param name="pWeighGrids"></param>
        /// <param name="desGrids"></param>
        /// <returns></returns>
        public Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> GetDesDirPtDis(Dictionary<Tuple<int, int>, double> pWeighGrids, List<Tuple<int, int>> desGrids, Dictionary<Tuple<int, int>, int> GridType)
        {
            Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> DesDirPt = new Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>>();


            foreach (Tuple<int, int> Grid in desGrids)
            {
                Dictionary<int, PathTrace> CacheDirPt = this.GetDirPtDis(pWeighGrids, Grid, desGrids, Grid, GridType);
                DesDirPt.Add(Grid, CacheDirPt);
            }

            return DesDirPt;
        }

        /// <summary>
        /// 获得给定节点不同约束方向下的搜索图1
        /// </summary>
        /// <param name="WeighGrids">权重格网</param>
        /// <param name="Grid">目标网格</param>
        /// <param name="desGrids">所有目标格网</param>
        /// <param name="i">当前格网编号</param>
        /// <returns></returns>获取给定节点不同方向编码的路径搜索
        public Dictionary<int, PathTrace> GetDirPt(Dictionary<Tuple<int, int>, double> pWeighGrids, Tuple<int, int> Grid, List<Tuple<int, int>> desGrids,Tuple<int,int> TargetDes)
        {          
            Dictionary<int, PathTrace> DirPt = new Dictionary<int, PathTrace>();
            for (int n = 0; n < 9; n++)
            {
                #region 获取DirList
                List<int> DirList = new List<int>();
                if (n >= 4 && n <= 6)
                {
                    DirList.Add(n);
                    DirList.Add(n + 1);
                    DirList.Add(n + 2);
                }

                else if (n >= 1 && n <= 3)
                {
                    DirList.Add(n + 2);
                    DirList.Add(n + 1);
                    DirList.Add(n);
                }

                else if (n == 0)
                {
                    DirList.Add(2);
                    DirList.Add(1);
                    DirList.Add(8);
                }

                else if (n == 7)
                {
                    DirList.Add(7);
                    DirList.Add(8);
                    DirList.Add(1);
                }

                else if (n == 8)
                {
                    DirList.Add(1);
                    DirList.Add(2);
                    DirList.Add(3);
                    DirList.Add(4);
                    DirList.Add(5);
                    DirList.Add(6);
                    DirList.Add(7);
                    DirList.Add(8);
                }
                #endregion

                Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝
                this.FlowOverLayContraint(desGrids, WeighGrids, 0, TargetDes);//Overlay约束
                PathTrace Pt = new PathTrace();
                List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                JudgeList.Add(Grid);//添加搜索的起点
                Pt.MazeAlg(JudgeList, WeighGrids, 1, DirList);//备注：每次更新以后,WeightGrid会清零  

                int Number = this.GetNumber(DirList);
                DirPt.Add(Number, Pt);
            }

            return DirPt;
        }

        /// <summary>
        /// 获得给定节点不同约束方向下的搜索图1[考虑网格距离差异]
        /// </summary>
        /// <param name="WeighGrids">权重格网</param>
        /// <param name="Grid">目标网格</param>
        /// <param name="desGrids">所有目标格网</param>
        /// <param name="i">当前格网编号</param>
        /// <returns></returns>获取给定节点不同方向编码的路径搜索
        public Dictionary<int, PathTrace> GetDirPtDis(Dictionary<Tuple<int, int>, double> pWeighGrids, Tuple<int, int> Grid, List<Tuple<int, int>> desGrids, Tuple<int, int> TargetDes, Dictionary<Tuple<int, int>, int> GridType)
        {
            Dictionary<int, PathTrace> DirPt = new Dictionary<int, PathTrace>();
            for (int n = 0; n < 9; n++)
            {
                #region 获取DirList
                List<int> DirList = new List<int>();
                if (n >= 4 && n <= 6)
                {
                    DirList.Add(n);
                    DirList.Add(n + 1);
                    DirList.Add(n + 2);
                }

                else if (n >= 1 && n <= 3)
                {
                    DirList.Add(n + 2);
                    DirList.Add(n + 1);
                    DirList.Add(n);
                }

                else if (n == 0)
                {
                    DirList.Add(2);
                    DirList.Add(1);
                    DirList.Add(8);
                }

                else if (n == 7)
                {
                    DirList.Add(7);
                    DirList.Add(8);
                    DirList.Add(1);
                }

                else if (n == 8)
                {
                    DirList.Add(1);
                    DirList.Add(2);
                    DirList.Add(3);
                    DirList.Add(4);
                    DirList.Add(5);
                    DirList.Add(6);
                    DirList.Add(7);
                    DirList.Add(8);
                }
                #endregion

                Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝
                Dictionary<Tuple<int, int>, int> pGridVisit = Clone((object)GridType) as Dictionary<Tuple<int, int>, int>;//深拷贝

                this.FlowOverLayContraint(desGrids, WeighGrids, 0, TargetDes);//Overlay约束
                PathTrace Pt = new PathTrace();
                List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                JudgeList.Add(Grid);//添加搜索的起点
                Pt.MazeAlgDis(JudgeList, WeighGrids, 1, DirList,GridType,pGridVisit);//备注：每次更新以后,WeightGrid会清零  

                int Number = this.GetNumber(DirList);
                DirPt.Add(Number, Pt);
            }

            return DirPt;
        }

        /// <summary>
        /// 获取给定路径的长度
        /// </summary>
        /// <param name="ShortestPath"></param>
        /// <returns></returns>
        public double GetPathLength(List<Tuple<int, int>> ShortestPath)
        {
            double Length = 0;

            for (int i = 0; i < ShortestPath.Count - 1; i++)
            {
                if (ShortestPath[i].Item1 == ShortestPath[i + 1].Item1 ||
                   ShortestPath[i].Item2 == ShortestPath[i + 1].Item2)
                {
                    Length = Length + 1;
                }

                else
                {
                    Length = Length + Math.Sqrt(2);
                }
            }

            return Length;
        }


        /// <summary>
        /// 获取给定路径的长度
        /// </summary>
        /// <param name="ShortestPath"></param>
        /// <returns></returns>
        public double GetPathLengthDis(List<Tuple<int, int>> ShortestPath,Dictionary<Tuple<int,int>,int> GridType)
        {
            double Length = 0;

            for (int i = 0; i < ShortestPath.Count - 1; i++)
            {
                if (ShortestPath[i].Item1 == ShortestPath[i + 1].Item1 ||
                   ShortestPath[i].Item2 == ShortestPath[i + 1].Item2)
                {
                    Length = (GridType[ShortestPath[i]] + 1) / 2 + (GridType[ShortestPath[i + 1]] + 1) / 2 + Length;
                }

                else
                {
                    Length = Length + (GridType[ShortestPath[i]] + 1) / 2 * Math.Sqrt(2) + (GridType[ShortestPath[i + 1]] + 1) / 2 * Math.Sqrt(2);
                }
            }

            return Length;
        }

        /// <summary>
        /// 获取给定路径的长度(类型对换)
        /// </summary>
        /// <param name="ShortestPath"></param>
        /// <returns></returns>
        public double GetPathLengthDisRever(List<Tuple<int, int>> ShortestPath, Dictionary<Tuple<int, int>, int> GridType)
        {
            double Length = 0;

            for (int i = 0; i < ShortestPath.Count - 1; i++)
            {
                if (ShortestPath[i].Item1 == ShortestPath[i + 1].Item1 ||
                   ShortestPath[i].Item2 == ShortestPath[i + 1].Item2)
                {
                    Length = (Math.Abs(GridType[ShortestPath[i]]-10) + 1) / 2 + (Math.Abs(GridType[ShortestPath[i + 1]]-10) + 1) / 2 + Length;
                }

                else
                {
                    Length = Length + (Math.Abs(GridType[ShortestPath[i]]-10) + 1) / 2 * Math.Sqrt(2) + (Math.Abs(GridType[ShortestPath[i + 1]]-10) + 1) / 2 * Math.Sqrt(2);
                }
            }

            return Length;
        }

        /// <summary>
        /// 判断PathGrid是否能作为DesGrid的潜在FlowInlocations
        /// 判断应该是作为同一侧的点才有可能是FlowInLocation
        /// </summary>
        /// <param name="DesGrid">终点</param>
        /// <param name="PathGrid">流入点</param>
        /// <param name="StartGrid">起点</param>
        /// <returns></returns>
        public bool JudgeGrid(Tuple<int, int> DesGrid, Tuple<int, int> PathGrid, Tuple<int, int> StartGrid)
        {
            #region 判断起点在终点的哪一侧
            int DSI = DesGrid.Item1 - StartGrid.Item1;
            int DSJ = DesGrid.Item2 - StartGrid.Item2;

            int PSI = PathGrid.Item1 - StartGrid.Item1;
            int PSJ = PathGrid.Item2 - StartGrid.Item2;
            #endregion

            if (DSI * PSI >= 0 && PSJ * DSJ >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断PathGrid是否能作为DesGrid的潜在FlowInlocations
        /// 判断应该是作为同一侧的点才有可能是FlowInLocation
        /// </summary>
        /// <param name="DesGrid">终点</param>
        /// <param name="PathGrid">流入点</param>
        /// <param name="StartGrid">起点</param>
        /// <returns></returns>
        public bool JudgeGrid2(Tuple<int, int> DesGrid, Tuple<int, int> PathGrid, Tuple<int, int> StartGrid)
        {
            #region 判断起点在终点的哪一侧
            int MinI = Math.Min(DesGrid.Item1, StartGrid.Item1);
            int MaxI = Math.Max(DesGrid.Item1, StartGrid.Item1);
            int MinJ = Math.Min(DesGrid.Item2, StartGrid.Item2);
            int MaxJ = Math.Max(DesGrid.Item2, StartGrid.Item2);
            #endregion

            if (PathGrid.Item1 >= MinI && PathGrid.Item1 <= MaxI
                && PathGrid.Item2 >= MinJ && PathGrid.Item2 <= MaxJ)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断Grid是否与直线在同一侧
        /// </summary>
        /// <param name="DesGrid"></param>
        /// <param name="PathGrid"></param>
        /// <param name="StartGrid"></param>
        /// <returns></returns>
        public bool RLJudgeGrid(Tuple<int, int> DesGrid, Tuple<int, int> PathGrid, Tuple<int, int> StartGrid)
        {
            if (this.JudgeGrid2(DesGrid, PathGrid, StartGrid))
            {
                return true;
            }
            

            double DP=Math.Sqrt((PathGrid.Item1-DesGrid.Item1)*(PathGrid.Item1-DesGrid.Item1)+(PathGrid.Item2-DesGrid.Item2)*(PathGrid.Item2-DesGrid.Item2));
            double DS=Math.Sqrt((StartGrid.Item1-DesGrid.Item1)*(StartGrid.Item1-DesGrid.Item1)+(StartGrid.Item2-DesGrid.Item2)*(StartGrid.Item2-DesGrid.Item2));
            double PS=Math.Sqrt((PathGrid.Item1-StartGrid.Item1)*(PathGrid.Item1-StartGrid.Item1)+(PathGrid.Item2-StartGrid.Item2)*(PathGrid.Item2-StartGrid.Item2));

            double CosA = (DS * DS + DP * DP - PS * PS) / (2 * DS * DP);
            if (CosA >= 0)
            {
                return true;
            }

            else
            {
                return false;
            }
            
        }


        /// <summary>
        /// 判断ePoint向sPoint延伸的限制性方向
        /// </summary>
        /// <param name="sPoint"></param>
        /// <param name="ePoint"></param>
        /// <returns></returns>
        public List<int> GetConDir(Tuple<int, int> sPoint, Tuple<int, int> ePoint)
        {
            List<int> DirList = new List<int>();//获取限定的方向列表

            #region 获取限定方向(方向编码：1-8,1正下，顺时针编码)
            int IADD = sPoint.Item1 - ePoint.Item1;
            int JADD = sPoint.Item2 - ePoint.Item2;

            if (IADD == 0)
            {
                if (JADD > 0)
                {
                    DirList.Add(4); DirList.Add(5); DirList.Add(6);
                }

                if (JADD < 0)
                {
                    DirList.Add(2); DirList.Add(1); DirList.Add(8);
                }
            }

            else if (IADD > 0)
            {
                if (JADD > 0)
                {
                    DirList.Add(5); DirList.Add(6); DirList.Add(7);
                }

                if (JADD == 0)
                {
                    DirList.Add(6); DirList.Add(7); DirList.Add(8);
                }

                if (JADD < 0)
                {
                    DirList.Add(7); DirList.Add(8); DirList.Add(1);
                }
            }

            else if (IADD < 0)
            {
                if (JADD > 0)
                {
                    DirList.Add(5); DirList.Add(4); DirList.Add(3);
                }

                if (JADD == 0)
                {
                    DirList.Add(4); DirList.Add(3); DirList.Add(2);
                }

                if (JADD < 0)
                {
                    DirList.Add(3); DirList.Add(2); DirList.Add(1);
                }
            }
            #endregion

            //DirList.Sort();
            return DirList;
        }

        /// <summary>
        /// 判断ePoint向sPoint延伸的限制性方向
        /// </summary>
        /// <param name="sPoint"></param>
        /// <param name="ePoint"></param>
        /// <returns></returns>
        public List<int> GetConDirR(Tuple<int, int> sPoint, Tuple<int, int> ePoint)
        {
            List<int> DirList = new List<int>();//获取限定的方向列表

            #region 获取限定方向(方向编码：1-8,1正下，顺时针编码)
            int IADD = sPoint.Item1 - ePoint.Item1;
            int JADD = sPoint.Item2 - ePoint.Item2;

            if (IADD == 0)
            {
                if (JADD > 0)
                {
                    DirList.Add(6); DirList.Add(7); DirList.Add(8);
                }

                if (JADD < 0)
                {
                    DirList.Add(4); DirList.Add(3); DirList.Add(2);
                }
            }

            else if (IADD > 0)
            {
                if (JADD > 0)
                {
                    DirList.Add(5); DirList.Add(6); DirList.Add(7);
                }

                if (JADD == 0)
                {
                    DirList.Add(4); DirList.Add(5); DirList.Add(6);
                }

                if (JADD < 0)
                {
                    DirList.Add(5); DirList.Add(4); DirList.Add(3);
                }
            }

            else if (IADD < 0)
            {
                if (JADD > 0)
                {
                    DirList.Add(7); DirList.Add(8); DirList.Add(1);
                }

                if (JADD == 0)
                {
                    DirList.Add(2); DirList.Add(1); DirList.Add(8);
                }

                if (JADD < 0)
                {
                    DirList.Add(3); DirList.Add(2); DirList.Add(1);
                }
            }
            #endregion

            //DirList.Sort();
            return DirList;
        }

        /// <summary>
        /// 判断ePoint向sPoint延伸的限制性方向
        /// </summary>
        /// <param name="sPoint"></param>
        /// <param name="ePoint"></param>
        /// <returns></returns>
        public List<int> GetConDir2(Tuple<int, int> sPoint, Tuple<int, int> ePoint)
        {
            List<int> DirList = new List<int>();//获取限定的方向列表

            #region 获取限定方向(方向编码：1-8,1正下，顺时针编码)
            int IADD = sPoint.Item1 - ePoint.Item1;
            int JADD = sPoint.Item2 - ePoint.Item2;

            #region JADD不等于0
            if (IADD != 0)
            {
                double Tan = JADD / IADD;
                double Angle = Math.Atan(Tan);

                #region IADD大于0
                if (IADD > 0)
                {
                    #region JADD大于0
                    if (JADD > 0)
                    {

                        if (Angle < Math.PI / 8)
                        {
                            DirList.Add(6); DirList.Add(7); DirList.Add(8);
                        }

                        else if (Angle < Math.PI / 8 * 3)
                        {
                            DirList.Add(5); DirList.Add(6); DirList.Add(7);
                        }

                        else
                        {
                            DirList.Add(4); DirList.Add(5); DirList.Add(6);
                        }
                    }
                    #endregion

                    #region JADD小于0
                    else
                    {
                        if (Math.Abs(Angle) < Math.PI / 8)
                        {
                            DirList.Add(6); DirList.Add(7); DirList.Add(8);
                        }

                        else if (Math.Abs(Angle) < Math.PI / 8 * 3)
                        {
                            DirList.Add(7); DirList.Add(8); DirList.Add(1);
                        }

                        else
                        {
                            DirList.Add(2); DirList.Add(1); DirList.Add(8);
                        }
                    }
                    #endregion
                }
                #endregion

                #region IDD小于0
                else
                {
                    #region JADD大于0
                    if (JADD > 0)
                    {
                        if (Math.Abs(Angle) < Math.PI / 8)
                        {
                            DirList.Add(4); DirList.Add(3); DirList.Add(2);
                        }

                        else if (Math.Abs(Angle) < Math.PI / 8 * 3)
                        {
                            DirList.Add(5); DirList.Add(4); DirList.Add(3);
                        }

                        else
                        {
                            DirList.Add(4); DirList.Add(5); DirList.Add(6);
                        }
                    }
                    #endregion

                    #region JADD小于0
                    else
                    {
                        if (Angle < Math.PI / 8)
                        {
                            DirList.Add(4); DirList.Add(3); DirList.Add(2);
                        }

                        else if (Angle < Math.PI / 8 * 3)
                        {
                            DirList.Add(3); DirList.Add(2); DirList.Add(1);
                        }

                        else
                        {
                            DirList.Add(2); DirList.Add(1); DirList.Add(8);
                        }
                    }
                    #endregion
                }
                #endregion
            
            }
            #endregion

            #region IADD==0
            else
            {
                if (JADD > 0)
                {
                    DirList.Add(6); DirList.Add(7); DirList.Add(8);
                }

                else
                {
                    DirList.Add(4); DirList.Add(3); DirList.Add(2);
                }
            }
            #endregion

            #endregion
            //DirList.Sort();
            return DirList;
        }

        /// <summary>
        /// 将List<int>转换成一个唯一标识的数字
        /// </summary>
        /// <param name="DirList"></param>
        /// <returns></returns>
        public int GetNumber(List<int> DirList)
        {
            int OutNumber = 0;

            for (int i = 0; i < DirList.Count; i++)
            {
                OutNumber = OutNumber + DirList[i] * (int)Math.Pow(10, i);
            }

            return OutNumber;
        }

        /// <summary>
        /// 判断新生成的路径是否相交
        /// </summary>
        /// <param name="CachePath">给定路径</param>
        /// <param name="PathGrids">已生成的路径Grids</param>
        /// <returns>=true表示相交；=false表示不相交</returns>
        public bool IntersectPath(List<Tuple<int, int>> CacheShortPath, List<Tuple<int, int>> PathGrids)
        {
            int IntersectCount = 0;

            foreach (Tuple<int, int> TaGrid in CacheShortPath)
            {
                if (this.GridContain(TaGrid,PathGrids))
                {
                    IntersectCount++;
                }
            }

            if (IntersectCount >= 2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断新生成的路径是否相交
        /// </summary>
        /// <param name="CachePath">给定路径</param>
        /// <param name="PathGrids">已生成的路径Grids</param>
        /// <returns>=true表示相交；=false表示不相交</returns>
        public int IntersectPathInt(List<Tuple<int, int>> CacheShortPath, List<Tuple<int, int>> PathGrids)
        {
            int IntersectCount = 0;
            bool IntersectLabel = false;

             if (this.GridContain(CacheShortPath[0], PathGrids))
             {
                  IntersectCount++;
             }

             for (int i = 1; i < CacheShortPath.Count; i++)
             {
                 if (this.GridContain(CacheShortPath[i], PathGrids))
                 {
                     IntersectCount++;
                 }

                 if (this.GridContain(CacheShortPath[i - 1], PathGrids) && this.GridContain(CacheShortPath[i], PathGrids))
                 {
                     return 1;
                 }

                 Tuple<int, int> CacheGrid1 = new Tuple<int, int>(CacheShortPath[i - 1].Item1, CacheShortPath[i].Item2);
                 Tuple<int, int> CacheGrid2 = new Tuple<int, int>(CacheShortPath[i].Item1, CacheShortPath[i-1].Item2);
                 if (this.GridContain(CacheGrid1, PathGrids) && this.GridContain(CacheGrid2, PathGrids))
                 {
                     IntersectLabel = true;
                 }

             }         

            if (IntersectCount >= 2||IntersectLabel)
            {
                return 2;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// 计算给定DesGrid到PathGrids的直线距离（不考虑搜索路径）
        /// </summary>
        /// <param name="DesGrid"></param>
        /// <param name="PathGrids"></param>
        /// <returns></returns>
        public double GetMinDis(Tuple<int, int> DesGrid, List<Tuple<int, int>> PathGrids)
        {
            double MinDis = 1000000;

            foreach (Tuple<int, int> Grid in PathGrids)
            {
                double IADD = Math.Abs(Grid.Item1 - DesGrid.Item1);
                double JADD = Math.Abs(Grid.Item2 - DesGrid.Item2);

                double Dis = Math.Sqrt(IADD * IADD + JADD * JADD);
                if (Dis < MinDis)
                {
                    MinDis = Dis;
                }
            }

            return MinDis;
        }

        /// <summary>
        /// 计算给定网格到起点的距离
        /// </summary>
        /// <param name="DesGrid"></param>
        /// <param name="StartOrder"></param>
        /// <returns></returns>
        public Dictionary<Tuple<int,int>,double> GetDisOrder(List<Tuple<int,int>> DesGrids,Tuple<int,int> StartGrid)
        {
            Dictionary<Tuple<int, int>, double> GridDis = new Dictionary<Tuple<int, int>, double>();

            foreach (Tuple<int, int> Grid in DesGrids)
            {
                double IADD = Grid.Item1 - StartGrid.Item1;
                double JADD = Grid.Item2 - StartGrid.Item2;

                double Dis = Math.Sqrt(IADD * IADD + JADD * JADD);
                GridDis.Add(Grid, Dis);
            }

            return GridDis;
        }

        /// <summary>
        /// 判断新生成的路径是否相交
        /// </summary>
        /// <param name="CachePath">给定路径</param>
        /// <param name="PathGrids">已生成的路径Grids</param>
        /// <returns>=true表示相交；=false表示不相交</returns>
        public bool LineIntersectPath(List<Tuple<int, int>> CacheShortPath, List<Tuple<int, int>> PathGrids, Dictionary<Tuple<int, int>, List<double>> Grids)
        {
            object missing = Type.Missing;

            #region CacheShortPath
            IGeometry shp1 = new PolylineClass();
            IPointCollection pointSet1 = shp1 as IPointCollection;
            IPoint curResultPoint1 = new PointClass();
            if (CacheShortPath != null)
            {
                for (int i = 0; i < CacheShortPath.Count; i++)
                {
                    double X = (Grids[CacheShortPath[i]][0] + Grids[CacheShortPath[i]][2]) / 2;
                    double Y = (Grids[CacheShortPath[i]][1] + Grids[CacheShortPath[i]][3]) / 2;

                    curResultPoint1.PutCoords(X, Y);
                    pointSet1.AddPoint(curResultPoint1, ref missing, ref missing);
                }
            }
            #endregion

            #region PathGrids
            IGeometry shp2= new PolylineClass();
            IPointCollection pointSet2 = shp2 as IPointCollection;
            IPoint curResultPoint2 = new PointClass();
            if (PathGrids != null)
            {
                for (int i = 0; i < PathGrids.Count; i++)
                {
                    double X = (Grids[PathGrids[i]][0] + Grids[PathGrids[i]][2]) / 2;
                    double Y = (Grids[PathGrids[i]][1] + Grids[PathGrids[i]][3]) / 2;

                    curResultPoint2.PutCoords(X, Y);
                    pointSet2.AddPoint(curResultPoint2, ref missing, ref missing);
                }
            }
            #endregion

            #region 判断过程
            ITopologicalOperator iTop = shp2 as ITopologicalOperator;
            IGeometry IGeo1 = iTop.Intersect(shp1 as IGeometry,esriGeometryDimension.esriGeometry0Dimension);
            IGeometry IGeo2 = iTop.Intersect(shp1 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);

            if (IGeo1 != null)
            {
                IPointCollection IPC = IGeo1 as IPointCollection;
                if (IPC.PointCount >= 2)
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }

            if (IGeo2 != null)
            {
                IPointCollection IPC = IGeo2 as IPointCollection;
                if (IPC.PointCount >= 2)
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }
            #endregion

            return false;
        }

        /// <summary>
        /// 判断生成路径是否与Features相交
        /// </summary>
        /// <param name="CacheShortPath"></param>
        /// <param name="Features"></param>
        /// <returns></returns>
        public bool obstacleIntersectPath(List<Tuple<int, int>> CacheShortPath, List<Tuple<IGeometry, esriGeometryType>> Features, Dictionary<Tuple<int, int>, List<double>> Grids)
        {
            bool obstacleIntersect = false;
           
            #region CacheShortPath
            object missing = Type.Missing;
            IGeometry shp1 = new PolylineClass();
            IPointCollection pointSet1 = shp1 as IPointCollection;
            IPoint curResultPoint1 = new PointClass();
            if (CacheShortPath != null)
            {
                for (int i = 0; i < CacheShortPath.Count; i++)
                {
                    double X = (Grids[CacheShortPath[i]][0] + Grids[CacheShortPath[i]][2]) / 2;
                    double Y = (Grids[CacheShortPath[i]][1] + Grids[CacheShortPath[i]][3]) / 2;

                    curResultPoint1.PutCoords(X, Y);
                    pointSet1.AddPoint(curResultPoint1, ref missing, ref missing);
                }
            }
            #endregion

            #region 判断相交（点目标无需判断）
            foreach(Tuple<IGeometry,esriGeometryType> Feature in Features)
            {
                #region 线状目标
                if (Feature.Item2 == esriGeometryType.esriGeometryPolyline)
                {
                    ITopologicalOperator iTo = Feature.Item1 as ITopologicalOperator;
                    IGeometry IGeo = iTo.Intersect(shp1, esriGeometryDimension.esriGeometry0Dimension);
                    if (!IGeo.IsEmpty)
                    {
                        return true;
                    }
                }
                #endregion

                #region 面状目标
                if (Feature.Item2 == esriGeometryType.esriGeometryPolygon)
                {
                    ITopologicalOperator iTo = Feature.Item1 as ITopologicalOperator;
                    IGeometry IGeo = iTo.Intersect(shp1, esriGeometryDimension.esriGeometry1Dimension);
                    if (!IGeo.IsEmpty)
                    {
                        return true;
                    }
                }
                #endregion
            }
            #endregion

            return obstacleIntersect;
        }

        /// <summary>
        /// 判断是否包含某一个Grid
        /// </summary>
        /// <param name="Grid"></param>
        /// <param name="PathGrids"></param>
        /// <returns></returns>
        public bool GridContain(Tuple<int, int> Grid, List<Tuple<int, int>> PathGrids)
        {
            foreach (Tuple<int, int> CacheGrid in PathGrids)
            {
                if (CacheGrid.Item1 == Grid.Item1 &&
                    CacheGrid.Item2 == Grid.Item2)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
