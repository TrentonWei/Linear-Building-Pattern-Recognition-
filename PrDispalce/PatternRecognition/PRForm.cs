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
    public partial class PRForm : Form
    {
        public PRForm(AxMapControl axMapControl)
        {
            InitializeComponent();
            this.pMap = axMapControl.Map;
            this.pMapControl = axMapControl;
        }

        #region 参数
        AxMapControl pMapControl;
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        string OutPath;
        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PRForm_Load(object sender, EventArgs e)
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
                        this.comboBox1.Items.Add(strLayerName);
                    }
                    #endregion

                    #region 添加面图层
                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        this.comboBox2.Items.Add(strLayerName);
                        this.comboBox4.Items.Add(strLayerName);
                        this.comboBox5.Items.Add(strLayerName);
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
            if (this.comboBox4.Items.Count > 0)
            {
                this.comboBox4.SelectedIndex = 0;
            }
            if (this.comboBox5.Items.Count > 0)
            {
                this.comboBox5.SelectedIndex = 0;
            }
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
        /// 邻近图生成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            BendProcess BP = new BendProcess();

            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            IFeatureLayer BuildingLayer=null;
            if (this.comboBox2.Text != null)
            {
                BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            IFeature curFeature = BuildingLayer.FeatureClass.GetFeature(0);

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            //map.InterpretatePoint2();

            SMap map2 = new SMap(list);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                List<TriNode> ConcaveNodes = BP.GetConcaveNode(map.PolygonList[i],5);
                //int TestLocation = 0;
            }

            #region DT+CDT+SKE
            DelaunayTin dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            ConvexNull cn = new ConvexNull(dt.TriNodeList);
            cn.CreateConvexNull();

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);

            Triangle.WriteID(dt.TriangleList);
            TriEdge.WriteID(dt.TriEdgeList);

            AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
            ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
            ske.TranverseSkeleton_Arc();
            //BP.SkeRefine2(ske, map2.PolygonList[0], map.PolygonList[0]);//消除多余的骨架线

            //ProxiGraph pg = new ProxiGraph();
            //pg.CreateProxiGraphfrmSkeletonBuildings(map, ske);
            //pg.DeleteRepeatedEdge(pg.EdgeList);

            //VoronoiDiagram vd = null;
            //vd = new AuxStructureLib.VoronoiDiagram(ske, pg, map);
            //vd.CreateVoronoiDiagramfrmSkeletonforBuildings();
            #endregion

            #region 输出
            //if (OutPath != null) { dt.WriteShp(OutPath, pMap.SpatialReference); }
            if (OutPath != null) { cdt.DT.WriteShp(OutPath, pMap.SpatialReference); }
            //if (OutPath != null) { ske.CDT.DT.WriteShp(OutPath, pMap.SpatialReference); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "邻近图", pMap.SpatialReference, pg.PgforBuildingNodesList,pg.PgforBuildingEdgesList); }
            if (OutPath != null) { ske.Create_WriteSkeleton_Segment2Shp(OutPath, "骨架", pMap.SpatialReference); }
            #endregion
        }

        #region 建筑物图形剖分
        private void button3_Click(object sender, EventArgs e)
        {
            BendProcess BP = new BendProcess();
            PrDispalce.PatternRecognition.TriangleProcess TP = new TriangleProcess();//TriangleProcess
            ConcaveNodeSolve CNS=new ConcaveNodeSolve(pMapControl);
            
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            IFeatureLayer BuildingLayer = null;
            if (this.comboBox2.Text != null)
            {
                BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            IFeature curFeature = BuildingLayer.FeatureClass.GetFeature(0);
            SMap map = new SMap();
            map.ReadDataFrmGivenPolygonObject(curFeature);
            map.InterpretatePoint2(4);

            SMap map2 = new SMap(list);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region DT+CDT+SKE
            DelaunayTin dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

            ConvexNull cn = new ConvexNull(dt.TriNodeList);
            cn.CreateConvexNull();

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);

            Triangle.WriteID(dt.TriangleList);
            TriEdge.WriteID(dt.TriEdgeList);

            AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
            ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
            ske.TranverseSkeleton_Arc();
            #endregion

            List<TriNode> ConcaveNodes = BP.GetConcaveNode(map2.PolygonList,5);//注意，这里需要用未加密的建筑物图形
            TriNode TestNode = this.GetMatchNode(ConcaveNodes[1], map.PolygonList);//获得对应节点
            List<Cut> AllCut = CNS.GetCuts(TestNode, cdt, map.PolygonList);
            CNS.GetCutProperty(ConcaveNodes[1],AllCut,map2.PolygonList,Math.PI/36,3);//注意，这里需要用未加密的建筑物图形

            #region 可视化
            foreach (Cut pCut in AllCut)
            {
                List<TriNode> CacheNodes = new List<TriNode>();               
                CacheNodes.Add(pCut.CutEdge.startPoint);
                CacheNodes.Add(pCut.CutEdge.endPoint);
                PolylineObject pl = new PolylineObject(0, CacheNodes, 1);
                map.PolylineList.Add(pl);
            }

            foreach (TriNode tn in ConcaveNodes)
            {
                PointObject po = new PointObject(0, tn);
                map.PointList.Add(po);
            }
            #endregion

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
        }
        #endregion

        #region 生成建筑物的凸包
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

            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();

            SMap map2 = new SMap();

            #region 凸包计算
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                ConvexNull cn = new ConvexNull(map.PolygonList[i].PointList);
                cn.CreateConvexNull();
                PolygonObject cPolygon = new PolygonObject(i, cn.ConvexVertexSet);
                map2.PolygonList.Add(cPolygon);
            }
            #endregion

            #region 输出
            if (OutPath != null) { map2.WriteResult2Shp(OutPath, pMap.SpatialReference); }
            #endregion
        }
        #endregion

        #region 弯曲深度获取
        private void button5_Click(object sender, EventArgs e)
        {
            BendProcess BP = new BendProcess();
            PrDispalce.PatternRecognition.TriangleProcess TP = new TriangleProcess();//TriangleProcess

            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            IFeatureLayer BuildingLayer = null;
            if (this.comboBox2.Text != null)
            {
                BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            IFeature curFeature = BuildingLayer.FeatureClass.GetFeature(0);
            SMap map = new SMap();
            map.ReadDataFrmGivenPolygonObject(curFeature);
            map.InterpretatePoint2(4);

            SMap map2 = new SMap(list);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region DT+CDT+SKE
            DelaunayTin dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

            ConvexNull cn = new ConvexNull(dt.TriNodeList);
            cn.CreateConvexNull();

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);

            Triangle.WriteID(dt.TriangleList);
            TriEdge.WriteID(dt.TriEdgeList);

            AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
            ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
            ske.TranverseSkeleton_Arc();
            #endregion

            TP.LabelInOutType(dt.TriangleList, map.PolygonList);//标记是内或外三角形
            TP.CommonEdgeTriangleLabel(dt.TriangleList);//标记共边三角形
            TP.LabelBoundaryBuilding(dt.TriangleList);//标记边缘三角形
            List<TriNode> ConvexNodes = BP.GetConcaveNode(map2.PolygonList[0],5);
            TriNode TestNode = this.GetMatchNode(ConvexNodes[1], map.PolygonList[0]);
            List<Skeleton_Arc> BendRoad=BP.GetOutBendRoadForNodes(TestNode,ske,map.PolygonList[0]);//获得外环的深度

            ske.Skeleton_ArcList = BendRoad;
            double RoadLength = BP.GetRoadLength(BendRoad);
            if (OutPath != null) { ske.Create_WriteSkeleton_Segment2Shp(OutPath, "ske", pMap.SpatialReference); } 
        }
        #endregion

        /// <summary>
        /// 获得两个图形的对应点
        /// </summary>
        /// <param name="ConvexNode"></param>
        /// <param name="Po"></param>
        /// <returns></returns>
        public TriNode GetMatchNode(TriNode ConvexNode, PolygonObject Po)
        {
            TriNode MatchedNode = null;
            for (int i = 0; i < Po.PointList.Count; i++)
            {
                if ((Po.PointList[i].X - ConvexNode.X) ==0 & (Po.PointList[i].Y - ConvexNode.Y) == 0)
                {
                    MatchedNode = Po.PointList[i];
                    break;
                }
            }

            return MatchedNode;
        }

        /// <summary>
        /// 获得两个图形的对应点
        /// </summary>
        /// <param name="ConvexNode"></param>
        /// <param name="Po"></param>
        /// <returns></returns>
        public TriNode GetMatchNode(TriNode ConvexNode, List<PolygonObject> PoList)
        {
            TriNode MatchedNode = null;
            foreach (PolygonObject Po in PoList)
            {
                bool Label = false;
                for (int i = 0; i < Po.PointList.Count; i++)
                {
                    if ((Po.PointList[i].X - ConvexNode.X) == 0 & (Po.PointList[i].Y - ConvexNode.Y) == 0)
                    {
                        MatchedNode = Po.PointList[i];
                        Label = true;
                        break;
                    }
                }

                if (Label)
                {
                    break;
                }
            }

            return MatchedNode;
        }

        #region 单个建筑物剖分测试
        private void button6_Click(object sender, EventArgs e)
        {
            BendProcess BP = new BendProcess();
            PrDispalce.PatternRecognition.TriangleProcess TP = new TriangleProcess();//TriangleProcess
            ConcaveNodeSolve CNS = new ConcaveNodeSolve(pMapControl);

            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(StreetLayer);
            }

            IFeatureLayer BuildingLayer = null;
            if (this.comboBox2.Text != null)
            {
                BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            IFeature curFeature = BuildingLayer.FeatureClass.GetFeature(0);
            SMap map = new SMap();
            map.ReadDataFrmGivenPolygonObject(curFeature);
            map.InterpretatePoint2(4);

            SMap map2 = new SMap(list);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region DT+CDT+SKE
            DelaunayTin dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

            ConvexNull cn = new ConvexNull(dt.TriNodeList);
            cn.CreateConvexNull();

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);

            Triangle.WriteID(dt.TriangleList);
            TriEdge.WriteID(dt.TriEdgeList);

            AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
            ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
            ske.TranverseSkeleton_Arc();
            #endregion

            #region 三角网与ske属性标注
            TP.LabelInOutType(dt.TriangleList, map.PolygonList);//标记是内或外三角形
            TP.CommonEdgeTriangleLabel(dt.TriangleList);//标记共边三角形
            TP.LabelBoundaryBuilding(dt.TriangleList);//标记边缘三角形
            BP.GetOutSkeArcLevel(ske, map.PolygonList[0]);//获得Ske中每一个Arc的层次
            #endregion

            List<TriNode> ConvexNodes = BP.GetConcaveNode(map2.PolygonList,5);

            #region 获得每一个节点对应的弯曲深度,并返回弯曲深度最深的节点
            Dictionary<TriNode, double> BendLength = new Dictionary<TriNode, double>();
            for (int i = 0; i < ConvexNodes.Count; i++)
            {
                TriNode TestNode = this.GetMatchNode(ConvexNodes[i], map.PolygonList);
                List<Skeleton_Arc> BendRoad = BP.GetOutBendRoadForNodes(TestNode, ske, map.PolygonList[0]);//获得外环的深度
                double RoadLength = BP.GetRoadLength(BendRoad);
                BendLength.Add(TestNode, RoadLength);
            }
            TriNode TargetNode = BP.GetDeepestNode(BendLength);
            #endregion

            List<Cut> AllCut = CNS.GetCuts(TargetNode, cdt, map.PolygonList);
            CNS.GetCutProperty(TargetNode, AllCut, map2.PolygonList,Math.PI/36,3);//注意，这里需要用未加密的建筑物图形
            List<Cut> RefinedCut = CNS.CutsRefine(AllCut,3);
            Cut TargetCut = CNS.GetBestCut(RefinedCut, true,false);
            List<Polygon> CutPolygons = CNS.GetPolygonAfterCut(BuildingLayer.FeatureClass.GetFeature(0).Shape as Polygon, TargetCut);

            SMap map3 = new SMap();
            map3.ReadDataFrmGivenPolygonObject(CutPolygons);
            map3.WriteResult2Shp(OutPath, pMap.SpatialReference);
        }
        #endregion

        #region 无洞建筑物图形渐进式剖分(不考虑直角和节点)
        private void button7_Click(object sender, EventArgs e)
        {
            #region 处理类
            BendProcess BP = new BendProcess();//弯曲处理类
            PrDispalce.PatternRecognition.TriangleProcess TP = new TriangleProcess();//TriangleProcess
            ConcaveNodeSolve CNS = new ConcaveNodeSolve(pMapControl);//凹点处理类
            List<Polygon> FinalCuttedPolygons = new List<Polygon>();//最终分割获取的建筑物
            #endregion

            #region 获取待分割的建筑物
            List<Polygon> CutPolygons = new List<Polygon>();
            IFeatureLayer BuildingLayer = null;
            if (this.comboBox2.Text != null)
            {
                BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
            }

            for (int i = 0; i < BuildingLayer.FeatureClass.FeatureCount(null); i++)
            {
                IFeature pFeature = BuildingLayer.FeatureClass.GetFeature(i);
                Polygon pPolygon = pFeature.Shape as Polygon;
                CutPolygons.Add(pPolygon);
            }
            #endregion

            #region 对建筑物图形进行分割
            for (int i = 0; i < CutPolygons.Count; i++)
            {
                Console.Write(i.ToString());

                #region 将当前分割建筑物加入待分割建筑物列表
                List<Polygon> CacheCutPolygons = new List<Polygon>();
                List<TriNode> CacheConcaveNodes = BP.GetConcaveNode(CutPolygons[i],5);
                if (CacheConcaveNodes.Count > 0)
                {
                    CacheCutPolygons.Add(CutPolygons[i]);
                }

                else
                {
                    FinalCuttedPolygons.Add(CutPolygons[i]);
                }
                #endregion

                while (CacheCutPolygons.Count > 0)
                {
                    #region 建筑物读取
                    SMap map = new SMap();
                    map.ReadDataFrmGivenPolygonObject(CacheCutPolygons[0]);
                    map.InterpretatePoint2(10);

                    SMap map2 = new SMap();
                    map2.ReadDataFrmGivenPolygonObject(CacheCutPolygons[0]);
                    #endregion

                    #region DT+CDT+SKE
                    DelaunayTin dt = new DelaunayTin(map.TriNodeList);
                    dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

                    ConvexNull cn = new ConvexNull(dt.TriNodeList);
                    cn.CreateConvexNull();

                    ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
                    cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);

                    Triangle.WriteID(dt.TriangleList);
                    TriEdge.WriteID(dt.TriEdgeList);

                    AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
                    ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
                    ske.TranverseSkeleton_Arc();
                    #endregion

                    #region 三角网与ske属性标注
                    TP.LabelInOutType(dt.TriangleList, map.PolygonList);//标记是内或外三角形
                    TP.CommonEdgeTriangleLabel(dt.TriangleList);//标记共边三角形
                    TP.LabelBoundaryBuilding(dt.TriangleList);//标记边缘三角形
                    BP.GetOutSkeArcLevel(ske, map.PolygonList[0]);//获得Ske中每一个Arc的层次
                    #endregion

                    List<TriNode> ConvexNodes = BP.GetConcaveNode(map2.PolygonList,5);//返回待分割的建筑物中的凹点

                    #region 获得每一个节点对应的弯曲深度,并返回弯曲深度最深的节点
                    Dictionary<TriNode, double> BendLength = new Dictionary<TriNode, double>();
                    for (int j = 0; j < ConvexNodes.Count; j++)
                    {
                        TriNode TestNode = this.GetMatchNode(ConvexNodes[j], map.PolygonList);
                        List<Skeleton_Arc> BendRoad = BP.GetOutBendRoadForNodes(TestNode, ske, map.PolygonList[0]);//获得外环的深度
                        double RoadLength = BP.GetRoadLength(BendRoad);
                        BendLength.Add(TestNode, RoadLength);
                    }
                    TriNode TargetNode = BP.GetDeepestNode(BendLength);
                    #endregion

                    #region 获得最优分割，并对图形进行分割
                    List<Cut> AllCut = CNS.GetCuts(TargetNode, cdt, map.PolygonList);
                    CNS.GetCutProperty(TargetNode, AllCut, map2.PolygonList, 5, 3);//注意，这里需要用未加密的建筑物图形
                    List<Cut> RefinedCut = CNS.CutsRefine(AllCut,3);
                    Cut TargetCut = CNS.GetBestCut(RefinedCut,true,false);
                    //这里裁剪的有问题！！
                    List<Polygon> CuttedPolygons = CNS.GetPolygonAfterCut(CacheCutPolygons[0], TargetCut);
                    #endregion

                    #region 更新CutPolygons
                    CacheCutPolygons.RemoveAt(0);
                    for (int j = 0; j < CuttedPolygons.Count; j++)
                    {
                        List<TriNode> CacheCuttedConcaveNodes = BP.GetConcaveNode(CuttedPolygons[j],5);

                        #region 可视化显示
                        for (int s = 0; s < CacheConcaveNodes.Count; s++)
                        {
                            IPoint CacheStartPoint = new PointClass();
                            CacheStartPoint.X = CacheConcaveNodes[s].X; CacheStartPoint.Y = CacheConcaveNodes[s].Y;
                            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
                            object PointSb = Sb.PointSymbolization(100, 100, 100);
                            pMapControl.DrawShape(CacheStartPoint, ref PointSb);
                        }
                        #endregion

                        if (CacheCuttedConcaveNodes.Count > 0)
                        {
                            CacheCutPolygons.Add(CuttedPolygons[j]);
                        }

                        else
                        {
                            FinalCuttedPolygons.Add(CuttedPolygons[j]);
                        }
                    }
                    #endregion
                }
            }
            #endregion

            #region 输出
            SMap map3 = new SMap();
            map3.ReadDataFrmGivenPolygonObject(FinalCuttedPolygons);
            map3.WriteResult2Shp(OutPath, pMap.SpatialReference);
            #endregion
        }
        #endregion

        #region 无洞建筑物图形的剖分（考虑直角）
        private void button8_Click(object sender, EventArgs e)
        {
            #region 处理类
            BendProcess BP = new BendProcess();//弯曲处理类
            PrDispalce.PatternRecognition.TriangleProcess TP = new TriangleProcess();//TriangleProcess
            ConcaveNodeSolve CNS = new ConcaveNodeSolve(pMapControl);//凹点处理类
            List<Polygon> FinalCuttedPolygons = new List<Polygon>();//最终分割获取的建筑物
            #endregion

            #region 获取待分割的建筑物
            List<Polygon> CutPolygons = new List<Polygon>();
            IFeatureLayer BuildingLayer = null;
            if (this.comboBox2.Text != null)
            {
                BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
            }

            for (int i = 0; i < BuildingLayer.FeatureClass.FeatureCount(null); i++)
            {
                IFeature pFeature = BuildingLayer.FeatureClass.GetFeature(i);
                Polygon pPolygon = pFeature.Shape as Polygon;
                CutPolygons.Add(pPolygon);
            }
            #endregion

            #region 对建筑物图形进行分割
            for (int i = 0; i < CutPolygons.Count; i++)
            {
                Console.Write(i.ToString());

                #region 将当前分割建筑物加入待分割建筑物列表
                List<Polygon> CacheCutPolygons = new List<Polygon>();
                List<TriNode> CacheConcaveNodes = BP.GetConcaveNode(CutPolygons[i],5);
                if (CacheConcaveNodes.Count > 0)
                {
                    CacheCutPolygons.Add(CutPolygons[i]);
                }

                else
                {
                    FinalCuttedPolygons.Add(CutPolygons[i]);
                }
                #endregion

                int Label = 0;//测试Label
                while (CacheCutPolygons.Count > 0)
                {
                    Label++;
                    
                    #region 建筑物读取
                    SMap map = new SMap();
                    map.ReadDataFrmGivenPolygonObject(CacheCutPolygons[0]);
                    map.InterpretatePoint2(10);

                    SMap map2 = new SMap();
                    map2.ReadDataFrmGivenPolygonObject(CacheCutPolygons[0]);
                    #endregion

                    #region DT+CDT+SKE
                    DelaunayTin dt = new DelaunayTin(map.TriNodeList);
                    dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

                    ConvexNull cn = new ConvexNull(dt.TriNodeList);
                    cn.CreateConvexNull();

                    ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
                    cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);

                    Triangle.WriteID(dt.TriangleList);
                    TriEdge.WriteID(dt.TriEdgeList);

                    AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
                    ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
                    ske.TranverseSkeleton_Arc();
                    BP.SkeRefine2(ske, map2.PolygonList[0], map.PolygonList[0]);
                    #endregion

                    #region 三角网与ske属性标注
                    TP.LabelInOutType(dt.TriangleList, map.PolygonList);//标记是内或外三角形
                    TP.CommonEdgeTriangleLabel(dt.TriangleList);//标记共边三角形
                    TP.LabelBoundaryBuilding(dt.TriangleList);//标记边缘三角形
                    BP.GetOutSkeArcLevel(ske, map.PolygonList[0]);//获得Ske中每一个Arc的层次
                    #endregion

                    List<TriNode> ConvexNodes = BP.GetConcaveNode(map2.PolygonList,5);//返回待分割的建筑物中的凹点

                    #region 获得每一个节点对应的弯曲深度,并返回弯曲深度最深的节点
                    Dictionary<TriNode, double> BendLength = new Dictionary<TriNode, double>();
                    for (int j = 0; j < ConvexNodes.Count; j++)
                    {
                        TriNode TestNode = this.GetMatchNode(ConvexNodes[j], map.PolygonList);
                        List<Skeleton_Arc> BendRoad = BP.GetOutBendRoadForNodes(TestNode, ske, map.PolygonList[0]);//获得外环的深度
                        double RoadLength = BP.GetRoadLength(BendRoad);
                        BendLength.Add(TestNode, RoadLength);
                    }
                    TriNode TargetNode = BP.GetDeepestNode(BendLength);
                    #endregion

                    #region 获得最优分割，并对图形进行分割
                    List<Cut> AllCut = CNS.GetCuts(TargetNode, cdt, map.PolygonList);
                    CNS.GetCutProperty(TargetNode, AllCut, map2.PolygonList, 5, 3);//注意，这里需要用未加密的建筑物图形
                    List<Cut> RefinedCut = CNS.CutsRefine(AllCut,5);
                    Cut TargetCut = CNS.GetBestCut2(RefinedCut, true, true, 0.3, 0.1);
                    //Cut TargetCut = CNS.GetBestCutConsiderOrth(RefinedCut, true, 3);
                    //这里裁剪的有问题！！
                    List<Polygon> CuttedPolygons = CNS.GetPolygonAfterCut(CacheCutPolygons[0], TargetCut);
                    #endregion

                    #region 更新CutPolygons
                    CacheCutPolygons.RemoveAt(0);
                    for (int j = 0; j < CuttedPolygons.Count; j++)
                    {
                        List<TriNode> CacheCuttedConcaveNodes = BP.GetConcaveNode(CuttedPolygons[j],5);

                        #region 可视化显示
                        for (int s = 0; s < CacheConcaveNodes.Count; s++)
                        {
                            IPoint CacheStartPoint = new PointClass();
                            CacheStartPoint.X = CacheConcaveNodes[s].X; CacheStartPoint.Y = CacheConcaveNodes[s].Y;
                            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
                            object PointSb = Sb.PointSymbolization(100, 100, 100);
                            pMapControl.DrawShape(CacheStartPoint, ref PointSb);
                        }
                        #endregion

                        if (CacheCuttedConcaveNodes.Count > 0)
                        {
                            CacheCutPolygons.Add(CuttedPolygons[j]);
                        }

                        else
                        {
                            FinalCuttedPolygons.Add(CuttedPolygons[j]);
                        }
                    }
                    #endregion

                    //#region 输出测试
                    //SMap map3 = new SMap();
                    //map3.ReadDataFrmGivenPolygonObject(CuttedPolygons);
                    //map3.Create_WritePolygonObject2Shp(OutPath,Label.ToString(),pMap.SpatialReference);
                    //#endregion
                }
            }
            #endregion

            #region 输出
            SMap map3 = new SMap();
            map3.ReadDataFrmGivenPolygonObject(FinalCuttedPolygons);
            map3.WriteResult2Shp(OutPath, pMap.SpatialReference);
            #endregion
        }
        #endregion

        #region 考虑变换凹度的结果
        private void button9_Click(object sender, EventArgs e)
        {
            #region 处理类
            BendProcess BP = new BendProcess();//弯曲处理类
            PrDispalce.PatternRecognition.TriangleProcess TP = new TriangleProcess();//TriangleProcess
            ConcaveNodeSolve CNS = new ConcaveNodeSolve(pMapControl);//凹点处理类
            List<Polygon> FinalCuttedPolygons = new List<Polygon>();//最终分割获取的建筑物
            #endregion

            #region 获取待分割的建筑物
            List<Polygon> CutPolygons = new List<Polygon>();
            IFeatureLayer BuildingLayer = null;
            if (this.comboBox2.Text != null)
            {
                BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
            }

            for (int i = 0; i < BuildingLayer.FeatureClass.FeatureCount(null); i++)
            {
                IFeature pFeature = BuildingLayer.FeatureClass.GetFeature(i);
                Polygon pPolygon = pFeature.Shape as Polygon;
                CutPolygons.Add(pPolygon);
            }
            #endregion

            #region 对建筑物图形进行分割
            for (int i = 0; i < CutPolygons.Count; i++)
            {
                Console.Write(i.ToString());

                #region 将当前分割建筑物加入待分割建筑物列表
                List<Polygon> CacheCutPolygons = new List<Polygon>();
                List<TriNode> CacheConcaveNodes = BP.GetConcaveNode(CutPolygons[i], 5);
                if (CacheConcaveNodes.Count > 0)
                {
                    CacheCutPolygons.Add(CutPolygons[i]);
                }

                else
                {
                    FinalCuttedPolygons.Add(CutPolygons[i]);
                }
                #endregion

                while (CacheCutPolygons.Count > 0)
                {
                    #region 建筑物读取
                    SMap map = new SMap();
                    map.ReadDataFrmGivenPolygonObject(CacheCutPolygons[0]);
                    map.InterpretatePoint2(10);

                    SMap map2 = new SMap();
                    map2.ReadDataFrmGivenPolygonObject(CacheCutPolygons[0]);
                    #endregion

                    #region DT+CDT+SKE
                    DelaunayTin dt = new DelaunayTin(map.TriNodeList);
                    dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

                    ConvexNull cn = new ConvexNull(dt.TriNodeList);
                    cn.CreateConvexNull();

                    ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
                    cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);

                    Triangle.WriteID(dt.TriangleList);
                    TriEdge.WriteID(dt.TriEdgeList);

                    AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
                    ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
                    ske.TranverseSkeleton_Arc();
                    #endregion

                    #region 三角网与ske属性标注
                    TP.LabelInOutType(dt.TriangleList, map.PolygonList);//标记是内或外三角形
                    TP.CommonEdgeTriangleLabel(dt.TriangleList);//标记共边三角形
                    TP.LabelBoundaryBuilding(dt.TriangleList);//标记边缘三角形
                    BP.GetOutSkeArcLevel(ske, map.PolygonList[0]);//获得Ske中每一个Arc的层次
                    #endregion

                    List<TriNode> ConvexNodes = BP.GetConcaveNode(map2.PolygonList, 5);//返回待分割的建筑物中的凹点

                    #region 获得每一个节点对应的弯曲深度,并返回弯曲深度最深的节点
                    Dictionary<TriNode, double> BendLength = new Dictionary<TriNode, double>();
                    for (int j = 0; j < ConvexNodes.Count; j++)
                    {
                        TriNode TestNode = this.GetMatchNode(ConvexNodes[j], map.PolygonList);
                        double RoadLength = BP.VisualDis(map2.PolygonList[0], TestNode);
                        BendLength.Add(TestNode, RoadLength);
                    }
                    TriNode TargetNode = BP.GetDeepestNode(BendLength);
                    #endregion

                    #region 获得最优分割，并对图形进行分割
                    List<Cut> AllCut = CNS.GetCuts(TargetNode, cdt, map.PolygonList);
                    CNS.GetCutProperty(TargetNode, AllCut, map2.PolygonList, 5, 3);//注意，这里需要用未加密的建筑物图形
                    List<Cut> RefinedCut = CNS.CutsRefine(AllCut, 5);
                    Cut TargetCut = CNS.GetBestCut2(RefinedCut, true, true, 0.1, 0.1);
                    //Cut TargetCut = CNS.GetBestCutConsiderOrth(RefinedCut, true, 3);
                    //这里裁剪的有问题！！
                    List<Polygon> CuttedPolygons = CNS.GetPolygonAfterCut(CacheCutPolygons[0], TargetCut);
                    #endregion

                    #region 更新CutPolygons
                    CacheCutPolygons.RemoveAt(0);
                    for (int j = 0; j < CuttedPolygons.Count; j++)
                    {
                        List<TriNode> CacheCuttedConcaveNodes = BP.GetConcaveNode(CuttedPolygons[j], 5);

                        #region 可视化显示
                        for (int s = 0; s < CacheConcaveNodes.Count; s++)
                        {
                            IPoint CacheStartPoint = new PointClass();
                            CacheStartPoint.X = CacheConcaveNodes[s].X; CacheStartPoint.Y = CacheConcaveNodes[s].Y;
                            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
                            object PointSb = Sb.PointSymbolization(100, 100, 100);
                            pMapControl.DrawShape(CacheStartPoint, ref PointSb);
                        }
                        #endregion

                        if (CacheCuttedConcaveNodes.Count > 0)
                        {
                            CacheCutPolygons.Add(CuttedPolygons[j]);
                        }

                        else
                        {
                            FinalCuttedPolygons.Add(CuttedPolygons[j]);
                        }
                    }
                    #endregion
                }
            }
            #endregion

            #region 输出
            SMap map3 = new SMap();
            map3.ReadDataFrmGivenPolygonObject(FinalCuttedPolygons);
            map3.WriteResult2Shp(OutPath, pMap.SpatialReference);
            #endregion
        }
        #endregion

        #region 计算Hamnning距离
        private void button10_Click(object sender, EventArgs e)
        {
            #region 获取两个建筑物图层
            IFeatureLayer TargetBuildingLayer = null;//获得视觉剖分图层
            if (this.comboBox2.Text != null)
            {
                TargetBuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
            }
            IFeatureLayer sBuildingLayer = null;//获得视觉剖分图层
            if (this.comboBox4.Text != null)
            {
                sBuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
            }
            IFeatureLayer aBuildingLayer = null;//获得实际剖分图层
            if (this.comboBox5.Text != null)
            {
                aBuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox5.Text);
            }
            #endregion

            #region 读取对应的建筑物
            IFeatureClass sFeatureClass = sBuildingLayer.FeatureClass;
            IFeatureClass aFeatureClass = aBuildingLayer.FeatureClass;
            Dictionary<int, List<IPolygon>> sDic = new Dictionary<int, List<IPolygon>>();
            Dictionary<int, List<IPolygon>> aDic = new Dictionary<int, List<IPolygon>>();

            #region 获得对应Target的编号
            IFields sFields = sFeatureClass.Fields; int snum;
            snum = sFields.FieldCount; int sID = -1;
            for (int m = 0; m < snum; m++)
            {
                if (sFields.get_Field(m).Name == "Target")
                {
                    sID = m;
                }
            }
            #endregion

            #region 获取对应Target建筑物
            IFeatureCursor sFeatureCursor = sFeatureClass.Update(null, false);
            IFeature sFeature = sFeatureCursor.NextFeature();       
            while (sFeature != null)
            {
                int KID = Convert.ToInt32(sFeature.get_Value(sID));

                if (sDic.ContainsKey(KID))
                {
                    List<IPolygon> PoList = sDic[KID];
                    IPolygon pPolygon = sFeature.Shape as IPolygon;
                    PoList.Add(pPolygon);

                    sDic.Remove(KID);
                    sDic.Add(KID, PoList);
                }
                else
                {
                    List<IPolygon> PoList = new List<IPolygon>();
                    IPolygon pPolygon = sFeature.Shape as IPolygon;
                    PoList.Add(pPolygon);

                    sDic.Add(KID, PoList);
                }

                sFeature = sFeatureCursor.NextFeature(); 
            }
            #endregion

            #region 获得对应Target编号
            IFields aFields = aFeatureClass.Fields; int anum;
            anum = aFields.FieldCount; int aID = -1;
            for (int m = 0; m < anum; m++)
            {
                if (aFields.get_Field(m).Name == "Target")
                {
                    aID = m;
                }
            }
            #endregion

            #region 获取对应Target建筑物
            IFeatureCursor aFeatureCursor = aFeatureClass.Update(null, false);
            IFeature aFeature = aFeatureCursor.NextFeature();
            while (aFeature != null)
            {
                int KID = Convert.ToInt32(aFeature.get_Value(aID));
                if (aDic.ContainsKey(KID))
                {
                    List<IPolygon> PoList = aDic[KID];
                    IPolygon pPolygon = aFeature.Shape as IPolygon;
                    PoList.Add(pPolygon);

                    aDic.Remove(KID);
                    aDic.Add(KID, PoList);
                }
                else
                {
                    List<IPolygon> PoList = new List<IPolygon>();
                    IPolygon pPolygon = aFeature.Shape as IPolygon;
                    PoList.Add(pPolygon);

                    aDic.Add(KID, PoList);
                }

                aFeature = aFeatureCursor.NextFeature();
            }
            #endregion
            #endregion

            System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\10988\Desktop\建筑物模式识别（bag of words；mofit；剖分）\建筑物图形剖分\aoduID.txt", System.IO.FileMode.OpenOrCreate);
            System.IO.FileStream fs1 = new System.IO.FileStream(@"C:\Users\10988\Desktop\建筑物模式识别（bag of words；mofit；剖分）\建筑物图形剖分\aodu.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);
            StreamWriter sw1 = new StreamWriter(fs1);

            #region 计算对应的a->b和b->a的面积
            List<int> sKeys = sDic.Keys.ToList();
            for (int i = 0; i < sKeys.Count; i++)
            {
                List<IPolygon> sCachePolygons = sDic[sKeys[i]];
                double AreaSum = 0;
                for (int m = 0; m < sCachePolygons.Count; m++)
                {
                    IArea pArea = sCachePolygons[m] as IArea;
                    AreaSum = AreaSum + pArea.Area;
                }

                if (aDic.ContainsKey(sKeys[i]))
                {
                    List<IPolygon> aCachePolygons = aDic[sKeys[i]];

                    #region a->b
                    double abArea = 0;
                    for (int m = 0; m < sCachePolygons.Count; m++)
                    {
                        double SINGLEArea = 0;
                        ITopologicalOperator iTo = sCachePolygons[m] as ITopologicalOperator;
                        for (int n = 0; n < aCachePolygons.Count; n++)
                        {
                            IGeometry iGeo = iTo.Intersect(aCachePolygons[n] as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                            if (iGeo != null)
                            {
                                IArea pArea = iGeo as IArea;
                                double CacheArea = pArea.Area;
                                if (CacheArea > SINGLEArea)
                                {
                                    SINGLEArea = CacheArea;
                                }
                            }
                        }

                        abArea = abArea + SINGLEArea;
                    }
                    #endregion

                    #region b->a
                    double baArea = 0;
                    for (int m = 0; m < aCachePolygons.Count; m++)
                    {
                        double SINGLEArea = 0;
                        ITopologicalOperator iTo = aCachePolygons[m] as ITopologicalOperator;
                        for (int n = 0; n < sCachePolygons.Count; n++)
                        {
                            IGeometry iGeo = iTo.Intersect(sCachePolygons[n] as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                            if (iGeo != null)
                            {
                                IArea pArea = iGeo as IArea;
                                double CacheArea = pArea.Area;
                                if (CacheArea > SINGLEArea)
                                {
                                    SINGLEArea = CacheArea;
                                }
                            }
                        }

                        baArea = baArea + SINGLEArea;
                    }
                    #endregion

                    double HanDis = (abArea + baArea) / (2 * AreaSum);

                    sw.Write(sKeys[i].ToString()); sw.Write("\r\n");
                    sw1.Write(HanDis.ToString()); sw1.Write("\r\n");
                }
            }
            #endregion

            sw.Close();
            fs.Close();
            sw1.Close();
            fs1.Close();
        }
        #endregion

        #region 无洞建筑物图形渐进式凸分解
        private void button11_Click(object sender, EventArgs e)
        {
            #region 处理类
            BendProcess BP = new BendProcess();//弯曲处理类
            PrDispalce.PatternRecognition.TriangleProcess TP = new TriangleProcess();//TriangleProcess
            ConcaveNodeSolve CNS = new ConcaveNodeSolve(pMapControl);//凹点处理类
            List<Polygon> FinalCuttedPolygons = new List<Polygon>();//最终分割获取的建筑物
            #endregion

            #region 获取待分割的建筑物
            List<Polygon> CutPolygons = new List<Polygon>();
            IFeatureLayer BuildingLayer = null;
            if (this.comboBox2.Text != null)
            {
                BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
            }

            for (int i = 0; i < BuildingLayer.FeatureClass.FeatureCount(null); i++)
            {
                IFeature pFeature = BuildingLayer.FeatureClass.GetFeature(i);
                Polygon pPolygon = pFeature.Shape as Polygon;
                CutPolygons.Add(pPolygon);
            }
            #endregion

            #region 对建筑物图形进行分割
            for (int i = 0; i < CutPolygons.Count; i++)
            {
                Console.Write(i.ToString());

                #region 将当前分割建筑物加入待分割建筑物列表
                List<Polygon> CacheCutPolygons = new List<Polygon>();
                List<TriNode> CacheConcaveNodes = BP.GetConcaveNode(CutPolygons[i], 5);
                if (CacheConcaveNodes.Count > 0)
                {
                    CacheCutPolygons.Add(CutPolygons[i]);
                }

                else
                {
                    FinalCuttedPolygons.Add(CutPolygons[i]);
                }
                #endregion

                while (CacheCutPolygons.Count > 0)
                {
                    #region 建筑物读取
                    SMap map = new SMap();
                    map.ReadDataFrmGivenPolygonObject(CacheCutPolygons[0]);
                    map.InterpretatePoint2(10);

                    SMap map2 = new SMap();
                    map2.ReadDataFrmGivenPolygonObject(CacheCutPolygons[0]);
                    #endregion

                    #region DT+CDT+SKE
                    DelaunayTin dt = new DelaunayTin(map.TriNodeList);
                    dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

                    ConvexNull cn = new ConvexNull(dt.TriNodeList);
                    cn.CreateConvexNull();

                    ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
                    cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);

                    Triangle.WriteID(dt.TriangleList);
                    TriEdge.WriteID(dt.TriEdgeList);

                    AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
                    ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
                    ske.TranverseSkeleton_Arc();
                    #endregion

                    #region 三角网与ske属性标注
                    TP.LabelInOutType(dt.TriangleList, map.PolygonList);//标记是内或外三角形
                    TP.CommonEdgeTriangleLabel(dt.TriangleList);//标记共边三角形
                    TP.LabelBoundaryBuilding(dt.TriangleList);//标记边缘三角形
                    BP.GetOutSkeArcLevel(ske, map.PolygonList[0]);//获得Ske中每一个Arc的层次
                    #endregion

                    List<TriNode> ConvexNodes = BP.GetConcaveNode(map2.PolygonList, 5);//返回待分割的建筑物中的凹点

                    #region 获得每一个节点对应的弯曲深度,并返回弯曲深度最深的节点
                    Dictionary<TriNode, double> BendLength = new Dictionary<TriNode, double>();
                    for (int j = 0; j < ConvexNodes.Count; j++)
                    {
                        TriNode TestNode = this.GetMatchNode(ConvexNodes[j], map.PolygonList);
                        List<Skeleton_Arc> BendRoad = BP.GetOutBendRoadForNodes(TestNode, ske, map.PolygonList[0]);//获得外环的深度
                        double RoadLength = BP.GetRoadLength(BendRoad);
                        BendLength.Add(TestNode, RoadLength);
                    }
                    TriNode TargetNode = BP.GetDeepestNode(BendLength);
                    double DeepLength = BP.GetDeepestLength(BendLength);
                    IArea pArea = CacheCutPolygons[0] as IArea;
                    IPolygon pPolygon = CacheCutPolygons[0] as IPolygon;
                    double ReletiveConcave = DeepLength / pPolygon.Length;
                    double RelativeConcave = DeepLength / (2 * Math.Sqrt(Math.PI * pArea.Area));
                    #endregion

                    List<Polygon> CuttedPolygons = new List<Polygon>();
                    if (RelativeConcave > 0.2)
                    {
                        #region 获得最优分割，并对图形进行分割
                        List<Cut> AllCut = CNS.GetCuts(TargetNode, cdt, map.PolygonList);
                        CNS.GetCutProperty(TargetNode, AllCut, map2.PolygonList, 5, 3);//注意，这里需要用未加密的建筑物图形
                        List<Cut> RefinedCut = CNS.CutsRefine(AllCut, 5);
                        Cut TargetCut = CNS.GetBestCut2(RefinedCut, true, true, 0.3, 0.1);
                        //Cut TargetCut = CNS.GetBestCutConsiderOrth(RefinedCut, true, 3);
                        //这里裁剪的有问题！！
                        CuttedPolygons = CNS.GetPolygonAfterCut(CacheCutPolygons[0], TargetCut);
                        #endregion
                    }
                    #region 更新CutPolygons
                  
                    if (CuttedPolygons.Count > 0)
                    {
                        for (int j = 0; j < CuttedPolygons.Count; j++)
                        {
                            List<TriNode> CacheCuttedConcaveNodes = BP.GetConcaveNode(CuttedPolygons[j], 5);

                            #region 可视化显示
                            for (int s = 0; s < CacheConcaveNodes.Count; s++)
                            {
                                IPoint CacheStartPoint = new PointClass();
                                CacheStartPoint.X = CacheConcaveNodes[s].X; CacheStartPoint.Y = CacheConcaveNodes[s].Y;
                                PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
                                object PointSb = Sb.PointSymbolization(100, 100, 100);
                                pMapControl.DrawShape(CacheStartPoint, ref PointSb);
                            }
                            #endregion

                            if (CacheCuttedConcaveNodes.Count > 0)
                            {
                                CacheCutPolygons.Add(CuttedPolygons[j]);
                            }

                            else
                            {
                                FinalCuttedPolygons.Add(CuttedPolygons[j]);
                            }
                        }
                    }

                    else
                    {
                        FinalCuttedPolygons.Add(CacheCutPolygons[0]);
                    }

                    CacheCutPolygons.RemoveAt(0);
                    #endregion
                }
            }
            #endregion

            #region 输出
            SMap map3 = new SMap();
            map3.ReadDataFrmGivenPolygonObject(FinalCuttedPolygons);
            map3.WriteResult2Shp(OutPath, pMap.SpatialReference);
            #endregion
        }
        #endregion

        #region 计算面积、方向和位置差异
        private void button12_Click(object sender, EventArgs e)
        {
            #region 获取两个建筑物图层
            IFeatureLayer sBuildingLayer = null;//获得基准图层
            if (this.comboBox4.Text != null)
            {
                sBuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
            }
            IFeatureLayer aBuildingLayer = null;//获得简化图层
            if (this.comboBox5.Text != null)
            {
                aBuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox5.Text);
            }
            #endregion

            #region 读取对应的建筑物
            IFeatureClass sFeatureClass = sBuildingLayer.FeatureClass;
            IFeatureClass aFeatureClass = aBuildingLayer.FeatureClass;
            Dictionary<int, IPolygon> sDic = new Dictionary<int, IPolygon>();//基准
            Dictionary<int, IPolygon> aDic = new Dictionary<int,IPolygon>();

            #region 获取对应Target建筑物（基准）
            IFeatureCursor sFeatureCursor = sFeatureClass.Update(null, false);
            IFeature sFeature = sFeatureCursor.NextFeature();
            while (sFeature != null)
            {
                int KID = Convert.ToInt32(sFeature.get_Value(0));
                IPolygon pPolygon = sFeature.Shape as IPolygon;
                sDic.Add(KID, pPolygon);

                sFeature = sFeatureCursor.NextFeature();
            }
            #endregion

            #region 获取对应Matching建筑物
            IFeatureCursor aFeatureCursor = aFeatureClass.Update(null, false);
            IFeature aFeature = aFeatureCursor.NextFeature();
            while (aFeature != null)
            {
                int KID = Convert.ToInt32(aFeature.get_Value(0));              
                IPolygon pPolygon = aFeature.Shape as IPolygon;
                aDic.Add(KID, pPolygon);

                aFeature = aFeatureCursor.NextFeature();
            }
            #endregion
            #endregion

            System.IO.FileStream fsID = new System.IO.FileStream(@"C:\Users\10988\Desktop\Original data\Pro25000ID.txt", System.IO.FileMode.OpenOrCreate);
            System.IO.FileStream fsArea = new System.IO.FileStream(@"C:\Users\10988\Desktop\Original data\Pro25000AreaDis.txt", System.IO.FileMode.OpenOrCreate);
            System.IO.FileStream fsOri = new System.IO.FileStream(@"C:\Users\10988\Desktop\Original data\Pro25000OriDis.txt", System.IO.FileMode.OpenOrCreate);
            System.IO.FileStream fsDis = new System.IO.FileStream(@"C:\Users\10988\Desktop\Original data\Pro25000Dis.txt", System.IO.FileMode.OpenOrCreate);
            System.IO.FileStream fsShape = new System.IO.FileStream(@"C:\Users\10988\Desktop\Original data\Pro25000Shape.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter swID = new StreamWriter(fsID);
            StreamWriter swArea = new StreamWriter(fsArea);
            StreamWriter swOri = new StreamWriter(fsOri);
            StreamWriter swDis = new StreamWriter(fsDis);
            StreamWriter swShape = new StreamWriter(fsShape);
            #region 计算相应的差异
            List<int> sKeys = sDic.Keys.ToList();
            for (int i = 0; i < sKeys.Count; i++)
            {
                IPolygon CachePolygon1 = sDic[i];
                IPolygon CachePolygon2 = aDic[i];
                IArea CacheArea1 = CachePolygon1 as IArea;
                IArea CacheArea2 = CachePolygon2 as IArea;

                #region 位置差异计算               
                IPoint CenterPoint1 = CacheArea1.Centroid;
                IPoint CenterPoint2 = CacheArea2.Centroid;

                double Dis = Math.Sqrt((CenterPoint1.X - CenterPoint2.X) * (CenterPoint1.X - CenterPoint2.X) +
                   (CenterPoint1.Y - CenterPoint2.Y) * (CenterPoint1.Y - CenterPoint2.Y));
                #endregion

                #region 方向差异计算
                工具类.ParameterCompute PC = new 工具类.ParameterCompute();
                double smbro1 = PC.GetSMBROrientation(CachePolygon1);
                double smbro2 = PC.GetSMBROrientation(CachePolygon2);

                double OriDis = 0;//方向差异在90以内
                if (Math.Abs(smbro1 - smbro2) > 90)
                {
                    OriDis = 180 - Math.Abs(smbro1 - smbro2);
                }

                else
                {
                    OriDis = Math.Abs(smbro1 - smbro2);
                }

                #endregion

                #region 面积差异计算
                double Area1 = CacheArea1.Area;
                double Area2 = CacheArea2.Area;

                double AreaDis = Math.Abs(Area2 - Area1) / Area1;
                #endregion

                #region 形状差异计算
                double MDSim = this.MDComputation(CachePolygon1, CachePolygon2);
                #endregion

                swID.Write(sKeys[i].ToString()); swID.Write("\r\n");
                swDis.Write(Dis.ToString()); swDis.Write("\r\n");
                swOri.Write(OriDis.ToString()); swOri.Write("\r\n");
                swArea.Write(AreaDis.ToString()); swArea.Write("\r\n");
                swShape.Write(MDSim.ToString());swShape.Write("\r\n");
            }
            #endregion

            swID.Close();
            fsID.Close();
            swArea.Close();
            fsArea.Close();
            swOri.Close();
            fsOri.Close();
            swDis.Close();
            fsDis.Close();
            swShape.Close();
            fsShape.Close();
        }
        #endregion

        /// <summary>
        /// MDSim (a^b)(a v b)
        /// </summary>
        /// <param name="TargetPo"></param>
        /// <param name="MatchingPo"></param>
        /// <returns></returns>
        public double MDComputation(IPolygon TargetPo, IPolygon MatchingPo)
        {
            double MDSim = 0;
            TargetPo.SimplifyPreserveFromTo();//保证拓扑正确
            MatchingPo.SimplifyPreserveFromTo();//保证拓扑正确

            if (TargetPo != null && MatchingPo != null)
            {               
                #region 计算MDSim
                ITopologicalOperator iTo = TargetPo as ITopologicalOperator;

                double idArea = 0;
                double udArea = 0;
                IGeometry iGeo = iTo.Intersect(MatchingPo as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                
                if (iGeo != null)
                {
                    IArea iArea = iGeo as IArea;
                    idArea = iArea.Area;
                }
            
                IGeometry uGeo = iTo.Union(MatchingPo as IGeometry);
                if (uGeo != null)
                {
                    IArea uArea = uGeo as IArea;
                    udArea = uArea.Area;
                }

                MDSim = idArea / udArea;
                #endregion
            }

            return MDSim;
        }
    }
}
