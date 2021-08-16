using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Windows.Forms;
using AuxStructureLib.IO;
using System.Data;
using System.IO;

namespace AuxStructureLib
{
    /// <summary>
    /// 地图类-用于处理数据读入和组织
    /// </summary>
    public class SMap
    {
        /// <summary>
        /// 数据列表
        /// </summary>
        public List<IFeatureLayer> lyrList = null;
        /// <summary>
        /// 点对象列表
        /// </summary>
        public List<PointObject> PointList = null;
        /// <summary>
        /// 线对象列表
        /// </summary>
        public List<PolylineObject> PolylineList = null;
        /// <summary>
        /// 多边形对象列表
        /// </summary>
        public List<PolygonObject> PolygonList = null;
        /// <summary>
        /// 多边形对象列表
        /// </summary>
        public Dictionary<PolygonObject, bool> PolygonDic = null;
        /// <summary>
        /// 坐标顶点列表
        /// </summary>
        public List<TriNode> TriNodeList = null;
        /// <summary>
        /// 线的关联点列表
        /// </summary>
        public List<ConNode> ConNodeList = null;

        /// <summary>
        /// 地图对象个数
        /// </summary>
        public int NumberofMapObject
        {
            get 
            {
                int n = 0;
                if (this.PointList != null && this.PointList.Count > 0)
                {
                    n += this.PointList.Count;
                }
                if (this.PolygonList != null && this.PolygonList.Count > 0)
                {
                    n += this.PolygonList.Count;
                }
                if (this.PolylineList != null && this.PolylineList.Count > 0)
                {
                    n += this.PolylineList.Count;
                }
                return n;
            }
        }

        /// <summary>
        /// 地图对象个数
        /// </summary>
        public List<MapObject> MapObjectList
        {
            get
            {
                List<MapObject> mapObjectList = new List<MapObject>();

                if (this.PointList != null && this.PointList.Count > 0)
                {
                    mapObjectList.AddRange(this.PointList);
                }
                if (this.PolygonList != null && this.PolygonList.Count > 0)
                {
                    mapObjectList.AddRange(this.PolygonList);
                }
                if (this.PolylineList != null && this.PolylineList.Count > 0)
                {
                    mapObjectList.AddRange(this.PolylineList);
                }
                return mapObjectList;
            }
        }

        /// <summary>
        /// 地图构造函数
        /// </summary>
        public SMap(List<IFeatureLayer> lyrs)
        {
            lyrList = lyrs;
            PointList = new List<PointObject>();
            PolylineList = new List<PolylineObject>();
            ConNodeList = new List<ConNode>();
            PolygonList = new List<PolygonObject>();
            TriNodeList = new List<TriNode>();
        }
        /// <summary>
        /// 地图构造函数
        /// </summary>
        public SMap()
        {
            PointList = new List<PointObject>();
            PolylineList = new List<PolylineObject>();
            ConNodeList = new List<ConNode>();
            PolygonList = new List<PolygonObject>();
            TriNodeList = new List<TriNode>();
        }

        public string strPath="";
        /// <summary>
        /// 从ArcGIS图层中读取地图数据
        /// </summary>
        public void ReadDateFrmEsriLyrs()
        {

            if (lyrList == null || lyrList.Count == 0)
            {
                return;
            }

            #region 读符号文件
            List<Symbol> symbolList = new List<Symbol>();
            //读文件========
            DataTable dt = TestIO.ReadData(strPath+@"\Symbol.xml");
            if (dt != null)
            {
                foreach (DataRow curR in dt.Rows)
                {
                    int ID = Convert.ToInt32(curR[0]);
                    int sylID = Convert.ToInt32(curR[1]);
                    string LayName = Convert.ToString(curR[2]);
                    double size = Convert.ToDouble(curR[3]);
                    string FillColor = Convert.ToString(curR[4]);
                    string BorderColor = Convert.ToString(curR[5]);

                    Symbol s = new Symbol(ID, sylID, LayName, size, FillColor, BorderColor);

                    symbolList.Add(s);
                }
            }

            #endregion

            #region 创建列表
            int pCount = 0;
            int lCount = 0;
            int aCount = 0;
            foreach (IFeatureLayer curLyr in lyrList)
            {
                if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                {
                    pCount++;

                }
                else if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                {
                    lCount++;
                }
                else if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                {
                    aCount++;
                }
            }
            if (pCount > 0)
            {
                PointList = new List<PointObject>();
            }
            if (lCount > 0)
            {
                PolylineList = new List<PolylineObject>();
                ConNodeList = new List<ConNode>();
            }
            if (aCount > 0)
            {
                PolygonList = new List<PolygonObject>();
            }
            TriNodeList = new List<TriNode>();
            #endregion

            int vextexID = 0;
            double sylSize=0;
            int pID =0;
            int plID =0;
            int ppID =0;

            foreach (IFeatureLayer curLyr in lyrList)
            {
                IFeatureCursor cursor = null;
                IFeature curFeature = null;
                IGeometry shp = null;
                Symbol curSyl= null;

                curSyl = Symbol.GetSymbolbyLyrName(curLyr.Name, symbolList);
                if (curSyl != null)
                    sylSize = curSyl.Size;
                switch (curLyr.FeatureClass.ShapeType)
                {
                       
                    case esriGeometryType.esriGeometryPoint:
                        {


                            #region 点要素
                            //点要素
                            cursor = curLyr.Search(null, false);

                            while ((curFeature = cursor.NextFeature()) != null)
                            {
                                shp = curFeature.Shape;
                                //pID = curFeature.OID;
                                IPoint point = null;
                                //几何图形
                                if (shp.GeometryType == esriGeometryType.esriGeometryPoint)
                                {
                                    point = shp as IPoint;
                                    PointObject curPoint = null;           //当前道路
                                    TriNode curVextex = null;                  //当前关联点
                                    double curX;
                                    double curY;

                                    curX = point.X;
                                    curY = point.Y;
                                    curVextex = new TriNode((float)curX, (float)curY, vextexID, pID, FeatureType.PointType);
                                    curPoint = new PointObject(pID, curVextex);
                                    curPoint.SylWidth = sylSize;
                                    TriNodeList.Add(curVextex);
                                    PointList.Add(curPoint);
                                    vextexID++;
                                    pID++;
                                }

                            }
                            #endregion
                            break;
                        }
                    case esriGeometryType.esriGeometryPolyline:
                        {

                            #region 线要素
                            cursor = curLyr.Search(null, false);
                            curFeature = null;
                            shp = null;
                            while ((curFeature = cursor.NextFeature()) != null)
                            {
                                shp = curFeature.Shape;
                                IPolyline polyline = null;
                                IGeometryCollection pathSet = null;
                                int indexofType=curFeature.Fields.FindField("Type");
                                int typeID = 1;
                                if (indexofType!=-1&&curFeature.get_Value(indexofType) != null)
                                {
                                    try
                                    {
                                        typeID = (Int16)(curFeature.get_Value(indexofType));
                                    }
                                    catch
                                    {
                                        typeID = 1;
                                    }
                                }
                                //几何图形
                                if (shp.GeometryType == esriGeometryType.esriGeometryPolyline)
                                {
                                    polyline = shp as IPolyline;
                                    //plID = curFeature.OID;
                                    pathSet = polyline as IGeometryCollection;
                                    int count = pathSet.GeometryCount;
                                    //Path对象
                                    IPath curPath = null;
                                    for (int i = 0; i < count; i++)
                                    {
                                        PolylineObject curPL = null;                      //当前道路
                                        TriNode curVextex = null;                  //当前关联点
                                        List<TriNode> curPointList = new List<TriNode>();
                                        double curX;
                                        double curY;

                                        curPath = pathSet.get_Geometry(i) as IPath;
                                        IPointCollection pointSet = curPath as IPointCollection;
                                        int pointCount = pointSet.PointCount;
                                        if (pointCount >= 2)
                                        {
                                            curX = pointSet.get_Point(0).X;
                                            curY = pointSet.get_Point(0).Y;
                                            TriNode cNode = ConNode.GetContainNode(ConNodeList, TriNodeList, curX, curY);
                                            if (cNode == null)   //该关联点还未加入的情况
                                            {
                                                curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                                TriNodeList.Add(curVextex);
                                                ConNode curNode = new ConNode(vextexID, 0.2f, curVextex);
                                                ConNodeList.Add(curNode);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }
                                            else //该关联点已经加入的情况
                                            {
                                                curPointList.Add(cNode);
                                                cNode.TagValue = -1;
                                                cNode.FeatureType = FeatureType.ConnNode;
                                            }
                                            //加入中间顶点
                                            for (int j = 1; j < pointCount - 1; j++)
                                            {
                                                curX = pointSet.get_Point(j).X;
                                                curY = pointSet.get_Point(j).Y;
                                                curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                                TriNodeList.Add(curVextex);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }
                                            //加入终点
                                            curX = pointSet.get_Point(pointCount - 1).X;
                                            curY = pointSet.get_Point(pointCount - 1).Y;
                                            cNode = ConNode.GetContainNode(ConNodeList, TriNodeList, curX, curY);
                                            if (cNode == null)   //该关联点还未加入的情况
                                            {
                                                curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                                TriNodeList.Add(curVextex);

                                                ConNode curNode = new ConNode(vextexID, -1f, curVextex);
                                                ConNodeList.Add(curNode);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }
                                            else //该关联点已经加入的情况
                                            {
                                                curPointList.Add(cNode);
                                                cNode.TagValue = -1;
                                                cNode.FeatureType = FeatureType.ConnNode;
                                            }

                                            //添加起点
                                            curPL = new PolylineObject(plID, curPointList, sylSize);
                                            curPL.TypeID = typeID;
                                            PolylineList.Add(curPL);
                                            plID++;
                                        }
                                    }
                                }
                            }

                            #endregion
                            break;
                        }

                    case esriGeometryType.esriGeometryPolygon:
                        {
                            #region 面要素
                            cursor = curLyr.Search(null, false);
                            curFeature = null;
                            shp = null;
                            while ((curFeature = cursor.NextFeature()) != null)
                            {
                                shp = curFeature.Shape;
                                IPolygon polygon = null;
                                IGeometryCollection pathSet = null;
                                //几何图形
                                if (shp.GeometryType == esriGeometryType.esriGeometryPolygon)
                                {
                                   // ppID = curFeature.OID;
                                    polygon = shp as IPolygon;
                                    pathSet = polygon as IGeometryCollection;
                                    int count = pathSet.GeometryCount;
                                    //Path对象
                                    IPath curPath = null;
                                    for (int i = 0; i < count; i++)
                                    {
                                        PolygonObject curPP = null;                      //当前道路
                                        TriNode curVextex = null;                  //当前关联点
                                        List<TriNode> curPointList = new List<TriNode>();
                                        double curX;
                                        double curY;
                                        curPath = pathSet.get_Geometry(i) as IPath;
                                        IPointCollection pointSet = curPath as IPointCollection;
                                        int pointCount = pointSet.PointCount;
                                        if (pointCount >= 3)
                                        {
                                            //ArcGIS中将起点和终点重复存储
                                            for (int j = 0; j < pointCount - 1; j++)
                                            {
                                                //添加起点
                                                curX = pointSet.get_Point(j).X;
                                                curY = pointSet.get_Point(j).Y;
                                                curVextex = new TriNode(curX, curY, vextexID, ppID, FeatureType.PolygonType);
                                                TriNodeList.Add(curVextex);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }

                                            //添加起点
                                            curPP = new PolygonObject(ppID, curPointList);
                                            curPP.SylWidth = sylSize;
                                            this.PolygonList.Add(curPP);
                                            ppID++;
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// 从ArcGIS图层中读取地图数据ForEnrichNetWork
        /// </summary>
        public void ReadDateFrmEsriLyrsForEnrichNetWork()
        {

            if (lyrList == null || lyrList.Count == 0)
            {
                return;
            }

            #region 读符号文件
            List<Symbol> symbolList = new List<Symbol>();
            //读文件========
            DataTable dt = TestIO.ReadData(strPath + @"\Symbol.xml");
            if (dt != null)
            {
                foreach (DataRow curR in dt.Rows)
                {
                    int ID = Convert.ToInt32(curR[0]);
                    int sylID = Convert.ToInt32(curR[1]);
                    string LayName = Convert.ToString(curR[2]);
                    double size = Convert.ToDouble(curR[3]);
                    string FillColor = Convert.ToString(curR[4]);
                    string BorderColor = Convert.ToString(curR[5]);

                    Symbol s = new Symbol(ID, sylID, LayName, size, FillColor, BorderColor);

                    symbolList.Add(s);
                }
            }

            #endregion

            #region 创建列表
            int pCount = 0;
            int lCount = 0;
            int aCount = 0;
            foreach (IFeatureLayer curLyr in lyrList)
            {
                if (curLyr != null)
                {
                    if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                    {
                        pCount++;

                    }
                    else if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                    {
                        lCount++;
                    }
                    else if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        aCount++;
                    }
                }
            }
            if (pCount > 0)
            {
                PointList = new List<PointObject>();
            }
            if (lCount > 0)
            {
                PolylineList = new List<PolylineObject>();
                ConNodeList = new List<ConNode>();
            }
            if (aCount > 0)
            {
                PolygonList = new List<PolygonObject>();
            }
            TriNodeList = new List<TriNode>();
            #endregion

            int vextexID = 0;
            double sylSize = 0;
            int pID = 0;
            int plID = 0;
            int ppID = 0;

            foreach (IFeatureLayer curLyr in lyrList)
            {
                if (curLyr != null)
                {
                    IFeatureCursor cursor = null;
                    IFeature curFeature = null;
                    IGeometry shp = null;
                    Symbol curSyl = null;

                    curSyl = Symbol.GetSymbolbyLyrName(curLyr.Name, symbolList);
                    if (curSyl != null)
                        sylSize = curSyl.Size;
                    if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                    {
                        #region 线要素
                        cursor = curLyr.Search(null, false);
                        curFeature = null;
                        shp = null;
                        while ((curFeature = cursor.NextFeature()) != null)
                        {
                            shp = curFeature.Shape;
                            IPolyline polyline = null;
                            IGeometryCollection pathSet = null;

                            int indexofType = curFeature.Fields.FindField("Type");

                            int typeID = 1;
                            if (indexofType != -1 && curFeature.get_Value(indexofType) != null)
                            {
                                typeID = (Int16)(curFeature.get_Value(indexofType));
                            }

                            //几何图形
                            if (shp.GeometryType == esriGeometryType.esriGeometryPolyline)
                            {
                                polyline = shp as IPolyline;
                                //plID = curFeature.OID;
                                pathSet = polyline as IGeometryCollection;
                                int count = pathSet.GeometryCount;
                                //Path对象
                                IPath curPath = null;
                                for (int i = 0; i < count; i++)
                                {
                                    PolylineObject curPL = null;                      //当前道路
                                    TriNode curVextex = null;                  //当前关联点
                                    List<TriNode> curPointList = new List<TriNode>();
                                    double curX;
                                    double curY;

                                    curPath = pathSet.get_Geometry(i) as IPath;
                                    IPointCollection pointSet = curPath as IPointCollection;
                                    int pointCount = pointSet.PointCount;
                                    if (pointCount >= 2)
                                    {
                                        curX = pointSet.get_Point(0).X;
                                        curY = pointSet.get_Point(0).Y;
                                        TriNode cNode = ConNode.GetContainNode(ConNodeList, TriNodeList, curX, curY);
                                        if (cNode == null)   //该关联点还未加入的情况
                                        {
                                            curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                            TriNodeList.Add(curVextex);
                                            ConNode curNode = new ConNode(vextexID, 0.2f, curVextex);
                                            ConNodeList.Add(curNode);
                                            curPointList.Add(curVextex);
                                            vextexID++;
                                        }
                                        else //该关联点已经加入的情况
                                        {
                                            curPointList.Add(cNode);
                                            cNode.TagValue = -1;
                                            cNode.FeatureType = FeatureType.ConnNode;
                                        }
                                        //加入中间顶点
                                        for (int j = 1; j < pointCount - 1; j++)
                                        {
                                            curX = pointSet.get_Point(j).X;
                                            curY = pointSet.get_Point(j).Y;
                                            curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                            TriNodeList.Add(curVextex);
                                            curPointList.Add(curVextex);
                                            vextexID++;
                                        }
                                        //加入终点
                                        curX = pointSet.get_Point(pointCount - 1).X;
                                        curY = pointSet.get_Point(pointCount - 1).Y;
                                        cNode = ConNode.GetContainNode(ConNodeList, TriNodeList, curX, curY);
                                        if (cNode == null)   //该关联点还未加入的情况
                                        {
                                            curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                            TriNodeList.Add(curVextex);

                                            ConNode curNode = new ConNode(vextexID, -1f, curVextex);
                                            ConNodeList.Add(curNode);
                                            curPointList.Add(curVextex);
                                            vextexID++;
                                        }
                                        else //该关联点已经加入的情况
                                        {
                                            curPointList.Add(cNode);
                                            cNode.TagValue = -1;
                                            cNode.FeatureType = FeatureType.ConnNode;
                                        }

                                        //添加起点
                                        curPL = new PolylineObject(plID, curPointList, sylSize);
                                        curPL.TypeID = typeID;
                                        PolylineList.Add(curPL);
                                        plID++;
                                    }
                                }
                            }
                        }

                        #endregion
                    }
                }
            }

            //this.InterpretatePoint(2);

            vextexID = this.TriNodeList.Count;

            foreach (IFeatureLayer curLyr in lyrList)
            {
                if (curLyr != null)
                {
                    IFeatureCursor cursor = null;
                    IFeature curFeature = null;
                    IGeometry shp = null;
                    Symbol curSyl = null;

                    curSyl = Symbol.GetSymbolbyLyrName(curLyr.Name, symbolList);
                    if (curSyl != null)
                        sylSize = curSyl.Size;
                    if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        #region 面要素
                        cursor = curLyr.Search(null, false);
                        curFeature = null;
                        shp = null;
                        int PolygonID = -1;
                        while ((curFeature = cursor.NextFeature()) != null)
                        {
                            PolygonID++;   
                            int ClassID = -1;//分类参数
                            int SourceTemp = 0;
                            //string[] PatternString = null;
                            //int PatternID = -1;
                            //int SortID = -1;
                            //int Road = -1;
                            //int PatternID = -1;//分属Pattern参数
                            //int OrderId = -1;//建筑物在pattern中的位置
                            //int SiSim = -1;//大小相似度
                            //int OriSim = -1;//方向相似度

                            #region 读取建筑物视觉匹配图形
                            try
                            {
                                IFields pFields = curFeature.Fields;
                                int field1 = pFields.FindField("Target");
                                SourceTemp = Convert.ToInt16(curFeature.get_Value(field1));
                            }

                            catch { }
                            #endregion

                            #region 读取分类
                            try
                            {
                                IFields pFields = curFeature.Fields;
                                int field1 = pFields.FindField("KID");
                                ClassID = Convert.ToInt16(curFeature.get_Value(field1));
                            }

                            catch { }
                            #endregion

                            #region 读取Pattern（不考虑pattern交叉）及建筑物的顺序
                            //try
                            //{
                            //    IFields pFields = curFeature.Fields;
                            //    int field1 = pFields.FindField("Pattern");
                            //    string Pattern = Convert.ToString(curFeature.get_Value(field1));
                            //    PatternString = Pattern.Split(new string[]{"_"}, StringSplitOptions.RemoveEmptyEntries);
                            //}

                            //catch { }
                            #endregion

                            #region 读取建筑物Pattern
                            //try
                            //{
                            //    IFields pFields = curFeature.Fields;
                            //    int field1 = pFields.FindField("PatternID");
                            //    PatternID = Convert.ToInt32(curFeature.get_Value(field1));
                            //}

                            //catch { }
                            #endregion

                            #region 读取建筑物Pattern顺序
                            //try
                            //{
                            //    IFields pFields = curFeature.Fields;
                            //    int field1 = pFields.FindField("SortID");
                            //    SortID = Convert.ToInt32(curFeature.get_Value(field1));
                            //}

                            //catch { }
                            #endregion

                            #region 读取建筑物Pattern
                            //try
                            //{
                            //    IFields pFields = curFeature.Fields;
                            //    int field1 = pFields.FindField("Road");
                            //    Road = Convert.ToInt32(curFeature.get_Value(field1));
                            //}

                            //catch { }
                            #endregion

                            #region 读取是否相似
                            //try
                            //{
                            //    IFields pFields = curFeature.Fields;
                            //    int field1 = pFields.FindField("SiSim");
                            //    int field2=pFields.FindField("OriSim");
                            //    SiSim = Convert.ToInt16(curFeature.get_Value(field1));
                            //    OriSim = Convert.ToInt16(curFeature.get_Value(field2));
                            //}

                            //catch { }
                            #endregion

                            shp = curFeature.Shape;
                            IPolygon polygon = null;
                            IGeometryCollection pathSet = null;
                            //几何图形
                            if (shp.GeometryType == esriGeometryType.esriGeometryPolygon)
                            {
                                // ppID = curFeature.OID;
                                polygon = shp as IPolygon;
                                pathSet = polygon as IGeometryCollection;
                                int count = pathSet.GeometryCount;
                                #region Path对象
                                IPath curPath = null;
                                for (int i = 0; i < count; i++)
                                {
                                    PolygonObject curPP = null;                 //当前道路
                                    TriNode curVextex = null;                  //当前关联点
                                    List<TriNode> curPointList = new List<TriNode>();
                                    double curX;
                                    double curY;
                                    curPath = pathSet.get_Geometry(i) as IPath;
                                    IPointCollection pointSet = curPath as IPointCollection;

                                    #region 节点读取
                                    int pointCount = pointSet.PointCount;
                                    if (pointCount >= 3)
                                    {
                                        //ArcGIS中将起点和终点重复存储
                                        for (int j = 0; j < pointCount - 1; j++)
                                        {
                                            //添加起点
                                            curX = pointSet.get_Point(j).X;
                                            curY = pointSet.get_Point(j).Y;
                                            curVextex = new TriNode(curX, curY, vextexID, ppID, FeatureType.PolygonType);
                                            curVextex.InOut = count;//记录节点是处于Holes或者外环
                                            TriNodeList.Add(curVextex);
                                            curPointList.Add(curVextex);
                                            vextexID++;
                                        }

                                        //添加起点
                                        curPP = new PolygonObject(ppID, curPointList);
                                        //curPP.PatternID = PatternID;
                                        //curPP.SortID = SortID;
                                        //curPP.Road = Road;
                                        curPP.ClassID = ClassID;
                                        curPP.SourceTemp = SourceTemp;
                                        //for (int p = 0; p < PatternString.Count(); p = p + 2)
                                        //{
                                        //    List<int> pList = new List<int>(); 
                                        //    pList.Add(Convert.ToInt32(PatternString[p])); 
                                        //    pList.Add(Convert.ToInt32(PatternString[p+1]));
                                        //    curPP.OrderIDList.Add(pList);
                                        //}

                                        //curPP.PatternIDList.Add(PatternID);
                                        //List<int> pList = new List<int>(); pList.Add(PatternID); pList.Add(OrderId);
                                        //curPP.OrderIDList.Add(pList);
                                        //if (SiSim == 0)
                                        //{
                                        //    curPP.SiSim = true;//0表示相似
                                        //}

                                        //if (SiSim == 1)
                                        //{
                                        //    curPP.SiSim = false;//1表示不相似
                                        //}

                                        //if (OriSim == 0)
                                        //{
                                        //    curPP.OriSim = true;//0表示相似
                                        //}

                                        //if (OriSim == 1)
                                        //{
                                        //    curPP.OriSim = false;//1表示不相似
                                        //}

                                        curPP.SylWidth = sylSize;
                                        curPP.BuildingID = PolygonID;//记录Polygon图形位于哪一个建筑物图形
                                        curPP.InOut = i;//记录建筑物是环还是Holes
                                        this.PolygonList.Add(curPP);
                                        ppID++;
                                    }
                                    #endregion
                                }
                                #endregion
                            }
                        }
                        #endregion
                    }
                }
            }

        }

        /// <summary>
        /// 针对给定的一个多边形（Feature），读取数据
        /// </summary>
        /// <param name="curFeature"></param>
        public void ReadDataFrmGivenPolygonObject(IFeature curFeature)
        {
            int vextexID = 0;
            double sylSize = 0;
            int pID = 0;
            int plID = 0;
            int ppID = 0;
            IGeometry shp = null;

            #region 面要素
            int PolygonID = -1;
            if(curFeature != null)
            {
                PolygonID++;

                shp = curFeature.Shape;
                IPolygon polygon = null;
                IGeometryCollection pathSet = null;
                //几何图形
                if (shp.GeometryType == esriGeometryType.esriGeometryPolygon)
                {
                    // ppID = curFeature.OID;
                    polygon = shp as IPolygon;
                    pathSet = polygon as IGeometryCollection;
                    int count = pathSet.GeometryCount;
                    #region Path对象
                    IPath curPath = null;
                    for (int i = 0; i < count; i++)
                    {
                        PolygonObject curPP = null;                 //当前道路
                        TriNode curVextex = null;                  //当前关联点
                        List<TriNode> curPointList = new List<TriNode>();
                        double curX;
                        double curY;
                        curPath = pathSet.get_Geometry(i) as IPath;
                        IPointCollection pointSet = curPath as IPointCollection;

                        #region 节点读取
                        int pointCount = pointSet.PointCount;
                        if (pointCount >= 3)
                        {
                            //ArcGIS中将起点和终点重复存储
                            for (int j = 0; j < pointCount - 1; j++)
                            {
                                //添加起点
                                curX = pointSet.get_Point(j).X;
                                curY = pointSet.get_Point(j).Y;
                                curVextex = new TriNode(curX, curY, vextexID, ppID, FeatureType.PolygonType);
                                curVextex.InOut = count;//记录节点是处于Holes或者外环
                                TriNodeList.Add(curVextex);
                                curPointList.Add(curVextex);
                                vextexID++;
                            }

                            //添加起点
                            curPP = new PolygonObject(ppID, curPointList);

                            curPP.SylWidth = sylSize;
                            curPP.BuildingID = PolygonID;//记录Polygon图形位于哪一个建筑物图形
                            curPP.InOut = i;//记录建筑物是环还是Holes
                            this.PolygonList.Add(curPP);
                            ppID++;
                        }
                        #endregion
                    }
                    #endregion
                }
            }
            #endregion
        }

        /// <summary>
        /// 针对给定的多边形列表，读取数据
        /// </summary>
        /// <param name="PoList"></param>
        public void ReadDataFrmGivenPolygonList(List<IPolygon> PoList)
        {
            int vextexID = 0;
            double sylSize = 0;
            int pID = 0;
            int plID = 0;
            int ppID = 0;

            #region 面要素
            int PolygonID = -1;
            for (int k = 0; k < PoList.Count; k++)
            {
                if (PoList[k] != null)
                {
                    PolygonID++;

                    IPolygon polygon = PoList[k];
                    IGeometryCollection pathSet = null;
                    //几何图形
                    // ppID = curFeature.OID;
                    pathSet = polygon as IGeometryCollection;
                    int count = pathSet.GeometryCount;

                    #region Path对象
                    IPath curPath = null;
                    for (int i = 0; i < count; i++)
                    {
                        PolygonObject curPP = null;                 //当前道路
                        TriNode curVextex = null;                  //当前关联点
                        List<TriNode> curPointList = new List<TriNode>();
                        double curX;
                        double curY;
                        curPath = pathSet.get_Geometry(i) as IPath;
                        IPointCollection pointSet = curPath as IPointCollection;

                        #region 节点读取
                        int pointCount = pointSet.PointCount;
                        if (pointCount >= 3)
                        {
                            //ArcGIS中将起点和终点重复存储
                            for (int j = 0; j < pointCount - 1; j++)
                            {
                                //添加起点
                                curX = pointSet.get_Point(j).X;
                                curY = pointSet.get_Point(j).Y;
                                curVextex = new TriNode(curX, curY, vextexID, ppID, FeatureType.PolygonType);
                                curVextex.InOut = count;//记录节点是处于Holes或者外环
                                TriNodeList.Add(curVextex);
                                curPointList.Add(curVextex);
                                vextexID++;
                            }

                            //添加起点
                            curPP = new PolygonObject(ppID, curPointList);

                            curPP.SylWidth = sylSize;
                            curPP.BuildingID = PolygonID;//记录Polygon图形位于哪一个建筑物图形
                            curPP.InOut = i;//记录建筑物是环还是Holes
                            this.PolygonList.Add(curPP);
                            ppID++;
                        }
                        #endregion
                    }
                    #endregion
                }
            }
            #endregion
        }

        /// <summary>
        /// 针对给定的一个多边形（Polygon），读取数据
        /// </summary>
        /// <param name="Po"></param>
        public void ReadDataFrmGivenPolygonObject(Polygon Po)
       {
           int vextexID = 0;
           double sylSize = 0;
           int pID = 0;
           int plID = 0;
           int ppID = 0;
           IGeometry shp = null;

           #region 面要素
           int PolygonID = -1;
           if (Po != null)
           {
               PolygonID++;

               shp = Po as IGeometry;
               IPolygon polygon = null;
               IGeometryCollection pathSet = null;
               //几何图形
               if (shp.GeometryType == esriGeometryType.esriGeometryPolygon)
               {
                   // ppID = curFeature.OID;
                   polygon = shp as IPolygon;
                   pathSet = polygon as IGeometryCollection;
                   int count = pathSet.GeometryCount;
                   #region Path对象
                   IPath curPath = null;
                   for (int i = 0; i < count; i++)
                   {
                       PolygonObject curPP = null;                 //当前道路
                       TriNode curVextex = null;                  //当前关联点
                       List<TriNode> curPointList = new List<TriNode>();
                       double curX;
                       double curY;
                       curPath = pathSet.get_Geometry(i) as IPath;
                       IPointCollection pointSet = curPath as IPointCollection;

                       #region 节点读取
                       int pointCount = pointSet.PointCount;
                       if (pointCount >= 3)
                       {
                           //ArcGIS中将起点和终点重复存储
                           for (int j = 0; j < pointCount - 1; j++)
                           {
                               //添加起点
                               curX = pointSet.get_Point(j).X;
                               curY = pointSet.get_Point(j).Y;
                               curVextex = new TriNode(curX, curY, vextexID, ppID, FeatureType.PolygonType);
                               curVextex.InOut = count;//记录节点是处于Holes或者外环
                               TriNodeList.Add(curVextex);
                               curPointList.Add(curVextex);
                               vextexID++;
                           }

                           //添加起点
                           curPP = new PolygonObject(ppID, curPointList);

                           curPP.SylWidth = sylSize;
                           curPP.BuildingID = PolygonID;//记录Polygon图形位于哪一个建筑物图形
                           curPP.InOut = i;//记录建筑物是环还是Holes
                           this.PolygonList.Add(curPP);
                           ppID++;
                       }
                       #endregion
                   }
                   #endregion
               }
           }
           #endregion
       }

        /// <summary>
        /// 针对给定的一个多边形（Polygon），读取数据
        /// </summary>
        /// <param name="Po"></param>
        public void ReadDataFrmGivenPolygonObject(List<Polygon> PoList)
        {
            int vextexID = 0;
            double sylSize = 0;
            int pID = 0;
            int plID = 0;
            int ppID = 0;
            IGeometry shp = null;

            #region 面要素
            int PolygonID = -1;
            for (int s = 0; s < PoList.Count; s++)
            {
                Polygon Po = PoList[s];
                if (Po != null)
                {
                    PolygonID++;

                    shp = Po as IGeometry;
                    IPolygon polygon = null;
                    IGeometryCollection pathSet = null;
                    //几何图形
                    if (shp.GeometryType == esriGeometryType.esriGeometryPolygon)
                    {
                        // ppID = curFeature.OID;
                        polygon = shp as IPolygon;
                        pathSet = polygon as IGeometryCollection;
                        int count = pathSet.GeometryCount;
                        #region Path对象
                        IPath curPath = null;
                        for (int i = 0; i < count; i++)
                        {
                            PolygonObject curPP = null;                 //当前道路
                            TriNode curVextex = null;                  //当前关联点
                            List<TriNode> curPointList = new List<TriNode>();
                            double curX;
                            double curY;
                            curPath = pathSet.get_Geometry(i) as IPath;
                            IPointCollection pointSet = curPath as IPointCollection;

                            #region 节点读取
                            int pointCount = pointSet.PointCount;
                            if (pointCount >= 3)
                            {
                                //ArcGIS中将起点和终点重复存储
                                for (int j = 0; j < pointCount - 1; j++)
                                {
                                    //添加起点
                                    curX = pointSet.get_Point(j).X;
                                    curY = pointSet.get_Point(j).Y;
                                    curVextex = new TriNode(curX, curY, vextexID, ppID, FeatureType.PolygonType);
                                    curVextex.InOut = count;//记录节点是处于Holes或者外环
                                    TriNodeList.Add(curVextex);
                                    curPointList.Add(curVextex);
                                    vextexID++;
                                }

                                //添加起点
                                curPP = new PolygonObject(ppID, curPointList);

                                curPP.SylWidth = sylSize;
                                curPP.BuildingID = PolygonID;//记录Polygon图形位于哪一个建筑物图形
                                curPP.InOut = i;//记录建筑物是环还是Holes
                                this.PolygonList.Add(curPP);
                                ppID++;
                            }
                            #endregion
                        }
                        #endregion
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// 针对给定的一个多边形（Polygon），读取数据
        /// </summary>
        /// <param name="Po"></param>
        public void ReadDataFrmGivenPolygonObject(List<IPolygon> PoList)
        {
            int vextexID = 0;
            double sylSize = 0;
            int pID = 0;
            int plID = 0;
            int ppID = 0;
            IGeometry shp = null;

            #region 面要素
            int PolygonID = -1;
            for (int s = 0; s < PoList.Count; s++)
            {
                IPolygon Po = PoList[s];
                if (Po != null)
                {
                    PolygonID++;

                    shp = Po as IGeometry;
                    IPolygon polygon = null;
                    IGeometryCollection pathSet = null;
                    //几何图形
                    if (shp.GeometryType == esriGeometryType.esriGeometryPolygon)
                    {
                        // ppID = curFeature.OID;
                        polygon = shp as IPolygon;
                        pathSet = polygon as IGeometryCollection;
                        int count = pathSet.GeometryCount;
                        #region Path对象
                        IPath curPath = null;
                        for (int i = 0; i < count; i++)
                        {
                            PolygonObject curPP = null;                 //当前道路
                            TriNode curVextex = null;                  //当前关联点
                            List<TriNode> curPointList = new List<TriNode>();
                            double curX;
                            double curY;
                            curPath = pathSet.get_Geometry(i) as IPath;
                            IPointCollection pointSet = curPath as IPointCollection;

                            #region 节点读取
                            int pointCount = pointSet.PointCount;
                            if (pointCount >= 3)
                            {
                                //ArcGIS中将起点和终点重复存储
                                for (int j = 0; j < pointCount - 1; j++)
                                {
                                    //添加起点
                                    curX = pointSet.get_Point(j).X;
                                    curY = pointSet.get_Point(j).Y;
                                    curVextex = new TriNode(curX, curY, vextexID, ppID, FeatureType.PolygonType);
                                    curVextex.InOut = count;//记录节点是处于Holes或者外环
                                    TriNodeList.Add(curVextex);
                                    curPointList.Add(curVextex);
                                    vextexID++;
                                }

                                //添加起点
                                curPP = new PolygonObject(ppID, curPointList);

                                curPP.SylWidth = sylSize;
                                curPP.BuildingID = PolygonID;//记录Polygon图形位于哪一个建筑物图形
                                curPP.InOut = i;//记录建筑物是环还是Holes
                                this.PolygonList.Add(curPP);
                                ppID++;
                            }
                            #endregion
                        }
                        #endregion
                    }
                }
            }
            #endregion
        }

        public void ReadDateFrmEsriLyrswithIDField()
        {

            if (lyrList == null || lyrList.Count == 0)
            {
                return;
            }

            #region 读符号文件
            List<Symbol> symbolList = new List<Symbol>();
            //读文件========
            DataTable dt = TestIO.ReadData(strPath + @"\Symbol.xml");
            if (dt != null)
            {
                foreach (DataRow curR in dt.Rows)
                {
                    int ID = Convert.ToInt32(curR[0]);
                    int sylID = Convert.ToInt32(curR[1]);
                    string LayName = Convert.ToString(curR[2]);
                    double size = Convert.ToDouble(curR[3]);
                    string FillColor = Convert.ToString(curR[4]);
                    string BorderColor = Convert.ToString(curR[5]);

                    Symbol s = new Symbol(ID, sylID, LayName, size, FillColor, BorderColor);

                    symbolList.Add(s);
                }
            }

            #endregion

            #region 创建列表
            int pCount = 0;
            int lCount = 0;
            int aCount = 0;
            foreach (IFeatureLayer curLyr in lyrList)
            {
                if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                {
                    pCount++;

                }
                else if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                {
                    lCount++;
                }
                else if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                {
                    aCount++;
                }
            }
            if (pCount > 0)
            {
                PointList = new List<PointObject>();
            }
            if (lCount > 0)
            {
                PolylineList = new List<PolylineObject>();
                ConNodeList = new List<ConNode>();
            }
            if (aCount > 0)
            {
                PolygonList = new List<PolygonObject>();
            }
            TriNodeList = new List<TriNode>();
            #endregion

            int vextexID = 0;
            double sylSize = 0;
            int pID = 0;
            int plID = 0;
            int ppID = 0;

            foreach (IFeatureLayer curLyr in lyrList)
            {
                IFeatureCursor cursor = null;
                IFeature curFeature = null;
                IGeometry shp = null;
                Symbol curSyl = null;

                curSyl = Symbol.GetSymbolbyLyrName(curLyr.Name, symbolList);
                if (curSyl != null)
                    sylSize = curSyl.Size;
                switch (curLyr.FeatureClass.ShapeType)
                {

                    case esriGeometryType.esriGeometryPoint:
                        {
                            #region 点要素
                            //点要素
                            cursor = curLyr.Search(null, false);

                            while ((curFeature = cursor.NextFeature()) != null)
                            {
                                shp = curFeature.Shape;
                                //ID
                                int index=curFeature.Fields.FindField("ID");
                                pID = (int)(curFeature.get_Value(index));
                                IPoint point = null;
                                //几何图形
                                if (shp.GeometryType == esriGeometryType.esriGeometryPoint)
                                {
                                    point = shp as IPoint;
                                    PointObject curPoint = null;           //当前道路
                                    TriNode curVextex = null;                  //当前关联点
                                    double curX;
                                    double curY;

                                    curX = point.X;
                                    curY = point.Y;
                                    curVextex = new TriNode((float)curX, (float)curY, vextexID, pID, FeatureType.PointType);
                                    curPoint = new PointObject(pID, curVextex);
                                    curPoint.SylWidth = sylSize;
                                    TriNodeList.Add(curVextex);
                                    PointList.Add(curPoint);
                                    vextexID++;
                                    //pID++;
                                }

                            }
                            #endregion
                            break;
                        }
                    case esriGeometryType.esriGeometryPolyline:
                        {
                            #region 线要素
                            cursor = curLyr.Search(null, false);
                            curFeature = null;
                            shp = null;
                            while ((curFeature = cursor.NextFeature()) != null)
                            {
                                shp = curFeature.Shape;
                                IPolyline polyline = null;
                                IGeometryCollection pathSet = null;
                                int indexofType = curFeature.Fields.FindField("Type");
                                int typeID = 1;
                                if (curFeature.get_Value(indexofType) != null)
                                {
                                    typeID = (Int16)(curFeature.get_Value(indexofType));
                                }
                                //几何图形
                                if (shp.GeometryType == esriGeometryType.esriGeometryPolyline)
                                {
                                    polyline = shp as IPolyline;
                                    //ID
                                    int index = curFeature.Fields.FindField("ID");
                                    pID = (int)(curFeature.get_Value(index));

                                    pathSet = polyline as IGeometryCollection;
                                    int count = pathSet.GeometryCount;
                                    //Path对象
                                    IPath curPath = null;
                                    for (int i = 0; i < count; i++)
                                    {
                                        PolylineObject curPL = null;                      //当前道路
                                        TriNode curVextex = null;                  //当前关联点
                                        List<TriNode> curPointList = new List<TriNode>();
                                        double curX;
                                        double curY;

                                        curPath = pathSet.get_Geometry(i) as IPath;
                                        IPointCollection pointSet = curPath as IPointCollection;
                                        int pointCount = pointSet.PointCount;
                                        if (pointCount >= 2)
                                        {
                                            curX = pointSet.get_Point(0).X;
                                            curY = pointSet.get_Point(0).Y;
                                            TriNode cNode = ConNode.GetContainNode(ConNodeList, TriNodeList, curX, curY);
                                            if (cNode == null)   //该关联点还未加入的情况
                                            {
                                                curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                                TriNodeList.Add(curVextex);
                                                ConNode curNode = new ConNode(vextexID, 0.2f, curVextex);
                                                ConNodeList.Add(curNode);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }
                                            else //该关联点已经加入的情况
                                            {
                                                curPointList.Add(cNode);
                                                cNode.TagValue = -1;
                                                cNode.FeatureType = FeatureType.ConnNode;
                                            }
                                            //加入中间顶点
                                            for (int j = 1; j < pointCount - 1; j++)
                                            {
                                                curX = pointSet.get_Point(j).X;
                                                curY = pointSet.get_Point(j).Y;
                                                curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                                TriNodeList.Add(curVextex);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }
                                            //加入终点
                                            curX = pointSet.get_Point(pointCount - 1).X;
                                            curY = pointSet.get_Point(pointCount - 1).Y;
                                            cNode = ConNode.GetContainNode(ConNodeList, TriNodeList, curX, curY);
                                            if (cNode == null)   //该关联点还未加入的情况
                                            {
                                                curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                                                TriNodeList.Add(curVextex);

                                                ConNode curNode = new ConNode(vextexID, -1f, curVextex);
                                                ConNodeList.Add(curNode);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }
                                            else //该关联点已经加入的情况
                                            {
                                                curPointList.Add(cNode);
                                                cNode.TagValue = -1;
                                                cNode.FeatureType = FeatureType.ConnNode;
                                            }

                                            //添加起点
                                            curPL = new PolylineObject(plID, curPointList, sylSize);
                                            curPL.TypeID = typeID;
                                            PolylineList.Add(curPL);
                                            //plID++;
                                        }
                                    }
                                }
                            }

                            #endregion
                            break;
                        }

                    case esriGeometryType.esriGeometryPolygon:
                        {
                            #region 面要素
                            cursor = curLyr.Search(null, false);
                            curFeature = null;
                            shp = null;
                            while ((curFeature = cursor.NextFeature()) != null)
                            {
                                shp = curFeature.Shape;
                                IPolygon polygon = null;
                                IGeometryCollection pathSet = null;
                                //几何图形
                                if (shp.GeometryType == esriGeometryType.esriGeometryPolygon)
                                {
                                    //ID
                                    int index = curFeature.Fields.FindField("ID");
                                    pID = (int)(curFeature.get_Value(index));

                                    polygon = shp as IPolygon;
                                    pathSet = polygon as IGeometryCollection;
                                    int count = pathSet.GeometryCount;
                                    //Path对象
                                    IPath curPath = null;
                                    for (int i = 0; i < count; i++)
                                    {
                                        PolygonObject curPP = null;                      //当前道路
                                        TriNode curVextex = null;                  //当前关联点
                                        List<TriNode> curPointList = new List<TriNode>();
                                        double curX;
                                        double curY;
                                        curPath = pathSet.get_Geometry(i) as IPath;
                                        IPointCollection pointSet = curPath as IPointCollection;
                                        int pointCount = pointSet.PointCount;
                                        if (pointCount >= 3)
                                        {
                                            //ArcGIS中将起点和终点重复存储
                                            for (int j = 0; j < pointCount - 1; j++)
                                            {
                                                //添加起点
                                                curX = pointSet.get_Point(j).X;
                                                curY = pointSet.get_Point(j).Y;
                                                curVextex = new TriNode(curX, curY, vextexID, ppID, FeatureType.PolygonType);
                                                TriNodeList.Add(curVextex);
                                                curPointList.Add(curVextex);
                                                vextexID++;
                                            }

                                            //添加起点
                                            curPP = new PolygonObject(ppID, curPointList);
                                            curPP.SylWidth = sylSize;
                                            this.PolygonList.Add(curPP);
                                           // ppID++;
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// 加密顶点（点存在偏移）
        /// </summary>
        /// <param name="k">加密系数，平均距离的多少倍</param>
        public void InterpretatePoint(int k)
        {
            AuxStructureLib.Interpretation Inter = new AuxStructureLib.Interpretation(this.PolylineList, this.PolygonList, this.TriNodeList);
            Inter.Interpretate(k);
            this.PolylineList = Inter.PLList;
            this.PolygonList = Inter.PPList;
        }

        /// <summary>
        /// 加密顶点（点存在偏移）
        /// </summary>
        /// <param name="k">加密系数，平均距离的多少倍</param>
        public void InterpretatePoint2(int k)
        {
            AuxStructureLib.Interpretation Inter = new AuxStructureLib.Interpretation(this.PolylineList, this.PolygonList, this.TriNodeList);
            Inter.Interpretate2(k);
            this.PolylineList = Inter.PLList;
            this.PolygonList = Inter.PPList;
        }
        /// <summary>
        /// 获取地图对象
        /// </summary>
        /// <param name="ID">ID</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public MapObject GetObjectbyID(int ID, FeatureType type)
        {
            if (type == FeatureType.PointType)
            {
                return PointObject.GetPPbyID(this.PointList, ID);
            }
            else if (type == FeatureType.PolylineType)
            {
                return PolylineObject.GetPLbyID(this.PolylineList, ID);
            }
            else if (type == FeatureType.PolygonType)
            {
                return PolygonObject.GetPPbyID(this.PolygonList, ID);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 通过给定重心位置返回建筑物
        /// </summary>
        /// <param name="PPList"></param>
        /// <param name="Dx"></param>
        /// <param name="Dy"></param>
        /// <returns></returns>
        public PolygonObject GetPPbyCenter(double Dx, double Dy)
        {
            foreach (PolygonObject curPP in this.PolygonList)
            {
                ProxiNode curPn = curPP.CalProxiNode();

                double Test1 = Math.Abs(curPn.X - Dx);
                double Test2 = Math.Abs(curPn.Y - Dy);

                if(Math.Abs(curPn.X-Dx)<0.1 && Math.Abs(curPn.Y-Dy)<0.1)
                    return curPP;
            }
            return null;
        }

        /// <summary>
        /// 判断是否约束边，如果是返回TagValue 和 类型FeatureType
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        public int GetConsEdge(TriNode p1, TriNode p2, out FeatureType featureType)
        {
            featureType=FeatureType.Unknown;
            //线
            foreach (PolylineObject polyline in this.PolylineList)
            {
                for (int i = 0; i < polyline.PointList.Count - 1; i++)
                {
                    if((p1== polyline.PointList[i]&& p2==polyline.PointList[i+1])
                        ||(p2== polyline.PointList[i]&& p1==polyline.PointList[i+1]))
                    {
                        featureType=FeatureType.PolylineType;
                        return polyline.ID;
                       
                    }
                }
            }
            //面
            foreach (PolygonObject polygon in this.PolygonList)
            {
                for (int i = 0; i < polygon.PointList.Count - 1; i++)
                {
                    if((p1== polygon.PointList[i]&& p2==polygon.PointList[i+1])
                        ||(p2== polygon.PointList[i]&& p1==polygon.PointList[i+1]))
                    {
                        featureType=FeatureType.PolygonType;
                        return polygon.ID;
                       
                    }
                }
                 if((p1== polygon.PointList[polygon.PointList.Count - 1]&& p2==polygon.PointList[0])
                        ||(p2== polygon.PointList[0]&& p1==polygon.PointList[polygon.PointList.Count - 1]))
                    {
                        featureType=FeatureType.PolygonType;
                        return polygon.ID;                      
                    }
            }

            return -1;

        }
        /// <summary>
        /// 将结果写入SHP
        /// </summary>
        /// <param name="filePath">目录</param>
        /// <param name="prj">投影</param>
        public void WriteResult2Shp(string filePath, ISpatialReference pSpatialReference)
        {
            if(this.TriNodeList!=null&&this.TriNodeList.Count>0)
            {
                TriNode.Create_WriteVetex2Shp(filePath, @"Vertices", this.TriNodeList, pSpatialReference);
            }
            if (this.PointList != null && this.PointList.Count > 0)
            {
                this.Create_WritePointObject2Shp(filePath, @"PointObjecrt", pSpatialReference);
            }
            if (this.PolylineList != null)
            {
                this.Create_WritePolylineObject2Shp(filePath, @"PolylineObjecrt", pSpatialReference);
            }
            if (this.PolygonList != null && this.PolygonList.Count > 0)
            {
                this.Create_WritePolygonObject2Shp(filePath, @"PolygonObjecrt", pSpatialReference);
            }
        }

        /// <summary>
        /// 将结果写入SHP
        /// </summary>
        /// <param name="filePath">目录</param>
        /// <param name="prj">投影</param>
        /// Type=0 非smooth；Type=1smooth Type=2 GridConn
        public void WriteResult2Shp(string filePath, ISpatialReference pSpatialReference,int Type)
        {
            if (this.TriNodeList != null && this.TriNodeList.Count > 0)
            {
                TriNode.Create_WriteVetex2Shp(filePath, @"Vertices", this.TriNodeList, pSpatialReference);
            }
            if (this.PointList != null && this.PointList.Count > 0)
            {
                this.Create_WritePointObject2Shp(filePath, @"PointObjecrt", pSpatialReference);
            }
            if (this.PolylineList != null&& Type==0)
            {
                this.Create_WritePolylineObject2Shp(filePath, @"PointPolylineObjecrt", pSpatialReference);
            }
            if (this.PolylineList != null && Type == 1)
            {
                this.Create_WritePolylineObject2Shp(filePath, @"SmoothPolylineObjecrt", pSpatialReference);
            }
            if (this.PolylineList != null && Type == 2)
            {
                this.Create_WritePolylineObject2Shp(filePath, @"GridPolylineObjecrt", pSpatialReference);
            }
            if (this.PolylineList != null && Type == 3)
            {
                this.Create_WritePolylineObject2Shp(filePath, @"Edges", pSpatialReference);
            }
            if (this.PolygonList != null && this.PolygonList.Count > 0)
            {
                this.Create_WritePolygonObject2Shp(filePath, @"PolygonObjecrt", pSpatialReference);
            }
        }

        /// <summary>
        /// 将结果写入SHP
        /// </summary>
        /// <param name="filePath">目录</param>
        /// <param name="prj">投影</param>
        public void WriteResult3Shp(string filePath, ISpatialReference pSpatialReference)
        {          
            if (this.PolygonList != null)
            {
                this.Create_WritePolygonObject3Shp(filePath, @"PolygonObjecrt", pSpatialReference);
            }
        }

        /// <summary>
        /// 输出建筑物
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="pSpatialReference"></param>
        public void WriteResultPolygon(string filePath, ISpatialReference pSpatialReference)
        {
            if (this.PolygonList != null)
            {
                this.Create_WritePolygonObject2Shp(filePath, @"PolygonObjecrt", pSpatialReference);
            }
        }
        /// <summary>
        /// 创建要素
        /// </summary>
        public void Export2FeatureClasses(out IFeatureClass pPointFeatClass, 
            out IFeatureClass pPolylineFeatClass, 
            out IFeatureClass pPolygonFeatClass)
        {
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            #region 点
            pPointFeatClass = new FeatureCacheClass() as IFeatureClass;
            IFeatureClassWrite pPointFeatClassW = pPointFeatClass as IFeatureClassWrite;
            // List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
            if (this.PointList != null && this.PointList.Count != 0)
            {
                int n = this.PointList.Count;
                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pPointFeatClass.CreateFeature();
                    IGeometry shp = new PointClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    //IPoint curResultPoint = null;
                    TriNode curPoint = null;
                    if (TriNodeList[i] == null)
                        continue;
                    curPoint = this.PointList[i].Point; ;
                    ((PointClass)shp).PutCoords(curPoint.X, curPoint.Y);
                    feature.Shape = shp;
                    feature.set_Value(2, this.PointList[i].ID);
                    feature.Store();//保存IFeature对象  
                    pPointFeatClassW.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                 
                }
            }
            #endregion

            #region 线
            pPolylineFeatClass = new FeatureCacheClass() as IFeatureClass;
            IFeatureClassWrite pPolylineFeatClassW= pPolylineFeatClass as IFeatureClassWrite;
            // List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
            if (this.PolylineList != null && this.PolylineList.Count != 0)
            {

                int n = this.PolylineList.Count;
                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pPolylineFeatClass.CreateFeature();
                    IGeometry shp = new PolylineClass();
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    TriNode curPoint = null;
                    if (this.PolylineList[i] == null)
                        continue;
                    int m = this.PolylineList[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = this.PolylineList[i].PointList[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    feature.Shape = shp;
                    feature.set_Value(2, this.PolylineList[i].ID);//编号 


                    feature.Store();//保存IFeature对象  
                    pPolylineFeatClassW.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
                }
            }
            #endregion

            #region 面
            pPolygonFeatClass = new FeatureCacheClass() as IFeatureClass;
            IFeatureClassWrite   pPolygonFeatClassW=pPolygonFeatClass as IFeatureClassWrite;
            // List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
            if (this.PolygonList != null && this.PolygonList.Count != 0)
            {

                int n = this.PolygonList.Count;
                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pPolygonFeatClass.CreateFeature();
                    IGeometry shp = new PolygonClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    TriNode curPoint = null;
                    if (this.PolygonList[i] == null)
                        continue;
                    int m = this.PolygonList[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = this.PolygonList[i].PointList[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    curPoint = this.PolygonList[i].PointList[0];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    feature.Shape = shp;
                    feature.Store();//保存IFeature对象  
                    pPolygonFeatClassW.WriteFeature(feature);
                }
            }
            #endregion
        }

        /// <summary>
        /// 将面写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">线列表</param>
        public void Create_WritePolygonObject2Shp(string filePath, string fileName, ISpatialReference pSpatialReference)
        {
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);
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
            pFieldEdit2.Name_2 = "SourceTemp";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField2);

            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit3.Name_2 = "TargetTemp";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeInteger;
            pFieldsEdit.AddField(pField3);
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

                int n = this.PolygonList.Count;
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
                    if (this.PolygonList[i] == null)
                        continue;
                    int m = this.PolygonList[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = this.PolygonList[i].PointList[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    curPoint = this.PolygonList[i].PointList[0];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    feature.Shape = shp;
                    feature.set_Value(2, this.PolygonList[i].ID);//编号 
                    feature.set_Value(3, this.PolygonList[i].SourceTemp);
                    feature.set_Value(4, this.PolygonList[i].TargetTemp);

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
        /// 将面写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">线列表</param>
        public void Create_WritePolygonObject3Shp(string filePath, string fileName, ISpatialReference pSpatialReference)
        {
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);
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

            IField pField3;
            IFieldEdit pFieldEdit3;
            pField3 = new FieldClass();
            pFieldEdit3 = pField3 as IFieldEdit;
            pFieldEdit3.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit3.Name_2 = "AveArea";
            pFieldEdit3.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField3);

            IField pField4;
            IFieldEdit pFieldEdit4;
            pField4 = new FieldClass();
            pFieldEdit4 = pField4 as IFieldEdit;
            pFieldEdit4.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit4.Name_2 = "AreaDif";
            pFieldEdit4.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField4);

            IField pField5;
            IFieldEdit pFieldEdit5;
            pField5 = new FieldClass();
            pFieldEdit5 = pField5 as IFieldEdit;
            pFieldEdit5.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit5.Name_2 = "VarADif";
            pFieldEdit5.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField5);

            IField pField6;
            IFieldEdit pFieldEdit6;
            pField6 = new FieldClass();
            pFieldEdit6 = pField6 as IFieldEdit;
            pFieldEdit6.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit6.Name_2 = "BWR";
            pFieldEdit6.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField6);

            IField pField7;
            IFieldEdit pFieldEdit7;
            pField7 = new FieldClass();
            pFieldEdit7 = pField7 as IFieldEdit;
            pFieldEdit7.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit7.Name_2 = "smbrR";
            pFieldEdit7.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField7);

            IField pField8;
            IFieldEdit pFieldEdit8;
            pField8 = new FieldClass();
            pFieldEdit8 = pField8 as IFieldEdit;
            pFieldEdit8.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit8.Name_2 = "AveDis";
            pFieldEdit8.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField8);

            IField pField9;
            IFieldEdit pFieldEdit9;
            pField9 = new FieldClass();
            pFieldEdit9 = pField9 as IFieldEdit;
            pFieldEdit9.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit9.Name_2 = "DisDif";
            pFieldEdit9.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField9);

            IField pField10;
            IFieldEdit pFieldEdit10;
            pField10 = new FieldClass();
            pFieldEdit10 = pField10 as IFieldEdit;
            pFieldEdit10.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit10.Name_2 = "VarDisD";
            pFieldEdit10.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField10);

            IField pField11;
            IFieldEdit pFieldEdit11;
            pField11 = new FieldClass();
            pFieldEdit11 = pField11 as IFieldEdit;
            pFieldEdit11.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit11.Name_2 = "EcDif";
            pFieldEdit11.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField11);

            IField pField12;
            IFieldEdit pFieldEdit12;
            pField12 = new FieldClass();
            pFieldEdit12 = pField12 as IFieldEdit;
            pFieldEdit12.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit12.Name_2 = "VarEcD";
            pFieldEdit12.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField12);

            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit2.Name_2 = "ICAve";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField2);

            IField pField13;
            IFieldEdit pFieldEdit13;
            pField13 = new FieldClass();
            pFieldEdit13 = pField13 as IFieldEdit;
            pFieldEdit13.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit13.Name_2 = "ICd";
            pFieldEdit13.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField13);

            IField pField14;
            IFieldEdit pFieldEdit14;
            pField14 = new FieldClass();
            pFieldEdit14 = pField14 as IFieldEdit;
            pFieldEdit14.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit14.Name_2 = "VarICD";
            pFieldEdit14.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField14);

            IField pField15;
            IFieldEdit pFieldEdit15;
            pField15 = new FieldClass();
            pFieldEdit15 = pField15 as IFieldEdit;
            pFieldEdit15.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit15.Name_2 = "RatAve";
            pFieldEdit15.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField15);
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

                int n = this.PolygonList.Count;
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
                    if (this.PolygonList[i] == null)
                        continue;
                    int m = this.PolygonList[i].PointList.Count;

                    for (int k = 0; k < m; k++)
                    {
                        curPoint = this.PolygonList[i].PointList[k];
                        curResultPoint = new PointClass();
                        curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                        pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    }
                    curPoint = this.PolygonList[i].PointList[0];
                    curResultPoint = new PointClass();
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                    feature.Shape = shp;
                    this.DataStore(feature, "AveArea", this.PolygonList[i].AverageArea);
                    this.DataStore(feature, "AreaDif", this.PolygonList[i].AreaDiff);
                    this.DataStore(feature, "VarADif", this.PolygonList[i].VarAreaDiff);
                    this.DataStore(feature, "BWR", this.PolygonList[i].BlackWhiteRatio);
                    this.DataStore(feature, "smbrR", this.PolygonList[i].smbrRatio);
                    this.DataStore(feature, "AveDis", this.PolygonList[i].AverageDistance);
                    this.DataStore(feature, "DisDif", this.PolygonList[i].DistanceDiff);
                    this.DataStore(feature, "VarDisD", this.PolygonList[i].VarDistanceDiff);
                    this.DataStore(feature, "EcDif", this.PolygonList[i].EdgeCountDiff);
                    this.DataStore(feature, "VarEcD",  this.PolygonList[i].VarEdgeCountDiff);
                    this.DataStore(feature, "ICAve", this.PolygonList[i].IPQComAverage);
                    this.DataStore(feature, "ICd", this.PolygonList[i].IPQComDiff);
                    this.DataStore(feature, "VarICD", this.PolygonList[i].AveIPQComDiff);
                    this.DataStore(feature, "RatAve", this.PolygonList[i].RatioAverage);

                    //feature.set_Value((int)pFields.FindField("AveArea"), this.PolygonList[i].AverageArea);
                    //feature.set_Value((int)pFields.FindField("AreaDif"), this.PolygonList[i].AreaDiff);
                    //feature.set_Value((int)pFields.FindField("VarADif"), this.PolygonList[i].VarAreaDiff);
                    //feature.set_Value((int)pFields.FindField("BWR"), this.PolygonList[i].BlackWhiteRatio);
                    //feature.set_Value((int)pFields.FindField("smbrR"), this.PolygonList[i].smbrRatio);
                    //feature.set_Value((int)pFields.FindField("AveDis"), this.PolygonList[i].AverageDistance);
                    //feature.set_Value((int)pFields.FindField("DisDif"), this.PolygonList[i].DistanceDiff);
                    //feature.set_Value((int)pFields.FindField("VarDisD"), this.PolygonList[i].VarDistanceDiff);
                    //feature.set_Value((int)pFields.FindField("EcDif"), this.PolygonList[i].EdgeCountDiff);
                    //feature.set_Value((int)pFields.FindField("VarEcD"), this.PolygonList[i].VarEdgeCountDiff);
                    //feature.set_Value((int)pFields.FindField("ICd"), this.PolygonList[i].IPQComDiff);
                    //feature.set_Value((int)pFields.FindField("VarICD"), this.PolygonList[i].VarIPQComDiff);
                    //feature.set_Value((int)pFields.FindField("RatAve"), this.PolygonList[i].RatioAverage);
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

        #region 将数据存储到字段下
        public void DataStore(IFeature pFeature, string s, object t)
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

        /// <summary>
        /// 将线写入Shp文件+
        /// </summary>
        /// <param name="filePath">文件名</param>
        /// <param name="Skeleton_SegmentList">线列表</param>
        public void Create_WritePolylineObject2Shp(string filePath, string fileName, ISpatialReference pSpatialReference)
        {
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);
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
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
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
            pFieldEdit1.Name_2 = "Width";
            pFieldEdit1.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField1);

            IField pField2;
            IFieldEdit pFieldEdit2;
            pField2 = new FieldClass();
            pFieldEdit2 = pField2 as IFieldEdit;
            pFieldEdit2.Length_2 = 30;//Length_2与Length的区别是一个是只读的，一个是可写的，以下Name_2,Type_2也是一样
            pFieldEdit2.Name_2 = "Volume";
            pFieldEdit2.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField2);
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

                int n = this.PolylineList.Count;
                // List<Skeleton_Segment> Skeleton_SegmentList = ske.Skeleton_SegmentList;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PolylineClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    IPointCollection pointSet = shp as IPointCollection;
                    IPoint curResultPoint = null;
                    TriNode curPoint = null;
                    if (this.PolylineList[i] == null)
                        continue;
                    int m = this.PolylineList[i].PointList.Count;

                    Dictionary<Tuple<double, double>, int> PointDic = new Dictionary<Tuple<double, double>, int>();
                    for (int k = 0; k < m; k++)
                    {
                        curPoint = this.PolylineList[i].PointList[k];

                        Tuple<double, double> PointXY = new Tuple<double, double>(curPoint.X, curPoint.Y);
                        if (!PointDic.ContainsKey(PointXY))
                        {
                            curResultPoint = new PointClass();
                            curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                            pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
                        }
                    }
                    feature.Shape = shp;
                    feature.set_Value(2, this.PolylineList[i].SylWidth);//编号 
                    feature.set_Value(3, this.PolylineList[i].Volume);//编号 

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
        /// 将点写入Shp
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="TriEdgeList"></param>
        /// <param name="prj"></param>
        public void Create_WritePointObject2Shp(string filePath, string fileName, ISpatialReference pSpatialReference)
        {
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);
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
            pGeomDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPoint;
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
            #endregion

            #region 创建要素类
            IFeatureClass pFeatClass;
            pFeatClass = pFWS.CreateFeatureClass(strName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
            #endregion
            #endregion

            #region 添加要素

            //IFeatureClass featureClass = null;
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

                int n = this.PointList.Count;
                if (n == 0)
                    return;

                for (int i = 0; i < n; i++)
                {
                    IFeature feature = pFeatClass.CreateFeature();
                    IGeometry shp = new PointClass();
                    // shp.SpatialReference = mapControl.SpatialReference;
                    //IPointCollection pointSet = shp as IPointCollection;
                    //IPoint curResultPoint = null;
                    TriNode curPoint = null;
                    //if (TriNodeList[i] == null)
                    //    continue;

                    curPoint = this.PointList[i].Point; ;
                    ((PointClass)shp).PutCoords(curPoint.X, curPoint.Y);

                    feature.Shape = shp;
                    feature.set_Value(2, this.PointList[i].ID);

                    feature.Store();//保存IFeature对象  
                    fr.WriteFeature(feature);//将IFeature对象，添加到当前图层上     
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
        /// 输出冲突到TXT文件
        /// </summary>
        /// <param name="strPath"></param>
        /// <param name="iteraID"></param>
        public void OutputConflictCount(string strPath,string fileName)
        {
            DataSet ds = new DataSet();
            //创建一个表
            DataTable tableforce = new DataTable();
            tableforce.TableName = "ConflictsCount";
            tableforce.Columns.Add("ID", typeof(int));
            tableforce.Columns.Add("Type", typeof(string));
            tableforce.Columns.Add("Count", typeof(int));
            if (this.PointList != null && PointList.Count != 0)
            {
                foreach (PointObject p in this.PointList)
                {
                    DataRow dr = tableforce.NewRow();
                    dr[0] = p.ID;
                    dr[1] = "Point";
                    dr[2] = p.ConflictCount;
                    tableforce.Rows.Add(dr);
                }
            }
            if (this.PointList != null && PointList.Count != 0)
            {
                foreach (PointObject p in this.PointList)
                {
                    DataRow dr = tableforce.NewRow();
                    dr[0] = p.ID;
                    dr[1] = "Point";
                    dr[2] = p.ConflictCount;
                    tableforce.Rows.Add(dr);
                }
            }
            if (this.PolylineList != null && PolylineList.Count != 0)
            {
                foreach (PolylineObject l in this.PolylineList)
                {
                    DataRow dr = tableforce.NewRow();
                    dr[0] = l.ID;
                    dr[1] = "Line";
                    dr[2] = l.ConflictCount;
                    tableforce.Rows.Add(dr);
                }
            }
            if (this.PolygonList != null && PolygonList.Count != 0)
            {
                foreach (PolygonObject pp in this.PolygonList)
                {
                    DataRow dr = tableforce.NewRow();
                    dr[0] = pp.ID;
                    dr[1] = "Polygon";
                    dr[2] = pp.ConflictCount;
                    tableforce.Rows.Add(dr);
                }
            }
           
            TXTHelper.ExportToTxt(tableforce, strPath + @"\"+fileName);
        }
        /// <summary>
        /// 提取在缓冲区多边形内部的额子网
        /// </summary>
        /// <param name="bufferArea">缓冲区</param>
        public void GetSubNetWorkbyBufferArea(IPolygon bufferArea)
        {
            IRelationalOperator iro = bufferArea as IRelationalOperator;
            foreach(PolylineObject l in this.PolylineList)
            {
                List<TriNode> rList = new List<TriNode>();

                foreach (TriNode p in l.PointList)
                {
                    IPoint point = new PointClass();
                    point.PutCoords(p.X, p.Y);
                    if (!iro.Contains(point))
                    {
                        rList.Add(p);
                    }
                }
                foreach (TriNode p in rList)
                {
                    l.PointList.Remove(p);
                    this.TriNodeList.Remove(p);
                }
            }
        }

        /// <summary>
        /// 提取在缓冲区多边形内部的额子网
        /// </summary>
        /// <param name="bufferArea">缓冲区</param>
        public SMap GetSubNetWorkbyBufferAreaClip(IPolygon bufferArea)
        {
            SMap map = new SMap();
            map.PolylineList = new List<PolylineObject>();
            map.ConNodeList = new List<ConNode>();
            map.TriNodeList = new List<TriNode>();
            int vextexID = 0;
            int plID = 0;

            List<PolylineObject> esriPolylineList = new List<PolylineObject>();
            int id = 0;
            foreach (PolylineObject l in this.PolylineList)
            {
                int typeID = l.TypeID;
                //if (l.ID == -200)
                //{

                //    int error = 0;
                //}
                double sylSize = l.SylWidth;
                IPolyline esriPolyline = l.ToEsriPolyline();
                ITopologicalOperator ito = esriPolyline as ITopologicalOperator;
                IGeometry geo = ito.Intersect(bufferArea, esriGeometryDimension.esriGeometry1Dimension);
                if (geo != null)
                {
                    IGeometryCollection pathSet = geo as IGeometryCollection;
                    if (pathSet.GeometryCount > 0)
                    {
                        if (l.ID == -200)
                        {
                            this.ProxiEdge2PolylineObjects(ref vextexID, ref plID, map, l, typeID, sylSize);
                        }
                        else
                        {
                            Paths2PolylineObjects(ref vextexID, ref plID, map, pathSet, typeID, sylSize);
                        }
                    }
                }
            }
            DetermineBoundaryPoints(bufferArea);
            return map;
        }

        /// <summary>
        /// 提取在缓冲区多边形内部的额子网
        /// </summary>
        /// <param name="bufferArea">缓冲区</param>
        public SMap GetSubNetWorkbyBufferAreaClip(IPolygon bufferArea,SMap oMap)
        {
            SMap map = new SMap();
            map.PolylineList = new List<PolylineObject>();
            map.ConNodeList = new List<ConNode>();
            map.TriNodeList = new List<TriNode>();
            int vextexID = 0;
            int plID = 0;

            List<PolylineObject> esriPolylineList = new List<PolylineObject>();
            int id = 0;
            foreach (PolylineObject l in this.PolylineList)
            {
                int typeID = l.TypeID;
                

                double sylSize = l.SylWidth;
                IPolyline esriPolyline = l.ToEsriPolyline();
                ITopologicalOperator ito = esriPolyline as ITopologicalOperator;
                IGeometry geo = ito.Intersect(bufferArea, esriGeometryDimension.esriGeometry1Dimension);
                if (geo != null)
                {

                    IGeometryCollection pathSet = geo as IGeometryCollection;
                    if (pathSet.GeometryCount > 0)
                    {
                        if (l.ID == -200)
                        {
                            ProxiEdge2PolylineObjects(ref vextexID, ref plID, map, oMap, l, typeID, sylSize);
                        }
                        else
                        {
                            Paths2PolylineObjects(ref vextexID, ref plID, map, oMap, pathSet, typeID, sylSize);
                        }
                    }
                }
            }
          //  DetermineBoundaryPoints(bufferArea);
            return map;
        }

        /// <summary>
        /// 从PathSet中读取坐标构建道路网地图
        /// </summary>
        public void ProxiEdge2PolylineObjects(
            ref int vextexID,
            ref int plID, 
            SMap map,
            PolylineObject l, 
            int typeID, 
            double sylSize)
        {
            int count = l.PointList.Count;
            if (count <= 1)
                return;
     
                PolylineObject curPL = null; //当前道路
                TriNode curVextex = null; //当前关联点
                List<TriNode> curPointList = new List<TriNode>();
                double curX;
                double curY;


       
                    curX = l.PointList[0].X;
                    curY =  l.PointList[0].Y;
                    TriNode cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        map.TriNodeList.Add(curVextex);
                        ConNode curNode = new ConNode(vextexID, 0.2f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }
                    //加入中间顶点
                    for (int j = 1; j < l.PointList.Count - 1; j++)
                    {
                        curX = l.PointList[j].X;
                        curY = l.PointList[j].Y;
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        map.TriNodeList.Add(curVextex);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    //加入终点
                    curX = l.PointList[l.PointList.Count- 1].X;
                     curY = l.PointList[l.PointList.Count- 1].Y;
                    cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        map.TriNodeList.Add(curVextex);

                        ConNode curNode = new ConNode(vextexID,-1f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }

                    //添加起点
                    curPL = new PolylineObject(plID, curPointList, sylSize);
                    curPL.TypeID = typeID;
                    map.PolylineList.Add(curPL);
                    plID++;

        }


        /// <summary>
        /// 从PathSet中读取坐标构建道路网地图
        /// </summary>
        public void Paths2PolylineObjects(
            ref int vextexID,
            ref int plID,
            SMap map,
            IGeometryCollection pathSet,
            int typeID,
            double sylSize)
        {
            int count = pathSet.GeometryCount;
            //Path对象
            IPath curPath = null;
            for (int i = 0; i < count; i++)
            {
                PolylineObject curPL = null; //当前道路
                TriNode curVextex = null; //当前关联点
                List<TriNode> curPointList = new List<TriNode>();
                double curX;
                double curY;

                curPath = pathSet.get_Geometry(i) as IPath;
                IPointCollection pointSet = curPath as IPointCollection;
                int pointCount = pointSet.PointCount;
                if (pointCount >= 2)
                {
                    curX = pointSet.get_Point(0).X;
                    curY = pointSet.get_Point(0).Y;
                    TriNode cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        map.TriNodeList.Add(curVextex);
                        ConNode curNode = new ConNode(vextexID, 0.2f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }
                    //加入中间顶点
                    for (int j = 1; j < pointCount - 1; j++)
                    {
                        curX = pointSet.get_Point(j).X;
                        curY = pointSet.get_Point(j).Y;
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        map.TriNodeList.Add(curVextex);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    //加入终点
                    curX = pointSet.get_Point(pointCount - 1).X;
                    curY = pointSet.get_Point(pointCount - 1).Y;
                    cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        map.TriNodeList.Add(curVextex);

                        ConNode curNode = new ConNode(vextexID, -1f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }

                    //添加起点
                    curPL = new PolylineObject(plID, curPointList, sylSize);
                    curPL.TypeID = typeID;
                    map.PolylineList.Add(curPL);
                    plID++;
                }
            }
        }

        /// <summary>
        /// 从PathSet中读取坐标构建道路网地图
        /// </summary>
        public void Paths2PolylineObjects(
            ref int vextexID,
            ref int plID,
            SMap map,
            SMap oMap,
            IGeometryCollection pathSet,
            int typeID,
            double sylSize)
        {
            int count = pathSet.GeometryCount;
            //Path对象
            IPath curPath = null;
            for (int i = 0; i < count; i++)
            {
                PolylineObject curPL = null; //当前道路
                TriNode curVextex = null; //当前关联点
                List<TriNode> curPointList = new List<TriNode>();
                double curX;
                double curY;

                curPath = pathSet.get_Geometry(i) as IPath;
                IPointCollection pointSet = curPath as IPointCollection;
                int pointCount = pointSet.PointCount;
                if (pointCount >= 2)
                {
                    curX = pointSet.get_Point(0).X;
                    curY = pointSet.get_Point(0).Y;
                    TriNode cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        TriNode oNode = ConNode.GetContainNode(oMap.TriNodeList, curX, curY);
                        if (oNode != null)
                        {
                            curVextex.SomeValue = oNode.ID;

                            if (oNode.FeatureType == FeatureType.PointType)
                            {
                                curVextex.SomeValue1 = 1;
                                curVextex.TagValue = oNode.TagValue;
                            }
                            else if (oNode.FeatureType == FeatureType.PolygonType)
                            {
                                curVextex.SomeValue1 = 2;
                                curVextex.TagValue = oNode.TagValue;
                            }
                        }
                        map.TriNodeList.Add(curVextex);
                        ConNode curNode = new ConNode(vextexID, 0.2f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }
                    //加入中间顶点
                    for (int j = 1; j < pointCount - 1; j++)
                    {
                        curX = pointSet.get_Point(j).X;
                        curY = pointSet.get_Point(j).Y;
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        
                        TriNode oNode = ConNode.GetContainNode(oMap.TriNodeList, curX, curY);
                        if (oNode != null)
                        {
                            curVextex.SomeValue = oNode.ID;

                            if (oNode.FeatureType == FeatureType.PointType)
                            {
                                curVextex.SomeValue1 = 1;
                                curVextex.TagValue = oNode.TagValue;
                            }
                            else if (oNode.FeatureType == FeatureType.PolygonType)
                            {
                                curVextex.SomeValue1 = 2;
                                curVextex.TagValue = oNode.TagValue;
                            }
                        }

                        map.TriNodeList.Add(curVextex);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    //加入终点
                    curX = pointSet.get_Point(pointCount - 1).X;
                    curY = pointSet.get_Point(pointCount - 1).Y;
                    cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        
                        TriNode oNode = ConNode.GetContainNode(oMap.TriNodeList, curX, curY);
                        if (oNode != null)
                        {
                            curVextex.SomeValue = oNode.ID;

                            if (oNode.FeatureType == FeatureType.PointType)
                            {
                                curVextex.SomeValue1 = 1;
                                curVextex.TagValue = oNode.TagValue;
                            }
                            else if (oNode.FeatureType == FeatureType.PolygonType)
                            {
                                curVextex.SomeValue1 = 2;
                                curVextex.TagValue = oNode.TagValue;
                            }
                        }
                        map.TriNodeList.Add(curVextex);

                        ConNode curNode = new ConNode(vextexID, -1f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }

                    //添加起点
                    curPL = new PolylineObject(plID, curPointList, sylSize);
                    curPL.TypeID = typeID;
                    map.PolylineList.Add(curPL);
                    plID++;
                }
            }
        }

        /// <summary>
        /// 从PathSet中读取坐标构建道路网地图
        /// </summary>
        public void ProxiEdge2PolylineObjects(
            ref int vextexID,
            ref int plID,
            SMap map,
            SMap oMap,
            PolylineObject l, 
            int typeID,
            double sylSize)
        {
            int count = l.PointList.Count;
            if (count < 2)
                return;
           
                PolylineObject curPL = null; //当前道路
                TriNode curVextex = null; //当前关联点
                List<TriNode> curPointList = new List<TriNode>();
                double curX;
                double curY;


                    curX = l.PointList[0].X;
                    curY =l.PointList[0].Y;
                    TriNode cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);
                        TriNode oNode = ConNode.GetContainNode(oMap.TriNodeList, curX, curY);
                        if (oNode != null)
                        {
                            curVextex.SomeValue = oNode.ID;

                            if (oNode.FeatureType == FeatureType.PointType)
                            {
                                curVextex.SomeValue1 = 1;
                                curVextex.TagValue = oNode.TagValue;
                            }
                            else if (oNode.FeatureType == FeatureType.PolygonType)
                            {
                                curVextex.SomeValue1 = 2;
                                curVextex.TagValue = oNode.TagValue;
                            }
                        }
                        map.TriNodeList.Add(curVextex);
                        ConNode curNode = new ConNode(vextexID, 0.2f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }
                    //加入中间顶点
                    for (int j = 1; j < count - 1; j++)
                    {
                        curX = l.PointList[j].X;
                        curY =l.PointList[j].Y;
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);

                        TriNode oNode = ConNode.GetContainNode(oMap.TriNodeList, curX, curY);
                        if (oNode != null)
                        {
                            curVextex.SomeValue = oNode.ID;

                            if (oNode.FeatureType == FeatureType.PointType)
                            {
                                curVextex.SomeValue1 = 1;
                                curVextex.TagValue = oNode.TagValue;
                            }
                            else if (oNode.FeatureType == FeatureType.PolygonType)
                            {
                                curVextex.SomeValue1 = 2;
                                curVextex.TagValue = oNode.TagValue;
                            }
                        }

                        map.TriNodeList.Add(curVextex);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    //加入终点
                    curX = l.PointList[count - 1].X;
                    curY = l.PointList[count - 1].Y;
                    cNode = ConNode.GetContainNode(map.ConNodeList, TriNodeList, curX, curY);
                    if (cNode == null)   //该关联点还未加入的情况
                    {
                        curVextex = new TriNode(curX, curY, vextexID, plID, FeatureType.PolylineType);

                        TriNode oNode = ConNode.GetContainNode(oMap.TriNodeList, curX, curY);
                        if (oNode != null)
                        {
                            curVextex.SomeValue = oNode.ID;

                            if (oNode.FeatureType == FeatureType.PointType)
                            {
                                curVextex.SomeValue1 = 1;
                                curVextex.TagValue = oNode.TagValue;
                            }
                            else if (oNode.FeatureType == FeatureType.PolygonType)
                            {
                                curVextex.SomeValue1 = 2;
                                curVextex.TagValue = oNode.TagValue;
                            }
                        }
                        map.TriNodeList.Add(curVextex);

                        ConNode curNode = new ConNode(vextexID, -1f, curVextex);
                        map.ConNodeList.Add(curNode);
                        curPointList.Add(curVextex);
                        vextexID++;
                    }
                    else //该关联点已经加入的情况
                    {
                        curPointList.Add(cNode);
                        cNode.TagValue = -1;
                        cNode.FeatureType = FeatureType.ConnNode;
                    }

                    //添加起点
                    curPL = new PolylineObject(plID, curPointList, sylSize);
                    curPL.TypeID = typeID;
                    map.PolylineList.Add(curPL);
                    plID++;
        }
        /// <summary>
        /// 确定边界点7-28
        /// </summary>
        /// <param name="buffer">缓冲区</param>
        public void DetermineBoundaryPoints(IPolygon buffer)
        {
            IRelationalOperator iro = buffer as IRelationalOperator;
            foreach(ConNode connode in this.ConNodeList)
            {
                if (connode.Point.TagValue != -1)
                {
                    IPoint curPoint = new PointClass();
                    curPoint.PutCoords(connode.Point.X, connode.Point.Y);
                    if (!iro.Contains(curPoint))
                    {
                        connode.Point.IsBoundaryPoint = true;
                    }
                }

            }
        }

        /// <summary>
        /// 通过坐标获取对应点在Map对象TriNode数组中的索引号
        /// 主要用于获取初始移位点在子图中的坐标索引号
        /// Liuygis:7-25
        /// </summary>
        /// <param name="coords">坐标点</param>
        /// <param name="delta">判断两点等同的距离阈值（e.g.,0.000001f）</param>
        /// <returns>索引号</returns>
        public int GetIndexofVertexbyX_Y(TriNode coords, double delta)
        {
            int n= this.TriNodeList.Count;
            double x = coords.X;
            double y = coords.Y;
            for (int i = 0; i < n; i++)
            {
                TriNode curV = TriNodeList[i];
                if(Math.Abs((1-curV.X/x)) <= delta && Math.Abs((1-curV.Y / y)) <= delta)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 将每个点的移位值写入文本文件
        /// </summary>
        /// <param name="strPath"></param>
        public void WritetodxdytoText(string strPath,string strFileName)
        {
            DataSet ds = new DataSet();
            //创建一个表
            DataTable tableforce = new DataTable();
            tableforce.TableName = @"DisplayStatic";
            tableforce.Columns.Add(@"ID", typeof(int));
            tableforce.Columns.Add(@"dx", typeof(double));
            tableforce.Columns.Add(@"dy", typeof(double));
            tableforce.Columns.Add(@"d", typeof(double));
            tableforce.Columns.Add(@"tagType", typeof(int));
            tableforce.Columns.Add(@"tagID", typeof(int));
            foreach (TriNode  curNode in this.TriNodeList)
            {
                int index = curNode.ID;
                double dx = curNode.dx;
                double dy = curNode.dy;
                double d = Math.Sqrt(dx * dx + dy * dy);
                int tagType = (curNode as TriNode).SomeValue1;
                int tagID = (curNode as TriNode).TagValue;


                    DataRow dr = tableforce.NewRow();
                    dr[0] = index;
                    dr[1] = dx;
                    dr[2] =dy;
                    dr[3] = d;
                    dr[4] = tagType;
                    dr[5] = tagID;
                    tableforce.Rows.Add(dr);
     
            }
            TXTHelper.ExportToTxt(tableforce, strPath + @"\" +strFileName+ @".txt");
        
        }
    }
}

                
         

