using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
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
using AuxStructureLib;
using AuxStructureLib.IO;

namespace PrDispalce.FlowMap
{
    public partial class FlowMap : Form
    {
        public FlowMap(AxMapControl axMapControl)
        {
            InitializeComponent();
            this.pMap = axMapControl.Map;
            this.pMapControl = axMapControl;
        }

        #region 参数
        AxMapControl pMapControl;
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        FlowSup Fs = new FlowSup();
        string OutlocalFilePath, OutfileNameExt, OutFilePath;
        PrDispalce.BuildingSim.PublicUtil Pu = new BuildingSim.PublicUtil();
        FlowMapUtil FMU = new FlowMapUtil();
        PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试 
        #endregion

        /// <summary>
        /// 深拷贝（通用拷贝）
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
        /// 非内插获取节点高程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            #region 参数
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<double> ElvList = new List<double>();
            int rowNum = 0; int colNum = 0;
            #endregion

            #region 索引
            double[] ExtendValue = new double[4];
            IGeoDataset pGeoDataset = pFeatureLayer as IGeoDataset;
            IEnvelope Extend = pGeoDataset.Extent;

            #region 可视化显示
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
            object PolygonSb = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 100, 100, 100);
            pMapControl.DrawShape(Extend, ref PolygonSb);
            pMapControl.Refresh();
            #endregion

            ExtendValue[0] = Extend.XMin; ExtendValue[1] = Extend.YMin; ExtendValue[2] = Extend.XMax; ExtendValue[3] = Extend.YMax;

            double[] GridXY = new double[2];
            GridXY[0] = 20; GridXY[1] = 20;

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);
            #endregion

            #region 获取相应的要素
            IFeatureCursor pFeatureCursor = pFeatureClass.Update(null, true);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                double Loc = Pu.GetValue(pFeature, "ID");
                IPoint nPoint = pFeature.Shape as IPoint;

                if (Pu.GetValue(pFeature, "ID") == 0)
                {
                    OriginPoint.X = nPoint.X;///pfeatureCursor本身可能存在问题
                    OriginPoint.Y = nPoint.Y;
                }

                else
                {
                    ElvList.Add(Pu.GetValue(pFeature, "FlowOut"));
                    IPoint rPoint = new PointClass();///pfeatureCursor本身可能存在问题
                    rPoint.X = nPoint.X;
                    rPoint.Y = nPoint.Y;
                    DesPoints.Add(rPoint);
                }

                pFeature = pFeatureCursor.NextFeature();
            }
            #endregion

            #region 生成相应的点
            IFeatureClass PointFeatureClass = pFeatureHandle.createPointshapefile(pMap.SpatialReference, OutFilePath, "IPoint");
            pFeatureHandle.AddField(PointFeatureClass, "Elv", esriFieldType.esriFieldTypeDouble);

            ////获取点图层的数据集，并创建工作空间
            //IDataset dataset = (IDataset)PointFeatureClass;
            //IWorkspace workspace = dataset.Workspace;
            //IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            ////定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
            //IFeatureClassWrite fr = (IFeatureClassWrite)PointFeatureClass;
            ////注意：此时，所编辑数据不能被其他程序打开
            //workspaceEdit.StartEditing(true);
            //workspaceEdit.StartEditOperation();

            IFeatureBuffer Fb = PointFeatureClass.CreateFeatureBuffer();
            IFeatureCursor insertCursor = PointFeatureClass.Insert(true);
            for (int i = 0; i < rowNum; i++)
            {
                for (int j = 0; j < colNum; j++)
                {
                    #region 点生成
                    IPoint TargetPoint = new PointClass();
                    Tuple<int, int> Key = new Tuple<int, int>(i, j);
                    TargetPoint.X = (Grids[Key][0] + Grids[Key][2]) / 2;
                    TargetPoint.Y = (Grids[Key][1] + Grids[Key][3]) / 2;

                    double Elv = Fs.GetElv(OriginPoint, TargetPoint, DesPoints, ElvList, 0);
                    #endregion

                    #region 点输出
                    IGeometry TGeometry = TargetPoint as IGeometry;
                    Fb.Shape = TGeometry;
                    Fb.set_Value(3, Elv);
                    insertCursor.InsertFeature(Fb);
                    #endregion
                }
            }

            insertCursor.Flush();
            //关闭编辑
            //workspaceEdit.StopEditOperation();
            //workspaceEdit.StopEditing(true);
            #endregion
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FlowMap_Load(object sender, EventArgs e)
        {
            if (this.pMap.LayerCount <= 0)
                return;

            #region 添加图层
            ILayer pLayer;
            string strLayerName;
            for (int i = 0; i < this.pMap.LayerCount; i++)
            {
                pLayer = this.pMap.get_Layer(i);
                strLayerName = pLayer.Name;
                IDataset LayerDataset = pLayer as IDataset;

                if (LayerDataset != null)
                {
                    this.comboBox1.Items.Add(strLayerName);
                    this.comboBox3.Items.Add(strLayerName);
                }
            }
            #endregion

            #region 默认显示第一个
            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }

            if (this.comboBox3.Items.Count > 0)
            {
                this.comboBox3.SelectedIndex = 0;
            }
            #endregion
        }

        /// <summary>
        /// 输出路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            //this.comboBox2.Items.Clear();
            //SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            //saveFileDialog1.Filter = " Raster files|" + "*";

            //if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            //{
            //    //获得文件路径
            //    OutlocalFilePath = saveFileDialog1.FileName.ToString();

            //    //获取文件名，不带路径
            //    OutfileNameExt = OutlocalFilePath.Substring(OutlocalFilePath.LastIndexOf("\\") + 1);

            //    //获取文件路径，不带文件名
            //    OutFilePath = OutlocalFilePath.Substring(0, OutlocalFilePath.LastIndexOf("\\"));
            //}

            //this.comboBox2.Text = OutlocalFilePath;

            FolderBrowserDialog fdialog = new FolderBrowserDialog();
            string outfilepath = null;

            if (fdialog.ShowDialog() == DialogResult.OK)
            {
                string Path = fdialog.SelectedPath;
                outfilepath = Path;
            }

            OutFilePath = outfilepath;
            this.comboBox2.Text = OutFilePath;
        }

        /// <summary>
        /// 栅格值修改测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            IRasterLayer rLayer = pFeatureHandle.GetRasterLayer(pMap, this.comboBox3.Text);

            //ESRI.ArcGIS.DataManagementTools.CopyRaster cpy = new ESRI.ArcGIS.DataManagementTools.CopyRaster();
            //cpy.in_raster = rLayer.FilePath;
            //cpy.out_rasterdataset = OutlocalFilePath;
            //Geoprocessor gp = new Geoprocessor();
            //gp.Execute(cpy, null);
            //IRasterLayer rasterLayer = new RasterLayerClass();
            //rasterLayer.CreateFromFilePath(OutlocalFilePath);

            Fs.ElvUpadate(rLayer.Raster);
        }

        /// <summary>
        /// 格网搜索测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            #region 参数
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            GridXY[0] = 20; GridXY[1] = 20;
            #endregion

            #region 索引
            double[] ExtendValue = new double[4];
            IGeoDataset pGeoDataset = pFeatureLayer as IGeoDataset;
            IEnvelope Extend = pGeoDataset.Extent;
            ExtendValue[0] = Extend.XMin; ExtendValue[1] = Extend.YMin; ExtendValue[2] = Extend.XMax; ExtendValue[3] = Extend.YMax;
            #endregion

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);

            #region 获取相应的要素(起点和终点)
            IFeatureCursor pFeatureCursor = pFeatureClass.Update(null, true);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                double Loc = Pu.GetValue(pFeature, "ID");
                IPoint nPoint = pFeature.Shape as IPoint;

                if (Pu.GetValue(pFeature, "ID") == 0)
                {
                    OriginPoint.X = nPoint.X;///pfeatureCursor本身可能存在问题
                    OriginPoint.Y = nPoint.Y;
                    AllPoints.Add(OriginPoint);
                }

                else
                {
                    IPoint rPoint = new PointClass();///pfeatureCursor本身可能存在问题
                    rPoint.X = nPoint.X;
                    rPoint.Y = nPoint.Y;
                    DesPoints.Add(rPoint);
                    AllPoints.Add(rPoint);
                }

                pFeature = pFeatureCursor.NextFeature();
            }
            #endregion

            #region 获取路径Grids
            PathTrace Pt = new PathTrace();
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);
            List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
            //Tuple<int, int> StartPoint = NodeInGrid[OriginPoint];
            Tuple<int, int> StartPoint = NodeInGrid[AllPoints[27]];
            JudgeList.Add(StartPoint);
            Pt.startPoint = NodeInGrid[OriginPoint];
            Pt.MazeAlg(JudgeList, Grids.Keys.ToList());
            #endregion

            #region 获取给定点的最短路径
            //List<Tuple<int, int>> ShortestPath = Pt.GetShortestPath(NodeInGrid[AllPoints[30]],Pt.startPoint);
            List<Tuple<int, int>> ShortestPath = Pt.GetShortestPath(Pt.startPoint, NodeInGrid[AllPoints[27]]);
            #endregion

            #region 可视化显示
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
            object PolylineSb = Sb.LineSymbolization(1, 100, 100, 100, 0);
            #endregion

            #region 点生成
            for (int i = 0; i < ShortestPath.Count - 1; i++)
            {
                IPoint sPoint = new PointClass();
                IPoint ePoint = new PointClass();
                sPoint.X = (Grids[ShortestPath[i]][0] + Grids[ShortestPath[i]][2]) / 2;
                sPoint.Y = (Grids[ShortestPath[i]][1] + Grids[ShortestPath[i]][3]) / 2;

                ePoint.X = (Grids[ShortestPath[i + 1]][0] + Grids[ShortestPath[i + 1]][2]) / 2;
                ePoint.Y = (Grids[ShortestPath[i + 1]][1] + Grids[ShortestPath[i + 1]][3]) / 2;

                IPolyline iLine = new PolylineClass();
                iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;

                pMapControl.DrawShape(iLine, ref PolylineSb);
                //pMapControl.Refresh();
            }
            #endregion
        }

        /// <summary>
        /// 权重格网搜索（不考虑Flow）给定点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            #region 参数
            IPoint OriginPoint = new PointClass();//起点
            List<IPoint> DesPoints = new List<IPoint>();//终点s
            List<IPoint> AllPoints = new List<IPoint>();//所有点
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            GridXY[0] = 20; GridXY[1] = 20;
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            #endregion

            #region 索引
            double[] ExtendValue = new double[4];
            IGeoDataset pGeoDataset = pFeatureLayer as IGeoDataset;
            IEnvelope Extend = pGeoDataset.Extent;
            ExtendValue[0] = Extend.XMin; ExtendValue[1] = Extend.YMin; ExtendValue[2] = Extend.XMax; ExtendValue[3] = Extend.YMax;
            #endregion

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网

            #region 获取相应的要素(起点和终点)
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
            #endregion

            #region 获取路径Grids
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）
            Dictionary<Tuple<int, int>, double> WeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 5, 1);

            PathTrace Pt = new PathTrace();
            List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
            //Tuple<int, int> StartPoint = NodeInGrid[OriginPoint];
            Tuple<int, int> StartPoint = NodeInGrid[AllPoints[8]];
            JudgeList.Add(StartPoint);
            Pt.startPoint = NodeInGrid[OriginPoint];
            Pt.MazeAlg(JudgeList, WeighGrids,2);
            #endregion

            #region 获取给定点的最短路径
            //List<Tuple<int, int>> ShortestPath = Pt.GetShortestPath(Pt.startPoint,NodeInGrid[AllPoints[16]]);
            List<Tuple<int, int>> ShortestPath = Pt.GetShortestPath( Pt.startPoint,NodeInGrid[AllPoints[8]]);
            #endregion

            #region 可视化显示
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
            object PolylineSb = Sb.LineSymbolization(1, 100, 100, 100, 0);
            #endregion

            #region 点生成
            //IFeatureClass LineFeatureClass = pFeatureHandle.createPolygonshapefile(pMap.SpatialReference, OutFilePath, "Line");
            //IFeatureBuffer Fb = LineFeatureClass.CreateFeatureBuffer();
            //IFeatureCursor insertCursor = LineFeatureClass.Insert(true);
            List<IPoint> ControlPoints = new List<IPoint>();
            for (int i = 0; i < ShortestPath.Count - 1; i++)
            {
                IPoint sPoint = new PointClass();
                IPoint ePoint = new PointClass();
                sPoint.X = (Grids[ShortestPath[i]][0] + Grids[ShortestPath[i]][2]) / 2;
                sPoint.Y = (Grids[ShortestPath[i]][1] + Grids[ShortestPath[i]][3]) / 2;

                ePoint.X = (Grids[ShortestPath[i + 1]][0] + Grids[ShortestPath[i + 1]][2]) / 2;
                ePoint.Y = (Grids[ShortestPath[i + 1]][1] + Grids[ShortestPath[i + 1]][3]) / 2;

                IPolyline iLine = new PolylineClass();
                iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;

                //ILine pLine=iLine as ILine;
                //IGeometry TGeometry = pLine as IGeometry;
                //Fb.Shape = TGeometry;
                //insertCursor.InsertFeature(Fb);

                #region 贝塞尔曲线考虑所有点
                ControlPoints.Add(sPoint);
                if (i == ShortestPath.Count - 2)
                {
                    ControlPoints.Add(ePoint);
                }
                #endregion

                pMapControl.DrawShape(iLine, ref PolylineSb);
                //pMapControl.Refresh();
            }

            ///贝塞尔曲线绘制
            #region 只考虑转折点
            //IPoint CacheSPoint = new PointClass();//起点
            //CacheSPoint.X = (Grids[ShortestPath[0]][0] + Grids[ShortestPath[0]][2]) / 2;
            //CacheSPoint.Y = (Grids[ShortestPath[0]][1] + Grids[ShortestPath[0]][3]) / 2;
            //ControlPoints.Add(CacheSPoint);


            //for (int i = 1; i < ShortestPath.Count - 1; i++)
            //{
            //    int AAD1 = Math.Abs(ShortestPath[i].Item1 - ShortestPath[i - 1].Item1) + Math.Abs(ShortestPath[i].Item2 - ShortestPath[i - 1].Item2);
            //    int AAD2 = Math.Abs(ShortestPath[i].Item1 - ShortestPath[i + 1].Item1) + Math.Abs(ShortestPath[i].Item2 - ShortestPath[i + 1].Item2);

            //    if (AAD1 != AAD2)
            //    {
            //        IPoint sPoint = new PointClass();
            //        sPoint.X = (Grids[ShortestPath[i]][0] + Grids[ShortestPath[i]][2]) / 2;
            //        sPoint.Y = (Grids[ShortestPath[i]][1] + Grids[ShortestPath[i]][3]) / 2;
            //        ControlPoints.Add(sPoint);
            //    }
            //}

            //IPoint CacheEPoint = new PointClass();//终点
            //CacheEPoint.X = (Grids[ShortestPath[ShortestPath.Count - 1]][0] + Grids[ShortestPath[ShortestPath.Count - 1]][2]) / 2;
            //CacheEPoint.Y = (Grids[ShortestPath[ShortestPath.Count - 1]][1] + Grids[ShortestPath[ShortestPath.Count - 1]][3]) / 2;
            //ControlPoints.Add(CacheEPoint);
            #endregion

            BezierCurve BC=new BezierCurve(ControlPoints);
            BC.CurveNGenerate(200);

            for (int i = 0; i < BC.CurvePoint.Count-1; i++)
            {
                IPolyline iLine = new PolylineClass();
                iLine.FromPoint = BC.CurvePoint[i];
                iLine.ToPoint = BC.CurvePoint[i + 1];

                //ILine pLine=iLine as ILine;
                //IGeometry TGeometry = pLine as IGeometry;
                //Fb.Shape = TGeometry;
                //insertCursor.InsertFeature(Fb);

                pMapControl.DrawShape(iLine, ref PolylineSb);
            }
            //insertCursor.Flush();
            #endregion
        }

        /// <summary>
        /// 生成FlowMap（格网权重赋值1+des最短距离，不考虑流量）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            #region 参数
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            GridXY[0] = 20; GridXY[1] = 20;
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            #endregion

            #region 索引
            double[] ExtendValue = new double[4];
            IGeoDataset pGeoDataset = pFeatureLayer as IGeoDataset;
            IEnvelope Extend = pGeoDataset.Extent;
            ExtendValue[0] = Extend.XMin; ExtendValue[1] = Extend.YMin; ExtendValue[2] = Extend.XMax; ExtendValue[3] = Extend.YMax;
            #endregion

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网

            #region 获取相应的要素(起点和终点)
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
            #endregion

            #region 获取Grids和节点编码
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）
            //Dictionary<Tuple<int, int>, double> WeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重

            //List<Tuple<int, int>> AllGrids = new List<Tuple<int, int>>();
            //for (int i = 0; i < AllPoints.Count; i++)
            //{
            //    AllGrids.Add(NodeInGrid[AllPoints[i]]);
            //}

            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }
            #endregion

            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);

            #region 遍历构成Flow过程
            double CountLabel = 0;//进程监控
            while (desGrids.Count > 0)
            {
                CountLabel++; Console.Write(CountLabel);//进程监控
                double MaxDis = 100000;
                Path CachePath = null;

                for (int i = 0; i < desGrids.Count; i++)
                {
                    //每次更新网格权重
                    Dictionary<Tuple<int, int>, double> WeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重

                    PathTrace Pt = new PathTrace();
                    List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                    JudgeList.Add(desGrids[i]);
                    Pt.startPoint = sGrid;
                    Pt.MazeAlg(JudgeList, WeighGrids,2);//备注：每次更新以后,WeightGrid会清零
                    List<Tuple<int, int>> TestShortestPath = Pt.GetShortestPath(cFM.startGrid, desGrids[i]);
                    double TestPathLength = Pt.GetPathLength(TestShortestPath);
                    List<Tuple<int, int>> ShortestPath = Pt.GetShortestPath(desGrids[i], cFM.PathGrids, cFM.GridForPaths, 0.65);
                    double PathLength = Pt.GetPathLength(ShortestPath);

                    if (PathLength < MaxDis)
                    {
                        MaxDis = PathLength;
                        CachePath = new Path(sGrid, desGrids[i], ShortestPath);
                    }
                }

                cFM.PathRefresh(CachePath, 1);
                desGrids.Remove(CachePath.endPoint);
            }
            #endregion

            #region 可视化显示
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
            object PolygonSb = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 100, 100, 100);

            for (int i = 0; i < cFM.PathGrids.Count; i++)
            {
                IEnvelope CacheEnve = new EnvelopeClass();
                CacheEnve.XMin = Grids[cFM.PathGrids[i]][0];
                CacheEnve.YMin = Grids[cFM.PathGrids[i]][1];
                CacheEnve.XMax = Grids[cFM.PathGrids[i]][2];
                CacheEnve.YMax = Grids[cFM.PathGrids[i]][3];
                pMapControl.DrawShape(CacheEnve, ref PolygonSb);
            }
            #endregion
        }

        /// <summary>
        /// 生成FlowMap（格网权重赋值1+des最长距离，不考虑流量）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            #region 参数
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            GridXY[0] = 20; GridXY[1] = 20;
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            #endregion

            #region 索引
            double[] ExtendValue = new double[4];
            IGeoDataset pGeoDataset = pFeatureLayer as IGeoDataset;
            IEnvelope Extend = pGeoDataset.Extent;
            ExtendValue[0] = Extend.XMin; ExtendValue[1] = Extend.YMin; ExtendValue[2] = Extend.XMax; ExtendValue[3] = Extend.YMax;
            #endregion

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网

            #region 获取相应的要素(起点和终点)
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
            #endregion

            #region 获取Grids和节点编码
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）
            //Dictionary<Tuple<int, int>, double> WeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重

            //List<Tuple<int, int>> AllGrids = new List<Tuple<int, int>>();
            //for (int i = 0; i < AllPoints.Count; i++)
            //{
            //    AllGrids.Add(NodeInGrid[AllPoints[i]]);
            //}

            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }
            #endregion

            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重

            #region 遍历构成Flow过程
            double CountLabel = 0;//进程监控
            while (desGrids.Count > 0)
            {
                CountLabel++; Console.WriteLine(CountLabel);//进程监控
                double MaxDis = 0;
                Path CachePath = null;

                for (int i = 0; i < desGrids.Count; i++)
                {
                    //每次更新网格权重
                    Console.Write(i);
                    Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝
                   
                    PathTrace Pt = new PathTrace();
                    List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                    JudgeList.Add(desGrids[i]);
                    Pt.startPoint = sGrid;
                    Pt.MazeAlg(JudgeList, WeighGrids,2);//备注：每次更新以后,WeightGrid会清零
                    List<Tuple<int, int>> TestShortestPath = Pt.GetShortestPath(cFM.startGrid, desGrids[i]);
                    double TestPathLength = Pt.GetPathLength(TestShortestPath);
                    List<Tuple<int, int>> ShortestPath = Pt.GetShortestPath(desGrids[i], cFM.PathGrids, cFM.GridForPaths, 0.65);
                    double PathLength = Pt.GetPathLength(ShortestPath);

                    if (PathLength > MaxDis)
                    {
                        MaxDis = PathLength;
                        CachePath = new Path(sGrid, desGrids[i], ShortestPath);
                    }
                }

                cFM.PathRefresh(CachePath, 1);
                desGrids.Remove(CachePath.endPoint);
            }
            #endregion

            #region 可视化显示
            //PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
            //object PolygonSb = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 100, 100, 100);

            //for (int i = 0; i < cFM.PathGrids.Count; i++)
            //{
            //    IEnvelope CacheEnve = new EnvelopeClass();
            //    CacheEnve.XMin = Grids[cFM.PathGrids[i]][0];
            //    CacheEnve.YMin = Grids[cFM.PathGrids[i]][1];
            //    CacheEnve.XMax = Grids[cFM.PathGrids[i]][2];
            //    CacheEnve.YMax = Grids[cFM.PathGrids[i]][3];
            //    pMapControl.DrawShape(CacheEnve, ref PolygonSb);
            //}

            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
            object PolylineSb = Sb.LineSymbolization(1, 100, 100, 100, 0);

            foreach (KeyValuePair<int, Path> kv in cFM.OrderPaths)
            {
                for (int j = 0; j < kv.Value.ePath.Count - 1; j++)
                {
                    IPoint sPoint = new PointClass();
                    IPoint ePoint = new PointClass();
                    sPoint.X = (Grids[kv.Value.ePath[j]][0] + Grids[kv.Value.ePath[j]][2]) / 2;
                    sPoint.Y = (Grids[kv.Value.ePath[j]][1] + Grids[kv.Value.ePath[j]][3]) / 2;

                    ePoint.X = (Grids[kv.Value.ePath[j + 1]][0] + Grids[kv.Value.ePath[j + 1]][2]) / 2;
                    ePoint.Y = (Grids[kv.Value.ePath[j + 1]][1] + Grids[kv.Value.ePath[j + 1]][3]) / 2;

                    IPolyline iLine = new PolylineClass();
                    iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;

                    pMapControl.DrawShape(iLine, ref PolylineSb);
                    //pMapControl.Refresh();
                }
            }
            #endregion
        }

        /// <summary>
        /// 权重格网（不考虑Flow）搜索（最长距离）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button8_Click(object sender, EventArgs e)
        {
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            #region 参数
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            GridXY[0] = 20; GridXY[1] = 20;
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            #endregion

            #region 索引
            double[] ExtendValue = new double[4];
            IGeoDataset pGeoDataset = pFeatureLayer as IGeoDataset;
            IEnvelope Extend = pGeoDataset.Extent;
            ExtendValue[0] = Extend.XMin; ExtendValue[1] = Extend.YMin; ExtendValue[2] = Extend.XMax; ExtendValue[3] = Extend.YMax;
            #endregion

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网

            #region 获取相应的要素(起点和终点)
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
            #endregion

            #region 获取Grids和节点编码
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）
            //Dictionary<Tuple<int, int>, double> WeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重

            //List<Tuple<int, int>> AllGrids = new List<Tuple<int, int>>();
            //for (int i = 0; i < AllPoints.Count; i++)
            //{
            //    AllGrids.Add(NodeInGrid[AllPoints[i]]);
            //}

            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }
            #endregion

            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重

            #region 遍历构成Flow过程
            double MaxDis = 0;
            Path CachePath = null;

            for (int i = 0; i < desGrids.Count; i++)
            {
                //每次更新网格权重
                Console.Write(i);
                Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝
                

                PathTrace Pt = new PathTrace();
                List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                JudgeList.Add(desGrids[i]);
                Pt.startPoint = sGrid;
                Pt.MazeAlg(JudgeList, WeighGrids,2);//备注：每次更新以后,WeightGrid会清零
                List<Tuple<int, int>> TestShortestPath = Pt.GetShortestPath(cFM.startGrid, desGrids[i]);
                double TestPathLength = Pt.GetPathLength(TestShortestPath);
                List<Tuple<int, int>> ShortestPath = Pt.GetShortestPath(desGrids[i], cFM.PathGrids, cFM.GridForPaths, 0.65);
                double PathLength = Pt.GetPathLength(ShortestPath);

                if (PathLength > MaxDis)
                {
                    MaxDis = PathLength;
                    CachePath = new Path(sGrid, desGrids[i], ShortestPath);
                }
            }
            cFM.PathRefresh(CachePath, 1);
            #endregion

            #region 可视化显示
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
            object PolygonSb = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 100, 100, 100);

            for (int i = 0; i < cFM.PathGrids.Count; i++)
            {
                IEnvelope CacheEnve = new EnvelopeClass();
                CacheEnve.XMin = Grids[cFM.PathGrids[i]][0];
                CacheEnve.YMin = Grids[cFM.PathGrids[i]][1];
                CacheEnve.XMax = Grids[cFM.PathGrids[i]][2];
                CacheEnve.YMax = Grids[cFM.PathGrids[i]][3];
                pMapControl.DrawShape(CacheEnve, ref PolygonSb);
            }
            #endregion
        }

        /// <summary>
        /// 格网权重搜索，邻近范围数量限制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button10_Click(object sender, EventArgs e)
        {
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            #region 参数
            IPoint OriginPoint = new PointClass();//起点
            List<IPoint> DesPoints = new List<IPoint>();//终点s
            List<IPoint> AllPoints = new List<IPoint>();//所有点
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            GridXY[0] = 20; GridXY[1] = 20;
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            #endregion

            #region 索引
            double[] ExtendValue = new double[4];
            IGeoDataset pGeoDataset = pFeatureLayer as IGeoDataset;
            IEnvelope Extend = pGeoDataset.Extent;
            ExtendValue[0] = Extend.XMin; ExtendValue[1] = Extend.YMin; ExtendValue[2] = Extend.XMax; ExtendValue[3] = Extend.YMax;
            #endregion

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网

            #region 获取相应的要素(起点和终点)
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
            #endregion

            #region 获取路径Grids
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）
            Dictionary<Tuple<int, int>, double> WeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 2, 1);

            PathTrace Pt = new PathTrace();
            List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
            Tuple<int, int> StartPoint = NodeInGrid[OriginPoint];
            //Tuple<int, int> StartPoint = NodeInGrid[AllPoints[16]];
            JudgeList.Add(StartPoint);
            Pt.startPoint = NodeInGrid[OriginPoint];
            Pt.MazeAlg(JudgeList, WeighGrids, 1);
            #endregion

            #region 获取给定点的最短路径
            //List<Tuple<int, int>> ShortestPath = Pt.GetShortestPath(Pt.startPoint,NodeInGrid[AllPoints[16]]);
            List<Tuple<int, int>> ShortestPath = Pt.GetShortestPath(NodeInGrid[AllPoints[16]], Pt.startPoint);
            #endregion

            #region 可视化显示
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
            object PolylineSb = Sb.LineSymbolization(1, 100, 100, 100, 0);
            #endregion

            #region 点生成
            for (int i = 0; i < ShortestPath.Count - 1; i++)
            {
                IPoint sPoint = new PointClass();
                IPoint ePoint = new PointClass();
                sPoint.X = (Grids[ShortestPath[i]][0] + Grids[ShortestPath[i]][2]) / 2;
                sPoint.Y = (Grids[ShortestPath[i]][1] + Grids[ShortestPath[i]][3]) / 2;

                ePoint.X = (Grids[ShortestPath[i + 1]][0] + Grids[ShortestPath[i + 1]][2]) / 2;
                ePoint.Y = (Grids[ShortestPath[i + 1]][1] + Grids[ShortestPath[i + 1]][3]) / 2;

                IPolyline iLine = new PolylineClass();
                iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;

                pMapControl.DrawShape(iLine, ref PolylineSb);
                //pMapControl.Refresh();
            }
            #endregion
        }

        /// <summary>
        /// 权重格网搜索，限制方向
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button11_Click(object sender, EventArgs e)
        {
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            #region 参数
            IPoint OriginPoint = new PointClass();//起点
            List<IPoint> DesPoints = new List<IPoint>();//终点s
            List<IPoint> AllPoints = new List<IPoint>();//所有点
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            GridXY[0] = 20; GridXY[1] = 20;
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            #endregion

            #region 索引
            double[] ExtendValue = new double[4];
            IGeoDataset pGeoDataset = pFeatureLayer as IGeoDataset;
            IEnvelope Extend = pGeoDataset.Extent;
            ExtendValue[0] = Extend.XMin; ExtendValue[1] = Extend.YMin; ExtendValue[2] = Extend.XMax; ExtendValue[3] = Extend.YMax;
            #endregion

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网

            #region 获取相应的要素(起点和终点)
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
            #endregion

            #region 获取路径Grids
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）
            Dictionary<Tuple<int, int>, double> WeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);

            PathTrace Pt = new PathTrace();
            List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
            //Tuple<int, int> StartPoint = NodeInGrid[OriginPoint];
            Tuple<int, int> StartPoint = NodeInGrid[AllPoints[22]];
            JudgeList.Add(StartPoint);
            Pt.startPoint = NodeInGrid[OriginPoint];
            List<int> DirList = Pt.GetConDir(NodeInGrid[OriginPoint], NodeInGrid[AllPoints[22]]);
            Pt.MazeAlg(JudgeList, WeighGrids, 2, DirList, NodeInGrid[OriginPoint]);
            #endregion

            #region 获取给定点的最短路径
            //List<Tuple<int, int>> ShortestPath = Pt.GetShortestPath(Pt.startPoint,NodeInGrid[AllPoints[16]]);
            List<Tuple<int, int>> ShortestPath = Pt.GetShortestPath(Pt.startPoint,NodeInGrid[AllPoints[22]]);
            #endregion

            #region 可视化显示
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
            object PolylineSb = Sb.LineSymbolization(1, 100, 100, 100, 0);
            #endregion

            #region 点生成
            for (int i = 0; i < ShortestPath.Count - 1; i++)
            {
                IPoint sPoint = new PointClass();
                IPoint ePoint = new PointClass();
                sPoint.X = (Grids[ShortestPath[i]][0] + Grids[ShortestPath[i]][2]) / 2;
                sPoint.Y = (Grids[ShortestPath[i]][1] + Grids[ShortestPath[i]][3]) / 2;

                ePoint.X = (Grids[ShortestPath[i + 1]][0] + Grids[ShortestPath[i + 1]][2]) / 2;
                ePoint.Y = (Grids[ShortestPath[i + 1]][1] + Grids[ShortestPath[i + 1]][3]) / 2;

                IPolyline iLine = new PolylineClass();
                iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;

                pMapControl.DrawShape(iLine, ref PolylineSb);
                //pMapControl.Refresh();
            }
            #endregion
        }

        /// <summary>
        /// 生成FlowMap（格网权重赋值1+des最长距离，不考虑流量）+依据起点到终点的方向关系限制搜索方向
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button12_Click(object sender, EventArgs e)
        {
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            #region 参数
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            GridXY[0] = 20; GridXY[1] = 20;
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            #endregion

            #region 索引
            double[] ExtendValue = new double[4];
            IGeoDataset pGeoDataset = pFeatureLayer as IGeoDataset;
            IEnvelope Extend = pGeoDataset.Extent;
            ExtendValue[0] = Extend.XMin; ExtendValue[1] = Extend.YMin; ExtendValue[2] = Extend.XMax; ExtendValue[3] = Extend.YMax;
            #endregion

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网

            #region 获取相应的要素(起点和终点)
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
            #endregion

            #region 获取Grids和节点编码
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）
            //Dictionary<Tuple<int, int>, double> WeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重

            //List<Tuple<int, int>> AllGrids = new List<Tuple<int, int>>();
            //for (int i = 0; i < AllPoints.Count; i++)
            //{
            //    AllGrids.Add(NodeInGrid[AllPoints[i]]);
            //}

            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }
            #endregion

            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重

            #region 遍历构成Flow过程
            double CountLabel = 0;//进程监控
            while (desGrids.Count > 0)
            {
                CountLabel++; Console.WriteLine(CountLabel);//进程监控
                double MaxDis = 0;
                Path CachePath = null;

                for (int i = 0; i < desGrids.Count; i++)
                {
                    //每次更新网格权重
                    Console.Write(i);

                    double MinLength = 10000000;
                    List<Tuple<int, int>> TargetPath = null;
                    for (int j = 0; j < cFM.PathGrids.Count; j++)
                    {
                        Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝
                        
                        PathTrace Pt = new PathTrace();
                        List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                        JudgeList.Add(desGrids[i]);
                        Pt.startPoint = cFM.PathGrids[j];
                        List<int> DirList = Pt.GetConDir(cFM.PathGrids[j], desGrids[i]);
                        Pt.MazeAlg(JudgeList, WeighGrids, 2,DirList,cFM.PathGrids[j]);//备注：每次更新以后,WeightGrid会清零                      
                        List<Tuple<int, int>> CacheShortPath = Pt.GetShortestPath(cFM.PathGrids[j], desGrids[i]);

                        double CacheShortPathLength = Pt.GetPathLength(CacheShortPath);
                        double TotalLength = CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65;

                        if (TotalLength < MinLength)
                        {
                            MinLength = TotalLength;
                            List<Tuple<int, int>> pCachePath = cFM.GridForPaths[cFM.PathGrids[j]].ePath.ToList();
                            CacheShortPath.RemoveAt(0);//移除第一个要素，避免存在重复元素

                            pCachePath.AddRange(CacheShortPath);
                            TargetPath = pCachePath;
                        }
                    }

                    if (MinLength > MaxDis)
                    {
                        MaxDis = MinLength;
                        CachePath = new Path(sGrid, desGrids[i], TargetPath);
                    }
                }

                cFM.PathRefresh(CachePath, 1);
                desGrids.Remove(CachePath.endPoint);
            }
            #endregion

            #region 可视化显示
            //PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
            //object PolygonSb = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 100, 100, 100);

            //for (int i = 0; i < cFM.PathGrids.Count; i++)
            //{
            //    IEnvelope CacheEnve = new EnvelopeClass();
            //    CacheEnve.XMin = Grids[cFM.PathGrids[i]][0];
            //    CacheEnve.YMin = Grids[cFM.PathGrids[i]][1];
            //    CacheEnve.XMax = Grids[cFM.PathGrids[i]][2];
            //    CacheEnve.YMax = Grids[cFM.PathGrids[i]][3];
            //    pMapControl.DrawShape(CacheEnve, ref PolygonSb);
            //}

            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
            object PolylineSb = Sb.LineSymbolization(1, 100, 100, 100, 0);

            foreach (KeyValuePair<int, Path> kv in cFM.OrderPaths)
            {
                for (int j = 0; j < kv.Value.ePath.Count - 1; j++)
                {
                    IPoint sPoint = new PointClass();
                    IPoint ePoint = new PointClass();
                    sPoint.X = (Grids[kv.Value.ePath[j]][0] + Grids[kv.Value.ePath[j]][2]) / 2;
                    sPoint.Y = (Grids[kv.Value.ePath[j]][1] + Grids[kv.Value.ePath[j]][3]) / 2;

                    ePoint.X = (Grids[kv.Value.ePath[j + 1]][0] + Grids[kv.Value.ePath[j + 1]][2]) / 2;
                    ePoint.Y = (Grids[kv.Value.ePath[j + 1]][1] + Grids[kv.Value.ePath[j + 1]][3]) / 2;

                    IPolyline iLine = new PolylineClass();
                    iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;

                    pMapControl.DrawShape(iLine, ref PolylineSb);
                    //pMapControl.Refresh();
                }
            }
            #endregion
        }

        /// <summary>
        /// 生成FlowMap（格网权重赋值1+des最长距离，不考虑流量）+依据起点到终点的方向关系限制搜索方向+起点优先
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button13_Click(object sender, EventArgs e)
        {
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            #region 参数
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            GridXY[0] = 20; GridXY[1] = 20;
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            #endregion

            #region 索引
            double[] ExtendValue = new double[4];
            IGeoDataset pGeoDataset = pFeatureLayer as IGeoDataset;
            IEnvelope Extend = pGeoDataset.Extent;
            ExtendValue[0] = Extend.XMin; ExtendValue[1] = Extend.YMin; ExtendValue[2] = Extend.XMax; ExtendValue[3] = Extend.YMax;
            #endregion

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网

            #region 获取相应的要素(起点和终点)
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
            #endregion

            #region 获取Grids和节点编码
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）
            //Dictionary<Tuple<int, int>, double> WeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重

            //List<Tuple<int, int>> AllGrids = new List<Tuple<int, int>>();
            //for (int i = 0; i < AllPoints.Count; i++)
            //{
            //    AllGrids.Add(NodeInGrid[AllPoints[i]]);
            //}

            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }
            #endregion

            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重

            #region 遍历构成Flow过程
            double CountLabel = 0;//进程监控
            while (desGrids.Count > 0)
            {
                CountLabel++; Console.WriteLine(CountLabel);//进程监控
                double MaxDis = 0;
                Path CachePath = null;

                for (int i = 0; i < desGrids.Count; i++)
                {
                    //每次更新网格权重
                    Console.Write(i);

                    double MinLength = 10000000;
                    List<Tuple<int, int>> TargetPath = null;
                    for (int j = 0; j < cFM.PathGrids.Count; j++)
                    {
                        Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝

                        PathTrace Pt = new PathTrace();
                        List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                        JudgeList.Add(desGrids[i]);
                        Pt.startPoint = cFM.PathGrids[j];
                        List<int> DirList = Pt.GetConDir(cFM.PathGrids[j], desGrids[i]);
                        Pt.MazeAlg(JudgeList, WeighGrids, 2, DirList, cFM.PathGrids[j]);//备注：每次更新以后,WeightGrid会清零                      
                        List<Tuple<int, int>> CacheShortPath = Pt.GetShortestPath(cFM.PathGrids[j], desGrids[i]);

                        double CacheShortPathLength = Pt.GetPathLength(CacheShortPath);
                        double TotalLength=0;
                        ///起点优先
                        if (cFM.GridForPaths[cFM.PathGrids[j]].Length == 0)
                        {
                            TotalLength = CacheShortPathLength + 100000;
                        }
                        else
                        {
                            TotalLength = CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65;
                        }

                        if (TotalLength < MinLength)
                        {
                            MinLength = TotalLength;
                            List<Tuple<int, int>> pCachePath = cFM.GridForPaths[cFM.PathGrids[j]].ePath.ToList();
                            CacheShortPath.RemoveAt(0);//移除第一个要素，避免存在重复元素

                            pCachePath.AddRange(CacheShortPath);
                            TargetPath = pCachePath;
                        }
                    }

                    if (MinLength > MaxDis)
                    {
                        MaxDis = MinLength;
                        CachePath = new Path(sGrid, desGrids[i], TargetPath);
                    }
                }

                //cFM.PathRefresh(CachePath, 1);
                cFM.PathRefresh(CachePath, 1, PointFlow[GridWithNode[CachePath.endPoint]]);//更新：包括子路径与流量更新
                desGrids.Remove(CachePath.endPoint);//移除一个Destination
            }
            #endregion

            #region 可视化显示
            //PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
            //object PolygonSb = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 100, 100, 100);

            //for (int i = 0; i < cFM.PathGrids.Count; i++)
            //{
            //    IEnvelope CacheEnve = new EnvelopeClass();
            //    CacheEnve.XMin = Grids[cFM.PathGrids[i]][0];
            //    CacheEnve.YMin = Grids[cFM.PathGrids[i]][1];
            //    CacheEnve.XMax = Grids[cFM.PathGrids[i]][2];
            //    CacheEnve.YMax = Grids[cFM.PathGrids[i]][3];
            //    pMapControl.DrawShape(CacheEnve, ref PolygonSb);
            //}

            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
            object PolylineSb = Sb.LineSymbolization(1, 100, 100, 100, 0);
            #region 按宽度绘制路径           
            foreach (Path Pa in cFM.SubPaths)
            {
                PolylineSb = Sb.LineSymbolization(Pa.Volume / 1000 * 10, 100, 100, 100, 0);
                for (int j = 0; j < Pa.ePath.Count - 1; j++)
                {
                    IPoint sPoint = new PointClass();
                    IPoint ePoint = new PointClass();
                    sPoint.X = (Grids[Pa.ePath[j]][0] + Grids[Pa.ePath[j]][2]) / 2;
                    sPoint.Y = (Grids[Pa.ePath[j]][1] + Grids[Pa.ePath[j]][3]) / 2;

                    ePoint.X = (Grids[Pa.ePath[j + 1]][0] + Grids[Pa.ePath[j + 1]][2]) / 2;
                    ePoint.Y = (Grids[Pa.ePath[j + 1]][1] + Grids[Pa.ePath[j + 1]][3]) / 2;

                    IPolyline iLine = new PolylineClass();
                    iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;

                    pMapControl.DrawShape(iLine, ref PolylineSb);
                    //pMapControl.Refresh();
                }
            }
            #endregion

            #region 绘制路径
            //PolylineSb = Sb.LineSymbolization(1, 100, 100, 100, 0);
            //foreach (KeyValuePair<int, Path> kv in cFM.OrderPaths)
            //{
            //    for (int j = 0; j < kv.Value.ePath.Count - 1; j++)
            //    {
            //        IPoint sPoint = new PointClass();
            //        IPoint ePoint = new PointClass();
            //        sPoint.X = (Grids[kv.Value.ePath[j]][0] + Grids[kv.Value.ePath[j]][2]) / 2;
            //        sPoint.Y = (Grids[kv.Value.ePath[j]][1] + Grids[kv.Value.ePath[j]][3]) / 2;

            //        ePoint.X = (Grids[kv.Value.ePath[j + 1]][0] + Grids[kv.Value.ePath[j + 1]][2]) / 2;
            //        ePoint.Y = (Grids[kv.Value.ePath[j + 1]][1] + Grids[kv.Value.ePath[j + 1]][3]) / 2;

            //        IPolyline iLine = new PolylineClass();
            //        iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;

            //        pMapControl.DrawShape(iLine, ref PolylineSb);
            //        //pMapControl.Refresh();
            //    }
            //}
            #endregion
            #endregion
        }

        /// <summary>
        /// FlowMap（权重赋值+最长距离+方向限制+起点优先+角度限制+避免重叠）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button14_Click(object sender, EventArgs e)
        {
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试

            #region 参数
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            GridXY[0] = 10; GridXY[1] = 10;
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            #endregion

            #region 索引
            double[] ExtendValue = new double[4];
            IGeoDataset pGeoDataset = pFeatureLayer as IGeoDataset;
            IEnvelope Extend = pGeoDataset.Extent;
            ExtendValue[0] = Extend.XMin; ExtendValue[1] = Extend.YMin; ExtendValue[2] = Extend.XMax; ExtendValue[3] = Extend.YMax;
            #endregion

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网

            #region 获取相应的要素(起点和终点)
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
            #endregion

            #region 获取Grids和节点编码
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）
            //Dictionary<Tuple<int, int>, double> WeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重

            //List<Tuple<int, int>> AllGrids = new List<Tuple<int, int>>();
            //for (int i = 0; i < AllPoints.Count; i++)
            //{
            //    AllGrids.Add(NodeInGrid[AllPoints[i]]);
            //}

            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }
            #endregion

            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 8, 1);//确定整个Grids的权重

            #region 遍历构成Flow过程
            double CountLabel = 0;//进程监控
            while (desGrids.Count > 0)
            {
                CountLabel++; Console.WriteLine(CountLabel);//进程监控
                double MaxDis = 0;
                Path CachePath = null;
                double textAngle2 = 0;//测试用

                for (int i = 0; i < desGrids.Count; i++)
                {
                    //每次更新网格权重
                    Console.Write(i);

                    #region 获取到PathGrid的路径
                    double MinLength = 10000;
                    double textAngle = 0;//测试用
                    List<Tuple<int, int>> TargetPath = null;

                    int Label = 0;//标识终点到起点最短距离的节是否是起点（=1表示终点是起点）
                    for (int j = 0; j < cFM.PathGrids.Count; j++)
                    {
                        Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝

                        #region 添加Flow与节点重叠的限制约束条件
                        for (int n = 0; n < desGrids.Count; n++)
                        {
                            if (n != i)
                            {
                                WeighGrids.Remove(desGrids[n]);
                            }
                        }
                        #endregion

                        #region 获取终点到已有路径中某点的最短路径
                        PathTrace Pt = new PathTrace();
                        List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                        JudgeList.Add(desGrids[i]);
                        Pt.startPoint = cFM.PathGrids[j];
                        List<int> DirList = Pt.GetConDir(cFM.PathGrids[j], desGrids[i]);
                        Pt.MazeAlg(JudgeList, WeighGrids, 2, DirList, cFM.PathGrids[j]);//备注：每次更新以后,WeightGrid会清零                      
                        List<Tuple<int, int>> CacheShortPath = Pt.GetShortestPath(cFM.PathGrids[j], desGrids[i]);

                        #region 需要考虑可能不存在路径的情况
                        double CacheShortPathLength = 0;
                        if (CacheShortPath != null)
                        {
                            CacheShortPathLength = Pt.GetPathLength(CacheShortPath);
                        }
                        else
                        {
                            CacheShortPathLength = 10000000;
                        }
                        #endregion

                        double TotalLength = 0;
                        ///起点优先(若某点到起点的最短距离为该点到起点的最短距离，则其被优先选择，即添加一个较大值！！！！)
                        //if (cFM.GridForPaths[cFM.PathGrids[j]].Length == 0)
                        //{
                        //    TotalLength = CacheShortPathLength + 100000;
                        //    //TotalLength = CacheShortPathLength * 0.1;
                        //}
                        //else
                        //{
                        TotalLength = CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65;
                        //}
                        #endregion

                        #region 添加角度约束限制
                        Tuple<int, int> FromGrid = null;
                        Tuple<int, int> MidGrid = null;
                        Tuple<int, int> ToGrid = null;
                        double CacheAngle = 0;
                        if (CacheShortPath != null && CacheShortPath.Count >= 2 && cFM.GridForPaths[cFM.PathGrids[j]].ePath.Count >= 2)
                        {
                            FromGrid = CacheShortPath[1];
                            MidGrid = CacheShortPath[0];
                            ToGrid = cFM.GridForPaths[cFM.PathGrids[j]].ePath[cFM.GridForPaths[cFM.PathGrids[j]].ePath.Count - 2];

                            TriNode FromPoint = new TriNode();
                            TriNode MidPoint = new TriNode();
                            TriNode ToPoint = new TriNode();

                            //IPoint fPoint = new PointClass();//测试用
                            //IPoint mPoint = new PointClass();
                            //IPoint tPoint = new PointClass();

                            FromPoint.X = (Grids[FromGrid][0] + Grids[FromGrid][2]) / 2;
                            FromPoint.Y = (Grids[FromGrid][1] + Grids[FromGrid][3]) / 2;

                            MidPoint.X = (Grids[MidGrid][0] + Grids[MidGrid][2]) / 2;
                            MidPoint.Y = (Grids[MidGrid][1] + Grids[MidGrid][3]) / 2;

                            ToPoint.X = (Grids[ToGrid][0] + Grids[ToGrid][2]) / 2;
                            ToPoint.Y = (Grids[ToGrid][1] + Grids[ToGrid][3]) / 2;

                            CacheAngle = Pu.GetAngle(MidPoint, ToPoint, FromPoint);

                            //fPoint.X = FromPoint.X; fPoint.Y = FromPoint.Y;//测试用
                            //mPoint.X = MidPoint.X; mPoint.Y = MidPoint.Y;
                            //tPoint.X = ToPoint.X; tPoint.Y = ToPoint.Y;
                            //object PointSy = Sb.PointSymbolization(100, 100, 100);
                            //pMapControl.DrawShape(fPoint, ref PointSy);
                            //pMapControl.DrawShape(mPoint, ref PointSy);
                            //pMapControl.DrawShape(tPoint, ref PointSy);
                            //pMapControl.Refresh();

                            if (CacheAngle > 3.1415927 / 3 && CacheAngle < 2 * 3.1415926 / 3)
                            {
                                TotalLength = TotalLength + 1000000;
                            }
                        }
                        #endregion

                        #region 比较获取某给定节点到起点的最短路径
                        if (TotalLength < MinLength)
                        {
                            if (cFM.GridForPaths[cFM.PathGrids[j]].Length == 0 && !cFM.PathGrids.Contains(CacheShortPath[1]))//消除某些点到起点的最短路径是经过已有路径的情况
                            {
                                Label = 1;//标识最短路径终点是起点
                            }

                            else
                            {
                                Label = 0;//标识最短路径终点非起点
                            }

                            MinLength = TotalLength;
                            List<Tuple<int, int>> pCachePath = cFM.GridForPaths[cFM.PathGrids[j]].ePath.ToList();
                            CacheShortPath.RemoveAt(0);//移除第一个要素，避免存在重复元素

                            pCachePath.AddRange(CacheShortPath);
                            TargetPath = pCachePath;

                            textAngle = CacheAngle;//测试用
                        }
                        #endregion
                    }
                    #endregion

                    #region 表示起点优先限制
                    if (Label == 1)
                    {
                        MinLength = MinLength + 10000;
                    }
                    #endregion

                    #region 获取到起点路径最长终点的路径
                    if (MinLength > MaxDis)
                    {
                        MaxDis = MinLength;
                        CachePath = new Path(sGrid, desGrids[i], TargetPath);

                        textAngle2 = textAngle;//测试用
                    }
                    #endregion
                }


                #region 可视化显示
                object cPolylineSb = Sb.LineSymbolization(1, 100, 100, 100, 0);
                
                for (int i = 0; i < CachePath.ePath.Count - 1; i++)
                {
                    IPoint sPoint = new PointClass();
                    IPoint ePoint = new PointClass();
                    sPoint.X = (Grids[CachePath.ePath[i]][0] + Grids[CachePath.ePath[i]][2]) / 2;
                    sPoint.Y = (Grids[CachePath.ePath[i]][1] + Grids[CachePath.ePath[i]][3]) / 2;

                    ePoint.X = (Grids[CachePath.ePath[i + 1]][0] + Grids[CachePath.ePath[i + 1]][2]) / 2;
                    ePoint.Y = (Grids[CachePath.ePath[i + 1]][1] + Grids[CachePath.ePath[i + 1]][3]) / 2;

                    IPolyline iLine = new PolylineClass();
                    iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;

                    pMapControl.DrawShape(iLine, ref cPolylineSb);
                    //pMap
                }
                #endregion

                //cFM.PathRefresh(CachePath, 1);
                cFM.PathRefresh(CachePath, 1, PointFlow[GridWithNode[CachePath.endPoint]]);//更新：包括子路径与流量更新
                desGrids.Remove(CachePath.endPoint);//移除一个Destination

            }
            #endregion

            #region 可视化显示
            //PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
            //object PolygonSb = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 100, 100, 100);

            //for (int i = 0; i < cFM.PathGrids.Count; i++)
            //{
            //    IEnvelope CacheEnve = new EnvelopeClass();
            //    CacheEnve.XMin = Grids[cFM.PathGrids[i]][0];
            //    CacheEnve.YMin = Grids[cFM.PathGrids[i]][1];
            //    CacheEnve.XMax = Grids[cFM.PathGrids[i]][2];
            //    CacheEnve.YMax = Grids[cFM.PathGrids[i]][3];
            //    pMapControl.DrawShape(CacheEnve, ref PolygonSb);
            //}

            
            object PolylineSb = Sb.LineSymbolization(1, 100, 100, 100, 0);
            #region 按宽度绘制路径
            foreach (Path Pa in cFM.SubPaths)
            {
                PolylineSb = Sb.LineSymbolization(Pa.Volume / 1000 * 10, 100, 100, 100, 0);
                for (int j = 0; j < Pa.ePath.Count - 1; j++)
                {
                    IPoint sPoint = new PointClass();
                    IPoint ePoint = new PointClass();
                    sPoint.X = (Grids[Pa.ePath[j]][0] + Grids[Pa.ePath[j]][2]) / 2;
                    sPoint.Y = (Grids[Pa.ePath[j]][1] + Grids[Pa.ePath[j]][3]) / 2;

                    ePoint.X = (Grids[Pa.ePath[j + 1]][0] + Grids[Pa.ePath[j + 1]][2]) / 2;
                    ePoint.Y = (Grids[Pa.ePath[j + 1]][1] + Grids[Pa.ePath[j + 1]][3]) / 2;

                    IPolyline iLine = new PolylineClass();
                    iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;

                    pMapControl.DrawShape(iLine, ref PolylineSb);
                    //pMapControl.Refresh();
                }
            }
            #endregion

            #region 绘制路径
            //PolylineSb = Sb.LineSymbolization(1, 100, 100, 100, 0);
            //foreach (KeyValuePair<int, Path> kv in cFM.OrderPaths)
            //{
            //    for (int j = 0; j < kv.Value.ePath.Count - 1; j++)
            //    {
            //        IPoint sPoint = new PointClass();
            //        IPoint ePoint = new PointClass();
            //        sPoint.X = (Grids[kv.Value.ePath[j]][0] + Grids[kv.Value.ePath[j]][2]) / 2;
            //        sPoint.Y = (Grids[kv.Value.ePath[j]][1] + Grids[kv.Value.ePath[j]][3]) / 2;

            //        ePoint.X = (Grids[kv.Value.ePath[j + 1]][0] + Grids[kv.Value.ePath[j + 1]][2]) / 2;
            //        ePoint.Y = (Grids[kv.Value.ePath[j + 1]][1] + Grids[kv.Value.ePath[j + 1]][3]) / 2;

            //        IPolyline iLine = new PolylineClass();
            //        iLine.FromPoint = sPoint; iLine.ToPoint = ePoint;

            //        pMapControl.DrawShape(iLine, ref PolylineSb);
            //        //pMapControl.Refresh();
            //    }
            //}
            #endregion
            #endregion
        }

        /// <summary>
        /// 贝塞尔曲线光滑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button15_Click(object sender, EventArgs e)
        {
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试

            #region 参数
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            GridXY[0] = 20; GridXY[1] = 20;
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            #endregion

            #region 索引
            double[] ExtendValue = new double[4];
            IGeoDataset pGeoDataset = pFeatureLayer as IGeoDataset;
            IEnvelope Extend = pGeoDataset.Extent;
            ExtendValue[0] = Extend.XMin; ExtendValue[1] = Extend.YMin; ExtendValue[2] = Extend.XMax; ExtendValue[3] = Extend.YMax;
            #endregion

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网

            #region 获取相应的要素(起点和终点)
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
            #endregion

            #region 获取Grids和节点编码
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）
            //Dictionary<Tuple<int, int>, double> WeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重

            //List<Tuple<int, int>> AllGrids = new List<Tuple<int, int>>();
            //for (int i = 0; i < AllPoints.Count; i++)
            //{
            //    AllGrids.Add(NodeInGrid[AllPoints[i]]);
            //}

            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }
            #endregion

            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 8, 1);//确定整个Grids的权重

            #region 遍历构成Flow过程
            double CountLabel = 0;//进程监控
            while (desGrids.Count > 0)
            {
                CountLabel++; Console.WriteLine(CountLabel);//进程监控
                double MaxDis = 0;
                Path CachePath = null;
                double textAngle2 = 0;//测试用

                for (int i = 0; i < desGrids.Count; i++)
                {
                    //每次更新网格权重
                    Console.Write(i);

                    #region 获取到PathGrid的路径
                    double MinLength = 100000;
                    double textAngle = 0;//测试用
                    List<Tuple<int, int>> TargetPath = null;

                    int Label = 0;//标识终点到起点最短距离的节是否是起点（=1表示终点是起点）
                    for (int j = 0; j < cFM.PathGrids.Count; j++)
                    {
                        Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝

                        #region 添加Flow与节点重叠的限制约束条件
                        for (int n = 0; n < desGrids.Count; n++)
                        {
                            if (n != i)
                            {
                                WeighGrids.Remove(desGrids[n]);
                            }
                        }
                        #endregion

                        #region 获取终点到已有路径中某点的最短路径
                        PathTrace Pt = new PathTrace();
                        List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                        JudgeList.Add(desGrids[i]);
                        Pt.startPoint = cFM.PathGrids[j];
                        List<int> DirList = Pt.GetConDir(cFM.PathGrids[j], desGrids[i]);
                        Pt.MazeAlg(JudgeList, WeighGrids, 2, DirList, cFM.PathGrids[j]);//备注：每次更新以后,WeightGrid会清零                      
                        List<Tuple<int, int>> CacheShortPath = Pt.GetShortestPath(cFM.PathGrids[j], desGrids[i]);
                        
                        #region 需要考虑可能不存在路径的情况
                        double CacheShortPathLength = 0;
                        if (CacheShortPath != null)
                        {
                            CacheShortPathLength = Pt.GetPathLength(CacheShortPath);
                        }
                        else
                        {
                            CacheShortPathLength = 10000000;
                        }
                        #endregion

                        double TotalLength = 0;
                        ///起点优先(若某点到起点的最短距离为该点到起点的最短距离，则其被优先选择，即添加一个较大值！！！！)
                        //if (cFM.GridForPaths[cFM.PathGrids[j]].Length == 0)
                        //{
                        //    TotalLength = CacheShortPathLength + 100000;
                        //    //TotalLength = CacheShortPathLength * 0.1;
                        //}
                        //else
                        //{
                        TotalLength = CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65;
                        //}
                        #endregion

                        #region 添加角度约束限制
                        Tuple<int, int> FromGrid = null;
                        Tuple<int, int> MidGrid = null;
                        Tuple<int, int> ToGrid = null;
                        double CacheAngle = 0;
                        if (CacheShortPath != null && CacheShortPath.Count >= 2 && cFM.GridForPaths[cFM.PathGrids[j]].ePath.Count >= 2)
                        {
                            FromGrid = CacheShortPath[1];
                            MidGrid = CacheShortPath[0];
                            ToGrid = cFM.GridForPaths[cFM.PathGrids[j]].ePath[cFM.GridForPaths[cFM.PathGrids[j]].ePath.Count - 2];

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

                            if (CacheAngle > 3.1415927 / 3 && CacheAngle < 2 * 3.1415926 / 3)
                            {
                                TotalLength = TotalLength + 100000;
                            }
                        }
                        #endregion

                        #region 比较获取某给定节点到起点的最短路径
                        if (TotalLength < MinLength)
                        {
                            if (cFM.GridForPaths[cFM.PathGrids[j]].Length == 0 && !cFM.PathGrids.Contains(CacheShortPath[1]))//消除某些点到起点的最短路径是经过已有路径的情况
                            {
                                Label = 1;//标识最短路径终点是起点
                            }

                            else
                            {
                                Label = 0;//标识最短路径终点非起点
                            }

                            MinLength = TotalLength;
                            List<Tuple<int, int>> pCachePath = cFM.GridForPaths[cFM.PathGrids[j]].ePath.ToList();
                            CacheShortPath.RemoveAt(0);//移除第一个要素，避免存在重复元素

                            pCachePath.AddRange(CacheShortPath);
                            TargetPath = pCachePath;

                            textAngle = CacheAngle;//测试用
                        }
                        #endregion
                    }
                    #endregion

                    #region 表示起点优先限制
                    if (Label == 1)
                    {
                        MinLength = MinLength + 10000;
                    }
                    #endregion

                    #region 获取到起点路径最长终点的路径
                    if (MinLength > MaxDis)
                    {
                        MaxDis = MinLength;
                        CachePath = new Path(sGrid, desGrids[i], TargetPath);

                        textAngle2 = textAngle;//测试用
                    }
                    #endregion
                }

                #region 可视化显示（表示Path的生成过程）
                FlowDraw FD1 = new FlowDraw(pMapControl,Grids);
                FD1.FlowPathDraw(CachePath, 1, 0);
                #endregion

                //cFM.PathRefresh(CachePath, 1);
                cFM.PathRefresh(CachePath, 1, PointFlow[GridWithNode[CachePath.endPoint]]);//更新：包括子路径与流量更新
                desGrids.Remove(CachePath.endPoint);//移除一个Destination

            }
            #endregion

            #region 可视化和输出
            FlowDraw FD2 = new FlowDraw(pMapControl, Grids, AllPoints, NodeInGrid, GridWithNode, OutFilePath);
            //FD2.SmoothFlowMap(cFM.SubPaths, 2, 2000, 20, 1, 0, 1, 1, 200);
            FD2.FlowMapDraw(cFM.SubPaths, 10, 1, 1);
            #endregion
        }

        /// <summary>
        /// 优化+距离限制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button16_Click(object sender, EventArgs e)
        {
            #region OD参数
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;                    
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();       
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();           
            FMU.GetOD(pFeatureClass,OriginPoint,DesPoints,AllPoints,PointFlow);
            #endregion

            #region 获取Grids和节点编码
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            GridXY[0] = 0.8; GridXY[1] = 0.8;
            double[] ExtendValue = FMU.GetExtend(pFeatureLayer);
            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网     
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）

            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }

            List<Tuple<int, int>> CopyDesGrids = Clone((object)desGrids) as List<Tuple<int, int>>;//深拷贝
            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重(这里参数需要设置)
            #endregion
           
            #region 遍历构成Flow过程
            double CountLabel = 0;//进程监控
            while (desGrids.Count > 0)
            {
                CountLabel++; Console.WriteLine(CountLabel);//进程监控
                double MaxDis = 0;
                Path CachePath = null;

                for (int i = 0; i < desGrids.Count; i++)
                {
                    //每次更新网格权重
                    Console.Write(i);

                    #region 获取到PathGrid的路径
                    double MinLength = 100000;
                    List<Tuple<int, int>> TargetPath = null;

                    int Label = 0;//标识终点到起点最短距离的节是否是起点（=1表示终点是起点）
         
                    for (int j = 0; j < cFM.PathGrids.Count; j++)
                    {
                        Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝

                        #region 添加Flow与节点重叠的限制约束条件                   
                        FMU.FlowOverLayContraint(CopyDesGrids, WeighGrids, 0, desGrids[i]);
                        #endregion
           
                        #region 获取终点到已有路径中某点的最短路径
                        PathTrace Pt = new PathTrace();
                        List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                        JudgeList.Add(desGrids[i]);
                        Pt.startPoint = cFM.PathGrids[j];
                        List<int> DirList = Pt.GetConDir(cFM.PathGrids[j], desGrids[i]);//获取限制性约束的方向(备注：这里的方向是有优先级的)
                        Pt.MazeAlg(JudgeList, WeighGrids, 2, DirList, cFM.PathGrids[j]);//备注：每次更新以后,WeightGrid会清零                      
                        List<Tuple<int, int>> CacheShortPath = Pt.GetShortestPath(cFM.PathGrids[j], desGrids[i]);  
                        #endregion                   

                        #region 需要考虑可能不存在路径的情况
                        double CacheShortPathLength = 0;
                        if (CacheShortPath != null)
                        {
                            CacheShortPathLength = Pt.GetPathLength(CacheShortPath);
                        }
                        else
                        {
                            CacheShortPathLength = 10000000;
                        }
                        #endregion

                        #region 添加长度约束
                        //if (CacheShortPathLength < 3)
                        //{
                        //    CacheShortPathLength = 100000 + CacheShortPathLength;
                        //}
                        #endregion

                        #region 添加交叉约束
                        if (CacheShortPath != null)
                        {
                            if (FMU.IntersectPath(CacheShortPath, cFM.PathGrids))
                            {
                                CacheShortPathLength = 100000 + CacheShortPathLength;
                            }

                            //if (FMU.LineIntersectPath(CacheShortPath, cFM.PathGrids, Grids))
                            //{
                            //    CacheShortPathLength = 100000 + CacheShortPathLength;
                            //}
                        }
                        #endregion

                        #region 添加角度约束限制
                        if (CacheShortPath != null)
                        {
                            if (FMU.AngleContraint(CacheShortPath, cFM.GridForPaths[cFM.PathGrids[j]], Grids))
                            {
                                CacheShortPathLength = 100000 + CacheShortPathLength;
                            }
                        }
                        #endregion

                        double TotalLength = 0;                    
                        TotalLength = CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65;
                                             
                        #region 比较获取某给定节点到起点的最短路径
                        if (TotalLength < MinLength)
                        {
                            if (cFM.GridForPaths[cFM.PathGrids[j]].Length == 0 && !cFM.PathGrids.Contains(CacheShortPath[1]))//消除某些点到起点的最短路径是经过已有路径的情况
                            {
                                Label = 1;//标识最短路径终点是起点
                            }

                            else
                            {
                                Label = 0;//标识最短路径终点非起点
                            }

                            MinLength = TotalLength;
                            List<Tuple<int, int>> pCachePath = cFM.GridForPaths[cFM.PathGrids[j]].ePath.ToList();
                            CacheShortPath.RemoveAt(0);//移除第一个要素，避免存在重复元素

                            pCachePath.AddRange(CacheShortPath);
                            TargetPath = pCachePath;
                        }
                        #endregion
                    }
                    #endregion

                    #region 表示起点优先限制
                    if (Label == 1)
                    {
                        MinLength = MinLength + 10000;
                    }
                    #endregion

                    #region 获取到起点路径最长终点的路径
                    if (TargetPath == null)//可能存在路径为空的情况
                    {
                        MinLength = 0;
                    }

                    if (MinLength > MaxDis)
                    {
                        MaxDis = MinLength;
                        CachePath = new Path(sGrid, desGrids[i], TargetPath);
                    }
                    #endregion
                }

                #region 可视化显示（表示Path的生成过程）
                FlowDraw FD1 = new FlowDraw(pMapControl, Grids);
                FD1.FlowPathDraw(CachePath, 1, 0);
                #endregion

                #region 防止可能出现空的情况
                //cFM.PathRefresh(CachePath, 1);
                if (CachePath != null)
                {
                    cFM.PathRefresh(CachePath, 1, PointFlow[GridWithNode[CachePath.endPoint]]);//更新：包括子路径与流量更新
                    desGrids.Remove(CachePath.endPoint);//移除一个Destination
                }

                else
                {
                    desGrids.RemoveAt(0);
                }
                #endregion
            }
            #endregion

            #region 可视化和输出
            FlowDraw FD2 = new FlowDraw(pMapControl, Grids, AllPoints, NodeInGrid, GridWithNode, OutFilePath);
            //FD2.SmoothFlowMap(cFM.SubPaths, 2, 2000, 20, 1, 0, 1, 1, 200);
            FD2.FlowMapDraw(cFM.SubPaths, 10, 1, 1);
            #endregion
        }

        /// <summary>
        /// 效率优化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button17_Click(object sender, EventArgs e)
        {
            #region OD参数
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            FMU.GetOD(pFeatureClass, OriginPoint, DesPoints, AllPoints, PointFlow);
            #endregion

            #region 获取Grids和节点编码
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            //GridXY[0] = 0.8; GridXY[1] = 0.8;
            GridXY = Fs.GetXY(AllPoints, 2);
            double[] ExtendValue = FMU.GetExtend(pFeatureLayer);
            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网     
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）

            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }

            
            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重(这里参数需要设置)
            #endregion

            #region 遍历构成Flow过程
            double CountLabel = 0;//进程监控
            while (desGrids.Count > 0)
            {
                CountLabel++; Console.WriteLine(CountLabel);//进程监控
                double MaxDis = 0;
                Path CachePath = null;

                for (int i = 0; i < desGrids.Count; i++)
                {
                    //每次更新网格权重
                    Console.Write(i);

                    #region 获取到PathGrid的路径
                    double MinLength = 100000;
                    List<Tuple<int, int>> TargetPath = null;
                    int Label = 0;//标识终点到起点最短距离的节是否是起点（=1表示终点是起点）
                    Dictionary<int, PathTrace> DirPt = FMU.GetDirPt(pWeighGrids, desGrids[i], desGrids, desGrids[i]);//获取给定节点的方向探索路径

                    for (int j = 0; j < cFM.PathGrids.Count; j++)
                    {
                        #region 获取终点到已有路径中某点的最短路径
                        List<int> DirList = FMU.GetConDir(cFM.PathGrids[j], desGrids[i]);//获取限制性约束的方向                   
                        int DirID = FMU.GetNumber(DirList);  
                        #region 可能存在重合点的情况
                        if (DirID == 0)
                        {
                            break;
                        }
                        #endregion                    
                        List<Tuple<int, int>> CacheShortPath = DirPt[DirID].GetShortestPath(cFM.PathGrids[j], desGrids[i]);
                        #endregion

                        #region 需要考虑可能不存在路径的情况
                        double CacheShortPathLength = 0;
                        if (CacheShortPath != null)
                        {
                            CacheShortPathLength = FMU.GetPathLength(CacheShortPath);
                        }
                        else
                        {
                            CacheShortPathLength = 10000000;
                        }
                        #endregion

                        #region 添加长度约束
                        if (CacheShortPathLength < Math.Sqrt(1.9))
                        {
                            CacheShortPathLength = 100000 + CacheShortPathLength;
                        }
                        #endregion

                        #region 添加交叉约束
                        if (CacheShortPath != null)
                        {
                            if (FMU.IntersectPath(CacheShortPath, cFM.PathGrids))
                            {
                                CacheShortPathLength = 100000 + CacheShortPathLength;
                            }

                            //if (FMU.LineIntersectPath(CacheShortPath, cFM.PathGrids, Grids))
                            //{
                            //    CacheShortPathLength = 1000000 + CacheShortPathLength;
                            //}
                        }
                        #endregion

                        #region 添加角度约束限制
                        if (CacheShortPath != null)
                        {
                            if (FMU.AngleContraint(CacheShortPath, cFM.GridForPaths[cFM.PathGrids[j]], Grids))
                            {
                                CacheShortPathLength = 1000000 + CacheShortPathLength;
                            }
                        }
                        #endregion

                        double TotalLength = 0;
                        TotalLength = CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65;

                        #region 比较获取某给定节点到起点的最短路径
                        if (TotalLength < MinLength)
                        {
                            if (cFM.GridForPaths[cFM.PathGrids[j]].Length == 0 && !cFM.PathGrids.Contains(CacheShortPath[1]))//消除某些点到起点的最短路径是经过已有路径的情况
                            {
                                Label = 1;//标识最短路径终点是起点
                            }

                            else
                            {
                                Label = 0;//标识最短路径终点非起点
                            }

                            MinLength = TotalLength;
                            List<Tuple<int, int>> pCachePath = cFM.GridForPaths[cFM.PathGrids[j]].ePath.ToList();
                            CacheShortPath.RemoveAt(0);//移除第一个要素，避免存在重复元素

                            pCachePath.AddRange(CacheShortPath);
                            TargetPath = pCachePath;
                        }
                        #endregion
                    }
                    #endregion

                    #region 表示起点优先限制
                    if (Label == 1)
                    {
                        MinLength = MinLength + 10000;
                    }
                    #endregion

                    #region 获取到起点路径最长终点的路径
                    if (TargetPath == null)//可能存在路径为空的情况
                    {
                        MinLength = 0;
                    }

                    if (MinLength > MaxDis)
                    {
                        MaxDis = MinLength;
                        CachePath = new Path(sGrid, desGrids[i], TargetPath);
                    }
                    #endregion
                }

                #region 可视化显示（表示Path的生成过程）
                FlowDraw FD1 = new FlowDraw(pMapControl, Grids);
                FD1.FlowPathDraw(CachePath, 1, 0);
                #endregion

                #region 防止可能出现空的情况
                //cFM.PathRefresh(CachePath, 1);
                if (CachePath != null)
                {
                    cFM.PathRefresh(CachePath, 1, PointFlow[GridWithNode[CachePath.endPoint]]);//更新：包括子路径与流量更新
                    desGrids.Remove(CachePath.endPoint);//移除一个Destination
                }

                else
                {
                    desGrids.RemoveAt(0);
                }
                #endregion
            }
            #endregion

            #region 可视化和输出
            FlowDraw FD2 = new FlowDraw(pMapControl, Grids, AllPoints, NodeInGrid, GridWithNode, OutFilePath);
            //FD2.SmoothFlowMap(cFM.SubPaths, 2, 2000, 20, 1, 0, 1, 1, 200);
            FD2.FlowMapDraw(cFM.SubPaths, 15, 2000, 20, 1, 1, 1);
            #endregion
        }

        /// <summary>
        /// 输出Grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button18_Click(object sender, EventArgs e)
        {
            #region OD参数
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureLayer CacheLayer = pFeatureHandle.GetLayer(pMap, this.comboBox3.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            FMU.GetOD(pFeatureClass, OriginPoint, DesPoints, AllPoints, PointFlow);
            #endregion

            #region 获取Grids和节点编码
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            //GridXY[0] = 0.8; GridXY[1] = 0.8;
            GridXY = Fs.GetXY(AllPoints, 2);
            double[] ExtendValue = FMU.GetExtend(pFeatureLayer);

            //List<IFeatureLayer> LayerList = new List<IFeatureLayer>();
            //LayerList.Add(CacheLayer);
            //List<Tuple<IGeometry, esriGeometryType>> Features = FMU.GetFeatures(LayerList);
            //Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGridConObstacle(ExtendValue,GridXY,Features, ref colNum, ref rowNum);//构建格网

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网

            //List<IFeatureLayer> LayerList = new List<IFeatureLayer>();
            //LayerList.Add(CacheLayer);
            //List<Tuple<IGeometry, esriGeometryType>> Features = FMU.GetFeatures(LayerList);
            //Dictionary<Tuple<int, int>, int> GridType = Fs.GetGridType(Grids, Features, 0.8);
            #endregion

            #region 输出网格
            SMap OutMap = new SMap();
            foreach (KeyValuePair<Tuple<int, int>, List<double>> Kv in Grids)
            {
                List<TriNode> NodeList = new List<TriNode>();

                TriNode Node1 = new TriNode();
                Node1.X = Kv.Value[0];
                Node1.Y = Kv.Value[1];

                TriNode Node2 = new TriNode();
                Node2.X = Kv.Value[2];
                Node2.Y = Kv.Value[1];

                TriNode Node3 = new TriNode();
                Node3.X = Kv.Value[2];
                Node3.Y = Kv.Value[3];

                TriNode Node4 = new TriNode();
                Node4.X = Kv.Value[0];
                Node4.Y = Kv.Value[3];

                NodeList.Add(Node1); NodeList.Add(Node2); NodeList.Add(Node3); NodeList.Add(Node4);

                TriNode MidNode = new TriNode();
                MidNode.X = (Kv.Value[0] + Kv.Value[2]) / 2;
                MidNode.Y = (Kv.Value[1] + Kv.Value[3]) / 2;
                PointObject CachePoint = new PointObject(0,MidNode);

                PolygonObject CachePo = new PolygonObject(0,NodeList);
                OutMap.PolygonList.Add(CachePo);
                OutMap.PointList.Add(CachePoint);
            }
            #endregion

            OutMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
        }

        /// <summary>
        /// 贝塞尔曲线光滑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button19_Click(object sender, EventArgs e)
        {
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            SMap OutMap = new SMap();

            IFeatureCursor pFeatureCursor = pFeatureClass.Update(null, true);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                #region 添加控制点
                double Volume = Pu.GetValue(pFeature, "Volume");
                IPointCollection PointSet = pFeature.Shape as IPointCollection;
                List<IPoint> ControlPoints = new List<IPoint>();

                for (int i = 0; i < PointSet.PointCount; i++)
                {
                    IPoint Po = PointSet.get_Point(i);
                    ControlPoints.Add(Po);
                }
                #endregion

                #region 贝塞尔曲线
                BezierCurve BC = new BezierCurve(ControlPoints);
                BC.CurveNGenerate(100, 0.1);
                #endregion

                List<TriNode> LinePoints = new List<TriNode>();//输出用
                for (int i = 0; i < BC.CurvePoint.Count - 1; i++)
                {
                    IPolyline iLine = new PolylineClass();
                    iLine.FromPoint = BC.CurvePoint[i];
                    iLine.ToPoint = BC.CurvePoint[i + 1];

                    #region 输出需要
                    TriNode pNode = new TriNode(BC.CurvePoint[i].X, BC.CurvePoint[i].Y);//输出用
                    LinePoints.Add(pNode);

                    if (i == BC.CurvePoint.Count - 2)//输出用
                    {
                        TriNode nNode = new TriNode(BC.CurvePoint[i + 1].X, BC.CurvePoint[i + 1].Y);
                        LinePoints.Add(nNode);
                    }             
                    #endregion 
                }

                PolylineObject CacheLine = new PolylineObject(LinePoints);
                CacheLine.Volume = Volume;
                OutMap.PolylineList.Add(CacheLine);

                pFeature = pFeatureCursor.NextFeature();
            }

            OutMap.WriteResult2Shp(OutFilePath, pMap.SpatialReference);
        }

        /// <summary>
        /// 考虑阻隔
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button20_Click(object sender, EventArgs e)
        {
            #region OD参数
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureLayer CacheLayer = pFeatureHandle.GetLayer(pMap, this.comboBox3.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            FMU.GetOD(pFeatureClass, OriginPoint, DesPoints, AllPoints, PointFlow);
            #endregion

            #region 获取Grids和节点编码
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            //GridXY[0] = 0.8; GridXY[1] = 0.8;
            GridXY = Fs.GetXY(AllPoints, 2);
            double[] ExtendValue = FMU.GetExtend(pFeatureLayer);
            List<IFeatureLayer> LayerList = new List<IFeatureLayer>();
            LayerList.Add(CacheLayer);
            List<Tuple<IGeometry, esriGeometryType>> Features = FMU.GetFeatures(LayerList);
            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGridConObstacle(ExtendValue, GridXY, Features, ref colNum, ref rowNum,0);//构建格网

            //Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网     
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）

            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }

            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重(这里参数需要设置)

            ///加快搜索，提前将所有探索方向全部提前计算（实际上应该是需要时才计算，这里可能导致后续计算存在重叠问题，在计算过程中解决即可）
            Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> DesDirPt = FMU.GetDesDirPt(pWeighGrids, desGrids);//获取给定节点的方向探索路径
            //Dictionary<Tuple<int, int>, double> DesDis = FMU.GetDisOrder(desGrids, sGrid);
            #endregion

            #region 遍历构成Flow过程
            double CountLabel = 0;//进程监控
            while (desGrids.Count > 0)
            {
                CountLabel++; Console.WriteLine(CountLabel);//进程监控
                double MaxDis = 0;
                Path CachePath = null;
                List<Tuple<int, int>> TestTargetPath = null;//测试用
                double TestShort = 0;//测试用

                for (int i = 0; i < desGrids.Count; i++)
                {
                    //每次更新网格权重
                    Console.Write(i);
                    double MinDis = FMU.GetMinDis(desGrids[i], cFM.PathGrids);
                    //List<double> DisList = DesDis.Values.ToList(); DisList.Sort();//升序排列
                    //double Order = DisList.IndexOf(DesDis[desGrids[i]]) / DisList.Count;

                    #region 获取到PathGrid的路径
                    double MinLength = 100000;
                    List<Tuple<int, int>> TargetPath = null;
                    int Label = 0;//标识终点到起点最短距离的节是否是起点（=1表示终点是起点）
                    Dictionary<int, PathTrace> DirPt = DesDirPt[desGrids[i]];

                    for (int j = 0; j < cFM.PathGrids.Count; j++)
                    {
                        #region 获取终点到已有路径中某点的最短路径
                        List<int> DirList = FMU.GetConDirR(cFM.PathGrids[j], desGrids[i]);//获取限制性约束的方向                   
                        int DirID = FMU.GetNumber(DirList);

                        #region 可能存在重合点的情况
                        if (DirID == 0)
                        {
                            break;
                        }
                        #endregion

                        List<Tuple<int, int>> CacheShortPath = DirPt[DirID].GetShortestPath(cFM.PathGrids[j], desGrids[i]);
                        #endregion

                        #region 需要考虑可能不存在路径的情况
                        double CacheShortPathLength = 0;
                        if (CacheShortPath != null)
                        {
                            CacheShortPathLength = FMU.GetPathLength(CacheShortPath);
                        }
                        else
                        {
                            CacheShortPathLength = 10000000;
                        }
                        #endregion

                        #region 添加交叉约束
                        if (CacheShortPath != null)
                        {
                            #region 存在交叉
                            if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids)==2)
                            {
                                CacheShortPathLength = 100000 + CacheShortPathLength;
                            }
                            #endregion

                            #region 存在重叠
                            else if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 1)
                            {                             
                                //if (MinDis < Math.Sqrt(1.9) && (CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65) > MaxDis)///如果长度小于该值才需要判断；否则无须判断
                                if (MinDis < Math.Sqrt(1.9))
                                {
                                    #region 考虑到搜索方向固定可能导致的重合
                                    Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝
                                    FMU.FlowCrosssingContraint(desGrids, WeighGrids, 0, desGrids[i], cFM.PathGrids[j], cFM.PathGrids);//Overlay约束
                                    PathTrace Pt = new PathTrace();
                                    List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                                    JudgeList.Add(desGrids[i]);//添加搜索的起点
                                    Pt.MazeAlg(JudgeList, WeighGrids, 1, DirList);//备注：每次更新以后,WeightGrid会清零  
                                    CacheShortPath = Pt.GetShortestPath(cFM.PathGrids[j], desGrids[i]);

                                    if (CacheShortPath != null)
                                    {
                                        CacheShortPathLength = FMU.GetPathLength(CacheShortPath);

                                        #region 判段交叉
                                        if (FMU.IntersectPath(CacheShortPath, cFM.PathGrids))
                                        {
                                            CacheShortPathLength = 100000 + CacheShortPathLength;
                                        }
                                        #endregion
                                    }
                                    else
                                    {
                                        CacheShortPathLength = 10000000;
                                    }
                                    #endregion
                                }

                                ///否则，加上一个极大值
                                else
                                {
                                    CacheShortPathLength = 100000 + CacheShortPathLength;
                                }
                            }
                            #endregion

                            //if (FMU.IntersectPath(CacheShortPath, cFM.PathGrids))
                            //{
                            //    CacheShortPathLength = 100000 + CacheShortPathLength;
                            //}

                            //if (FMU.LineIntersectPath(CacheShortPath, cFM.PathGrids, Grids))
                            //{
                            //    CacheShortPathLength = 1000000 + CacheShortPathLength;
                            //}

                            //if (FMU.obstacleIntersectPath(CacheShortPath, Features, Grids))
                            //{
                            //    CacheShortPathLength = 1000000 + CacheShortPathLength;
                            //}

                        }
                        #endregion 

                        #region 添加角度约束限制
                        if (CacheShortPath != null)
                        {
                            if (FMU.AngleContraint(CacheShortPath, cFM.GridForPaths[cFM.PathGrids[j]], Grids))
                            {
                                CacheShortPathLength = 1000000 + CacheShortPathLength;
                            }
                        }
                        #endregion

                        #region 添加长度约束
                        if (CacheShortPathLength < Math.Sqrt(1.9))
                        {                         
                            CacheShortPathLength = 100000 + CacheShortPathLength;
                        }
                        #endregion
                   
                        double TotalLength = 0;
                        TotalLength = CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65;

                        #region 比较获取某给定节点到起点的最短路径
                        if (TotalLength < MinLength)
                        {
                            if (cFM.GridForPaths[cFM.PathGrids[j]].Length == 0 && !cFM.PathGrids.Contains(CacheShortPath[1]))//消除某些点到起点的最短路径是经过已有路径的情况
                            {
                                Label = 1;//标识最短路径终点是起点
                            }

                            else
                            {
                                Label = 0;//标识最短路径终点非起点
                            }

                            MinLength = TotalLength;
                            List<Tuple<int, int>> pCachePath = cFM.GridForPaths[cFM.PathGrids[j]].ePath.ToList();
                            CacheShortPath.RemoveAt(0);//移除第一个要素，避免存在重复元素

                            pCachePath.AddRange(CacheShortPath);
                            TargetPath = pCachePath;

                            TestTargetPath = CacheShortPath;//测试用
                            TestShort = CacheShortPathLength;//测试用
                        }
                        #endregion
                    }
                    #endregion

                    #region 表示起点优先限制
                    if (Label == 1)
                    {
                        MinLength = MinLength + 10000;
                    }
                    #endregion

                    #region 获取到起点路径最长终点的路径
                    if (TargetPath == null)//可能存在路径为空的情况
                    {
                        MinLength = 0;
                    }

                    if (MinLength > MaxDis)
                    {
                        MaxDis = MinLength;
                        CachePath = new Path(sGrid, desGrids[i], TargetPath);

                        //double Test = FMU.GetPathLength(TestTargetPath);//测试用
                        //int TesTloc = 0;
                    }
                    #endregion
                }

                #region 可视化显示（表示Path的生成过程）
                FlowDraw FD1 = new FlowDraw(pMapControl, Grids);
                FD1.FlowPathDraw(CachePath, 1, 0);
                #endregion

                #region 防止可能出现空的情况
                //cFM.PathRefresh(CachePath, 1);
                if (CachePath != null)
                {
                    cFM.PathRefresh(CachePath, 1, PointFlow[GridWithNode[CachePath.endPoint]]);//更新：包括子路径与流量更新
                    desGrids.Remove(CachePath.endPoint);//移除一个Destination
                    //DesDis.Remove(CachePath.endPoint);
                }

                else
                {
                    desGrids.RemoveAt(0);
                }
                #endregion
            }
            #endregion

            #region 可视化和输出
            FlowDraw FD2 = new FlowDraw(pMapControl, Grids, AllPoints, NodeInGrid, GridWithNode, OutFilePath);
            //FD2.SmoothFlowMap(cFM.SubPaths, 2, 2000, 20, 1, 0, 1, 1, 200);
            FD2.FlowMapDraw(cFM.SubPaths, 15, 2000, 20, 1, 1, 1);
            #endregion
        }

        /// <summary>
        /// 效率优化2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button21_Click(object sender, EventArgs e)
        {
            #region OD参数
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            //IFeatureLayer CacheLayer = pFeatureHandle.GetLayer(pMap, this.comboBox3.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            FMU.GetOD(pFeatureClass, OriginPoint, DesPoints, AllPoints, PointFlow);
            #endregion

            #region 获取Grids和节点编码
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            //GridXY[0] = 0.8; GridXY[1] = 0.8;
            GridXY = Fs.GetXY(AllPoints, 2);
            double[] ExtendValue = FMU.GetExtend(pFeatureLayer);
            //List<IFeatureLayer> LayerList = new List<IFeatureLayer>();
            //LayerList.Add(CacheLayer);
            //List<Tuple<IGeometry, esriGeometryType>> Features = FMU.GetFeatures(LayerList);
            //Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGridConObstacle(ExtendValue, GridXY, Features, ref colNum, ref rowNum, 0);//构建格网

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网     
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）

            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }

            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重(这里参数需要设置)
            //Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 0, 1);//确定整个Grids的权重(这里参数需要设置)

            ///加快搜索，提前将所有探索方向全部提前计算（实际上应该是需要时才计算，这里可能导致后续计算存在重叠问题，在计算过程中解决即可）
            Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> DesDirPt = FMU.GetDesDirPt(pWeighGrids, desGrids);//获取给定节点的方向探索路径
            //Dictionary<Tuple<int, int>, double> DesDis = FMU.GetDisOrder(desGrids, sGrid);
            #endregion

            #region 遍历构成Flow过程
            double CountLabel = 0;//进程监控
            while (desGrids.Count > 0)
            {
                CountLabel++; Console.WriteLine(CountLabel);//进程监控
                double MaxDis = 0;
                Path CachePath = null;
                List<Tuple<int, int>> TestTargetPath = null;//测试用
                double TestShort = 0;//测试用

                for (int i = 0; i < desGrids.Count; i++)
                {
                    //每次更新网格权重
                    Console.Write(i);
                    double MinDis = FMU.GetMinDis(desGrids[i], cFM.PathGrids);
                    //List<double> DisList = DesDis.Values.ToList(); DisList.Sort();//升序排列
                    //double Order = DisList.IndexOf(DesDis[desGrids[i]]) / DisList.Count;

                    #region 获取到PathGrid的路径
                    double MinLength = 100000;
                    List<Tuple<int, int>> TargetPath = null;
                    int Label = 0;//标识终点到起点最短距离的节是否是起点（=1表示终点是起点）
                    Dictionary<int, PathTrace> DirPt = DesDirPt[desGrids[i]];

                    for (int j = 0; j < cFM.PathGrids.Count; j++)
                    {
                        #region 优化搜索速度（只搜索制定范围内的节点）
                        //if (FMU.RLJudgeGrid(desGrids[i], cFM.PathGrids[j], sGrid))
                        //{
                        #region 获取终点到已有路径中某点的最短路径

                        #region 不考虑方向限制
                        //List<int> DirList = new List<int>();
                        
                        //DirList.Add(1);
                        //DirList.Add(2);
                        //DirList.Add(3);
                        //DirList.Add(4);
                        //DirList.Add(5);
                        //DirList.Add(6);
                        //DirList.Add(7);
                        //DirList.Add(8);
                        #endregion

                        List<int> DirList = FMU.GetConDirR(cFM.PathGrids[j], desGrids[i]);//获取限制性约束的方向                   
                        int DirID = FMU.GetNumber(DirList);

                        #region 可能存在重合点的情况
                        if (DirID == 0)
                        {
                            break;
                        }
                        #endregion

                        List<Tuple<int, int>> CacheShortPath = DirPt[DirID].GetShortestPath(cFM.PathGrids[j], desGrids[i]);
                        #endregion

                        #region 需要考虑可能不存在路径的情况
                        double CacheShortPathLength = 0;
                        if (CacheShortPath != null)
                        {
                            CacheShortPathLength = FMU.GetPathLength(CacheShortPath);
                        }
                        else
                        {
                            CacheShortPathLength = 10000000;
                        }
                        #endregion

                        #region 添加交叉约束
                        if (CacheShortPath != null)
                        {
                            #region 存在交叉
                            if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 2)//这里的相交修改了
                            {
                                CacheShortPathLength = 1000000 + CacheShortPathLength;//交叉惩罚系数更高
                            }
                            #endregion

                            #region 存在重叠
                            else if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 1)
                            {
                                //if (MinDis < Math.Sqrt(1.9) && (CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65) > MaxDis)///如果长度小于该值才需要判断；否则无须判断
                                if (MinDis < Math.Sqrt(1.9))
                                //if (MinDis < 2.9)
                                {
                                    #region 考虑到搜索方向固定可能导致的重合
                                    Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝
                                    FMU.FlowCrosssingContraint(desGrids, WeighGrids, 0, desGrids[i], cFM.PathGrids[j], cFM.PathGrids);//Overlay约束
                                    PathTrace Pt = new PathTrace();
                                    List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                                    JudgeList.Add(desGrids[i]);//添加搜索的起点
                                    Pt.MazeAlg(JudgeList, WeighGrids, 1, DirList);//备注：每次更新以后,WeightGrid会清零  
                                    CacheShortPath = Pt.GetShortestPath(cFM.PathGrids[j], desGrids[i]);

                                    if (CacheShortPath != null)
                                    {
                                        CacheShortPathLength = FMU.GetPathLength(CacheShortPath);

                                        #region 判段交叉
                                        if (FMU.IntersectPath(CacheShortPath, cFM.PathGrids))
                                        {
                                            CacheShortPathLength = 1000000 + CacheShortPathLength;//交叉惩罚系数更高
                                        }
                                        #endregion
                                    }
                                    else
                                    {
                                        CacheShortPathLength = 10000000;
                                    }
                                    #endregion
                                }

                                ///否则，加上一个极大值
                                //else
                                //{
                                //    CacheShortPathLength = 100000 + CacheShortPathLength;
                                //}
                            }
                            #endregion

                            //if (FMU.IntersectPath(CacheShortPath, cFM.PathGrids))
                            //{
                            //    CacheShortPathLength = 100000 + CacheShortPathLength;
                            //}

                            //if (FMU.LineIntersectPath(CacheShortPath, cFM.PathGrids, Grids))
                            //{
                            //    CacheShortPathLength = 1000000 + CacheShortPathLength;
                            //}

                            //if (FMU.obstacleIntersectPath(CacheShortPath, Features, Grids))
                            //{
                            //    CacheShortPathLength = 1000000 + CacheShortPathLength;
                            //}

                        }
                        #endregion

                        #region 添加角度约束限制
                        if (CacheShortPath != null)
                        {
                            if (FMU.AngleContraint(CacheShortPath, cFM.GridForPaths[cFM.PathGrids[j]], Grids))
                            {
                                CacheShortPathLength = 15 + CacheShortPathLength;
                            }
                        }
                        #endregion

                        #region 添加长度约束
                        if (CacheShortPathLength < Math.Sqrt(1.9))
                        //if (CacheShortPathLength < 2.9)
                        {
                            CacheShortPathLength = 15 + CacheShortPathLength;
                        }
                        #endregion

                        double TotalLength = 0;
                        TotalLength = CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65;

                        #region 比较获取某给定节点到起点的最短路径
                        if (TotalLength < MinLength)
                        {
                            if (cFM.GridForPaths[cFM.PathGrids[j]].Length == 0 && !cFM.PathGrids.Contains(CacheShortPath[1]))//消除某些点到起点的最短路径是经过已有路径的情况
                            {
                                Label = 1;//标识最短路径终点是起点
                            }

                            else
                            {
                                Label = 0;//标识最短路径终点非起点
                            }

                            MinLength = TotalLength;
                            List<Tuple<int, int>> pCachePath = cFM.GridForPaths[cFM.PathGrids[j]].ePath.ToList();
                            CacheShortPath.RemoveAt(0);//移除第一个要素，避免存在重复元素

                            pCachePath.AddRange(CacheShortPath);
                            TargetPath = pCachePath;

                            TestTargetPath = CacheShortPath;//测试用
                            TestShort = CacheShortPathLength;//测试用
                        }
                        #endregion
                    }
                        #endregion
                    //}
                    #endregion

                    #region 表示起点优先限制
                    if (Label == 1)
                    {
                        //MinLength = MinLength + 10000;
                    }
                    #endregion

                    #region 获取到起点路径最长终点的路径
                    if (TargetPath == null)//可能存在路径为空的情况
                    {
                        MinLength = 0;
                    }

                    if (MinLength > MaxDis)
                    {
                        MaxDis = MinLength;
                        CachePath = new Path(sGrid, desGrids[i], TargetPath);

                        //double Test = FMU.GetPathLength(TestTargetPath);//测试用
                        //int TesTloc = 0;
                    }
                    #endregion
                }

                #region 可视化显示（表示Path的生成过程）
                FlowDraw FD1 = new FlowDraw(pMapControl, Grids);
                FD1.FlowPathDraw(CachePath, 1, 0);
                #endregion

                #region 防止可能出现空的情况
                //cFM.PathRefresh(CachePath, 1);
                if (CachePath != null)
                {
                    cFM.PathRefresh(CachePath, 1, PointFlow[GridWithNode[CachePath.endPoint]]);//更新：包括子路径与流量更新
                    desGrids.Remove(CachePath.endPoint);//移除一个Destination
                    //DesDis.Remove(CachePath.endPoint);
                }

                else
                {
                    desGrids.RemoveAt(0);
                }
                #endregion
            }
            #endregion

            #region 可视化和输出
            FlowDraw FD2 = new FlowDraw(pMapControl, Grids, AllPoints, NodeInGrid, GridWithNode, OutFilePath);
            FD2.SmoothFlowMap(cFM.SubPaths, 2, 2000, 20, 1, 0, 1, 0, 200);
            FD2.FlowMapDraw(cFM.SubPaths, 15, 2000, 20, 1, 1, 1);
            #endregion
        }

        /// <summary>
        /// 考虑阻隔优化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button22_Click(object sender, EventArgs e)
        {
            #region OD参数
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureLayer CacheLayer = pFeatureHandle.GetLayer(pMap, this.comboBox3.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            FMU.GetOD(pFeatureClass, OriginPoint, DesPoints, AllPoints, PointFlow);
            #endregion

            #region 获取Grids和节点编码
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            //GridXY[0] = 0.8; GridXY[1] = 0.8;
            GridXY = Fs.GetXY(AllPoints, 2);
            double[] ExtendValue = FMU.GetExtend(pFeatureLayer);
            List<IFeatureLayer> LayerList = new List<IFeatureLayer>();
            LayerList.Add(CacheLayer);
            List<Tuple<IGeometry, esriGeometryType>> Features = FMU.GetFeatures(LayerList);
            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGridConObstacle(ExtendValue, GridXY, Features, ref colNum, ref rowNum, 0);//构建格网

            //Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网     
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）

            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }

            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重(这里参数需要设置)

            ///加快搜索，提前将所有探索方向全部提前计算（实际上应该是需要时才计算，这里可能导致后续计算存在重叠问题，在计算过程中解决即可）
            Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> DesDirPt = FMU.GetDesDirPt(pWeighGrids, desGrids);//获取给定节点的方向探索路径
            //Dictionary<Tuple<int, int>, double> DesDis = FMU.GetDisOrder(desGrids, sGrid);
            #endregion

            #region 遍历构成Flow过程
            double CountLabel = 0;//进程监控
            while (desGrids.Count > 0)
            {
                CountLabel++; Console.WriteLine(CountLabel);//进程监控
                double MaxDis = 0;
                Path CachePath = null;
                List<Tuple<int, int>> TestTargetPath = null;//测试用
                double TestShort = 0;//测试用

                for (int i = 0; i < desGrids.Count; i++)
                {               
                    //每次更新网格权重
                    Console.Write(i);
                    double MinDis = FMU.GetMinDis(desGrids[i], cFM.PathGrids);
                    //List<double> DisList = DesDis.Values.ToList(); DisList.Sort();//升序排列
                    //double Order = DisList.IndexOf(DesDis[desGrids[i]]) / DisList.Count;

                    #region 获取到PathGrid的路径
                    double MinLength = 100000;
                    List<Tuple<int, int>> TargetPath = null;
                    int Label = 0;//标识终点到起点最短距离的节是否是起点（=1表示终点是起点）
                    Dictionary<int, PathTrace> DirPt = DesDirPt[desGrids[i]];

                    for (int j = 0; j < cFM.PathGrids.Count; j++)
                    {
                        #region 优化搜索速度（只搜索制定范围内的节点）
                        if (FMU.RLJudgeGrid(desGrids[i], cFM.PathGrids[j], sGrid))
                        {
                            #region 获取终点到已有路径中某点的最短路径
                            List<int> DirList = FMU.GetConDirR(cFM.PathGrids[j], desGrids[i]);//获取限制性约束的方向                   
                            int DirID = FMU.GetNumber(DirList);

                            #region 可能存在重合点的情况
                            if (DirID == 0)
                            {
                                break;
                            }
                            #endregion

                            List<Tuple<int, int>> CacheShortPath = DirPt[DirID].GetShortestPath(cFM.PathGrids[j], desGrids[i]);
                            #endregion

                            #region 需要考虑可能不存在路径的情况
                            double CacheShortPathLength = 0;
                            if (CacheShortPath != null)
                            {
                                CacheShortPathLength = FMU.GetPathLength(CacheShortPath);
                            }
                            else
                            {
                                CacheShortPathLength = 10000000;
                            }
                            #endregion

                            #region 添加交叉约束
                            if (CacheShortPath != null)
                            {
                                #region 存在交叉
                                if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 2)
                                {
                                    CacheShortPathLength = 100000 + CacheShortPathLength;
                                }
                                #endregion

                                #region 存在重叠
                                else if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 1)
                                {
                                    //if (MinDis < Math.Sqrt(1.9) && (CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65) > MaxDis)///如果长度小于该值才需要判断；否则无须判断
                                    if (MinDis < Math.Sqrt(1.9))
                                    {
                                        #region 考虑到搜索方向固定可能导致的重合
                                        Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝
                                        FMU.FlowCrosssingContraint(desGrids, WeighGrids, 0, desGrids[i], cFM.PathGrids[j], cFM.PathGrids);//Overlay约束
                                        PathTrace Pt = new PathTrace();
                                        List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                                        JudgeList.Add(desGrids[i]);//添加搜索的起点
                                        Pt.MazeAlg(JudgeList, WeighGrids, 1, DirList);//备注：每次更新以后,WeightGrid会清零  
                                        CacheShortPath = Pt.GetShortestPath(cFM.PathGrids[j], desGrids[i]);

                                        if (CacheShortPath != null)
                                        {
                                            CacheShortPathLength = FMU.GetPathLength(CacheShortPath);

                                            #region 判段交叉
                                            if (FMU.IntersectPath(CacheShortPath, cFM.PathGrids))
                                            {
                                                CacheShortPathLength = 100000 + CacheShortPathLength;
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            CacheShortPathLength = 10000000;
                                        }
                                        #endregion
                                    }

                                    ///否则，加上一个极大值
                                    else
                                    {
                                        CacheShortPathLength = 100000 + CacheShortPathLength;
                                    }
                                }
                                #endregion

                                //if (FMU.IntersectPath(CacheShortPath, cFM.PathGrids))
                                //{
                                //    CacheShortPathLength = 100000 + CacheShortPathLength;
                                //}

                                //if (FMU.LineIntersectPath(CacheShortPath, cFM.PathGrids, Grids))
                                //{
                                //    CacheShortPathLength = 1000000 + CacheShortPathLength;
                                //}

                                //if (FMU.obstacleIntersectPath(CacheShortPath, Features, Grids))
                                //{
                                //    CacheShortPathLength = 1000000 + CacheShortPathLength;
                                //}

                            }
                            #endregion

                            #region 添加角度约束限制
                            if (CacheShortPath != null)
                            {
                                if (FMU.AngleContraint(CacheShortPath, cFM.GridForPaths[cFM.PathGrids[j]], Grids))
                                {
                                    CacheShortPathLength = 10 + CacheShortPathLength;
                                }
                            }
                            #endregion

                            #region 添加长度约束
                            if (CacheShortPathLength < Math.Sqrt(1.9))
                            {
                                CacheShortPathLength = 10 + CacheShortPathLength;
                            }
                            #endregion

                            double TotalLength = 0;
                            TotalLength = CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65;

                            #region 比较获取某给定节点到起点的最短路径
                            if (TotalLength < MinLength)
                            {
                                if (cFM.GridForPaths[cFM.PathGrids[j]].Length == 0 && !cFM.PathGrids.Contains(CacheShortPath[1]))//消除某些点到起点的最短路径是经过已有路径的情况
                                {
                                    Label = 1;//标识最短路径终点是起点
                                }

                                else
                                {
                                    Label = 0;//标识最短路径终点非起点
                                }

                                MinLength = TotalLength;
                                List<Tuple<int, int>> pCachePath = cFM.GridForPaths[cFM.PathGrids[j]].ePath.ToList();
                                CacheShortPath.RemoveAt(0);//移除第一个要素，避免存在重复元素

                                pCachePath.AddRange(CacheShortPath);
                                TargetPath = pCachePath;

                                TestTargetPath = CacheShortPath;//测试用
                                TestShort = CacheShortPathLength;//测试用
                            }
                            #endregion
                        }
                        #endregion
                    }
                    #endregion

                    #region 表示起点优先限制
                    if (Label == 1)
                    {
                        MinLength = MinLength + 10000;
                    }
                    #endregion

                    #region 获取到起点路径最长终点的路径
                    if (TargetPath == null)//可能存在路径为空的情况
                    {
                        MinLength = 0;
                    }

                    if (MinLength > MaxDis)
                    {
                        MaxDis = MinLength;
                        CachePath = new Path(sGrid, desGrids[i], TargetPath);

                        //double Test = FMU.GetPathLength(TestTargetPath);//测试用
                        //int TesTloc = 0;
                    }
                    #endregion
                }

                #region 可视化显示（表示Path的生成过程）
                FlowDraw FD1 = new FlowDraw(pMapControl, Grids);
                FD1.FlowPathDraw(CachePath, 1, 0);
                #endregion

                #region 防止可能出现空的情况
                //cFM.PathRefresh(CachePath, 1);
                if (CachePath != null)
                {
                    cFM.PathRefresh(CachePath, 1, PointFlow[GridWithNode[CachePath.endPoint]]);//更新：包括子路径与流量更新
                    desGrids.Remove(CachePath.endPoint);//移除一个Destination
                    //DesDis.Remove(CachePath.endPoint);
                }

                else
                {
                    desGrids.RemoveAt(0);
                }
                #endregion
            }
            #endregion

            #region 可视化和输出
            FlowDraw FD2 = new FlowDraw(pMapControl, Grids, AllPoints, NodeInGrid, GridWithNode, OutFilePath);
            //FD2.SmoothFlowMap(cFM.SubPaths, 2, 2000, 20, 1, 0, 1, 1, 200);
            FD2.FlowMapDraw(cFM.SubPaths, 15, 2000, 20, 1, 1, 1);
            #endregion
        }

        /// <summary>
        /// 考虑空间异质性
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button23_Click(object sender, EventArgs e)
        {
            #region OD参数
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureLayer CacheLayer = pFeatureHandle.GetLayer(pMap, this.comboBox3.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            IPoint OriginPoint = new PointClass();
            List<IPoint> DesPoints = new List<IPoint>();
            List<IPoint> AllPoints = new List<IPoint>();
            Dictionary<IPoint, double> PointFlow = new Dictionary<IPoint, double>();
            FMU.GetOD(pFeatureClass, OriginPoint, DesPoints, AllPoints, PointFlow);
            #endregion

            #region 获取Grids和节点编码
            int rowNum = 0; int colNum = 0;
            double[] GridXY = new double[2];
            //GridXY[0] = 0.8; GridXY[1] = 0.8;
            GridXY = Fs.GetXY(AllPoints, 2);
            double[] ExtendValue = FMU.GetExtend(pFeatureLayer);
            List<IFeatureLayer> LayerList = new List<IFeatureLayer>();
            LayerList.Add(CacheLayer);
            List<Tuple<IGeometry, esriGeometryType>> Features = FMU.GetFeatures(LayerList);
            //Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGridConObstacle(ExtendValue, GridXY, Features, ref colNum, ref rowNum, 0);//构建格网

            Dictionary<Tuple<int, int>, List<double>> Grids = Fs.GetGrid(ExtendValue, GridXY, ref colNum, ref rowNum);//构建格网
            Dictionary<Tuple<int, int>, int> GridType = Fs.GetGridType(Grids, Features, 0.8);
            Dictionary<IPoint, Tuple<int, int>> NodeInGrid = Fs.GetNodeInGrid(Grids, AllPoints);//获取点对应的格网
            Dictionary<Tuple<int, int>, IPoint> GridWithNode = Fs.GetGridContainNodes(Grids, AllPoints);//获取格网中的点（每个格网最多对应一个点）

            Tuple<int, int> sGrid = NodeInGrid[OriginPoint];//起点格网编码
            List<Tuple<int, int>> desGrids = new List<Tuple<int, int>>();//终点格网编码
            for (int i = 0; i < DesPoints.Count; i++)
            {
                desGrids.Add(NodeInGrid[DesPoints[i]]);
            }

            cFlowMap cFM = new cFlowMap(sGrid, desGrids, PointFlow);
            Dictionary<Tuple<int, int>, double> pWeighGrids = Fs.GetWeighGrid(Grids, GridWithNode, PointFlow, 4, 1);//确定整个Grids的权重(这里参数需要设置)

            ///加快搜索，提前将所有探索方向全部提前计算（实际上应该是需要时才计算，这里可能导致后续计算存在重叠问题，在计算过程中解决即可）【考虑数据类型】
            Dictionary<Tuple<int, int>, Dictionary<int, PathTrace>> DesDirPt = FMU.GetDesDirPtDis(pWeighGrids, desGrids, GridType);//获取给定节点的方向探索路径
            //Dictionary<Tuple<int, int>, double> DesDis = FMU.GetDisOrder(desGrids, sGrid);
            #endregion

            #region 遍历构成Flow过程
            double CountLabel = 0;//进程监控
            while (desGrids.Count > 0)
            {
                CountLabel++; Console.WriteLine(CountLabel);//进程监控
                double MaxDis = 0;
                Path CachePath = null;
                List<Tuple<int, int>> TestTargetPath = null;//测试用
                double TestShort = 0;//测试用

                for (int i = 0; i < desGrids.Count; i++)
                {
                    //每次更新网格权重
                    Console.Write(i);
                    double MinDis = FMU.GetMinDis(desGrids[i], cFM.PathGrids);
                    //List<double> DisList = DesDis.Values.ToList(); DisList.Sort();//升序排列
                    //double Order = DisList.IndexOf(DesDis[desGrids[i]]) / DisList.Count;

                    #region 获取到PathGrid的路径
                    double MinLength = 100000;
                    double tMinLength = 100000;
                    List<Tuple<int, int>> TargetPath = null;
                    int Label = 0;//标识终点到起点最短距离的节是否是起点（=1表示终点是起点）
                    Dictionary<int, PathTrace> DirPt = DesDirPt[desGrids[i]];

                    for (int j = 0; j < cFM.PathGrids.Count; j++)
                    {
                        #region 优化搜索速度（只搜索制定范围内的节点）
                        //if (FMU.RLJudgeGrid(desGrids[i], cFM.PathGrids[j], sGrid))
                        //{
                        #region 获取终点到已有路径中某点的最短路径
                        List<int> DirList = FMU.GetConDirR(cFM.PathGrids[j], desGrids[i]);//获取限制性约束的方向                   
                        int DirID = FMU.GetNumber(DirList);

                        #region 可能存在重合点的情况
                        if (DirID == 0)
                        {
                            break;
                        }
                        #endregion

                        List<Tuple<int, int>> CacheShortPath = DirPt[DirID].GetShortestPath(cFM.PathGrids[j], desGrids[i]);
                        #endregion

                        #region 需要考虑可能不存在路径的情况
                        double CacheShortPathLength = 0;
                        double tCacheShortPathLength = 0;
                        if (CacheShortPath != null)
                        {
                            //考虑数据类型
                            //tCacheShortPathLength = FMU.GetPathLengthDisRever(CacheShortPath,GridType);
                            //CacheShortPathLength = FMU.GetPathLengthDis(CacheShortPath, GridType);

                            CacheShortPathLength = FMU.GetPathLength(CacheShortPath);
                        }
                        else
                        {
                            CacheShortPathLength = 10000000;
                        }
                        #endregion

                        #region 添加交叉约束
                        if (CacheShortPath != null)
                        {
                            #region 存在交叉
                            if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 2)
                            {
                                CacheShortPathLength = 100000 + CacheShortPathLength;
                            }
                            #endregion

                            #region 存在重叠
                            else if (FMU.IntersectPathInt(CacheShortPath, cFM.PathGrids) == 1)
                            {
                                //if (MinDis < Math.Sqrt(1.9) && (CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65) > MaxDis)///如果长度小于该值才需要判断；否则无须判断
                                if (MinDis < Math.Sqrt(1.9))
                                {
                                    #region 考虑到搜索方向固定可能导致的重合
                                    Dictionary<Tuple<int, int>, double> WeighGrids = Clone((object)pWeighGrids) as Dictionary<Tuple<int, int>, double>;//深拷贝
                                    FMU.FlowCrosssingContraint(desGrids, WeighGrids, 0, desGrids[i], cFM.PathGrids[j], cFM.PathGrids);//Overlay约束
                                    PathTrace Pt = new PathTrace();
                                    List<Tuple<int, int>> JudgeList = new List<Tuple<int, int>>();
                                    JudgeList.Add(desGrids[i]);//添加搜索的起点

                                    //考虑数据类型
                                    Pt.MazeAlgDis(JudgeList, WeighGrids, 1, DirList,GridType);//备注：每次更新以后,WeightGrid会清零（这里需要考虑数据类型）
                                    CacheShortPath = Pt.GetShortestPath(cFM.PathGrids[j], desGrids[i]);

                                    if (CacheShortPath != null)
                                    {
                                        //考虑数据类型
                                        //tCacheShortPathLength = FMU.GetPathLengthDisRever(CacheShortPath, GridType);
                                        //CacheShortPathLength = FMU.GetPathLengthDis(CacheShortPath, GridType);

                                        CacheShortPathLength = FMU.GetPathLength(CacheShortPath);

                                        #region 判段交叉
                                        if (FMU.IntersectPath(CacheShortPath, cFM.PathGrids))
                                        {
                                            CacheShortPathLength = 100000 + CacheShortPathLength;
                                        }
                                        #endregion
                                    }
                                    else
                                    {
                                        CacheShortPathLength = 10000000;
                                    }
                                    #endregion
                                }

                                ///否则，加上一个极大值
                                else
                                {
                                    CacheShortPathLength = 100000 + CacheShortPathLength;
                                }
                            }
                            #endregion

                            //if (FMU.IntersectPath(CacheShortPath, cFM.PathGrids))
                            //{
                            //    CacheShortPathLength = 100000 + CacheShortPathLength;
                            //}

                            //if (FMU.LineIntersectPath(CacheShortPath, cFM.PathGrids, Grids))
                            //{
                            //    CacheShortPathLength = 1000000 + CacheShortPathLength;
                            //}

                            //if (FMU.obstacleIntersectPath(CacheShortPath, Features, Grids))
                            //{
                            //    CacheShortPathLength = 1000000 + CacheShortPathLength;
                            //}

                        }
                        #endregion

                        #region 添加角度约束限制
                        if (CacheShortPath != null)
                        {
                            if (FMU.AngleContraint(CacheShortPath, cFM.GridForPaths[cFM.PathGrids[j]], Grids))
                            {
                                CacheShortPathLength = 1000000 + CacheShortPathLength;
                            }
                        }
                        #endregion

                        #region 添加长度约束
                        if (CacheShortPathLength < Math.Sqrt(1.9))
                        {
                            CacheShortPathLength = 100000 + CacheShortPathLength;
                        }
                        #endregion

                        double TotalLength = 0;
                        //double tTotalLength = 0;
                        //TotalLength = CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].GetPathLengthType(GridType) * 0.65;
                        TotalLength = CacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].Length * 0.65;
                        //tTotalLength = tCacheShortPathLength + cFM.GridForPaths[cFM.PathGrids[j]].GetPathLengthTypeRever(GridType) * 0.65;

                        #region 比较获取某给定节点到起点的最短路径
                        if (TotalLength < MinLength)
                        {
                            if (cFM.GridForPaths[cFM.PathGrids[j]].Length == 0 && !cFM.PathGrids.Contains(CacheShortPath[1]))//消除某些点到起点的最短路径是经过已有路径的情况
                            {
                                Label = 1;//标识最短路径终点是起点
                            }

                            else
                            {
                                Label = 0;//标识最短路径终点非起点
                            }

                            MinLength = TotalLength;
                            //tMinLength = tTotalLength;
                            List<Tuple<int, int>> pCachePath = cFM.GridForPaths[cFM.PathGrids[j]].ePath.ToList();
                            CacheShortPath.RemoveAt(0);//移除第一个要素，避免存在重复元素

                            pCachePath.AddRange(CacheShortPath);
                            TargetPath = pCachePath;

                            TestTargetPath = CacheShortPath;//测试用
                            TestShort = CacheShortPathLength;//测试用
                        }
                        #endregion
                    }
                        #endregion
                    //}
                    #endregion

                    #region 表示起点优先限制
                    if (Label == 1)
                    {
                        tMinLength = tMinLength + 10000;
                    }
                    #endregion

                    #region 获取到起点路径最长终点的路径
                    if (TargetPath == null)//可能存在路径为空的情况
                    {
                        MinLength = 0;
                    }

                    if (MinLength > MaxDis)
                    {
                        MaxDis = MinLength;
                        CachePath = new Path(sGrid, desGrids[i], TargetPath);

                        double Test = FMU.GetPathLength(TestTargetPath);//测试用
                        int TesTloc = 0;
                    }
                    #endregion
                }

                #region 可视化显示（表示Path的生成过程）
                FlowDraw FD1 = new FlowDraw(pMapControl, Grids);
                FD1.FlowPathDraw(CachePath, 1, 0);
                #endregion

                #region 防止可能出现空的情况
                //cFM.PathRefresh(CachePath, 1);
                if (CachePath != null)
                {
                    cFM.PathRefresh(CachePath, 1, PointFlow[GridWithNode[CachePath.endPoint]]);//更新：包括子路径与流量更新
                    desGrids.Remove(CachePath.endPoint);//移除一个Destination
                    //DesDis.Remove(CachePath.endPoint);
                }

                else
                {
                    desGrids.RemoveAt(0);
                }
                #endregion
            }
            #endregion

            #region 可视化和输出
            FlowDraw FD2 = new FlowDraw(pMapControl, Grids, AllPoints, NodeInGrid, GridWithNode, OutFilePath);
            //FD2.SmoothFlowMap(cFM.SubPaths, 2, 2000, 20, 1, 0, 1, 1, 200);
            FD2.FlowMapDraw(cFM.SubPaths, 15, 2000, 20, 1, 1, 1);
            #endregion
        }
    }
}
