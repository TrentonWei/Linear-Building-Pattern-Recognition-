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

namespace PrDispalce.模式识别
{
    public partial class LinearPattern : Form
    {     
        public LinearPattern(IMap cMap)
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

        #region Pattern探测按钮
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
            foreach (PolygonObject Po in map.PolygonList)
            {
                Po.MBRO = PC.GetSMBROrientation(PC.ObjectConvert(Po));
                Po.tArea = PC.GetArea(PC.ObjectConvert(Po));
                Po.EdgeCount = Po.PointList.Count;
            }

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

            //pg.CreateMSTForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            //pg.CreateRNGForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            //pg.DeleteRepeatedEdge(pg.RNGBuildingEdgesListShortestDistance);

            //pg.LinearPatternDetected1(pg.RNGBuildingEdgesListShortestDistance, pg.RNGBuildingNodesListShortestDistance, double.Parse(this.textBox2.Text.ToString()), double.Parse(this.textBox1.Text.ToString()),double.Parse(this.textBox3.Text.ToString()));
            List<List<ProxiEdge>> PatternEdgeList = pg.LinearPatternDetected3(map, pg.PgforBuildingEdgesList, pg.PgforBuildingNodesList, double.Parse(this.textBox2.Text.ToString()), double.Parse(this.textBox1.Text.ToString()), double.Parse(this.textBox3.Text.ToString()), double.Parse(this.textBox4.Text.ToString()), double.Parse(this.textBox6.Text.ToString()), double.Parse(this.textBox5.Text.ToString()));
            pg.EdgeforPattern(PatternEdgeList);

            //裁剪pattern
            //List<List<ProxiNode>> PatternNodes = pg.LinearPatternRefinement1(PatternEdgeList);
            //pg.NodesforPattern(PatternNodes);
            
            //相似建筑物pattern
            //List<List<ProxiNode>> PatternNodes = pg.LinearPatternRefinement2(PatternEdgeList, map);
            //List<List<ProxiNode>> rPatternNodes = pg.LinearPatternRefinement1(PatternNodes);
            //pg.EdgeforPattern(rPatternNodes);

            //pg.LinearPatternDetected(pg.PgforBuildingEdgesList, pg.PgforBuildingNodesList, double.Parse(this.textBox2.Text.ToString()), double.Parse(this.textBox1.Text.ToString()), double.Parse(this.textBox3.Text.ToString()));
            #endregion

            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "LinearPattern", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.LinearEdges); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "MSTShortest", pMap.SpatialReference, pg.MSTBuildingNodesListShortestDistance, pg.MSTBuildingEdgesListShortestDistance); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "RNGShortest", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.RNGBuildingEdgesListShortestDistance); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "LinearPattern", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.PgforBuildingPatternList); }
            //裁剪pattern输出
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "rLinearPattern", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.PgforRefineBuildingPatternList); }
            //相似pattern输出
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "rLinearPattern", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.PgforRefineSimilarBuildingPatternList); }
            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "邻近图", pMap.SpatialReference, pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "邻近图", pMap.SpatialReference, pg.NodeList, pg.EdgeList); }
        }
        #endregion

        #region 初始化
        private void LinearPattern_Load(object sender, EventArgs e)
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

        #region 邻近图与RNG输出路径
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
        #endregion        

        #region 计算两条直线是否满足局部距离差异
        public bool DistanceConstrain(ProxiEdge pline1, ProxiEdge pline2)
        {
            bool label = false;
            double DistanceConstraint = double.Parse(this.textBox2.Text.ToString());//距离差异
            double Length1 = pline1.NearestEdge.NearestDistance;
            double Length2 = pline2.NearestEdge.NearestDistance;

            if (Length1 > Length2 && Length1 / Length2 < DistanceConstraint)
            {
                label = true;
            }

            if (Length1 < Length2 && Length2 / Length1 < DistanceConstraint)
            {
                label = true;
            }

            return label;
        }
        #endregion

        #region 计算两条直线是否满足局部方向差异
        public bool OrientationConstrain(ProxiEdge Pline1, ProxiEdge Pline2)
        {
            bool label = false;
            double OrientationConstraint = double.Parse(this.textBox1.Text.ToString());//局部方向差异

            ILine pline1 = new LineClass(); ILine pline2 = new LineClass();
            IPoint Point11=new PointClass();IPoint Point12=new PointClass();
            IPoint Point21=new PointClass();IPoint Point22=new PointClass();

            Point11.X=Pline1.Node1.X;Point11.Y=Pline1.Node1.Y;
            Point12.X=Pline1.Node2.X;Point12.Y=Pline1.Node2.Y;
            Point21.X=Pline2.Node1.X;Point21.Y=Pline2.Node1.Y;
            Point22.X=Pline2.Node2.X;Point22.Y=Pline2.Node2.Y;

            pline1.FromPoint = Point11; pline1.ToPoint = Point12;
            pline2.FromPoint = Point21; pline2.ToPoint = Point22;

            double angle1 = pline1.Angle;
            double angle2 = pline2.Angle;

            #region 将angle装换到0-180
            double Pi = 4 * Math.Atan(1);
            double dAngle1Degree = (180 * angle1) / Pi;
            double dAngle2Degree = (180 * angle2) / Pi;

            if (dAngle1Degree < 0)
            {
                dAngle1Degree = dAngle1Degree + 180;
            }

            if (dAngle2Degree < 0)
            {
                dAngle2Degree = dAngle2Degree + 180;
            }
            #endregion

            if (Math.Abs(dAngle1Degree - dAngle2Degree) < 90 && Math.Abs(dAngle1Degree - dAngle2Degree) < OrientationConstraint)
            {
                label = true;
            }

            if (Math.Abs(dAngle1Degree - dAngle2Degree) > 90 && Math.Abs(180 - Math.Abs(dAngle1Degree - dAngle2Degree)) < OrientationConstraint)
            {
                label = true;
            }

            return label;
        }
        #endregion

        #region 获得某条RNG边对应的连接边
        public List<ProxiEdge> ReturnEdgeList(List<ProxiEdge> PeList,ProxiEdge Pe)
        {
            List<ProxiEdge> EdgeList = new List<ProxiEdge>();

            ProxiNode Node1 = Pe.Node1; ProxiNode Node2 = Pe.Node2;
            for (int i = 0; i < PeList.Count; i++)
            {
                ProxiNode pNode1 = PeList[i].Node1; ProxiNode pNode2 = PeList[i].Node2;
                if (Node1.X == pNode1.X && Node2.X != pNode2.X)
                {
                    EdgeList.Add(PeList[i]);
                }

                if (Node2.X == pNode1.X && Node1.X != pNode2.X)
                {
                    EdgeList.Add(PeList[i]);
                }

                if (pNode2.X == Node2.X && pNode1.X != Node1.X)
                {
                    EdgeList.Add(PeList[i]);
                }

                if (pNode2.X == Node1.X && pNode1.X != Node2.X)
                {
                    EdgeList.Add(PeList[i]);
                }
            }

            return EdgeList;
        }
        #endregion

        #region 重构Pattern的邻近关系
        private void button3_Click(object sender, EventArgs e)
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
            foreach (PolygonObject Po in map.PolygonList)
            {
                Po.MBRO = PC.GetSMBROrientation(PC.ObjectConvert(Po));
                Po.tArea = PC.GetArea(PC.ObjectConvert(Po));
                Po.EdgeCount = Po.PointList.Count;
            }

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

            //pg.CreateMSTForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            //pg.CreateRNGForBuildingsShortestDistance(pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList);
            //pg.DeleteRepeatedEdge(pg.RNGBuildingEdgesListShortestDistance);

            //pg.LinearPatternDetected1(pg.RNGBuildingEdgesListShortestDistance, pg.RNGBuildingNodesListShortestDistance, double.Parse(this.textBox2.Text.ToString()), double.Parse(this.textBox1.Text.ToString()),double.Parse(this.textBox3.Text.ToString()));
            List<List<ProxiEdge>> PatternEdgeList = pg.LinearPatternDetected4(map, pg.PgforBuildingEdgesList, pg.PgforBuildingNodesList, double.Parse(this.textBox2.Text.ToString()), double.Parse(this.textBox1.Text.ToString()), double.Parse(this.textBox3.Text.ToString()), double.Parse(this.textBox4.Text.ToString()), double.Parse(this.textBox6.Text.ToString()), double.Parse(this.textBox5.Text.ToString()), double.Parse(this.textBox7.Text.ToString()));
            pg.EdgeforPattern(PatternEdgeList);

            //裁剪pattern
            //List<List<ProxiNode>> PatternNodes = pg.LinearPatternRefinement1(PatternEdgeList);
            //pg.NodesforPattern(PatternNodes);

            //相似建筑物pattern
            //List<List<ProxiNode>> PatternNodes = pg.LinearPatternRefinement2(PatternEdgeList, map);
            //List<List<ProxiNode>> rPatternNodes = pg.LinearPatternRefinement1(PatternNodes);
            //pg.EdgeforPattern(rPatternNodes);

            //pg.LinearPatternDetected(pg.PgforBuildingEdgesList, pg.PgforBuildingNodesList, double.Parse(this.textBox2.Text.ToString()), double.Parse(this.textBox1.Text.ToString()), double.Parse(this.textBox3.Text.ToString()));
            #endregion

            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "LinearPattern", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.LinearEdges); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "MSTShortest", pMap.SpatialReference, pg.MSTBuildingNodesListShortestDistance, pg.MSTBuildingEdgesListShortestDistance); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "RNGShortest", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.RNGBuildingEdgesListShortestDistance); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "LinearPattern", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.PgforBuildingPatternList); }
            //裁剪pattern输出
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "rLinearPattern", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.PgforRefineBuildingPatternList); }
            //相似pattern输出
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "rLinearPattern", pMap.SpatialReference, pg.RNGBuildingNodesListShortestDistance, pg.PgforRefineSimilarBuildingPatternList); }
            if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "邻近图", pMap.SpatialReference, pg.PgforBuildingNodesList, pg.PgforBuildingEdgesList); }
            //if (OutPath != null) { pg.WriteProxiGraph2Shp(OutPath, "邻近图", pMap.SpatialReference, pg.NodeList, pg.EdgeList); }
        }
        #endregion
    }
}
