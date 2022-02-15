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

            #region 指标计算
            if (checkBox1.Checked)//Area
            {
                pFH.AddField(PerimeterFeatureClass, "Area", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double area1 = PC.GetArea((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "Area", area1);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox2.Checked)//Perimeter
            {
                pFH.AddField(PerimeterFeatureClass, "Perimeter", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    IPolygon pPolygon = (IPolygon)pFeature.Shape;
                    double length1 = pPolygon.Length;

                    pFH.DataStore(PerimeterFeatureClass, pFeature, "Perimeter", length1);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox3.Checked)//SArea
            {
                pFH.AddField(PerimeterFeatureClass, "SArea", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double SArea = PC.GetSArea((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "SArea", SArea);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox4.Checked)//CArea
            {
                pFH.AddField(PerimeterFeatureClass, "CArea", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double CArea = PC.GetCArea((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "CArea", CArea);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox5.Checked)//LChord
            {
                pFH.AddField(PerimeterFeatureClass, "LChord", esriFieldType.esriFieldTypeDouble);
                int Testint = 0;
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double LChord = PC.GetLChord((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "LChord", LChord);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox6.Checked)//MWO
            {
                pFH.AddField(PerimeterFeatureClass, "MWO", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double MWO = PC.GetMWOrientation((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "MWO", MWO);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox7.Checked)//SWWO
            {
                pFH.AddField(PerimeterFeatureClass, "SWWO", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double SWWO = PC.GetSWWOrientation((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "SWWO", SWWO);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox8.Checked)//MBRO
            {
                pFH.AddField(PerimeterFeatureClass, "MBRO", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double MBRO = PC.GetSMBROrientation((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "MBRO", MBRO);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox9.Checked)//EdgeCount
            {
                pFH.AddField(PerimeterFeatureClass, "EC", esriFieldType.esriFieldTypeInteger);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    int EdgeCount = PC.GetEdgeCount((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "EC", EdgeCount);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox10.Checked)//IPQ
            {
                pFH.AddField(PerimeterFeatureClass, "IPQ", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double IPQ = PC.GetIPQCom((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "IPQ", IPQ);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox11.Checked)//Cv
            {
                pFH.AddField(PerimeterFeatureClass, "Cv", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double Cv = PC.GetCv((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "Cv", Cv);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox12.Checked)//ShapeIndex
            {
                pFH.AddField(PerimeterFeatureClass, "SI", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double ShapeIndex = PC.GetShapeIndex((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "SI", ShapeIndex);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox13.Checked)//RCom
            {
                pFH.AddField(PerimeterFeatureClass, "RCom", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint=0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null&&Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double RCom = PC.GetRCom((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "RCom", RCom);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox14.Checked)//GibCom
            {
                pFH.AddField(PerimeterFeatureClass, "GibCom", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null&&Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double GibCom = PC.GetGibCom((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "GibCom", GibCom);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox15.Checked)//DCMCom
            {
                pFH.AddField(PerimeterFeatureClass, "DCMCom", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double DCMCom = PC.GetDCMCom((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "DCMCom", DCMCom);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox16.Checked)//BotCom
            {
                pFH.AddField(PerimeterFeatureClass, "BotCom", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double BotCom = PC.GetBotCom((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "BotCom", BotCom);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox17.Checked)//BoyCom
            {
                pFH.AddField(PerimeterFeatureClass, "BoyCom", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double BoyCom = PC.GetBoyCom((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "BoyCom", BoyCom);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }
            if (checkBox18.Checked)//ELL
            {
                pFH.AddField(PerimeterFeatureClass, "ELL", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double ELL = PC.GetELL((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "ELL", ELL);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox19.Checked)//Fd
            {
                pFH.AddField(PerimeterFeatureClass, "Fd", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double Fd = PC.GetFd((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "Fd", Fd);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }
            if (checkBox20.Checked)//Comp1
            {
                pFH.AddField(PerimeterFeatureClass, "Compl", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double Compl = PC.GetCompl((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "Compl", Compl);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }

            if (checkBox21.Checked)//NCSP
            {
                pFH.AddField(PerimeterFeatureClass, "NCSP", esriFieldType.esriFieldTypeDouble);
                IFeatureCursor pFeatureCursor = PerimeterFeatureClass.Update(null, false);
                int Testint = 0;
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null && Testint <= PerimeterFeatureClass.FeatureCount(null))
                {
                    Testint++;
                    double NCSP = PC.GetNCSP((IPolygon)pFeature.Shape);
                    pFH.DataStore(PerimeterFeatureClass, pFeature, "NCSP", NCSP);
                }

                pFeatureCursor.UpdateFeature(pFeature);
                pFeature = pFeatureCursor.NextFeature();
            }
            #endregion
        }

    }
}
