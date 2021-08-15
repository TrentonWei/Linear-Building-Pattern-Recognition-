using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using PrDispalce.PatternRecognition;

using AuxStructureLib;
using AuxStructureLib.IO;
using PrDispalce.建筑物聚合;

//PolygonCut algorithms
namespace PrDispalce.BuildingSim
{
    /// <summary>
    /// 将给定图形剖分为Convex Polygons
    /// </summary>
    class PolygonCut
    {
        /// <summary>
        /// 针对给定的建筑物，将其剖分为Convex Polygons
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <param name="InterN">三角网内插倍率</param>
        /// <param name="ConvexAngle">Convex阈值</param>
        /// <param name="OnlineAngle">OnLine阈值</param>
        /// <param name="NodeConsi">是否考虑节点</param>
        /// <param name="OrthConsi">是否考虑直角</param>
        /// <param name="NodeWeight">考虑节点的权重</param>
        /// <param name="OrthWeight">考虑直角的权重</param>
        /// <returns></returns>
        public List<Polygon> CutPolygonWithoutHole(Polygon pPolygon, int InterN, double ConvexAngle, double OnlineAngle, bool NodeConsi,bool OrthConsi,double NodeWeight, double OrthWeight)
        {
            List<Polygon> FinalCutPolygons = new List<Polygon>();

            #region 处理类
            PrDispalce.PatternRecognition.BendProcess BP = new PatternRecognition.BendProcess();//弯曲处理类
            PrDispalce.建筑物聚合.TriangleProcess TP = new 建筑物聚合.TriangleProcess();//三角形处理类
            PrDispalce.PatternRecognition.ConcaveNodeSolve CNS = new PatternRecognition.ConcaveNodeSolve();//凹点处理类
            #endregion

            #region 初始化
            List<Polygon> CacheCutPolygons = new List<Polygon>();
            List<TriNode> CacheConcaveNodes = BP.GetConcaveNode(pPolygon, ConvexAngle);
            if (CacheConcaveNodes.Count > 0)
            {
                CacheCutPolygons.Add(pPolygon);
            }

            else
            {
                FinalCutPolygons.Add(pPolygon);
            }
            #endregion

            #region 分割
            while (CacheCutPolygons.Count > 0)
            {
                #region 建筑物读取
                SMap map = new SMap();
                map.ReadDataFrmGivenPolygonObject(CacheCutPolygons[0]);
                map.InterpretatePoint2(InterN);

                SMap map2 = new SMap();
                map2.ReadDataFrmGivenPolygonObject(CacheCutPolygons[0]);
                #endregion

                #region DT+CDT+SKE
                DelaunayTin dt = new DelaunayTin(map.TriNodeList);
                dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

                ConvexNull cn = new ConvexNull(dt.TriNodeList);
                cn.CreateConvexNull();

                ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
                cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);

                Triangle.WriteID(dt.TriangleList);
                TriEdge.WriteID(dt.TriEdgeList);

                AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
                ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
                ske.TranverseSkeleton_Arc();
                BP.SkeRefine2(ske, map2.PolygonList[0], map.PolygonList[0]);
                #endregion

                #region 三角网与ske属性标注
                TP.LabelInOutType(dt.TriangleList, map.PolygonList);//标记是内或外三角形
                TP.CommonEdgeTriangleLabel(dt.TriangleList);//标记共边三角形
                TP.LabelBoundaryBuilding(dt.TriangleList);//标记边缘三角形
                BP.GetOutSkeArcLevel(ske, map.PolygonList[0]);//获得Ske中每一个Arc的层次
                #endregion

                List<TriNode> ConvexNodes = BP.GetConcaveNode(map2.PolygonList, ConvexAngle);//返回待分割的建筑物中的凹点

                #region 获得每一个节点对应的弯曲深度,并返回弯曲深度最深的节点
                Dictionary<TriNode, double> BendLength = new Dictionary<TriNode, double>();
                for (int j = 0; j < ConvexNodes.Count; j++)
                {
                    TriNode TestNode = this.GetMatchNode(ConvexNodes[j], map.PolygonList);
                    List<Skeleton_Arc> BendRoad = BP.GetOutBendRoadForNodes(TestNode, ske, map.PolygonList[0]);//获得外环的深度
                    double RoadLength = BP.GetRoadLength(BendRoad);
                    BendLength.Add(TestNode, RoadLength);
                }
                TriNode TargetNode = BP.GetDeepestNode(BendLength);
                #endregion

                #region 获得最优分割，并对图形进行分割
                List<Cut> AllCut = CNS.GetCuts(TargetNode, cdt, map.PolygonList);
                CNS.GetCutProperty(TargetNode, AllCut, map2.PolygonList, ConvexAngle, OnlineAngle);//注意，这里需要用未加密的建筑物图形
                List<Cut> RefinedCut = CNS.CutsRefine(AllCut, OnlineAngle);
                Cut TargetCut = CNS.GetBestCut2(RefinedCut, NodeConsi, OrthConsi, NodeWeight, OrthWeight);
                List<Polygon> CuttedPolygons = CNS.GetPolygonAfterCut(CacheCutPolygons[0], TargetCut);
                #endregion

                #region 更新CutPolygons
                CacheCutPolygons.RemoveAt(0);
                for (int j = 0; j < CuttedPolygons.Count; j++)
                {
                    List<TriNode> CacheCuttedConcaveNodes = BP.GetConcaveNode(CuttedPolygons[j], ConvexAngle);

                    if (CacheCuttedConcaveNodes.Count > 0)
                    {
                        CacheCutPolygons.Add(CuttedPolygons[j]);
                    }

                    else
                    {
                        FinalCutPolygons.Add(CuttedPolygons[j]);
                    }
                }
                #endregion
            }
            #endregion

            return FinalCutPolygons;
        }

        /// <summary>
        /// 获得两个图形的对应点
        /// </summary>
        /// <param name="ConvexNode"></param>
        /// <param name="Po"></param>
        /// <returns></returns>
        public TriNode GetMatchNode(TriNode ConvexNode, List<PolygonObject> PoList)
        {
            TriNode MatchedNode = null;
            foreach (PolygonObject Po in PoList)
            {
                bool Label = false;
                for (int i = 0; i < Po.PointList.Count; i++)
                {
                    if ((Po.PointList[i].X - ConvexNode.X) == 0 & (Po.PointList[i].Y - ConvexNode.Y) == 0)
                    {
                        MatchedNode = Po.PointList[i];
                        Label = true;
                        break;
                    }
                }

                if (Label)
                {
                    break;
                }
            }

            return MatchedNode;
        }

        /// <summary>
        /// (顺时针角度为正；逆时针角度为负)
        /// 判断给定角是否是凹角(角度是[-PI,PI])
        /// </summary>
        /// <param name="Angle"></param>
        /// <returns></returns> true表示是凹角；false表示不是凹角
        public bool ConcaveNode(double Angle,double ConcaveCons)
        {
            bool ConcaveLabel = false; double PI = 3.1415926;

            #region 将角度从[-PI,PI]转化为[0,360]
            Angle = Angle * 180 / 3.1415926;

            if (Angle < 0)
            {
                Angle = 360 + Angle;
            }
            #endregion

            #region 判断过程
            if (Angle > (180 + ConcaveCons))
            {
                ConcaveLabel = true;
            }

            else
            {
                ConcaveLabel = false;
            }
            #endregion

            return ConcaveLabel;
        }

        /// <summary>
        /// 判断给定节点是否在给定的结构列表中
        /// </summary>
        /// <param name="TriNode"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public bool StructNode(TriNode TargetNode,List<BasicStruct> StructList)
        {
            bool StructLabel=false;

            for (int i = 0; i < StructList.Count; i++)
            {
                if (this.StructNode(TargetNode, StructList[i]))
                {
                    StructLabel = true;
                    break;
                }
            }

            return StructLabel;
        }

        /// <summary>
        /// 判断给定节点是否在给定的结构中
        /// </summary>
        /// <param name="TriNode"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public bool StructNode(TriNode TargetNode, BasicStruct BStruct)
        {
            bool StructLabel = false;

            if (BStruct.NodeList.Contains(TargetNode))
            {
                StructLabel = true;
            }

            return StructLabel;
        }

        /// <summary>
        /// 返回节点所在的结构列表
        /// </summary>
        /// <param name="TargetNode"></param>
        /// <param name="BStruct"></param>
        /// <returns></returns>
        public List<BasicStruct> ReturnFirstStrcut(TriNode TargetNode, List<BasicStruct> StructList)
        {
            List<BasicStruct> CacheBsList = new List<BasicStruct>();
            for (int i = 0; i < StructList.Count; i++)
            {
                if (this.StructNode(TargetNode, StructList[i]))
                {
                    CacheBsList.Add(StructList[i]);
                }
            }

            return CacheBsList;            
        }

        /// <summary>
        /// 返回凹点连接线构成的建筑物分割线
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <param name="PerAngle"></param>
        public List<Cut> ConcaveConnect(PolygonObject Po,double PerAngle)
        {
            List<Cut> ConcaveConnectCut=new List<Cut>();
           
            PrDispalce.BuildingSim.PublicUtil Pu = new PublicUtil();
            PrDispalce.PatternRecognition.ConcaveNodeSolve CNS=new ConcaveNodeSolve();
            List<TriNode> ConcaveNode = Pu.GetConcaveNode(Po, PerAngle);//获取给定的凹点
            IPolygon CachePolygon = Pu.PolygonObjectConvert(Po);

            #region 判断连接凹点在建筑物内且能消除当前凹点的Cut
            if (ConcaveNode.Count > 1)
            {
                for (int i = 0; i < ConcaveNode.Count - 1; i++)
                {
                    IPoint sPoint = new PointClass(); sPoint.X = ConcaveNode[i].X; sPoint.Y = ConcaveNode[i].Y;

                    for (int j = i + 1; j < ConcaveNode.Count; j++)
                    {
                        ILine CacheLine =new LineClass();
                        IPoint ePoint = new PointClass(); ePoint.X = ConcaveNode[j].X; ePoint.Y = ConcaveNode[j].Y;
                        CacheLine.FromPoint = sPoint; CacheLine.ToPoint = ePoint;

                        #region 判断是否在建筑物内
                        IRelationalOperator IRO = CachePolygon as IRelationalOperator;
                        IPolyline CachePolyline = new PolylineClass();
                        CachePolyline.FromPoint = sPoint; CachePolyline.ToPoint = ePoint;
                        if (IRO.Contains(CachePolyline as IGeometry))
                        {
                            #region 生成对应的Cut
                            TriEdge CutEdge = new TriEdge();
                            CutEdge.startPoint = ConcaveNode[i];
                            CutEdge.endPoint = ConcaveNode[j];

                            Cut CacheCut = new Cut(CutEdge);                      
                            #endregion

                            #region 是否消除凹点
                            if (this.CutRemoveConcave(Po,CacheCut,PerAngle))
                            {                           
                                ConcaveConnectCut.Add(CacheCut);
                            }
                            #endregion
                        }
                        #endregion
                    }
                }
            }
            #endregion

            return ConcaveConnectCut;
        }

        /// <summary>
        /// 给定凹点和凹点关联的直线（如延长线或平行线）
        /// 获取给定的满足条件的分割
        /// </summary>
        /// <param name="Po"></param>
        /// <param name="PerAngle"></param>
        /// <param name="CurPoint"></param>
        /// <param name="pLine"></param>
        /// LineLabel=1表示延长线；LineLabel=2表示平行线
        /// <returns></returns>
        public List<Cut> TriNodeConnect(PolygonObject Po, double PerAngle, double OnLineAngle,TriNode CurPoint, IPolyline pLine)
        {
            List<Cut> CutList = new List<Cut>();

            #region 获得潜在的cut
            PublicUtil Pu=new PublicUtil();
            IPolygon CachePolygon = Pu.PolygonObjectConvert(Po);//转化为IPolygon
            ITopologicalOperator iTop = CachePolygon as ITopologicalOperator;
            IGeometry iGb= iTop.Intersect(pLine as IGeometry,esriGeometryDimension.esriGeometry0Dimension);
            List<Cut> CacheList = new List<Cut>();

            if (iGb != null)
            {
                IPointCollection pPointCollection = iGb as IPointCollection;
                for (int i = 0; i < pPointCollection.PointCount; i++)
                {
                    if ((Math.Abs(pPointCollection.get_Point(i).X - CurPoint.X) > 0.00001) || 
                        (Math.Abs(pPointCollection.get_Point(i).Y - CurPoint.Y) > 0.00001))
                    {
                        TriNode EndNode = new TriNode(pPointCollection.get_Point(i).X, pPointCollection.get_Point(i).Y);
                        TriEdge CutEdge = new TriEdge(CurPoint, EndNode);
                        Cut CacheCut = new Cut(CutEdge);
                        CacheList.Add(CacheCut);
                    }
                }
            }
            #endregion

            #region 判断在建筑物内，且能消除凹点的Cut
            for (int i = 0; i < CacheList.Count; i++)
            {
                //bool test1 = this.CutInPolygon(Po, CacheList[i]);
                //bool test2 = this.CutRemoveConcave(Po, CacheList[i], OnLineAngle);

                if (this.CutInPolygon(Po, CacheList[i]) && this.CutRemoveConcave(Po, CacheList[i], OnLineAngle))
                {
                    #region Cut调整（若Cut的endNode与PolygonObject中节点邻近，则调整节点为PolygonObject节点）
                    CacheList[i] = this.NearCutRefine(CacheList[i], Po, 3);
                    #endregion

                    CutList.Add(CacheList[i]);
                }
            }
            #endregion

           

            return CutList;
        }

        /// <summary>
        /// 判断对应的Cut是否在多边形内
        /// </summary>
        /// <param name="Po"></param>
        /// <param name="pCut"></param>
        /// <returns></returns>true=cut在Polygon内；false=cut在Polygon外
        public bool CutInPolygon(PolygonObject Po, Cut pCut)
        {
            IPolyline CacheLine = new PolylineClass();

            PublicUtil Pu = new PublicUtil();
            IPolygon CachePolygon = Pu.PolygonObjectConvert(Po);
            IPoint sPoint = new PointClass(); sPoint.X = pCut.CutEdge.startPoint.X; sPoint.Y = pCut.CutEdge.startPoint.Y;
            IPoint ePoint = new PointClass(); ePoint.X = pCut.CutEdge.endPoint.X; ePoint.Y = pCut.CutEdge.endPoint.Y;
            CacheLine.FromPoint = sPoint; CacheLine.ToPoint = ePoint;

            #region 判断是否在建筑物内
            IRelationalOperator IRO = CachePolygon as IRelationalOperator;
            if (IRO.Contains(CacheLine as IGeometry))
            {
                return true;
            }
            else
            {
                return false;
            }
            #endregion
        }

        /// <summary>
        /// 判断对应的Cut是否消除了对应凹点
        /// </summary>
        /// <param name="Po"></param>
        /// <param name="pCut"></param>
        /// <returns></returns>true=消除了对应凹点；false=未消除对应凹点
        public bool CutRemoveConcave(PolygonObject Po, Cut pCut,double OnLineAngle)
        {
            ConcaveNodeSolve CNS = new ConcaveNodeSolve();

            List<PolygonObject> CachePolygonObjectList = new List<PolygonObject>(); CachePolygonObjectList.Add(Po);
            CNS.GetCutAfterAngle(pCut, CachePolygonObjectList,OnLineAngle);

            #region 判断Cut的四个角
            double CutAngle11 = pCut.CutAngle11;
            double CutAngle12 = pCut.CutAngle12;
            double CutAngle21 = pCut.CutAngle21;
            double CutAngle22 = pCut.CutAngle22;

            CutAngle11 = CutAngle11 * 180 / 3.1415926;
            CutAngle12 = CutAngle12 * 180 / 3.1415926;
            CutAngle21 = CutAngle21 * 180 / 3.1415926;
            CutAngle22 = CutAngle22 * 180 / 3.1415926;

            if (CutAngle11 < 0)
            {
                CutAngle11 = 360 + CutAngle11;
            }
            if (CutAngle12 < 0)
            {
                CutAngle12 = 360 + CutAngle12;
            }
            if (CutAngle21 < 0)
            {
                CutAngle21 = 360 + CutAngle21;
            }
            if (CutAngle22 < 0)
            {
                CutAngle22 = 360 + CutAngle22;
            }
            #endregion

            #region 判断角度的凹凸性
            if (CutAngle11 <= (180 + OnLineAngle) && CutAngle12 <= (180 + OnLineAngle) &&
                CutAngle21 <= (180 + OnLineAngle) && CutAngle22 <= (180 + OnLineAngle))
            {
                return true;
            }

            else
            {
                return false;
            }
            #endregion
        }

        /// <summary>
        /// 获得给定pPolygon所有的潜在分割线
        /// </summary>
        /// <param name="Po"></param>
        /// pPolygon 给定的建筑物
        /// PerAngle直角阈值
        /// <returns></returns>
        public List<Cut> CutList(PolygonObject TargetPo,double PerAngle,double OnLineAngle)
        {
            PrDispalce.BuildingSim.PublicUtil PU = new PublicUtil();//通用工具
            PrDispalce.PatternRecognition.ConcaveNodeSolve CNS = new ConcaveNodeSolve();
            List<Cut> CutList = new List<Cut>();//返回的CutList
            IPolygon pPolygon = PU.PolygonObjectConvert(TargetPo); 
            TargetPo.GetBendAngle();//获取给定建筑物各节点处的角度
            List<BasicStruct> StructList = PU.GetStructedNodes(TargetPo, PerAngle * 3.1415926 / 180);//获得建筑物的结构点(PerAngle属于[-180,180])，需将PerAngle转换

            #region 生成对应多边形的三角网(不内插点)
            SMap map = new SMap();
            map.ReadDataFrmGivenPolygonObject(pPolygon as Polygon);

            DelaunayTin dt = new DelaunayTin(map.TriNodeList);
            dt.CreateDelaunayTin(AlgDelaunayType.Side_extent2);

            ConsDelaunayTin cdt = new ConsDelaunayTin(dt);
            cdt.CreateConsDTfromPolylineandPolygon(null, map.PolygonList);

            Triangle.WriteID(dt.TriangleList);
            TriEdge.WriteID(dt.TriEdgeList);

            AuxStructureLib.Skeleton ske = new AuxStructureLib.Skeleton(cdt, map);
            ske.TranverseSkeleton_Segment_NT_DeleteIntraSkel();
            ske.TranverseSkeleton_Arc();
            #endregion

            #region 获取点的对应分割线
            for (int i = 0; i < TargetPo.PointList.Count; i++)
            {
                double Angle = TargetPo.BendAngle[i][1];//角度是[-PI,PI]
                if (this.ConcaveNode(Angle, OnLineAngle))//判断是否是凹点,角度是[-PI,PI]
                {
                    #region 如果是结构点对应的Cut
                    if (this.StructNode(TargetPo.PointList[i], StructList))
                    {
                        #region 角度转换
                        Angle = Angle * 180 / 3.1415926;

                        if (Angle < 0)
                        {
                            Angle = 360 + Angle;
                        }
                        #endregion 
                        
                        #region 是凹直角                
                        if (Math.Abs(Angle - 270) < PerAngle)
                        {
                            #region 获得对应的延长线
                            IPolyline ExtendingLine1 = new PolylineClass();
                            IPolyline ExtendingLine2 = new PolylineClass();

                            if (i == 0)
                            {
                                ExtendingLine1 = PU.GetExtendingLine(TargetPo.PointList[i], TargetPo.PointList[1]);
                                ExtendingLine2 = PU.GetExtendingLine(TargetPo.PointList[i], TargetPo.PointList[TargetPo.PointList.Count - 1]);
                            }

                            else if (i == (TargetPo.PointList.Count - 1))
                            {
                                ExtendingLine1 = PU.GetExtendingLine(TargetPo.PointList[i], TargetPo.PointList[0]);
                                ExtendingLine2 = PU.GetExtendingLine(TargetPo.PointList[i], TargetPo.PointList[i - 1]);
                            }

                            else
                            {
                                ExtendingLine1 = PU.GetExtendingLine(TargetPo.PointList[i], TargetPo.PointList[i - 1]);
                                ExtendingLine2 = PU.GetExtendingLine(TargetPo.PointList[i], TargetPo.PointList[i + 1]);
                            }
                            #endregion

                            #region 获得对应的Cuts
                            List<Cut> ExtendingLineCut1 = this.TriNodeConnect(TargetPo, PerAngle,OnLineAngle, TargetPo.PointList[i], ExtendingLine1);
                            List<Cut> ExtendingLineCut2 = this.TriNodeConnect(TargetPo, PerAngle,OnLineAngle, TargetPo.PointList[i], ExtendingLine2);

                            CutList.AddRange(ExtendingLineCut1);
                            CutList.AddRange(ExtendingLineCut2);
                            #endregion
                        }
                        #endregion

                        #region 不是凹直角
                        else
                        {
                            List<BasicStruct> CacheBsList = this.ReturnFirstStrcut(TargetPo.PointList[i], StructList);
                            for (int j = 0; j < CacheBsList.Count; j++)
                            {
                                BasicStruct CacheBs = CacheBsList[j];
                                int PointIndex = CacheBs.NodeList.IndexOf(TargetPo.PointList[i]);
                                IPolyline ParaLine = new PolylineClass();//平行线
                                IPolyline ExtendingLine = new PolylineClass();//延长线

                                ParaLine = PU.GetParaLine(TargetPo.PointList[i], CacheBs.NodeList[1], CacheBs.NodeList[2]);
                                ExtendingLine = PU.GetExtendingLine(TargetPo.PointList[i], CacheBs.NodeList[PointIndex / 3 + 1]);

                                List<Cut> ParaLineCut = this.TriNodeConnect(TargetPo, PerAngle, OnLineAngle, TargetPo.PointList[i], ParaLine);
                                List<Cut> ExtendingLineCut = this.TriNodeConnect(TargetPo, PerAngle, OnLineAngle, TargetPo.PointList[i], ExtendingLine);

                                #region 去除裁剪后非直角的Cut
                                for (int m = ExtendingLineCut.Count-1; m >=0; m--)
                                {
                                    double test1 = Math.Abs(ExtendingLineCut[m].CutAngle21) - 3.1415926 / 2;
                                    double test2 = Math.Abs(ExtendingLineCut[m].CutAngle22) - 3.1415916 / 2;
                                    if ((Math.Abs(ExtendingLineCut[m].CutAngle21) - 3.1415926 / 2) > PerAngle * 3.1415926 / 180 || (Math.Abs(ExtendingLineCut[m].CutAngle22) - 3.1415916 / 2) > PerAngle * 3.1415926 / 180)
                                    {
                                        ExtendingLineCut.RemoveAt(m);
                                    }
                                }
                                #endregion

                                CutList.AddRange(ParaLineCut);
                                CutList.AddRange(ExtendingLineCut);
                            }
                        }
                        #endregion
                    }
                    #endregion

                    #region 非结构点对应的Cut
                    List<PolygonObject> PoList=new List<PolygonObject>();PoList.Add(TargetPo);
                    List<Cut> unStructCut = CNS.GetCuts(TargetPo.PointList[i], cdt, PoList);
                    CutList.AddRange(unStructCut);
                    #endregion
                }
            }
            #endregion

            #region 连接凹点连接线丰富已有分割线
            List<Cut> ConcaveConnect = this.ConcaveConnect(TargetPo, PerAngle);
            CutList.AddRange(ConcaveConnect);
            #endregion

            return this.DeleteRepeated(CutList);
        }

        /// <summary>
        /// 删除重复的Cuts
        /// </summary>
        /// <param name="initialCuts"></param>
        /// <returns></returns>
        public List<Cut> DeleteRepeated(List<Cut> initialCuts)
        {
            for (int i =initialCuts.Count-1; i >=1; i--)
            {
                Cut InitialCut = initialCuts[i];
                TriNode Node1 = InitialCut.CutEdge.startPoint;
                TriNode Node2 = InitialCut.CutEdge.endPoint;
                for (int j = i - 1; j >= 0; j--)
                {
                    Cut CacheCut = initialCuts[j];

                    TriNode cNode1 = CacheCut.CutEdge.startPoint;
                    TriNode cNode2 = CacheCut.CutEdge.endPoint;

                    if ((Math.Abs(Node1.X - cNode1.X) < 0.00001 && Math.Abs(Node1.Y - cNode1.Y) < 0.00001 &&
                        Math.Abs(Node2.X - cNode2.X) < 0.00001 && Math.Abs(Node2.Y - cNode2.Y) < 0.00001)||
                        (Math.Abs(Node1.X - cNode2.X) < 0.00001 && Math.Abs(Node1.Y - cNode2.Y) < 0.00001 &&
                        Math.Abs(Node2.X - cNode1.X) < 0.00001 && Math.Abs(Node2.Y - cNode1.Y) < 0.00001))
                    {
                        initialCuts.RemoveAt(i);
                        break;
                    }
                }
            }

            return initialCuts;
        }

        /// <summary>
        /// Cut调整（若Cut的endNode与PolygonObject中节点邻近，则调整节点为PolygonObject节点）
        /// 调整过程：
        /// </summary>
        /// <param name="TargetCut"></param>
        /// 备注：TargetCut中startnode是建筑物节点；endnode不一定是建筑物节点
        /// <param name="TargetPo"></param>
        /// <param name="NearAngle"></param>
        /// <returns></returns>
        public Cut NearCutRefine(Cut TargetCut, PolygonObject TargetPo, double NearAngle)
        {
            ConcaveNodeSolve CNS = new ConcaveNodeSolve();

            #region 调整过程
            Dictionary<double, TriNode> NodeAngle =new Dictionary<double,TriNode>();

            #region 获得所有满足条件点
            for (int i = 0; i < TargetPo.PointList.Count; i++)
            {
                bool StartLabel = false; bool EndLabel = false;
                if (Math.Abs(TargetPo.PointList[i].X - TargetCut.CutEdge.startPoint.X) < 0.00001 && Math.Abs(TargetPo.PointList[i].Y - TargetCut.CutEdge.startPoint.Y) < 0.00001)
                {
                    StartLabel = true;
                }

                if (Math.Abs(TargetPo.PointList[i].X - TargetCut.CutEdge.endPoint.X) < 0.00001 && Math.Abs(TargetPo.PointList[i].Y - TargetCut.CutEdge.endPoint.Y) < 0.00001)
                {
                    EndLabel = true;
                }

                if (!(StartLabel || EndLabel))
                {
                    double Angle = CNS.GetAngle(TargetCut.CutEdge.startPoint, TargetCut.CutEdge.endPoint, TargetPo.PointList[i]);

                    if (Math.Abs(Angle) < NearAngle * 3.1415926 / 180)
                    {
                        NodeAngle.Add(Angle,TargetPo.PointList[i]);
                    }
                }
            }
            #endregion

            #region 选择角度最大的节点
            List<double> ValueList = NodeAngle.Keys.ToList();

            if (ValueList.Count > 0)
            {
                TriNode MaxNode = NodeAngle[ValueList.Max()];
                TargetCut.CutEdge.endPoint = MaxNode;
            }
            #endregion          
            #endregion

            return TargetCut;
        }

        /// <summary>
        /// 给定凹建筑物的可行Cuts，返回能将建筑物剖分的所有可能Cuts的集合。
        /// </summary>
        /// <param name="PotentialCuts"></param>
        /// <param name="TargetPo"></param>
        /// <returns></returns>
        public List<List<Cut>> satifiedCuts(List<Cut> PotentialCuts, PolygonObject TargetPo,double OnLineAngleT)
        {
            List<List<Cut>> satifiedCuts = new List<List<Cut>>();
            BendProcess BP = new BendProcess();
            List<PolygonObject> PoList = new List<PolygonObject>(); PoList.Add(TargetPo);
            List<TriNode> ConcaveNodes = BP.GetConcaveNode(PoList, OnLineAngleT);///获取图形中的凹点
            int ConcaveNodeCount = ConcaveNodes.Count;///凹点数量

            #region 从潜在的Cuts集合中筛选出符合要求的CutsList
            for (int i = 1; i < ConcaveNodeCount+1; i++)
            {
                List<List<Cut>> SelectmCuts = this.SelectmCuts(PotentialCuts, i);//从potentialCuts中选择m个Cuts
                #region 判断每一个CutList(判断条件：1. Cuts之间不相交；2.Cuts列表能消除图形中所有凹点)
                for (int j = 0; j < SelectmCuts.Count; j++)
                {
                    if (this.satConcaveCut(SelectmCuts[j], TargetPo, OnLineAngleT) && !this.CutInterSect(SelectmCuts[j]))
                    {
                        satifiedCuts.Add(SelectmCuts[j]);
                    }
                }
                #endregion
            }
            #endregion

            return satifiedCuts;
        }

        /// <summary>
        /// 判断给定的CutList集合能否消除给定图形中所有凹点
        /// </summary>
        /// <param name="CutList"></param>
        /// <param name="TargetPo"></param>
        /// <returns></returns>true=能消除图形中所有凹点；false=不能消除图形中所有凹点
        public bool satConcaveCut(List<Cut> CutList, PolygonObject TargetPo,double OnLineAngleT)
        {
            bool satConcaveLabel = false;
            BendProcess BP = new BendProcess();
            List<PolygonObject> PoList = new List<PolygonObject>(); PoList.Add(TargetPo);
            List<TriNode> ConcaveNodes = BP.GetConcaveNode(PoList, OnLineAngleT);///获取图形中的凹点

            #region 判断过程
            List<TriNode> pConcaveNodes = new List<TriNode>();
            for (int i = 0; i < CutList.Count; i++)
            {
                pConcaveNodes.AddRange(CutList[i].ConcaveNodes);
            }
            pConcaveNodes = pConcaveNodes.Distinct().ToList();
            #endregion

            if (pConcaveNodes.Count >= ConcaveNodes.Count)
            {
                satConcaveLabel = true;
            }

            return satConcaveLabel;
        }

        /// <summary>
        /// 判断给定的CutsList中Cut是否相交
        /// </summary>
        /// <param name="CutList"></param>
        /// <returns></returns>true=相交；false=不相交
        public bool CutInterSect(List<Cut> CutList)
        {
            bool IntersectLabel=false;

            #region 判断过程
            for (int i = 0; i < CutList.Count; i++)
            {
                for (int j = 0; j < CutList.Count; j++)
                {
                    if (j != i)
                    {
                        if (CutList[i].IntersectCuts.Contains(CutList[j]))
                        {
                            IntersectLabel = true;
                            break;
                        }
                    }
                }

                if (IntersectLabel)
                {
                    break;
                }
            }
            #endregion

            return IntersectLabel;
        }

        /// <summary>
        /// 从给定的Cut集合中返回m个Cut的集合
        /// </summary>
        /// <param name="PotentialCuts"></param>
        /// <returns></returns>
        public List<List<Cut>> SelectmCuts(List<Cut> PotentialCuts, int m)
        {
            if (PotentialCuts.Count == 0)
            {
                return null;
            }

            List<List<Cut>> result = new List<List<Cut>>();//存放返回的列表
            List<List<Cut>> temp = null; //临时存放从下一级递归调用中返回的结果
            List<Cut> oneList = null; //存放每次选取的第一个元素构成的列表，当只需选取一个元素时，用来存放剩下的元素分别取其中一个构成的列表；
            Cut oneElment; //每次选取的元素
            List<Cut> source = new List<Cut>(PotentialCuts); //将传递进来的元素列表拷贝出来进行处理，防止后续步骤修改原始列表，造成递归返回后原始列表被修改；
            int n = 0; //待处理的元素个数

            if (PotentialCuts != null)
            {
                n = PotentialCuts.Count;
            }
            if (n == m && m != 1)//n=m时只需将剩下的元素作为一个列表全部输出
            {
                result.Add(source);
                return result;
            }
            if (m == 1)  //只选取一个时，将列表中的元素依次列出
            {
                foreach (Cut el in source)
                {
                    oneList = new List<Cut>();
                    oneList.Add(el);
                    result.Add(oneList);
                    oneList = null;
                }
                return result;
            }
            if (m > n)
            {
                return null;
            }

            for (int i = 0; i <= n - m; i++)
            {
                oneElment = source[0];
                source.RemoveAt(0);
                temp = SelectmCuts(source, m - 1);
                for (int j = 0; j < temp.Count; j++)
                {
                    oneList = new List<Cut>();
                    oneList.Add(oneElment);
                    oneList.AddRange(temp[j]);
                    result.Add(oneList);
                    oneList = null;
                }
            }

            return result;
        }
    }
}