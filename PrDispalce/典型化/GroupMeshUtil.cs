using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.Geometry;
using AuxStructureLib;
using AuxStructureLib.IO;
using ESRI.ArcGIS.Carto;

namespace PrDispalce.典型化
{
    class GroupMeshUtil
    {
        #region 参数
        public Dictionary<int,Pattern> PatternDic = new Dictionary<int,Pattern>();//存储地图中的Pattern
        PrDispalce.典型化.Symbolization SB = new Symbolization();
        #endregion

        /// <summary>
        /// 简单Mesh的典型化（终止条件为数量）
        /// </summary>
        /// <param name="Map">地图数据</param>
        /// <param name="pg">邻近图</param>
        public void SimplyMesh(SMap Map, ProxiGraph pg,double Number)
        {
            while (Map.PolygonList.Count > Number)
            {
                #region 获得需要操作的边
                Dictionary<ProxiEdge, double> SortEdgeList = this.SortPgListByDistance(pg.PgforBuildingEdgesList);
                KeyValuePair<ProxiEdge, double> FirstKey = SortEdgeList.First();
                ProxiEdge pEdge= FirstKey.Key;
                #endregion

                #region 生成新的建筑物
                ProxiNode Node1 = pEdge.Node1;
                ProxiNode Node2 = pEdge.Node2;

                PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                PolygonObject NewPolygon = null; ProxiNode NewNode = null;
                this.GetNewPolygonAndNode(Po1, Po2, 1, 1, Node1, Node2,out NewPolygon,out NewNode,25000);
                #endregion

                #region 更新Map和邻近图
                this.UpdataMapAndPg(Map,pg,Po1,Po2,NewPolygon,Node1,Node2,NewNode);
                #endregion
            }
        }

        /// <summary>
        /// 简单Mesh的典型化（终止条件为冲突判断）
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="pg"></param>
        public void SimplyMesh(SMap Map, ProxiGraph pg, double Scale, double MinDis)
        {
            while (!SimplyMeshStop(Map, pg, Scale, MinDis))
            {
                #region 获得需要操作的边
                Dictionary<ProxiEdge, double> SortEdgeList = this.SortPgListByDistance(pg.PgforBuildingEdgesList);
                KeyValuePair<ProxiEdge, double> FirstKey = SortEdgeList.First();
                ProxiEdge pEdge = FirstKey.Key;
                #endregion

                #region 生成新的建筑物
                ProxiNode Node1 = pEdge.Node1;
                ProxiNode Node2 = pEdge.Node2;

                PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                PolygonObject NewPolygon = null; ProxiNode NewNode = null;
                this.GetNewPolygonAndNode(Po1, Po2, 1, 1, Node1, Node2, out NewPolygon, out NewNode, 25000);
                #endregion

                #region 更新Map和邻近图
                this.UpdataMapAndPg(Map, pg, Po1, Po2, NewPolygon, Node1, Node2, NewNode);
                #endregion
            }
        }

        /// <summary>
        /// 简单Mesh的典型化（终止条件为冲突判断）;距离以面积加权
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="pg"></param>
        /// <param name="Scale"></param>
        /// <param name="MinDis"></param>
        /// <param name="Weight"></param>
        /// <param name="AreaWeight"></param>
        public void SimplyMesh(SMap Map, ProxiGraph pg, double Scale, double MinDis, double AreaWeight)
        {
            while (!SimplyMeshStop(Map, pg, Scale, MinDis))
            {
                #region 获得需要操作的边
                Dictionary<ProxiEdge, double> SortEdgeList = this.SortPgListByDistance(Map, pg.PgforBuildingEdgesList, AreaWeight);
                KeyValuePair<ProxiEdge, double> FirstKey = SortEdgeList.First();
                ProxiEdge pEdge = FirstKey.Key;
                #endregion

                #region 生成新的建筑物
                ProxiNode Node1 = pEdge.Node1;
                ProxiNode Node2 = pEdge.Node2;

                PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                PolygonObject NewPolygon = null; ProxiNode NewNode = null;
                this.GetNewPolygonAndNode(Po1, Po2, 1, 1, Node1, Node2, out NewPolygon, out NewNode, Scale);
                #endregion

                #region 更新Map和邻近图
                this.UpdataMapAndPg(Map, pg, Po1, Po2, NewPolygon, Node1, Node2, NewNode);
                #endregion
            }
        }

        /// <summary>
        /// 简单Mesh的典型化（终止条件为冲突判断），考虑Pattern
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="pg"></param>
        /// <param name="Scale"></param>
        /// <param name="AreaWeigth">符号化面积距离权重；Pattern间的距离权重，目前两个取相等</param>
        public void SimplyMeshConsiderPattern(SMap Map,ProxiGraph pg,double Scale, double AreaWeigth,double PatternWeight,double PatternInWeight,double MinDis,double AverageDis,int DistanceLabel)
        {
            this.GetPatterns(Map);//获取建筑物群中的Pattern
            this.PatternBounaryBuilding(Map, pg);//标识Pattern中的边界建筑物

            while (!MeshStopConsiderPattern(Map, pg, Scale,MinDis,AverageDis))
            {
                #region 获得需要操作的边
                Dictionary<ProxiEdge, double> SortEdgeList = this.SortPgListByDistanceConsiderPattern(Map, DistanceLabel, pg.PgforBuildingEdgesList, AreaWeigth, PatternWeight, PatternInWeight, Scale);
                KeyValuePair<ProxiEdge, double> FirstKey = SortEdgeList.First();
                ProxiEdge pEdge = FirstKey.Key;
                #endregion

                #region 生成新的建筑物
                ProxiNode Node1 = pEdge.Node1;
                ProxiNode Node2 = pEdge.Node2;

                PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                PolygonObject NewPolygon = null; ProxiNode NewNode = null;
                this.GetNewPolygonAndNode(Po1, Po2, 1, 4, Node1, Node2, out NewPolygon, out NewNode, Scale);
                #endregion

                //更新Map和邻近图
                this.UpdataMapAndPg(Map, pg, Po1, Po2, NewPolygon, Node1, Node2, NewNode);
                
                //更新Pattern
                this.UpdataPattern(Po1, Po2, NewPolygon,Map,pg);
            }
        }

        /// <summary>
        /// 顾及尺度（即建筑物是否依比例尺符号化）的典型化
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="pg"></param>
        /// <param name="Scale"></param>
        /// <param name="MinDis"></param>
        /// <param name="Weight"></param>
        public void ScaleMesh(SMap Map, ProxiGraph pg,double Scale,double MinDis,double Weight)
        {
            while (!MeshStop(Map, pg, Scale, MinDis))
            {
                #region 获得需要操作的边
                Dictionary<ProxiEdge, double> SortEdgeList = this.SortPgListByDistance(Map, pg.PgforBuildingEdgesList, Scale, Weight);
                KeyValuePair<ProxiEdge, double> FirstKey = SortEdgeList.First();
                ProxiEdge pEdge = FirstKey.Key;
                #endregion

                #region 生成新的建筑物
                ProxiNode Node1 = pEdge.Node1;
                ProxiNode Node2 = pEdge.Node2;

                PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                PolygonObject NewPolygon = null; ProxiNode NewNode = null;
                this.GetNewPolygonAndNode(Po1, Po2, 1, 3, Node1, Node2, out NewPolygon, out NewNode,Scale);
                #endregion

                #region 更新Map和邻近图
                this.UpdataMapAndPg(Map, pg, Po1, Po2, NewPolygon, Node1, Node2, NewNode);
                #endregion
            }
        }

        /// <summary>
        /// 渐进式合并、删除与典型化
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="pg"></param>
        /// <param name="Scale"></param>
        /// <param name="AreaWeight"></param>
        /// <param name="PatternWeight"></param>
        /// <param name="MinDis"></param>
        /// <param name="AverageDis"></param>
        public void ProgressiveGeneralization(SMap Map, ProxiGraph pg, double Scale, double AreaWeight, double PatternWeight,double PatternInWeigth, double MinDis, double AverageDis,IMap pMap)
        {
            this.GetPatterns(Map);//获取建筑物群中的Pattern
            this.PatternBounaryBuilding(Map, pg);//标识Pattern中的边界建筑物

            while (!ProgressiveMeshStop(Map, pg, Scale, MinDis, AverageDis))
            {
                #region 获得需要操作的边
                Dictionary<ProxiEdge, double> SortEdgeList = this.SortPgListByDistanceInProgressiveMesh(Map,pg.PgforBuildingEdgesList, AreaWeight, PatternWeight, PatternInWeigth, Scale);
                KeyValuePair<ProxiEdge, double> FirstKey = SortEdgeList.First();
                ProxiEdge pEdge = FirstKey.Key;
                #endregion

                #region 生成新的建筑物
                ProxiNode Node1 = pEdge.Node1;
                ProxiNode Node2 = pEdge.Node2;

                PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                PolygonObject NewPolygon = null; ProxiNode NewNode = null;
                this.GetNewPolygonAndNodeInProgressiveMesh(Po1, Po2, Node1, Node2, out NewPolygon, out NewNode, Scale,pMap);
                #endregion

                #region 更新
                //更新Map和邻近图
                this.UpdataMapAndPg(Map, pg, Po1, Po2, NewPolygon, Node1, Node2, NewNode);

                //更新Pattern
                this.UpdataPattern(Po1, Po2, NewPolygon,Map,pg);
                #endregion
            }

        }

        /// <summary>
        /// 2.22 渐进式综合
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="pg"></param>
        /// <param name="Scale"></param>
        /// <param name="AreaWeigth"></param>
        /// <param name="MinDis"></param>
        /// <param name="pMap"></param>
        public void ProgressiveGeneralization(SMap Map, ProxiGraph pg, double Scale, double Weight, double MinDis, IMap pMap)
        {
            while (!ProgressiveMeshStop(Map, pg, Scale, MinDis))
            {
                #region 获得需要操作的边
                Dictionary<ProxiEdge, double> SortEdgeList = this.SortPgListByDistance2(Map, pg.PgforBuildingEdgesList, Scale, Weight);
                KeyValuePair<ProxiEdge, double> FirstKey = SortEdgeList.First();
                ProxiEdge pEdge = FirstKey.Key;
                #endregion

                #region 生成新的建筑物
                ProxiNode Node1 = pEdge.Node1;
                ProxiNode Node2 = pEdge.Node2;

                PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                PolygonObject NewPolygon = null; ProxiNode NewNode = null;
                this.GetNewPolygonAndNodeInProgressiveMesh2(Po1, Po2, Node1, Node2, out NewPolygon, out NewNode, Scale, pMap);
                #endregion

                #region 更新
                //更新Map和邻近图
                this.UpdataMapAndPg(Map, pg, Po1, Po2, NewPolygon, Node1, Node2, NewNode);
                #endregion
            }
        }

        /// <summary>
        /// 2.22 渐进式综合
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="pg"></param>
        /// <param name="Scale"></param>
        /// <param name="AreaWeigth"></param>
        /// <param name="MinDis"></param>
        /// <param name="pMap"></param>
        public void ProgressiveGeneralizationP(SMap Map, ProxiGraph pg, double Scale, double Weight, double MinDis, IMap pMap,string OutPath)
        {
            int Count = 0;
            while (!ProgressiveMeshStop(Map, pg, Scale, MinDis))
            {
                #region 获得需要操作的边
                Dictionary<ProxiEdge, double> SortEdgeList = this.SortPgListByDistance2(Map, pg.PgforBuildingEdgesList, Scale, Weight);
                KeyValuePair<ProxiEdge, double> FirstKey = SortEdgeList.First();
                ProxiEdge pEdge = FirstKey.Key;
                #endregion

                #region 生成新的建筑物
                ProxiNode Node1 = pEdge.Node1;
                ProxiNode Node2 = pEdge.Node2;

                PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                PolygonObject NewPolygon = null; ProxiNode NewNode = null;
                this.GetNewPolygonAndNodeInProgressiveMesh2(Po1, Po2, Node1, Node2, out NewPolygon, out NewNode, Scale, pMap);
                #endregion

                #region 更新
                //更新Map和邻近图
                this.UpdataMapAndPg(Map, pg, Po1, Po2, NewPolygon, Node1, Node2, NewNode);
                Count++;
                Map.WriteResult2Shp(OutPath+Count.ToString(), pMap.SpatialReference);
                #endregion
            }
        }


        /// <summary>
        /// 获取地图中的pattern
        /// </summary>
        public void GetPatterns(SMap Map)
        {
            for (int i = 0; i < Map.PolygonList.Count; i++)
            {
                if (Map.PolygonList[i].PatternID != -1 && Map.PolygonList[i].PatternID != 0)
                {
                    if (!PatternDic.Keys.Contains(Map.PolygonList[i].PatternID))
                    {
                        List<PolygonObject> PatternObject = new List<PolygonObject>();
                        PatternObject.Add(Map.PolygonList[i]);
                        Pattern NewPattern = new Pattern(Map.PolygonList[i].PatternID, PatternObject);
                        PatternDic.Add(Map.PolygonList[i].PatternID, NewPattern);
                    }

                    else
                    {
                        PatternDic[Map.PolygonList[i].PatternID].PatternObjects.Add(Map.PolygonList[i]);
                    }
                }
            }
        }

        /// <summary>
        /// 确定Pattern中的边界建筑物(true是边界建筑物，false是非边界建筑物)
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="pg"></param>
        public void PatternBounaryBuilding(SMap Map, ProxiGraph pg)
        {
            foreach (KeyValuePair<int, Pattern> kv in PatternDic)
            {
                for (int i = 0; i < kv.Value.PatternObjects.Count;i++ )
                {
                    int Count = 0;

                    for (int j = 0; j < pg.PgforBuildingEdgesList.Count; j++)
                    {
                        ProxiEdge Pe = pg.PgforBuildingEdgesList[j];

                        ProxiNode Node1 = Pe.Node1;
                        ProxiNode Node2 = Pe.Node2;

                        PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                        PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                        if (Po1 == kv.Value.PatternObjects[i] && kv.Value.PatternObjects.Contains(Po2))
                        {
                            Count = Count + 1;
                        }

                        else if (Po2 == kv.Value.PatternObjects[i] && kv.Value.PatternObjects.Contains(Po1))
                        {
                            Count = Count + 1;
                        }
                    }

                    if (Count == 1)
                    {
                        kv.Value.PatternObjects[i].BoundaryBuilding = true;
                    }
                }
            }
        }

        /// <summary>
        /// 判断考虑是否依比例尺符号化终止的条件
        /// </summary>
        /// <param name="Map">地图</param>
        /// <param name="pg">邻近图</param>
        /// <param name="Scale">尺度</param>
        /// <returns>返回False不满足终止条件，需要继续判断；true表示满足终止条件</returns>
        public bool MeshStop(SMap Map, ProxiGraph pg,double Scale,double MinDis)
        {
            bool Stop = true;

            foreach (ProxiEdge Pe in pg.PgforBuildingEdgesList)
            {
                ProxiNode Node1 = Pe.Node1;
                ProxiNode Node2 = Pe.Node2;

                PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                if (Po1.Area < Scale * 0.7 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.7 / 1000 * Scale * 0.5 / 1000)
                {
                    double Distance = Math.Sqrt((Po1.CalProxiNode().X - Po2.CalProxiNode().X) * (Po1.CalProxiNode().X - Po2.CalProxiNode().X) +
                        (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y) * (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y));
                    if (Distance < (0.5 + 0.5 + MinDis) * Scale / 1000)
                    {
                        Stop = false;
                    }
                }

                #region 顾及到不依比例尺与依比例尺建筑物冲突（有时可省略）
                else if (Po1.Area < Scale * 0.7 / 1000 * Scale * 0.5 / 1000 && Po2.Area > Scale * 0.7/ 1000 * Scale * 0.5 / 1000)
                {
                    IPoint pPoint = new PointClass();
                    pPoint.X = Po1.CalProxiNode().X; pPoint.Y = Po1.CalProxiNode().Y;

                    IPolygon pPolygon = this.ObjectConvert(Po2);
                    IProximityOperator IPO = pPolygon as IProximityOperator;
                    double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                    if (Distance < (0.5 + MinDis) * Scale / 1000)
                    {
                        Stop = false;
                    }
                }

                else if (Po1.Area > Scale * 0.7/ 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.7 / 1000 * Scale * 0.5 / 1000)
                {
                    IPoint pPoint = new PointClass();
                    pPoint.X = Po2.CalProxiNode().X; pPoint.Y = Po2.CalProxiNode().Y;

                    IPolygon pPolygon = this.ObjectConvert(Po1);
                    IProximityOperator IPO = pPolygon as IProximityOperator;
                    double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                    if (Distance < (0.5 + MinDis) * Scale / 1000)
                    {
                        Stop = false;
                    }
                }
                #endregion
            }

            return Stop;
        }

        /// <summary>
        /// 判断简单典型化依比例尺符号化终止的条件
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="pg"></param>
        /// <param name="Scale"></param>
        /// <param name="MinDis"></param>
        /// <returns></returns>
        public bool SimplyMeshStop(SMap Map, ProxiGraph pg, double Scale, double MinDis)
        {
             bool Stop = true;

             foreach (ProxiEdge Pe in pg.PgforBuildingEdgesList)
             {
                 ProxiNode Node1 = Pe.Node1;
                 ProxiNode Node2 = Pe.Node2;

                 PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                 PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                 #region 不依比例尺符号化建筑物潜在冲突
                 if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                 {
                     double Distance = Math.Sqrt((Po1.CalProxiNode().X - Po2.CalProxiNode().X) * (Po1.CalProxiNode().X - Po2.CalProxiNode().X) +
                         (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y) * (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y));
                     if (Distance < (0.5 + 0.5 + MinDis) * Scale / 1000)
                     {
                         Stop = false;
                     }
                 }
                 #endregion

                 #region 顾及到不依比例尺与依比例尺建筑物冲突(依比例尺符号与不依比例尺符号潜在冲突)
                 else if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                 {
                     IPoint pPoint = new PointClass();
                     pPoint.X = Po1.CalProxiNode().X; pPoint.Y = Po1.CalProxiNode().Y;

                     IPolygon pPolygon = this.ObjectConvert(Po2);
                     IProximityOperator IPO = pPolygon as IProximityOperator;
                     double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                     if (Distance < (0.5 + MinDis) * Scale / 1000)
                     {
                         Stop = false;
                     }
                 }

                 else if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                 {
                     IPoint pPoint = new PointClass();
                     pPoint.X = Po2.CalProxiNode().X; pPoint.Y = Po2.CalProxiNode().Y;

                     IPolygon pPolygon = this.ObjectConvert(Po1);
                     IProximityOperator IPO = pPolygon as IProximityOperator;
                     double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                     if (Distance < (0.5 + MinDis) * Scale / 1000)
                     {
                         Stop = false;
                     }
                 }
                 #endregion

                 #region 依比例尺符号化冲突
                 if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                 {
                     IPolygon Polygon1 = this.ObjectConvert(Po1);
                     IPolygon Polygon2 = this.ObjectConvert(Po2);

                     IProximityOperator IPO = Polygon1 as IProximityOperator;
                     double Distance = IPO.ReturnDistance(Polygon2 as IGeometry);

                     if (Distance <  MinDis * Scale / 1000)
                     {
                         Stop = false;
                     }
                 }
                 #endregion
             }

             return Stop;
        }

        /// <summary>
        /// 判断考虑是否依比例尺符号化和Pattern的终止条件
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="pg"></param>
        /// <param name="Scale"></param>
        /// <param name="MinDis">建筑物间的最短距离</param>
        /// <param name="AverageDistance">Pattern间的平均距离</param>
        /// <returns></returns>
        public bool MeshStopConsiderPattern(SMap Map, ProxiGraph pg, double Scale, double MinDis,double AverageDistance)
        {
            bool Stop = true;

            #region 判断非Pattern建筑物的冲突
            foreach (ProxiEdge Pe in pg.PgforBuildingEdgesList)
            {
                ProxiNode Node1 = Pe.Node1;
                ProxiNode Node2 = Pe.Node2;

                PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                bool PatternLabel1 = this.PatternBuilding(Po1);
                bool PatternLabel2 = this.PatternBuilding(Po2);

                int PatternID = -1;
                int EdgeType = this.PatternEdge(Po1, Po2, out PatternID);

                #region 如果不是Pattern内的边
                if (EdgeType==2||EdgeType==3)
                {
                    #region 与Pattern无关的边
                    if (EdgeType == 3)
                    {
                        #region 两个不依比例尺符号化的建筑物冲突判断
                        if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            double Distance = Math.Sqrt((Po1.CalProxiNode().X - Po2.CalProxiNode().X) * (Po1.CalProxiNode().X - Po2.CalProxiNode().X) +
                                (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y) * (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y));
                            if (Distance < (0.5 + 0.5 + MinDis) * Scale / 1000)
                            {
                                Stop = false;
                            }
                        }
                        #endregion

                        #region 顾及到不依比例尺与依比例尺建筑物冲突（有时可省略）
                        else if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && !PatternLabel1 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            IPoint pPoint = new PointClass();
                            pPoint.X = Po1.CalProxiNode().X; pPoint.Y = Po1.CalProxiNode().Y;

                            IPolygon pPolygon = this.ObjectConvert(Po2);
                            IProximityOperator IPO = pPolygon as IProximityOperator;
                            double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                            if (Distance < (0.5 + MinDis) * Scale / 1000)
                            {
                                Stop = false;
                            }
                        }

                        else if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && !PatternLabel2)
                        {
                            IPoint pPoint = new PointClass();
                            pPoint.X = Po2.CalProxiNode().X; pPoint.Y = Po2.CalProxiNode().Y;

                            IPolygon pPolygon = this.ObjectConvert(Po1);
                            IProximityOperator IPO = pPolygon as IProximityOperator;
                            double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                            if (Distance < (0.5 + MinDis) * Scale / 1000)
                            {
                                Stop = false;
                            }
                        }
                        #endregion
                    }
                    #endregion

                    if (EdgeType == 2)
                    {
                        #region 两个不依比例尺符号化的建筑物冲突判断
                        if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            double Distance = Math.Sqrt((Po1.CalProxiNode().X - Po2.CalProxiNode().X) * (Po1.CalProxiNode().X - Po2.CalProxiNode().X) +
                                (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y) * (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y));
                            if (Distance < (0.5 + 0.5 + MinDis) * Scale / 1000)
                            {
                                Stop = false;
                            }
                        }
                        #endregion

                        #region 至少有一个依比例尺符号化，需要考虑Pattern的问题
                        else if (PatternLabel1 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            IPoint pPoint = new PointClass();
                            pPoint.X = Po2.CalProxiNode().X; pPoint.Y = Po2.CalProxiNode().Y;

                            IPolygon pPolygon = this.ObjectConvert(Po1);
                            IProximityOperator IPO = pPolygon as IProximityOperator;
                            double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                            if (Distance < (0.5 + MinDis) * Scale / 1000)
                            {
                                Stop = false;
                            }
                        }

                        else if (PatternLabel2 && Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            IPoint pPoint = new PointClass();
                            pPoint.X = Po1.CalProxiNode().X; pPoint.Y = Po1.CalProxiNode().Y;

                            IPolygon pPolygon = this.ObjectConvert(Po2);
                            IProximityOperator IPO = pPolygon as IProximityOperator;
                            double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                            if (Distance < (0.5 + MinDis) * Scale / 1000)
                            {
                                Stop = false;
                            }
                        }
                        #endregion
                    }
                }
                #endregion
            }
            #endregion

            #region 判断Pattern内的建筑物是否冲突
            foreach (KeyValuePair<int, Pattern> kv in this.PatternDic)
            {
                double DistanceSum = 0;

                #region 获得一个Pattern内建筑物间的总长度
                foreach (ProxiEdge Pe in pg.PgforBuildingEdgesList)
                {
                    ProxiNode Node1 = Pe.Node1;
                    ProxiNode Node2 = Pe.Node2;

                    PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                    PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                    if (kv.Value.PatternObjects.Contains(Po1) && kv.Value.PatternObjects.Contains(Po2))
                    {
                        #region 两个不依比例尺符号化的建筑物
                        if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            double Distance = Math.Sqrt((Po1.CalProxiNode().X - Po2.CalProxiNode().X) * (Po1.CalProxiNode().X - Po2.CalProxiNode().X) +
                            (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y) * (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y));

                            DistanceSum = DistanceSum + Distance * 1000 / Scale - 0.5 - 0.5;
                        }
                        #endregion

                        #region 两个依比例尺符号化的建筑物
                        else if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            IPolygon Polygon1 = this.ObjectConvert(Po1);
                            IPolygon Polygon2 = this.ObjectConvert(Po2);

                            IProximityOperator IPO = Polygon1 as IProximityOperator;
                            double Distance = IPO.ReturnDistance(Polygon2 as IGeometry);

                            DistanceSum = DistanceSum + Distance * 1000 / Scale;
                        }
                        #endregion

                        #region 一个依比例尺符号化的建筑物，一个不依比例尺符号化的建筑物
                        else if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            IPoint pPoint = new PointClass();
                            pPoint.X = Po1.CalProxiNode().X; pPoint.Y = Po1.CalProxiNode().Y;

                            IPolygon pPolygon = this.ObjectConvert(Po2);
                            IProximityOperator IPO = pPolygon as IProximityOperator;
                            double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                            DistanceSum = DistanceSum + Distance * 1000 / Scale - 0.5;
                        }

                        else if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            IPoint pPoint = new PointClass();
                            pPoint.X = Po2.CalProxiNode().X; pPoint.Y = Po2.CalProxiNode().Y;

                            IPolygon pPolygon = this.ObjectConvert(Po1);
                            IProximityOperator IPO = pPolygon as IProximityOperator;
                            double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                            DistanceSum = DistanceSum + Distance * 1000 / Scale - 0.5;
                        }
                        #endregion 
                    }
                }
                #endregion

                //平均距离小于阈值
                if (DistanceSum / (kv.Value.PatternObjects.Count - 1) < AverageDistance)
                {
                    Stop = false;
                }
            }
            #endregion

            return Stop;
        }

        /// <summary>
        /// 判断
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="pg"></param>
        /// <param name="Scale"></param>
        /// <param name="MinDis"></param>
        /// <param name="AverageDistance"></param>
        /// <returns></returns>
        public bool ProgressiveMeshStop(SMap Map, ProxiGraph pg, double Scale, double MinDis, double AverageDistance)
        {
            bool Stop = true;

            #region 判断非Pattern内建筑物的冲突（无关Pattern；只关于一个Pattern；连接两个Pattern的边）
            foreach (ProxiEdge Pe in pg.PgforBuildingEdgesList)
            {
                ProxiNode Node1 = Pe.Node1;
                ProxiNode Node2 = Pe.Node2;

                PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                bool PatternLabel1 = this.PatternBuilding(Po1);
                bool PatternLabel2 = this.PatternBuilding(Po2);

                int PatternID = -1;
                int EdgeType = this.PatternEdge(Po1, Po2, out PatternID);

                if (EdgeType == 2 || EdgeType == 3)
                {
                    #region 与Pattern无关的边
                    if (EdgeType == 3)
                    {
                        #region 两个不依比例尺符号化的建筑物冲突判断
                        if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            double Distance = Math.Sqrt((Po1.CalProxiNode().X - Po2.CalProxiNode().X) * (Po1.CalProxiNode().X - Po2.CalProxiNode().X) +
                                (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y) * (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y));
                            if (Distance < (0.5 + 0.5 + MinDis) * Scale / 1000)
                            {
                                Stop = false;
                            }
                        }
                        #endregion

                        #region 顾及到不依比例尺与依比例尺建筑物冲突
                        else if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            IPoint pPoint = new PointClass();
                            pPoint.X = Po1.CalProxiNode().X; pPoint.Y = Po1.CalProxiNode().Y;

                            IPolygon pPolygon = this.ObjectConvert(Po2);
                            IProximityOperator IPO = pPolygon as IProximityOperator;
                            double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                            if (Distance < (0.5 + MinDis) * Scale / 1000)
                            {
                                Stop = false;
                            }
                        }

                        else if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            IPoint pPoint = new PointClass();
                            pPoint.X = Po2.CalProxiNode().X; pPoint.Y = Po2.CalProxiNode().Y;

                            IPolygon pPolygon = this.ObjectConvert(Po1);
                            IProximityOperator IPO = pPolygon as IProximityOperator;
                            double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                            if (Distance < (0.5 + MinDis) * Scale / 1000)
                            {
                                Stop = false;
                            }
                        }
                        #endregion

                        #region 两个依比例尺符号化建筑物的冲突 
                        else if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            IPolygon Polygon1 = this.ObjectConvert(Po1);
                            IPolygon Polygon2 = this.ObjectConvert(Po2);

                            PrDispalce.工具类.ParameterCompute parametercompute = new 工具类.ParameterCompute();
                            double Angle1 = parametercompute.GetSMBROrientation(Polygon1);
                            double Angle2 = parametercompute.GetSMBROrientation(Polygon2);

                            #region 判断两个建筑物是否适合合并 true=适合合并；false=不适合合并
                            bool OriLabel = false;
                            if (Math.Abs(Angle1 - Angle2) < 15)
                            {
                                OriLabel = true;
                            }

                            else if (Math.Abs(Angle1 - Angle2) > 90 && (180 - Math.Abs(Angle1 - Angle2) < 15))
                            {
                                OriLabel = true;
                            }

                            else if (Math.Abs(Angle1 - Angle2) > 75 && Math.Abs(Angle1 - Angle2) < 105)
                            {
                                OriLabel = true;
                            }
                            #endregion

                            //若适合合并，但是距离小于阈值需要处理
                            if (OriLabel)
                            {
                                IProximityOperator IPO = Polygon1 as IProximityOperator;
                                double Distance = IPO.ReturnDistance(Polygon2 as IGeometry);

                                if (Distance < MinDis * Scale / 1000)
                                {
                                    Stop = false;
                                }
                            }
                        }
                        #endregion
                    }
                    #endregion

                    #region 有一个建筑物是Pattern中的建筑物
                    if (EdgeType == 2)
                    {
                        #region 两个不依比例尺符号化的建筑物冲突判断
                        //包括一个建筑物在pattern上，另一个建筑物不在pattern上；且都不依比例尺表达
                        //两个建筑物都在不同的pattern上，且都不依比例尺表达
                        if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            double Distance = Math.Sqrt((Po1.CalProxiNode().X - Po2.CalProxiNode().X) * (Po1.CalProxiNode().X - Po2.CalProxiNode().X) +
                                (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y) * (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y));
                            if (Distance < (0.5 + 0.5 + MinDis) * Scale / 1000)
                            {
                                Stop = false;
                            }
                        }
                        #endregion

                        #region 至少有一个依比例尺符号化，需要考虑Pattern的问题
                        #region 若其中一个建筑物是Pattern上的建筑物，且另外一个面积小于阈值
                        //包括一个建筑物，该建筑物在Pattern上，且依比例尺表达；另一个建筑物不依比例尺表达，可在Pattern上，也可不在Pattern上
                        else if (PatternLabel1 && Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            IPoint pPoint = new PointClass();
                            pPoint.X = Po2.CalProxiNode().X; pPoint.Y = Po2.CalProxiNode().Y;

                            IPolygon pPolygon = this.ObjectConvert(Po1);
                            IProximityOperator IPO = pPolygon as IProximityOperator;
                            double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                            if (Distance < (0.5 + MinDis) * Scale / 1000)
                            {
                                Stop = false;
                            }
                        }

                        else if (PatternLabel2 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            IPoint pPoint = new PointClass();
                            pPoint.X = Po1.CalProxiNode().X; pPoint.Y = Po1.CalProxiNode().Y;

                            IPolygon pPolygon = this.ObjectConvert(Po2);
                            IProximityOperator IPO = pPolygon as IProximityOperator;
                            double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                            if (Distance < (0.5 + MinDis) * Scale / 1000)
                            {
                                Stop = false;
                            }
                        }
                        #endregion
                        #endregion
                    }
                    #endregion
                }
            }
            #endregion

            #region 判断Pattern建筑物的冲突
            foreach (KeyValuePair<int, Pattern> kv in this.PatternDic)
            {
                double DistanceSum = 0;

                #region 获得一个Pattern内建筑物间的总长度
                foreach (ProxiEdge Pe in pg.PgforBuildingEdgesList)
                {
                    ProxiNode Node1 = Pe.Node1;
                    ProxiNode Node2 = Pe.Node2;

                    PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                    PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                    if (kv.Value.PatternObjects.Contains(Po1) && kv.Value.PatternObjects.Contains(Po2))
                    {
                        #region 两个不依比例尺符号化的建筑物
                        if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            double Distance = Math.Sqrt((Po1.CalProxiNode().X - Po2.CalProxiNode().X) * (Po1.CalProxiNode().X - Po2.CalProxiNode().X) +
                            (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y) * (Po1.CalProxiNode().Y - Po2.CalProxiNode().Y));

                            DistanceSum = DistanceSum + Distance * 1000 / Scale - 0.5 - 0.5;
                        }
                        #endregion

                        #region 两个依比例尺符号化的建筑物
                        else if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            IPolygon Polygon1 = this.ObjectConvert(Po1);
                            IPolygon Polygon2 = this.ObjectConvert(Po2);

                            IProximityOperator IPO = Polygon1 as IProximityOperator;
                            double Distance = IPO.ReturnDistance(Polygon2 as IGeometry);

                            DistanceSum = DistanceSum + Distance * 1000 / Scale;
                        }
                        #endregion

                        #region 一个依比例尺符号化的建筑物，一个不依比例尺符号化的建筑物
                        else if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            IPoint pPoint = new PointClass();
                            pPoint.X = Po1.CalProxiNode().X; pPoint.Y = Po1.CalProxiNode().Y;

                            IPolygon pPolygon = this.ObjectConvert(Po2);
                            IProximityOperator IPO = pPolygon as IProximityOperator;
                            double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                            DistanceSum = DistanceSum + Distance * 1000 / Scale - 0.5;
                        }

                        else if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                        {
                            IPoint pPoint = new PointClass();
                            pPoint.X = Po2.CalProxiNode().X; pPoint.Y = Po2.CalProxiNode().Y;

                            IPolygon pPolygon = this.ObjectConvert(Po1);
                            IProximityOperator IPO = pPolygon as IProximityOperator;
                            double Distance = IPO.ReturnDistance(pPoint as IGeometry);

                            DistanceSum = DistanceSum + Distance * 1000 / Scale - 0.5;
                        }
                        #endregion
                    }
                }
                #endregion

                //平均距离小于阈值
                if (DistanceSum / (kv.Value.PatternObjects.Count - 1) < AverageDistance)
                {
                    Stop = false;
                }
            }
            #endregion

            return Stop;
        }

        /// <summary>
        /// 判断居民地间是否有冲突(考虑了居民地的符号化与居民地的重要性)
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="pg"></param>
        /// <param name="Scale"></param>
        /// <param name="MinDis"></param>
        /// <returns></returns>
        public bool ProgressiveMeshStop(SMap Map, ProxiGraph pg, double Scale, double MinDis)
        {
             bool Stop = true;

             #region 判断居民地间是否存在冲突
             foreach (ProxiEdge Pe in pg.PgforBuildingEdgesList)
             {
                 ProxiNode Node1 = Pe.Node1;
                 ProxiNode Node2 = Pe.Node2;

                 int Label1 = 0; int Label2 = 0;
                 PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                 PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                 PolygonObject SymbolPo1 = SymbolizedPolygon(Po1, Scale, 0.7, 0.5);
                 PolygonObject SymbolPo2 = SymbolizedPolygon(Po2, Scale, 0.7, 0.5);

                 IPolygon Polygon1 = PolygonObjectConvert(SymbolPo1);
                 IPolygon Polygon2 = PolygonObjectConvert(SymbolPo2);

                 IProximityOperator IPO =  Polygon1 as IProximityOperator;
                 double Distance = IPO.ReturnDistance(Polygon2);

                 #region 获取两个建筑物的重要性
                 if (Po1.PatternID == 1)
                 {
                     Label1 = 3;
                 }
                 else if (Po1.Area > Scale * 0.7 / 1000 * Scale * 0.5 / 1000 || Po1.Road == 1)
                 {
                     Label1 = 2;
                 }

                 if (Po2.PatternID == 1)
                 {
                     Label2 = 3;
                 }
                 else if (Po2.Area > Scale * 0.7 / 1000 * Scale * 0.5 / 1000 || Po2.Road == 1)
                 {
                     Label2 = 2;
                 }
                 #endregion

                 #region 冲突计算标志
                 bool cLabel = true;
                 if (Label1 == 3 && Label2 == 3)
                 {
                     cLabel = false;
                 }
                 else if ((Label1 == 3 && Label2 == 2) || (Label2 == 3 && Label1 == 2))
                 {
                     cLabel = false;
                 }
                 else if (Label1 == 2 && Label2 == 2)
                 {
                     PrDispalce.工具类.ParameterCompute parametercompute = new 工具类.ParameterCompute();
                     double Angle1 = parametercompute.GetSMBROrientation(Polygon1);
                     double Angle2 = parametercompute.GetSMBROrientation(Polygon2);

                     #region 判断两个建筑物是否适合合并 true=适合合并；false=不适合合并
                     bool OriLabel = false;
                     if (Math.Abs(Angle1 - Angle2) < 15)
                     {
                         OriLabel = true;
                     }

                     else if (Math.Abs(Angle1 - Angle2) > 90 && (180 - Math.Abs(Angle1 - Angle2) < 15))
                     {
                         OriLabel = true;
                     }

                     else if (Math.Abs(Angle1 - Angle2) > 75 && Math.Abs(Angle1 - Angle2) < 105)
                     {
                         OriLabel = true;
                     }
                     #endregion

                     if (!OriLabel)
                     {
                         cLabel = false;
                     }
                 }
                 #endregion

                 if (cLabel)
                 {
                     if (Distance < MinDis * Scale / 1000)
                     {
                         Stop = false;
                     }
                 }

             }
            #endregion

             return Stop;
        }

        /// <summary>
        /// 返回符号化的建筑物
        /// </summary>
        /// <param name="Polygon"></param>
        /// <param name="Scale"></param>
        /// <returns></returns>
        public PolygonObject SymbolizedPolygon(PolygonObject Polygon, double Scale, double lLength, double sLength)
        {
            PrDispalce.工具类.ParameterCompute parametercompute = new 工具类.ParameterCompute();
            //MultipleLevelDisplace Md = new MultipleLevelDisplace();

            IPolygon pPolygon = PolygonObjectConvert(Polygon);
            IPolygon SMBR = parametercompute.GetSMBR(pPolygon);

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
            if (LLength < lLength * Scale / 1000 || SLength < sLength * Scale / 1000)
            {
                if (LLength < lLength * Scale / 1000)
                {
                    LLength = lLength * Scale / 1000;
                }

                if (SLength < sLength * Scale / 1000)
                {
                    SLength = sLength * Scale / 1000;
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

        /// <summary>
        /// 判断一个建筑物是Pattern中的建筑物
        /// </summary>
        /// <param name="Po"></param>
        /// <returns>=false,建筑物不是Pattern中的建筑物；=true，建筑物是Pattern中的建筑物</returns>
        bool PatternBuilding(PolygonObject Po)
        {
            bool ContainLabel = false;

            foreach (KeyValuePair<int, Pattern> kv in this.PatternDic)
            {
                if (kv.Value.PatternObjects.Contains(Po))
                {
                    ContainLabel = true;
                }
            }

            return ContainLabel;
        }

        /// <summary>
        /// 判断一个建筑物是Pattern中的建筑物
        /// </summary>
        /// <param name="Po"></param>
        /// <returns>=false,建筑物不是Pattern中的建筑物；=true，建筑物是Pattern中的建筑物
        /// 同时，返回Pattern的ID</returns>
        bool PatternBuildingAndID(PolygonObject Po,out int PatternID)
        {
            PatternID = -1;
            bool ContainLabel = false;

            foreach (KeyValuePair<int, Pattern> kv in this.PatternDic)
            {
                if (kv.Value.PatternObjects.Contains(Po))
                {
                    ContainLabel = true;
                    PatternID = kv.Key;
                }
            }

            return ContainLabel;
        }

        /// <summary>
        /// 对一个给定的邻近边list按照距离降序排序（简单Mesh）
        /// </summary>
        /// <param name="EdgeList"></param>
        Dictionary<ProxiEdge, double> SortPgListByDistance(List<ProxiEdge> EdgeList)
        {
            Dictionary<ProxiEdge, double> dEdgeList = new Dictionary<ProxiEdge, double>();
            for (int i = 0; i < EdgeList.Count; i++)
            {
                dEdgeList.Add(EdgeList[i], EdgeList[i].NearestDistance);
            }

            List<List<ProxiNode>> NodePairList = new List<List<ProxiNode>>();
            Dictionary<ProxiEdge, double> SortEdgeList = dEdgeList.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);

            return SortEdgeList;
        }

        /// <summary>
        /// 对一个给定的邻近边list按照面积加权距离降序排序（简单Mesh）
        /// </summary>
        /// <param name="EdgeList"></param>
        Dictionary<ProxiEdge, double> SortPgListByDistance(SMap smap, List<ProxiEdge> EdgeList, double AreaWeigth)
        {
            Dictionary<ProxiEdge, double> dEdgeList = new Dictionary<ProxiEdge, double>();
            for (int i = 0; i < EdgeList.Count; i++)
            {
                ProxiNode Node1 = EdgeList[i].Node1;
                ProxiNode Node2 = EdgeList[i].Node2;

                PolygonObject Po1 = smap.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject Po2 = smap.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                double NearestDistance = (Po1.Area + Po2.Area) / AreaWeigth * EdgeList[i].NearestDistance;
                dEdgeList.Add(EdgeList[i], NearestDistance);
            }

            List<List<ProxiNode>> NodePairList = new List<List<ProxiNode>>();
            Dictionary<ProxiEdge, double> SortEdgeList = dEdgeList.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);

            return SortEdgeList;
        }

        /// <summary>
        /// 对一个给定的邻近边list按照距离降序排序（简单Mesh，不仅考虑Pattern，还考虑尺度）
        /// </summary>
        /// <param name="smap"></param>
        /// <param name="DistanceLabel">计算Pattern内建筑物的方式 1=最短距离；2=顾及建筑物面积距离</param>
        /// <param name="EdgeList"></param>
        /// <param name="AreaWeigth"></param>
        /// <param name="PatternWeigth"></param>
        /// <returns></returns>
        Dictionary<ProxiEdge, double> SortPgListByDistanceConsiderPattern(SMap smap, int DistanceLabel,List<ProxiEdge> EdgeList, double AreaWeigth,double PatternWeigth,double PatternInWeigth,double Scale)
        {
            Dictionary<ProxiEdge, double> dEdgeList = new Dictionary<ProxiEdge, double>();
            for (int i = 0; i < EdgeList.Count; i++)
            {
                ProxiNode Node1 = EdgeList[i].Node1;
                ProxiNode Node2 = EdgeList[i].Node2;

                PolygonObject Po1 = smap.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject Po2 = smap.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                int PatternID=-1;
                int EdgeType = this.PatternEdge(Po1, Po2,out PatternID);

                #region Pattern内的一条边
                if (EdgeType == 1 )
                {
                    if (DistanceLabel == 1 && (Po1.BoundaryBuilding||Po2.BoundaryBuilding))
                    {
                        double NearestDistance = EdgeList[i].NearestDistance*PatternInWeigth;
                        dEdgeList.Add(EdgeList[i], NearestDistance);
                    }

                    else if (DistanceLabel == 1)
                    {
                        double NearestDistance = EdgeList[i].NearestDistance;
                        dEdgeList.Add(EdgeList[i], NearestDistance);
                    }
                }
                #endregion

                #region 分别连接Pattern内与Pattern外建筑物的一条边
                else if (EdgeType == 2)
                {
                    bool PatternBuildingLabel1 = this.PatternBuilding(Po1);
                    bool PatternBuildingLabel2 = this.PatternBuilding(Po2);

                    if (!PatternBuildingLabel1 && Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                    {
                        dEdgeList.Add(EdgeList[i], 50000);
                    }

                    else if (!PatternBuildingLabel2 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                    {
                        dEdgeList.Add(EdgeList[i], 50000);
                    }

                    else
                    {
                        double NearestDistance = PatternWeigth * EdgeList[i].NearestDistance;
                        dEdgeList.Add(EdgeList[i], NearestDistance);
                    }
                }
                #endregion

                #region Pattern外的一条边
                else if(EdgeType==3)
                {
                    if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                    {
                        dEdgeList.Add(EdgeList[i], EdgeList[i].NearestDistance);
                    }

                    else if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                    {
                        dEdgeList.Add(EdgeList[i], 50000);
                    }

                    else
                    {
                        dEdgeList.Add(EdgeList[i], EdgeList[i].NearestDistance * AreaWeigth);
                    }
                }
                #endregion
            }

            List<List<ProxiNode>> NodePairList = new List<List<ProxiNode>>();
            Dictionary<ProxiEdge, double> SortEdgeList = dEdgeList.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);

            return SortEdgeList;
        }

        /// <summary>
        /// 在渐进式综合过程中，将一个给定的邻近边List按照距离降序排列
        /// </summary>
        /// <param name="smap"></param>
        /// <param name="EdgeList"></param>
        /// <param name="AreaWeight"></param>
        /// <param name="PatternWeight"></param>
        /// <param name="PatternInWeigth"></param>
        /// <param name="Scale"></param>
        /// <returns></returns>
        Dictionary<ProxiEdge, double> SortPgListByDistanceInProgressiveMesh(SMap smap,List<ProxiEdge> EdgeList,double AreaWeight,double PatternWeight,double PatternInWeigth, double Scale)
        {
             Dictionary<ProxiEdge, double> dEdgeList = new Dictionary<ProxiEdge, double>();
             for (int i = 0; i < EdgeList.Count; i++)
             {
                 ProxiNode Node1 = EdgeList[i].Node1;
                 ProxiNode Node2 = EdgeList[i].Node2;

                 PolygonObject Po1 = smap.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                 PolygonObject Po2 = smap.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;


                 int PatternID = -1;
                 int EdgeType = this.PatternEdge(Po1, Po2, out PatternID);

                 #region Pattern内的一条边
                 if (EdgeType == 1)
                 {
                     if (Po1.BoundaryBuilding || Po2.BoundaryBuilding)
                     {
                         double NearestDistance = EdgeList[i].NearestDistance *PatternInWeigth;
                         dEdgeList.Add(EdgeList[i], NearestDistance);
                     }

                     else
                     {
                         double pWeight = 1;
                         if (Po1.Area > Po2.Area)
                         {
                             pWeight = Po1.Area / Po2.Area;
                         }

                         else
                         {
                             pWeight = Po2.Area / Po1.Area;
                         }

                         double NearestDistance = EdgeList[i].NearestDistance * pWeight;
                         dEdgeList.Add(EdgeList[i], NearestDistance);
                     }
                 }
                 #endregion

                 #region 分别连接Pattern内与Pattern外建筑物的一条边
                 else if (EdgeType == 2)
                 {
                     bool PatternBuildingLabel1 = this.PatternBuilding(Po1);
                     bool PatternBuildingLabel2 = this.PatternBuilding(Po2);

                     //Po1不是Pattern上的建筑物，且Po1依比例尺表达
                     if (!PatternBuildingLabel1 && Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                     {
                         dEdgeList.Add(EdgeList[i], 50000);
                     }

                     //Po2不是Pattern上的建筑物，且Po2依比例尺表达
                     else if (!PatternBuildingLabel2 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                     {
                         dEdgeList.Add(EdgeList[i], 50000);
                     }

                     //Po1与Po2是Pattern上的建筑物，且都依比例尺表达
                     else if (PatternBuildingLabel1 && PatternBuildingLabel2 && Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                     {
                         dEdgeList.Add(EdgeList[i], 50000);
                     }

                     //Po1是Pattern上的建筑物，Po2不是Pattern上的建筑物，且其不依比例尺表达
                     else if (PatternBuildingLabel1 && !PatternBuildingLabel2 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                     {
                         double NearestDistance = EdgeList[i].NearestDistance * PatternWeight;
                         dEdgeList.Add(EdgeList[i], NearestDistance);
                     }

                     //Po2是Pattern上的建筑物，但是Po1不是Pattern上的建筑物，且不依比例尺表达
                     else if (PatternBuildingLabel2 && !PatternBuildingLabel1 && Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                     {
                         double NearestDistance = EdgeList[i].NearestDistance * PatternWeight;
                         dEdgeList.Add(EdgeList[i], NearestDistance);
                     }

                     //Po1和Po2都是Pattern上的建筑物
                     else if (PatternBuildingLabel1 && PatternBuildingLabel2)
                     {
                         dEdgeList.Add(EdgeList[i], EdgeList[i].NearestDistance);
                     }
                 }
                 #endregion

                 #region Pattern外的一条边
                 else if (EdgeType == 3)
                 {
                     #region 都不依比例尺表达
                     if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                     {
                         dEdgeList.Add(EdgeList[i], EdgeList[i].NearestDistance);
                     }
                     #endregion

                     #region 都依比例尺表达
                     else if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                     {
                         IPolygon Polygon1 = this.ObjectConvert(Po1);
                         IPolygon Polygon2 = this.ObjectConvert(Po2);

                         PrDispalce.工具类.ParameterCompute parametercompute = new 工具类.ParameterCompute();
                         double Angle1 = parametercompute.GetSMBROrientation(Polygon1);
                         double Angle2 = parametercompute.GetSMBROrientation(Polygon2);

                         #region 判断两个建筑物是否适合合并 true=适合合并；false=不适合合并
                         bool OriLabel = false;
                         if (Math.Abs(Angle1 - Angle2) < 15)
                         {
                             OriLabel = true;
                         }

                         else if (Math.Abs(Angle1 - Angle2) > 90 && (180 - Math.Abs(Angle1 - Angle2) < 15))
                         {
                             OriLabel = true;
                         }

                         else if (Math.Abs(Angle1 - Angle2) > 75 && Math.Abs(Angle1 - Angle2) < 105)
                         {
                             OriLabel = true;
                         }
                         #endregion

                         if (OriLabel)
                         {
                             dEdgeList.Add(EdgeList[i], EdgeList[i].NearestDistance);
                         }

                         else
                         {
                             dEdgeList.Add(EdgeList[i], 50000);
                         }
                     }
                     #endregion

                     #region 其它情况
                     else
                     {
                         dEdgeList.Add(EdgeList[i], EdgeList[i].NearestDistance *AreaWeight);
                     }
                     #endregion
                 }
                 #endregion
             }

             List<List<ProxiNode>> NodePairList = new List<List<ProxiNode>>();
             Dictionary<ProxiEdge, double> SortEdgeList = dEdgeList.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);
             return SortEdgeList;
        }

        /// <summary>
        ///对一个给定的邻近边list按照距离降序排列，但是考虑了尺度
        /// </summary>
        /// <param name="EdgeList"></param>
        /// <param name="Scale"></param>
        /// <returns></returns>
        Dictionary<ProxiEdge, double> SortPgListByDistance(SMap smap, List<ProxiEdge> EdgeList, double Scale, double Weigth)
        {
            Dictionary<ProxiEdge, double> dEdgeList = new Dictionary<ProxiEdge, double>();
            for (int i = 0; i < EdgeList.Count; i++)
            {
                ProxiNode Node1 = EdgeList[i].Node1;
                ProxiNode Node2 = EdgeList[i].Node2;

                PolygonObject Po1 = smap.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject Po2 = smap.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                {
                    dEdgeList.Add(EdgeList[i], EdgeList[i].NearestDistance);
                }


                else if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                {
                    dEdgeList.Add(EdgeList[i], 50000);
                }

                else
                {
                    dEdgeList.Add(EdgeList[i], EdgeList[i].NearestDistance * Weigth);
                }
            }

            List<List<ProxiNode>> NodePairList = new List<List<ProxiNode>>();
            Dictionary<ProxiEdge, double> SortEdgeList = dEdgeList.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);

            return SortEdgeList;
        }

        /// <summary>
        ///对一个给定的邻近边list按照距离降序排列，但是考虑了尺度
        /// </summary>
        /// <param name="EdgeList"></param>
        /// <param name="Scale"></param>
        /// <returns></returns>
        Dictionary<ProxiEdge, double> SortPgListByDistance2(SMap smap, List<ProxiEdge> EdgeList, double Scale, double Weigth)
        {
            Dictionary<ProxiEdge, double> dEdgeList = new Dictionary<ProxiEdge, double>();
            for (int i = 0; i < EdgeList.Count; i++)
            {
                int Label1=0;int Label2=0;

                ProxiNode Node1 = EdgeList[i].Node1;
                ProxiNode Node2 = EdgeList[i].Node2;

                PolygonObject Po1 = smap.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                PolygonObject Po2 = smap.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                #region 获取两个建筑物的重要性
                if(Po1.PatternID==1)
                {
                    Label1=3;
                }
                else if (Po1.Area > Scale * 0.7 / 1000 * Scale * 0.5 / 1000 ||Po1.Road==1)
                {
                    Label1 = 2;
                }
                else if (Po1.SortID == 1)
                {
                    Label1 = 1;
                }
                else
                {
                    Label1=0;
                }

                if (Po2.PatternID == 1)
                {
                    Label2 = 3;
                }
                else if (Po2.Area > Scale * 0.7 / 1000 * Scale * 0.5 / 1000 || Po2.Road == 1)
                {
                    Label2 = 2;
                }
                else if (Po2.SortID == 1)
                {
                    Label2 = 1;
                }
                else
                {
                    Label2 = 0;
                }
                #endregion

                #region 边赋值
                #region 排列中的两个居民地
                if ( Label1 == 3 && (Label2 == 3 || Label2 == 2))
                {
                    dEdgeList.Add(EdgeList[i], 50000);
                }

                else if (Label2 == 3 && (Label1 == 2 || Label1 == 3))
                {
                    dEdgeList.Add(EdgeList[i], 50000);
                }
                #endregion

                #region 都是需要保留的非排列中居民地
                else if (Label1 == Label2 && Label1 == 2)
                {
                    IPolygon Polygon1 = this.ObjectConvert(Po1);
                    IPolygon Polygon2 = this.ObjectConvert(Po2);

                    PrDispalce.工具类.ParameterCompute parametercompute = new 工具类.ParameterCompute();
                    double Angle1 = parametercompute.GetSMBROrientation(Polygon1);
                    double Angle2 = parametercompute.GetSMBROrientation(Polygon2);

                    #region 判断两个建筑物是否适合合并 true=适合合并；false=不适合合并
                    bool OriLabel = false;
                    if (Math.Abs(Angle1 - Angle2) < 15)
                    {
                        OriLabel = true;
                    }

                    else if (Math.Abs(Angle1 - Angle2) > 90 && (180 - Math.Abs(Angle1 - Angle2) < 15))
                    {
                        OriLabel = true;
                    }

                    else if (Math.Abs(Angle1 - Angle2) > 75 && Math.Abs(Angle1 - Angle2) < 105)
                    {
                        OriLabel = true;
                    }
                    #endregion

                    if (OriLabel && EdgeList[i].NearestDistance < Scale * 0.2 / 1000)
                    {
                        dEdgeList.Add(EdgeList[i], EdgeList[i].NearestDistance);
                    }
                    else
                    {
                        dEdgeList.Add(EdgeList[i], 50000);
                    }
                }
                #endregion

                #region 重要性不相同的居民地
                else if (Label1 != Label2)
                {
                    dEdgeList.Add(EdgeList[i], EdgeList[i].NearestDistance * Weigth);
                }
                #endregion

                #region 重要性相同的居民地
                else
                {
                    dEdgeList.Add(EdgeList[i], EdgeList[i].NearestDistance);
                }
                #endregion
                #endregion
            }

            List<List<ProxiNode>> NodePairList = new List<List<ProxiNode>>();
            Dictionary<ProxiEdge, double> SortEdgeList = dEdgeList.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);

            return SortEdgeList;
        }

        /// <summary>
        /// 判断是否是Pattern中的一条边
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <returns>1=边是Pattern内的一条边；=2边分别连接了Pattern内与Pattern外的一个建筑物；=3边连接了Pattern外的两个建筑物</returns>
        int PatternEdge(PolygonObject Po1, PolygonObject Po2,out int PatternID)
        {
            int PatternEdge = 3;
            PatternID=-1;
            foreach (KeyValuePair<int, Pattern> kv in PatternDic)
            {
                if (kv.Value.PatternObjects.Contains(Po1) && kv.Value.PatternObjects.Contains(Po2))
                {
                    PatternEdge = 1;
                    PatternID = kv.Key;                                                               
                }

                else if (kv.Value.PatternObjects.Contains(Po1) || kv.Value.PatternObjects.Contains(Po2))
                {
                    PatternEdge = 2;
                    PatternID = kv.Key;
                }

            }

            return PatternEdge;
        }

        /// <summary>
        /// 根据给定的两个建筑物，生成一个新的建筑物
        /// </summary>
        /// <param name="Po1">建筑物1</param>
        /// <param name="Po2">建筑物2</param>
        /// <param name="Label1">方法标识，1=删除一个；2=利用两个建筑物创建一个新的</param>
        /// <param name="Label2">考虑因素标识，1=只考虑位置；2=不仅考虑位置，还考虑面积（利用面积加权）；
        /// 3=考虑面积，但是即大于一定面积的建筑物不移动;4=考虑Pattern，即若pattern内与pattern外建筑物冲突，
        /// pattern内建筑物不移动，且Pattern内建筑物位置还是中心;=5,Pattern内建筑物位置考虑Area</param>
        /// <returns></returns>
        void GetNewPolygonAndNode(PolygonObject Po1, PolygonObject Po2,int Label1,int Label2,ProxiNode Node1,ProxiNode Node2,out PolygonObject NewPolygonObject,out ProxiNode NewNode,double Scale)
        {
            NewPolygonObject = null;
            NewNode = null;

            if (Label1 == 1 && Label2 == 1)
            {
                #region 计算新建筑物的位置
                double NewX = (Po1.CalProxiNode().X + Po2.CalProxiNode().X) / 2;
                double NewY = (Po1.CalProxiNode().Y + Po2.CalProxiNode().Y) / 2;
                #endregion

                #region 保留大面积的建筑物
                if (Po1.Area > Po2.Area)
                {
                    NewPolygonObject = Po1;
                    NewNode = Node1;

                    NewPolygonObject.TyList.Add(Po2.ID);//存储哪些建筑物被当做一个建筑物表达
                    if (Po2.TyList.Count > 0)
                    {
                        NewPolygonObject.TyList.AddRange(Po2.TyList);
                    }
                }

                else
                {
                    NewPolygonObject = Po2;
                    NewNode = Node2;

                    NewPolygonObject.TyList.Add(Po1.ID);//存储哪些建筑物被当做一个建筑物表达
                    if (Po1.TyList.Count > 0)
                    {
                        NewPolygonObject.TyList.AddRange(Po1.TyList);
                    }
                }
                #endregion

                #region 将建筑物和节点移到新的位置
                double curDx = NewX - NewPolygonObject.CalProxiNode().X;
                double curDy = NewY - NewPolygonObject.CalProxiNode().Y;

                //更新多边形点集的每一个点坐标
                foreach (TriNode curPoint in NewPolygonObject.PointList)
                {
                    curPoint.X += curDx;
                    curPoint.Y += curDy;
                }

                //更新节点坐标
                NewNode.X += curDx;
                NewNode.Y += curDy;
                #endregion
            }

            if (Label1 == 1 && Label2 == 3)
            {
                #region 计算新建筑物的位置
                double NewX = 0; double NewY = 0;
                if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                {
                    NewX = (Po1.CalProxiNode().X + Po2.CalProxiNode().X) / 2;
                    NewY = (Po1.CalProxiNode().Y + Po2.CalProxiNode().Y) / 2;
                }

                if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                {
                    NewX = Po1.CalProxiNode().X ;
                    NewY = Po1.CalProxiNode().Y ;
                }

                else if (Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                {
                    NewX = Po2.CalProxiNode().X;
                    NewY = Po2.CalProxiNode().Y;
                }
                #endregion

                #region 保留大面积的建筑物
                if (Po1.Area >= Po2.Area)
                {
                    NewPolygonObject = Po1;
                    NewNode = Node1;

                    NewPolygonObject.TyList.Add(Po2.ID);//存储哪些建筑物被当做一个建筑物表达
                    if (Po2.TyList.Count > 0)
                    {
                        NewPolygonObject.TyList.AddRange(Po2.TyList);
                    }
                }

                else
                {
                    NewPolygonObject = Po2;
                    NewNode = Node2;

                    NewPolygonObject.TyList.Add(Po1.ID);//存储哪些建筑物被当做一个建筑物表达
                    if (Po1.TyList.Count > 0)
                    {
                        NewPolygonObject.TyList.AddRange(Po1.TyList);
                    }
                }
                #endregion

                #region 将建筑物和节点移到新的位置
                double curDx = NewX - NewPolygonObject.CalProxiNode().X;
                double curDy = NewY - NewPolygonObject.CalProxiNode().Y;

                //更新多边形点集的每一个点坐标
                foreach (TriNode curPoint in NewPolygonObject.PointList)
                {
                    curPoint.X += curDx;
                    curPoint.Y += curDy;
                }

                //更新节点坐标
                NewNode.X += curDx;
                NewNode.Y += curDy;
                #endregion
            }

            if (Label1 == 1 && Label2 == 4)
            {
                int PatternID = -1;
                int EdgeType = this.PatternEdge(Po1, Po2, out PatternID);

                double NewX = 0; double NewY = 0;

                if (EdgeType == 1 || EdgeType == 3)
                {
                    #region 计算新建筑物的位置
                    if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                    {
                        NewX = (Po1.CalProxiNode().X + Po2.CalProxiNode().X) / 2;
                        NewY = (Po1.CalProxiNode().Y + Po2.CalProxiNode().Y) / 2;
                    }

                    if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                    {
                        NewX = Po1.CalProxiNode().X;
                        NewY = Po1.CalProxiNode().Y;
                    }

                    else if (Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                    {
                        NewX = Po2.CalProxiNode().X;
                        NewY = Po2.CalProxiNode().Y;
                    }
                    #endregion

                    #region 保留大面积的建筑物
                    if (Po1.Area >= Po2.Area)
                    {
                        NewPolygonObject = Po1;
                        NewNode = Node1;

                        NewPolygonObject.TyList.Add(Po2.ID);//存储哪些建筑物被当做一个建筑物表达
                        if (Po2.TyList.Count > 0)
                        {
                            NewPolygonObject.TyList.AddRange(Po2.TyList);
                        }
                    }

                    else
                    {
                        NewPolygonObject = Po2;
                        NewNode = Node2;

                        NewPolygonObject.TyList.Add(Po1.ID);//存储哪些建筑物被当做一个建筑物表达
                        if (Po1.TyList.Count > 0)
                        {
                            NewPolygonObject.TyList.AddRange(Po1.TyList);
                        }
                    }
                    #endregion             
                }

                else if (EdgeType == 2)
                {
                    #region 计算新建筑物的位置
                    bool PatternBuildingLabel1 = this.PatternBuilding(Po1);
                    bool PatternBuildingLabel2 = this.PatternBuilding(Po2);

                    if (PatternBuildingLabel1)
                    {
                        NewX = Po1.CalProxiNode().X;
                        NewY = Po1.CalProxiNode().Y;
                    }

                    if (PatternBuildingLabel2)
                    {
                        NewX = Po2.CalProxiNode().X;
                        NewY = Po2.CalProxiNode().Y;
                    }
                    #endregion

                    #region 保留Pattern中的建筑物
                    if (PatternBuildingLabel1)
                    {
                        NewPolygonObject = Po1;
                        NewNode = Node1;

                        NewPolygonObject.TyList.Add(Po2.ID);//存储哪些建筑物被当做一个建筑物表达
                        if (Po2.TyList.Count > 0)
                        {
                            NewPolygonObject.TyList.AddRange(Po2.TyList);
                        }
                    }

                    if (PatternBuildingLabel2)
                    {

                        NewPolygonObject = Po2;
                        NewNode = Node2;

                        NewPolygonObject.TyList.Add(Po1.ID);//存储哪些建筑物被当做一个建筑物表达
                        if (Po1.TyList.Count > 0)
                        {
                            NewPolygonObject.TyList.AddRange(Po1.TyList);
                        }
                    }
                    #endregion
                }

                #region 将建筑物和节点移到新的位置
                double curDx = NewX - NewPolygonObject.CalProxiNode().X;
                double curDy = NewY - NewPolygonObject.CalProxiNode().Y;

                //更新多边形点集的每一个点坐标
                foreach (TriNode curPoint in NewPolygonObject.PointList)
                {
                    curPoint.X += curDx;
                    curPoint.Y += curDy;
                }

                //更新节点坐标
                NewNode.X += curDx;
                NewNode.Y += curDy;
                #endregion
            }
        }

        /// <summary>
        /// 在渐进式综合中获得新建筑物
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <param name="Node1"></param>
        /// <param name="Node2"></param>
        /// <param name="NewPolygonObject"></param>
        /// <param name="NewNode"></param>
        /// <param name="Scale"></param>
        /// <param name="pMap"></param>
        void GetNewPolygonAndNodeInProgressiveMesh(PolygonObject Po1, PolygonObject Po2, ProxiNode Node1, ProxiNode Node2, out PolygonObject NewPolygonObject, out ProxiNode NewNode, double Scale,IMap pMap)
        {
            NewPolygonObject = null;
            NewNode = null;

            int PatternID = -1; int PatternID1 = -1; int PatternID2 = -1;
            int EdgeType = this.PatternEdge(Po1, Po2, out PatternID);

            bool PatternLabel1=this.PatternBuildingAndID(Po1,out PatternID1);
            bool PatternLabel2=this.PatternBuildingAndID(Po2,out PatternID2);
            
            #region Pattern内的两个建筑物
            if (EdgeType == 1)
            {
                double NewX = 0; double NewY = 0;

                #region 计算新建筑物的位置
                NewX = (Po1.CalProxiNode().X + Po2.CalProxiNode().X) / 2;
                NewY = (Po1.CalProxiNode().Y + Po2.CalProxiNode().Y) / 2;
                #endregion

                #region 保留大面积的建筑物
                if (Po1.Area > Po2.Area)
                {
                    NewPolygonObject = Po1;
                    NewNode = Node1;

                    NewPolygonObject.TyList.Add(Po2.ID);//存储哪些建筑物被当做一个建筑物表达
                    if (Po2.TyList.Count > 0)
                    {
                        NewPolygonObject.TyList.AddRange(Po2.TyList);
                    }
                }

                else
                {
                    NewPolygonObject = Po2;
                    NewNode = Node2;

                    NewPolygonObject.TyList.Add(Po1.ID);//存储哪些建筑物被当做一个建筑物表达
                    if (Po1.TyList.Count > 0)
                    {
                        NewPolygonObject.TyList.AddRange(Po1.TyList);
                    }
                }
                #endregion

                #region 将建筑物和节点移到新的位置
                double curDx = NewX - NewPolygonObject.CalProxiNode().X;
                double curDy = NewY - NewPolygonObject.CalProxiNode().Y;

                //更新多边形点集的每一个点坐标
                foreach (TriNode curPoint in NewPolygonObject.PointList)
                {
                    curPoint.X += curDx;
                    curPoint.Y += curDy;
                }

                //更新节点坐标
                NewNode.X += curDx;
                NewNode.Y += curDy;
                #endregion
            }
            #endregion
            
            #region Pattern外的两个建筑物
            if (EdgeType == 3)
            {
                double NewX = 0; double NewY = 0; bool GetLabel = false;

                #region 计算新建筑物的位置
                //都是不依比例尺符号化的建筑物
                if (Po1.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area < Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                {
                    NewX = (Po1.CalProxiNode().X + Po2.CalProxiNode().X) / 2;
                    NewY = (Po1.CalProxiNode().Y + Po2.CalProxiNode().Y) / 2;
                }
                
                //都是依比例尺符号化的建筑物（由于在距离计算时，不适合合并的两个建筑物被标识为MaxDis，所以，这里的建筑物都是适合合并的建筑物）
                else if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000 && Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                {
                    PrDispalce.工具类.CollabrativeDisplacement.Collabration Co = new 工具类.CollabrativeDisplacement.Collabration();
                    NewPolygonObject = Co.BuildingAggreationInTypification(Po1, Po2, pMap, Po1.ID.ToString() + Po2.ID.ToString(), true, null);
                    NewNode = Node1; NewNode.X = NewPolygonObject.CalProxiNode().X; NewNode.Y = NewPolygonObject.CalProxiNode().Y;
                    NewX = NewPolygonObject.CalProxiNode().X; NewY = NewPolygonObject.CalProxiNode().Y;

                    NewPolygonObject.TyList.Add(Po2.ID);//存储哪些建筑物被当做一个建筑物表达
                    if (Po2.TyList.Count > 0)
                    {
                        NewPolygonObject.TyList.AddRange(Po2.TyList);
                    }

                    GetLabel = true;
                }

                //有一个是依比例尺符号化的建筑物
                else if (Po1.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                {
                    NewX = Po1.CalProxiNode().X;
                    NewY = Po1.CalProxiNode().Y;
                }

                else if (Po2.Area > Scale * 0.5 / 1000 * Scale * 0.5 / 1000)
                {
                    NewX = Po2.CalProxiNode().X;
                    NewY = Po2.CalProxiNode().Y;
                }
                #endregion

                #region 保留大面积的建筑物
                if (Po1.Area >= Po2.Area && !GetLabel)
                {
                    NewPolygonObject = Po1;
                    NewNode = Node1;

                    NewPolygonObject.TyList.Add(Po2.ID);//存储哪些建筑物被当做一个建筑物表达
                    if (Po2.TyList.Count > 0)
                    {
                        NewPolygonObject.TyList.AddRange(Po2.TyList);
                    }
                }

                else if (Po1.Area < Po2.Area && !GetLabel)
                {
                    NewPolygonObject = Po2;
                    NewNode = Node2;

                    NewPolygonObject.TyList.Add(Po1.ID);//存储哪些建筑物被当做一个建筑物表达
                    if (Po1.TyList.Count > 0)
                    {
                        NewPolygonObject.TyList.AddRange(Po1.TyList);
                    }
                }
                #endregion             

                #region 将建筑物和节点移到新的位置
                double curDx = NewX - NewPolygonObject.CalProxiNode().X;
                double curDy = NewY - NewPolygonObject.CalProxiNode().Y;

                //更新多边形点集的每一个点坐标
                foreach (TriNode curPoint in NewPolygonObject.PointList)
                {
                    curPoint.X += curDx;
                    curPoint.Y += curDy;
                }

                //更新节点坐标
                NewNode.X += curDx;
                NewNode.Y += curDy;
                #endregion

            }
            #endregion

            #region 至少涉及到一个Pattern的两个建筑物
            if (EdgeType == 2)
            {
                double NewX = 0; double NewY = 0;

                #region 计算新建筑物的位置
                #region 如果两个建筑物属于两个不同的Pattern
                if (PatternLabel1 && PatternLabel2)
                {
                    #region 根据重要性对Pattern进行删除
                    if (this.PatternDic[PatternID1].PatternObjects.Count > this.PatternDic[PatternID2].PatternObjects.Count)
                    {
                        NewX = Po1.CalProxiNode().X;
                        NewY = Po1.CalProxiNode().Y;

                        //需要对Pattern进行更新
                    }

                    else if (this.PatternDic[PatternID1].PatternObjects.Count < this.PatternDic[PatternID2].PatternObjects.Count)
                    {
                        NewX = Po2.CalProxiNode().X;
                        NewY = Po2.CalProxiNode().Y;
                    }

                    else
                    {
                        double AreaSum1 = 0; double AreaSum2 = 0;

                        for (int i = 0; i < this.PatternDic[PatternID1].PatternObjects.Count; i++)
                        {
                            AreaSum1 = AreaSum1 + this.PatternDic[PatternID1].PatternObjects[i].Area;
                        }

                        for (int i = 0; i < this.PatternDic[PatternID2].PatternObjects.Count; i++)
                        {
                            AreaSum2 = AreaSum2 + this.PatternDic[PatternID2].PatternObjects[i].Area;
                        }

                        if (AreaSum1 > AreaSum2)
                        {
                            NewX = Po1.CalProxiNode().X;
                            NewY = Po1.CalProxiNode().Y;
                        }

                        else
                        {
                            NewX = Po2.CalProxiNode().X;
                            NewY = Po2.CalProxiNode().Y;
                        }
                    }
                    #endregion

                    #region 根据Pattern的特征考虑是否连接
                    #endregion
                }
                #endregion

                #region 如果只有一个建筑物属于一个Pattern
                else if (PatternLabel1)//Po1属于一个Pattern
                {
                    NewX = Po1.CalProxiNode().X;
                    NewY = Po1.CalProxiNode().Y;
                }

                else if (PatternLabel2)//Po2属于一个Pattern
                {
                    NewX = Po2.CalProxiNode().X;
                    NewY = Po2.CalProxiNode().Y;
                }
                #endregion
                #endregion

                #region 保留Pattern中的建筑物
                if (PatternLabel1 && PatternLabel2)//如果两个建筑物属于两个不同的Pattern
                {
                    #region 根据重要性对Pattern进行删除
                    if (this.PatternDic[PatternID1].PatternObjects.Count > this.PatternDic[PatternID2].PatternObjects.Count)
                    {
                        NewPolygonObject = Po1;
                        NewNode = Node1;

                        NewPolygonObject.TyList.Add(Po2.ID);//存储哪些建筑物被当做一个建筑物表达
                        if (Po2.TyList.Count > 0)
                        {
                            NewPolygonObject.TyList.AddRange(Po2.TyList);
                        }
                    }

                    else if (this.PatternDic[PatternID1].PatternObjects.Count < this.PatternDic[PatternID2].PatternObjects.Count)
                    {
                        NewPolygonObject = Po2;
                        NewNode = Node2;

                        NewPolygonObject.TyList.Add(Po1.ID);//存储哪些建筑物被当做一个建筑物表达
                        if (Po1.TyList.Count > 0)
                        {
                            NewPolygonObject.TyList.AddRange(Po1.TyList);
                        }
                    }

                    else
                    {
                        double AreaSum1 = 0; double AreaSum2 = 0;

                        for (int i = 0; i < this.PatternDic[PatternID1].PatternObjects.Count; i++)
                        {
                            AreaSum1 = AreaSum1 + this.PatternDic[PatternID1].PatternObjects[i].Area;
                        }

                        for (int i = 0; i < this.PatternDic[PatternID2].PatternObjects.Count; i++)
                        {
                            AreaSum2 = AreaSum2 + this.PatternDic[PatternID2].PatternObjects[i].Area;
                        }

                        if (AreaSum1 > AreaSum2)
                        {
                            NewPolygonObject = Po1;
                            NewNode = Node1;

                            NewPolygonObject.TyList.Add(Po2.ID);//存储哪些建筑物被当做一个建筑物表达
                            if (Po2.TyList.Count > 0)
                            {
                                NewPolygonObject.TyList.AddRange(Po2.TyList);
                            }
                        }

                        else
                        {
                            NewPolygonObject = Po2;
                            NewNode = Node2;

                            NewPolygonObject.TyList.Add(Po1.ID);//存储哪些建筑物被当做一个建筑物表达
                            if (Po1.TyList.Count > 0)
                            {
                                NewPolygonObject.TyList.AddRange(Po1.TyList);
                            }
                        }
                    }
                    #endregion
                }

                else if (PatternLabel1)//Po1属于一个Pattern
                {
                    NewPolygonObject = Po1;
                    NewNode = Node1;

                    NewPolygonObject.TyList.Add(Po2.ID);//存储哪些建筑物被当做一个建筑物表达
                    if (Po2.TyList.Count > 0)
                    {
                        NewPolygonObject.TyList.AddRange(Po2.TyList);
                    }
                }

                else if (PatternLabel2)//Po2属于一个Pattern
                {

                    NewPolygonObject = Po2;
                    NewNode = Node2;

                    NewPolygonObject.TyList.Add(Po1.ID);//存储哪些建筑物被当做一个建筑物表达
                    if (Po1.TyList.Count > 0)
                    {
                        NewPolygonObject.TyList.AddRange(Po1.TyList);
                    }
                }
                #endregion

                #region 将建筑物和节点移到新的位置
                double curDx = NewX - NewPolygonObject.CalProxiNode().X;
                double curDy = NewY - NewPolygonObject.CalProxiNode().Y;

                //更新多边形点集的每一个点坐标
                foreach (TriNode curPoint in NewPolygonObject.PointList)
                {
                    curPoint.X += curDx;
                    curPoint.Y += curDy;
                }

                //更新节点坐标
                NewNode.X += curDx;
                NewNode.Y += curDy;
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// 在渐进式综合中获得新建筑物
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <param name="Node1"></param>
        /// <param name="Node2"></param>
        /// <param name="NewPolygonObject"></param>
        /// <param name="NewNode"></param>
        /// <param name="Scale"></param>
        /// <param name="pMap"></param>
        void GetNewPolygonAndNodeInProgressiveMesh2(PolygonObject Po1, PolygonObject Po2, ProxiNode Node1, ProxiNode Node2, out PolygonObject NewPolygonObject, out ProxiNode NewNode, double Scale, IMap pMap)
        {
            NewPolygonObject = null;
            NewNode = null;

            int Label1 = 0; int Label2 = 0;

            #region 获取两个建筑物的重要性
            if (Po1.PatternID == 1)
            {
                Label1 = 3;
            }
            else if (Po1.Area > Scale * 0.7 / 1000 * Scale * 0.5 / 1000 || Po1.Road == 1)
            {
                Label1 = 2;
            }
            else if (Po1.SortID == 1)
            {
                Label1 = 1;
            }
            else
            {
                Label1 = 0;
            }

            if (Po2.PatternID == 1)
            {
                Label2 = 3;
            }
            else if (Po2.Area > Scale * 0.7 / 1000 * Scale * 0.5 / 1000 || Po2.Road == 1)
            {
                Label2 = 2;
            }
            else if (Po2.SortID == 1)
            {
                Label2 = 1;
            }
            else
            {
                Label2 = 0;
            }
            #endregion

            #region 若综合的目标涉及的Pattern中居民地
            if (Label1 == 3 && (Label2 == 0||Label2==1))
            {
                //居民地的新位置
                double NewX = Po1.CalProxiNode().X; double NewY = Po1.CalProxiNode().Y; 

                #region 新居民地与新节点
                NewPolygonObject = Po1;
                NewNode = Node1;

                NewPolygonObject.TyList.Add(Po2.ID);//存储哪些建筑物被当做一个建筑物表达
                if (Po2.TyList.Count > 0)
                {
                    NewPolygonObject.TyList.AddRange(Po2.TyList);
                }
                #endregion

                #region 将建筑物和节点移到新的位置
                double curDx = NewX - NewPolygonObject.CalProxiNode().X;
                double curDy = NewY - NewPolygonObject.CalProxiNode().Y;

                //更新多边形点集的每一个点坐标
                foreach (TriNode curPoint in NewPolygonObject.PointList)
                {
                    curPoint.X += curDx;
                    curPoint.Y += curDy;
                }

                //更新节点坐标
                NewNode.X += curDx;
                NewNode.Y += curDy;
                #endregion

            }

            else if (Label2 == 3 && (Label1 == 0 || Label1 == 1))
            {
                double NewX = Po2.CalProxiNode().X; double NewY = Po2.CalProxiNode().Y;

                #region 生成新节点与新建筑物
                NewPolygonObject = Po2;
                NewNode = Node2;

                NewPolygonObject.TyList.Add(Po1.ID);//存储哪些建筑物被当做一个建筑物表达
                if (Po1.TyList.Count > 0)
                {
                    NewPolygonObject.TyList.AddRange(Po1.TyList);
                }
                #endregion

                #region 将建筑物和节点移到新的位置
                double curDx = NewX - NewPolygonObject.CalProxiNode().X;
                double curDy = NewY - NewPolygonObject.CalProxiNode().Y;

                //更新多边形点集的每一个点坐标
                foreach (TriNode curPoint in NewPolygonObject.PointList)
                {
                    curPoint.X += curDx;
                    curPoint.Y += curDy;
                }

                //更新节点坐标
                NewNode.X += curDx;
                NewNode.Y += curDy;
                #endregion

            }
            #endregion

            #region 综合的两个居民地均为重要居民地；合并
            else if (Label1 == Label2 && Label1 == 2)
            {
                #region 合并操作
                PrDispalce.工具类.CollabrativeDisplacement.Collabration Co = new 工具类.CollabrativeDisplacement.Collabration();
                NewPolygonObject = Co.BuildingAggreationInTypification(Po1, Po2, pMap, Po1.ID.ToString() + Po2.ID.ToString(), true, null);
                NewNode = Node1; NewNode.X = NewPolygonObject.CalProxiNode().X; NewNode.Y = NewPolygonObject.CalProxiNode().Y;
                double NewX = NewPolygonObject.CalProxiNode().X;double NewY = NewPolygonObject.CalProxiNode().Y;
                NewPolygonObject.SortID = Math.Max(Po1.SortID, Po2.SortID);
                NewPolygonObject.Road = Math.Max(Po1.Road, Po2.Road);

                NewPolygonObject.TyList.Add(Po2.ID);//存储哪些建筑物被当做一个建筑物表达
                if (Po2.TyList.Count > 0)
                {
                    NewPolygonObject.TyList.AddRange(Po2.TyList);
                }
                #endregion

                #region 将建筑物和节点移到新的位置
                double curDx = NewX - NewPolygonObject.CalProxiNode().X;
                double curDy = NewY - NewPolygonObject.CalProxiNode().Y;

                //更新多边形点集的每一个点坐标
                foreach (TriNode curPoint in NewPolygonObject.PointList)
                {
                    curPoint.X += curDx;
                    curPoint.Y += curDy;
                }

                //更新节点坐标
                NewNode.X += curDx;
                NewNode.Y += curDy;
                #endregion

            }
            #endregion

            #region 两个居民地的重要性不同
            else if (Label1 != Label2)
            {
                #region 居民地1较重要
                if (Label1 > Label2)
                {
                    //居民地的新位置
                    double NewX = Po1.CalProxiNode().X; double NewY = Po1.CalProxiNode().Y;

                    #region 新居民地与新节点
                    NewPolygonObject = Po1;
                    NewNode = Node1;

                    NewPolygonObject.TyList.Add(Po2.ID);//存储哪些建筑物被当做一个建筑物表达
                    if (Po2.TyList.Count > 0)
                    {
                        NewPolygonObject.TyList.AddRange(Po2.TyList);
                    }
                    #endregion

                    #region 将建筑物和节点移到新的位置
                    double curDx = NewX - NewPolygonObject.CalProxiNode().X;
                    double curDy = NewY - NewPolygonObject.CalProxiNode().Y;

                    //更新多边形点集的每一个点坐标
                    foreach (TriNode curPoint in NewPolygonObject.PointList)
                    {
                        curPoint.X += curDx;
                        curPoint.Y += curDy;
                    }

                    //更新节点坐标
                    NewNode.X += curDx;
                    NewNode.Y += curDy;
                    #endregion
                }
                #endregion

                #region 居民点2较重要
                else
                {
                    double NewX = Po2.CalProxiNode().X; double NewY = Po2.CalProxiNode().Y;

                    #region 生成新节点与新建筑物
                    NewPolygonObject = Po2;
                    NewNode = Node2;

                    NewPolygonObject.TyList.Add(Po1.ID);//存储哪些建筑物被当做一个建筑物表达
                    if (Po1.TyList.Count > 0)
                    {
                        NewPolygonObject.TyList.AddRange(Po1.TyList);
                    }
                    #endregion

                    #region 将建筑物和节点移到新的位置
                    double curDx = NewX - NewPolygonObject.CalProxiNode().X;
                    double curDy = NewY - NewPolygonObject.CalProxiNode().Y;

                    //更新多边形点集的每一个点坐标
                    foreach (TriNode curPoint in NewPolygonObject.PointList)
                    {
                        curPoint.X += curDx;
                        curPoint.Y += curDy;
                    }

                    //更新节点坐标
                    NewNode.X += curDx;
                    NewNode.Y += curDy;
                    #endregion
                }
                #endregion
            }
            #endregion

            #region 两个居民地重要性相同
            else
            {
                double NewX = 0; double NewY = 0;

                #region 计算新建筑物的位置              
                NewX = (Po1.CalProxiNode().X + Po2.CalProxiNode().X) / 2;
                NewY = (Po1.CalProxiNode().Y + Po2.CalProxiNode().Y) / 2;
                #endregion

                #region 保留大面积的建筑物
                if (Po1.Area >= Po2.Area)
                {
                    NewPolygonObject = Po1;
                    NewNode = Node1;

                    NewPolygonObject.TyList.Add(Po2.ID);//存储哪些建筑物被当做一个建筑物表达
                    if (Po2.TyList.Count > 0)
                    {
                        NewPolygonObject.TyList.AddRange(Po2.TyList);
                    }
                }

                else if (Po1.Area <= Po2.Area)
                {
                    NewPolygonObject = Po2;
                    NewNode = Node2;

                    NewPolygonObject.TyList.Add(Po1.ID);//存储哪些建筑物被当做一个建筑物表达
                    if (Po1.TyList.Count > 0)
                    {
                        NewPolygonObject.TyList.AddRange(Po1.TyList);
                    }
                }
                #endregion             

                #region 将建筑物和节点移到新的位置
                double curDx = NewX - NewPolygonObject.CalProxiNode().X;
                double curDy = NewY - NewPolygonObject.CalProxiNode().Y;

                //更新多边形点集的每一个点坐标
                foreach (TriNode curPoint in NewPolygonObject.PointList)
                {
                    curPoint.X += curDx;
                    curPoint.Y += curDy;
                }

                //更新节点坐标
                NewNode.X += curDx;
                NewNode.Y += curDy;
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// 更新邻近图和地图；备注：只更新邻近图的pgNodeForBuilding和pgEdgeForBuilding
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="pg"></param>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <param name="NewPo"></param>
        void UpdataMapAndPg(SMap Map, ProxiGraph pg, PolygonObject Po1, PolygonObject Po2, PolygonObject NewPo,ProxiNode Node1,ProxiNode Node2,ProxiNode NewNode)
        {
            #region 更新地图
            Map.PolygonList.Remove(Po1);
            Map.PolygonList.Remove(Po2);
            Map.PolygonList.Add(NewPo);
            #endregion

            #region 更新邻近图
            //更新邻近边
            for (int i = pg.PgforBuildingEdgesList.Count - 1; i > -1; i--)
            {
                ProxiEdge Pe = pg.PgforBuildingEdgesList[i];

                if ((Pe.Node1 == Node1 && Pe.Node2 == Node2) || (Pe.Node1 == Node2 && Pe.Node2 == Node1))
                {
                    pg.PgforBuildingEdgesList.Remove(Pe);
                }

                else if (Pe.Node1 == Node1||Pe.Node1==Node2)
                {
                    Pe.Node1 = NewNode;
                }

                else if (Pe.Node2 == Node1 || Pe.Node2 == Node2)
                {
                    Pe.Node2 = NewNode;
                }
            }

            //更新节点
            pg.PgforBuildingNodesList.Remove(Node1);
            pg.PgforBuildingNodesList.Remove(Node2);
            pg.PgforBuildingNodesList.Add(NewNode);

            pg.DeleteRepeatedEdge(pg.PgforBuildingEdgesList);
            #endregion

            #region 更新邻近图中邻近边的最短距离
            for (int i = pg.PgforBuildingEdgesList.Count - 1; i > -1; i--)
            {
                ProxiEdge Pe = pg.PgforBuildingEdgesList[i];

                PolygonObject nPo1 = Map.GetObjectbyID(Pe.Node1.TagID, Pe.Node1.FeatureType) as PolygonObject;
                PolygonObject nPo2 = Map.GetObjectbyID(Pe.Node2.TagID, Pe.Node2.FeatureType) as PolygonObject;

                if (nPo1 == null || nPo2 == null)
                {
                    pg.PgforBuildingEdgesList.Remove(Pe);
                }

                else
                {
                    IPolygon pPolygon1 = this.ObjectConvert(nPo1);
                    IPolygon pPolygon2 = this.ObjectConvert(nPo2);

                    IProximityOperator IPo = pPolygon1 as IProximityOperator;
                    double NearDistance = IPo.ReturnDistance(pPolygon2 as IGeometry);
                    Pe.NearestDistance = NearDistance;
                }
            }
            #endregion
        }

        /// <summary>
        /// 更新Pattern中的建筑物
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <param name="NewPo"></param>
        /// <param name="Node1"></param>
        /// <param name="Node2"></param>
        /// <param name="NewNode"></param>
        void UpdataPattern(PolygonObject Po1, PolygonObject Po2, PolygonObject NewPo,SMap Map,ProxiGraph pg)
        {
            int PatternID=-1;
            int EdgeType = this.PatternEdge(Po1, Po2, out PatternID);
            int PatternID1 = -1; int PatternID2 = -1;
            bool PatternLabel1 = this.PatternBuildingAndID(Po1,out PatternID1);
            bool PatternLabel2 = this.PatternBuildingAndID(Po2,out PatternID2);

            #region Pattern内的一条边
            if (EdgeType == 1 && PatternID != -1)
            {
                PatternDic[PatternID].PatternObjects.Remove(Po1);
                PatternDic[PatternID].PatternObjects.Remove(Po2);
                PatternDic[PatternID].PatternObjects.Add(NewPo);

                //若Pattern中建筑物较少，则该Pattern已经不能被保持，移除该Pattern
                if (PatternDic[PatternID].PatternObjects.Count < 3)
                {
                    PatternDic.Remove(PatternID);
                }
            }
            #endregion

            #region Pattern外的一条边，但是这条边连接两个Pattern
            if (EdgeType == 2 && PatternLabel1 && PatternLabel2)
            {
                #region 根据重要性对Pattern进行更新[更新的方法是删除其中一个Pattern]
                if (this.PatternDic[PatternID1].PatternObjects.Count > this.PatternDic[PatternID2].PatternObjects.Count)
                {
                    PatternDic.Remove(PatternID2);
                }

                else if (this.PatternDic[PatternID1].PatternObjects.Count < this.PatternDic[PatternID2].PatternObjects.Count)
                {
                    PatternDic.Remove(PatternID1);
                }

                else
                {
                    double AreaSum1 = 0; double AreaSum2 = 0;

                    for (int i = 0; i < this.PatternDic[PatternID1].PatternObjects.Count; i++)
                    {
                        AreaSum1 = AreaSum1 + this.PatternDic[PatternID1].PatternObjects[i].Area;
                    }

                    for (int i = 0; i < this.PatternDic[PatternID2].PatternObjects.Count; i++)
                    {
                        AreaSum2 = AreaSum2 + this.PatternDic[PatternID2].PatternObjects[i].Area;
                    }

                    if (AreaSum1 > AreaSum2)
                    {
                        PatternDic.Remove(PatternID2);         
                    }

                    else
                    {
                        PatternDic.Remove(PatternID1);
                    }
                }
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// polygon转换成polygonobject
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <returns></returns>
        public PolygonObject PolygonConvert(IPolygon pPolygon)
        {
            int ppID = 0;//（polygonobject自己的编号，应该无用）
            //int pID = 0;//重心编号（应该无用，故没用编号）
            List<TriNode> trilist = new List<TriNode>();
            //Polygon的点集
            IPointCollection pointSet = pPolygon as IPointCollection;
            int count = pointSet.PointCount;
            double curX;
            double curY;
            //ArcGIS中，多边形的首尾点重复存储
            for (int i = 0; i < count; i++)
            {
                curX = pointSet.get_Point(i).X;
                curY = pointSet.get_Point(i).Y;
                //初始化每个点对象
                TriNode tPoint = new TriNode(curX, curY, ppID, 1);
                trilist.Add(tPoint);
            }
            //生成自己写的多边形
            PolygonObject mPolygonObject = new PolygonObject(ppID, trilist);

            return mPolygonObject;
        }

        /// <summary>
        /// 将polygonobject转换成polygon
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
    }
}
