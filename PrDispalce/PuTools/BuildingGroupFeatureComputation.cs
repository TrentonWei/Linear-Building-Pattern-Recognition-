using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Controls;

namespace PrDispalce.工具类
{
    class BuildingGroupFeatureComputation //群组建筑物空间特征计算
    {
        ParameterCompute PC = new ParameterCompute();

        /// <summary>
        /// 计算建筑物群组的面积平均值
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
       public double AreaAverage(List<PolygonObject> PolygonList)
        {
            double AreaSum = this.TotalArea(PolygonList);
            double  AreaAverage = AreaSum / PolygonList.Count;
            return AreaAverage;
        }

        /// <summary>
        /// 计算给定建筑物群组的总面积
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double TotalArea(List<PolygonObject> PolygonList)
        {
            double AreaSum = 0;
            for (int i = 0; i < PolygonList.Count; i++)
            {
                AreaSum = AreaSum + PolygonList[i].Area;
            }

            return AreaSum;
        }

        /// <summary>
        /// 计算给定建筑物群组的面积差异(标准差)
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double AreaDiff(List<PolygonObject> PolygonList)
        {
            double AreaDiff=0;
            double AreaAverage = this.AreaAverage(PolygonList);//平均值

            double DiffSum = 0;
            for (int i = 0; i < PolygonList.Count; i++)
            {
                DiffSum = DiffSum + (PolygonList[i].Area - AreaAverage) * (PolygonList[i].Area - AreaAverage);
            }

            AreaDiff = Math.Sqrt(DiffSum / PolygonList.Count);
            return AreaDiff;
        }

        /// <summary>
        /// 计算给定建筑物群组的面积差异（变异系数）
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double VarAreaDiff(List<PolygonObject> PolygonList)
        {
            double AverageArea = this.AreaAverage(PolygonList);
            double AreaDiff = this.AreaDiff(PolygonList);
            double VarAreaDiff = AreaDiff / AverageArea;
            return VarAreaDiff;
        }

        /// <summary>
        /// 获得给定点集的凸包
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public IPolygon ConvexPolygon(List<PolygonObject> PolygonList)
        {
            IPointCollection pPointCollection = new MultipointClass();

            for (int i = 0; i < PolygonList.Count; i++)
            {
                for (int j = 0; j < PolygonList[i].PointList.Count; j++)
                {
                    IPoint Point = new PointClass();
                    Point.X = PolygonList[i].PointList[j].X;
                    Point.Y = PolygonList[i].PointList[j].Y;

                    pPointCollection.AddPoint(Point);
                }
            }

            ITopologicalOperator iTopo = pPointCollection as ITopologicalOperator;
            IGeometry pConvexHull = iTopo.ConvexHull();
            IPolygon ConvexPolygon = pConvexHull as IPolygon;
            return ConvexPolygon;
        }

        /// <summary>
        /// 计算建筑物群的凸包黑白面积比
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double BlackWhiteRatio(List<PolygonObject> PolygonList)
        {
            double BlackWhiteRatio = 0;
      
            IPolygon ConvexPolygon = this.ConvexPolygon(PolygonList);
            IArea pArea=ConvexPolygon as IArea;
            double ConvexArea=pArea.Area;//凸包面积

            double AreaSum = this.TotalArea(PolygonList);
            BlackWhiteRatio = AreaSum / ConvexArea;
            return BlackWhiteRatio;
        }

        /// <summary>
        /// 计算绑定矩形黑白面积比
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double smbrRatio(List<PolygonObject> PolygonList)
        {
            double smbrRatio = 0;

            IPolygon ConvexPolygon = this.ConvexPolygon(PolygonList);//凸包
            IPolygon SMBR = PC.GetSMBR(ConvexPolygon);//绑定矩形
            IArea sArea = SMBR as IArea;
            double smbrArea = sArea.Area;//凸包面积
            double AreaSum = this.TotalArea(PolygonList);
            smbrRatio = AreaSum / smbrArea; ;

            return smbrRatio;
        }

        /// <summary>
        /// 计算给定建筑物群组的方向差异（同时，需要说明的是每一个给定的建筑物记录其邻近的建筑物列表）
        /// 【说明1：这里目前考虑的是一阶RNG邻近】【说明2：方向在[0,180]】（参考均值偏差的概念）
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double SMBRODiff1(List<PolygonObject> PolygonList)
        {
            double SMBRODiff1 = 0;

            int Count = 0; double SMBROdSum = 0;
            for (int i = 0; i < PolygonList.Count; i++)
            {
                double OriSMBRO = PC.GetSMBROrientation(PolygonList[i]);

                for (int j = 0; j < PolygonList[i].RNGProximity1.Count; j++)
                {
                    double TarSMBRO = PC.GetSMBROrientation(PolygonList[i].RNGProximity1[j]);
                    double SMBROd = Math.Abs(OriSMBRO - TarSMBRO);
                    SMBROdSum = SMBROdSum + SMBROd;
                    Count++;
                }
            }

            SMBRODiff1 = SMBROdSum / Count;
            return SMBRODiff1;
        }

        /// <summary>
        /// 计算给定建筑物群组的方向平均差异（同时，需要说明的是每一个给定的建筑物记录其邻近的建筑物列表）
        /// 【说明1：这里目前考虑的是一阶RNG邻近】【说明2：方向在[0,90]】（参考均值偏差的概念）
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double SMBRODiff2(List<PolygonObject> PolygonList)
        {
            double SMBRODiff2 = 0;

            int Count = 0; double SMBROdSum = 0;
            for (int i = 0; i < PolygonList.Count; i++)
            {
                double OriSMBRO = PC.GetSMBROrientation(PolygonList[i]);

                for (int j = 0; j < PolygonList[i].RNGProximity1.Count; j++)
                {
                    double TarSMBRO = PC.GetSMBROrientation(PolygonList[i].RNGProximity1[j]);
                    double SMBROd = Math.Abs(OriSMBRO - TarSMBRO);

                    if (SMBROd > 90)
                    {
                        SMBROd = 180 - SMBROd;
                    }

                    SMBROdSum = SMBROdSum + SMBROd;
                    Count++;
                }
            }

            SMBRODiff2 = SMBROdSum / Count;
            return SMBRODiff2;
        }

        /// <summary>
        /// 计算邻近建筑物间的平均距离（定义邻近的邻近距离）
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double AverageDistance(List<PolygonObject> PolygonList)
        {
            double AverageDistance = 0;

            int Count = 0; double DistanceSum = 0;
            for (int i = 0; i < PolygonList.Count; i++)
            {
                PolygonObject Po1 = PolygonList[i];

                for (int j = 0; j < PolygonList[i].RNGProximity1.Count; j++)
                {
                    PolygonObject Po2 = PolygonList[i].RNGProximity1[j];
                    double Distance = this.NearDistance(Po1, Po2);
                    DistanceSum = DistanceSum + Distance;
                    Count++;
                }
            }

            AverageDistance = DistanceSum / Count;
            return AverageDistance;
        }

        /// <summary>
        /// 计算给定群组建筑物距离的标准差（定义邻近的邻近距离）
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double DistanceDiff(List<PolygonObject> PolygonList)
        {
            double AverageDistance = this.AverageDistance(PolygonList);

            #region 距离的标准差
            double DiffSum = 0; int Count = 0;
            for (int i = 0; i < PolygonList.Count; i++)
            {
                PolygonObject Po1 = PolygonList[i];

                for (int j = 0; j < PolygonList[i].RNGProximity1.Count; j++)
                {
                    PolygonObject Po2 = PolygonList[i].RNGProximity1[j];
                    double Distance = this.NearDistance(Po1, Po2);

                    DiffSum = DiffSum + (Distance - AverageDistance) * (Distance - AverageDistance);
                    Count++;
                }                   
            }

            double DistanceDiff = Math.Sqrt(DiffSum / Count);
            #endregion

            return DistanceDiff;
        }

        /// <summary>
        /// 计算给定群组建筑物距离的变异系数（定义邻近的邻近距离）
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double VarDistanceDiff(List<PolygonObject> PolygonList)
        {
            double AverageDistance = this.AverageDistance(PolygonList);
            double DistanceDiff = this.DistanceDiff(PolygonList);
            double VarDistanceDiff = DistanceDiff / AverageDistance;

            return VarDistanceDiff;
        }

        /// <summary>
        /// 计算两个建筑物间的最短距离
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <returns></returns>
        public double NearDistance(PolygonObject Po1,PolygonObject Po2)
        {
            IPolygon pPolygon1 = PC.ObjectConvert(Po1);
            IPolygon pPolygon2 = PC.ObjectConvert(Po2);

            IProximityOperator IPo = pPolygon1 as IProximityOperator;
            double NearDistance = IPo.ReturnDistance(pPolygon2 as IGeometry);

            return NearDistance;
        }

        /// <summary>
        /// 获得给定群组建筑物的边数差异（标准差）
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double EdgeCountDiff(List<PolygonObject> PolygonList)
        {
            double EdgeCountDiff = 0;

            #region 边数平均值
            double EdgeCountSum = 0;
            for (int i = 0; i < PolygonList.Count; i++)
            {
                EdgeCountSum = EdgeCountSum + PolygonList[i].PointList.Count;
            }
            double EdgeCountAverage = EdgeCountSum / PolygonList.Count;
            #endregion

            #region 边数标准差
            double DiffSum = 0;
            for (int i = 0; i < PolygonList.Count; i++)
            {
                DiffSum = DiffSum + (PolygonList[i].PointList.Count - EdgeCountAverage) * (PolygonList[i].PointList.Count - EdgeCountAverage);
            }

            EdgeCountDiff = Math.Sqrt(DiffSum / PolygonList.Count);
            #endregion

            return EdgeCountDiff;
        }

        /// <summary>
        /// 获得给定群组建筑物的边数变异系数（标准差/平均值）
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double VarEdgeCountDiff(List<PolygonObject> PolygonList)
        {
            #region 边数平均值
            double EdgeCountSum = 0;
            for (int i = 0; i < PolygonList.Count; i++)
            {
                EdgeCountSum = EdgeCountSum + PolygonList[i].PointList.Count;
            }
            double EdgeCountAverage = EdgeCountSum / PolygonList.Count;
            #endregion

            double EdgeCountDiff = this.EdgeCountDiff(PolygonList);
            double VarEdgeCountDiff = EdgeCountDiff / EdgeCountAverage;
            return VarEdgeCountDiff;
        }

        /// <summary>
        /// 获得给定群组建筑物的IPQCom标准差
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double IPQComDiff(List<PolygonObject> PolygonList)
        {
            double IPQComDiff = 0;
            double IPQComAverage = this.IPQComAverage(PolygonList);

            #region IPQCom标准差
            double DiffSum = 0;
            for (int i = 0; i < PolygonList.Count; i++)
            {
                DiffSum = DiffSum + (PC.GetIPQCom(PolygonList[i]) - IPQComAverage) * (PC.GetIPQCom(PolygonList[i]) - IPQComAverage);
            }

            IPQComDiff = Math.Sqrt(DiffSum / PolygonList.Count);
            #endregion

            return IPQComDiff;
        }

        /// <summary>
        /// 获得给定群组建筑物中邻近IPQCom差异的平均值(参考均值偏差)
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double AveIPQComDiff(List<PolygonObject> PolygonList)
        {
            double AveIPQComSum = 0; int Count = 0;
            for (int i = 0; i < PolygonList.Count; i++)
            {
                double IPQComPo1=PC.GetIPQCom(PolygonList[i]);

                for (int j = 0; j < PolygonList[i].RNGProximity1.Count; j++)
                {
                    double IPQComPo2=PC.GetIPQCom(PolygonList[i].RNGProximity1[j]);
                    AveIPQComSum = AveIPQComSum + Math.Abs(IPQComPo1 - IPQComPo2);
                    Count++;
                }
            }

            double AveIPQComDiff = AveIPQComSum / Count;

            return AveIPQComDiff;
        }

        /// <summary>
        /// 获得给定群组建筑物的IPQCom平均值
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double IPQComAverage(List<PolygonObject> PolygonList)
        {
            double IPQComSum = 0;
            for (int i = 0; i < PolygonList.Count; i++)
            {
                IPQComSum = IPQComSum + PC.GetIPQCom(PolygonList[i]);
            }
            double IPQComAverage = IPQComSum / PolygonList.Count;
            return IPQComAverage;
        }

        /// <summary>
        /// 获得邻近正对面积的平均值
        /// </summary>
        /// <returns></returns>
        public double RatioAverage(List<PolygonObject> PolygonList)
        {
            double RatioAverage = 0;

            int Count = 0; double RatioSum = 0;
            for (int i = 0; i < PolygonList.Count; i++)
            {
                PolygonObject Po1 = PolygonList[i];

                for (int j = 0; j < PolygonList[i].RNGProximity1.Count; j++)
                {
                    PolygonObject Po2 = PolygonList[i].RNGProximity1[j];
                    double Ratio=PC.GetFaceRatio(Po1, Po2);
                    RatioSum = RatioSum + Ratio;
                    Count++;
                }
            }

            RatioAverage = RatioSum / Count;
            return RatioAverage;
        }

    }
}
