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

namespace PrDispalce.工具窗体
{
    public partial class ConflictDetect2 : Form
    {
        public ConflictDetect2(IMap cMap,AxMapControl mMapControl)
        {
            InitializeComponent();
            this.pMap = cMap;
            this.pMapControl = mMapControl;
        }

        #region 参数
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        string ConflictPath;
        AxMapControl pMapControl;
        //string localFilePath, fileNameExt, FilePath;
        #endregion

        #region 初始化
        private void ConflictDetect2_Load(object sender, EventArgs e)
        {
            this.checkBox1.Checked = true;

            if (this.pMap.LayerCount <= 0)
                return;

            ILayer pLayer;
            for (int i = 0; i < this.pMap.LayerCount; i++)
            {
                pLayer = this.pMap.get_Layer(i);
                IDataset LayerDataset = pLayer as IDataset;

                if (LayerDataset != null)
                {
                    IFeatureLayer pFeatureLayer = pLayer as IFeatureLayer;
                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                    {
                        this.listBox1.Items.Add(pLayer.Name);
                        this.comboBox1.Items.Add(pLayer.Name);
                        if (this.comboBox1.Items.Count > 0)
                        {
                            this.comboBox1.SelectedIndex = 0;
                        }
                    }

                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        this.comboBox2.Items.Add(pLayer.Name);
                        if (this.comboBox2.Items.Count > 0)
                        {
                            this.comboBox2.SelectedIndex = 0;
                        }
                    }
                }
            }

        }
        #endregion

        #region 冲突探测
        private void button1_Click(object sender, EventArgs e)
        {
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            AuxStructureLib.ConflictLib.ConflictDetector cd = new AuxStructureLib.ConflictLib.ConflictDetector();

            #region 图层获取
            if (this.checkBox1.Checked == true)
            {              
                for (int i = 0; i < this.listBox1.Items.Count; i++)
                {
                    IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.listBox1.Items[i].ToString());
                    list.Add(StreetLayer);
                }
            }

            if (this.checkBox1.Checked == false)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text.ToString());
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text.ToString());
                list.Add(StreetLayer); 
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            map.InterpretatePoint(2);
            #endregion

            #region 创建线图层的dt，cdt和骨架
            // 冲突探测需要设置比例尺和阈值
            //探测冲突，并将冲突的三角形区域进行输出
            if (this.checkBox1.Checked == true)
            {               
                DelaunayTin dt = new DelaunayTin(map.TriNodeList);
                dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

                ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
                cdt.CreateConsDTfromPolylineandPolygon(map.PolylineList, map.PolygonList);
                Triangle.WriteID(dt.TriangleList);
                TriEdge.WriteID(dt.TriEdgeList);

                AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
                ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();//删除相同道路内的三角形

                cd.Skel_arcList = ske.Skeleton_ArcList;
                cd.targetScale = 1000;               
                cd.threshold = double.Parse(this.textBox1.Text);              
                cd.DetectConflictasTriRegions();

                //冲突输出
                int cont = 0;
                foreach (AuxStructureLib.ConflictLib.Conflict_L cl in cd.ConflictList)
                {
                    (cl as AuxStructureLib.ConflictLib.Conflict_L).WriteConflict2Shp2(ConflictPath, cont,pMap.SpatialReference);
                    cont++;
                }
            }
            #endregion

            #region 面图层的 DT+CDT+SKE
            //探测冲突，并将冲突的线输出
            //冲突探测需要设置阈值和比例尺
            if (this.checkBox1.Checked == false)
            {
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

                //VoronoiDiagram vd = null;
                //vd = new AuxStructureLib.VoronoiDiagram(ske, pg, map);
                //vd.CreateVoronoiDiagramfrmSkeletonforBuildings();

                cd.Skel_arcList = ske.Skeleton_ArcList;
                cd.PG = pg;
                cd.threshold = double.Parse(this.textBox3.Text); 
                cd.targetScale = 1000;
                cd.DetectConflictByPG(map.PolygonList,pMapControl);//根据邻近图检测冲突

                //冲突输出
                int cont = 0;
                foreach (AuxStructureLib.ConflictLib.Conflict_R cl in cd.ConflictList)
                {
                    (cl as AuxStructureLib.ConflictLib.Conflict_R).WriteConflict2(ConflictPath,cont,pMap.SpatialReference);
                    cont++;
                }
            }
            #endregion          
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

            ConflictPath = outfilepath;
            this.comboBox3.Text = ConflictPath;         
        }
        #endregion
    }
}
