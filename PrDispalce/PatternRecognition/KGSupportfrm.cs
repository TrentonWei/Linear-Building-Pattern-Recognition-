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

namespace PrDispalce.PatternRecognition
{
    public partial class KGSupportfrm : Form
    {
        public KGSupportfrm(IMap cMap)
        {
            InitializeComponent();
            this.pMap = cMap;
        }

        #region 参数
        IMap pMap;
        string OutPath;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        PrDispalce.工具类.ParameterCompute PC = new 工具类.ParameterCompute();
        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KGSupportfrm_Load(object sender, EventArgs e)
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

        /// <summary>
        /// 单选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (checkedListBox1.CheckedItems.Count > 0)
            {
                for (int i = 0; i < checkedListBox1.Items.Count; i++)
                {
                    if (i != e.Index)
                    {
                        // cklData.SetItemCheckState(i, CheckState.Unchecked);
                        checkedListBox1.SetItemChecked(i, false);
                    }
                }
            }
        }

        /// <summary>
        /// ok
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
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

            #region 空间特征计算+数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            foreach (PolygonObject Po in map.PolygonList)
            {
                Po.MBRO = PC.GetSMBROrientation(PC.ObjectConvert(Po));
                Po.tArea = PC.GetArea(PC.ObjectConvert(Po));
                Po.EdgeCount = Po.PointList.Count;
            }

            map.InterpretatePoint(2);
            #endregion

            #region 邻近关系计算
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
            
            if (this.checkedListBox1.SelectedItem.ToString() == "DT")
            {
                pg.CreatePgForBuildings(pg.NodeList, pg.EdgeList);
            }
            if (this.checkedListBox1.SelectedItem.ToString() == "RNG")
            {
                pg.CreateRNGForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            }
            if (this.checkedListBox1.SelectedItem.ToString() == "MST")
            {
                pg.CreateMSTForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            }
            if (this.checkedListBox1.SelectedItem.ToString() == "NNG")
            {
                pg.CreateNNGForBuildingShortestDistance();
            }
            #endregion
            
            #region 邻近相似关系计算
            double SizeConstraint = 100000000000; double ShapeConstraint = 100000000000; double OriConstraint = 90;
            if (this.checkBox1.Checked)
            {
                SizeConstraint = double.Parse(this.textBox1.Text.ToString());
            }
            if (this.checkBox2.Checked)
            {
                OriConstraint = double.Parse(this.textBox2.Text.ToString());
            }
            if (this.checkBox3.Checked)
            {
                ShapeConstraint=double.Parse(this.textBox3.Text.ToString());
            }

            for (int i = 0; i < pg.KGEdgesList.Count; i++)
            {
                ProxiEdge VisitedEdge = pg.KGEdgesList[i];//探测的初始边
                ProxiNode VisitedNode1 = VisitedEdge.Node1; ProxiNode VisitedNode2 = VisitedEdge.Node2;//当前被访问的节点

                PolygonObject Po1 = map.GetObjectbyID(VisitedNode1.TagID, FeatureType.PolygonType) as PolygonObject;
                PolygonObject Po2 = map.GetObjectbyID(VisitedNode2.TagID, FeatureType.PolygonType) as PolygonObject;
                bool SimLabel = pg.Sim(Po1, Po2, SizeConstraint, ShapeConstraint, OriConstraint);

                pg.KGEdgesList[i].SimR = SimLabel;
            }
            #endregion

            #region SLinearArrange关系计算
            double DistanceConstraint = 100000000000; double MinDis = 0; double AngleConstraint = 180;
            List<Tuple<ProxiEdge,ProxiEdge>> TripleList = new List<Tuple<ProxiEdge, ProxiEdge>>();
            if (this.checkBox4.Checked)
            {
                AngleConstraint = double.Parse(this.textBox4.Text.ToString());
            }
            if (this.checkBox5.Checked)
            {
                DistanceConstraint = double.Parse(this.textBox5.Text.ToString());
                MinDis = double.Parse(this.textBox7.Text.ToString());
            }
            for (int i = 0; i < pg.KGEdgesList.Count; i++)
            {
                ProxiEdge VisitedEdge = pg.KGEdgesList[i];//当前被访问的边
                ProxiNode VisitedNode1 = VisitedEdge.Node1; ProxiNode VisitedNode2 = VisitedEdge.Node2;//当前被访问的节点

                List<ProxiEdge> EdgeList1 = pg.ReturnEdgeList(pg.KGEdgesList, VisitedNode1);//Node1关联的边
                List<ProxiEdge> EdgeList2 = pg.ReturnEdgeList(pg.KGEdgesList, VisitedNode2);//Node2关联的边
                List<ProxiEdge> MixedEgdeList = EdgeList1.Union(EdgeList2).ToList();

                for (int j = 0; j < MixedEgdeList.Count; j++)
                {
                    if (!MixedEgdeList[j].KGVisit)
                    {
                        bool DistanceAccept = pg.DistanceConstrain(VisitedEdge, MixedEgdeList[j], DistanceConstraint, MinDis);
                        bool OrientationAccept = pg.OrientationConstrain(VisitedEdge, MixedEgdeList[j], AngleConstraint);

                        if (DistanceAccept && OrientationAccept)
                        {
                            Tuple<ProxiEdge,ProxiEdge> Triple=new Tuple<ProxiEdge,ProxiEdge>(VisitedEdge,MixedEgdeList[j]);
                            TripleList.Add(Triple);
                        }
                    }
                }

                pg.KGEdgesList[i].KGVisit = true;
            }
            #endregion

            #region CurLinearArrange关系计算

            #endregion

            #region 结果输出
            #endregion
        }

        /// <summary>
        /// OutPut FilePath
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
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
    }
}
