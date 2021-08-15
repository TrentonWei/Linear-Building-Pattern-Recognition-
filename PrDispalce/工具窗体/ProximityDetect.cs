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

namespace PrDispalce.工具窗体
{
    public partial class ProximityDetect : Form
    {
        public ProximityDetect(IMap cMap)
        {
            InitializeComponent();
            this.pMap = cMap;
        }

        #region 参数
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        string CDTOutPath,SKEOutPath;
        #endregion

        #region  初始化
        private void ProximityDetect_Load(object sender, EventArgs e)
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
                     IFeatureLayer pFeatureLayer = pLayer as IFeatureLayer;
                     if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                     {
                         this.comboBox1.Items.Add(strLayerName);
                         this.comboBox2.Items.Add(strLayerName);
                     }
                 }
            }

            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
                this.comboBox2.SelectedIndex = 0;
            }
        }
        #endregion

        #region CDT输出路径
        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fdialog = new FolderBrowserDialog();
            string outfilepath = null;

            if (fdialog.ShowDialog() == DialogResult.OK)
            {
                string Path = fdialog.SelectedPath;
                outfilepath = Path;
            }

            CDTOutPath = outfilepath;
            this.comboBox3.Text = CDTOutPath;
        }
        #endregion
         
        #region 确定
        private void button2_Click(object sender, EventArgs e)
        {
            #region 获取图层
            IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);

            List<IFeatureLayer> list = new List<IFeatureLayer>();
            list.Add(StreetLayer);
            list.Add(BuildingLayer);
            #endregion

            #region 读取数据
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrs();
            map.InterpretatePoint(1);//加密顶点的系数
            #endregion

            #region 创建dt，cdt和骨架
            DelaunayTin dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(map.PolylineList, map.PolygonList);
            Triangle.WriteID(dt.TriangleList);
            TriEdge.WriteID(dt.TriEdgeList);

            AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
            ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
            #endregion

            #region 输出
            if (CDTOutPath != null) { dt.WriteShp(CDTOutPath, pMap.SpatialReference); }
            //if (SKEOutPath != null) { ConflictDetection.WriteSHP.Create_WriteSkeleton_Segment2Shp(SKEOutPath, @"Skeleton_Seg", ske, esriSRProjCS4Type.esriSRProjCS_Beijing1954_3_Degree_GK_CM_108E); ; }
            #endregion
        }
        #endregion

        #region SKE输出路径
        private void button4_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fdialog = new FolderBrowserDialog();
            string outfilepath = null;

            if (fdialog.ShowDialog() == DialogResult.OK)
            {
                string Path = fdialog.SelectedPath;
                outfilepath = Path;
            }

            SKEOutPath = outfilepath;
            this.comboBox4.Text = SKEOutPath;
        }
        #endregion
    }
}
