using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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

namespace PrDispalce.FlowMap
{
    class cFlowMap
    {
        public Tuple<int, int> startGrid=null;//起点网格
        public List<Tuple<int, int>> desGrids=new List<Tuple<int,int>>();//终点网格

        public IPoint startPoint=null;//起点
        public List<IPoint> desPoints=new List<IPoint>();//终点

        public Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();//终点对应的流量(若流量为0则说明该点为起点)

        public List<Tuple<int, int>> PathGrids=new List<Tuple<int,int>>();//标识为路径的网格
        public List<Path> Paths=new List<Path>();//标识为路径的网格中每一个点到起点的最短路径
        public Dictionary<Tuple<int, int>, Path> GridForPaths=new Dictionary<Tuple<int,int>,Path>();//所有GridNode到起点的路径
        public Dictionary<int, Path> OrderPaths = new Dictionary<int, Path>();//按照顺序添加的路径
        
        public List<Path> SubPaths=new List<Path>();//每一段子Paths

        /// <summary>
        /// 构造函数1 
        /// </summary>
        public cFlowMap()
        {

        }

        /// <summary>
        /// 构造函数2 
        /// </summary>
        public cFlowMap(Tuple<int,int> sGrid,List<Tuple<int,int>> dGrids)
        {
            this.startGrid = sGrid;
            this.desGrids = dGrids;

            if (!PathGrids.Contains(sGrid))
            {
                PathGrids.Add(sGrid);
            }

            //添加起点与起点的路径
            List<Tuple<int, int>> startPath = new List<Tuple<int, int>>(); 
            startPath.Add(sGrid);
            Path wstartPath = new Path(sGrid, sGrid, startPath);

            if (!GridForPaths.Keys.Contains(sGrid))
            {
                Paths.Add(wstartPath);
                GridForPaths.Add(sGrid, wstartPath);

                SubPaths.Add(wstartPath);
            }

            //OrderPaths.Add(0, startPath);//添加起点为路径
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
        /// 构造函数3(添加了一个单独以起点作为路径的路径)（同时，PathGrids中初始存在起点）
        /// </summary>
        public cFlowMap(Tuple<int, int> sGrid, List<Tuple<int, int>> dGrids, IPoint sPoint, List<IPoint> dPoints, Dictionary<IPoint, double> PointFlow)
        {
            this.startGrid = sGrid;
            this.desGrids = dGrids;
            this.startPoint = sPoint;
            this.desPoints = dPoints;
            this.PointFlow = PointFlow;

            //添加起点与起点的路径
            if (!PathGrids.Contains(sGrid))
            {
                PathGrids.Add(sGrid);
            }
            List<Tuple<int, int>> startPath = new List<Tuple<int, int>>(); startPath.Add(sGrid);
            Path wstartPath = new Path(sGrid, sGrid, startPath);
            if (!GridForPaths.Keys.Contains(sGrid))
            {
                Paths.Add(wstartPath);
                GridForPaths.Add(sGrid, wstartPath);

                SubPaths.Add(wstartPath);
            }

            //OrderPaths.Add(0, startPath);//添加起点为路径
        }

        /// <summary>
        /// 构造函数3(添加了一个单独以起点作为路径的路径)（同时，PathGrids中初始存在起点）
        /// </summary>
        public cFlowMap(Tuple<int, int> sGrid, List<Tuple<int, int>> dGrids, Dictionary<IPoint, double> PointFlow)
        {
            this.startGrid = sGrid;
            this.desGrids = dGrids;
            this.PointFlow = PointFlow;

            //添加起点与起点的路径
            if (!PathGrids.Contains(sGrid))
            {
                PathGrids.Add(sGrid);
            }
            List<Tuple<int, int>> startPath = new List<Tuple<int, int>>();
            startPath.Add(sGrid);
            Path wstartPath = new Path(sGrid, sGrid, startPath);

            if (!GridForPaths.Keys.Contains(sGrid))
            {
                Paths.Add(wstartPath);
                GridForPaths.Add(sGrid, wstartPath);

                SubPaths.Add(wstartPath);
            }

            //OrderPaths.Add(0, startPath);//添加起点为路径
        }

        /// <summary>
        /// 依据给定的Path,更新FlowMap中的路径网格和路径(不考虑流量和子路径的更新)
        /// </summary>
        /// <param name="CachePath"></param>
        /// Type标识的是路径的起点
        /// Type=1 标识起点是FlowMap的起点（origin）
        /// Type=2 标识起点是FlowMap的重点（destination）
        public void PathRefresh(Path CachePath,int Type)
        {
            OrderPaths.Add(OrderPaths.Keys.Count, CachePath);//添加每一条新增加的路径

            foreach (Tuple<int, int> PathNode in CachePath.ePath)
            {             
                if (!PathGrids.Contains(PathNode))
                {
                    #region 起点是FlowMap的起点
                    if (Type == 1)
                    {
                        PathGrids.Add(PathNode);//添加标识为路径的格网
                        Path GetPath = new Path(startGrid, PathNode, CachePath.ePath.Take(CachePath.ePath.IndexOf(PathNode) + 1).ToList());
                        GetPath.Length = GetPath.GetPathLength();

                        if (!GridForPaths.Keys.Contains(PathNode))
                        {
                            Paths.Add(GetPath);
                            GridForPaths.Add(PathNode, GetPath);                            
                        }
                    }
                    #endregion

                    #region 终点是FlowMap的终点
                    else if (Type == 2)
                    {
                        PathGrids.Add(PathNode);//添加标识为路径的格网
                        Path GetPath = new Path(startGrid, PathNode, CachePath.ePath.GetRange(CachePath.ePath.IndexOf(PathNode), CachePath.ePath.Count - CachePath.ePath.IndexOf(PathNode)));
                        GetPath.Length = GetPath.GetPathLength();

                        if (!GridForPaths.Keys.Contains(PathNode))
                        {
                            Paths.Add(GetPath);
                            GridForPaths.Add(PathNode, GetPath);
                        }
                    }
                    #endregion
                }

                //else
                //{
                //    tCachePath.Remove(PathNode);
                //}
            }

            //OrderPaths.Add(OrderPaths.Keys.Count, tCachePath);
        }

        /// <summary>
        /// 依据给定的Path,更新FlowMap中的路径网格和路径(考虑流量和子路径的更新)
        /// </summary>
        /// <param name="CachePath"></param>
        /// Type标识的是路径的起点
        /// Type=1 标识起点是FlowMap的起点（origin）
        /// Type=2 标识起点是FlowMap的重点（destination）
        public void PathRefresh(Path CachePath, int Type,double Volume)
        {
            OrderPaths.Add(OrderPaths.Keys.Count, CachePath);//添加每一条新增加的路径

            #region 更新子路径
            #region 获取交叉点
            int IntersectLocation = 0; Tuple<int, int> TargetIntersect = null;
            for (int i = 0; i < CachePath.ePath.Count; i++)
            {
                if (!PathGrids.Contains(CachePath.ePath[i]))
                {
                    IntersectLocation = i - 1;
                    break;
                }
            }

            if (IntersectLocation <= 0)//考虑到第一条路径更新时可能存在该情况
            {
                IntersectLocation = 0;
            }
            TargetIntersect = CachePath.ePath[IntersectLocation];//目标交叉网格
            #endregion

            #region 更新路径和流量
            foreach (Path CachePa in SubPaths)
            {
                bool StopLabel = false;
                if (CachePa.ePath.Contains(TargetIntersect))
                {
                    int Index = CachePa.ePath.IndexOf(TargetIntersect);//获取交叉点的Index
                    Path pCacheIntersectPath = new Path(TargetIntersect, CachePath.ePath[CachePath.ePath.Count - 1], CachePath.ePath.GetRange(CachePath.ePath.IndexOf(TargetIntersect), CachePath.ePath.Count - CachePath.ePath.IndexOf(TargetIntersect)));

                    #region 连接的路径终点（若连接的是路径的起点，则不更新）
                    if (Index == CachePa.ePath.Count - 1)
                    {
                        CachePath.FlowOutPath.Add(CachePa);//更新CachePath的FlowOutPath
                        CachePa.FlowInPath.Add(pCacheIntersectPath);//更新CachePa的FlowInPath

                        CachePa.Volume = CachePa.Volume + Volume;//更新CachePa的流量
                        pCacheIntersectPath.Volume = Volume;

                        #region 更新CachePa FlowIn关联的Path的流量
                        //List<Path> CacheFlowInList = Clone((object)CachePa.FlowOutPath) as List<Path>;
                        //int Label = 0;//为了避免误删
                        List<Path> CacheFlowInList = new List<Path>();//为了避免改变原始值
                        for (int i = 0; i < CachePa.FlowOutPath.Count; i++)
                        {
                            CacheFlowInList.Add(CachePa.FlowOutPath[i]);
                        }
                        while (CacheFlowInList.Count >0)
                        {
                            CacheFlowInList[0].Volume = CacheFlowInList[0].Volume + Volume;
                            CacheFlowInList.AddRange(CacheFlowInList[0].FlowOutPath);
                            CacheFlowInList.RemoveAt(0);
                            //Label++;
                        }
                        #endregion

                        SubPaths.Add(pCacheIntersectPath);
                        StopLabel = true;
                    }
                    #endregion

                    #region 连接的路径中间点
                    else if (Index > 0)
                    {
                        Path CacheIntersectPath = new Path(TargetIntersect, CachePath.ePath[CachePath.ePath.Count - 1], CachePath.ePath.GetRange(CachePath.ePath.IndexOf(TargetIntersect), CachePath.ePath.Count - CachePath.ePath.IndexOf(TargetIntersect)));
                    
                        //CachePa分两段
                        Path Path1 = new Path(CachePa.ePath[0],TargetIntersect, CachePa.ePath.Take(CachePa.ePath.IndexOf(TargetIntersect) + 1).ToList());//前半段
                        Path Path2 = new Path(TargetIntersect, CachePa.ePath[CachePa.ePath.Count-1], CachePa.ePath.GetRange(CachePa.ePath.IndexOf(TargetIntersect), CachePa.ePath.Count - CachePa.ePath.IndexOf(TargetIntersect)));//后半段
                        
                        CacheIntersectPath.FlowOutPath.Add(Path1);//更新CacheIntersectPath属性
                        CacheIntersectPath.Volume = Volume;

                        #region 更新CachePa关联的FlowOutPath的FlowInPath（更新Path1和Path2的FlowOut）
                        Path2.FlowOutPath.Add(Path1);//Path2的Flowout
                        for (int i = 0; i < CachePa.FlowOutPath.Count; i++)
                        {
                            Path1.FlowOutPath.Add(CachePa.FlowOutPath[i]);
                            CachePa.FlowOutPath[i].FlowInPath.Remove(CachePa);
                            CachePa.FlowOutPath[i].FlowInPath.Add(Path1);
                        }
                        #endregion

                        #region 更新CachePa关联的FlowInPath的FlowOutPath（更新Path1和Path2的FlowIn） 
                        Path1.FlowInPath.Add(Path2);
                        Path1.FlowInPath.Add(CacheIntersectPath);
                        Path2.Volume = CachePa.Volume;
                        //if (CachePa.FlowInPath.Count == 0)//
                        //{
                        //    Path2.Volume = CachePa.Volume;
                        //}
                        
                        for (int i = 0; i < CachePa.FlowInPath.Count; i++)
                        {
                            if (Path1.ePath.Contains(CachePa.FlowInPath[i].ePath[0]))
                            {
                                Path1.FlowInPath.Add(CachePa.FlowInPath[i]);
                                CachePa.FlowInPath[i].FlowOutPath.Remove(CachePa);
                                CachePa.FlowInPath[i].FlowOutPath.Add(Path1);

                                //Path2.Volume = Path2.Volume - CachePa.FlowInPath[i].Volume;//更新Path2的流量
                            }

                            else if(Path2.ePath.Contains(CachePa.FlowInPath[i].ePath[0]))
                            {
                                Path2.FlowInPath.Add(CachePa.FlowInPath[i]);
                                CachePa.FlowInPath[i].FlowOutPath.Remove(CachePa);
                                CachePa.FlowInPath[i].FlowOutPath.Add(Path2);
                            }
                        }
                        #endregion

                        #region 更新流量(备注：Path2的流量在更新Path1和Path的flowin时更新)
                        //CachePath.Volume = Volume;//获取子路径的流量

                        Path1.Volume = CachePa.Volume + Volume;//
                        //更新Path1关联的FlowutPath的流量
                        //List<Path> pCacheFlowInList = Clone((object)Path1.FlowOutPath) as List<Path>;//为了避免误删，需要进行深拷贝
                        List<Path> CacheFlowInList = new List<Path>();//为了避免改变原始值
                        for (int i = 0; i < Path1.FlowOutPath.Count; i++)
                        {
                            CacheFlowInList.Add(Path1.FlowOutPath[i]);
                        }
                        while (CacheFlowInList.Count>0)
                        {
                            CacheFlowInList[0].Volume = CacheFlowInList[0].Volume + Volume;
                            CacheFlowInList.AddRange(CacheFlowInList[0].FlowOutPath);
                            CacheFlowInList.RemoveAt(0);
                        }
                        #endregion

                        SubPaths.Add(Path1);
                        SubPaths.Add(Path2);
                        SubPaths.Add(CacheIntersectPath);
                        SubPaths.Remove(CachePa);

                        StopLabel = true;
                    }
                    #endregion
                }

                if (StopLabel)
                {
                    break;
                }
            }
            #endregion

            #endregion

            #region 更新路径点和路网
            foreach (Tuple<int, int> PathNode in CachePath.ePath)
            {
                if (!PathGrids.Contains(PathNode))
                {
                    #region 起点是FlowMap的起点
                    if (Type == 1)
                    {
                        PathGrids.Add(PathNode);//添加标识为路径的格网
                        Path GetPath = new Path(startGrid, PathNode, CachePath.ePath.Take(CachePath.ePath.IndexOf(PathNode) + 1).ToList());
                        GetPath.Length = GetPath.GetPathLength();

                        if (!GridForPaths.Keys.Contains(PathNode))
                        {
                            Paths.Add(GetPath);
                            GridForPaths.Add(PathNode, GetPath);
                        }
                    }
                    #endregion

                    #region 终点是FlowMap的终点
                    else if (Type == 2)
                    {
                        PathGrids.Add(PathNode);//添加标识为路径的格网
                        Path GetPath = new Path(startGrid, PathNode, CachePath.ePath.GetRange(CachePath.ePath.IndexOf(PathNode), CachePath.ePath.Count - CachePath.ePath.IndexOf(PathNode)));
                        GetPath.Length = GetPath.GetPathLength();

                        if (!GridForPaths.Keys.Contains(PathNode))
                        {
                            Paths.Add(GetPath);
                            GridForPaths.Add(PathNode, GetPath);
                        }
                    }
                    #endregion
                }

                //else
                //{
                //    tCachePath.Remove(PathNode);
                //}
            }   
            #endregion

            //OrderPaths.Add(OrderPaths.Keys.Count, tCachePath);
        }

        /// <summary>
        /// 依据给定的Path,更新FlowMap中的路径网格和路径(考虑流量和子路径的更新)[考虑不同类型]
        /// </summary>
        /// <param name="CachePath"></param>
        /// Type标识的是路径的起点
        /// Type=1 标识起点是FlowMap的起点（origin）
        /// Type=2 标识起点是FlowMap的重点（destination）
        public void PathRefresh(Path CachePath, int Type, double Volume, Dictionary<Tuple<int, int>, int> GridType)
        {
            OrderPaths.Add(OrderPaths.Keys.Count, CachePath);//添加每一条新增加的路径

            #region 更新子路径
            #region 获取交叉点
            int IntersectLocation = 0; Tuple<int, int> TargetIntersect = null;
            for (int i = 0; i < CachePath.ePath.Count; i++)
            {
                if (!PathGrids.Contains(CachePath.ePath[i]))
                {
                    IntersectLocation = i - 1;
                    break;
                }
            }

            if (IntersectLocation <= 0)//考虑到第一条路径更新时可能存在该情况
            {
                IntersectLocation = 0;
            }
            TargetIntersect = CachePath.ePath[IntersectLocation];//目标交叉网格
            #endregion

            #region 更新路径和流量
            foreach (Path CachePa in SubPaths)
            {
                bool StopLabel = false;
                if (CachePa.ePath.Contains(TargetIntersect))
                {
                    int Index = CachePa.ePath.IndexOf(TargetIntersect);//获取交叉点的Index
                    Path pCacheIntersectPath = new Path(TargetIntersect, CachePath.ePath[CachePath.ePath.Count - 1], CachePath.ePath.GetRange(CachePath.ePath.IndexOf(TargetIntersect), CachePath.ePath.Count - CachePath.ePath.IndexOf(TargetIntersect)));

                    #region 连接的路径终点（若连接的是路径的起点，则不更新）
                    if (Index == CachePa.ePath.Count - 1)
                    {
                        CachePath.FlowOutPath.Add(CachePa);//更新CachePath的FlowOutPath
                        CachePa.FlowInPath.Add(pCacheIntersectPath);//更新CachePa的FlowInPath

                        CachePa.Volume = CachePa.Volume + Volume;//更新CachePa的流量
                        pCacheIntersectPath.Volume = Volume;

                        #region 更新CachePa FlowIn关联的Path的流量
                        //List<Path> CacheFlowInList = Clone((object)CachePa.FlowOutPath) as List<Path>;
                        //int Label = 0;//为了避免误删
                        List<Path> CacheFlowInList = new List<Path>();//为了避免改变原始值
                        for (int i = 0; i < CachePa.FlowOutPath.Count; i++)
                        {
                            CacheFlowInList.Add(CachePa.FlowOutPath[i]);
                        }
                        while (CacheFlowInList.Count > 0)
                        {
                            CacheFlowInList[0].Volume = CacheFlowInList[0].Volume + Volume;
                            CacheFlowInList.AddRange(CacheFlowInList[0].FlowOutPath);
                            CacheFlowInList.RemoveAt(0);
                            //Label++;
                        }
                        #endregion

                        SubPaths.Add(pCacheIntersectPath);
                        StopLabel = true;
                    }
                    #endregion

                    #region 连接的路径中间点
                    else if (Index > 0)
                    {
                        Path CacheIntersectPath = new Path(TargetIntersect, CachePath.ePath[CachePath.ePath.Count - 1], CachePath.ePath.GetRange(CachePath.ePath.IndexOf(TargetIntersect), CachePath.ePath.Count - CachePath.ePath.IndexOf(TargetIntersect)));

                        //CachePa分两段
                        Path Path1 = new Path(CachePa.ePath[0], TargetIntersect, CachePa.ePath.Take(CachePa.ePath.IndexOf(TargetIntersect) + 1).ToList());//前半段
                        Path Path2 = new Path(TargetIntersect, CachePa.ePath[CachePa.ePath.Count - 1], CachePa.ePath.GetRange(CachePa.ePath.IndexOf(TargetIntersect), CachePa.ePath.Count - CachePa.ePath.IndexOf(TargetIntersect)));//后半段

                        CacheIntersectPath.FlowOutPath.Add(Path1);//更新CacheIntersectPath属性
                        CacheIntersectPath.Volume = Volume;

                        #region 更新CachePa关联的FlowOutPath的FlowInPath（更新Path1和Path2的FlowOut）
                        Path2.FlowOutPath.Add(Path1);//Path2的Flowout
                        for (int i = 0; i < CachePa.FlowOutPath.Count; i++)
                        {
                            Path1.FlowOutPath.Add(CachePa.FlowOutPath[i]);
                            CachePa.FlowOutPath[i].FlowInPath.Remove(CachePa);
                            CachePa.FlowOutPath[i].FlowInPath.Add(Path1);
                        }
                        #endregion

                        #region 更新CachePa关联的FlowInPath的FlowOutPath（更新Path1和Path2的FlowIn）
                        Path1.FlowInPath.Add(Path2);
                        Path1.FlowInPath.Add(CacheIntersectPath);
                        Path2.Volume = CachePa.Volume;
                        //if (CachePa.FlowInPath.Count == 0)//
                        //{
                        //    Path2.Volume = CachePa.Volume;
                        //}

                        for (int i = 0; i < CachePa.FlowInPath.Count; i++)
                        {
                            if (Path1.ePath.Contains(CachePa.FlowInPath[i].ePath[0]))
                            {
                                Path1.FlowInPath.Add(CachePa.FlowInPath[i]);
                                CachePa.FlowInPath[i].FlowOutPath.Remove(CachePa);
                                CachePa.FlowInPath[i].FlowOutPath.Add(Path1);

                                //Path2.Volume = Path2.Volume - CachePa.FlowInPath[i].Volume;//更新Path2的流量
                            }

                            else if (Path2.ePath.Contains(CachePa.FlowInPath[i].ePath[0]))
                            {
                                Path2.FlowInPath.Add(CachePa.FlowInPath[i]);
                                CachePa.FlowInPath[i].FlowOutPath.Remove(CachePa);
                                CachePa.FlowInPath[i].FlowOutPath.Add(Path2);
                            }
                        }
                        #endregion

                        #region 更新流量(备注：Path2的流量在更新Path1和Path的flowin时更新)
                        //CachePath.Volume = Volume;//获取子路径的流量

                        Path1.Volume = CachePa.Volume + Volume;//
                        //更新Path1关联的FlowutPath的流量
                        //List<Path> pCacheFlowInList = Clone((object)Path1.FlowOutPath) as List<Path>;//为了避免误删，需要进行深拷贝
                        List<Path> CacheFlowInList = new List<Path>();//为了避免改变原始值
                        for (int i = 0; i < Path1.FlowOutPath.Count; i++)
                        {
                            CacheFlowInList.Add(Path1.FlowOutPath[i]);
                        }
                        while (CacheFlowInList.Count > 0)
                        {
                            CacheFlowInList[0].Volume = CacheFlowInList[0].Volume + Volume;
                            CacheFlowInList.AddRange(CacheFlowInList[0].FlowOutPath);
                            CacheFlowInList.RemoveAt(0);
                        }
                        #endregion

                        SubPaths.Add(Path1);
                        SubPaths.Add(Path2);
                        SubPaths.Add(CacheIntersectPath);
                        SubPaths.Remove(CachePa);

                        StopLabel = true;
                    }
                    #endregion
                }

                if (StopLabel)
                {
                    break;
                }
            }
            #endregion

            #endregion

            #region 更新路径点和路网
            foreach (Tuple<int, int> PathNode in CachePath.ePath)
            {
                if (!PathGrids.Contains(PathNode))
                {
                    #region 起点是FlowMap的起点
                    if (Type == 1)
                    {
                        PathGrids.Add(PathNode);//添加标识为路径的格网
                        Path GetPath = new Path(startGrid, PathNode, CachePath.ePath.Take(CachePath.ePath.IndexOf(PathNode) + 1).ToList());

                        //考虑数据类型
                        GetPath.Length = GetPath.GetPathLengthType(GridType);

                        if (!GridForPaths.Keys.Contains(PathNode))
                        {
                            Paths.Add(GetPath);
                            GridForPaths.Add(PathNode, GetPath);
                        }
                    }
                    #endregion

                    #region 终点是FlowMap的终点
                    else if (Type == 2)
                    {
                        PathGrids.Add(PathNode);//添加标识为路径的格网
                        Path GetPath = new Path(startGrid, PathNode, CachePath.ePath.GetRange(CachePath.ePath.IndexOf(PathNode), CachePath.ePath.Count - CachePath.ePath.IndexOf(PathNode)));

                        //考虑数据类型
                        GetPath.Length = GetPath.GetPathLengthType(GridType);

                        if (!GridForPaths.Keys.Contains(PathNode))
                        {
                            Paths.Add(GetPath);
                            GridForPaths.Add(PathNode, GetPath);
                        }
                    }
                    #endregion
                }

                //else
                //{
                //    tCachePath.Remove(PathNode);
                //}
            }
            #endregion

            //OrderPaths.Add(OrderPaths.Keys.Count, tCachePath);
        }
    }
}
