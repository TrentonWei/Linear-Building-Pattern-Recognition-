using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using AuxStructureLib;
using Microsoft.Office.Interop.Excel;

namespace PrDispalce.建筑物群空间特征分析小工具
{
    public partial class BuildingFeatureComputation : Form
    {
        public BuildingFeatureComputation(AxMapControl pMapControl)
        {
            InitializeComponent();
            this.pMap = pMapControl.Map;
            this.pMapControl = pMapControl;
        }

        #region 参数
        IMap pMap; AxMapControl pMapControl;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        PrDispalce.工具类.BuildingGroupFeatureComputation BFC = new 工具类.BuildingGroupFeatureComputation();
        PrDispalce.工具类.ProximityFeatureComputation PFC = new 工具类.ProximityFeatureComputation();
        string OutPath;
        string localFilePath, fileNameExt, FilePath;
        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BuildingFeatureComputation_Load(object sender, EventArgs e)
        {
            if (this.pMap.LayerCount <= 0)
                return;

            ILayer pLayer;
            string strLayerName;
            for (int i = 0; i < this.pMap.LayerCount; i++)
            {
                pLayer = this.pMap.get_Layer(i);
                strLayerName = pLayer.Name;

                IDataset LayerDataset = pLayer as IDataset;

                if (LayerDataset != null)
                {
                    #region 添加图层
                    IFeatureLayer pFeatureLayer = pLayer as IFeatureLayer;
                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        this.comboBox2.Items.Add(strLayerName);
                    }

                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                    {
                        this.comboBox1.Items.Add(strLayerName);
                    }

                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                    {
                        this.comboBox4.Items.Add(strLayerName);
                    }
                    #endregion
                }
            }

            #region 默认显示第一个
            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }

            if (this.comboBox2.Items.Count > 0)
            {
                this.comboBox2.SelectedIndex = 0;
            }

            if (this.comboBox4.Items.Count > 0)
            {
                this.comboBox4.SelectedIndex = 0;
            }
            #endregion
        }

        /// <summary>
        /// 计算群组建筑物空间特征
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox2.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            map.InterpretatePoint(2);
            #endregion

            #region DT+CDT+SKE+pg
            DelaunayTin dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            ConvexNull cn = new ConvexNull(dt.TriNodeList);
            cn.CreateConvexNull();

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(map.PolylineList, map.PolygonList);

            Triangle.WriteID(dt.TriangleList);
            TriEdge.WriteID(dt.TriEdgeList);

            AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
            ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
            ske.TranverseSkeleton_Segment_PLP_NONull();

            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGraphfrmSkeletonBuildings(map, ske);
            pg.DeleteRepeatedEdge(pg.EdgeList);//删除重复的邻近边
            pg.CreatePgForBuildings(pg.NodeList, pg.EdgeList);//创建建筑物的邻近图
            pg.CreateRNGForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);//生成RNG图
            pg.DeletelongEdges(pg.RNGBuildingEdgesListShortestDistance, 50);//删除长边
            #endregion

            #region 获得任意建筑物的一阶RNG邻近
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                ProxiNode tPoint = map.PolygonList[i].CalProxiNode();
                foreach (ProxiEdge Pe in pg.RNGBuildingEdgesListShortestDistance)
                {
                    ProxiNode Pn1 = Pe.Node1;
                    ProxiNode Pn2 = Pe.Node2;

                    if (tPoint.X == Pn1.X && tPoint.Y == Pn1.Y)
                    {
                        PolygonObject Po1 = map.GetObjectbyID(Pn2.TagID, Pn2.FeatureType) as PolygonObject;
                        map.PolygonList[i].RNGProximity1.Add(Po1);
                    }

                    if (tPoint.X == Pn2.X && tPoint.Y == Pn2.Y)
                    {
                        PolygonObject Po1 = map.GetObjectbyID(Pn1.TagID, Pn1.FeatureType) as PolygonObject;
                        map.PolygonList[i].RNGProximity1.Add(Po1);
                    }
                }
            }
            #endregion

            #region 计算任意建筑物的群组参数
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                List<PolygonObject> PolygonList = map.PolygonList[i].RNGProximity1;
                PolygonList.Add(map.PolygonList[i]);

                if (PolygonList.Count >= 2)//只有建筑物群组大于2的才会被计算（等于0的都不会被计算）
                {
                    map.PolygonList[i].AverageArea = BFC.AreaAverage(PolygonList);//平均面积
                    map.PolygonList[i].AreaDiff = BFC.AreaDiff(PolygonList);//面积标准差
                    map.PolygonList[i].VarAreaDiff = BFC.VarAreaDiff(PolygonList);//面积变异系数
                    map.PolygonList[i].BlackWhiteRatio = BFC.BlackWhiteRatio(PolygonList);//黑白面积对比
                    map.PolygonList[i].smbrRatio = BFC.smbrRatio(PolygonList);//绑定矩形黑白面积对比
                    map.PolygonList[i].AverageDistance = BFC.AverageDistance(PolygonList);//平均距离
                    map.PolygonList[i].DistanceDiff = BFC.DistanceDiff(PolygonList);//距离标准差
                    map.PolygonList[i].VarDistanceDiff = BFC.VarDistanceDiff(PolygonList);//距离变异系数
                    map.PolygonList[i].EdgeCountDiff = BFC.EdgeCountDiff(PolygonList);//边数的标准差
                    map.PolygonList[i].VarEdgeCountDiff = BFC.VarEdgeCountDiff(PolygonList);//边数的变异系数
                    map.PolygonList[i].IPQComAverage = BFC.IPQComAverage(PolygonList);//IPQCom平均值
                    map.PolygonList[i].IPQComDiff = BFC.IPQComDiff(PolygonList);//IPQCom标准差
                    map.PolygonList[i].AveIPQComDiff = BFC.AveIPQComDiff(PolygonList);//均值偏差
                    map.PolygonList[i].RatioAverage = BFC.RatioAverage(PolygonList);//邻近正对面积的平均值
                }
            }
            #endregion

            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "RNGShortest", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.RNGBuildingEdgesListShortestDistance); }
            if (OutPath != null) { ske.CDT.DT.WriteShp(OutPath, pMap.SpatialReference); }
            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "邻近图", pMap.SpatialReference, pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList); }
            if (OutPath != null) { ske.Create_WriteSkeleton_Segment2Shp(OutPath, "骨架", pMap.SpatialReference); }
            if (OutPath != null){map.WriteResult3Shp(OutPath, pMap.SpatialReference);}//输出
        }

        #region 将数据存储到字段下
        public void DataStore(IFeatureClass pFeatureClass, IFeature pFeature, string s, double t)
        {
            //IDataset dataset = pFeatureClass as IDataset;
            //IWorkspace workspace = dataset.Workspace;
            //IWorkspaceEdit wse = workspace as IWorkspaceEdit;

            IFields pFields = pFeature.Fields;

            //wse.StartEditing(false);
            //wse.StartEditOperation();

            int fnum;
            fnum = pFields.FieldCount;

            for (int m = 0; m < fnum; m++)
            {
                if (pFields.get_Field(m).Name == s)
                {
                    int field1 = pFields.FindField(s);
                    pFeature.set_Value(field1, t);
                    //pFeature.Store();
                }
            }

            //wse.StopEditOperation();
            //wse.StopEditing(true);
        }
        #endregion

        /// <summary>
        /// 输出路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fdialog = new FolderBrowserDialog();
            string outfilepath = null;

            if (fdialog.ShowDialog() == DialogResult.OK)
            {
                string Path = fdialog.SelectedPath;
                outfilepath = Path;
            }

            OutPath = outfilepath;
            this.comboBox3.Text = OutPath;
        }
        
        /// <summary>
        /// 邻近建筑物空间特征计算
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            #region 获取建筑物图层与邻近图图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }

            if (this.comboBox4.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list.Add(BuildingLayer);
            }

            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            map.InterpretatePoint(2);
            #endregion

            #region 创建excel
            Microsoft.Office.Interop.Excel.Application app = new Microsoft.Office.Interop.Excel.Application();
            Workbook wbk = app.Workbooks.Open(localFilePath);//工作簿
            Sheets shs = wbk.Sheets;
            Worksheet wsh = (Worksheet)shs.get_Item(1);
            #endregion

            //wsh.Cells[2, 1] = "str";

            #region 遍历输出邻近要素特征
            int j = 1;
            for (int i = 0; i < map.PolylineList.Count; i++) //3323
            {
                TriNode Pn1 = map.PolylineList[i].PointList[0];
                TriNode Pn2 = map.PolylineList[i].PointList[map.PolylineList[i].PointList.Count-1];

                PolygonObject Po1 = map.GetPPbyCenter(Pn1.X, Pn1.Y);
                PolygonObject Po2 = map.GetPPbyCenter(Pn2.X, Pn2.Y);

                if (Po1 != null && Po2 != null)
                {
                    j++;
                    #region 计算指标
                    double sizer = PFC.SizeRelation(Po1, Po2);
                    double Orir = PFC.OrientationRelation(Po1, Po2);
                    double IPQr = PFC.IPQComRelation(Po1, Po2);
                    double Edger = PFC.EdgeCountRelation(Po1, Po2);
                    double Convexr = PFC.ConvexRelation(Po1, Po2);
                    double smbrr = PFC.smbrRelation(Po1, Po2);
                    double Facer = PFC.FaceRelation(Po1, Po2);
                    #endregion

                    #region 输出到excel
                    wsh.Cells[j, 2] = sizer;
                    wsh.Cells[j, 3] = Orir;
                    wsh.Cells[j, 4] = IPQr;
                    wsh.Cells[j, 5] = Edger;
                    wsh.Cells[j, 6] = Convexr;
                    wsh.Cells[j, 7] = smbrr;
                    wsh.Cells[j, 8] = Facer;
                    #endregion
                }
            }
            #endregion

            wbk.Save();//保存
            app.Quit();//退出
            System.Runtime.InteropServices.Marshal.ReleaseComObject(app);//释放
        }

        #region 邻近元素空间特征输出路径（依据计算输出获得邻近图计算邻近空间特征；可能会导致计算的数量偏少）
        private void button4_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = " excel files(*.xls)|";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //获得文件路径
                localFilePath = saveFileDialog1.FileName.ToString();

                //获取文件名，不带路径
                fileNameExt = localFilePath.Substring(localFilePath.LastIndexOf("\\") + 1);

                //获取文件路径，不带文件名
                FilePath = localFilePath.Substring(0, localFilePath.LastIndexOf("\\"));
            }

            this.comboBox5.Text = localFilePath;
        }
        #endregion

        /// <summary>
        /// 邻近元素空间特征输出路径（计算邻近图，不输出，获取相应空间特征）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox2.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            map.InterpretatePoint(2);
            #endregion

            #region DT+CDT+SKE+pg
            DelaunayTin dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            ConvexNull cn = new ConvexNull(dt.TriNodeList);
            cn.CreateConvexNull();

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(map.PolylineList, map.PolygonList);

            Triangle.WriteID(dt.TriangleList);
            TriEdge.WriteID(dt.TriEdgeList);

            AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
            ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
            ske.TranverseSkeleton_Segment_PLP_NONull();

            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGraphfrmSkeletonBuildings(map, ske);
            pg.DeleteRepeatedEdge(pg.EdgeList);//删除重复的邻近边
            pg.CreatePgForBuildings(pg.NodeList, pg.EdgeList);//创建建筑物的邻近图
            pg.CreateRNGForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);//生成RNG图
            pg.DeletelongEdges(pg.RNGBuildingEdgesListShortestDistance, 50);//删除长边
            #endregion

            #region 创建excel
            Microsoft.Office.Interop.Excel.Application app = new Microsoft.Office.Interop.Excel.Application();
            Workbook wbk = app.Workbooks.Open(localFilePath);//工作簿
            Sheets shs = wbk.Sheets;
            Worksheet wsh = (Worksheet)shs.get_Item(1);
            #endregion

            //wsh.Cells[2, 1] = "str";

            #region 遍历输出邻近要素特征
            int j = 1;
            foreach (ProxiEdge Pe in pg.RNGBuildingEdgesListShortestDistance)
            {
                ProxiNode Pn1 = Pe.Node1;
                ProxiNode Pn2 = Pe.Node2;

                PolygonObject Po1 = map.GetPPbyCenter(Pn1.X, Pn1.Y);
                PolygonObject Po2 = map.GetPPbyCenter(Pn2.X, Pn2.Y);

                if (Po1 != null && Po2 != null)
                {
                    j++;

                    #region 计算指标
                    double sizer = PFC.SizeRelation(Po1, Po2);
                    double Orir = PFC.OrientationRelation(Po1, Po2);
                    double IPQr = PFC.IPQComRelation(Po1, Po2);
                    double Edger = PFC.EdgeCountRelation(Po1, Po2);
                    double Convexr = PFC.ConvexRelation(Po1, Po2);
                    double smbrr = PFC.smbrRelation(Po1, Po2);
                    double Facer = PFC.FaceRelation(Po1, Po2);
                    double SDis = Pe.NearestDistance;
                    #endregion

                    #region 输出到excel
                    wsh.Cells[j, 2] = sizer;
                    wsh.Cells[j, 3] = Orir;
                    wsh.Cells[j, 4] = IPQr;
                    wsh.Cells[j, 5] = Edger;
                    wsh.Cells[j, 6] = Convexr;
                    wsh.Cells[j, 7] = smbrr;
                    wsh.Cells[j, 8] = Facer;
                    wsh.Cells[j, 9] = SDis;
                    #endregion
                }
            }
            #endregion

            wbk.Save();//保存
            app.Quit();//退出
            System.Runtime.InteropServices.Marshal.ReleaseComObject(app);//释放
        }
    }
}
