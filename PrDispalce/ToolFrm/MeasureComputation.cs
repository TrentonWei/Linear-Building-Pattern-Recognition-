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

namespace PrDispalce.ToolFrm
{
    public partial class MeasureComputation : Form
    {
        public MeasureComputation(IMap cMap)
        {
            InitializeComponent();
            this.pMap = cMap;
        }

        #region 参数
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFH = new 工具类.FeatureHandle();
        PrDispalce.工具类.ParameterCompute PC = new 工具类.ParameterCompute();
        #endregion

        //初始化
        private void MeasureComputation_Load(object sender, EventArgs e)
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
            #endregion
        }

        /// <summary>
        /// 计算
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            String LayerName = comboBox1.Text;
            IFeatureClass PerimeterFeatureClass = pFH.GetFeatureClass(pMap, LayerName);

            #region 添加字段
            if (checkBox1.Checked)
            {
                pFH.AddField(PerimeterFeatureClass, "Area", esriFieldType.esriFieldTypeDouble);
            }

            if (checkBox2.Checked)
            {
                pFH.AddField(PerimeterFeatureClass, "Perimeter", esriFieldType.esriFieldTypeDouble);
            }

            if (checkBox3.Checked)
            {
                pFH.AddField(PerimeterFeatureClass, "SArea", esriFieldType.esriFieldTypeDouble);
            }

            if (checkBox4.Checked)
            {
                pFH.AddField(PerimeterFeatureClass, "CArea", esriFieldType.esriFieldTypeDouble);
            }

            if (checkBox5.Checked)
            {
                pFH.AddField(PerimeterFeatureClass, "LChord", esriFieldType.esriFieldTypeDouble);
            }

            if (checkBox6.Checked)
            {
                pFH.AddField(PerimeterFeatureClass, "MWO", esriFieldType.esriFieldTypeDouble);
            }

            if (checkBox7.Checked)
            {
                pFH.AddField(PerimeterFeatureClass, "SWWO", esriFieldType.esriFieldTypeDouble);
            }

            if (checkBox8.Checked)
            {
                pFH.AddField(PerimeterFeatureClass, "MBRO", esriFieldType.esriFieldTypeDouble);
            }

             if (checkBox9.Checked)
            {
                pFH.AddField(PerimeterFeatureClass, "EC", esriFieldType.esriFieldTypeInteger);
            }

             if (checkBox10.Checked)
             {
                 pFH.AddField(PerimeterFeatureClass, "IPQ", esriFieldType.esriFieldTypeDouble);
             }

             if (checkBox11.Checked)
             {
                 pFH.AddField(PerimeterFeatureClass, "Cv", esriFieldType.esriFieldTypeDouble);
             }

             if (checkBox12.Checked)
             {
                 pFH.AddField(PerimeterFeatureClass, "SI", esriFieldType.esriFieldTypeDouble);
             }

             if (checkBox13.Checked)
             {
                 pFH.AddField(PerimeterFeatureClass, "RCom", esriFieldType.esriFieldTypeDouble);
             }

             if (checkBox14.Checked)
             {
                 pFH.AddField(PerimeterFeatureClass, "GibCom", esriFieldType.esriFieldTypeDouble);
             }

             if (checkBox15.Checked)
             {
                 pFH.AddField(PerimeterFeatureClass, "DCMCom", esriFieldType.esriFieldTypeDouble);
             }

             if (checkBox16.Checked)
             {
                 pFH.AddField(PerimeterFeatureClass, "BotCom", esriFieldType.esriFieldTypeDouble);
             }

             if (checkBox17.Checked)
             {
                 pFH.AddField(PerimeterFeatureClass, "BoyCom", esriFieldType.esriFieldTypeDouble);
             }

             if (checkBox18.Checked)
             {
                 pFH.AddField(PerimeterFeatureClass, "ELL", esriFieldType.esriFieldTypeDouble);
             }

             if (checkBox19.Checked)
             {
                 pFH.AddField(PerimeterFeatureClass, "Fd", esriFieldType.esriFieldTypeDouble);
             }

             if (checkBox20.Checked)
             {
                 pFH.AddField(PerimeterFeatureClass, "Compl", esriFieldType.esriFieldTypeDouble);
             }

             if (checkBox21.Checked)
             {
                 pFH.AddField(PerimeterFeatureClass, "NCSP", esriFieldType.esriFieldTypeDouble);
             }
            #endregion

            IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                #region 指标计算
                if (checkBox1.Checked)//Area
                {
                    double area1 = PC.GetArea((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "Area", area1);
                }

                if (checkBox2.Checked)//Perimeter
                {
                    IPolygon pPolygon = (IPolygon)pFeature.Shape;
                    double length1 = pPolygon.Length;

                    pFH.DataStore(PerimeterFeatureClass, pFeature, "Perimeter", length1);
                }

                if (checkBox3.Checked)//SArea
                {
                    double area1 = PC.GetSArea((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "SArea", area1);
                }

                if (checkBox4.Checked)//CArea
                {
                    double area1 = PC.GetCArea((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "CArea", area1);
                }

                if (checkBox5.Checked)//LChord
                {
                    double area1 = PC.GetLChord((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "LChord", area1);
                }

                if (checkBox6.Checked)//MWO
                {
                    double area1 = PC.GetMWOrientation((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "MWO", area1);
                }

                if (checkBox7.Checked)//SWWO
                {
                    double area1 = PC.GetSWWOrientation((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "SWWO", area1);
                }

                if (checkBox8.Checked)//MBRO
                {
                    double area1 = PC.GetSMBROrientation((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "MBRO", area1);
                }

                if (checkBox9.Checked)//EdgeCount
                {
                    int area1 = PC.GetEdgeCount((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "EC", area1);
                }

                if (checkBox10.Checked)//IPQ
                {
                    double area1 = PC.GetIPQCom((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "IPQ", area1);
                }

                if (checkBox11.Checked)//Cv
                {
                    double area1 = PC.GetCv((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "Cv", area1);
                }

                if (checkBox12.Checked)//ShapeIndex
                {
                    double area1 = PC.GetShapeIndex((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "SI", area1);
                }

                if (checkBox13.Checked)//RCom
                {
                    double area1 = PC.GetRCom((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "RCom", area1);
                }

                if (checkBox14.Checked)//GibCom
                {
                    double area1 = PC.GetGibCom((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "GibCom", area1);
                }

                if (checkBox15.Checked)//DCMCom
                {
                    double area1 = PC.GetDCMCom((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "DCMCom", area1);
                }

                if (checkBox16.Checked)//BotCom
                {
                    double area1 = PC.GetBotCom((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "BotCom", area1);
                }

                if (checkBox17.Checked)//BoyCom
                {
                    double area1 = PC.GetBoyCom((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "BoyCom", area1);
                }
                if (checkBox18.Checked)//ELL
                {
                    double area1 = PC.GetELL((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "ELL", area1);
                }

                if (checkBox19.Checked)//Fd
                {
                    double area1 = PC.GetFd((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "Fd", area1);
                }
                if (checkBox20.Checked)//Comp1
                {
                    double area1 = PC.GetCompl((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "Compl", area1);
                }

                if (checkBox21.Checked)//NCSP
                {
                    double area1 = PC.GetNCSP((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "NCSP", area1);
                }
                #endregion

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }
        }

    }
}
