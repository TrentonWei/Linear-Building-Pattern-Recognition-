using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;


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
    public partial class RelationComputationFrm : Form
    {
        public RelationComputationFrm(AxMapControl axMapControl)
        {
            InitializeComponent();
            this.pMap = axMapControl.Map;
            this.pMapControl = axMapControl;
        }

        #region 参数
        IMap pMap;
        string OutPath;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        PrDispalce.工具类.ParameterCompute PC = new 工具类.ParameterCompute();       
        AxMapControl pMapControl;       
        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RelationComputationFrm_Load(object sender, EventArgs e)
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
                        this.comboBox3.Items.Add(strLayerName);
                    }
                    #endregion

                    #region 添加面图层
                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        this.comboBox1.Items.Add(strLayerName);
                        this.comboBox2.Items.Add(strLayerName);
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
            if (this.comboBox3.Items.Count > 0)
            {
                this.comboBox3.SelectedIndex = 0;
            }
            #endregion
        }

        /// <summary>
        /// OutPut
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            PrDispalce.BuildingSim.BuildingPairSim BPS = new BuildingSim.BuildingPairSim(pMapControl);

            #region 获取图层
            IFeatureLayer BuildingReLayer = null;
            IFeatureLayer BuildingRoadReBuLayer = null;
            IFeatureLayer BuildingRoadReRoLayer = null;
            if (this.comboBox1.Text != null)
            {
                BuildingReLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            }

            if (this.comboBox2.Text != null)
            {
                BuildingRoadReBuLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
            }

            if (this.comboBox3.Text != null)
            {
                BuildingRoadReRoLayer = pFeatureHandle.GetLayer(pMap, this.comboBox3.Text);
            }
            #endregion

            #region 建筑物间关系计算
            IFeatureClass BuildingReFeatureClass = BuildingReLayer.FeatureClass;
            int FeatureCount = BuildingReFeatureClass.FeatureCount(null);

            #region 尺寸相似关系计算
            Dictionary<Tuple<int, int>, double> SizeSimDic = new Dictionary<Tuple<int, int>, double>();            
            if (this.checkBox1.Checked)
            {
                for (int i = 0; i < FeatureCount - 1; i++)
                {
                    IFeature iFeature = BuildingReFeatureClass.GetFeature(i);
                    IPolygon iPolygon = iFeature.Shape as IPolygon;
                    for (int j = i + 1; j < FeatureCount; j++)
                    {
                        IFeature jFeature = BuildingReFeatureClass.GetFeature(j);
                        IPolygon jPolygon = jFeature.Shape as IPolygon;

                        double SizeSim = BPS.SizeSimilarity(iPolygon, jPolygon, 0);
                        Tuple<int, int> IDLabel = new Tuple<int, int>(i, j);
                        SizeSimDic.Add(IDLabel, SizeSim);
                    }
                }
            }
            #endregion

            #region 方向相似关系计算
            Dictionary<Tuple<int, int>, double> OriSimDic = new Dictionary<Tuple<int, int>, double>();
            if (this.checkBox2.Checked)
            {
                for (int i = 0; i < FeatureCount - 1; i++)
                {
                    IFeature iFeature = BuildingReFeatureClass.GetFeature(i);
                    IPolygon iPolygon = iFeature.Shape as IPolygon;
                    for (int j = i + 1; j < FeatureCount; j++)
                    {
                        IFeature jFeature = BuildingReFeatureClass.GetFeature(j);
                        IPolygon jPolygon = jFeature.Shape as IPolygon;

                        double OriSim = BPS.OrientationSimilarity(iPolygon, jPolygon, 1);
                        Tuple<int, int> IDLabel = new Tuple<int, int>(i, j);
                        OriSimDic.Add(IDLabel, OriSim);
                    }
                }
            }
            #endregion

            #region 形状相似关系计算
            Dictionary<Tuple<int, int>, double> ShapeSimDic = new Dictionary<Tuple<int, int>, double>();
            if (this.checkBox3.Checked)
            {
                for (int i = 0; i < FeatureCount - 1; i++)
                {
                    IFeature iFeature = BuildingReFeatureClass.GetFeature(i);
                    IPolygon iPolygon = iFeature.Shape as IPolygon;
                    for (int j = i + 1; j < FeatureCount; j++)
                    {
                        IFeature jFeature = BuildingReFeatureClass.GetFeature(j);
                        IPolygon jPolygon = jFeature.Shape as IPolygon;

                        double ShapeSim = BPS.ShapeCountSimilarity(iPolygon, jPolygon, 0);
                        Tuple<int, int> IDLabel = new Tuple<int, int>(i, j);
                        ShapeSimDic.Add(IDLabel, ShapeSim);
                    }
                }
            }
            #endregion

            #region 正对面积计算
            Dictionary<Tuple<int, int>, double> FRDic = new Dictionary<Tuple<int, int>, double>();
            if (this.checkBox4.Checked)
            {
                for (int i = 0; i < FeatureCount - 1; i++)
                {
                    IFeature iFeature = BuildingReFeatureClass.GetFeature(i);
                    IPolygon iPolygon = iFeature.Shape as IPolygon;
                    for (int j = i + 1; j < FeatureCount; j++)
                    {
                        IFeature jFeature = BuildingReFeatureClass.GetFeature(j);
                        IPolygon jPolygon = jFeature.Shape as IPolygon;

                        double FR = BPS.FRCompute(iPolygon, jPolygon);
                        Tuple<int, int> IDLabel = new Tuple<int, int>(i, j);
                        FRDic.Add(IDLabel, FR);
                    }
                }
            }
            #endregion

            #region OriMatrix计算
            Dictionary<Tuple<int, int>, double[,]> OriMatrixDic = new Dictionary<Tuple<int, int>, double[,]>();
            if (this.checkBox5.Checked)
            {
                for (int i = 0; i < FeatureCount - 1; i++)
                {
                    IFeature iFeature = BuildingReFeatureClass.GetFeature(i);
                    IPolygon iPolygon = iFeature.Shape as IPolygon;
                    for (int j = i + 1; j < FeatureCount; j++)
                    {
                        IFeature jFeature = BuildingReFeatureClass.GetFeature(j);
                        IPolygon jPolygon = jFeature.Shape as IPolygon;

                        double[,] OriMatrix = BPS.OritationComputation(iPolygon, jPolygon);
                        Tuple<int, int> IDLabel = new Tuple<int, int>(i, j);
                        OriMatrixDic.Add(IDLabel, OriMatrix);
                    }
                }
            }
            #endregion

            #region 距离关系计算
            Dictionary<Tuple<int, int>, double> DisDic = new Dictionary<Tuple<int, int>, double>();
            if (this.checkBox6.Checked)
            {
                for (int i = 0; i < FeatureCount - 1; i++)
                {
                    IFeature iFeature = BuildingReFeatureClass.GetFeature(i);
                    IPolygon iPolygon = iFeature.Shape as IPolygon;
                    for (int j = i + 1; j < FeatureCount; j++)
                    {
                        IFeature jFeature = BuildingReFeatureClass.GetFeature(j);
                        IPolygon jPolygon = jFeature.Shape as IPolygon;

                        double Dis = BPS.BuidlingDis(iPolygon, jPolygon,2);
                        Tuple<int, int> IDLabel = new Tuple<int, int>(i, j);
                        DisDic.Add(IDLabel, Dis);
                    }
                }
            }
            #endregion
            #endregion

            #region 建筑物与道路关系计算
            #region 距离关系计算
            if (this.checkBox7.Checked)
            {

            }
            #endregion
            #endregion

            #region 输出
            //OutMap.WriteResult2Shp(OutPath, pMap.SpatialReference);
            System.IO.FileStream SimTXT = new System.IO.FileStream(OutPath + @"\SimOutPut.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(SimTXT);

            #region 尺寸相似关系输出
            if (SizeSimDic.Count > 0)
            {
                sw.Write("SizeSimDic");
                foreach (KeyValuePair<Tuple<int, int>, double> kv in SizeSimDic)
                {
                    sw.Write(kv.Key);
                    sw.Write(" ");
                    sw.Write(kv.Value);
                    sw.Write("\r\n");
                }
            }
            #endregion

            #region 方向相似关系输出
            if (OriSimDic.Count > 0)
            {
                sw.Write("\r\n"); sw.Write("\r\n");
                sw.Write("OriSimDic");
                foreach (KeyValuePair<Tuple<int, int>, double> kv in OriSimDic)
                {
                    sw.Write(kv.Key);
                    sw.Write(" ");
                    sw.Write(kv.Value);
                    sw.Write("\r\n");
                }
            }
            #endregion

            #region 形状相似关系输出
            if (ShapeSimDic.Count > 0)
            {
                sw.Write("\r\n"); sw.Write("\r\n");
                sw.Write("ShapeSimDic");
                foreach (KeyValuePair<Tuple<int, int>, double> kv in ShapeSimDic)
                {
                    sw.Write(kv.Key);
                    sw.Write(" ");
                    sw.Write(kv.Value);
                    sw.Write("\r\n");
                }
            }
            #endregion

            #region 正对面积关系输出
            if (FRDic.Count > 0)
            {
                sw.Write("\r\n"); sw.Write("\r\n");
                sw.Write("FRDic");
                foreach (KeyValuePair<Tuple<int, int>, double> kv in FRDic)
                {
                    sw.Write(kv.Key);
                    sw.Write(" ");
                    sw.Write(kv.Value);
                    sw.Write("\r\n");
                }
            }
            #endregion

            #region  方向关系矩阵输出
            if (OriMatrixDic.Count > 0)
            {
                sw.Write("\r\n"); sw.Write("\r\n");
                sw.Write("OriMatrixDic");
                foreach (KeyValuePair<Tuple<int, int>, double[,]> kv in OriMatrixDic)
                {
                    sw.Write(kv.Key);
                    sw.Write(" ");
                    sw.Write(kv.Value);
                    sw.Write("\r\n");
                }
            }
            #endregion

            #region 距离关系输出
            if (DisDic.Count > 0)
            {
                sw.Write("\r\n"); sw.Write("\r\n");
                sw.Write("DisDic");
                foreach (KeyValuePair<Tuple<int, int>, double> kv in DisDic)
                {
                    sw.Write(kv.Key);
                    sw.Write(" ");
                    sw.Write(kv.Value);
                    sw.Write("\r\n");
                }
            }
            #endregion

            sw.Close();
            SimTXT.Close();
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
            this.comboBox4.Text = OutPath;
        }
    }
}
