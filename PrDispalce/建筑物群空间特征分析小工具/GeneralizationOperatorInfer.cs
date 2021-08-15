using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Display;

namespace PrDispalce.建筑物群空间特征分析小工具
{
    public partial class GeneralizationOperatorInfer : Form
    {
        public GeneralizationOperatorInfer(IMap cMap)
        {
            InitializeComponent();
            this.pMap = cMap;
        }

        #region 参数
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        //string OutPath;
        #endregion

        #region 初始化
        private void GeneralizationOperatorInfer_Load(object sender, EventArgs e)
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
                        this.comboBox3.Items.Add(strLayerName);
                        this.comboBox4.Items.Add(strLayerName);
                        this.comboBox5.Items.Add(strLayerName);
                        this.comboBox6.Items.Add(strLayerName);
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

            if (this.comboBox4.Items.Count > 0)
            {
                this.comboBox4.SelectedIndex = 0;
            }

            if (this.comboBox5.Items.Count > 0)
            {
                this.comboBox5.SelectedIndex = 0;
            }

            if (this.comboBox6.Items.Count > 0)
            {
                this.comboBox6.SelectedIndex = 0;
            }
            #endregion
        }
        #endregion

        #region 提取单个建筑物综合算子
        private void button1_Click(object sender, EventArgs e)
        {
            IFeatureLayer LocalFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);//获取LocalGrid图层
            IFeatureLayer DistrictFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);//获取DistrictGrid图层
            IFeatureLayer bFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox3.Text);//获取建筑物图层

            IFeatureClass DistrictFeatureClass=DistrictFeatureLayer.FeatureClass;
            IFeatureClass LocalFeatureClass = LocalFeatureLayer.FeatureClass;
            IFeatureClass bFeatureClass = bFeatureLayer.FeatureClass;

            #region 添加一个字段
            if (bFeatureClass.Fields.FindField("GoP") < 0)
            {
                IClass bClass = bFeatureClass as IClass;

                IFieldsEdit fldsE = bFeatureClass.Fields as IFieldsEdit;
                IField fld = new FieldClass();
                IFieldEdit2 fldE = fld as IFieldEdit2;
                fldE.Type_2 = esriFieldType.esriFieldTypeInteger;
                fldE.Name_2 = "GoP";
                bClass.AddField(fld);
            }
            #endregion

            #region infer generalization operator
            for (int i=0;i<DistrictFeatureClass.FeatureCount(null);i++)
            {
                IFeature DFeature=DistrictFeatureClass.GetFeature(i);
                int IDValue=(int)DFeature.get_Value(7);

                #region 如何对应的Grid无建筑物
                if (IDValue == 0)
                {
                    IQueryFilter bQueryFilter = new QueryFilterClass();
                    IFeatureCursor bFeatureCursor = bFeatureClass.Update(bQueryFilter, false);

                    IFeature bFeature = bFeatureCursor.NextFeature();
                    while (bFeature != null)
                    {
                        if (bFeature.get_Value(2) == LocalFeatureClass.GetFeature(i).get_Value(7))
                        {
                            bFeature.set_Value(3, 0);
                        }

                        bFeature = bFeatureCursor.NextFeature();
                    }
                }
                #endregion

                #region 对应的Grid有建筑物
                else
                {
                    IQueryFilter bQueryFilter = new QueryFilterClass();
                    IFeatureCursor bFeatureCursor = bFeatureClass.Update(bQueryFilter, false);

                    IFeature bFeature = bFeatureCursor.NextFeature();
                    while (bFeature != null)
                    {
                        if (bFeature.get_Value(2) == LocalFeatureClass.GetFeature(i).get_Value(7))
                        {
                            bFeature.set_Value(3, 1);
                        }

                        bFeature = bFeatureCursor.NextFeature();
                    }
                }
                #endregion
            }
            #endregion

        }
        #endregion

        #region 删除不满足约束条件的2.5万建筑物
        private void button2_Click(object sender, EventArgs e)
        {
            #region 获取图层
            IFeatureLayer LocalFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);//获取LocalGrid图层
            IFeatureLayer DistrictFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox5.Text);//获取DistrictGrid图层
            #endregion

            #region 建立索引
            BuildingRasterIndex BRI = new BuildingRasterIndex();
            BRI.GetGrid(LocalFeatureLayer, 10000);
            BRI.GetGridBuilding(LocalFeatureLayer);
            BRI.GetBuildingGrid();

            BuildingRasterIndex BRI2 = new BuildingRasterIndex();
            BRI2.GridInfor = BRI.GridInfor;
            BRI2.GetGridBuilding(DistrictFeatureLayer);
            BRI2.GetBuildingGrid();
            #endregion

            #region 删除建筑物
            IWorkspaceEdit ipWksEdt = (DistrictFeatureLayer as IDataset).Workspace as IWorkspaceEdit;
            if (ipWksEdt.IsBeingEdited())
            {

                ipWksEdt.StopEditOperation();
                ipWksEdt.StopEditing(true);
            }

            ipWksEdt.StartEditing(false);
            ipWksEdt.StartEditOperation();

            IQueryFilter pQueryFilter = new QueryFilterClass();
            IFeatureCursor pFeatureCursor = DistrictFeatureLayer.FeatureClass.Update(pQueryFilter, false);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                double SumArea = 0; bool boolLabel = false;

                if (BRI2.BuildingGrid.ContainsKey((int)pFeature.get_Value(0)))
                {
                    List<int> GridID = BRI2.BuildingGrid[(int)pFeature.get_Value(0)];
                    List<int> GridBuilding = BRI.GridBuilding[GridID];

                    for (int i = 0; i < GridBuilding.Count; i++)
                    {
                        ITopologicalOperator Ito = pFeature.Shape as ITopologicalOperator;
                        IGeometry pGeometry = Ito.Intersect(LocalFeatureLayer.FeatureClass.GetFeature(GridBuilding[i]).Shape, esriGeometryDimension.esriGeometry2Dimension);                      

                        if (pGeometry != null)
                        {
                            IArea fArea = LocalFeatureLayer.FeatureClass.GetFeature(GridBuilding[i]).Shape as IArea;
                            IArea pArea = pGeometry as IArea;

                            if (pArea.Area > 0)
                            {
                                SumArea = SumArea + fArea.Area;
                            }

                            if (pArea.Area < fArea.Area * 0.8 && pArea.Area>0)
                            {
                                pFeatureCursor.DeleteFeature();
                                boolLabel = true;
                                break;
                            }
                        }
                    }
                }

                IArea kArea = pFeature.Shape as IArea;
                if (!boolLabel)
                {
                    if (kArea.Area > SumArea * 2)
                    {
                        pFeatureCursor.DeleteFeature();
                    }
                }

                pFeature = pFeatureCursor.NextFeature();
            }

            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatureCursor);
            ipWksEdt.StopEditOperation();
            ipWksEdt.StopEditing(true);
            #endregion
        }
        #endregion

        #region 典型化建筑物提取
        private void button3_Click(object sender, EventArgs e)
        {
            #region 获取图层
            IFeatureLayer LocalFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);//获取LocalGrid图层
            IFeatureLayer DistrictFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox5.Text);//获取DistrictGrid图层
            #endregion

            #region 建立索引
            BuildingRasterIndex BRI = new BuildingRasterIndex();
            BRI.GetGrid(LocalFeatureLayer, 10000);
            BRI.GetGridBuilding(LocalFeatureLayer);
            BRI.GetBuildingGrid();

            BuildingRasterIndex BRI2 = new BuildingRasterIndex();
            BRI2.GridInfor = BRI.GridInfor;
            BRI2.GetGridBuilding(DistrictFeatureLayer);
            BRI2.GetBuildingGrid();
            #endregion

            #region 删除建筑物
            IWorkspaceEdit ipWksEdt = (DistrictFeatureLayer as IDataset).Workspace as IWorkspaceEdit;
            if (ipWksEdt.IsBeingEdited())
            {

                ipWksEdt.StopEditOperation();
                ipWksEdt.StopEditing(true);
            }

            ipWksEdt.StartEditing(false);
            ipWksEdt.StartEditOperation();

            IQueryFilter pQueryFilter = new QueryFilterClass();
            IFeatureCursor pFeatureCursor = DistrictFeatureLayer.FeatureClass.Update(pQueryFilter, false);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                double SumArea = 0; bool boolLabel = false;

                if (BRI2.BuildingGrid.ContainsKey((int)pFeature.get_Value(0)))
                {
                    List<int> GridID = BRI2.BuildingGrid[(int)pFeature.get_Value(0)];
                    List<int> GridBuilding = BRI.GridBuilding[GridID];

                    for (int i = 0; i < GridBuilding.Count; i++)
                    {
                        ITopologicalOperator Ito = pFeature.Shape as ITopologicalOperator;
                        IGeometry pGeometry = Ito.Intersect(LocalFeatureLayer.FeatureClass.GetFeature(GridBuilding[i]).Shape, esriGeometryDimension.esriGeometry2Dimension);

                        if (pGeometry != null)
                        {
                            IArea fArea = LocalFeatureLayer.FeatureClass.GetFeature(GridBuilding[i]).Shape as IArea;
                            IArea pArea = pGeometry as IArea;

                            if (pArea.Area > 0)
                            {
                                SumArea = SumArea + fArea.Area;

                                if (pArea.Area < fArea.Area * 0.2 || pArea.Area > fArea.Area * 0.6)
                                {
                                    pFeatureCursor.DeleteFeature();
                                    boolLabel = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                IArea kArea = pFeature.Shape as IArea;
                if (!boolLabel)
                {
                    if (kArea.Area > SumArea * 2)
                    {
                        pFeatureCursor.DeleteFeature();
                    }
                }

                pFeature = pFeatureCursor.NextFeature();
            }

            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatureCursor);
            ipWksEdt.StopEditOperation();
            ipWksEdt.StopEditing(true);
            #endregion
        }
        #endregion

        #region 判断选取
        private void button4_Click(object sender, EventArgs e)
        {
            #region 获取图层
            IFeatureLayer LocalFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox4.Text);//获取LocalGrid图层
            IFeatureLayer DistrictFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox5.Text);//获取DistrictGrid图层
            #endregion

            #region 建立索引
            BuildingRasterIndex BRI = new BuildingRasterIndex();
            BRI.GetGrid(LocalFeatureLayer, 10000);
            BRI.GetGridBuilding(LocalFeatureLayer);
            BRI.GetBuildingGrid();

            BuildingRasterIndex BRI2 = new BuildingRasterIndex();
            BRI2.GridInfor = BRI.GridInfor;
            BRI2.GetGridBuilding(DistrictFeatureLayer);
            BRI2.GetBuildingGrid();
            #endregion

            #region 删除建筑物
            IWorkspaceEdit ipWksEdt = (DistrictFeatureLayer as IDataset).Workspace as IWorkspaceEdit;
            if (ipWksEdt.IsBeingEdited())
            {

                ipWksEdt.StopEditOperation();
                ipWksEdt.StopEditing(true);
            }

            ipWksEdt.StartEditing(false);
            ipWksEdt.StartEditOperation();

            IQueryFilter pQueryFilter = new QueryFilterClass();
            IFeatureCursor pFeatureCursor = DistrictFeatureLayer.FeatureClass.Update(pQueryFilter, false);
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                if (BRI2.BuildingGrid.ContainsKey((int)pFeature.get_Value(0)))
                {
                    List<int> GridID = BRI2.BuildingGrid[(int)pFeature.get_Value(0)];
                    List<int> GridBuilding = BRI.GridBuilding[GridID];

                    for (int i = 0; i < GridBuilding.Count; i++)
                    {
                        ITopologicalOperator Ito = pFeature.Shape as ITopologicalOperator;
                        IGeometry pGeometry = Ito.Intersect(LocalFeatureLayer.FeatureClass.GetFeature(GridBuilding[i]).Shape, esriGeometryDimension.esriGeometry2Dimension);

                        if (pGeometry != null)
                        {
                            IArea fArea = LocalFeatureLayer.FeatureClass.GetFeature(GridBuilding[i]).Shape as IArea;
                            IArea pArea = pGeometry as IArea;
                            IArea cArea=pFeature.Shape as IArea;

                            if (pArea.Area > 0)
                            {
                                if (pArea.Area<fArea.Area*0.8||cArea.Area>fArea.Area*1.5)
                                {
                                    pFeatureCursor.DeleteFeature();
                                    break;
                                }
                            }
                        }
                    }
                }

                pFeature = pFeatureCursor.NextFeature();
            }

            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatureCursor);
            ipWksEdt.StopEditOperation();
            ipWksEdt.StopEditing(true);
            #endregion
        }
        #endregion

        #region 计算差异
        private void button5_Click(object sender, EventArgs e)
        {
            #region 获取图层
            IFeatureLayer LocalFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox6.Text);//获取LocalGrid图层
            #endregion

            #region 添加字段
            CreateFiled(LocalFeatureLayer.FeatureClass, esriFieldType.esriFieldTypeDouble, "AreaDiff");
            CreateFiled(LocalFeatureLayer.FeatureClass, esriFieldType.esriFieldTypeDouble, "MComDiff");
            CreateFiled(LocalFeatureLayer.FeatureClass, esriFieldType.esriFieldTypeDouble, "SIDiff");
            CreateFiled(LocalFeatureLayer.FeatureClass, esriFieldType.esriFieldTypeDouble, "FdDiff");
            CreateFiled(LocalFeatureLayer.FeatureClass, esriFieldType.esriFieldTypeDouble, "EdDiff");
            CreateFiled(LocalFeatureLayer.FeatureClass, esriFieldType.esriFieldTypeDouble, "SMBRODiff1");
            CreateFiled(LocalFeatureLayer.FeatureClass, esriFieldType.esriFieldTypeDouble, "SMBRODiff2");
            CreateFiled(LocalFeatureLayer.FeatureClass, esriFieldType.esriFieldTypeDouble, "SDis");
            #endregion

            #region 遍历计算
            List<double> AreaDiffList = new List<double>();
            List<double> MComDiffList = new List<double>();
            List<double> SIDiffList = new List<double>();
            List<double> FdDiffList = new List<double>();
            List<double> EdDiffList = new List<double>();
            List<double> SMBRO1DiffList = new List<double>();
            List<double> SMBRO2DiffList = new List<double>();
            List<double> DisDiffList = new List<double>();

            for (int i = 0; i < LocalFeatureLayer.FeatureClass.FeatureCount(null) / 2; i++)
            {
                List<int> FeatureIdList = new List<int>();

                IQueryFilter pQueryFilter = new QueryFilterClass();
                string Clause = "BID_1 =" +i.ToString();
                pQueryFilter.WhereClause = Clause;
                IFeatureCursor pFeatureCursor = LocalFeatureLayer.FeatureClass.Update(pQueryFilter, false);
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null)
                {
                    FeatureIdList.Add((int)pFeature.get_Value(0));
                    pFeature = pFeatureCursor.NextFeature();
                }

                IFeature Feature1 = LocalFeatureLayer.FeatureClass.GetFeature(FeatureIdList[0]);
                IFeature Feature2 = LocalFeatureLayer.FeatureClass.GetFeature(FeatureIdList[1]);

                #region 计算面积差异               
                double Area1 = (double)GetValue(Feature1, "Convexhull");
                double Area2 = (double)GetValue(Feature2, "Convexhull");

                double AreaDiff = -1;
                if (Area1 > Area2)
                {
                    AreaDiff = Area2 / Area1;
                }

                else
                {
                    AreaDiff = Area1 / Area2;
                }
                AreaDiffList.Add(AreaDiff);
                #endregion

                #region 计算MCom差异                
                double MCom1 = (double)GetValue(Feature1, "MCom");
                double MCom2 = (double)GetValue(Feature2, "MCom");

                double MComDiff = -1;
                if (MCom1 > MCom2)
                {
                    MComDiff = MCom2 / MCom1;
                }

                else
                {
                    MComDiff = MCom1 / MCom2;
                }
                MComDiffList.Add(MComDiff);
                #endregion

                #region 计算ShapeIndex差异             
                double ShapeIndex1 = (double)GetValue(Feature1, "SIndex");
                double ShapeIndex2 = (double)GetValue(Feature2, "SIndex");

                double SIDiff = -1;
                if (ShapeIndex1 > ShapeIndex2)
                {
                    SIDiff = ShapeIndex2 / ShapeIndex1;
                }

                else
                {
                    SIDiff = ShapeIndex1 / ShapeIndex2;
                }
                SIDiffList.Add(SIDiff);
                #endregion

                #region 计算Fd差异              
                double Fd1 = (double)GetValue(Feature1, "Fd");
                double Fd2 = (double)GetValue(Feature2, "Fd");

                double FDDiff = -1;
                if (Fd1 > Fd2)
                {
                    FDDiff = Fd2 / Fd1;
                }

                else
                {
                    FDDiff = Fd1 / Fd2;
                }

                FdDiffList.Add(FDDiff);
                #endregion

                #region 计算EdgeCount差异              
                IFields pFields = Feature1.Fields;

                int fnum; int field1 = 0;
                fnum = pFields.FieldCount;

                for (int m = 0; m < fnum; m++)
                {
                    if (pFields.get_Field(m).Name == "EdgeCount")
                    {
                        field1 = pFields.FindField("EdgeCount");
                    }
                }

                double EdgeCount1 = (int)Feature1.get_Value(field1);
                double EdgeCount2 = (int)Feature2.get_Value(field1);

                double EdDiff = -1;
                if (EdgeCount1 > EdgeCount2)
                {
                    EdDiff = EdgeCount2 / EdgeCount1;
                }

                else
                {
                    EdDiff = EdgeCount1 / EdgeCount2;
                }
                EdDiffList.Add(EdDiff);
                #endregion

                #region 计算方向差异
                double smbro1 = (double)GetValue(Feature1, "SMBRO");
                double smbro2 = (double)GetValue(Feature2, "SMBRO");

                double SMBRODiff1 = -1; double SMBRODiff2 = -1;
                SMBRODiff1 = Math.Abs(smbro1 - smbro2);

                if (Math.Abs(smbro1 - smbro2) > 90)
                {
                    SMBRODiff2 = 180 - Math.Abs(smbro1 - smbro2);
                }

                else
                {
                    SMBRODiff2 = Math.Abs(smbro1 - smbro2);
                }

                SMBRO1DiffList.Add(SMBRODiff1);
                SMBRO2DiffList.Add(SMBRODiff2);
                #endregion

                #region 计算两个要素间的距离
                IProximityOperator IPO = Feature1.Shape as IProximityOperator;
                double ShortDistance = IPO.ReturnDistance(Feature2.Shape);
                DisDiffList.Add(ShortDistance);              
                #endregion

                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatureCursor);
            }
            #endregion

            #region 计算差异
            for (int i = 0; i < LocalFeatureLayer.FeatureClass.FeatureCount(null) / 2; i++)
            {
                IQueryFilter pQueryFilter = new QueryFilterClass();
                string Clause = "BID_1 =" + i.ToString();
                pQueryFilter.WhereClause = Clause;
                IFeatureCursor pFeatureCursor = LocalFeatureLayer.FeatureClass.Update(pQueryFilter, false);
                IFeature pFeature = pFeatureCursor.NextFeature();
                while (pFeature != null)
                {
                    DataStore(pFeature, "AreaDiff", AreaDiffList[i]);
                    DataStore(pFeature, "MComDiff", MComDiffList[i]);
                    DataStore(pFeature, "SIDiff", SIDiffList[i]);
                    DataStore(pFeature, "EdDiff", EdDiffList[i]);
                    DataStore(pFeature, "FdDiff", FdDiffList[i]);
                    DataStore(pFeature, "SMBRODiff1", SMBRO1DiffList[i]);
                    DataStore(pFeature, "SMBRODiff2", SMBRO2DiffList[i]);
                    DataStore(pFeature, "SDis", DisDiffList[i]);

                    pFeatureCursor.UpdateFeature(pFeature);
                    pFeature = pFeatureCursor.NextFeature();
                }

                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatureCursor);
            }
            #endregion
        }
        #endregion

        #region 添加字段
        public void CreateFiled(IFeatureClass pFeatureClass, esriFieldType Type, string s)
        {
            if (pFeatureClass.Fields.FindField(s) < 0)
            {
                IClass pClass = pFeatureClass as IClass;
                IFieldsEdit fldsE = pFeatureClass.Fields as IFieldsEdit;
                IField fld = new FieldClass();
                IFieldEdit2 fldE = fld as IFieldEdit2;
                fldE.Type_2 = Type;
                fldE.Name_2 = s;
                pClass.AddField(fld);
            }
        }
        #endregion

        #region 存储数据
        public void DataStore(IFeature pFeature, string s, double t)
        {
            IFields pFields = pFeature.Fields;

            int fnum;
            fnum = pFields.FieldCount;

            for (int m = 0; m < fnum; m++)
            {
                if (pFields.get_Field(m).Name == s)
                {
                    int field1 = pFields.FindField(s);
                    pFeature.set_Value(field1, t);
                }
            }
        }
        #endregion

        #region 获得对应字段下的Value
        double GetValue(IFeature pFeature,string s)
        {
            IFields pFields = pFeature.Fields;

            int fnum; int field1 = 0;
            fnum = pFields.FieldCount;

            for (int m = 0; m < fnum; m++)
            {
                if (pFields.get_Field(m).Name == s)
                {
                    field1 = pFields.FindField(s);
                }
            }

            return (double)pFeature.get_Value(field1);
        }
        #endregion
    }
}
