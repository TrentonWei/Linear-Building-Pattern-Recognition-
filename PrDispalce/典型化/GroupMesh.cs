using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
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

using AuxStructureLib;
using AuxStructureLib.IO;
using PrDispalce.工具类;


namespace PrDispalce.典型化
{
    public partial class GroupMesh : Form
    {
        public GroupMesh(IMap cMap, AxMapControl pMapControl)
        {
            InitializeComponent();
            this.pMap = cMap;
            this.pMapControl = pMapControl;
        }


        #region 参数
        IMap pMap;
        AxMapControl pMapControl;
        string OutPath;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        List<PolygonObject> OutPolygonObject = new List<PolygonObject>();
        PrDispalce.工具类.Symbolization Symbol = new PrDispalce.工具类.Symbolization();
        GroupMeshUtil GMU = new GroupMeshUtil();
        PrDispalce.典型化.Symbolization SB = new Symbolization();
        #endregion

        #region 初始化
        private void GroupMesh_Load(object sender, EventArgs e)
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
                    #region 添加线图层
                    IFeatureLayer pFeatureLayer = pLayer as IFeatureLayer;
                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                    {
                        this.comboBox2.Items.Add(strLayerName);
                    }
                    #endregion

                    #region 添加面图层
                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        this.comboBox1.Items.Add(strLayerName);
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
            #endregion
        }
        #endregion

        #region 简单Mesh的典型化（终止条件是数量）
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

            SMap map2 = new SMap(list);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            map2.InterpretatePoint(2);
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
            #endregion

            #region 典型化
            GMU.SimplyMesh(map, pg, map.PolygonList.Count * 0.05);
            #endregion

            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                IPolygon ConvexPolygon = new PolygonClass();//输出群对应的多边形
                SB.SymbolizedPolygon(pMapControl, map2, map.PolygonList[i], 25000, 0.5, 0.5, 1, out ConvexPolygon);//要注意这里的地图有改变，不是改变后的地图
            }

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);//输出
        }
        #endregion

        #region 输出路径
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
        #endregion

        #region 顾及依比例尺与不依比例尺Mesh典型化
        private void button3_Click(object sender, EventArgs e)
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
            SMap initialMap = new SMap(list);
            initialMap.ReadDateFrmEsriLyrsForEnrichNetWork();
            initialMap.InterpretatePoint(2);

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
            #endregion

            #region 典型化
            GMU.ScaleMesh(map, pg, 25000, 0.2, 2);
            #endregion

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);//输出
        }
        #endregion

        #region 顾及pattern的简单Mesh典型化
        private void button4_Click(object sender, EventArgs e)
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

            SMap map2 = new SMap(list);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            map2.InterpretatePoint(2);
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
            #endregion

            #region 典型化
            GMU.SimplyMeshConsiderPattern(map, pg, 50000, 2, 2, 2, 0.2, 0.0, 1);
            #endregion

            #region Pattern位置调整
            if (checkBox2.Checked)
            {
                //GMU.GetPatterns(map);
                foreach (KeyValuePair<int, Pattern> kv in GMU.PatternDic)
                {
                    kv.Value.SortPatternInTypification(map, pg);
                }

                foreach (KeyValuePair<int, Pattern> kv in GMU.PatternDic)
                {
                    kv.Value.PatternRelocationInTypification();
                }

                //更新地图
                foreach (KeyValuePair<int, Pattern> kv in GMU.PatternDic)
                {
                    for (int i = 0; i < kv.Value.PatternObjects.Count; i++)
                    {
                        PolygonObject Po = map.GetObjectbyID(kv.Value.PatternObjects[i].ID, FeatureType.PolygonType) as PolygonObject;
                        int Label = map.PolygonList.IndexOf(Po);
                        map.PolygonList[i] = kv.Value.PatternObjects[i];
                    }
                }
            }
            #endregion

            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                IPolygon ConvexPolygon = new PolygonClass();//输出群对应的多边形
                SB.SymbolizedPolygon(pMapControl, map2, map.PolygonList[i], 50000, 0.5, 0.5, 1, out ConvexPolygon);//要注意这里的地图有改变，不是改变后的地图
            }

            #region 典型化建筑物方向调整
            if (this.checkBox1.Checked)
            {
                IFeatureClass pFeatureClass=pFeatureHandle.createPolygonshapefile(pMap.SpatialReference,OutPath,"ConvexHull");
                //获取顶图层的数据集，并创建工作空间
                IDataset dataset = (IDataset)pFeatureClass;
                IWorkspace workspace = dataset.Workspace;
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatureClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();

                for (int i = 0; i < map.PolygonList.Count; i++)
                {
                    IPolygon ConvexPolygon = new PolygonClass();//输出群对应的多边形
                    map.PolygonList[i] = SB.SymbolizedPolygon(pMapControl,map2, map.PolygonList[i], 50000, 0.5, 0.5, 1, out ConvexPolygon);
          
                    IFeature feature = pFeatureClass.CreateFeature();

                    int index;
                    index = feature.Fields.FindField("Shape");
                    IGeometryDef pGeometryDef;
                    pGeometryDef = feature.Fields.get_Field(index).GeometryDef as IGeometryDef;

                    bool hasz =pGeometryDef.HasZ;

                    if (hasz)
                    {
                        IZAware za = (IZAware)(ConvexPolygon as IGeometry);
                        za.ZAware = true;
                        IZ z = (IZ)(ConvexPolygon as IGeometry);
                        z.SetConstantZ(1); //将Z值设置为1
                    }
                    else
                    {
                        IZAware za = (IZAware)(ConvexPolygon as IGeometry);
                        za.ZAware = false;
                    }


                    feature.Shape = ConvexPolygon as IGeometry;
                    feature.Store();
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            #endregion

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);//输出
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "建筑物邻近图", pMap.SpatialReference, pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList); }         
        }
        #endregion

        #region 简单Mesh的典型化（终止条件为冲突判断）
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

            SMap map2 = new SMap(list);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            map2.InterpretatePoint(2);
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
            #endregion

            #region 典型化
            GMU.SimplyMesh(map, pg, 25000, 0.2);
            #endregion

            #region 输出移位距离
            //System.IO.FileStream fs = new System.IO.FileStream(@"G:\多尺度居民地尺度和区域分异规律\典型化\OurBP.txt", System.IO.FileMode.OpenOrCreate);
            //StreamWriter sw = new StreamWriter(fs);

            //for (int i = 0; i < map.PolygonList.Count; i++)
            //{
            //    if (map.PolygonList[i].TyList.Count > 0)
            //    {
            //        PolygonObject nPolygonObject = map2.GetObjectbyID(map.PolygonList[i].ID, FeatureType.PolygonType) as PolygonObject;
            //        double Distance = Math.Sqrt((map.PolygonList[i].CalProxiNode().X - nPolygonObject.CalProxiNode().X) * (map.PolygonList[i].CalProxiNode().X - nPolygonObject.CalProxiNode().X)
            //            + (map.PolygonList[i].CalProxiNode().Y - nPolygonObject.CalProxiNode().Y) * (map.PolygonList[i].CalProxiNode().Y - nPolygonObject.CalProxiNode().Y));
            //        sw.Write(map.PolygonList[i].ID + "+" + Distance);
            //        sw.Write("\r\n");

            //        for (int j = 0; j < map.PolygonList[i].TyList.Count; j++)
            //        {
            //            nPolygonObject = map2.GetObjectbyID(map.PolygonList[i].TyList[j], FeatureType.PolygonType) as PolygonObject;
            //            Distance = Math.Sqrt((map.PolygonList[i].CalProxiNode().X - nPolygonObject.CalProxiNode().X) * (map.PolygonList[i].CalProxiNode().X - nPolygonObject.CalProxiNode().X)
            //            + (map.PolygonList[i].CalProxiNode().Y - nPolygonObject.CalProxiNode().Y) * (map.PolygonList[i].CalProxiNode().Y - nPolygonObject.CalProxiNode().Y));
            //            sw.Write(map.PolygonList[i].TyList[j] + "+" + Distance);
            //            sw.Write("\r\n");
            //        }
            //    }

            //    else
            //    {
            //        sw.Write(map.PolygonList[i].ID + "+" + 0);
            //        sw.Write("\r\n");
            //    }
            //}

            //sw.Close();
            //fs.Close();
            #endregion

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);//输出
        }
        #endregion

        #region 简单Mesh的典型化（终止条件为冲突判断+距离以建筑物面积加权）
        private void button6_Click(object sender, EventArgs e)
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
            #endregion

            #region 典型化
            GMU.SimplyMesh(map, pg, 50000, 0.2, 625);
            #endregion

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);//输出
        }
        #endregion

        #region 考虑合并和选取的典型化
        private void button7_Click(object sender, EventArgs e)
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

            SMap map2 = new SMap(list);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            map2.InterpretatePoint(2);
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
            #endregion

            #region 典型化
            GMU.ProgressiveGeneralization(map, pg, 25000, 2, 2, 2, 0.2, 0.1, pMap);
            #endregion

            #region Pattern位置调整
            if (checkBox2.Checked)
            {
                //GMU.GetPatterns(map);
                foreach (KeyValuePair<int, Pattern> kv in GMU.PatternDic)
                {
                    kv.Value.SortPatternInTypification(map, pg);
                }

                foreach (KeyValuePair<int, Pattern> kv in GMU.PatternDic)
                {
                    kv.Value.PatternRelocationInTypification();
                }

                //更新地图
                foreach (KeyValuePair<int, Pattern> kv in GMU.PatternDic)
                {
                    for (int i = 0; i < kv.Value.PatternObjects.Count; i++)
                    {
                        PolygonObject Po = map.GetObjectbyID(kv.Value.PatternObjects[i].ID, FeatureType.PolygonType) as PolygonObject;
                        int Label = map.PolygonList.IndexOf(Po);
                        map.PolygonList[Label] = kv.Value.PatternObjects[i];
                    }
                }
            }
            #endregion

            //for (int i = 0; i < map.PolygonList.Count; i++)
            //{
            //    if (map.PolygonList[i].Area < 25000 * 0.5 / 1000 * 25000 * 0.5 / 1000)
            //    {
            //        IPolygon ConvexPolygon = new PolygonClass();//输出群对应的多边形
            //        SB.SymbolizedPolygon(pMapControl, map2, map.PolygonList[i], 25000, 0.5, 0.5, 1, out ConvexPolygon);//要注意这里的地图有改变，不是改变后的地图
            //    }
            //}

            #region 典型化建筑物方向调整
            if (this.checkBox1.Checked)
            {
                IFeatureClass pFeatureClass = pFeatureHandle.createPolygonshapefile(pMap.SpatialReference, OutPath, "ConvexHull");
                //获取顶图层的数据集，并创建工作空间
                IDataset dataset = (IDataset)pFeatureClass;
                IWorkspace workspace = dataset.Workspace;
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatureClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();

                for (int i = 0; i < map.PolygonList.Count; i++)
                {
                    if (map.PolygonList[i].Area < 25000 * 0.5 / 1000 * 25000 * 0.5 / 1000)
                    {
                        IPolygon ConvexPolygon = new PolygonClass();//输出群对应的多边形
                        map.PolygonList[i] = SB.SymbolizedPolygon(pMapControl, map2, map.PolygonList[i], 25000, 0.5, 0.5, 1, out ConvexPolygon);

                        IFeature feature = pFeatureClass.CreateFeature();

                        int index;
                        index = feature.Fields.FindField("Shape");
                        IGeometryDef pGeometryDef;
                        pGeometryDef = feature.Fields.get_Field(index).GeometryDef as IGeometryDef;

                        bool hasz = pGeometryDef.HasZ;

                        if (hasz)
                        {
                            IZAware za = (IZAware)(ConvexPolygon as IGeometry);
                            za.ZAware = true;
                            IZ z = (IZ)(ConvexPolygon as IGeometry);
                            z.SetConstantZ(1); //将Z值设置为1
                        }
                        else
                        {
                            IZAware za = (IZAware)(ConvexPolygon as IGeometry);
                            za.ZAware = false;
                        }


                        feature.Shape = ConvexPolygon as IGeometry;
                        feature.Store();
                    }
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            #endregion

            #region 输出移位距离
            System.IO.FileStream fs = new System.IO.FileStream(@"G:\多尺度居民地尺度和区域分异规律\典型化\OurBS.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);

            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                if (map.PolygonList[i].TyList.Count > 0)
                {
                    PolygonObject nPolygonObject = map2.GetObjectbyID(map.PolygonList[i].ID, FeatureType.PolygonType) as PolygonObject;
                    double Distance=Math.Sqrt((map.PolygonList[i].CalProxiNode().X-nPolygonObject.CalProxiNode().X)*(map.PolygonList[i].CalProxiNode().X-nPolygonObject.CalProxiNode().X)
                        +(map.PolygonList[i].CalProxiNode().Y-nPolygonObject.CalProxiNode().Y)*(map.PolygonList[i].CalProxiNode().Y-nPolygonObject.CalProxiNode().Y));
                    sw.Write(map.PolygonList[i].ID + "+" + Distance);
                    sw.Write("\r\n");

                    for (int j = 0; j < map.PolygonList[i].TyList.Count; j++)
                    {
                        nPolygonObject = map2.GetObjectbyID(map.PolygonList[i].TyList[j], FeatureType.PolygonType) as PolygonObject;
                        Distance = Math.Sqrt((map.PolygonList[i].CalProxiNode().X - nPolygonObject.CalProxiNode().X) * (map.PolygonList[i].CalProxiNode().X - nPolygonObject.CalProxiNode().X)
                        + (map.PolygonList[i].CalProxiNode().Y - nPolygonObject.CalProxiNode().Y) * (map.PolygonList[i].CalProxiNode().Y - nPolygonObject.CalProxiNode().Y));
                        sw.Write(map.PolygonList[i].TyList[j] + "+" + Distance);
                        sw.Write("\r\n");
                    }
                }

                else
                {
                    sw.Write(map.PolygonList[i].ID + "+" + 0);
                    sw.Write("\r\n");
                }
            }

            sw.Close();
            fs.Close();     
            #endregion

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);//输出
        }
        #endregion

        #region 2.21 典型化
        private void button8_Click(object sender, EventArgs e)
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

            SMap map2 = new SMap(list);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            map2.InterpretatePoint(2);
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
            #endregion

            #region 典型化
            GMU.ProgressiveGeneralization(map, pg, 25000, 2, 0.2, pMap);
            #endregion

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
        }
        #endregion

        #region 为形成邻近尺度间的连续表达
        private void button9_Click(object sender, EventArgs e)
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

            SMap map2 = new SMap(list);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            map2.InterpretatePoint(2);
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
            #endregion

            #region 典型化
            GMU.ProgressiveGeneralizationP(map, pg, 25000, 2, 0.2, pMap,OutPath);
            #endregion
        }
        #endregion
    }
}
