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
    public partial class KGSupportfrm : Form
    {
        public KGSupportfrm(IMap cMap)
        {
            InitializeComponent();
            this.pMap = cMap;
        }

        #region 参数
        IMap pMap;
        string OutPath;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        PrDispalce.工具类.ParameterCompute PC = new 工具类.ParameterCompute();
        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KGSupportfrm_Load(object sender, EventArgs e)
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
                        this.comboBox2.Items.Add(strLayerName);
                    }
                    #endregion

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

            if (this.comboBox2.Items.Count > 0)
            {
                this.comboBox2.SelectedIndex = 0;
            }
            #endregion
        }

        /// <summary>
        /// 单选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (checkedListBox1.CheckedItems.Count > 0)
            {
                for (int i = 0; i < checkedListBox1.Items.Count; i++)
                {
                    if (i != e.Index)
                    {
                        // cklData.SetItemCheckState(i, CheckState.Unchecked);
                        checkedListBox1.SetItemChecked(i, false);
                    }
                }
            }
        }

        /// <summary>
        /// ok
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            BuildingSim.BuildingPairSim BPS = new BuildingSim.BuildingPairSim();

            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox2.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 空间特征计算+数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            foreach (PolygonObject Po in map.PolygonList)
            {
                Po.MBRO = PC.GetSMBROrientation(PC.ObjectConvert(Po));
                Po.tArea = PC.GetArea(PC.ObjectConvert(Po));
                Po.EdgeCount = Po.PointList.Count;
            }

            map.InterpretatePoint(2);
            #endregion

            #region 邻近关系计算
            DelaunayTin dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            ConvexNull cn = new ConvexNull(dt.TriNodeList);
            cn.CreateConvexNull();

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(map.PolylineList, map.PolygonList);

            Triangle.WriteID(dt.TriangleList);
            TriEdge.WriteID(dt.TriEdgeList);

            AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
            ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
            ske.TranverseSkeleton_Segment_PLP_NONull();

            ProxiGraph pg = new ProxiGraph();
            pg.CreateProxiGraphfrmSkeletonBuildings(map, ske);
            pg.DeleteRepeatedEdge(pg.EdgeList);
            pg.CreatePgForBuildings(pg.NodeList, pg.EdgeList);//构建建筑物间的邻近关系
          
            if (this.checkedListBox1.SelectedItem.ToString() == "DT")
            {
                pg.CreatePgForBuildings(pg.NodeList, pg.EdgeList);
            }
            if (this.checkedListBox1.SelectedItem.ToString() == "RNG")
            {
                pg.CreateRNGForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            }
            if (this.checkedListBox1.SelectedItem.ToString() == "MST")
            {
                pg.CreateMSTForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            }
            if (this.checkedListBox1.SelectedItem.ToString() == "NNG")
            {
                pg.CreateNNGForBuildingShortestDistance();
            }
            #endregion

            #region 邻近相似关系计算
            double SizeConstraint = 100000000000; double ShapeConstraint = 100000000000; double OriConstraint = 90;
            if (this.checkBox1.Checked)
            {
                SizeConstraint = double.Parse(this.textBox1.Text.ToString());
            }
            if (this.checkBox2.Checked)
            {
                OriConstraint = double.Parse(this.textBox2.Text.ToString());
            }
            if (this.checkBox3.Checked)
            {
                ShapeConstraint = double.Parse(this.textBox3.Text.ToString());
            }

            for (int i = 0; i < pg.KGEdgesList.Count; i++)
            {
                ProxiEdge VisitedEdge = pg.KGEdgesList[i];//探测的初始边
                ProxiNode VisitedNode1 = VisitedEdge.Node1; ProxiNode VisitedNode2 = VisitedEdge.Node2;//当前被访问的节点

                PolygonObject Po1 = map.GetObjectbyID(VisitedNode1.TagID, FeatureType.PolygonType) as PolygonObject;
                PolygonObject Po2 = map.GetObjectbyID(VisitedNode2.TagID, FeatureType.PolygonType) as PolygonObject;
                bool SimLabel = pg.Sim(Po1, Po2, SizeConstraint, ShapeConstraint, OriConstraint);

                pg.KGEdgesList[i].SimR = SimLabel;
            }
            #endregion

            #region SLinearArrange关系计算
            #region 获取线性关系
            double DistanceConstraint = 100000000000; double MinDis = 0; double AngleConstraint = 180; double FRConstraint = 0;
            List<Tuple<ProxiEdge, ProxiEdge>> TripleList = new List<Tuple<ProxiEdge, ProxiEdge>>();
            if (this.checkBox4.Checked)
            {
                AngleConstraint = double.Parse(this.textBox4.Text.ToString());
            }
            if (this.checkBox5.Checked)
            {
                DistanceConstraint = double.Parse(this.textBox5.Text.ToString());
                MinDis = double.Parse(this.textBox7.Text.ToString());
            }
            if (this.checkBox6.Checked)
            {
                FRConstraint = double.Parse(this.textBox6.Text.ToString());
            }

            for (int i = 0; i < pg.KGEdgesList.Count; i++)
            {
                ProxiEdge VisitedEdge = pg.KGEdgesList[i];//当前被访问的边
                ProxiNode VisitedNode1 = VisitedEdge.Node1; ProxiNode VisitedNode2 = VisitedEdge.Node2;//当前被访问的节点

                PolygonObject Po1 = map.GetObjectbyID(VisitedNode1.TagID, FeatureType.PolygonType) as PolygonObject;
                PolygonObject Po2 = map.GetObjectbyID(VisitedNode2.TagID, FeatureType.PolygonType) as PolygonObject;
                bool FRLabel = BPS.FRConstraint(Po1, Po2, FRConstraint);

                if (FRLabel)
                {
                    List<ProxiEdge> EdgeList1 = pg.ReturnEdgeList(pg.KGEdgesList, VisitedNode1);//Node1关联的边
                    EdgeList1.Remove(VisitedEdge);
                    List<ProxiEdge> EdgeList2 = pg.ReturnEdgeList(pg.KGEdgesList, VisitedNode2);//Node2关联的边
                    EdgeList2.Remove(VisitedEdge);
                    List<ProxiEdge> MixedEgdeList = EdgeList1.Union(EdgeList2).ToList();

                    for (int j = 0; j < MixedEgdeList.Count; j++)
                    {
                        if (!MixedEgdeList[j].KGVisit && MixedEgdeList[j] != VisitedEdge)
                        {
                            bool DistanceAccept = pg.DistanceConstrain(VisitedEdge, MixedEgdeList[j], DistanceConstraint, MinDis);
                            bool OrientationAccept = pg.OrientationConstrain(VisitedEdge, MixedEgdeList[j], AngleConstraint);

                            if (DistanceAccept && OrientationAccept)
                            {
                                PolygonObject CachePo1 = map.GetObjectbyID(MixedEgdeList[j].Node1.TagID, FeatureType.PolygonType) as PolygonObject;
                                PolygonObject CachePo2 = map.GetObjectbyID(MixedEgdeList[j].Node1.TagID, FeatureType.PolygonType) as PolygonObject;
                                bool CacheFRLabel = BPS.FRConstraint(CachePo1, CachePo2, FRConstraint);
                                if (CacheFRLabel)
                                {
                                    Tuple<ProxiEdge, ProxiEdge> Triple = new Tuple<ProxiEdge, ProxiEdge>(VisitedEdge, MixedEgdeList[j]);
                                    TripleList.Add(Triple);
                                }
                            }
                        }
                    }
                }

                pg.KGEdgesList[i].KGVisit = true;
            }
            #endregion

            #region 删除重复的集合
            //bool Stop=false;
            //do
            //{
            //    Stop=false;
            //    foreach (Tuple<ProxiEdge, ProxiEdge> TriplePattern in TripleList)
            //    {
            //        #region IDList
            //        List<int> TagIDList = new List<int>();
            //        if (!TagIDList.Contains(TriplePattern.Item1.Node1.TagID))
            //        {
            //            TagIDList.Add(TriplePattern.Item1.Node1.TagID);
            //        }
            //        if (!TagIDList.Contains(TriplePattern.Item1.Node2.TagID))
            //        {
            //            TagIDList.Add(TriplePattern.Item1.Node2.TagID);
            //        }
            //        if (!TagIDList.Contains(TriplePattern.Item2.Node1.TagID))
            //        {
            //            TagIDList.Add(TriplePattern.Item2.Node1.TagID);

            //        }
            //        if (!TagIDList.Contains(TriplePattern.Item2.Node2.TagID))
            //        {
            //            TagIDList.Add(TriplePattern.Item2.Node2.TagID);
            //        }
            //        TagIDList.Sort();
            //        #endregion

            //        foreach (Tuple<ProxiEdge, ProxiEdge> CacheTriplePattern in TripleList)
            //        {
            //            if (CacheTriplePattern != TriplePattern)
            //            {
            //                if ((CacheTriplePattern.Item1 == TriplePattern.Item1 && CacheTriplePattern.Item2 == TriplePattern.Item2) ||
            //                    (CacheTriplePattern.Item1 == TriplePattern.Item2 && CacheTriplePattern.Item2 == TriplePattern.Item1))
            //                {
            //                    TripleList.Remove(TriplePattern);
            //                    Stop = true;
            //                    break;
            //                }

            //                #region IDList
            //                List<int> CacheTagIDList = new List<int>();
            //                if (!CacheTagIDList.Contains(CacheTriplePattern.Item1.Node1.TagID))
            //                {
            //                    CacheTagIDList.Add(CacheTriplePattern.Item1.Node1.TagID);
            //                }
            //                if (!CacheTagIDList.Contains(CacheTriplePattern.Item1.Node2.TagID))
            //                {
            //                    CacheTagIDList.Add(CacheTriplePattern.Item1.Node2.TagID);
            //                }
            //                if (!CacheTagIDList.Contains(CacheTriplePattern.Item2.Node1.TagID))
            //                {
            //                    CacheTagIDList.Add(CacheTriplePattern.Item2.Node1.TagID);

            //                }
            //                if (!CacheTagIDList.Contains(CacheTriplePattern.Item2.Node2.TagID))
            //                {
            //                    CacheTagIDList.Add(CacheTriplePattern.Item2.Node2.TagID);
            //                }
            //                CacheTagIDList.Sort();
            //                #endregion

            //                if (TagIDList[0] == CacheTagIDList[0] && TagIDList[1] == CacheTagIDList[1] && TagIDList[2] == CacheTagIDList[2])
            //                {
            //                    TripleList.Remove(TriplePattern);
            //                    Stop = true;
            //                    break;
            //                }

            //            }
            //        }

            //        if (Stop)
            //        {
            //            break;
            //        }
            //    }
            //} while (Stop);
            #endregion

            #region 获取每一个建筑物所处的三元组列表
            for (int i = 0; i < TripleList.Count; i++)
            {
                Tuple<ProxiEdge, ProxiEdge> TriplePattern = TripleList[i];

                ProxiEdge VisitedEdge_1 = TriplePattern.Item1;
                ProxiEdge VisitedEdge_2 = TriplePattern.Item2;

                ProxiNode VisitedNode11 = VisitedEdge_1.Node1; ProxiNode VisitedNode12 = VisitedEdge_1.Node2;//当前被访问的节点
                ProxiNode VisitedNode21 = VisitedEdge_2.Node1; ProxiNode VisitedNode22 = VisitedEdge_2.Node2;//当前被访问的节点

                PolygonObject Po11 = map.GetObjectbyID(VisitedNode11.TagID, FeatureType.PolygonType) as PolygonObject;
                if (!Po11.PatternIDList.Contains(i))
                {
                    Po11.PatternIDList.Add(i);
                }
                PolygonObject Po12 = map.GetObjectbyID(VisitedNode12.TagID, FeatureType.PolygonType) as PolygonObject;
                if (!Po12.PatternIDList.Contains(i))
                {
                    Po12.PatternIDList.Add(i);
                }
                PolygonObject Po21 = map.GetObjectbyID(VisitedNode21.TagID, FeatureType.PolygonType) as PolygonObject;
                if (!Po21.PatternIDList.Contains(i))
                {
                    Po21.PatternIDList.Add(i);
                }
                PolygonObject Po22 = map.GetObjectbyID(VisitedNode22.TagID, FeatureType.PolygonType) as PolygonObject;
                if (!Po22.PatternIDList.Contains(i))
                {
                    Po22.PatternIDList.Add(i);
                }
            }

            foreach (PolygonObject Po in map.PolygonList)
            {
                Po.PatternIDList.Sort();//降序排列
            }
            #endregion
            #endregion

            #region 结果输出
            #region 实体输出（包括该关系的ID和PIDList）
            System.IO.FileStream SimTXT = new System.IO.FileStream(OutPath + @"\BuildingOutPut.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(SimTXT);

            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                sw.Write(map.PolygonList[i].ID);
                sw.Write(",");

                if (map.PolygonList[i].PatternIDList.Count == 0)
                {
                    sw.Write("null");
                }
                else
                {
                    for (int j = 0; j < map.PolygonList[i].PatternIDList.Count; j++)
                    {
                        sw.Write(map.PolygonList[i].PatternIDList[j]);
                        if (j < map.PolygonList[i].PatternIDList.Count - 1)
                        {
                            sw.Write(";");
                        }
                    }
                }

                sw.Write("\r\n");
            }

            sw.Close();
            SimTXT.Close();
            #endregion

            #region 邻近关系输出（该邻近关系存在属性角度！）
            System.IO.FileStream SimTXT_Proxi = new System.IO.FileStream(OutPath + @"\ProxiOutPut.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw_Proxi = new StreamWriter(SimTXT_Proxi);

            for (int i = 0; i < pg.KGEdgesList.Count; i++)
            {
                ProxiEdge VisitedEdge = pg.KGEdgesList[i];//探测的初始边
                ProxiNode VisitedNode1 = VisitedEdge.Node1; ProxiNode VisitedNode2 = VisitedEdge.Node2;//当前被访问的节点

                sw_Proxi.Write(VisitedNode1.TagID); sw_Proxi.Write(",");
                sw_Proxi.Write("HAS_Proxi"); sw_Proxi.Write(",");
                sw_Proxi.Write(VisitedNode2.TagID); sw_Proxi.Write(",");
                ILine pline1 = new LineClass();
                IPoint Point11 = new PointClass(); IPoint Point12 = new PointClass();

                #region 边角度计算
                Point11.X = VisitedNode1.X; Point11.Y = VisitedNode1.Y;
                Point12.X = VisitedNode2.X; Point12.Y = VisitedNode2.Y;
                pline1.FromPoint = Point11; pline1.ToPoint = Point12;
                double angle1 = pline1.Angle;
                double dAngle1Degree = (180 * angle1) / Math.PI;

                if (dAngle1Degree < 0)
                {
                    dAngle1Degree = dAngle1Degree + 180;
                }

                sw_Proxi.Write(dAngle1Degree); sw_Proxi.Write("\r\n");
                #endregion              
            }

            sw_Proxi.Close();
            SimTXT_Proxi.Close();

            #endregion

            #region 相似关系输出
            System.IO.FileStream SimTXT_Sim = new System.IO.FileStream(OutPath + @"\SimOutPut.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw_Sim = new StreamWriter(SimTXT_Sim);

            for (int i = 0; i < pg.KGEdgesList.Count; i++)
            {
                ProxiEdge VisitedEdge = pg.KGEdgesList[i];//探测的初始边
                ProxiNode VisitedNode1 = VisitedEdge.Node1; ProxiNode VisitedNode2 = VisitedEdge.Node2;//当前被访问的节点

                if (pg.KGEdgesList[i].SimR)
                {
                    sw_Sim.Write(VisitedNode1.TagID); sw_Sim.Write(",");
                    sw_Sim.Write("HAS_Sim"); sw_Sim.Write(",");
                    sw_Sim.Write(VisitedNode2.TagID); sw_Sim.Write("\r\n");
                }
            }

            sw_Sim.Close();
            SimTXT_Sim.Close();
            #endregion
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
            this.comboBox3.Text = OutPath;
        }
    }
}
