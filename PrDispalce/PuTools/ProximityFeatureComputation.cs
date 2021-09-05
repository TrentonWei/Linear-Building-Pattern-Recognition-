using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Controls;

namespace PrDispalce.工具类
{
    class ProximityFeatureComputation //邻近建筑物相对空间特征计算
    {
        ParameterCompute PC = new ParameterCompute();
        BuildingGroupFeatureComputation BGFC = new BuildingGroupFeatureComputation();

        /// <summary>
        /// 大小关系计算(大面积与小面积的比值                                                                                            )
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <returns></returns>
        public double SizeRelation(PolygonObject Po1,PolygonObject Po2)
        {
            double sizer = 0;

            double Area1 = Po1.Area;
            double Area2 = Po2.Area;

            double MaxArea = 0; double MinArea = 0;
            if (Area1 > Area2)
            {
                MaxArea = Area1;
                MinArea = Area2;
            }
            else
            {
                MaxArea = Area2;
                MinArea = Area1;
            }

            sizer = MaxArea / MinArea;

            return sizer;
        }

        /// <summary>
        /// 方向关系计算
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <returns></returns>
        public double OrientationRelation(PolygonObject Po1, PolygonObject Po2)
        {
            double Orir = 0;

            double OriPo1 = PC.GetSMBROrientation(Po1);//(0,360)
            double OriPo2 = PC.GetSMBROrientation(Po2);//(0,360)
            Orir = Math.Abs(OriPo1 - OriPo2);

            if (Orir > 90)
            {
                Orir = 180 - Orir;
            }

            return Orir;
        }

        /// <summary>
        /// IPQCom计算
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <returns></returns>
        public double IPQComRelation(PolygonObject Po1, PolygonObject Po2)
        {
            double IPQr = 0;

            double IPQCom1 = PC.GetIPQCom(Po1);
            double IPQCom2 = PC.GetIPQCom(Po2);
            IPQr = Math.Abs(IPQCom1 - IPQCom2);

            return IPQr;
        }

        /// <summary>
        /// EdgeCount计算（大边比小边）
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <returns></returns>
        public double EdgeCountRelation(PolygonObject Po1, PolygonObject Po2)
        {
            double Edger = 0;

            double EdgeCount1 = Po1.PointList.Count;
            double EdgeCount2 = Po2.PointList.Count;

            double MaxEdgeCount = Math.Max(EdgeCount1, EdgeCount2);
            double MinEdgeCount = Math.Min(EdgeCount1, EdgeCount2);
          
            Edger = MaxEdgeCount / MinEdgeCount;
            return Edger;
        }

        /// <summary>
        /// Convex关系计算
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <returns></returns>
        public double ConvexRelation(PolygonObject Po1, PolygonObject Po2)
        {
            double Convexr = 0;

            List<PolygonObject> PolygonList = new List<PolygonObject>();
            PolygonList.Add(Po1); PolygonList.Add(Po2);

            IPolygon ConvexPolygon = BGFC.ConvexPolygon(PolygonList);
            IArea pArea = ConvexPolygon as IArea;
            double ConvexArea = pArea.Area;//凸包面积
            double AreaSum = BGFC.TotalArea(PolygonList);
            Convexr = AreaSum / ConvexArea;

            return Convexr;
        }

        /// <summary>
        /// smbr关系计算
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <returns></returns>
        public double smbrRelation(PolygonObject Po1, PolygonObject Po2)
        {
            double smbrr = 0;

            List<PolygonObject> PolygonList = new List<PolygonObject>();
            PolygonList.Add(Po1); PolygonList.Add(Po2);

            IPolygon ConvexPolygon = BGFC.ConvexPolygon(PolygonList);
            IPolygon SMBR = PC.GetSMBR(ConvexPolygon);//绑定矩形
            IArea sArea = SMBR as IArea;
            double smbrArea = sArea.Area;//凸包面积
            double AreaSum = BGFC.TotalArea(PolygonList);
            smbrr = AreaSum / smbrArea;

            return smbrr;
        }

        /// <summary>
        /// 正对面积关系计算
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <returns></returns>
        public double FaceRelation(PolygonObject Po1, PolygonObject Po2)
        {
            double Facer = 0;
            Facer = PC.GetFaceRatio(Po1, Po2);
            return Facer;
        }
    }
}
