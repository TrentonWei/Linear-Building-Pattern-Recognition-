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
    public partial class VGraphfrm : Form
    {
        IMap pMap;
        string localFilePath, fileNameExt, FilePath;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();

        public VGraphfrm(IMap cMap)
        {
            InitializeComponent();
            this.pMap = cMap;
        }

        #region 初始化
        private void VGraph_Load(object sender, EventArgs e)
        {
            if (this.pMap.LayerCount <= 0)
                return;

            ILayer pLayer;
            string strLayerName;
            for (int i = 0; i < this.pMap.LayerCount; i++)
            {
                pLayer = this.pMap.get_Layer(i);
                strLayerName = pLayer.Name;

                this.comboBox1.Items.Add(strLayerName);
                this.comboBox3.Items.Add(strLayerName);
            }

            this.comboBox1.SelectedIndex = 0;
            this.comboBox3.SelectedIndex = 0;
        }
        #endregion

        #region 给输出路径
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

            comboBox2.Text = localFilePath;
        }
        #endregion

        #region 确定
        private void button2_Click(object sender, EventArgs e)
        {
            IFeatureClass InputFeatureClass = pFeatureHandle.GetFeatureClass(pMap, this.comboBox1.Text.ToString());

            #region 输入图层为点要素生成算法
            if (InputFeatureClass.ShapeType == esriGeometryType.esriGeometryPoint)
            {
                if (InputFeatureClass.FeatureCount(null)<3)
                {
                    MessageBox.Show("输入的点数小于3个，不能构成三角形！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                IFields pFields = InputFeatureClass.Fields;

                #region Tin生成
                IGeoDataset pGDS = (IGeoDataset)InputFeatureClass;
                IEnvelope pEnv = (IEnvelope)pGDS.Extent;
                pEnv.SpatialReference = pGDS.SpatialReference;

                IField pHeightField = pFields.get_Field(pFields.FindField("Id"));
                ITinEdit pTinEdit = new TinClass();
                pTinEdit.InitNew(pEnv);
                object Missing = Type.Missing;
                pTinEdit.AddFromFeatureClass(InputFeatureClass, null, pHeightField, null, esriTinSurfaceType.esriTinMassPoint, ref Missing);
                #endregion

                #region 由Tin生成V图
                ISpatialReference sr = pMap.SpatialReference;
                IFeatureClass pFClass = pFeatureHandle.createPolygonshapefile(pMap.SpatialReference, FilePath, fileNameExt);
                ITinNodeCollection pTinNodeCollection = (ITinNodeCollection)pTinEdit;
                IFeatureLayer pFVLayer = new FeatureLayerClass();

                #region 判断是否需要约束图层
                if (checkBox1.CheckState == CheckState.Checked)
                {
                    IFeatureClass FVCclass = pFeatureHandle.GetFeatureClass(pMap, this.comboBox3.Text.ToString());
                    if (FVCclass.ShapeType == esriGeometryType.esriGeometryPolygon)//约束范围为区域
                    {
                        int count = FVCclass.FeatureCount(null);
                        if (count == 0)
                        {
                            MessageBox.Show("约束范围不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        else if (count > 1)
                        {
                            MessageBox.Show("约束区域不能有多个", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        else if (count == 1)//只能有一个约束范围
                        {
                            IFeatureCursor fcur = FVCclass.GetFeatures(null, false);
                            IFeature pfeature = fcur.NextFeature();
                            IPolygon pPolygon = pfeature.Shape as IPolygon;
                            //生成Voronoi图
                            pTinNodeCollection.ConvertToVoronoiRegions(pFClass, null, pPolygon, "Id", "Id");
                            //添加到地图文档中
                            pFVLayer.FeatureClass = pFClass;
                            pFVLayer.Name = fileNameExt;
                            ILayer pVLayer = (ILayer)pFVLayer;
                            pMap.AddLayer(pFVLayer);
                        }
                    }
                    else if (FVCclass.ShapeType == esriGeometryType.esriGeometryPolyline)//约束范围为线
                    {
                        //将线围成多边形
                        IPolygon pPolygon = pFeatureHandle.RoundLineToPolygon(FVCclass);
                        //生成Voronoi图
                        pTinNodeCollection.ConvertToVoronoiRegions(pFClass, null, pPolygon, "Id", "Id");
                        //添加到地图文档中
                        pFVLayer.FeatureClass = pFClass;
                        pFVLayer.Name = fileNameExt;
                        ILayer pVLayer = (ILayer)pFVLayer;
                        pMap.AddLayer(pFVLayer);
                    }
                    else if (FVCclass.ShapeType == esriGeometryType.esriGeometryPoint)
                    {
                        MessageBox.Show("约束区域不能是Point类型", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                #endregion

                #region 无约束图层
                else 
                {
                    //生成Voronoi图
                    pTinNodeCollection.ConvertToVoronoiRegions(pFClass, null, null, "Id", "Id");
                    //添加到地图文档中
                    pFVLayer.FeatureClass = pFClass;
                    pFVLayer.Name = fileNameExt;
                    ILayer pVLayer = (ILayer)pFVLayer;
                    pMap.AddLayer(pFVLayer);
                }
                #endregion 
                #endregion 

                #region 建立V图与建筑物的对应关系
                IFeatureClass pFeatureClass = pFVLayer.FeatureClass;
                for (int i = 0; i < pFeatureClass.FeatureCount(null); i++)
                {
                    IFeature pFeature = pFeatureClass.GetFeature(i);
                    IPolygon pPolygon = (IPolygon)pFeature.Shape;
                    ITopologicalOperator itPolygon = pPolygon as ITopologicalOperator;

                    for (int j = 0; j < InputFeatureClass.FeatureCount(null); j++)
                    {
                        IFeature cFeature = InputFeatureClass.GetFeature(j);
                        IPoint cPoint = (IPoint)cFeature.Shape;
                        IGeometry gTheIntersectPart = itPolygon.Intersect(cPoint, esriGeometryDimension.esriGeometry0Dimension);
                        if (gTheIntersectPart != null)
                        {
                            IDataset dataset = pFeatureClass as IDataset;
                            IWorkspace workspace = dataset.Workspace;
                            IWorkspaceEdit wse = workspace as IWorkspaceEdit;
                            IFields cFields = pFeature.Fields;

                            wse.StartEditing(false);
                            wse.StartEditOperation();

                            int fnum;
                            fnum = cFields.FieldCount;

                            for (int m = 0; m < fnum; m++)
                            {
                                if (cFields.get_Field(m).Name == "Id")
                                {
                                    int field1 = cFields.FindField("Id");
                                    pFeature.set_Value(field1, j);
                                    pFeature.Store();
                                }
                            }

                            wse.StopEditOperation();
                            wse.StopEditing(true);
                        }
                    }
                }
                #endregion

            }
            #endregion

            #region 输入图层为面要素生成算法
             else if(InputFeatureClass.ShapeType==esriGeometryType.esriGeometryPolygon)
            {
                //生成点集要素集，因为点集要生成TIN，所以生成三维点集
                ISpatialReference sr =pMap.SpatialReference;
                //将面要素转化为点元素
                IFeatureClass CenterFeatureClass = pFeatureHandle.createPointshapefile(pMap.SpatialReference, FilePath, fileNameExt + "Center");

                 for(int i=0;i<InputFeatureClass.FeatureCount(null);i++)
                 {
                    IFeature pFeature =InputFeatureClass.GetFeature(i);
                    IPolygon pPolygon = pFeature.Shape as IPolygon;
                    IArea pArea = (IArea)pPolygon;
                    IPoint pCenter = pArea.Centroid;

                    IFeature cFeature=CenterFeatureClass.CreateFeature();
                    cFeature.Shape= pCenter as IGeometry;
                    cFeature.Store();
                 }

                if (CenterFeatureClass.FeatureCount(null)<3)
                {
                    MessageBox.Show("输入的点数小于3个，不能构成三角形！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                } 
 
                IFields pFields = CenterFeatureClass.Fields;
                //先生成TIN
                IGeoDataset pGDS = (IGeoDataset)CenterFeatureClass;
                IEnvelope pEnv = (IEnvelope)pGDS.Extent;
                pEnv.SpatialReference = pGDS.SpatialReference;

                IField pHeightField = pFields.get_Field(pFields.FindField("Id"));
                ITinEdit pTinEdit = new TinClass();
                pTinEdit.InitNew(pEnv);
                object Missing = Type.Missing;
                pTinEdit.AddFromFeatureClass(CenterFeatureClass, null,pHeightField, null, esriTinSurfaceType.esriTinMassPoint, ref Missing);

                //由TIN生成Voronoi图
                //先生成Voronoi图的要素集
                IFeatureClass pFClass = pFeatureHandle.createPolygonshapefile(pMap.SpatialReference, FilePath, fileNameExt);
                ITinNodeCollection pTinNodeCollection = (ITinNodeCollection)pTinEdit;
                IFeatureLayer pFVLayer = new FeatureLayerClass();

                #region 判断是否需要约束图层
                if (checkBox1.CheckState == CheckState.Checked)
                {
                    IFeatureClass FVCclass = pFeatureHandle.GetFeatureClass(pMap, this.comboBox3.Text.ToString());
                    if (FVCclass.ShapeType == esriGeometryType.esriGeometryPolygon)//约束范围为区域
                    {
                        int count = FVCclass.FeatureCount(null);
                        if (count == 0)
                        {
                            MessageBox.Show("约束范围不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        else if (count > 1)
                        {
                            MessageBox.Show("约束区域不能有多个", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        else if (count == 1)//只能有一个约束范围
                        {
                            IFeature pfeature = FVCclass.GetFeature(0);
                            IPolygon pPolygon = pfeature.Shape as IPolygon;
                            //生成Voronoi图
                            pTinNodeCollection.ConvertToVoronoiRegions(pFClass, null, pPolygon, "Id", "Id");
                            //添加到地图文档中
                            pFVLayer.FeatureClass = pFClass;
                            pFVLayer.Name = fileNameExt;
                            ILayer pVLayer = (ILayer)pFVLayer;
                            pMap.AddLayer(pFVLayer);
                        }
                    }
                    else if (FVCclass.ShapeType == esriGeometryType.esriGeometryPolyline)//约束范围为线
                    {
                        //将线围成多边形
                        IPolygon pPolygon = pFeatureHandle.RoundLineToPolygon(FVCclass);
                        //生成Voronoi图
                        pTinNodeCollection.ConvertToVoronoiRegions(pFClass, null, pPolygon, "Id", "Id");
                        //添加到地图文档中
                        pFVLayer.FeatureClass = pFClass;
                        pFVLayer.Name = fileNameExt;
                        ILayer pVLayer = (ILayer)pFVLayer;
                        pMap.AddLayer(pFVLayer);
                    }
                    else if (FVCclass.ShapeType == esriGeometryType.esriGeometryPoint)
                    {
                        MessageBox.Show("约束区域不能是Point类型", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                #endregion

                #region 无约束图层
                else 
                {
                    //生成Voronoi图
                    pTinNodeCollection.ConvertToVoronoiRegions(pFClass, null, null, "Id", "Id");
                    //添加到地图文档中
                    pFVLayer.FeatureClass = pFClass;
                    pFVLayer.Name = fileNameExt;
                    ILayer pVLayer = (ILayer)pFVLayer;
                    pMap.AddLayer(pFVLayer);
                }
                #endregion

                #region 建立V图与建筑物的对应关系
                IFeatureClass pFeatureClass = pFVLayer.FeatureClass;
                for (int i = 0; i < pFeatureClass.FeatureCount(null); i++)
                {
                    IFeature pFeature = pFeatureClass.GetFeature(i);
                    IPolygon pPolygon = (IPolygon)pFeature.Shape;
                    ITopologicalOperator itPolygon = pPolygon as ITopologicalOperator;

                    for (int j = 0; j < CenterFeatureClass.FeatureCount(null); j++)
                    {
                        IFeature cFeature = CenterFeatureClass.GetFeature(j);
                        IPoint cPoint = (IPoint)cFeature.Shape;
                        IGeometry gTheIntersectPart = itPolygon.Intersect(cPoint, esriGeometryDimension.esriGeometry0Dimension);
                        if (!gTheIntersectPart.IsEmpty)
                        {
                            IDataset dataset = pFeatureClass as IDataset;
                            IWorkspace workspace = dataset.Workspace;
                            IWorkspaceEdit wse = workspace as IWorkspaceEdit;
                            IFields cFields = pFeature.Fields;

                            wse.StartEditing(false);
                            wse.StartEditOperation();

                            int fnum;
                            fnum = cFields.FieldCount;

                            for (int m = 0; m < fnum; m++)
                            {
                                if (cFields.get_Field(m).Name == "Id")
                                {
                                    int field1 = cFields.FindField("Id");
                                    pFeature.set_Value(field1, j);
                                    pFeature.Store();
                                }
                            }

                            wse.StopEditOperation();
                            wse.StopEditing(true);
                        }
                    }
                }
                #endregion

            }
            #endregion

            #region 输入图层为线要素生成算法
            else if (InputFeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
            {
                //生成点集要素集，因为点集要生成TIN，所以生成三维点集
                ISpatialReference sr = pMap.SpatialReference;
                IFeatureClass PointFeatureClass = pFeatureHandle.createPointshapefile(pMap.SpatialReference, FilePath, fileNameExt + "Point");
                //往点要素中添加点
                for (int i = 0; i < InputFeatureClass.FeatureCount(null);i++)
                {
                    IFeature pFeature = InputFeatureClass.GetFeature(i);
                    IFields fields = pFeature.Fields;
                    int b = (int)pFeature.get_Value(fields.FindField("Id"));

                    IPolyline plPolyline = pFeature.Shape as IPolyline;
                    IPointCollection pPointColl = (IPointCollection)plPolyline;
                    for (int t = 0; t < pPointColl.PointCount; t++)
                    {
                        IPoint pPo = pPointColl.get_Point(t);
                        IFeature zpfeature = PointFeatureClass.CreateFeature();
                        zpfeature.Shape = pPo as IGeometry;
                        zpfeature.Store();
                    }
                }
                int Count = PointFeatureClass.FeatureCount(null);
                if (Count < 3)
                {
                    MessageBox.Show("输入的点数小于3个，不能构成三角形！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                IFields pFields = PointFeatureClass.Fields;
                //先生成TIN
                IGeoDataset pGDS = (IGeoDataset)PointFeatureClass;
                IEnvelope pEnv = (IEnvelope)pGDS.Extent;
                pEnv.SpatialReference = pGDS.SpatialReference;

                IField pHeightField = pFields.get_Field(pFields.FindField("Id"));
                ITinEdit pTinEdit = new TinClass();
                pTinEdit.InitNew(pEnv);
                object Missing = Type.Missing;
                pTinEdit.AddFromFeatureClass(PointFeatureClass, null, pHeightField, null, esriTinSurfaceType.esriTinMassPoint, ref Missing);

                //由TIN生成Voronoi图
                //先生成Voronoi图的要素集
                IFeatureClass pFClass = pFeatureHandle.createPolygonshapefile(pMap.SpatialReference, FilePath, fileNameExt);
                ITinNodeCollection pTinNodeCollection = (ITinNodeCollection)pTinEdit;

                #region 判断是否需要约束图层
                if (checkBox1.CheckState == CheckState.Checked)
                {
                    IFeatureClass FVCclass = pFeatureHandle.GetFeatureClass(pMap, this.comboBox3.Text.ToString());
                    if (FVCclass.ShapeType == esriGeometryType.esriGeometryPolygon)//约束范围为区域
                    {
                        int count = FVCclass.FeatureCount(null);
                        if (count == 0)
                        {
                            MessageBox.Show("约束范围不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        else if (count > 1)
                        {
                            MessageBox.Show("约束区域不能有多个", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        else if (count == 1)//只能有一个约束范围
                        {
                            IFeatureCursor fcur = FVCclass.GetFeatures(null, false);
                            IFeature pfeature = fcur.NextFeature();
                            IPolygon pPolygon = pfeature.Shape as IPolygon;
                            //生成Voronoi图
                            pTinNodeCollection.ConvertToVoronoiRegions(pFClass, null, pPolygon, "Id", "Id");
                            //添加到地图文档中
                            IFeatureLayer pFVLayer = new FeatureLayerClass();
                            pFVLayer.FeatureClass = pFClass;
                            pFVLayer.Name = fileNameExt;
                            ILayer pVLayer = (ILayer)pFVLayer;
                            pMap.AddLayer(pFVLayer);
                        }
                    }
                    else if (FVCclass.ShapeType == esriGeometryType.esriGeometryPolyline)//约束范围为线
                    {
                        //将线围成多边形
                        IPolygon pPolygon = pFeatureHandle.RoundLineToPolygon(FVCclass);
                        //生成Voronoi图
                        pTinNodeCollection.ConvertToVoronoiRegions(pFClass, null, pPolygon, "Id", "Id");
                        //添加到地图文档中
                        IFeatureLayer pFVLayer = new FeatureLayerClass();
                        pFVLayer.FeatureClass = pFClass;
                        pFVLayer.Name = fileNameExt;
                        ILayer pVLayer = (ILayer)pFVLayer;
                        pMap.AddLayer(pFVLayer);
                    }
                    else if (FVCclass.ShapeType == esriGeometryType.esriGeometryPoint)
                    {
                        MessageBox.Show("约束区域不能是Point类型", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                #endregion

                #region 无约束图层
                else
                {
                    //生成Voronoi图
                    pTinNodeCollection.ConvertToVoronoiRegions(pFClass, null, null, "Id", "Id");
                    //添加到地图文档中
                    IFeatureLayer pFVLayer = new FeatureLayerClass();
                    pFVLayer.FeatureClass = pFClass;
                    pFVLayer.Name = fileNameExt;
                    ILayer pVLayer = (ILayer)pFVLayer;
                    pMap.AddLayer(pFVLayer);
                }
                #endregion

            }
            #endregion
           
        }
        #endregion

        #region 取消
        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion
    }
}
