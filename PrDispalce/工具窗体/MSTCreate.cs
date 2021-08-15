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


namespace PrDispalce.工具窗体
{
    public partial class MSTCreate : Form
    {
        public MSTCreate(IMap cMap, AxMapControl aMap)
        {
            InitializeComponent();
            this.pMap = cMap;
            this.mMapControl = aMap;
        }

        #region 参数
        IMap pMap;
        string localFilePath, fileNameExt, FilePath;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        AxMapControl mMapControl;
        #endregion

        #region 输出路径
        private void button3_Click(object sender, EventArgs e)
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

        #region 初始化
        private void MSTCreate_Load(object sender, EventArgs e)
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
                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                    {
                        this.comboBox1.Items.Add(strLayerName);
                    }

                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                    {
                        this.comboBox2.Items.Add(strLayerName);
                    }
                }
            }

            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }

            if (this.comboBox2.Items.Count > 0)
            {
                this.comboBox2.SelectedIndex = 0;
            }
        }
        #endregion

        #region MST生成
        private void button1_Click(object sender, EventArgs e)
        {
            string s1 = comboBox2.Text;
            string s2 = comboBox1.Text;

            IFeatureClass PointsFeatureClass = pFeatureHandle.GetFeatureClass(pMap, s1);
            IFeatureClass LineFeatureClass = pFeatureHandle.GetFeatureClass(pMap, s2);

            #region 获取节点图层中的点集
            IPointArray Points = new PointArrayClass();
            for (int i = 0; i < PointsFeatureClass.FeatureCount(null); i++)
            {
                IFeature pFeature1;
                pFeature1 = PointsFeatureClass.GetFeature(i);
                IPoint pPoint = (IPoint)pFeature1.Shape;

                Points.Add(pPoint);
            }
            #endregion

            int PointNum = Points.Count;
            int LineNum = LineFeatureClass.FeatureCount(null);

            #region 创建MSTshapefile并添加字段
            IFeatureClass MSTFeatureClass = pFeatureHandle.createLineshapefile(pMap.SpatialReference, FilePath, fileNameExt);
            pFeatureHandle.AddField(MSTFeatureClass, "ShoDis", esriFieldType.esriFieldTypeDouble);
            pFeatureHandle.AddField(MSTFeatureClass, "SPoint", esriFieldType.esriFieldTypeInteger);
            pFeatureHandle.AddField(MSTFeatureClass, "EPoint", esriFieldType.esriFieldTypeInteger);
            #endregion

            #region 矩阵初始化
            float[,] matrixGraph = new float[PointNum, PointNum];

            for (int i = 0; i < PointNum; i++)
            {
                for (int j = 0; j < PointNum; j++)
                {
                    matrixGraph[i, j] = -1;
                }
            }
            #endregion

            #region 矩阵赋值
            for (int i = 0; i < LineNum; i++)
            {
                IFeature pFeature = LineFeatureClass.GetFeature(i);
                IFields pFields = pFeature.Fields;

                int fnum;
                int id1 = -1;
                int id2 = -1;
                float Distance = 0;
                fnum = pFields.FieldCount;

                for (int j = 0; j < fnum; j++)
                {
                    if (pFields.get_Field(j).Name == "NODE1")
                    {
                        int field1 = pFields.FindField("NODE1");
                        id1 = (int)pFeature.get_Value(field1);
                    }

                    if (pFields.get_Field(j).Name == "NODE2")
                    {
                        int field1 = pFields.FindField("NODE2");
                        id2 = (int)pFeature.get_Value(field1);
                    }

                    if (pFields.get_Field(j).Name == "MinDis")
                    {
                        int field1 = pFields.FindField("MinDis");
                        Distance = (float)pFeature.get_Value(field1);
                    }
                }

                matrixGraph[id1, id2] = matrixGraph[id2, id1] = Distance;
            }

            for (int i = 0; i < PointNum; i++)
            {
                for (int j = 0; j < PointNum; j++)
                {
                    if (matrixGraph[i, j] == -1)
                    {
                        matrixGraph[i, j] = matrixGraph[j, i] = 10000;
                    }
                }
            }
            #endregion

            #region MST计算
            List<List<int>> EdgesGroup = new List<List<int>>();
            IArray LabelArray = new ArrayClass();
            IArray fLabelArray = new ArrayClass();

            for (int F = 0; F < PointNum; F++)
            {
                fLabelArray.Add(F);
            }

            LabelArray.Add(0);
            //int x = 0;
            int LabelArrayNum;
            do
            {
                LabelArrayNum = LabelArray.Count;
                int fLabelArrayNum = fLabelArray.Count;
                double MinDist = 10001;
                List<int> Edge = new List<int>();

                int EdgeLabel2 = -1;
                int EdgeLabel1 = -1;
                int Label = -1;

                for (int i = 0; i < LabelArrayNum; i++)
                {
                    int p1 = (int)LabelArray.get_Element(i);

                    for (int j = 0; j < fLabelArrayNum; j++)
                    {
                        int p2 = (int)fLabelArray.get_Element(j);

                        if (matrixGraph[p1, p2] < MinDist)
                        {
                            MinDist = matrixGraph[p1, p2];
                            EdgeLabel2 = p2;
                            EdgeLabel1 = p1;
                            Label = j;
                        }
                    }
                }

                //x++;
                Edge.Add(EdgeLabel1);
                Edge.Add(EdgeLabel2);
                EdgesGroup.Add(Edge);

                fLabelArray.Remove(Label);
                LabelArray.Add(EdgeLabel2); ;

            } while (LabelArrayNum < PointNum);
            #endregion

            #region MST绘制与存储
            int EdgesGroupNum = EdgesGroup.Count;

            for (int i = 0; i < EdgesGroupNum; i++)
            {
                int m, n;
                m = EdgesGroup[i][0];
                n = EdgesGroup[i][1];
                IPoint EdgePoint1, EdgePoint2;

                EdgePoint1 = Points.get_Element(m);
                EdgePoint2 = Points.get_Element(n);

                IPolyline pLine = new PolylineClass();

                pLine.FromPoint = EdgePoint1;
                pLine.ToPoint = EdgePoint2;

                IFeature feature = MSTFeatureClass.CreateFeature();
                feature.Shape = pLine as IGeometry;

                IFields sFields = feature.Fields;
                int sfnum = sFields.FieldCount;
                for (int j = 0; j < sfnum; j++)
                {
                    if (sFields.get_Field(j).Name == "ShoDis")
                    {
                        int field1 = sFields.FindField("ShoDis");
                        feature.set_Value(field1, matrixGraph[m, n]);
                        feature.Store();
                    }

                    if (sFields.get_Field(j).Name == "SPoint")
                    {
                        int field1 = sFields.FindField("SPoint");
                        feature.set_Value(field1, m);
                        feature.Store();
                    }

                    if (sFields.get_Field(j).Name == "EPoint")
                    {
                        int field1 = sFields.FindField("EPoint");
                        feature.set_Value(field1, n);
                        feature.Store();
                    }
                }


                feature.Store();

                ESRI.ArcGIS.Display.ISimpleLineSymbol simpleLineSymbol = new ESRI.ArcGIS.Display.SimpleLineSymbolClass();
                simpleLineSymbol.Width = 5;

                IRgbColor rgbColor1 = new RgbColorClass();
                rgbColor1.Red = 255;
                rgbColor1.Green = 215;
                rgbColor1.Blue = 0;
                simpleLineSymbol.Color = rgbColor1;

                simpleLineSymbol.Style = 0;

                object SimpleLillSymbol = simpleLineSymbol;

                mMapControl.DrawShape(pLine, ref SimpleLillSymbol);
            #endregion
            }
        }
        #endregion
    }
}
