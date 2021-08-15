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
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesFile;

namespace PrDispalce.地图要素
{
    public class MapReady
    {
        /// <summary>
        /// 数据列表
        /// </summary>
        private List<IFeatureLayer> lyrList = null;

        /// <summary>
        /// 所有多边形数据
        /// </summary>
        public PolygonLayer PPLayer;
        /// <summary>
        /// 所有线数据
        /// </summary>
        public PolylineLayer PLLayer;
        /// <summary>
        /// 所有多边形重心点和所有线上所有点数据
        /// </summary>
        public PointLayer POLayer;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="layerlist">数据列表</param>
        public MapReady(List<IFeatureLayer> layerlist)
        {
            lyrList = layerlist;
        }
        

        /// <summary>
        /// 数据综合前准备
        /// </summary>
        public void Ready(double miniDist)
        {
            if (lyrList == null || lyrList.Count == 0)
            {
                return;
            }
            //PolylinesRarefy(miniDist);  //线抽稀
            MapRead();  //读数据

        }
        public void Ready()
        {
            if (lyrList == null || lyrList.Count == 0)
            {
                return;
            }
            //PolylinesRarefy(miniDist);  //线抽稀
            MapRead();  //读数据

        }

        #region 线数据抽稀
        /// <summary>
        /// 线抽稀
        /// </summary>
        /// <param name="miniDist">线上两点最小间隔</param>
        private void PolylinesRarefy(double miniDist)
        {
            foreach (IFeatureLayer curLyr in lyrList)
            {
                if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                {
                    IFeatureClass lineclass = curLyr.FeatureClass;
                    IFeatureCursor pcursor = lineclass.Search(null, false);
                    IFeature fea;
                    IPolyline poly; IGeometry geo;
                    while ((fea = pcursor.NextFeature()) != null)
                    {
                        geo = fea.Shape;
                        if (geo.GeometryType == esriGeometryType.esriGeometryPolyline)
                        {
                            poly = geo as IPolyline;
                            IPointCollection coll = poly as IPointCollection;
                            List<IPoint> plist = new List<IPoint>();
                            plist.Add(coll.get_Point(0));
                            IPoint p1, p2;
                            double dist;
                            //将线上除头尾两点之外，线中间两点之间距离大于阈值的点加入点集
                            for (int i = 1; i < coll.PointCount - 1; i++)
                            {
                                p1 = plist[(plist.Count - 1)];   //已加入点集的最后一个点
                                p2 = coll.get_Point(i);         //线上的点
                                dist = Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                                if (dist > miniDist)
                                {
                                    plist.Add(p2);
                                }
                            }
                            p1 = plist[(plist.Count - 1)];    //点集的最后一个点
                            p2 = coll.get_Point((coll.PointCount - 1));     //线上的最后一个点
                            dist = Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                            if (dist <= miniDist)
                            {
                                plist.Remove(p1);    //若点集最后一个点与线最后一点的距离小于阈值
                            }                        //删除点集的最后一个点，加入线的最后一个点
                            plist.Add(p2);

                            IGeometry shp = new PolylineClass();
                            IPointCollection pointSet = shp as IPointCollection;
                            object missing1 = Type.Missing;
                            object missing2 = Type.Missing;
                            for (int i = 0; i < plist.Count; i++)
                            {
                                pointSet.AddPoint(plist[i], missing1, missing2);
                            }
                            fea.Shape = shp;
                            fea.Store();
                        }
                    }
                }
            }
        }
        #endregion

        #region 读数据
        /// <summary>
        /// 从ArcGIS图层中读取地图数据
        /// </summary>
        private void MapRead()
        {
            PPLayer = new PolygonLayer();
            PLLayer = new PolylineLayer();
            POLayer = new PointLayer();
            List<PointObject> PointList = new List<PointObject>();
            List<PolylineObject> PolylineList = new List<PolylineObject>();
            List<PolygonObject> PolygonList = new List<PolygonObject>();

            int pID = 0;//重心点的序号
            int plID = -1;//线要素的编号
            int ppID = -1;

            IFeatureClass polylineClass, polygonClass;
            IFeatureCursor cursor = null;
            IFeature curfeature = null;
            IGeometry shp = null;

            #region 将ArcGIS中数据读成自己所用数据
            foreach (IFeatureLayer curLyr in lyrList)
            {
                #region 线要素
                if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                {
                    polylineClass = curLyr.FeatureClass;
                    cursor = polylineClass.Search(null, false);
                    curfeature = null;
                    shp = null;
                    while ((curfeature = cursor.NextFeature())!= null)
                    {
                        shp = curfeature.Shape;   //要素几何形状
                        IPolyline polyline = null;
                        if (shp.GeometryType == esriGeometryType.esriGeometryPolyline)
                        {
                            plID = curfeature.OID;   //线要素的OID号赋给新建的线要素的PILD
                            polyline = shp as IPolyline;
                            List<TriDot> trilist = new List<TriDot>();
                            //线Polyline的点集
                            IPointCollection pointSet = polyline as IPointCollection;
                            //点集个数
                            int count = pointSet.PointCount;
                            double curX; double curY;
                            TriDot tPoint = null; PointObject curPoint = null;
                            curX = pointSet.get_Point(0).X; curY = pointSet.get_Point(0).Y;
                            //起点
                            tPoint = new TriDot(curX, curY, plID, FeatureType.PolylineType);
                            bool bol = PointObject.Include(PointList, tPoint);//判断点序列中是否包含折线的起点
                            int k = 0;
                            if (bol == false)
                            {
                                curPoint = new PointObject(pID, tPoint);
                                curPoint.nodeID = -1;
                                curPoint.nodeCount = 1;
                                tPoint.pointID = k++;
                                tPoint.polylineID = plID;
                                tPoint.label = "Line";
                                PointList.Add(curPoint);
                                tPoint.ID = pID;//端点编号
                                trilist.Add(tPoint);
                                pID += 1;
                            }
                            else
                            {
                                int t = PointObject.GetIDbyPoint(PointList, tPoint);//得到端点的ID号
                                PointObject po = PointList[t];
                                tPoint.pointID = k++;
                                tPoint.polylineID = plID;
                                tPoint.label = "Line";
                                po.nodeID = 1;
                                po.nodeCount += 1;
                                tPoint.ID = t;
                                trilist.Add(tPoint);
                            }
                            //中间点
                            for (int i = 1; i < count - 1; i++)
                            {
                                curX = pointSet.get_Point(i).X;
                                curY = pointSet.get_Point(i).Y;
                                //初始化每个点对象
                                tPoint = new TriDot(curX, curY, pID, plID, FeatureType.PolylineType);
                                //生成算法中的点对象
                                curPoint = new PointObject(pID, tPoint);
                                tPoint.pointID = k++;
                                tPoint.polylineID = plID;
                                tPoint.label = "Line";
                                curPoint.nodeID = 0;//如果该点不是线段的端点，nodeID就置为0
                                PointList.Add(curPoint);
                                //添加到线数据的点链表
                                trilist.Add(tPoint);
                                pID += 1;
                            }
                            //终点
                            curX = pointSet.get_Point(count - 1).X; curY = pointSet.get_Point(count - 1).Y;
                            tPoint = new TriDot(curX, curY, plID, FeatureType.PolylineType);
                            bool boll = PointObject.Include(PointList, tPoint);
                            //如果点集中不包括该折线的端点，则需要将该点加入
                            if (boll == false)
                            {
                                curPoint = new PointObject(pID, tPoint);
                                curPoint.nodeID = -1;
                                curPoint.nodeCount = 1;
                                tPoint.pointID = k++;
                                tPoint.polylineID = plID;
                                tPoint.label = "Line";
                                PointList.Add(curPoint);
                                tPoint.ID = pID;
                                trilist.Add(tPoint);
                                pID += 1;
                            }
                                //如果点序列链表已经包括了该线段的端点，则不能重复存储
                            else
                            {
                                int t = PointObject.GetIDbyPoint(PointList, tPoint);//得到该端点的ID号
                                PointObject po = PointList[t];
                                po.nodeID = 1;
                                po.nodeCount += 1;//该点被重复存储的次数属性nodeCount
                                tPoint.ID = t;
                                trilist.Add(tPoint);
                            }
                            //生成自己写的线
                            PolylineObject curPolyline = new PolylineObject(plID, trilist);
                            PolylineList.Add(curPolyline);
                        }
                    }
                }
                #endregion
                #region 面要素
                else if (curLyr.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                {
                    polygonClass = curLyr.FeatureClass;
                    cursor = polygonClass.Search(null, false);
                    curfeature = null;
                    shp = null;
                    while ((curfeature = cursor.NextFeature()) != null)
                    {
                        shp = curfeature.Shape;
                        IPolygon polygon = null;
                        if (shp.GeometryType == esriGeometryType.esriGeometryPolygon)
                        {
                            ppID = curfeature.OID;
                            polygon = shp as IPolygon;
                            List<TriDot> trilist = new List<TriDot>();
                            //Polygon的点集
                            IPointCollection pointSet = polygon as IPointCollection;
                            int count = pointSet.PointCount;
                            double curX;
                            double curY;
                            //ArcGIS中，多边形的首尾点重复存储
                            for (int i = 0; i < count; i++)
                            {
                                curX = pointSet.get_Point(i).X;
                                curY = pointSet.get_Point(i).Y;
                                //初始化每个点对象
                                TriDot tPoint = new TriDot(curX, curY, ppID, FeatureType.PolygonType);
                                trilist.Add(tPoint);
                            }
                            //生成自己写的多边形
                            PolygonObject curPolygon = new PolygonObject(ppID, trilist, pID);
                            PolygonList.Add(curPolygon);

                            //添加多边形重心
                            IArea area = polygon as IArea;
                            double x = area.Centroid.X;
                            double y = area.Centroid.Y;
                            TriDot tCenter = new TriDot(x, y, pID, ppID, FeatureType.PolygonType);//将点实例化成自己的点数据格式
                            PointObject curPoint = new PointObject(pID, tCenter);
                            PointList.Add(curPoint);
                            pID += 1;

                            #region 写入建筑物的类型
                            IFields pFields = curfeature.Fields;
                            for (int j = 0; j < pFields.FieldCount; j++)
                            {
                                string s1 = pFields.get_Field(j).Name;
                                if (s1 == "type_1")
                                {
                                    curPolygon.Type = (int)curfeature.get_Value(curfeature.Fields.FindField(s1));                                   
                                }
                            }
                            #endregion
                        }
                    }
                }
                #endregion
            }
            #endregion
            PPLayer.PolygonList = PolygonList; PLLayer.PolylineList = PolylineList; POLayer.PointList = PointList;
        }
        #endregion


    }
}
