using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Geometry;

namespace PrDispalce.地图要素
{
    /// <summary>
    /// 要素类型
    /// </summary>
    public enum FeatureType
    {
        PointType,
        PolylineType,
        PolygonType,
        Unknown
    }

    public abstract class MapObjects
    {
        public int ID = -1;

        public abstract FeatureType FeatureType
        {
            get;
        }

    }

    #region 点目标
    /// <summary>
    /// 点目标
    /// </summary>
    public class PointObject : MapObjects
    {
        public TriDot Point = null;
        public int nodeID = 0;   //若为端点，值为-1；若是交点，值为1；若是其他点，值为0
        public int nodeCount = 0;    //端点相交的次数
        public int withPolygon = 0;   //若在邻近图中与多边形连接，值为1
        public override FeatureType FeatureType
        {
            get { return FeatureType.PointType; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">ID号</param>
        /// <param name="point">点坐标</param>
        public PointObject(int id, TriDot point)
        {
            ID = id;
            Point = point;
        }


        #region 点的搜索
        /// <summary>
        /// 通过ID号获取点目标
        /// </summary>
        /// <param name="PLList">点列表</param>
        /// <param name="ID">ID号</param>
        /// <returns></returns>
        public static PointObject GetPointbyID(List<PointObject> PList, int ID)
        {
            foreach (PointObject curP in PList)
            {
                if (curP.ID == ID)
                    return curP;
            }
            return null;
        }

        /// <summary>
        /// 通过点目标获得ID号
        /// </summary>
        /// <param name="PList"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static int GetIDbyPoint(List<PointObject> PList, TriDot point)
        {
            int n = PList.Count;
            int t = -1;
            for (int i = 0; i < n; i++)
            {
                TriDot curNode = PList[i].Point;
                if ((curNode.X == point.X) && (curNode.Y == point.Y))
                {
                    t = i;
                    break;
                }
            }
            return t;
        }

        public override bool Equals(object obj)
        {
            if (obj is PointObject)
            {
                PointObject ppoint = obj as PointObject;
                return ((ppoint.Point.X == Point.X) && (ppoint.Point.Y == Point.Y));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (Point.X.GetHashCode() ^ Point.Y.GetHashCode());
        }

        /// <summary>
        /// 判断点集中是否包含点
        /// </summary>
        /// <param name="List">点集</param>
        /// <param name="point">点</param>
        /// <returns></returns>
        public static bool Include(List<PointObject> List, TriDot point)
        {
            bool bol = false;
            if (List.Count == 0)
            {
                bol = false;
            }
            else
            {
                foreach (PointObject curP in List)
                {
                    TriDot curNode = curP.Point;
                    if ((curNode.X == point.X) && (curNode.Y == point.Y))
                    {
                        bol = true;
                        break;
                    }
                }
            }
            return bol;
        }
        #endregion

        #region 点的距离计算
        /// <summary>
        /// 点到点的距离
        /// </summary>
        /// <param name="Po"></param>
        /// <returns></returns>
        public double GetMiniDistance(PointObject Po)
        {
            TriDot tP = Po.Point;
            double x1 = Point.X; double y1 = Point.Y;
            double x2 = tP.X; double y2 = tP.Y;
            double dist = Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
            return dist;
        }
        /// <summary>
        /// 点到点的距离
        /// </summary>
        /// <param name="Po"></param>
        /// <returns></returns>
        public double GetMiniDistance(TriDot Po)
        {
            double x1 = Point.X; double y1 = Point.Y;
            double x2 = Po.X; double y2 = Po.Y;
            double dist = Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
            return dist;
        }
        /// <summary>
        /// 点到多义线的距离
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public double GetMiniDistance(PolylineObject polyline)
        {
            List<TriDot> list = polyline.PointList;
            double minidist = Point.ReturnDist(list, true);
            return minidist;
        }
        /// <summary>
        /// 点到多边形的最短距离
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public double GetMiniDistance(PolygonObject polygon)
        {
            List<TriDot> list = polygon.PointList;
            double minidist = Point.ReturnDist(list, false);
            return minidist;
        }
        #endregion

        #region 返回最近点
        /// <summary>
        /// 点到node的最近点
        /// </summary>
        /// <param name="node"></param>
        /// <param name="sel"></param>
        /// <returns></returns>
        public TriDot ReturnNearestPoint(TriDot node, bool sel)
        {
            TriDot tnode = Point;
            return tnode;
        }
        /// <summary>
        /// 点到po的最近点
        /// </summary>
        /// <param name="po"></param>
        /// <param name="sel"></param>
        /// <returns></returns>
        public TriDot ReturnNearestPoint(PointObject po, bool sel)
        {
            TriDot tnode = Point;
            return tnode;
        }
        #endregion
    }
    #endregion

    #region 线目标
    /// <summary>
    /// 线目标
    /// </summary>
    public class PolylineObject : MapObjects
    {
        //public int ID = -1;
        public List<TriDot> PointList = null;    //节点列表

        public override FeatureType FeatureType
        {
            get { return FeatureType.PolylineType; }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">ID号/TagValue值</param>
        public PolylineObject(int id)
        {
            ID = id;
        }

        public PolylineObject()
        {
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">ID号/TagValue值</param>
        /// <param name="pointList">点列表</param>
        public PolylineObject(int id, List<TriDot> pointList)
        {
            ID = id;
            PointList = pointList;
        }

        public override bool Equals(object obj)
        {
            if (obj is PolylineObject)
            {
                PolylineObject pPolyline = obj as PolylineObject;
                if (pPolyline.PointList.Count == PointList.Count)
                {
                    int n = PointList.Count;
                    int t = 0;
                    for (int i = 0; i < n; i++)
                    {
                        TriDot d = pPolyline.PointList[i];
                        if (!PointList.Contains(d))
                        {
                            t += 1;
                            break;
                        }
                    }
                    if (t == 0)
                        return true;
                    else
                        return false;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return PointList.Count;
        }

        #region 方法
        /// <summary>
        /// 通过ID号获取线目标
        /// </summary>
        /// <param name="PLList">线列表</param>
        /// <param name="ID">ID号</param>
        /// <returns></returns>
        public static PolylineObject GetPolylinebyID(List<PolylineObject> PLList, int ID)
        {
            foreach (PolylineObject curPL in PLList)
            {
                if (curPL.ID == ID)
                    return curPL;
            }
            return null;
        }
        /// <summary>
        /// 判断线上是否包含点
        /// </summary>
        /// <param name="point">点</param>
        /// <returns></returns>
        public bool Contain(PointObject point)
        {
            bool bol = false;
            if (PointList.Count == 0)
                bol = false;

            foreach (TriDot tdot in PointList)
            {
                if ((tdot.X == point.Point.X) && (tdot.Y == point.Point.Y))
                {
                    bol = true;
                    break;
                }
            }
            return bol;
        }
        /// <summary>
        /// 判断线上是否包含点
        /// </summary>
        /// <param name="point">点</param>
        /// <returns></returns>
        public bool Contain(TriDot point)
        {
            bool bol = false;
            if (PointList.Count == 0)
                bol = false;

            foreach (TriDot tdot in PointList)
            {
                if ((tdot.X == point.X) && (tdot.Y == point.Y))
                {
                    bol = true;
                    break;
                }
            }
            return bol;
        }
        #region 距离计算
        /// <summary>
        /// 多义线和点的最短距离
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public double GetMiniDistance(PointObject point)
        {
            double minidist = 0.0;
            TriDot node = point.Point;
            minidist = node.ReturnDist(PointList, false);
            return minidist;
        }
        /// <summary>
        /// 线到点TriNode的最小值
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public double GetMiniDistance(TriDot node)
        {
            double dist = node.ReturnDist(PointList, false);
            return dist;
        }
        /// <summary>
        /// 线到线的最短距离
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public double GetMiniDistance(PolylineObject polyline)
        {
            double minidist = 0.0;
            //线polyline上每一点到所求线上最短距离
            List<TriDot> list = polyline.PointList;
            for (int i = 0; i < list.Count; i++)
            {
                TriDot node = list[i];
                double dist = node.ReturnDist(PointList, true);
                if (minidist == 0.0)
                {
                    minidist = dist;
                }
                else
                {
                    if (dist < minidist)
                    {
                        minidist = dist;
                    }
                }
            }
            //所求线上每一点到线polyline的最短距离 
            for (int i = 0; i < PointList.Count; i++)
            {
                TriDot node = PointList[i];
                double dist = node.ReturnDist(list, false);
                if (dist < minidist)
                    minidist = dist;
            }
            return minidist;
        }
        #region 专求两个比较近的线之间的最短垂距
        public double GetMiniDistance_1(PolylineObject polyline)
        {
            double minidist = 0.0;
            //线polyline上每一点到所求线上最短距离
            List<TriDot> list = polyline.PointList;
            for (int i = 0; i < list.Count; i++)
            {
                TriDot node = list[i];
                double dist = node.ReturnDist(PointList, true);
                if (minidist == 0.0)
                {
                    minidist = dist;
                }
                else
                {
                    if (dist < minidist)
                    {
                        minidist = dist;
                    }
                }
            }
            //所求线上每一点到线polyline的最短距离 
            for (int i = 0; i < PointList.Count; i++)
            {
                TriDot node = PointList[i];
                double dist = node.ReturnDist(list, false);
                if (dist < minidist)
                    minidist = dist;
            }
            return minidist;
        }
        #endregion
        #endregion

        #region 返回最近点
        /// <summary>
        /// 点node到线的最近点
        /// </summary>
        /// <param name="node">邻近点</param>
        /// <param name="sel">是否选择延长线上的点</param>
        /// <returns></returns>
        public TriDot ReturnNearestPoint(TriDot node, bool sel)
        {
            TriDot nearestNode = node.ReturnNearestPoint(PointList, sel);
            return nearestNode;
        }
        /// <summary>
        /// 点po到线的最近点
        /// </summary>
        /// <param name="po">邻近点</param>
        /// <param name="sel">是否选择延长线上的点</param>
        /// <returns></returns>
        public TriDot ReturnNearestPoint(PointObject po, bool sel)
        {
            TriDot node = po.Point;
            TriDot nearestNode = node.ReturnNearestPoint(PointList, sel);
            return nearestNode;
        }
        #endregion
        #endregion
    }
    #endregion

    #region 面目标
    /// <summary>
    /// 面目标
    /// </summary>
    public class PolygonObject : MapObjects, ICloneable
    {
        // public int ID = -1;
        public List<TriDot> PointList = null;    //节点列表
        public int CID;     //重心编号
        public List<int> closelineTag = new List<int>();
        IPoint cpoint;//重心点
        double parea;//面积
        public List<PointObject> PointList1 = null;
        public string Label = "noConflict";
        public int conflictId = 999999;
        public int Level = 0; //标识是层次移位中的第几层次建筑物
        public List<int> ConflictIDs = new List<int>();
        public List<int> bbConflictIDs = new List<int>();
        public bool DisplacementLabel = false;//标识是否进行了移位
        public int Type = 0;//标识建筑物的类型
        public List<int> IDList = new List<int>();//聚合的建筑物ID
        public bool IsReshaped = false;//标识是否reshaped，false未reshape，truereshape
        public List<List<double>> BendAngle = new List<List<double>>();//记录三角网的BendAngle

        public object Clone()
        {
            PolygonObject cPolygonObject = new PolygonObject();
            cPolygonObject.PointList = this.PointList;
            cPolygonObject.CID = this.CID;
            cPolygonObject.closelineTag = this.closelineTag;
            cPolygonObject.cpoint = this.cpoint;
            cPolygonObject.parea = this.parea;
            cPolygonObject.PointList1 = this.PointList1;
            cPolygonObject.Label = this.Label;
            cPolygonObject.conflictId = this.conflictId;
            cPolygonObject.Level = this.Level;
            cPolygonObject.ConflictIDs = this.ConflictIDs;
            cPolygonObject.bbConflictIDs = this.bbConflictIDs;
            return cPolygonObject;
        }

        public double GetArea()  //计算多边形的面积
        {
            double area = 0.0;
            int n = PointList.Count;
            TriDot dot1 = null, dot2 = null;
            for (int i = 0; i < n - 1; i++)
            {
                dot1 = PointList[i]; dot2 = PointList[i + 1];
                area += ((dot1.X * dot2.Y - dot1.Y * dot2.X) / 2);
            }
            return Math.Abs(area);
        }
        public IPoint Cpoint
        {
            get { return this.cpoint; }
            //set { this.cpoint = value; }
        }

        public double Parea
        {
            get { return this.parea; }
        }


        public override FeatureType FeatureType
        {
            get { return FeatureType.PolygonType; }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">ID号/TagValue值</param>
        /// <param name="pointList">点列表</param>
        public PolygonObject(int id, List<TriDot> pointList)
        {
            ID = id;
            PointList = pointList;
        }

        public PolygonObject(int id, List<int> conflictids, string label, List<TriDot> pointList)
        {
            ID = id;
            Label = label;
            PointList = pointList;
            ConflictIDs = conflictids;
        }
        public PolygonObject(int id, List<PointObject> pointList)
        {
            ID = id;
            PointList1 = pointList;
        }

        public PolygonObject()
        {
        }

        public PolygonObject(IPolygon polygon)
        {
            IArea area = (IArea)polygon;
            cpoint = area.Centroid;
            parea = area.Area;

        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">ID号/TagValue</param>
        /// <param name="pointList">点列表</param>
        /// <param name="cid">重心编号</param>
        public PolygonObject(int id, List<TriDot> pointList, int cid)
        {
            ID = id;
            PointList = pointList;
            CID = cid;
        }

        public override bool Equals(object obj)
        {
            if (obj is PolygonObject)
            {
                PolygonObject pPolygon = obj as PolygonObject;
                if (pPolygon.PointList.Count == PointList.Count)
                {
                    int n = PointList.Count;
                    int t = 0;
                    for (int i = 0; i < n; i++)
                    {
                        TriDot d = pPolygon.PointList[i];
                        if (!PointList.Contains(d))
                        {
                            t += 1;
                            break;
                        }
                    }
                    if (t == 0)
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
            return false;
        }

        public override int GetHashCode()
        {
            double tx = 0.0, ty = 0.0;
            foreach (TriDot dot in PointList)
            {
                tx += dot.X; ty += dot.Y;
            }
            int cx = (int)(tx / PointList.Count);
            int cy = (int)(ty / PointList.Count);
            return (cx.GetHashCode() ^ cy.GetHashCode());
        }

        /// <summary>
        /// 通过ID号获取线目标
        /// </summary>
        /// <param name="PLList">线列表</param>
        /// <param name="ID">ID号</param>
        /// <returns></returns>
        public static PolygonObject GetPolygonbyID(List<PolygonObject> PPList, int ID)
        {
            foreach (PolygonObject curPP in PPList)
            {
                if (curPP.ID == ID)
                    return curPP;
            }
            return null;
        }

        public static PolygonObject GetPolygonbyCID(List<PolygonObject> PPList, int ID)
        {
            foreach (PolygonObject curPP in PPList)
            {
                if (curPP.CID == ID)
                    return curPP;
            }
            return null;
        }
        public TriDot CalPolygonCenterPoint(List<TriDot> PointList)
        {
            if (1 == PointList.Count)
            {
                return PointList[0];
            }
            else if (2 == PointList.Count)
            {
                return new TriDot((PointList[0].X + PointList[0].X) / 2, (PointList[1].Y + PointList[1].Y) / 2);
            }
            else if (this.PointList.Count >= 3)
            {
                //List<TriDot> newList = new List<TriDot>();
                //for (int i = 1; i < PointList.Count - 1; ++i)
                //{
                //    newList.Add(CalTriCenterPoint(PointList[0], PointList[i], PointList[i + 1]));
                //}
                //return CalPolygonCenterPoint(newList);
                double sumX = 0.0, sumY = 0.0;
                int count = PointList.Count;
                for (int i = 0; i < PointList.Count; i++)
                {
                    sumX += PointList[i].X;
                    sumY += PointList[i].Y;

                }

                TriDot medPoint = new TriDot(sumX / count, sumY / count);
                return medPoint;
            }
            else
            {
                throw new Exception("点的集合为空！");
            }
        }

        public TriDot CalTriCenterPoint(TriDot pt1, TriDot pt2, TriDot pt3)
        {
            double x = pt1.X + pt2.X + pt3.X;
            x /= 3;
            double y = pt1.Y + pt2.Y + pt3.Y;
            y /= 3;
            return new TriDot(x, y);
        }

        #region 距离计算
        /// <summary>
        /// 多边形到点的最小距离
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public double GetMiniDistance(TriDot node)
        {
            double dist = 0.0;
            dist = node.ReturnDist(PointList, false);
            return dist;
        }
        /// <summary>
        /// 多边形到点的最小距离
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public double GetMiniDistance(PointObject point)
        {
            TriDot node = point.Point;
            double minidist = node.ReturnDist(PointList, false);
            return minidist;
        }
        /// <summary>
        /// 多边形到多义线的最小距离
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public double GetMiniDistance(PolylineObject polyline)
        {
            double minidist = 0.0;
            //线的点链
            List<TriDot> list = polyline.PointList;
            //多边形上每一点到线的最短距离 
            for (int i = 0; i < PointList.Count; i++)
            {
                TriDot node = PointList[i];
                double dist = node.ReturnDist(list, false);
                if (minidist == 0.0)
                {
                    minidist = dist;
                }
                else
                {
                    if (dist < minidist)
                    {
                        minidist = dist;
                    }
                }
            }
            return minidist;
        }
        /// <summary>
        /// 多边形到多边形的最小距离
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public double GetMiniDistance(PolygonObject polygon)
        {
            double minidist = 0.0;
            //多边形的点链
            List<TriDot> list = polygon.PointList;
            //参数多边形上每一点到多边形的最短距离
            for (int i = 0; i < list.Count; i++)
            {
                TriDot node = list[i];
                double dist = node.ReturnDist(PointList, false);
                if (minidist == 0.0)
                {
                    minidist = dist;
                }
                else
                {
                    if (dist < minidist)
                    {
                        minidist = dist;
                    }
                }
            }
            //多边形上每一点到参数多边形的最短距离 
            for (int i = 0; i < PointList.Count; i++)
            {
                TriDot node = PointList[i];
                double dist = node.ReturnDist(list, false);
                if (dist < minidist)
                    minidist = dist;
            }
            return minidist;
        }
        #endregion

        #region 返回最近点
        /// <summary>
        /// 返回点po到多边形的最近点
        /// </summary>
        /// <param name="po">邻近点</param>
        /// <param name="sel">是否取延长线上的点</param>
        /// <returns></returns>
        public TriDot ReturnNearestPoint(PointObject po, bool sel)
        {
            TriDot node = po.Point;
            TriDot nearestNode = node.ReturnNearestPoint(PointList, sel);
            return nearestNode;
        }
        /// <summary>
        /// 返回点node到多边形的最近点
        /// </summary>
        /// <param name="node">邻近点</param>
        /// <param name="sel">是否取延长线上的点</param>
        /// <returns></returns>
        public TriDot ReturnNearestPoint(TriDot node, bool sel)
        {
            TriDot nearestNode = node.ReturnNearestPoint(PointList, sel);
            return nearestNode;
        }
        /// 多边形到多边形之间最小距离的顶点
        /// </summary>
        /// <param name="po"></param>
        /// <param name="sel"></param>
        /// <returns></returns>
        public PointObject NearVexPoint(PolygonObject polygon)
        {
            PointObject nearVexPoint = null;
            double minidist = 0.0;
            //多边形的点链
            List<TriDot> list = polygon.PointList;
            //参数多边形上每一点到多边形的最短距离
            //for (int i = 0; i < list.Count; i++)
            //{
            //    TriDot node = list[i];
            //    double dist = node.ReturnDist(PointList, false);
            //    if (minidist == 0.0)
            //    {
            //        minidist = dist;
            //    }
            //    else
            //    {
            //        if (dist < minidist)
            //        {
            //            minidist = dist;

            //        }
            //    }
            //}
            //多边形上每一点到参数多边形的最短距离 
            for (int i = 0; i < PointList.Count; i++)
            {
                TriDot node = PointList[i];
                double dist = node.ReturnDist(list, false);
                if (minidist == 0.0)
                {
                    minidist = dist;
                    minidist = dist;
                    TriDot tridot = new TriDot(node.X, node.Y);
                    nearVexPoint = new PointObject(tridot.ID, tridot);

                }
                else
                {
                    if ((dist < minidist))
                    {
                        minidist = dist;
                        TriDot tridot = new TriDot(node.X, node.Y);
                        nearVexPoint = new PointObject(tridot.ID, tridot);

                    }

                    if (minidist < 0.02)
                    {
                        break;
                    }

                }

            }
            return nearVexPoint;
        }

        #endregion

        /// <summary>
        /// 计算一个多边形的转角函数参数
        /// </summary>
        public void GetBendAngle()
        {
            double Dis = 0;

            for (int i = 0; i < this.PointList.Count; i++)
            {
                #region 获取节点信息
                TriDot Point1 = null; TriDot Point2 = null; TriDot Point3 = null;

                if (i == 0)
                {
                    Point1 = this.PointList[i];
                    Point2 = this.PointList[i + 1];
                    Point3 = this.PointList[this.PointList.Count - 1];                
                }

                else if (i == this.PointList.Count - 1)
                {
                    Point1 = this.PointList[i];
                    Point2 = this.PointList[0];
                    Point3 = this.PointList[i - 1];
                }

                else
                {
                    Point1 = this.PointList[i];
                    Point2 = this.PointList[i + 1];
                    Point3 = this.PointList[i - 1];
                }
                #endregion

                #region 计算叉积信息
                double Vector1X = Point3.X - Point1.X; double Vector1Y = Point3.Y - Point1.Y;
                double Vector2X = Point2.X - Point1.X; double Vector2Y = Point2.Y - Point1.Y;

                double xMultiply = Vector1X * Vector2Y - Vector1Y * Vector2X;//获得叉积信息，用于判断顺逆时针
                #endregion

                #region 计算角度信息(顺时针角度为正；逆时针角度为负)
                double Angle = GetAngle(Point1, Point2, Point3);
                if (xMultiply < 0)
                {
                    Angle = Angle * (-1);
                }
                #endregion

                List<double> OneAngle = new List<double>();
                OneAngle.Add(Dis); OneAngle.Add(Angle);
                this.BendAngle.Add(OneAngle);

                double EdgeDis = Math.Sqrt((Point2.X - Point1.X) * (Point2.X - Point1.X) + (Point2.Y - Point1.Y) * (Point2.Y - Point1.Y));
                Dis = EdgeDis + Dis;
            }
        }

        /// <summary>
        /// 给定三点，计算该点的角度值
        /// </summary>
        /// <param name="curNode"></param>
        /// <param name="TriNode1"></param>
        /// <param name="TriNode2"></param>
        /// <returns></returns>
        public double GetAngle(TriDot curNode, TriDot TriNode1, TriDot TriNode2)
        {
            double a = Math.Sqrt((curNode.X - TriNode1.X) * (curNode.X - TriNode1.X) + (curNode.Y - TriNode1.Y) * (curNode.Y - TriNode1.Y));
            double b = Math.Sqrt((curNode.X - TriNode2.X) * (curNode.X - TriNode2.X) + (curNode.Y - TriNode2.Y) * (curNode.Y - TriNode2.Y));
            double c = Math.Sqrt((TriNode1.X - TriNode2.X) * (TriNode1.X - TriNode2.X) + (TriNode1.Y - TriNode2.Y) * (TriNode1.Y - TriNode2.Y));

            double CosCur = (a * a + b * b - c * c) / (2 * a * b);
            double Angle = Math.Acos(CosCur);

            return Angle;
        }
    }

#endregion       
}
