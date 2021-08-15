using System;
using System.Collections.Generic;
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
using PrDispalce.地图要素;

namespace PrDispalce.工具类
{

    public class SaveNewObjects
    {
        public static int iIndex = 0;
        /// <summary>
        /// 保存多边形
        /// </summary>
        /// <param name="polygonlist">多边形数据</param>
        /// <param name="sr">空间参考</param>
        /// <param name="strPath">存放路径</param>
         public static void SavePointObject(List<PointObject> medPointLst, ISpatialReference sr, string filePath)
        {


            //生成新的多边形要素集
            #region 创建一个点的shape文件
            string strFolder = filePath;
            string name = (iIndex++).ToString();
            string strName = "MedPoint" + name;
            string strShapeFieldName = "Shape";
            IFeatureWorkspace pFWS;//ESRI.ArcGIS.Geodatabase;
            IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();//ESRI.ArcGIS.DataSourcesFile
            pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;
            IFeatureClass pFeatClass;
            //创建一个字段集
            IFields pFields = new ESRI.ArcGIS.Geodatabase.FieldsClass();
            IFieldsEdit pFieldsEdit;
            pFieldsEdit = pFields as IFieldsEdit;

            #region 创建图形字段
            IField pField;
            IFieldEdit pFieldEdit;
            //创建图形字段
            pField = new FieldClass();
            pFieldEdit = pField as IFieldEdit;
            pFieldEdit.Name_2 = strShapeFieldName;
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPoint;
            pGeomDefEdit.SpatialReference_2 = sr;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            //ID
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);
            #endregion
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFWS);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pWorkspaceFactory);
            #endregion


            //添加点
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            //获取图层的数据集
            IDataset pIDataset = (IDataset)pFeatClass;
            IWorkspaceEdit workspaceEdit = null;
            if (pIDataset != null)
            {
                //并创建工作空间
                workspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            }
            try
            {
                if (pFeatClass == null)
                    return;

                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();
                //点个数
                int n = medPointLst.Count;
                if (n == 0)
                    return;
                
                for (int i = 0; i < n; i++)
                {

                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PointClass();
                    IPointCollection pointSet = shp as IPointCollection;
                    PointObject curPoint = null;
                    if (medPointLst[i] == null)
                        continue;
                    curPoint = medPointLst[i];
                    ((PointClass)shp).PutCoords(curPoint.Point.X, curPoint.Point.Y);
                    feature.Shape = shp;
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);
                }
                
                //将IFeature对象，添加到当前图层上 
                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("异常信息" + ex.Message);
            }
        }
        public static void SavePolygons(List<PolygonObject> polygonlist, ISpatialReference sr, string filePath,string pname)
        {


            //生成新的多边形要素集
            #region 创建一个多边形的shape文件
            string strFolder = filePath;

            string name = (iIndex++).ToString();
            string strName = "Cluster" + name;
            string strShapeFieldName = "Shape";
            IFeatureWorkspace pFWS;//ESRI.ArcGIS.Geodatabase;
            IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();//ESRI.ArcGIS.DataSourcesFile
            pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;
            IFeatureClass pFeatClass;
            //创建一个字段集
            IFields pFields = new ESRI.ArcGIS.Geodatabase.FieldsClass();
            IFieldsEdit pFieldsEdit;
            pFieldsEdit = pFields as IFieldsEdit;

            #region 创建图形字段
            IField pField;
            IFieldEdit pFieldEdit;
            //创建图形字段
            pField = new FieldClass();
            pFieldEdit = pField as IFieldEdit;
            pFieldEdit.Name_2 = strShapeFieldName;
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;

            //ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            //IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            //ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
            pGeomDefEdit.SpatialReference_2 = sr;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            //ID
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);
            #endregion
            //Label

            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;
            pFieldEdit2.Name_2 = "label";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField2);
            pFeatClass = pFWS.CreateFeatureClass(pname, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");

            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFWS);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pWorkspaceFactory);
            #endregion


            //添加多边形
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            //获取图层的数据集
            IDataset pIDataset = (IDataset)pFeatClass;
            IWorkspaceEdit workspaceEdit = null;
            if (pIDataset != null)
            {
                //并创建工作空间
                workspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            }
            try
            {
                if (pFeatClass == null)
                    return;

                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();
                //多边形个数
                int n = polygonlist.Count;
                if (n == 0)
                    return;
                PolygonObject polygon = null;
                List<TriDot> dotlist = null;
                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolygonClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = new PointClass();
                    TriDot curPoint = null;
                    polygon = polygonlist[i];
                    if (polygon == null)
                        continue;
                    dotlist = polygon.PointList;
                    int count = dotlist.Count;
                    if (count == 0)
                        continue;
                    for (int j = 0; j < count; j++)
                    {
                        curPoint = dotlist[j];
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, missing1, missing2);
                    }

                    feature.Shape = shp;
                    feature.set_Value(2, polygon.ID);
                    feature.set_Value(3, polygon.Label);
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }
                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("异常信息" + ex.Message);
            }
        }
        public static void SavePolygonsDis(List<PolygonObject> polygonlist, ISpatialReference sr, string filePath)
        {


            //生成新的多边形要素集
            #region 创建一个多边形的shape文件
            string strFolder = filePath;

            string name = (iIndex++).ToString();
            string strName = "afterDisPolygon" + name;
            string strShapeFieldName = "Shape";
            IFeatureWorkspace pFWS;//ESRI.ArcGIS.Geodatabase;
            IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();//ESRI.ArcGIS.DataSourcesFile
            pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;
            IFeatureClass pFeatClass;
            //创建一个字段集
            IFields pFields = new ESRI.ArcGIS.Geodatabase.FieldsClass();
            IFieldsEdit pFieldsEdit;
            pFieldsEdit = pFields as IFieldsEdit;

            #region 创建图形字段
            IField pField;
            IFieldEdit pFieldEdit;
            //创建图形字段
            pField = new FieldClass();
            pFieldEdit = pField as IFieldEdit;
            pFieldEdit.Name_2 = strShapeFieldName;
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;

            //ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            //IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            //ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
            pGeomDefEdit.SpatialReference_2 = sr;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            //ID
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);
            #endregion

            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");

            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFWS);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pWorkspaceFactory);
            #endregion


            //添加多边形
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            //获取图层的数据集
            IDataset pIDataset = (IDataset)pFeatClass;
            IWorkspaceEdit workspaceEdit = null;
            if (pIDataset != null)
            {
                //并创建工作空间
                workspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            }
            try
            {
                if (pFeatClass == null)
                    return;

                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();
                //多边形个数
                int n = polygonlist.Count;
                if (n == 0)
                    return;
                PolygonObject polygon = null;
                List<TriDot> dotlist = null;
                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolygonClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = new PointClass();
                    //PointObject curPoint = null;
                    polygon = polygonlist[i];
                    if (polygon == null)
                        continue;
                    dotlist = polygon.PointList;
                    int count = dotlist.Count;
                    if (count == 0)
                        continue;
                    for (int j = 0; j < count; j++)
                    {
                        curResultPoint.PutCoords(dotlist[j].X, dotlist[j].Y);
                        pointSet.AddPoint(curResultPoint, missing1, missing2);
                    }
                    feature.Shape = shp;
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }
                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("异常信息" + ex.Message);
            }
        }
        public static void SavePolygonsbetweenDis(List<PolygonObject> polygonlist, ISpatialReference sr, string filePath)
        {


            //生成新的多边形要素集
            #region 创建一个多边形的shape文件
            string strFolder = filePath;

            string name = (iIndex++).ToString();
            string strName = "afterDisPolygonbetweenRoad" + name;
            string strShapeFieldName = "Shape";
            IFeatureWorkspace pFWS;//ESRI.ArcGIS.Geodatabase;
            IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();//ESRI.ArcGIS.DataSourcesFile
            pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;
            IFeatureClass pFeatClass;
            //创建一个字段集
            IFields pFields = new ESRI.ArcGIS.Geodatabase.FieldsClass();
            IFieldsEdit pFieldsEdit;
            pFieldsEdit = pFields as IFieldsEdit;

            #region 创建图形字段
            IField pField;
            IFieldEdit pFieldEdit;
            //创建图形字段
            pField = new FieldClass();
            pFieldEdit = pField as IFieldEdit;
            pFieldEdit.Name_2 = strShapeFieldName;
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;

            //ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            //IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            //ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
            pGeomDefEdit.SpatialReference_2 = sr;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            //ID
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);
            #endregion

            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");

            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFWS);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pWorkspaceFactory);
            #endregion


            //添加多边形
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            //获取图层的数据集
            IDataset pIDataset = (IDataset)pFeatClass;
            IWorkspaceEdit workspaceEdit = null;
            if (pIDataset != null)
            {
                //并创建工作空间
                workspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            }
            try
            {
                if (pFeatClass == null)
                    return;

                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();
                //多边形个数
                int n = polygonlist.Count;
                if (n == 0)
                    return;
                PolygonObject polygon = null;
                List<TriDot> dotlist = null;
                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolygonClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = new PointClass();
                    //PointObject curPoint = null;
                    polygon = polygonlist[i];
                    if (polygon == null)
                        continue;
                    dotlist = polygon.PointList;
                    int count = dotlist.Count;
                    if (count == 0)
                        continue;
                    for (int j = 0; j < count; j++)
                    {
                        curResultPoint.PutCoords(dotlist[j].X, dotlist[j].Y);
                        pointSet.AddPoint(curResultPoint, missing1, missing2);
                    }
                    feature.Shape = shp;
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }
                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("异常信息" + ex.Message);
            }
        }
        public static void SavePolygonsRoadCluster(List<PolygonObject> polygonlist, ISpatialReference sr, string filePath)
        {


            //生成新的多边形要素集
            #region 创建一个多边形的shape文件
            string strFolder = filePath;

            string name = (iIndex++).ToString();
            string strName = "betweenRoadbuildsCluster" + name;
            string strShapeFieldName = "Shape";
            IFeatureWorkspace pFWS;//ESRI.ArcGIS.Geodatabase;
            IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();//ESRI.ArcGIS.DataSourcesFile
            pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;
            IFeatureClass pFeatClass;
            //创建一个字段集
            IFields pFields = new ESRI.ArcGIS.Geodatabase.FieldsClass();
            IFieldsEdit pFieldsEdit;
            pFieldsEdit = pFields as IFieldsEdit;
            #region 创建图形字段
            IField pField;
            IFieldEdit pFieldEdit;
            //创建图形字段
            pField = new FieldClass();
            pFieldEdit = pField as IFieldEdit;
            pFieldEdit.Name_2 = strShapeFieldName;
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
            //ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            //IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            //ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;
            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
            pGeomDefEdit.SpatialReference_2 = sr;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            //ID
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);
            #endregion
            // label
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;
            pFieldEdit2.Name_2 = "label";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField2);
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFWS);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pWorkspaceFactory);
            #endregion
            //添加多边形
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            //获取图层的数据集
            IDataset pIDataset = (IDataset)pFeatClass;
            IWorkspaceEdit workspaceEdit = null;
            if (pIDataset != null)
            {
                //并创建工作空间
                workspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            }
            try
            {
                if (pFeatClass == null)
                    return;

                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();
                //多边形个数
                int n = polygonlist.Count;
                if (n == 0)
                    return;
                PolygonObject polygon = null;
                List<TriDot> dotlist = null;
                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolygonClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = new PointClass();
                    TriDot curPoint = null;
                    polygon = polygonlist[i];
                    if (polygon == null)
                        continue;
                    dotlist = polygon.PointList;
                    int count = dotlist.Count;
                    if (count == 0)
                        continue;
                    for (int j = 0; j < count; j++)
                    {
                        curPoint = dotlist[j];
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, missing1, missing2);
                    }
                    feature.Shape = shp;
                    feature.set_Value(2, polygon.ID);
                    feature.set_Value(3, polygon.Label);
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }
                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("异常信息" + ex.Message);
            }
        }//添加上有街道的建筑物聚类，增加显示label属性字段，方便查看聚类效果
        public static void SavePolygonsafterRoadCluster(List<PolygonObject> polygonlist, ISpatialReference sr, string filePath)
        {


            //生成新的多边形要素集
            #region 创建一个多边形的shape文件
            string strFolder = filePath;

            string name = (iIndex++).ToString();
            string strName = "afterRoadbuildsConflictDect" + name;
            string strShapeFieldName = "Shape";
            IFeatureWorkspace pFWS;//ESRI.ArcGIS.Geodatabase;
            IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();//ESRI.ArcGIS.DataSourcesFile
            pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;
            IFeatureClass pFeatClass;
            //创建一个字段集
            IFields pFields = new ESRI.ArcGIS.Geodatabase.FieldsClass();
            IFieldsEdit pFieldsEdit;
            pFieldsEdit = pFields as IFieldsEdit;
            #region 创建图形字段
            IField pField;
            IFieldEdit pFieldEdit;
            //创建图形字段
            pField = new FieldClass();
            pFieldEdit = pField as IFieldEdit;
            pFieldEdit.Name_2 = strShapeFieldName;
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
            //ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            //IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            //ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;
            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
            pGeomDefEdit.SpatialReference_2 = sr;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            //ID
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);
            #endregion
            // label
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;
            pFieldEdit2.Name_2 = "label";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField2);
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");

            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFWS);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pWorkspaceFactory);
            #endregion
            //添加多边形
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            //获取图层的数据集
            IDataset pIDataset = (IDataset)pFeatClass;
            IWorkspaceEdit workspaceEdit = null;
            if (pIDataset != null)
            {
                //并创建工作空间
                workspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            }
            try
            {
                if (pFeatClass == null)
                    return;

                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();
                //多边形个数
                int n = polygonlist.Count;
                if (n == 0)
                    return;
                PolygonObject polygon = null;
                List<TriDot> dotlist = null;
                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolygonClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = new PointClass();
                    TriDot curPoint = null;
                    polygon = polygonlist[i];
                    if (polygon == null)
                        continue;
                    dotlist = polygon.PointList;
                    int count = dotlist.Count;
                    if (count == 0)
                        continue;
                    for (int j = 0; j < count; j++)
                    {
                        curPoint = dotlist[j];
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, missing1, missing2);
                    }
                    feature.Shape = shp;
                    feature.set_Value(2, polygon.ID);
                    feature.set_Value(3, polygon.Label);
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }
                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("异常信息" + ex.Message);
            }
        }//添加上有街道的建筑物聚类，增加显示label属性字段，方便查看聚类效果
        public static void SavePolygonsafterRoadDisplacement(List<PolygonObject> polygonlist, ISpatialReference sr, string filePath)
        {


            //生成新的多边形要素集
            #region 创建一个多边形的shape文件
            string strFolder = filePath;

            string name = (iIndex++).ToString();
            string strName = "building(road)_afterDis" + name;
            string strShapeFieldName = "Shape";
            IFeatureWorkspace pFWS;//ESRI.ArcGIS.Geodatabase;
            IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();//ESRI.ArcGIS.DataSourcesFile
            pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;
            IFeatureClass pFeatClass;
            //创建一个字段集
            IFields pFields = new ESRI.ArcGIS.Geodatabase.FieldsClass();
            IFieldsEdit pFieldsEdit;
            pFieldsEdit = pFields as IFieldsEdit;
            #region 创建图形字段
            IField pField;
            IFieldEdit pFieldEdit;
            //创建图形字段
            pField = new FieldClass();
            pFieldEdit = pField as IFieldEdit;
            pFieldEdit.Name_2 = strShapeFieldName;
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
            //ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            //IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            //ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;
            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
            pGeomDefEdit.SpatialReference_2 = sr;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            //ID
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);
            #endregion
            // label
            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;
            pFieldEdit2.Name_2 = "label";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField2);
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");

            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFWS);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pWorkspaceFactory);
            #endregion
            //添加多边形
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            //获取图层的数据集
            IDataset pIDataset = (IDataset)pFeatClass;
            IWorkspaceEdit workspaceEdit = null;
            if (pIDataset != null)
            {
                //并创建工作空间
                workspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            }
            try
            {
                if (pFeatClass == null)
                    return;

                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();
                //多边形个数
                int n = polygonlist.Count;
                if (n == 0)
                    return;
                PolygonObject polygon = null;
                List<TriDot> dotlist = null;
                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolygonClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = new PointClass();
                    TriDot curPoint = null;
                    polygon = polygonlist[i];
                    if (polygon == null)
                        continue;
                    dotlist = polygon.PointList;
                    int count = dotlist.Count;
                    if (count == 0)
                        continue;
                    for (int j = 0; j < count; j++)
                    {
                        curPoint = dotlist[j];
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, missing1, missing2);
                    }
                    feature.Shape = shp;
                    feature.set_Value(2, polygon.ID);
                    feature.set_Value(3, polygon.Label);
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }
                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("异常信息" + ex.Message);
            }
        }
        /// <summary>
        /// 保存线
        /// </summary>
        /// <param name="polylinelist">线数据</param>
        /// <param name="sr">空间参考</param>
        /// /// <param name="strPath">存放路径</param>
        public static void SavePolylines(List<PolylineObject> polylinelist, ISpatialReference sr, string filePath)
        {
            //生成新的多边形要素集
            #region 创建一个线的shape文件
            string Folderpathstr = filePath;
            string LyrName = "distPolyline2";
            string strFolder = Folderpathstr;
            string strName = LyrName;
            string strShapeFieldName = "Shape";

            IFeatureWorkspace pFWS;//ESRI.ArcGIS.Geodatabase;
            IWorkspaceFactory pWorkspaceFactory = new ESRI.ArcGIS.DataSourcesFile.ShapefileWorkspaceFactoryClass();//ESRI.ArcGIS.DataSourcesFile
            pFWS = pWorkspaceFactory.OpenFromFile(strFolder, 0) as IFeatureWorkspace;

            //创建一个字段集
            IFields pFields = new ESRI.ArcGIS.Geodatabase.FieldsClass();
            IFieldsEdit pFieldsEdit;
            pFieldsEdit = pFields as IFieldsEdit;

            #region 创建图形字段
            IField pField;
            IFieldEdit pFieldEdit;
            //创建图形字段
            pField = new FieldClass();
            pFieldEdit = pField as IFieldEdit;
            pFieldEdit.Name_2 = strShapeFieldName;
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;

            //ISpatialReferenceFactory ispfac = new SpatialReferenceEnvironmentClass();
            //IProjectedCoordinateSystem iprcoorsys = ispfac.CreateProjectedCoordinateSystem((int)prj);
            //ISpatialReference pSpatialReference = iprcoorsys as ISpatialReference;

            IGeometryDef pGeomDef = new GeometryDefClass();
            IGeometryDefEdit pGeomDefEdit = pGeomDef as IGeometryDefEdit;
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
            pGeomDefEdit.SpatialReference_2 = sr;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            ////ID
            //IField pField1;
            //IFieldEdit pFieldEdit1;
            //pField1 = new FieldClass();
            //pFieldEdit1 = pField1 as IFieldEdit;
            //pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            //pFieldEdit1.Name_2 = "ID";
            //pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            //pFieldsEdit.AddField(pField1);
            #endregion

            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");

            #endregion

            //添加多边形
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            //获取图层的数据集
            IDataset pIDataset = (IDataset)pFeatClass;
            IWorkspaceEdit workspaceEdit = null;
            if (pIDataset != null)
            {
                //并创建工作空间
                workspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            }
            try
            {
                if (pFeatClass == null)
                    return;

                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();
                //线个数
                int n = polylinelist.Count;
                if (n == 0)
                    return;
                PolylineObject polyline = null;
                List<TriDot> dotlist = null;
                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolylineClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = new PointClass();
                    TriDot curPoint = null;
                    polyline = polylinelist[i];
                    if (polyline == null)
                        continue;
                    dotlist = polyline.PointList;
                    int count = dotlist.Count;
                    if (count == 0)
                        continue;
                    for (int j = 0; j < count; j++)
                    {
                        curPoint = dotlist[j];
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, missing1, missing2);
                    }
                    feature.Shape = shp;
                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }
                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("异常信息" + ex.Message);
            }
        }
    }
}

        