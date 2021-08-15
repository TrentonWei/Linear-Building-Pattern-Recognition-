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
    public partial class CenterGet : Form
    {
        public CenterGet(AxMapControl mMapControl)
        {
            InitializeComponent();
            this.pMapControl = mMapControl;
            this.pMap = pMapControl.Map;
        }

        #region 参数
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        string ConflictPath;
        AxMapControl pMapControl;
        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CenterGet_Load(object sender, EventArgs e)
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
                  
                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        strLayerName = pLayer.Name;
                        this.comboBox1.Items.Add(strLayerName);
                       
                    }
                }
            }

            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 输出
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

            ConflictPath = outfilepath;
            this.comboBox2.Text = ConflictPath;     
        }

        /// <summary>
        /// 确定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            IFeatureLayer pFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            SMap PointMap = new SMap();

            IFeatureCursor pFeatureCursor = pFeatureClass.Update(null, true);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                IPolygon pPolygon = pFeature.Shape as IPolygon;
                IArea pArea = pPolygon as IArea;
                IPoint pPoint = pArea.Centroid;

                TriNode pNode = new TriNode();
                pNode.X = pPoint.X;
                pNode.Y = pPoint.Y;

                PointObject pPo = new PointObject(0, pNode);
                PointMap.PointList.Add(pPo);

                pFeature = pFeatureCursor.NextFeature();
            }

            PointMap.WriteResult2Shp(ConflictPath, pMap.SpatialReference);
        }
    }
}
