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

//1、不需要识别pattern，给定的pattern都是提前识别的，并给出了pattern的顺序，直接读取即可
//2、计算pattern中建筑物的相似性
//3、渐进式的确定需要合并的建筑物，并表达该建筑物
//4、确定pattern中建筑物的位置
namespace PrDispalce.典型化
{
    public partial class ArbitrarilyMesh : Form
    {
        public ArbitrarilyMesh(IMap cMap, AxMapControl pMapControl)
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
        /// 顾及面积差异的典型化（representation就是选择面积大的）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            LocAndRepNewbuilding LR=new LocAndRepNewbuilding();

            #region 获取图层，并读取数据
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }

            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            #region 确定图层中的每个pattern
            List<Pattern> PatternList = new List<Pattern>();
            for (int i = 0; i < int.Parse(this.textBox1.Text); i++)
            {              
                Dictionary<int, PolygonObject> PatternDic = new Dictionary<int, PolygonObject>();

                for (int j = 0; j < map.PolygonList.Count; j++)
                {
                    for (int m = 0; m < map.PolygonList[j].PatternIDList.Count; m++)
                    {
                        if (i == map.PolygonList[j].PatternIDList[m])
                        {
                            for (int n = 0; n < map.PolygonList[j].OrderIDList.Count; n++)
                            {
                                if (map.PolygonList[j].OrderIDList[n][0] == i)
                                {
                                    PatternDic.Add(map.PolygonList[j].OrderIDList[n][1],map.PolygonList[j] );
                                }
                            }
                        }
                    }
                }

                #region 对pattern中的建筑物进行排序
                Dictionary<int, PolygonObject> SortIndexList = PatternDic.OrderBy(o => o.Key).ToDictionary(p => p.Key, o => o.Value);
                List<PolygonObject> PatternObjects = new List<PolygonObject>(SortIndexList.Values);
                PatternObjects[0].BoundaryBuilding = true; PatternObjects[PatternObjects.Count - 1].BoundaryBuilding = true;
                #endregion

                Pattern pPattern = new Pattern(i, PatternObjects, PatternObjects[0].SiSim, PatternObjects[0].OriSim);
                PatternList.Add(pPattern);
            }
            #endregion

            #region 符号化pattern中的建筑物
            for (int i = 0; i < PatternList.Count; i++)
            {
                for (int j = 0; j < PatternList[i].PatternObjects.Count; j++)
                {
                    PatternList[i].PatternObjects[j] = SymbolizedPolygon(PatternList[i].PatternObjects[j], 25000);
                }
            }
            #endregion

            #region 依次典型化每一个Pattern
            for (int i = 0; i < PatternList.Count; i++)
            {
                while (PatternList[i].NoConflict(5))
                {
                    int BuildingIndex = PatternList[i].GetTwoObjectIndex(PatternList[i].bSiSim, 1);//获得需要操作的两个建筑物的Index
                    if (BuildingIndex != -1)
                    {
                        LR.NewPattern(PatternList[i], BuildingIndex);//更新建筑物的位置
                    }

                    else
                    {
                        break;
                    }
                }
            }
            #endregion

            map.PolygonList.Clear();
            for (int i = 0; i < PatternList.Count; i++)
            {
                for(int j=0;j<PatternList[i].PatternObjects.Count;j++)
                {
                    map.PolygonList.Add(PatternList[i].PatternObjects[j]);
                }
            }

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
        }

        /// <summary>
        /// 返回符号化的建筑物
        /// </summary>
        /// <param name="Polygon"></param>
        /// <param name="Scale"></param>
        /// <returns></returns>
        public PolygonObject SymbolizedPolygon(PolygonObject Polygon, double Scale)
        {
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
            if (LLength < 0.7 * Scale / 1000 || SLength < 0.5 * Scale / 1000)
            {
                if (LLength < 0.7 * Scale / 1000)
                {
                    LLength = 0.7 * Scale / 1000;
                }

                if (SLength < 0.5 * Scale / 1000)
                {
                    SLength = 0.5 * Scale / 1000;
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
            this.comboBox2.Text = OutPath;
        }

        #region 初始化
        private void ArbitrarilyMesh_Load(object sender, EventArgs e)
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
        #endregion

    }
}
