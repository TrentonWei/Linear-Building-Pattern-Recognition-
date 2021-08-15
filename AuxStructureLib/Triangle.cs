using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Windows.Forms;

namespace AuxStructureLib
{
    /// <summary>
    /// 三角形
    /// </summary>
    public class Triangle
    {
        public int ID;
        public TriNode point1, point2, point3;
        public TriEdge edge1, edge2, edge3;
        public List<Triangle> CommonEdgeTriangleList = new List<Triangle>();//记录一个三角形邻接的建筑物列表

        public double W = -1;
     //  public int 

        //三角形类型-1,0,1,2,3
        private int type = -1;//师兄原始的三角形Type定义，不管
        public int TriangleConnectType = 0;//定义三角形的邻近三角形个数 1代表一个邻近三角形；2代表两个邻近三角形；3代表三个邻近三角形
        public int PolygonConnectType = 0;//定义三角形连接的建筑物个数 1代表连接一个建筑物；2代表连接两个建筑物；3代表连接三个建筑物
        public int InOutType=0;//定义三角形是在建筑物内还是在建筑物外;0表示建筑物外部；1建筑物内部
        public int DentType = 0;//0表示非凹部，1表示凹部
        public int TriangleConnectTypeConsiderInOut = 0;//顾及建筑物内部与外部的三角形连接的三角形个数
        public int OutBoundary = 0;//0表示不是建筑物外部的边缘三角形；1表示是建筑物外部的边缘三角形
        public int ConnectConvexEdge = 1;//0表示三角形的三个点都在建筑物中同一个侵入或侵出上；1表示三角形的三个点不是都在建筑物同一个侵入或侵出上
        //备注：边缘三角形：1、考虑三角形内外的三角形的连接建筑物是0或是1；2、若其连接的建筑物是1，
        //且其不是外边缘三角形，则是一个Corner三角形；2、若其连接建筑物为0，且其实一个边缘三角形，则
        //其是一个边缘三角形

        public TriNode rV = null;
        public TriEdge rE = null;
        public bool isLeft = true;//顶点在左边

        /// <summary>
        /// 三角形类型0,1,2,3
        /// </summary>
        public int TriType
        {
            get
            {
                if (type == -1)
                {
                    type = 0;
                    if (this.edge1.tagID != -1)
                        type++;
                    if (this.edge2.tagID != -1)
                        type++;
                    if (this.edge3.tagID != -1)
                        type++;
                    return type;
                }
                else
                    return type;
            }
        }

        /// <summary>
        /// 复制三角形列表
        /// </summary>
        /// <param name="triangles"></param>
        /// <returns></returns>
        public static List<Triangle> EliDuplicateTris(List<Triangle> triangles)
        {
            List<Triangle> AmendedTris = new List<Triangle>();

            for (int i = 0; i < triangles.Count; i++)
            {
                if (AmendedTris.Count == 0)
                    AmendedTris.Add(triangles[i]);
                else
                {
                    if (!triangles[i].isContainedINTris(AmendedTris))
                        AmendedTris.Add(triangles[i]);
                }
            }
            return AmendedTris;
        }

        //private TriPoint circumCenter;
        /// <summary>
        /// 构造函数
        /// </summary>
        public Triangle()
        {

        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        public Triangle(TriNode p1, TriNode p2, TriNode p3)
        {
            this.point1 = p1;
            this.point2 = p2;
            this.point3 = p3;
        }

        public TriNode CircumCenter
        {
            get
            {
                TriNode point = new TriNode();
                double x1 = point1.X;
                double y1 = point1.Y;
                double x2 = point2.X;
                double y2 = point2.Y;
                double x3 = point3.X;
                double y3 = point3.Y;
                point.X = ((y2 - y1) * (y3 * y3 - y1 * y1 + x3 * x3 - x1 * x1) - (y3 - y1) * (y2 * y2 - y1 * y1 + x2 * x2 - x1 * x1)) / (2 * (x3 - x1) * (y2 - y1) - 2 * ((x2 - x1) * (y3 - y1)));
                point.Y = ((x2 - x1) * (x3 * x3 - x1 * x1 + y3 * y3 - y1 * y1) - (x3 - x1) * (x2 * x2 - x1 * x1 + y2 * y2 - y1 * y1)) / (2 * (y3 - y1) * (x2 - x1) - 2 * ((y2 - y1) * (x3 - x1)));
                return point;
            }
            set
            {

            }
        }

        /// <summary>
        /// 计算三角形中心
        /// </summary>
        /// <param name="triangle"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void CalCenter(Triangle triangle, out double x, out double y)
        {

            double x1 = triangle.point1.X;
            double y1 = triangle.point1.Y;
            double x2 = triangle.point2.X;
            double y2 = triangle.point2.Y;
            double x3 = triangle.point3.X;
            double y3 = triangle.point3.Y;
            x = ((y2 - y1) * (y3 * y3 - y1 * y1 + x3 * x3 - x1 * x1) - (y3 - y1) * (y2 * y2 - y1 * y1 + x2 * x2 - x1 * x1)) / (2 * (x3 - x1) * (y2 - y1) - 2 * ((x2 - x1) * (y3 - y1)));
            y = ((x2 - x1) * (x3 * x3 - x1 * x1 + y3 * y3 - y1 * y1) - (x3 - x1) * (x2 * x2 - x1 * x1 + y2 * y2 - y1 * y1)) / (2 * (y3 - y1) * (x2 - x1) - 2 * ((y2 - y1) * (x3 - x1)));
        }
        /// <summary>
        /// 点列表
        /// </summary>
        public List<TriNode> points
        {
            get
            {
                List<TriNode> points = new List<TriNode>();
                points.Add(point1);
                points.Add(point2);
                points.Add(point3);
                return points;
            }
        }
        /// <summary>
        /// 是否包含顶点P
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool ContainPoint(TriNode p)
        {
            if (point1.ID == p.ID || point2.ID == p.ID || point3.ID == p.ID)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 是否包含顶点P
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool IsContainPoint(TriNode p)
        {
            if (point1.ID == p.ID || point2.ID == p.ID || point3.ID == p.ID)
                return true;
            else
                return false;
        }

        public static Triangle FindTriangbyEdge(List<Triangle> triList,TriEdge edge)
        {
            foreach (Triangle tri in triList)
            {
                if (tri.edge1.ID == edge.ID||tri.edge2.ID == edge.ID|| tri.edge3.ID == edge.ID)
                {
                    return tri;
                }
           
            }
            return null;
        }

        public static Triangle FindTriangbyNodes(List<Triangle> triList, TriNode p1,TriNode p2,out TriNode p)
        {
            if (p1.ID ==43 && p2.ID==246)
            {
                int error=0;
            }
            p= null;
            foreach (Triangle tri in triList)
            {
                if ((tri.point1.ID == p1.ID && tri.point2.ID == p2.ID)||(tri.point2.ID == p1.ID && tri.point1.ID == p2.ID))
                {
                    p = tri.point3;
                    return tri;
                }
                if ((tri.point2.ID == p1.ID && tri.point3.ID == p2.ID) || (tri.point3.ID == p1.ID && tri.point2.ID == p2.ID))
                {
                    p = tri.point1;
                    return tri;
                }
                if ((tri.point1.ID == p1.ID && tri.point3.ID == p2.ID) || (tri.point3.ID == p1.ID && tri.point1.ID == p2.ID))
                {
                    p = tri.point2;
                    return tri;
                }
            }
            return null;
        }

        public static TriEdge GetEdge(List<TriEdge> edges, Triangle angle)
        {
            foreach (TriEdge e in edges)
            {
                if (angle.edge1 == e)
                    return angle.edge1;
                if (angle.edge2 == e)
                    return angle.edge2;
                if (angle.edge3 == e)
                    return angle.edge3;
            }
            return null;
        }

        public static List<TriEdge> GetEdges(List<Triangle> triangles)
        {
            List<TriEdge> edges = new List<TriEdge>();
            foreach (Triangle angle in triangles)
            {
                edges.Add(angle.edge1);
                edges.Add(angle.edge2);
                edges.Add(angle.edge3);
            }
            if (edges.Count == 0)
                return null;
            return edges;
        }
        /// <summary>
        /// 是否包含某条边
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public bool ContainEdge(TriEdge edge)
        {
            if ((edge.startPoint.ID == point1.ID || edge.startPoint.ID == point2.ID || edge.startPoint.ID == point3.ID) && (edge.endPoint.ID == point1.ID || edge.endPoint.ID == point2.ID || edge.endPoint.ID == point3.ID))
            {
                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// 给三角形编号
        /// </summary>
        /// <param name="TriangleList"></param>
        public static void WriteID(List<Triangle> TriangleList)
        {
            if (TriangleList == null || TriangleList.Count == 0)
                return;
            int n = TriangleList.Count;
            for (int i = 0; i < n; i++)
            {
                TriangleList[i].ID = i;
            }
        }

        /// <summary>
        /// 将三角形写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">骨架线段列表</param>
        public static void Create_WriteTriange2Shp(string filePath, string fileName, List<Triangle> TriangleList, ISpatialReference pSpatialReference)
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
            //ID
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
            pFieldEdit2.Length_2 = 30;
            pFieldEdit2.Name_2 = "P1";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField2);

            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;
            pFieldEdit3.Name_2 = "P2";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField3);

            IField pField4;
            IFieldEdit pFieldEdit4;
            pField4 = new FieldClass();
            pFieldEdit4 = pField4 as IFieldEdit;
            pFieldEdit4.Length_2 = 30;
            pFieldEdit4.Name_2 = "P3";
            pFieldEdit4.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField4);

            IField pField5;
            IFieldEdit pFieldEdit5;
            pField5 = new FieldClass();
            pFieldEdit5 = pField5 as IFieldEdit;
            pFieldEdit5.Length_2 = 30;
            pFieldEdit5.Name_2 = "E1";
            pFieldEdit5.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField5);

            IField pField6;
            IFieldEdit pFieldEdit6;
            pField6 = new FieldClass();
            pFieldEdit6 = pField6 as IFieldEdit;
            pFieldEdit6.Length_2 = 30;
            pFieldEdit6.Name_2 = "E2";
            pFieldEdit6.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField6);

            IField pField7;
            IFieldEdit pFieldEdit7;
            pField7 = new FieldClass();
            pFieldEdit7 = pField7 as IFieldEdit;
            pFieldEdit7.Length_2 = 30;
            pFieldEdit7.Name_2 = "E3";
            pFieldEdit7.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField7);

            IField pField8;
            IFieldEdit pFieldEdit8;
            pField8 = new FieldClass();
            pFieldEdit8 = pField8 as IFieldEdit;
            pFieldEdit8.Length_2 = 30;
            pFieldEdit8.Name_2 = "Type";
            pFieldEdit8.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField8);

            IField pField9;
            IFieldEdit pFieldEdit9;
            pField9 = new FieldClass();
            pFieldEdit9 = pField9 as IFieldEdit;
            pFieldEdit9.Length_2 = 30;
            pFieldEdit9.Name_2 = "Width";
            pFieldEdit9.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField9);

            IField pField10;
            IFieldEdit pFieldEdit10;
            pField10 = new FieldClass();
            pFieldEdit10 = pField10 as IFieldEdit;
            pFieldEdit10.Length_2 = 30;
            pFieldEdit10.Name_2 = "IOType";
            pFieldEdit10.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField10);

            IField pField11;
            IFieldEdit pFieldEdit11;
            pField11 = new FieldClass();
            pFieldEdit11 = pField11 as IFieldEdit;
            pFieldEdit11.Length_2 = 30;
            pFieldEdit11.Name_2 = "PCType";
            pFieldEdit11.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField11);

            IField pField12;
            IFieldEdit pFieldEdit12;
            pField12 = new FieldClass();
            pFieldEdit12 = pField12 as IFieldEdit;
            pFieldEdit12.Length_2 = 30;
            pFieldEdit12.Name_2 = "TCType";
            pFieldEdit12.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField12);

            IField pField13;
            IFieldEdit pFieldEdit13;
            pField13 = new FieldClass();
            pFieldEdit13 = pField13 as IFieldEdit;
            pFieldEdit13.Length_2 = 30;
            pFieldEdit13.Name_2 = "DentType";
            pFieldEdit13.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField13);

            IField pField14;
            IFieldEdit pFieldEdit14;
            pField14 = new FieldClass();
            pFieldEdit14 = pField14 as IFieldEdit;
            pFieldEdit14.Length_2 = 30;
            pFieldEdit14.Name_2 = "TCTypeIO";
            pFieldEdit14.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField14);

            IField pField15;
            IFieldEdit pFieldEdit15;
            pField15 = new FieldClass();
            pFieldEdit15 = pField15 as IFieldEdit;
            pFieldEdit15.Length_2 = 30;
            pFieldEdit15.Name_2 = "BLable";
            pFieldEdit15.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField15);
            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 向线层添加线要素

            //IFeatureClass featureClass = null;
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            IWorkspaceEdit pIWorkspaceEdit = null;
            IDataset pIDataset = (IDataset)pFeatClass;

            if (pIDataset != null)
            {
                pIWorkspaceEdit = (IWorkspaceEdit)pIDataset.Workspace;
            }
            try
            {
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

                int n = TriangleList.Count;
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
                    if (TriangleList[i] == null)
                        continue;

                    curPoint = TriangleList[i].point1; ;
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    curPoint = TriangleList[i].point2;
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    curPoint = TriangleList[i].point3;
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    curPoint = TriangleList[i].point1;
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);

                    feature.Shape = shp;
                    feature.set_Value(2, TriangleList[i].ID);
                    feature.set_Value(3, TriangleList[i].point1.ID);
                    feature.set_Value(4, TriangleList[i].point2.ID);
                    feature.set_Value(5, TriangleList[i].point3.ID);
                    feature.set_Value(6, TriangleList[i].edge1.ID);
                    feature.set_Value(7, TriangleList[i].edge2.ID);
                    feature.set_Value(8, TriangleList[i].edge3.ID);
                    feature.set_Value(9, TriangleList[i].type);
                    feature.set_Value(10, TriangleList[i].W);
                    feature.set_Value(11, TriangleList[i].InOutType);
                    feature.set_Value(12, TriangleList[i].PolygonConnectType);
                    feature.set_Value(13, TriangleList[i].TriangleConnectType);
                    feature.set_Value(14, TriangleList[i].DentType);
                    feature.set_Value(15, TriangleList[i].TriangleConnectTypeConsiderInOut);
                    feature.set_Value(16, TriangleList[i].OutBoundary);
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
            #endregion
        }

   

        /// <summary>
        /// 是否单通道1类三角形
        /// </summary>
        /// <returns></returns>
        public bool IsSinglePathT1( )
        {
            int count = 0;
            Triangle nextTri = null;
            if (this.edge1.tagID == -1)
            {
                nextTri = this.edge1.rightTriangle;
                if (nextTri != null)
                    count++;
            }
            if (this.edge2.tagID == -1)
            {

                nextTri = this.edge2.rightTriangle;
                if (nextTri != null)
                    count++;
            }

            if (this.edge3.tagID == -1)
            {
                nextTri = this.edge3.rightTriangle;
                if (nextTri != null)
                    count++;
            }
            if (count == 1)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 是否无通道1类三角形
        /// </summary>
        /// <returns></returns>
        public bool IsNoPathT1()
        {
            int count = 0;
            Triangle nextTri = null;
            if (this.edge1.tagID == -1)
            {
                nextTri = this.edge1.rightTriangle;
                if (nextTri != null)
                    count++;
            }
            if (this.edge2.tagID == -1)
            {

                nextTri = this.edge2.rightTriangle;
                if (nextTri != null)
                    count++;
            }

            if (this.edge3.tagID == -1)
            {
                nextTri = this.edge3.rightTriangle;
                if (nextTri != null)
                    count++;
            }
            if (count == 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 获取其他的另外两条边
        /// </summary>
        /// <param name="tri">三角形</param>
        /// <param name="ve">边</param>
        /// <param name="edge1">1</param>
        /// <param name="edge2">2</param>
        public void GetAnother2EdgeofT0(TriEdge ve, out TriEdge edge1, out TriEdge edge2)
        {
            edge1 = null;
            edge2 = null;
            if (ve.doulEdge != null)
            {

                if (this.edge1.ID == ve.doulEdge.ID)
                {
                    edge1 = this.edge2;
                    edge2 = this.edge3;
                }
                else if (this.edge2.ID == ve.doulEdge.ID)
                {
                    edge1 = this.edge1;
                    edge2 = this.edge3;
                }

                else if (this.edge3.ID == ve.doulEdge.ID)
                {
                    edge1 = this.edge1;
                    edge2 = this.edge2;
                }
            }
        }

        /// <summary>
        /// 获取1类三角形的两条虚边和一条实际边
        /// </summary>
        /// <param name="vEdge1">第一条虚边</param>
        /// <param name="vEdge2">第二条虚边</param>
        ///  <param name="vVex">实边相对的顶点</param>
        public TriEdge GetVEdgeofT1(out TriEdge vEdge1, out TriEdge vEdge2, out TriNode vVex)
        {
            TriEdge re = null;
            vEdge1 = null;
            vEdge2 = null;
            vVex = null;
            if (this.edge1.tagID != -1)
            {
                vEdge1 = this.edge2;
                vEdge2 = this.edge3;
                re = this.edge1;

            }
            else if (this.edge2.tagID != -1)
            {
                vEdge1 = this.edge3;
                vEdge2 = this.edge1;
                re = this.edge2;

            }
            else if (this.edge3.tagID != -1)
            {
                vEdge1 = this.edge1;
                vEdge2 = this.edge2;
                re = this.edge3;
            }
            //实边相对的顶点
            if (this.point1.ID != re.startPoint.ID && this.point1.ID != re.endPoint.ID)
            {
                vVex = this.point1;
            }
            else if (this.point2.ID != re.startPoint.ID && this.point2.ID != re.endPoint.ID)
            {
                vVex = this.point2;
            }
            else if (this.point3.ID != re.startPoint.ID && this.point3.ID != re.endPoint.ID)
            {
                vVex = this.point3;
            }
            return re;
        }

        /// <summary>
        /// 获取2类三角形的起点和虚边
        /// </summary>
        /// <param name="vEdge">实边</param>
        /// <param name="redge1">左虚边</param>
        /// <param name="redge2">右虚边</param>
        /// <returns></returns>
        public TriNode GetStartPointofT2(out TriEdge vEdge,out TriEdge redge1,out TriEdge redge2)
        {
            redge1 = null;
            redge2 = null;
            vEdge = null;

            if (this.edge1.tagID == -1)
            {
                redge1 = this.edge2;
                redge2 = this.edge3;
                vEdge = this.edge1;

            }
            else if (this.edge2.tagID == -1)
            {
                redge1 = this.edge3;
                redge2 = this.edge1;
                vEdge = this.edge2;
            }
            else if (this.edge3.tagID == -1)
            {
                redge1 = this.edge1;
                redge2 = this.edge2;
                vEdge = this.edge3;
            }

            return redge1.endPoint;
        }

        /// <summary>
        /// 获取1类三角形的起点
        /// </summary>
        /// <returns></returns>
        public TriEdge GetStartEdgeofT1()
        {
            Triangle nextTri = null;
            if (this.edge1.tagID == -1)
            {
                nextTri = this.edge1.rightTriangle;
                if (nextTri == null)
                    return this.edge1;
            }

            if (this.edge2.tagID == -1)
            {
                nextTri = this.edge2.rightTriangle;
                if (nextTri == null)
                    return this.edge2;
            }

            if (this.edge3.tagID == -1)
            {
                nextTri = this.edge3.rightTriangle;
                if (nextTri == null)
                    return this.edge3;
            }
            return null;
        }

        /// <summary>
        /// 获取一类三角形中另外一条虚边
        /// </summary>
        /// <returns></returns>
        public TriEdge GetOtherVEdgeofT1(TriEdge vedge)
        {
            //=========调试出现空错误9-6
            if (vedge == null)
                return null;
            TriEdge ovedge = null; //虚拟边
            if (this.edge1.tagID == -1 && this.edge1.ID != vedge.ID)
            {
                ovedge = this.edge1;
            }
            else if (this.edge2.tagID == -1 && this.edge2.ID != vedge.ID)
            {
                ovedge = this.edge2;
            }
            else if (this.edge3.tagID == -1 && this.edge3.ID != vedge.ID)
            {
                ovedge = this.edge3;
            }
            return ovedge;
        }
        /// <summary>
        /// 获取3类三角形的两条左边边和前边
        /// </summary>
        /// <param name="p">顶点</param>
        /// <param name="LE"></param>
        /// <param name="RE"></param>
        /// <param name="FE"></param>
        public void GetEdgesofT3(out TriEdge LE, out TriEdge RE, out TriEdge FE)
        {
            LE = null;
            RE = null;
            FE = null;

            if (this.edge1.startPoint.ID != this.point1.ID && this.edge1.endPoint.ID != this.point1.ID)
            {
                FE = this.edge1;
            }
            else if (this.edge2.startPoint.ID != this.point1.ID && this.edge2.endPoint.ID != this.point1.ID)
            {
                FE = this.edge2;
            }
            else if (this.edge3.startPoint.ID != this.point1.ID && this.edge3.endPoint.ID != this.point1.ID)
            {
                FE = this.edge3;
            }

            if (this.edge1.startPoint.ID == this.point3.ID && this.edge1.endPoint.ID == this.point1.ID)
            {
                LE = this.edge1;
            }
            else if (this.edge2.startPoint.ID == this.point3.ID && this.edge2.endPoint.ID== this.point1.ID)
            {
                LE = this.edge2;
            }
            else if (this.edge3.startPoint.ID == this.point3.ID && this.edge3.endPoint.ID == this.point1.ID)
            {
                LE = this.edge3;
            }

            if (this.edge1.startPoint.ID == this.point1.ID && this.edge1.endPoint.ID == this.point2.ID)
            {
                RE = this.edge1;
            }
            else if (this.edge2.startPoint.ID == this.point1.ID && this.edge2.endPoint.ID == this.point2.ID)
            {
                RE = this.edge2;
            }
            else if (this.edge3.startPoint.ID == this.point1.ID && this.edge3.endPoint.ID == this.point2.ID)
            {
                RE = this.edge3;
            }
        }

        /// <summary>
        /// 获取3类三角形某个顶点对应Skeleton_Arc的平均距离
        /// </summary>
        /// <param name="v">顶点</param>
        /// <returns></returns>
        public double CalAveDisforT2_3(TriNode v)
        {
            TriEdge corEdge = null;
            if (this.edge1.startPoint.ID != v.ID && this.edge1.endPoint.ID != v.ID)
            {
                corEdge = edge1;
            }
            else if (this.edge2.startPoint.ID != v.ID && this.edge2.endPoint.ID != v.ID)
            {
                corEdge = edge2;
            }
            else if (this.edge3.startPoint.ID != v.ID && this.edge3.endPoint.ID != v.ID)
            {
                corEdge = edge3;
            }
            return 0.333333 * corEdge.Length;
        }

        /// <summary>
        /// 求1类三角形的最小距离
        /// </summary>
        /// <param name="v1">顶点</param>
        /// <param name="v2">底边顶点1</param>
        /// <param name="v3">底边顶点2</param>
        /// <param name="nearestPoint">最近距离</param>
        /// <returns>最小距离</returns>
        public double CalMinDisforT1(TriNode v1, TriNode v2, TriNode v3, out NearestPoint nearestPoint)
        {

            nearestPoint = new NearestPoint();
            nearestPoint.ID = v2.TagValue;
            double A = (v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y);
            double B = (v2.X - v3.X) * (v2.X - v3.X) + (v2.Y - v3.Y) * (v2.Y - v3.Y);
            double C = (v3.X - v1.X) * (v3.X - v1.X) + (v3.Y - v1.Y) * (v3.Y - v1.Y);
            double a = Math.Sqrt(A);
            double b = Math.Sqrt(B);
            double c = Math.Sqrt(C);

            double cosB = (A + B - C) / (2 * a * b);
            double cosC = (C + B - A) / (2 * b * c);

            if (cosB > 0 && cosC > 0)
            {
                double d = Math.Sqrt((v2.X - v3.X) * (v2.X - v3.X) + (v2.Y - v3.Y) * (v2.Y - v3.Y));
                double e = (v3.X - v2.X) * (v1.X - v2.X) + (v3.Y - v2.Y) * (v1.Y - v2.Y);
                e = e / (d * d);
                double x4 = v2.X + (v3.X - v2.X) * e;
                double y4 = v2.Y + (v3.Y - v2.Y) * e;
                nearestPoint.X = x4;
                nearestPoint.Y = y4;
                return Math.Sqrt((x4 - v1.X) * (x4 - v1.X) + (y4 - v1.Y) * (y4 - v1.Y));
            }
            else
            {
                if (cosB <= 0)
                {
                    nearestPoint.X = v2.X;
                    nearestPoint.Y = v2.Y;
                    nearestPoint.ID = v2.TagValue;
                    return a;
                }
                else if (cosC <= 0)
                {
                    nearestPoint.X = v3.X;
                    nearestPoint.Y = v3.Y;
                    nearestPoint.ID = v3.TagValue;
                    return c;
                }
            }
            return 9999999;
        }

        /// <summary>
        /// 判断输入的三角形集合中是否有三角形和本三角形为同一三角形
        /// </summary>
        /// <param name="triangles"></param>
        /// <returns></returns>
        public bool isContainedINTris(List<Triangle> triangles)
        {
            for (int i = 0; i < triangles.Count; i++)
            {
                if (this.Equal(triangles[i]))
                    return true;
            }
            return false;
        }

        public bool isContainedInTris2(List<Triangle> TriangleList)
        {
            if (TriangleList.Contains(this))
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断输入的三角形集合是否与已有的三角形相交
        /// </summary>
        /// <param name="Triangles"></param>
        /// <returns>false不相交；true相交</returns>
        public bool IsIntersectAsPolygon(List<Triangle> Triangles)
        {
            IPolygon pTriangle = this.ObjectConvert(this);

            for (int i = 0; i < Triangles.Count; i++)
            {
                IPolygon tTriangle = this.ObjectConvert(Triangles[i]);
                ITopologicalOperator iTo = pTriangle as ITopologicalOperator;
                IGeometry IGeo = iTo.Intersect(tTriangle, esriGeometryDimension.esriGeometry2Dimension);

                if (IGeo != null)
                {
                    IArea iArea = IGeo as IArea;
                    if (iArea.Area > 0)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// 判断给定的三角形的三个节点是在同一条直线上的点
        /// </summary>
        /// <returns>true表示在同一条直线上；false表示不在同一条直线上</returns>
        public bool IsOnLine()
        {
            float k1 = (float)((this.point1.Y - this.point2.Y) / (this.point1.X - this.point2.X));
            float k2 = (float)((this.point2.Y - this.point3.Y) / (this.point2.X - this.point3.X));

            if (k1 == k2)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 将polygonobject转换成polygon
        /// </summary>
        /// <param name="pPolygonObject"></param>
        /// <returns></returns>
        public IPolygon ObjectConvert(Triangle curTri)
        {
            Ring ring1 = new RingClass();
            object missing = Type.Missing;

            IPoint curResultPoint = new PointClass();
            TriNode curPoint = null;
            if (curTri != null)
            {
                for (int i = 0; i < curTri.points.Count; i++)
                {
                    curPoint = curTri.points[i];
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
        /// 比较两个三角形是否为同一三角形
        /// </summary>
        /// <param name="tri"></param>
        /// <returns></returns>
        public bool Equal(Triangle tri)
        {
            if (tri.IsContainPoint(this.point1) && tri.IsContainPoint(this.point2) && tri.IsContainPoint(this.point3))
                return true;
            else
                return false;
        }

    }
}
