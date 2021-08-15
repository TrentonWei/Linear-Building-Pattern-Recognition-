using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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

namespace PrDispalce.协同移位_整体_
{
    public partial class ProximityGraphBuilder : Form
    {
        public ProximityGraphBuilder(IMap cMap)
        {
            InitializeComponent();
            this.pMap = cMap;
        }

        #region 参数
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        string OutPath;
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

        #region 初始化
        private void ProximityGraphBuilder_Load(object sender, EventArgs e)
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
                    #region 添加点图层
                    IFeatureLayer pFeatureLayer = pLayer as IFeatureLayer;
                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                    {
                        this.comboBox4.Items.Add(strLayerName);
                    }
                    #endregion

                    #region 添加线图层
                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                    {
                        this.comboBox1.Items.Add(strLayerName);
                    }
                    #endregion

                    #region 添加面图层
                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        this.comboBox2.Items.Add(strLayerName);
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

        #region 确定
        private void button1_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }

            if (this.comboBox4.Text != null)
            {
                IFeatureLayer NodeLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list.Add(NodeLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            map.InterpretatePoint(2);
            #endregion

            #region DT+CDT+SKE
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
            pg.DeleteRepeatedEdge(pg.EdgeList);

            //VoronoiDiagram vd = null;
            //vd = new AuxStructureLib.VoronoiDiagram(ske, pg, map);
            //vd.CreateVoronoiDiagramfrmSkeletonforBuildings();

            //pg.CreatePgwithoutlongEdges(pg.NodeList,pg.EdgeList,0.0004);
            //pg.CreatePgwithoutAcorssEdges(pg.PgwithoutLongEdgesNodesList, pg.PgwithouLongEdgesEdgesList, vd);
            pg.CreatePgForBuildings(pg.NodeList, pg.EdgeList);
            //pg.CreateAlphaShape(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList, 38);


            //pg.CreateMSTForBuildingsGravityDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            //pg.CreateMSTForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);

            pg.CreateMSTForBuildingsGravityDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            pg.CreateMSTForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            //pg.CreateNNGForBuildingShortestDistance();

            //pg.CreateMSTForBuildingsGravityDistance(pg.NodeList, pg.EdgeList);
            //vd.FieldBuildBasedonBuildings2(32, 15, pg.EdgeList, map.PolygonList);
            //pg.CreateRNGForBuildingsGravityDistance(pg.NodeList, pg.EdgeList);
            pg.CreateRNGForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            pg.CreateRNGForBuildingsGravityDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            //pg.CreateGGGravityDistance(pg.NodeList, pg.EdgeList);
            //pg.CreateGGGravityDistance(pg.NodeList, pg.EdgeList);
            //pg.CreateGGShortestDistance(pg.NodeList, pg.EdgeList);
            //pg.CreateGGShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            //pg.CreateGGGravityDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            //pg.CreateNNGForBuildingShortestDistance();
           
            #endregion

            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "删除长边", pMap.SpatialReference, pg.PgwithoutLongEdgesNodesList, pg.PgwithouLongEdgesEdgesList); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "删除穿过边", pMap.SpatialReference, pg.PgwithoutAcrossEdgesNodesList, pg.PgwithoutAcorssEdgesEdgesList); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "邻近图", pMap.SpatialReference, pg.NodeList, pg.EdgeList); }
            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "MSTGravity", pMap.SpatialReference,pg.MSTBuildingNodesListGravityDistance,pg.MSTBuildingEdgesListGravityDistance); }
            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "MSTShortest", pMap.SpatialReference, pg.MSTBuildingNodesListShortestDistance,pg.MSTBuildingEdgesListShortestDistance); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "NNGShortest", pMap.SpatialReference, pg.MSTBuildingNodesListShortestDistance, pg.NNGforBuilding); }

            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "RNGGravity", pMap.SpatialReference,pg.RNGBuildingNodesListGravityDistance,pg.RNGBuildingEdgesListGravityDistance); }
            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "RNGShortest", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance,pg.RNGBuildingEdgesListShortestDistance); }

            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "AlphaShapeEdge", pMap.SpatialReference,pg.PgforBuildingNodesList, pg.AlphaShapeEdge); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "GGGravity", pMap.SpatialReference, pg.GGBuildingNodesListGravityDistance, pg.GGBuildingEdgesListGravityDistance); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "GGShortest", pMap.SpatialReference, pg.GGBuildingNodesListShortestDistance, pg.GGBuildingEdgesListShortestDistance); }
            //vd.FieldBuildBasedonRoads(pg.PgwithouLongEdgesEdgesList);
            //vd.FieldBuildBasedonBuildings(32, 15, pg.PgwithouLongEdgesEdgesList);
            //if (OutPath != null) { vd.Create_WritePolygonObject2ShpwithLevels(OutPath, "道路长边场", pMap.SpatialReference, vd.FielderListforRoads); }
            //if (OutPath != null) { vd.Create_WritePolygonObject2ShpwithLevels(OutPath, "建筑物长边场", pMap.SpatialReference, vd.FielderListforBuildings); }           
            //vd.FieldBuildBasedonRoads(pg.PgwithoutAcorssEdgesEdgesList);
            //if (OutPath != null) { vd.Create_WritePolygonObject2ShpwithLevels(OutPath, "穿过边场", pMap.SpatialReference, vd.FielderListforRoads); }

            if (OutPath != null) { ske.CDT.DT.WriteShp(OutPath, pMap.SpatialReference); }
            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "邻近图", pMap.SpatialReference, pg.PgforBuildingNodesList,pg.PgforBuildingEdgesList); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "邻近图", pMap.SpatialReference, pg.NodeList, pg.EdgeList); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "建筑物邻近图", pMap.SpatialReference, pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList); }
            if (OutPath != null) { ske.Create_WriteSkeleton_Segment2Shp(OutPath, "骨架", pMap.SpatialReference); }
        }
        #endregion
    }
}
