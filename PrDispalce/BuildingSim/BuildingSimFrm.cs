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
using PrDispalce.PatternRecognition;

namespace PrDispalce.BuildingSim
{
    public partial class BuildingSimFrm : Form
    {
        public BuildingSimFrm(AxMapControl axMapControl)
        {
            InitializeComponent();
            this.pMap = axMapControl.Map;
            this.pMapControl = axMapControl;
        }

        #region Parameters
        AxMapControl pMapControl;
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        string OutPath;
        PrDispalce.BuildingSim.PublicUtil Pu = new PublicUtil();
        #endregion

        /// <summary>
        /// initialize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BuildingSimFrm_Load(object sender, EventArgs e)
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
                    #region 添加面图层
                    IFeatureLayer pFeatureLayer = pLayer as IFeatureLayer;

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
            #endregion
        }

        /// <summary>
        /// 计算两个建筑物图形的相似性
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            #region 获得对应建筑物
            List<IFeature> TargetFeatureList = new List<IFeature>(); ;
            if (this.comboBox1.Text != null)
            {
                TargetFeatureList = pFeatureHandle.GetFeatures(pMap, this.comboBox1.Text);
            }
            List<IFeature> MatchingFeatureList = new List<IFeature>(); ;
            if (this.comboBox2.Text != null)
            {
                MatchingFeatureList = pFeatureHandle.GetFeatures(pMap, this.comboBox2.Text);
            }
            #endregion

            #region 计算相似性
            PrDispalce.BuildingSim.BuildingPairSim BS = new BuildingPairSim(pMapControl);

            double[,] SimMartxi=new double[TargetFeatureList.Count,MatchingFeatureList.Count];
            for (int i = 0; i < TargetFeatureList.Count; i++)
            {
                IPolygon TargetPo=TargetFeatureList[i].Shape as IPolygon;
                for (int j = 0; j < MatchingFeatureList.Count; j++)
                {
                    IPolygon MatchingPo=MatchingFeatureList[j].Shape as IPolygon;
                    double MDSim = BS.MDComputation(TargetPo, MatchingPo);

                    SimMartxi[i, j] = MDSim;
                }
            }
            #endregion

            #region 输出
            System.IO.FileStream SimTXT = new System.IO.FileStream(OutPath+@"\MDSim.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(SimTXT);

            for (int i = 0; i < TargetFeatureList.Count; i++)
            {
                for (int j = 0; j < MatchingFeatureList.Count; j++)
                {
                    sw.Write(SimMartxi[i,j]); sw.Write(" ");
                }

                sw.Write("\r\n");
            }
            sw.Close();
            SimTXT.Close();            
            #endregion
        }

        /// <summary>
        /// 输出路径
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
            this.comboBox3.Text = OutPath;
        }

        /// <summary>
        /// 空间方向关系测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            #region 获得对应建筑物
            List<IFeature> TargetFeatureList = new List<IFeature>(); ;
            if (this.comboBox1.Text != null)
            {
                TargetFeatureList = pFeatureHandle.GetFeatures(pMap, this.comboBox1.Text);
            }
            List<IFeature> MatchingFeatureList = new List<IFeature>(); ;
            if (this.comboBox2.Text != null)
            {
                MatchingFeatureList = pFeatureHandle.GetFeatures(pMap, this.comboBox2.Text);
            }
            #endregion

            #region 计算相似性
            PrDispalce.BuildingSim.BuildingPairSim BS= new BuildingPairSim(pMapControl);
            PublicUtil Pu = new PublicUtil();

            for (int i = 0; i < TargetFeatureList.Count; i++)
            {
                IPolygon TargetPo = TargetFeatureList[i].Shape as IPolygon;
                for (int j = 0; j < MatchingFeatureList.Count; j++)
                {
                    IPolygon MatchingPo = MatchingFeatureList[j].Shape as IPolygon;
                    PolygonObject oTargetPo = Pu.PolygonConvert(TargetPo);
                    PolygonObject oMatchPo = Pu.PolygonConvert(MatchingPo);
                    double[,] OriMatrix = BS.OritationComputation(oTargetPo, oMatchPo);
                }
            }
            #endregion
        }

        /// <summary>
        /// 基于相似性的渐进式剖分
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            List<List<IPolygon>> mMatchedPolygons = new List<List<IPolygon>>();

            #region 获得对应建筑物
            List<IFeature> TargetFeatureList = new List<IFeature>(); 
            if (this.comboBox1.Text != null)
            {
                TargetFeatureList = pFeatureHandle.GetFeatures(pMap, this.comboBox1.Text);
            }
            List<IFeature> MatchingFeatureList = new List<IFeature>(); 
            if (this.comboBox2.Text != null)
            {
                MatchingFeatureList = pFeatureHandle.GetFeatures(pMap, this.comboBox2.Text);
            }
            #endregion

            #region 计算相似性
            PrDispalce.BuildingSim.ContextSim CS = new ContextSim(pMapControl);
            PublicUtil Pu = new PublicUtil();
            proPolygonCut pPC = new proPolygonCut(pMapControl);

            List<IPolygon> CacheList = new List<IPolygon>();
            for (int i = 0; i < TargetFeatureList.Count; i++)
            {
                #region 剖分
                IPolygon TargetPo = TargetFeatureList[i].Shape as IPolygon;
                IArea tArea = TargetPo as IArea;
                
                CuttedPolygon TargetCutPo = new CuttedPolygon(TargetPo);
                for (int j = 0; j < MatchingFeatureList.Count; j++)
                {
                    #region 渐进式剖分
                    IPolygon CacheMatchingPo = MatchingFeatureList[j].Shape as IPolygon;
                    IArea mArea = CacheMatchingPo as IArea;
                    IPolygon MatchingPo = Pu.GetEnlargedPolygon(CacheMatchingPo, tArea.Area / mArea.Area);
                    CuttedPolygon MatchingCutPo = new CuttedPolygon(MatchingPo);
                    
                    double OutSim = 0;
                    while (pPC.CutOver(TargetCutPo.MatchedPolygons, MatchingCutPo.MatchedPolygons, 5))
                    {
                        List<List<CuttedPolygon>> CuttedPolygonPairs = pPC.GetStepCuttedPolygons(TargetCutPo, MatchingCutPo, 5, 5, 3);

                        #region 依次选择Sim最大的剖分
                        List<CuttedPolygon> stepCuttedPolygonPair = new List<CuttedPolygon>();
                        double MaxSim = 0;
                        for (int k = 0; k < CuttedPolygonPairs.Count; k++)
                        {
                            //double MatchSim = CS.CuttedPolygonSim(CuttedPolygonPairs[k][0], CuttedPolygonPairs[k][1], 0.6, 0.4, 0.2, 0.2, 0.2, 0.4, 0, 2);
                            double MatchSim = CS.CuttedPolygonSim(CuttedPolygonPairs[k][0], CuttedPolygonPairs[k][1], 0, 2);
                            if (MatchSim > MaxSim)
                            {
                                MaxSim = MatchSim;
                                stepCuttedPolygonPair = CuttedPolygonPairs[k];
                            }
                        }
                        TargetCutPo = stepCuttedPolygonPair[0];
                        MatchingCutPo = stepCuttedPolygonPair[1];
                        #endregion

                        OutSim = MaxSim;
                    }
                    #endregion

                    #region 添加CuttedPolygon
                    //for (int m = 0; m < TargetCutPo.CuttedPolygons.Count; m++)
                    //{
                    //    CacheList.Add(TargetCutPo.CuttedPolygons[m]);
                    //}

                    //for (int n = 0; n < MatchingCutPo.CuttedPolygons.Count; n++)
                    //{
                    //    CacheList.Add(MatchingCutPo.CuttedPolygons[n]);
                    //}
                    #endregion

                    #region 添加MatchededPolygon
                    for (int m = 0; m < TargetCutPo.MatchedPolygons.Count; m++)
                    {
                        CacheList.Add(TargetCutPo.MatchedPolygons[m]);
                    }

                    for (int n = 0; n < MatchingCutPo.MatchedPolygons.Count; n++)
                    {
                        CacheList.Add(MatchingCutPo.MatchedPolygons[n]);
                    }
                    #endregion
                }
                #endregion
            }
            #endregion

            #region 输出
            SMap map3 = new SMap();
            map3.ReadDataFrmGivenPolygonObject(CacheList);
            map3.WriteResult2Shp(OutPath, pMap.SpatialReference);
            #endregion
        }

        /// <summary>
        /// 测试专用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            #region 获得对应建筑物
            List<IFeature> TargetFeatureList = new List<IFeature>(); ;
            if (this.comboBox1.Text != null)
            {
                TargetFeatureList = pFeatureHandle.GetFeatures(pMap, this.comboBox1.Text);
            }
            #endregion

            #region 测试
            PublicUtil Pu = new PublicUtil();

            for (int i = 0; i < TargetFeatureList.Count; i++)
            {
                IPolygon TargetPo = TargetFeatureList[i].Shape as IPolygon;

                PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
                object PolygonSb = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 100, 100, 100);

                IPolygon enlargedPolygon = Pu.GetEnlargedPolygon(TargetPo, 2);
                pMapControl.DrawShape(enlargedPolygon, ref PolygonSb);
                
                //int TestLocation = 0;
            }
            #endregion
        }

        /// <summary>
        /// 邻近性获取测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            #region 获得对应建筑物
            List<IFeature> TargetFeatureList = new List<IFeature>(); ;
            if (this.comboBox1.Text != null)
            {
                TargetFeatureList = pFeatureHandle.GetFeatures(pMap, this.comboBox1.Text);
            }
            #endregion

            #region 邻近性获取
            List<IPolygon> PoList = new List<IPolygon>();
            for (int i = 0; i < TargetFeatureList.Count;i++ )
            {
                PoList.Add(TargetFeatureList[i].Shape as IPolygon);
            }

            ContextSim CS = new ContextSim();
            CS.GetAdjacentPair(PoList);
            #endregion
        }

        /// <summary>
        /// 实验1，模板匹配相似性计算
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            List<List<IPolygon>> mMatchedPolygons = new List<List<IPolygon>>();
            List<List<double>> SimList = new List<List<double>>();

            #region 获得对应建筑物
            List<IFeature> TargetFeatureList = new List<IFeature>(); ;
            if (this.comboBox1.Text != null)
            {
                TargetFeatureList = pFeatureHandle.GetFeatures(pMap, this.comboBox1.Text);
            }
            List<IFeature> MatchingFeatureList = new List<IFeature>(); 
            if (this.comboBox2.Text != null)
            {
                MatchingFeatureList = pFeatureHandle.GetFeatures(pMap, this.comboBox2.Text);
            }
            #endregion

            #region 计算相似性
            PrDispalce.BuildingSim.ContextSim CS = new ContextSim(pMapControl);
            PublicUtil Pu = new PublicUtil();
            proPolygonCut pPC = new proPolygonCut(pMapControl);

            List<IPolygon> CacheList = new List<IPolygon>();
            for (int i = 0; i < TargetFeatureList.Count; i++)
            {
                List<double> CacheSimList = new List<double>();
                Console.WriteLine(i.ToString());

                #region 剖分
                for (int j = 0; j < MatchingFeatureList.Count; j++)
                {
                    IPolygon TargetPo = TargetFeatureList[i].Shape as IPolygon;
                    IArea tArea = TargetPo as IArea;
                    CuttedPolygon TargetCutPo = new CuttedPolygon(TargetPo);
                    Console.WriteLine(j.ToString());

                    #region 渐进式剖分
                    IPolygon CacheMatchingPo = MatchingFeatureList[j].Shape as IPolygon;
                    IArea mArea = CacheMatchingPo as IArea;
                    IPolygon MatchingPo = Pu.GetEnlargedPolygon(CacheMatchingPo, tArea.Area / mArea.Area);
                    CuttedPolygon MatchingCutPo = new CuttedPolygon(MatchingPo);

                    double OutSim = 0;
                    List<List<CuttedPolygon>> CuttedPolygonPairs = new List<List<CuttedPolygon>>();
                    while (pPC.CutOver(TargetCutPo.MatchedPolygons, MatchingCutPo.MatchedPolygons, 5))
                    {
                        CuttedPolygonPairs = pPC.GetStepCuttedPolygons(TargetCutPo, MatchingCutPo, 5, 5, 3);

                        if (CuttedPolygonPairs.Count > 0)
                        {
                            #region 依次选择Sim最大的剖分
                            List<CuttedPolygon> stepCuttedPolygonPair = new List<CuttedPolygon>();
                            double MaxSim = 0;
                            for (int k = 0; k < CuttedPolygonPairs.Count; k++)
                            {
                                //double MatchSim = CS.CuttedPolygonSim(CuttedPolygonPairs[k][0], CuttedPolygonPairs[k][1], 0.4, 0.6, 0.15, 0.15, 0.15, 0.55, 0, 2);
                                double MatchSim = CS.CuttedPolygonSim(CuttedPolygonPairs[k][0], CuttedPolygonPairs[k][1], 0, 2);
                                if (MatchSim > MaxSim)
                                {
                                    MaxSim = MatchSim;
                                    stepCuttedPolygonPair = CuttedPolygonPairs[k];
                                }
                            }
                            TargetCutPo = stepCuttedPolygonPair[0];
                            MatchingCutPo = stepCuttedPolygonPair[1];
                            #endregion

                            OutSim = MaxSim;
                        }

                        else
                        {
                            break;
                        }
                    }
                    #endregion

                    CacheSimList.Add(OutSim);
                }
                #endregion

                SimList.Add(CacheSimList);
            }
            #endregion

            #region 输出
            System.IO.FileStream SimTXT = new System.IO.FileStream(OutPath + @"\MDSim.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(SimTXT);

            for (int i = 0; i < SimList.Count; i++)
            {
                sw.Write(SimList[i].IndexOf(SimList[i].Max()));
                sw.Write(" ");
                sw.Write(SimList[i].Max());
                sw.Write("\r\n");
            }

            sw.Close();
            SimTXT.Close();
            #endregion
        }

        /// <summary>
        /// 基于相似性的一次渐进式剖分
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button9_Click(object sender, EventArgs e)
        {
            List<List<IPolygon>> mMatchedPolygons = new List<List<IPolygon>>();

            #region 获得对应建筑物
            List<IFeature> TargetFeatureList = new List<IFeature>(); ;
            if (this.comboBox1.Text != null)
            {
                TargetFeatureList = pFeatureHandle.GetFeatures(pMap, this.comboBox1.Text);
            }
            List<IFeature> MatchingFeatureList = new List<IFeature>(); ;
            if (this.comboBox2.Text != null)
            {
                MatchingFeatureList = pFeatureHandle.GetFeatures(pMap, this.comboBox2.Text);
            }
            #endregion

            #region 计算相似性
            PrDispalce.BuildingSim.ContextSim CS = new ContextSim(pMapControl);
            PublicUtil Pu = new PublicUtil();
            proPolygonCut pPC = new proPolygonCut(pMapControl);
            List<IPolygon> CacheList = new List<IPolygon>();
            IPolygon TargetPo = TargetFeatureList[0].Shape as IPolygon;
            IArea tArea = TargetPo as IArea;
            CuttedPolygon TargetCutPo = new CuttedPolygon(TargetPo);
            IPolygon CacheMatchingPo = MatchingFeatureList[0].Shape as IPolygon;
            IArea mArea = CacheMatchingPo as IArea;
            IPolygon MatchingPo = Pu.GetEnlargedPolygon(CacheMatchingPo, tArea.Area / mArea.Area);
            CuttedPolygon MatchingCutPo = new CuttedPolygon(MatchingPo);

            double OutSim = 0;

            List<List<CuttedPolygon>> CuttedPolygonPairs = pPC.GetStepCuttedPolygons(TargetCutPo, MatchingCutPo, 5, 5, 3);

            #region 依次选择Sim最大的剖分
            List<CuttedPolygon> stepCuttedPolygonPair = new List<CuttedPolygon>();
            double MaxSim = 0;
            for (int k = 0; k < CuttedPolygonPairs.Count; k++)
            {
                //double MatchSim = CS.CuttedPolygonSim(CuttedPolygonPairs[k][0], CuttedPolygonPairs[k][1], 0.6, 0.4, 0.2, 0.2, 0.2, 0.4, 0, 2);
                double MatchSim = CS.CuttedPolygonSim(CuttedPolygonPairs[k][0], CuttedPolygonPairs[k][1],0, 2);
                if (MatchSim > MaxSim)
                {
                    MaxSim = MatchSim;
                    stepCuttedPolygonPair = CuttedPolygonPairs[k];
                }
            }
            TargetCutPo = stepCuttedPolygonPair[0];
            MatchingCutPo = stepCuttedPolygonPair[1];
            #endregion

            OutSim = MaxSim;

            #region 添加CuttedPolygon
            //for (int m = 0; m < TargetCutPo.CuttedPolygons.Count; m++)
            //{
            //    CacheList.Add(TargetCutPo.CuttedPolygons[m]);
            //}

            //for (int n = 0; n < MatchingCutPo.CuttedPolygons.Count; n++)
            //{
            //    CacheList.Add(MatchingCutPo.CuttedPolygons[n]);
            //}
            #endregion

            #region 添加MatchededPolygon
            for (int m = 0; m < TargetCutPo.MatchedPolygons.Count; m++)
            {
                CacheList.Add(TargetCutPo.MatchedPolygons[m]);
            }

            for (int n = 0; n < MatchingCutPo.MatchedPolygons.Count; n++)
            {
                CacheList.Add(MatchingCutPo.MatchedPolygons[n]);
            }
            #endregion
            #endregion

            #region 输出
            SMap map3 = new SMap();
            map3.ReadDataFrmGivenPolygonObject(CacheList);
            map3.WriteResult2Shp(OutPath, pMap.SpatialReference);
            #endregion
        }

        /// <summary>
        /// 获取给定多边形的分割线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button10_Click(object sender, EventArgs e)
        {
            SMap OutMap = new SMap();
            ConcaveNodeSolve CNS = new ConcaveNodeSolve();
            PolygonCut PC=new PolygonCut();

            #region 获得对应建筑物
            List<IFeature> TargetFeatureList = new List<IFeature>(); ;
            if (this.comboBox1.Text != null)
            {
                TargetFeatureList = pFeatureHandle.GetFeatures(pMap, this.comboBox1.Text);
            }
            #endregion

            #region 获得对应的Cut
            for (int i = 0; i < TargetFeatureList.Count; i++)
            {
                IPolygon TargetPolygon = TargetFeatureList[i].Shape as IPolygon;
                PolygonObject TargetPo = Pu.PolygonConvert(TargetPolygon);

                List<Cut> CutList = PC.CutList(TargetPo, 15, 3);
                CNS.GetCutProperty(CutList, TargetPo,3);//判断Cut之间的相交关系和凹点消除关系
                List<List<Cut>> satCuts = PC.satifiedCuts(CutList, TargetPo, 3);

                foreach(List<Cut> pCutList in satCuts)
                {
                    foreach (Cut pCut in pCutList)
                    {
                        List<TriNode> CacheNodeList = new List<TriNode>();
                        CacheNodeList.Add(pCut.CutEdge.startPoint);
                        CacheNodeList.Add(pCut.CutEdge.endPoint);
                        PolylineObject CachePl = new PolylineObject(CacheNodeList);
                        OutMap.PolylineList.Add(CachePl);
                    }
                }
            }
            #endregion

            OutMap.WriteResult2Shp(OutPath, pMap.SpatialReference);
        }

        /// <summary>
        /// 获取匹配关系
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button11_Click(object sender, EventArgs e)
        {
            SMap OutMap = new SMap();
            ConcaveNodeSolve CNS = new ConcaveNodeSolve(this.pMapControl);
            PolygonCut PC = new PolygonCut();
            PrDispalce.BuildingSim.ContextSim CS = new ContextSim();

            #region 获得对应建筑物
            List<IFeature> TargetFeatureList = new List<IFeature>(); 
            if (this.comboBox1.Text != null)
            {
                TargetFeatureList = pFeatureHandle.GetFeatures(pMap, this.comboBox1.Text);
            }
            List<IFeature> MatchingFeatureList = new List<IFeature>(); 
            if (this.comboBox2.Text != null)
            {
                MatchingFeatureList = pFeatureHandle.GetFeatures(pMap, this.comboBox2.Text);
            }
            #endregion

            #region 获得对应的Cut
            Dictionary<int, int> simMatch = new Dictionary<int, int>();
            for (int i = 0; i < TargetFeatureList.Count; i++)
            {
                IPolygon TargetPolygon = TargetFeatureList[i].Shape as IPolygon;
                PolygonObject TargetPo = Pu.PolygonConvert(TargetPolygon);

                List<Cut> tCutList = PC.CutList(TargetPo, 15, 3);
                CNS.GetCutProperty(tCutList, TargetPo, 3);//判断Cut之间的相交关系和凹点消除关系
                List<List<Cut>> tsatCuts = PC.satifiedCuts(tCutList, TargetPo, 3);

                #region 获取建筑物i与匹配建筑物的匹配结果
                List<double> PoiSimList = new List<double>();//建筑物Po i与MatchingPo中建筑物的相似性 
                for (int j = 0; j < MatchingFeatureList.Count; j++)
                {
                    Console.Write(i.ToString() + "_" + j.ToString());

                    IPolygon MatchingPolygon = MatchingFeatureList[j].Shape as IPolygon;
                    MatchingPolygon = Pu.GetEnlargedPolygon(MatchingPolygon, TargetPolygon);
                    PolygonObject MatchingPo = Pu.PolygonConvert(MatchingPolygon);

                    List<Cut> mCutList = PC.CutList(MatchingPo, 15, 3);
                    CNS.GetCutProperty(mCutList, MatchingPo, 3);//判断Cut之间的相交关系和凹点消除关系
                    List<List<Cut>> msatCuts = PC.satifiedCuts(mCutList, MatchingPo, 3);

                    #region 获取不同剖分下的相似性结果
                    List<double> CachePoSimList = new List<double>();//存储Po i和Po j不同分割条件下的相似度                   

                    if (tsatCuts.Count == 0)
                    {
                        List<PolygonObject> TargetPos = new List<PolygonObject>();
                        TargetPos.Add(Pu.PolygonConvert(TargetPolygon));

                        #region 计算依据不同Cut形成的匹配相似度
                        if (msatCuts.Count == 0)
                        {
                            List<PolygonObject> MatchingPos = new List<PolygonObject>();
                            MatchingPos.Add(Pu.PolygonConvert(MatchingPolygon));

                            GraphMatching GM = new GraphMatching(TargetPos, MatchingPos);
                            GM.GraphMatchingProcess(0.1);

                            CuttedPolygon sourceCuttedPos = new CuttedPolygon(TargetPolygon, GM.sourcePolygons, GM.MatchingsourcePolygons);
                            CuttedPolygon targetCuttedPos = new CuttedPolygon(MatchingPolygon, GM.targetPolygons, GM.MatchingtargetPolygons);

                            double MatchSim = CS.CuttedPolygonSim(sourceCuttedPos, targetCuttedPos, 0, 2);
                            CachePoSimList.Add(MatchSim);
                        }

                        else
                        {
                            for (int n = 0; n < msatCuts.Count; n++)
                            {
                                List<PolygonObject> MatchingPos = CNS.GetPoListAfterCut(MatchingPolygon as Polygon, msatCuts[n]);
                                //OutMap.PolygonList.AddRange(MatchingPos);
                                GraphMatching GM = new GraphMatching(TargetPos, MatchingPos);
                                GM.GraphMatchingProcess(0.1);

                                CuttedPolygon sourceCuttedPos = new CuttedPolygon(TargetPolygon, GM.sourcePolygons, GM.MatchingsourcePolygons);
                                CuttedPolygon targetCuttedPos = new CuttedPolygon(MatchingPolygon, GM.targetPolygons, GM.MatchingtargetPolygons);

                                double MatchSim = CS.CuttedPolygonSim(sourceCuttedPos, targetCuttedPos, 0, 2);
                                CachePoSimList.Add(MatchSim);
                            }
                        }
                        #endregion
                    }

                    else
                    {
                        for (int m = 0; m < tsatCuts.Count; m++)
                        {
                            List<PolygonObject> TargetPos = CNS.GetPoListAfterCut(TargetPolygon as Polygon, tsatCuts[m]);
                            //OutMap.PolygonList.AddRange(TargetPos);

                            #region 计算依据不同Cut形成的匹配相似度
                            if (msatCuts.Count == 0)
                            {
                                List<PolygonObject> MatchingPos = new List<PolygonObject>();
                                MatchingPos.Add(Pu.PolygonConvert(MatchingPolygon));

                                GraphMatching GM = new GraphMatching(TargetPos, MatchingPos);
                                GM.GraphMatchingProcess(0.1);

                                CuttedPolygon sourceCuttedPos = new CuttedPolygon(TargetPolygon, GM.sourcePolygons, GM.MatchingsourcePolygons);
                                CuttedPolygon targetCuttedPos = new CuttedPolygon(MatchingPolygon, GM.targetPolygons, GM.MatchingtargetPolygons);

                                double MatchSim = CS.CuttedPolygonSim(sourceCuttedPos, targetCuttedPos, 0, 2);
                                CachePoSimList.Add(MatchSim);
                            }

                            else
                            {
                                for (int n = 0; n < msatCuts.Count; n++)
                                {
                                    List<PolygonObject> MatchingPos = CNS.GetPoListAfterCut(MatchingPolygon as Polygon, msatCuts[n]);
                                    //OutMap.PolygonList.AddRange(MatchingPos);
                                    GraphMatching GM = new GraphMatching(TargetPos, MatchingPos);
                                    GM.GraphMatchingProcess(0.1);

                                    CuttedPolygon sourceCuttedPos = new CuttedPolygon(TargetPolygon, GM.sourcePolygons, GM.MatchingsourcePolygons);
                                    CuttedPolygon targetCuttedPos = new CuttedPolygon(MatchingPolygon, GM.targetPolygons, GM.MatchingtargetPolygons);

                                    double MatchSim = CS.CuttedPolygonSim(sourceCuttedPos, targetCuttedPos, 0, 2);
                                    CachePoSimList.Add(MatchSim);
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion

                    PoiSimList.Add(CachePoSimList.Max());
                }
                #endregion

                double MaxISim = PoiSimList.Max();
                int MaxIndex = PoiSimList.IndexOf(MaxISim);
                simMatch.Add(i, MaxIndex);
            }
            #endregion

            //OutMap.WriteResult2Shp(OutPath, pMap.SpatialReference);
            System.IO.FileStream SimTXT = new System.IO.FileStream(OutPath + @"\MDSim.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(SimTXT);

            foreach (KeyValuePair<int, int> kv in simMatch)
            {
                sw.Write(kv.Key);
                sw.Write(" ");
                sw.Write(kv.Value);
                sw.Write("\r\n");
            }

            sw.Close();
            SimTXT.Close();
        }
    }
}
