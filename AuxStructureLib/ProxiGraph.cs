using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using AuxStructureLib;
using System.IO;
using System.Windows.Forms;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Controls;


namespace AuxStructureLib
{
    /// <summary>
    /// 邻近图
    /// </summary>
    public class ProxiGraph
    {
        public List<ProxiEdge> NNGforBuilding = new List<ProxiEdge>();

        //基于约束探测到的pattern边
        public List<ProxiEdge> PgforBuildingPatternList = new List<ProxiEdge>();

        //AlphaShape的边
        public List<ProxiEdge> AlphaShapeEdge = new List<ProxiEdge>();

        //表示LinearPattern
        public List<ProxiEdge> LinearEdges = new List<ProxiEdge>();

        //refinement pattern边
        public List<ProxiEdge> PgforRefineBuildingPatternList = new List<ProxiEdge>();

        //refinement pattern边，pattern中边建筑物是同一类型
        public List<ProxiEdge> PgforRefineSimilarBuildingPatternList = new List<ProxiEdge>();

        //只包含建筑物的邻近图
        public List<ProxiNode> PgforBuildingNodesList = new List<ProxiNode>();
        public List<ProxiEdge> PgforBuildingEdgesList = new List<ProxiEdge>();

        //去掉长度较长边的邻近图
        public List<ProxiNode> PgwithoutLongEdgesNodesList = new List<ProxiNode>();
        public List<ProxiEdge> PgwithouLongEdgesEdgesList = new List<ProxiEdge>();

        //去掉穿过V图的邻近图
        public List<ProxiNode> PgwithoutAcrossEdgesNodesList = new List<ProxiNode>();
        public List<ProxiEdge> PgwithoutAcorssEdgesEdgesList = new List<ProxiEdge>();

        //RNG(重心距离)
        public List<ProxiNode> RNGBuildingNodesListGravityDistance = new List<ProxiNode>();
        public List<ProxiEdge> RNGBuildingEdgesListGravityDistance = new List<ProxiEdge>();

        //RNG(最短距离)
        public List<ProxiNode> RNGBuildingNodesListShortestDistance = new List<ProxiNode>();
        public List<ProxiEdge> RNGBuildingEdgesListShortestDistance = new List<ProxiEdge>();

        //MST（重心距离）
        public List<ProxiNode> MSTBuildingNodesListGravityDistance = new List<ProxiNode>();
        public List<ProxiEdge> MSTBuildingEdgesListGravityDistance = new List<ProxiEdge>();

        //MST（最短距离）
        public List<ProxiNode> MSTBuildingNodesListShortestDistance = new List<ProxiNode>();
        public List<ProxiEdge> MSTBuildingEdgesListShortestDistance = new List<ProxiEdge>();

        //GG(重心距离)
        public List<ProxiNode> GGBuildingNodesListGravityDistance = new List<ProxiNode>();
        public List<ProxiEdge> GGBuildingEdgesListGravityDistance = new List<ProxiEdge>();

        //GG（最短距离）
        public List<ProxiNode> GGBuildingNodesListShortestDistance = new List<ProxiNode>();
        public List<ProxiEdge> GGBuildingEdgesListShortestDistance = new List<ProxiEdge>();

        //ForKG
        public List<ProxiNode> KGNodesList = new List<ProxiNode>();
        public List<ProxiEdge> KGEdgesList = new List<ProxiEdge>();
        /// <summary>
        /// 点列表
        /// </summary>
        public List<ProxiNode> NodeList = null;
        /// <summary>
        /// 边列表
        /// </summary>
        public List<ProxiEdge> EdgeList = null;
        /// <summary>
        /// 父亲
        /// </summary>
        public ProxiGraph ParentGraph = null;
        /// <summary>
        /// 孩子
        /// </summary>
        public List<ProxiGraph> SubGraphs = null;
        /// <summary>
        /// 多变形的个数字段
        /// </summary>
        private int polygonCount = -1;
        /// <summary>
        /// 多边形个数属性
        /// </summary>
        public int PolygonCount
        {
            get
            {
                if (this.polygonCount != -1)
                {
                    return this.polygonCount;
                }
                else
                {
                    int count = 0;
                    if (this.NodeList == null || this.NodeList.Count == 0)
                        return -1;
                    foreach (ProxiNode node in this.NodeList)
                    {
                        if (node.FeatureType == FeatureType.PolygonType)
                            count++;
                    }
                    this.polygonCount = count;
                    return this.polygonCount;
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ProxiGraph()
        {
            NodeList = new List<ProxiNode>();
            EdgeList = new List<ProxiEdge>();
        }

        /// <summary>
        /// 创建结点列表
        /// </summary>
        /// <param name="map">地图</param>
        private void CreateNodes(SMap map)
        {
            int nID = 0;
            //点
            if (map.PointList != null)
            {
                foreach (PointObject point in map.PointList)
                {
                    ProxiNode curNode = point.CalProxiNode();
                    curNode.ID = nID;
                    this.NodeList.Add(curNode);
                    nID++;
                }
            }
            //线
            if (map.PolylineList != null)
            {
                foreach (PolylineObject pline in map.PolylineList)
                {
                    ProxiNode curNode = pline.CalProxiNode();
                    curNode.ID = nID;
                    this.NodeList.Add(curNode);
                    nID++;
                }
            }
            //面
            if (map.PolygonList != null)
            {
                foreach (PolygonObject polygon in map.PolygonList)
                {
                    ProxiNode curNode = polygon.CalProxiNode();
                    curNode.ID = nID;
                    this.NodeList.Add(curNode);
                    nID++;
                }
            }
        }

        /// <summary>
        /// 创建边
        /// </summary>
        /// <param name="skeleton">骨架线</param>
        private void CreateEdges(Skeleton skeleton)
        {
            if (skeleton == null || skeleton.Skeleton_ArcList == null || skeleton.Skeleton_ArcList.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;

            ProxiNode node1 = null;
            ProxiNode node2 = null;

            ProxiEdge curEdge = null;

            // int eID = 0;

            foreach (Skeleton_Arc curArc in skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {

                    curTagID = curArc.LeftMapObj.ID;
                    curType = curArc.LeftMapObj.FeatureType;
                    node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    curTagID = curArc.RightMapObj.ID;
                    curType = curArc.RightMapObj.FeatureType;
                    node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    curEdge = new ProxiEdge(curArc.ID, node1, node2);
                    this.EdgeList.Add(curEdge);
                    node1.EdgeList.Add(curEdge);
                    node2.EdgeList.Add(curEdge);
                    curEdge.NearestEdge = curArc.NearestEdge;
                    curEdge.Weight = curArc.AveDistance;
                    curEdge.Ske_Arc = curArc;
                }
            }

        }

        /// <summary>
        /// 创建结点列表
        /// </summary>
        /// <param name="map">地图</param>
        private void CreateNodesforPointandPolygon(SMap map)
        {
            int nID = 0;
            //点
            if (map.PointList != null)
            {
                foreach (PointObject point in map.PointList)
                {
                    ProxiNode curNode = point.CalProxiNode();
                    curNode.ID = nID;
                    this.NodeList.Add(curNode);
                    nID++;
                }
            }
            ////线
            //if (map.PolylineList != null)
            //{
            //    foreach (PolylineObject pline in map.PolylineList)
            //    {
            //        ProxiNode curNode = pline.CalProxiNode();
            //        curNode.ID = nID;
            //        this.NodeList.Add(curNode);
            //        nID++;
            //    }
            //}
            //面
            if (map.PolygonList != null)
            {
                foreach (PolygonObject polygon in map.PolygonList)
                {
                    ProxiNode curNode = polygon.CalProxiNode();
                    curNode.ID = nID;
                    this.NodeList.Add(curNode);
                    nID++;
                }
            }
        }

        /// <summary>
        /// 创建边
        /// </summary>
        /// <param name="skeleton">骨架线</param>
        private void CreateEdgesforPointandPolygon(Skeleton skeleton)
        {
            if (skeleton == null || skeleton.Skeleton_ArcList == null || skeleton.Skeleton_ArcList.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;

            ProxiNode node1 = null;
            ProxiNode node2 = null;

            ProxiEdge curEdge = null;

            // int eID = 0;

            foreach (Skeleton_Arc curArc in skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {
                    if (curArc.LeftMapObj.FeatureType != FeatureType.PolylineType && curArc.RightMapObj.FeatureType != FeatureType.PolylineType)
                    {
                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        curEdge = new ProxiEdge(curArc.ID, node1, node2);
                        this.EdgeList.Add(curEdge);
                        node1.EdgeList.Add(curEdge);
                        node2.EdgeList.Add(curEdge);
                        curEdge.NearestEdge = curArc.NearestEdge;
                        curEdge.NearestDistance = curEdge.NearestEdge.NearestDistance;
                        curEdge.Weight = curArc.AveDistance;
                        curEdge.Ske_Arc = curArc;
                    }
                    //eID++;
                }
            }

        }

        /// <summary>
        /// 创建边
        /// </summary>
        /// <param name="skeleton">冲突</param>
        private void CreateEdges(List<Conflict> conflicts)
        {
            if (conflicts == null || conflicts.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;
            ProxiNode node1 = null;
            ProxiNode node2 = null;

            ProxiEdge curEdge = null;

            // int eID = 0;

            foreach (Conflict curConflict in conflicts)
            {
                if (curConflict.Obj1 != null && curConflict.Obj2 != null)
                {
                    if (curConflict.Obj1.ToString() == @"AuxStructureLib.SDS_PolylineObj")
                    {
                        SDS_PolylineObj curl = curConflict.Obj1 as SDS_PolylineObj;
                        curTagID = curl.ID;
                        curType = FeatureType.PolylineType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    }
                    else if (curConflict.Obj1.ToString() == @"AuxStructureLib.SDS_PolygonO")
                    {
                        SDS_PolygonO curO = curConflict.Obj1 as SDS_PolygonO;
                        curTagID = curO.ID;
                        curType = FeatureType.PolygonType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    }
                    if (curConflict.Obj2.ToString() == @"AuxStructureLib.SDS_PolylineObj")
                    {
                        SDS_PolylineObj curl = curConflict.Obj2 as SDS_PolylineObj;
                        curTagID = curl.ID;
                        curType = FeatureType.PolylineType;
                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    }
                    else if (curConflict.Obj2.ToString() == @"AuxStructureLib.SDS_PolygonO")
                    {
                        SDS_PolygonO curO = curConflict.Obj2 as SDS_PolygonO;
                        curTagID = curO.ID;
                        curType = FeatureType.PolygonType;
                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                    }

                    curEdge = new ProxiEdge(-1, node1, node2);
                    this.EdgeList.Add(curEdge);
                    node1.EdgeList.Add(curEdge);
                    node2.EdgeList.Add(curEdge);
                    //eID++;
                }
            }
        }

        /// <summary>
        /// 从骨架线构造邻近图
        /// </summary>
        public void CreateProxiGraphfrmSkeleton(SMap map, Skeleton skeleton)
        {
            CreateNodes(map);
            CreateEdges(skeleton);
        }

        /// <summary>
        /// 从骨架线构造邻近图
        /// </summary>
        public void CreateProxiGraphfrmSkeletonBuildings_Perpendicular(SMap map, Skeleton skeleton)
        {
            this.CreateNodesforPointandPolygon(map);
            this.CreateEdgesforPointandPolygon(skeleton);
            CreateNodesandPerpendicular_EdgesforPolyline_(map, skeleton);
        }

        /// <summary>
        /// 从骨架线构造邻近图
        /// </summary>
        public void CreateProxiGraphfrmSkeletonBuildings(SMap map, Skeleton skeleton)
        {
            this.CreateNodesforPointandPolygon(map);
            this.CreateEdgesforPointandPolygon(skeleton);
            CreateNodesandEdgesforPolyline_LP(map, skeleton);
        }

        /// <summary>
        /// 从骨架线构造邻近图
        /// </summary>
        public void CreateProxiGraphfrmSkeletonForEnrichNetwork(SMap map, Skeleton skeleton)
        {
            this.CreateNodesforPointandPolygon(map);
            this.CreateEdgesforPointandPolygon(skeleton);
            this.CreateNodesandNearestLine2PolylineVertices(map, skeleton);
            this.RemoveSuperfluousEdges();
        }

        /// <summary>
        /// 删除邻近图中多余的边
        /// </summary>
        private void RemoveSuperfluousEdges()
        {
            List<ProxiEdge> edgeList = new List<ProxiEdge>();
            foreach (ProxiEdge curEdge in this.EdgeList)
            {
                if (!this.IsContainEdge(edgeList, curEdge))
                {
                    edgeList.Add(curEdge);
                }
            }
            this.EdgeList = edgeList;
        }

        /// <summary>
        /// 是否包含该边
        /// </summary>
        /// <returns></returns>
        private bool IsContainEdge(List<ProxiEdge> edgeList,ProxiEdge edge)
        {
            if (edgeList == null || edgeList.Count == 0)
                return false;
            foreach (ProxiEdge curEdge in edgeList)
            {
                if ((edge.Node1.ID == curEdge.Node1.ID && edge.Node2.ID == curEdge.Node2.ID) || (edge.Node2.ID == curEdge.Node1.ID && edge.Node1.ID == curEdge.Node2.ID))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 添加点与线、面与线的邻近边和线上的邻近点（仅仅加入与街道垂直的邻近边）
        /// </summary>
        /// <param name="map"></param>
        /// <param name="skeleton"></param>
        private void CreateNodesandEdgesforPolyline_LP(SMap map, Skeleton skeleton)
        {
            if (skeleton == null || skeleton.Skeleton_ArcList == null || skeleton.Skeleton_ArcList.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;

            ProxiNode node1 = null;
            ProxiNode node2 = null;
            bool isPerpendicular = false;

            ProxiEdge curEdge = null;

            int id = this.NodeList.Count;

            foreach (Skeleton_Arc curArc in skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {
                    if (curArc.LeftMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.RightMapObj.FeatureType == FeatureType.PointType || curArc.RightMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        Node node = ComFunLib.MinDisPoint2Polyline(node2, curline, out isPerpendicular);


                        node1 = new ProxiNode(node.X, node.Y, id, curArc.LeftMapObj.ID, FeatureType.PolylineType);
                        this.NodeList.Add(node1);
                        id++;


                        curEdge = new ProxiEdge(curArc.ID, node1, node2);
                        this.EdgeList.Add(curEdge);
                        node1.EdgeList.Add(curEdge);
                        node2.EdgeList.Add(curEdge);

                        curEdge.NearestEdge = curArc.NearestEdge;
                        curEdge.Weight = curArc.AveDistance;
                        curEdge.Ske_Arc = curArc;

                    }

                    else if (curArc.RightMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.LeftMapObj.FeatureType == FeatureType.PointType || curArc.LeftMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);

                        Node node = ComFunLib.MinDisPoint2Polyline(node1, curline, out isPerpendicular);

                        node2 = new ProxiNode(node.X, node.Y, id, curArc.RightMapObj.ID, FeatureType.PolylineType);
                        this.NodeList.Add(node2);
                        id++;


                        curEdge = new ProxiEdge(curArc.ID, node1, node2);
                        this.EdgeList.Add(curEdge);
                        node1.EdgeList.Add(curEdge);
                        node2.EdgeList.Add(curEdge);

                        curEdge.NearestEdge = curArc.NearestEdge;
                        curEdge.Weight = curArc.AveDistance;
                        curEdge.Ske_Arc = curArc;

                    }
                    //eID++;
                }
            }
        }

        /// <summary>
        /// 添加点与线、面与线的邻近边和线上的邻近点（仅仅加入与街道垂直的邻近边）
        /// </summary>
        /// <param name="map"></param>
        /// <param name="skeleton"></param>
        private void CreateNodesandPerpendicular_EdgesforPolyline_(SMap map, Skeleton skeleton)
        {
            if (skeleton == null || skeleton.Skeleton_ArcList == null || skeleton.Skeleton_ArcList.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;

            ProxiNode node1 = null;
            ProxiNode node2 = null;
            bool isPerpendicular = false;

            ProxiEdge curEdge = null;

            int id = this.NodeList.Count;

            foreach (Skeleton_Arc curArc in skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {
                    if (curArc.LeftMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.RightMapObj.FeatureType == FeatureType.PointType || curArc.RightMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        Node node = ComFunLib.MinDisPoint2Polyline(node2, curline, out isPerpendicular);

                        if (isPerpendicular)//仅仅加入与街道垂直的邻近边
                        {

                            node1 = new ProxiNode(node.X, node.Y, id, curArc.LeftMapObj.ID, FeatureType.PolylineType);
                            this.NodeList.Add(node1);
                            id++;


                            curEdge = new ProxiEdge(curArc.ID, node1, node2);
                            this.EdgeList.Add(curEdge);
                            node1.EdgeList.Add(curEdge);
                            node2.EdgeList.Add(curEdge);

                            curEdge.NearestEdge = curArc.NearestEdge;
                            curEdge.Weight = curArc.AveDistance;
                            curEdge.Ske_Arc = curArc;
                        }
                    }

                    else if (curArc.RightMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.LeftMapObj.FeatureType == FeatureType.PointType || curArc.LeftMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);

                        Node node = ComFunLib.MinDisPoint2Polyline(node1, curline, out isPerpendicular);
                        if (isPerpendicular)//仅仅加入与街道垂直的邻近边
                        {
                            node2 = new ProxiNode(node.X, node.Y, id, curArc.RightMapObj.ID, FeatureType.PolylineType);
                            this.NodeList.Add(node2);
                            id++;


                            curEdge = new ProxiEdge(curArc.ID, node1, node2);
                            this.EdgeList.Add(curEdge);
                            node1.EdgeList.Add(curEdge);
                            node2.EdgeList.Add(curEdge);

                            curEdge.NearestEdge = curArc.NearestEdge;
                            curEdge.Weight = curArc.AveDistance;
                            curEdge.Ske_Arc = curArc;
                        }
                    }
                    //eID++;
                }
            }
        }

        /// <summary>
        /// 添加点与线、面与线的邻近边和线上的邻近点（仅仅加入与街道垂直的邻近边）
        /// </summary>
        /// <param name="map"></param>
        /// <param name="skeleton"></param>
        private void CreateNodesandNearestLine2PolylineVertices(SMap map, Skeleton skeleton)
        {
            if (skeleton == null || skeleton.Skeleton_ArcList == null || skeleton.Skeleton_ArcList.Count == 0)
            {
                return;
            }
            int curTagID = -1;
            FeatureType curType = FeatureType.Unknown;

            ProxiNode node1 = null;
            ProxiNode node2 = null;
            ProxiEdge curEdge = null;

            int id = this.NodeList.Count;

            foreach (Skeleton_Arc curArc in skeleton.Skeleton_ArcList)
            {
                if (curArc.LeftMapObj != null && curArc.RightMapObj != null)
                {
                    if (curArc.LeftMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.RightMapObj.FeatureType == FeatureType.PointType || curArc.RightMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.LeftMapObj.ID;
                        curType = curArc.LeftMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;

                        node2 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        Node node = ComFunLib.MinDisPoint2PolylineVertices(node2, curline);
                        ProxiNode exitNode = GetContainNode(this.NodeList, node.X, node.Y);
                        if (exitNode == null)
                        {

                            node1 = new ProxiNode(node.X, node.Y, id, curArc.LeftMapObj.ID, FeatureType.PolylineType);
                            node1.SomeValue = node.ID;
                            this.NodeList.Add(node1);
                            id++;
                        }
                        else
                        {
                            node1 = exitNode;
                        }
                        curEdge = new ProxiEdge(curArc.ID, node1, node2);
                        this.EdgeList.Add(curEdge);
                        node1.EdgeList.Add(curEdge);
                        node2.EdgeList.Add(curEdge);

                        curEdge.NearestEdge = curArc.NearestEdge;
                        curEdge.Weight = curArc.AveDistance;
                        curEdge.Ske_Arc = curArc;
            
                    }

                    else if (curArc.RightMapObj.FeatureType == FeatureType.PolylineType &&
                        (curArc.LeftMapObj.FeatureType == FeatureType.PointType || curArc.LeftMapObj.FeatureType == FeatureType.PolygonType))
                    {
                        curTagID = curArc.RightMapObj.ID;
                        curType = curArc.RightMapObj.FeatureType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;

                        curTagID = curArc.LeftMapObj.ID;

                        curType = curArc.LeftMapObj.FeatureType;
                        node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        Node node = ComFunLib.MinDisPoint2PolylineVertices(node1, curline);
                        ProxiNode exitNode = GetContainNode(this.NodeList, node.X, node.Y);
                        if (exitNode == null)
                        {

                            node2 = new ProxiNode(node.X, node.Y, id, curArc.RightMapObj.ID, FeatureType.PolylineType);
                            node2.SomeValue = node.ID;
                            this.NodeList.Add(node2);
                            id++;
                        }
                        else
                        {
                            node2 = exitNode;
                        }
                        curEdge = new ProxiEdge(curArc.ID, node1, node2);
                        this.EdgeList.Add(curEdge);
                        node1.EdgeList.Add(curEdge);
                        node2.EdgeList.Add(curEdge);

                        curEdge.NearestEdge = curArc.NearestEdge;
                        curEdge.Weight = curArc.AveDistance;
                        curEdge.Ske_Arc = curArc;
                    }
                    //eID++;
                }
            }
        }

        /// <summary>
        /// 判断当期的顶点是否已经在关联点集合中
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns></returns>
        private ProxiNode GetContainNode(List<ProxiNode> nodeList, double x, double y)
        {

            if (nodeList == null || nodeList.Count == 0)
            {
                return null;
            }
            foreach (ProxiNode curNode in nodeList)
            {
                // int id = curNode.ID;
                ProxiNode curV = curNode;

                if (Math.Abs((1 - curV.X / x)) <= 0.000001f && Math.Abs((1 - curV.Y / y)) <= 0.000001f)
                {
                    return curV;
                }
            }
            return null;
        }

        /// <summary>
        /// 从骨架线构造邻近图
        /// </summary>
        public void CreateProxiGraphfrmConflicts(SMap map, List<Conflict> conflicts)
        {
            CreateNodes(map);
            CreateEdges(conflicts);
        }

        /// <summary>
        /// 根据索引获取结点
        /// </summary>
        /// <param name="tagID"></param>
        /// <returns></returns>
        public ProxiNode GetNodebyTagID(int tagID)
        {
            foreach (ProxiNode curNode in this.NodeList)
            {
                if (curNode.TagID == tagID)
                {
                    return curNode;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据索引获取结点
        /// </summary>
        /// <param name="tagID"></param>
        /// <returns></returns>
        public ProxiNode GetNodebyTagIDandType(int tagID, FeatureType type)
        {
            foreach (ProxiNode curNode in this.NodeList)
            {
                if (curNode.TagID == tagID && type == curNode.FeatureType)
                {
                    return curNode;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据索引获取结点
        /// </summary>
        /// <param name="tagID"></param>
        /// <returns></returns>
        public ProxiNode GetNodebyID(int ID)
        {
            foreach (ProxiNode curNode in this.NodeList)
            {
                if (curNode.ID == ID)
                {
                    return curNode;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据两端点的索引号获取边
        /// </summary>
        /// <param name="index1"></param>
        /// <param name="index2"></param>
        /// <returns></returns>
        public ProxiEdge GetEdgebyNodeIndexs(int index1, int index2)
        {
            foreach (ProxiEdge edge in this.EdgeList)
            {
                if ((edge.Node1.ID == index1 && edge.Node2.ID == index2) || (edge.Node1.ID == index2 && edge.Node2.ID == index1))
                    return edge;
            }
            return null;
        }

        /// <summary>
        /// 获取所有与node相关联的边
        /// </summary>
        /// <param name="node"></param>
        /// <returns>边序列</returns>
        public List<ProxiEdge> GetEdgesbyNode(ProxiNode node)
        {
            int index = node.ID;
            List<ProxiEdge> resEdgeList = new List<ProxiEdge>();
            foreach (ProxiEdge edge in this.EdgeList)
            {
                if (edge.Node1.ID == index || edge.Node2.ID == index)
                    resEdgeList.Add(edge);
            }
            if (resEdgeList.Count > 0) return resEdgeList;
            else return null;
        }

        /// <summary>
        /// 将邻近图写入SHP文件
        /// </summary>
        public void WriteProxiGraph2Shp(string filePath, string fileName, ISpatialReference pSpatialReference,List<ProxiNode> PnList,List<ProxiEdge> PeList)
        {
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);

            ProxiNode.Create_WriteProxiNodes2Shp(filePath, @"Node_" + fileName,PnList, pSpatialReference);
            ProxiEdge.Create_WriteEdge2Shp(filePath, @"Edges_" + fileName, PeList, pSpatialReference);
            if (PeList != null && PeList.Count > 0)
            {
                if (PeList[0].NearestEdge != null)
                {
                    ProxiEdge.Create_WriteNearestDis2Shp(filePath, @"Nearest_" + fileName, PeList, pSpatialReference);
                }
            }
        }

        /// <summary>
        /// 拷贝邻近图-   //求吸引力-2014-3-20所用
        /// </summary>
        /// <returns></returns>
        public ProxiGraph Copy()
        {
            ProxiGraph pg = new ProxiGraph();
            foreach (ProxiNode node in this.NodeList)
            {
                ProxiNode newNode = new ProxiNode(node.X, node.Y, node.ID, node.TagID, node.FeatureType);
                pg.NodeList.Add(newNode);

            }

            foreach (ProxiEdge edge in this.EdgeList)
            {
                ProxiEdge newedge = new ProxiEdge(edge.ID, this.GetNodebyID(edge.Node1.ID), this.GetNodebyID(edge.Node1.ID));
                pg.EdgeList.Add(newedge);
            }
            return pg;

        }

        /// <summary>
        /// 就算边的权重
        /// </summary>
        public void CalWeightbyNearestDistance()
        {
            foreach (ProxiEdge edge in this.EdgeList)
            {
                edge.Weight = edge.NearestEdge.NearestDistance;
            }
        }

        /// <summary>
        /// 从最小外接矩形中获取相似性信息
        /// </summary>
        public void GetSimilarityInfofrmSMBR(List<SMBR> SMBRList, SMap map)
        {
            foreach (ProxiEdge edge in this.EdgeList)
            {
                int tagID1 = edge.Node1.TagID;
                int tagID2 = edge.Node2.TagID;
                FeatureType type1 = edge.Node1.FeatureType;
                FeatureType type2 = edge.Node2.FeatureType;
                SMBR smbr1 = SMBR.GetSMBR(tagID1, type1, SMBRList);
                SMBR smbr2 = SMBR.GetSMBR(tagID2, type2, SMBRList);


                if (smbr1 == null || smbr2 == null)
                    continue;

                if (type1 == FeatureType.PolygonType && type2 == FeatureType.PolygonType)
                {
                    PolygonObject obj1 = PolygonObject.GetPPbyID(map.PolygonList, tagID1);
                    PolygonObject obj2 = PolygonObject.GetPPbyID(map.PolygonList, tagID2);
                    double A1 = smbr1.Direct1;
                    double A2 = smbr2.Direct1;
                    int EN1 = obj1.PointList.Count;
                    int EN2 = obj2.PointList.Count;
                    double Area1 = obj1.Area;
                    double Area2 = obj2.Area;
                    double Peri1 = obj1.Perimeter;
                    double Peri2 = obj2.Perimeter;

                    if (EN1 > EN2)
                    {
                        int temp;
                        temp = EN1;
                        EN1 = EN2;
                        EN2 = temp;
                    }
                    if (Area1 > Area2)
                    {
                        double temp;
                        temp = Area1;
                        Area1 = Area2;
                        Area2 = temp;
                    }
                    if (Peri1 > Peri2)
                    {
                        double temp;
                        temp = Peri1;
                        Peri1 = Peri2;
                        Peri2 = temp;
                    }

                    double a = Math.Abs(A1 - A2);
                    if (a > Math.PI / 2)
                    {
                        a = Math.PI - a;
                    }
                    edge.W_A_Simi = 2 * a / Math.PI;

                    edge.W_Area_Simi = Area1 / Area2;
                    edge.W_EdgeN_Simi = EN1 * 1.0 / EN2;
                    edge.W_Peri_Simi = Peri1 / Peri2;

                    edge.CalWeight();//重新计算全重
                }

                else if (type1 == FeatureType.PolylineType && type2 == FeatureType.PolylineType)
                {
                    //待续
                }
                //线线之间相似性，讨论线面之间，
                else if (type1 == FeatureType.PolygonType && type2 == FeatureType.PolylineType)
                {
                    //待续
                }

                else if (type1 == FeatureType.PolylineType && type2 == FeatureType.PolygonType)
                {
                    //待续
                }
            }
        }

        /// <summary>
        /// 用分组信息对邻近图进行优化-04-19
        /// </summary>
        /// <param name="groups"></param>
        public void OptimizeGraphbyBuildingGroups(List<GroupofMapObject> groups,SMap map)
        {
            if (groups == null || groups.Count == 0)
                return;
            foreach (GroupofMapObject curGroup in groups)
            {
                if (curGroup.ListofObjects == null || curGroup.ListofObjects.Count == 0)
                    continue;
                //获取图中对应的结点
                List<ProxiNode> curNodeList = new List<ProxiNode>();
                int tagID = curGroup.ID;
                foreach (MapObject curO in curGroup.ListofObjects)
                {
                    PolygonObject curB = curO as PolygonObject;
                    int curTagId = curB.ID;
                    FeatureType curType = curB.FeatureType;

                    ProxiNode curNode = this.GetNodebyTagIDandType(curTagId, curType);
                    curNodeList.Add(curNode);
                }

                List<ProxiEdge> curIntraEdgeList = new List<ProxiEdge>();
                List<ProxiEdge> curInterEdgeList = new List<ProxiEdge>();
                List<ProxiNode> curNeighbourNodeList = new List<ProxiNode>();//与组内邻近但在组外的结点
                List<ProxiNode> curNeighbourBoundaryNodeList = new List<ProxiNode>();
                foreach (ProxiEdge curEdge in this.EdgeList)
                {
                    ProxiNode sN = curEdge.Node1;
                    ProxiNode eN = curEdge.Node2;
                    bool f1 = this.IsContainNode(curNodeList, sN);
                    bool f2 = this.IsContainNode(curNodeList, eN);
                    if (f1 == true && f2 == true)
                    {
                        curIntraEdgeList.Add(curEdge);
                    }
                    else if (f1 == false && f2 == false)
                    {

                    }
                    else
                    {
                        curInterEdgeList.Add(curEdge);
                        if (f1 == true && f2 == false)
                        {
                            if (!this.IsContainNode(curNeighbourNodeList, eN))
                            {
                                curNeighbourNodeList.Add(eN);
                            }
                            else
                            {
                                if (eN.FeatureType == FeatureType.PolylineType)
                                    curNeighbourBoundaryNodeList.Add(eN);
                            }
                        }
                        else
                        {
                            if (!this.IsContainNode(curNeighbourNodeList, sN))
                            {
                                curNeighbourNodeList.Add(sN);
                            }
                            else
                            {
                                if (sN.FeatureType == FeatureType.PolylineType)
                                    curNeighbourBoundaryNodeList.Add(sN);
                            }
                        }
                    }

                }


                ProxiNode groupNode = AuxStructureLib.ComFunLib.CalGroupCenterPoint(curNodeList);
                groupNode.TagID = tagID;
                groupNode.FeatureType = FeatureType.Group;
                this.NodeList.Add(groupNode);//加入结点

                foreach (ProxiNode curNeighbouringNode in curNeighbourNodeList)
                {
                    if (curNeighbouringNode.FeatureType == FeatureType.PolygonType||curNeighbouringNode.FeatureType == FeatureType.PointType||curNeighbouringNode.FeatureType==FeatureType.Group)
                    {
                        ProxiEdge newEdge = new ProxiEdge(-1, groupNode, curNeighbouringNode);
                        this.EdgeList.Add(newEdge);
                    }
                    else if (curNeighbouringNode.FeatureType == FeatureType.PolylineType)
                    {
                        int curTagID = curNeighbouringNode.TagID;
                        FeatureType curType = FeatureType.PolylineType;
                        // node1 = ProxiNode.GetProxiNodebyTagIDandFType(this.NodeList, curTagID, curType);
                        PolylineObject curline = map.GetObjectbyID(curTagID, FeatureType.PolylineType) as PolylineObject;
                        bool isPerpendicular = true;
                        Node newNode = ComFunLib.MinDisPoint2Polyline(groupNode, curline, out isPerpendicular);
                        ProxiNode nodeonLine = new ProxiNode(newNode.X, newNode.Y, -1, curTagID, FeatureType.PolylineType);
                        this.NodeList.Add(nodeonLine);
                        ProxiEdge newEdge = new ProxiEdge(-1, groupNode, nodeonLine);
                        this.EdgeList.Add(newEdge);

                        this.NodeList.Remove(curNeighbouringNode);
                    }
                }

                foreach (ProxiEdge edge in curIntraEdgeList)
                {
                    this.EdgeList.Remove(edge);

                }
                foreach (ProxiEdge edge in curInterEdgeList)
                {
                    this.EdgeList.Remove(edge);

                }
                foreach (ProxiNode node in curNodeList)
                {
                    this.NodeList.Remove(node);
                }
                foreach (ProxiNode node in curNeighbourBoundaryNodeList)
                {
                    this.NodeList.Remove(node);
                }
                int nodeID=0;
                int edgeID=0;

                foreach (ProxiNode node in this.NodeList)
                {
                    node.ID = nodeID;
                    nodeID++;
                }
                
                 foreach (ProxiEdge edge in this.EdgeList)
                {
                    edge.ID = edgeID;
                    edgeID++;

                }
            }
        }


        /// <summary>
        /// 化简边
        /// </summary>
        /// <param name="MaxDistance"></param>
        private void SimplifyPG(double MaxDistance)
        {
            List<ProxiEdge> delEdgeList = new List<ProxiEdge>();
            foreach(ProxiEdge curEdge in this.EdgeList)
            {
                delEdgeList.Add(curEdge);
            }
            foreach (ProxiEdge delcurEdge in delEdgeList)
            {
                this.EdgeList.Remove(delcurEdge);
            }
        }
        
        /// <summary>
        /// 判断结点集合中是否含有结点-用于分组优化函数;OptimizeGraphbyBuildingGroups
        /// </summary>
        /// <param name="nodeList">结点集合</param>
        /// <param name="node">结点</param>
        /// <returns></returns>
        private bool IsContainNode(List<ProxiNode> nodeList, ProxiNode node)
        {
            if (nodeList == null || nodeList.Count == 0)
                return false;

            foreach (ProxiNode curNode in nodeList)
            {
                if (curNode.TagID==node.TagID&&curNode.FeatureType==node.FeatureType)//线上的结点
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 创建只包含建筑物的邻近图
        /// </summary>
        public void CreatePgForBuildings(List<ProxiNode> PnList,List<ProxiEdge> PeList)
        {
            foreach (ProxiNode Pn in PnList)
            {
                if (Pn.FeatureType != FeatureType.PolylineType)
                {
                    PgforBuildingNodesList.Add(Pn);
                }
            }

            foreach(ProxiEdge Pe in PeList)
            {
                if (!(Pe.Node1.FeatureType == FeatureType.PolylineType || Pe.Node2.FeatureType == FeatureType.PolylineType))
                {
                    PgforBuildingEdgesList.Add(Pe);
                }
            }

            this.KGNodesList = PgforBuildingNodesList;
            this.KGEdgesList = PgforBuildingEdgesList;
        }
        
        /// <summary>
        /// 将大于一定长度的边从当前邻近图中删除
        /// </summary>
        /// <param name="PnList"></param> 邻近图点集
        /// <param name="PeList"></param> 邻近图边集
        /// <param name="LengthThrehold"></param> 长度阈值
        public void CreatePgwithoutlongEdges(List<ProxiNode> PnList,List<ProxiEdge> PeList,double LengthThrehold)
        {
            foreach (ProxiEdge Pe in PeList)
            {
                if (Pe.NearestEdge.NearestDistance < LengthThrehold)
                {
                    PgwithouLongEdgesEdgesList.Add(Pe);
                }
            }

            this.PgwithoutLongEdgesNodesList = PnList;
        }

        /// <summary>
        /// 将大于一定长度的边从当前邻近图中删除
        /// </summary>
        /// <param name="PnList"></param> 邻近图点集
        /// <param name="PeList"></param> 邻近图边集
        /// <param name="LengthThrehold"></param> 长度阈值
        public void DeletelongEdges(List<ProxiEdge> PeList, double LengthThrehold)
        {
            for (int i = PeList.Count - 1; i >= 0; i--)
            {
                if (PeList[i].NearestDistance > LengthThrehold)
                {
                    PeList.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 将穿过v图的邻近图边删除
        /// </summary>
        /// <param name="PnList"></param> 邻近图点集
        /// <param name="PeList"></param> 邻近图边集
        /// <param name="vd"></param> 给定的v图集合
        public void CreatePgwithoutAcorssEdges(List<ProxiNode> PnList, List<ProxiEdge> PeList, VoronoiDiagram vd)
        {
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;

            foreach (ProxiEdge Pe in PeList)
            {
                int intersectCount = 0;

                #region 将邻近边转换为ipolyline
                IPolyline pPolyline = new PolylineClass();
                IPoint FromPoint = new PointClass(); IPoint ToPoint = new PointClass();
                FromPoint.PutCoords(Pe.Node1.X, Pe.Node1.Y);
                ToPoint.PutCoords(Pe.Node2.X, Pe.Node2.Y);
                pPolyline.FromPoint = FromPoint; pPolyline.ToPoint = ToPoint;

                ITopologicalOperator pTopo=pPolyline as ITopologicalOperator;
                #endregion

                foreach (VoronoiPolygon vp in vd.VorPolygonList)
                {
                    IPolygon pPolygon = ObjectConvert(vp);

                    IGeometry pGeo=pTopo.Intersect(pPolygon,esriGeometryDimension.esriGeometry1Dimension);
                    if (!pGeo.IsEmpty)
                    {
                        intersectCount = intersectCount + 1;
                    }
                }

                if (intersectCount < 3)
                {
                    PgwithoutAcorssEdgesEdgesList.Add(Pe);
                }
            }

            PgwithoutAcrossEdgesNodesList = PnList;
        }

        /// <summary>
        /// 创建建筑物的NNG（最短距离）
        /// </summary>
        /// <param name="PnList"></param>
        /// <param name="PeList"></param>
        public void CreateNNGForBuildingShortestDistance()
        {
            for (int i = 0; i < this.MSTBuildingNodesListShortestDistance.Count; i++)
            {
                double ShortestLength = 1000000;
                ProxiEdge ShortestLine = this.MSTBuildingEdgesListShortestDistance[0];

                for (int j = 0; j < this.MSTBuildingEdgesListShortestDistance.Count; j++)
                {
                    if (this.MSTBuildingNodesListShortestDistance[i].X == this.MSTBuildingEdgesListShortestDistance[j].Node1.X ||
                        this.MSTBuildingNodesListShortestDistance[i].X == this.MSTBuildingEdgesListShortestDistance[j].Node2.X)
                    {
                        if (this.MSTBuildingEdgesListShortestDistance[j].NearestEdge.NearestDistance < ShortestLength)
                        {
                            ShortestLength = this.MSTBuildingEdgesListShortestDistance[j].NearestEdge.NearestDistance;
                            ShortestLine = this.MSTBuildingEdgesListShortestDistance[j];
                        }
                    }
                }

                this.NNGforBuilding.Add(ShortestLine);
            }

            this.KGNodesList = this.PgforBuildingNodesList;
            this.KGEdgesList = this.NNGforBuilding;
        }

        /// <summary>
        /// 创建点集的MST(重心距离)
        /// 普里姆算法
        /// </summary>
        public void CreateMSTForBuildingsGravityDistance(List<ProxiNode> PnList,List<ProxiEdge> PeList)
        {
            #region 矩阵初始化
            double[,] matrixGraph = new double[PnList.Count, PnList.Count];

            for (int i = 0; i < PnList.Count; i++)
            {
                for (int j = 0; j < PnList.Count; j++)
                {
                    matrixGraph[i, j] = -1;
                }
            }
            #endregion

            #region 矩阵赋值
            for (int i = 0; i < PnList.Count; i++)
            {
                ProxiNode Point1 = PnList[i];

                for (int j = 0; j < PeList.Count; j++)
                {
                    ProxiEdge Edge1 = PeList[j];

                    ProxiNode pPoint1 = Edge1.Node1;
                    ProxiNode pPoint2 = Edge1.Node2;
                    if (Point1.X == pPoint1.X && Point1.Y == pPoint1.Y)
                    {
                        for (int m = 0; m < PnList.Count; m++)
                        {
                            ProxiNode Point2 = PnList[m];

                            if (Point2.X == pPoint2.X && Point2.Y == pPoint2.Y)
                            {
                                matrixGraph[i, m] = matrixGraph[m, i] = (double)DistanceCompute(Point1, Point2);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < PnList.Count; i++)
            {
                for (int j = 0; j < PnList.Count; j++)
                {
                    if (matrixGraph[i, j] == -1)
                    {
                        matrixGraph[i, j] = matrixGraph[j, i] = 100000;
                    }
                }
            }
            #endregion

            #region MST计算
            IArray LabelArray = new ArrayClass();//MST点集
            IArray fLabelArray = new ArrayClass();
            List<List<int>> EdgesGroup = new List<List<int>>();//MST边集

            for (int F = 0; F < PnList.Count; F++)
            {
                fLabelArray.Add(F);
            }

            int LabelFirst = 0;//任意添加一个节点
            LabelArray.Add(LabelFirst);
            //int x = 0;
            int LabelArrayNum;
            do
            {
                LabelArrayNum = LabelArray.Count;
                int fLabelArrayNum = fLabelArray.Count;
                double MinDist = 100001;
                List<int> Edge = new List<int>();

                int EdgeLabel2 = -1;
                int EdgeLabel1 = -1;
                int Label = -1;

                for (int i = 0; i < LabelArrayNum; i++)
                {
                    int p1 = (int)LabelArray.get_Element(i);

                    for (int j = 0; j < fLabelArrayNum; j++)
                    {
                        int p2 = (int)fLabelArray.get_Element(j);

                        if (matrixGraph[p1, p2] < MinDist)
                        {
                            MinDist = matrixGraph[p1, p2];
                            EdgeLabel2 = p2;
                            EdgeLabel1 = p1;
                            Label = j;
                        }
                    }
                }

                //x++;
      

                Edge.Add(EdgeLabel1);
                Edge.Add(EdgeLabel2);
                EdgesGroup.Add(Edge);

                fLabelArray.Remove(Label);
                LabelArray.Add(EdgeLabel2);


            } while (LabelArrayNum < PnList.Count);
            #endregion

            #region 生成MST的nodes和Edges
            int EdgesGroupNum = EdgesGroup.Count;

            for (int i = 0; i < EdgesGroupNum; i++)
            {
                int m, n;
                m = EdgesGroup[i][0];
                n = EdgesGroup[i][1];

                ProxiNode Pn1 = PnList[m];
                ProxiNode Pn2 = PnList[n];

                foreach (ProxiEdge Pe in PeList)
                {
                    if ((Pe.Node1.X == Pn1.X && Pe.Node2.X == Pn2.X) || (Pe.Node1.X == Pn2.X && Pe.Node2.X == Pn1.X))
                    {
                        this.MSTBuildingEdgesListGravityDistance.Add(Pe);
                        break;
                    }
                }

            }

            #region 去除MST中的重复边
            for (int i = 0; i < this.MSTBuildingEdgesListGravityDistance.Count; i++)
            {
                bool Lable = false;
                ProxiEdge Pe1 = MSTBuildingEdgesListGravityDistance[i];
                for (int j = i; j < this.MSTBuildingEdgesListGravityDistance.Count; j++)
                {
                    ProxiEdge Pe2 = MSTBuildingEdgesListGravityDistance[j];
                    if ((Pe1.Node1.X == Pe2.Node1.X && Pe1.Node2.X == Pe2.Node2.X) || (Pe1.Node1.X == Pe2.Node2.X && Pe1.Node2.X == Pe1.Node2.X))
                    {
                        this.MSTBuildingEdgesListGravityDistance.Remove(Pe1);                       
                        Lable = true;
                        break;
                    }
                }

                if (Lable)
                {
                    break;
                }
            }
            #endregion

            this.MSTBuildingNodesListGravityDistance = PnList;
            #endregion
        }
        
        /// <summary>
        /// 创建点集的MST（最短距离）
        /// </summary>
        public void CreateMSTForBuildingsShortestDistance(List<ProxiNode> PnList,List<ProxiEdge> PeList)
        {
            #region 矩阵初始化
            double[,] matrixGraph = new double[PnList.Count, PnList.Count];

            for (int i = 0; i < PnList.Count; i++)
            {
                for (int j = 0; j < PnList.Count; j++)
                {
                    matrixGraph[i, j] = -1;
                }
            }
            #endregion

            #region 矩阵赋值
            for (int i = 0; i < PnList.Count; i++)
            {
                ProxiNode Point1 = PnList[i];

                for (int j = 0; j < PeList.Count; j++)
                {
                    ProxiEdge Edge1 = PeList[j];

                    ProxiNode pPoint1 = Edge1.Node1;
                    ProxiNode pPoint2 = Edge1.Node2;
                    if (Point1.X == pPoint1.X && Point1.Y == pPoint1.Y)
                    {
                        for (int m = 0; m < PnList.Count; m++)
                        {
                            ProxiNode Point2 = PnList[m];

                            if (Point2.X == pPoint2.X && Point2.Y == pPoint2.Y)
                            {
                                matrixGraph[i, m] = matrixGraph[m, i] = Edge1.NearestEdge.NearestDistance;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < PnList.Count; i++)
            {
                for (int j = 0; j < PnList.Count; j++)
                {
                    if (matrixGraph[i, j] == -1)
                    {
                        matrixGraph[i, j] = matrixGraph[j, i] = 100000;
                    }
                }
            }
            #endregion

            #region MST计算
            IArray LabelArray = new ArrayClass();//MST点集
            IArray fLabelArray = new ArrayClass();
            List<List<int>> EdgesGroup = new List<List<int>>();//MST边集

            for (int F = 0; F < PnList.Count; F++)
            {
                fLabelArray.Add(F);
            }

            int LabelFirst = 0;//任意添加一个节点
            LabelArray.Add(LabelFirst);
            //int x = 0;
            int LabelArrayNum;
            do
            {
                LabelArrayNum = LabelArray.Count;
                int fLabelArrayNum = fLabelArray.Count;
                double MinDist = 100001;
                List<int> Edge = new List<int>();

                int EdgeLabel2 = -1;
                int EdgeLabel1 = -1;
                int Label = -1;

                for (int i = 0; i < LabelArrayNum; i++)
                {
                    int p1 = (int)LabelArray.get_Element(i);

                    for (int j = 0; j < fLabelArrayNum; j++)
                    {
                        int p2 = (int)fLabelArray.get_Element(j);

                        if (matrixGraph[p1, p2] < MinDist)
                        {
                            MinDist = matrixGraph[p1, p2];
                            EdgeLabel2 = p2;
                            EdgeLabel1 = p1;
                            Label = j;
                        }
                    }
                }


                //x++;
                Edge.Add(EdgeLabel1);
                Edge.Add(EdgeLabel2);
                EdgesGroup.Add(Edge);

                fLabelArray.Remove(Label);
                LabelArray.Add(EdgeLabel2);

            } while (LabelArrayNum < PnList.Count);
            #endregion

            #region 生成MST的nodes和Edges
            int EdgesGroupNum = EdgesGroup.Count;

            for (int i = 0; i < EdgesGroupNum; i++)
            {
                int m, n;
                m = EdgesGroup[i][0];
                n = EdgesGroup[i][1];

                ProxiNode Pn1 = PnList[m];
                ProxiNode Pn2 = PnList[n];

                foreach (ProxiEdge Pe in PeList)
                {
                    if ((Pe.Node1.X == Pn1.X && Pe.Node2.X == Pn2.X) || (Pe.Node1.X == Pn2.X && Pe.Node2.X == Pn1.X))
                    {
                        this.MSTBuildingEdgesListShortestDistance.Add(Pe);
                        break;
                    }
                }

            }

            #region 去除MST中的重复边
            for (int i = 0; i < this.MSTBuildingEdgesListShortestDistance.Count; i++)
            {
                bool Lable = false;
                ProxiEdge Pe1 = MSTBuildingEdgesListShortestDistance[i];
                for (int j = i; j < this.MSTBuildingEdgesListShortestDistance.Count; j++)
                {
                    ProxiEdge Pe2 = MSTBuildingEdgesListShortestDistance[j];
                    if ((Pe1.Node1.X == Pe2.Node1.X && Pe1.Node2.X == Pe2.Node2.X) || (Pe1.Node1.X == Pe2.Node2.X && Pe1.Node2.X == Pe1.Node2.X))
                    {
                        this.MSTBuildingEdgesListShortestDistance.Remove(Pe1);
                        Lable = true;
                        break;
                    }
                }

                if (Lable)
                {
                    break;
                }
            }
            #endregion

            this.MSTBuildingNodesListShortestDistance = PnList;

            this.KGNodesList = this.MSTBuildingNodesListShortestDistance;
            this.KGEdgesList = this.MSTBuildingEdgesListShortestDistance;
            #endregion
        }

        /// <summary>
        /// 创建点集的RNG图（重心距离）
        ///即对于三角网的边，其它任意点与节点连接的形成的线的最大值不大于三角网边长度，那么这条边会被保留
        ///即对于三角形中的边来说，该边只要不是最长边，则保留
        ///说明：对于任意一条边，找到对应的三角形，如果是最长边，则删除。
        /// </summary>
        public void CreateRNGForBuildingsGravityDistance(List<ProxiNode> PnList,List<ProxiEdge> PeList)
        {
            #region RNG计算
            for (int i = 0; i < PeList.Count; i++)
            {
                ProxiEdge TinLine = PeList[i];
                double tDistance = DistanceCompute(TinLine.Node1, TinLine.Node2);

                ProxiNode Point1 = TinLine.Node1;
                ProxiNode Point2 = TinLine.Node2;
                int mark = 1;

                for (int j = 0; j < PnList.Count; j++)
                {
                    ProxiNode Point = PnList[j];
                    double Distance1 = DistanceCompute(Point1, Point);
                    double Distance2 = DistanceCompute(Point2, Point);
                    double fDistance = 100000;

                    if (Distance1 != 0 || Distance2 != 0)
                    {
                        if (Distance1 > Distance2) { fDistance = Distance1; }
                        else { fDistance = Distance2; }
                    }

                    if (fDistance < tDistance)
                    {
                        mark = 2;
                    }
                }

                if (mark == 1)
                {
                    this.RNGBuildingEdgesListGravityDistance.Add(TinLine);
                }
            }
            #endregion

            this.RNGBuildingNodesListGravityDistance = PnList;
        }

        /// <summary>
        /// 创建点集的RNG图（最短距离）
        ///RNG计算(找到邻近图中每一个三角形，删除三角形中的最长边)
        ///说明：对于任意一条边，找到对应的三角形；如果是最长边，则删除（如果是一条边，保留；如果是两条边，若是最长边，删除）
        /// </summary>
        public void CreateRNGForBuildingsShortestDistance(List<ProxiNode> PnList,List<ProxiEdge> PeList)
        {
            #region 找到潜在RNG对应的每条边，判断是否是邻近图中三角形对应的最长边，如果是，删除
            for (int i = 0; i<PeList.Count; i++)
            {
                ProxiEdge TinLine = PeList[i];

                #region 获取边的属性，即节点和对应的最短距离
                ProxiNode Pn1 = TinLine.Node1;
                ProxiNode Pn2 = TinLine.Node2;
                double Distance = TinLine.NearestEdge.NearestDistance;
                #endregion

                #region 判断边是否为三角形对应的最长边
                bool SingleEdgeLabel=false;
                for (int j = 0; j < PnList.Count; j++)
                {
                    bool Label1 = false; bool Label2 = false;
                    double Distance1 = 0; double Distance2 = 0;
                    ProxiNode mPn = PnList[j];
                    if (mPn.X != Pn1.X && mPn.X != Pn2.X)
                    {
                        for (int m = 0; m < PeList.Count; m++)
                        {                          
                            #region mPn与Pn1是否为边
                            if ((mPn.TagID == PeList[m].Node1.TagID && Pn1.TagID == PeList[m].Node2.TagID) || mPn.TagID == PeList[m].Node2.TagID && Pn1.TagID == PeList[m].Node1.TagID)
                            {
                                Label1 = true;
                                Distance1 = PeList[m].NearestEdge.NearestDistance;
                            }
                            #endregion

                            #region mPn与Pn2是否为边
                            if ((mPn.TagID == PeList[m].Node1.TagID && Pn2.TagID == PeList[m].Node2.TagID) || mPn.TagID == PeList[m].Node2.TagID && Pn2.TagID == PeList[m].Node1.TagID)
                            {
                                Label2 = true;
                                Distance2 = PeList[m].NearestEdge.NearestDistance;
                            }
                            #endregion
                        }
                    }

                    #region 存在三角形
                    if (Label1 && Label2)
                    {
                        SingleEdgeLabel = true;
                        if (Distance <= Distance1 || Distance <= Distance2)
                        {
                            this.RNGBuildingEdgesListShortestDistance.Add(PeList[i]);
                            break;
                        }
                    }
                    #endregion
                }

                if (!SingleEdgeLabel)
                {
                    this.RNGBuildingEdgesListShortestDistance.Add(PeList[i]);
                }
                #endregion
            }
            #endregion

            this.RNGBuildingNodesListShortestDistance = PnList;

            this.KGNodesList = this.RNGBuildingNodesListShortestDistance;
            this.KGEdgesList = this.RNGBuildingEdgesListShortestDistance;
        }

        /// <summary>
        /// 计算alphashape的边
        /// </summary>
        /// <param name="PnList"></param>
        /// <param name="PeList"></param>
        /// <param name="cdt"></param>
        /// <param name="Distance"></param>
        public void CreateAlphaShape(List<ProxiNode> PnList, List<ProxiEdge> PeList, double Distance)
        {
            List<GraphTriangle> TriangleList = this.GetTriangleForGraph(PnList, PeList);

            Dictionary<ProxiEdge, List<GraphTriangle>> TriangleDic = new Dictionary<ProxiEdge, List<GraphTriangle>>();//存储边及其邻接三角形
            List<ProxiEdge> LongerPeList = new List<ProxiEdge>();//存储较长的边界边

            #region 找到每条边对应的邻接三角形
            for (int i = 0; i < PeList.Count; i++)
            {
                List<GraphTriangle> EdgeTriangle = new List<GraphTriangle>();

                for (int j = 0; j < TriangleList.Count; j++)
                {
                    if (TriangleList[j].EdgeList.Contains(PeList[i]))
                    {
                        EdgeTriangle.Add(TriangleList[j]);
                    }
                }

                TriangleDic.Add(PeList[i], EdgeTriangle);
            }
            #endregion

            #region 找到长度大于给定长度的边界边
            for (int i = 0; i < PeList.Count; i++)
            {
                if (PeList[i].NearestEdge.NearestDistance > Distance && TriangleDic[PeList[i]].Count == 1)
                {
                    LongerPeList.Add(PeList[i]);
                }
            }
            #endregion

            #region 遍历删除边
            while (LongerPeList.Count > 0)
            {
                if (TriangleDic[LongerPeList[0]].Count == 1)
                {
                    GraphTriangle pGraphTriangle = TriangleDic[LongerPeList[0]][0];//找到给定边的邻接三角形T
                    pGraphTriangle.EdgeList.Remove(LongerPeList[0]);//找到另外两条边，并将它们的邻接三角形删除T

                    TriangleDic[pGraphTriangle.EdgeList[0]].Remove(pGraphTriangle);
                    TriangleDic[pGraphTriangle.EdgeList[1]].Remove(pGraphTriangle);

                    //将两条边中大于给定距离的边加入队列
                    if (pGraphTriangle.EdgeList[0].NearestEdge.NearestDistance > Distance && TriangleDic[pGraphTriangle.EdgeList[0]].Count == 1)
                    {
                        LongerPeList.Add(pGraphTriangle.EdgeList[0]);
                    }

                    if (pGraphTriangle.EdgeList[1].NearestEdge.NearestDistance > Distance && TriangleDic[pGraphTriangle.EdgeList[1]].Count == 1)
                    {
                        LongerPeList.Add(pGraphTriangle.EdgeList[1]);
                    }
                }

                LongerPeList.RemoveAt(0);
            }
            #endregion

            #region 找到当前列表中长度小于阈值的边界边
            for (int i = 0; i < PeList.Count; i++)
            {
                if (PeList[i].NearestEdge.NearestDistance < Distance && TriangleDic[PeList[i]].Count == 1)
                {
                    this.AlphaShapeEdge.Add(PeList[i]);
                }
            }
            #endregion
        }

        /// <summary>
        /// 邻近图的三角形结构
        /// </summary>
        public class GraphTriangle
        {
            public int ID;
            public List<ProxiEdge> EdgeList = new List<ProxiEdge>();
        }

        /// <summary>
        /// 求给定邻近图的三角形列表
        /// </summary>
        /// <param name="PnList"></param>
        /// <param name="PeList"></param>
        /// <returns></returns>
        public List<GraphTriangle> GetTriangleForGraph(List<ProxiNode> PnList, List<ProxiEdge> PeList)
        {
            List<ProxiEdge> PfList = new List<ProxiEdge>();
            for (int i = 0; i < PeList.Count; i++)
            {
                PfList.Add(PeList[i]);
            }

            List<GraphTriangle> GraphTriangleList = new List<GraphTriangle>();

            while (PfList.Count>0)
            {
                int TriangleId = 0;

                ProxiNode Pn1 = PfList[0].Node1;
                ProxiNode Pn2 = PfList[0].Node2;
                for (int j = 0; j < PnList.Count; j++)
                {
                    #region 判断是否是三角形的第三点
                    bool Label1 = false; bool Label2 = false;
                    ProxiEdge ProxiEdge1 = null; ProxiEdge ProxiEdge2 = null;
                    ProxiNode mPn = PnList[j];
                    if (mPn.X != Pn1.X && mPn.X != Pn2.X)
                    {
                        for (int m = 0; m < PfList.Count; m++)
                        {
                            #region mPn与Pn1是否为边
                            if ((mPn.TagID == PfList[m].Node1.TagID && Pn1.TagID == PfList[m].Node2.TagID) || mPn.TagID == PfList[m].Node2.TagID && Pn1.TagID == PfList[m].Node1.TagID)
                            {
                                Label1 = true;
                                ProxiEdge1 = PfList[m];
                            }
                            #endregion

                            #region mPn与Pn2是否为边
                            if ((mPn.TagID == PfList[m].Node1.TagID && Pn2.TagID == PfList[m].Node2.TagID) || mPn.TagID == PfList[m].Node2.TagID && Pn2.TagID == PfList[m].Node1.TagID)
                            {
                                Label2 = true;
                                ProxiEdge2 = PfList[m];
                            }
                            #endregion
                        }
                    }
                    #endregion

                    #region 若是第三点，添加该三角形
                    if (Label1 && Label2)
                    {
                        GraphTriangle pGraphTriangle = new GraphTriangle();
                        pGraphTriangle.ID = TriangleId;
                        TriangleId = TriangleId + 1;
                        pGraphTriangle.EdgeList.Add(PfList[0]);
                        pGraphTriangle.EdgeList.Add(ProxiEdge1);
                        pGraphTriangle.EdgeList.Add(ProxiEdge2);

                        GraphTriangleList.Add(pGraphTriangle);
                    }
                    #endregion
                }

                PfList.RemoveAt(0);
            }

            return GraphTriangleList;
        }

        /// <summary>
        /// 创建点集的GG图（最短距离）
        /// </summary> 即对于三角网的一条边，只要该边的平方小于任意与该边组成三角形的节点与边节点形成线长度的平方和，则该边是GG图的一条边
        /// <param name="PnList"></param> 创建GG的点集
        /// <param name="PeList"></param> 创建GG的边集
        public void CreateGGShortestDistance(List<ProxiNode> PnList,List<ProxiEdge> PeList)
        {
            //System.IO.FileStream fs = new System.IO.FileStream("C:\\Users\\Administrator\\Desktop\\协同移位（整体）实验\\11.25\\test.txt", FileMode.Create);
            //StreamWriter sw = new StreamWriter(fs);

            //MessageBox.Show(PeList.Count.ToString());

            #region GG计算
            for (int i = 0; i < PeList.Count; i++)
            {
                ProxiEdge TinLine = PeList[i];

                #region 获取边的属性，即节点和对应的最短距离
                ProxiNode Pn1 = TinLine.Node1;
                ProxiNode Pn2 = TinLine.Node2;
                double Distance = TinLine.NearestEdge.NearestDistance;
                #endregion

                int mark = 1;

                #region 判断节点是否为边对应的三角形中的节点
                for (int t = 0; t < PnList.Count; t++)
                {
                    ProxiNode mPn = PnList[t];
                    List<double> DistanceList = new List<double>();
                    double Distancep = 0;
                    for (int m = 0; m < PeList.Count; m++)
                    {
                        if (m != i)
                        {
                            ProxiEdge Pe1 = PeList[m];
                            ProxiNode pPn1 = Pe1.Node1;
                            ProxiNode pPn2 = Pe1.Node2;
                            double tDistance = Pe1.NearestEdge.NearestDistance;

                            if ((pPn1.X == Pn1.X && pPn2.X == mPn.X) || (pPn2.X == Pn1.X && pPn1.X == mPn.X) || (pPn1.X == Pn2.X && pPn2.X == mPn.X) || (pPn2.X == Pn2.X && Pn1.X == mPn.X))
                            {
                                Distancep = Distancep + tDistance * tDistance;
                                DistanceList.Add(tDistance);
                            }
                        }
                    }

                    //sw.Write(DistanceList.Count.ToString());

                    //只考虑有两条边的情况
                    if (DistanceList.Count == 2)
                    {
                        double d = Distance * Distance;
                        if (d > Distancep)
                        {
                            mark = 2;
                        }
                    }

                }
                #endregion
                //MessageBox.Show(mark.ToString());
                if (mark == 1)
                {
                    this.GGBuildingEdgesListShortestDistance.Add(TinLine);
                }
            }
            #endregion

            this.GGBuildingNodesListShortestDistance = PnList;
        }

        /// <summary>
        /// GG图计算（重心距离）
        /// </summary>
        /// <param name="PnList"></param> 点集
        /// <param name="PeList"></param> 边集
        public void CreateGGGravityDistance(List<ProxiNode> PnList, List<ProxiEdge> PeList)
        {
            #region GG计算
            for (int i = 0; i < PeList.Count; i++)
            {
                ProxiEdge TinLine = PeList[i];

                #region 获取边的属性，即节点和对应的最短距离
                ProxiNode Pn1 = TinLine.Node1;
                ProxiNode Pn2 = TinLine.Node2;
                double Distance = DistanceCompute(Pn1, Pn2);
                #endregion

                int mark = 1;

                #region 判断节点是否为边对应的三角形中的节点
                for (int t = 0; t < PnList.Count; t++)
                {                                   
                    ProxiNode Point = PnList[t];
                    double Distance1 = DistanceCompute(Pn1, Point);
                    double Distance2 = DistanceCompute(Pn2, Point);

                    if (Distance * Distance > (Distance1 * Distance1 + Distance2 * Distance2))
                    {
                        mark = 2;
                    }
                }
                #endregion

                if (mark == 1)
                {
                    this.GGBuildingEdgesListGravityDistance.Add(TinLine);
                }
            }
            #endregion

            this.GGBuildingNodesListGravityDistance = PnList;
        }

        /// <summary>
        /// 计算两点间的距离
        /// </summary>
        /// <param name="pPoint1"></param> 点
        /// <param name="pPoint2"></param> 点
        /// <returns></returns>
        public double DistanceCompute(ProxiNode pPoint1, ProxiNode pPoint2)
        {
            double distance;
            distance = Math.Sqrt((pPoint2.Y - pPoint1.Y) * (pPoint2.Y - pPoint1.Y) + (pPoint2.X - pPoint1.X) * (pPoint2.X - pPoint1.X));
            return distance;
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
            if (Vp!=null)
            {
                for (int i = 0; i <Vp.PointSet.Count; i++)
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

        /// <summary>
        /// 删除邻近图中的重复边（最短距离较大的边被删除）另外，目前最多只处理三条重复边的情况
        /// </summary>
        public void DeleteRepeatedEdge(List<ProxiEdge> PeList)
        {
            #region 删除一遍
            //for (int i = 0; i < PeList.Count; i++)
            //{
            //    ProxiEdge Pe1 = PeList[i];
            //    for (int j = 0; j < PeList.Count; j++)
            //    {
            //        if (j != i)
            //        {
            //            ProxiEdge Pe2 = PeList[j];

            //            if ((Pe1.Node1.X == Pe2.Node1.X && Pe1.Node2.X == Pe2.Node2.X) ||
            //                (Pe1.Node1.X == Pe2.Node2.X && Pe1.Node2.X == Pe2.Node1.X))
            //            {
            //                if (Pe1.NearestEdge.NearestDistance > Pe2.NearestEdge.NearestDistance)
            //                {
            //                    PeList.RemoveAt(i);
            //                    break;
            //                }

            //                else
            //                {
            //                    PeList.RemoveAt(j);
            //                    break;
            //                }
            //            }
            //        }
            //    }
            //}
            #endregion

            #region 删除两遍
            //for (int i = 0; i < PeList.Count; i++)
            //{
            //    ProxiEdge Pe1 = PeList[i];
            //    for (int j = 0; j < PeList.Count; j++)
            //    {
            //        if (j != i)
            //        {
            //            ProxiEdge Pe2 = PeList[j];

            //            if ((Pe1.Node1.X == Pe2.Node1.X && Pe1.Node2.X == Pe2.Node2.X) ||
            //                 (Pe1.Node1.X == Pe2.Node2.X && Pe1.Node2.X == Pe2.Node1.X))
            //            {
            //                if (Pe1.NearestEdge.NearestDistance > Pe2.NearestEdge.NearestDistance)
            //                {
            //                    PeList.RemoveAt(i);
            //                    break;
            //                }

            //                else
            //                {
            //                    PeList.RemoveAt(j);
            //                    break;
            //                }
            //            }
            //        }
            //    }
            //}
            #endregion

            #region 将PeList添加入Dictionary中
            Dictionary<ProxiEdge, bool> EdgeDic = new Dictionary<ProxiEdge, bool>();

            for (int i = 0; i < PeList.Count; i++)
            {
                EdgeDic.Add(PeList[i], false);
            }
            #endregion

            #region 将重复的边标记为删除
            for (int i = 0; i < PeList.Count; i++)
            {
                ProxiEdge Pe1 = PeList[i];
                if (!EdgeDic[Pe1])
                {
                    for (int j = 0; j < PeList.Count; j++)
                    {
                        if (j != i)
                        {
                            ProxiEdge Pe2 = PeList[j];

                            if (!EdgeDic[Pe2])
                            {
                                if ((Pe1.Node1.TagID == Pe2.Node1.TagID && Pe1.Node2.TagID == Pe2.Node2.TagID) ||
                                    (Pe1.Node1.TagID== Pe2.Node2.TagID && Pe1.Node2.TagID == Pe2.Node1.TagID))
                                {
                                    EdgeDic[Pe2] = true;
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region 将删除的边在PeList中删除
            foreach (KeyValuePair<ProxiEdge, bool> kvp in EdgeDic)
            {
                if (kvp.Value)
                {
                    PeList.Remove(kvp.Key);
                }
            }
            #endregion
        }

        /// <summary>
        /// 直线模式探测 删除不符合条件的边
        /// </summary>
        /// <param name="PeList"></param>边集合
        /// <param name="PnList"></param>点集合
        /// <param name="DistanceConstraint"></param>距离约束
        /// <param name="OrientationConstraint"></param>方向约束
        public void LinearPatternDetected1(List<ProxiEdge> PeList,List<ProxiNode> PnList,double DistanceConstraint,double OrientationConstraint,double shortestDis)
        {
            for (int i = 0; i < PeList.Count; i++)
            {
                this.PgforBuildingPatternList.Add(PeList[i]);
            }

            for (int i = 0; i < PeList.Count; i++)
            {
                ProxiEdge Pe = PeList[i];
                List<ProxiEdge> EdgeList1 = ReturnEdgeList(PeList, Pe);
                bool Label = true;

                for (int j = 0; j < EdgeList1.Count; j++)
                {
                    bool DistanceAccept = DistanceConstrain(Pe, EdgeList1[j], DistanceConstraint,shortestDis);
                    bool OrientationAccept = OrientationConstrain(Pe, EdgeList1[j], OrientationConstraint);

                    if (DistanceAccept && OrientationAccept)
                    {
                        Label = false;
                    }
                }

                if (Label)
                {
                    this.PgforBuildingPatternList.Remove(Pe);
                }
            }
        }

        /// <summary>
        /// 直线模式探测 根据关系确定边
        /// </summary>
        /// <param name="PeList"></param>边集合
        /// <param name="PnList"></param>点集合
        /// <param name="DistanceConstraint"></param>距离约束
        /// <param name="OrientationConstraint"></param>方向约束
        public List<List<ProxiEdge>> LinearPatternDetected2(List<ProxiEdge> PeList, List<ProxiNode> PnList, double DistanceConstraint, double OrientationConstraint, double shortestDis)
        {
            List<List<ProxiEdge>> PatternEdgeList = new List<List<ProxiEdge>>();

            #region 标识所有边未被访问
            Dictionary<ProxiEdge, bool> EdgeVisitLabel = new Dictionary<ProxiEdge, bool>();//标识每条边是否被访问
            for (int i = 0; i < PeList.Count; i++)
            {
                EdgeVisitLabel.Add(PeList[i], false);
            }
            #endregion

            #region pattern detection
            for (int i = 0; i < PeList.Count; i++)
            {
                if (!EdgeVisitLabel[PeList[i]])
                {
                    List<ProxiEdge> PatternEdge = new List<ProxiEdge>();

                    ProxiEdge OriginalEdge = PeList[i];//探测的初始边
                    PatternEdge.Add(OriginalEdge);
                    EdgeVisitLabel[OriginalEdge] = true;

                    ProxiEdge VisitedEdge = OriginalEdge;//当前被访问的边
                    ProxiNode VisitedNode1 = VisitedEdge.Node1; ProxiNode VisitedNode2 = VisitedEdge.Node2;//当前被访问的节点

                    #region 沿node1方向做pattern detected
                    bool Node1DetectLabel = false;
                    do
                    {
                        Node1DetectLabel = false;
                        List<ProxiEdge> EdgeList1 = ReturnEdgeList(PeList, VisitedNode1);
                        EdgeList1.Remove(VisitedEdge);//移除当前访问的边

                        for (int j = 0; j < EdgeList1.Count; j++)
                        {
                            if (!EdgeVisitLabel[EdgeList1[j]])
                            {
                                bool DistanceAccept = DistanceConstrain(VisitedEdge, EdgeList1[j], DistanceConstraint, shortestDis);
                                bool OrientationAccept = OrientationConstrain(VisitedEdge, EdgeList1[j], OrientationConstraint);

                                if (DistanceAccept && OrientationAccept)
                                {
                                    VisitedEdge = EdgeList1[j];//把新加入的边作为访问边
                                    PatternEdge.Add(VisitedEdge);
                                    EdgeVisitLabel[VisitedEdge] = true;

                                    #region 把新加入的点作为访问点
                                    if (VisitedNode1 == EdgeList1[j].Node1)
                                    {
                                        VisitedNode1 = EdgeList1[j].Node2;
                                    }

                                    else if (VisitedNode1 == EdgeList1[j].Node2)
                                    {
                                        VisitedNode1 = EdgeList1[j].Node1;
                                    }
                                    #endregion

                                    Node1DetectLabel = true;
                                    break;
                                }
                            }
                        }
                    } while (Node1DetectLabel);
                    #endregion

                    #region 以node2为起点做pattern detected
                    VisitedEdge = OriginalEdge;
                    bool Node2DetectLabel = false;
                    do
                    {
                        Node2DetectLabel = false;                       
                        List<ProxiEdge> EdgeList2 = ReturnEdgeList(PeList, VisitedNode2);
                        EdgeList2.Remove(VisitedEdge);//移除当前访问的边

                        for (int j = 0; j < EdgeList2.Count; j++)
                        {
                            if (!EdgeVisitLabel[EdgeList2[j]])
                            {
                                bool DistanceAccept = DistanceConstrain(VisitedEdge, EdgeList2[j], DistanceConstraint, shortestDis);
                                bool OrientationAccept = OrientationConstrain(VisitedEdge, EdgeList2[j], OrientationConstraint);

                                if (DistanceAccept && OrientationAccept)
                                {
                                    VisitedEdge = EdgeList2[j];//把新加入的边作为访问边
                                    PatternEdge.Add(VisitedEdge);
                                    EdgeVisitLabel[VisitedEdge] = true;

                                    #region 把新加入的点作为访问点
                                    if (VisitedNode2 == EdgeList2[j].Node1)
                                    {
                                        VisitedNode2 = EdgeList2[j].Node2;
                                    }

                                    else if (VisitedNode2 == EdgeList2[j].Node2)
                                    {
                                        VisitedNode2 = EdgeList2[j].Node1;
                                    }
                                    #endregion

                                    Node2DetectLabel = true;
                                    break;
                                }
                            }
                        }
                    } while (Node2DetectLabel);
                    #endregion

                    if (PatternEdge.Count > 1)
                    {
                        PatternEdgeList.Add(PatternEdge);
                    }
                }
            }
            #endregion

            #region 获得边的集合
            for (int i = 0; i < PatternEdgeList.Count; i++)
            {
                for (int j = 0; j < PatternEdgeList[i].Count; j++)
                {
                    this.PgforBuildingPatternList.Add(PatternEdgeList[i][j]);
                }
            }
            #endregion

            return PatternEdgeList;
        }

        /// 直线模式探测 根据关系确定边
        /// </summary>
        /// <param name="PeList"></param>边集合
        /// <param name="PnList"></param>点集合
        /// <param name="DistanceConstraint"></param>距离约束
        /// <param name="AngleConstraint"></param>方向约束
        /// <param name="OrientationConstraint"></param>方向约束
        /// <param name="OrientationConstraint"></param>方向约束
        /// <param name="OrientationConstraint"></param>方向约束
        public List<List<ProxiEdge>> LinearPatternDetected3(SMap map,List<ProxiEdge> PeList, List<ProxiNode> PnList, double DistanceConstraint, double AngleConstraint, double shortestDis, double SizeConstraint, double OriConstraint, double ShapeConstraint)
        {
            List<List<ProxiEdge>> PatternEdgeList = new List<List<ProxiEdge>>();

            #region pattern detection
            for (int i = 0; i < PeList.Count; i++)
            {

                #region Node2为起点
                List<ProxiEdge> PatternEdge = new List<ProxiEdge>();//Node2为起点的Pattern
                ProxiEdge OriginalEdge = PeList[i];//探测的初始边
                PatternEdge.Add(OriginalEdge);

                ProxiEdge VisitedEdge = OriginalEdge;//当前被访问的边
                ProxiNode VisitedNode1 = VisitedEdge.Node1; ProxiNode VisitedNode2 = VisitedEdge.Node2;//当前被访问的节点
                #endregion

                #region Node1为起点
                List<ProxiEdge> tPatternEdge = new List<ProxiEdge>();//Node2为起点的Pattern
                tPatternEdge.Add(OriginalEdge);
                ProxiEdge tVisitedEdge = OriginalEdge;//当前被访问的边
                ProxiNode tVisitedNode1 = VisitedEdge.Node1; ProxiNode tVisitedNode2 = VisitedEdge.Node2;//当前被访问的节点
                #endregion

                #region 判断两个建筑物是否相似
                PolygonObject Po1 = map.GetObjectbyID(VisitedNode1.TagID, FeatureType.PolygonType) as PolygonObject;
                PolygonObject Po2 = map.GetObjectbyID(VisitedNode2.TagID, FeatureType.PolygonType) as PolygonObject;
                bool SimLabel = this.Sim(Po1, Po2, SizeConstraint, ShapeConstraint, OriConstraint);
                #endregion

                #region 沿Node2方向探索
                if (SimLabel)
                {
                    VisitedEdge = OriginalEdge;
                    bool Node2DetectLabel = false;
                    do
                    {
                        Node2DetectLabel = false;
                        List<ProxiEdge> EdgeList2 = ReturnEdgeList(PeList, VisitedNode2);
                        EdgeList2.Remove(VisitedEdge);//移除当前访问的边

                        for (int j = 0; j < EdgeList2.Count; j++)
                        {
                            ProxiNode VisitedNode3 = EdgeList2[j].Node1; ProxiNode VisitedNode4 = EdgeList2[j].Node2;//当前被访问的节点

                            PolygonObject Po3 = map.GetObjectbyID(VisitedNode3.TagID, FeatureType.PolygonType) as PolygonObject;
                            PolygonObject Po4 = map.GetObjectbyID(VisitedNode4.TagID, FeatureType.PolygonType) as PolygonObject;

                            bool SimLabelP = this.Sim(Po3, Po4, SizeConstraint, ShapeConstraint, OriConstraint);
                            bool DistanceAccept = DistanceConstrain(VisitedEdge, EdgeList2[j], DistanceConstraint, shortestDis);
                            bool OrientationAccept = OrientationConstrain(VisitedEdge, EdgeList2[j], AngleConstraint);

                            if (DistanceAccept && OrientationAccept && SimLabelP)
                            {
                                VisitedEdge = EdgeList2[j];
                                if (!PatternEdge.Contains(VisitedEdge))
                                {
                                    //把新加入的边作为访问边
                                    PatternEdge.Add(VisitedEdge);

                                    #region 把新加入的点作为访问点
                                    if (VisitedNode2 == EdgeList2[j].Node1)
                                    {
                                        VisitedNode2 = EdgeList2[j].Node2;
                                    }

                                    else if (VisitedNode2 == EdgeList2[j].Node2)
                                    {
                                        VisitedNode2 = EdgeList2[j].Node1;
                                    }
                                    #endregion

                                    Node2DetectLabel = true;
                                    break;
                                }
                            }
                        }
                    } while (Node2DetectLabel);
                }
                #endregion

                #region 沿Node1方向探索
                if (SimLabel)
                {
                    tVisitedEdge = OriginalEdge;
                    bool Node2DetectLabel = false;
                    do
                    {
                        Node2DetectLabel = false;
                        List<ProxiEdge> EdgeList2 = ReturnEdgeList(PeList, tVisitedNode1);
                        EdgeList2.Remove(tVisitedEdge);//移除当前访问的边

                        for (int j = 0; j < EdgeList2.Count; j++)
                        {
                            ProxiNode VisitedNode3 = EdgeList2[j].Node1; ProxiNode VisitedNode4 = EdgeList2[j].Node2;//当前被访问的节点

                            PolygonObject Po3 = map.GetObjectbyID(VisitedNode3.TagID, FeatureType.PolygonType) as PolygonObject;
                            PolygonObject Po4 = map.GetObjectbyID(VisitedNode4.TagID, FeatureType.PolygonType) as PolygonObject;

                            bool SimLabelP = this.Sim(Po3, Po4, SizeConstraint, ShapeConstraint, OriConstraint);
                            bool DistanceAccept = DistanceConstrain(tVisitedEdge, EdgeList2[j], DistanceConstraint, shortestDis);
                            bool OrientationAccept = OrientationConstrain(tVisitedEdge, EdgeList2[j], AngleConstraint);

                            if (DistanceAccept && OrientationAccept && SimLabelP)
                            {
                                tVisitedEdge = EdgeList2[j];
                                if (!PatternEdge.Contains(tVisitedEdge))
                                {
                                    //把新加入的边作为访问边
                                    tPatternEdge.Add(tVisitedEdge);

                                    #region 把新加入的点作为访问点
                                    if (tVisitedNode1 == EdgeList2[j].Node1)
                                    {
                                        tVisitedNode1 = EdgeList2[j].Node2;
                                    }

                                    else if (tVisitedNode1 == EdgeList2[j].Node2)
                                    {
                                        tVisitedNode1 = EdgeList2[j].Node1;
                                    }
                                    #endregion

                                    Node2DetectLabel = true;
                                    break;
                                }
                            }
                        }
                    } while (Node2DetectLabel);
                }
                #endregion

                PatternEdgeList.Add(PatternEdge);
                PatternEdgeList.Add(tPatternEdge);
            }
            #endregion

            #region Post-Process：删除重复的集合
            bool Stop=false;
            do
            {
                Stop=false;
                foreach(List<ProxiEdge> Pattern in PatternEdgeList)
                {
                    if (Pattern.Count <= 1)
                    {
                        PatternEdgeList.Remove(Pattern);
                        Stop = true;
                        break;
                    }

                    foreach(List<ProxiEdge> CachePattern in PatternEdgeList)
                    {
                        if (Pattern != CachePattern)
                        {
                            if (this.SubSet(Pattern, CachePattern))
                            {
                                PatternEdgeList.Remove(Pattern);
                                Stop = true;
                                break;
                            }
                        }
                    }

                    if (Stop)
                    {
                        break;
                    }
                }

            }while(Stop);
            #endregion

            return PatternEdgeList;
        }

        /// 直线模式探测 根据关系确定边
        /// </summary>
        /// <param name="PeList"></param>边集合
        /// <param name="PnList"></param>点集合
        /// <param name="DistanceConstraint"></param>距离约束
        /// <param name="AngleConstraint"></param>方向约束
        /// <param name="OrientationConstraint"></param>方向约束
        /// <param name="OrientationConstraint"></param>方向约束
        /// <param name="OrientationConstraint"></param>方向约束
        public List<List<ProxiEdge>> LinearPatternDetected4(SMap map, List<ProxiEdge> PeList, List<ProxiNode> PnList, double DistanceConstraint, double AngleConstraint, double shortestDis, double SizeConstraint, double OriConstraint, double ShapeConstraint, double alignConstraint)
        {            
            List<List<ProxiEdge>> PatternEdgeList = new List<List<ProxiEdge>>();

            #region pattern detection
            for (int i = 0; i < PeList.Count; i++)
            {

                #region Node2为起点
                List<ProxiEdge> PatternEdge = new List<ProxiEdge>();//Node2为起点的Pattern
                ProxiEdge OriginalEdge = PeList[i];//探测的初始边
                PatternEdge.Add(OriginalEdge);

                ProxiEdge VisitedEdge = OriginalEdge;//当前被访问的边
                ProxiNode VisitedNode1 = VisitedEdge.Node1; ProxiNode VisitedNode2 = VisitedEdge.Node2;//当前被访问的节点
                #endregion

                #region Node1为起点
                List<ProxiEdge> tPatternEdge = new List<ProxiEdge>();//Node2为起点的Pattern
                tPatternEdge.Add(OriginalEdge);
                ProxiEdge tVisitedEdge = OriginalEdge;//当前被访问的边
                ProxiNode tVisitedNode1 = VisitedEdge.Node1; ProxiNode tVisitedNode2 = VisitedEdge.Node2;//当前被访问的节点
                #endregion

                #region 判断两个建筑物是否相似
                PolygonObject Po1 = map.GetObjectbyID(VisitedNode1.TagID, FeatureType.PolygonType) as PolygonObject;
                PolygonObject Po2 = map.GetObjectbyID(VisitedNode2.TagID, FeatureType.PolygonType) as PolygonObject;
                bool SimLabel = this.Sim(Po1, Po2, SizeConstraint, ShapeConstraint, OriConstraint);
                #endregion

                #region 沿Node2方向探索
                if (SimLabel)
                {
                    VisitedEdge = OriginalEdge;
                    bool Node2DetectLabel = false;
                    do
                    {
                        Node2DetectLabel = false;
                        List<ProxiEdge> EdgeList2 = ReturnEdgeList(PeList, VisitedNode2);
                        EdgeList2.Remove(VisitedEdge);//移除当前访问的边

                        for (int j = 0; j < EdgeList2.Count; j++)
                        {
                            ProxiNode VisitedNode3 = EdgeList2[j].Node1; ProxiNode VisitedNode4 = EdgeList2[j].Node2;//当前被访问的节点

                            PolygonObject Po3 = map.GetObjectbyID(VisitedNode3.TagID, FeatureType.PolygonType) as PolygonObject;
                            PolygonObject Po4 = map.GetObjectbyID(VisitedNode4.TagID, FeatureType.PolygonType) as PolygonObject;

                            bool SimLabelP = this.Sim(Po3, Po4, SizeConstraint, ShapeConstraint, OriConstraint);
                            bool DistanceAccept = DistanceConstrain(VisitedEdge, EdgeList2[j], DistanceConstraint, shortestDis);
                            bool alignAngle2 = this.alignAngleConstrain(Po3, Po4, EdgeList2[j], alignConstraint);
                            bool OrientationAccept = OrientationConstrain(VisitedEdge, EdgeList2[j], AngleConstraint);

                            if (DistanceAccept && OrientationAccept && SimLabelP)
                            {
                                VisitedEdge = EdgeList2[j];
                                if (!PatternEdge.Contains(VisitedEdge))
                                {
                                    //把新加入的边作为访问边
                                    PatternEdge.Add(VisitedEdge);

                                    #region 把新加入的点作为访问点
                                    if (VisitedNode2 == EdgeList2[j].Node1)
                                    {
                                        VisitedNode2 = EdgeList2[j].Node2;
                                    }

                                    else if (VisitedNode2 == EdgeList2[j].Node2)
                                    {
                                        VisitedNode2 = EdgeList2[j].Node1;
                                    }
                                    #endregion

                                    Node2DetectLabel = true;
                                    break;
                                }
                            }
                        }
                    } while (Node2DetectLabel);
                }
                #endregion

                #region 沿Node1方向探索
                if (SimLabel)
                {
                    tVisitedEdge = OriginalEdge;
                    bool Node2DetectLabel = false;
                    do
                    {
                        Node2DetectLabel = false;
                        List<ProxiEdge> EdgeList2 = ReturnEdgeList(PeList, tVisitedNode1);
                        EdgeList2.Remove(tVisitedEdge);//移除当前访问的边

                        for (int j = 0; j < EdgeList2.Count; j++)
                        {
                            ProxiNode VisitedNode3 = EdgeList2[j].Node1; ProxiNode VisitedNode4 = EdgeList2[j].Node2;//当前被访问的节点

                            PolygonObject Po3 = map.GetObjectbyID(VisitedNode3.TagID, FeatureType.PolygonType) as PolygonObject;
                            PolygonObject Po4 = map.GetObjectbyID(VisitedNode4.TagID, FeatureType.PolygonType) as PolygonObject;

                            bool SimLabelP = this.Sim(Po3, Po4, SizeConstraint, ShapeConstraint, OriConstraint);
                            bool DistanceAccept = DistanceConstrain(tVisitedEdge, EdgeList2[j], DistanceConstraint, shortestDis);
                            bool OrientationAccept = OrientationConstrain(tVisitedEdge, EdgeList2[j], AngleConstraint);

                            if (DistanceAccept && OrientationAccept && SimLabelP)
                            {
                                tVisitedEdge = EdgeList2[j];
                                if (!PatternEdge.Contains(tVisitedEdge))
                                {
                                    //把新加入的边作为访问边
                                    tPatternEdge.Add(tVisitedEdge);

                                    #region 把新加入的点作为访问点
                                    if (tVisitedNode1 == EdgeList2[j].Node1)
                                    {
                                        tVisitedNode1 = EdgeList2[j].Node2;
                                    }

                                    else if (tVisitedNode1 == EdgeList2[j].Node2)
                                    {
                                        tVisitedNode1 = EdgeList2[j].Node1;
                                    }
                                    #endregion

                                    Node2DetectLabel = true;
                                    break;
                                }
                            }
                        }
                    } while (Node2DetectLabel);
                }
                #endregion

                PatternEdgeList.Add(PatternEdge);
                PatternEdgeList.Add(tPatternEdge);
            }
            #endregion

            #region Post-Process：删除重复的集合
            bool Stop = false;
            do
            {
                Stop = false;
                foreach (List<ProxiEdge> Pattern in PatternEdgeList)
                {
                    if (Pattern.Count <= 1)
                    {
                        PatternEdgeList.Remove(Pattern);
                        Stop = true;
                        break;
                    }

                    foreach (List<ProxiEdge> CachePattern in PatternEdgeList)
                    {
                        if (Pattern != CachePattern)
                        {
                            if (this.SubSet(Pattern, CachePattern))
                            {
                                PatternEdgeList.Remove(Pattern);
                                Stop = true;
                                break;
                            }
                        }
                    }

                    if (Stop)
                    {
                        break;
                    }
                }

            } while (Stop);
            #endregion

            return PatternEdgeList;
        }

        /// <summary>
        /// 判断P1是否是P2的子集
        /// </summary>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <returns></returns>
        bool SubSet(List<ProxiEdge> P1, List<ProxiEdge> P2)
        {
            foreach (ProxiEdge Pe in P1)
            {
                if (!P2.Contains(Pe))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断P1是否是P2的子集
        /// </summary>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <returns></returns>
        bool SubSet2(List<ProxiEdge> P1, List<ProxiEdge> P2)
        {
            IPointCollection PL1 = new PolylineClass();
            IPointCollection PL2 = new PolylineClass();

            #region PL1
            for (int i = 0; i < P1.Count; i++)
            {
                if (i == 0)
                {
                    IPoint Pn1 = new PointClass();
                    Pn1.X = P1[i].Node1.X;
                    Pn1.Y = P1[i].Node1.Y;
                    PL1.AddPoint(Pn1);
                }

                IPoint Pn2 = new PointClass();
                Pn2.X = P1[i].Node2.X;
                Pn2.Y = P1[i].Node2.Y;

                PL1.AddPoint(Pn2);
            }
            #endregion

            #region PL2
            for (int i = 0; i < P2.Count; i++)
            {
                if (i == 0)
                {
                    IPoint Pn1 = new PointClass();
                    Pn1.X = P2[i].Node1.X;
                    Pn1.Y = P2[i].Node1.Y;
                    PL2.AddPoint(Pn1);
                }

                IPoint Pn2 = new PointClass();
                Pn2.X = P2[i].Node2.X;
                Pn2.Y = P2[i].Node2.Y;

                PL2.AddPoint(Pn2);
            }
            #endregion

            IPolyline ipL1 = PL1 as IPolyline;
            IPolyline ipL2 = PL2 as IPolyline;

            IRelationalOperator iRO = ipL2 as IRelationalOperator;
            if (iRO.Contains(ipL1 as IGeometry)||iRO.Equals(ipL1 as IGeometry))
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }

        /// <summary>
        /// 直线模式refinement 不全局调整（输入为pattern中的边）
        /// 1、首先保持最长的边
        /// 2、其次，处理与最长边关系的pattern，若是边界pattern，则允许；若不是，则取消。
        /// </summary>
        public List<List<ProxiNode>> LinearPatternRefinement1(List<List<ProxiEdge>> PatternEdgeList) //Node标识哪些建筑物组成了边的集合
        {
            List<List<ProxiNode>> refinedPatternNodes = new List<List<ProxiNode>>();//存储
            List<List<List<ProxiNode>>> PatternCluster = this.PatternCluster(PatternEdgeList);//存储相交pattern组成的群集合

            for (int i = 0; i < PatternCluster.Count; i++)
            {
                if (PatternCluster[i].Count == 1)
                {
                    refinedPatternNodes.Add(PatternCluster[i][0]);
                }

                else
                {
                    #region 终止条件：1、当前组中无pattern；2、当前组中有pattern，但是pattern建筑物个数小于3
                    do
                    {
                        Dictionary<List<ProxiNode>, int> VisitPatternLabel = new Dictionary<List<ProxiNode>, int>();//标识pattern是否被访问
                        List<List<ProxiNode>> PatternNodeCache = new List<List<ProxiNode>>();//存储当前已被识别的pattern

                        for (int j = 0; j < PatternCluster[i].Count; j++)
                        {
                            VisitPatternLabel.Add(PatternCluster[i][j], PatternCluster[i][j].Count);
                        }

                        VisitPatternLabel = VisitPatternLabel.OrderByDescending(o => o.Value).ToDictionary(p => p.Key, o => o.Value);
                        KeyValuePair<List<ProxiNode>, int> pair = VisitPatternLabel.First();

                        //若当前剩余的pattern中建筑物个数都不大于2个，则终止判断
                        if (pair.Key.Count > 2)
                        {
                            PatternCluster[i].Remove(pair.Key);
                            refinedPatternNodes.Add(pair.Key);
                            PatternNodeCache.Add(pair.Key);

                            #region 对VisitPatternLabel中的pattern建筑物做更新
                            for (int j = 0; j < PatternCluster[i].Count; j++)
                            {
                                for (int m = 0; m < PatternNodeCache.Count; m++)
                                {
                                    for (int n = 0; n < PatternNodeCache[m].Count; n++)
                                    {    
                                        if (PatternCluster[i][j].Contains(PatternNodeCache[m][n]))
                                        {
                                            PatternCluster[i][j].Remove(PatternNodeCache[m][n]);
                                        }
                                    }
                                }
                            }
                            #endregion
                        }

                        else
                        {
                            PatternCluster[i].Remove(pair.Key);
                        }
                    } while (PatternCluster[i].Count>0);
                    #endregion

                }
            }

            return refinedPatternNodes;
        }

        /// <summary>
        /// 直线模式refinement 不全局调整(输入为pattern中的点)
        /// 1、首先保持最长的边
        /// 2、其次，处理与最长边关系的pattern，若是边界pattern，则允许；若不是，则取消。
        /// </summary>
        public List<List<ProxiNode>> LinearPatternRefinement1(List<List<ProxiNode>> PatternNodeList) //Node标识哪些建筑物组成了边的集合
        {
            List<List<ProxiNode>> refinedPatternNodes = new List<List<ProxiNode>>();//存储裁剪后的pattern
            List<List<List<ProxiNode>>> PatternCluster = this.PatternCluster(PatternNodeList);//存储相交pattern组成的群集合

            for (int i = 0; i < PatternCluster.Count; i++)
            {
                if (PatternCluster[i].Count == 1)
                {
                    refinedPatternNodes.Add(PatternCluster[i][0]);
                }

                else
                {
                    #region 终止条件：1、当前组中无pattern；2、当前组中有pattern，但是pattern建筑物个数小于3
                    do
                    {
                        Dictionary<List<ProxiNode>, int> VisitPatternLabel = new Dictionary<List<ProxiNode>, int>();//标识pattern是否被访问
                        List<List<ProxiNode>> PatternNodeCache = new List<List<ProxiNode>>();//存储当前已被识别的pattern

                        for (int j = 0; j < PatternCluster[i].Count; j++)
                        {
                            VisitPatternLabel.Add(PatternCluster[i][j], PatternCluster[i][j].Count);
                        }

                        VisitPatternLabel = VisitPatternLabel.OrderByDescending(o => o.Value).ToDictionary(p => p.Key, o => o.Value);
                        KeyValuePair<List<ProxiNode>, int> pair = VisitPatternLabel.First();

                        //若当前剩余的pattern中建筑物个数都不大于2个，则终止判断
                        if (pair.Key.Count > 2)
                        {
                            PatternCluster[i].Remove(pair.Key);
                            refinedPatternNodes.Add(pair.Key);
                            PatternNodeCache.Add(pair.Key);

                            #region 对VisitPatternLabel中的pattern建筑物做更新
                            for (int j = 0; j < PatternCluster[i].Count; j++)
                            {
                                for (int m = 0; m < PatternNodeCache.Count; m++)
                                {
                                    for (int n = 0; n < PatternNodeCache[m].Count; n++)
                                    {                                       
                                        for (int a = 0; a < PatternCluster[i][j].Count; a++)
                                        {
                                            if (PatternCluster[i][j].Contains(PatternNodeCache[m][n]))
                                            {
                                                PatternCluster[i][j].Remove(PatternNodeCache[m][n]);
                                            }                            
                                        }                                       
                                    }
                                }
                            }
                            #endregion
                        }

                        else
                        {
                            PatternCluster[i].Remove(pair.Key);
                        }
                    } while (PatternCluster[i].Count > 0);
                    #endregion

                }
            }

            return refinedPatternNodes;
        }

        /// <summary>
        /// 直线模式refinement 只保留直线模式中建筑物是同类型的pattern(Node按顺序排列) 输入的是pattern的边
        /// </summary>
        /// <param name="PatternEdgeList"></param>
        /// <returns></returns>
        public List<List<ProxiNode>> LinearPatternRefinement2(List<List<ProxiEdge>> PatternEdgeList,SMap map)
        {
            List<List<ProxiNode>> SimilarPattern = new List<List<ProxiNode>>();
            for (int i = 0; i < PatternEdgeList.Count; i++)
            {                
                List<ProxiNode> OrderNode = GetOrderProxiNode(PatternEdgeList[i]);//首先，将pattern中的点按顺序排列

                for (int j = 0; j < OrderNode.Count-1; j++)
                {
                    List<ProxiNode> NodeList = new List<ProxiNode>();
                    NodeList.Add(OrderNode[j]);

                    bool Label = false;
                    do
                    {
                        Label = false;
                        PolygonObject Po1 = map.GetObjectbyID(OrderNode[j].TagID, FeatureType.PolygonType) as PolygonObject;
                        PolygonObject Po2 = map.GetObjectbyID(OrderNode[j + 1].TagID, FeatureType.PolygonType) as PolygonObject;

                        if (Po1.ClassID == Po2.ClassID)
                        {
                            NodeList.Add(OrderNode[j + 1]);
                            Label = true;
                            if (j < OrderNode.Count - 2)
                            {
                                j = j + 1;
                            }

                            else
                            {
                                break;
                            }
                        }
                    } while (Label);

                    if (NodeList.Count > 2)
                    {
                        SimilarPattern.Add(NodeList);
                    }
                }
            }

            return SimilarPattern;
        }

        /// <summary>
        /// 获得建筑物中的PatternNodes
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public List<List<ProxiNode>> GetPatterns(SMap map)
        {
            List<List<ProxiNode>> PatternNodeList = new List<List<ProxiNode>>();
            Dictionary<int, List<ProxiNode>> Dic = new Dictionary<int, List<ProxiNode>>();

            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                if (map.PolygonList[i].PatternID != 0)
                {

                    if (Dic.Keys.Contains(map.PolygonList[i].PatternID))
                    {
                        ProxiNode pNode = this.GetNode(map.PolygonList[i].ID, FeatureType.PolygonType);
                        pNode.SortID = map.PolygonList[i].SortID;
                        Dic[map.PolygonList[i].PatternID].Add(pNode);
                    }

                    else
                    {
                        List<ProxiNode> NodeList = new List<ProxiNode>();
                        ProxiNode pNode = this.GetNode(map.PolygonList[i].ID, FeatureType.PolygonType);
                        pNode.SortID = map.PolygonList[i].SortID;
                        NodeList.Add(pNode);
                        Dic.Add(map.PolygonList[i].PatternID, NodeList);
                    }
                }
            }

            PatternNodeList = Dic.Values.ToList();
            return PatternNodeList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        ProxiNode GetNode(int TagID, FeatureType type)
        {
            foreach (ProxiNode Pn in PgforBuildingNodesList)
            {
                if (Pn.FeatureType == type & Pn.TagID == TagID)
                    return Pn;
            }

            return null;
        }

        /// <summary>
        ///获取按顺序排列的点边，并存储 
        /// </summary>
        /// <param name="PatternNodeList"></param>
        public void EdgeforPattern(List<List<ProxiNode>> PatternNodeList)
        {
            for (int i = 0; i < PatternNodeList.Count; i++)
            {
                for (int m = 0; m < PatternNodeList[i].Count-1; m++)
                {
                    for (int j = 0; j < this.PgforBuildingPatternList.Count; j++)
                    {
                        if ((PgforBuildingPatternList[j].Node1 == PatternNodeList[i][m] && PgforBuildingPatternList[j].Node2 == PatternNodeList[i][m + 1]) ||
                            (PgforBuildingPatternList[j].Node1 == PatternNodeList[i][m + 1] && PgforBuildingPatternList[j].Node2 == PatternNodeList[i][m]))
                        {
                            this.PgforRefineSimilarBuildingPatternList.Add(PgforBuildingPatternList[j]);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///获取按顺序排列的点边，并存储 
        /// </summary>
        /// <param name="PatternNodeList"></param>
        public void EdgeforPattern(List<List<ProxiEdge>> LinearPatternEdge)
        {
            for (int i = 0; i < LinearPatternEdge.Count; i++)
            {
                for (int m = 0; m < LinearPatternEdge[i].Count; m++)
                {
                    this.LinearEdges.Add(LinearPatternEdge[i][m]);
                }
            }
        }

        /// <summary>
        /// 将pattern包含边中的点按照一定的顺序排列
        /// </summary>
        /// <returns></returns>
        List<ProxiNode> GetOrderProxiNode(List<ProxiEdge> PatternEdge)
        {
            List<ProxiNode> OrderProxiNode = new List<ProxiNode>();

            #region 添加第一条边的两个点
            for (int i = 0; i < PatternEdge.Count; i++)
            {
                bool Node1Label = false; bool Node2Label = false;
                for (int j = 0; j < PatternEdge.Count; j++)
                {
                    if (j != i)
                    {
                        if (PatternEdge[i].Node1 == PatternEdge[j].Node1 || PatternEdge[i].Node1 == PatternEdge[j].Node2)
                        {
                            Node1Label = true;
                        }

                        if (PatternEdge[i].Node2 == PatternEdge[j].Node1 || PatternEdge[i].Node2 == PatternEdge[j].Node2)
                        {
                            Node2Label = true;
                        }
                    }
                }

                if (!Node1Label || !Node2Label)
                {
                    if (!Node1Label)
                    {
                        OrderProxiNode.Add(PatternEdge[i].Node1);
                        OrderProxiNode.Add(PatternEdge[i].Node2);
                    }

                    else
                    {
                        OrderProxiNode.Add(PatternEdge[i].Node2);
                        OrderProxiNode.Add(PatternEdge[i].Node1);
                    }

                    PatternEdge.RemoveAt(i);
                    break;
                }
            }
            #endregion

            #region 依次获取边关联的点
            do
            {
                for (int i = 0; i < PatternEdge.Count; i++)
                {
                    try
                    {
                        if (PatternEdge[i].Node1 == OrderProxiNode[OrderProxiNode.Count - 1])
                        {
                            OrderProxiNode.Add(PatternEdge[i].Node2);
                            PatternEdge.RemoveAt(i);
                            break;
                        }

                        else if (PatternEdge[i].Node2 == OrderProxiNode[OrderProxiNode.Count - 1])
                        {
                            OrderProxiNode.Add(PatternEdge[i].Node1);
                            PatternEdge.RemoveAt(i);
                            break;
                        }
                    }
                    catch
                    {
                    }
                }
            } while (PatternEdge.Count > 0);
            #endregion

            OrderProxiNode[0].BoundaryNode = true;
            OrderProxiNode[OrderProxiNode.Count - 1].BoundaryNode = true;
            return OrderProxiNode;
        }

        /// <summary>
        /// 获取到了pattern的点，将其转化为边集合
        /// </summary>
        public void NodesforPattern(List<List<ProxiNode>> PatternNodes)
        {
            for (int i = 0; i < this.PgforBuildingPatternList.Count; i++)
            {
                for (int j = 0; j < PatternNodes.Count; j++)
                {
                    if (PatternNodes[j].Contains(this.PgforBuildingPatternList[i].Node1) && PatternNodes[j].Contains(this.PgforBuildingPatternList[i].Node2))
                    {
                        this.PgforRefineBuildingPatternList.Add(this.PgforBuildingPatternList[i]);
                    }
                }
            }
        }

        /// <summary>
        /// 对相交的pattern进行聚类（输入的是pattern对应的边）
        /// </summary>
        /// <param name="PatternEdgeList"></param> pattern组成的pattern的边集
        /// <returns></returns>
        public List<List<List<ProxiNode>>> PatternCluster(List<List<ProxiEdge>> PatternEdgeList)
        {
            List<List<List<ProxiNode>>> PatternCluster = new List<List<List<ProxiNode>>>();
            
            #region 获取每个pattern对应的建筑物
            List<List<ProxiNode>> PatternNodeList = new List<List<ProxiNode>>();
            Dictionary<List<ProxiNode>, bool> PatternNodeListVisit = new Dictionary<List<ProxiNode>, bool>();//标识后面PatternNodeList是否被访问
            for (int i = 0; i < PatternEdgeList.Count; i++)
            {
                List<ProxiNode> PatternNodes = new List<ProxiNode>();
                PatternNodes = this.GetNodesforPattern(PatternEdgeList[i]);
                PatternNodeList.Add(PatternNodes);
                PatternNodeListVisit.Add(PatternNodes, false);
            }
            #endregion

            #region 获取相交的pattern
            for (int i = 0; i < PatternNodeList.Count; i++)
            {
                if (!PatternNodeListVisit[PatternNodeList[i]])
                {
                    List<List<ProxiNode>> PatternClusterList = new List<List<ProxiNode>>();
                    PatternClusterList.Add(PatternNodeList[i]);
                    PatternNodeListVisit[PatternNodeList[i]] = true;

                    for (int n = 0; n < PatternClusterList.Count; n++)
                    {
                        for (int j = 0; j < PatternNodeList.Count; j++)
                        {
                            if (!PatternNodeListVisit[PatternNodeList[j]])
                            {
                                for (int m = 0; m < PatternNodeList[j].Count; m++)
                                {
                                    if (PatternClusterList[n].Contains(PatternNodeList[j][m]))
                                    {
                                        PatternClusterList.Add(PatternNodeList[j]);
                                        PatternNodeListVisit[PatternNodeList[j]] = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    PatternCluster.Add(PatternClusterList);
                }
            }
            #endregion

            #region 获得任意两个pattern的相交关系(相交为true；不相交false)
            //bool[,] IntersectMatrix=new bool[PatternEdgeList.Count,PatternEdgeList.Count];

            #region 矩阵初始化
            //for (int i = 0; i < PatternEdgeList.Count ; i++)
            //{
            //    for (int j = i ; j < PatternEdgeList.Count; j++)
            //    {
            //        IntersectMatrix[i, j] = IntersectMatrix[j, i] = false;
            //    }
            //}
            #endregion

            #region 矩阵赋值
            //for (int i = 0; i < PatternEdgeList.Count-1; i++)
            //{
            //    for (int j = i+1; j < PatternEdgeList.Count; j++)
            //    {
            //        for (int n = 0; n < PatternNodeList[j].Count; n++)
            //        {
            //            if (PatternNodeList[i].Contains(PatternNodeList[j][n]))
            //            {
            //                IntersectMatrix[i,j] = IntersectMatrix[j,i] = true;
            //            }
            //        }
            //    }
            //}
            #endregion
            #endregion

            #region Pattern的聚类
            //for (int i = 0; i < PatternNodeList.Count; i++)
            //{
            //    if (!PatternNodeListVisit[PatternNodeList[i]])
            //    {
            //        List<List<ProxiNode>> PatternClusterList = new List<List<ProxiNode>>();//存储每一组的建筑物pattern
            //        PatternClusterList.Add(PatternNodeList[i]);
            //        PatternNodeListVisit[PatternNodeList[i]] = true;

            //        #region 与pattern相交则加入
            //        for (int j = 0; j < PatternClusterList.Count; j++)
            //        {
            //            for (int m = 0; m < PatternNodeList.Count; m++)
            //            {
            //                if (!PatternNodeListVisit[PatternNodeList[m]])
            //                {
            //                    if (IntersectMatrix[i, m])
            //                    {
            //                        PatternClusterList.Add(PatternNodeList[m]);
            //                        PatternNodeListVisit[PatternNodeList[m]] = true;
            //                    }
            //                }
            //            }
            //        }
            //        #endregion
                       
            //        PatternCluster.Add(PatternClusterList);
            //    }
            //}  
            #endregion

            return PatternCluster;
        }

        /// <summary>
        /// 对相交的pattern进行聚类（输入的是pattern对应的Nodes）
        /// </summary>
        /// <param name="PatternNodeList"></param>
        /// <returns></returns>
        public List<List<List<ProxiNode>>> PatternCluster(List<List<ProxiNode>> PatternNodeList)
        {
            List<List<List<ProxiNode>>> PatternCluster = new List<List<List<ProxiNode>>>();

            #region 标记每个pattern是否被访问
            Dictionary<List<ProxiNode>, bool> PatternNodeListVisit = new Dictionary<List<ProxiNode>, bool>();//标识后面PatternNodeList是否被访问
            for (int i = 0; i < PatternNodeList.Count; i++)
            {
                PatternNodeListVisit.Add(PatternNodeList[i], false);
            }
            #endregion

            #region 获取相交的pattern
            for (int i = 0; i < PatternNodeList.Count; i++)
            {
                if (!PatternNodeListVisit[PatternNodeList[i]])
                {
                    List<List<ProxiNode>> PatternClusterList = new List<List<ProxiNode>>();
                    PatternClusterList.Add(PatternNodeList[i]);
                    PatternNodeListVisit[PatternNodeList[i]] = true;

                    for (int n = 0; n < PatternClusterList.Count; n++)
                    {
                        for (int j = 0; j < PatternNodeList.Count; j++)
                        {
                            if (!PatternNodeListVisit[PatternNodeList[j]])
                            {
                                for (int m = 0; m < PatternNodeList[j].Count; m++)
                                {
                                    if (PatternClusterList[n].Contains(PatternNodeList[j][m]))
                                    {
                                        PatternClusterList.Add(PatternNodeList[j]);
                                        PatternNodeListVisit[PatternNodeList[j]] = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    PatternCluster.Add(PatternClusterList);
                }
            }
            #endregion

            return PatternCluster;
        }

        /// <summary>
        /// 获得给定LinearPattern边集对应的建筑物
        /// </summary>
        /// <param name="PatternEdge"></param>
        /// <returns></returns>
        public List<ProxiNode> GetNodesforPattern(List<ProxiEdge> PatternEdge)
        {
            List<ProxiNode> NodeList = new List<ProxiNode>();

            for (int i = 0; i < PatternEdge.Count; i++)
            {
                if (!NodeList.Contains(PatternEdge[i].Node1))
                {
                    NodeList.Add(PatternEdge[i].Node1);
                }

                if (!NodeList.Contains(PatternEdge[i].Node2))
                {
                    NodeList.Add(PatternEdge[i].Node2);
                }
            }

            return NodeList;
        }

        /// <summary>
        /// 计算两条直线是否满足局部距离差异
        /// </summary>
        /// <param name="pline1"></param>
        /// <param name="pline2"></param>
        /// <returns></returns>
        public bool DistanceConstrain(ProxiEdge pline1, ProxiEdge pline2,double DistanceConstraint,double shortestDis)
        {
            bool label = false;

            double Length1 = pline1.NearestEdge.NearestDistance;
            double Length2 = pline2.NearestEdge.NearestDistance;

            if (Length1 < shortestDis)
            {
                Length1 = shortestDis;
            }

            if (Length2 < shortestDis)
            {
                Length2 = shortestDis;
            }

            double MaxLength = Math.Max(Length1, Length2);
            double MinLength = Math.Min(Length1, Length2);
            double LengthRate = MaxLength / MinLength;

            if (LengthRate < DistanceConstraint)
            {
                label = true;
            }

            return label;
        }

        /// <summary>
        /// 计算是否满足alignAngle约束
        /// </summary>
        /// <param name="pline1"></param>
        /// <param name="pline2"></param>
        /// <returns></returns>
        public bool alignAngleConstrain2(PolygonObject Po1,PolygonObject Po2,ProxiEdge Pe, double alignAngleConstraint)
        {
            #region 计算Pe角度
            ILine pline1 = new LineClass(); 
            IPoint Point11 = new PointClass(); IPoint Point12 = new PointClass();
            Point11.X = Pe.Node1.X; Point11.Y = Pe.Node1.Y;
            Point12.X = Pe.Node2.X; Point12.Y = Pe.Node2.Y;
            pline1.FromPoint = Point11; pline1.ToPoint = Point12;
            double angle1 = pline1.Angle;
            double dAngle1Degree = (180 * angle1) / Math.PI;
            
            if (dAngle1Degree < 0)
            {
                dAngle1Degree = dAngle1Degree + 180;
            }
            #endregion


            double Angle1 = Math.Abs(Po1.MBRO - dAngle1Degree);
            double Angle2 = Math.Abs(Po2.MBRO - dAngle1Degree);

            if (Angle1 <= alignAngleConstraint && Angle2 <= alignAngleConstraint)
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        /// <summary>
        /// 计算两条直线是否满足局部方向差异
        /// </summary>
        /// <param name="Pline1"></param>
        /// <param name="Pline2"></param>
        /// <returns></returns>
        public bool OrientationConstrain(ProxiEdge Pline1, ProxiEdge Pline2,double OrientationConstraint)
        {
            #region Constraint 1
            //bool label = false;

            //ILine pline1 = new LineClass(); ILine pline2 = new LineClass();
            //IPoint Point11 = new PointClass(); IPoint Point12 = new PointClass();
            //IPoint Point21 = new PointClass(); IPoint Point22 = new PointClass();

            //Point11.X = Pline1.Node1.X; Point11.Y = Pline1.Node1.Y;
            //Point12.X = Pline1.Node2.X; Point12.Y = Pline1.Node2.Y;
            //Point21.X = Pline2.Node1.X; Point21.Y = Pline2.Node1.Y;
            //Point22.X = Pline2.Node2.X; Point22.Y = Pline2.Node2.Y;

            //pline1.FromPoint = Point11; pline1.ToPoint = Point12;
            //pline2.FromPoint = Point21; pline2.ToPoint = Point22;

            //double angle1 = pline1.Angle;
            //double angle2 = pline2.Angle;

            //#region 将angle装换到0-180
            //double Pi = 4 * Math.Atan(1);
            //double dAngle1Degree = (180 * angle1) / Pi;
            //double dAngle2Degree = (180 * angle2) / Pi;

            //if (dAngle1Degree < 0)
            //{
            //    dAngle1Degree = dAngle1Degree + 180;
            //}

            //if (dAngle2Degree < 0)
            //{
            //    dAngle2Degree = dAngle2Degree + 180;
            //}
            //#endregion

            //if (Math.Abs(dAngle1Degree - dAngle2Degree) < 90 && Math.Abs(dAngle1Degree - dAngle2Degree) < OrientationConstraint)
            //{
            //    label = true;
            //}

            //if (Math.Abs(dAngle1Degree - dAngle2Degree) > 90 && Math.Abs(180 - Math.Abs(dAngle1Degree - dAngle2Degree)) < OrientationConstraint)
            //{
            //    label = true;
            //}
            #endregion

            #region Constraint 2
            #region 后点是前点 (pline1 Node2;pLine 2 Node1)
            if (Pline1.Node2.X == Pline2.Node1.X && Pline1.Node2.Y == Pline2.Node1.Y)
            {
                double a = Math.Sqrt((Pline1.Node2.X - Pline2.Node2.X) * (Pline1.Node2.X - Pline2.Node2.X) + (Pline1.Node2.Y - Pline2.Node2.Y) * (Pline1.Node2.Y - Pline2.Node2.Y));
                double b = Math.Sqrt((Pline1.Node2.X - Pline1.Node1.X) * (Pline1.Node2.X - Pline1.Node1.X) + (Pline1.Node2.Y - Pline1.Node1.Y) * (Pline1.Node2.Y - Pline1.Node1.Y));
                double c = Math.Sqrt((Pline2.Node2.X - Pline1.Node1.X) * (Pline2.Node2.X - Pline1.Node1.X) + (Pline2.Node2.Y - Pline1.Node1.Y) * (Pline2.Node2.Y - Pline1.Node1.Y));

                double CosCur = (a * a + b * b - c * c) / (2 * a * b);
                double Angle = Math.Acos(CosCur);
                Angle = (180 * Angle) / Math.PI;

                if (Angle >= 180 - OrientationConstraint || Angle == 0)
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }
            #endregion

            #region 后点是前点 (pline1 Node2;pLine2 Node2)
            else if (Pline1.Node2.X == Pline2.Node2.X && Pline1.Node2.Y == Pline2.Node2.Y)
            {
                double a = Math.Sqrt((Pline1.Node2.X - Pline2.Node1.X) * (Pline1.Node2.X - Pline2.Node1.X) + (Pline1.Node2.Y - Pline2.Node1.Y) * (Pline1.Node2.Y - Pline2.Node1.Y));
                double b = Math.Sqrt((Pline1.Node2.X - Pline1.Node1.X) * (Pline1.Node2.X - Pline1.Node1.X) + (Pline1.Node2.Y - Pline1.Node1.Y) * (Pline1.Node2.Y - Pline1.Node1.Y));
                double c = Math.Sqrt((Pline2.Node1.X - Pline1.Node1.X) * (Pline2.Node1.X - Pline1.Node1.X) + (Pline2.Node1.Y - Pline1.Node1.Y) * (Pline2.Node1.Y - Pline1.Node1.Y));

                double CosCur = (a * a + b * b - c * c) / (2 * a * b);
                double Angle = Math.Acos(CosCur);
                Angle = (180 * Angle) / Math.PI;

                if (Angle >= 180 - OrientationConstraint || Angle == 0)
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }
            #endregion

            #region 第三种情况 (pline1 Node1;pLine 2 Node2)
            else if (Pline1.Node1.X == Pline2.Node2.X && Pline1.Node1.Y == Pline2.Node2.Y)
            {
                double a = Math.Sqrt((Pline1.Node1.X - Pline2.Node1.X) * (Pline1.Node1.X - Pline2.Node1.X) + (Pline1.Node1.Y - Pline2.Node1.Y) * (Pline1.Node1.Y - Pline2.Node1.Y));
                double b = Math.Sqrt((Pline1.Node1.X - Pline1.Node2.X) * (Pline1.Node1.X - Pline1.Node2.X) + (Pline1.Node1.Y - Pline1.Node2.Y) * (Pline1.Node1.Y - Pline1.Node2.Y));
                double c = Math.Sqrt((Pline2.Node1.X - Pline1.Node2.X) * (Pline2.Node1.X - Pline1.Node2.X) + (Pline2.Node1.Y - Pline1.Node2.Y) * (Pline2.Node1.Y - Pline1.Node2.Y));

                double CosCur = (a * a + b * b - c * c) / (2 * a * b);
                double Angle = Math.Acos(CosCur);
                Angle = (180 * Angle) / Math.PI;

                if (Angle >= 180 - OrientationConstraint || Angle == 0)
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }
            #endregion

            #region (pline1 Node1;pLine2 Node1)
            else
            {
                double a = Math.Sqrt((Pline1.Node1.X - Pline2.Node2.X) * (Pline1.Node1.X - Pline2.Node2.X) + (Pline1.Node1.Y - Pline2.Node2.Y) * (Pline1.Node1.Y - Pline2.Node2.Y));
                double b = Math.Sqrt((Pline1.Node1.X - Pline1.Node2.X) * (Pline1.Node1.X - Pline1.Node2.X) + (Pline1.Node1.Y - Pline1.Node2.Y) * (Pline1.Node1.Y - Pline1.Node2.Y));
                double c = Math.Sqrt((Pline2.Node2.X - Pline1.Node2.X) * (Pline2.Node2.X - Pline1.Node2.X) + (Pline2.Node2.Y - Pline1.Node2.Y) * (Pline2.Node2.Y - Pline1.Node2.Y));

                double CosCur = (a * a + b * b - c * c) / (2 * a * b);
                double Angle = Math.Acos(CosCur);
                Angle = (180 * Angle) / Math.PI;

                if (Angle >= 180 - OrientationConstraint || Angle == 0)
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }
            #endregion
            #endregion

            //return label;
        }

        /// <summary>
        /// 计算两个建筑物是否满足size约束
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <returns></returns>
        public bool SizeConstrain(PolygonObject Po1, PolygonObject Po2, double SizeConstraint)
        {
            double Area1 = Po1.tArea;
            double Area2 = Po2.tArea;

            double MaxArea = Math.Max(Area1, Area2);
            double MinArea = Math.Min(Area1, Area2);

            double AreaRate = MaxArea / MinArea;

            if (AreaRate > SizeConstraint)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 计算两个建筑物是否满足Shape约束
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <param name="SizeConstraint"></param>
        /// <returns></returns>
        public bool ShapeConstrain(PolygonObject Po1, PolygonObject Po2, double ShapeConstraint)
        {
            double Ed1 = Po1.EdgeCount;
            double Ed2 = Po2.EdgeCount;

            double MaxEd = Math.Max(Ed1, Ed2);
            double MinEd = Math.Min(Ed1, Ed2);

            double EdRate = MaxEd / MinEd;

            if (EdRate > ShapeConstraint)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 计算两个建筑物是否满足ori约束
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <param name="SizeConstraint"></param>
        /// <returns></returns>
        public bool OriConstrain(PolygonObject Po1, PolygonObject Po2, double OriConstraint)
        {
            double Ori1 = Po1.MBRO;
            double Ori2 = Po2.MBRO;

            double AddOri = Math.Abs(Ori1 - Ori2);
            if (AddOri > 90)
            {
                AddOri = 180 - AddOri;
            }

            if (AddOri < OriConstraint)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断两个建筑物是否相似
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <param name="SizeConstraint"></param>
        /// <param name="ShapeConstraint"></param>
        /// <param name="OriConstraint"></param>
        /// <returns></returns>
        public bool Sim(PolygonObject Po1, PolygonObject Po2, double SizeConstraint, double ShapeConstraint, double OriConstraint)
        {
            if (this.SizeConstrain(Po1, Po2, SizeConstraint) && this.ShapeConstrain(Po1, Po2, ShapeConstraint) && this.OriConstrain(Po1, Po2, OriConstraint))
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断PathAngle是否一致
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <param name="alignAngleConstraint"></param>
        /// <returns></returns>
        public bool alignAngleConstrain(PolygonObject Po1, PolygonObject Po2,ProxiEdge Pe, double alignAngleConstraint)
        {
            double Ori1 = Po1.MBRO;
            double Ori2 = Po2.MBRO;

            #region 边方向计算
            ILine pline1 = new LineClass();
            IPoint Point11 = new PointClass(); IPoint Point12 = new PointClass();
            Point11.X = Pe.Node1.X; Point11.Y = Pe.Node1.Y;
            Point12.X = Pe.Node2.X; Point12.Y = Pe.Node2.Y;
            pline1.FromPoint = Point11; pline1.ToPoint = Point12;
            double angle1 = pline1.Angle;
            #endregion

            #region 判断过程
            double AddOri1 = Math.Abs(Ori1 - angle1);
            if (AddOri1 > 90)
            {
                AddOri1 = 180 - AddOri1;
            }

            double AddOri2 = Math.Abs(Ori2 - angle1);
            if (AddOri2 > 90)
            {
                AddOri2 = 180 - AddOri2;
            }

            if (AddOri1 <= alignAngleConstraint && AddOri2 <= alignAngleConstraint)
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
        /// 获得某条边对应的连接边
        /// </summary>
        /// <param name="PeList"></param>
        /// <param name="Pe"></param>
        /// <returns></returns>
        public List<ProxiEdge> ReturnEdgeList(List<ProxiEdge> PeList, ProxiEdge Pe)
        {
            List<ProxiEdge> EdgeList = new List<ProxiEdge>();

            ProxiNode Node1 = Pe.Node1; ProxiNode Node2 = Pe.Node2;
            for (int i = 0; i < PeList.Count; i++)
            {
                ProxiNode pNode1 = PeList[i].Node1; ProxiNode pNode2 = PeList[i].Node2;
                if (Node1.X == pNode1.X && Node2.X != pNode2.X)
                {
                    EdgeList.Add(PeList[i]);
                }

                if (Node2.X == pNode1.X && Node1.X != pNode2.X)
                {
                    EdgeList.Add(PeList[i]);
                }

                if (pNode2.X == Node2.X && pNode1.X != Node1.X)
                {
                    EdgeList.Add(PeList[i]);
                }

                if (pNode2.X == Node1.X && pNode1.X != Node2.X)
                {
                    EdgeList.Add(PeList[i]);
                }
            }

            return EdgeList;
        }

        /// <summary>
        /// 获得某个节点对应的链接边
        /// </summary>
        /// <param name="PeList"></param>
        /// <param name="Pn"></param>
        /// <returns></returns>
        public List<ProxiEdge> ReturnEdgeList(List<ProxiEdge> PeList, ProxiNode Pn)
        {
            List<ProxiEdge> EdgeList = new List<ProxiEdge>();

            for (int i = 0; i < PeList.Count; i++)
            {
                ProxiNode pNode1 = PeList[i].Node1; ProxiNode pNode2 = PeList[i].Node2;
                if (Pn.X == pNode1.X && Pn.Y == pNode1.Y)
                {
                    EdgeList.Add(PeList[i]);
                }

                if (Pn.X == pNode2.X && Pn.Y == pNode2.Y)
                {
                    EdgeList.Add(PeList[i]);
                }
            }

            return EdgeList;

        }
    }
}
