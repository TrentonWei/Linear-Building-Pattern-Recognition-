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
    public partial class MSTtoPP : Form
    {
        public MSTtoPP(IMap cMap)
        {
            InitializeComponent();
            this.pMap = cMap;
        }

        #region 参数
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        #endregion

        #region 初始化
        private void MSTtoPP_Load(object sender, EventArgs e)
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

                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                    {
                        this.comboBox1.Items.Add(strLayerName);
                    }

                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        this.comboBox2.Items.Add(strLayerName);
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

        #region 确定（构建节点与建筑物和街道的对应关系）
        private void button1_Click(object sender, EventArgs e)
        {
            IFeatureClass PointFeatureClass = pFeatureHandle.GetFeatureClass(pMap, this.comboBox1.Text.ToString());
            IFeatureClass PolylineFeatureClass = pFeatureHandle.GetFeatureClass(pMap, this.comboBox3.Text.ToString());
            IFeatureClass PolygonFeatureClass = pFeatureHandle.GetFeatureClass(pMap, this.comboBox2.Text.ToString());
            pFeatureHandle.AddField(PointFeatureClass,"TouchID",esriFieldType.esriFieldTypeInteger);//用于存储点对应的街道或建筑物要素，一个建筑物只能对应一个要素

            for (int i = 0; i < PointFeatureClass.FeatureCount(null); i++)
            {
                IFeature pFeature1 = PointFeatureClass.GetFeature(i);
                if (pFeature1 != null)
                {
                    IPoint pPoint = pFeature1.Shape as IPoint;
                    IRelationalOperator pRelationalOperator = (IRelationalOperator)pPoint;

                    for (int j = 0; j < PolylineFeatureClass.FeatureCount(null); j++)
                    {
                        IFeature pFeature2 = PolylineFeatureClass.GetFeature(j);
                        if (pFeature2 != null)
                        {
                            IPolyline pPolyline = pFeature2.Shape as IPolyline;
                            if (pRelationalOperator.Touches(pPolyline) || pRelationalOperator.Overlaps(pPolyline))
                            {
                                pFeature1.set_Value(pFeature1.Fields.FindField("TouchID"), j);
                                pFeature1.Store();
                            }
                        }
                    }

                    for (int j = 0; j < PolygonFeatureClass.FeatureCount(null); j++)
                    {
                        IFeature pFeature2 = PolylineFeatureClass.GetFeature(j);
                        if (pFeature2 != null)
                        {
                            IPolygon pPolygon = pFeature2.Shape as IPolygon;
                            if (pRelationalOperator.Touches(pPolygon) || pRelationalOperator.Overlaps(pPolygon))
                            {
                                pFeature1.set_Value(pFeature1.Fields.FindField("TouchID"), j);
                                pFeature1.Store();
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}
