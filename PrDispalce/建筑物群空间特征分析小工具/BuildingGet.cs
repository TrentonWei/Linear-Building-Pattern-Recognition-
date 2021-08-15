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
    public partial class BuildingGet : Form
    {
        public BuildingGet(IMap cMap)
        {
            InitializeComponent();
            this.pMap = cMap;
        }

        #region 参数
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        string localFilePath, fileNameExt, FilePath;
        #endregion

        #region 初始化
        private void BuildingGet_Load(object sender, EventArgs e)
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
                        this.comboBox3.Items.Add(strLayerName);
                    }
                    #endregion
                }
            }

            #region 默认显示第一个
            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }

            if (this.comboBox3.Items.Count > 0)
            {
                this.comboBox3.SelectedIndex = 0;
            }
            #endregion
        }
        #endregion

        #region 输出路径
        private void button2_Click(object sender, EventArgs e)
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

            this.comboBox2.Text = localFilePath;

        }
        #endregion

        #region 获取Single建筑物Grid（规则=该Grid中只有一个建筑物，且其邻近Grid无建筑物）
        private void button1_Click(object sender, EventArgs e)
        {
            IFeatureLayer gFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);//获取Grid图层

            #region 遍历Grid图层，获取每一个Grid的属性
            Dictionary<int, int> FeatureCountDic = new Dictionary<int, int>();//存储每一个Grid的编号和对应的建筑物Count数
            IFeatureClass gFeatureClass = gFeatureLayer.FeatureClass;
            IQueryFilter pQueryFilter = new QueryFilterClass();
            IFeatureCursor pFeatureCursor = gFeatureClass.Update(pQueryFilter, false);
            
            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                int CountValue = (int)pFeature.get_Value(4);
                FeatureCountDic.Add((int)pFeature.get_Value(0), CountValue);//从0开始编号，并记录其Count
                pFeature = pFeatureCursor.NextFeature();
            }
            #endregion

            #region 选取Count=1的Grid，并判断Count=1的Grid是否是符合条件的Grid
            List<IFeature> TrueFeatureList = new List<IFeature>();

            IQueryFilter nQueryFilter = new QueryFilterClass();
            string Clause = "Count_" + "=1";
            nQueryFilter.WhereClause = Clause;
            IFeatureCursor nFeatureCursor = gFeatureClass.Update(nQueryFilter, false);
            IFeature nFeature = nFeatureCursor.NextFeature();
            while (nFeature != null)
            {
                int Fid = (int)nFeature.get_Value(0);
                int i_1 = (int)(Math.Floor(Fid / double.Parse(this.textBox1.Text)) + 1);
                int j_1 = (int)(Math.IEEERemainder(Fid, double.Parse(this.textBox1.Text)) + 1);

                int i_0 = i_1 - 1; int i_2 = i_1 + 1;
                int j_0 = j_1 - 1; int j_2 = j_1 + 1;

                int i_0_j_0 = (i_0 - 1) * int.Parse(this.textBox1.Text) + j_0 - 1;
                int i_0_j_1 = (i_0 - 1) * int.Parse(this.textBox1.Text) + j_1 - 1;
                int i_0_j_2 = (i_0 - 1) * int.Parse(this.textBox1.Text) + j_2 - 1;
                int i_1_j_0 = (i_1 - 1) * int.Parse(this.textBox1.Text) + j_0 - 1;
                int i_1_j_2 = (i_1 - 1) * int.Parse(this.textBox1.Text) + j_2 - 1;
                int i_2_j_0 = (i_2 - 1) * int.Parse(this.textBox1.Text) + j_0 - 1;
                int i_2_j_1 = (i_2 - 1) * int.Parse(this.textBox1.Text) + j_1 - 1;
                int i_2_j_2 = (i_2 - 1) * int.Parse(this.textBox1.Text) + j_2 - 1;

                bool trueLabel = true;
                if (FeatureCountDic.ContainsKey(i_0_j_0) && FeatureCountDic[i_0_j_0] != 0)
                {
                    trueLabel = false;
                }

                if (FeatureCountDic.ContainsKey(i_0_j_1) && FeatureCountDic[i_0_j_1] != 0)
                {
                    trueLabel = false;
                }

                if (FeatureCountDic.ContainsKey(i_0_j_2) && FeatureCountDic[i_0_j_2] != 0)
                {
                    trueLabel = false;
                }

                if (FeatureCountDic.ContainsKey(i_1_j_0) && FeatureCountDic[i_1_j_0] != 0)
                {
                    trueLabel = false;
                }

                if (FeatureCountDic.ContainsKey(i_1_j_2) && FeatureCountDic[i_1_j_2] != 0)
                {
                    trueLabel = false;
                }

                if (FeatureCountDic.ContainsKey(i_2_j_0) && FeatureCountDic[i_2_j_0] != 0)
                {
                    trueLabel = false;
                }

                if (FeatureCountDic.ContainsKey(i_2_j_1) && FeatureCountDic[i_2_j_1] != 0)
                {
                    trueLabel = false;
                }

                if (FeatureCountDic.ContainsKey(i_2_j_2) && FeatureCountDic[i_2_j_2] != 0)
                {
                    trueLabel = false;
                }

                if (trueLabel)
                {
                    TrueFeatureList.Add(nFeature);
                }

                nFeature = nFeatureCursor.NextFeature();
            }
            #endregion

            #region 将满足条件的Grid输出
            IFeatureClass OutFeatureClass = pFeatureHandle.createPolygonshapefile(pMap.SpatialReference, FilePath, fileNameExt);

            IDataset dataset = (IDataset)OutFeatureClass;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
            IFeatureClassWrite fr = (IFeatureClassWrite)OutFeatureClass;
            //注意：此时，所编辑数据不能被其他程序打开
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            for (int i = 0; i < TrueFeatureList.Count; i++)
            {
                IFeature feature = OutFeatureClass.CreateFeature();
                feature.Shape = TrueFeatureList[i].Shape;
                feature.Store();//保存IFeature对象  
                fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上 
            }

            //关闭编辑
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
            #endregion
        }
        #endregion

        #region 获取Pair建筑物Grid（规则=该Grid中只有两个建筑物，且其邻近Grid无建筑物）
        private void button3_Click(object sender, EventArgs e)
        {
            IFeatureLayer gFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);//获取Grid图层

            #region 遍历Grid图层，获取每一个Grid的属性
            Dictionary<int, int> FeatureCountDic = new Dictionary<int, int>();//存储每一个Grid的编号和对应的建筑物Count数
            IFeatureClass gFeatureClass = gFeatureLayer.FeatureClass;
            IQueryFilter pQueryFilter = new QueryFilterClass();
            IFeatureCursor pFeatureCursor = gFeatureClass.Update(pQueryFilter, false);

            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                int CountValue = (int)pFeature.get_Value(4);
                FeatureCountDic.Add((int)pFeature.get_Value(0), CountValue);
                pFeature = pFeatureCursor.NextFeature();
            }
            #endregion

            #region 选取Count=1的Grid，并判断Count=1的Grid是否是符合条件的Grid
            List<IFeature> TrueFeatureList = new List<IFeature>();

            IQueryFilter nQueryFilter = new QueryFilterClass();
            string Clause = "Count_" + "=2";
            nQueryFilter.WhereClause = Clause;
            IFeatureCursor nFeatureCursor = gFeatureClass.Update(nQueryFilter, false);
            IFeature nFeature = nFeatureCursor.NextFeature();
            while (nFeature != null)
            {
                int Fid = (int)nFeature.get_Value(0);
                int i_1 = (int)(Math.Floor(Fid / double.Parse(this.textBox1.Text)) + 1);
                int j_1 = (int)(Math.IEEERemainder(Fid, double.Parse(this.textBox1.Text)) + 1);

                int i_0 = i_1 - 1; int i_2 = i_1 + 1;
                int j_0 = j_1 - 1; int j_2 = j_1 + 1;

                int i_0_j_0 = (i_0 - 1) * int.Parse(this.textBox1.Text) + j_0 - 1;
                int i_0_j_1 = (i_0 - 1) * int.Parse(this.textBox1.Text) + j_1 - 1;
                int i_0_j_2 = (i_0 - 1) * int.Parse(this.textBox1.Text) + j_2 - 1;
                int i_1_j_0 = (i_1 - 1) * int.Parse(this.textBox1.Text) + j_0 - 1;
                int i_1_j_2 = (i_1 - 1) * int.Parse(this.textBox1.Text) + j_2 - 1;
                int i_2_j_0 = (i_2 - 1) * int.Parse(this.textBox1.Text) + j_0 - 1;
                int i_2_j_1 = (i_2 - 1) * int.Parse(this.textBox1.Text) + j_1 - 1;
                int i_2_j_2 = (i_2 - 1) * int.Parse(this.textBox1.Text) + j_2 - 1;

                bool trueLabel = true;
                if (FeatureCountDic.ContainsKey(i_0_j_0) && FeatureCountDic[i_0_j_0] != 0)
                {
                    trueLabel = false;
                }

                if (FeatureCountDic.ContainsKey(i_0_j_1) && FeatureCountDic[i_0_j_1] != 0)
                {
                    trueLabel = false;
                }

                if (FeatureCountDic.ContainsKey(i_0_j_2) && FeatureCountDic[i_0_j_2] != 0)
                {
                    trueLabel = false;
                }

                if (FeatureCountDic.ContainsKey(i_1_j_0) && FeatureCountDic[i_1_j_0] != 0)
                {
                    trueLabel = false;
                }

                if (FeatureCountDic.ContainsKey(i_1_j_2) && FeatureCountDic[i_1_j_2] != 0)
                {
                    trueLabel = false;
                }

                if (FeatureCountDic.ContainsKey(i_2_j_0) && FeatureCountDic[i_2_j_0] != 0)
                {
                    trueLabel = false;
                }

                if (FeatureCountDic.ContainsKey(i_2_j_1) && FeatureCountDic[i_2_j_1] != 0)
                {
                    trueLabel = false;
                }

                if (FeatureCountDic.ContainsKey(i_2_j_2) && FeatureCountDic[i_2_j_2] != 0)
                {
                    trueLabel = false;
                }

                if (trueLabel)
                {
                    TrueFeatureList.Add(nFeature);
                }

                nFeature = nFeatureCursor.NextFeature();
            }
            #endregion

            #region 将满足条件的Grid输出
            IFeatureClass OutFeatureClass = pFeatureHandle.createPolygonshapefile(pMap.SpatialReference, FilePath, fileNameExt);

            IDataset dataset = (IDataset)OutFeatureClass;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
            IFeatureClassWrite fr = (IFeatureClassWrite)OutFeatureClass;
            //注意：此时，所编辑数据不能被其他程序打开
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            for (int i = 0; i < TrueFeatureList.Count; i++)
            {
                IFeature feature = OutFeatureClass.CreateFeature();
                feature.Shape = TrueFeatureList[i].Shape;
                feature.Store();//保存IFeature对象  
                fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上 
            }

            //关闭编辑
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
            #endregion
        }
        #endregion

        #region 获取Single建筑物
        private void button4_Click(object sender, EventArgs e)
        {
            IFeatureLayer gFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);//获取建筑物图层
            IFeatureLayer bFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox3.Text);//获取Grid图层

            #region 遍历Grid，获取每一个被标记的建筑物的编号
            List<int> ContainBuilding = new List<int>();//存储每一个Grid的编号和对应的建筑物Count数
            IFeatureClass gFeatureClass = gFeatureLayer.FeatureClass;
            IQueryFilter pQueryFilter = new QueryFilterClass();
            IFeatureCursor pFeatureCursor = gFeatureClass.Update(pQueryFilter, false);

            IFeature pFeature = pFeatureCursor.NextFeature();
            while (pFeature != null)
            {
                int IDValue = (int)pFeature.get_Value(7);
                if (IDValue != 0)
                {
                    ContainBuilding.Add(IDValue);
                }
                pFeature = pFeatureCursor.NextFeature();
            }
            #endregion

            #region 遍历Grid，获取其对应的建筑物
            List<IFeature> SingleBuildingList = new List<IFeature>();
            IFeatureClass bFeatureClass = bFeatureLayer.FeatureClass;
            IQueryFilter bQueryFilter = new QueryFilterClass();
            IFeatureCursor bFeatureCursor = bFeatureClass.Update(bQueryFilter, false);

            IFeature bFeature = bFeatureCursor.NextFeature();
            while (bFeature != null)
            {
                int IDValue = (int)bFeature.get_Value(0);
                if (ContainBuilding.Contains(IDValue))
                {
                    SingleBuildingList.Add(bFeature);
                }
                bFeature = bFeatureCursor.NextFeature();
            }
            #endregion

            #region 将满足条件的Building输出
            IFeatureClass OutFeatureClass = pFeatureHandle.createPolygonshapefile(pMap.SpatialReference, FilePath, fileNameExt);

            #region 添加一个字段
            if (OutFeatureClass.Fields.FindField("BID") < 0)
            {
                IClass bClass = OutFeatureClass as IClass;

                IFieldsEdit fldsE = OutFeatureClass.Fields as IFieldsEdit;
                IField fld = new FieldClass();
                IFieldEdit2 fldE = fld as IFieldEdit2;
                fldE.Type_2 = esriFieldType.esriFieldTypeInteger;
                fldE.Name_2 = "BID";
                bClass.AddField(fld);
            }
            #endregion

            IDataset dataset = (IDataset)OutFeatureClass;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
            IFeatureClassWrite fr = (IFeatureClassWrite)OutFeatureClass;
            //注意：此时，所编辑数据不能被其他程序打开
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            for (int i = 0; i < SingleBuildingList.Count; i++)
            {
                IFeature feature = OutFeatureClass.CreateFeature();
                feature.Shape = SingleBuildingList[i].Shape;
                feature.set_Value(3,SingleBuildingList[i].get_Value(4));
                feature.Store();//保存IFeature对象  
                fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上 
            }

            //关闭编辑
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
            #endregion

        }
        #endregion
    }
}
