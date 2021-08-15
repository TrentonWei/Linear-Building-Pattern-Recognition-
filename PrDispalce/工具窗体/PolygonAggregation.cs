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
using ESRI.ArcGIS.CartographyTools;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Geoprocessor;

namespace PrDispalce.工具窗体
{
    public partial class PolygonAggregation : Form
    {
        public PolygonAggregation(IMap cMap)
        {
            InitializeComponent();
            this.pMap = cMap;
        }

        #region 参数
        IMap pMap;
        string localFilePath, fileNameExt, FilePath;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        #endregion 

        #region 初始化
        private void PolygonAggregation_Load(object sender, EventArgs e)
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
                        this.comboBox4.Items.Add(strLayerName);
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

            this.checkBox1.Checked = true;
        }
        #endregion

        #region 输出路径
        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            //saveFileDialog1.Filter = " shp files(*.shp)|";

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

        #region 确定
        private void button1_Click(object sender, EventArgs e)
        {
            ESRI.ArcGIS.CartographyTools.AggregatePolygons BuildingAggregation = new AggregatePolygons();
            Geoprocessor gp = new Geoprocessor();
            gp.OverwriteOutput = true;

            try
            {
                string s1 = comboBox1.Text; 
                IFeatureLayer tLayer1 = pFeatureHandle.GetLayer(pMap, s1);
                BuildingAggregation.in_features = tLayer1;
                BuildingAggregation.out_feature_class = this.comboBox2.Text;
                if (this.comboBox4 != null)
                {
                    string s2 = this.comboBox4.Text;
                    IFeatureLayer tLayer2 = pFeatureHandle.GetLayer(pMap, s2);
                    BuildingAggregation.barrier_features = tLayer2;
                }

                if (this.textBox1.Text != null)
                {
                    BuildingAggregation.aggregation_distance = this.textBox1.Text + " " + "Meters";
                }

                if (this.textBox2.Text != null)
                {
                    BuildingAggregation.minimum_area = this.textBox2.Text + " " + "SquareMeters";
                }

                if (this.textBox3.Text != null)
                {
                    BuildingAggregation.minimum_hole_size = this.textBox3.Text + " " + "SquareMeters";
                }

                if (this.checkBox1.Checked)
                {
                    BuildingAggregation.orthogonality_option = "ORTHOGONAL";
                }

                else
                {
                    BuildingAggregation.orthogonality_option = "NON_ORTHOGONAL"; 
                }

                gp.Execute(BuildingAggregation, null);

                #region 添加面合并图层
                IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();
                IWorkspace pWorkspace = pWorkspaceFactory.OpenFromFile(FilePath, 0);
                IFeatureWorkspace pFeatureWorkspace = pWorkspace as IFeatureWorkspace;
                IFeatureClass pFC = pFeatureWorkspace.OpenFeatureClass(fileNameExt);
                IFeatureLayer pFLayer = new FeatureLayerClass();
                pFLayer.FeatureClass = pFC;
                pFLayer.Name = pFC.AliasName;
                ILayer pLayer = pFLayer as ILayer;
                pMap.AddLayer(pLayer);
                #endregion

                //刷新视图
                IActiveView ActiveView = pMap as IActiveView;
                ActiveView.Refresh();
                MessageBox.Show("建筑物合并完毕");
            }

            catch
            {                
                //MessageBox.Show("建筑物合并失败");
                string ms = "";
                if (gp.MessageCount > 0)
                {
                    for (int Count = 0; Count <= gp.MessageCount - 1; Count++)
                    {
                        ms += gp.GetMessage(Count);
                    }
                }

                MessageBox.Show(ms);
            }
        }
        #endregion
    }
}
