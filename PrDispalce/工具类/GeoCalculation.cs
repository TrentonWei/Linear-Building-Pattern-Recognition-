using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
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

//比例射线移位力方法计算
namespace PrDispalce.工具类
{
    class GeoCalculation
    {
        IFeatureClass pfeatureclass;

        public GeoCalculation(IFeatureClass featureclass)
        {
            pfeatureclass = featureclass;
        }

        public GeoCalculation()
        {
        }
        #region 考虑面积的移位中心
        public PointObject Get_AreaCenterPoint(List<PolygonObject> polygonLst)
        {
            double sumX = 0.0, sumY = 0.0, sumarea = 0.0;
            int polygonNumCount=polygonLst.Count;
            for (int j = 0;j < polygonNumCount;j++ )
            {
                
                PolygonObject mPolygon = polygonLst[j] ;
                List<TriDot> mTrilst = mPolygon.PointList;
                TriDot cenTdot = mPolygon.CalPolygonCenterPoint(mTrilst);
                PointObject centerPoint = new PointObject(cenTdot.ID,cenTdot);
                double area = mPolygon.GetArea();
                sumX += cenTdot.X * area;
                sumY += cenTdot.Y * area;
                sumarea += area;
            }
            IPoint point1 = new PointClass();
            point1.PutCoords(sumX /sumarea, sumY /sumarea);
            TriDot mTrdt = new TriDot(sumX / sumarea, sumY / sumarea);
            PointObject pObt1 = new PointObject(point1.ID, mTrdt);
            return pObt1;
        }
        #endregion
        #region 不考虑面积的移位中心
        public PointObject Get_noAreaCenterPoint(List<PolygonObject> polygonLst)
        {
            double sumX = 0.0, sumY = 0.0;
            //sumarea = 0.0;
            int polygonNumCount = polygonLst.Count;
            for (int j = 0; j < polygonNumCount; j++)
            {

                PolygonObject mPolygon = polygonLst[j];
                List<TriDot> mTrilst = mPolygon.PointList;
                TriDot cenTdot = mPolygon.CalPolygonCenterPoint(mTrilst);
                PointObject centerPoint = new PointObject(cenTdot.ID, cenTdot);
                sumX += cenTdot.X ;
                sumY += cenTdot.Y ;
                
            }
            IPoint point1 = new PointClass();
            point1.PutCoords(sumX / polygonNumCount, sumY / polygonNumCount);
            TriDot mTrdt = new TriDot(sumX / polygonNumCount, sumY / polygonNumCount);
            PointObject pObt1 = new PointObject(point1.ID, mTrdt);
            return pObt1;
        }
        #endregion
        #region  计算对象欧几何距离
        public static double getDistance(PolygonObject dpA, PolygonObject dpB)
        {
            double distance = 0;
            IArea mAeaA = dpA as IArea;
            IArea mAeaB = dpB as IArea;
            distance = Math.Sqrt((mAeaA.Centroid.X - mAeaB.Centroid.X) * (mAeaA.Centroid.X - mAeaB.Centroid.X) + (mAeaA.Centroid.Y - mAeaB.Centroid.Y) * (mAeaA.Centroid.Y - mAeaB.Centroid.Y));
            return distance;
        }
        #endregion
        #region 两个数值取其中较大值
        public double MaxNum(double num1,double num2)
        {
            double value = 0.0;
            if (Math.Abs(num1) >Math.Abs(num2))
                value = num1;
            else
            {
                value = num2;
            }
            return value;
        }
        #endregion
        #region  计算中心点到面的欧几何距离
        public double getPointToPolygonDistance(PointObject pointObjet, PolygonObject dpB)
        {
            double distance = 0;
            List<TriDot> triDolst = dpB.PointList;
            TriDot cenTdot = dpB.CalPolygonCenterPoint(triDolst);
            PointObject centerPoint = new PointObject(cenTdot.ID, cenTdot);
            double area = dpB.GetArea();
            distance = Math.Sqrt((pointObjet.Point.X - cenTdot.X) * (pointObjet.Point.X - cenTdot.X) + (pointObjet.Point.Y - cenTdot.Y) * (pointObjet.Point.Y - cenTdot.Y));
            return distance;
        }
        #endregion
        #region 计算距离中心点最近的距离
        public double NearDistance(List<PolygonObject> polyObtlst)
        {
            PointObject pointObt = Get_AreaCenterPoint(polyObtlst);//中心点
            double MinDistance = 10000000000;
            int PolyNum=polyObtlst.Count;
            for (int i = 0; i < PolyNum;i++ )
            {
                PolygonObject MP =polyObtlst[i];
                double distance1 = getPointToPolygonDistance(pointObt, MP);
                if (MinDistance > distance1)
                    MinDistance = distance1;
            }
            return MinDistance;
        }
        #endregion
        #region 已知中心点，求多边形中距中心点的平均距离
        public double MeanDistance(PointObject centerPoint, List<PolygonObject> polyObtlst)
        {
            //double MinDistance = 10000000000;
            int PolyNum = polyObtlst.Count;
            double sumDis = 0.0;
            for (int i = 0; i < PolyNum; i++)
            {
                PolygonObject MP = polyObtlst[i];
                double distance1 = getPointToPolygonDistance(centerPoint, MP);
                sumDis = sumDis + distance1;
            }
            return (sumDis / polyObtlst.Count);
        }
        #endregion 
        #region 已知中心点，求多边形中距中心点的最近距离
        public double NearDistance(PointObject centerPoint,List<PolygonObject> polyObtlst)
        {
            double MinDistance = 10000000000;
            int PolyNum = polyObtlst.Count;
            for (int i = 0; i < PolyNum; i++)
            {
                PolygonObject MP = polyObtlst[i];
                double distance1 = getPointToPolygonDistance(centerPoint, MP);
                if (MinDistance > distance1)
                    MinDistance = distance1;
            }
            return MinDistance;
        }
        #endregion 
        #region 已知中心点，求多边形中距中心点的最远距离
        public double FastDistance(PointObject centerPoint, List<PolygonObject> polyObtlst)
        {
            double MinDistance = 0.0;
            int PolyNum = polyObtlst.Count;
            for (int i = 0; i < PolyNum; i++)
            {
                PolygonObject MP = polyObtlst[i];
                double distance1 = getPointToPolygonDistance(centerPoint, MP);
                if (MinDistance < distance1)
                    MinDistance = distance1;
            }
            return MinDistance;
        }
        #endregion 
        #region  计算多边形之间的平均最邻近距离
        public double MeanDistance(List<PolygonObject> polyObtlst)
        {
            double meanDis = 0.0;
            int polyNum = polyObtlst.Count;
            double sumDis = 0.0;
            for (int i = 0; i < polyNum-1;i++ )
            {
                double polyBetDis = MinDistance(polyObtlst, polyObtlst[i]);
                    sumDis += polyBetDis;
                
            }
            meanDis=sumDis/(polyNum);
            return meanDis;
        }
        #endregion

        #region 转换PolygonObjectToPolygon
        public IPolygon ObjectConvert(PolygonObject pPolygonObject)
        {
            Ring ring1 = new RingClass();
            object missing = Type.Missing;

            IPoint curResultPoint = new PointClass();
            TriDot curPoint = null;
            if (pPolygonObject != null)
            {
                for (int i = 0; i < pPolygonObject.PointList.Count; i++)
                {
                    curPoint = pPolygonObject.PointList[i];
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
        #endregion

        #region 计算多边形之间的最小距离 //将建筑物与线等都抽象为点
        public double MinDistance(List<PolygonObject> polyObtlst)
        {
            //double minDis = 999999999;
            //int polyNum = polyObtlst.Count;
            //for (int i = 0; i < polyNum-1;i++ )
            //{
            //    for (int j = i + 1; j < polyNum; j++)
            //    {
            //        double polyBetDis =polyObtlst[i]. GetMiniDistance(polyObtlst[j]);
            //        if(polyBetDis<minDis)
            //            minDis=polyBetDis;
            //    }
            //}
            //return minDis;
            double MinDis = 1000000000000000; 

            for (int i = 0; i < polyObtlst.Count-1; i++)
            {
                IPolygon mPolygon = this.ObjectConvert(polyObtlst[i]);
                for (int j = i + 1; j < polyObtlst.Count; j++)
                {
                    IPolygon nPolygon = this.ObjectConvert(polyObtlst[j]);
                    IPointCollection mPointCollection = mPolygon as IPointCollection;
                    IPointCollection nPointCollection = nPolygon as IPointCollection;
                    IProximityOperator mProximityOperator = mPolygon as IProximityOperator;
                    IProximityOperator nProximityOperator = nPolygon as IProximityOperator;

                    for (int k = 0;  k< mPointCollection.PointCount; k++)
                    {
                        if (nProximityOperator.ReturnDistance(mPointCollection.get_Point(k)) < MinDis)
                        {
                            MinDis = nProximityOperator.ReturnDistance(mPointCollection.get_Point(k));                           
                        }
                    }

                    for (int k = 0; k < nPointCollection.PointCount; k++)
                    {
                        if (mProximityOperator.ReturnDistance(nPointCollection.get_Point(k)) < MinDis)
                        {
                            MinDis = mProximityOperator.ReturnDistance(nPointCollection.get_Point(k));                           
                        }
                    }
                }
            }

            return MinDis;

        }
         #endregion
        #region 计算已知多边形，到多边形群中的最邻近距离
        public double MinDistance(List<PolygonObject> polyObtlst,PolygonObject Mp)
        {
            double minDis = 999999999;
            int polyNum = polyObtlst.Count;
            for (int i = 0; i < polyNum-1;i++ )
            {

                double polyBetDis =Mp. GetMiniDistance(polyObtlst[i]);
                if(polyBetDis!=0)
                {
                    if(polyBetDis<minDis)
                        minDis=polyBetDis;
                }
            }
            return minDis;
        }
        #endregion
        #region 计算多边形之间最大的距离
        public double MaxDistance(List<PolygonObject> polyObtlst)
        {
            double maxDis = 0.0;
            int polyNum = polyObtlst.Count;
            for (int i = 0; i < polyNum - 1; i++)
            {
                for (int j = i + 1; j < polyNum; j++)
                {
                    double polyBetDis = polyObtlst[i].GetMiniDistance(polyObtlst[j]);
                    if (polyBetDis > maxDis)
                        maxDis = polyBetDis;
                }
            }
            return maxDis;
        }
        #endregion
        #region 计算距离中心点最远的距离
        public double FastDistance(List<PolygonObject> polyObtlst)
        {

            PointObject point1 = Get_AreaCenterPoint(polyObtlst);//中心点
            double MaxDistance = 0.0;
            int PolyNum = polyObtlst.Count;
            for (int i = 0; i < PolyNum; i++)
            {

                PolygonObject MP = polyObtlst[i];
                double distance1 = getPointToPolygonDistance(point1, MP);
                if (MaxDistance < distance1)
                    MaxDistance = distance1;
            }

            return MaxDistance;
        }
        #endregion
        #region 计算比例径向函数的系数（径向函数说明）
        public double GetK(List<PolygonObject> polyObtlst)
        {
            double k = 0.0,k1=0.0,k2=0.0;
            double mini =2;//？？？
            double dis = NearDistance(polyObtlst);
            double dis1 = MinDistance(polyObtlst);
            double dis2 = MeanDistance(polyObtlst);
            k2 = 2 / dis2;
            k = 0.5 / dis;
            k1 = mini / dis1;
            //MessageBox.Show("合适的" + k + "错误的移位比率2为" + k1+ "错误的移位比率3为" + k2);
            //if (k > 1)
            // k = k - 1;
            #region 如果建筑物间的最短距离大于最小阈值，说明没有冲突
            if (dis1>mini)
            {
                k = 99999;
            }
            #endregion
            return Math.Abs(k);
        }
        #endregion
        #region 计算移位量
        public double DisplcementDistance(PolygonObject polygon,List<PolygonObject> polyObtlst, double K, PointObject point1,double threshold)
        {
            //double MaxdisplaceDis=5;
            double MaxDis = FastDistance(polyObtlst);//点到移位中心的最远距离
            double MinDis = NearDistance(polyObtlst);//点到移位中心的最近距离
            double minDis=MinDistance(polyObtlst);//多边形之间的最短距离
            double distance = getPointToPolygonDistance(point1, polygon);
            double displaceDis = 0.0;
            displaceDis = K * distance * Math.Exp(((MinDis - distance) / (MaxDis - MinDis)));
            //if (distance == MinDis)
            //{
            //    displaceDis = ((2 * distance) / minDis) - 2;

            //}
            //else
            //{
               // displaceDis = (K - 1) * distance * (1 / Math.Exp((2.5*distance)/MaxDis));
            //}
            //displaceDis = (K - 1) * distance;
           // displaceDis = K * distance *( 1/(Math.Exp((2.5*distance)/MaxDis)));
            //if (displaceDis > MaxdisplaceDis)//只有两个建筑物时，此时计算的移位比率很大，造成的移位距离很大，需要加以控制
            //{
            //    //控制在最大移动范围内，0.5mm，如果比例尺是30000，则最多移动15m
            //    //double displaceDis = scalCoefficient * distance * Math.Exp((MinDis - distance) / (MaxDis - MinDis)) * (0.7 + 1.2 * (((distance - MinDis) / (MaxDis - MinDis))));
            //    // displaceDis = MaxdisplaceDis;
            //    displaceDis = displaceDis - MaxdisplaceDis;
            //}
                return displaceDis;

        }
        #endregion
        #region  将群类数据转化为面数据
        public List<PolygonObject> cluster_PolygonList(List<PolygonCluster> clusterLst)
        {
            List<PolygonObject> polygon_list = new List<PolygonObject>();
            for (int i = 0; i < clusterLst.Count;i++ )
            {
                for(int j=0;j<clusterLst[i].polygonList.Count;j++)
              {
                  polygon_list.Add(clusterLst[i].polygonList[j]);
               }
            }
            return polygon_list;
        }
        #endregion

        #region 只考虑建筑物群的移位，并返回移位后的面数据
        public  List<PolygonCluster> Displacement(List<PolygonCluster> polyClusterlst,double threshold)
        {
            int polyCluNum = polyClusterlst.Count;
            List<PolygonCluster> polyCluList = new List<PolygonCluster>();
            for (int i = 0; i < polyCluNum; i++)
            {
              int polyNum = polyClusterlst[i].polygonList.Count;
              if (polyNum > 1)
              {
                  Displacement Dis = new Displacement();
                  GeoCalculation Gc = new GeoCalculation();
                  PointObject medPoint = Gc.Get_AreaCenterPoint(polyClusterlst[i].polygonList);//中心点
                  double k =Gc.GetK(polyClusterlst[i].polygonList);//在GetK中判断了建筑物间的最短距离是否大于最小阈值
                  //针对每一类建筑物群每一个建筑物进行移位量计算
                  List<PolygonObject> polygonList = new List<PolygonObject>();

                  #region 聚类建筑物内不存在冲突
                  if (k == 99999)//如果该类中建筑物之间的最短距离大于冲突阈值，说明没有冲突，则不进行移位处理
                  {
                      ;// polyClusterlst[i].polygonList = polygonList;

                  }
                  #endregion
                  else
                  {
                      #region 建筑物个数为2个
                      if (polyNum == 2)
                      {
                          //若两个建筑物都不与道路冲突或两个建筑物都与道路冲突
                          if (((polyClusterlst[i].polygonList[0].Label == "noConflict") && (polyClusterlst[i].polygonList[1].Label == "noConflict")) || ((polyClusterlst[i].polygonList[1].Label != "noConflict") && (polyClusterlst[i].polygonList[0].Label != "noConflict")))
                          {
                              double distance = Gc.MinDistance(polyClusterlst[i].polygonList);
                              double area1 = polyClusterlst[i].polygonList[0].GetArea();
                              double area2 = polyClusterlst[i].polygonList[1].GetArea();
                              double sumArea = area1 + area2;

                              //移位距离根据两者的面积比率确定（这里的Distance是人为给定的）
                              double displaceDis1 = (2 - distance) * (area2 / sumArea);
                              double displaceDis2 = (2 - distance) * (area1 / sumArea);
                            //  PointObject vex1 = polyClusterlst[i].polygonList[0].NearVexPoint(polyClusterlst[i].polygonList[1]);//多边形0到多边形1最近顶点，在多边形0上，相当于移位顶点
                              TriDot cenTdot1 = polyClusterlst[i].polygonList[0].CalPolygonCenterPoint(polyClusterlst[i].polygonList[0].PointList);
                              //PointObject vex2 = polyClusterlst[i].polygonList[1].NearVexPoint(polyClusterlst[i].polygonList[0]);
                              TriDot cenTdot2 = polyClusterlst[i].polygonList[1].CalPolygonCenterPoint(polyClusterlst[i].polygonList[1].PointList) ;//定义移位参照中心点
                              double length = Gc.TriDot_getDistance(cenTdot1, cenTdot2);
                              double dx2 = (cenTdot2.X - cenTdot1.X) / length * displaceDis2;
                              double dy2 = (cenTdot2.Y - cenTdot1.Y) / length * displaceDis2;
                              double dy1 = (cenTdot1.Y - cenTdot2.Y) / length * displaceDis1;
                              double dx1 = (cenTdot1.X - cenTdot2.X)/length* displaceDis1 ;
                              PolygonObject mPolygon1 = Dis.MoveSingleFeatures1(polyClusterlst[i].polygonList[0], dx1, dy1);//按照每个多边形的移位位置生成一个多边形
                              polygonList.Add(mPolygon1);
                              PolygonObject mPolygon2 = Dis.MoveSingleFeatures1(polyClusterlst[i].polygonList[1], dx2, dy2);//按照每个多边形的移位位置生成一个多边形
                              polygonList.Add(mPolygon2);

                          }
                          //有一个建筑物存在与道路冲突
                          else
                          {
                              #region 如果建筑物1与道路冲突，直接给定一个移位比率
                              if ((polyClusterlst[i].polygonList[0].Label == "noConflict") && (polyClusterlst[i].polygonList[1].Label != "noConflict"))
                              {
                                  double distance = Gc.MinDistance(polyClusterlst[i].polygonList);
                                  //这里的数据是人为给定的
                                  double displaceDis1 = (2.7 - distance);
                                  TriDot cenTdot1 = polyClusterlst[i].polygonList[0].CalPolygonCenterPoint(polyClusterlst[i].polygonList[0].PointList);//将建筑物转换成点集，求点集的中心
                                  //PointObject vex2 = polyClusterlst[i].polygonList[1].NearVexPoint(polyClusterlst[i].polygonList[0]);
                                  TriDot cenTdot2 = polyClusterlst[i].polygonList[1].CalPolygonCenterPoint(polyClusterlst[i].polygonList[1].PointList);//定义移位参照中心点
                                  double length = Gc.TriDot_getDistance(cenTdot1, cenTdot2);
                                 // double dx2 = (cenTdot2.X - cenTdot1.X) / length * displaceDis2;
                                 // double dy2 = (cenTdot2.Y - cenTdot1.Y) / length * displaceDis2;
                                  double dy1 = (cenTdot1.Y - cenTdot2.Y) / length * displaceDis1;
                                  double dx1 = (cenTdot1.X - cenTdot2.X) / length * displaceDis1;
                                  PolygonObject mPolygon1 = Dis.MoveSingleFeatures1(polyClusterlst[i].polygonList[0], dx1, dy1);//按照每个多边形的移位位置生成一个多边形
                                  polygonList.Add(mPolygon1);
                                  PolygonObject mPolygon2 = Dis.MoveSingleFeatures1(polyClusterlst[i].polygonList[1], 0, 0);//按照每个多边形的移位位置生成一个多边形
                                  polygonList.Add(mPolygon2);
                              }
                              #endregion
                              #region 如果建筑物与道路冲突
                              //如何考虑与道路的冲突的呢？
                              if ((polyClusterlst[i].polygonList[1].Label == "noConflict") && (polyClusterlst[i].polygonList[0].Label != "noConflict"))
                              {
                                  double distance = Gc.MinDistance(polyClusterlst[i].polygonList);
                                  double displaceDis2 = (2.7 - distance);
                                  TriDot cenTdot1 = polyClusterlst[i].polygonList[0].CalPolygonCenterPoint(polyClusterlst[i].polygonList[0].PointList);
                                  //PointObject vex2 = polyClusterlst[i].polygonList[1].NearVexPoint(polyClusterlst[i].polygonList[0]);
                                  TriDot cenTdot2 = polyClusterlst[i].polygonList[1].CalPolygonCenterPoint(polyClusterlst[i].polygonList[1].PointList);//定义移位参照中心点
                                  double length = Gc.TriDot_getDistance(cenTdot1, cenTdot2);
                                  double dx2 = (cenTdot2.X - cenTdot1.X) / length * displaceDis2;
                                  double dy2 = (cenTdot2.Y - cenTdot1.Y) / length * displaceDis2;
                                 // double dy1 = (cenTdot1.Y - cenTdot2.Y) / length * displaceDis1;
                                 // double dx1 = (cenTdot1.X - cenTdot2.X) / length * displaceDis1;
                                  PolygonObject mPolygon1 = Dis.MoveSingleFeatures1(polyClusterlst[i].polygonList[0], 0, 0);//按照每个多边形的移位位置生成一个多边形
                                  polygonList.Add(mPolygon1);
                                  PolygonObject mPolygon2 = Dis.MoveSingleFeatures1(polyClusterlst[i].polygonList[1], dx2, dy2);//按照每个多边形的移位位置生成一个多边形
                                  polygonList.Add(mPolygon2);
                              }
                              #endregion                           
                          }
                      }
                      #endregion

                      #region 建筑物个数大于2
                      else
                      {

                          for (int j = 0; j < polyNum; j++)
                          {
                              PolygonObject MP = polyClusterlst[i].polygonList[j];
                              double displaceDis = Gc.DisplcementDistance(MP, polyClusterlst[i].polygonList, k, medPoint, threshold);//计算每一个多边形的移位量
                              double distance1 = Gc.getPointToPolygonDistance(medPoint, MP);//计算该点距离中心的距离
                              List<TriDot> triDolst = MP.PointList;
                              TriDot cenTdot = MP.CalPolygonCenterPoint(triDolst);
                              double area = MP.GetArea();
                              double dy = (((cenTdot.Y - medPoint.Point.Y) * (distance1 + displaceDis)) / distance1) + (medPoint.Point.Y - cenTdot.Y);
                              double dx = (((cenTdot.X - medPoint.Point.X) * (distance1 + displaceDis)) / distance1) + (medPoint.Point.X - cenTdot.X);
                              PolygonObject mPolygon = Dis.MoveSingleFeatures1(MP, dx, dy);//按照每个多边形的移位位置生成一个多边形
                              polygonList.Add(mPolygon);
                          }
                      }
                      #endregion

                      polyClusterlst[i].polygonList = polygonList;
                  }
              }
                polyCluList.Add(polyClusterlst[i]);
            }

            return polyCluList;
        }
        #endregion

        #region 对于次生冲突的二次平移处理
        public List<PolygonCluster> Translation(ref List<PolygonCluster> polyClusterlst, double viewMini)
        {
            int polyCluNum = polyClusterlst.Count;
            List<PolygonCluster> polyCluList = new List<PolygonCluster>();
            for (int i = 0; i < polyCluNum; i++)
            {
                int polyNum = polyClusterlst[i].polygonList.Count;
                List<PolygonObject>  polygonList=new List<PolygonObject>();
                if (polyNum ==2)
                {
                    Displacement Dis = new Displacement();
                    GeoCalculation Gc = new GeoCalculation();
                    PolygonObject nearRoadmedpolygon = null;
                    PointObject medPoint = null;
                    PointObject cenTdot=null;
                    double distance1=0.0;
                    PolygonObject fixPolygon=null;  
                    PolygonObject disPolygon=null;
                    for (int s = 0; s < 2;s++ )
                    {
                        nearRoadmedpolygon = polyClusterlst[i].polygonList[s];
                        if (nearRoadmedpolygon.Label != "noConflict")
                        {
                            fixPolygon = nearRoadmedpolygon;

                            if (s == 0)
                            {
                                disPolygon = polyClusterlst[i].polygonList[s + 1];
                                medPoint = fixPolygon.NearVexPoint(disPolygon);//中心点
                                cenTdot = disPolygon.NearVexPoint(fixPolygon);//移位参照顶点
                                distance1 = fixPolygon.GetMiniDistance(disPolygon);

                            }
                            else
                            {
                                disPolygon = polyClusterlst[i].polygonList[s - 1];
                                medPoint = fixPolygon.NearVexPoint(disPolygon);//中心点
                                cenTdot = disPolygon.NearVexPoint(fixPolygon);//移位参照顶点
                                distance1 = fixPolygon.GetMiniDistance(disPolygon);
                            }
                        }
                        else
                        {
                            fixPolygon = nearRoadmedpolygon;

                            if (s == 0)
                            {
                                disPolygon = polyClusterlst[i].polygonList[s + 1];
                                medPoint = fixPolygon.NearVexPoint(disPolygon);//中心点
                                cenTdot = disPolygon.NearVexPoint(fixPolygon);//移位参照顶点
                                distance1 = fixPolygon.GetMiniDistance(disPolygon);

                            }
                            else
                            {
                                disPolygon = polyClusterlst[i].polygonList[s - 1];
                                medPoint = fixPolygon.NearVexPoint(disPolygon);//中心点
                                cenTdot = disPolygon.NearVexPoint(fixPolygon);//移位参照顶点
                                distance1 = fixPolygon.GetMiniDistance(disPolygon);
                            }
                            
                        }
                      }
                          
                            //计算两个多边形存在时，其中另一个多边形的移位量
                             //计算两个多变形之间的距离
                            double displaceDis = viewMini-distance1;//计算平移的移位量
                            List<TriDot> triDolst = disPolygon.PointList;
                            double area = disPolygon.GetArea();
                            double dy = (((cenTdot.Point.Y - medPoint.Point.Y) * (distance1 + displaceDis)) / distance1) + (medPoint.Point.Y - cenTdot.Point.Y);
                            double dx = (((cenTdot.Point.X - medPoint.Point.X) * (distance1 + displaceDis)) / distance1) + (medPoint.Point.X - cenTdot.Point.X);
                            PolygonObject mPolygon = Dis.MoveSingleFeatures(disPolygon, dx, dy);//按照每个多边形的移位位置生成一个多边形
                            polygonList.Add(mPolygon);
                            polygonList.Add(fixPolygon);
                            polyClusterlst[i].polygonList = polygonList;

                  }
                    polyCluList.Add(polyClusterlst[i]);
               }
                    return polyCluList;
             }
        #endregion
        #region 识别面图层与线图层之间的冲突
        public List<PolygonObject> buildingsAndRoadsConflict1( List<PolygonObject> polyList,List<PolylineObject> polylineList,double miniDis)
        {
            List<PolygonObject> potListNew = new List<PolygonObject>();
            for (int i = 0; i < polyList.Count;i++ )
            {
                for (int j = 0; j < polylineList.Count; j++)
                {
                    
                    double buildtoRoadDis = polyList[i].GetMiniDistance(polylineList[j]);
                    List<int> conflictEdgeNumber = new List<int>();
                    if (buildtoRoadDis < miniDis)
                    {
                        //将与某段折线有冲突的面识别出来，并做了标记，方便以后拿出来移位

                        conflictEdgeNumber.Add(polylineList[j].ID);
                    }
                    polyList[i].ConflictIDs = conflictEdgeNumber;

                    if (polyList[i].ConflictIDs.Count == 1)
                    {
                        polyList[i].Label = "oneConflict";
                        potListNew.Add(polyList[i]);
                    }
                    if (polyList[i].ConflictIDs.Count > 1)
                    {
                        polyList[i].Label = "oneMoreConflict";
                        potListNew.Add(polyList[i]);
                    }
                }
            }

            return potListNew;           
        }
        public List<PolygonCluster> buildingsAndRoadsConflict( List<PolygonCluster> polygonClusterlst, List<PolylineObject> polylineList, double miniDis)
        {
            List<PolygonCluster> potListNew = new List<PolygonCluster>();
            for (int k = 0; k < polygonClusterlst.Count;k++ )
            {
                for (int i = 0; i < polygonClusterlst[k].polygonList.Count;i++)
                {
                    for (int j = 0; j < polylineList.Count; j++)
                    {
                        double buildtoRoadDis = polygonClusterlst[k].polygonList[i].GetMiniDistance(polylineList[j]);
                        //polygonClusterlst[k].polygonList[i].ConflictIDs = new List<int>();
                        if (buildtoRoadDis < miniDis)
                        {
                            //将与某段折线有冲突的面识别出来，并做了标记，方便以后拿出来移位

                            polygonClusterlst[k].polygonList[i].ConflictIDs.Add(polylineList[j].ID);
                        }
                        //polygonClusterlst[k].polygonList[i].ConflictIDs = conflictEdgeNumber;

                        if (polygonClusterlst[k].polygonList[i].ConflictIDs.Count == 1)
                          {
                            polygonClusterlst[k].polygonList[i].Label = "oneConflict";
                            polygonClusterlst[k].sign = 1;//其中与一条线发生冲突的聚类群进行标记
                            //potListNew.Add(polygonClusterlst[k]); 
                           // MessageBox.Show("该多边形的冲突边是"+polygonClusterlst[k].polygonList[i].ConflictIDs[0]);
                         }
                        if (polygonClusterlst[k].polygonList[i].ConflictIDs.Count > 1)
                        {
                            polygonClusterlst[k].polygonList[i].Label = "oneMoreConflict";
                            //potListNew.Add(polygonClusterlst[k]);
                            polygonClusterlst[k].sign = 2;
                        }
                        
                    }

                    //若建筑物与街道冲突，则将该建筑物标识为移位的第一层建筑物
                    if (polygonClusterlst[k].polygonList[i].Label == "oneConflict" || polygonClusterlst[k].polygonList[i].Label == "oneMoreConflict")
                    {
                        polygonClusterlst[k].polygonList[i].Level = 1;
                    }
                }
               
                    potListNew.Add(polygonClusterlst[k]);
              
            }

            return potListNew;

        }
        #region 判断所有的建筑物与道路都没有冲突
        public bool buildingsAndRoadsConflict1(List<PolygonCluster> polygonClusterlst, List<PolylineObject> polylineList, double miniDis)
        {
            bool isConflt = true;
            List<PolygonCluster> potListNew = new List<PolygonCluster>();
            for (int k = 0; k < polygonClusterlst.Count; k++)
            {
                for (int i = 0; i < polygonClusterlst[k].polygonList.Count; i++)
                {
                    for (int j = 0; j < polylineList.Count; j++)
                    {
                        double buildtoRoadDis = polygonClusterlst[k].polygonList[i].GetMiniDistance(polylineList[j]);
                        //polygonClusterlst[k].polygonList[i].ConflictIDs = new List<int>();
                        if (buildtoRoadDis < miniDis)
                        {
                            isConflt = false;
                        }
                        //polygonClusterlst[k].polygonList[i].ConflictIDs = conflictEdgeNumber;

                        if (polygonClusterlst[k].polygonList[i].ConflictIDs.Count == 1)
                        {
                            polygonClusterlst[k].polygonList[i].Label = "oneConflict";
                            polygonClusterlst[k].sign = 1;//其中与一条线发生冲突的聚类群进行标记
                            //potListNew.Add(polygonClusterlst[k]); 
                            // MessageBox.Show("该多边形的冲突边是"+polygonClusterlst[k].polygonList[i].ConflictIDs[0]);
                        }
                        if (polygonClusterlst[k].polygonList[i].ConflictIDs.Count > 1)
                        {
                            polygonClusterlst[k].polygonList[i].Label = "oneMoreConflict";
                            //potListNew.Add(polygonClusterlst[k]);
                            polygonClusterlst[k].sign = 2;
                        }

                    }

                }

                potListNew.Add(polygonClusterlst[k]);

            }

            return isConflt;

        }
        #endregion
        public List<PolygonObject> buildingsAndRoadsConflict2(List<PolygonObject> polygonlst, List<PolylineObject> polylineList, double miniDis)
        {
            List<PolygonObject> potListNew = new List<PolygonObject>();
            for (int i = 0; i < polygonlst.Count; i++)
                {
                    for (int j = 0; j < polylineList.Count; j++)
                    {
                        double buildtoRoadDis = polygonlst[i].GetMiniDistance(polylineList[j]);
                        //polygonClusterlst[k].polygonList[i].ConflictIDs = new List<int>();
                        if (buildtoRoadDis < miniDis)
                        {
                            //将与某段折线有冲突的面识别出来，并做了标记，方便以后拿出来移位
                             
                          polygonlst[i].conflictId=polylineList[j].ID;
                          polygonlst[i].Label = "oneConflict";
                        }
                        //polygonClusterlst[k].polygonList[i].ConflictIDs = conflictEdgeNumber;


                    }
                    potListNew.Add(polygonlst[i]);

                }


                

            

            return potListNew;

        }
        #endregion

        #region 计算建筑物群到道路的最近距离
        public double MinbuildingToroad_Dis(List<PolygonObject> polyObtlst,PolylineObject polyLineObt)
        {
            double MinDis = 99999999999;
            
            for(int i=0;i<polyObtlst.Count;i++)//
            {
                
                    if ((polyObtlst[i].GetMiniDistance(polyLineObt)) < MinDis)
                    {
                        MinDis = polyObtlst[i].GetMiniDistance(polyLineObt);
                    }
               
            }
            return MinDis;
            
        }
        #endregion     
        #region 计算建筑物群到道路的最远距离
        public double MaxbuildingToroad_Dis(List<PolygonObject> polyObtlst, PolylineObject polyLineObt)
        {
            double MaxDis = 0.00000000000001;
            for (int i = 0; i < polyObtlst.Count; i++)//
            {
                if (polyObtlst[i].Label == "noConflict")//找出比例射线移位聚类群的最远距离
                {
                    if ((polyObtlst[i].GetMiniDistance(polyLineObt)) > MaxDis)
                        MaxDis = polyObtlst[i].GetMiniDistance(polyLineObt);
                }

            }
            return MaxDis;
        }
        #endregion
        #region 计算建筑物群到道路的平均距离
        public double MeanbuildingToroad_Dis(List<PolygonObject> polyObtlst, PolylineObject polyLineObt)
        {
            double SumDis = 0.00000000000001;
            for (int i = 0; i < polyObtlst.Count; i++)//
            {
                SumDis = SumDis + polyObtlst[i].GetMiniDistance(polyLineObt);
                

            }
            return (SumDis / polyObtlst.Count);
        }
        #endregion
        #region 计算一个面群中面积最小值
        public double min_Area(List<PolygonObject> polygonlst)
        {
            double min_Area = 9999999999999;
            for (int i = 0; i < polygonlst.Count; i++)
            {
                if ((polygonlst[i].GetArea()) < min_Area)
                    min_Area = polygonlst[i].GetArea();
            }
            return min_Area;
        }
        #endregion
        #region 计算考虑道路要素的移位比率
        public double GetRoad_k(double meanDistance)
        {
            return (meanDistance/6);
        }
        #endregion
        #region 计算考虑道路要素的移位比率1
        public double GetRoad_k1(double nearDistance)
        {
            //double k = 0;
            //k = 0.3 / nearDistance;
            
            //return ( 0.3/nearDistance);
            double near_dis = nearDistance;
            double k = 0.0;
            if (near_dis < 1)
                k = 0.3 / near_dis;
            //else
            //    if (near_dis > 10)
            //    {
            //    }
                else 
                { k = 0.5 / near_dis;}
            if (k * nearDistance > 7)
                k = 0.5;
            if(k<0.01)
            {
                k = 0.5;
            }
            return k;
        }
        #endregion
        #region 计算考虑道路要素的移位比率2
        public double GetRoad_k2(double nearDistance)
        {
            //double k = 0;
            //k = 0.3 / nearDistance;

            //return ( 0.3/nearDistance);
            double near_dis = nearDistance;
            double k = 0.0;
            if ((near_dis < 1))
            {
                k = 0.5 / near_dis;
            }
            if ((near_dis < 5) && (near_dis >1))
                k = 0.6/ near_dis;
            if ((near_dis > 5) && (near_dis < 6))
            { k = 0.6/ near_dis; }
            if (near_dis > 6)
            {
                k = 0.2 / near_dis;
            }
          
            return k;
        }
        public double GetRoad_k3(double nearDistance)
        {
            //double k = 0;
            //k = 0.3 / nearDistance;

            //return ( 0.3/nearDistance);
            double near_dis = nearDistance;
            double k = 0.0;
            if (near_dis < 1)
            {
                k = 0.8 / near_dis;
            }
            if ((near_dis < 5) && (near_dis > 1))
                k = 2/ near_dis;
            if ((near_dis > 5) && (near_dis < 10))
            { k = 2/ near_dis; }
            if (near_dis >10)
            { k = 6/ near_dis; }
            return k;
        }
        #endregion
        #region  求点到线的垂点,作为比例射线移位的中心点
        public PointObject ReurnNearstPoint(PointObject point1,PolylineObject polyline1)
        {

            TriDot pedal_tridot = polyline1.ReturnNearestPoint(point1, false);
            PointObject pedal_Point = new PointObject(pedal_tridot.ID, pedal_tridot);
            return pedal_Point;    
        }
        #endregion
        #region 求多边形距离多线的最短距离顶点
        public PointObject NearestPoint(PolygonObject polygonObt,PolylineObject polyline)
        {
            double minidist = 9999999999999999;
            //线的点链
            List<TriDot> list = polyline.PointList;
            //多边形上每一点到线的最短距离 
             TriDot tridot_nearest = null;
            for (int i = 0; i < polygonObt.PointList.Count; i++)
            {
               
                TriDot node =polygonObt.PointList[i];
                double dist = node.ReturnDist(list, false);
               
                    if (dist < minidist)
                    {
                        minidist = dist;
                        tridot_nearest = new TriDot(polygonObt.PointList[i].X, polygonObt.PointList[i].Y);
                    }
                
            }
            PointObject point_nearest = new PointObject(tridot_nearest.ID,tridot_nearest);
            return point_nearest;
        }
        #endregion
        #region 计算道路要素存在的情况下距离道路最近的建筑物移位量的计算
        public double buildingtoRoad_dis(PolygonObject polygon, double K,PointObject centerPoint,double Near_dis,double Fast_dis)
        {
            //double Area = polygon.GetArea();
            //double MaxdisplaceDis = 5;
            //double MaxDis = MaxbuildingToroad_Dis(polyObtlst, polyLineObt);
            //double MinDis = MinbuildingToroad_Dis(polyObtlst, polyLineObt);
            GeoCalculation Gc = new GeoCalculation();
            double displaceDis = 0.0;
            double distance = Gc.getPointToPolygonDistance(centerPoint, polygon);//计算该建筑物距离移位中心的最短距离
           
            if((Fast_dis-Near_dis)<2)
            {
                displaceDis = K * distance;
            }
            else 
            {
                 displaceDis = K * distance * Math.Exp(((Near_dis - distance) / (Fast_dis - Near_dis)));
               // displaceDis = K * distance;
            }
            return displaceDis;
        }
        #endregion       

        #region 与道路发生冲突的建筑物移位
        public List<PolygonCluster> building_Road_Displacement(List<PolygonCluster> polyClusterlst, List<PolylineObject> polylineLst, double MiniDis,ISpatialReference sr)
        {
            int polyCluNum = polyClusterlst.Count;
            List<PolygonCluster> polyCluList = new List<PolygonCluster>();
            //int ID = 9999999, ID2 = 9999999;
            List<PointObject> medPotLst = new List<PointObject>();
            for (int i = 0; i < polyCluNum; i++)
            {

                int ID = 9999999, ID2 = 9999999;
                #region 说明该聚类群存在与一条道路冲突的建筑物
                if (polyClusterlst[i].sign == 1)//说明该聚类群存在与一条道路有冲突的建筑物
                {
                    Displacement Dis = new Displacement();
                    GeoCalculation Gc = new GeoCalculation();
                    for (int j = 0; j < polyClusterlst[i].polygonList.Count; j++)//取得与该类群建筑物有冲突的道路线的编号
                    {
                        if (polyClusterlst[i].polygonList[j].ConflictIDs.Count == 1)
                        { ID = polyClusterlst[i].polygonList[j].ConflictIDs[0]; }
                        if (ID != 9999999)
                        {
                            //  MessageBox.Show("提取的ID号是" + ID);
                            break;
                        }
                    }
                    PolylineObject o_polyline = PolylineObject.GetPolylinebyID(polylineLst, ID);
                    double near_dis = Gc.MinbuildingToroad_Dis(polyClusterlst[i].polygonList, o_polyline);//群内与道路产生冲突的建筑物距离道路的最短距离
                    //double k = Gc.GetRoad_k(near_dis);
                    int polyNum = polyClusterlst[i].polygonList.Count;
                    double min_area = Gc.min_Area(polyClusterlst[i].polygonList);//计算一个面群中面积最小值
                    #region 若群中只有一个建筑物
                    if (polyNum == 1)//若群中只有一个与道路产生冲突的建筑物
                    {
                        List<PolygonObject> polygonList = new List<PolygonObject>();
                        PolygonObject MP = polyClusterlst[i].polygonList[0];//进行移位的当前面
                        PointObject VexPoint = Gc.NearestPoint(MP, o_polyline);//距离道路最近的多边形顶点作为多边形移位的参照点
                        PointObject cenPoint = Gc.ReurnNearstPoint(VexPoint, o_polyline);//求垂点作为每次移位的中心点
                        double displaceDis = MiniDis - near_dis;//计算该多边形的移位量
                        double dy = (((VexPoint.Point.Y - cenPoint.Point.Y) * (near_dis + displaceDis)) / near_dis) + (cenPoint.Point.Y - VexPoint.Point.Y);
                        double dx = (((VexPoint.Point.X - cenPoint.Point.X) * (near_dis + displaceDis)) / near_dis) + (cenPoint.Point.X - VexPoint.Point.X);
                        PolygonObject mPolygon = Dis.MoveSingleFeatures(MP, dx, dy);//按照每个多边形的移位位置生成一个多边形
                        polygonList.Add(mPolygon);
                        polyClusterlst[i].polygonList = polygonList;//对于与道路有冲突的聚类群，对其进行移位并保存结果
                    }
                    #endregion
                    //针对每一类建筑物群每一个建筑物进行移位量计算
                    #region 若群中存在多个建筑物
                    if (polyNum > 1)
                    {
                        PointObject cenPoint = Gc.Get_noAreaCenterPoint(polyClusterlst[i].polygonList);//求这几个多边形的中心点
                        PointObject RefrenDisPot = Gc.ReurnNearstPoint(cenPoint, o_polyline);
                        
                        //MessageBox.Show("移位中心点的X坐标: " + RefrenDisPot.Point.X + "移位中心点的Y坐标: " + RefrenDisPot.Point.Y);//求多边形中心点到冲突线段的垂点作为每次移位的中心点
                        List<PolygonObject> polygonList = new List<PolygonObject>();
                        double near_dis1 = Gc.NearDistance(RefrenDisPot, polyClusterlst[i].polygonList);//群内距移位中心的最短距离
                        double fast_dis1 = Gc.FastDistance(RefrenDisPot, polyClusterlst[i].polygonList);//群内距移位中心的最远距离
                        double mini_dis = Gc.MinbuildingToroad_Dis(polyClusterlst[i].polygonList, o_polyline);//群内到道路的最短距离
                        double mean_dis1 = Gc.MeanbuildingToroad_Dis(polyClusterlst[i].polygonList, o_polyline);
                        double k = Gc.GetRoad_k2(mini_dis);
                        for (int j = 0; j < polyNum; j++)
                        {
                            PolygonObject MP = polyClusterlst[i].polygonList[j];//进行移位的当前面
                            List<TriDot> triDolst = MP.PointList;
                            TriDot cenTdot = MP.CalPolygonCenterPoint(triDolst);
                            double displaceDis = Gc.buildingtoRoad_dis(MP, k, RefrenDisPot, near_dis1, fast_dis1);//计算每一个多边形的移位量
                            //double MaxdisplaceDis = 5;
                            double distance1 = Gc.getPointToPolygonDistance(RefrenDisPot, MP);//计算该建筑物距离移位中心的最短距离;
                            //double dy = (((cenPoint.Point.Y - RefrenDisPot.Point.Y) * (distance1 + displaceDis)) / distance1) + (RefrenDisPot.Point.Y - cenPoint.Point.Y);
                            //double dx = (((cenPoint.Point.X - RefrenDisPot.Point.X) * (distance1 + displaceDis)) / distance1) + (RefrenDisPot.Point.X - cenPoint.Point.X);
                            double dy = (displaceDis / distance1) * (cenTdot.Y - RefrenDisPot.Point.Y);
                            double dx = (displaceDis / distance1) * (cenTdot.X - RefrenDisPot.Point.X);
                            PolygonObject mPolygon = Dis.MoveSingleFeatures(MP, dx, dy);//按照每个多边形的移位位置生成一个多边形
                            polygonList.Add(mPolygon);
                        }
                        polyClusterlst[i].polygonList = polygonList;//对于与道路有冲突的聚类群，对其进行移位并保存结果
                        medPotLst.Add(RefrenDisPot);
                        medPotLst.Add(cenPoint);
                    }
                    #endregion
                }
                #endregion

                #region 说明该聚类簇与两条建筑物冲突
                if (polyClusterlst[i].sign == 2)//说明该聚类群与两条道路有冲突的建筑物
                {
                    Displacement Dis = new Displacement();
                    GeoCalculation Gc = new GeoCalculation();
                    for (int j = 0; j < polyClusterlst[i].polygonList.Count; j++)//取得与该类群建筑物有冲突的道路线的编号
                    {
                        if (polyClusterlst[i].polygonList[j].ConflictIDs.Count == 2)
                        { ID = polyClusterlst[i].polygonList[j].ConflictIDs[0];
                          ID2 = polyClusterlst[i].polygonList[j].ConflictIDs[1];
                        }
                        if ((ID != 9999999) && (ID2 != 9999999))
                        {
                            //  MessageBox.Show("提取的ID号是" + ID);
                            break;
                        }
                    }
                    List<PolylineObject> o_polylinelst = new List<PolylineObject>();
                    List<double> near_dislst = new List<double>();
                    PolylineObject o_polyline1 = PolylineObject.GetPolylinebyID(polylineLst, ID);
                    PolylineObject o_polyline2 = PolylineObject.GetPolylinebyID(polylineLst, ID2);
                    o_polylinelst.Add(o_polyline1); o_polylinelst.Add(o_polyline2); 
                    double near_dis1 = Gc.MinbuildingToroad_Dis(polyClusterlst[i].polygonList, o_polyline1);
                    double near_dis2= Gc.MinbuildingToroad_Dis(polyClusterlst[i].polygonList, o_polyline2);
                    near_dislst.Add(near_dis1); near_dislst.Add(near_dis2);
                    int polyNum = polyClusterlst[i].polygonList.Count;
                    #region 若群中只有一个与两条道路冲突
                    if (polyNum == 1)//若群中只有一个与两条道路产生冲突的建筑物
                    {
                        double Dy = 0.0, Dx = 0.0;
                        List<PolygonObject> polygonList = new List<PolygonObject>();
                        PolygonObject MP = polyClusterlst[i].polygonList[0];//进行移位的当前面
                        List<double> dylst=new List<double>();
                        List<double> dxlst=new List<double>();
                        for (int m = 0; m < o_polylinelst.Count; m++)
                        {


                            PointObject VexPoint = Gc.NearestPoint(MP, o_polylinelst[m]);//距离道路最近的多边形顶点作为多边形移位的参照点
                            PointObject cenPoint = Gc.ReurnNearstPoint(VexPoint, o_polylinelst[m]);//求垂点作为每次移位的中心点
                            double displaceDis = MiniDis - near_dislst[m];//计算该多边形的移位量
                            double dy = (((VexPoint.Point.Y - cenPoint.Point.Y) * (near_dislst[m] + displaceDis)) / near_dislst[m]) + (cenPoint.Point.Y - VexPoint.Point.Y);
                            double dx = (((VexPoint.Point.X - cenPoint.Point.X) * (near_dislst[m] + displaceDis)) / near_dislst[m]) + (cenPoint.Point.X - VexPoint.Point.X);
                            dylst.Add(dy);
                            dxlst.Add(dx);
                        }
                            if ((dylst[0]*dylst[1]) < 0)
                            { Dy =dylst[0]+dylst[1]; }
                            else { Dy = MaxNum(dylst[0], dylst[1]); }
                            if ((dxlst[0] * dxlst[1]) < 0)
                            { Dx = dxlst[0] + dxlst[1]; }
                            else { Dx = MaxNum(dxlst[0], dxlst[1]); }
                        
                            PolygonObject mPolygon = Dis.MoveSingleFeatures(MP, Dx, Dy);//按照每个多边形的移位位置生成一个多边形
                            polygonList.Add(mPolygon);
                            polyClusterlst[i].polygonList = polygonList;//对于与道路有冲突的聚类群，对其进行移位并保存结果


                    }
                    #endregion

                    #region 若群中与道路冲突的建筑物大于1
                    if (polyNum > 1)//与两条道路产生冲突的建筑物群包含的建筑物个数大于1
                    {

                            List<PolygonObject> polygonList = new List<PolygonObject>();
                            PointObject RefrenDisPot = Gc.CrossPoint(o_polyline1, o_polyline2);
                            //MessageBox.Show("移位中心点的X坐标: " + RefrenDisPot.Point.X + "移位中心点的Y坐标: " + RefrenDisPot.Point.Y);//求多边形中心点到冲突线段的垂点作为每次移位的中心点
                            double neardis1 = Gc.NearDistance(RefrenDisPot, polyClusterlst[i].polygonList);//群内距移位中心的最短距离
                            double fastdis1 = Gc.FastDistance(RefrenDisPot, polyClusterlst[i].polygonList);//群内距移位中心的最远距离
                           // double mini_dis = Gc.MinbuildingToroad_Dis(polyClusterlst[i].polygonList, o_polyline);//群内到道路的最短距离
                           // double mean_dis1 = Gc.MeanbuildingToroad_Dis(polyClusterlst[i].polygonList, o_polyline);
                            double k = Gc.GetRoad_k3(neardis1);
                            for (int j = 0; j < polyNum; j++)
                            {
                                PolygonObject MP = polyClusterlst[i].polygonList[j];//进行移位的当前面
                                
                                double displaceDis = Gc.buildingtoRoad_dis(MP, k, RefrenDisPot, neardis1, fastdis1);//计算每一个多边形的移位量
                                List<TriDot> triDolst = MP.PointList;
                                TriDot cenTdot = MP.CalPolygonCenterPoint(triDolst);
                                //double MaxdisplaceDis = 5;
                                double distance1 = Gc.getPointToPolygonDistance(RefrenDisPot, MP);//计算该建筑物距离移位中心的最短距离;
                                //double dy = (((cenPoint.Point.Y - RefrenDisPot.Point.Y) * (distance1 + displaceDis)) / distance1) + (RefrenDisPot.Point.Y - cenPoint.Point.Y);
                                //double dx = (((cenPoint.Point.X - RefrenDisPot.Point.X) * (distance1 + displaceDis)) / distance1) + (RefrenDisPot.Point.X - cenPoint.Point.X);
                                double dy = (displaceDis / distance1) * (cenTdot.Y - RefrenDisPot.Point.Y);
                                double dx = (displaceDis / distance1) * (cenTdot.X - RefrenDisPot.Point.X);
                                PolygonObject mPolygon = Dis.MoveSingleFeatures(MP, dx, dy);//按照每个多边形的移位位置生成一个多边形
                                polygonList.Add(mPolygon);
                            }
                            polyClusterlst[i].polygonList = polygonList;//对于与道路有冲突的聚类群，对其进行移位并保存结果
                            //medPotLst.Add(RefrenDisPot);
                            medPotLst.Add(RefrenDisPot);
                            polyClusterlst[i].polygonList = polygonList;//对于与道路有冲突的聚类群，对其进行移位并保存结果                      
                    }
                    #endregion

                } 
                #endregion
                polyCluList.Add(polyClusterlst[i]);
            }
               
            //SaveNewObjects sc = new SaveNewObjects();
            //SaveNewObjects.SavePointObject(medPotLst, sr,path);
            return polyCluList;
        }
        #endregion

        #region 与道路发生冲突的建筑物移位
        public List<PolygonObject> building_Road_Displacement1(List<PolygonObject> polyList, List<PolylineObject> polylineLst, double threshold, double mapScale, double MiniDis, ISpatialReference sr)
        {

            int ID = 9999999;
            List<PointObject> medPotLst = new List<PointObject>();
            List<PolygonObject> polygonList = new List<PolygonObject>();
            for (int i = 0; i < polyList.Count; i++)
            {
                if (polyList[i].Label == "oneConflict")
                {
                    ID = polyList[i].conflictId;
                    Displacement Dis = new Displacement();
                    GeoCalculation Gc = new GeoCalculation();
                    PolylineObject o_polyline = PolylineObject.GetPolylinebyID(polylineLst, ID);
                    double near_dis = polyList[i].GetMiniDistance(o_polyline);//群内与道路产生冲突的建筑物距离道路的最短距离

                    PolygonObject MP = polyList[i];
                    //进行移位的当前面
                    PointObject VexPoint = Gc.NearestPoint(MP, o_polyline);//距离道路最近的多边形顶点作为多边形移位的参照点
                    PointObject cenPoint = Gc.ReurnNearstPoint(VexPoint, o_polyline);//求垂点作为每次移位的中心点
                      double displaceDis = MiniDis - near_dis;//计算该多边形的移位量
                      double dy = (((VexPoint.Point.Y - cenPoint.Point.Y) * (near_dis + displaceDis)) / near_dis) + (cenPoint.Point.Y - VexPoint.Point.Y);
                      double dx = (((VexPoint.Point.X - cenPoint.Point.X) * (near_dis + displaceDis)) / near_dis) + (cenPoint.Point.X - VexPoint.Point.X);
                    
                    PolygonObject mPolygon = Dis.MoveSingleFeatures(MP, dx, dy);//按照每个多边形的移位位置生成一个多边形
                    polygonList.Add(mPolygon);

                }
                else
                {
                    polygonList.Add(polyList[i]);

                }
            }
            return polygonList;
        }                  
        #endregion

        #region  根据冲突的线群移位
        public List<PolyLineCluster> Roads_dis(ref PolyLineCluster polylinecluster,double threshlod)
        {
            List<PolyLineCluster> newPolylinelst = new List<PolyLineCluster>();
            if (polylinecluster.polylineList.Count > 2)
            {

            }
            return newPolylinelst;
        }
        #endregion
        #region 计算每一个道路群的移位结果
        public List<PolylineObject> road_dis(ref List<PolylineObject> polylinelst,double viewDis)
        {
            List<PolylineObject> newpolylinelst = new List<PolylineObject>();
            List<PointObject> pointObtlst = new List<PointObject>();
            for (int i = 0; i < polylinelst.Count;i++)   //将线里边的点序列提出来
            {
                for (int j = 0; j < polylinelst[i].PointList.Count;j++)
                {
                    TriDot newTridot = new TriDot(polylinelst[i].PointList[j].X, polylinelst[i].PointList[j].Y);
                    newTridot.ID = polylinelst[i].PointList[j].ID;
                    PointObject newPointObt = new PointObject(newTridot.ID, newTridot);
                    pointObtlst.Add(newPointObt);
                }
            }
            GeoCalculation new_gc=new GeoCalculation();
            List<PointCluster> conflictPointCluster = PointCluster.startAnalysis(pointObtlst, viewDis);
            List<PointObject>  new_pointObtlst=new List<PointObject>();
            for (int k = 0; k < conflictPointCluster.Count;k++ )
            {
                if (conflictPointCluster[k].pointList.Count>1)//寻找出有问题的点群
                {
                    new_pointObtlst = new_gc.Point_Displace(ref conflictPointCluster[k].pointList);
                }
                new_pointObtlst.Add(conflictPointCluster[k].pointList[0]);
            }


            return newpolylinelst;     

        }

        #endregion
        #region 点群移位
        public  List<PointObject> Point_Displace(ref List<PointObject> pointList)
        {
            PointObject medPoint = MedPoint(pointList);
            List<PointObject> newPointList = new List<PointObject>();
            for (int i = 0; i < pointList.Count; i++)
            {

                TriDot newDot = new TriDot();
                double k = GetK(pointList);
                double distance1 = Point_getDistance(medPoint, pointList[i]);
                double displaceDis = Points_DisplcementDistance(pointList[i], pointList, k);
                newDot.Y = (((pointList[i].Point.Y - medPoint.Point.Y) * (distance1 + displaceDis)) / distance1) + medPoint.Point.Y;
                newDot.X = (((pointList[i].Point.X - medPoint.Point.X) * (distance1 + displaceDis)) / distance1) + medPoint.Point.X;
                PointObject newPointObt = new PointObject(newDot.ID, newDot);
                newPointList.Add(newPointObt);
            }
            return newPointList;
        }
        #endregion
        #region  计算点群的移位比率K
        public  double GetK(List<PointObject> pointObtlst)
        {
            double k=0.0;
            k = 5.5 / NearDistance(pointObtlst);
            return k;
        }
        #endregion
        #region 计算距离中心点最近的距离
         public double NearDistance(List<PointObject> pointObtlst)
         {

             PointObject point1 = MedPoint(pointObtlst);
             double MinDistance = 10000000000;
             for (int i = 0; i < pointObtlst.Count; i++)
             {
                 double distance1 = Point_getDistance(point1, pointObtlst[i]);
                 if (MinDistance > distance1)
                     MinDistance = distance1;
             }
             return MinDistance;
         }
         #endregion
        #region 计算聚类簇的中心点
         public PointObject  MedPoint(List<PointObject> pointObtlst)
         {
             PointObject point1 = null;
             double sumX = 0.0, sumY = 0.0;
            
             for (int i = 0; i < pointObtlst.Count; i++)
             {
                 sumX += pointObtlst[i].Point.X;
                 sumY += pointObtlst[i].Point.Y;

             }
             TriDot newDot = new TriDot(sumX / pointObtlst.Count, sumY / pointObtlst.Count);
             newDot.ID = 999999;
             point1 = new PointObject(newDot.ID,newDot);
             return point1;
         }
         #endregion
        #region  计算点对象欧几何距离
         public double Point_getDistance(PointObject dpA, PointObject dpB)
         {
             double distance = 0;
             distance = Math.Sqrt((dpA.Point.X - dpB.Point.X) * (dpA.Point.X - dpB.Point.X) + (dpA.Point.Y - dpB.Point.Y) * (dpA.Point.Y - dpB.Point.Y));
             return distance;
         }
         #endregion
        public  double TriDot_getDistance(TriDot dpA, TriDot dpB)
         {
             double distance = 0;
             distance = Math.Sqrt((dpA.X - dpB.X) * (dpA.X - dpB.X) + (dpA.Y - dpB.Y) * (dpA.Y - dpB.Y));
             return distance;

         }
        #region 计算点群的移位量
         public double Points_DisplcementDistance(PointObject point, List<PointObject> pointList, double scalCoefficient)
         {

             PointObject point1 = MedPoint(pointList);

             double MaxDis = FastDistance(pointList);
             double MinDis = NearDistance(pointList);
             double distance = Point_getDistance(point, point1);
             double displaceDis = scalCoefficient * distance * Math.Exp(1 * (MinDis - distance) / (MaxDis - MinDis)) * (0.5 + (0.6 * (distance - MinDis) / (MaxDis - MinDis)));
           
             return displaceDis;
         }
         #endregion
        #region 计算距离中心点最远的距离
         public  double FastDistance(List<PointObject> pointList)
         {

             PointObject point1 = MedPoint(pointList);
             double MaxDistance = 0.0;
             for (int i = 0; i < pointList.Count; i++)
             {
                 double distance1 = Point_getDistance(point1, pointList[i]);
                 if (MaxDistance < distance1)
                     MaxDistance = distance1;
             }
             return MaxDistance;
         }
         #endregion
        #region 返回每个群的中心点
         public List<PointObject> ReturnMedPoint(List<PolygonCluster> mCluster)
         {
             GeoCalculation Gc = new GeoCalculation();
             List<PointObject> pointLst = new List<PointObject>();
             for (int i = 0; i < mCluster.Count; i++)
             {
                 if (mCluster[i].polygonList.Count>0)
                 {
                 PointObject medPoint = Gc.Get_AreaCenterPoint(mCluster[i].polygonList);
                 pointLst.Add(medPoint);
                 }
             }
             return pointLst;

         }
         #endregion
        #region 返回两条线之间的交点
         public PointObject CrossPoint(PolylineObject line1, PolylineObject line2)
         {
             int line1_Count = line1.PointList.Count;
             int line2_Count = line2.PointList.Count;
             PointObject crsPot = null;
             TriDot point1_firstPoint = null;
             TriDot point1_lastPoint = null;
             TriDot point2_firstPoint = null;
             TriDot point2_lastPoint = null;
             point1_firstPoint = line1.PointList[0];
             point1_lastPoint = line1.PointList[line1_Count - 1];
             point2_firstPoint = line2.PointList[0];
             point2_lastPoint = line2.PointList[line2_Count - 1];
             if ((Equal(point1_firstPoint, point2_firstPoint)) || (Equal(point1_firstPoint, point2_lastPoint)))
             {
                 crsPot = new PointObject(point1_firstPoint.ID, point1_firstPoint);

             }
             else
             {
                 crsPot = new PointObject(point1_lastPoint.ID, point1_lastPoint);
             }

             return crsPot;

         }
         #endregion
        #region 判断两个点是否相等
         public bool Equal(TriDot dot1, Dot dot2)
         {
             if ((dot1.X == dot2.X) && (dot1.Y == dot2.Y))
                 return true;
             else

                 return false;

         }
         #endregion
        #region 判断一个数组中是否所有的数都等于a，是返回true,不是返回false
         public bool numEqualAllA(List<int> num, int a)
         {
             bool sign = true;
             for (int i = 0; i < num.Count; i++)
             {
                 if (num[i] != a)
                 {
                     sign = false;
                     break;
                 }
             }
             return sign;
         }
         #endregion       
    }
}
