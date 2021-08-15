using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;

using AuxStructureLib;
using AuxStructureLib.IO;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;
using System.Text.RegularExpressions;

namespace PrDispalce.建筑物聚合
{
    public partial class ProgressiveAggregation : Form
    {
        public ProgressiveAggregation(IMap cMap, AxMapControl pMapControl)
        {
            InitializeComponent();
            this.pMap = cMap;
            this.pMapControl = pMapControl;
        }

        #region 参数
        IMap pMap;
        AxMapControl pMapControl;
        string OutPath;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        PolygonSimplify PS = new PolygonSimplify();
        PolygonPreprocess PP = new PolygonPreprocess();
        TinUpdate TU = new TinUpdate();
        TriangleProcess TP = new TriangleProcess();
        double Pi = 3.1415926;
        PrDispalce.典型化.Symbolization Symbol = new 典型化.Symbolization();
        Dictionary<string, double> ScoreDic = new Dictionary<string, double>();//分数表
        #endregion

        /// <summary>
        /// 无道路的建筑物三角网生成测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
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

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            //map.InterpretatePoint2(1);//按照平均距离内插，2表示取平均距离的1/2作为阈值
            #endregion

            #region 生成限制性三角网（去除多边形内部的三角形）
            DelaunayTin dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(map.PolylineList, map.PolygonList);

            Triangle.WriteID(dt.TriangleList);//给三角形编号
            TriEdge.WriteID(dt.TriEdgeList);//给边编号

            AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
            //ske.PreProcessCDTforPLP(false);


            #endregion

            #region 删除两个节点的测试
            //List<TriNode> NodeList = new List<TriNode>(); NodeList.Add(cdt.TriNodeList[0]); 
            //List<Triangle> TriangleList = TU.GetProcessTriangle(NodeList, cdt);
            //List<TriEdge> EdgeList = TU.GetProcessEdge(TriangleList, cdt);
            //TU.DeleteDelaunay2(NodeList, EdgeList, TriangleList, cdt);
            //TU.ReBuildDelaunay2(EdgeList, cdt);
            #endregion

            #region 输出
            if (OutPath != null) { ske.CDT.DT.WriteShp(OutPath, pMap.SpatialReference); }
            //if (OutPath != null) { dt.WriteShp(OutPath, pMap.SpatialReference); }
            #endregion
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProgressiveAggregation_Load(object sender, EventArgs e)
        {
            //this.GetScoreBook();//获取得分表
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
                        this.comboBox4.Items.Add(strLayerName);
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
        /// 建筑物简化测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
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

            #region 数据读取并内插
            SMap map = new SMap(list);
            SMap map2 = new SMap();
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region 最短边测试
            //double ShortDis = 0;
            //List<TriNode> NodesList = PP.GetShortestEdge(map.PolygonList[0], out ShortDis);
            //PolylineObject CachePolyline = new PolylineObject(); CachePolyline.PointList = NodesList;
            //map.PolylineList.Add(CachePolyline);
            #endregion

            #region 结构识别测试
            //List<BasicStruct> BasicStructList = PP.GetStructedNodes(map.PolygonList[0], Pi / 18);
            //for (int i = 0; i < BasicStructList.Count; i++)
            //{
            //    PolylineObject CachePolyline = new PolylineObject();
            //    CachePolyline.PointList = BasicStructList[i].NodeList;
            //    CachePolyline.ID = BasicStructList[i].Type;
            //    map.PolylineList.Add(CachePolyline);
            //}
            #endregion

            #region 一次简化测试
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                PP.DeleteOnLinePoint(map.PolygonList[i], Pi / 18);
                bool Label = false;
                map.PolygonList[i] = PS.PolygonSimplified2(map.PolygonList[i], map.PolygonList[i], Pi / 18, 10, 0.3, out Label);
                PP.DeleteOnLinePoint(map.PolygonList[i], Pi / 18);
            }
            #endregion

            #region 建立层次结构测试
            //for (int i = 0; i < map.PolygonList.Count; i++)
            //{
            //    List<PolygonObject> PolygonList = PS.SimplifiedLevelBuilt1(map.PolygonList[i], Pi / 18);

            //    for (int j = 0; j < PolygonList.Count; j++)
            //    {
            //        map2.PolygonList.Add(PolygonList[j]);
            //    }
            //}
            #endregion

            #region 输出
            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
            #endregion
        }

        /// <summary>
        /// 三角形分类标识测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
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

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            //map.InterpretatePoint2(5);//按照平均距离内插，2表示取平均距离的1/2作为阈值
            #endregion

            #region 生成限制性三角网（去除多边形内部的三角形）
            DelaunayTin dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(map.PolylineList, map.PolygonList);

            Triangle.WriteID(dt.TriangleList);//给三角形编号
            TriEdge.WriteID(dt.TriEdgeList);//给边编号
            #endregion

            #region 对三角形类型进行标识
            TP.LabelInOutType(cdt.TriangleList, map.PolygonList);//标记建筑物是否在建筑物内
            TP.CommonEdgeTriangleLabel(cdt.TriangleList);//获取每一个三角形共边的三角形 
            TP.LabelTriangleConnect(cdt.TriangleList);//标记每一个三角形连接的三角形个数
            TP.LabelTriangleConnectConsiderInOut(cdt.TriangleList);//标记每一个三角形连接的三角形个数（考虑建筑物的内外情况！！）
            TP.LabelBoundaryBuilding(cdt.TriangleList);//判断是否是边缘三角形
            #endregion

            #region 输出
            if (OutPath != null) { cdt.DT.WriteShp(OutPath, pMap.SpatialReference); }
            //if (OutPath != null) { dt.WriteShp(OutPath, pMap.SpatialReference); }
            #endregion
        }

        #region 输出转角
        private void button5_Click(object sender, EventArgs e)
        {
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

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                map.PolygonList[i].GetBendAngle();
            }

            #region 输出转角参数
            System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\10988\Desktop\实验\BendInfor1.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);

            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                for (int j = 0; j < map.PolygonList[i].BendAngle.Count; j++)
                {
                    sw.Write(map.PolygonList[i].BendAngle[j][0] + "+" + map.PolygonList[i].BendAngle[j][1]);
                    sw.Write("\r\n");
                }

                sw.Write("\r\n");
            }

            sw.Close();
            fs.Close();
            #endregion

        }
        #endregion

        #region 独立可处理单元测试(只获取连接同一个侵入或侵出的三角形)
        private void button6_Click(object sender, EventArgs e)
        {
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

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();

            SMap map2 = new SMap(list);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            //map2.InterpretatePoint2(5);//按照平均距离内插，2表示取平均距离的1/2作为阈值
            #endregion

            #region 生成限制性三角网（去除多边形内部的三角形）
            DelaunayTin dt = new DelaunayTin(map2.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(map2.PolylineList, map2.PolygonList);

            Triangle.WriteID(dt.TriangleList);//给三角形编号
            TriEdge.WriteID(dt.TriEdgeList);//给边编号
            #endregion

            #region 三角形类型标识
            TP.LabelInOutType(cdt.TriangleList, map.PolygonList);//标记建筑物是否在建筑物内
            TP.CommonEdgeTriangleLabel(cdt.TriangleList);//获取每一个三角形共边的三角形 
            TP.LabelTriangleConnect(cdt.TriangleList);//标记每一个三角形连接的三角形个数
            TP.LabelTriangleConnectConsiderInOut(cdt.TriangleList);//标记每一个三角形连接的三角形个数（考虑建筑物的内外情况！！）
            TP.LabelBoundaryBuilding(cdt.TriangleList);//判断是否是边缘三角形
            #endregion

            #region 计算转角
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                map.PolygonList[i].GetBendAngle();
            }
            #endregion

            #region 三角形分类
            List<List<Triangle>> TriangleClusterList = new List<List<Triangle>>();
            Dictionary<List<TriNode>, int> ConvexDic = new Dictionary<List<TriNode>, int>();
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                ConvexDic = TP.GetConvexDic(map.PolygonList[i]);//获得凹部与凸部的节点
                TriangleClusterList = TP.GetTriangleCluster2(map.PolygonList[i], cdt.TriangleList);
            }
            #endregion

            #region 输出三角形
            List<Triangle> OutTriangle = new List<Triangle>();
            for (int i = 0; i < TriangleClusterList.Count; i++)
            {
                for (int j = 0; j < TriangleClusterList[i].Count; j++)
                {
                    OutTriangle.Add(TriangleClusterList[i][j]);
                }
            }

            Triangle.Create_WriteTriange2Shp(OutPath, @"Triangle", OutTriangle, pMap.SpatialReference);
            #endregion

            #region 输出点
            int test = 0;
            foreach (KeyValuePair<List<TriNode>, int> kv in ConvexDic)
            {
                TriNode.Create_WriteVetex2Shp(OutPath, @"Vextex" + test.ToString(), kv.Key, pMap.SpatialReference);
                test++;
            }
            #endregion
        }
        #endregion

        #region 独立可处理单元测试(不保证是凸多边形)
        private void button7_Click(object sender, EventArgs e)
        {
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

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();

            SMap map2 = new SMap(list);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            //map2.InterpretatePoint2(5);//按照平均距离内插，2表示取平均距离的1/2作为阈值
            #endregion

            #region 生成限制性三角网（去除多边形内部的三角形）
            DelaunayTin dt = new DelaunayTin(map2.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(map2.PolylineList, map2.PolygonList);

            Triangle.WriteID(dt.TriangleList);//给三角形编号
            TriEdge.WriteID(dt.TriEdgeList);//给边编号
            #endregion

            #region 三角形类型标识
            TP.LabelInOutType(cdt.TriangleList, map.PolygonList);//标记建筑物是否在建筑物内
            TP.CommonEdgeTriangleLabel(cdt.TriangleList);//获取每一个三角形共边的三角形 
            TP.LabelTriangleConnect(cdt.TriangleList);//标记每一个三角形连接的三角形个数
            TP.LabelTriangleConnectConsiderInOut(cdt.TriangleList);//标记每一个三角形连接的三角形个数（考虑建筑物的内外情况！！）
            TP.LabelBoundaryBuilding(cdt.TriangleList);//判断是否是边缘三角形
            #endregion

            #region 计算转角
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                map.PolygonList[i].GetBendAngle();
            }
            #endregion

            #region 三角形分类
            List<List<Triangle>> TriangleClusterList = new List<List<Triangle>>();
            Dictionary<List<TriNode>, int> ConvexDic = new Dictionary<List<TriNode>, int>();
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                ConvexDic = TP.GetConvexDic(map.PolygonList[i]);//获得凹部与凸部的节点
                TriangleClusterList = TP.GetTriangleCluster3(map.PolygonList[i], cdt.TriangleList);
            }
            #endregion

            #region 输出三角形
            List<Triangle> OutTriangle = new List<Triangle>();
            for (int i = 0; i < TriangleClusterList.Count; i++)
            {
                for (int j = 0; j < TriangleClusterList[i].Count; j++)
                {
                    OutTriangle.Add(TriangleClusterList[i][j]);
                }
            }

            Triangle.Create_WriteTriange2Shp(OutPath, @"Triangle", OutTriangle, pMap.SpatialReference);
            #endregion

            #region 输出点
            int test = 0;
            foreach (KeyValuePair<List<TriNode>, int> kv in ConvexDic)
            {
                TriNode.Create_WriteVetex2Shp(OutPath, @"Vextex" + test.ToString(), kv.Key, pMap.SpatialReference);
                test++;
            }
            #endregion
        }
        #endregion

        #region 独立可处理单元测试(保证是凸多边形)
        private void button8_Click(object sender, EventArgs e)
        {
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

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();

            SMap map2 = new SMap(list);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            //map2.InterpretatePoint2(5);//按照平均距离内插，2表示取平均距离的1/2作为阈值
            #endregion

            #region 生成限制性三角网（去除多边形内部的三角形）
            DelaunayTin dt = new DelaunayTin(map2.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(map2.PolylineList, map2.PolygonList);

            Triangle.WriteID(dt.TriangleList);//给三角形编号
            TriEdge.WriteID(dt.TriEdgeList);//给边编号
            #endregion

            #region 三角形类型标识
            TP.LabelInOutType(cdt.TriangleList, map.PolygonList);//标记建筑物是否在建筑物内
            TP.CommonEdgeTriangleLabel(cdt.TriangleList);//获取每一个三角形共边的三角形 
            TP.LabelTriangleConnect(cdt.TriangleList);//标记每一个三角形连接的三角形个数
            TP.LabelTriangleConnectConsiderInOut(cdt.TriangleList);//标记每一个三角形连接的三角形个数（考虑建筑物的内外情况！！）
            TP.LabelBoundaryBuilding(cdt.TriangleList);//判断是否是边缘三角形
            #endregion

            #region 计算转角
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                map.PolygonList[i].GetBendAngle();
            }
            #endregion

            #region 三角形分类
            List<List<Triangle>> TriangleClusterList = new List<List<Triangle>>();
            Dictionary<List<TriNode>, int> ConvexDic = new Dictionary<List<TriNode>, int>();
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                ConvexDic = TP.GetConvexDic(map.PolygonList[i]);//获得凹部与凸部的节点
                TriangleClusterList = TP.GetTriangleCluster5(map.PolygonList[i], cdt.TriangleList);
            }
            #endregion

            #region 输出三角形
            List<Triangle> OutTriangle = new List<Triangle>();
            for (int i = 0; i < TriangleClusterList.Count; i++)
            {
                for (int j = 0; j < TriangleClusterList[i].Count; j++)
                {
                    OutTriangle.Add(TriangleClusterList[i][j]);
                }
            }

            Triangle.Create_WriteTriange2Shp(OutPath, @"Triangle", OutTriangle, pMap.SpatialReference);
            #endregion

            #region 输出点
            int test = 0;
            foreach (KeyValuePair<List<TriNode>, int> kv in ConvexDic)
            {
                TriNode.Create_WriteVetex2Shp(OutPath, @"Vextex" + test.ToString(), kv.Key, pMap.SpatialReference);
                test++;
            }
            #endregion
        }
        #endregion

        #region 建筑物预处理测试
        private void button9_Click(object sender, EventArgs e)
        {
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

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region 预处理
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                PP.DeleteOnLinePoint(map.PolygonList[i], 0.26);
                PP.DeleteSmallAngle(map.PolygonList[i], 0.26);
            }
            #endregion

            #region 输出
            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
            #endregion
        }
        #endregion

        #region 大数据集处理
        private void button10_Click(object sender, EventArgs e)
        {
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

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region 简化
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                #region 符号化
                bool Label = false;
                map.PolygonList[i] = Symbol.SymbolizedPolygon(map.PolygonList[i], 24000, 0.7, 0.5, out Label);
                #endregion

                #region 简化
                if (!Label)
                {
                    PolygonObject CachePolygon1 = map.PolygonList[i];
                    PP.DeleteOnLinePoint(CachePolygon1, Pi / 72);
                    PP.DeleteSamePoint(CachePolygon1, 0.01);

                    PolygonObject CachePolygon2 = CachePolygon1;
                    double ShortDis = 0;
                    List<TriNode> ShortestEdge = PP.GetShortestEdge(CachePolygon1, out ShortDis);

                    while (ShortDis < 7.2 && CachePolygon1.PointList.Count > 4)
                    {
                        bool sLabel = false;
                        CachePolygon1 = PS.PolygonSimplified2(map.PolygonList[i], CachePolygon1, Pi / 18, 7.2, 0.3, out sLabel);
                        if (CachePolygon1 != null)
                        {
                            ShortestEdge = PP.GetShortestEdge(CachePolygon1, out ShortDis);
                            PP.DeleteOnLinePoint(CachePolygon1, Pi / 72);
                            PP.DeleteSamePoint(CachePolygon1, 0.01);
                            CachePolygon2 = CachePolygon1;
                        }
                        else
                        {
                            ShortDis = 100000;

                            if (sLabel)
                            {
                                CachePolygon2.SimLabel = 1;
                            }
                        }
                    }

                    map.PolygonList[i] = CachePolygon2;
                }
                #endregion

            }
            #endregion

            #region 输出
            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
            #endregion
        }
        #endregion

        #region 输出编码
        private void button11_Click(object sender, EventArgs e)
        {
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

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region 计算转角
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                map.PolygonList[i].GetBendAngle();
            }
            #endregion

            #region 输出编码
            System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\10988\Desktop\实验\BendInfor.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);

            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                for (int j = 0; j < map.PolygonList[i].BendAngle.Count; j++)
                {
                    #region 获得前后三个角
                    double Angle0 = 0;
                    if (j == 0)
                    {
                        Angle0 = map.PolygonList[i].BendAngle[map.PolygonList[i].BendAngle.Count - 1][1];
                    }

                    else
                    {
                        Angle0 = map.PolygonList[i].BendAngle[j - 1][1];
                    }
                    int Label0 = this.GetLabel(Angle0);

                    double Angle1 = map.PolygonList[i].BendAngle[j][1];
                    int Label1 = this.GetLabel(Angle1);

                    double Angle2 = 0;
                    if (j == map.PolygonList[i].BendAngle.Count - 1)
                    {
                        Angle2 = map.PolygonList[i].BendAngle[0][1];
                    }

                    else
                    {
                        Angle2 = map.PolygonList[i].BendAngle[j + 1][1];
                    }
                    int Label2 = this.GetLabel(Angle2);
                    #endregion

                    #region 编码1（是一个直角）
                    if ((Label1 == 0 || Label1 == 3) && (Label0 != 0 && Label0 != 3) && (Label2 != 0 && Label2 != 3))
                    {
                        if (Label1 == 0)
                        { sw.Write("D"); }
                        else if (Label1 == 1)
                        { sw.Write("E"); }
                        else if (Label1 == 2)
                        { sw.Write("F"); }
                        else if (Label1 == 3)
                        { sw.Write("G"); }
                        else if (Label1 == 4)
                        { sw.Write("H"); }
                        else if (Label1 == 5)
                        { sw.Write("I"); }
                    }
                    #endregion

                    #region 编码2（是一个折角）
                    if (Label1 == 1)
                    { sw.Write("E"); }
                    else if (Label1 == 2)
                    { sw.Write("F"); }
                    else if (Label1 == 4)
                    { sw.Write("H"); }
                    else if (Label1 == 5)
                    { sw.Write("I"); }
                    #endregion

                    #region 编码3（是一个特殊结构）
                    if (Label1 == 0 && Label2 == 0)
                    {
                        { sw.Write("A"); }
                    }

                    if (Label1 == 3 && Label2 == 3)
                    {
                        { sw.Write("C"); }
                    }

                    if ((Label1 == 0 && Label2 == 3) || (Label1 == 3 && Label2 == 0))
                    {
                        { sw.Write("B"); }
                    }
                    #endregion
                }

                sw.Write("\r\n");
            }

            sw.Close();
            fs.Close();
            #endregion
        }
        #endregion

        /// <summary>
        ///  获取角度的编码1（从85开始编码）
        /// </summary>
        /// <param name="Angle"></param>
        /// <returns></returns>      
        int GetLabel(double Angle)
        {
            int Label = -1;
            Angle = Angle * 180 / 3.1415926;

            if (Angle < 0)
            {
                Angle = 360 + Angle;
            }

            if (Angle >= 85 && Angle <= 95)
            {
                Label = 0;
            }

            else if (Angle > 95 && Angle < 180)
            {
                Label = 1;
            }

            else if (Angle >= 180 && Angle < 265)
            {
                Label = 2;
            }

            else if (Angle >= 265 && Angle <= 275)
            {
                Label = 3;
            }

            else if (Angle > 275 && Angle < 360)
            {
                Label = 4;
            }

            else
            {
                Label = 5;
            }

            return Label;
        }

        /// <summary>
        ///  获取角度的编码0（从85开始编码）
        /// </summary>
        /// <param name="Angle"></param>
        /// CodeType=1,直角：85-95；265-275
        /// CodeType=2,直角：75-105,255-285
        /// <returns></returns>      
        int GetLabel2(double Angle,int CodeType)
        {
            int Label = -1;
            Angle = Angle * 180 / 3.1415926;

            if (Angle < 0)
            {
                Angle = 360 + Angle;
            }

            #region 编码1
            if (CodeType == 1)
            {
                if (Angle >= 85 && Angle <= 95)
                {
                    Label = 2;
                }

                else if (Angle > 95 && Angle < 180)
                {
                    Label = 3;
                }

                else if (Angle >= 180 && Angle < 265)
                {
                    Label = 4;
                }

                else if (Angle >= 265 && Angle <= 275)
                {
                    Label = 5;
                }

                else if (Angle > 275 && Angle < 360)
                {
                    Label = 6;
                }

                else
                {
                    Label = 1;
                }
            }
            #endregion

            #region 编码2
            if (CodeType == 2)
            {
                if (Angle >= 75 && Angle <= 105)
                {
                    Label = 2;
                }

                else if (Angle > 105 && Angle < 180)
                {
                    Label = 3;
                }

                else if (Angle >= 180 && Angle < 255)
                {
                    Label = 4;
                }

                else if (Angle >= 255 && Angle <= 285)
                {
                    Label = 5;
                }

                else if (Angle > 285 && Angle < 360)
                {
                    Label = 6;
                }

                else
                {
                    Label = 1;
                }
            }
            #endregion

            return Label;
        }

        #region 生成建筑物的三角网
        private void button12_Click(object sender, EventArgs e)
        {
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

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            //map.InterpretatePoint2(1);//按照平均距离内插，2表示取平均距离的1/2作为阈值
            #endregion

            #region 生成限制性三角网（去除多边形内部的三角形）
            DelaunayTin dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(map.PolylineList, map.PolygonList);

            Triangle.WriteID(dt.TriangleList);//给三角形编号
            TriEdge.WriteID(dt.TriEdgeList);//给边编号

            AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
            //ske.PreProcessCDTforPLP(false);


            #endregion

            #region 输出
            if (OutPath != null) { ske.CDT.DT.WriteShp(OutPath, pMap.SpatialReference); }
            //if (OutPath != null) { dt.WriteShp(OutPath, pMap.SpatialReference); }
            #endregion
        }
        #endregion

        #region 输出编码不考虑转折
        private void button13_Click(object sender, EventArgs e)
        {
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

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region 计算转角
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                map.PolygonList[i].GetBendAngle2();
            }
            #endregion

            #region 输出编码
            System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\BendInfor1.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);

            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                for (int j = 0; j < map.PolygonList[i].BendAngle.Count; j++)
                {
                    #region 获得前后两个角
                    double Angle1 = map.PolygonList[i].BendAngle[j][1];
                    int Label1 = this.GetLabel2(Angle1,2);

                    double Angle2 = 0;
                    if (j == map.PolygonList[i].BendAngle.Count - 1)
                    {
                        Angle2 = map.PolygonList[i].BendAngle[0][1];
                    }

                    else
                    {
                        Angle2 = map.PolygonList[i].BendAngle[j + 1][1];
                    }
                    int Label2 = this.GetLabel2(Angle2,2);
                    #endregion

                    #region 编码
                    if (Label1 == 1)
                    {
                        switch (Label2)
                        {
                            case 1: sw.Write("A"); break;
                            case 2: sw.Write("B"); break;
                            case 3: sw.Write("C"); break;
                            case 4: sw.Write("J"); break;
                            case 5: sw.Write("K"); break;
                            case 6: sw.Write("L"); break;
                        }
                    }

                    else if (Label1 == 2)
                    {
                        switch (Label2)
                        {
                            case 1: sw.Write("D"); break;
                            case 2: sw.Write("E"); break;
                            case 3: sw.Write("F"); break;
                            case 4: sw.Write("M"); break;
                            case 5: sw.Write("N"); break;
                            case 6: sw.Write("O"); break;
                        }
                    }

                    else if (Label1 == 3)
                    {
                        switch (Label2)
                        {
                            case 1: sw.Write("G"); break;
                            case 2: sw.Write("H"); break;
                            case 3: sw.Write("I"); break;
                            case 4: sw.Write("P"); break;
                            case 5: sw.Write("Q"); break;
                            case 6: sw.Write("R"); break;
                        }
                    }

                    else if (Label1 == 4)
                    {
                        switch (Label2)
                        {
                            case 1: sw.Write("S"); break;
                            case 2: sw.Write("T"); break;
                            case 3: sw.Write("U"); break;
                            case 4: sw.Write("1"); break;
                            case 5: sw.Write("2"); break;
                            case 6: sw.Write("3"); break;
                        }
                    }

                    else if (Label1 == 5)
                    {
                        switch (Label2)
                        {
                            case 1: sw.Write("V"); break;
                            case 2: sw.Write("W"); break;
                            case 3: sw.Write("X"); break;
                            case 4: sw.Write("4"); break;
                            case 5: sw.Write("5"); break;
                            case 6: sw.Write("6"); break;
                        }
                    }

                    else if (Label1 == 6)
                    {
                        switch (Label2)
                        {
                            case 1: sw.Write("Y"); break;
                            case 2: sw.Write("Z"); break;
                            case 3: sw.Write("0"); break;
                            case 4: sw.Write("7"); break;
                            case 5: sw.Write("8"); break;
                            case 6: sw.Write("9"); break;
                        }
                    }
                    #endregion
                }

                sw.Write("\r\n");
            }

            sw.Close();
            fs.Close();
            #endregion
        }
        #endregion

        /// <summary>
        /// 读取计算相似度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button14_Click(object sender, EventArgs e)
        {
            StreamReader sr = new StreamReader(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\log_name.txt", Encoding.Default);
            String pline;
            List<string> LineList = new List<string>();
            List<List<List<string>>> GlobalMatchList = new List<List<List<string>>>();
            List<List<List<string>>> LocalMatchList = new List<List<List<string>>>();

            #region 获取所有txt
            while ((pline = sr.ReadLine()) != null)
            {
                LineList.Add(pline);
            }
            #endregion

            #region 获取queues
            int i = 0; bool pLabel = false;
            List<List<string>> GlobalMatch = new List<List<string>>();
            List<List<string>> LocalMatch = new List<List<string>>();
            List<string> pGlobalMatch = new List<string>();
            List<string> pLocalMatch = new List<string>();
            foreach (string line in LineList)
            {
                #region 开启一个新循环
                if (line == "StartLabel")
                {
                    i = 0;

                    if (pLabel)
                    {
                        GlobalMatchList.Add(GlobalMatch);
                        LocalMatchList.Add(LocalMatch);
                    }

                    GlobalMatch = new List<List<string>>();
                    LocalMatch = new List<List<string>>();//不能是清空，注意地址引用与值引用
                    pLabel = true;
                }
                #endregion

                #region 添加每一个匹配对
                if (i % 13 == 0 && i != 0 && pLabel)
                {
                    GlobalMatch.Add(pGlobalMatch);
                    LocalMatch.Add(pLocalMatch);

                    pGlobalMatch = new List<string>();
                    pLocalMatch = new List<string>();
                }

                if (i % 13 == 1 && pLabel)
                {
                    pGlobalMatch.Add(line);
                }

                if (i % 13 == 2 && pLabel)
                {
                    pGlobalMatch.Add(line);
                }

                if (i % 13 == 10 && pLabel)
                {
                    pLocalMatch.Add(line);
                }

                if (i % 13 == 11 && pLabel)
                {
                    pLocalMatch.Add(line);
                }
                #endregion

                i++;
            }
            #endregion

            #region 对localMatch的调整
            foreach (List<List<string>> mLocalMatch in LocalMatchList)
            {
                foreach (List<string> nLocalMatch in mLocalMatch)
                {
                    int Index1 = nLocalMatch[0].IndexOf("[");
                    string M1 = nLocalMatch[0].Substring(2, Index1 - 4);

                    int Index2 = nLocalMatch[1].IndexOf("[");
                    string M2 = nLocalMatch[1].Substring(2, Index2 - 4);

                    nLocalMatch[0] = M1;
                    nLocalMatch[1] = M2;
                }
            }
            #endregion

            //int TestLocation = 0;

            #region 图形编码
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }

            List<IFeatureLayer> list2 = new List<IFeatureLayer>();
            if (this.comboBox4.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list2.Add(BuildingLayer);
            }

            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();

            SMap map2 = new SMap(list2);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();

            Dictionary<List<string>, List<List<double>>> SourceBuildingCode = this.GetCode(map.PolygonList,1);//长度归一化编码
            Dictionary<List<string>, List<List<double>>> TargetBuildingCode = this.GetCode(map2.PolygonList,1);
            #endregion

            //int TestLocation = 0;

            List<List<List<double>>> GlobalSim = this.GetSimilarity(GlobalMatchList,LocalMatchList,SourceBuildingCode,TargetBuildingCode,1,1,1);//计算最大和最小的相似
            List<List<List<double>>> LocalSim = this.GetSimilarity(GlobalMatchList, LocalMatchList, SourceBuildingCode, TargetBuildingCode, 1, 2,1);//计算最大和最小的相似

            #region 获取每一个匹配对下的最大值
            List<List<double>> pGlobalSim = new List<List<double>>();
            List<List<double>> pLocalSim = new List<List<double>>();

            for (int k = 0; k < GlobalSim.Count; k++)
            {
                List<double> pCacheGlobal = new List<double>();
                List<double> pCacheLocal = new List<double>();
                for (int p = 0; p < GlobalSim[k].Count; p++)
                {
                    pCacheGlobal.Add(GlobalSim[k][p].Max());
                    pCacheLocal.Add(LocalSim[k][p].Max());
                }
                pGlobalSim.Add(pCacheGlobal);
                pLocalSim.Add(pCacheLocal);
            }
            #endregion

            #region 输出
            System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\Simlarity.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);
            
            sw.Write("GlobalSim");
            sw.Write("\r\n");
            for (int k = 0; k < pGlobalSim.Count; k++)
            {
                for (int p = 0; p < pGlobalSim[k].Count; p++)
                {
                    sw.Write(pGlobalSim[k][p].ToString());
                    sw.Write("\r\n");
                }
                sw.Write("\r\n");
            }

            sw.Write("\r\n");
            sw.Write("LocalSim");
            sw.Write("\r\n");
            for (int k = 0; k < pLocalSim.Count; k++)
            {
                for (int p = 0; p < pLocalSim[k].Count; p++)
                {
                    sw.Write(pLocalSim[k][p].ToString());
                    sw.Write("\r\n");
                }
                sw.Write("\r\n");
            }

            sw.Close();
            fs.Close();
            #endregion

            //TestLocation = 0;
        }

        #region 输出编码；记录长度
        private void button15_Click(object sender, EventArgs e)
        {
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

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region 计算转角
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                map.PolygonList[i].GetBendAngle2();
            }
            #endregion

            #region 输出编码
            System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\BendInforLen.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);

            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                for (int j = 0; j < map.PolygonList[i].BendAngle.Count; j++)
                {
                    #region 获得前后两个角和长度
                    double Length2 = map.PolygonList[i].BendAngle[j][0];
                    double Angle1 = map.PolygonList[i].BendAngle[j][1];
                    int Label1 = this.GetLabel2(Angle1,2);

                    double Angle2 = 0; double Length3 = 0;
                    if (j == map.PolygonList[i].BendAngle.Count - 1)
                    {
                        Angle2 = map.PolygonList[i].BendAngle[0][1];
                        Length3 = map.PolygonList[i].BendAngle[0][0];
                    }

                    else
                    {
                        Angle2 = map.PolygonList[i].BendAngle[j + 1][1];
                        Length3 = map.PolygonList[i].BendAngle[j + 1][0];
                    }

                    double Length1 = 0;
                    if (j == 0)
                    {
                        Length1 = map.PolygonList[i].BendAngle[map.PolygonList[i].BendAngle.Count - 1][0];
                    }

                    else
                    {
                        Length1 = map.PolygonList[i].BendAngle[j - 1][0];
                    }
                    int Label2 = this.GetLabel2(Angle2,2);
                    #endregion

                    #region 编码
                    if (Label1 == 1)
                    {
                        switch (Label2)
                        {
                            case 1: sw.Write("A"); break;
                            case 2: sw.Write("B"); break;
                            case 3: sw.Write("C"); break;
                            case 4: sw.Write("J"); break;
                            case 5: sw.Write("K"); break;
                            case 6: sw.Write("L"); break;
                        }
                    }

                    else if (Label1 == 2)
                    {
                        switch (Label2)
                        {
                            case 1: sw.Write("D"); break;
                            case 2: sw.Write("E"); break;
                            case 3: sw.Write("F"); break;
                            case 4: sw.Write("M"); break;
                            case 5: sw.Write("N"); break;
                            case 6: sw.Write("O"); break;
                        }
                    }

                    else if (Label1 == 3)
                    {
                        switch (Label2)
                        {
                            case 1: sw.Write("G"); break;
                            case 2: sw.Write("H"); break;
                            case 3: sw.Write("I"); break;
                            case 4: sw.Write("P"); break;
                            case 5: sw.Write("Q"); break;
                            case 6: sw.Write("R"); break;
                        }
                    }

                    else if (Label1 == 4)
                    {
                        switch (Label2)
                        {
                            case 1: sw.Write("S"); break;
                            case 2: sw.Write("T"); break;
                            case 3: sw.Write("U"); break;
                            case 4: sw.Write("1"); break;
                            case 5: sw.Write("2"); break;
                            case 6: sw.Write("3"); break;
                        }
                    }

                    else if (Label1 == 5)
                    {
                        switch (Label2)
                        {
                            case 1: sw.Write("V"); break;
                            case 2: sw.Write("W"); break;
                            case 3: sw.Write("X"); break;
                            case 4: sw.Write("4"); break;
                            case 5: sw.Write("5"); break;
                            case 6: sw.Write("6"); break;
                        }
                    }

                    else if (Label1 == 6)
                    {
                        switch (Label2)
                        {
                            case 1: sw.Write("Y"); break;
                            case 2: sw.Write("Z"); break;
                            case 3: sw.Write("0"); break;
                            case 4: sw.Write("7"); break;
                            case 5: sw.Write("8"); break;
                            case 6: sw.Write("9"); break;
                        }
                    }
                    #endregion

                    sw.Write("(" + Length1.ToString() + "," + Length2.ToString() + "," + Length3.ToString() + ")");
                }

                sw.Write("\r\n");
            }

            sw.Close();
            fs.Close();
            #endregion
        }
        #endregion

        /// <summary>
        /// 获取序列的编码（这里存在一个小问题，也就是说Diction中的内容key必须不同，这一点在建筑物形状相同时无法有效保证）
        /// </summary>
        /// <param name="PolygonList"></param>
        /// Label长度编码方式；Label=1长度归一化编码；Label=2长度不归一化编码
        /// <returns></returns>
        Dictionary<List<string>, List<List<double>>> GetCode(List<PolygonObject> PolygonList,int Label)
        {
            Dictionary<List<string>, List<List<double>>> Code = new Dictionary<List<string>, List<List<double>>>();

            #region 计算转角
            for (int i = 0; i < PolygonList.Count; i++)
            {
                PolygonList[i].GetBendAngle2();
            }
            #endregion

            #region 输出编码
            for (int i = 0; i < PolygonList.Count; i++)
            {
                List<string> AngleCode = new List<string>();
                List<List<double>> LengthCode = new List<List<double>>();
                for (int j = 0; j < PolygonList[i].BendAngle.Count; j++)
                {
                    #region 获得前后两个角和长度
                    double Length2 = PolygonList[i].BendAngle[j][0];
                    double Angle1 = PolygonList[i].BendAngle[j][1];
                    int Label1 = this.GetLabel2(Angle1,2);

                    double Angle2 = 0; double Length3 = 0;
                    if (j == PolygonList[i].BendAngle.Count - 1)
                    {
                        Angle2 = PolygonList[i].BendAngle[0][1];
                        Length3 = PolygonList[i].BendAngle[0][0];
                    }

                    else
                    {
                        Angle2 = PolygonList[i].BendAngle[j + 1][1];
                        Length3 = PolygonList[i].BendAngle[j + 1][0];
                    }

                    double Length1 = 0;
                    if (j == 0)
                    {
                        Length1 = PolygonList[i].BendAngle[PolygonList[i].BendAngle.Count - 1][0];
                    }

                    else
                    {
                        Length1 = PolygonList[i].BendAngle[j - 1][0];
                    }
                    int Label2 = this.GetLabel2(Angle2,2);
                    #endregion

                    #region 编码
                    if (Label1 == 1)
                    {
                        switch (Label2)
                        {
                            case 1: AngleCode.Add("A"); break;
                            case 2: AngleCode.Add("B"); break;
                            case 3: AngleCode.Add("C"); break;
                            case 4: AngleCode.Add("J"); break;
                            case 5: AngleCode.Add("K"); break;
                            case 6: AngleCode.Add("L"); break;
                        }
                    }

                    else if (Label1 == 2)
                    {
                        switch (Label2)
                        {
                            case 1: AngleCode.Add("D"); break;
                            case 2: AngleCode.Add("E"); break;
                            case 3: AngleCode.Add("F"); break;
                            case 4: AngleCode.Add("M"); break;
                            case 5: AngleCode.Add("N"); break;
                            case 6: AngleCode.Add("O"); break;
                        }
                    }

                    else if (Label1 == 3)
                    {
                        switch (Label2)
                        {
                            case 1: AngleCode.Add("G"); break;
                            case 2: AngleCode.Add("H"); break;
                            case 3: AngleCode.Add("I"); break;
                            case 4: AngleCode.Add("P"); break;
                            case 5: AngleCode.Add("Q"); break;
                            case 6: AngleCode.Add("R"); break;
                        }
                    }

                    else if (Label1 == 4)
                    {
                        switch (Label2)
                        {
                            case 1: AngleCode.Add("S"); break;
                            case 2: AngleCode.Add("T"); break;
                            case 3: AngleCode.Add("U"); break;
                            case 4: AngleCode.Add("1"); break;
                            case 5: AngleCode.Add("2"); break;
                            case 6: AngleCode.Add("3"); break;
                        }
                    }

                    else if (Label1 == 5)
                    {
                        switch (Label2)
                        {
                            case 1: AngleCode.Add("V"); break;
                            case 2: AngleCode.Add("W"); break;
                            case 3: AngleCode.Add("X"); break;
                            case 4: AngleCode.Add("4"); break;
                            case 5: AngleCode.Add("5"); break;
                            case 6: AngleCode.Add("6"); break;
                        }
                    }

                    else if (Label1 == 6)
                    {
                        switch (Label2)
                        {
                            case 1: AngleCode.Add("Y"); break;
                            case 2: AngleCode.Add("Z"); break;
                            case 3: AngleCode.Add("0"); break;
                            case 4: AngleCode.Add("7"); break;
                            case 5: AngleCode.Add("8"); break;
                            case 6: AngleCode.Add("9"); break;
                        }
                    }

                    //添加长度特征
                    List<double> subLengthCode = new List<double>();
                    if (Label == 1)//长度归一化编码
                    {
                        subLengthCode.Add(Length1 / (PolygonList[i].Perimeter*3)); subLengthCode.Add(Length2 / (PolygonList[i].Perimeter*3)); subLengthCode.Add(Length3 / (PolygonList[i].Perimeter*3));
                    }
                    else if (Label == 2)//长度不归一化编码
                    {
                        subLengthCode.Add(Length1); subLengthCode.Add(Length2); subLengthCode.Add(Length3);
                    }
                    LengthCode.Add(subLengthCode);
                    #endregion
                }

                Code.Add(AngleCode, LengthCode);
            }
            #endregion

            return Code;
        }
        
        /// <summary>
        /// 对给定的建筑物进行编码，记录编码（Char Code 对应一个Char类型）和其长度特征（List<double>）
        /// </summary>
        /// <param name="Po"></param>
        /// <param name="Label"></param>Label=1表示长度归一化编码；Label=2表示长度不归一化编码
        /// <returns></returns>
        Dictionary<string, List<double>> GetCode(PolygonObject Po, int Label)
        {
            Dictionary<string, List<double>> Code = new Dictionary<string, List<double>>();
            Po.GetBendAngle2();//计算转角

            for (int j = 0; j < Po.BendAngle.Count; j++)
            {
                string AngleCode = null;//角度编码
                List<double> subLengthCode = new List<double>();//长度编码

                #region 获得前后两个角和长度
                double Length2 = Po.BendAngle[j][0];
                double Angle1 = Po.BendAngle[j][1];
                int Label1 = this.GetLabel2(Angle1, 2);

                double Angle2 = 0; double Length3 = 0;
                if (j == Po.BendAngle.Count - 1)
                {
                    Angle2 = Po.BendAngle[0][1];
                    Length3 = Po.BendAngle[0][0];
                }

                else
                {
                    Angle2 = Po.BendAngle[j + 1][1];
                    Length3 = Po.BendAngle[j + 1][0];
                }

                double Length1 = 0;
                if (j == 0)
                {
                    Length1 = Po.BendAngle[Po.BendAngle.Count - 1][0];
                }

                else
                {
                    Length1 = Po.BendAngle[j - 1][0];
                }
                int Label2 = this.GetLabel2(Angle2, 2);
                #endregion

                #region 编码
                if (Label1 == 1)
                {
                    switch (Label2)
                    {
                        case 1: AngleCode = j.ToString()+"A"; break;//添加j是为了避免dic中key相同
                        case 2: AngleCode = j.ToString()+"B"; break;
                        case 3: AngleCode = j.ToString() + "C"; break;
                        case 4: AngleCode = j.ToString() + "J"; break;
                        case 5: AngleCode = j.ToString() + "K"; break;
                        case 6: AngleCode = j.ToString() + "L"; break;
                    }
                }

                else if (Label1 == 2)
                {
                    switch (Label2)
                    {
                        case 1: AngleCode = j.ToString() + "D"; break;
                        case 2: AngleCode = j.ToString() + "E"; break;
                        case 3: AngleCode = j.ToString() + "F"; break;
                        case 4: AngleCode = j.ToString() + "M"; break;
                        case 5: AngleCode = j.ToString() + "N"; break;
                        case 6: AngleCode = j.ToString() + "O"; break;
                    }
                }

                else if (Label1 == 3)
                {
                    switch (Label2)
                    {
                        case 1: AngleCode = j.ToString() + "G"; break;
                        case 2: AngleCode = j.ToString() + "H"; break;
                        case 3: AngleCode = j.ToString() + "I"; break;
                        case 4: AngleCode = j.ToString() + "P"; break;
                        case 5: AngleCode = j.ToString() + "Q"; break;
                        case 6: AngleCode = j.ToString() + "R"; break;
                    }
                }

                else if (Label1 == 4)
                {
                    switch (Label2)
                    {
                        case 1: AngleCode = j.ToString() + "S"; break;
                        case 2: AngleCode = j.ToString() + "T"; break;
                        case 3: AngleCode = j.ToString() + "U"; break;
                        case 4: AngleCode = j.ToString() + "1"; break;
                        case 5: AngleCode = j.ToString() + "2"; break;
                        case 6: AngleCode = j.ToString() + "3"; break;
                    }
                }

                else if (Label1 == 5)
                {
                    switch (Label2)
                    {
                        case 1: AngleCode = j.ToString() + "V"; break;
                        case 2: AngleCode = j.ToString() + "W"; break;
                        case 3: AngleCode = j.ToString() + "X"; break;
                        case 4: AngleCode = j.ToString() + "4"; break;
                        case 5: AngleCode = j.ToString() + "5"; break;
                        case 6: AngleCode = j.ToString() + "6"; break;
                    }
                }

                else if (Label1 == 6)
                {
                    switch (Label2)
                    {
                        case 1: AngleCode = j.ToString() + "Y"; break;
                        case 2: AngleCode = j.ToString() + "Z"; break;
                        case 3: AngleCode = j.ToString() + "0"; break;
                        case 4: AngleCode = j.ToString() + "7"; break;
                        case 5: AngleCode = j.ToString() + "8"; break;
                        case 6: AngleCode = j.ToString() + "9"; break;
                    }
                }

                //添加长度特征
                if (Label == 1)//长度归一化编码
                {
                    subLengthCode.Add(Length1 / (Po.Perimeter * 3)); subLengthCode.Add(Length2 / (Po.Perimeter * 3)); subLengthCode.Add(Length3 / (Po.Perimeter * 3));
                }
                else if (Label == 2)//长度不归一化编码
                {
                    subLengthCode.Add(Length1); subLengthCode.Add(Length2); subLengthCode.Add(Length3);
                }
                #endregion

                Code.Add(AngleCode, subLengthCode);
            }
            return Code;
        }

        /// <summary>
        /// 获得转角函数
        /// </summary>
        /// StartLocation=由于转角函数的起点选择对于相似度计算很大；所以StartLocation标记了起算点
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        List<List<double>> GetTurningAngle(PolygonObject pPolygon,int StartLocation)
        {
            List<List<double>> TurningAngle = new List<List<double>>();

            //获取角度
            pPolygon.GetBendAngle2(); double TotalAngle = 0;
            
            for (int i = 0; i < pPolygon.BendAngle.Count; i++)
            {
                List<double> tAngleDis = new List<double>();
                List<double> oAngleDis = pPolygon.BendAngle[(StartLocation + i) % pPolygon.BendAngle.Count];

                if (i == 0)
                {
                    tAngleDis.Add(0);//添加长度
                    tAngleDis.Add(oAngleDis[0] / pPolygon.Perimeter);//添加角度
                }

                if (i != 0)
                {
                    TotalAngle = TotalAngle + oAngleDis[1];
                    tAngleDis.Add(TotalAngle % (2 * Pi));//添加角度
                    tAngleDis.Add(oAngleDis[0] / pPolygon.Perimeter+TurningAngle[i-1][1]);//添加长度
                }

                TurningAngle.Add(tAngleDis);
            }

            return TurningAngle;
        }

        /// <summary>
        /// 计算两个转角函数的相似度
        /// </summary>
        /// <param name="TurningAngle1"></param>
        /// <param name="TurningAngle2"></param>
        /// <returns></returns>
        double GetTurningSim(List<List<double>> TurningAngle1, List<List<double>> TurningAngle2)
        {
            double TurningSim = 0;

            List<double> CacheTurning1 = new List<double>();
            List<double> CacheTurning2 = new List<double>();
            int i = 0; int j = 0;
            double StartDis = 0;double EndDis = 0;

            while (Math.Abs(StartDis-1)>0.001 && Math.Abs(EndDis-1)>0.001)
            {
                CacheTurning1 = TurningAngle1[i];
                CacheTurning2 = TurningAngle2[j];

                if (CacheTurning1[1] < CacheTurning2[1])
                {
                    i++;
                    EndDis = CacheTurning1[1];

                    TurningSim = (EndDis - StartDis) * Math.Abs(CacheTurning1[0] - CacheTurning2[0])+TurningSim;
                    StartDis = CacheTurning1[1];

                    //int TestLocation = 0;
                }

                else
                {
                    j++;
                    EndDis = CacheTurning2[1];

                    TurningSim = (EndDis - StartDis) * Math.Abs(CacheTurning1[0] - CacheTurning2[0])+TurningSim;
                    StartDis = CacheTurning2[1];

                    //int TestLocaiton = 0;
                }
            }

            return TurningSim;
        }

        /// <summary>
        /// 获取给定序列的全局相似性
        /// </summary>
        /// <param name="AngleCode"></param>
        /// <param name="LengthCode"></param>
        /// <param name="GlobalMatchQueue"></param>
        /// Label=计算相似性的方法；=1表示匹配对相似取最小值；匹配对不相似取最大值
        /// =2 表示匹配对相似取重叠的距离值；匹配对不相似取最大值
        /// =3表示匹配对不考虑长度的权重
        /// GapLabel=1,gap得分相同；GapLabel=2Gapopening 得分高，缺失得分低
        /// <returns></returns>
        double getGlobalSimilarity(List<string> SourceAngleCode,List<string> TargetAngleCode, List<List<double>> SourceLengthCode,List<List<double>> TargerLengthCode,List<string> GlobalMatchQueue,int Label,int GapLabel)
        {
            double GlobalSimilarity = 0;

            int mLabel = -1; int nLabel = -1;
            for (int i = 0; i <GlobalMatchQueue[0].Length; i++)
            {
                #region 获取匹配的要素

                #region 角度编码
                string sTargetAngleCode = GlobalMatchQueue[0].ElementAt(i).ToString();//源编码
                string sSourceAngleCode = GlobalMatchQueue[1].ElementAt(i).ToString();//匹配编码
                #endregion

                #region 分数编码
                double AngleMatchScore = 0;//分数

                if (GapLabel == 1)
                {
                    if (GlobalMatchQueue[0].ElementAt(i).ToString() == "-" || GlobalMatchQueue[1].ElementAt(i).ToString() == "-")
                    {
                        AngleMatchScore = -4;
                    }
                    else
                    {
                        List<String> MatchCacheCode = new List<string>(); MatchCacheCode.Add(sTargetAngleCode); MatchCacheCode.Add(sSourceAngleCode);
                        //利用键值对匹配似乎会出问题
                        AngleMatchScore = ScoreDic[string.Join("", MatchCacheCode)];

                        //int TestLocation = 0;
                    }
                }

                else if (GapLabel == 2)
                {
                    if ((i == 0) && (GlobalMatchQueue[0].ElementAt(i).ToString() == "-" || GlobalMatchQueue[1].ElementAt(i).ToString() == "-"))
                    {
                        AngleMatchScore = -4;
                    }
                    //考虑GapExtending分数相对较小
                    else if ((i != 0) && ((GlobalMatchQueue[0].ElementAt(i).ToString() == "-" && GlobalMatchQueue[0].ElementAt(i - 1).ToString() == "-") || (GlobalMatchQueue[1].ElementAt(i).ToString() == "-" && GlobalMatchQueue[1].ElementAt(i-1).ToString() == "-")))
                    {
                        AngleMatchScore = -2;
                    }
                    else if (GlobalMatchQueue[0].ElementAt(i).ToString() == "-" || GlobalMatchQueue[1].ElementAt(i).ToString() == "-")
                    {
                        AngleMatchScore = -4;
                    }
                    else
                    {
                        List<String> MatchCacheCode = new List<string>(); MatchCacheCode.Add(sTargetAngleCode); MatchCacheCode.Add(sSourceAngleCode);
                        //利用键值对匹配似乎会出问题
                        AngleMatchScore = ScoreDic[string.Join("", MatchCacheCode)];
                    }
                }
                #endregion

                #region 获得长度编码
                List<double> sSourceLenghtCode = new List<double>();
                List<double> sTargetLengthCode = new List<double>();
                if (GlobalMatchQueue[0].ElementAt(i).ToString() != "-")
                {
                    mLabel++;                                   
                }
                if (mLabel != -1)
                {
                    sSourceLenghtCode = SourceLengthCode[mLabel];//源长度编码  
                }
                if (GlobalMatchQueue[1].ElementAt(i).ToString() != "-")
                {
                    nLabel++;                  
                }
                if (nLabel != -1)
                {
                    sTargetLengthCode = TargerLengthCode[nLabel];//匹配长度编码
                }
                #endregion
                #endregion

                #region 相似度计算
                double Sim = this.GetMatchSimilarity(sSourceLenghtCode, sTargetLengthCode, AngleMatchScore, Label);
                #endregion

                GlobalSimilarity = GlobalSimilarity + Sim;
            }

            return GlobalSimilarity;
        }

        /// <summary>
        /// 获取给定序列的全局相似性
        /// </summary>
        /// <param name="AngleCode"></param>
        /// <param name="LengthCode"></param>
        /// <param name="GlobalMatchQueue"></param>
        /// Label=计算相似性的方法；=1表示匹配对相似取最小值；匹配对不相似取最大值
        /// =2 表示匹配对相似取重叠的距离值；匹配对不相似取最大值
        /// =3表示匹配对不考虑长度的权重
        /// GapLabel=1,gap得分相同；GapLabel=2Gapopening 得分高，缺失得分低
        /// <returns></returns>
        double getGlobalSimilarity2(List<string> SourceAngleCode, List<string> TargetAngleCode, List<List<double>> SourceLengthCode, List<List<double>> TargerLengthCode, List<string> GlobalMatchQueue, int Label, int GapLabel)
        {
            double GlobalSimilarity = 0;

            int mLabel = -1; int nLabel = -1;
            for (int i = 0; i < GlobalMatchQueue[0].Length; i++)
            {
                #region 获取匹配的要素

                #region 角度编码
                string sTargetAngleCode = GlobalMatchQueue[0].ElementAt(i).ToString();//源编码
                string sSourceAngleCode = GlobalMatchQueue[1].ElementAt(i).ToString();//匹配编码
                #endregion

                #region 分数编码
                double AngleMatchScore = 0;//分数

                if (GapLabel == 1)
                {
                    if (GlobalMatchQueue[0].ElementAt(i).ToString() == "-" || GlobalMatchQueue[1].ElementAt(i).ToString() == "-")
                    {
                        AngleMatchScore = -0.4;
                    }
                    else
                    {
                        List<String> MatchCacheCode = new List<string>(); MatchCacheCode.Add(sTargetAngleCode); MatchCacheCode.Add(sSourceAngleCode);
                        //利用键值对匹配似乎会出问题
                        AngleMatchScore = ScoreDic[string.Join("", MatchCacheCode)];

                        //int TestLocation = 0;
                    }
                }

                else if (GapLabel == 2)
                {
                    if ((i == 0) && (GlobalMatchQueue[0].ElementAt(i).ToString() == "-" || GlobalMatchQueue[1].ElementAt(i).ToString() == "-"))
                    {
                        AngleMatchScore = -0.4;
                    }
                    //考虑GapExtending分数相对较小
                    else if ((i != 0) && 
                        (GlobalMatchQueue[0].ElementAt(i).ToString() == "-" && (GlobalMatchQueue[0].ElementAt(i - 1).ToString() == "-"|| GlobalMatchQueue[1].ElementAt(i - 1).ToString() == "-")) || 
                        (GlobalMatchQueue[1].ElementAt(i).ToString() == "-" && (GlobalMatchQueue[1].ElementAt(i - 1).ToString() == "-"|| GlobalMatchQueue[0].ElementAt(i - 1).ToString() == "-")))
                    {
                        AngleMatchScore = -0.25;
                    }
                    else if (GlobalMatchQueue[0].ElementAt(i).ToString() == "-" || GlobalMatchQueue[1].ElementAt(i).ToString() == "-")
                    {
                        AngleMatchScore = -0.4;
                    }
                    else
                    {
                        if (i == 0)
                        {
                            List<String> MatchCacheCode = new List<string>(); MatchCacheCode.Add(sTargetAngleCode); MatchCacheCode.Add(sSourceAngleCode);
                            //利用键值对匹配似乎会出问题
                            AngleMatchScore = ScoreDic[string.Join("", MatchCacheCode)];

                            if (AngleMatchScore < 0)
                            {
                                AngleMatchScore = -0.4;
                            }
                        }

                        else if (i != 0)
                        {
                            List<String> MatchCacheCode1 = new List<string>(); MatchCacheCode1.Add(sTargetAngleCode); MatchCacheCode1.Add(sSourceAngleCode);
                            //利用键值对匹配似乎会出问题
                            AngleMatchScore = ScoreDic[string.Join("", MatchCacheCode1)];

                            if (GlobalMatchQueue[0].ElementAt(i - 1).ToString() != "-" && GlobalMatchQueue[1].ElementAt(i - 1).ToString() != "-")
                            {
                                List<String> MatchCacheCode2 = new List<string>(); MatchCacheCode2.Add(GlobalMatchQueue[0].ElementAt(i-1).ToString()); MatchCacheCode2.Add(GlobalMatchQueue[1].ElementAt(i - 1).ToString());
                                double AngleMatchScore2 = ScoreDic[string.Join("", MatchCacheCode2)];
                                if (AngleMatchScore2 < 0 && AngleMatchScore < 0)
                                {
                                    AngleMatchScore = -0.25;
                                }
                            }
                        }
                    }
                }
                #endregion

                #region 获得长度编码
                List<double> sSourceLenghtCode = new List<double>();
                List<double> sTargetLengthCode = new List<double>();
                if (GlobalMatchQueue[0].ElementAt(i).ToString() != "-")
                {
                    mLabel++;
                }
                if (mLabel != -1)
                {
                    sSourceLenghtCode = SourceLengthCode[mLabel];//源长度编码  
                }
                if (GlobalMatchQueue[1].ElementAt(i).ToString() != "-")
                {
                    nLabel++;
                }
                if (nLabel != -1)
                {
                    sTargetLengthCode = TargerLengthCode[nLabel];//匹配长度编码
                }
                #endregion
                #endregion

                #region 相似度计算
                double Sim = this.GetMatchSimilarity2(sSourceLenghtCode, sTargetLengthCode, AngleMatchScore, Label);
                #endregion

                GlobalSimilarity = GlobalSimilarity + Sim;
            }

            return GlobalSimilarity;
        }

        /// <summary>
        /// 计算匹配局部序列的相似性
        /// </summary>
        /// <param name="SourceAngleCode"></param>
        /// <param name="TargetAngleCode"></param>
        /// <param name="SourceLengthCode"></param>
        /// <param name="TargerLengthCode"></param>
        /// <param name="GlobalMatchQueue"></param>
        ///  Label=计算相似性的方法；=1表示匹配对相似取最小值；匹配对不相似取最大值
        /// =2 表示匹配对相似取重叠的距离值；匹配对不相似取最大值
        /// =3表示匹配对不考虑长度的权重
        /// <returns></returns>
        double GetLocalSimilarity(List<string> SourceAngleCode, List<string> TargetAngleCode, List<List<double>> SourceLengthCode, List<List<double>> TargerLengthCode, List<string> LocalMatchQueue,int Label,int GapLabel)
        {
            double LocalSimilarity = 0;

            List<double> LocalsimList = new List<double>();
            string LocalMatch1 = LocalMatchQueue[0].Replace("-", "");
            string LocalMatch2 = LocalMatchQueue[1].Replace("-", "");

            int LocalLength1 = LocalMatch1.Length;
            int LocalLength2 = LocalMatch2.Length;
            for (int i = 0; i < (SourceAngleCode.Count+1)-LocalLength1; i++)
            {
                bool pLabel = true;
                for (int j = 0; j < LocalLength1; j++)
                {
                    if(LocalMatch1.ElementAt(j).ToString()!=SourceAngleCode[i+j])
                    {
                        pLabel = false;
                    }
                }

                #region 若获得了匹配序列
                if (pLabel)
                {
                    for (int m = 0; m < (TargetAngleCode.Count+1)-LocalLength2; m++)
                    {
                        bool qLabel = true;
                        for (int n= 0; n < LocalLength2; n++)
                        {
                            if (LocalMatch2.ElementAt(n).ToString() != TargetAngleCode[m + n])
                            {
                                qLabel = false;
                            }
                        }

                        #region 获得了匹配序列
                        if (qLabel)
                        {
                            int mLabel = -1; int nLabel = -1; double LocalSim = 0;
                            for (int s = 0; s < LocalMatchQueue[0].Length; s++)
                            {
                                #region 获取匹配的要素
                                #region 角度编码
                                string sTargetAngleCode = LocalMatchQueue[0].ElementAt(s).ToString();//源编码
                                string sSourceAngleCode = LocalMatchQueue[1].ElementAt(s).ToString();//匹配编码
                                #endregion

                                #region 分数编码
                                double AngleMatchScore = 0;//分数

                                if (GapLabel == 1)
                                {
                                    if (LocalMatchQueue[0].ElementAt(s).ToString() == "-" || LocalMatchQueue[1].ElementAt(s).ToString() == "-")
                                    {
                                        AngleMatchScore = -4;
                                    }
                                    else
                                    {
                                        List<String> MatchCacheCode = new List<string>(); MatchCacheCode.Add(sTargetAngleCode); MatchCacheCode.Add(sSourceAngleCode);
                                        //利用键值对匹配似乎会出问题
                                        AngleMatchScore = ScoreDic[string.Join("", MatchCacheCode)];

                                        //int TestLocation = 0;
                                    }
                                }

                                else if (GapLabel == 2)
                                {
                                    if ((s == 0) && (LocalMatchQueue[0].ElementAt(s).ToString() == "-" || LocalMatchQueue[1].ElementAt(s).ToString() == "-"))
                                    {
                                        AngleMatchScore = -4;
                                    }
                                    //考虑GapExtending分数相对较小
                                    else if ((s != 0) && ((LocalMatchQueue[0].ElementAt(s).ToString() == "-" && LocalMatchQueue[0].ElementAt(s - 1).ToString() == "-") || (LocalMatchQueue[1].ElementAt(s).ToString() == "-" && LocalMatchQueue[1].ElementAt(s - 1).ToString() == "-")))
                                    {
                                        AngleMatchScore = -2;
                                    }
                                    else if (LocalMatchQueue[0].ElementAt(s).ToString() == "-" || LocalMatchQueue[1].ElementAt(s).ToString() == "-")
                                    {
                                        AngleMatchScore = -4;
                                    }
                                    else
                                    {
                                        List<String> MatchCacheCode = new List<string>(); MatchCacheCode.Add(sTargetAngleCode); MatchCacheCode.Add(sSourceAngleCode);
                                        //利用键值对匹配似乎会出问题
                                        AngleMatchScore = ScoreDic[string.Join("", MatchCacheCode)];
                                    }
                                }
                                #endregion

                                #region 获得长度编码
                                List<double> sSourceLenghtCode = new List<double>();
                                List<double> sTargetLengthCode = new List<double>();
                                if (LocalMatchQueue[0].ElementAt(s).ToString() != "-")
                                {
                                    mLabel++;
                                }
                                if (mLabel != -1)
                                {
                                    sSourceLenghtCode = SourceLengthCode[mLabel + i];//源长度编码 
                                }
                                if (LocalMatchQueue[1].ElementAt(s).ToString() != "-")
                                {
                                    nLabel++;
                                }
                                if (nLabel != -1)
                                {
                                    sTargetLengthCode = TargerLengthCode[nLabel + m];//匹配长度编码
                                }
                                #endregion
                                #endregion

                                #region 相似度计算
                                double Sim = this.GetMatchSimilarity(sSourceLenghtCode, sTargetLengthCode, AngleMatchScore, Label);
                                #endregion

                                LocalSim = LocalSim + Sim;
                            }

                            LocalsimList.Add(LocalSim);
                        }
                        #endregion
                    }
                }
                #endregion
            }

            LocalSimilarity = LocalsimList.Max();
            return LocalSimilarity;
        }

        /// <summary>
        /// 计算匹配局部序列的相似性
        /// </summary>
        /// <param name="SourceAngleCode"></param>
        /// <param name="TargetAngleCode"></param>
        /// <param name="SourceLengthCode"></param>
        /// <param name="TargerLengthCode"></param>
        /// <param name="GlobalMatchQueue"></param>
        ///  Label=计算相似性的方法；=1表示匹配对相似取最小值；匹配对不相似取最大值
        /// =2 表示匹配对相似取重叠的距离值；匹配对不相似取最大值
        /// =3表示匹配对不考虑长度的权重
        /// <returns></returns>
        double GetLocalSimilarity2(List<string> SourceAngleCode, List<string> TargetAngleCode, List<List<double>> SourceLengthCode, List<List<double>> TargerLengthCode, List<string> LocalMatchQueue, int Label, int GapLabel)
        {
            double LocalSimilarity = 0;

            List<double> LocalsimList = new List<double>();
            string LocalMatch1 = LocalMatchQueue[0].Replace("-", "");
            string LocalMatch2 = LocalMatchQueue[1].Replace("-", "");

            int LocalLength1 = LocalMatch1.Length;
            int LocalLength2 = LocalMatch2.Length;
            for (int i = 0; i < (SourceAngleCode.Count + 1) - LocalLength1; i++)
            {
                bool pLabel = true;
                for (int j = 0; j < LocalLength1; j++)
                {
                    if (LocalMatch1.ElementAt(j).ToString() != SourceAngleCode[i + j])
                    {
                        pLabel = false;
                    }
                }

                #region 若获得了匹配序列
                if (pLabel)
                {
                    for (int m = 0; m < (TargetAngleCode.Count + 1) - LocalLength2; m++)
                    {
                        bool qLabel = true;
                        for (int n = 0; n < LocalLength2; n++)
                        {
                            if (LocalMatch2.ElementAt(n).ToString() != TargetAngleCode[m + n])
                            {
                                qLabel = false;
                            }
                        }

                        #region 获得了匹配序列
                        if (qLabel)
                        {
                            int mLabel = -1; int nLabel = -1; double LocalSim = 0;
                            for (int s = 0; s < LocalMatchQueue[0].Length; s++)
                            {
                                #region 获取匹配的要素
                                #region 角度编码
                                string sTargetAngleCode = LocalMatchQueue[0].ElementAt(s).ToString();//源编码
                                string sSourceAngleCode = LocalMatchQueue[1].ElementAt(s).ToString();//匹配编码
                                #endregion

                                #region 分数编码
                                double AngleMatchScore = 0;//分数

                                if (GapLabel == 1)
                                {
                                    if (LocalMatchQueue[0].ElementAt(s).ToString() == "-" || LocalMatchQueue[1].ElementAt(s).ToString() == "-")
                                    {
                                        AngleMatchScore = -0.4;
                                    }
                                    else
                                    {
                                        List<String> MatchCacheCode = new List<string>(); MatchCacheCode.Add(sTargetAngleCode); MatchCacheCode.Add(sSourceAngleCode);
                                        //利用键值对匹配似乎会出问题
                                        AngleMatchScore = ScoreDic[string.Join("", MatchCacheCode)];

                                        //int TestLocation = 0;
                                    }
                                }

                                else if (GapLabel == 2)
                                {
                                    if ((s == 0) && (LocalMatchQueue[0].ElementAt(s).ToString() == "-" || LocalMatchQueue[1].ElementAt(s).ToString() == "-"))
                                    {
                                        AngleMatchScore = -0.4;
                                    }
                                    //考虑GapExtending分数相对较小
                                    else if ((s != 0) &&
                                         (LocalMatchQueue[0].ElementAt(s).ToString() == "-" && (LocalMatchQueue[0].ElementAt(s - 1).ToString() == "-" || LocalMatchQueue[1].ElementAt(s - 1).ToString() == "-")) ||
                                         (LocalMatchQueue[1].ElementAt(s).ToString() == "-" && (LocalMatchQueue[1].ElementAt(s - 1).ToString() == "-" || LocalMatchQueue[0].ElementAt(s - 1).ToString() == "-")))
                                    {
                                        AngleMatchScore = -0.25;
                                    }
                                    else if (LocalMatchQueue[0].ElementAt(s).ToString() == "-" || LocalMatchQueue[1].ElementAt(s).ToString() == "-")
                                    {
                                        AngleMatchScore = -0.4;
                                    }
                                    else
                                    {
                                        if (s == 0)
                                        {
                                            List<String> MatchCacheCode = new List<string>(); MatchCacheCode.Add(sTargetAngleCode); MatchCacheCode.Add(sSourceAngleCode);
                                            //利用键值对匹配似乎会出问题
                                            AngleMatchScore = ScoreDic[string.Join("", MatchCacheCode)];

                                            if (AngleMatchScore < 0)
                                            {
                                                AngleMatchScore = -0.4;
                                            }
                                        }

                                        else if (s != 0)
                                        {
                                            List<String> MatchCacheCode1 = new List<string>(); MatchCacheCode1.Add(sTargetAngleCode); MatchCacheCode1.Add(sSourceAngleCode);
                                            //利用键值对匹配似乎会出问题
                                            AngleMatchScore = ScoreDic[string.Join("", MatchCacheCode1)];

                                            if (LocalMatchQueue[0].ElementAt(s - 1).ToString() != "-" && LocalMatchQueue[1].ElementAt(s - 1).ToString() != "-")
                                            {
                                                List<String> MatchCacheCode2 = new List<string>(); MatchCacheCode2.Add(LocalMatchQueue[0].ElementAt(s-1).ToString()); MatchCacheCode2.Add(LocalMatchQueue[1].ElementAt(s - 1).ToString());
                                                double AngleMatchScore2 = ScoreDic[string.Join("", MatchCacheCode2)];
                                                if (AngleMatchScore2 < 0 && AngleMatchScore < 0)
                                                {
                                                    AngleMatchScore = -0.25;
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion

                                #region 获得长度编码
                                List<double> sSourceLenghtCode = new List<double>();
                                List<double> sTargetLengthCode = new List<double>();
                                if (LocalMatchQueue[0].ElementAt(s).ToString() != "-")
                                {
                                    mLabel++;
                                }
                                if (mLabel != -1)
                                {
                                    sSourceLenghtCode = SourceLengthCode[mLabel + i];//源长度编码 
                                }
                                if (LocalMatchQueue[1].ElementAt(s).ToString() != "-")
                                {
                                    nLabel++;
                                }
                                if (nLabel != -1)
                                {
                                    sTargetLengthCode = TargerLengthCode[nLabel + m];//匹配长度编码
                                }
                                #endregion
                                #endregion

                                #region 相似度计算
                                double Sim = this.GetMatchSimilarity2(sSourceLenghtCode, sTargetLengthCode, AngleMatchScore, Label);
                                #endregion

                                LocalSim = LocalSim + Sim;
                            }

                            LocalsimList.Add(LocalSim);
                        }
                        #endregion
                    }
                }
                #endregion
            }

            LocalSimilarity = LocalsimList.Max();
            return LocalSimilarity;
        }

        /// <summary>
        /// 获得给定匹配对的相似度
        /// </summary>
        /// <param name="sSourceLenghtCode"></param>
        /// <param name="sTargetLengthCode"></param>
        /// <param name="Score"></param>
        /// MatchType=1 表示匹配对相似取最小值；匹配对不相似取最大值
        /// MatchType=2 表示匹配对相似取重叠的距离值；匹配对不相似取最大值
        /// MatchType=3表示匹配对不考虑长度的权重
        /// <returns></returns>
        double GetMatchSimilarity(List<double> sSourceLenghtCode, List<double> sTargetLengthCode, double Score,int MatchType)
        {
            double MatchScore = 0;

            #region 长度计算
            double MaxLength = 0; double MinLength = 0; double InterLength = 0;
            double sLengthTotal = 0; if (sSourceLenghtCode.Count > 0) { sLengthTotal = sSourceLenghtCode[0] + sSourceLenghtCode[1] + sSourceLenghtCode[2]; }
            double tLengthTotal = 0; if (sTargetLengthCode.Count > 0) { tLengthTotal = sTargetLengthCode[0] + sTargetLengthCode[1] + sTargetLengthCode[2]; }
            MaxLength = Math.Max(sLengthTotal, tLengthTotal); MinLength = Math.Min(sLengthTotal, tLengthTotal);

            if (sSourceLenghtCode.Count > 0 && sTargetLengthCode.Count > 0)
            {
                InterLength = Math.Min(sSourceLenghtCode[0], sTargetLengthCode[0]) + Math.Min(sSourceLenghtCode[1], sTargetLengthCode[1]) + Math.Min(sSourceLenghtCode[2], sTargetLengthCode[2]);
            }

            if (sSourceLenghtCode.Count > 0 && sTargetLengthCode.Count==0)
            {
                InterLength =sSourceLenghtCode[0]+sSourceLenghtCode[1]+sSourceLenghtCode[2];
            }

            if (sTargetLengthCode.Count > 0 && sSourceLenghtCode.Count == 0)
            {
                InterLength = sTargetLengthCode[0] + sTargetLengthCode[1] + sTargetLengthCode[2];
            }
            #endregion

            #region 得分方式1计算
            if (MatchType == 1)
            {
                if (Score > 0)
                {
                    MatchScore = MinLength * Score;
                }

                else
                {
                    MatchScore = MaxLength * Score;
                }
            }
            #endregion

            #region 得分方式2计算
            else if (MatchType == 2)
            {
                if (Score > 0)
                {
                    MatchScore = InterLength * Score;
                }

                else
                {
                    MatchScore = MaxLength * Score;
                }
            }
            #endregion

            #region 得分方式3计算
            else if (MatchType == 3)
            {
                
                MatchScore =  Score;
            }
            #endregion

            return MatchScore;
        }

        /// <summary>
        /// 获得给定匹配对的相似度
        /// </summary>
        /// <param name="sSourceLenghtCode"></param>
        /// <param name="sTargetLengthCode"></param>
        /// <param name="Score"></param>
        /// MatchType=1 表示匹配对相似取最小值；匹配对不相似取最大值
        /// MatchType=2 表示匹配对相似取重叠的距离值；匹配对不相似取最大值
        /// MatchType=3表示匹配对不考虑长度的权重
        /// <returns></returns>
        double GetMatchSimilarity2(List<double> sSourceLenghtCode, List<double> sTargetLengthCode, double Score, int MatchType)
        {
            double MatchScore = 0;

            #region 长度计算
            double MaxLength = 0; double MinLength = 0; double InterLength = 0;
            double sLengthTotal = 0; if (sSourceLenghtCode.Count > 0) { sLengthTotal = sSourceLenghtCode[0] + sSourceLenghtCode[1] + sSourceLenghtCode[2]; }
            double tLengthTotal = 0; if (sTargetLengthCode.Count > 0) { tLengthTotal = sTargetLengthCode[0] + sTargetLengthCode[1] + sTargetLengthCode[2]; }
            MaxLength = Math.Max(sLengthTotal, tLengthTotal); MinLength = Math.Min(sLengthTotal, tLengthTotal);

            if (sSourceLenghtCode.Count > 0 && sTargetLengthCode.Count > 0)
            {
                InterLength = Math.Min(sSourceLenghtCode[0], sTargetLengthCode[0]) + Math.Min(sSourceLenghtCode[1], sTargetLengthCode[1]) + Math.Min(sSourceLenghtCode[2], sTargetLengthCode[2]);
            }

            if (sSourceLenghtCode.Count > 0 && sTargetLengthCode.Count == 0)
            {
                InterLength = sSourceLenghtCode[0] + sSourceLenghtCode[1] + sSourceLenghtCode[2];
            }

            if (sTargetLengthCode.Count > 0 && sSourceLenghtCode.Count == 0)
            {
                InterLength = sTargetLengthCode[0] + sTargetLengthCode[1] + sTargetLengthCode[2];
            }
            #endregion

            #region 匹配对关系的相似度计算
            if (Score > 0)
            {
                double LengthScore = (Math.Min(sSourceLenghtCode[0], sTargetLengthCode[0]) / Math.Max(sSourceLenghtCode[0], sTargetLengthCode[0]) +
                    Math.Min(sSourceLenghtCode[1], sTargetLengthCode[1]) / Math.Max(sSourceLenghtCode[1], sTargetLengthCode[1]) +
                    Math.Min(sSourceLenghtCode[2], sTargetLengthCode[2]) / Math.Max(sSourceLenghtCode[2], sTargetLengthCode[2])) / 3;
                double AngleScore = Score / 5;
                Score = AngleScore * 0.5 + LengthScore * 0.5; ;
            }
            #endregion

            #region 得分方式1计算
            if (MatchType == 1)
            {
                if (Score > 0)
                {
                    MatchScore = MinLength * Score;
                }

                else
                {
                    MatchScore = MaxLength * Score;
                }
            }
            #endregion

            #region 得分方式2计算
            else if (MatchType == 2)
            {
                if (Score > 0)
                {
                    MatchScore = InterLength * Score;
                }

                else
                {
                    MatchScore = MaxLength * Score;
                }
            }
            #endregion

            #region 得分方式3计算
            else if (MatchType == 3)
            {

                MatchScore = Score;
            }
            #endregion

            return MatchScore;
        }

        /// <summary>
        /// 获得给定匹配对的相似度(最新)（综合考虑长度相似性与角度相似性）
        /// </summary>
        /// <param name="sSourceLenghtCode"></param>
        /// <param name="sTargetLengthCode"></param>
        /// <param name="Score">AngleMatchScore，有正负</param>
        /// MatchType=1 表示匹配对相似取最小值；匹配对不相似取最大值
        /// MatchType=2 表示匹配对相似取重叠的距离值；匹配对不相似取最大值
        /// MatchType=3表示匹配对不考虑长度的权重
        /// <returns></returns>
        double GetLengthMatchSimilarity(List<double> sSourceLenghtCode, List<double> sTargetLengthCode, double Score, int MatchType,double AngleWeight,double LengthWeigth)
        {
            double MatchScore = 0;

            #region 长度计算
            double MaxLength = 0; double MinLength = 0; double InterLength = 0;
            double sLengthTotal = 0; if (sSourceLenghtCode.Count > 0) { sLengthTotal = sSourceLenghtCode[0] + sSourceLenghtCode[1] + sSourceLenghtCode[2]; }
            double tLengthTotal = 0; if (sTargetLengthCode.Count > 0) { tLengthTotal = sTargetLengthCode[0] + sTargetLengthCode[1] + sTargetLengthCode[2]; }
            MaxLength = Math.Max(sLengthTotal, tLengthTotal); MinLength = Math.Min(sLengthTotal, tLengthTotal);

            if (sSourceLenghtCode.Count > 0 && sTargetLengthCode.Count > 0)
            {
                InterLength = Math.Min(sSourceLenghtCode[0], sTargetLengthCode[0]) + Math.Min(sSourceLenghtCode[1], sTargetLengthCode[1]) + Math.Min(sSourceLenghtCode[2], sTargetLengthCode[2]);
            }

            if (sSourceLenghtCode.Count > 0 && sTargetLengthCode.Count == 0)
            {
                InterLength = sSourceLenghtCode[0] + sSourceLenghtCode[1] + sSourceLenghtCode[2];
            }

            if (sTargetLengthCode.Count > 0 && sSourceLenghtCode.Count == 0)
            {
                InterLength = sTargetLengthCode[0] + sTargetLengthCode[1] + sTargetLengthCode[2];
            }
            #endregion

            #region 匹配对关系的相似度计算
            if (Score > 0)
            {
                double LengthScore = (Math.Min(sSourceLenghtCode[0], sTargetLengthCode[0]) / Math.Max(sSourceLenghtCode[0], sTargetLengthCode[0]) +
                    Math.Min(sSourceLenghtCode[1], sTargetLengthCode[1]) / Math.Max(sSourceLenghtCode[1], sTargetLengthCode[1]) +
                    Math.Min(sSourceLenghtCode[2], sTargetLengthCode[2]) / Math.Max(sSourceLenghtCode[2], sTargetLengthCode[2])) / 3;
                double AngleScore = Score / 5;
                Score = AngleScore * AngleWeight + LengthScore * LengthWeigth ;
            }
            #endregion

            #region 得分方式1计算
            if (MatchType == 1)
            {
                if (Score > 0)
                {
                    MatchScore = MinLength * Score;
                }

                else
                {
                    MatchScore = MaxLength * Score;
                }
            }
            #endregion

            #region 得分方式2计算
            else if (MatchType == 2)
            {
                if (Score > 0)
                {
                    MatchScore = InterLength * Score;
                }

                else
                {
                    MatchScore = MaxLength * Score;
                }
            }
            #endregion

            #region 得分方式3计算
            else if (MatchType == 3)
            {

                MatchScore = Score;
            }
            #endregion

            return MatchScore;
        }

        /// <summary>
        /// 获取角度编码的得分（考虑了Gap和MisMatch的连续性）-->即依据匹配关系获得匹配分数
        /// </summary>
        /// <param name="CacheSourceAngleCode">当前已匹配的Source序列</param>
        /// <param name="CacheTargetAngleCode">当前已匹配的Target序列</param>
        /// <param name="GapLabel">分数计算说明
        /// =1不考虑extending；=2考虑extending，但不考虑extending长度；=3考虑extending长度;=4（gap和MisMatch分开考虑）</param>
        /// <param name="d">gap或MisMatch罚分</param>
        /// <param name="e">考虑extending罚分</param>
        /// MatchLabel说明前一个匹配关系=1match；=2Mismatch；=3Gap
        /// <returns></returns>
        double GetAngleMatchSimilarity(List<string> CacheSourceAngleCode, List<string> CacheTargetAngleCode,int GapLabel,double d,double e,int MatchLabel)
        {
            double AngleMatchScore = 0;//分数

            #region 分数编码
            #region 不考虑extending罚分
            if (GapLabel == 1 ||CacheSourceAngleCode.Count==1)
            {
                #region 存在Gap关系
                if (CacheSourceAngleCode[CacheSourceAngleCode.Count-1].ToString() == "-" || CacheTargetAngleCode[CacheTargetAngleCode.Count-1] == "-")
                {
                    AngleMatchScore = d;
                }
                #endregion

                #region 判断MisMatch和Match关系
                else 
                {
                    List<String> MatchCacheCode = new List<string>(); 
                    MatchCacheCode.Add(CacheSourceAngleCode[CacheSourceAngleCode.Count-1].ToString()); 
                    MatchCacheCode.Add(CacheTargetAngleCode[CacheTargetAngleCode.Count-1].ToString());
                    //利用键值对匹配似乎会出问题
                    AngleMatchScore = ScoreDic[string.Join("", MatchCacheCode)];

                    #region 存在MisMatch关系
                    if (AngleMatchScore < 0)
                    {
                        AngleMatchScore = d;
                    }
                    #endregion
                }
                #endregion
            }
            #endregion

            #region 考虑extending罚分(将Gap与extending同等对待)
            else if (GapLabel == 2)
            {
                #region 获得最后一个匹配关系得分
                if (CacheSourceAngleCode[CacheSourceAngleCode.Count - 1].ToString() == "-" || CacheTargetAngleCode[CacheTargetAngleCode.Count - 1] == "-")
                {
                    AngleMatchScore = d;
                }
                else
                {
                    List<String> MatchCacheCode = new List<string>();
                    MatchCacheCode.Add(CacheSourceAngleCode[CacheSourceAngleCode.Count - 1].ToString());
                    MatchCacheCode.Add(CacheTargetAngleCode[CacheTargetAngleCode.Count - 1].ToString());
                    //利用键值对匹配似乎会出问题
                    AngleMatchScore = ScoreDic[string.Join("", MatchCacheCode)];

                    #region 存在MisMatch关系
                    if (AngleMatchScore < 0)
                    {
                        AngleMatchScore = d;
                    }
                    #endregion
                }
                #endregion

                if (MatchLabel == 2 || MatchLabel == 3)
                {
                    if (AngleMatchScore < 0)
                    {
                        AngleMatchScore = e;
                    }
                }
            }
            #endregion

            #region 考虑extending 罚分（将Gap与extending不同等对待）
            else if (GapLabel == 4)
            {
                #region 获得最后一个匹配关系得分
                if (CacheSourceAngleCode[CacheSourceAngleCode.Count - 1].ToString() == "-" || CacheTargetAngleCode[CacheTargetAngleCode.Count - 1] == "-")
                {
                    if (MatchLabel == 3)
                    {
                        AngleMatchScore = e;
                    }

                    else
                    {
                        AngleMatchScore = d;
                    }
                }

                else
                {
                    List<String> MatchCacheCode = new List<string>();
                    MatchCacheCode.Add(CacheSourceAngleCode[CacheSourceAngleCode.Count - 1].ToString());
                    MatchCacheCode.Add(CacheTargetAngleCode[CacheTargetAngleCode.Count - 1].ToString());
                    //利用键值对匹配似乎会出问题
                    AngleMatchScore = ScoreDic[string.Join("", MatchCacheCode)];

                    #region 存在MisMatch关系
                    if (AngleMatchScore < 0)
                    {
                        if (MatchLabel == 2)
                        {
                            AngleMatchScore = e;
                        }

                        else
                        {
                            AngleMatchScore = d;
                        }
                    }
                    #endregion
                }
                #endregion
            }
            #endregion
            #endregion

            return AngleMatchScore;
        }

        /// <summary>
        /// 读取分数表
        /// </summary>
        void GetScoreBook()
        {
            StreamReader sr = new StreamReader(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\YANtt2.txt", Encoding.Default);
            String pline;
            List<string> LineList = new List<string>();

            while ((pline = sr.ReadLine()) != null)
            {
                LineList.Add(pline);
            }

            List<List<string>> SplitStringList = new List<List<string>>();
            for (int i = 0; i < LineList.Count; i++)
            {
                string[] CacheList = Regex.Split(LineList[i], "\\s+", RegexOptions.IgnoreCase);
                SplitStringList.Add(CacheList.ToList());
            }

            //int TestLocation = 0;

            for (int i = 1; i < SplitStringList[9].Count; i++)
            {
                for (int j = 0; j < SplitStringList.Count; j++)
                {
                    if (j > 9)
                   {
                        List<string> MatchCode = new List<string>();
                        MatchCode.Add(SplitStringList[9][i].ToString()); MatchCode.Add(SplitStringList[j][0].ToString());
                        ScoreDic.Add(string.Join("",MatchCode), double.Parse(SplitStringList[j][i]));
                    }
                }
            }

           //int TestLocation = 0;
        }

        /// <summary>
        /// 依据匹配序列，计算所有匹配对的相似度
        /// </summary>
        /// <param name="GlobalMatchList"></param>
        /// <param name="LocalMatchList"></param>
        /// <param name="SourceBuildingCode"></param>
        /// <param name="TargetBuildingCode"></param>
        /// <param name="Label"></param>计算相似性的方法
        /// /// Label=计算相似性的方法；=1表示匹配对相似取最小值；匹配对不相似取最大值
        /// =2 表示匹配对相似取重叠的距离值；匹配对不相似取最大值
        /// =3表示匹配对不考虑长度的权重
        /// SimType:=1计算GlobalSimilarity；=2计算localSimilarity
        /// <returns></returns>
        List<List<List<double>>> GetSimilarity(List<List<List<string>>> GlobalMatchList, List<List<List<string>>> LocalMatchList, Dictionary<List<string>, List<List<double>>> SourceBuildingCode,Dictionary<List<string>, List<List<double>>> TargetBuildingCode,int Label,int SimType,int GapLabel)
        {
            List<List<List<double>>> SimilarityList = new List<List<List<double>>>();

            int sBuildingCount = -1;
            foreach (KeyValuePair<List<string>, List<List<double>>> skvp in SourceBuildingCode)
            {
                List<string> slBuildingCode = skvp.Key;
                sBuildingCount++; int tBuildingCount = -1;
                string sBuildingCode = string.Join("", slBuildingCode);//列表转换为string

                #region 一个建筑物的计算
                List<List<double>> bSimilarityList = new List<List<double>>();
                foreach (KeyValuePair<List<string>, List<List<double>>> tsvp in TargetBuildingCode)
                {
                    List<string> tlBuildingCode = tsvp.Key; tBuildingCount++;
                    string tBuildingCode = string.Join("", tlBuildingCode);//列表转换为string
                    List<double> SimilaritySingleList = new List<double>();

                    #region 建筑物的循环
                    for (int i = 0; i < slBuildingCode.Count; i++)
                    {
                        string fsBuildingCode = sBuildingCode.Substring(i, sBuildingCode.Length - i) + sBuildingCode.Substring(0, i);//源角度编码
                        List<List<double>> sCacheLengthCode = new List<List<double>>();//源长度编码
                        for (int j = 0; j < skvp.Value.Count; j++)
                        {
                            sCacheLengthCode.Add(skvp.Value[(i+j)%skvp.Value.Count]);
                        }

                        for (int m = 0; m < tlBuildingCode.Count; m++)
                        {
                            string ftBuildingCode = tBuildingCode.Substring(m, tBuildingCode.Length - m) + tBuildingCode.Substring(0, m);//Target角度编码
                            List<List<double>> tCacheLengthCode = new List<List<double>>();//target长度编码
                            for (int n = 0; n < tsvp.Value.Count; n++)
                            {
                                tCacheLengthCode.Add(tsvp.Value[(m + n) % tsvp.Value.Count]);
                            }

                            //判断索引是否还在列表中存在
                            if (sBuildingCount * TargetBuildingCode.Keys.Count + tBuildingCount < GlobalMatchList.Count && i * tBuildingCode.Length + m < GlobalMatchList[sBuildingCount * TargetBuildingCode.Keys.Count + tBuildingCount].Count)
                            {
                                //int Test1 = sBuildingCount * (TargetBuildingCode.Keys.Count+1) + tBuildingCount;
                                //int Test2 = i * tBuildingCode.Length + m;
                                //这里(TargetBuildingCode.Keys.Count+1)原因：每一个TargetBuilding的txt末尾都添加有一个测试案例，这个案例在实际中不计数
                                List<string> GlobalMatch = GlobalMatchList[sBuildingCount *(TargetBuildingCode.Keys.Count+1) + tBuildingCount][i * tBuildingCode.Length + m];//获取匹配序列编码
                                List<string> LocalMatch = LocalMatchList[sBuildingCount * (TargetBuildingCode.Keys.Count + 1) + tBuildingCount][i * tBuildingCode.Length + m];

                                #region String与List的替换
                                List<string> sAngleCodeListType = new List<string>();
                                List<string> tAngleCodeListType = new List<string>();
                                for (int k = 0; k < fsBuildingCode.Length; k++)
                                {
                                    sAngleCodeListType.Add(fsBuildingCode.ElementAt(k).ToString());
                                }

                                for (int k = 0; k < ftBuildingCode.Length; k++)
                                {
                                    tAngleCodeListType.Add(ftBuildingCode.ElementAt(k).ToString());
                                }
                                #endregion

                                #region 相似度计算 ==1计算全局相似度
                                if (SimType == 1)
                                {
                                    double GlobalSim = this.getGlobalSimilarity2(sAngleCodeListType, tAngleCodeListType, sCacheLengthCode, tCacheLengthCode, GlobalMatch, Label,GapLabel);
                                    SimilaritySingleList.Add(GlobalSim);
                                }

                                else if (SimType == 2) //==2计算局部相似度
                                {
                                    double LocalSim = this.GetLocalSimilarity2(sAngleCodeListType, tAngleCodeListType, sCacheLengthCode, tCacheLengthCode, LocalMatch, Label,GapLabel);
                                    SimilaritySingleList.Add(LocalSim);
                                }
                                #endregion
                            }
                        }
                    }
                    #endregion

                    bSimilarityList.Add(SimilaritySingleList);
                }
                #endregion

                SimilarityList.Add(bSimilarityList);
            }

            return SimilarityList;
        }

        /// <summary>
        /// 获取两个序列的全局相似性
        /// </summary>
        /// <param name="SourceBuildingCode"></param>源建筑物编码
        /// <param name="TargetBuildingCode"></param>目标建筑物编码
        /// <param name="d"></param>罚分
        /// <param name="e"></param>考虑extending罚分
        /// <param name="MatchType"></param>计算得分时考虑对应元素的长度匹配关系
        /// <param name="AngleWeight"></param>角度相似性考虑
        /// <param name="LengthWeight"></param>长度相似性考虑
        /// <returns></returns>
        public double GetGlobalSim(List<String> SourceCode, List<List<double>> SourceCodeLength, List<String> TargetCode, List<List<double>> TargetCodeLength, double d, double e, int MatchType, double AngleWeight, double LengthWeight,int GapLabel)
        {
            double GlobalSim = 0;

            #region 构建矩阵计算相似性
            double[,] matrixGraph = new double[TargetCode.Count + 1, SourceCode.Count+1];//构建一个（p+1）*（q+1）的矩阵
            int[,] MatchGraph = new int[TargetCode.Count + 1, SourceCode.Count + 1];//匹配关系1=Match；2=Mismatch；3=Gap

            #region 初始化矩阵
            matrixGraph[0, 0] = 0;
            MatchGraph[0, 0] = 1;
            for (int i = 1; i < SourceCode.Count + 1; i++)
            {
                string SourceKey = SourceCode[i - 1];
                double AngleScore = 0;
                if (i == 1)
                {
                    AngleScore = d;
                }

                else
                {
                    AngleScore = e;
                }

                List<double> CacheTargetLength = new List<double>();
                double LengthScore = this.GetLengthMatchSimilarity(SourceCodeLength[SourceCode.IndexOf(SourceKey)], CacheTargetLength, AngleScore, MatchType, AngleWeight, LengthWeight);//计算长度得分

                matrixGraph[0, i] = LengthScore + matrixGraph[0, i - 1];
                MatchGraph[0, i] = 3;
            }

            for (int i= 1; i < TargetCode.Count + 1; i++)
            {
                string TargetKey =  TargetCode[i - 1];

                double AngleScore = 0;
                if (i == 1)
                {
                    AngleScore = d;
                }

                else
                {
                    AngleScore = e;
                }

                List<double> CacheSourceLength = new List<double>();
                double LengthScore = this.GetLengthMatchSimilarity(TargetCodeLength[TargetCode.IndexOf(TargetKey)], CacheSourceLength, AngleScore, MatchType, AngleWeight, LengthWeight);//计算长度得分

                matrixGraph[i, 0] = LengthScore + matrixGraph[i - 1, 0];
                MatchGraph[i, 0] = 3;
            }
            #endregion

            #region 矩阵更新
            for (int i = 1; i < TargetCode.Count + 1; i++)
            {
                for (int j = 1; j < SourceCode.Count + 1; j++)
                {
                    #region 获取前一个匹配对关系
                    int MatchLable = 0;//匹配关系
                    int CacheMatchLable=0;

                    double CacheScore1 = matrixGraph[i - 1, j - 1];
                    double CacheScore2 = matrixGraph[i - 1, j];
                    double CacheScore3 = matrixGraph[i, j - 1];

                    if (CacheScore1 > CacheScore2 && CacheScore1 > CacheScore3)
                    {
                        CacheMatchLable = MatchGraph[i - 1, j - 1];
                    }

                    else if (CacheScore2 > CacheScore1 && CacheScore2 > CacheScore3)
                    {
                        CacheMatchLable = MatchGraph[i - 1, j];
                    }

                    else if (CacheScore3 > CacheScore1 && CacheScore3 > CacheScore2)
                    {
                        CacheMatchLable = MatchGraph[i, j - 1];
                    }
                    #endregion

                    #region 参数定义
                    string SourceKey = null;
                    string TargetKey = null;
                    string fSourceCacheCode;
                    string fTargetCacheCode;

                    List<string> CacheSourceCode = new List<string>();
                    List<string> CacheTargetCode = new List<string>();
                    List<String> GapCode = new List<string>();
                    #endregion

                    #region 获得待匹配要素
                    SourceKey = SourceCode[j - 1];
                    fSourceCacheCode = SourceKey.Last().ToString();
                    CacheSourceCode.Add(fSourceCacheCode);

                    TargetKey = TargetCode[i - 1];
                    fTargetCacheCode = TargetKey.Last().ToString();
                    CacheTargetCode.Add(fTargetCacheCode);

                    GapCode.Add("-");
                    #endregion

                    #region 计算三种匹配序列得分
                    double AngleScore1 = this.GetAngleMatchSimilarity(CacheSourceCode, CacheTargetCode, GapLabel, d, e,CacheMatchLable);//计算角度得分
                    double LengthScore1 = this.GetLengthMatchSimilarity(SourceCodeLength[SourceCode.IndexOf(SourceKey)],TargetCodeLength[TargetCode.IndexOf(TargetKey)], AngleScore1, MatchType, AngleWeight, LengthWeight);//计算长度得分

                    double AngleScore2 = this.GetAngleMatchSimilarity(CacheSourceCode, GapCode, GapLabel, d, e,CacheMatchLable);//计算角度得分
                    List<double> CacheLengthList = new List<double>();
                    double LengthScore2 = this.GetLengthMatchSimilarity(SourceCodeLength[SourceCode.IndexOf(SourceKey)], CacheLengthList, AngleScore2, MatchType, AngleWeight, LengthWeight);//计算长度得分

                    double AngleScore3 = this.GetAngleMatchSimilarity(GapCode, CacheTargetCode, GapLabel, d, e,CacheMatchLable);//计算角度得分
                    double LengthScore3 = this.GetLengthMatchSimilarity(CacheLengthList, TargetCodeLength[TargetCode.IndexOf(TargetKey)], AngleScore3, MatchType, AngleWeight, LengthWeight);//计算长度得分
                    #endregion

                    #region 更新得分
                    double Score1 = matrixGraph[i - 1, j - 1] + LengthScore1;
                    double Score2 = matrixGraph[i - 1, j] + LengthScore2;
                    double Score3 = matrixGraph[i, j - 1] + LengthScore3;

                    if (Score1 > Score2 && Score1 > Score3)
                    {
                        matrixGraph[i, j] = Score1;
                        if (LengthScore1 > 0)
                        {
                            MatchLable = 1;
                        }

                        else
                        {
                            MatchLable = 2;
                        }
                    }

                    else if (Score2 > Score1 && Score2 > Score3)
                    {
                        matrixGraph[i, j] =  Score2;
                        MatchLable = 3;
                    }

                    else
                    {
                        matrixGraph[i, j] =  Score3;
                        MatchLable = 3;
                    }
                    #endregion

                    MatchGraph[i, j] = MatchLable;//更新对应组匹配关系
                }
            }
            #endregion

            GlobalSim = matrixGraph[TargetCode.Count, SourceCode.Count];//获得最大全局相似度
            #endregion

            return GlobalSim;
        }

        /// <summary>
        /// 获取两个序列的局部相似性
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <param name="d"></param>
        /// <param name="e"></param>
        /// <param name="MatchType"></param>
        /// <param name="AngleWeight"></param>
        /// <param name="LengthWeight"></param>
        /// <returns></returns>
        public double GetLocalSim(List<String> SourceCode, List<List<double>> SourceCodeLength, List<String> TargetCode, List<List<double>> TargetCodeLength, double d, double e, int MatchType, double AngleWeight, double LengthWeight,int GapLabel)
        {
            double LocalSim=0;

            #region 构建矩阵计算相似性
            double[,] matrixGraph = new double[TargetCode.Count + 1, SourceCode.Count + 1];//构建一个（p+1）*（q+1）的矩阵
            int[,] MatchGraph = new int[TargetCode.Count + 1, SourceCode.Count + 1];//匹配关系1=Match；2=Mismatch；3=Gap

            #region 初始化矩阵
            matrixGraph[0, 0] = 0;
            MatchGraph[0, 0] = 1;
            for (int i = 1; i < SourceCode.Count + 1; i++)
            {
                matrixGraph[0, i] = 0;
                MatchGraph[0, i] = 3;
            }

            for (int i = 1; i < TargetCode.Count + 1; i++)
            {
                matrixGraph[i, 0] = 0;
                MatchGraph[i, 0] = 3;
            }
            #endregion

            #region 矩阵更新
            for (int i = 1; i < TargetCode.Count + 1; i++)
            {
                for (int j = 1; j < SourceCode.Count + 1; j++)
                {
                    #region 获取前一个匹配对关系
                    int MatchLable = 0;//匹配关系
                    int CacheMatchLable = 0;

                    double CacheScore1 = matrixGraph[i - 1, j - 1];
                    double CacheScore2 = matrixGraph[i - 1, j];
                    double CacheScore3 = matrixGraph[i, j - 1];

                    if (CacheScore1 > CacheScore2 && CacheScore1 > CacheScore3)
                    {
                        CacheMatchLable = MatchGraph[i - 1, j - 1];
                    }

                    else if (CacheScore2 > CacheScore1 && CacheScore2 > CacheScore3)
                    {
                        CacheMatchLable = MatchGraph[i - 1, j];
                    }

                    else if (CacheScore3 > CacheScore1 && CacheScore3 > CacheScore2)
                    {
                        CacheMatchLable = MatchGraph[i, j - 1];
                    }
                    #endregion

                    #region 参数定义
                    string SourceKey = null;
                    string TargetKey = null;
                    string fSourceCacheCode;
                    string fTargetCacheCode;

                    List<string> CacheSourceCode = new List<string>();
                    List<string> CacheTargetCode = new List<string>();
                    List<String> GapCode = new List<string>();
                    #endregion

                    #region 获得待匹配要素
                    SourceKey = SourceCode[j - 1];
                    fSourceCacheCode = SourceKey.Last().ToString();
                    CacheSourceCode.Add(fSourceCacheCode);

                    TargetKey = TargetCode[i - 1];
                    fTargetCacheCode = TargetKey.Last().ToString();
                    CacheTargetCode.Add(fTargetCacheCode);

                    GapCode.Add("-");
                    #endregion

                    #region 计算三种匹配序列得分
                    double AngleScore1 = this.GetAngleMatchSimilarity(CacheSourceCode, CacheTargetCode, GapLabel, d, e, CacheMatchLable);//计算角度得分
                    double LengthScore1 = this.GetLengthMatchSimilarity(SourceCodeLength[SourceCode.IndexOf(SourceKey)], TargetCodeLength[TargetCode.IndexOf(TargetKey)], AngleScore1, MatchType, AngleWeight, LengthWeight);//计算长度得分

                    double AngleScore2 = this.GetAngleMatchSimilarity(CacheSourceCode, GapCode, GapLabel, d, e, CacheMatchLable);//计算角度得分
                    List<double> CacheLengthList = new List<double>();
                    double LengthScore2 = this.GetLengthMatchSimilarity(SourceCodeLength[SourceCode.IndexOf(SourceKey)], CacheLengthList, AngleScore2, MatchType, AngleWeight, LengthWeight);//计算长度得分

                    double AngleScore3 = this.GetAngleMatchSimilarity(GapCode, CacheTargetCode, GapLabel, d, e, CacheMatchLable);//计算角度得分
                    double LengthScore3 = this.GetLengthMatchSimilarity(CacheLengthList, TargetCodeLength[TargetCode.IndexOf(TargetKey)], AngleScore3, MatchType, AngleWeight, LengthWeight);//计算长度得分
                    #endregion

                    #region 更新得分
                    double Score1 = matrixGraph[i - 1, j - 1] + LengthScore1;
                    double Score2 = matrixGraph[i - 1, j] + LengthScore2;
                    double Score3 = matrixGraph[i, j - 1] + LengthScore3;

                    if (Score1 > Score2 && Score1 > Score3)
                    {
                        matrixGraph[i, j] = Score1;

                        if (matrixGraph[i, j] < 0)
                        {
                            matrixGraph[i, j] = 0;
                            MatchLable = 2;
                        }

                        else
                        {
                            MatchLable = 1;
                        }
                    }

                    else if (Score2 > Score1 && Score2 > Score3)
                    {
                        matrixGraph[i, j] =  Score2;
                        MatchLable = 3;

                        if (matrixGraph[i, j] < 0)
                        {
                            matrixGraph[i, j] = 0;
                        }
                    }

                    else
                    {
                        matrixGraph[i, j] =  Score3;
                        MatchLable = 3;

                        if (matrixGraph[i, j] < 0)
                        {
                            matrixGraph[i, j] = 0;
                        }
                    }
                    #endregion

                    MatchGraph[i, j] = MatchLable;//更新对应组匹配关系
                }
            }
            #endregion

            #region 获得最大的局部相似度
            for (int i = 1; i < TargetCode.Count + 1; i++)
            {
                for (int j = 1; j < SourceCode.Count + 1; j++)
                {
                    if (matrixGraph[i, j] > LocalSim)
                    {
                        LocalSim = matrixGraph[i, j];
                    }
                }
            }
            #endregion
            #endregion

            return LocalSim;
        }

        /// <summary>
        /// 获得i,j位置上一位置的匹配关系
        /// </summary>
        /// <param name="matrixGraph"></param>
        /// <param name="i"></param>
        /// <param name="?"></param>
        /// <returns></returns>=1 match;=2 MisMatch;=3 上Gap;=4 下Gap
        public int GetLastMatch(double[,] matrixGraph,int i,int j)
        {
            int MatchLabel = 0;

            double Score1 = matrixGraph[i - 1, j - 1];
            double Score2 = matrixGraph[i - 1, j];
            double Score3 = matrixGraph[i, j - 1];

            if (Score1 > Score2 && Score1 > Score3)
            {
                if (Score1 > 0)
                {
                    MatchLabel = 1;
                }

                else
                {
                    MatchLabel = 2;
                }
            }

            else if (Score2 > Score1 & Score2 > Score3)
            {
                MatchLabel = 4;
            }

            else
            {
                MatchLabel = 3;
            }

            return MatchLabel;
        }

        /// <summary>
        /// 转角函数相似度计算
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button16_Click(object sender, EventArgs e)
        {
            #region 图形编码
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }

            List<IFeatureLayer> list2 = new List<IFeatureLayer>();
            if (this.comboBox4.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list2.Add(BuildingLayer);
            }

            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();

            SMap map2 = new SMap(list2);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region 计算基于转角函数的相似程度
            List<List<List<double>>> BBTurningAngleList = new List<List<List<double>>>();
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                PolygonObject Po1 = map.PolygonList[i];
                List<List<double>> BTurningAngleList = new List<List<double>>();

                for (int j = 0; j < map2.PolygonList.Count; j++)
                {
                    #region 计算一对建筑物起点不同的相似度
                    List<double> TurningAngleList = new List<double>();
                    PolygonObject Po2 = map2.PolygonList[j];

                    for (int m = 0; m < Po1.PointList.Count; m++)
                    {
                        List<List<double>> TurningAngle1 = this.GetTurningAngle(Po1, m);

                        for (int n = 0; n < Po2.PointList.Count; n++)
                        {
                            List<List<double>> TurningAngle2 = this.GetTurningAngle(Po2, n);
                            double TurningAngleSim = this.GetTurningSim(TurningAngle1, TurningAngle2);
                            TurningAngleList.Add(TurningAngleSim);
                        }
                    }
                    #endregion

                    BTurningAngleList.Add(TurningAngleList);
                }

                BBTurningAngleList.Add(BTurningAngleList);
            }
            #endregion

            #region 获得最大相似度(实际是数值越小，相似度越大)
            List<List<double>> pAngleSim = new List<List<double>>();

            for (int k = 0; k < BBTurningAngleList.Count; k++)
            {
                List<double> pCacheGlobal = new List<double>();
                for (int p = 0; p < BBTurningAngleList[k].Count; p++)
                {
                    pCacheGlobal.Add(BBTurningAngleList[k][p].Min());
                }
                pAngleSim.Add(pCacheGlobal);
            }

            //int TestLocation = 0;

            System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\AngleSimlarity.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);
            for (int k = 0; k < pAngleSim.Count; k++)
            {
                for (int p = 0; p < pAngleSim[k].Count; p++)
                {
                    sw.Write(pAngleSim[k][p].ToString());
                    sw.Write("\r\n");
                }
                sw.Write("\r\n");
            }

            sw.Close();
            fs.Close();

            //int TestLocation = 0;
            #endregion 
        }

        /// <summary>
        /// 计算给定的两个建筑物的转角函数相似性
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <returns></returns>
        public double GetPolygonSimBasedTuringAngle(PolygonObject Po1, PolygonObject Po2)
        {
            double PolygonSimBasedTurningAngle = 0;

            #region 以不同起点计算相似程度
            List<double> TurningAngleList = new List<double>();
            for (int m = 0; m < Po1.PointList.Count; m++)
            {
                List<List<double>> TurningAngle1 = this.GetTurningAngle(Po1, m);

                for (int n = 0; n < Po2.PointList.Count; n++)
                {
                    List<List<double>> TurningAngle2 = this.GetTurningAngle(Po2, n);
                    double TurningAngleSim = this.GetTurningSim(TurningAngle1, TurningAngle2);
                    TurningAngleList.Add(TurningAngleSim);
                }
            }
            #endregion

            PolygonSimBasedTurningAngle = TurningAngleList.Min();//获得相似度最小的相似度
            return PolygonSimBasedTurningAngle;
        }

        /// <summary>
        /// 计算顾及GapExtending得分不一样的结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button17_Click(object sender, EventArgs e)
        {
            StreamReader sr = new StreamReader(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\log_name.txt", Encoding.Default);
            String pline;
            List<string> LineList = new List<string>();
            List<List<List<string>>> GlobalMatchList = new List<List<List<string>>>();
            List<List<List<string>>> LocalMatchList = new List<List<List<string>>>();

            #region 获取所有txt
            while ((pline = sr.ReadLine()) != null)
            {
                LineList.Add(pline);
            }
            #endregion

            #region 获取queues
            int i = 0; bool pLabel = false;
            List<List<string>> GlobalMatch = new List<List<string>>();
            List<List<string>> LocalMatch = new List<List<string>>();
            List<string> pGlobalMatch = new List<string>();
            List<string> pLocalMatch = new List<string>();
            foreach (string line in LineList)
            {
                #region 开启一个新循环
                if (line == "StartLabel")
                {
                    i = 0;

                    if (pLabel)
                    {
                        GlobalMatchList.Add(GlobalMatch);
                        LocalMatchList.Add(LocalMatch);
                    }

                    GlobalMatch = new List<List<string>>();
                    LocalMatch = new List<List<string>>();//不能是清空，注意地址引用与值引用
                    pLabel = true;
                }
                #endregion

                #region 添加每一个匹配对
                if (i % 13 == 0 && i != 0 && pLabel)
                {
                    GlobalMatch.Add(pGlobalMatch);
                    LocalMatch.Add(pLocalMatch);

                    pGlobalMatch = new List<string>();
                    pLocalMatch = new List<string>();
                }

                if (i % 13 == 1 && pLabel)
                {
                    pGlobalMatch.Add(line);
                }

                if (i % 13 == 2 && pLabel)
                {
                    pGlobalMatch.Add(line);
                }

                if (i % 13 == 10 && pLabel)
                {
                    pLocalMatch.Add(line);
                }

                if (i % 13 == 11 && pLabel)
                {
                    pLocalMatch.Add(line);
                }
                #endregion

                i++;
            }
            #endregion

            #region 对localMatch的调整
            foreach (List<List<string>> mLocalMatch in LocalMatchList)
            {
                foreach (List<string> nLocalMatch in mLocalMatch)
                {
                    int Index1 = nLocalMatch[0].IndexOf("[");
                    string M1 = nLocalMatch[0].Substring(2, Index1 - 4);

                    int Index2 = nLocalMatch[1].IndexOf("[");
                    string M2 = nLocalMatch[1].Substring(2, Index2 - 4);

                    nLocalMatch[0] = M1;
                    nLocalMatch[1] = M2;
                }
            }
            #endregion

            //int TestLocation = 0;

            #region 图形编码
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }

            List<IFeatureLayer> list2 = new List<IFeatureLayer>();
            if (this.comboBox4.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list2.Add(BuildingLayer);
            }

            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();

            SMap map2 = new SMap(list2);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();

            Dictionary<List<string>, List<List<double>>> SourceBuildingCode = this.GetCode(map.PolygonList, 1);//长度归一化编码
            Dictionary<List<string>, List<List<double>>> TargetBuildingCode = this.GetCode(map2.PolygonList, 1);
            #endregion

            //int TestLocation = 0;

            List<List<List<double>>> GlobalSim = this.GetSimilarity(GlobalMatchList, LocalMatchList, SourceBuildingCode, TargetBuildingCode, 2, 1, 2);//计算最大和最小的相似
            List<List<List<double>>> LocalSim = this.GetSimilarity(GlobalMatchList, LocalMatchList, SourceBuildingCode, TargetBuildingCode, 2, 2, 2);//计算最大和最小的相似

            #region 获取每一个匹配对下的最大值
            List<List<double>> pGlobalSim = new List<List<double>>();
            List<List<double>> pLocalSim = new List<List<double>>();

            for (int k = 0; k < GlobalSim.Count; k++)
            {
                List<double> pCacheGlobal = new List<double>();
                List<double> pCacheLocal = new List<double>();
                for (int p = 0; p < GlobalSim[k].Count; p++)
                {
                    pCacheGlobal.Add(GlobalSim[k][p].Max());
                    pCacheLocal.Add(LocalSim[k][p].Max());
                }
                pGlobalSim.Add(pCacheGlobal);
                pLocalSim.Add(pCacheLocal);
            }
            #endregion

            #region 输出
            System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\Simlarity.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);

            sw.Write("GlobalSim");
            sw.Write("\r\n");
            for (int k = 0; k < pGlobalSim.Count; k++)
            {
                for (int p = 0; p < pGlobalSim[k].Count; p++)
                {
                    sw.Write(pGlobalSim[k][p].ToString());
                    sw.Write("\r\n");
                }
                sw.Write("\r\n");
            }

            sw.Write("\r\n");
            sw.Write("LocalSim");
            sw.Write("\r\n");
            for (int k = 0; k < pLocalSim.Count; k++)
            {
                for (int p = 0; p < pLocalSim[k].Count; p++)
                {
                    sw.Write(pLocalSim[k][p].ToString());
                    sw.Write("\r\n");
                }
                sw.Write("\r\n");
            }

            sw.Close();
            fs.Close();
            #endregion

            //int TestLocation = 0;
        }

        /// <summary>
        /// 论文中相似度的计算
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button18_Click(object sender, EventArgs e)
        {
            StreamReader sr = new StreamReader(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\log_name.txt", Encoding.Default);
            String pline;
            List<string> LineList = new List<string>();
            List<List<List<string>>> GlobalMatchList = new List<List<List<string>>>();
            List<List<List<string>>> LocalMatchList = new List<List<List<string>>>();

            #region 获取所有txt
            while ((pline = sr.ReadLine()) != null)
            {
                LineList.Add(pline);
            }
            #endregion

            #region 获取queues
            int i = 0; bool pLabel = false;
            List<List<string>> GlobalMatch = new List<List<string>>();
            List<List<string>> LocalMatch = new List<List<string>>();
            List<string> pGlobalMatch = new List<string>();
            List<string> pLocalMatch = new List<string>();
            foreach (string line in LineList)
            {
                #region 开启一个新循环
                if (line == "StartLabel")
                {
                    i = 0;

                    if (pLabel)
                    {
                        GlobalMatchList.Add(GlobalMatch);
                        LocalMatchList.Add(LocalMatch);
                    }

                    GlobalMatch = new List<List<string>>();
                    LocalMatch = new List<List<string>>();//不能是清空，注意地址引用与值引用
                    pLabel = true;
                }
                #endregion

                #region 添加每一个匹配对
                if (i % 13 == 0 && i != 0 && pLabel)
                {
                    GlobalMatch.Add(pGlobalMatch);
                    LocalMatch.Add(pLocalMatch);

                    pGlobalMatch = new List<string>();
                    pLocalMatch = new List<string>();
                }

                if (i % 13 == 1 && pLabel)
                {
                    pGlobalMatch.Add(line);
                }

                if (i % 13 == 2 && pLabel)
                {
                    pGlobalMatch.Add(line);
                }

                if (i % 13 == 10 && pLabel)
                {
                    pLocalMatch.Add(line);
                }

                if (i % 13 == 11 && pLabel)
                {
                    pLocalMatch.Add(line);
                }
                #endregion

                i++;
            }
            #endregion

            #region 对localMatch的调整
            foreach (List<List<string>> mLocalMatch in LocalMatchList)
            {
                foreach (List<string> nLocalMatch in mLocalMatch)
                {
                    int Index1 = nLocalMatch[0].IndexOf("[");
                    string M1 = nLocalMatch[0].Substring(2, Index1 - 4);

                    int Index2 = nLocalMatch[1].IndexOf("[");
                    string M2 = nLocalMatch[1].Substring(2, Index2 - 4);

                    nLocalMatch[0] = M1;
                    nLocalMatch[1] = M2;
                }
            }
            #endregion

            //int TestLocation = 0;

            #region 图形编码
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }

            List<IFeatureLayer> list2 = new List<IFeatureLayer>();
            if (this.comboBox4.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list2.Add(BuildingLayer);
            }

            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();

            SMap map2 = new SMap(list2);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();

            Dictionary<List<string>, List<List<double>>> SourceBuildingCode = this.GetCode(map.PolygonList, 1);//长度归一化编码
            Dictionary<List<string>, List<List<double>>> TargetBuildingCode = this.GetCode(map2.PolygonList, 1);
            #endregion

            //int TestLocation = 0;

            List<List<List<double>>> GlobalSim = this.GetSimilarity(GlobalMatchList, LocalMatchList, SourceBuildingCode, TargetBuildingCode, 1, 1, 2);//计算最大和最小的相似
            List<List<List<double>>> LocalSim = this.GetSimilarity(GlobalMatchList, LocalMatchList, SourceBuildingCode, TargetBuildingCode, 1, 2, 2);//计算最大和最小的相似

            #region 获取每一个匹配对下的最大值
            List<List<double>> pGlobalSim = new List<List<double>>();
            List<List<double>> pLocalSim = new List<List<double>>();

            for (int k = 0; k < GlobalSim.Count; k++)
            {
                List<double> pCacheGlobal = new List<double>();
                List<double> pCacheLocal = new List<double>();
                for (int p = 0; p < GlobalSim[k].Count; p++)
                {
                    pCacheGlobal.Add(GlobalSim[k][p].Max());
                    pCacheLocal.Add(LocalSim[k][p].Max());
                }

                pGlobalSim.Add(pCacheGlobal);
                pLocalSim.Add(pCacheLocal);
            }
            #endregion

            #region 输出
            System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\Simlarity.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);

            sw.Write("GlobalSim");
            sw.Write("\r\n");
            for (int k = 0; k < pGlobalSim.Count; k++)
            {
                for (int p = 0; p < pGlobalSim[k].Count; p++)
                {
                    sw.Write(pGlobalSim[k][p].ToString());
                    sw.Write("\r\n");
                }
                sw.Write("\r\n");
            }

            sw.Write("\r\n");
            sw.Write("LocalSim");
            sw.Write("\r\n");
            for (int k = 0; k < pLocalSim.Count; k++)
            {
                for (int p = 0; p < pLocalSim[k].Count; p++)
                {
                    sw.Write(pLocalSim[k][p].ToString());
                    sw.Write("\r\n");
                }
                sw.Write("\r\n");
            }

            sw.Close();
            fs.Close();
            #endregion

            //int TestLocation = 0;
        }

        /// <summary>
        /// 讨论1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button19_Click(object sender, EventArgs e)
        {
            StreamReader sr = new StreamReader(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\log_name.txt", Encoding.Default);
            String pline;
            List<string> LineList = new List<string>();
            List<List<List<string>>> GlobalMatchList = new List<List<List<string>>>();
            List<List<List<string>>> LocalMatchList = new List<List<List<string>>>();

            #region 获取所有txt
            while ((pline = sr.ReadLine()) != null)
            {
                LineList.Add(pline);
            }
            #endregion

            #region 获取queues
            int i = 0; bool pLabel = false;
            List<List<string>> GlobalMatch = new List<List<string>>();
            List<List<string>> LocalMatch = new List<List<string>>();
            List<string> pGlobalMatch = new List<string>();
            List<string> pLocalMatch = new List<string>();
            foreach (string line in LineList)
            {
                #region 开启一个新循环
                if (line == "StartLabel")
                {
                    i = 0;

                    if (pLabel)
                    {
                        GlobalMatchList.Add(GlobalMatch);
                        LocalMatchList.Add(LocalMatch);
                    }

                    GlobalMatch = new List<List<string>>();
                    LocalMatch = new List<List<string>>();//不能是清空，注意地址引用与值引用
                    pLabel = true;
                }
                #endregion

                #region 添加每一个匹配对
                if (i % 13 == 0 && i != 0 && pLabel)
                {
                    GlobalMatch.Add(pGlobalMatch);
                    LocalMatch.Add(pLocalMatch);

                    pGlobalMatch = new List<string>();
                    pLocalMatch = new List<string>();
                }

                if (i % 13 == 1 && pLabel)
                {
                    pGlobalMatch.Add(line);
                }

                if (i % 13 == 2 && pLabel)
                {
                    pGlobalMatch.Add(line);
                }

                if (i % 13 == 10 && pLabel)
                {
                    pLocalMatch.Add(line);
                }

                if (i % 13 == 11 && pLabel)
                {
                    pLocalMatch.Add(line);
                }
                #endregion

                i++;
            }
            #endregion

            #region 对localMatch的调整
            foreach (List<List<string>> mLocalMatch in LocalMatchList)
            {
                foreach (List<string> nLocalMatch in mLocalMatch)
                {
                    int Index1 = nLocalMatch[0].IndexOf("[");
                    string M1 = nLocalMatch[0].Substring(2, Index1 - 4);

                    int Index2 = nLocalMatch[1].IndexOf("[");
                    string M2 = nLocalMatch[1].Substring(2, Index2 - 4);

                    nLocalMatch[0] = M1;
                    nLocalMatch[1] = M2;
                }
            }
            #endregion

            //int TestLocation = 0;

            #region 图形编码
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }

            List<IFeatureLayer> list2 = new List<IFeatureLayer>();
            if (this.comboBox4.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list2.Add(BuildingLayer);
            }

            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();

            SMap map2 = new SMap(list2);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();

            Dictionary<List<string>, List<List<double>>> SourceBuildingCode = this.GetCode(map.PolygonList, 1);//长度归一化编码
            Dictionary<List<string>, List<List<double>>> TargetBuildingCode = this.GetCode(map2.PolygonList, 1);
            #endregion

            //int TestLocation = 0;

            List<List<List<double>>> GlobalSim = this.GetSimilarity(GlobalMatchList, LocalMatchList, SourceBuildingCode, TargetBuildingCode, 1, 1, 2);//计算最大和最小的相似
            List<List<List<double>>> LocalSim = this.GetSimilarity(GlobalMatchList, LocalMatchList, SourceBuildingCode, TargetBuildingCode, 1, 2, 2);//计算最大和最小的相似

            #region 获取每一个匹配对下的最大值
            List<List<double>> pGlobalSim = new List<List<double>>();
            List<List<double>> pLocalSim = new List<List<double>>();

            for (int k = 0; k < GlobalSim.Count; k++)
            {
                List<double> pCacheGlobal = new List<double>();
                List<double> pCacheLocal = new List<double>();
                for (int p = 0; p < GlobalSim[k].Count; p++)
                {
                    pCacheGlobal.Add(GlobalSim[k][p].Max());
                    pCacheLocal.Add(LocalSim[k][p].Max());
                }
                pGlobalSim.Add(pCacheGlobal);
                pLocalSim.Add(pCacheLocal);
            }
            #endregion

            #region 输出
            System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\Simlarity.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);

            sw.Write("GlobalSim");
            sw.Write("\r\n");
            for (int k = 0; k < pGlobalSim.Count; k++)
            {
                for (int p = 0; p < pGlobalSim[k].Count; p++)
                {
                    sw.Write(pGlobalSim[k][p].ToString());
                    sw.Write("\r\n");
                }
                sw.Write("\r\n");
            }

            sw.Write("\r\n");
            sw.Write("LocalSim");
            sw.Write("\r\n");
            for (int k = 0; k < pLocalSim.Count; k++)
            {
                for (int p = 0; p < pLocalSim[k].Count; p++)
                {
                    sw.Write(pLocalSim[k][p].ToString());
                    sw.Write("\r\n");
                }
                sw.Write("\r\n");
            }

            sw.Close();
            fs.Close();
            #endregion

            //int TestLocation = 0;
        }

        /// <summary>
        /// 讨论3
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button21_Click(object sender, EventArgs e)
        {
            StreamReader sr = new StreamReader(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\log_name.txt", Encoding.Default);
            String pline;
            List<string> LineList = new List<string>();
            List<List<List<string>>> GlobalMatchList = new List<List<List<string>>>();
            List<List<List<string>>> LocalMatchList = new List<List<List<string>>>();

            #region 获取所有txt
            while ((pline = sr.ReadLine()) != null)
            {
                LineList.Add(pline);
            }
            #endregion

            #region 获取queues
            int i = 0; bool pLabel = false;
            List<List<string>> GlobalMatch = new List<List<string>>();
            List<List<string>> LocalMatch = new List<List<string>>();
            List<string> pGlobalMatch = new List<string>();
            List<string> pLocalMatch = new List<string>();
            foreach (string line in LineList)
            {
                #region 开启一个新循环
                if (line == "StartLabel")
                {
                    i = 0;

                    if (pLabel)
                    {
                        GlobalMatchList.Add(GlobalMatch);
                        LocalMatchList.Add(LocalMatch);
                    }

                    GlobalMatch = new List<List<string>>();
                    LocalMatch = new List<List<string>>();//不能是清空，注意地址引用与值引用
                    pLabel = true;
                }
                #endregion

                #region 添加每一个匹配对
                if (i % 13 == 0 && i != 0 && pLabel)
                {
                    GlobalMatch.Add(pGlobalMatch);
                    LocalMatch.Add(pLocalMatch);

                    pGlobalMatch = new List<string>();
                    pLocalMatch = new List<string>();
                }

                if (i % 13 == 1 && pLabel)
                {
                    pGlobalMatch.Add(line);
                }

                if (i % 13 == 2 && pLabel)
                {
                    pGlobalMatch.Add(line);
                }

                if (i % 13 == 10 && pLabel)
                {
                    pLocalMatch.Add(line);
                }

                if (i % 13 == 11 && pLabel)
                {
                    pLocalMatch.Add(line);
                }
                #endregion

                i++;
            }
            #endregion

            #region 对localMatch的调整
            foreach (List<List<string>> mLocalMatch in LocalMatchList)
            {
                foreach (List<string> nLocalMatch in mLocalMatch)
                {
                    int Index1 = nLocalMatch[0].IndexOf("[");
                    string M1 = nLocalMatch[0].Substring(2, Index1 - 4);

                    int Index2 = nLocalMatch[1].IndexOf("[");
                    string M2 = nLocalMatch[1].Substring(2, Index2 - 4);

                    nLocalMatch[0] = M1;
                    nLocalMatch[1] = M2;
                }
            }
            #endregion

            //int TestLocation = 0;

            #region 图形编码
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }

            List<IFeatureLayer> list2 = new List<IFeatureLayer>();
            if (this.comboBox4.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list2.Add(BuildingLayer);
            }

            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();

            SMap map2 = new SMap(list2);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();

            Dictionary<List<string>, List<List<double>>> SourceBuildingCode = this.GetCode(map.PolygonList, 1);//长度归一化编码
            Dictionary<List<string>, List<List<double>>> TargetBuildingCode = this.GetCode(map2.PolygonList, 1);
            #endregion

            //int TestLocation = 0;

            List<List<List<double>>> GlobalSim = this.GetSimilarity(GlobalMatchList, LocalMatchList, SourceBuildingCode, TargetBuildingCode, 1, 1, 2);//计算最大和最小的相似
            List<List<List<double>>> LocalSim = this.GetSimilarity(GlobalMatchList, LocalMatchList, SourceBuildingCode, TargetBuildingCode, 1, 2, 2);//计算最大和最小的相似

            #region 获取每一个匹配对下的最大值
            List<List<double>> pGlobalSim = new List<List<double>>();
            List<List<double>> pLocalSim = new List<List<double>>();

            for (int k = 0; k < GlobalSim.Count; k++)
            {
                List<double> pCacheGlobal = new List<double>();
                List<double> pCacheLocal = new List<double>();
                for (int p = 0; p < GlobalSim[k].Count; p++)
                {
                    pCacheGlobal.Add(GlobalSim[k][p].Max());
                    pCacheLocal.Add(LocalSim[k][p].Max());
                }
                pGlobalSim.Add(pCacheGlobal);
                pLocalSim.Add(pCacheLocal);
            }
            #endregion

            #region 输出
            System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\Simlarity.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);

            sw.Write("GlobalSim");
            sw.Write("\r\n");
            for (int k = 0; k < pGlobalSim.Count; k++)
            {
                for (int p = 0; p < pGlobalSim[k].Count; p++)
                {
                    sw.Write(pGlobalSim[k][p].ToString());
                    sw.Write("\r\n");
                }
                sw.Write("\r\n");
            }

            sw.Write("\r\n");
            sw.Write("LocalSim");
            sw.Write("\r\n");
            for (int k = 0; k < pLocalSim.Count; k++)
            {
                for (int p = 0; p < pLocalSim[k].Count; p++)
                {
                    sw.Write(pLocalSim[k][p].ToString());
                    sw.Write("\r\n");
                }
                sw.Write("\r\n");
            }

            sw.Close();
            fs.Close();
            #endregion

            //int TestLocation = 0;
        }

        /// <summary>
        /// 投稿论文中的简化调整
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button20_Click(object sender, EventArgs e)
        {
            PS.pMapControl = pMapControl;//测试用

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

            #region 数据读取并内插
            SMap map3 = new SMap(list);//测试用
            map3.ReadDateFrmEsriLyrsForEnrichNetWork();//测试用
            SMap map2 = new SMap();//测试用
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();

            PS.pMap = map2;//测试用
            #endregion

            #region 简化
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                Thread.Sleep(1000);//测试用

                PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
                object PolygonSymbol = Sb.PolygonSymbolization(2, 100, 100, 100, 0, 0, 20, 20);
                IPolygon CacheMapPo = this.PolygonObjectConvert(map.PolygonList[i]);
                pMapControl.DrawShape(CacheMapPo, ref PolygonSymbol);
                pMapControl.Map.RecalcFullExtent();

                //map2.PolygonList.Add(map.PolygonList[i]);

                #region 符号化
                bool Label = false;
                map.PolygonList[i] = Symbol.SymbolizedPolygon(map.PolygonList[i], 50000, 0.7, 0.5, out Label);
                #endregion

                #region 简化
                if (!Label)
                {
                    PolygonObject CachePolygon1 = map.PolygonList[i];
                    PP.DeleteSamePoint(CachePolygon1, 0.01);                    
                    //map2.PolygonList.Add(CachePolygon1);//测试用

                    PP.DeleteOnLinePoint(CachePolygon1, Pi / 36);
                    //map2.PolygonList.Add(CachePolygon1);//测试用

                    PP.DeleteSmallAngle(CachePolygon1, Pi / 36);


                    //PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
                    //object PolygonSymbol = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);
                    //IPolygon CacheCurPo = this.PolygonObjectConvert(CachePolygon1);
                    //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
                    //pMapControl.Map.RecalcFullExtent();

                    PolygonObject CachePolygon2 = CachePolygon1;
                    double ShortDis = 0;
                    List<TriNode> ShortestEdge = PP.GetShortestEdge(CachePolygon1, out ShortDis);
                    //ShortDis = 100;

                    #region 判断使简化后的最短边大于阈值
                    while (ShortDis < 15 && CachePolygon1.PointList.Count > 4)
                    {
                        bool sLabel = false;//false表示简化成功；true表示简化失败
                        CachePolygon1 = PS.PolygonSimplified3(map.PolygonList[i], CachePolygon1, Pi / 12, 15, 0.3,30, out sLabel);
                        if (CachePolygon1 != null)
                        {
                            PP.DeleteOnLinePoint(CachePolygon1, Pi / 36);
                            PP.DeleteSamePoint(CachePolygon1, 0.01);
                            PP.DeleteSmallAngle(CachePolygon1, Pi / 36);

                            ShortestEdge = PP.GetShortestEdge(CachePolygon1, out ShortDis);
                            //ShortDis = 100;//测试用
                            map2.PolygonList.Add(CachePolygon1);//测试用
                            //map2.PolygonList.Add(CachePolygon1);//测试用

                            CachePolygon2 = CachePolygon1;
                        }
                        else
                        {
                            ShortDis = 100000;

                            if (sLabel)
                            {
                                CachePolygon2.SimLabel = 1;
                            }
                        }
                    }
                    #endregion

                    //添加测试用
                    //PP.DeleteOnLinePoint(CachePolygon2, Pi / 36);
                    //PP.DeleteSamePoint(CachePolygon2, 0.01);

                    map.PolygonList[i] = CachePolygon2;
                }
                #endregion

            }
            #endregion

            #region 输出
            //map3.WriteResult2Shp(OutPath, pMap.SpatialReference);//测试用
            //map2.WriteResult2Shp(OutPath, pMap.SpatialReference);//测试用
            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
            #endregion
        }

        /// <summary>
        /// 将建筑物转化为IPolygon
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

            curPoint = pPolygonObject.PointList[0];
            curResultPoint.PutCoords(curPoint.X, curPoint.Y);
            ring1.AddPoint(curResultPoint, ref missing, ref missing);

            IGeometryCollection pointPolygon = new PolygonClass();
            pointPolygon.AddGeometry(ring1 as IGeometry, ref missing, ref missing);
            IPolygon pPolygon = pointPolygon as IPolygon;

            //PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
            //object PolygonSymbol = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);

            //pMapControl.DrawShape(pPolygon, ref PolygonSymbol);
            //pMapControl.Map.RecalcFullExtent();

            pPolygon.SimplifyPreserveFromTo();
            return pPolygon;
        }

        /// <summary>
        /// polygon转换成polygonobject
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <returns></returns>
        public PolygonObject PolygonConvert(IPolygon pPolygon)
        {
            int ppID = 0;//（polygonobject自己的编号，应该无用）
            List<TriNode> trilist = new List<TriNode>();
            //Polygon的点集
            IPointCollection pointSet = pPolygon as IPointCollection;
            int count = pointSet.PointCount;
            double curX;
            double curY;
            //ArcGIS中，多边形的首尾点重复存储
            for (int i = 0; i < count - 1; i++)
            {
                curX = pointSet.get_Point(i).X;
                curY = pointSet.get_Point(i).Y;
                //初始化每个点对象
                TriNode tPoint = new TriNode(curX, curY, ppID, 1);
                trilist.Add(tPoint);
            }
            //生成自己写的多边形
            PolygonObject mPolygonObject = new PolygonObject(ppID, trilist);

            return mPolygonObject;
        }

        /// <summary>
        /// 若建筑物符号化则标识该建筑物
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button22_Click(object sender, EventArgs e)
        {
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

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            //System.IO.FileStream fsEdgeID = new System.IO.FileStream(@"C:\Users\10988\Desktop\Original data\AFPM50000EdgeID.txt", System.IO.FileMode.OpenOrCreate);
            //System.IO.FileStream fsAreaID = new System.IO.FileStream(@"C:\Users\10988\Desktop\Original data\AFPM50000AreaID.txt", System.IO.FileMode.OpenOrCreate);
            //StreamWriter swEdgeID = new StreamWriter(fsEdgeID);
            //StreamWriter swAreaID = new StreamWriter(fsAreaID);

            int Count1 = 0;
            int Count2 = 0;
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                double ShortDis = 0;
                List<TriNode> ShortestEdge = PP.GetShortestEdge(map.PolygonList[i], out ShortDis);
                if (ShortDis < 7.5)
                {
                    //swEdgeID.Write(i.ToString()); swEdgeID.Write("\r\n");
                    Count2++;
                }

                #region 符号化
                bool Label = false;
                map.PolygonList[i] = Symbol.SymbolizedPolygon(map.PolygonList[i], 25000, 0.7, 0.5, out Label);
                #endregion

                if (Label)
                {
                    //swAreaID.Write(i.ToString()); swAreaID.Write("\r\n");
                    Count1++;
                }
            }

            //swEdgeID.Close();
            //fsEdgeID.Close();
            //swAreaID.Close();
            //fsAreaID.Close();
            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
        }

        /// <summary>
        /// 角度和结构特征统计
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button23_Click(object sender, EventArgs e)
        {
            #region 获取图层
            IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            #endregion

            #region 遍历并统计计算
            IQueryFilter queryFilter = new QueryFilterClass();
            IFeatureCursor pFeatureCursor = BuildingLayer.Search(queryFilter, false);
            IFeature pFeature = pFeatureCursor.NextFeature();

            #region 计数
            int OAngleCount = 0;//直角计数
            int NonOAngleCount = 0;//非直角计数
            int NodeSCountBuilding = 0;//小于建筑物节点阈值的建筑物数量
            int NodeBCountBuilding = 0;//大于建筑物节点阈值的建筑物数量
            int ACount = 0;
            int BCount = 0;
            int CCount = 0;
            int DCount = 0;
            int ECount = 0;
            int FCount = 0;
            int GCount = 0;
            int HCount = 0;
            int ICount = 0;
            int JCount = 0;
            int KCount = 0;
            int LCount = 0;
            int MCount = 0;
            int NCount = 0;
            int OCount = 0;
            int PCount = 0;
            int QCount = 0;
            int RCount = 0;
            int SCount = 0;
            int TCount = 0;
            int UCount = 0;
            int VCount = 0;
            int WCount = 0;
            int XCount = 0;
            int YCount = 0;
            int ZCount = 0;
            int Count0 = 0;
            int Count1 = 0;
            int Count2 = 0;
            int Count3 = 0;
            int Count4 = 0;
            int Count5 = 0;
            int Count6 = 0;
            int Count7 = 0;
            int Count8 = 0;
            int Count9 = 0;
            #endregion

            while (pFeature != null)
            {
                IPolygon pPolygon = pFeature.Shape as IPolygon;
                PolygonObject pPolygonObject = PolygonConvert(pPolygon);
                pPolygonObject.GetBendAngle2();

                if (pPolygonObject.PointList.Count <= 8)
                {
                    NodeSCountBuilding++;
                }
                else
                {
                    NodeBCountBuilding++;
                }

                #region 计算角度特征
                for (int i = 0; i < pPolygonObject.BendAngle.Count; i++)
                {
                    double Angle1 = pPolygonObject.BendAngle[i][1];
                    int Label1 = this.GetLabel2(Angle1, 2);

                    if (Label1 == 2 || Label1 == 5)
                    {
                        OAngleCount++;
                    }

                    else
                    {
                        NonOAngleCount++;
                    }
                }
                #endregion

                #region 计算结构特征
                for (int i = 0; i < pPolygonObject.BendAngle.Count; i++)
                {
                    #region 计算角度
                    double Angle1 = pPolygonObject.BendAngle[i][1];
                    int Label1 = this.GetLabel2(Angle1, 2);

                    double Angle2 = 0;
                    if (i == pPolygonObject.BendAngle.Count - 1)
                    {
                        Angle2 = pPolygonObject.BendAngle[0][1];
                    }

                    else
                    {
                        Angle2 = pPolygonObject.BendAngle[i + 1][1];
                    }
                    int Label2 = this.GetLabel2(Angle2, 2);
                    #endregion

                    #region 编码
                    if (Label1 == 1)
                    {
                        switch (Label2)
                        {
                            case 1: ACount++; break;
                            case 2: BCount++; break;
                            case 3: CCount++; break;
                            case 4: JCount++; break;
                            case 5: KCount++; break;
                            case 6: LCount++; break;
                        }
                    }

                    else if (Label1 == 2)
                    {
                        switch (Label2)
                        {
                            case 1: DCount++; break;
                            case 2: ECount++; break;
                            case 3: FCount++; break;
                            case 4: MCount++; break;
                            case 5: NCount++; break;
                            case 6: OCount++; break;
                        }
                    }

                    else if (Label1 == 3)
                    {
                        switch (Label2)
                        {
                            case 1: GCount++; break;
                            case 2: HCount++; break;
                            case 3: ICount++; break;
                            case 4: PCount++; break;
                            case 5: QCount++; break;
                            case 6: RCount++; break;
                        }
                    }

                    else if (Label1 == 4)
                    {
                        switch (Label2)
                        {
                            case 1: SCount++; break;
                            case 2: TCount++; break;
                            case 3: UCount++; break;
                            case 4: Count1++; break;
                            case 5: Count2++; break;
                            case 6: Count3++; break;
                        }
                    }

                    else if (Label1 == 5)
                    {
                        switch (Label2)
                        {
                            case 1: VCount++; break;
                            case 2: WCount++; break;
                            case 3: XCount++; break;
                            case 4: Count4++; break;
                            case 5: Count5++; break;
                            case 6: Count6++; break;
                        }
                    }

                    else if (Label1 == 6)
                    {
                        switch (Label2)
                        {
                            case 1: YCount++; break;
                            case 2: ZCount++; break;
                            case 3: Count0++; break;
                            case 4: Count7++; break;
                            case 5: Count8++; break;
                            case 6: Count9++; break;
                        }
                    }
                    #endregion
                }
                #endregion

                pFeature = pFeatureCursor.NextFeature();
            }
            #endregion

            #region 输出
            System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\10988\Desktop\SZAngleStas.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);

            sw.Write("OAngleCount:"+OAngleCount.ToString()); sw.Write("\r\n");
            sw.Write("NonOAngleCount:" + NonOAngleCount.ToString()); sw.Write("\r\n");
            sw.Write("NodeBCountBuilding:" + NodeBCountBuilding.ToString()); sw.Write("\r\n");
            sw.Write("NodeSCountBuilding" + NodeSCountBuilding.ToString());
            //sw.Write("ACount:" + ACount.ToString()); sw.Write("\r\n");
            //sw.Write("BCount:" + BCount.ToString()); sw.Write("\r\n");
            //sw.Write("CCount:" + CCount.ToString()); sw.Write("\r\n");
            //sw.Write("DCount:" + DCount.ToString()); sw.Write("\r\n");
            //sw.Write("ECount:" + ECount.ToString()); sw.Write("\r\n");
            //sw.Write("FCount:" + FCount.ToString()); sw.Write("\r\n");
            //sw.Write("GCount:" + GCount.ToString()); sw.Write("\r\n");
            //sw.Write("HCount:" + HCount.ToString()); sw.Write("\r\n");
            //sw.Write("ICount:" + ICount.ToString()); sw.Write("\r\n");
            //sw.Write("JCount:" + JCount.ToString()); sw.Write("\r\n");
            //sw.Write("KCount:" + KCount.ToString()); sw.Write("\r\n");
            //sw.Write("LCount:" + LCount.ToString()); sw.Write("\r\n");
            //sw.Write("MCount:" + MCount.ToString()); sw.Write("\r\n");
            //sw.Write("NCount:" + NCount.ToString()); sw.Write("\r\n");
            //sw.Write("OCount:" + OCount.ToString()); sw.Write("\r\n");
            //sw.Write("PCount:" + PCount.ToString()); sw.Write("\r\n");
            //sw.Write("QCount:" + QCount.ToString()); sw.Write("\r\n");
            //sw.Write("RCount:" + RCount.ToString()); sw.Write("\r\n");
            //sw.Write("SCount:" + SCount.ToString()); sw.Write("\r\n");
            //sw.Write("TCount:" + TCount.ToString()); sw.Write("\r\n");
            //sw.Write("UCount:" + UCount.ToString()); sw.Write("\r\n");
            //sw.Write("VCount:" + VCount.ToString()); sw.Write("\r\n");
            //sw.Write("WCount:" + WCount.ToString()); sw.Write("\r\n");
            //sw.Write("XCount:" + XCount.ToString()); sw.Write("\r\n");
            //sw.Write("YCount:" + YCount.ToString()); sw.Write("\r\n");
            //sw.Write("ZCount:" + ZCount.ToString()); sw.Write("\r\n");
            //sw.Write("Count0:" + Count0.ToString()); sw.Write("\r\n");
            //sw.Write("Count1:" + Count1.ToString()); sw.Write("\r\n");
            //sw.Write("Count2:" + Count2.ToString()); sw.Write("\r\n");
            //sw.Write("Count3:" + Count3.ToString()); sw.Write("\r\n");
            //sw.Write("Count4:" + Count4.ToString()); sw.Write("\r\n");
            //sw.Write("Count5:" + Count5.ToString()); sw.Write("\r\n");
            //sw.Write("Count6:" + Count6.ToString()); sw.Write("\r\n");
            //sw.Write("Count7:" + Count7.ToString()); sw.Write("\r\n");
            //sw.Write("Count8:" + Count8.ToString()); sw.Write("\r\n");
            //sw.Write("Count9:" + Count9.ToString()); sw.Write("\r\n");
            sw.Close();
            fs.Close();
            #endregion
        }

        /// <summary>
        /// 第一稿论文修改
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button24_Click(object sender, EventArgs e)
        {
            #region 获得建筑物图形
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }

            List<IFeatureLayer> list2 = new List<IFeatureLayer>();
            if (this.comboBox4.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list2.Add(BuildingLayer);
            }

            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();//TargetBuilding

            SMap map2 = new SMap(list2);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();//SourceBuilding
            #endregion

            #region 相似度计算
            double[,] GlobalSimMatrixGraph = new double[map.PolygonList.Count, map2.PolygonList.Count];
            double[,] LocalSimMatrixGraph = new double[map.PolygonList.Count, map2.PolygonList.Count];
            double[,] FinalSimMatrixGraph = new double[map.PolygonList.Count, map2.PolygonList.Count];
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                PolygonObject Po1 = map.PolygonList[i];

                for (int j = 0; j < map2.PolygonList.Count; j++)
                {
                    PolygonObject Po2 = map2.PolygonList[j];

                    Dictionary<string, List<double>> SourceBuildingCode = this.GetCode(Po1, 1);//长度归一化编码
                    Dictionary<string, List<double>> TargetBuildingCode = this.GetCode(Po2, 1);//长度归一化编码

                    #region 循环不同起点的编码，获取最大相似度
                    List<double> GlobalSimList = new List<double>();//相似度列表
                    List<double> LocalSimList = new List<double>();
                    List<string> SourceCode = SourceBuildingCode.Keys.ToList();
                    List<List<double>> SourceCodeLength = SourceBuildingCode.Values.ToList();
                    List<string> TargetCode = TargetBuildingCode.Keys.ToList();
                    List<List<double>> TargetCodelength = TargetBuildingCode.Values.ToList();
                    for (int m = 0; m < SourceCode.Count; m++)
                    {
                        for (int n = 0; n < TargetCode.Count; n++)
                        {
                            double GlobalSim = this.GetGlobalSim(SourceCode, SourceCodeLength, TargetCode, TargetCodelength, -0.8, -0.5, 1, 0.8, 0.2, 4);
                            double LocalSim = this.GetLocalSim(SourceCode, SourceCodeLength, TargetCode, TargetCodelength, -0.8, -0.5, 1, 0.8, 0.2, 4);
                            GlobalSimList.Add(GlobalSim);
                            LocalSimList.Add(LocalSim);

                            String tFirstCode = TargetCode[0]; List<double> tFirstCodeLength = TargetCodelength[0];
                            TargetCode.Remove(tFirstCode); TargetCode.Add(tFirstCode);
                            TargetCodelength.Remove(tFirstCodeLength); TargetCodelength.Add(tFirstCodeLength);
                        }

                        String sFirstCode = SourceCode[0]; List<double> sFirstCodeLength = SourceCodeLength[0];
                        SourceCode.Remove(sFirstCode); SourceCode.Add(sFirstCode);
                        SourceCodeLength.Remove(sFirstCodeLength); SourceCodeLength.Add(sFirstCodeLength);
                    }
                    #endregion

                    GlobalSimMatrixGraph[i, j] = GlobalSimList.Max();
                    LocalSimMatrixGraph[i, j] = LocalSimList.Max();
                    FinalSimMatrixGraph[i, j] = this.GetFinalSim(GlobalSimList.Max(), LocalSimList.Max(), 2, 0.7, 0.3);
                }
            }
            #endregion

            #region 输出
            System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\Simlarity.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);

            sw.Write("GlobalSim");
            sw.Write("\r\n");
            for (int i= 0;  i< map.PolygonList.Count; i++)
            {
                for (int j = 0; j < map2.PolygonList.Count; j++)
                {
                    sw.Write(GlobalSimMatrixGraph[i,j].ToString());
                    sw.Write("\r\n");
                }
                sw.Write("\r\n");
            }

            sw.Write("\r\n");
            sw.Write("LocalSim");
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                for (int j = 0; j < map2.PolygonList.Count; j++)
                {
                    sw.Write(LocalSimMatrixGraph[i, j].ToString());
                    sw.Write("\r\n");
                }
                sw.Write("\r\n");
            }

            sw.Write("\r\n");
            sw.Write("FinalSim");
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                for (int j = 0; j < map2.PolygonList.Count; j++)
                {
                    sw.Write(FinalSimMatrixGraph[i, j].ToString());
                    sw.Write("\r\n");
                }
                sw.Write("\r\n");
            }

            sw.Close();
            fs.Close();
            #endregion
        }

        /// <summary>
        /// </summary>
        /// <param name="Po1"></param>建筑物1
        /// <param name="Po2"></param>建筑物2
        /// <param name="Label"></param>编码类型，1表示长度归一化编码，2表示长度非归一化编码
        /// <param name="d"></param>罚分
        /// <param name="e"></param>考虑extending罚分
        /// <param name="MatchType"></param>计算得分时考虑对应元素的长度匹配关系
        /// <param name="AngleWeight"></param>角度相似性考虑
        /// <param name="LengthWeight"></param>长度相似性考虑
        /// <param name="GapLabel"></param>
        /// =1不考虑extending；=2考虑extending，但不考虑extending长度；=3考虑extending长度;=4（gap和MisMatch分开考虑）
        /// <param name="SimType"></param>计算相似度的方法=1加法；=2乘法
        /// <param name="GlobalWeight"></param>
        /// <param name="LocalWeight"></param>
        /// <returns></returns>
        public double GetPolygonSim(PolygonObject Po1,PolygonObject Po2,int Label,double d, double e, int MatchType, double AngleWeight, double LengthWeight,int GapLabel,int SimType,double GlobalWeight,double LocalWeight)
        {
            double PolygonSim=0;

            #region 对建筑物图形长度和角度进行编码
            Dictionary<string, List<double>> SourceBuildingCode = this.GetCode(Po1, Label);//长度归一化编码(Label=1 长度归一化编码)
            Dictionary<string, List<double>> TargetBuildingCode = this.GetCode(Po2, Label);//长度归一化编码（Label=2长度不归一化编码）
            #endregion

            #region 循环不同起点的编码，获取最大相似度
            List<double> GlobalSimList = new List<double>();//相似度列表
            List<double> LocalSimList = new List<double>();
            List<string> SourceCode = SourceBuildingCode.Keys.ToList();
            List<List<double>> SourceCodeLength = SourceBuildingCode.Values.ToList();
            List<string> TargetCode = TargetBuildingCode.Keys.ToList();
            List<List<double>> TargetCodelength = TargetBuildingCode.Values.ToList();
            for (int m = 0; m < SourceCode.Count; m++)
            {
                for (int n = 0; n < TargetCode.Count; n++)
                {
                    double GlobalSim = this.GetGlobalSim(SourceCode, SourceCodeLength, TargetCode, TargetCodelength, d, e, MatchType, AngleWeight, LengthWeight, GapLabel);
                    double LocalSim = this.GetLocalSim(SourceCode, SourceCodeLength, TargetCode, TargetCodelength, d, e, MatchType, AngleWeight, LengthWeight, GapLabel);
                    GlobalSimList.Add(GlobalSim);
                    LocalSimList.Add(LocalSim);

                    String tFirstCode = TargetCode[0]; List<double> tFirstCodeLength = TargetCodelength[0];
                    TargetCode.Remove(tFirstCode); TargetCode.Add(tFirstCode);
                    TargetCodelength.Remove(tFirstCodeLength); TargetCodelength.Add(tFirstCodeLength);
                }

                String sFirstCode = SourceCode[0]; List<double> sFirstCodeLength = SourceCodeLength[0];
                SourceCode.Remove(sFirstCode); SourceCode.Add(sFirstCode);
                SourceCodeLength.Remove(sFirstCodeLength); SourceCodeLength.Add(sFirstCodeLength);
            }
            #endregion

            #region 全局相似度与局部相似度的加权
            double GlobalMax = GlobalSimList.Max();
            double LocalMax = LocalSimList.Max();
            PolygonSim = this.GetFinalSim(GlobalSimList.Max(), LocalSimList.Max(), SimType, GlobalWeight, LocalWeight);//全局相似性与局部相似性的加权
            #endregion

            return PolygonSim;
        }

        /// <summary>
        /// 计算最终相似度
        /// </summary>
        /// <param name="GlobalSim"></param>全局相似度
        /// <param name="LocalSim"></param>局部相似度
        /// <param name="SimType"></param>计算相似度的方法=1加法；=2乘法
        /// <param name="GlobalWeight"></param>
        /// <param name="LocalWeight"></param>
        /// <returns></returns>
        double GetFinalSim(double GlobalSim,double LocalSim,int SimType,double GlobalWeight,double LocalWeight)
        {
            double FinalSim = 0;

            #region 加法相似度
            if (SimType == 1)
            {
                FinalSim = GlobalSim * GlobalWeight + LocalSim * LocalWeight;
            }
            #endregion

            #region 乘法相似度
            if (SimType == 2)
            {
                FinalSim = Math.Sqrt(Math.Abs(GlobalSim) * LocalSim);

                if (GlobalSim < 0)
                {
                    FinalSim = FinalSim * (-1);
                }
            }
            #endregion

            return FinalSim;
        }

        /// <summary>
        /// 配对计算
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button25_Click(object sender, EventArgs e)
        {
            #region 获得建筑物图形
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }

            List<IFeatureLayer> list2 = new List<IFeatureLayer>();
            if (this.comboBox4.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list2.Add(BuildingLayer);
            }

            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();//TargetBuilding

            SMap map2 = new SMap(list2);
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();//SourceBuilding
            #endregion

            #region 读取制定的建筑物
            Dictionary<int, PolygonObject> SourcePolygonList = new Dictionary<int, PolygonObject>();
            Dictionary<int, PolygonObject> TargetPolygonList = new Dictionary<int, PolygonObject>();

            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                if (!SourcePolygonList.ContainsKey(map.PolygonList[i].ClassID))
                {
                    SourcePolygonList.Add(map.PolygonList[i].ClassID, map.PolygonList[i]);
                }
            }

            for (int j = 0; j < map2.PolygonList.Count; j++)
            {
                if (!TargetPolygonList.ContainsKey(map2.PolygonList[j].ClassID))
                {
                    TargetPolygonList.Add(map2.PolygonList[j].ClassID, map2.PolygonList[j]);
                }
            }
            #endregion

            System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\10988\Desktop\图形简化与符号相似性度量\MatchSimlarity.txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);

            foreach (KeyValuePair<int, PolygonObject> kv in SourcePolygonList)
            {
                int SourceID = kv.Key;
                PolygonObject SourcePolygon = kv.Value;
                 
                if (TargetPolygonList.ContainsKey(SourceID))
                {
                    PolygonObject TargetPolygon = TargetPolygonList[SourceID];

                    #region 计算相似度
                    Dictionary<string, List<double>> SourceBuildingCode = this.GetCode(SourcePolygon, 1);//长度归一化编码
                    Dictionary<string, List<double>> TargetBuildingCode = this.GetCode(TargetPolygon, 1);//长度归一化编码

                    List<double> GlobalSimList = new List<double>();//相似度列表
                    List<double> LocalSimList = new List<double>();
                    List<string> SourceCode = SourceBuildingCode.Keys.ToList();
                    List<List<double>> SourceCodeLength = SourceBuildingCode.Values.ToList();
                    List<string> TargetCode = TargetBuildingCode.Keys.ToList();
                    List<List<double>> TargetCodelength = TargetBuildingCode.Values.ToList();
                    for (int m = 0; m < SourceCode.Count; m++)
                    {
                        for (int n = 0; n < TargetCode.Count; n++)
                        {
                            double GlobalSim = this.GetGlobalSim(SourceCode, SourceCodeLength, TargetCode, TargetCodelength, -0.8, -0.5, 1, 0.8, 0.2, 4);
                            double LocalSim = this.GetLocalSim(SourceCode, SourceCodeLength, TargetCode, TargetCodelength, -0.8, -0.5, 1, 0.8, 0.2, 4);
                            GlobalSimList.Add(GlobalSim);
                            LocalSimList.Add(LocalSim);

                            String tFirstCode = TargetCode[0]; List<double> tFirstCodeLength = TargetCodelength[0];
                            TargetCode.Remove(tFirstCode); TargetCode.Add(tFirstCode);
                            TargetCodelength.Remove(tFirstCodeLength); TargetCodelength.Add(tFirstCodeLength);
                        }

                        String sFirstCode = SourceCode[0]; List<double> sFirstCodeLength = SourceCodeLength[0];
                        SourceCode.Remove(sFirstCode); SourceCode.Add(sFirstCode);
                        SourceCodeLength.Remove(sFirstCodeLength); SourceCodeLength.Add(sFirstCodeLength);
                    }

                    Double FinalSim = this.GetFinalSim(GlobalSimList.Max(), LocalSimList.Max(), 2, 0.7, 0.3);
                    #endregion

                    sw.Write(FinalSim.ToString());
                    sw.Write("\r\n");
                }
            }

            sw.Close();
            fs.Close();
        }

        #region 第三稿转角函数计算
        private void button26_Click(object sender, EventArgs e)
        {
            DateTime dt1 = System.DateTime.Now;

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();//查询建筑物图层
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }

            List<IFeatureLayer> list2 = new List<IFeatureLayer>();
            if (this.comboBox4.Text != null)//模板建筑物图层
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list2.Add(BuildingLayer);
            }

            SMap map = new SMap(list);//原始建筑物图层
            map.ReadDateFrmEsriLyrsForEnrichNetWork();

            SMap map2 = new SMap(list2);//模板建筑物图层
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region 相似度计算
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                List<Double> SimList = new List<double>();
                for (int j = 0; j < map2.PolygonList.Count; j++)
                {
                    double AngleBasedSim = this.GetPolygonSimBasedTuringAngle(map.PolygonList[i], map2.PolygonList[j]);
                    SimList.Add(AngleBasedSim);
                }

                double TargetSim = SimList.Min();
                map.PolygonList[i].TargetTemp = SimList.IndexOf(TargetSim) + 1;
            }
            #endregion

            #region 准确率计算
            int RightCount = 0;
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                if (map.PolygonList[i].SourceTemp == map.PolygonList[i].TargetTemp)
                {
                    RightCount++;
                }
            }

            double SumCount = map.PolygonList.Count;
            double Accur = RightCount / SumCount;           
            #endregion

            #region 输出时间
            DateTime dt2 = System.DateTime.Now;
            TimeSpan ts = dt2.Subtract(dt1);
            MessageBox.Show(ts.TotalMilliseconds.ToString());
            MessageBox.Show(Accur.ToString());
            #endregion

            #region 计算tp、fp、fn
            int tp1 = 0; int fp1 = 0; 
            int tp2 = 0; int fp2 = 0; 
            int tp3 = 0; int fp3 = 0; 
            int tp4 = 0; int fp4 = 0; 
            int tp5 = 0; int fp5 = 0; 
            int tp6 = 0; int fp6 = 0; 

            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                #region 计算tp
                if ((map.PolygonList[i].SourceTemp == map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp ==1))
                {
                    tp1++;
                }
                if ((map.PolygonList[i].SourceTemp == map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 2))
                {
                    tp2++;
                }
                if ((map.PolygonList[i].SourceTemp == map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 3))
                {
                    tp3++;
                }
                if ((map.PolygonList[i].SourceTemp == map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 4))
                {
                    tp4++;
                }
                if ((map.PolygonList[i].SourceTemp == map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 5))
                {
                    tp5++;
                }
                if ((map.PolygonList[i].SourceTemp == map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 6))
                {
                    tp6++;
                }
                #endregion

                #region 计算fp
                if ((map.PolygonList[i].SourceTemp != map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 1))
                {
                    fp1++;
                }
                if ((map.PolygonList[i].SourceTemp != map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 2))
                {
                    fp2++;
                }
                if ((map.PolygonList[i].SourceTemp != map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 3))
                {
                    fp3++;
                }
                if ((map.PolygonList[i].SourceTemp != map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 4))
                {
                    fp4++;
                }
                if ((map.PolygonList[i].SourceTemp != map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 5))
                {
                    fp5++;
                }
                if ((map.PolygonList[i].SourceTemp != map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 6))
                {
                    fp6++;
                }
                #endregion
            }
            #endregion

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
        }
        #endregion

        #region 第三稿论文函数计算
        private void button27_Click(object sender, EventArgs e)
        {
            DateTime dt1 = System.DateTime.Now;

            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();//查询建筑物图层
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }

            List<IFeatureLayer> list2 = new List<IFeatureLayer>();
            if (this.comboBox4.Text != null)//模板建筑物图层
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);
                list2.Add(BuildingLayer);
            }

            SMap map = new SMap(list);//原始建筑物图层
            map.ReadDateFrmEsriLyrsForEnrichNetWork();

            SMap map2 = new SMap(list2);//模板建筑物图层
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region 相似度计算
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                List<Double> SimList = new List<double>();
                for (int j = 0; j < map2.PolygonList.Count; j++)
                {
                    double OurBasedSim = this.GetPolygonSim(map.PolygonList[i], map2.PolygonList[j], 1, -0.8, -0.5, 1, 1.0, 0.0, 4, 2, 0.7, 0.3);
                    SimList.Add(OurBasedSim);
                }

                double TargetSim = SimList.Max();
                map.PolygonList[i].TargetTemp = SimList.IndexOf(TargetSim) + 1;
            }
            #endregion

            #region 准确率计算
            int RightCount = 0;
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                if (map.PolygonList[i].SourceTemp == map.PolygonList[i].TargetTemp)
                {
                    RightCount++;
                }
            }

            double SumCount = map.PolygonList.Count;
            double Accur = RightCount / SumCount;
            #endregion

            #region 准确率和时间输出
            DateTime dt2 = System.DateTime.Now;
            TimeSpan ts = dt2.Subtract(dt1);

            MessageBox.Show(ts.TotalMilliseconds.ToString());
            MessageBox.Show(Accur.ToString());
            #endregion

            #region 计算tp、fp、fn
            int tp1 = 0; int fp1 = 0; 
            int tp2 = 0; int fp2 = 0; 
            int tp3 = 0; int fp3 = 0; 
            int tp4 = 0; int fp4 = 0; 
            int tp5 = 0; int fp5 = 0; 
            int tp6 = 0; int fp6 = 0; 

            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                #region 计算tp
                if ((map.PolygonList[i].SourceTemp == map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 1))
                {
                    tp1++;
                }
                if ((map.PolygonList[i].SourceTemp == map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 2))
                {
                    tp2++;
                }
                if ((map.PolygonList[i].SourceTemp == map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 3))
                {
                    tp3++;
                }
                if ((map.PolygonList[i].SourceTemp == map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 4))
                {
                    tp4++;
                }
                if ((map.PolygonList[i].SourceTemp == map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 5))
                {
                    tp5++;
                }
                if ((map.PolygonList[i].SourceTemp == map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 6))
                {
                    tp6++;
                }
                #endregion

                #region 计算fp
                if ((map.PolygonList[i].SourceTemp != map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 1))
                {
                    fp1++;
                }
                if ((map.PolygonList[i].SourceTemp != map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 2))
                {
                    fp2++;
                }
                if ((map.PolygonList[i].SourceTemp != map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 3))
                {
                    fp3++;
                }
                if ((map.PolygonList[i].SourceTemp != map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 4))
                {
                    fp4++;
                }
                if ((map.PolygonList[i].SourceTemp != map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 5))
                {
                    fp5++;
                }
                if ((map.PolygonList[i].SourceTemp != map.PolygonList[i].TargetTemp) && (map.PolygonList[i].SourceTemp == 6))
                {
                    fp6++;
                }
                #endregion
            }
            #endregion

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
        }
        #endregion

        #region 顺序编码
        private void button28_Click(object sender, EventArgs e)
        {
            #region 获取图层
            IFeatureLayer sBuildingLayer = null;//获得基准图层
            if (this.comboBox1.Text != null)
            {
                sBuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
            }
            #endregion

            #region 获取对应Target建筑物（基准）
            IFeatureClass sFeatureClass = sBuildingLayer.FeatureClass;
            Dictionary<int, IPolygon> sDic = new Dictionary<int, IPolygon>();//基准

            IFeatureCursor sFeatureCursor = sFeatureClass.Update(null, false);
            IFeature sFeature = sFeatureCursor.NextFeature();
            while (sFeature != null)
            {
                int KID = Convert.ToInt32(sFeature.get_Value(4));
                IPolygon pPolygon = sFeature.Shape as IPolygon;
                sDic.Add(KID, pPolygon);

                sFeature = sFeatureCursor.NextFeature();
            }
            #endregion

            #region 顺序编码
            SMap map = new SMap();
            List<int> sKeys = sDic.Keys.ToList();
            for (int i = 0; i < sKeys.Count; i++)
            {
                IPolygon CachePolygon1 = sDic[i];
                PolygonObject pPolygonObject = PolygonConvert(CachePolygon1);
                map.PolygonList.Add(pPolygonObject);
            }
            #endregion

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
        }
        #endregion
    }
}