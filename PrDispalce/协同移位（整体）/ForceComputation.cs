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

namespace PrDispalce.协同移位_整体_
{
    public partial class ForceComputation : Form
    {
        public ForceComputation(IMap cMap)
        {
            InitializeComponent();
            this.pMap = cMap;
        }

        #region 参数
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        string localFilePath, fileNameExt, FilePath;
        #endregion

        #region 初始化
        private void ForceComputation_Load(object sender, EventArgs e)
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

        #region 冲突输出路径
        private void button1_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = " shp files(*.shp)|";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //获得文件路径
                localFilePath = saveFileDialog1.FileName.ToString();

                //获取文件名，不带路径
                fileNameExt = localFilePath.Substring(localFilePath.LastIndexOf("\\") + 1);

                //获取文件路径，不带文件名
                FilePath = localFilePath.Substring(0, localFilePath.LastIndexOf("\\"));
            }

            this.comboBox3.Text = localFilePath;
        }
        #endregion

        private void button2_Click(object sender, EventArgs e)
        {
            AuxStructureLib.ConflictLib.ConflictDetector cd = new AuxStructureLib.ConflictLib.ConflictDetector();

            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text.ToString());
            IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text.ToString());
            list.Add(StreetLayer);
            list.Add(BuildingLayer);
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            map.InterpretatePoint(2);

            //SMap mapCopy2 = new SMap(list);
            //mapCopy2.ReadDateFrmEsriLyrsForEnrichNetWork();
            //mapCopy2.InterpretatePoint(2);
            #endregion

            #region dt,ske等
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

            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGraphfrmSkeletonBuildings(map, ske);
            pg.DeleteRepeatedEdge(pg.EdgeList);

            VoronoiDiagram vd = null;
            vd = new AuxStructureLib.VoronoiDiagram(ske, pg, map);
            //vd.CreateVoronoiDiagramfrmSkeletonforBuildings();

            pg.CreatePgwithoutlongEdges(pg.NodeList, pg.EdgeList, 50);
            #endregion

            #region 冲突探测，解决冲突
            PrDispalce.工具类.CollabrativeDisplacement.CConflictDetector ccd = new 工具类.CollabrativeDisplacement.CConflictDetector();//创建冲突探测工具对象
            PrDispalce.工具类.CollabrativeDisplacement.ForceComputation pForceComputaiton = new 工具类.CollabrativeDisplacement.ForceComputation();
            ccd.ConflictDetectByPg(pg.PgwithouLongEdgesEdgesList, 7, double.Parse(this.textBox1.Text), map.PolygonList, map.PolylineList);
            #endregion

            #region 暂时无用
            //for (int i = 0; i < ccd.ConflictEdge.Count; i++)
            //{
            //    ProxiEdge Pe1 = ccd.ConflictEdge[i];

            //    #region 建筑物与道路次生冲突
            //    if (Pe1.Node1.FeatureType == FeatureType.PolygonType && Pe1.Node2.FeatureType == FeatureType.PolylineType)//node2在道路上
            //    {
            //        double f = double.Parse(this.textBox1.Text) - Pe1.NearestEdge.NearestDistance;
            //        double cos = (Pe1.NearestEdge.Point1.X - Pe1.NearestEdge.Point2.X) / Pe1.NearestEdge.NearestDistance;
            //        double sin = (Pe1.NearestEdge.Point1.Y - Pe1.NearestEdge.Point2.Y) / Pe1.NearestEdge.NearestDistance;

            //        #region 找到对应建筑物更新位置
            //        for (int j = 0; j < mapCopy2.PolygonList.Count; j++)
            //        {
            //            if (Pe1.Node1.TagID == mapCopy2.PolygonList[j].ID && mapCopy2.PolygonList[j].FeatureType==FeatureType.PolygonType)
            //            {
            //                foreach (TriNode curPoint in map.PolygonList[j].PointList)
            //                {
            //                    curPoint.X += f * cos;
            //                    curPoint.Y += f * sin;
            //                }
            //            }
            //        }
            //        #endregion
            //    }

            //    if (Pe1.Node1.FeatureType == FeatureType.PolylineType && Pe1.Node2.FeatureType == FeatureType.PolygonType)//node1在道路上
            //    {
            //        double f = double.Parse(this.textBox1.Text) - Pe1.NearestEdge.NearestDistance;
            //        double cos = (Pe1.NearestEdge.Point2.X - Pe1.NearestEdge.Point1.X) / Pe1.NearestEdge.NearestDistance;
            //        double sin = (Pe1.NearestEdge.Point2.Y - Pe1.NearestEdge.Point1.Y) / Pe1.NearestEdge.NearestDistance;

            //        #region 找到对应建筑物更新位置
            //        for (int j = 0; j < mapCopy2.PolygonList.Count; j++)
            //        {
            //            if (Pe1.Node2.TagID == mapCopy2.PolygonList[j].ID && mapCopy2.PolygonList[j].FeatureType==FeatureType.PolygonType)
            //            {
            //                foreach (TriNode curPoint in map.PolygonList[j].PointList)
            //                {
            //                    curPoint.X += f * cos;
            //                    curPoint.Y += f * sin;
            //                }
            //            }
            //        }
            //        #endregion
            //    }
            //    #endregion

            //    #region 建筑物间次生冲突
            //    if (Pe1.Node1.FeatureType == FeatureType.PolygonType && Pe1.Node2.FeatureType == FeatureType.PolygonType)
            //    {
            //        PolygonObject Po1 = null; PolygonObject Po2 = null;

            //        #region 找到冲突边对应的建筑物
            //        for (int j = 0; j < mapCopy2.PolygonList.Count; j++)
            //        {
            //            if (Pe1.Node1.TagID == mapCopy2.PolygonList[j].ID && mapCopy2.PolygonList[j].FeatureType==FeatureType.PolygonType)
            //            {
            //                Po1 = mapCopy2.PolygonList[j];
            //            }

            //            if (Pe1.Node2.TagID == mapCopy2.PolygonList[j].ID && mapCopy2.PolygonList[j].FeatureType==FeatureType.PolygonType)
            //            {
            //                Po2 = mapCopy2.PolygonList[j];
            //            }
            //        }
            //        #endregion

            //        vd.FieldBuildBasedonBuildings2(Po1.ID, Po2.ID, pg.PgwithouLongEdgesEdgesList, mapCopy2.PolygonList);
            //        pForceComputaiton.FieldBasedForceComputationforBuildings(vd.FielderListforBuildings1, pg.PgwithoutLongEdgesNodesList, pg.PgwithouLongEdgesEdgesList, 3, 0, double.Parse(this.textBox1.Text));
            //        pForceComputaiton.UpdataCoordsforPGbyForce_Group2(pg.NodeList, map, pForceComputaiton.CombinationForceListforBuildingField);//更新建筑物位置（对map做更新）
            //    }
            //    #endregion
            //}
            //#endregion
            #endregion

            pg.WriteProxiGraph2Shp(FilePath, "邻近图", pMap.SpatialReference, pg.NodeList, pg.EdgeList);
            pg.WriteProxiGraph2Shp(FilePath, "冲突边", pMap.SpatialReference, pg.NodeList, ccd.ConflictEdge);
            map.WriteResult2Shp(FilePath, pMap.SpatialReference);
        }

    }
}
