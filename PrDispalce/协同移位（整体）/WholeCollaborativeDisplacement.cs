using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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
using PrDispalce.工具类.CollabrativeDisplacement;
using PrDispalce.工具类;

namespace PrDispalce.协同移位_整体_
{
    public partial class WholeCollaborativeDisplacement : Form
    {
        public WholeCollaborativeDisplacement(IMap mMap)
        {
            InitializeComponent();
            this.pMap = mMap;
        }

        #region 参数
        string localFilePath, fileNameExt, FilePath;
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        #endregion

        #region 初始化
        private void WholeCollaborativeDisplacement_Load(object sender, EventArgs e)
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
        #endregion

        #region 输出路径
        private void button1_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = " shp files(*.shp)|";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //获得文件路径
                localFilePath = saveFileDialog1.FileName.ToString();

                //获取文件名，不带路径
                fileNameExt = localFilePath.Substring(localFilePath.LastIndexOf("\\") + 1);

                //获取文件路径，不带文件名
                FilePath = localFilePath.Substring(0, localFilePath.LastIndexOf("\\"));
            }

            this.comboBox3.Text = localFilePath;
        }
        #endregion

        #region 协同+场移位+协同+局部移位
        private void button2_Click(object sender, EventArgs e)
        {
            CommonTools cT = new CommonTools();
            Collabration col = new Collabration();

            #region 获取图层
            List<IFeatureLayer> list1 = new List<IFeatureLayer>();
            IFeatureLayer StreetLayer1 = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text.ToString());
            IFeatureLayer BuildingLayer1 = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text.ToString());
            list1.Add(StreetLayer1);
            list1.Add(BuildingLayer1);
            #endregion

            #region 读取约束参数
            double RoadWidth = double.Parse(this.textBox1.Text);
            double BuildingWidth = double.Parse(this.textBox2.Text);
            double MinDis = double.Parse(this.textBox3.Text);

            double RoadBuildingDis = RoadWidth / 2;//靠近道路的建筑物移位距离
            double BuildingDis = BuildingWidth * 2 + MinDis;//建筑物间的最小距离

            double MinRoadBuilding = RoadWidth / 2 + BuildingWidth + MinDis;//建筑物与道路的最小距离
            #endregion

            #region 图层数据读入指定数据结构并内插
            SMap map1Copy = new SMap(list1); //最初始的ID存储在map1Copy中
            map1Copy.ReadDateFrmEsriLyrsForEnrichNetWork();
            map1Copy.InterpretatePoint(2);

            SMap map1 = new SMap(list1);
            map1.ReadDateFrmEsriLyrsForEnrichNetWork();
            map1.InterpretatePoint(2);
            #endregion

            #region 构造map1的邻近关系
            DelaunayTin dt1 = new DelaunayTin(map1.TriNodeList);
            dt1.CreateDelaunayTin(AlgDelaunayType.Side_extent);

            ConvexNull cn1 = new ConvexNull(dt1.TriNodeList);
            cn1.CreateConvexNull();

            ConsDelaunayTin cdt1 = new ConsDelaunayTin(dt1);
            cdt1.CreateConsDTfromPolylineandPolygon(map1.PolylineList, map1.PolygonList);

            Triangle.WriteID(dt1.TriangleList);
            TriEdge.WriteID(dt1.TriEdgeList);

            AuxStructureLib.Skeleton ske1 = new AuxStructureLib.Skeleton(cdt1, map1);
            ske1.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
            ske1.TranverseSkeleton_Segment_PLP_NONull();

            ProxiGraph pg1 = new ProxiGraph();
            pg1.CreateProxiGraphfrmSkeletonBuildings(map1, ske1);
            pg1.DeleteRepeatedEdge(pg1.EdgeList);//去重

            VoronoiDiagram vd = new AuxStructureLib.VoronoiDiagram(ske1, pg1, map1); //只用于基于邻近图计算场
            pg1.CreatePgwithoutlongEdges(pg1.NodeList, pg1.EdgeList, 0.0004);//删除长边
            #endregion

            #region 原生冲突探测
            PrDispalce.工具类.CollabrativeDisplacement.CConflictDetector ccd1 = new CConflictDetector();//创建冲突探测工具对象
            ccd1.ConflictDetectByPg(pg1.EdgeList, MinRoadBuilding, BuildingDis, map1.PolygonList, map1.PolylineList);
            #endregion

            while (ccd1.ConflictEdge.Count > 0)
            {
                #region 用于解决原生冲突的建筑物备份与还原 map2
                List<IFeatureLayer> list2 = new List<IFeatureLayer>();
                IFeatureLayer StreetLayer2 = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text.ToString());

                IFeatureLayer MemoryBuildingFeatureLayer2 = pFeatureHandle.CreatePolygonFeatureLayerInmemeory(pMap, "MemoryLayer2");
                IFeatureClass MemoryBuildingFeatureClass2 = MemoryBuildingFeatureLayer2.FeatureClass;
                for (int u = 0; u < map1.PolygonList.Count; u++)
                {
                    IPolygon pPolygon = cT.ObjectConvert(map1.PolygonList[u]);
                    IFeature pfea = MemoryBuildingFeatureClass2.CreateFeature();
                    pfea.Shape = pPolygon as IGeometry;
                    pfea.Store();
                    IPolygon qPolygon = pfea.Shape as IPolygon;
                    PolygonObject qPolygonObject = cT.PolygonConvert(qPolygon);                   
                }

                list2.Add(StreetLayer2);
                list2.Add(MemoryBuildingFeatureLayer2);

                SMap map2 = new SMap(list2);
                map2.ReadDateFrmEsriLyrs();
                map2.InterpretatePoint(2);

                SMap map2Copy = new SMap(list2);
                map2Copy.ReadDateFrmEsriLyrs();
                map2Copy.InterpretatePoint(2);
                #endregion

                #region map2与map1建筑物匹配;map2Copy与map1做匹配
                for (int p2 = 0; p2 < map2.PolygonList.Count; p2++)
                {
                    for (int p1 = 0; p1 < map1.PolygonList.Count; p1++)
                    {
                        if (map1.PolygonList[p1].CalProxiNode().X == map2.PolygonList[p2].CalProxiNode().X && map1.PolygonList[p1].CalProxiNode().Y == map2.PolygonList[p2].CalProxiNode().Y)
                        {
                            map2.PolygonList[p2].ID = map1.PolygonList[p1].ID;
                            map2.PolygonList[p2].IsReshape = map1.PolygonList[p1].IsReshape;
                            map2.PolygonList[p2].IDList = map1.PolygonList[p1].IDList;
                        }
                    }
                }

                for (int p2 = 0; p2 < map2Copy.PolygonList.Count; p2++)
                {
                    for (int p1 = 0; p1 < map1.PolygonList.Count; p1++)
                    {
                        if (map1.PolygonList[p1].CalProxiNode().X == map2Copy.PolygonList[p2].CalProxiNode().X && map1.PolygonList[p1].CalProxiNode().Y == map2Copy.PolygonList[p2].CalProxiNode().Y)
                        {
                            map2Copy.PolygonList[p2].ID = map1.PolygonList[p1].ID;
                            map2Copy.PolygonList[p2].IsReshape = map1.PolygonList[p1].IsReshape;
                            map2Copy.PolygonList[p2].IDList = map1.PolygonList[p1].IDList;
                        }
                    }
                }
                #endregion

                #region 基于道路场的移位
                vd.FieldBuildBasedonRoads2(pg1.PgwithouLongEdgesEdgesList, map1.PolygonList);//建立map1基于道路的场
                PrDispalce.工具类.CollabrativeDisplacement.ForceComputation pForceComputaiton = new 工具类.CollabrativeDisplacement.ForceComputation();
                pForceComputaiton.FieldBasedForceComputation2(RoadBuildingDis, vd.FielderListforRoads1, pg1.PgwithoutLongEdgesNodesList, pg1.PgwithouLongEdgesEdgesList, 5);
                pForceComputaiton.UpdataCoordsforPGbyForce_Group2(pg1.NodeList, map2, pForceComputaiton.CombinationForceListforRoadsField);//更新建筑物位置(对map做更新)
                pForceComputaiton.UpdataCoordsforPGbyForce_Group2(pg1.NodeList, map2Copy, pForceComputaiton.CombinationForceListforRoadsField);//更新建筑物位置(对map做更新)
                #endregion

                #region 基于建筑物间冲突的移位
                for (int i = 0; i < ccd1.ConflictEdge.Count; i++)
                {
                    ProxiEdge Pe1 = ccd1.ConflictEdge[i];
                    if (Pe1.Node1.FeatureType == FeatureType.PolygonType && Pe1.Node2.FeatureType == FeatureType.PolygonType)
                    {
                        PolygonObject Po1 = null; PolygonObject Po2 = null;

                        #region 找到冲突边对应的建筑物
                        for (int j = 0; j < map1.PolygonList.Count; j++)
                        {
                            if (Pe1.Node1.TagID == map1.PolygonList[j].ID && map1.PolygonList[j].FeatureType == FeatureType.PolygonType)
                            {
                                Po1 = map1.PolygonList[j];
                            }

                            if (Pe1.Node2.TagID == map1.PolygonList[j].ID && map1.PolygonList[j].FeatureType == FeatureType.PolygonType)
                            {
                                Po2 = map1.PolygonList[j];
                            }
                        }
                        #endregion

                        vd.FieldBuildBasedonBuildings2(Po1.ID, Po2.ID, pg1.PgwithouLongEdgesEdgesList, map1.PolygonList);//建立map1基于建筑物间冲突的移位场
                        pForceComputaiton.FieldBasedForceComputationforBuildings(vd.FielderListforBuildings1, pg1.PgwithoutLongEdgesNodesList, pg1.PgwithouLongEdgesEdgesList, 3, 3, BuildingDis);
                        pForceComputaiton.UpdataCoordsforPGbyForce_Group2(pg1.NodeList, map2, pForceComputaiton.CombinationForceListforBuildingField);//更新建筑物位置（对map做更新）
                        pForceComputaiton.UpdataCoordsforPGbyForce_Group2(pg1.NodeList, map2Copy, pForceComputaiton.CombinationForceListforBuildingField);//更新建筑物位置（对map做更新）
                    }
                }
                #endregion

                #region 构造移位后Map2的邻近关系
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
                pg2.DeleteRepeatedEdge(pg2.EdgeList);//去重
                pg2.CreatePgwithoutlongEdges(pg2.NodeList, pg2.EdgeList, 0.0004);//删除长边
                #endregion

                #region 次生冲突探测（Map2冲突探测）
                PrDispalce.工具类.CollabrativeDisplacement.CConflictDetector ccd2 = new CConflictDetector();//创建冲突探测工具对象
                ccd2.ConflictDetectByPg(pg2.PgwithouLongEdgesEdgesList, MinRoadBuilding, BuildingDis, map2.PolygonList, map2.PolylineList);
                #endregion

                #region 判断是否还存在冲突（Map2是否还存在冲突）
                while (ccd2.ConflictEdge.Count > 0)
                {
                    #region 用于解决次生冲突的建筑物备份与还原 map3
                    List<IFeatureLayer> list3 = new List<IFeatureLayer>();
                    IFeatureLayer StreetLayer3 = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text.ToString());

                    IFeatureLayer MemoryBuildingFeatureLayer3 = pFeatureHandle.CreatePolygonFeatureLayerInmemeory(pMap, "MemoryLayer3");
                    IFeatureClass MemoryBuildingFeatureClass3 = MemoryBuildingFeatureLayer3.FeatureClass;
                    for (int u = 0; u < map2.PolygonList.Count; u++)
                    {
                        IPolygon pPolygon = cT.ObjectConvert(map2.PolygonList[u]);
                        IFeature pfea = MemoryBuildingFeatureClass3.CreateFeature();
                        pfea.Shape = pPolygon as IGeometry;
                        pfea.Store();
                        IPolygon qPolygon = pfea.Shape as IPolygon;
                        PolygonObject qPolygonObject = cT.PolygonConvert(qPolygon);
                    }

                    list3.Add(StreetLayer3);
                    list3.Add(MemoryBuildingFeatureLayer3);

                    SMap map3 = new SMap(list3);
                    map3.ReadDateFrmEsriLyrs();
                    map3.InterpretatePoint(2);

                    SMap map3Copy = new SMap(list3);
                    map3Copy.ReadDateFrmEsriLyrs();
                    map3Copy.InterpretatePoint(2);
                    #endregion

                    #region map2与map3建筑物匹配;map2与map3Copy做匹配
                    for (int p3 = 0; p3 < map3.PolygonList.Count; p3++)
                    {
                        for (int p2 = 0; p2 < map2.PolygonList.Count; p2++)
                        {
                            if (map2.PolygonList[p2].CalProxiNode().X == map3.PolygonList[p3].CalProxiNode().X && map2.PolygonList[p2].CalProxiNode().Y == map3.PolygonList[p3].CalProxiNode().Y)
                            {
                                map3.PolygonList[p3].ID = map2.PolygonList[p2].ID;
                                map3.PolygonList[p3].IsReshape = map2.PolygonList[p2].IsReshape;
                                map3.PolygonList[p3].IDList = map2.PolygonList[p2].IDList;
                            }
                        }
                    }

                    for (int p3 = 0; p3 < map3Copy.PolygonList.Count; p3++)
                    {
                        for (int p2 = 0; p2 < map2.PolygonList.Count; p2++)
                        {
                            if (map2.PolygonList[p2].CalProxiNode().X == map3Copy.PolygonList[p3].CalProxiNode().X && map2.PolygonList[p2].CalProxiNode().Y == map3Copy.PolygonList[p3].CalProxiNode().Y)
                            {
                                map3Copy.PolygonList[p3].ID = map2.PolygonList[p2].ID;
                                map3Copy.PolygonList[p3].IsReshape = map2.PolygonList[p2].IsReshape;
                                map3Copy.PolygonList[p3].IDList = map2.PolygonList[p2].IDList;
                            }
                        }
                    }
                    #endregion

                    #region 若还存在原生冲突；则判断原生冲突是否能被解决（只移位原生冲突建筑物），无法解决则协同
                    if (ccd2.IsInitialConflictSolved(ccd1.ConflictEdge, ccd2.ConflictEdge))
                    {
                        #region 局部移位
                        for (int i = 0; i < ccd2.ConflictEdge.Count; i++)
                        {
                            ProxiEdge Pe1 = ccd2.ConflictEdge[i];

                            #region 若是未解决的原生冲突
                            if (ccd2.IsOneInitialConflictSolved(Pe1, ccd1.ConflictEdge))
                            {
                                #region 建筑物与道路次生冲突
                                if (Pe1.Node1.FeatureType == FeatureType.PolygonType && Pe1.Node2.FeatureType == FeatureType.PolylineType)//node2在道路上
                                {
                                    double f = MinRoadBuilding - Pe1.NearestEdge.NearestDistance;
                                    double cos = (Pe1.NearestEdge.Point1.X - Pe1.NearestEdge.Point2.X) / Pe1.NearestEdge.NearestDistance;
                                    double sin = (Pe1.NearestEdge.Point1.Y - Pe1.NearestEdge.Point2.Y) / Pe1.NearestEdge.NearestDistance;

                                    #region 找到对应建筑物更新位置
                                    for (int j = 0; j < map3.PolygonList.Count; j++)
                                    {
                                        if (Pe1.Node1.TagID == map3.PolygonList[j].ID && map3.PolygonList[j].FeatureType == FeatureType.PolygonType)
                                        {
                                            foreach (TriNode curPoint in map3.PolygonList[j].PointList)
                                            {
                                                curPoint.X += f * cos;
                                                curPoint.Y += f * sin;
                                            }
                                        }
                                    }

                                    for (int j = 0; j < map3Copy.PolygonList.Count; j++)
                                    {
                                        if (Pe1.Node1.TagID == map3Copy.PolygonList[j].ID && map3Copy.PolygonList[j].FeatureType == FeatureType.PolygonType)
                                        {
                                            foreach (TriNode curPoint in map3Copy.PolygonList[j].PointList)
                                            {
                                                curPoint.X += f * cos;
                                                curPoint.Y += f * sin;
                                            }
                                        }
                                    }
                                    #endregion
                                }

                                if (Pe1.Node1.FeatureType == FeatureType.PolylineType && Pe1.Node2.FeatureType == FeatureType.PolygonType)//node1在道路上
                                {
                                    double f = MinRoadBuilding - Pe1.NearestEdge.NearestDistance;
                                    double cos = (Pe1.NearestEdge.Point2.X - Pe1.NearestEdge.Point1.X) / Pe1.NearestEdge.NearestDistance;
                                    double sin = (Pe1.NearestEdge.Point2.Y - Pe1.NearestEdge.Point1.Y) / Pe1.NearestEdge.NearestDistance;

                                    #region 找到对应建筑物更新位置
                                    for (int j = 0; j < map3.PolygonList.Count; j++)
                                    {
                                        if (Pe1.Node2.TagID == map3.PolygonList[j].ID && map3.PolygonList[j].FeatureType == FeatureType.PolygonType)
                                        {
                                            foreach (TriNode curPoint in map3.PolygonList[j].PointList)
                                            {
                                                curPoint.X += f * cos;
                                                curPoint.Y += f * sin;
                                            }
                                        }
                                    }

                                    for (int j = 0; j < map3Copy.PolygonList.Count; j++)
                                    {
                                        if (Pe1.Node2.TagID == map3Copy.PolygonList[j].ID && map3Copy.PolygonList[j].FeatureType == FeatureType.PolygonType)
                                        {
                                            foreach (TriNode curPoint in map3Copy.PolygonList[j].PointList)
                                            {
                                                curPoint.X += f * cos;
                                                curPoint.Y += f * sin;
                                            }
                                        }
                                    }
                                    #endregion
                                }
                                #endregion

                                #region 建筑物间次生冲突
                                if (Pe1.Node1.FeatureType == FeatureType.PolygonType && Pe1.Node2.FeatureType == FeatureType.PolygonType)
                                {
                                    PolygonObject Po1 = null; PolygonObject Po2 = null;

                                    #region 找到冲突边对应的建筑物
                                    for (int j = 0; j < map2.PolygonList.Count; j++)
                                    {
                                        if (Pe1.Node1.TagID == map2.PolygonList[j].ID && map2.PolygonList[j].FeatureType == FeatureType.PolygonType)
                                        {
                                            Po1 = map2.PolygonList[j];
                                        }

                                        if (Pe1.Node2.TagID == map2.PolygonList[j].ID && map2.PolygonList[j].FeatureType == FeatureType.PolygonType)
                                        {
                                            Po2 = map2.PolygonList[j];
                                        }
                                    }
                                    #endregion

                                    vd.FieldBuildBasedonBuildings2(Po1.ID, Po2.ID, pg2.PgwithouLongEdgesEdgesList, map2.PolygonList);//
                                    pForceComputaiton.FieldBasedForceComputationforBuildings(vd.FielderListforBuildings1, pg2.PgwithoutLongEdgesNodesList, pg2.PgwithouLongEdgesEdgesList, 3, 0, BuildingDis);
                                    pForceComputaiton.UpdataCoordsforPGbyForce_Group2(pg2.NodeList, map3, pForceComputaiton.CombinationForceListforBuildingField);//更新建筑物位置（对map3做更新）
                                    pForceComputaiton.UpdataCoordsforPGbyForce_Group2(pg2.NodeList, map3Copy, pForceComputaiton.CombinationForceListforBuildingField);
                                }
                                #endregion
                            }
                            #endregion
                        }
                        #endregion

                        #region 重构移位后map3的邻近关系
                        DelaunayTin dt3 = new DelaunayTin(map3.TriNodeList);
                        dt3.CreateDelaunayTin(AlgDelaunayType.Side_extent);

                        ConvexNull cn3 = new ConvexNull(dt3.TriNodeList);
                        cn3.CreateConvexNull();

                        ConsDelaunayTin cdt3 = new ConsDelaunayTin(dt3);
                        cdt3.CreateConsDTfromPolylineandPolygon(map3.PolylineList, map3.PolygonList);

                        Triangle.WriteID(dt3.TriangleList);
                        TriEdge.WriteID(dt3.TriEdgeList);

                        AuxStructureLib.Skeleton ske3 = new AuxStructureLib.Skeleton(cdt3, map3);
                        ske3.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
                        ske3.TranverseSkeleton_Segment_PLP_NONull();

                        ProxiGraph pg3 = new ProxiGraph();
                        pg3.CreateProxiGraphfrmSkeletonBuildings(map3, ske3);
                        pg3.DeleteRepeatedEdge(pg3.EdgeList);//去重
                        pg3.CreatePgwithoutlongEdges(pg3.NodeList, pg3.EdgeList, 0.0004);//删除长边
                        #endregion

                        #region map3冲突检测
                        PrDispalce.工具类.CollabrativeDisplacement.CConflictDetector ccd3 = new CConflictDetector();//创建冲突探测工具对象
                        ccd3.ConflictDetectByPg(pg3.PgwithouLongEdgesEdgesList, MinRoadBuilding, BuildingDis, map3.PolygonList, map3.PolylineList);
                        #endregion

                        #region 若原生冲突未被解决，则协同操作map1
                        if (ccd3.IsInitialConflictSolved(ccd1.ConflictEdge, ccd3.ConflictEdge))
                        {
                            //找到未被解决的原生冲突
                            List<ProxiEdge> InvolvedEdges = ccd3.ReturnInvolvedInitialEdges(ccd1.ConflictEdge, ccd3.ConflictEdge);
                            //返回需要处理的原生冲突
                            ProxiEdge pTargetEdge = ccd3.ReturnEdgeTobeSolved(InvolvedEdges);
                            if (pTargetEdge != null)
                            {
                                col.pCollabration(pTargetEdge, map1, pMap, true, null);

                                #region map1与map1Copy匹配
                                for (int p1 = 0; p1 < map1.PolygonList.Count; p1++)
                                {
                                    for (int p = 0; p < map1Copy.PolygonList.Count; p++)
                                    {
                                        if (map1.PolygonList[p1].CalProxiNode().X == map1Copy.PolygonList[p].CalProxiNode().X && map1.PolygonList[p1].CalProxiNode().Y == map1Copy.PolygonList[p].CalProxiNode().Y)
                                        {
                                            map1.PolygonList[p1].ID = map1Copy.PolygonList[p].ID;
                                            map1.PolygonList[p1].IsReshape = map1Copy.PolygonList[p].IsReshape;
                                            map1.PolygonList[p1].IDList = map1Copy.PolygonList[p].IDList;
                                        }
                                    }
                                }
                                #endregion
                            }

                            else
                            {
                                col.pCollabration(ccd1.ConflictEdge[0], map1, pMap, true, null);

                                #region map1与map1Copy匹配
                                for (int p1 = 0; p1 < map1.PolygonList.Count; p1++)
                                {
                                    for (int p = 0; p < map1Copy.PolygonList.Count; p++)
                                    {
                                        if (map1.PolygonList[p1].CalProxiNode().X == map1Copy.PolygonList[p].CalProxiNode().X && map1.PolygonList[p1].CalProxiNode().Y == map1Copy.PolygonList[p].CalProxiNode().Y)
                                        {
                                            map1.PolygonList[p1].ID = map1Copy.PolygonList[p].ID;
                                            map1.PolygonList[p1].IsReshape = map1Copy.PolygonList[p].IsReshape;
                                            map1.PolygonList[p1].IDList = map1Copy.PolygonList[p].IDList;
                                        }
                                    }
                                }
                                #endregion
                            }

                            #region 重构map1邻近关系，并冲突探测
                            dt1 = new DelaunayTin(map1.TriNodeList);
                            dt1.CreateDelaunayTin(AlgDelaunayType.Side_extent);

                            cn1 = new ConvexNull(dt1.TriNodeList);
                            cn1.CreateConvexNull();

                            cdt1 = new ConsDelaunayTin(dt1);
                            cdt1.CreateConsDTfromPolylineandPolygon(map1.PolylineList, map1.PolygonList);

                            Triangle.WriteID(dt1.TriangleList);
                            TriEdge.WriteID(dt1.TriEdgeList);

                            ske1 = new AuxStructureLib.Skeleton(cdt1, map1);
                            ske1.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
                            ske1.TranverseSkeleton_Segment_PLP_NONull();

                            pg1 = new ProxiGraph();
                            pg1.CreateProxiGraphfrmSkeletonBuildings(map1, ske1);
                            pg1.DeleteRepeatedEdge(pg1.EdgeList);//去重
                            pg1.CreatePgwithoutlongEdges(pg1.NodeList, pg1.EdgeList, 0.0004);//删除长边

                            ccd1.ConflictDetectByPg(pg1.EdgeList, MinRoadBuilding, BuildingDis, map1.PolygonList, map1.PolylineList);
                            break;
                            #endregion

                        }
                        #endregion

                        #region 若原生冲突被解决，即不存在原生冲突;则只判断是否存在新产生的次生冲突，若还存在新产生的次生冲突，对map3局部移位
                        else
                        {
                            #region 用于解决次生冲突的建筑物备份与还原 map4
                            List<IFeatureLayer> list4 = new List<IFeatureLayer>();
                            IFeatureLayer StreetLayer4 = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text.ToString());

                            IFeatureLayer MemoryBuildingFeatureLayer4 = pFeatureHandle.CreatePolygonFeatureLayerInmemeory(pMap, "MemoryLayer4");
                            IFeatureClass MemoryBuildingFeatureClass4 = MemoryBuildingFeatureLayer4.FeatureClass;
                            for (int u = 0; u < map3.PolygonList.Count; u++)
                            {
                                IPolygon pPolygon = cT.ObjectConvert(map3.PolygonList[u]);
                                IFeature pfea = MemoryBuildingFeatureClass4.CreateFeature();
                                pfea.Shape = pPolygon as IGeometry;
                                pfea.Store();
                                IPolygon qPolygon = pfea.Shape as IPolygon;
                                PolygonObject qPolygonObject = cT.PolygonConvert(qPolygon);
                            }

                            list4.Add(StreetLayer4);
                            list4.Add(MemoryBuildingFeatureLayer4);

                            SMap map4 = new SMap(list4);
                            map4.ReadDateFrmEsriLyrs();
                            map4.InterpretatePoint(2);
                            #endregion

                            while (ccd3.ConflictEdge.Count > 0)
                            {                              

                                #region 局部移位
                                for (int i = 0; i < ccd3.ConflictEdge.Count; i++)
                                {
                                    ProxiEdge Pe1 = ccd3.ConflictEdge[i];

                                    #region 若不是未解决的原生冲突
                                    if (!ccd3.IsOneInitialConflictSolved(Pe1, ccd1.ConflictEdge))
                                    {
                                        #region 建筑物与道路次生冲突
                                        if (Pe1.Node1.FeatureType == FeatureType.PolygonType && Pe1.Node2.FeatureType == FeatureType.PolylineType)//node2在道路上
                                        {
                                            double f = MinRoadBuilding - Pe1.NearestEdge.NearestDistance;
                                            double cos = (Pe1.NearestEdge.Point1.X - Pe1.NearestEdge.Point2.X) / Pe1.NearestEdge.NearestDistance;
                                            double sin = (Pe1.NearestEdge.Point1.Y - Pe1.NearestEdge.Point2.Y) / Pe1.NearestEdge.NearestDistance;

                                            #region 找到对应建筑物更新位置
                                            for (int j = 0; j < map4.PolygonList.Count; j++)
                                            {
                                                if (Pe1.Node1.TagID == map4.PolygonList[j].ID && map4.PolygonList[j].FeatureType == FeatureType.PolygonType)
                                                {
                                                    foreach (TriNode curPoint in map4.PolygonList[j].PointList)
                                                    {
                                                        curPoint.X += f * cos;
                                                        curPoint.Y += f * sin;
                                                    }
                                                }
                                            }
                                            #endregion
                                        }

                                        if (Pe1.Node1.FeatureType == FeatureType.PolylineType && Pe1.Node2.FeatureType == FeatureType.PolygonType)//node1在道路上
                                        {
                                            double f = MinRoadBuilding - Pe1.NearestEdge.NearestDistance;
                                            double cos = (Pe1.NearestEdge.Point2.X - Pe1.NearestEdge.Point1.X) / Pe1.NearestEdge.NearestDistance;
                                            double sin = (Pe1.NearestEdge.Point2.Y - Pe1.NearestEdge.Point1.Y) / Pe1.NearestEdge.NearestDistance;

                                            #region 找到对应建筑物更新位置
                                            for (int j = 0; j < map4.PolygonList.Count; j++)
                                            {
                                                if (Pe1.Node2.TagID == map4.PolygonList[j].ID && map4.PolygonList[j].FeatureType == FeatureType.PolygonType)
                                                {
                                                    foreach (TriNode curPoint in map4.PolygonList[j].PointList)
                                                    {
                                                        curPoint.X += f * cos;
                                                        curPoint.Y += f * sin;
                                                    }
                                                }
                                            }
                                            #endregion
                                        }
                                        #endregion

                                        #region 建筑物间次生冲突
                                        if (Pe1.Node1.FeatureType == FeatureType.PolygonType && Pe1.Node2.FeatureType == FeatureType.PolygonType)
                                        {
                                            PolygonObject Po1 = null; PolygonObject Po2 = null;

                                            #region 找到冲突边对应的建筑物
                                            for (int j = 0; j < map3.PolygonList.Count; j++)
                                            {
                                                if (Pe1.Node1.TagID == map3.PolygonList[j].ID && map3.PolygonList[j].FeatureType == FeatureType.PolygonType)
                                                {
                                                    Po1 = map3.PolygonList[j];
                                                }

                                                if (Pe1.Node2.TagID == map3.PolygonList[j].ID && map3.PolygonList[j].FeatureType == FeatureType.PolygonType)
                                                {
                                                    Po2 = map3.PolygonList[j];
                                                }
                                            }
                                            #endregion

                                            vd.FieldBuildBasedonBuildings2(Po1.ID, Po2.ID, pg3.PgwithouLongEdgesEdgesList, map3.PolygonList);//
                                            pForceComputaiton.FieldBasedForceComputationforBuildings(vd.FielderListforBuildings1, pg3.PgwithoutLongEdgesNodesList, pg3.PgwithouLongEdgesEdgesList, 3, 0, BuildingDis);
                                            pForceComputaiton.UpdataCoordsforPGbyForce_Group2(pg3.NodeList, map4, pForceComputaiton.CombinationForceListforBuildingField);//更新建筑物位置（对map做更新）
                                        }
                                        #endregion
                                    }
                                    #endregion
                                }
                                #endregion

                                #region 重构map4的邻近关系，并冲突探测
                                DelaunayTin dt4 = new DelaunayTin(map4.TriNodeList);
                                dt4.CreateDelaunayTin(AlgDelaunayType.Side_extent);

                                ConvexNull cn4 = new ConvexNull(dt4.TriNodeList);
                                cn4.CreateConvexNull();

                                ConsDelaunayTin cdt4 = new ConsDelaunayTin(dt4);
                                cdt4.CreateConsDTfromPolylineandPolygon(map4.PolylineList, map4.PolygonList);

                                Triangle.WriteID(dt4.TriangleList);
                                TriEdge.WriteID(dt4.TriEdgeList);

                                AuxStructureLib.Skeleton ske4 = new AuxStructureLib.Skeleton(cdt4, map4);
                                ske4.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
                                ske4.TranverseSkeleton_Segment_PLP_NONull();

                                ProxiGraph pg4 = new ProxiGraph();
                                pg4.CreateProxiGraphfrmSkeletonBuildings(map4, ske4);
                                pg4.DeleteRepeatedEdge(pg4.EdgeList);//去重
                                pg4.CreatePgwithoutlongEdges(pg4.NodeList, pg4.EdgeList, 0.0004);//删除长边
                                #endregion

                                #region map4冲突检测
                                PrDispalce.工具类.CollabrativeDisplacement.CConflictDetector ccd4 = new CConflictDetector();//创建冲突探测工具对象
                                ccd4.ConflictDetectByPg(pg4.PgwithouLongEdgesEdgesList, MinRoadBuilding, BuildingDis, map4.PolygonList, map4.PolylineList);
                                #endregion

                                #region 若还存在冲突，则协同操作map3
                                if (ccd4.ConflictEdge.Count > 0)
                                {
                                    //找到相关的次生冲突
                                    List<ProxiEdge> InvolvedEdges = ccd4.ReturnInvolvedEdges(ccd3.ConflictEdge, ccd4.ConflictEdge);
                                    //找到待解决的冲突
                                    ProxiEdge pTargetEdge = ccd4.ReturnEdgeTobeSolved(InvolvedEdges);

                                    if (pTargetEdge != null)
                                    {
                                        col.pCollabration(pTargetEdge, map3, pMap, true, null);

                                        #region Map3与Map3Copy匹配
                                        for (int p1 = 0; p1 < map3.PolygonList.Count; p1++)
                                        {
                                            for (int p = 0; p < map3Copy.PolygonList.Count; p++)
                                            {
                                                if (map3.PolygonList[p1].CalProxiNode().X == map3Copy.PolygonList[p].CalProxiNode().X && map3.PolygonList[p1].CalProxiNode().Y == map3Copy.PolygonList[p].CalProxiNode().Y)
                                                {
                                                    map3.PolygonList[p1].ID = map3Copy.PolygonList[p].ID;
                                                    map3.PolygonList[p1].IsReshape = map3Copy.PolygonList[p].IsReshape;
                                                    map3.PolygonList[p1].IDList = map3Copy.PolygonList[p].IDList;
                                                }
                                            }
                                        }
                                        #endregion
                                    }

                                    else if (InvolvedEdges.Count > 0)
                                    {
                                        col.pCollabration(InvolvedEdges[0], map3, pMap, true, null);

                                        #region Map3与Map3Copy匹配
                                        for (int p1 = 0; p1 < map3.PolygonList.Count; p1++)
                                        {
                                            for (int p = 0; p < map3Copy.PolygonList.Count; p++)
                                            {
                                                if (map3.PolygonList[p1].CalProxiNode().X == map3Copy.PolygonList[p].CalProxiNode().X && map3.PolygonList[p1].CalProxiNode().Y == map3Copy.PolygonList[p].CalProxiNode().Y)
                                                {
                                                    map3.PolygonList[p1].ID = map3Copy.PolygonList[p].ID;
                                                    map3.PolygonList[p1].IsReshape = map3Copy.PolygonList[p].IsReshape;
                                                    map3.PolygonList[p1].IDList = map3Copy.PolygonList[p].IDList;
                                                }
                                            }
                                        }
                                        #endregion
                                    }

                                    else
                                    {
                                        map4.WriteResult2Shp(FilePath, pMap.SpatialReference);
                                        return;
                                    }
                                }
                                #endregion

                                #region 重新构建map3的邻近关系，并冲突探测
                                dt3 = new DelaunayTin(map3.TriNodeList);
                                dt3.CreateDelaunayTin(AlgDelaunayType.Side_extent);

                                cn3 = new ConvexNull(dt3.TriNodeList);
                                cn3.CreateConvexNull();

                                cdt3 = new ConsDelaunayTin(dt3);
                                cdt3.CreateConsDTfromPolylineandPolygon(map3.PolylineList, map3.PolygonList);

                                Triangle.WriteID(dt3.TriangleList);
                                TriEdge.WriteID(dt3.TriEdgeList);

                                ske3 = new AuxStructureLib.Skeleton(cdt3, map3);
                                ske3.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
                                ske3.TranverseSkeleton_Segment_PLP_NONull();

                                pg3 = new ProxiGraph();
                                pg3.CreateProxiGraphfrmSkeletonBuildings(map3, ske3);
                                pg3.DeleteRepeatedEdge(pg3.EdgeList);//去重
                                pg3.CreatePgwithoutlongEdges(pg3.NodeList, pg3.EdgeList, 0.0004);//删除长边

                                ccd3.ConflictDetectByPg(pg3.PgwithouLongEdgesEdgesList, MinRoadBuilding, BuildingDis, map3.PolygonList, map3.PolylineList);
                                #endregion
                            }

                            map4.WriteResult2Shp(FilePath, pMap.SpatialReference);
                            return;
                        }
                        #endregion
                    
                    }
                    #endregion

                    #region 不存在原生冲突；那就是存在次生冲突
                    else
                    {
                        #region 局部移位
                        for (int i = 0; i < ccd2.ConflictEdge.Count; i++)
                        {
                            ProxiEdge Pe1 = ccd2.ConflictEdge[i];

                            #region 若不是未解决的原生冲突
                            if (!ccd2.IsOneInitialConflictSolved(Pe1, ccd1.ConflictEdge))
                            {
                                #region 建筑物与道路次生冲突
                                if (Pe1.Node1.FeatureType == FeatureType.PolygonType && Pe1.Node2.FeatureType == FeatureType.PolylineType)//node2在道路上
                                {
                                    double f = MinRoadBuilding - Pe1.NearestEdge.NearestDistance;
                                    double cos = (Pe1.NearestEdge.Point1.X - Pe1.NearestEdge.Point2.X) / Pe1.NearestEdge.NearestDistance;
                                    double sin = (Pe1.NearestEdge.Point1.Y - Pe1.NearestEdge.Point2.Y) / Pe1.NearestEdge.NearestDistance;

                                    #region 找到对应建筑物更新位置
                                    for (int j = 0; j < map3.PolygonList.Count; j++)
                                    {
                                        if (Pe1.Node1.TagID == map3.PolygonList[j].ID && map3.PolygonList[j].FeatureType == FeatureType.PolygonType)
                                        {
                                            foreach (TriNode curPoint in map3.PolygonList[j].PointList)
                                            {
                                                curPoint.X += f * cos;
                                                curPoint.Y += f * sin;
                                            }
                                        }
                                    }
                                    #endregion
                                }

                                if (Pe1.Node1.FeatureType == FeatureType.PolylineType && Pe1.Node2.FeatureType == FeatureType.PolygonType)//node1在道路上
                                {
                                    double f = MinRoadBuilding - Pe1.NearestEdge.NearestDistance;
                                    double cos = (Pe1.NearestEdge.Point2.X - Pe1.NearestEdge.Point1.X) / Pe1.NearestEdge.NearestDistance;
                                    double sin = (Pe1.NearestEdge.Point2.Y - Pe1.NearestEdge.Point1.Y) / Pe1.NearestEdge.NearestDistance;

                                    #region 找到对应建筑物更新位置
                                    for (int j = 0; j < map3.PolygonList.Count; j++)
                                    {
                                        if (Pe1.Node2.TagID == map3.PolygonList[j].ID && map3.PolygonList[j].FeatureType == FeatureType.PolygonType)
                                        {
                                            foreach (TriNode curPoint in map3.PolygonList[j].PointList)
                                            {
                                                curPoint.X += f * cos;
                                                curPoint.Y += f * sin;
                                            }
                                        }
                                    }
                                    #endregion
                                }
                                #endregion

                                #region 建筑物间次生冲突
                                if (Pe1.Node1.FeatureType == FeatureType.PolygonType && Pe1.Node2.FeatureType == FeatureType.PolygonType)
                                {
                                    PolygonObject Po1 = null; PolygonObject Po2 = null;

                                    #region 找到冲突边对应的建筑物
                                    for (int j = 0; j < map2.PolygonList.Count; j++)
                                    {
                                        if (Pe1.Node1.TagID == map2.PolygonList[j].ID && map2.PolygonList[j].FeatureType == FeatureType.PolygonType)
                                        {
                                            Po1 = map2.PolygonList[j];
                                        }

                                        if (Pe1.Node2.TagID == map2.PolygonList[j].ID && map2.PolygonList[j].FeatureType == FeatureType.PolygonType)
                                        {
                                            Po2 = map2.PolygonList[j];
                                        }
                                    }
                                    #endregion

                                    vd.FieldBuildBasedonBuildings2(Po1.ID, Po2.ID, pg2.PgwithouLongEdgesEdgesList, map2.PolygonList);//
                                    pForceComputaiton.FieldBasedForceComputationforBuildings(vd.FielderListforBuildings1, pg2.PgwithoutLongEdgesNodesList, pg2.PgwithouLongEdgesEdgesList, 3, 0, BuildingDis);
                                    pForceComputaiton.UpdataCoordsforPGbyForce_Group2(pg2.NodeList, map3, pForceComputaiton.CombinationForceListforBuildingField);//更新建筑物位置（对map做更新）
                                }
                                #endregion
                            }
                            #endregion
                        }

                        #endregion

                        #region 重构移位后map3的邻近关系
                        DelaunayTin dt3 = new DelaunayTin(map3.TriNodeList);
                        dt3.CreateDelaunayTin(AlgDelaunayType.Side_extent);

                        ConvexNull cn3 = new ConvexNull(dt3.TriNodeList);
                        cn3.CreateConvexNull();

                        ConsDelaunayTin cdt3 = new ConsDelaunayTin(dt3);
                        cdt3.CreateConsDTfromPolylineandPolygon(map3.PolylineList, map3.PolygonList);

                        Triangle.WriteID(dt3.TriangleList);
                        TriEdge.WriteID(dt3.TriEdgeList);

                        AuxStructureLib.Skeleton ske3 = new AuxStructureLib.Skeleton(cdt3, map3);
                        ske3.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
                        ske3.TranverseSkeleton_Segment_PLP_NONull();

                        ProxiGraph pg3 = new ProxiGraph();
                        pg3.CreateProxiGraphfrmSkeletonBuildings(map3, ske3);
                        pg3.DeleteRepeatedEdge(pg3.EdgeList);//去重
                        pg3.CreatePgwithoutlongEdges(pg3.NodeList, pg3.EdgeList, 0.0004);//删除长边
                        #endregion

                        #region map3冲突检测
                        PrDispalce.工具类.CollabrativeDisplacement.CConflictDetector ccd3 = new CConflictDetector();//创建冲突探测工具对象
                        ccd3.ConflictDetectByPg(pg3.PgwithouLongEdgesEdgesList, MinRoadBuilding, BuildingDis, map3.PolygonList, map3.PolylineList);
                        #endregion

                        #region 若次生冲突未被解决，则协同操作map2
                        if(ccd3.ConflictEdge.Count > 0)
                        {
                            //找到相关的次生冲突
                            List<ProxiEdge> InvolvedEdges = ccd3.ReturnInvolvedEdges(ccd2.ConflictEdge, ccd3.ConflictEdge);
                            //找到待解决的冲突
                            ProxiEdge pTargetEdge = ccd3.ReturnEdgeTobeSolved(InvolvedEdges);
                            if (pTargetEdge != null)
                            {
                                col.pCollabration(pTargetEdge, map2, pMap, true, null);

                                #region Map2与Map2Copy匹配
                                for (int p1 = 0; p1 < map2.PolygonList.Count; p1++)
                                {
                                    for (int p = 0; p < map2Copy.PolygonList.Count; p++)
                                    {
                                        if (map2.PolygonList[p1].CalProxiNode().X == map2Copy.PolygonList[p].CalProxiNode().X && map2.PolygonList[p1].CalProxiNode().Y == map2Copy.PolygonList[p].CalProxiNode().Y)
                                        {
                                            map2.PolygonList[p1].ID = map2Copy.PolygonList[p].ID;
                                            map2.PolygonList[p1].IsReshape = map2Copy.PolygonList[p].IsReshape;
                                            map2.PolygonList[p1].IDList = map2Copy.PolygonList[p].IDList;
                                        }
                                    }
                                }
                                #endregion
                            }

                            //备注：有可能是找不到目标边，需要确认问题！！！
                            else
                            {
                                col.pCollabration(ccd2.ConflictEdge[0], map2, pMap, true, null);

                                #region Map2与Map2Copy匹配
                                for (int p1 = 0; p1 < map2.PolygonList.Count; p1++)
                                {
                                    for (int p = 0; p < map2Copy.PolygonList.Count; p++)
                                    {
                                        if (map2.PolygonList[p1].CalProxiNode().X == map2Copy.PolygonList[p].CalProxiNode().X && map2.PolygonList[p1].CalProxiNode().Y == map2Copy.PolygonList[p].CalProxiNode().Y)
                                        {
                                            map2.PolygonList[p1].ID = map2Copy.PolygonList[p].ID;
                                            map2.PolygonList[p1].IsReshape = map2Copy.PolygonList[p].IsReshape;
                                            map2.PolygonList[p1].IDList = map2Copy.PolygonList[p].IDList;
                                        }
                                    }
                                }
                                #endregion
                            }
                        }
                        #endregion

                        else
                        {
                            map3.WriteResult2Shp(FilePath, pMap.SpatialReference);
                            return;
                        }

                        #region 重构map2邻近关系，并冲突探测
                        dt2 = new DelaunayTin(map2.TriNodeList);
                        dt2.CreateDelaunayTin(AlgDelaunayType.Side_extent);

                        cn2 = new ConvexNull(dt2.TriNodeList);
                        cn2.CreateConvexNull();

                        cdt2 = new ConsDelaunayTin(dt2);
                        cdt2.CreateConsDTfromPolylineandPolygon(map2.PolylineList, map2.PolygonList);

                        Triangle.WriteID(dt2.TriangleList);
                        TriEdge.WriteID(dt2.TriEdgeList);

                        ske2 = new AuxStructureLib.Skeleton(cdt2, map2);
                        ske2.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
                        ske2.TranverseSkeleton_Segment_PLP_NONull();

                        pg2 = new ProxiGraph();
                        pg2.CreateProxiGraphfrmSkeletonBuildings(map2, ske2);
                        pg2.DeleteRepeatedEdge(pg2.EdgeList);//去重
                        pg2.CreatePgwithoutlongEdges(pg2.NodeList, pg2.EdgeList, 0.0004);//删除长边

                        ccd2.ConflictDetectByPg(pg2.PgwithouLongEdgesEdgesList, MinRoadBuilding, BuildingDis, map2.PolygonList, map2.PolylineList);
                        #endregion
                    }
                    #endregion
                }
                #endregion

                if (!(ccd2.ConflictEdge.Count > 0))
                {
                    map2.WriteResult2Shp(FilePath, pMap.SpatialReference);
                    return;
                }
            }

            map1.WriteResult2Shp(FilePath, pMap.SpatialReference);
        }
        #endregion

        /// <summary>
        /// 深拷贝 但是需要涉及的类都能被序列化！！ 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T DeepCopy<T>(T obj)
        {
            object retval;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                //序列化成流
                bf.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                //反序列化成对象
                retval = bf.Deserialize(ms);
                ms.Close();
            }
            return (T)retval;
        }
    }
}
