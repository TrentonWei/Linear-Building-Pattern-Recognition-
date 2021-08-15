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
using PrDispalce.地图要素;
using PrDispalce.工具类;

namespace PrDispalce.工具窗体
{
    public partial class pDisplace : Form
    {
        public pDisplace(IMap cMap)
        {
            InitializeComponent();
            this.pMap = cMap;
        }

        #region 参数
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        string localFilePath, fileNameExt, FilePath;
        #endregion

        #region 确定
        private void button1_Click(object sender, EventArgs e)
        {
            #region 读取数据
            List<IFeatureLayer> layerList = new List<IFeatureLayer>();
            IFeatureLayer PolygonFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text.ToString());
            IFeatureLayer PolylineFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox3.Text.ToString());
            layerList.Add(PolygonFeatureLayer); layerList.Add(PolylineFeatureLayer);
            PrDispalce.地图要素.MapReady MR = new 地图要素.MapReady(layerList);
            MR.Ready();
            #endregion

            #region 聚类分析
            PrDispalce.地图要素.PolygonLayer polygondata = MR.PPLayer;
            PolylineLayer polylinedata = MR.PLLayer;//从地图中获得的线数据图层
            List<PolygonObject> mpolyList = new List<PolygonObject>(); List<PolylineObject> mpolineList = new List<PolylineObject>();
            mpolyList = polygondata.PolygonList;//从地图上获得的面目标list
            mpolineList = polylinedata.PolylineList;//从地图上获得的线目标list
            List<PolygonCluster> mCluster = PolygonCluster.startAnalysis(mpolyList, double.Parse(textBox1.Text));//建筑物聚类（最后一个参数是聚类约束）
            #endregion

            #region 移位向量计算与移位
            GeoCalculation Gc = new GeoCalculation();
            List<PolygonCluster> list_a = Gc.buildingsAndRoadsConflict(mCluster, mpolineList, double.Parse(textBox2.Text));//探测类群与建筑物的冲突（最后一个参数是冲突约束）[建筑物与街道冲突]
            List<PolygonCluster> list_b = Gc.building_Road_Displacement(list_a, mpolineList, double.Parse(textBox3.Text),null);//首先解决道路与建筑物间的冲突
            List<PolygonObject> poly_list_a = Gc.cluster_PolygonList(list_b);
            List<PolygonCluster> list_c = PolygonCluster.startAnalysis(poly_list_a, double.Parse(textBox1.Text));//再次对建筑物聚类
            List<PolygonCluster> list_d = Gc.Displacement(list_c,double.Parse(this.textBox3.Text));//解决建筑物内部的冲突
            //需要进行迭代处理

             List<PolygonObject> list_e = Gc.cluster_PolygonList(list_d);
             SaveNewObjects.SavePolygons(list_e, pMap.SpatialReference, FilePath,fileNameExt);
            #endregion
        }
        #endregion

        #region 初始化
        private void pDisplace_Load(object sender, EventArgs e)
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
                        this.comboBox3.Items.Add(strLayerName);
                    }

                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        this.comboBox1.Items.Add(strLayerName);
                    }
                }
            }

            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }

            if (this.comboBox3.Items.Count > 0)
            {
                this.comboBox3.SelectedIndex = 0;
            }
        }
        #endregion

        #region 输出路径
        private void button2_Click(object sender, EventArgs e)
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

            this.comboBox2.Text = localFilePath;
        }
        #endregion
    }
}
