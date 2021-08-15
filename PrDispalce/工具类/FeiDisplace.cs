using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Geometry;
using PrDispalce.地图要素;

namespace PrDispalce.工具类
{
    class FeiDisplace
    {
        #region 建筑物与道路冲突移位
        public List<PolygonCluster> building_Road_Displacement(List<PolygonCluster> polyClusterlst, List<PolylineObject> polylineLst, double MiniDis, ISpatialReference sr)
        {
            List<PolygonCluster> mPolygonClusterList = new List<PolygonCluster>();

            for (int i = 0; i < polyClusterlst.Count; i++)
            {
                PolygonCluster mPolygonCluster = polyClusterlst[i];
                for (int j = 0; j < mPolygonCluster.polygonList.Count; j++)
                {
                    int ID1 = 9999999, ID2 = 9999999;//标识道路的编号
                    Displacement Dis = new Displacement();
                    GeoCalculation Gc = new GeoCalculation();
                    PolygonObject mPolygonObject = mPolygonCluster.polygonList[j];//当前建筑物

                    #region 若道路不与道路冲突，则不处理
                    if (mPolygonCluster.polygonList[j].ConflictIDs.Count == 0)
                    {
                        break;
                    }
                    #endregion

                    #region 若建筑物与一条道路冲突移位
                    else if(mPolygonCluster.polygonList[j].ConflictIDs.Count==1)
                    {
                        ID1 = mPolygonCluster.polygonList[j].ConflictIDs[0];//获得道路编号
                        PolylineObject o_polyline = PolylineObject.GetPolylinebyID(polylineLst, ID1);//获取对应的道路
                        PointObject VexPoint = Gc.NearestPoint(mPolygonObject, o_polyline);//距离道路最近的多边形顶点作为多边形移位的参照点
                        PointObject cenPoint = Gc.ReurnNearstPoint(VexPoint, o_polyline);//求垂点作为每次移位的中心点
                        double near_dis = mPolygonObject.GetMiniDistance(o_polyline);//求建筑物与道路的最短距离
                        double displaceDis = MiniDis - near_dis;//计算该多边形的移位量
                        double dy = (((VexPoint.Point.Y - cenPoint.Point.Y) * (near_dis + displaceDis)) / near_dis) + (cenPoint.Point.Y - VexPoint.Point.Y);
                        double dx = (((VexPoint.Point.X - cenPoint.Point.X) * (near_dis + displaceDis)) / near_dis) + (cenPoint.Point.X - VexPoint.Point.X);
                        PolygonObject mPolygon = Dis.MoveSingleFeatures(mPolygonObject, dx, dy);//按照每个多边形的移位位置生成一个多边形 
                        mPolygonCluster.polygonList[j] = mPolygon;
                    }
                    #endregion

                    #region 若建筑物与两条道理冲突移位
                    else if(mPolygonCluster.polygonList[j].ConflictIDs.Count==2)
                    {
                        List<PolylineObject> o_polylinelst = new List<PolylineObject>();
                        ID1 = mPolygonCluster.polygonList[j].ConflictIDs[0];//获得道路编号
                        ID2 = mPolygonCluster.polygonList[j].ConflictIDs[1];
                        PolylineObject o_polyline1 = PolylineObject.GetPolylinebyID(polylineLst, ID1);//获取对应的道路
                        PolylineObject o_polyline2 = PolylineObject.GetPolylinebyID(polylineLst, ID2);
                        o_polylinelst.Add(o_polyline1); o_polylinelst.Add(o_polyline2); 

                        List<double> near_dislst = new List<double>();
                        double near_dis1 = mPolygonObject.GetMiniDistance(o_polyline1);//求建筑物与道路的最短距离
                        double near_dis2 = mPolygonObject.GetMiniDistance(o_polyline2);
                        near_dislst.Add(near_dis1); near_dislst.Add(near_dis2);

                        double Dy = 0.0, Dx = 0.0;
                        List<PolygonObject> polygonList = new List<PolygonObject>();
                        List<double> dylst = new List<double>();
                        List<double> dxlst = new List<double>();
                        //分别计算对应建筑物与道路应该的移位量
                        for (int m = 0; m < o_polylinelst.Count; m++)
                        {
                            PointObject VexPoint = Gc.NearestPoint(mPolygonObject, o_polylinelst[m]);//距离道路最近的多边形顶点作为多边形移位的参照点
                            PointObject cenPoint = Gc.ReurnNearstPoint(VexPoint, o_polylinelst[m]);//求垂点作为每次移位的中心点
                            double displaceDis = MiniDis - near_dislst[m];//计算该多边形的移位量
                            double dy = (((VexPoint.Point.Y - cenPoint.Point.Y) * (near_dislst[m] + displaceDis)) / near_dislst[m]) + (cenPoint.Point.Y - VexPoint.Point.Y);
                            double dx = (((VexPoint.Point.X - cenPoint.Point.X) * (near_dislst[m] + displaceDis)) / near_dislst[m]) + (cenPoint.Point.X - VexPoint.Point.X);
                            dylst.Add(dy);
                            dxlst.Add(dx);
                        }


                        if ((dylst[0] * dylst[1]) < 0)
                        { Dy = dylst[0] + dylst[1]; }
                        else { Dy = Gc.MaxNum(dylst[0], dylst[1]); }
                        if ((dxlst[0] * dxlst[1]) < 0)
                        { Dx = dxlst[0] + dxlst[1]; }
                        else { Dx = Gc.MaxNum(dxlst[0], dxlst[1]); }

                        PolygonObject mPolygon = Dis.MoveSingleFeatures(mPolygonObject, Dx, Dy);//按照每个多边形的移位位置生成一个多边形
                        mPolygonCluster.polygonList[j] = mPolygon;//对于与道路有冲突的聚类群，对其进行移位并保存结果
                    }
                    #endregion
                }

                mPolygonClusterList.Add(mPolygonCluster);
            }

           return mPolygonClusterList;
        }
        #endregion
    }
}
