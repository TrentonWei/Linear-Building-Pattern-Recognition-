using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AuxStructureLib;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Data;
using AuxStructureLib.IO;
using AuxStructureLib.ConflictLib;

namespace PrDispalce.工具类.CollabrativeDisplacement
{
    class DisplacementEvaluation
    {
        /// <summary>
        /// 是否还存在道路冲突
        /// </summary>
        /// <returns></returns>
        public bool LegibleConstraintsEvaluation1(List<ProxiEdge> ConflictEdge)
        {
            bool Deny = false;
            if (ConflictEdge != null)
            {
                for (int i = 0; i < ConflictEdge.Count; i++)
                {
                    ProxiEdge Pe = ConflictEdge[i];

                    if (Pe.Node1.FeatureType == FeatureType.PolylineType || Pe.Node2.FeatureType == FeatureType.PolylineType)
                    {
                        Deny = true;
                        break;
                    }
                }
            }
            return Deny;
        }

        /// <summary>
        /// 判断两个给定的建筑物是否还有冲突
        /// </summary>
        /// <param name="ID1"></param>冲突建筑物ID
        /// <param name="ID2"></param>冲突建筑物ID
        /// <param name="ConflictEdge"></param>冲突探测后得到的冲突边
        /// <returns></returns>
        public bool LegibleConstraintsEvaluation2(int ID1,int ID2,List<ProxiEdge> ConflictEdge)
        {
            bool Deny = false;
            if (ConflictEdge != null)
            {
                for (int i = 0; i < ConflictEdge.Count; i++)
                {
                    ProxiEdge Pe = ConflictEdge[i];
                    if (Pe.Node1.FeatureType == FeatureType.PolygonType && Pe.Node2.FeatureType == FeatureType.PolygonType)
                    {
                        if ((Pe.Node1.ID == ID1 && Pe.Node2.ID == ID2) || Pe.Node1.ID == ID2 && Pe.Node2.ID == ID1)
                        {
                            Deny = true;
                            break;
                        }
                    }
                }
            }
            
            return Deny;
        }

        /// <summary>
        /// 计算是否满足位置精度约束
        /// </summary>
        /// <param name="OriginalPolygonList"></param> 原始多边形集合
        /// <param name="DisplacedPolygonList"></param> 移位后多边形集合
        /// <param name="PositionalThreshold"></param> 位置精度约束
        /// <returns></returns>
        public bool PositonalAccuracyConstraintsEvalution(List<PolygonObject> OriginalPolygonList,List<PolygonObject> DisplacedPolygonList,double PositionalThreshold)
        {  
            bool Deny = false;
            for (int i = 0; i < DisplacedPolygonList.Count; i++)
            {               
                #region 是合并后的建筑物
                if (DisplacedPolygonList[i].IDList != null)
                {
                    #region 计算合并前建筑物的中心
                    double sumX = 0; double sumY = 0;
                    for (int m = 0; m < DisplacedPolygonList[i].IDList.Count; m++)
                    {
                        for (int n = 0; n < OriginalPolygonList.Count; n++)
                        {
                            if (OriginalPolygonList[n].ID == DisplacedPolygonList[i].IDList[m])
                            {
                                ProxiNode DPn = OriginalPolygonList[n].CalProxiNode();
                                sumX = sumX + DPn.X; sumY = sumY + DPn.Y;
                            }
                        }
                    }

                    double CenterX = sumX / DisplacedPolygonList[i].IDList.Count;
                    double CenterY = sumY / DisplacedPolygonList[i].IDList.Count;
                    #endregion

                    ProxiNode OPn = OriginalPolygonList[i].CalProxiNode();
                    double DisplaceDistance = Math.Sqrt((OPn.X - CenterX) * (OPn.X - CenterX) + (OPn.Y - CenterY) * (OPn.Y - CenterY));
                    if (DisplaceDistance > PositionalThreshold)
                    {
                        Deny = true;
                    }                  
                }
                #endregion

                #region 不是合并后的建筑物
                else
                {                 
                    ProxiNode DPn = DisplacedPolygonList[i].CalProxiNode();
                    for(int j=0;j<OriginalPolygonList.Count;j++)
                    {
                        if (DisplacedPolygonList[i].ID == OriginalPolygonList[j].ID)
                        {
                            ProxiNode OPn = OriginalPolygonList[j].CalProxiNode();

                            double DisplaceDistance = Math.Sqrt((DPn.X - OPn.X) * (DPn.X - OPn.X) + (DPn.Y - OPn.Y) * (DPn.Y - OPn.Y));
                            if (DisplaceDistance > PositionalThreshold)
                            {
                                Deny = true;
                                break;
                            }
                        }
                    }
                }
                #endregion

                if (Deny)
                {
                    break;
                }              
            }            
            return Deny;
        }

        /// <summary>
        /// 计算是否满足关系约束
        /// </summary>
        /// <param name="vd"></param> 原始V图
        /// <param name="DisplacedPolygonList"></param> 移位后建筑物集合
        /// <returns></returns>
        public bool RelationConstraintsEvalution(VoronoiDiagram vd,List<PolygonObject> DisplacedPolygonList)
        {
            bool Deny = false;
            for (int i = 0; i < DisplacedPolygonList.Count; i++)
            {
                #region 是合并后建筑物
                if (DisplacedPolygonList[i].IDList != null)
                {
                    ProxiNode DPn = DisplacedPolygonList[i].CalProxiNode();
                    for (int j = 0; j < DisplacedPolygonList[i].IDList.Count; j++)
                    {
                        for (int m = 0; m < vd.VorPolygonList.Count; m++)
                        {
                            if (DisplacedPolygonList[i].IDList[j] == vd.VorPolygonList[m].MapObj.ID)
                            {
                                IPoint pPoint = new PointClass();
                                pPoint.PutCoords(DPn.X, DPn.Y);
                                IPolygon pPolygon = ObjectConvert(vd.VorPolygonList[j]);

                                ITopologicalOperator iTopo = DPn as ITopologicalOperator;
                                IGeometry pGeo = iTopo.Intersect(pPolygon, esriGeometryDimension.esriGeometry0Dimension);

                                if (pGeo.IsEmpty)
                                {
                                    Deny = true;
                                    break;
                                }
                            }
                        }

                        if (Deny)
                        {
                            break;
                        }
                    }
                }
                #endregion

                #region 不是合并后建筑物
                else
                {
                    ProxiNode DPn = DisplacedPolygonList[i].CalProxiNode();
                    for (int j = 0; j < vd.VorPolygonList.Count; j++)
                    {
                        if (DisplacedPolygonList[i].ID == vd.VorPolygonList[j].MapObj.ID)
                        {
                            IPoint pPoint = new PointClass();
                            pPoint.PutCoords(DPn.X, DPn.Y);
                            IPolygon pPolygon=ObjectConvert(vd.VorPolygonList[j]);

                            ITopologicalOperator iTopo = DPn as ITopologicalOperator;
                            IGeometry pGeo = iTopo.Intersect(pPolygon, esriGeometryDimension.esriGeometry0Dimension);

                            if (pGeo.IsEmpty)
                            {
                                Deny = true;
                                break;
                            }
                        }
                    }
                }
                #endregion

                if (Deny)
                {
                    break;
                }
            }
            return Deny;
        }

        /// <summary>
        /// 将VoronoiPolygon转化为Polygon
        /// </summary>
        /// <param name="pPolygonObject"></param>
        /// <returns></returns>
        public IPolygon ObjectConvert(VoronoiPolygon Vp)
        {
            Ring ring1 = new RingClass();
            object missing = Type.Missing;

            IPoint curResultPoint = new PointClass();
            TriNode curPoint = null;
            if (Vp != null)
            {
                for (int i = 0; i < Vp.PointSet.Count; i++)
                {
                    curPoint = Vp.PointSet[i];
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
    }
}
