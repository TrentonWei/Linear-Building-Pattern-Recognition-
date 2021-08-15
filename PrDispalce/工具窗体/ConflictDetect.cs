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

namespace PrDispalce.工具窗体
{
    public partial class ConflictDetect : Form
    {
        public ConflictDetect(IMap cMap)
        {
            InitializeComponent();
            this.pMap = cMap;
        }

        #region 参数
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        PrDispalce.工具类.ConflictDetect dConflictDetect = new 工具类.ConflictDetect();
        #endregion

        #region 建筑物冲突检测
        //将冲突的建筑物分组成团，并按BuildingConflictID给其赋值，赋值为0表示不冲突，赋值为1表示为1的一组建筑物冲突
        private void button2_Click(object sender, EventArgs e)
        {
            IFeatureClass PolygonFeatureClass = pFeatureHandle.GetFeatureClass(pMap, this.comboBox2.Text.ToString());
            IFeatureClass VGraphFeatureClass = pFeatureHandle.GetFeatureClass(pMap, this.comboBox3.Text.ToString());
            double MinDis = double.Parse(textBox1.Text);

            dConflictDetect.BuildingConflicDetect(VGraphFeatureClass, PolygonFeatureClass, MinDis);
        }
        #endregion

        #region 道路与建筑物冲突探测（将与道路冲突的建筑物标记出来，并标识与几条道路冲突）
        private void button1_Click(object sender, EventArgs e)
        {
            IFeatureClass PolygonFeatureClass = pFeatureHandle.GetFeatureClass(pMap, this.comboBox2.Text.ToString());
            IFeatureClass VGraphFeatureClass = pFeatureHandle.GetFeatureClass(pMap, this.comboBox3.Text.ToString());
            IFeatureClass LineFeatureClass = pFeatureHandle.GetFeatureClass(pMap, this.comboBox1.Text.ToString());
            double MinDis = double.Parse(textBox2.Text);

            dConflictDetect.RoadConflictDetect(VGraphFeatureClass, PolygonFeatureClass, LineFeatureClass, MinDis);
        }
        #endregion

        #region 初始化
        private void ConflictDetect_Load(object sender, EventArgs e)
        {
            if (this.pMap.LayerCount <= 0)
                return;

            ILayer pLayer;
            string strLayerName;
            for (int i = 0; i < this.pMap.LayerCount; i++)
            {
                pLayer = this.pMap.get_Layer(i);
                IDataset LayerDataset = pLayer as IDataset;

                if (LayerDataset != null)
                {
                    IFeatureLayer pFeatureLayer = pLayer as IFeatureLayer;
                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                    {
                        strLayerName = pLayer.Name;
                        this.comboBox1.Items.Add(strLayerName);
                    }

                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        strLayerName = pLayer.Name;
                         this.comboBox2.Items.Add(strLayerName);
                        this.comboBox3.Items.Add(strLayerName);
                    }
                }            
            }

            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }
            if (this.comboBox2.Items.Count > 0)
            {
                this.comboBox2.SelectedIndex = 0;
            }
            if (this.comboBox3.Items.Count > 0)
            {
                this.comboBox3.SelectedIndex = 0;
            }
        }
        #endregion
    }
}
