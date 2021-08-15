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
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.Geoprocessing;

using AuxStructureLib;
using AuxStructureLib.IO;

namespace PrDispalce.FlowMap
{
    /// <summary>
    /// Flow的绘制
    /// </summary>
    class FlowDraw
    {
        #region 参数
        AxMapControl pMapControl;//绘制控件
        Dictionary<Tuple<int, int>, List<double>> Grids = new Dictionary<Tuple<int, int>, List<double>>();//控件格网划分
        List<IPoint> AllPoints = new List<IPoint>();//ODPoints
        PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
        Dictionary<IPoint, Tuple<int, int>> NodeInGrid=new Dictionary<IPoint,Tuple<int,int>>() ;//获取点对应的格网
        string OutPath;
        Dictionary<Tuple<int, int>, IPoint> GridWithNode=new Dictionary<Tuple<int,int>,IPoint>();//获取格网中的点（每个格网最多对应一个点）
        #endregion

        #region 构造函数
        public FlowDraw()
        {
        }

        public FlowDraw(AxMapControl pMapControl, Dictionary<Tuple<int, int>, List<double>> Grids)
        {
            this.pMapControl = pMapControl;
            this.Grids = Grids;
        }

        public FlowDraw(AxMapControl pMapControl, Dictionary<Tuple<int, int>, List<double>> Grids, List<IPoint> AllPoints,Dictionary<IPoint, Tuple<int, int>> NodeInGrid, Dictionary<Tuple<int, int>, IPoint> GridWithNode)
        {
            this.pMapControl = pMapControl;
            this.Grids = Grids;
            this.AllPoints = AllPoints;
            this.NodeInGrid=NodeInGrid;
            this.GridWithNode=GridWithNode;
        }

        public FlowDraw(AxMapControl pMapControl, Dictionary<Tuple<int, int>, List<double>> Grids, List<IPoint> AllPoints, Dictionary<IPoint, Tuple<int, int>> NodeInGrid, Dictionary<Tuple<int, int>, IPoint> GridWithNode,string OutPath)
        {
            this.pMapControl = pMapControl;
            this.Grids = Grids;
            this.AllPoints = AllPoints;
            this.NodeInGrid = NodeInGrid;
            this.GridWithNode = GridWithNode;
            this.OutPath=OutPath;
        }
        #endregion

        /// <summary>
        /// Darw a Path
        /// </summary>
        /// <param name="CachePath">FlowPath</param>
        /// <param name="Width">给定宽度</param>
        /// <param name="Type">ODType=1不考虑ODPoints绘制；ODType=1，考虑ODPoints绘制</param>
        /// OutType是否输出 =0；不输出 =1输出
        public void FlowPathDraw(Path CachePath, double Width, int ODType, int OutType, out PolylineObject CachePoLine)
        {           
            object cPolylineSb = Sb.LineSymbolization(Width, 100, 100, 100, 0);

            #region CachePath共线
            if (this.OnLine(CachePath))
            {
                List<TriNode> NodeList = new List<TriNode>();

                IPoint sPoint = new PointClass();
                IPoint ePoint = new PointClass();

                #region 不考虑ODPoints
                sPoint.X = (Grids[CachePath.ePath[0]][0] + Grids[CachePath.ePath[0]][2]) / 2;
                sPoint.Y = (Grids[CachePath.ePath[0]][1] + Grids[CachePath.ePath[0]][3]) / 2;

                ePoint.X = (Grids[CachePath.ePath[CachePath.ePath.Count - 1]][0] + Grids[CachePath.ePath[CachePath.ePath.Count - 1]][2]) / 2;
                ePoint.Y = (Grids[CachePath.ePath[CachePath.ePath.Count - 1]][1] + Grids[CachePath.ePath[CachePath.ePath.Count - 1]][3]) / 2;
                #endregion

                #region 考虑ODPoints
                if (ODType == 1)
                {
                    if (GridWithNode.Keys.Contains(CachePath.ePath[0]))
                    {
                        sPoint.X = GridWithNode[CachePath.ePath[0]].X;
                        sPoint.Y = GridWithNode[CachePath.ePath[0]].Y; ;
                    }

                    if (GridWithNode.Keys.Contains(CachePath.ePath[CachePath.ePath.Count - 1]))
                    {
                        ePoint.X = GridWithNode[CachePath.ePath[CachePath.ePath.Count - 1]].X;
                        ePoint.Y = GridWithNode[CachePath.ePath[CachePath.ePath.Count - 1]].Y;
                    }
                }
                #endregion

                #region 如果需要输出
                if (OutType == 1)
                {
                    TriNode CacheNode1 = new TriNode();
                    CacheNode1.X = sPoint.X;
                    CacheNode1.Y = sPoint.Y;
                    NodeList.Add(CacheNode1);


                    TriNode CacheNode2 = new TriNode();
                    CacheNode2.X = ePoint.X;
                    CacheNode2.Y = ePoint.Y;
                    NodeList.Add(CacheNode2);
                }           
                #endregion   
                
                IPolyline iLine = new PolylineClass();
                iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;
                //pMapControl.DrawShape(iLine, ref cPolylineSb);
                CachePoLine = new PolylineObject(NodeList);
            }
            #endregion

            #region CachePath不是一条直线
            else
            {
                List<TriNode> NodeList = new List<TriNode>();
                for (int i = 0; i < CachePath.ePath.Count - 1; i++)
                {
                    IPoint sPoint = new PointClass();
                    IPoint ePoint = new PointClass();

                    #region 不考虑ODPoints
                    sPoint.X = (Grids[CachePath.ePath[i]][0] + Grids[CachePath.ePath[i]][2]) / 2;
                    sPoint.Y = (Grids[CachePath.ePath[i]][1] + Grids[CachePath.ePath[i]][3]) / 2;

                    ePoint.X = (Grids[CachePath.ePath[i + 1]][0] + Grids[CachePath.ePath[i + 1]][2]) / 2;
                    ePoint.Y = (Grids[CachePath.ePath[i + 1]][1] + Grids[CachePath.ePath[i + 1]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(CachePath.ePath[i]))
                        {
                            sPoint.X = GridWithNode[CachePath.ePath[i]].X;
                            sPoint.Y = GridWithNode[CachePath.ePath[i]].Y; ;
                        }

                        if (GridWithNode.Keys.Contains(CachePath.ePath[i + 1]))
                        {
                            ePoint.X = GridWithNode[CachePath.ePath[i + 1]].X;
                            ePoint.Y = GridWithNode[CachePath.ePath[i + 1]].Y;
                        }
                    }
                    #endregion

                    #region 如果需要输出
                    if (OutType == 1)
                    {
                        TriNode CacheNode1 = new TriNode();
                        CacheNode1.X = sPoint.X;
                        CacheNode1.Y = sPoint.Y;
                        NodeList.Add(CacheNode1);

                        if (i == CachePath.ePath.Count - 2)
                        {
                            TriNode CacheNode2 = new TriNode();
                            CacheNode2.X = ePoint.X;
                            CacheNode2.Y = ePoint.Y;
                            NodeList.Add(CacheNode2);
                        }
                    }
                    #endregion

                    IPolyline iLine = new PolylineClass();
                    iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;

                    //pMapControl.DrawShape(iLine, ref cPolylineSb);
                }

                CachePoLine = new PolylineObject(NodeList);
            }
            #endregion

            CachePoLine.Volume = CachePath.Volume;
        }

        /// <summary>
        /// Darw a Path
        /// </summary>
        /// <param name="CachePath">FlowPath</param>
        /// <param name="Width">给定宽度</param>
        /// <param name="Type">ODType=1不考虑ODPoints绘制；ODType=1，考虑ODPoints绘制</param>
        /// OutType是否输出 =0；不输出 =1输出
        public void FlowPathDraw(Path CachePath, double Width, int ODType)
        {
            if (CachePath != null)
            {
                object cPolylineSb = Sb.LineSymbolization(Width, 100, 100, 100, 0);

                List<TriNode> NodeList = new List<TriNode>();
                for (int i = 0; i < CachePath.ePath.Count - 1; i++)
                {
                    IPoint sPoint = new PointClass();
                    IPoint ePoint = new PointClass();

                    #region 不考虑ODPoints
                    sPoint.X = (Grids[CachePath.ePath[i]][0] + Grids[CachePath.ePath[i]][2]) / 2;
                    sPoint.Y = (Grids[CachePath.ePath[i]][1] + Grids[CachePath.ePath[i]][3]) / 2;

                    ePoint.X = (Grids[CachePath.ePath[i + 1]][0] + Grids[CachePath.ePath[i + 1]][2]) / 2;
                    ePoint.Y = (Grids[CachePath.ePath[i + 1]][1] + Grids[CachePath.ePath[i + 1]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(CachePath.ePath[i]))
                        {
                            sPoint.X = GridWithNode[CachePath.ePath[i]].X;
                            sPoint.Y = GridWithNode[CachePath.ePath[i]].Y; ;
                        }

                        if (GridWithNode.Keys.Contains(CachePath.ePath[i + 1]))
                        {
                            ePoint.X = GridWithNode[CachePath.ePath[i + 1]].X;
                            ePoint.Y = GridWithNode[CachePath.ePath[i + 1]].Y;
                        }
                    }
                    #endregion

                    IPolyline iLine = new PolylineClass();
                    iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;

                    pMapControl.DrawShape(iLine, ref cPolylineSb);
                }
            }
        }

        /// <summary>
        /// DrawAFlowMap(按给定宽度绘制)
        /// </summary>
        /// <param name="SubPaths">FlowMap</param>
        /// <param name="Width">指定宽度</param>
        /// <param name="ODType">是否考虑ODPoint Type=0不考虑；Type=1考虑</param>
        /// OutType是否输出: =0；不输出 =1输出
        public void FlowMapDraw(List<Path> SubPaths,double Width,int ODType,int OutType)
        {
            SMap OutMap = new SMap();

            foreach (Path Pa in SubPaths)
            {
                PolylineObject CacheLine;
                this.FlowPathDraw(Pa, Width, ODType,OutType,out CacheLine);
                CacheLine.Volume = Pa.Volume;

                #region 需要输出
                if (OutType == 1)
                {
                    OutMap.PolylineList.Add(CacheLine);
                }
                #endregion
            }

            #region 需要输出
            if (OutType == 1 && OutPath != null)
            {
                OutMap.WriteResult2Shp(OutPath, pMapControl.Map.SpatialReference);
            }
            #endregion
        }

        /// <summary>
        /// 宽度按照给定参数变化绘制FlowMap
        /// </summary>
        /// <param name="SubPaths">FlowMap</param>
        /// <param name="MaxWidth">最大宽度</param>
        /// <param name="MaxVolume">最大流量</param>
        /// <param name="MinVolume">最小流量</param>
        /// <param name="Type">Type=0宽度线性变化；=1宽度三角函数变化</param>
        /// <param name="ODType">是否考虑ODPoint Type=0不考虑；Type=1考虑</param>
        /// OutType是否输出 =0；不输出 =1输出
        public void FlowMapDraw(List<Path> SubPaths, double MaxWidth, double MaxVolume, double MinVolume, int Type,int ODType,int OutType)
        {
            SMap OutMap = new SMap();

            foreach (Path Pa in SubPaths)
            {
                double CacheWidth = 0;

                #region 宽度线性变化
                if (Type == 1)
                {
                    CacheWidth = (Pa.Volume - MinVolume) / (MaxVolume - MinVolume) * (MaxWidth - 0.2) + 0.2;
                }
                #endregion

                #region 宽度三角函数变化
                else if (CacheWidth == 1)
                {
                    CacheWidth = Math.Sin((Pa.Volume - MinVolume) / (MaxVolume - MinVolume) * Math.PI / 2) * (MaxWidth - 0.2) + 0.2;
                }
                #endregion

                PolylineObject CacheLine;
                this.FlowPathDraw(Pa, CacheWidth, ODType,OutType,out CacheLine);

                #region 需要输出
                if (OutType == 1)
                {
                    OutMap.PolylineList.Add(CacheLine);
                }
                #endregion
            }

            #region 需要输出
            if (OutType == 1 && OutPath != null)
            {
                OutMap.WriteResult2Shp(OutPath, pMapControl.Map.SpatialReference,0);
            }
            #endregion
        }

        /// <summary>
        /// 宽度按照给定参数变化绘制SmoothFlowMap
        /// </summary>
        /// <param name="SubPaths">FlowMap</param>
        /// <param name="MaxWidth">最大宽度</param>
        /// <param name="MaxVolume">最大流量</param>
        /// <param name="MinVolume">最小流量</param>
        /// <param name="Type">Type=0宽度线性变化；=1宽度三角函数变化</param>
        /// <param name="ODType">是否考虑ODPoint Type=0不考虑；Type=1考虑</param>
        /// OutType是否输出 =0；不输出 =1输出
        /// ///BeType =0 考虑直线绘制；=1不考虑直线绘制
        public void SmoothFlowMap(List<Path> SubPaths, double MaxWidth, double MaxVolume, double MinVolume, int Type,int ODType, int OutType,int BeType,int InsertPoint)
        {
            BuildingSim.PublicUtil PU = new BuildingSim.PublicUtil();
            SMap OutMap = new SMap();

            foreach (Path Pa in SubPaths)
            {
                double CacheWidth = 0;

                #region 宽度线性变化
                if (Type == 1)
                {
                    CacheWidth = (Pa.Volume - MinVolume) / (MaxVolume - MinVolume) * (MaxWidth - 0.2) + 0.2;
                }
                #endregion

                #region 宽度三角函数变化
                else if (CacheWidth == 1)
                {
                    CacheWidth = Math.Sin((Pa.Volume - MinVolume) / (MaxVolume - MinVolume) * Math.PI / 2) * (MaxWidth - 0.2) + 0.2;
                }
                #endregion

                object PolylineSb = Sb.LineSymbolization(CacheWidth, 100, 100, 100, 0);

                #region 贝塞尔曲线绘制
                List<IPoint> ControlPoints = new List<IPoint>();

                #region 如果是直线
                if (this.OnLine(Pa))
                {
                    #region 不考虑ODPoints
                    IPoint sPoint = new PointClass();
                    sPoint.X = (Grids[Pa.ePath[0]][0] + Grids[Pa.ePath[0]][2]) / 2;
                    sPoint.Y = (Grids[Pa.ePath[0]][1] + Grids[Pa.ePath[0]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (ODType == 1)
                    {
                        if (GridWithNode.Keys.Contains(Pa.ePath[0]))
                        {
                            sPoint.X = GridWithNode[Pa.ePath[0]].X;
                            sPoint.Y = GridWithNode[Pa.ePath[0]].Y; ;
                        }
                    }
                    #endregion

                    ControlPoints.Add(sPoint);


                    #region 不考虑ODPoints
                    IPoint ePoint = new PointClass();
                    ePoint.X = (Grids[Pa.ePath[Pa.ePath.Count-1]][0] + Grids[Pa.ePath[Pa.ePath.Count-1]][2]) / 2;
                    ePoint.Y = (Grids[Pa.ePath[Pa.ePath.Count-1]][1] + Grids[Pa.ePath[Pa.ePath.Count-1]][3]) / 2;
                    #endregion

                    #region 考虑ODPoints
                    if (GridWithNode.Keys.Contains(Pa.ePath[Pa.ePath.Count-1]))
                    {
                        ePoint.X = GridWithNode[Pa.ePath[Pa.ePath.Count-1]].X;
                        ePoint.Y = GridWithNode[Pa.ePath[Pa.ePath.Count-1]].Y;
                    }
                    ControlPoints.Add(ePoint);
                    #endregion
                }
                #endregion

                #region 如果是非直线
                else
                {
                    for (int j = 0; j < Pa.ePath.Count - 1; j++)
                    {
                        #region 不考虑ODPoints
                        IPoint sPoint = new PointClass();
                        sPoint.X = (Grids[Pa.ePath[j]][0] + Grids[Pa.ePath[j]][2]) / 2;
                        sPoint.Y = (Grids[Pa.ePath[j]][1] + Grids[Pa.ePath[j]][3]) / 2;
                        #endregion

                        #region 考虑ODPoints
                        if (ODType == 1)
                        {
                            if (GridWithNode.Keys.Contains(Pa.ePath[j]))
                            {
                                sPoint.X = GridWithNode[Pa.ePath[j]].X;
                                sPoint.Y = GridWithNode[Pa.ePath[j]].Y; ;
                            }
                        }
                        #endregion

                        ControlPoints.Add(sPoint);

                        if (j == Pa.ePath.Count - 2)
                        {
                            #region 不考虑ODPoints
                            IPoint ePoint = new PointClass();
                            ePoint.X = (Grids[Pa.ePath[j + 1]][0] + Grids[Pa.ePath[j + 1]][2]) / 2;
                            ePoint.Y = (Grids[Pa.ePath[j + 1]][1] + Grids[Pa.ePath[j + 1]][3]) / 2;
                            #endregion

                            #region 考虑ODPoints
                            if (GridWithNode.Keys.Contains(Pa.ePath[j + 1]))
                            {
                                ePoint.X = GridWithNode[Pa.ePath[j + 1]].X;
                                ePoint.Y = GridWithNode[Pa.ePath[j + 1]].Y;
                            }
                            ControlPoints.Add(ePoint);
                            #endregion
                        }
                    }
                }
                #endregion

                if (ControlPoints.Count > 0)
                {
                    BezierCurve BC = new BezierCurve(ControlPoints);

                    if (BeType == 0)
                    {
                        #region 若FlowInPath=0，且FlowOutPath不是直线
                        bool OnLineLabel = false;
                        for (int i = 0; i < Pa.FlowOutPath.Count; i++)
                        {
                            if (Pa.FlowOutPath[i].ePath.Count > 1)
                            {
                                IPoint CachePoint = new PointClass();

                                CachePoint.X = (Grids[Pa.FlowOutPath[i].ePath[Pa.FlowOutPath[i].ePath.Count - 2]][0] + Grids[Pa.FlowOutPath[i].ePath[Pa.FlowOutPath[i].ePath.Count - 2]][2]) / 2;
                                CachePoint.Y = (Grids[Pa.FlowOutPath[i].ePath[Pa.FlowOutPath[i].ePath.Count - 2]][1] + Grids[Pa.FlowOutPath[i].ePath[Pa.FlowOutPath[i].ePath.Count - 2]][3]) / 2;

                                double Angle = PU.GetAngle(ControlPoints[0], CachePoint, ControlPoints[1]);

                                if (Math.Abs(Angle - Math.PI) < 0.001)
                                {
                                    OnLineLabel = true;
                                }
                            }
                        }
                        #endregion

                        #region 贝塞尔曲线生成
                        if (Pa.FlowInPath.Count == 0 && !OnLineLabel)
                        {

                            BC.CurveNGenerate(InsertPoint, 0.1);
                        }

                        else
                        {
                            BC.CurveNGenerate(InsertPoint);
                        }
                        #endregion
                    }

                    else if (BeType == 1)
                    {
                        BC.CurveNGenerate(InsertPoint);
                    }
                     
                    #region 贝塞尔曲线绘制
                    List<TriNode> LinePoints = new List<TriNode>();//输出用
                    for (int i = 0; i < BC.CurvePoint.Count - 1; i++)
                    {
                        IPolyline iLine = new PolylineClass();
                        iLine.FromPoint = BC.CurvePoint[i];
                        iLine.ToPoint = BC.CurvePoint[i + 1];

                        #region 输出需要
                        if (OutType == 1)
                        {
                            TriNode pNode = new TriNode(BC.CurvePoint[i].X, BC.CurvePoint[i].Y);//输出用
                            LinePoints.Add(pNode);

                            if (i == BC.CurvePoint.Count - 2)//输出用
                            {
                                TriNode nNode = new TriNode(BC.CurvePoint[i + 1].X, BC.CurvePoint[i + 1].Y);
                                LinePoints.Add(nNode);
                            }
                        }
                        #endregion

                        pMapControl.DrawShape(iLine, ref PolylineSb);
                    }

                    #region 需要输出
                    if (OutType == 1)
                    {
                        PolylineObject CacheLine = new PolylineObject(LinePoints);
                        CacheLine.Volume = Pa.Volume;
                        OutMap.PolylineList.Add(CacheLine);
                    }
                    #endregion

                    #endregion
                }
                #endregion
            }

            #region 需要输出
            if (OutType == 1 && OutPath != null)
            {
                OutMap.WriteResult2Shp(OutPath, pMapControl.Map.SpatialReference,1);
            }
            #endregion
        }

        /// <summary>
        /// 判断给定的路径是否是直线
        /// </summary>
        /// <param name="CachePath"></param>
        /// <returns></returns>
        /// true表示Online；false表示不Online
        public bool OnLine(Path CachePath)
        {
            if (CachePath.ePath.Count > 2)
            {
                for (int i = 0; i < CachePath.ePath.Count - 2; i++)
                {
                    int add11 = CachePath.ePath[i].Item1 - CachePath.ePath[i + 1].Item1;
                    int add12 = CachePath.ePath[i].Item2 - CachePath.ePath[i + 1].Item2;

                    int add21 = CachePath.ePath[i + 1].Item1 - CachePath.ePath[i + 2].Item1;
                    int add22 = CachePath.ePath[i + 1].Item2 - CachePath.ePath[i + 2].Item2;

                    if (add11 != add21 || add12 != add22)
                    {
                        return false;
                    }
                }

                return true;
            }

            else
            {
                return true;
            }
        }
    }
}
