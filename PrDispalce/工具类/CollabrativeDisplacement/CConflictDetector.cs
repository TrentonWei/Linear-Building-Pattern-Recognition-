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
using PrDispalce.典型化;

namespace PrDispalce.工具类.CollabrativeDisplacement
{
    class CConflictDetector
    {
        //存储存在冲突的邻近边
        public List<ProxiEdge> ConflictEdge = null;

        /// <summary>
        /// 通过邻近图探测冲突；（1、道路与建筑物（相交-冲突；不相交，小于阈值-冲突）；2、建筑物与建筑物（相交-冲突）；不相交，小于阈值-冲突）
        /// </summary>
        /// <param name="PeList"></param>
        /// <param name="ThresholdPr"></param> 建筑物与道路冲突
        /// <param name="ThresholdPP"></param> 建筑物与建筑物冲突
        /// <param name="PolygonList"></param>
        public void ConflictDetectByPg(List<ProxiEdge> PeList,double ThresholdPr,double ThresholdPP,List<PolygonObject> PolygonList,List<PolylineObject> PolylineList)
        {
            ConflictEdge = new List<ProxiEdge>();

            foreach (ProxiEdge edge in PeList)
            {
                if (edge == null)
                    continue;

                edge.intersectedConflict = false;
                ProxiNode Pn1 = edge.Node1;
                ProxiNode Pn2 = edge.Node2;
                bool Label = false;

                #region 建筑物与道路冲突
                if (Pn1.FeatureType != Pn2.FeatureType)
                {
                    //Linear conflict
                    if (!(edge.Ske_Arc.LeftMapObj.FeatureType == FeatureType.PolylineType
                        && edge.Ske_Arc.RightMapObj.FeatureType == FeatureType.PolylineType))
                    {
                        if (edge.Ske_Arc.LeftMapObj.TypeID == 1 && edge.Ske_Arc.RightMapObj.TypeID == 1)//4-10排除Buffer边界线，TypeID==2
                        {

                            if (edge.NearestEdge.NearestDistance < ThresholdPr)
                            {
                                //edge.intersectedConflict = true;
                                ConflictEdge.Add(edge);
                            }
                        }
                    }
                }
                #endregion

                #region 建筑物与建筑物冲突
                else
                {
                    for (int i = 0; i < PolygonList.Count; i++)
                    {
                        PolygonObject PolygonObject1 = PolygonList[i];
                        if (Pn1.TagID == PolygonObject1.ID && Pn1.FeatureType == FeatureType.PolygonType)
                        {
                            for (int j = 0; j < PolygonList.Count; j++)
                            {
                                if (j != i)
                                {
                                    PolygonObject PolygonObject2 = PolygonList[j];
                                    if (Pn2.TagID == PolygonObject2.ID && Pn2.FeatureType == FeatureType.PolygonType)
                                    {
                                        Label = true;

                                        #region 将polygonobject转换为ipolygon
                                        IPolygon pPolygon1 = ObjectConvert(PolygonObject1);
                                        IPolygon pPolygon2 = ObjectConvert(PolygonObject2);
                                        #endregion

                                        IRelationalOperator iRe = pPolygon1 as IRelationalOperator;
                                        bool isIntersects = iRe.Touches(pPolygon2 as IGeometry);

                                        #region 如果两个建筑物相交
                                        if (isIntersects)
                                        {
                                            edge.intersectedConflict = true;
                                            this.ConflictEdge.Add(edge);
                                        }
                                        #endregion

                                        #region 如果两个建筑物不相交
                                        else
                                        {
                                            //Linear conflict
                                            if (!(edge.Ske_Arc.LeftMapObj.FeatureType == FeatureType.PolylineType
                                                && edge.Ske_Arc.RightMapObj.FeatureType == FeatureType.PolylineType))
                                            {
                                                if (edge.Ske_Arc.LeftMapObj.TypeID == 1 && edge.Ske_Arc.RightMapObj.TypeID == 1)//4-10排除Buffer边界线，TypeID==2
                                                {

                                                    if (edge.NearestEdge.NearestDistance < ThresholdPP)
                                                    {
                                                        ConflictEdge.Add(edge);
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
                                    }
                                }

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
                } 
                #endregion
            }
        }

        /// <summary>
        /// 通过邻近图探测pattern间的冲突（建筑物与建筑物（相交-冲突）；不相交，小于阈值-冲突）
        /// </summary>
        /// <param name="PeList"></param>
        /// <param name="ThresholdPP"></param>
        /// <param name="PatternList"></param>
        public void ConflictDetectByPg(List<ProxiEdge> PeList, double ThresholdPP, List<Pattern> PatternList,SMap Map,double Scale)
        {
              ConflictEdge = new List<ProxiEdge>();

              foreach (ProxiEdge edge in PeList)
              {
                  if (edge == null)
                      continue;

                  edge.intersectedConflict = false;
                  ProxiNode Pn1 = edge.Node1;
                  ProxiNode Pn2 = edge.Node2;
                  bool Label = false;

                  //保证是建筑物相连的两条边
                  if (Pn1.FeatureType == Pn2.FeatureType)
                  {
                      bool PatternInEdge = false;
                      for (int i = 0; i < PatternList.Count; i++)
                      {
                          List<int> PatternPolygonIDList = new List<int>();
                          for (int j = 0; j < PatternList[i].PatternObjects.Count; j++)
                          {
                              PatternPolygonIDList.Add(PatternList[i].PatternObjects[j].ID);
                          }

                          //保证不是同一个pattern上的两个建筑物
                          if (PatternPolygonIDList.Contains(Pn1.TagID) && PatternPolygonIDList.Contains(Pn2.TagID))
                          {
                              PatternInEdge = true;
                          }
                      }

                      #region 如果不是pattern内的边
                      if (!PatternInEdge)
                      {
                          for (int i = 0; i < Map.PolygonList.Count; i++)
                          {
                              PolygonObject PolygonObject1 =this.SymbolizedPolygon(Map.PolygonList[i],Scale);

                              if (Pn1.TagID == PolygonObject1.ID && Pn1.FeatureType == FeatureType.PolygonType)
                              {
                                  for (int j = 0; j < Map.PolygonList.Count; j++)
                                  {
                                      if (j != i)
                                      {
                                          PolygonObject PolygonObject2 = this.SymbolizedPolygon(Map.PolygonList[j], Scale);
                                          if (Pn2.TagID == PolygonObject2.ID && Pn2.FeatureType == FeatureType.PolygonType)
                                          {
                                              Label = true;

                                              #region 将polygonobject转换为ipolygon
                                              IPolygon pPolygon1 = ObjectConvert(PolygonObject1);
                                              IPolygon pPolygon2 = ObjectConvert(PolygonObject2);
                                              #endregion

                                              IRelationalOperator iRe = pPolygon1 as IRelationalOperator;
                                              bool isIntersects = iRe.Touches(pPolygon2 as IGeometry);

                                              #region 如果两个建筑物相交
                                              if (isIntersects)
                                              {
                                                  edge.intersectedConflict = true;
                                                  this.ConflictEdge.Add(edge);
                                              }
                                              #endregion

                                              #region 如果两个建筑物不相交
                                              else
                                              {
                                                  IProximityOperator iPo = pPolygon1 as IProximityOperator;
                                                  double DisTance = iPo.ReturnDistance(pPolygon2 as IGeometry);

                                                  if (DisTance < ThresholdPP)
                                                  {
                                                      ConflictEdge.Add(edge);
                                                  }
                                              }
                                              #endregion
                                          }
                                      }

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
                      } 
                      #endregion
                  }                 
              }
        }

        /// <summary>
        /// 将polygonobject转化为Polygon
        /// </summary>
        /// <param name="pPolygonObject"></param>
        /// <returns></returns>
        public IPolygon ObjectConvert(PolygonObject pPolygonObject)
        {
            Ring ring1 = new RingClass();
            object missing = Type.Missing;

            IPoint curResultPoint = new PointClass();
            TriNode curPoint = null;
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

        /// <summary>
        /// 找到导致未被解决的次生冲突相关的冲突
        /// </summary>
        /// <param name="FormerConflictEdge"></param> 次生冲突
        /// <param name="LatterConflictEdge"></param> 解决次生后产生的新冲突
        /// <returns></returns>
        public List<ProxiEdge> ReturnInvolvedEdges(List<ProxiEdge> FormerConflictEdge,List<ProxiEdge> LatterConflictEdge)
        {
            List<ProxiEdge> InvolvedEdges = new List<ProxiEdge>();
            if (FormerConflictEdge.Count > 0)
            {
                for (int i = 0; i < LatterConflictEdge.Count; i++)
                {
                    ProxiEdge Pe1 = LatterConflictEdge[i];

                    //判断冲突边涉及的建筑物是不是在另一个冲突中即可
                    if (Pe1.Node1.FeatureType == FeatureType.PolygonType)
                    {
                        for (int j = FormerConflictEdge.Count-1; j >= 0; j--)
                        {
                            ProxiEdge Pe2 = FormerConflictEdge[j];

                            if ((Pe2.Node1.FeatureType == FeatureType.PolygonType && Pe1.Node1.TagID==Pe2.Node1.TagID)||
                                (Pe2.Node2.FeatureType==FeatureType.PolygonType&&Pe1.Node1.TagID==Pe2.Node2.TagID))
                            {
                                InvolvedEdges.Add(Pe2);
                                FormerConflictEdge.RemoveAt(j);
                            }
                        }
                    }

                    if (Pe1.Node2.FeatureType == FeatureType.PolygonType)
                    {
                        for (int j = FormerConflictEdge.Count-1; j >= 0; j--)
                        {
                            ProxiEdge Pe2 = FormerConflictEdge[j];

                            if ((Pe2.Node1.FeatureType == FeatureType.PolygonType && Pe1.Node2.TagID == Pe2.Node1.TagID) ||
                                (Pe2.Node2.FeatureType == FeatureType.PolygonType && Pe1.Node2.TagID == Pe2.Node2.TagID))
                            {
                                InvolvedEdges.Add(Pe2);
                                FormerConflictEdge.RemoveAt(j);
                            }
                        }
                    }
                }
            }

            return InvolvedEdges;
        }

        /// <summary>
        /// 找到局部移位后仍然未被解决的原生冲突
        /// </summary>
        /// <param name="FormerConflictEdge"></param> 原生冲突边集合
        /// <param name="LatterConflictEdge"></param> 移位后冲突边集合
        /// <returns></returns>
        public List<ProxiEdge> ReturnInvolvedInitialEdges(List<ProxiEdge> FormerConflictEdge, List<ProxiEdge> LatterConflictEdge)
        {
            List<ProxiEdge> InvolvedEdges = new List<ProxiEdge>();

            if (FormerConflictEdge.Count > 0)
            {
                for (int i = 0; i < LatterConflictEdge.Count; i++)
                {
                    ProxiEdge Pe1 = LatterConflictEdge[i];
                    
                    for (int j = FormerConflictEdge.Count - 1; j >= 0; j--)
                    {
                        ProxiEdge Pe2 = FormerConflictEdge[j];

                        if ((Pe1.Node1.FeatureType == Pe2.Node1.FeatureType && Pe1.Node1.TagID == Pe2.Node1.TagID &&Pe1.Node2.FeatureType == Pe2.Node2.FeatureType && Pe1.Node2.TagID == Pe2.Node2.TagID)||
                            (Pe1.Node1.FeatureType == Pe2.Node2.FeatureType && Pe1.Node1.TagID == Pe2.Node2.TagID && Pe1.Node2.FeatureType == Pe2.Node1.FeatureType && Pe1.Node2.TagID == Pe2.Node1.TagID))
                        {
                            InvolvedEdges.Add(Pe2);
                            FormerConflictEdge.RemoveAt(j);
                        }
                    }                   
                }
            }

            return InvolvedEdges;
        }

        /// <summary>
        /// 返回需要被优先解决的冲突
        /// </summary>
        /// <param name="InvolvedEdges"></param> 相关的次生冲突
        /// InitialConflictEdge 原生冲突列表
        /// <returns></returns>
        public TargetEdge ReturnTragetEdgeTobeSolved(List<ProxiEdge> InvolvedEdges,List<ProxiEdge> InitialConflictEdge)
        {
            TargetEdge pTargetEdge = new TargetEdge();

            if (InvolvedEdges.Count > 0)
            {
                ProxiEdge TargetEdge = InvolvedEdges[0];
                bool pInitalConflictLabel = false;

                for (int i = 1; i < InvolvedEdges.Count; i++)
                {
                    ProxiEdge Pe1 = InvolvedEdges[i];

                    bool tLabel=false;bool pLabel=false;

                    #region 判断是否是原生冲突
                    for (int j = 0; j < InitialConflictEdge.Count; j++)
                    {
                        ProxiEdge Pe2 = InitialConflictEdge[j];

                        if((Pe1.Node1.TagID==Pe2.Node1.TagID && Pe1.Node1.FeatureType==Pe2.Node1.FeatureType && Pe1.Node2.TagID==Pe2.Node2.TagID&&Pe1.Node2.FeatureType==Pe2.Node2.FeatureType)||
                             (Pe1.Node1.TagID==Pe2.Node2.TagID&&Pe1.Node1.FeatureType==Pe2.Node2.FeatureType&&Pe1.Node2.TagID==Pe2.Node1.TagID&&Pe1.Node2.FeatureType==Pe2.Node1.FeatureType))
                        {
                            pLabel = true;
                        }

                        if ((TargetEdge.Node1.TagID == Pe2.Node1.TagID && TargetEdge.Node1.FeatureType == Pe2.Node1.FeatureType && TargetEdge.Node2.TagID == Pe2.Node2.TagID && TargetEdge.Node2.FeatureType == Pe2.Node2.FeatureType) ||
                            (TargetEdge.Node1.TagID == Pe2.Node2.TagID && TargetEdge.Node1.FeatureType == Pe2.Node2.FeatureType && TargetEdge.Node2.TagID == Pe2.Node1.TagID && TargetEdge.Node2.FeatureType == Pe2.Node1.FeatureType))
                        {
                            tLabel = true;
                        }
                    }
                    #endregion

                    #region 当TargetEdge 是原生冲突时
                    if (tLabel)
                    {
                        #region 待检测边也是原生冲突
                        if (pLabel)
                        {
                            #region 当targetedge 是建筑物间冲突时
                            if (TargetEdge.Node1.FeatureType == FeatureType.PolygonType && TargetEdge.Node2.FeatureType == FeatureType.PolygonType)
                            {
                                #region 当待检测edge是建筑物间冲突时
                                if (Pe1.Node1.FeatureType == FeatureType.PolygonType && Pe1.Node2.FeatureType == FeatureType.PolygonType)
                                {
                                    #region 若Targetedge不是相交冲突
                                    if (!TargetEdge.intersectedConflict)
                                    {
                                        if (TargetEdge.NearestEdge.NearestDistance > Pe1.NearestEdge.NearestDistance)
                                        {
                                            TargetEdge = Pe1;
                                        }
                                    }
                                    #endregion
                                }
                                #endregion

                                #region 当待检测edge是建筑物与道路冲突时
                                else
                                {
                                    TargetEdge = Pe1;
                                }
                                #endregion
                            }
                            #endregion

                            #region 当Targetedge 是建筑物与道路冲突时
                            else
                            {
                                #region 当待检测edge是建筑物与道路冲突时
                                if (Pe1.Node1.FeatureType == FeatureType.PolylineType || Pe1.Node2.FeatureType == FeatureType.PolylineType)
                                {
                                    if (TargetEdge.NearestEdge.NearestDistance > Pe1.NearestEdge.NearestDistance)
                                    {
                                        TargetEdge = Pe1;
                                    }
                                }
                                #endregion
                            }
                            #endregion
                        }
                        #endregion

                        pInitalConflictLabel = true;
                    }
                    #endregion

                    #region 当TargetEdge 不是原生冲突时
                    else
                    {
                        #region 待检测边是原生冲突
                        if (pLabel)
                        {
                            TargetEdge = Pe1;
                            pInitalConflictLabel = true;
                        }
                        #endregion

                        #region 待检测边也不是原生冲突
                        else
                        {
                            #region 当targetedge 是建筑物间冲突时
                            if (TargetEdge.Node1.FeatureType == FeatureType.PolygonType && TargetEdge.Node2.FeatureType == FeatureType.PolygonType)
                            {
                                #region 当待检测edge是建筑物间冲突时
                                if (Pe1.Node1.FeatureType == FeatureType.PolygonType && Pe1.Node2.FeatureType == FeatureType.PolygonType)
                                {
                                    #region 若Targetedge不是相交冲突
                                    if (!TargetEdge.intersectedConflict)
                                    {
                                        if (TargetEdge.NearestEdge.NearestDistance > Pe1.NearestEdge.NearestDistance)
                                        {
                                            TargetEdge = Pe1;
                                        }
                                    }
                                    #endregion
                                }
                                #endregion

                                #region 当待检测edge是建筑物与道路冲突时
                                else
                                {
                                    TargetEdge = Pe1;
                                }
                                #endregion
                            }
                            #endregion

                            #region 当Targetedge 是建筑物与道路冲突时
                            else
                            {
                                #region 当待检测edge是建筑物与道路冲突时
                                if (Pe1.Node1.FeatureType == FeatureType.PolylineType || Pe1.Node2.FeatureType == FeatureType.PolylineType)
                                {
                                    if (TargetEdge.NearestEdge.NearestDistance > Pe1.NearestEdge.NearestDistance)
                                    {
                                        TargetEdge = Pe1;
                                    }
                                }
                                #endregion
                            }
                            #endregion

                            pInitalConflictLabel = false;
                        }
                        #endregion
                    }
                    #endregion
                }

                pTargetEdge.pTargetEdge = TargetEdge;
                pTargetEdge.InitialConflictLabel = pInitalConflictLabel;
            }

            return pTargetEdge;
        }

        /// <summary>
        /// 返回需要被优先解决的冲突
        /// </summary>
        /// <param name="InvolvedEdges"></param>
        /// <param name="PatternList"></param>
        /// <returns></returns>
        public PatternTargetEdge ReturnTragetEdgeTobeSolved(List<ProxiEdge> InvolvedEdges,List<Pattern> PatternList)
        {
            PatternTargetEdge pTargetEdge = new PatternTargetEdge();

            if (InvolvedEdges.Count > 0)
            {
                ProxiEdge TargetEdge = InvolvedEdges[0];

                #region 判断是端点冲突还是内点冲突
                bool InPolygon1 = false;//非内点
                bool InPolygon2 = false;//非内点
                for (int j = 0; j < PatternList.Count; j++)
                {
                    for (int m = 1; m < PatternList[j].PatternObjects.Count-1; m++)
                    {
                        if (TargetEdge.Node1.ID == PatternList[j].PatternObjects[m].ID)
                        {
                            InPolygon1 = true;
                        }

                        if (TargetEdge.Node2.ID == PatternList[j].PatternObjects[m].ID)
                        {
                            InPolygon2 = true;
                        }
                    }
                }

                int ConflictType = 1;//一个端点冲突
                if (InPolygon1 && InPolygon2)
                {
                    ConflictType = 2;//内点冲突
                }

                if (!InPolygon1 && !InPolygon2)
                {
                    ConflictType = 0;//只有端点冲突
                }
                #endregion

                for (int i = 1; i < InvolvedEdges.Count; i++)
                {
                    ProxiEdge Pe1 = InvolvedEdges[i];

                    #region 判断是端点冲突还是内点冲突
                    bool pInPolygon1 = false;//非内点
                    bool pInPolygon2 = false;//非内点
                    for (int j = 0; j < PatternList.Count; j++)
                    {
                        for (int m = 1; m < PatternList[j].PatternObjects.Count - 1; m++)
                        {
                            if (Pe1.Node1.ID == PatternList[j].PatternObjects[m].ID)
                            {
                                pInPolygon1 = true;
                            }

                            if (Pe1.Node2.ID == PatternList[j].PatternObjects[m].ID)
                            {
                                pInPolygon2 = true;
                            }
                        }
                    }

                    int pConflictType = 1;//一个端点冲突
                    if (pInPolygon1 && pInPolygon2)
                    {
                        pConflictType = 2;//内点冲突
                    }

                    if (!pInPolygon1 && !pInPolygon2)
                    {
                        pConflictType = 0;//只有端点冲突
                    }
                    #endregion

                    //两条判断边冲突相同
                    if (ConflictType == pConflictType)
                    {
                        if (TargetEdge.NearestEdge.NearestDistance > Pe1.NearestEdge.NearestDistance)
                        {
                            TargetEdge = Pe1;
                        }
                    }

                    //判断边是端点冲突，待判断边是内点冲突
                    else
                    {
                        if (ConflictType < pConflictType)
                        {
                            TargetEdge = Pe1;
                            ConflictType = pConflictType;
                        }
                    }
                }

                pTargetEdge.pTargetEdge = TargetEdge;
                pTargetEdge.ConflictType = ConflictType;
                return pTargetEdge;
            }

            else
            {
                return null;
            }
        }

        /// <summary>
        /// 返回需要被优先解决的冲突
        /// </summary>
        /// <param name="InvolvedEdges"></param>
        /// <returns></returns>
        public ProxiEdge ReturnEdgeTobeSolved(List<ProxiEdge> InvolvedEdges)
        {

            if (InvolvedEdges.Count > 0)
            {
                ProxiEdge pTargetEdge = InvolvedEdges[0];
                for (int i = 1; i < InvolvedEdges.Count; i++)
                {
                    ProxiEdge Pe1 = InvolvedEdges[i];

                    #region 当pTargetedge 是建筑物间冲突时
                    if (pTargetEdge.Node1.FeatureType == FeatureType.PolygonType && pTargetEdge.Node2.FeatureType == FeatureType.PolygonType)
                    {
                        #region 当待检测edge是建筑物间冲突时
                        if (Pe1.Node1.FeatureType == FeatureType.PolygonType && Pe1.Node2.FeatureType == FeatureType.PolygonType)
                        {
                            #region 若Targetedge不是相交冲突
                            if (!pTargetEdge.intersectedConflict)
                            {
                                if (pTargetEdge.NearestEdge.NearestDistance > Pe1.NearestEdge.NearestDistance)
                                {
                                    pTargetEdge = Pe1;
                                }
                            }
                            #endregion
                        }
                        #endregion

                        #region 当待检测edge是建筑物与道路冲突时
                        else
                        {
                            pTargetEdge = Pe1;
                        }
                        #endregion
                    }
                    #endregion

                    #region 当pTargetedge 是建筑物与道路冲突时
                    else
                    {
                        #region 当待检测edge是建筑物与道路冲突时
                        if (Pe1.Node1.FeatureType == FeatureType.PolylineType || Pe1.Node2.FeatureType == FeatureType.PolylineType)
                        {
                            if (pTargetEdge.NearestEdge.NearestDistance > Pe1.NearestEdge.NearestDistance)
                            {
                                pTargetEdge = Pe1;
                            }
                        }
                        #endregion
                    }
                    #endregion
                }

                return pTargetEdge;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 判断所有相关原生冲突是否解决
        /// </summary>
        /// <param name="InitialConflictEdge"></param>原生冲突边集
        /// <param name="ConflictEdgeafterDisplacement"></param>移位后冲突边集
        /// <returns></returns> 如果存在未解决的原生冲突，返回true；否则返回false
        public bool IsInitialConflictSolved(List<ProxiEdge> InitialConflictEdge,List<ProxiEdge> ConflictEdgeafterDisplacement)
        {
            bool IsInitialConflictSolvedLabel = false;

            for (int i = 1; i < ConflictEdgeafterDisplacement.Count; i++)
            {
                ProxiEdge Pe1 = ConflictEdgeafterDisplacement[i];

                #region 判断是否是原生冲突
                for (int j = 0; j < InitialConflictEdge.Count; j++)
                {
                    ProxiEdge Pe2 = InitialConflictEdge[j];

                    if ((Pe1.Node1.TagID == Pe2.Node1.TagID && Pe1.Node1.FeatureType == Pe2.Node1.FeatureType && Pe1.Node2.TagID == Pe2.Node2.TagID && Pe1.Node2.FeatureType == Pe2.Node2.FeatureType) ||
                         (Pe1.Node1.TagID == Pe2.Node2.TagID && Pe1.Node1.FeatureType == Pe2.Node2.FeatureType && Pe1.Node2.TagID == Pe2.Node1.TagID && Pe1.Node2.FeatureType == Pe2.Node1.FeatureType))
                    {
                        IsInitialConflictSolvedLabel = true;
                    }               
                }
                #endregion
            }

            return IsInitialConflictSolvedLabel;
        }

        /// <summary>
        /// 判断某条给定的冲突边是否是原生冲突
        /// </summary>
        /// <param name="Pe"></param>
        /// <param name="ConflictEdgeafterDisplacement"></param>
        /// <returns></returns>
        public bool IsOneInitialConflictSolved(ProxiEdge Pe1, List<ProxiEdge> InitialConflictEdge)
        {
            bool IsInitialConflictSolvedLabel = false;

            for (int j = 0; j < InitialConflictEdge.Count; j++)
            {
                ProxiEdge Pe2 = InitialConflictEdge[j];

                if ((Pe1.Node1.TagID == Pe2.Node1.TagID && Pe1.Node1.FeatureType == Pe2.Node1.FeatureType && Pe1.Node2.TagID == Pe2.Node2.TagID && Pe1.Node2.FeatureType == Pe2.Node2.FeatureType) ||
                     (Pe1.Node1.TagID == Pe2.Node2.TagID && Pe1.Node1.FeatureType == Pe2.Node2.FeatureType && Pe1.Node2.TagID == Pe2.Node1.TagID && Pe1.Node2.FeatureType == Pe2.Node1.FeatureType))
                {
                    IsInitialConflictSolvedLabel = true;
                }
            }

            return IsInitialConflictSolvedLabel;
        }

        /// <summary>
        /// 返回符号化的建筑物
        /// </summary>
        /// <param name="Polygon"></param>
        /// <param name="Scale"></param>
        /// <returns></returns>
        public PolygonObject SymbolizedPolygon(PolygonObject Polygon, double Scale)
        {
            PrDispalce.工具类.ParameterCompute parametercompute = new 工具类.ParameterCompute();
            //MultipleLevelDisplace Md = new MultipleLevelDisplace();

            IPolygon pPolygon = PolygonObjectConvert(Polygon);
            IPolygon SMBR = parametercompute.GetSMBR(pPolygon);
            //pMapControl.DrawShape(SMBR, ref PolygonSymbol);
            //pMapControl.DrawShape(pPolygon, ref PolygonSymbol);

            #region 计算SMBR的方向
            IArea sArea = SMBR as IArea;
            IPoint CenterPoint = sArea.Centroid;

            IPointCollection sPointCollection = SMBR as IPointCollection;
            IPoint Point1 = sPointCollection.get_Point(1);
            IPoint Point2 = sPointCollection.get_Point(2);
            IPoint Point3 = sPointCollection.get_Point(3);
            IPoint Point4 = sPointCollection.get_Point(4);

            ILine Line1 = new LineClass(); Line1.FromPoint = Point1; Line1.ToPoint = Point2;
            ILine Line2 = new LineClass(); Line2.FromPoint = Point2; Line2.ToPoint = Point3;

            double Length1 = Line1.Length; double Length2 = Line2.Length; double Angle = 0;

            double LLength = 0; double SLength = 0;

            IPoint oPoint1 = new PointClass(); IPoint oPoint2 = new PointClass();
            IPoint oPoint3 = new PointClass(); IPoint oPoint4 = new PointClass();

            if (Length1 > Length2)
            {
                Angle = Line1.Angle;
                LLength = Length1; SLength = Length2;
            }

            else
            {
                Angle = Line2.Angle;
                LLength = Length2; SLength = Length1;
            }
            #endregion

            #region 对不能依比例尺符号化的SMBR旋转后符号化
            if (LLength < 0.7 * Scale / 1000 || SLength < 0.5 * Scale / 1000)
            {
                if (LLength < 0.7 * Scale / 1000)
                {
                    LLength = 0.7 * Scale / 1000;
                }

                if (SLength < 0.5 * Scale / 1000)
                {
                    SLength = 0.5 * Scale / 1000;
                }

                IEnvelope pEnvelope = new EnvelopeClass();
                pEnvelope.XMin = CenterPoint.X - LLength / 2;
                pEnvelope.YMin = CenterPoint.Y - SLength / 2;
                pEnvelope.Width = LLength; pEnvelope.Height = SLength;

                #region 将SMBR旋转回来
                TriNode pNode1 = new TriNode(); TriNode pNode2 = new TriNode();
                TriNode pNode3 = new TriNode(); TriNode pNode4 = new TriNode();

                pNode1.X = (pEnvelope.XMin - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMin - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode1.Y = (pEnvelope.XMin - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMin - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                pNode2.X = (pEnvelope.XMax - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMin - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode2.Y = (pEnvelope.XMax - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMin - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                pNode3.X = (pEnvelope.XMax - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMax - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode3.Y = (pEnvelope.XMax - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMax - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                pNode4.X = (pEnvelope.XMin - CenterPoint.X) * Math.Cos(Angle) - (pEnvelope.YMax - CenterPoint.Y) * Math.Sin(Angle) + CenterPoint.X;
                pNode4.Y = (pEnvelope.XMin - CenterPoint.X) * Math.Sin(Angle) + (pEnvelope.YMax - CenterPoint.Y) * Math.Cos(Angle) + CenterPoint.Y;

                List<TriNode> TList = new List<TriNode>();
                TList.Add(pNode1); TList.Add(pNode2); TList.Add(pNode3); TList.Add(pNode4); //TList.Add(pNode1);
                PolygonObject pPolygonObject = new PolygonObject(Polygon.ID, TList);//ID标识建筑物的标号
                #endregion

                #region 新建筑物属性赋值
                pPolygonObject.ClassID = Polygon.ClassID;
                pPolygonObject.ID = Polygon.ID;
                pPolygonObject.TagID = Polygon.TagID;
                pPolygonObject.TypeID = Polygon.TypeID;
                #endregion

                return pPolygonObject;
            }
            #endregion

            else
            {
                return Polygon;
            }
        }

        /// <summary>
        /// 将建筑物转化为Polygonobject
        /// </summary>
        /// <param name="pPolygonObject"></param>
        /// <returns></returns>
        IPolygon PolygonObjectConvert(PolygonObject pPolygonObject)
        {
            Ring ring1 = new RingClass();
            object missing = Type.Missing;

            IPoint curResultPoint = new PointClass();
            TriNode curPoint = null;
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
    }
}
