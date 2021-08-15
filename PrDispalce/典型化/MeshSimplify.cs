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
using PrDispalce.工具类;
using PrDispalce.典型化;
using PrDispalce.工具类.CollabrativeDisplacement;

namespace PrDispalce.工具窗体
{
    public partial class MeshSimplify : Form
    {
        public MeshSimplify(IMap cMap,AxMapControl pMapControl)
        {
            InitializeComponent();
            this.pMap = cMap;
            this.pMapControl = pMapControl;
        }

        /// <summary>
        /// 参数
        /// </summary>
        #region 参数
        IMap pMap;
        AxMapControl pMapControl;
        string OutPath;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        List<PolygonObject> OutPolygonObject = new List<PolygonObject>();
        PrDispalce.工具类.Symbolization Symbol = new PrDispalce.工具类.Symbolization();
        #endregion

        /// <summary>
        /// 窗体确定事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            map.InterpretatePoint(2);
            #endregion

            #region DT+CDT+SKE
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
            pg.CreatePgForBuildings(pg.NodeList, pg.EdgeList);

            //VoronoiDiagram vd = null;
            //vd = new AuxStructureLib.VoronoiDiagram(ske, pg, map);
            //vd.CreateVoronoiDiagramfrmSkeletonforBuildings();
            #endregion

            simplemesh(map, pg);//最简单的mesh
            //DensityBasedmesh(map, pg);//顾及密度的mesh
            //AreaConsideredmesh(map, pg);//顾及面积差异的mesh
            //pg.CreateRNGForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            //pg.DeleteRepeatedEdge(pg.RNGBuildingEdgesListShortestDistance);

            //pg.LinearPatternDetected1(pg.RNGBuildingEdgesListShortestDistance, pg.RNGBuildingNodesListShortestDistance, double.Parse(this.textBox2.Text.ToString()), double.Parse(this.textBox1.Text.ToString()),double.Parse(this.textBox3.Text.ToString()));
            //List<List<ProxiEdge>> PatternEdgeList = pg.LinearPatternDetected2(pg.RNGBuildingEdgesListShortestDistance, pg.RNGBuildingNodesListShortestDistance, double.Parse(this.textBox3.Text.ToString()), double.Parse(this.textBox2.Text.ToString()), double.Parse(this.textBox4.Text.ToString()));
            //List<List<ProxiNode>> PatternNodes = pg.LinearPatternRefinement1(PatternEdgeList);
            //pg.NodesforPattern(PatternNodes);

            //LinearPatternMesh(PatternNodes,map,pg,double.Parse(this.textBox5.Text));

            #region 输出

            //for (int i = 0; i < map.PolygonList.Count; i++)
            //{
            //    map.PolygonList[i] = SymbolizedPolygon(map.PolygonList[i], 25000);
            //} 

            //map.WriteResult2Shp(OutPath, pMap.SpatialReference);
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "邻近图", pMap.SpatialReference, pg.NodeList, pg.EdgeList); }

            if (OutPath != null) { ske.CDT.DT.WriteShp(OutPath, pMap.SpatialReference); }
            //if (OutPath != null) { vd.Create_WritePolygonObject2Shp(OutPath, "V图", pMap.SpatialReference); }
            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "邻近图", pMap.SpatialReference, pg.NodeList, pg.EdgeList); }
            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "建筑物邻近图", pMap.SpatialReference, pg.NodeList, pg.PgforBuildingEdgesList); }
            if (OutPath != null) { ske.Create_WriteSkeleton_Segment2Shp(OutPath, "骨架", pMap.SpatialReference); }
            #endregion
        }

        /// <summary>
        /// 输出路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
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
        /// 初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeshSimplify_Load(object sender, EventArgs e)
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
        /// 最简单的典型化
        /// </summary>
        /// <param name="map"></param>
        /// <param name="pg"></param>        
        void simplemesh(SMap map,ProxiGraph pg)
        {
            List<ProxiEdge> EdgeList = pg.PgforBuildingEdgesList;
            List<ProxiNode> NodeList = pg.PgforBuildingNodesList;

            #region 对所有边排序
            Dictionary<ProxiEdge, double> dEdgeList = new Dictionary<ProxiEdge, double>();
            for (int i = 0; i < EdgeList.Count; i++)
            {
                dEdgeList.Add(EdgeList[i], EdgeList[i].NearestEdge.NearestDistance);
            }

            //List<List<ProxiNode>> PairNode = new List<List<ProxiNode>>();
            //for (int i = 0; i < NodeList.Count; i++)
            //{
            //    List<ProxiNode> pNodeList = new List<ProxiNode>();
            //    pNodeList.Add(NodeList[i]);
            //    PairNode.Add(pNodeList);
            //}

            List<List<ProxiNode>> NodePairList = new List<List<ProxiNode>>();
            Dictionary<ProxiEdge, double> SortEdgeList = dEdgeList.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);
            #endregion

            #region 找到需要降维的边
            int Number = 0; Dictionary<ProxiEdge, bool> InEdgeDic = new Dictionary<ProxiEdge, bool>(); List<ProxiEdge> InEdgeList = new List<ProxiEdge>();
            foreach (KeyValuePair<ProxiEdge, double> kvp in SortEdgeList)
            {
                ProxiEdge sEdge = kvp.Key;
                InEdgeDic.Add(sEdge, false);
                InEdgeList.Add(sEdge);

                Number++;
                if (Number > NodeList.Count * (1 - double.Parse(this.textBox1.Text)))
                {
                    break;
                }
            }
            #endregion

            #region 根据相关边生成pair点
            for (int i = 0; i < InEdgeList.Count; i++)
            {
                List<ProxiNode> NodePair = new List<ProxiNode>();
                if (!InEdgeDic[InEdgeList[i]])
                {
                    NodePair.Add(InEdgeList[i].Node1);
                    NodePair.Add(InEdgeList[i].Node2);
                    InEdgeDic[InEdgeList[i]] = true;

                    for (int j = i + 1; j < InEdgeList.Count; j++)
                    {
                        if (!InEdgeDic[InEdgeList[j]])
                        {
                            if (NodePair.Contains(InEdgeList[j].Node1) && !NodePair.Contains(InEdgeList[j].Node2))
                            {
                                NodePair.Add(InEdgeList[j].Node2);
                                InEdgeDic[InEdgeList[j]] = true;
                            }

                            else if (NodePair.Contains(InEdgeList[j].Node2) && !NodePair.Contains(InEdgeList[j].Node1))
                            {
                                NodePair.Add(InEdgeList[j].Node1);
                                InEdgeDic[InEdgeList[j]] = true;
                            }
                        }
                    }

                    NodePairList.Add(NodePair);
                }
            }
            #endregion

            #region PairMerge
            List<List<ProxiNode>> newNodePairList = new List<List<ProxiNode>>();
            Dictionary<List<ProxiNode>, bool> NodeListDic = new Dictionary<List<ProxiNode>, bool>();
            for (int i = 0; i < NodePairList.Count; i++)
            {
                NodeListDic.Add(NodePairList[i], false);
            }

            for (int i = 0; i < NodePairList.Count; i++)
            {
                List<ProxiNode> NodePair = new List<ProxiNode>();
                if (!NodeListDic[NodePairList[i]])
                {
                    NodePair = NodePairList[i];
                    for (int j = i + 1; j < NodePairList.Count; j++)
                    {
                        if (NodePairList[i].Intersect(NodePairList[j]).Count() > 0)
                        {
                            NodePair = NodePair.Union(NodePairList[j]).ToList();
                            NodeListDic[NodePairList[j]] = true;
                        }
                    }

                    newNodePairList.Add(NodePair);
                }
            }
            #endregion

            #region 根据pair点在对应点生成新的建筑物 polygonlist
            List<PolygonObject> RemovePolygonList = new List<PolygonObject>();
            for (int i = 0; i < newNodePairList.Count; i++)
            {
                List<ProxiNode> NodePair = newNodePairList[i];
                double MaxArea = 0; int tagID = -1; FeatureType fType = FeatureType.PolygonType; PolygonObject Maxpo = null; double Sumx = 0; double Sumy = 0; double Newx = 0; double Newy = 0;
                for (int j = 0; j < NodePair.Count; j++)
                {
                    PolygonObject po = map.GetObjectbyID(NodePair[j].TagID, NodePair[j].FeatureType) as PolygonObject;
                    RemovePolygonList.Add(po);
                    double Area = po.Area; Sumx = po.CalProxiNode().X + Sumx; Sumy = po.CalProxiNode().Y + Sumy;
                    if (Area > MaxArea)
                    {
                        tagID = NodePair[j].TagID;
                        //map.PolygonList.Remove(po);
                        MaxArea = Area;
                        Maxpo = po;
                    }
                }
                RemovePolygonList.Remove(Maxpo);

                Newx = Sumx / NodePair.Count; Newy = Sumy / NodePair.Count;
                PolygonObject newpo = map.GetObjectbyID(tagID, fType) as PolygonObject;
                if (newpo != null)
                {
                    double curDx = Newx - Maxpo.CalProxiNode().X;
                    double curDy = Newy - Maxpo.CalProxiNode().Y;

                    //更新多边形点集的每一个点坐标
                    foreach (TriNode curPoint in newpo.PointList)
                    {
                        curPoint.X += curDx;
                        curPoint.Y += curDy;
                    }
                }
            }

            #region 删除所有待删除的建筑物
            for (int i = 0; i < RemovePolygonList.Count; i++)
            {
                map.PolygonList.Remove(RemovePolygonList[i]);
            }
            #endregion
            #endregion
        }

        /// <summary>
        /// 顾及密度的典型化
        /// </summary>
        /// <param name="map"></param>
        /// <param name="pg"></param>
        void DensityBasedmesh(SMap map, ProxiGraph pg)
        {
            List<ProxiEdge> EdgeList = pg.PgforBuildingEdgesList;
            List<ProxiNode> NodeList = pg.PgforBuildingNodesList;
            

            #region 添加原始的边和边对应的最短距离
            Dictionary<ProxiEdge, double> SortEdgeList = new Dictionary<ProxiEdge, double>();//排序表
            Dictionary<ProxiEdge,bool> VisitEdgeList=new Dictionary<ProxiEdge,bool>();//访问表

            for (int i = 0; i < EdgeList.Count; i++)
            {
                SortEdgeList.Add(EdgeList[i], EdgeList[i].NearestEdge.NearestDistance);
                VisitEdgeList.Add(EdgeList[i],false);
            }
            #endregion

            //List<List<ProxiNode>> PairNode = new List<List<ProxiNode>>();
            //for (int i = 0; i < NodeList.Count; i++)
            //{
            //    List<ProxiNode> pNodeList = new List<ProxiNode>();
            //    pNodeList.Add(NodeList[i]);
            //    PairNode.Add(pNodeList);
            //}

            List<List<ProxiNode>> NodePairList = new List<List<ProxiNode>>();//临时哪些mesh被当做一个点降维集合
            List<List<ProxiNode>> newNodePairList = new List<List<ProxiNode>>();//PairMerge后哪些mesh被当做一个点降维集合

            int MeshNumber=0;
            do
            {
                #region 降维：每次选择当前距离最短的边为待降维边；生成pair点；
                SortEdgeList = SortEdgeList.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);//排序
                KeyValuePair<ProxiEdge, double> pair = SortEdgeList.First();//找到边长最短的边

                bool ContainLabel = false;//标识是否当前mesh边与已mesh的边关联
                for (int i = 0; i < NodePairList.Count; i++)
                {
                    if (NodePairList[i].Contains(pair.Key.Node1) && NodePairList[i].Contains(pair.Key.Node2))
                    {
                        NodePairList[i].Add(pair.Key.Node2);
                        ContainLabel = true;
                        break;
                    }

                    else if (NodePairList[i].Contains(pair.Key.Node2) && NodePairList[i].Contains(pair.Key.Node1))
                    {
                        NodePairList[i].Add(pair.Key.Node1);
                        ContainLabel = true;
                        break;
                    }
                }

                if (!ContainLabel)
                {
                    List<ProxiNode> NodePair = new List<ProxiNode>();
                    NodePair.Add(pair.Key.Node1); NodePair.Add(pair.Key.Node2);
                    NodePairList.Add(NodePair);
                }

                SortEdgeList.Remove(pair.Key);
                #endregion

                #region PairMerge
                newNodePairList = new List<List<ProxiNode>>();
                Dictionary<List<ProxiNode>, bool> NodeListDic = new Dictionary<List<ProxiNode>, bool>();
                for (int i = 0; i < NodePairList.Count; i++)
                {
                    NodeListDic.Add(NodePairList[i], false);
                }

                for (int i = 0; i < NodePairList.Count; i++)
                {
                    List<ProxiNode> NodePair = new List<ProxiNode>();
                    if (!NodeListDic[NodePairList[i]])
                    {
                        NodePair = NodePairList[i];
                        for (int j = i + 1; j < NodePairList.Count; j++)
                        {
                            if (NodePairList[i].Intersect(NodePairList[j]).Count() > 0)
                            {
                                NodePair = NodePair.Union(NodePairList[j]).ToList();
                                NodeListDic[NodePairList[j]] = true;
                            }
                        }

                        newNodePairList.Add(NodePair);
                    }
                }
                #endregion

                #region 根据NodePair进行距离更新
                List<ProxiEdge> EdgeKey = new List<ProxiEdge>(SortEdgeList.Keys);
                //foreach (KeyValuePair<ProxiEdge, double> kvp in SortEdgeList)集合更改不能用foreach
                for (int i = 0; i < EdgeKey.Count; i++)
                {
                    ProxiNode Node1 = EdgeKey[i].Node1;
                    ProxiNode Node2 = EdgeKey[i].Node2;

                    int nodeCount = 1;
                    for (int j = 0; j < newNodePairList.Count; j++)
                    {
                        if (newNodePairList[j].Contains(Node1))
                        {
                            nodeCount = nodeCount + newNodePairList[j].Count - 1;
                        }

                        if (newNodePairList[j].Contains(Node2))
                        {
                            nodeCount = nodeCount + newNodePairList[j].Count - 1;
                        }
                    }

                    SortEdgeList[EdgeKey[i]] = ((nodeCount - 1) * 0.5 + 1)*EdgeKey[i].NearestEdge.NearestDistance;//对dictionary中某个值赋值
                }

                #endregion

                MeshNumber++;
            } while (MeshNumber < NodeList.Count * (1 - double.Parse(this.textBox1.Text)));

            #region 根据pair点在对应点生成新的建筑物 polygonlist
            List<PolygonObject> RemovePolygonList = new List<PolygonObject>();
            for (int i = 0; i < newNodePairList.Count; i++)
            {
                List<ProxiNode> NodePair = newNodePairList[i];
                double MaxArea = 0; int tagID = -1; FeatureType fType = FeatureType.PolygonType; PolygonObject Maxpo = null; double Sumx = 0; double Sumy = 0; double Newx = 0; double Newy = 0;
                for (int j = 0; j < NodePair.Count; j++)
                {
                    PolygonObject po = map.GetObjectbyID(NodePair[j].TagID, NodePair[j].FeatureType) as PolygonObject;
                    RemovePolygonList.Add(po);
                    double Area = po.Area; Sumx = po.CalProxiNode().X + Sumx; Sumy = po.CalProxiNode().Y + Sumy;
                    if (Area > MaxArea)
                    {
                        tagID = NodePair[j].TagID;
                        //map.PolygonList.Remove(po);
                        MaxArea = Area;
                        Maxpo = po;
                    }
                }
                RemovePolygonList.Remove(Maxpo);

                Newx = Sumx / NodePair.Count; Newy = Sumy / NodePair.Count;
                PolygonObject newpo = map.GetObjectbyID(tagID, fType) as PolygonObject;
                if (newpo != null)
                {
                    double curDx = Newx - Maxpo.CalProxiNode().X;
                    double curDy = Newy - Maxpo.CalProxiNode().Y;

                    //更新多边形点集的每一个点坐标
                    foreach (TriNode curPoint in newpo.PointList)
                    {
                        curPoint.X += curDx;
                        curPoint.Y += curDy;
                    }
                }
            }

            #region 删除所有待删除的建筑物
            for (int i = 0; i < RemovePolygonList.Count; i++)
            {
                map.PolygonList.Remove(RemovePolygonList[i]);
            }
            #endregion
            #endregion
        }

        /// <summary>
        /// 顾及面积差异的典型化
        /// </summary>
        /// <param name="map"></param>
        /// <param name="pg"></param>
        void AreaConsideredmesh(SMap map, ProxiGraph pg)
        {
            List<ProxiEdge> EdgeList = pg.PgforBuildingEdgesList;
            List<ProxiNode> NodeList = pg.PgforBuildingNodesList;

            #region 添加原始的边和边对应的最短距离
            Dictionary<ProxiEdge, double> SortEdgeList = new Dictionary<ProxiEdge, double>();//排序表
            Dictionary<ProxiEdge, bool> VisitEdgeList = new Dictionary<ProxiEdge, bool>();//访问表

            for (int i = 0; i < EdgeList.Count; i++)
            {
                SortEdgeList.Add(EdgeList[i], EdgeList[i].NearestEdge.NearestDistance);
                VisitEdgeList.Add(EdgeList[i], false);
            }
            #endregion

            //List<List<ProxiNode>> PairNode = new List<List<ProxiNode>>();
            //for (int i = 0; i < NodeList.Count; i++)
            //{
            //    List<ProxiNode> pNodeList = new List<ProxiNode>();
            //    pNodeList.Add(NodeList[i]);
            //    PairNode.Add(pNodeList);
            //}

            List<List<ProxiNode>> NodePairList = new List<List<ProxiNode>>();//临时哪些mesh被当做一个点降维集合
            List<List<ProxiNode>> newNodePairList = new List<List<ProxiNode>>();//PairMerge后哪些mesh被当做一个点降维集合

            int MeshNumber = 0;
            do
            {
                #region 降维：每次选择当前距离最短的边为待降维边；生成pair点；
                SortEdgeList = SortEdgeList.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);//排序
                KeyValuePair<ProxiEdge, double> pair = SortEdgeList.First();//找到边长最短的边

                bool ContainLabel = false;//标识是否当前mesh边与已mesh的边关联
                for (int i = 0; i < NodePairList.Count; i++)
                {
                    if (NodePairList[i].Contains(pair.Key.Node1) && NodePairList[i].Contains(pair.Key.Node2))
                    {
                        NodePairList[i].Add(pair.Key.Node2);
                        ContainLabel = true;
                        break;
                    }

                    else if (NodePairList[i].Contains(pair.Key.Node2) && NodePairList[i].Contains(pair.Key.Node1))
                    {
                        NodePairList[i].Add(pair.Key.Node1);
                        ContainLabel = true;
                        break;
                    }
                }

                if (!ContainLabel)
                {
                    List<ProxiNode> NodePair = new List<ProxiNode>();
                    NodePair.Add(pair.Key.Node1); NodePair.Add(pair.Key.Node2);
                    NodePairList.Add(NodePair);
                }

                SortEdgeList.Remove(pair.Key);
                #endregion

                #region PairMerge
                newNodePairList = new List<List<ProxiNode>>();
                Dictionary<List<ProxiNode>, bool> NodeListDic = new Dictionary<List<ProxiNode>, bool>();
                for (int i = 0; i < NodePairList.Count; i++)
                {
                    NodeListDic.Add(NodePairList[i], false);
                }

                for (int i = 0; i < NodePairList.Count; i++)
                {
                    List<ProxiNode> NodePair = new List<ProxiNode>();
                    if (!NodeListDic[NodePairList[i]])
                    {
                        NodePair = NodePairList[i];
                        for (int j = i + 1; j < NodePairList.Count; j++)
                        {
                            if (NodePairList[i].Intersect(NodePairList[j]).Count() > 0)
                            {
                                NodePair = NodePair.Union(NodePairList[j]).ToList();
                                NodeListDic[NodePairList[j]] = true;
                            }
                        }

                        newNodePairList.Add(NodePair);
                    }
                }
                #endregion

                #region 根据NodePair进行距离更新
                List<ProxiEdge> EdgeKey = new List<ProxiEdge>(SortEdgeList.Keys);
                //foreach (KeyValuePair<ProxiEdge, double> kvp in SortEdgeList)集合更改不能用foreach
                for (int i = 0; i < EdgeKey.Count; i++)
                {
                    ProxiNode Node1 = EdgeKey[i].Node1;
                    ProxiNode Node2 = EdgeKey[i].Node2;

                    int nodeCount = 1;
                    for (int j = 0; j < newNodePairList.Count; j++)
                    {
                        if (newNodePairList[j].Contains(Node1))
                        {
                            nodeCount = nodeCount + newNodePairList[j].Count - 1;
                        }

                        if (newNodePairList[j].Contains(Node2))
                        {
                            nodeCount = nodeCount + newNodePairList[j].Count - 1;
                        }
                    }

                    SortEdgeList[EdgeKey[i]] = ((nodeCount - 1) * 0.5 + 1) * EdgeKey[i].NearestEdge.NearestDistance; ;//对dictionary中某个值赋值
                }

                #endregion

                MeshNumber++;
            } while (MeshNumber < NodeList.Count * (1 - double.Parse(this.textBox1.Text)));

            #region 根据pair点在对应点生成新的建筑物 polygonlist
            List<PolygonObject> RemovePolygonList = new List<PolygonObject>();
            for (int i = 0; i < newNodePairList.Count; i++)
            {
                List<ProxiNode> NodePair = newNodePairList[i];
                double MaxArea = 0; int tagID = -1; FeatureType fType = FeatureType.PolygonType; PolygonObject Maxpo = null; double AreaSumx = 0;
                double AreaSumy = 0; double Newx = 0; double Newy = 0; double AreaSum = 0;
                for (int j = 0; j < NodePair.Count; j++)
                {
                    PolygonObject po = map.GetObjectbyID(NodePair[j].TagID, NodePair[j].FeatureType) as PolygonObject;
                    RemovePolygonList.Add(po);
                    double Area = po.Area; AreaSum = AreaSum + Area;
                    AreaSumx = po.CalProxiNode().X*po.Area + AreaSumx; AreaSumy = po.CalProxiNode().Y*po.Area + AreaSumy;
                    if (Area > MaxArea)
                    {
                        tagID = NodePair[j].TagID;
                        //map.PolygonList.Remove(po);
                        MaxArea = Area;
                        Maxpo = po;
                    }
                }
                RemovePolygonList.Remove(Maxpo);

                Newx = AreaSumx/ AreaSum; Newy = AreaSumy / AreaSum;
                PolygonObject newpo = map.GetObjectbyID(tagID, fType) as PolygonObject;
                if (newpo != null)
                {
                    double curDx = Newx - Maxpo.CalProxiNode().X;
                    double curDy = Newy - Maxpo.CalProxiNode().Y;

                    //更新多边形点集的每一个点坐标
                    foreach (TriNode curPoint in newpo.PointList)
                    {
                        curPoint.X += curDx;
                        curPoint.Y += curDy;
                    }
                }
            }

            #region 删除所有待删除的建筑物
            for (int i = 0; i < RemovePolygonList.Count; i++)
            {
                map.PolygonList.Remove(RemovePolygonList[i]);
            }
            #endregion
            #endregion
        }

        /// <summary>
        /// 将pattern包含边中的点按照一定的顺序排列
        /// </summary>
        /// <returns></returns>
        List<ProxiNode> GetOrderProxiNode(List<ProxiEdge> PatternEdge)
        {
            List<ProxiNode> OrderProxiNode = new List<ProxiNode>();

            #region 添加第一条边的两个点
            for (int i = 0; i < PatternEdge.Count; i++)
            {
                bool Node1Label = false; bool Node2Label = false;
                for (int j = 0; j < PatternEdge.Count; j++)
                {
                    if (j != i)
                    {
                        if (PatternEdge[i].Node1 == PatternEdge[j].Node1 || PatternEdge[i].Node1 == PatternEdge[j].Node2)
                        {
                            Node1Label = true;
                        }

                        if (PatternEdge[i].Node2 == PatternEdge[j].Node1 || PatternEdge[i].Node2 == PatternEdge[j].Node2)
                        {
                            Node2Label = true;
                        }
                    }
                }

                if (!Node1Label || !Node2Label)
                {
                    if (!Node1Label)
                    {
                        OrderProxiNode.Add(PatternEdge[i].Node1);
                        OrderProxiNode.Add(PatternEdge[i].Node2);
                    }

                    else
                    {
                        OrderProxiNode.Add(PatternEdge[i].Node2);
                        OrderProxiNode.Add(PatternEdge[i].Node1);
                    }

                    PatternEdge.RemoveAt(i);
                    break;
                }
            }
            #endregion

            #region 依次获取边关联的点
            do
            {
                for (int i = 0; i < PatternEdge.Count; i++)
                {
                    if (PatternEdge[i].Node1 == OrderProxiNode[OrderProxiNode.Count - 1])
                    {
                        OrderProxiNode.Add(PatternEdge[i].Node2);
                        PatternEdge.RemoveAt(i);
                        break;
                    }

                    else if (PatternEdge[i].Node2 == OrderProxiNode[OrderProxiNode.Count - 1])
                    {
                        OrderProxiNode.Add(PatternEdge[i].Node1);
                        PatternEdge.RemoveAt(i);
                        break;
                    }
                }
            } while (PatternEdge.Count > 0);
            #endregion

            return OrderProxiNode;
        }

        /// <summary>
        /// 对LinearPattern中建筑物做典型化,传入的是建筑物pattern对应的Nodes;其中，pattern之间不相交
        /// </summary>
        void LinearPatternMesh(List<List<ProxiNode>> PatternNodes, SMap map,ProxiGraph pg)
        {
            List<PolygonObject> RemovePolygonList = new List<PolygonObject>();
            for (int j = 0; j < PatternNodes.Count; j++)
            {
                #region 获得每一个pattern对应的边
                List<ProxiEdge> PatternEdge = new List<ProxiEdge>();

                for (int i = 0; i < pg.PgforBuildingPatternList.Count; i++)
                {                   
                    if (PatternNodes[j].Contains(pg.PgforBuildingPatternList[i].Node1) && PatternNodes[j].Contains(pg.PgforBuildingPatternList[i].Node2))
                    {
                        PatternEdge.Add(pg.PgforBuildingPatternList[i]);
                    }                    
                }
                #endregion

                #region 选择边中最短的一条边，将边连接的两个建筑物进行典型化
                List<ProxiEdge> CachePatternEdge = new List<ProxiEdge>();
                for (int i = 0; i < PatternEdge.Count; i++)
                {
                    CachePatternEdge.Add(PatternEdge[i]);
                }

                List<ProxiNode> NodesofPattern = GetOrderProxiNode(CachePatternEdge);//(在其中对patternEdge进行了操作)
                int MinNum = -1; double MinDis = 10000000;
                for (int i = 0; i < PatternEdge.Count; i++)
                {
                    if (PatternEdge[i].NearestEdge.NearestDistance < MinDis)
                    {
                        MinDis = PatternEdge[i].NearestEdge.NearestDistance;
                        MinNum = i;
                    }
                }

                #region 将两个建筑物中面积大的建筑物移到新的位置，并将面积小的建筑物标记为删除
                PolygonObject po1 = map.GetObjectbyID(PatternEdge[MinNum].Node1.TagID, PatternEdge[MinNum].Node1.FeatureType) as PolygonObject;
                PolygonObject po2 = map.GetObjectbyID(PatternEdge[MinNum].Node2.TagID, PatternEdge[MinNum].Node2.FeatureType) as PolygonObject;

                //List<double> PointXY = NewPositionComputation(PatternEdge[MinNum].Node1, PatternEdge[MinNum].Node2, map);
                List<double> PointXY = NewPositionComputation(PatternEdge[MinNum].Node1, PatternEdge[MinNum].Node2,NodesofPattern, map);
                if (po1.Area >= po2.Area)
                {
                    if (po1 != null)
                    {
                        double curDx = PointXY[0] - po1.CalProxiNode().X;
                        double curDy = PointXY[1] - po1.CalProxiNode().Y;

                        //更新多边形点集的每一个点坐标
                        foreach (TriNode curPoint in po1.PointList)
                        {
                            curPoint.X += curDx;
                            curPoint.Y += curDy;
                        }
                    }

                    RemovePolygonList.Add(po2);
                }

                else if (po1.Area < po2.Area)
                {
                    if (po2 != null)
                    {
                        double curDx = PointXY[0] - po2.CalProxiNode().X;
                        double curDy = PointXY[1] - po2.CalProxiNode().Y;

                        //更新多边形点集的每一个点坐标
                        foreach (TriNode curPoint in po2.PointList)
                        {
                            curPoint.X += curDx;
                            curPoint.Y += curDy;
                        }
                    }

                    RemovePolygonList.Add(po1);
                }
                #endregion
                #endregion
            }

            #region 删除所有待删除的建筑物
            for (int i = 0; i < RemovePolygonList.Count; i++)
            {
                map.PolygonList.Remove(RemovePolygonList[i]);
            }
            #endregion
        }

        /// <summary>
        /// 对LinearPattern中的建筑物做典型化，传入的是建筑物pattern对应的Nodes；其中，pattern之间不相交；每次处理最短的边，若边的距离小于阈值就需要典型化
        /// </summary>
        /// <param name="PatternNodes"></param>
        /// <param name="map"></param>
        /// <param name="pg"></param>
        /// <param name="MinDistance"></param>
        void LinearPatternMesh(List<List<ProxiNode>> PatternNodes, SMap map, ProxiGraph pg, double MinDistance)
        {
             List<PolygonObject> RemovePolygonList = new List<PolygonObject>();
             for (int j = 0; j < PatternNodes.Count; j++)
             {
                 #region 获得每一个pattern对应的边
                 List<ProxiEdge> PatternEdge = new List<ProxiEdge>();

                 for (int i = 0; i < pg.PgforBuildingPatternList.Count; i++)
                 {
                     if (PatternNodes[j].Contains(pg.PgforBuildingPatternList[i].Node1) && PatternNodes[j].Contains(pg.PgforBuildingPatternList[i].Node2))
                     {
                         PatternEdge.Add(pg.PgforBuildingPatternList[i]);
                     }
                 }
                 #endregion

                 #region 将建筑物按顺序排列起来
                 List<ProxiEdge> CachePatternEdge = new List<ProxiEdge>();
                 for (int i = 0; i < PatternEdge.Count; i++)
                 {
                     CachePatternEdge.Add(PatternEdge[i]);
                 }

                 List<ProxiNode> NodesofPattern = GetOrderProxiNode(CachePatternEdge);//(在其中对patternEdge进行了操作)
                 #endregion

                 #region 找到pattern中的最短边
                 double MinDis = 10000000; int MinPoNum1 = -1; int MinPoNum2 = -1;
                 for (int i = 0; i < NodesofPattern.Count - 1; i++)
                 {
                     for (int m = i + 1; m < NodesofPattern.Count; m++)
                     {
                         PolygonObject po1 = map.GetObjectbyID(NodesofPattern[i].TagID, NodesofPattern[i].FeatureType) as PolygonObject;
                         PolygonObject po2 = map.GetObjectbyID(NodesofPattern[m].TagID, NodesofPattern[m].FeatureType) as PolygonObject;

                         if (po1.GetMiniDistance(po2) < MinDis)
                         {
                             MinDis = po1.GetMiniDistance(po2);
                             MinPoNum1 = i; MinPoNum2 = m;
                         }
                     }
                 }
                 #endregion

                 #region 将两个建筑物中面积大的建筑物移到新的位置，并将面积小的建筑物标记为删除；并在pattern中移除该点
                 while (MinDis < MinDistance)
                 {
                     PolygonObject po1 = map.GetObjectbyID(NodesofPattern[MinPoNum1].TagID, NodesofPattern[MinPoNum1].FeatureType) as PolygonObject;
                     PolygonObject po2 = map.GetObjectbyID(NodesofPattern[MinPoNum2].TagID, NodesofPattern[MinPoNum2].FeatureType) as PolygonObject;

                     //List<double> PointXY = NewPositionComputation(PatternEdge[MinNum].Node1, PatternEdge[MinNum].Node2, map);
                     List<double> PointXY = NewPositionComputation(NodesofPattern[MinPoNum1], NodesofPattern[MinPoNum2], NodesofPattern, map,2);
                     if (po1.Area >= po2.Area)
                     {
                         if (po1 != null)
                         {
                             double curDx = PointXY[0] - po1.CalProxiNode().X;
                             double curDy = PointXY[1] - po1.CalProxiNode().Y;

                             //更新多边形点集的每一个点坐标
                             foreach (TriNode curPoint in po1.PointList)
                             {
                                 curPoint.X += curDx;
                                 curPoint.Y += curDy;
                             }
                         }

                         RemovePolygonList.Add(po2);
                         NodesofPattern.RemoveAt(MinPoNum2);
                     }

                     else if (po1.Area < po2.Area)
                     {
                         if (po2 != null)
                         {
                             double curDx = PointXY[0] - po2.CalProxiNode().X;
                             double curDy = PointXY[1] - po2.CalProxiNode().Y;

                             //更新多边形点集的每一个点坐标
                             foreach (TriNode curPoint in po2.PointList)
                             {
                                 curPoint.X += curDx;
                                 curPoint.Y += curDy;
                             }
                         }

                         RemovePolygonList.Add(po1);
                         NodesofPattern.RemoveAt(MinPoNum1);
                     }

                     #region 重新计算最短边
                     MinDis = 10000000; MinPoNum1 = -1; MinPoNum2 = -1;
                     for (int i = 0; i < NodesofPattern.Count - 1; i++)
                     {
                         for (int m = i + 1; m < NodesofPattern.Count; m++)
                         {
                             po1 = map.GetObjectbyID(NodesofPattern[i].TagID, NodesofPattern[i].FeatureType) as PolygonObject;
                             po2 = map.GetObjectbyID(NodesofPattern[m].TagID, NodesofPattern[m].FeatureType) as PolygonObject;

                             if (po1.GetMiniDistance(po2) < MinDis)
                             {
                                 MinDis = po1.GetMiniDistance(po2);
                                 MinPoNum1 = i; MinPoNum2 = m;
                             }
                         }
                     }
                     #endregion
                 }
                 #endregion
             }

             #region 删除所有待删除的建筑物
             for (int i = 0; i < RemovePolygonList.Count; i++)
             {
                 map.PolygonList.Remove(RemovePolygonList[i]);
             }
             #endregion
        }

        /// <summary>
        /// 对相似的LinearPattern中的建筑物做典型化，传入的是建筑物pattern对应的Nodes；其中Pattern是相交的。这里不考虑建筑物相交的情况
        /// </summary>
        /// <param name="PatternNodes"></param>
        /// <param name="map"></param>
        /// <param name="pg"></param>
        /// <param name="MinDistance"></param>
        void SimilarLinearPatternMesh(List<List<ProxiNode>> PatternNodes, SMap map, double MinDistance)
        {
            #region 首先对建筑物进行符号化
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                bool Label = false;
                map.PolygonList[i] = SymbolizedPolygon(map.PolygonList[i], 25000, 0.7, 0.5,out Label);
            } 
            #endregion

            List<PolygonObject> RemovePolygonList = new List<PolygonObject>();
            for (int j = 0; j < PatternNodes.Count; j++)
            {
                double MinDis = 1000000;

                #region PatternNodes的备份
                List<ProxiNode> CacheNodes = new List<ProxiNode>();
                for (int i = 0; i < PatternNodes[j].Count; i++)
                {
                    CacheNodes.Add(PatternNodes[j][i]);
                }
                #endregion

                #region 计算初始建筑物pattern建筑物间的最短距离
                for (int i = 0; i < CacheNodes.Count - 1; i++)
                {
                    for (int m = i + 1; m < CacheNodes.Count; m++)
                    {
                        PolygonObject po1 = map.GetObjectbyID(CacheNodes[i].TagID, CacheNodes[i].FeatureType) as PolygonObject;
                        PolygonObject po2 = map.GetObjectbyID(CacheNodes[m].TagID, CacheNodes[m].FeatureType) as PolygonObject;

                        if (po1.GetMiniDistance(po2) < MinDis)
                        {
                            MinDis = po1.GetMiniDistance(po2);
                        }
                    }
                }
                #endregion

                while (MinDis < MinDistance && CacheNodes.Count > 2)
                {
                    int Num = CacheNodes.Count - 1;
                    //List<List<double>> NewPositions = NewPositionComputation(PatternNodes[j], Num);
                    ProxiNode MinNode = GetMinPolygonNode(CacheNodes, map);
                    PatternNodes[j].Remove(MinNode);
                    PolygonObject RemovePolygon = map.GetObjectbyID(MinNode.TagID, MinNode.FeatureType) as PolygonObject;
                    RemovePolygonList.Add(RemovePolygon);
                    CacheNodes.Remove(MinNode);

                    if (CacheNodes.Count == 2)
                    {
                        break;
                    }

                    List<List<double>> NewPositions = NewPositionComputation(PatternNodes[j], 0.8);

                    #region 更新建筑物位置
                    for (int i = 0; i < CacheNodes.Count; i++)
                    {
                        PolygonObject po3 = map.GetObjectbyID(CacheNodes[i].TagID, CacheNodes[i].FeatureType) as PolygonObject;

                        double curDx = NewPositions[i][0] - po3.CalProxiNode().X;
                        double curDy = NewPositions[i][1] - po3.CalProxiNode().Y;

                        //更新多边形点集的每一个点坐标
                        foreach (TriNode curPoint in po3.PointList)
                        {
                            curPoint.X += curDx;
                            curPoint.Y += curDy;
                        }
                    }
                    #endregion

                    #region 重新计算最短距离
                    MinDis = 10000000;
                    for (int i = 0; i < CacheNodes.Count - 1; i++)
                    {
                        for (int m = i + 1; m < CacheNodes.Count; m++)
                        {
                            PolygonObject po1 = map.GetObjectbyID(CacheNodes[i].TagID, CacheNodes[i].FeatureType) as PolygonObject;
                            PolygonObject po2 = map.GetObjectbyID(CacheNodes[m].TagID, CacheNodes[m].FeatureType) as PolygonObject;

                            if (po1.GetMiniDistance(po2) < MinDis)
                            {
                                MinDis = po1.GetMiniDistance(po2);
                            }
                        }
                    }
                    #endregion
                }
             }

            #region 删除所有待删除的建筑物
            for (int i = 0; i < RemovePolygonList.Count; i++)
            {
                map.PolygonList.Remove(RemovePolygonList[i]);
            }
            #endregion
        }

        /// <summary>
        /// 处理相交且相似的LinearPattern中建筑物的典型化（将pattern划分，只实现了对划分后pattern保留与去除，没有具体到对应的pattern中建筑物的保留与否）
        /// </summary>
        /// <param name="PatternNodes"></param>
        /// <param name="map"></param>
        /// <param name="MinDistance"></param>
        void SimilarIntesectLinearPatternMesh(List<List<ProxiNode>> PatternNodes, SMap map, double MinDistance)
        {
            List<List<ProxiNode>> RemainedPatternNodes = new List<List<ProxiNode>>();//保留下来的Pattern

            #region 首先对建筑物进行符号化
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                bool Label = false;
                map.PolygonList[i] = SymbolizedPolygon(map.PolygonList[i], 25000,0.5,0.5,out Label);
            }
            #endregion

            LabelIntesectNode(PatternNodes);//对相交的建筑物进行标识
            List<List<List<ProxiNode>>> PatternCluster = this.PatternCluster(PatternNodes);//相交建筑物聚类
            
            #region 典型化过程
            for (int i = 0; i < PatternNodes.Count; i++)
            {
                List<List<ProxiNode>> ClipPatternNodes = this.PatternCluster(PatternNodes[i]);//建筑物裁剪

                for (int j = 0; j < ClipPatternNodes.Count; j++)
                {
                    double MinDis = 1000000;

                    #region ClipPatternNodes[j]的备份
                    List<ProxiNode> CacheNodes = new List<ProxiNode>();
                    for (int m= 0; m < ClipPatternNodes[j].Count; m++)
                    {
                        CacheNodes.Add(ClipPatternNodes[j][m]);
                    }
                    #endregion

                    #region 计算初始建筑物pattern建筑物间的最短距离
                    for (int m = 0; m < CacheNodes.Count - 1; m++)
                    {
                        for (int n= m + 1; n < CacheNodes.Count; n++)
                        {
                            PolygonObject po1 = map.GetObjectbyID(CacheNodes[m].TagID, CacheNodes[m].FeatureType) as PolygonObject;
                            PolygonObject po2 = map.GetObjectbyID(CacheNodes[n].TagID, CacheNodes[n].FeatureType) as PolygonObject;

                            if (po1.GetMiniDistance(po2) < MinDis)
                            {
                                MinDis = po1.GetMiniDistance(po2);
                            }
                        }
                    }
                    #endregion
               
                    while (MinDis < MinDistance && CacheNodes.Count > 2)
                    {
                        int Num = CacheNodes.Count - 1;
                        List<List<double>> NewPositions = NewPositionComputation(ClipPatternNodes[j], Num);
                        ProxiNode MinNode = GetMinPolygonNode(CacheNodes, map);
                        PolygonObject RemovePolygon = map.GetObjectbyID(MinNode.TagID, MinNode.FeatureType) as PolygonObject;
                        CacheNodes.Remove(MinNode);

                        #region 更新建筑物位置
                        for (int m = 0; m < CacheNodes.Count; m++)
                        {
                            PolygonObject po3 = map.GetObjectbyID(CacheNodes[m].TagID, CacheNodes[m].FeatureType) as PolygonObject;

                            double curDx = NewPositions[m][0] - po3.CalProxiNode().X;
                            double curDy = NewPositions[m][1] - po3.CalProxiNode().Y;

                            //更新多边形点集的每一个点坐标
                            foreach (TriNode curPoint in po3.PointList)
                            {
                                curPoint.X += curDx;
                                curPoint.Y += curDy;
                            }
                        }
                        #endregion

                        #region 重新计算最短距离
                        MinDis = 10000000;
                        for (int m= 0; m < CacheNodes.Count - 1; m++)
                        {
                            for (int n = m + 1; n < CacheNodes.Count; n++)
                            {
                                PolygonObject po1 = map.GetObjectbyID(CacheNodes[m].TagID, CacheNodes[m].FeatureType) as PolygonObject;
                                PolygonObject po2 = map.GetObjectbyID(CacheNodes[n].TagID, CacheNodes[n].FeatureType) as PolygonObject;

                                if (po1.GetMiniDistance(po2) < MinDis)
                                {
                                    MinDis = po1.GetMiniDistance(po2);
                                }
                            }
                        }
                        #endregion
                    }

                    if (MinDis > MinDistance&&CacheNodes.Count>=2)
                    {
                        RemainedPatternNodes.Add(CacheNodes);
                    }
                   
                }
            }
            #endregion

            #region 删除非Pattern的建筑物点
            List<PolygonObject> ReMainedPolygon = new List<PolygonObject>();
            for (int i = 0; i < RemainedPatternNodes.Count; i++)
            {
                for (int j = 0; j < RemainedPatternNodes[i].Count; j++)
                {
                    PolygonObject CachePolygonObject = map.GetObjectbyID(RemainedPatternNodes[i][j].TagID, RemainedPatternNodes[i][j].FeatureType) as PolygonObject;
                    ReMainedPolygon.Add(CachePolygonObject);
                }
            }

            ReMainedPolygon = ReMainedPolygon.Distinct().ToList();
            map.PolygonList = ReMainedPolygon;
            #endregion
        }

        /// <summary>
        /// 处理相交且相似的LinearPattern中建筑物的典型化（将pattern划分，根据划分后pattern保留与去除，确定对应pattern中建筑物的保留与否）
        /// 1、首先处理单挑pattern
        /// 2、其次处理相交pattern
        /// 3、对于相交pattern，首先处理涉及相交条数少的pattern；对于涉及条数相同的pattern，首先处理个数少的pattern；对于个数一致的pattern，处理面积和更小的pattern
        /// </summary>
        /// <param name="PatternNodes"></param>
        /// <param name="map"></param>
        /// <param name="MinDistance"></param>
        void SimilarIntesectLinearPatternMesh2(List<List<ProxiNode>> PatternNodes, SMap map, double MinDistance)
        {
            List<List<ProxiNode>> ProcessedPattern = new List<List<ProxiNode>>();

            #region 首先对建筑物进行符号化
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                bool Label = false;
                map.PolygonList[i] = SymbolizedPolygon(map.PolygonList[i], 25000,0.5,0.5,out Label);
            }
            #endregion

            LabelIntesectNode(PatternNodes);//对相交的建筑物进行标识

            #region 计算每个Pattern的权重，并排序
            Dictionary<List<ProxiNode>, int> PatternImportance = new Dictionary<List<ProxiNode>, int>();
            for (int i = 0; i < PatternNodes.Count; i++)
            {
                int IntersectNum = 0; int BoundaryNum = 0;
                for (int j = 0; j < PatternNodes[i].Count; j++)
                {
                    if (PatternNodes[i][j].IntersectNode)
                    {
                        IntersectNum = IntersectNum + 1;
                    }

                    if (PatternNodes[i][j].BoundaryNode)
                    {
                        BoundaryNum = BoundaryNum + 1;
                    }
                }

                PatternImportance.Add(PatternNodes[i], IntersectNum * 100 + BoundaryNum * 20 + PatternNodes[i].Count);
            }

            PatternImportance = PatternImportance.OrderByDescending(o => o.Value).ToDictionary(p => p.Key, o => o.Value);//权重降序排序
            #endregion

            #region 计算每个Node的权重
            Dictionary<ProxiNode, int> NodeImportance = new Dictionary<ProxiNode, int>();
            List<ProxiNode> NodeList = new List<ProxiNode>();

            for (int i = 0; i < PatternNodes.Count; i++)
            {
                for (int j = 0; j < PatternNodes[i].Count; j++)
                {
                    NodeList.Add(PatternNodes[i][j]);
                }
            }
            NodeList = NodeList.Distinct().ToList();//去重

            for (int i = 0; i < NodeList.Count; i++)
            {
                int IntersectNum = 0; int NodeNum = 0; int BoundaryNum = 0;
                for (int j = 0; j < PatternNodes.Count; j++)
                {
                    if (PatternNodes[j].Contains(NodeList[i]))
                    {
                        IntersectNum = IntersectNum + 1;
                        NodeNum = NodeNum + PatternNodes[j].Count;
                    }

                    if (PatternNodes[j][0] == NodeList[i] || PatternNodes[j][PatternNodes[j].Count - 1] == NodeList[i])
                    {
                        BoundaryNum = BoundaryNum + 1;
                    }
                }

                NodeImportance.Add(NodeList[i], IntersectNum * 100 + BoundaryNum * 20 + NodeNum);
            }
            #endregion

            //原因：每次处理权重更小的pattern，那么在处理过程中，涉及到的Pattern小，即对整体结构影响相对小
            PatternImportance = PatternImportance.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);//权重升序排序
            List<List<ProxiNode>> ProcessPatterns = new List<List<ProxiNode>>(PatternImportance.Keys);//待处理的Pattern

            #region 按权重依次处理每一个Pattern
            for (int i = 0; i < ProcessPatterns.Count; i++)
            {
                List<List<ProxiNode>> ClipPatternNodes = this.PatternCluster(ProcessPatterns[i], NodeList);//建筑物裁剪(这样的一个裁剪方法，保证每次只需要更新NodeList中Node的属性即可)
                List<List<ProxiNode>> ReMainedPattern = new List<List<ProxiNode>>();//存储分割后处理pattern中保留的Pattern
                bool Label = false;

                for (int j = 0; j < ClipPatternNodes.Count; j++)
                {
                    double MinDis = 1000000;

                    #region ClipPatternNodes[j]的备份
                    List<ProxiNode> CacheNodes = new List<ProxiNode>();
                    for (int m = 0; m < ClipPatternNodes[j].Count; m++)
                    {
                        CacheNodes.Add(ClipPatternNodes[j][m]);
                    }
                    #endregion

                    #region 计算初始建筑物pattern建筑物间的最短距离
                    for (int m = 0; m < CacheNodes.Count - 1; m++)
                    {
                        for (int n = m + 1; n < CacheNodes.Count; n++)
                        {
                            PolygonObject po1 = map.GetObjectbyID(CacheNodes[m].TagID, CacheNodes[m].FeatureType) as PolygonObject;
                            PolygonObject po2 = map.GetObjectbyID(CacheNodes[n].TagID, CacheNodes[n].FeatureType) as PolygonObject;

                            if (po1.GetMiniDistance(po2) < MinDis)
                            {
                                MinDis = po1.GetMiniDistance(po2);
                            }
                        }
                    }
                    #endregion

                    while (MinDis < MinDistance && CacheNodes.Count > 2)
                    {
                        int Num = CacheNodes.Count - 1;
                        List<List<double>> NewPositions = NewPositionComputation(ClipPatternNodes[j], Num);
                        ProxiNode MinNode = GetMinPolygonNode2(CacheNodes, map);
                        PolygonObject RemovePolygon = map.GetObjectbyID(MinNode.TagID, MinNode.FeatureType) as PolygonObject;
                        CacheNodes.Remove(MinNode);                       

                        #region 更新建筑物位置
                        for (int m = 0; m < CacheNodes.Count; m++)
                        {
                            PolygonObject po3 = map.GetObjectbyID(CacheNodes[m].TagID, CacheNodes[m].FeatureType) as PolygonObject;

                            double curDx = NewPositions[m][0] - po3.CalProxiNode().X;
                            double curDy = NewPositions[m][1] - po3.CalProxiNode().Y;

                            //更新多边形点集的每一个点坐标
                            foreach (TriNode curPoint in po3.PointList)
                            {
                                curPoint.X += curDx;
                                curPoint.Y += curDy;
                            }
                        }
                        #endregion

                        #region 重新计算最短距离
                        MinDis = 10000000;
                        for (int m = 0; m < CacheNodes.Count - 1; m++)
                        {
                            for (int n = m + 1; n < CacheNodes.Count; n++)
                            {
                                PolygonObject po1 = map.GetObjectbyID(CacheNodes[m].TagID, CacheNodes[m].FeatureType) as PolygonObject;
                                PolygonObject po2 = map.GetObjectbyID(CacheNodes[n].TagID, CacheNodes[n].FeatureType) as PolygonObject;

                                if (po1.GetMiniDistance(po2) < MinDis)
                                {
                                    MinDis = po1.GetMiniDistance(po2);
                                }
                            }
                        }
                        #endregion
                    }

                    #region 如果处理后满足要求
                    if (MinDis > MinDistance && CacheNodes.Count >= 2)
                    {
                        ReMainedPattern.Add(CacheNodes);
                    }
                    #endregion

                    #region 如果处理后不满足要求
                    else
                    {
                        #region 两个点都是交点
                        if (CacheNodes[0].IntersectNode && CacheNodes[1].IntersectNode)
                        {
                            #region 获得权值较小的交点
                            int Node0Im = 0; int Node1Im = 0;
                            foreach (KeyValuePair<ProxiNode, int> nKvp in NodeImportance)
                            {
                                if (nKvp.Key.TagID == CacheNodes[0].TagID)
                                {
                                    Node0Im = nKvp.Value;
                                }

                                if (nKvp.Key.TagID == CacheNodes[1].TagID)
                                {
                                    Node1Im = nKvp.Value;
                                }
                            }

                            ProxiNode CacheNode = CacheNodes[0];
                            if (Node0Im > Node1Im)
                            {
                                CacheNode = CacheNodes[1];
                            }
                            #endregion

                            #region 对Node相交属性进行更新
                            for (int m = 0; m < NodeList.Count; m++)
                            {
                                if (NodeList[m].TagID == CacheNode.TagID)
                                {
                                    NodeList[m].IntersectNode = false;
                                }
                            }
                            #endregion

                            #region 处理权值较小的交点(只处理权值更小的Pattern)（对于Pattern的情况，也需要出权重更大的Pattern）（从结构中删除交点）                         
                            #region 将处理过的Pattern在交点处分割
                            List<List<ProxiNode>> CacheProcessedPattern = new List<List<ProxiNode>>();
                            for (int m = ProcessedPattern.Count - 1; m >= 0; m--)
                            {
                                List<ProxiNode> List1 = new List<ProxiNode>(); List<ProxiNode> List2 = new List<ProxiNode>();
                                for (int n = 0; n < ProcessedPattern[m].Count; n++)
                                {
                                    if (CacheNode.TagID == ProcessedPattern[m][n].TagID)
                                    {
                                        for (int L1 = 0; L1 < n; L1++)
                                        {
                                            List1.Add(ProcessedPattern[m][L1]);
                                        }

                                        for (int L2 = n + 1; L2 < ProcessedPattern[m].Count; L2++)
                                        {
                                            List2.Add(ProcessedPattern[m][L2]);
                                        }

                                        ProcessedPattern.RemoveAt(m);

                                        break;
                                    }
                                }

                                if (List1.Count >= 2)
                                {
                                    CacheProcessedPattern.Add(List1);
                                }

                                if (List2.Count >= 2)
                                {
                                    CacheProcessedPattern.Add(List2);
                                }
                            }

                            if (CacheProcessedPattern.Count > 0)
                            {
                                for (int m = 0; m < CacheProcessedPattern.Count; m++)
                                {
                                    ProcessedPattern.Add(CacheProcessedPattern[m]);
                                }
                            }
                            #endregion                     

                            #region 将未处理的pattern在交点处分割
                            List<List<ProxiNode>> CacheProcessPattern = new List<List<ProxiNode>>();
                            for (int m = ProcessPatterns.Count - 1; m >= i + 1; m--)
                            {
                                List<ProxiNode> List1 = new List<ProxiNode>(); List<ProxiNode> List2 = new List<ProxiNode>();
                                for (int n = 0; n < ProcessPatterns[m].Count; n++)
                                {
                                    if (CacheNode.TagID == ProcessPatterns[m][n].TagID)
                                    {
                                        for (int L1 = 0; L1 < n; L1++)
                                        {
                                            List1.Add(ProcessPatterns[m][L1]);
                                        }

                                        for (int L2 = n + 1; L2 < ProcessPatterns[m].Count; L2++)
                                        {
                                            List2.Add(ProcessPatterns[m][L2]);
                                        }

                                        ProcessPatterns.RemoveAt(m);
                                        break;
                                    }
                                }

                                if (List1.Count >= 2)
                                {
                                    CacheProcessPattern.Add(List1);
                                }

                                if (List2.Count >= 2)
                                {
                                    CacheProcessPattern.Add(List2);
                                }
                            }

                            for (int m = 0; m < CacheProcessPattern.Count; m++)
                            {
                                ProcessPatterns.Add(CacheProcessPattern[m]);
                            }
                            #endregion 

                            #region 修改Node属性后，重新处理该pattern
                            i = i - 1; Label = true; break;
                            #endregion                           
                            #endregion                           
                        }
                        #endregion                                  
                    }
                    #endregion
                }

                if (!Label)
                {
                    #region 获得处理后的Pattern
                    List<ProxiNode> FinalPattern = new List<ProxiNode>();
                    for (int j = 0; j < ReMainedPattern.Count; j++)
                    {
                        for (int m = 0; m < ReMainedPattern[j].Count; m++)
                        {
                            FinalPattern.Add(ReMainedPattern[j][m]);
                        }
                    }
                    FinalPattern = FinalPattern.Distinct().ToList();//去重
                    #endregion

                    #region 如果处理后Pattern消失,更新Node的相交属性
                    if (FinalPattern.Count < 2)
                    {
                        for (int j = 0; j < ProcessPatterns[i].Count; j++)
                        {
                            if (NodeImportance[ProcessPatterns[i][j]] > 100)
                            {
                                int NewNodeImportance = NodeImportance[ProcessPatterns[i][j]] - 100 - ProcessPatterns[i].Count;
                                NodeImportance.Remove(ProcessPatterns[i][j]);
                                if (NewNodeImportance < 90)
                                {

                                    ProcessPatterns[i][j].IntersectNode = false;
                                }

                                NodeImportance.Add(ProcessPatterns[i][j], NewNodeImportance);
                            }
                        }
                    }

                    else
                    {
                        ProcessedPattern.Add(FinalPattern);
                    }
                    #endregion
                }
            }
            #endregion

            #region 删除非Pattern的建筑物点
            List<PolygonObject> ReMainedPolygon = new List<PolygonObject>();
            for (int i = 0; i <ProcessedPattern.Count; i++)
            {
                for (int j = 0; j < ProcessedPattern[i].Count; j++)
                {
                    PolygonObject CachePolygonObject = map.GetObjectbyID(ProcessedPattern[i][j].TagID, ProcessedPattern[i][j].FeatureType) as PolygonObject;
                    ReMainedPolygon.Add(CachePolygonObject);
                }
            }

            ReMainedPolygon = ReMainedPolygon.Distinct().ToList();
            map.PolygonList = ReMainedPolygon;
            #endregion

            #region 无效
            //KeyValuePair<List<ProxiNode>, int> Firstpair = PatternImportance.First();//每次处理权重最小的pattern
            //List<List<ProxiNode>> RemainedPattern = new List<List<ProxiNode>>();//存储Pattern处理后的结果            
            //List<List<ProxiNode>> ClipPatternNodes = this.PatternCluster(Firstpair.Key,NodeList);//建筑物裁剪(这样的一个裁剪方法，保证每次只需要更新NodeList中Node的属性即可)

            //do
            //{
            //    PatternImportance = PatternImportance.OrderByDescending(o => o.Value).ToDictionary(p => p.Key, o => o.Value);//权重降序排序
            //    KeyValuePair<List<ProxiNode>, int> Firstpair = PatternImportance.First();//每次处理权重最小的pattern
            //    List<List<ProxiNode>> RemainedPattern = new List<List<ProxiNode>>();//存储Pattern处理后的结果            
            //    List<List<ProxiNode>> ClipPatternNodes = this.PatternCluster(Firstpair.Key);//建筑物裁剪
            //    PatternImportance.Remove(Firstpair.Key);//在当前这个dictionary中去除该Pattern

            //    for (int j = 0; j < ClipPatternNodes.Count; j++)
            //    {
            //        double MinDis = 1000000;

            //        #region ClipPatternNodes[j]的备份
            //        List<ProxiNode> CacheNodes = new List<ProxiNode>();
            //        for (int m = 0; m < ClipPatternNodes[j].Count; m++)
            //        {
            //            CacheNodes.Add(ClipPatternNodes[j][m]);
            //        }
            //        #endregion

            //        #region 计算初始建筑物pattern建筑物间的最短距离
            //        for (int m = 0; m < CacheNodes.Count - 1; m++)
            //        {
            //            for (int n = m + 1; n < CacheNodes.Count; n++)
            //            {
            //                PolygonObject po1 = map.GetObjectbyID(CacheNodes[m].TagID, CacheNodes[m].FeatureType) as PolygonObject;
            //                PolygonObject po2 = map.GetObjectbyID(CacheNodes[n].TagID, CacheNodes[n].FeatureType) as PolygonObject;

            //                if (po1.GetMiniDistance(po2) < MinDis)
            //                {
            //                    MinDis = po1.GetMiniDistance(po2);
            //                }
            //            }
            //        }
            //        #endregion

            //        #region 典型化过程
            //        while (MinDis < MinDistance && CacheNodes.Count > 2)
            //        {
            //            int Num = CacheNodes.Count - 1;
            //            List<List<double>> NewPositions = NewPositionComputation(ClipPatternNodes[j], Num);
            //            ProxiNode MinNode = GetMinPolygonNode(CacheNodes, map);
            //            PolygonObject RemovePolygon = map.GetObjectbyID(MinNode.TagID, MinNode.FeatureType) as PolygonObject;
            //            CacheNodes.Remove(MinNode);

            //            #region 更新建筑物位置
            //            for (int m = 0; m < CacheNodes.Count; m++)
            //            {
            //                PolygonObject po3 = map.GetObjectbyID(CacheNodes[m].TagID, CacheNodes[m].FeatureType) as PolygonObject;

            //                double curDx = NewPositions[m][0] - po3.CalProxiNode().X;
            //                double curDy = NewPositions[m][1] - po3.CalProxiNode().Y;

            //                //更新多边形点集的每一个点坐标
            //                foreach (TriNode curPoint in po3.PointList)
            //                {
            //                    curPoint.X += curDx;
            //                    curPoint.Y += curDy;
            //                }
            //            }
            //            #endregion

            //            #region 重新计算最短距离
            //            MinDis = 10000000;
            //            for (int m = 0; m < CacheNodes.Count - 1; m++)
            //            {
            //                for (int n = m + 1; n < CacheNodes.Count; n++)
            //                {
            //                    PolygonObject po1 = map.GetObjectbyID(CacheNodes[m].TagID, CacheNodes[m].FeatureType) as PolygonObject;
            //                    PolygonObject po2 = map.GetObjectbyID(CacheNodes[n].TagID, CacheNodes[n].FeatureType) as PolygonObject;

            //                    if (po1.GetMiniDistance(po2) < MinDis)
            //                    {
            //                        MinDis = po1.GetMiniDistance(po2);
            //                    }
            //                }
            //            }
            //            #endregion
            //        }
            //        #endregion

            //        #region 处理完不存在冲突
            //        if (MinDis > MinDistance && CacheNodes.Count >= 2)
            //        {
            //            RemainedPattern.Add(CacheNodes);
            //        }
            //        #endregion

            //        #region 处理完仍存在冲突
            //        else
            //        {
            //            if (CacheNodes[0].IntersectNode && CacheNodes[1].IntersectNode)
            //            {
            //                #region 获得交点的权值
            //                int Node0Im = 0; int Node1Im = 0;
            //                foreach (KeyValuePair<ProxiNode, int> nKvp in NodeImportance)
            //                {
            //                    if (nKvp.Key.TagID == CacheNodes[0].TagID)
            //                    {
            //                        Node0Im = nKvp.Value;
            //                    }

            //                    if (nKvp.Key.TagID == CacheNodes[1].TagID)
            //                    {
            //                        Node1Im = nKvp.Value;
            //                    }
            //                }
            //                #endregion

            //                #region 处理权值较小的交点(只处理权值更小的Pattern)
            //                ProxiNode CacheNode = CacheNodes[0];
            //                if (Node0Im > Node1Im)
            //                {
            //                    CacheNode = CacheNodes[1];
            //                }

            //                List<List<ProxiNode>> CacheProcessedPattern = new List<List<ProxiNode>>();
            //                for (int i = ProcessedPattern.Count - 1; i >= 0; i--)
            //                {
            //                    List<ProxiNode> List1 = new List<ProxiNode>(); List<ProxiNode> List2 = new List<ProxiNode>();
            //                    for (int m = 0; m < ProcessedPattern[i].Count; m++)
            //                    {
            //                        if (CacheNode.TagID == ProcessedPattern[i][m].TagID)
            //                        {
            //                            for (int L1 = 0; L1 < m; L1++)
            //                            {
            //                                List1.Add(ProcessedPattern[i][L1]);
            //                            }

            //                            for (int L2 = m + 1; L2 < ProcessedPattern[i].Count; L2++)
            //                            {
            //                                List2.Add(ProcessedPattern[i][L2]);
            //                            }

            //                            ProcessedPattern.RemoveAt(i);

            //                            break;
            //                        }
            //                    }

            //                    if (List1.Count >= 2)
            //                    {
            //                        CacheProcessedPattern.Add(List1);
            //                    }

            //                    if (List2.Count >= 2)
            //                    {
            //                        CacheProcessedPattern.Add(List2);
            //                    }
            //                }

            //                if (CacheProcessedPattern.Count > 0)
            //                {
            //                    for (int i = 0; i < CacheProcessedPattern.Count; i++)
            //                    {
            //                        ProcessedPattern.Add(CacheProcessedPattern[i]);
            //                    }
            //                }
            //                #endregion

            //                #region 更新Node属性(是否是相交点);并更新权重                        
            //                List<List<ProxiNode>> CachePatternImportance = new List<List<ProxiNode>>(PatternImportance.Keys);
                            
            //                for (int m = 0; m < CachePatternImportance.Count;m++)
            //                {
            //                    int OldPatternValue = PatternImportance[CachePatternImportance[m]];
            //                    bool ChangeLabel = false;
            //                    for (int i = 0; i < CachePatternImportance[m].Count; i++)
            //                    {
            //                        if (CachePatternImportance[m][i].TagID == CacheNode.TagID)
            //                        {
            //                            CachePatternImportance[m][i].IntersectNode = false;
            //                            ChangeLabel = true;
            //                        }
            //                    }

            //                    if (ChangeLabel)
            //                    {
            //                        PatternImportance.Remove(CachePatternImportance[m]);
            //                        PatternImportance.Add(CachePatternImportance[m], OldPatternValue);
            //                    }
            //                }
            //                #endregion
            //            }
            //        }
            //        #endregion
            //    }

            //    #region 获得处理后的Pattern
            //    List<ProxiNode> FinalPattern = new List<ProxiNode>();
            //    for (int i = 0; i < RemainedPattern.Count; i++)
            //    {
            //        for (int j = 0; j < RemainedPattern[i].Count; j++)
            //        {
            //            FinalPattern.Add(RemainedPattern[i][j]);
            //        }
            //    }
            //    FinalPattern = FinalPattern.Distinct().ToList();//去重
            //    ProcessedPattern.Add(FinalPattern);
            //    #endregion

            //    #region 如果处理后Pattern消失
            //    if (FinalPattern.Count < 2)
            //    {                 
            //        #region 更新PatternImportance的权重;更新NodeImportance的权重;并更新Pattern中Node的相交属性
            //        List<List<ProxiNode>> CachePatternImportance = new List<List<ProxiNode>>(PatternImportance.Keys);
            //        for (int i = 0; i < Firstpair.Key.Count; i++)
            //        {
            //            int OldNodeValue = NodeImportance[Firstpair.Key[i]];
            //            for (int j = 0; j < CachePatternImportance.Count; j++)
            //            {
            //                int OldPatternValue = PatternImportance[CachePatternImportance[j]];
            //                bool ChangeLabel = false;
            //                for (int m = 0; m < CachePatternImportance[j].Count; m++)
            //                {
            //                    if (CachePatternImportance[j][m] == Firstpair.Key[i])//表示剩下的Pattern中存在Pattern与消失的Pattern相交
            //                    {
            //                        OldPatternValue = OldPatternValue - 100;
            //                        OldNodeValue = OldNodeValue - 100;
            //                        CachePatternImportance[j][m].IntersectNode = false;
            //                        ChangeLabel = true;
            //                    }
            //                }

            //                if (ChangeLabel)
            //                {
            //                    PatternImportance.Add(CachePatternImportance[j], OldPatternValue - Firstpair.Key.Count);
            //                    PatternImportance.Remove(CachePatternImportance[j]);
            //                }
            //            }

            //            NodeImportance.Remove(Firstpair.Key[i]);
            //            NodeImportance.Add(Firstpair.Key[i], OldNodeValue - Firstpair.Key.Count);
            //        }
            //       #endregion               
            //    }
            //    #endregion
            //} while (PatternImportance != null);
            #endregion
        }

        /// <summary>
        /// 根据两个给定的Node点，计算Node的中心
        /// </summary>
        /// <param name="Node1"></param>
        /// <param name="Node2"></param>
        /// <returns></returns>
        List<double> NewPositionComputation(ProxiNode Node1,ProxiNode Node2)
        {
            List<double> PointXY = new List<double>();

            double X = (Node1.X + Node2.X) / 2;
            double Y = (Node1.Y + Node2.Y) / 2;
            PointXY.Add(X); PointXY.Add(Y);

            return PointXY;
        }

        /// <summary>
        /// 根据两个给定的Node点面积，算加权平均Node的中心
        /// </summary>
        /// <param name="Node1"></param>
        /// <param name="Node2"></param>
        /// <returns></returns>
        List<double> NewPositionComputation(ProxiNode Node1, ProxiNode Node2,SMap map)
        {
            List<double> PointXY = new List<double>();

            PolygonObject po1 = map.GetObjectbyID(Node1.TagID,Node1.FeatureType) as PolygonObject;
            PolygonObject po2 = map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

            double X = (Node1.X * po1.Area + Node2.X * po2.Area) / (po1.Area + po2.Area);
            double Y = (Node1.Y * po1.Area + Node2.Y * po2.Area) / (po1.Area + po2.Area);
            PointXY.Add(X); PointXY.Add(Y);

            return PointXY;
        }

        /// <summary>
        /// 根据给定的pattern和pattern中需保留建筑物的个数，计算新的建筑物位置
        /// </summary>
        /// <param name="PatternNodes"></param>
        /// <param name="Num"></param>
        /// <returns></returns>
        List<List<double>> NewPositionComputation(List<ProxiNode> PatternNodes,int Num)
        {
            List<List<double>> NewPositionList = new List<List<double>>();
            
            for (int i = 0; i < Num; i++)
            {
                List<double> PositionXY=new List<double>();
                double X = ((Num - i - 1) * PatternNodes[0].X + i * PatternNodes[PatternNodes.Count - 1].X) / (Num - 1);
                double Y = ((Num - i - 1) * PatternNodes[0].Y + i * PatternNodes[PatternNodes.Count - 1].Y) / (Num - 1);

                PositionXY.Add(X); PositionXY.Add(Y);
                NewPositionList.Add(PositionXY);
            }

            return NewPositionList;
        }

        /// <summary>
        /// 计算调整后的间隔(只按照重心计算)
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        List<List<double>> NewPositionComputation(List<ProxiNode> PatternNodes, double Td)
        {
            List<List<double>> NewPositionList = new List<List<double>>();

            int RelocatedPosition = -1;
            while(this.NeedRelocation(PatternNodes,out RelocatedPosition,Td))
            {
                #region 计算移位距离
                double Dis1 = Math.Sqrt((PatternNodes[RelocatedPosition].X - PatternNodes[RelocatedPosition + 1].X) * (PatternNodes[RelocatedPosition].X - PatternNodes[RelocatedPosition + 1].X)
              + (PatternNodes[RelocatedPosition].Y - PatternNodes[RelocatedPosition + 1].Y) * (PatternNodes[RelocatedPosition].Y - PatternNodes[RelocatedPosition + 1].Y));
                double Dis2 = Math.Sqrt((PatternNodes[RelocatedPosition + 1].X - PatternNodes[RelocatedPosition + 2].X) * (PatternNodes[RelocatedPosition + 1].X - PatternNodes[RelocatedPosition + 2].X)
                    + (PatternNodes[RelocatedPosition + 1].Y - PatternNodes[RelocatedPosition + 2].Y) * (PatternNodes[RelocatedPosition + 1].Y - PatternNodes[RelocatedPosition + 2].Y));
                double DisplaceDis = 0.5 * Math.Abs(Dis1 - Dis2);

                double dx=0;double dy=0;

                if (Dis1 > Dis2)
                {
                    dx = (PatternNodes[RelocatedPosition].X - PatternNodes[RelocatedPosition + 1].X) / Dis1 * DisplaceDis;
                    dy = (PatternNodes[RelocatedPosition].Y - PatternNodes[RelocatedPosition + 1].Y) / Dis1 * DisplaceDis;
                }

                else
                {
                    dx = (PatternNodes[RelocatedPosition+2].X - PatternNodes[RelocatedPosition + 1].X) / Dis1 * DisplaceDis;
                    dy = (PatternNodes[RelocatedPosition+2].Y - PatternNodes[RelocatedPosition + 1].Y) / Dis1 * DisplaceDis;
                }
                #endregion

                #region 更新
                PatternNodes[RelocatedPosition + 1].X = PatternNodes[RelocatedPosition + 1].X + dx;
                PatternNodes[RelocatedPosition + 1].Y = PatternNodes[RelocatedPosition + 1].Y + dy;
                #endregion
            }

            for (int i = 0; i < PatternNodes.Count; i++)
            {
                List<double> PositionXY = new List<double>();
                double X = PatternNodes[i].X;
                double Y = PatternNodes[i].Y;

                PositionXY.Add(X); PositionXY.Add(Y);
                NewPositionList.Add(PositionXY);
            }

            return NewPositionList;
        }

        /// <summary>
        /// 计算Pattern是否需要调整，并返回需要调整的位置
        /// </summary>
        /// <param name="PatternNodes"></param>
        /// <returns></returns>
        bool NeedRelocation(List<ProxiNode> PatternNodes,out int RelocatedPosition,double Td)
        {
            bool NeedRelocated = false;
            RelocatedPosition = -1;

            #region 获得不同间隔的比例
            List<double> IntList = new List<double>();
            for (int i = 0; i < PatternNodes.Count - 2;i++ )
            {
                double Dis1 = Math.Sqrt((PatternNodes[i].X - PatternNodes[i + 1].X) * (PatternNodes[i].X - PatternNodes[i + 1].X)
                    + (PatternNodes[i].Y - PatternNodes[i + 1].Y) * (PatternNodes[i].Y - PatternNodes[i + 1].Y));
                double Dis2 = Math.Sqrt((PatternNodes[i+1].X - PatternNodes[i + 2].X) * (PatternNodes[i+1].X - PatternNodes[i + 2].X)
                    + (PatternNodes[i+1].Y - PatternNodes[i + 2].Y) * (PatternNodes[i+1].Y - PatternNodes[i + 2].Y));

                double IntRate = -1;
                if (Dis1 < Dis2)
                {
                    IntRate = Dis1 / Dis2;
                }

                else
                {
                    IntRate = Dis2 / Dis1;
                }

                IntList.Add(IntRate);
            }
            #endregion

            #region 获取其中比例尺最小的间隔
            double MinInt = IntList.Min();
            RelocatedPosition = IntList.IndexOf(MinInt);
            #endregion

            if (MinInt < Td)
            {
                NeedRelocated = true;
            }

            return NeedRelocated;
        }


        /// <summary>
        /// 顾及建筑物所处的位置，若处于边界，则位置取边界的位置（只考虑边界的位置）
        /// </summary>
        /// <returns></returns>
        List<double> NewPositionComputation(ProxiNode Node1, ProxiNode Node2, List<ProxiNode> NodesofPattern,SMap map)
        {
            List<double> PointXY = new List<double>();


            if (NodesofPattern[0] == Node1 || NodesofPattern[NodesofPattern.Count - 1] == Node1)
            {
                PointXY.Add(Node1.X);
                PointXY.Add(Node1.Y);
            }

            else if (NodesofPattern[0] == Node2 || NodesofPattern[NodesofPattern.Count - 1] == Node2)
            {
                PointXY.Add(Node2.X);
                PointXY.Add(Node2.Y);
            }

            else
            {
                PolygonObject po1 = map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject po2 = map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                double X = (Node1.X * po1.Area + Node2.X * po2.Area) / (po1.Area + po2.Area);
                double Y = (Node1.Y * po1.Area + Node2.Y * po2.Area) / (po1.Area + po2.Area);
                PointXY.Add(X); PointXY.Add(Y);
            }

            return PointXY;
        }

        /// <summary>
        /// 同时顾及建筑物所处的位置，若处于边界，则位置取边界的位置（只考虑边界的位置）
        /// </summary>
        /// <returns></returns>
        List<double> NewPositionComputation(ProxiNode Node1, ProxiNode Node2, List<ProxiNode> NodesofPattern, SMap map,double Weight)
        {
            List<double> PointXY = new List<double>();
            PolygonObject po1 = map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
            PolygonObject po2 = map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;
            double po1Area=po1.Area;double po2Area=po2.Area;

            if (NodesofPattern[0] == Node1 || NodesofPattern[NodesofPattern.Count - 1] == Node1)
            {
                po1Area = po1Area * Weight;
            }

            else if (NodesofPattern[0] == Node2 || NodesofPattern[NodesofPattern.Count - 1] == Node2)
            {
                po2Area = po2Area * Weight;
            }
                       
            double X = (Node1.X * po1Area + Node2.X * po2Area) / (po1Area + po2Area);
            double Y = (Node1.Y * po1Area + Node2.Y * po2Area) / (po1Area + po2Area);
            PointXY.Add(X); PointXY.Add(Y);
   
            return PointXY;
        }

        /// <summary>
        /// 计算pattern中面积最小
        /// </summary>
        /// <param name="PatternNode"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        ProxiNode GetMinPolygonNode(List<ProxiNode> PatternNodes,SMap map)
        {
            double MinArea = 10000000;
            ProxiNode MinNode = null;

            for (int i = 1; i < PatternNodes.Count-1; i++)
            {
                PolygonObject po1 = map.GetObjectbyID(PatternNodes[i].TagID, PatternNodes[i].FeatureType) as PolygonObject;
                if (po1.Area < MinArea)
                {
                    MinArea = po1.Area;
                    MinNode = PatternNodes[i];
                }
            }

            return MinNode;
        }

        /// <summary>
        /// 计算pattern非端点中面积最小的点
        /// </summary>
        /// <param name="PatternNode"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        ProxiNode GetMinPolygonNode2(List<ProxiNode> PatternNodes, SMap map)
        {
            double MinArea = 10000000;
            ProxiNode MinNode = null;

            for (int i = 1; i < PatternNodes.Count-1; i++)
            {
                PolygonObject po1 = map.GetObjectbyID(PatternNodes[i].TagID, PatternNodes[i].FeatureType) as PolygonObject;
                if (po1.Area < MinArea)
                {
                    MinArea = po1.Area;
                    MinNode = PatternNodes[i];
                }
            }

            return MinNode;
        }

        /// <summary>
        /// 计算pattern中非交点或边界点面积最小的点
        /// </summary>
        /// <param name="PatternNodes"></param>
        /// <param name="map"></param>
        /// <param name="NodeList"></param>
        /// <returns></returns>
        ProxiNode GetMinPolygonNode(List<ProxiNode> PatternNodes, SMap map, List<ProxiNode> NodeList)
        {
            double MinArea = 10000000;
            ProxiNode MinNode = null;

            for (int i = 0; i < PatternNodes.Count; i++)
            {
                PolygonObject po1 = map.GetObjectbyID(PatternNodes[i].TagID, PatternNodes[i].FeatureType) as PolygonObject;

                for (int j = 0; j < NodeList.Count; j++)
                {
                    if (PatternNodes[i].TagID == NodeList[j].TagID)
                    {
                        if (!(NodeList[j].IntersectNode || NodeList[j].BoundaryNode))
                        {
                            if (po1.Area < MinArea)
                            {
                                MinArea = po1.Area;
                                MinNode = PatternNodes[i];
                            }
                        }
                    }
                }
            }

            return MinNode;
        }

        /// <summary>
        /// 返回符号化的建筑物
        /// </summary>
        /// <param name="Polygon"></param>
        /// <param name="Scale"></param>
        /// <returns></returns>
        public PolygonObject SymbolizedPolygon(PolygonObject Polygon, double Scale,double lLength,double sLength,out bool Label)
        {
            Label = false;

            PrDispalce.工具类.ParameterCompute parametercompute = new 工具类.ParameterCompute();
            //MultipleLevelDisplace Md = new MultipleLevelDisplace();

            IPolygon pPolygon = PolygonObjectConvert(Polygon);
            IPolygon SMBR = parametercompute.GetSMBR(pPolygon);
            object PolygonSymbol = Symbol.PolygonSymbolization(3, 100, 100, 100, 0, 0, 0, 0);
            //pMapControl.DrawShape(SMBR, ref PolygonSymbol);
            //pMapControl.DrawShape(pPolygon, ref PolygonSymbol);

            #region 计算SMBR的方向
            IArea sArea = SMBR as IArea;
            IPoint CenterPoint = sArea.Centroid;

            IPointCollection sPointCollection = SMBR as IPointCollection;
            IPoint Point1 = sPointCollection.get_Point(1);
            IPoint Point2 = sPointCollection.get_Point(2);
            IPoint Point3 = sPointCollection.get_Point(3);
            IPoint Point4 = sPointCollection.get_Point(4);

            ILine Line1 = new LineClass(); Line1.FromPoint = Point1; Line1.ToPoint = Point2;
            ILine Line2 = new LineClass(); Line2.FromPoint = Point2; Line2.ToPoint = Point3;

            double Length1 = Line1.Length; double Length2 = Line2.Length; double Angle = 0;

            double LLength = 0; double SLength = 0;

            IPoint oPoint1 = new PointClass(); IPoint oPoint2 = new PointClass();
            IPoint oPoint3 = new PointClass(); IPoint oPoint4 = new PointClass();

            if (Length1 > Length2)
            {
                Angle = Line1.Angle;
                LLength = Length1; SLength = Length2;
            }

            else
            {
                Angle = Line2.Angle;
                LLength = Length2; SLength = Length1;
            }
            #endregion

            #region 对不能依比例尺符号化的SMBR旋转后符号化
            if (LLength < lLength * Scale / 1000 || SLength < sLength * Scale / 1000)
            {
                Label = true;

                if (LLength < lLength * Scale / 1000)
                {
                    LLength = lLength * Scale / 1000;
                }

                if (SLength < sLength * Scale / 1000)
                {
                    SLength = sLength * Scale / 1000;
                }

                IEnvelope pEnvelope = new EnvelopeClass();
                pEnvelope.XMin = CenterPoint.X - LLength / 2;
                pEnvelope.YMin = CenterPoint.Y - SLength / 2;
                pEnvelope.Width = LLength; pEnvelope.Height = SLength;
                pMapControl.DrawShape(pEnvelope, ref PolygonSymbol);
           
                #region 将SMBR旋转回来
                TriNode pNode1 = new TriNode(); TriNode pNode2 = new TriNode();
                TriNode pNode3 = new TriNode(); TriNode pNode4 = new TriNode();

                pNode1.X = (pEnvelope.XMin - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMin - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode1.Y = (pEnvelope.XMin - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMin - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                pNode2.X = (pEnvelope.XMax - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMin - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode2.Y = (pEnvelope.XMax - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMin - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                pNode3.X = (pEnvelope.XMax - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMax - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode3.Y = (pEnvelope.XMax - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMax - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                pNode4.X = (pEnvelope.XMin - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMax - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode4.Y = (pEnvelope.XMin - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMax - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                List<TriNode> TList = new List<TriNode>();
                TList.Add(pNode1); TList.Add(pNode2); TList.Add(pNode3); TList.Add(pNode4); //TList.Add(pNode1);
                PolygonObject pPolygonObject = new PolygonObject(Polygon.ID, TList);//ID标识建筑物的标号
                #endregion

                #region 新建筑物属性赋值
                pPolygonObject.ClassID = Polygon.ClassID;
                pPolygonObject.ID = Polygon.ID;
                pPolygonObject.TagID = Polygon.TagID;
                pPolygonObject.TypeID = Polygon.TypeID;
                #endregion

                return pPolygonObject;
            } 
            #endregion

            else
            {
                return Polygon;
            }
        }

        /// 返回符号化的建筑物
        /// </summary>
        /// <param name="Polygon"></param>
        /// <param name="Scale"></param>
        /// <returns></returns>
        public PolygonObject SymbolizedPolygon2(PolygonObject Polygon, double Scale, double lLength, double sLength, out bool Label)
        {
            Label = false;

            PrDispalce.工具类.ParameterCompute parametercompute = new 工具类.ParameterCompute();
            //MultipleLevelDisplace Md = new MultipleLevelDisplace();

            IPolygon pPolygon = PolygonObjectConvert(Polygon);
            IPolygon SMBR = parametercompute.GetSMBR(pPolygon);
            object PolygonSymbol = Symbol.PolygonSymbolization(3, 100, 100, 100, 0, 0, 0, 0);
            //pMapControl.DrawShape(SMBR, ref PolygonSymbol);
            //pMapControl.DrawShape(pPolygon, ref PolygonSymbol);

            #region 计算SMBR的方向
            IArea sArea = SMBR as IArea;
            IPoint CenterPoint = sArea.Centroid;

            IPointCollection sPointCollection = SMBR as IPointCollection;
            IPoint Point1 = sPointCollection.get_Point(1);
            IPoint Point2 = sPointCollection.get_Point(2);
            IPoint Point3 = sPointCollection.get_Point(3);
            IPoint Point4 = sPointCollection.get_Point(4);

            ILine Line1 = new LineClass(); Line1.FromPoint = Point1; Line1.ToPoint = Point2;
            ILine Line2 = new LineClass(); Line2.FromPoint = Point2; Line2.ToPoint = Point3;

            double Length1 = Line1.Length; double Length2 = Line2.Length; double Angle = 0;

            double LLength = 0; double SLength = 0;

            IPoint oPoint1 = new PointClass(); IPoint oPoint2 = new PointClass();
            IPoint oPoint3 = new PointClass(); IPoint oPoint4 = new PointClass();

            if (Length1 > Length2)
            {
                Angle = Line1.Angle;
                LLength = Length1; SLength = Length2;
            }

            else
            {
                Angle = Line2.Angle;
                LLength = Length2; SLength = Length1;
            }
            #endregion

            #region 对不能依比例尺符号化的SMBR旋转后符号化
            if (LLength < lLength * Scale / 1000 || SLength < sLength * Scale / 1000)
            {
                Label = true;

                if (LLength < lLength * Scale / 1000)
                {
                    LLength = lLength * Scale / 1000;
                }

                if (SLength < sLength * Scale / 1000)
                {
                    SLength = sLength * Scale / 1000;
                }

                lLength = Scale / 1000 * lLength; sLength = Scale / 1000 * sLength;
                IEnvelope pEnvelope = new EnvelopeClass();
                pEnvelope.XMin = CenterPoint.X - LLength / 2;
                pEnvelope.YMin = CenterPoint.Y - SLength / 2;
                pEnvelope.Width = LLength; pEnvelope.Height = SLength;
                pMapControl.DrawShape(pEnvelope, ref PolygonSymbol);

                #region 将SMBR旋转回来
                TriNode pNode1 = new TriNode(); TriNode pNode2 = new TriNode();
                TriNode pNode3 = new TriNode(); TriNode pNode4 = new TriNode();

                pNode1.X = (pEnvelope.XMin - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMin - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode1.Y = (pEnvelope.XMin - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMin - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                pNode2.X = (pEnvelope.XMax - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMin - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode2.Y = (pEnvelope.XMax - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMin - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                pNode3.X = (pEnvelope.XMax - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMax - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode3.Y = (pEnvelope.XMax - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMax - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                pNode4.X = (pEnvelope.XMin - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMax - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode4.Y = (pEnvelope.XMin - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMax - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                List<TriNode> TList = new List<TriNode>();
                TList.Add(pNode1); TList.Add(pNode2); TList.Add(pNode3); TList.Add(pNode4); //TList.Add(pNode1);
                PolygonObject pPolygonObject = new PolygonObject(Polygon.ID, TList);//ID标识建筑物的标号
                #endregion

                #region 新建筑物属性赋值
                pPolygonObject.ClassID = Polygon.ClassID;
                pPolygonObject.ID = Polygon.ID;
                pPolygonObject.TagID = Polygon.TagID;
                pPolygonObject.TypeID = Polygon.TypeID;
                #endregion

                return pPolygonObject;
            }
            #endregion

            else
            {
                return Polygon;
            }
        }

        /// <summary>
        /// 将建筑物转化为Polygonobject
        /// </summary>
        /// <param name="pPolygonObject"></param>
        /// <returns></returns>
        IPolygon PolygonObjectConvert(PolygonObject pPolygonObject)
        {
            Ring ring1 = new RingClass();
            object missing = Type.Missing;

            IPoint curResultPoint = new PointClass();
            TriNode curPoint = null;
            if (pPolygonObject != null)
            {
                for (int i = 0; i < pPolygonObject.PointList.Count; i++)
                {
                    curPoint = pPolygonObject.PointList[i];
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    ring1.AddPoint(curResultPoint, ref missing, ref missing);
                }
            }

            IGeometryCollection pointPolygon = new PolygonClass();
            pointPolygon.AddGeometry(ring1 as IGeometry, ref missing, ref missing);
            IPolygon pPolygon = pointPolygon as IPolygon;
            pPolygon.SimplifyPreserveFromTo();
            return pPolygon;
        }

        /// <summary>
        /// 对相交的pattern进行聚类（输入的是pattern对应的Nodes）
        /// </summary>
        /// <param name="PatternNodeList"></param>
        /// <returns></returns>
        public List<List<List<ProxiNode>>> PatternCluster(List<List<ProxiNode>> PatternNodeList)
        {
            List<List<List<ProxiNode>>> PatternCluster = new List<List<List<ProxiNode>>>();

            #region 标记每个pattern是否被访问
            Dictionary<List<ProxiNode>, bool> PatternNodeListVisit = new Dictionary<List<ProxiNode>, bool>();//标识后面PatternNodeList是否被访问
            for (int i = 0; i < PatternNodeList.Count; i++)
            {
                PatternNodeListVisit.Add(PatternNodeList[i], false);
            }
            #endregion

            #region 获取相交的pattern
            for (int i = 0; i < PatternNodeList.Count; i++)
            {
                if (!PatternNodeListVisit[PatternNodeList[i]])
                {
                    List<List<ProxiNode>> PatternClusterList = new List<List<ProxiNode>>();
                    PatternClusterList.Add(PatternNodeList[i]);
                    PatternNodeListVisit[PatternNodeList[i]] = true;

                    for (int n = 0; n < PatternClusterList.Count; n++)
                    {
                        for (int j = 0; j < PatternNodeList.Count; j++)
                        {
                            if (!PatternNodeListVisit[PatternNodeList[j]])
                            {
                                for (int m = 0; m < PatternNodeList[j].Count; m++)
                                {
                                    if (PatternClusterList[n].Contains(PatternNodeList[j][m]))
                                    {
                                        PatternNodeList[j][m].IntersectNode = true;//将其标识为交点

                                        PatternClusterList.Add(PatternNodeList[j]);
                                        PatternNodeListVisit[PatternNodeList[j]] = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    PatternCluster.Add(PatternClusterList);
                }
            }
            #endregion

            return PatternCluster;
        }

        /// <summary>
        /// 对传入的Pattern，标识其中的交点
        /// </summary>
        /// <param name="PatternNodeList"></param>
        public void LabelIntesectNode(List<List<ProxiNode>> PatternNodeList)
        {
            for (int i = 0; i < PatternNodeList.Count; i++)
            {
                for (int m = 0; m < PatternNodeList[i].Count; m++)
                {
                    if (!PatternNodeList[i][m].IntersectNode)
                    {
                        for (int j = 0; j < PatternNodeList.Count; j++)
                        {
                            if (i != j)
                            {
                                if (PatternNodeList[j].Contains(PatternNodeList[i][m]))
                                {
                                    PatternNodeList[i][m].IntersectNode = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 对pattern进行拆分（裁剪），即碰到相交点，则将pattern拆分成两个，其中拆分后的pattern个数大于等于2
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public List<List<ProxiNode>> PatternCluster(List<ProxiNode> PatternNodes)
        {
            List<List<ProxiNode>> PatternClipCluster=new List<List<ProxiNode>>();

            for (int i = 0; i < PatternNodes.Count; i++)
            {
                List<ProxiNode> NodeList = new List<ProxiNode>();
                NodeList.Add(PatternNodes[i]);

                do
                {
                    if (i < PatternNodes.Count - 1)
                    {
                        NodeList.Add(PatternNodes[i + 1]);
                        i = i + 1;
                    }

                    else
                    {
                        break;
                    }
                } while (!PatternNodes[i].IntersectNode);
                
                
                PatternClipCluster.Add(NodeList);

                if (i < PatternNodes.Count-1 && PatternNodes[i].IntersectNode)
                {
                    i = i - 1;
                }
            }

            return PatternClipCluster;
        }

        /// <summary>
        /// 对pattern进行拆分（裁剪），即碰到相交点，则将pattern拆分成两个，其中拆分后的pattern个数大于等于2
        /// </summary>
        /// <param name="PatternNodes"></param>
        /// <param name="AllPatternNodes"></param>标识了交点是否为交点的情况
        /// <returns></returns>
        public List<List<ProxiNode>> PatternCluster(List<ProxiNode> PatternNodes, List<ProxiNode> AllPatternNodes)
        {
            List<List<ProxiNode>> PatternClipCluster = new List<List<ProxiNode>>();

            for (int i = 0; i < PatternNodes.Count; i++)
            {
                List<ProxiNode> NodeList = new List<ProxiNode>();
                NodeList.Add(PatternNodes[i]);

                do
                {
                    if (i < PatternNodes.Count - 1)
                    {
                        NodeList.Add(PatternNodes[i + 1]);
                        i = i + 1;
                    }

                    else
                    {
                        break;
                    }

                    for (int j = 0; j < AllPatternNodes.Count; j++)
                    {
                        if (AllPatternNodes[j].TagID == PatternNodes[i].TagID)
                        {
                            PatternNodes[i].IntersectNode = AllPatternNodes[j].IntersectNode;
                        }
                    }
                   
                } while (!PatternNodes[i].IntersectNode);


                PatternClipCluster.Add(NodeList);

                if (i < PatternNodes.Count - 1 && PatternNodes[i].IntersectNode)
                {
                    i = i - 1;
                }
            }

            return PatternClipCluster;
        }

        /// <summary>
        /// 对相似的建筑物做典型化（且不相交）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
           
        }

        /// <summary>
        /// 对相交且相似的Linearpattern做典型化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            map.InterpretatePoint(2);
            #endregion

            #region DT+CDT+SKE
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
            pg.CreatePgForBuildings(pg.NodeList, pg.EdgeList);

            //VoronoiDiagram vd = null;
            //vd = new AuxStructureLib.VoronoiDiagram(ske, pg, map);
            //vd.CreateVoronoiDiagramfrmSkeletonforBuildings();
            #endregion

            #region 获得相似且相交的建筑物排列
            pg.CreateRNGForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            pg.DeleteRepeatedEdge(pg.RNGBuildingEdgesListShortestDistance);
            List<List<ProxiEdge>> PatternEdgeList = pg.LinearPatternDetected2(pg.RNGBuildingEdgesListShortestDistance, pg.RNGBuildingNodesListShortestDistance, double.Parse(this.textBox3.Text.ToString()), double.Parse(this.textBox2.Text.ToString()), double.Parse(this.textBox4.Text.ToString()));
            for (int i = 0; i < PatternEdgeList.Count; i++)
            {
                for (int j = 0; j < PatternEdgeList[i].Count; j++)
                {
                    pg.PgforRefineBuildingPatternList.Add(PatternEdgeList[i][j]);
                }
            }

            //List<List<ProxiNode>> PatternNodes = pg.LinearPatternRefinement2(PatternEdgeList, map);
            //pg.EdgeforPattern(PatternNodes);
            #endregion

            #region 将待处理的Node坐标更新为其对应建筑物的重心（似乎在处理完后，个别Node点的坐标与其重心有一定偏差）
                //for (int i = 0; i < PatternNodes.Count; i++)
                //{
                //    for (int j = 0; j < PatternNodes[i].Count; j++)
                //    {
                //        for (int m = 0; m < map.PolygonList.Count; m++)
                //        {
                //            if (PatternNodes[i][j].TagID == map.PolygonList[m].TagID)
                //            {
                //                PatternNodes[i][j].X = map.PolygonList[m].CalProxiNode().X;
                //                PatternNodes[i][j].Y = map.PolygonList[m].CalProxiNode().Y;
                //            }
                //        }
                //    }
                //}
                #endregion

            //SimilarIntesectLinearPatternMesh2(PatternNodes, map, double.Parse(this.textBox5.Text));
            //map.WriteResult2Shp(OutPath, pMap.SpatialReference);
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "相似LinearPattern", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.PgforRefineSimilarBuildingPatternList); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "RNG", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.RNGBuildingEdgesListShortestDistance); }
            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "直线pattern", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.PgforRefineBuildingPatternList); }
        }

        #region 顾及邻近环境的建筑物典型化
        private void button5_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            map.InterpretatePoint(2);
            #endregion

            #region DT+CDT+SKE
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
            pg.CreatePgForBuildings(pg.NodeList, pg.EdgeList);

            //VoronoiDiagram vd = null;
            //vd = new AuxStructureLib.VoronoiDiagram(ske, pg, map);
            //vd.CreateVoronoiDiagramfrmSkeletonforBuildings();
            #endregion

            #region 获得相似且相交的建筑物排列
            pg.CreateRNGForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            pg.DeleteRepeatedEdge(pg.RNGBuildingEdgesListShortestDistance);
            List<List<ProxiEdge>> PatternEdgeList = pg.LinearPatternDetected2(pg.RNGBuildingEdgesListShortestDistance, pg.RNGBuildingNodesListShortestDistance, double.Parse(this.textBox3.Text.ToString()), double.Parse(this.textBox2.Text.ToString()), double.Parse(this.textBox4.Text.ToString()));
            List<List<ProxiNode>> PatternNodes = pg.LinearPatternRefinement2(PatternEdgeList, map);
            pg.EdgeforPattern(PatternNodes);
            #endregion
         
            #region 确定构建Pattern邻近图的建筑物
            List<PolygonObject> PolygonList = new List<PolygonObject>();
            //Dictionary<PolygonObject, bool> PolygonDic = new Dictionary<PolygonObject, bool>();
            for (int i = 0; i < PatternNodes.Count; i++)
            {
                for (int j = 0; j < PatternNodes[i].Count; j++)
                {
                    PolygonObject CacheObject = map.GetObjectbyID(PatternNodes[i][j].TagID, PatternNodes[i][j].FeatureType) as PolygonObject;
                    PolygonList.Add(CacheObject);
                    //PolygonDic.Add(CacheObject, false);
                }
            }
            PolygonList = PolygonList.Distinct().ToList();

            #region 去重 多余
            //for (int i = 0; i < PolygonList.Count; i++)
            //{
            //    PolygonObject Po1 = PolygonList[i];
            //    if (!PolygonDic[Po1])
            //    {
            //        for (int j = 0; j < PolygonList.Count; j++)
            //        {
            //            if (j != i)
            //            {
            //                if (PolygonList[i].TagID == PolygonList[j].TagID)
            //                {
            //                    PolygonDic[PolygonList[j]] = false;
            //                }
            //            }
            //        }
            //    }
            //}

            //foreach (KeyValuePair<PolygonObject, bool> kvp in PolygonDic)
            //{
            //    if (kvp.Value)
            //    {
            //        PolygonList.Remove(kvp.Key);
            //    }
            //}
            #endregion
            #endregion

            #region 获取图层
            List<IFeatureLayer> list2 = new List<IFeatureLayer>();
            if (this.comboBox2.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list2.Add(StreetLayer);
            }
            #endregion

            #region 建筑物数据读取
            SMap map2 = new SMap(list2);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();

            for (int i = 0; i < PolygonList.Count; i++)
            {
                List<TriNode> curPointList = new List<TriNode>();
                for (int j = 0; j < PolygonList[i].PointList.Count; j++)
                {
                    TriNode curVextex = new TriNode(PolygonList[i].PointList[j].X, PolygonList[i].PointList[j].Y, map2.TriNodeList.Count, i, FeatureType.PolygonType);
                    map2.TriNodeList.Add(curVextex);
                    curPointList.Add(curVextex);
                }
                PolygonObject curPP = new PolygonObject(i, curPointList);
                map2.PolygonList.Add(curPP);
            }
            #endregion

            #region 建立pattern的邻近图
            DelaunayTin dt2 = new DelaunayTin(map2.TriNodeList);
            dt2.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            ConvexNull cn2 = new ConvexNull(dt2.TriNodeList);
            cn2.CreateConvexNull();

            ConsDelaunayTin cdt2 = new ConsDelaunayTin(dt2);
            cdt2.CreateConsDTfromPolylineandPolygon(map2.PolylineList, map2.PolygonList);

            Triangle.WriteID(dt2.TriangleList);
            TriEdge.WriteID(dt2.TriEdgeList);

            AuxStructureLib.Skeleton ske2 = new AuxStructureLib.Skeleton(cdt2, map2);
            ske2.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
            ske2.TranverseSkeleton_Segment_PLP_NONull();

            ProxiGraph pg2 = new ProxiGraph();
            pg2.CreateProxiGraphfrmSkeletonBuildings(map2, ske2);
            pg2.DeleteRepeatedEdge(pg2.EdgeList);

            VoronoiDiagram vd2 = null;
            vd2 = new AuxStructureLib.VoronoiDiagram(ske2, pg2, map2);
            vd2.CreateVoronoiDiagramfrmSkeletonforBuildings();
            #endregion

            #region 重新找到在Map2构成Pattern的建筑物
            List<List<PolygonObject>> PolygonPatterns = new List<List<PolygonObject>>();
            List<List<ProxiNode>> newPatternNodes = new List<List<ProxiNode>>();
            for (int i = 0; i < PatternNodes.Count; i++)
            {
                List<PolygonObject> newPatternPolygon = new List<PolygonObject>();
                for (int j = 0; j < PatternNodes[i].Count; j++)
                {
                    PolygonObject CacheObject = map.GetObjectbyID(PatternNodes[i][j].TagID, PatternNodes[i][j].FeatureType) as PolygonObject;

                    for (int m = 0; m < map2.PolygonList.Count; m++)
                    {
                        if (CacheObject.CalProxiNode().X == map2.PolygonList[m].CalProxiNode().X && CacheObject.CalProxiNode().Y == map2.PolygonList[m].CalProxiNode().Y)
                        {
                            newPatternPolygon.Add(map2.PolygonList[m]);
                        }
                    }
                }
                PolygonPatterns.Add(newPatternPolygon);
            }

            for (int i = 0; i < PolygonPatterns.Count; i++)
            {
                List<ProxiNode> NewPatternNode = new List<ProxiNode>();
                for (int j = 0; j < PolygonPatterns[i].Count; j++)
                {
                    for (int m = 0; m < pg2.NodeList.Count; m++)
                    {
                        if (PolygonPatterns[i][j].ID == pg2.NodeList[m].TagID && PolygonPatterns[i][j].CalProxiNode().X == pg2.NodeList[m].X)
                        {
                            NewPatternNode.Add(pg2.NodeList[m]);
                        }
                    }
                }
                newPatternNodes.Add(NewPatternNode);
            }
            #endregion

            #region 建立Pattern顾及道路的层次邻近关系
            List<List<List<ProxiNode>>> PatternClusters = this.PatternCluster(newPatternNodes);
            List<List<List<PolygonObject>>> PatternPolygonClusters=new List<List<List<PolygonObject>>>();
            for (int i = 0; i < PatternClusters.Count; i++)
            {
                List<List<PolygonObject>> PatternPolygon=new List<List<PolygonObject>>();
                for (int j = 0; j < PatternClusters[i].Count; j++)
                {
                    List<PolygonObject> Pattern = new List<PolygonObject>();
                    for (int m = 0; m < PatternClusters[i][j].Count; m++)
                    {
                        PolygonObject CachePolygon = map2.GetObjectbyID(PatternClusters[i][j][m].TagID, FeatureType.PolygonType) as PolygonObject;
                        Pattern.Add(CachePolygon);
                    }
                    PatternPolygon.Add(Pattern);
                }
                PatternPolygonClusters.Add(PatternPolygon);
            }
            #endregion

            vd2.PatternFieldBuildBasedonRoad(pg2.EdgeList, PatternPolygonClusters);

            #region 力的计算；移位；符号化过程
            #endregion

            if (OutPath != null) { ske2.CDT.DT.WriteShp(OutPath, pMap.SpatialReference); }
            if (OutPath != null) { vd2.Create_WritePolygonObject2Shp(OutPath, "V图", pMap.SpatialReference); }
            if (OutPath != null) { pg2.WriteProxiGraph2Shp(OutPath, "邻近图", pMap.SpatialReference, pg2.NodeList, pg2.EdgeList); }
            if (OutPath != null) { ske2.Create_WriteSkeleton_Segment2Shp(OutPath, "骨架", pMap.SpatialReference); }
            //map2.WriteResult2Shp(OutPath, pMap.SpatialReference);

        }
        #endregion

        #region 建筑物符号化
        private void button6_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            map.InterpretatePoint(2);
            #endregion

            #region 对建筑物进行符号化
            int Count = 0;
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                if (map.PolygonList[i].Area < 25000 * 0.7 / 1000 * 25000 * 0.5 / 1000)
                {
                    bool Label = false;
                    map.PolygonList[i] = SymbolizedPolygon2(map.PolygonList[i], 25000, 0.7, 0.5, out Label);

                    if (Label)
                    {
                        Count++;
                    }
                }

                else
                {
                    bool Label = false;
                    map.PolygonList[i] = SymbolizedPolygon(map.PolygonList[i], 25000, 0.7, 0.5, out Label);
                }
            }
            MessageBox.Show(Count.ToString());
            #endregion

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
        }
        #endregion

        #region Pattern normalization
        private void button7_Click_1(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            map.InterpretatePoint(2);
            #endregion

            #region
            Pattern newPattern = new Pattern(0, map.PolygonList);
            newPattern.PatternNormalization();
            map.PolygonList = newPattern.NormalizedPatternObjects;
            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
            #endregion
        }
        #endregion

        private void button8_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            map.InterpretatePoint(2);
            #endregion

            #region 获取pattern
            List<Pattern> PatternList = new List<Pattern>();
            for (int i = 0; i < 2; i++)
            {
                List<PolygonObject> PolygonList = new List<PolygonObject>();
                for (int j = 0; j < map.PolygonList.Count; j++)
                {                   
                    if (map.PolygonList[j].PatternIDList[0] == i)
                    {
                        PolygonList.Add(map.PolygonList[j]);
                    }                  
                }

                Pattern newPattern = new Pattern(0, PolygonList);
                newPattern.PatternNormalization();
                PatternList.Add(newPattern);
            }
            #endregion

            PatternProcess pp = new PatternProcess();
            Pattern MergedPattern = pp.MergeTwoPatterns(PatternList[0], PatternList[1]);
            map.PolygonList = MergedPattern.NormalizedPatternObjects;
            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
        }

        #region PatternMerged整体测试
        private void button10_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            map.InterpretatePoint(2);
            #endregion

            #region DT+CDT+SKE
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
            pg.CreatePgForBuildings(pg.NodeList, pg.EdgeList);

            //VoronoiDiagram vd = null;
            //vd = new AuxStructureLib.VoronoiDiagram(ske, pg, map);
            //vd.CreateVoronoiDiagramfrmSkeletonforBuildings();
            #endregion

            #region 获得相似且相交的建筑物排列
            pg.CreateRNGForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            pg.DeleteRepeatedEdge(pg.RNGBuildingEdgesListShortestDistance);
            List<List<ProxiEdge>> PatternEdgeList = pg.LinearPatternDetected2(pg.RNGBuildingEdgesListShortestDistance, pg.RNGBuildingNodesListShortestDistance, double.Parse(this.textBox3.Text.ToString()), double.Parse(this.textBox2.Text.ToString()), double.Parse(this.textBox4.Text.ToString()));
            List<List<ProxiNode>> PatternNodes = pg.LinearPatternRefinement2(PatternEdgeList, map);
            pg.EdgeforPattern(PatternNodes);
            #endregion

            #region 获得Pattern
            List<Pattern> PatternList = new List<Pattern>();
            for (int i = 0; i < PatternNodes.Count; i++)
            {
                List<PolygonObject> PolygonList = new List<PolygonObject>();
                for (int j = 0; j < PatternNodes[i].Count; j++)
                {
                    PolygonObject po1 = map.GetObjectbyID(PatternNodes[i][j].TagID, PatternNodes[i][j].FeatureType) as PolygonObject;
                    PolygonList.Add(po1);
                }

                Pattern NewPattern = new Pattern(i, PolygonList);
                NewPattern.PatternNormalization();
                PatternList.Add(NewPattern);
            }
            #endregion

            #region 计算pattern间的关系
            #endregion
        }
        #endregion

        #region 线相交测试
        private void button11_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region 转化为线
            List<典型化.Line> LineList = new List<典型化.Line>();
            for (int i = 0; i < map.PolylineList.Count; i++)
            {
                List<典型化.Point> PointList = new List<典型化.Point>();
                for (int j = 0; j < map.PolylineList[i].PointList.Count; j++)
                {
                    典型化.Point IPoint = new 典型化.Point(map.PolylineList[i].PointList[j].X, map.PolylineList[i].PointList[j].Y);
                    PointList.Add(IPoint);
                }

                典型化.Line Line = new 典型化.Line(PointList);
                LineList.Add(Line);
            }
            #endregion

            #region 处理
            LineProcess LP = new LineProcess();
            for (int i = 0; i < LineList.Count-1; i++)
            {
                for (int j = i + 1; j < LineList.Count; j++)
                {
                    LP.InsertPointComputation(LineList[i], LineList[j], 20, 15,pMapControl);
                }
            }
            #endregion

            #region 添加并输出
            //int Id = 0;
            map.TriNodeList.Clear();
            for (int i = 0; i < LineList.Count; i++)
            {
                for (int j = 0; j < LineList[i].BoundaryPointList.Count; j++)
                {
                    TriNode pTriNode = new TriNode(LineList[i].BoundaryPointList[j].X, LineList[i].BoundaryPointList[j].Y);
                    map.TriNodeList.Add(pTriNode);

                    IPoint Point1 = new PointClass();
                    Point1.X = pTriNode.X; Point1.Y = pTriNode.Y;

                    ISimpleMarkerSymbol pMarkerSymbol = new SimpleMarkerSymbolClass();
                    esriSimpleMarkerStyle eSMS = (esriSimpleMarkerStyle)0;
                    pMarkerSymbol.Style = eSMS;

                    IRgbColor rgbColor = new RgbColorClass();
                    rgbColor.Red = 100;
                    rgbColor.Green = 100;
                    rgbColor.Blue = 100;

                    pMarkerSymbol.Color = rgbColor;
                    object oMarkerSymbol = pMarkerSymbol;
                    pMapControl.DrawShape(Point1, ref oMarkerSymbol);                 
                }

                for (int j = 0; j < LineList[i].IntersectPointList.Count; j++)
                {
                    TriNode pTriNode = new TriNode(LineList[i].IntersectPointList[j].X, LineList[i].IntersectPointList[j].Y);
                    map.TriNodeList.Add(pTriNode);

                    IPoint Point1 = new PointClass();
                    Point1.X = pTriNode.X; Point1.Y = pTriNode.Y;

                    ISimpleMarkerSymbol pMarkerSymbol = new SimpleMarkerSymbolClass();
                    esriSimpleMarkerStyle eSMS = (esriSimpleMarkerStyle)0;
                    pMarkerSymbol.Style = eSMS;

                    IRgbColor rgbColor = new RgbColorClass();
                    rgbColor.Red = 100;
                    rgbColor.Green = 100;
                    rgbColor.Blue = 100;

                    pMarkerSymbol.Color = rgbColor;
                    object oMarkerSymbol = pMarkerSymbol;
                    pMapControl.DrawShape(Point1, ref oMarkerSymbol);     
                }

                for (int j = 0; j < LineList[i].TemporaryPointList.Count; j++)
                {
                    TriNode pTriNode = new TriNode(LineList[i].TemporaryPointList[j].X, LineList[i].TemporaryPointList[j].Y);
                    map.TriNodeList.Add(pTriNode);

                    IPoint Point1 = new PointClass();
                    Point1.X = pTriNode.X; Point1.Y = pTriNode.Y;

                    ISimpleMarkerSymbol pMarkerSymbol = new SimpleMarkerSymbolClass();
                    esriSimpleMarkerStyle eSMS = (esriSimpleMarkerStyle)0;
                    pMarkerSymbol.Style = eSMS;

                    IRgbColor rgbColor = new RgbColorClass();
                    rgbColor.Red = 100;
                    rgbColor.Green = 100;
                    rgbColor.Blue = 100;

                    pMarkerSymbol.Color = rgbColor;
                    object oMarkerSymbol = pMarkerSymbol;
                    pMapControl.DrawShape(Point1, ref oMarkerSymbol);     
                }
            }
            #endregion

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);


        }
        #endregion

        #region 邻近关系更新测试
        private void button12_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region Pattern读取
            Dictionary<int, Pattern> PatternDic = new Dictionary<int, Pattern>();//找到每个pattern的建筑物
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                for (int j = 0; j < map.PolygonList[i].OrderIDList.Count; j++)
                {
                    if (PatternDic.ContainsKey(map.PolygonList[i].OrderIDList[j][0]))
                    {
                        PatternDic[map.PolygonList[i].OrderIDList[j][0]].PatternObjects.Add(map.PolygonList[i]);
                    }

                    else
                    {
                        List<PolygonObject> PatternObject = new List<PolygonObject>();
                        PatternObject.Add(map.PolygonList[i]);
                        Pattern NewPattern = new Pattern(map.PolygonList[i].OrderIDList[j][0], PatternObject);
                        PatternDic.Add(map.PolygonList[i].OrderIDList[j][0], NewPattern);
                    }
                }
            }

            Dictionary<int, Pattern> SortedPatternDic = new Dictionary<int, Pattern>();//pattern中建筑物按顺序排列
            foreach (var keyValuePair in PatternDic)
            {
                Dictionary<int, PolygonObject> SortPattern = new Dictionary<int, PolygonObject>();
                for (int i = 0; i < keyValuePair.Value.PatternObjects.Count; i++)
                {
                    for (int j = 0; j < keyValuePair.Value.PatternObjects[i].OrderIDList.Count; j++)
                    {
                        if (keyValuePair.Key == keyValuePair.Value.PatternObjects[i].OrderIDList[j][0])
                        {
                            SortPattern.Add(keyValuePair.Value.PatternObjects[i].OrderIDList[j][1], keyValuePair.Value.PatternObjects[i]);
                        }
                    }
                }

                Dictionary<int, PolygonObject> SortedPattern = SortPattern.OrderByDescending(o => o.Key).ToDictionary(p => p.Key, o => o.Value);//按pattern中建筑物的先后降序排序
                List<PolygonObject> SortBuildingPatterns = new List<PolygonObject>(SortedPattern.Values);
                Pattern sSortPattern = new Pattern(keyValuePair.Value.PatternID, SortBuildingPatterns);
                SortedPatternDic.Add(keyValuePair.Key, sSortPattern);
            }
            #endregion

            #region DT+CDT+SKE
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
            pg.CreatePgForBuildings(pg.NodeList, pg.EdgeList);
            #endregion

            #region 计算pattern的重要性
            List<Pattern> PatternList = new List<Pattern>(SortedPatternDic.Values);
            this.LabelBuilding(PatternList);
            for (int i = 0; i < PatternList.Count; i++)
            {
                int IntersectNum = 0; int BoundaryNum = 0;
                for (int j = 0; j < PatternList[i].PatternObjects.Count; j++)
                {
                    if (PatternList[i].PatternObjects[j].IntersectBuilding)
                    {
                        IntersectNum = IntersectNum + 1;
                    }

                    if (PatternList[i].PatternObjects[j].BoundaryBuilding)
                    {
                        BoundaryNum = BoundaryNum + 1;
                    }
                }

                PatternList[i].Importance = IntersectNum * 100 + BoundaryNum * 20 + PatternList[i].PatternObjects.Count;
            }
            #endregion

            #region 将pattern按权重重要性排序
            Dictionary<Pattern, double> ImportanceDic = new Dictionary<Pattern, double>();
            for (int i = 0; i < PatternList.Count; i++)
            {
                ImportanceDic.Add(PatternList[i], PatternList[i].Importance);
            }

            ImportanceDic = ImportanceDic.OrderByDescending(o => o.Value).ToDictionary(p => p.Key, o => o.Value);//权重降序排序
            List<Pattern> ProcessPatterns = new List<Pattern>(ImportanceDic.Keys);//待处理的Pattern
            #endregion

            #region 处理最重要的pattern
            //Pattern MostImportantPattern = ProcessPatterns[0];
                        
            //bool IntersectLabel = false; bool AdjaectLabel = false;
            //for (int j = 0; j < PatternList.Count; j++)
            //{
            //    #region 计算pattern与最重要pattern的关系
            //    if (MostImportantPattern.PatternID != PatternList[j].PatternID)
            //    {
            //        foreach (PolygonObject po in MostImportantPattern.PatternObjects)
            //        {
            //            foreach (PolygonObject ppo in PatternList[j].PatternObjects)
            //            {
            //                //判断是否相交
            //                if (po == ppo)
            //                {
            //                    IntersectLabel = true;
            //                }

            //                //判断是否相邻
            //                foreach (ProxiEdge pe in pg.PgforBuildingEdgesList)
            //                {
            //                    if ((pe.Node1.TagID == po.TagID && pe.Node2.TagID == ppo.TagID) ||
            //                        (pe.Node2.TagID == po.TagID && pe.Node1.TagID == ppo.TagID))
            //                    {
            //                        AdjaectLabel = true;
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    #endregion

            //    #region 处理找到的邻近Patterns
            //    if (!IntersectLabel && AdjaectLabel)
            //    {
            //        #region 依次计算邻近pattern与最重要pattern的冲突情况
            //        #endregion

            //        #region 根据冲突情况处理
            //        #endregion

            //        #region 更新建筑物；更新patterns
            //        #endregion
            //    }
            //    #endregion
            //}        
            #endregion

            #region 冲突探测
            PrDispalce.工具类.CollabrativeDisplacement.CConflictDetector ccd1 = new CConflictDetector();
            ccd1.ConflictDetectByPg(pg.PgforBuildingEdgesList, 7, PatternList, map,35000);
            PatternTargetEdge TargetEdge = ccd1.ReturnTragetEdgeTobeSolved(ccd1.ConflictEdge, PatternList);
            #endregion

            #region 找到冲突边涉及到的patterns;并找到其中最重要的pattern
            List<Pattern> InvolvedPatterns = new List<Pattern>();
            for (int i = 0; i < PatternList.Count; i++)
            {
                List<int> IdList = new List<int>();
                for (int j = 0; j < PatternList[i].PatternObjects.Count; j++)
                {
                    IdList.Add(PatternList[i].PatternObjects[j].ID);
                }

                if (IdList.Contains(TargetEdge.pTargetEdge.Node1.ID) || IdList.Contains(TargetEdge.pTargetEdge.Node2.ID))
                {
                    InvolvedPatterns.Add(PatternList[i]);
                }
            }

            Pattern SolidPattern = InvolvedPatterns[0];
            for (int i = 1; i < InvolvedPatterns.Count; i++)
            {
                if (ImportanceDic[SolidPattern] < ImportanceDic[InvolvedPatterns[i]])
                {
                    SolidPattern = InvolvedPatterns[i];
                }
            }
            #endregion

            #region 解决冲突
            if (TargetEdge.pTargetEdge!=null)//端点建筑物冲突
            {
                #region 确定那个不变的节点
                int SolidPolygonID = 0;
                for (int i = 0; i < SolidPattern.PatternObjects.Count; i++)
                {
                    if (SolidPattern.PatternObjects[i].ID == TargetEdge.pTargetEdge.Node1.ID)
                    {
                        SolidPolygonID = TargetEdge.pTargetEdge.Node1.ID;
                    }

                    else if (SolidPattern.PatternObjects[i].ID == TargetEdge.pTargetEdge.Node2.ID)
                    {
                        SolidPolygonID = TargetEdge.pTargetEdge.Node2.ID;
                    }
                }
                #endregion

                for (int i = 0; i < InvolvedPatterns.Count; i++)
                {
                    bool ContainSolidPolygon = false;
                    for (int j = 0; j < InvolvedPatterns[i].PatternObjects.Count; j++)
                    {
                        if (InvolvedPatterns[i].PatternObjects[j].ID == SolidPolygonID)
                        {
                            ContainSolidPolygon = true;
                        }
                    }

                    if (!ContainSolidPolygon)
                    {

                        if (InvolvedPatterns[i].PatternObjects[0].ID == TargetEdge.pTargetEdge.Node1.ID)
                        {
                            InvolvedPatterns[i].PatternObjects.Insert(0, map.GetObjectbyID(TargetEdge.pTargetEdge.Node2.ID, FeatureType.PolygonType) as PolygonObject);
                        }

                        if (InvolvedPatterns[i].PatternObjects[InvolvedPatterns[i].PatternObjects.Count - 1].ID == TargetEdge.pTargetEdge.Node1.ID)
                        {
                            InvolvedPatterns[i].PatternObjects.Add(map.GetObjectbyID(TargetEdge.pTargetEdge.Node2.ID, FeatureType.PolygonType) as PolygonObject);
                        }

                        if (InvolvedPatterns[i].PatternObjects[0].ID == TargetEdge.pTargetEdge.Node2.ID)
                        {
                            InvolvedPatterns[i].PatternObjects.Insert(0, map.GetObjectbyID(TargetEdge.pTargetEdge.Node1.ID, FeatureType.PolygonType) as PolygonObject);
                        }

                        if (InvolvedPatterns[i].PatternObjects[InvolvedPatterns[i].PatternObjects.Count - 1].ID == TargetEdge.pTargetEdge.Node2.ID)
                        {
                            InvolvedPatterns[i].PatternObjects.Add(map.GetObjectbyID(TargetEdge.pTargetEdge.Node1.ID, FeatureType.PolygonType) as PolygonObject);
                        }

                        for (int j = 1; j < InvolvedPatterns[i].PatternObjects.Count-1; j++)
                        {
                            if ((InvolvedPatterns[i].PatternObjects[j].ID == TargetEdge.pTargetEdge.Node1.ID) || (InvolvedPatterns[i].PatternObjects[j].ID == TargetEdge.pTargetEdge.Node2.ID))
                            {
                                if (j > 2)
                                {
                                    List<PolygonObject> PolygonList = new List<PolygonObject>();
                                    for (int m = 0; m < j; m++)
                                    {
                                        PolygonList.Add(InvolvedPatterns[i].PatternObjects[m]);
                                    }

                                    Pattern newPattern = new Pattern(InvolvedPatterns.Count + 1, PolygonList);
                                }

                                if ((InvolvedPatterns[i].PatternObjects.Count - j) > 3)
                                {
                                    List<PolygonObject> PolygonList = new List<PolygonObject>();
                                    for (int m = j + 1; m < InvolvedPatterns.Count; m++)
                                    {
                                        PolygonList.Add(InvolvedPatterns[i].PatternObjects[m]);
                                    }

                                    Pattern newPattern = new Pattern(InvolvedPatterns.Count + 1, PolygonList);
                                }

                                PatternList.Remove(InvolvedPatterns[i]);
                            }
                        }
                    }
                }
            }
            #endregion

            #region 重新更新map中的建筑物和TrinodeList
            map.PolygonList.Clear();
            for (int i = 0; i < PatternList.Count; i++)
            {
                for (int j = 0; j < PatternList[i].PatternObjects.Count; j++)
                {
                    map.PolygonList.Add(PatternList[i].PatternObjects[j]);
                }
            }
            map.PolygonList = map.PolygonList.Distinct().ToList();

            for (int i = map.TriNodeList.Count - 1; i >= 0; i--)
            {
                if (map.TriNodeList[i].FeatureType == FeatureType.PolygonType)
                {
                    bool Label = false;


                    for (int j = 0; j < map.PolygonList.Count; j++)
                    {
                        if (map.TriNodeList[i].TagValue == map.PolygonList[j].ID)
                        {
                            Label = true;
                        }
                    }

                    if (!Label)
                    {
                        map.TriNodeList.RemoveAt(i);
                    }
                }
            }

            for (int i = 0; i < map.TriNodeList.Count; i++)
            {
                map.TriNodeList[i].ID = i;
            }
            #endregion

            #region DT+CDT+SKE
            dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            cn = new ConvexNull(dt.TriNodeList);
            cn.CreateConvexNull();

            cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(map.PolylineList, map.PolygonList);

            Triangle.WriteID(dt.TriangleList);
            TriEdge.WriteID(dt.TriEdgeList);

            ske = new AuxStructureLib.Skeleton(cdt, map);
            ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
            ske.TranverseSkeleton_Segment_PLP_NONull();

            pg = new ProxiGraph();
            pg.CreateProxiGraphfrmSkeletonBuildings(map, ske);
            pg.DeleteRepeatedEdge(pg.EdgeList);
            pg.CreatePgForBuildings(pg.NodeList, pg.EdgeList);
            #endregion

            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "邻近图", pMap.SpatialReference, pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList); }
            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
        }
        #endregion

        /// <summary>
        /// 标识建筑物属性
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="PatternList"></param>
        public void LabelBuilding(List<Pattern> PatternList)
        {
            for (int i = 0; i < PatternList.Count; i++)
            {
                for (int j = 0; j < PatternList[i].PatternObjects.Count; j++)
                {
                    if (j == 0 || j == PatternList[i].PatternObjects.Count - 1)
                    {
                        PatternList[i].PatternObjects[j].BoundaryBuilding = true;
                        if (PatternList[i].PatternObjects[j].OrderIDList.Count > 3)
                        {
                            PatternList[i].PatternObjects[j].IntersectBuilding = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 建筑物沿给定的直线排列
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button13_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            map.InterpretatePoint(2);
            #endregion

            #region Pattern读取
            Dictionary<int, Pattern> PatternDic = new Dictionary<int, Pattern>();//找到每个pattern的建筑物
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                for (int j = 0; j < map.PolygonList[i].OrderIDList.Count; j++)
                {
                    if (PatternDic.ContainsKey(map.PolygonList[i].OrderIDList[j][0]))
                    {
                        PatternDic[map.PolygonList[i].OrderIDList[j][0]].PatternObjects.Add(map.PolygonList[i]);
                    }

                    else
                    {
                        List<PolygonObject> PatternObject = new List<PolygonObject>();
                        PatternObject.Add(map.PolygonList[i]);
                        Pattern NewPattern = new Pattern(map.PolygonList[i].OrderIDList[j][0], PatternObject);
                        PatternDic.Add(map.PolygonList[i].OrderIDList[j][0], NewPattern);
                    }
                }
            }

            Dictionary<int, Pattern> SortedPatternDic = new Dictionary<int, Pattern>();
            foreach (var keyValuePair in PatternDic)
            {
                Dictionary<int, PolygonObject> SortPattern = new Dictionary<int, PolygonObject>();
                for (int i = 0; i < keyValuePair.Value.PatternObjects.Count; i++)
                {
                    for (int j = 0; j < keyValuePair.Value.PatternObjects[i].OrderIDList.Count; j++)
                    {
                        if (keyValuePair.Key == keyValuePair.Value.PatternObjects[i].OrderIDList[j][0])
                        {
                            SortPattern.Add(keyValuePair.Value.PatternObjects[i].OrderIDList[j][1], keyValuePair.Value.PatternObjects[i]);
                        }
                    }
                }

                Dictionary<int, PolygonObject> SortedPattern = SortPattern.OrderByDescending(o => o.Key).ToDictionary(p => p.Key, o => o.Value);//按pattern中建筑物的先后降序排序
                List<PolygonObject> SortBuildingPatterns = new List<PolygonObject>(SortedPattern.Values); 
                Pattern sSortPattern = new Pattern(keyValuePair.Value.PatternID, SortBuildingPatterns);
                SortedPatternDic.Add(keyValuePair.Key, sSortPattern);
            }
            #endregion

            foreach (var keyValuePair in SortedPatternDic)
            {
                Pattern NewPattern = new Pattern(keyValuePair.Key, keyValuePair.Value.PatternObjects);
                NewPattern.PatternRelocation(30);
            }

            map.PolygonList.Clear();
            foreach (var keyValuePair in SortedPatternDic)
            {
                Pattern NewPattern = new Pattern(keyValuePair.Key, keyValuePair.Value.PatternObjects);
                for (int i = 0; i < NewPattern.PatternObjects.Count; i++)
                {
                    map.PolygonList.Add(NewPattern.PatternObjects[i]);
                }
            }
            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
        }

        /// <summary>
        /// pattern间冲突渐进式处理（不考虑固定点为端点）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button14_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region Pattern读取
            Dictionary<int, Pattern> PatternDic = new Dictionary<int, Pattern>();//找到每个pattern的建筑物
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                for (int j = 0; j < map.PolygonList[i].OrderIDList.Count; j++)
                {
                    if (PatternDic.ContainsKey(map.PolygonList[i].OrderIDList[j][0]))
                    {
                        PatternDic[map.PolygonList[i].OrderIDList[j][0]].PatternObjects.Add(map.PolygonList[i]);
                    }

                    else
                    {
                        List<PolygonObject> PatternObject = new List<PolygonObject>();
                        PatternObject.Add(map.PolygonList[i]);
                        Pattern NewPattern = new Pattern(map.PolygonList[i].OrderIDList[j][0], PatternObject);
                        PatternDic.Add(map.PolygonList[i].OrderIDList[j][0], NewPattern);
                    }
                }
            }

            Dictionary<int, Pattern> SortedPatternDic = new Dictionary<int, Pattern>();//pattern中建筑物按顺序排列
            foreach (var keyValuePair in PatternDic)
            {
                Dictionary<int, PolygonObject> SortPattern = new Dictionary<int, PolygonObject>();
                for (int i = 0; i < keyValuePair.Value.PatternObjects.Count; i++)
                {
                    for (int j = 0; j < keyValuePair.Value.PatternObjects[i].OrderIDList.Count; j++)
                    {
                        if (keyValuePair.Key == keyValuePair.Value.PatternObjects[i].OrderIDList[j][0])
                        {
                            SortPattern.Add(keyValuePair.Value.PatternObjects[i].OrderIDList[j][1], keyValuePair.Value.PatternObjects[i]);
                        }
                    }
                }

                Dictionary<int, PolygonObject> SortedPattern = SortPattern.OrderByDescending(o => o.Key).ToDictionary(p => p.Key, o => o.Value);//按pattern中建筑物的先后降序排序
                List<PolygonObject> SortBuildingPatterns = new List<PolygonObject>(SortedPattern.Values);
                Pattern sSortPattern = new Pattern(keyValuePair.Value.PatternID, SortBuildingPatterns);
                SortedPatternDic.Add(keyValuePair.Key, sSortPattern);
            }
            #endregion

            #region DT+CDT+SKE
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
            pg.CreatePgForBuildings(pg.NodeList, pg.EdgeList);
            #endregion

            #region 冲突探测
            List<Pattern> PatternList = new List<Pattern>(SortedPatternDic.Values);//pattern的集合
            PrDispalce.工具类.CollabrativeDisplacement.CConflictDetector ccd1 = new CConflictDetector();
            ccd1.ConflictDetectByPg(pg.PgforBuildingEdgesList, 7, PatternList, map, 35000);           
            #endregion

            while (ccd1.ConflictEdge.Count > 0)
            {
                PatternTargetEdge TargetEdge = ccd1.ReturnTragetEdgeTobeSolved(ccd1.ConflictEdge, PatternList);//找到需要处理的pattern

                #region 计算pattern的重要性
                this.LabelBuilding(PatternList);
                for (int i = 0; i < PatternList.Count; i++)
                {
                    int IntersectNum = 0; int BoundaryNum = 0;
                    for (int j = 0; j < PatternList[i].PatternObjects.Count; j++)
                    {
                        if (PatternList[i].PatternObjects[j].IntersectBuilding)
                        {
                            IntersectNum = IntersectNum + 1;
                        }

                        if (PatternList[i].PatternObjects[j].BoundaryBuilding)
                        {
                            BoundaryNum = BoundaryNum + 1;
                        }
                    }

                    PatternList[i].Importance = IntersectNum * 100 + BoundaryNum * 20 + PatternList[i].PatternObjects.Count;
                }
                #endregion

                #region 将pattern按权重重要性排序
                Dictionary<Pattern, double> ImportanceDic = new Dictionary<Pattern, double>();
                for (int i = 0; i < PatternList.Count; i++)
                {
                    ImportanceDic.Add(PatternList[i], PatternList[i].Importance);
                }

                ImportanceDic = ImportanceDic.OrderByDescending(o => o.Value).ToDictionary(p => p.Key, o => o.Value);//权重降序排序
                List<Pattern> ProcessPatterns = new List<Pattern>(ImportanceDic.Keys);//待处理的Pattern
                #endregion

                #region 找到冲突边涉及到的patterns;并找到其中最重要的pattern
                List<Pattern> InvolvedPatterns = new List<Pattern>();
                for (int i = 0; i < PatternList.Count; i++)
                {
                    List<int> IdList = new List<int>();
                    for (int j = 0; j < PatternList[i].PatternObjects.Count; j++)
                    {
                        IdList.Add(PatternList[i].PatternObjects[j].ID);
                    }

                    if (IdList.Contains(TargetEdge.pTargetEdge.Node1.TagID) || IdList.Contains(TargetEdge.pTargetEdge.Node2.TagID))
                    {
                        InvolvedPatterns.Add(PatternList[i]);
                    }
                }

                Pattern SolidPattern = InvolvedPatterns[0];
                for (int i = 1; i < InvolvedPatterns.Count; i++)
                {
                    if (ImportanceDic[SolidPattern] < ImportanceDic[InvolvedPatterns[i]])
                    {
                        SolidPattern = InvolvedPatterns[i];
                    }
                }
                #endregion

                #region 解决冲突
                if (TargetEdge.pTargetEdge != null)//端点建筑物冲突
                {
                    #region 确定那个不变的节点
                    int SolidPolygonID = 0;
                    for (int i = 0; i < SolidPattern.PatternObjects.Count; i++)
                    {
                        if (SolidPattern.PatternObjects[i].ID == TargetEdge.pTargetEdge.Node1.TagID)
                        {
                            SolidPolygonID = TargetEdge.pTargetEdge.Node1.TagID;
                        }

                        else if (SolidPattern.PatternObjects[i].ID == TargetEdge.pTargetEdge.Node2.TagID)
                        {
                            SolidPolygonID = TargetEdge.pTargetEdge.Node2.TagID;
                        }
                    }
                    #endregion

                    for (int i = 0; i < InvolvedPatterns.Count; i++)
                    {
                        bool ContainSolidPolygon = false;
                        for (int j = 0; j < InvolvedPatterns[i].PatternObjects.Count; j++)
                        {
                            if (InvolvedPatterns[i].PatternObjects[j].ID == SolidPolygonID)
                            {
                                ContainSolidPolygon = true;
                            }
                        }

                        if (!ContainSolidPolygon)
                        {

                            if (InvolvedPatterns[i].PatternObjects[0].ID == TargetEdge.pTargetEdge.Node1.ID)
                            {
                                InvolvedPatterns[i].PatternObjects.Insert(0, map.GetObjectbyID(TargetEdge.pTargetEdge.Node2.TagID, FeatureType.PolygonType) as PolygonObject);
                            }

                            if (InvolvedPatterns[i].PatternObjects[InvolvedPatterns[i].PatternObjects.Count - 1].ID == TargetEdge.pTargetEdge.Node1.TagID)
                            {
                                InvolvedPatterns[i].PatternObjects.Add(map.GetObjectbyID(TargetEdge.pTargetEdge.Node2.TagID, FeatureType.PolygonType) as PolygonObject);
                            }

                            if (InvolvedPatterns[i].PatternObjects[0].ID == TargetEdge.pTargetEdge.Node2.TagID)
                            {
                                InvolvedPatterns[i].PatternObjects.Insert(0, map.GetObjectbyID(TargetEdge.pTargetEdge.Node1.TagID, FeatureType.PolygonType) as PolygonObject);
                            }

                            if (InvolvedPatterns[i].PatternObjects[InvolvedPatterns[i].PatternObjects.Count - 1].ID == TargetEdge.pTargetEdge.Node2.TagID)
                            {
                                InvolvedPatterns[i].PatternObjects.Add(map.GetObjectbyID(TargetEdge.pTargetEdge.Node1.TagID, FeatureType.PolygonType) as PolygonObject);
                            }

                            for (int j = 1; j < InvolvedPatterns[i].PatternObjects.Count - 1; j++)
                            {
                                if ((InvolvedPatterns[i].PatternObjects[j].ID == TargetEdge.pTargetEdge.Node1.TagID) || (InvolvedPatterns[i].PatternObjects[j].ID == TargetEdge.pTargetEdge.Node2.TagID))
                                {
                                    if (j > 2)
                                    {
                                        List<PolygonObject> PolygonList = new List<PolygonObject>();
                                        for (int m = 0; m < j; m++)
                                        {
                                            PolygonList.Add(InvolvedPatterns[i].PatternObjects[m]);
                                        }

                                        Pattern newPattern = new Pattern(InvolvedPatterns.Count + 1, PolygonList);
                                    }

                                    if ((InvolvedPatterns[i].PatternObjects.Count - j) > 3)
                                    {
                                        List<PolygonObject> PolygonList = new List<PolygonObject>();
                                        for (int m = j + 1; m < InvolvedPatterns.Count; m++)
                                        {
                                            PolygonList.Add(InvolvedPatterns[i].PatternObjects[m]);
                                        }

                                        Pattern newPattern = new Pattern(InvolvedPatterns.Count + 1, PolygonList);
                                    }

                                    PatternList.Remove(InvolvedPatterns[i]);
                                }
                            }
                        }
                    }
                }
                #endregion

                #region 重新更新map中的建筑物和TrinodeList
                map.PolygonList.Clear();
                for (int i = 0; i < PatternList.Count; i++)
                {
                    for (int j = 0; j < PatternList[i].PatternObjects.Count; j++)
                    {
                        map.PolygonList.Add(PatternList[i].PatternObjects[j]);
                    }
                }
                map.PolygonList = map.PolygonList.Distinct().ToList();

                for (int i = map.TriNodeList.Count - 1; i >= 0; i--)
                {
                    if (map.TriNodeList[i].FeatureType == FeatureType.PolygonType)
                    {
                        bool Label = false;


                        for (int j = 0; j < map.PolygonList.Count; j++)
                        {
                            if (map.TriNodeList[i].TagValue == map.PolygonList[j].ID)
                            {
                                Label = true;
                            }
                        }

                        if (!Label)
                        {
                            map.TriNodeList.RemoveAt(i);
                        }
                    }
                }

                for (int i = 0; i < map.TriNodeList.Count; i++)
                {
                    map.TriNodeList[i].ID = i;
                }
                #endregion

                #region DT+CDT+SKE
                dt = new DelaunayTin(map.TriNodeList);
                dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

                cn = new ConvexNull(dt.TriNodeList);
                cn.CreateConvexNull();

                cdt = new ConsDelaunayTin(dt);
                cdt.CreateConsDTfromPolylineandPolygon(map.PolylineList, map.PolygonList);

                Triangle.WriteID(dt.TriangleList);
                TriEdge.WriteID(dt.TriEdgeList);

                ske = new AuxStructureLib.Skeleton(cdt, map);
                ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
                ske.TranverseSkeleton_Segment_PLP_NONull();

                pg = new ProxiGraph();
                pg.CreateProxiGraphfrmSkeletonBuildings(map, ske);
                pg.DeleteRepeatedEdge(pg.EdgeList);
                pg.CreatePgForBuildings(pg.NodeList, pg.EdgeList);
                #endregion

                //冲突探测
                ccd1.ConflictDetectByPg(pg.PgforBuildingEdgesList, 7, PatternList, map, 35000);
            }

            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "邻近图", pMap.SpatialReference, pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList); }
            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
        }

        #region pattern的典型化
        private void button15_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox2.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            map.InterpretatePoint(2);
            #endregion

            #region DT+CDT+SKE
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
            pg.CreatePgForBuildings(pg.NodeList, pg.EdgeList);

            //VoronoiDiagram vd = null;
            //vd = new AuxStructureLib.VoronoiDiagram(ske, pg, map);
            //vd.CreateVoronoiDiagramfrmSkeletonforBuildings();
            #endregion

            #region 获得相似且不相交的建筑物排列
            //pg.CreateRNGForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            //pg.DeleteRepeatedEdge(pg.RNGBuildingEdgesListShortestDistance);
            //List<List<ProxiEdge>> PatternEdgeList = pg.LinearPatternDetected2(pg.RNGBuildingEdgesListShortestDistance, pg.RNGBuildingNodesListShortestDistance, double.Parse(this.textBox3.Text.ToString()), double.Parse(this.textBox2.Text.ToString()), double.Parse(this.textBox4.Text.ToString()));
            //List<List<ProxiNode>> PatternNodes = pg.LinearPatternRefinement2(PatternEdgeList, map);
            //List<List<ProxiNode>> rPatternNodes = pg.LinearPatternRefinement1(PatternNodes);//对pattern进行裁剪
            //pg.NodesforPattern(rPatternNodes);
            List<List<ProxiNode>> PatternNodes = pg.GetPatterns(map);
            #endregion

            #region Pattern中点的顺序调整
            List<List<ProxiNode>> rPatternNodes = new List<List<ProxiNode>>();
            for (int i = 0; i < PatternNodes.Count; i++)
            {
                List<ProxiNode> CachePatternNodes = new List<ProxiNode>();
                while (PatternNodes[i].Count > 0)
                {
                    for (int j = 0; j < PatternNodes[i].Count; j++)
                    {
                        if (PatternNodes[i][j].SortID == CachePatternNodes.Count + 1)
                        {
                            CachePatternNodes.Add(PatternNodes[i][j]);
                            PatternNodes[i].RemoveAt(j);
                            break;
                        }
                    }
                }

                rPatternNodes.Add(CachePatternNodes);
            }
            #endregion

            #region 将待处理的Node坐标更新为其对应建筑物的重心（似乎在处理完后，个别Node点的坐标与其重心有一定偏差）
            //for (int i = 0; i < rPatternNodes.Count; i++)
            //{
            //    for (int j = 0; j < rPatternNodes[i].Count; j++)
            //    {
            //        for (int m = 0; m < map.PolygonList.Count; m++)
            //        {
            //            if (rPatternNodes[i][j].TagID == map.PolygonList[m].TagID)
            //            {
            //                rPatternNodes[i][j].X = map.PolygonList[m].CalProxiNode().X;
            //                rPatternNodes[i][j].Y = map.PolygonList[m].CalProxiNode().Y;
            //            }
            //        }
            //    }
            //}
            #endregion

            SimilarLinearPatternMesh(rPatternNodes, map, double.Parse(this.textBox5.Text));

            #region 获得pattern对应的所有点
            List<ProxiNode> Nodes = new List<ProxiNode>();
            for (int i = 0; i < rPatternNodes.Count; i++)
            {
                for (int j = 0; j < rPatternNodes[i].Count; j++)
                {
                    Nodes.Add(rPatternNodes[i][j]);
                }
            }
            #endregion

            #region 删除地图中不是pattern的建筑物
            List<PolygonObject> RemovePolygonList = new List<PolygonObject>();
            for (int i = 0; i < pg.PgforBuildingNodesList.Count; i++)
            {
                if (!Nodes.Contains(pg.PgforBuildingNodesList[i]))
                {
                    PolygonObject RemovePolygon = map.GetObjectbyID(pg.PgforBuildingNodesList[i].TagID, FeatureType.PolygonType) as PolygonObject;
                    RemovePolygonList.Add(RemovePolygon);
                }
            }

            for (int i = 0; i < RemovePolygonList.Count; i++)
            {
                map.PolygonList.Remove(RemovePolygonList[i]);
            }
            #endregion

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);

            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "RNG", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.RNGBuildingEdgesListShortestDistance); }
            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "rLinearPattern", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.PgforRefineBuildingPatternList); }
        }
        #endregion
    }
}
