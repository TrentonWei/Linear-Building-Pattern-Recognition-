using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using System.Windows.Forms;
using System.IO;
using System.Data;
using AuxStructureLib.IO;

namespace AuxStructureLib
{
    /// <summary>
    /// Voronoi
    /// </summary>
    public class VoronoiDiagram
    {
        public List<VoronoiPolygon> VorPolygonList = null; //Voronoi多边形列表
        public List<TriNode> PointList = null;             //点集
        DelaunayTin tin  = null;                    //三角网
        ConvexNull convexNull = null;               //点集的凸包
        public Skeleton Skeleton = null;
        public ProxiGraph ProxiGraph=null;
        public SMap Map=null;
        public List<List<VoronoiPolygon>> FielderListforRoads = null;//基于道路建立的移位场
        public List<List<VoronoiPolygon>> FielderListforBuildings = null;//基于两个建筑物建立的移位场
        public List<List<VoronoiPolygon>> FielderListforBuilding = null;//基于一个建筑物建立的移位场

        public List<List<PolygonObject>> FielderListforRoads1 = null;//基于道路建立的移位场
        public List<List<PolygonObject>> FielderListforBuildings1 = null;//基于两个建筑物建立的移位场
        public List<List<PolygonObject>> FielderListforBuilding1 = null;//基于一个建筑物建立的移位场

        public List<List<List<List<PolygonObject>>>> PatternFieldListforRoads = null;//顾及道路建立的pattern层次邻近关系
        /// <summary>
        /// 从骨架线创建V图
        /// </summary>
        /// <param name="skeleton"></param>
        public VoronoiDiagram(Skeleton skeleton, ProxiGraph proxiGraph, SMap map)
        {
            ProxiGraph = proxiGraph;          
            Skeleton = skeleton;
            Map=map;
            VorPolygonList = new List<VoronoiPolygon>();
        }

        /// <summary>
        /// 根据ID和类型获取多边形对象
        /// </summary>
        /// <param name="id">ID号</param>
        /// <param name="Type">类型</param>
        /// <returns>返回类型</returns>
        public VoronoiPolygon GetVPbyIDandType(int id, FeatureType Type)
        {
            foreach (VoronoiPolygon vp in this.VorPolygonList)
            {
                if (vp.MapObj != null && vp.MapObj.ID == id && vp.MapObj.FeatureType == Type)
                    return vp;
            }
            return null;
        }

        /// <summary>
        /// 从骨架线创建建筑物和点群的的V图
        /// </summary>
        public void CreateVoronoiDiagramfrmSkeletonforBuildings()
        {
            //初始化多边形，根据邻近图
            foreach (ProxiNode node in ProxiGraph.NodeList)
            {
                if (node.FeatureType == FeatureType.PolygonType)
                {
                    int id = node.TagID;
                    TriNode point=new TriNode(node.X,node.Y);
                    MapObject mapObj=this.Map.GetObjectbyID(id,FeatureType.PolygonType);
                    VoronoiPolygon curPolygon = new VoronoiPolygon(mapObj, point);
                    this.VorPolygonList.Add(curPolygon);
                }
            }
            //将骨架线弧段分配到每个多边形的弧段列表中
            foreach (Skeleton_Arc curArc in this.Skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.LeftMapObj.FeatureType == FeatureType.PolygonType)
                {
                    int id = curArc.LeftMapObj.ID;
                    VoronoiPolygon curVP=this.GetVoronoiPolygonbyTagIDandType(id, curArc.LeftMapObj.FeatureType);
                   if(curVP!=null)
                    {curVP.ArcList.Add(curArc);}
                }
                if (curArc.RightMapObj != null && curArc.RightMapObj.FeatureType == FeatureType.PolygonType)
                {
                    int id = curArc.RightMapObj.ID;
                    VoronoiPolygon curVP=this.GetVoronoiPolygonbyTagIDandType(id, curArc.RightMapObj.FeatureType);
                    if (curVP != null)
                    { curVP.ArcList.Add(curArc); }
                }
            }
            //创建VP
            foreach(VoronoiPolygon vp in this.VorPolygonList)
            {
                //StreamWriter streamw = File.CreateText(@"K:\4-10\北京\Result" +@"\temp.TXT");
                //streamw.Write("No" + "  " + "sx" + "  " + "sy" + "  " + "sx" + "  " + "sy");
                //streamw.WriteLine();
                //foreach (Skeleton_Arc arc in vp.ArcList)
                //{
                //    streamw.Write(arc.PointList[0].X.ToString() + "  " + arc.PointList[0].Y.ToString() + "  " + arc.PointList[arc.PointList.Count - 1].X.ToString() + "  " + arc.PointList[arc.PointList.Count - 1].Y.ToString());
                //    streamw.WriteLine();
                //}
                //streamw.Close();
                //break;
                vp.CreateVoronoiPolygonfrmSkeletonArcList();
            }
        }

        /// <summary>
        /// 从骨架线创建建筑物的V图
        /// </summary>
        public void CreateVoronoiDiagramfrmSkeletonforBuildingsPoints()
        {
            //初始化多边形，根据邻近图
            foreach (ProxiNode node in ProxiGraph.NodeList)
            {
                if (node.FeatureType != FeatureType.PolylineType)
                {
                    int id = node.TagID;
                    TriNode point = new TriNode(node.X, node.Y);
                    MapObject mapObj = this.Map.GetObjectbyID(id, node.FeatureType);
                    VoronoiPolygon curPolygon = new VoronoiPolygon(mapObj, point);
                    this.VorPolygonList.Add(curPolygon);
                }
            }
            //将骨架线弧段分配到每个多边形的弧段列表中
            foreach (Skeleton_Arc curArc in this.Skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.LeftMapObj.FeatureType != FeatureType.PolylineType)
                {
                    int id = curArc.LeftMapObj.ID;
                    VoronoiPolygon curVP = this.GetVoronoiPolygonbyTagIDandType(id, curArc.LeftMapObj.FeatureType);
                    if (curVP != null)
                    { curVP.ArcList.Add(curArc); }
                }
                if (curArc.RightMapObj != null && curArc.RightMapObj.FeatureType != FeatureType.PolylineType)
                {
                    int id = curArc.RightMapObj.ID;
                    VoronoiPolygon curVP = this.GetVoronoiPolygonbyTagIDandType(id, curArc.RightMapObj.FeatureType);
                    if (curVP != null)
                    { curVP.ArcList.Add(curArc); }
                }
            }
            //创建VP
            foreach (VoronoiPolygon vp in this.VorPolygonList)
            {
                //StreamWriter streamw = File.CreateText(@"K:\4-10\北京\Result" +@"\temp.TXT");
                //streamw.Write("No" + "  " + "sx" + "  " + "sy" + "  " + "sx" + "  " + "sy");
                //streamw.WriteLine();
                //foreach (Skeleton_Arc arc in vp.ArcList)
                //{
                //    streamw.Write(arc.PointList[0].X.ToString() + "  " + arc.PointList[0].Y.ToString() + "  " + arc.PointList[arc.PointList.Count - 1].X.ToString() + "  " + arc.PointList[arc.PointList.Count - 1].Y.ToString());
                //    streamw.WriteLine();
                //}
                //streamw.Close();
                //break;
                vp.CreateVoronoiPolygonfrmSkeletonArcList();
            }
        }

        /// <summary>
        /// 根据ID和对象类型获取其对应的V图多边形
        /// </summary>
        /// <param name="id">对象ID</param>
        /// <param name="type">FeatureType</param>
        public VoronoiPolygon GetVoronoiPolygonbyTagIDandType(int id, FeatureType type)
        {
            foreach(VoronoiPolygon curVP in this.VorPolygonList)
            {
                if(curVP.MapObj.ID==id&&curVP.MapObj.FeatureType==type)
                    return curVP;
            }
            return null;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="PointList"></param>
        public VoronoiDiagram(List<TriNode> pointList)
        {
            PointList = pointList;
            VorPolygonList = new List<VoronoiPolygon>();
        }

        /// <summary>
        /// 创建Voronoi图
        /// </summary>
        public void CreateVoronoiDiagram()
        {
            //创建凸壳
            this.convexNull = new ConvexNull(this.PointList);
            convexNull.CreateConvexNull();
            //创建TIN
            this.tin = new DelaunayTin(this.PointList);
            tin.CreateDelaunayTin(AlgDelaunayType.Side_extent);
            foreach (TriNode point in this.PointList)
            {
                VoronoiPolygon vp = new VoronoiPolygon(point);
                //判断是否凸壳上的点
                int index = 0;
                if(convexNull.ContainPoint(point,out index))
                {
                    vp.IsPolygon = false;
                    int n=PointList.Count;
                    TriEdge edge1 = new TriEdge(point, PointList[(index - 1 + n) % n]);
                    TriEdge edge2 = new TriEdge(point, PointList[index + 1 % n]);

                    foreach (Triangle curTri in tin.TriangleList)
                    {

                        if (curTri.ContainEdge(edge1))
                        {
                            vp.PointSet.Add(edge1.EdgeMidPoint);
                        }
                        else if (curTri.ContainEdge(edge2))
                        {
                            vp.PointSet.Add(edge2.EdgeMidPoint);
                        }
                        else if (curTri.ContainPoint(point))
                        {
                            vp.PointSet.Add(curTri.CircumCenter);
                        }
                    }
                }

                else
                {
                    foreach (Triangle curTri in tin.TriangleList)
                    {
                        if (curTri.ContainPoint(point))
                        {
                            vp.PointSet.Add(curTri.CircumCenter);
                        }
                    }
                }
                vp.CreateVoronoiPolygon();
                if (vp.IsPolygon)
                {
                    this.VorPolygonList.Add(vp);
                }
            }
        }


        /// <summary>
        /// 将面写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">线列表</param>
        public void Create_WritePolygonObject2Shp(string filePath, string fileName, ISpatialReference pSpatialReference)
        {
            #region 创建一个线的shape文件
            string Folderpathstr = filePath;
            string LyrName = fileName;
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
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
            pGeomDefEdit.SpatialReference_2 = pSpatialReference;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            //属性字段1
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);

            //IField pField2;
            //IFieldEdit pFieldEdit2;
            //pField2 = new FieldClass();
            //pFieldEdit2 = pField2 as IFieldEdit;
            //pFieldEdit2.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            //pFieldEdit2.Name_2 = "Level";
            //pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeInteger;
            //pFieldsEdit.AddField(pField2);

            //IField pField2;
            //IFieldEdit pFieldEdit2;
            //pField2 = new FieldClass();
            //pFieldEdit2 = pField1 as IFieldEdit;
            //pFieldEdit2.Length_2 = 30;//对象类型
            //pFieldEdit2.Name_2 = "Type";
            //pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeString;
            //pFieldsEdit.AddField(pField2);
            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 向线层添加线要素

            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            IWorkspaceEdit pIWorkspaceEdit = null;
            IDataset pIDataset = (IDataset)pFeatClass;

            if (pIDataset != null)
            {
                pIWorkspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            }
            //try
            //{
                if (pFeatClass == null)
                    return;
                //获取顶点图层的数据集，并创建工作空间
                IDataset dataset = (IDataset)pFeatClass;
                IWorkspace workspace = dataset.Workspace;
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
                IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;
                //注意：此时，所编辑数据不能被其他程序打开
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();

                int n = this.VorPolygonList.Count;
                // List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolygonClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    TriNode curPoint = null;
                    if (this.VorPolygonList[i] == null)
                        continue;
                    if (this.VorPolygonList[i].PointSet != null)
                    {
                        int m = this.VorPolygonList[i].PointSet.Count;

                        for (int k = 0; k < m; k++)
                        {
                            curPoint = this.VorPolygonList[i].PointSet[k];
                            curResultPoint = new PointClass();
                            curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                            pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                        }
                        //curPoint = this.VorPolygonList[i].PointSet[0];
                        //curResultPoint = new PointClass();
                        //curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        //pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                        feature.Shape = shp;
                        feature.set_Value(2, this.VorPolygonList[i].MapObj.ID);//编号 
                        //  feature.set_Value(3, this.VorPolygonList[i].MapObj.FeatureType.ToString());//编号 

                        feature.Store();//保存IFeature对象  
                        fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上   
                    }
                }

                //关闭编辑
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("异常信息" + ex.Message);
            //}
            #endregion
        }

        /// <summary>
        /// 输出带level的V图(即生成的场)
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="pSpatialReference"></param>
        public void Create_WritePolygonObject2ShpwithLevels(string filePath, string fileName, ISpatialReference pSpatialReference,List<List<VoronoiPolygon>> FieldList)
        {
            #region 创建一个线的shape文件
            string Folderpathstr = filePath;
            string LyrName = fileName;
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
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
            pGeomDefEdit.SpatialReference_2 = pSpatialReference;
            pFieldEdit.GeometryDef_2 = pGeomDef;
            pFieldsEdit.AddField(pField);
            #endregion

            #region 创建属性字段
            //属性字段1
            IField pField1;
            IFieldEdit pFieldEdit1;
            pField1 = new FieldClass();
            pFieldEdit1 = pField1 as IFieldEdit;
            pFieldEdit1.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit1.Name_2 = "ID";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField1);

            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit2.Name_2 = "Level";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField2);

            //IField pField2;
            //IFieldEdit pFieldEdit2;
            //pField2 = new FieldClass();
            //pFieldEdit2 = pField1 as IFieldEdit;
            //pFieldEdit2.Length_2 = 30;//对象类型
            //pFieldEdit2.Name_2 = "Type";
            //pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeString;
            //pFieldsEdit.AddField(pField2);
            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 向线层添加线要素

            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            IWorkspaceEdit pIWorkspaceEdit = null;
            IDataset pIDataset = (IDataset)pFeatClass;

            if (pIDataset != null)
            {
                pIWorkspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            }
            //try
            //{
            if (pFeatClass == null)
                return;
            //获取顶点图层的数据集，并创建工作空间
            IDataset dataset = (IDataset)pFeatClass;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            //定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
            IFeatureClassWrite fr = (IFeatureClassWrite)pFeatClass;
            //注意：此时，所编辑数据不能被其他程序打开
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            int n = FieldList.Count;
            // List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
            if (n == 0)
                return;

            for (int j = 0; j < n; j++)
            {
                List<VoronoiPolygon> Lvp = FieldList[j];
                if (Lvp.Count > 0)
                {
                    for (int i = 0; i < Lvp.Count; i++)
                    {
                        IFeature feature = pFeatClass.CreateFeature();
                        IGeometry shp = new PolygonClass();
                        // shp.SpatialReference = mapControl.SpatialReference;
                        IPointCollection pointSet = shp as IPointCollection;
                        IPoint curResultPoint = null;
                        TriNode curPoint = null;
                        if (Lvp[i] == null)
                            continue;
                        if (Lvp[i].PointSet != null)
                        {
                            int m = Lvp[i].PointSet.Count;

                            for (int k = 0; k < m; k++)
                            {
                                curPoint = Lvp[i].PointSet[k];
                                curResultPoint = new PointClass();
                                curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                                pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                            }
                            //curPoint = this.VorPolygonList[i].PointSet[0];
                            //curResultPoint = new PointClass();
                            //curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                            //pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                            feature.Shape = shp;
                            feature.set_Value(2, Lvp[i].MapObj.ID);//编号 
                            feature.set_Value(3, j);//level
                            //  feature.set_Value(3, this.VorPolygonList[i].MapObj.FeatureType.ToString());//编号 

                            feature.Store();//保存IFeature对象  
                            fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上   
                        }
                    }
                }
            }

            //关闭编辑
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("异常信息" + ex.Message);
            //}
            #endregion
        }


        /// <summary>
        ///密度计算
        /// </summary>
        /// <param name="pg"></param>
        /// <param name="strPath"></param>
        /// <param name="iterID"></param>
        public void CalandOutputDensity(ProxiGraph pg,string strPath,int iterID)
        {
            if (this.VorPolygonList == null || VorPolygonList.Count == 0)
                return;
            DataSet ds = new DataSet();
            //创建一个表
            DataTable tableforce = new DataTable();
            tableforce.TableName = "Desity" + iterID.ToString();
            tableforce.Columns.Add("ID", typeof(int));
            tableforce.Columns.Add("Density", typeof(double));
            foreach (ProxiNode curNode in pg.NodeList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                VoronoiPolygon vp = null;
                PolygonObject po=null;
                if (fType == FeatureType.PolygonType)
                {

                    po = this.Map.GetObjectbyID(tagID, fType) as PolygonObject;
                    vp = this.GetVPbyIDandType(tagID, fType);
                    DataRow dr = tableforce.NewRow();
                    dr[0] = tagID;
                    dr[1] = po.Area / vp.Area;
                    tableforce.Rows.Add(dr);
                }
            }
            TXTHelper.ExportToTxt(tableforce, strPath + @"\Density" + iterID.ToString() + @".txt");
        }

        /// <summary>
        /// 基于道路与建筑物冲突建立移位场(给定邻近图) 存储在 List<List<VoronoiPolygon>>中
        /// </summary>
        /// PeList 给定邻近图的边集
        public void FieldBuildBasedonRoads(List<ProxiEdge> PeList)
        {
            FielderListforRoads = new List<List<VoronoiPolygon>>();

            //对v图中的每个元素进行判断；因此，在建立场之前，首先需要建立V图
            #region 建立一个V图的备份
            List<VoronoiPolygon> VGraphCopy=new List<VoronoiPolygon>();
            foreach(VoronoiPolygon Vp in VorPolygonList)
            {
                VGraphCopy.Add(Vp);
            }
            #endregion

            #region 找到第一层
            List<VoronoiPolygon> FirstLevel = new List<VoronoiPolygon>();
            for (int i = 0; i < VGraphCopy.Count;i++ )
            {
                VoronoiPolygon Vp = VGraphCopy[i];
                TriNode tPoint = Vp.Point;
                List<ProxiEdge> EdgeList = PeList;
                foreach (ProxiEdge Pe in EdgeList)
                {
                    ProxiNode Pn1 = Pe.Node1;
                    ProxiNode Pn2 = Pe.Node2;

                    if (tPoint.X == Pn1.X && tPoint.Y == Pn1.Y)
                    {
                        if (Pn2.FeatureType == FeatureType.PolylineType)
                        {
                            FirstLevel.Add(Vp);
                            VGraphCopy.Remove(Vp);
                            i--;
                            break;
                        }
                    }

                    if (tPoint.X == Pn2.X && tPoint.Y == Pn2.Y)
                    {
                        if (Pn1.FeatureType == FeatureType.PolylineType)
                        {
                            FirstLevel.Add(Vp);
                            VGraphCopy.Remove(Vp);
                            i--;
                            break;
                        }
                    }
                }
            }
            FielderListforRoads.Add(FirstLevel);
            #endregion

            #region 递归判断
            do
            {
                List<VoronoiPolygon> NextLevel = new List<VoronoiPolygon>();
                for (int i = 0; i < VGraphCopy.Count;i++ )
                {
                    bool Label = false;
                    VoronoiPolygon Vp = VGraphCopy[i];
                    TriNode tPoint = Vp.Point;
                    List<ProxiEdge> EdgeList = PeList;
                    foreach (ProxiEdge Pe in EdgeList)
                    {
                        ProxiNode Pn1 = Pe.Node1;
                        ProxiNode Pn2 = Pe.Node2;

                        if (tPoint.X == Pn1.X && tPoint.Y == Pn1.Y)
                        {
                            foreach (List<VoronoiPolygon> Lvp in FielderListforRoads)
                            {
                                foreach (VoronoiPolygon Vpa in Lvp)
                                {
                                    TriNode tPointa = Vpa.Point;
                                    if (Pn2.X == tPointa.X && Pn2.Y == tPointa.Y)
                                    {
                                        NextLevel.Add(Vp);
                                        VGraphCopy.Remove(Vp);
                                        i--;
                                        Label = true;
                                        break;
                                    }
                                }

                                if (Label)
                                {
                                    break;
                                }
                            }
                        }

                        if (tPoint.X == Pn2.X && tPoint.Y == Pn2.Y)
                        {
                            foreach (List<VoronoiPolygon> Lvp in FielderListforRoads)
                            {
                                foreach (VoronoiPolygon Vpa in Lvp)
                                {
                                    TriNode tPointa = Vpa.Point;
                                    if (Pn1.X == tPointa.X && Pn1.Y == tPointa.Y)
                                    {
                                        NextLevel.Add(Vp);
                                        VGraphCopy.Remove(Vp);
                                        i--;
                                        Label = true;
                                        break;
                                    }
                                }

                                if (Label)
                                {
                                    break;
                                }
                            }
                        }

                        if (Label)
                        {
                            break;
                        }
                    }
                }

                FielderListforRoads.Add(NextLevel);
            } while (VGraphCopy.Count > 0);
            #endregion
        }

        /// <summary>
        /// 基于道路与建筑物冲突建立移位场(给定邻近图) 存储在 List<List<polygonobject>>中
        /// </summary>
        /// PeList 给定邻近图的边集
        public void FieldBuildBasedonRoads2(List<ProxiEdge> PeList,List<PolygonObject> PolygonObjectList)
        {
            FielderListforRoads1 = new List<List<PolygonObject>>();

            //对v图中的每个元素进行判断；因此，在建立场之前，首先需要建立V图
            #region 建立一个V图的备份
            List<PolygonObject> VGraphCopy = new List<PolygonObject>();
            foreach (PolygonObject Vp in PolygonObjectList)
            {
                VGraphCopy.Add(Vp);
            }
            #endregion

            #region 找到第一层
            List<PolygonObject> FirstLevel = new List<PolygonObject>();
            for (int i = 0; i < VGraphCopy.Count; i++)
            {
                PolygonObject Vp = VGraphCopy[i];
                ProxiNode tPoint = Vp.CalProxiNode();
                List<ProxiEdge> EdgeList = PeList;
                foreach (ProxiEdge Pe in EdgeList)
                {
                    ProxiNode Pn1 = Pe.Node1;
                    ProxiNode Pn2 = Pe.Node2;

                    if (tPoint.X == Pn1.X && tPoint.Y == Pn1.Y)
                    {
                        if (Pn2.FeatureType == FeatureType.PolylineType)
                        {
                            FirstLevel.Add(Vp);
                            VGraphCopy.Remove(Vp);
                            i--;
                            break;
                        }
                    }

                    if (tPoint.X == Pn2.X && tPoint.Y == Pn2.Y)
                    {
                        if (Pn1.FeatureType == FeatureType.PolylineType)
                        {
                            FirstLevel.Add(Vp);
                            VGraphCopy.Remove(Vp);
                            i--;
                            break;
                        }
                    }
                }
            }
            FielderListforRoads1.Add(FirstLevel);
            #endregion

            #region 递归判断
            do
            {
                List<PolygonObject> NextLevel = new List<PolygonObject>();
                for (int i = 0; i < VGraphCopy.Count; i++)
                {
                    bool Label = false;
                    PolygonObject Vp = VGraphCopy[i];
                    ProxiNode tPoint = Vp.CalProxiNode();
                    List<ProxiEdge> EdgeList = PeList;
                    foreach (ProxiEdge Pe in EdgeList)
                    {
                        ProxiNode Pn1 = Pe.Node1;
                        ProxiNode Pn2 = Pe.Node2;

                        if (tPoint.X == Pn1.X && tPoint.Y == Pn1.Y)
                        {
                            foreach (List<PolygonObject> Lvp in FielderListforRoads1)
                            {
                                foreach (PolygonObject Vpa in Lvp)
                                {
                                    ProxiNode tPointa = Vpa.CalProxiNode();
                                    if (Pn2.X == tPointa.X && Pn2.Y == tPointa.Y)
                                    {
                                        NextLevel.Add(Vp);
                                        VGraphCopy.Remove(Vp);
                                        i--;
                                        Label = true;
                                        break;
                                    }
                                }

                                if (Label)
                                {
                                    break;
                                }
                            }
                        }

                        if (tPoint.X == Pn2.X && tPoint.Y == Pn2.Y)
                        {
                            foreach (List<PolygonObject> Lvp in FielderListforRoads1)
                            {
                                foreach (PolygonObject Vpa in Lvp)
                                {
                                    ProxiNode tPointa = Vpa.CalProxiNode();
                                    if (Pn1.X == tPointa.X && Pn1.Y == tPointa.Y)
                                    {
                                        NextLevel.Add(Vp);
                                        VGraphCopy.Remove(Vp);
                                        i--;
                                        Label = true;
                                        break;
                                    }
                                }

                                if (Label)
                                {
                                    break;
                                }
                            }
                        }

                        if (Label)
                        {
                            break;
                        }
                    }
                }

                FielderListforRoads1.Add(NextLevel);
            } while (VGraphCopy.Count > 0);
            #endregion
        }

        /// <summary>
        /// 基于道路建立pattern与道路的层次邻近关系
        /// </summary>
        /// <param name="PeList"></param>
        /// <param name="PatternPolygon"></param>
        public void PatternFieldBuildBasedonRoad(List<ProxiEdge> PeList,List<List<List<PolygonObject>>> PatternClusterPolygon)
        {
            PatternFieldListforRoads = new List<List<List<List<PolygonObject>>>>();//存储PatternPolygon

            #region 建立一个PatternCluster的备份
            List<List<List<PolygonObject>>> PatternClusterCopy = new List<List<List<PolygonObject>>>();
            foreach (List<List<PolygonObject>> PatternCluster in PatternClusterPolygon)
            {
                PatternClusterCopy.Add(PatternCluster);
            }
            #endregion

            #region 找到第一层
            List<List<List<PolygonObject>>> FirstLevel = new List<List<List<PolygonObject>>>();
            for (int i = 0; i < PatternClusterCopy.Count; i++)
            {
                List<List<PolygonObject>> PatternCluster = PatternClusterCopy[i];
                bool Label = false;

                for(int j=0;j<PatternCluster.Count;j++)
                {
                    for(int m=0;m<PatternCluster[j].Count;m++)
                    {
                        ProxiNode tPoint = PatternCluster[j][m].CalProxiNode();

                        #region 判断邻近边
                        foreach (ProxiEdge Pe in PeList)
                        {
                            ProxiNode Pn1 = Pe.Node1;
                            ProxiNode Pn2 = Pe.Node2;

                            if (tPoint.X == Pn1.X && tPoint.Y == Pn1.Y)
                            {
                                if (Pn2.FeatureType == FeatureType.PolylineType)
                                {
                                    FirstLevel.Add(PatternCluster);
                                    PatternClusterCopy.Remove(PatternCluster);
                                    i--;
                                    Label = true;
                                    break;
                                }
                            }


                            if (tPoint.X == Pn2.X && tPoint.Y == Pn2.Y)
                            {
                                if (Pn1.FeatureType == FeatureType.PolylineType)
                                {
                                    FirstLevel.Add(PatternCluster);
                                    PatternClusterCopy.Remove(PatternCluster);
                                    i--;
                                    Label = true;
                                    break;
                                }
                            }
                        }
                        #endregion

                        if (Label)
                        {
                            break;
                        }
                    }

                    if (Label)
                    {
                        break;
                    }
                }
               
            }
            PatternFieldListforRoads.Add(FirstLevel);
            #endregion

            #region 递归判断
            do
            {
                List<List<List<PolygonObject>>> NextLevel = new List<List<List<PolygonObject>>>();

                for (int i = 0; i < PatternClusterCopy.Count; i++)
                {
                    bool Label = false;
                    List<List<PolygonObject>> PatternCluster = PatternClusterCopy[i];

                    for (int j = 0; j < PatternCluster.Count; j++)
                    {
                        for (int m = 0; m < PatternCluster[j].Count; m++)
                        {
                            ProxiNode tPoint = PatternCluster[j][m].CalProxiNode();

                            #region 判断邻近边
                            foreach (ProxiEdge Pe in PeList)
                            {
                                ProxiNode Pn1 = Pe.Node1;
                                ProxiNode Pn2 = Pe.Node2;

                                if (tPoint.X == Pn1.X && tPoint.Y == Pn1.Y)
                                {
                                    #region for4
                                    foreach (List<List<List<PolygonObject>>> Lvp in PatternFieldListforRoads)
                                    {
                                        #region for3
                                        foreach (List<List<PolygonObject>> Vpa in Lvp)
                                        {
                                            #region for2
                                            for (int n = 0; n < Vpa.Count; n++)
                                            {
                                                #region for1
                                                for (int k = 0; k < Vpa[n].Count; k++)
                                                {
                                                    ProxiNode tPointa = Vpa[n][k].CalProxiNode();
                                                    if (Pn2.X == tPointa.X && Pn2.Y == tPointa.Y)
                                                    {
                                                        NextLevel.Add(PatternCluster);
                                                        PatternClusterCopy.Remove(PatternCluster);
                                                        i--;
                                                        Label = true;
                                                        break;
                                                    }
                                                }

                                                if (Label)
                                                {
                                                    break;
                                                }
                                                #endregion
                                            }
                                            #endregion

                                            if (Label)
                                            {
                                                break;
                                            }
                                        }
                                        #endregion

                                        if (Label)
                                        {
                                            break;
                                        }
                                    }
                                    #endregion
                                }

                                if (tPoint.X == Pn2.X && tPoint.Y == Pn2.Y)
                                {
                                    #region for4
                                    foreach (List<List<List<PolygonObject>>> Lvp in PatternFieldListforRoads)
                                    {
                                        #region for3
                                        foreach (List<List<PolygonObject>> Vpa in Lvp)
                                        {
                                            #region for2
                                            for (int n = 0; n < Vpa.Count; n++)
                                            {
                                                #region for1
                                                for (int k = 0; k < Vpa[n].Count; k++)
                                                {
                                                    ProxiNode tPointa = Vpa[n][k].CalProxiNode();
                                                    if (Pn1.X == tPointa.X && Pn1.Y == tPointa.Y)
                                                    {
                                                        NextLevel.Add(PatternCluster);
                                                        PatternClusterCopy.Remove(PatternCluster);
                                                        i--;
                                                        Label = true;
                                                        break;
                                                    }
                                                }

                                                if (Label)
                                                {
                                                    break;
                                                }
                                                #endregion
                                            }
                                            #endregion

                                            if (Label)
                                            {
                                                break;
                                            }
                                        }
                                        #endregion

                                        if (Label)
                                        {
                                            break;
                                        }
                                    }
                                    #endregion
                                }

                                if (Label)
                                {
                                    break;
                                }
                            }
                            #endregion

                            if (Label)
                            {
                                break;
                            }
                        }

                        if (Label)
                        {
                            break;
                        }
                    }
                }

            } while (PatternClusterCopy.Count > 0);
            #endregion
        }

        /// <summary>
        /// 基于某两个给定建筑物建立移位场 存储在 List<List<VoronoiPolygon>>中
        /// </summary>
        /// <param name="ID"></param> 建筑物ID
        /// PeList 建筑物邻近图
        public void FieldBuildBasedonBuildings(int ID1,int ID2,List<ProxiEdge> PeList)
        {
            FielderListforBuildings = new List<List<VoronoiPolygon>>();

            //对v图中的每个元素进行判断；因此，在建立场之前，首先需要建立V图
            #region 建立一个V图的备份
            List<VoronoiPolygon> VGraphCopy = new List<VoronoiPolygon>();
            foreach (VoronoiPolygon Vp in VorPolygonList)
            {
                VGraphCopy.Add(Vp);
            }
            #endregion

            #region 递归判断
            List<VoronoiPolygon> FirstLevel=new List<VoronoiPolygon>();
            VoronoiPolygon Vp1=this.GetVoronoiPolygonbyTagIDandType(ID1,FeatureType.PolygonType);
            VoronoiPolygon Vp2=this.GetVoronoiPolygonbyTagIDandType(ID2,FeatureType.PolygonType);
            VGraphCopy.Remove(Vp1);
            VGraphCopy.Remove(Vp2);
            FirstLevel.Add(Vp1); FirstLevel.Add(Vp2);
            FielderListforBuildings.Add(FirstLevel);

            do
            {
                List<VoronoiPolygon> NextLevel = new List<VoronoiPolygon>();
                for (int i = 0; i < VGraphCopy.Count; i++)
                {
                    bool Label = false;
                    VoronoiPolygon Vp = VGraphCopy[i];
                    TriNode tPoint = Vp.Point;
                    List<ProxiEdge> EdgeList = PeList;
                    foreach (ProxiEdge Pe in EdgeList)
                    {
                        ProxiNode Pn1 = Pe.Node1;
                        ProxiNode Pn2 = Pe.Node2;

                        if (tPoint.X == Pn1.X && tPoint.Y == Pn1.Y)
                        {
                            foreach (List<VoronoiPolygon> Lvp in FielderListforBuildings)
                            {
                                foreach (VoronoiPolygon Vpa in Lvp)
                                {
                                    TriNode tPointa = Vpa.Point;
                                    if (Pn2.X == tPointa.X && Pn2.Y == tPointa.Y)
                                    {
                                        NextLevel.Add(Vp);
                                        VGraphCopy.Remove(Vp);
                                        i--;
                                        Label = true;
                                        break;
                                    }
                                }

                                if (Label)
                                {
                                    break;
                                }
                            }
                        }

                        if (tPoint.X == Pn2.X && tPoint.Y == Pn2.Y)
                        {
                            foreach (List<VoronoiPolygon> Lvp in FielderListforBuildings)
                            {
                                foreach (VoronoiPolygon Vpa in Lvp)
                                {
                                    TriNode tPointa = Vpa.Point;
                                    if (Pn1.X == tPointa.X && Pn1.Y == tPointa.Y)
                                    {
                                        NextLevel.Add(Vp);
                                        VGraphCopy.Remove(Vp);
                                        i--;
                                        Label = true;
                                        break;
                                    }
                                }

                                if (Label)
                                {
                                    break;
                                }
                            }
                        }

                        if (Label)
                        {
                            break;
                        }
                    }
                }

                FielderListforBuildings.Add(NextLevel);
            } while (VGraphCopy.Count > 0);
            #endregion

        }

        /// <summary>
        /// 基于某两个给定建筑物建立移位场 存储在 存储在 List<List<polygonobject>>中
        /// </summary>
        /// <param name="ID"></param> 建筑物ID
        /// PeList 建筑物邻近图
        public void FieldBuildBasedonBuildings2(int ID1, int ID2, List<ProxiEdge> PeList,List<PolygonObject> PolygonObjectList)
        {
            FielderListforBuildings1 = new List<List<PolygonObject>>();

            //对v图中的每个元素进行判断；因此，在建立场之前，首先需要建立V图
            #region 建立一个V图的备份
            List<PolygonObject> VGraphCopy = new List<PolygonObject>();
            foreach (PolygonObject Vp in PolygonObjectList)
            {
                VGraphCopy.Add(Vp);
            }
            #endregion

            #region 递归判断
            List<PolygonObject> FirstLevel = new List<PolygonObject>();
            for(int i=0;i<PolygonObjectList.Count;i++)
            {
                PolygonObject Po=PolygonObjectList[i];
                if(Po.ID==ID1||Po.ID==ID2)
                {          
                   VGraphCopy.Remove(Po);
                   FirstLevel.Add(Po);              
                }        
            }    
            FielderListforBuildings1.Add(FirstLevel);

            List<PolygonObject> NextLevel = null;
            do
            {
                NextLevel = new List<PolygonObject>();
                for (int i = 0; i < VGraphCopy.Count; i++)
                {
                    bool Label = false;
                    PolygonObject Vp = VGraphCopy[i];
                    ProxiNode tPoint = Vp.CalProxiNode();
                    foreach (ProxiEdge Pe in PeList)
                    {
                        ProxiNode Pn1 = Pe.Node1;
                        ProxiNode Pn2 = Pe.Node2;

                        if (tPoint.X == Pn1.X && tPoint.Y == Pn1.Y)
                        {
                            foreach (List<PolygonObject> Lvp in FielderListforBuildings1)
                            {
                                foreach (PolygonObject Vpa in Lvp)
                                {
                                    ProxiNode tPointa = Vpa.CalProxiNode();
                                    if (Pn2.X == tPointa.X && Pn2.Y == tPointa.Y)
                                    {
                                        NextLevel.Add(Vp);
                                        VGraphCopy.Remove(Vp);
                                        i--;
                                        Label = true;
                                        break;
                                    }
                                }

                                if (Label)
                                {
                                    break;
                                }
                            }
                        }

                        if (tPoint.X == Pn2.X && tPoint.Y == Pn2.Y)
                        {
                            foreach (List<PolygonObject> Lvp in FielderListforBuildings1)
                            {
                                foreach (PolygonObject Vpa in Lvp)
                                {
                                    ProxiNode tPointa = Vpa.CalProxiNode();
                                    if (Pn1.X == tPointa.X && Pn1.Y == tPointa.Y)
                                    {
                                        NextLevel.Add(Vp);
                                        VGraphCopy.Remove(Vp);
                                        i--;
                                        Label = true;
                                        break;
                                    }
                                }

                                if (Label)
                                {
                                    break;
                                }
                            }
                        }

                        if (Label)
                        {
                            break;
                        }
                    }
                }

                FielderListforBuildings1.Add(NextLevel);
            } while (VGraphCopy.Count > 0 && NextLevel.Count>0);//若当前图层没建筑物，或有一层建筑物为0
            #endregion

        }

        /// <summary>
        /// 基于某个给定的建筑物建立移位场
        /// </summary>
        /// <param name="ID1"></param>
        /// <param name="PeList"></param>
        public void FieldBuildBasedonBuilding(int ID1, List<ProxiEdge> PeList)
        {
            FielderListforBuilding = new List<List<VoronoiPolygon>>();

            //对v图中的每个元素进行判断；因此，在建立场之前，首先需要建立V图
            #region 建立一个V图的备份
            List<VoronoiPolygon> VGraphCopy = new List<VoronoiPolygon>();
            foreach (VoronoiPolygon Vp in VorPolygonList)
            {
                VGraphCopy.Add(Vp);
            }
            #endregion

            #region 递归判断
            List<VoronoiPolygon> FirstLevel = new List<VoronoiPolygon>();
            VoronoiPolygon Vp1 = this.GetVoronoiPolygonbyTagIDandType(ID1, FeatureType.PolygonType);
            VGraphCopy.Remove(Vp1);
            FirstLevel.Add(Vp1); 
            FielderListforBuilding.Add(FirstLevel);

            do
            {
                List<VoronoiPolygon> NextLevel = new List<VoronoiPolygon>();
                for (int i = 0; i < VGraphCopy.Count; i++)
                {
                    bool Label = false;
                    VoronoiPolygon Vp = VGraphCopy[i];
                    TriNode tPoint = Vp.Point;
                    List<ProxiEdge> EdgeList = PeList;
                    foreach (ProxiEdge Pe in EdgeList)
                    {
                        ProxiNode Pn1 = Pe.Node1;
                        ProxiNode Pn2 = Pe.Node2;

                        if (tPoint.X == Pn1.X && tPoint.Y == Pn1.Y)
                        {
                            foreach (List<VoronoiPolygon> Lvp in FielderListforBuilding)
                            {
                                foreach (VoronoiPolygon Vpa in Lvp)
                                {
                                    TriNode tPointa = Vpa.Point;
                                    if (Pn2.X == tPointa.X && Pn2.Y == tPointa.Y)
                                    {
                                        NextLevel.Add(Vp);
                                        VGraphCopy.Remove(Vp);
                                        i--;
                                        Label = true;
                                        break;
                                    }
                                }

                                if (Label)
                                {
                                    break;
                                }
                            }
                        }

                        if (tPoint.X == Pn2.X && tPoint.Y == Pn2.Y)
                        {
                            foreach (List<VoronoiPolygon> Lvp in FielderListforBuilding)
                            {
                                foreach (VoronoiPolygon Vpa in Lvp)
                                {
                                    TriNode tPointa = Vpa.Point;
                                    if (Pn1.X == tPointa.X && Pn1.Y == tPointa.Y)
                                    {
                                        NextLevel.Add(Vp);
                                        VGraphCopy.Remove(Vp);
                                        i--;
                                        Label = true;
                                        break;
                                    }
                                }

                                if (Label)
                                {
                                    break;
                                }
                            }
                        }

                        if (Label)
                        {
                            break;
                        }
                    }
                }

                FielderListforBuilding.Add(NextLevel);
            } while (VGraphCopy.Count > 0);
            #endregion
        }
    }
}
