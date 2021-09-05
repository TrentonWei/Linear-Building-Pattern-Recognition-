using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;

namespace AuxStructureLib
{
    public abstract class MapObject
    {
        public int ID=-1;
        public double SylWidth =0;
        public int TypeID = 1;//1线要素，2边界
        public int SomeValue=-1;
        public int SimLabel = 0;//0表示可以简化；1表示不可以简化

        public int ConflictCount = 0;
        public List<int> IDList = new List<int>();//存储合并的建筑物ID
        public List<int> TyList = new List<int>();
        public double Volume = 0;
        

        /// <summary>
        /// 获取类型
        /// </summary>
        public abstract FeatureType FeatureType
        {
            get;
        }

    }
    /// <summary>
    /// 点目标
    /// </summary>
    public class PointObject : MapObject
    {
        public TriNode Point= null;

        public override FeatureType FeatureType
        {
            get { return FeatureType.PointType; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">ID号</param>
        /// <param name="point">点坐标</param>
        public PointObject(int id, TriNode point)
        {
            ID = id;
            Point = point;
        }

        /// <summary>
        /// 通过ID号获取点目标
        /// </summary>
        /// <param name="PLList">线列表</param>
        /// <param name="ID">ID号</param>
        /// <returns></returns>
        public static PointObject GetPPbyID(List<PointObject> PList, int ID)
        {
            foreach (PointObject curP in PList)
            {
                if (curP.ID == ID)
                    return curP;
            }
            return null;
        }


        /// <summary>
        /// 计算邻近点-坐标位置为对象的中心
        /// </summary>
        /// <returns>返回邻近点</returns>
        public  ProxiNode CalProxiNode()
        {
            //throw new NotImplementedException();
            return new ProxiNode(this.Point.X, this.Point.Y, -1, this.ID,FeatureType.PointType);
        }
    }

    /// <summary>
    /// 线目标
    /// </summary>
    public class PolylineObject : MapObject
    {
        //public int ID = -1;
        public List<TriNode> PointList = null;    //节点列表


        public override FeatureType FeatureType
        {
            get { return FeatureType.PolylineType; }
        }

        public PolylineObject(int id)
        {
            ID = id;
        }

        public PolylineObject(List<TriNode> pointList)
        {
            this.PointList = pointList;
        }

        public PolylineObject()
        {
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">ID号</param>
        /// <param name="pointList">线列表</param>
        /// <param name="width">符号宽度</param>
        public PolylineObject(int id, List<TriNode> pointList, double width)
        {
            ID = id;
            PointList = pointList;
            this.SylWidth = width;
        }

        /// <summary>
        /// 通过ID号获取线目标
        /// </summary>
        /// <param name="PLList">线列表</param>
        /// <param name="ID">ID号</param>
        /// <returns></returns>
        public static PolylineObject GetPLbyID(List<PolylineObject> PLList,int ID)
        {
            foreach (PolylineObject curPL in PLList)
            {
                if (curPL.ID == ID)
                    return curPL;
            }
            return null;
        }

        public IPolyline ToEsriPolyline()
        {
            object missing1 = Type.Missing;
            object missing2 = Type.Missing;
            IPolyline esriPolyline = new PolylineClass();
            // shp.SpatialReference = mapControl.SpatialReference;
            IPointCollection pointSet = esriPolyline as IPointCollection;
            IPoint curResultPoint = null;
            TriNode curPoint = null;
            if (this == null)
                return null;
            int m = this.PointList.Count;

            for (int k = 0; k < m; k++)
            {
                curPoint = this.PointList[k];
                curResultPoint = new PointClass();
                curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                pointSet.AddPoint(curResultPoint, ref missing1, ref missing2);
            }
            return esriPolyline;
        }
        /// <summary>
        /// 计算邻近点-坐标位置为对象的中心
        /// </summary>
        /// <returns>返回邻近点</returns>
        public  ProxiNode CalProxiNode()
        {
            double sumx = 0;
            double sumy = 0;
            foreach (TriNode curP in this.PointList)
            {
                sumx += curP.X;
                sumy += curP.Y;
            }
            sumx = sumx / this.PointList.Count;
            sumy = sumy / this.PointList.Count;
            return new ProxiNode(sumx, sumy, -1, this.ID, FeatureType.PolylineType); 
        }
    }
    /// <summary>
    /// 构造函数
    /// </summary>
    public class ConNode : MapObject
    {
       // public int ID = -1;
       // public double SylWidth = -1;              //以毫米为单位的符号宽度
        public TriNode Point = null;

        public override FeatureType FeatureType
        {
            get { return FeatureType.Unknown; }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id"></param>
        /// <param name="width"></param>
        /// <param name="point"></param>
        public ConNode(int id, double width, TriNode point)
        {
            ID = id;
            this.SylWidth = width;
            Point = point;
        }
        /// <summary>
        /// 获取ID的结点
        /// </summary>
        /// <param name="NList"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static ConNode GetPLbyID(List<ConNode> NList, int ID)
        {
            foreach (ConNode curNL in NList)
            {
                if (curNL.ID == ID)
                    return curNL;
            }
            return null;
        }

        /// <summary>
        /// 判断当期的顶点是否已经在关联点集合中
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns></returns>
        public static TriNode GetContainNode(List<ConNode> NList, List<TriNode> vexList,double x, double y)
        {
            
            if (NList == null || NList.Count == 0)
            {
                return null;
            }
            foreach (ConNode curNode in NList)
            {
               // int id = curNode.ID;
                TriNode curV = curNode.Point;

                if (Math.Abs((1-curV.X/x)) <= 0.000001f && Math.Abs((1-curV.Y / y)) <= 0.000001f)
                {
                    return curV;
                }
            }
            return null;
        }

        /// <summary>
        /// 判断当期的顶点是否已经在关联点集合中
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns></returns>
        public static TriNode GetContainNode(List<TriNode> NList,  double x, double y)
        {

            if (NList == null || NList.Count == 0)
            {
                return null;
            }
            foreach (TriNode curNode in NList)
            {
                // int id = curNode.ID;
                TriNode curV = curNode;

                if (Math.Abs((1 - curV.X / x)) <= 0.00001f && Math.Abs((1 - curV.Y / y)) <= 0.00001f)
                {
                    return curV;
                }
            }
            return null;
        }

        /// <summary>
        /// 判断当期的顶点是否已经在关联点集合中
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns></returns>
        public static TriNode GetContainNode(List<ConNode> NList, double x, double y)
        {

            if (NList == null || NList.Count == 0)
            {
                return null;
            }
            foreach (ConNode curNode in NList)
            {
                // int id = curNode.ID;
                TriNode curV = curNode.Point;

                if (Math.Abs((1 - curV.X / x)) <= 0.000001f && Math.Abs((1 - curV.Y / y)) <= 0.000001f)
                {
                    return curV;
                }
            }
            return null;
        }


    }

    /// <summary>
    /// 面目标
    /// </summary>
    public class PolygonObject : MapObject
    {
       // public int ID = -1;
        public List<TriNode> PointList = null;    //节点列表

        public List<TrialPosition> TriableList = null;    //节点列表
        private double area=-1;
        private double perimeter = -1;
        public bool IsReshape = false;//标识是否进行reshaped；true是reshaped
        public int TagID=-1;//建筑物的固定标识

        public int ClassID;//建筑物所属的分类
        public int PatternID;//建筑物所属的Pattern（没有考虑相交）（Pattern中居民地）
        public int SortID;//在Pattern中的顺序（范围多边形）
        public int Road;//是否邻近道路

        public List<int> PatternIDList=new List<int>();//建筑物所属的pattern;可能分属多个pattern
        public List<List<int>> OrderIDList=new List<List<int>>();//建筑物在pattern中的排序，分别存储对应的pattern和在pattern中的排序;可能分属多个pattern
        public bool SiSim = false;//建筑物所属的pattern大小是否相似 true表示相似 
        public bool OriSim = false;//建筑物所属的pattern方向是否相似 false代表不相似
        public bool BoundaryBuilding = false;//是否是Pattern边界建筑物
        public bool IntersectBuilding = false;//是否是边界建筑物
        public bool InnerBuilding = false;//是否为内部建筑物
        public List<List<double>> BendAngle = new List<List<double>>();//记录多边形的BendAngle（长度累积和角度）
        public List<PolygonObject> RNGProximity1 = new List<PolygonObject>();//在RNG上一阶邻近的建筑物
        public List<PolygonObject> SurrPos = new List<PolygonObject>();//概率松弛方法中建筑物的邻域范围内建筑物列表

        public double AverageArea = 0;//平均面积
        public double AreaDiff = 0;//面积标准差
        public double VarAreaDiff = 0;//面积变异系数
        public double BlackWhiteRatio = 0;//黑白面积对比
        public double smbrRatio = 0;//绑定矩形黑白面积对比
        public double AverageDistance = 0;//平均距离
        public double DistanceDiff = 0;//距离标准差
        public double VarDistanceDiff = 0;//距离变异系数
        public double EdgeCountDiff=0;//边数的标准差
        public double VarEdgeCountDiff = 0;//边数的变异系数
        public double IPQComAverage = 0;//IPQCom的平均值
        public double IPQComDiff = 0;//IPQCom标准差
        public double AveIPQComDiff = 0;//均值偏差
        public double RatioAverage = 0;//邻近正对面积的平均值

        public double MBRO = 0;//标识建筑物得最小绑定矩形方向
        public int EdgeCount=0;
        public double tArea = 0;

        public int InOut;//表示建筑物图形是环状，0表示外环；>0表示Holes
        public int BuildingID = -1;//表示建筑物所在ID

        public int SourceTemp = 0;
        public int TargetTemp = 0;

        public override FeatureType FeatureType
        {
            get { return FeatureType.PolygonType; }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">ID号</param>
        /// <param name="pointList">线列表</param>
        public PolygonObject(int id, List<TriNode> pointList)
        {
            ID = id;
            PointList = pointList;
            TriableList = new List<TrialPosition>();
        }

        /// <summary>
        /// 通过ID号获取线目标
        /// </summary>
        /// <param name="PLList">线列表</param>
        /// <param name="ID">ID号</param>
        /// <returns></returns>
        public static PolygonObject GetPPbyID(List<PolygonObject> PPList, int ID)
        {
            foreach (PolygonObject curPP in PPList)
            {
                if (curPP.ID == ID)
                    return curPP;
            }
            return null;
        }

        /// <summary>
        /// 计算邻近点-坐标位置为对象的中心
        /// </summary>
        /// <returns>返回邻近点</returns>
        public  ProxiNode CalProxiNode()
        {
            double sumx = 0;
            double sumy = 0;
            foreach (TriNode curP in this.PointList)
            {
                sumx += curP.X;
                sumy += curP.Y;
            }
            sumx = sumx / this.PointList.Count;
            sumy = sumy / this.PointList.Count;
            return new ProxiNode(sumx, sumy, -1, this.ID, FeatureType.PolygonType); 
        }

        /// <summary>
        /// 计算多边形面积
        /// </summary>
        /// <returns></returns>
        public double Area
        {
            get
            {
                if (this.area != -1)
                    return this.area;
                else
                {
                    this.area = 0;
                    int n = this.PointList.Count;
                    this.PointList.Add(PointList[0]);
                    for (int i = 0; i < n; i++)
                    {
                        area += (PointList[i].X * PointList[i + 1].Y - PointList[i + 1].X * PointList[i].Y);
                       
                    }
                    area = 0.5*Math.Abs(area);
                    this.PointList.RemoveAt(n);
                    return area;
                }
            }
        }

        /// <summary>
        /// 面平移
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public void Translate(double dx, double dy)
        {
            if (dx != 0 || dy != 0)
            {
                foreach (TriNode vetex in this.PointList)
                {
                    vetex.X += dx;
                    vetex.Y += dy;
                }
            }
        }

        /// <summary>
        /// 计算多边形周长
        /// </summary>
        /// <returns></returns>
        public double Perimeter
        {
            get
            {
                if (this.perimeter != -1)
                    return this.perimeter;
                else
                {
                    this.perimeter = 0;
                    int n = this.PointList.Count;
                    for (int i = 0; i < n - 1; i++)
                    {
                        perimeter += ComFunLib.CalLineLength(PointList[i], PointList[i + 1]);
                    }
                    perimeter += ComFunLib.CalLineLength(PointList[n-1], PointList[0]);
                    return perimeter;
                }
            }
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
            List<TriNode> list = polygon.PointList;
            //参数多边形上每一点到多边形的最短距离
            for (int i = 0; i < list.Count; i++)
            {
                TriNode node = list[i];
                double dist = node.ReturnDist(PointList,false);
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
                TriNode node = PointList[i];
                double dist = node.ReturnDist(list,false);
                if (dist < minidist)
                    minidist = dist;
            }
            return minidist;
        }

        /// <summary>
        /// 计算一个多边形的转角函数参数（这里的转角函数，记录的是内外角，需要说明！！！）（长度记录节点到下一条边的累积长度）
        /// 
        /// </summary>
        public void GetBendAngle()
        {
            this.BendAngle.Clear();//每次计算BendAngle之前，先将BendAngle清空

            double Dis = 0;

            for (int i = 0; i < this.PointList.Count; i++)
            {
                #region 获取节点信息
                TriNode Point1 = null; TriNode Point2 = null; TriNode Point3 = null;

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

                double EdgeDis = Math.Sqrt((Point2.X - Point1.X) * (Point2.X - Point1.X) + (Point2.Y - Point1.Y) * (Point2.Y - Point1.Y));
                Dis = EdgeDis + Dis;

                OneAngle.Add(Dis); OneAngle.Add(Angle);
                this.BendAngle.Add(OneAngle);
            }
        }

        /// <summary>
        /// 计算一个多变形节点的转角与长度(长度只记录节点对应的下一条边的长度)
        /// </summary>
        public void GetBendAngle2()
        {
            this.BendAngle.Clear();//每次计算BendAngle之前，先将BendAngle清空

            for (int i = 0; i < this.PointList.Count; i++)
            {
                #region 获取节点信息
                TriNode Point1 = null; TriNode Point2 = null; TriNode Point3 = null;

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
                double EdgeDis = Math.Sqrt((Point2.X - Point1.X) * (Point2.X - Point1.X) + (Point2.Y - Point1.Y) * (Point2.Y - Point1.Y));
                OneAngle.Add(EdgeDis); OneAngle.Add(Angle);
                this.BendAngle.Add(OneAngle);               
            }
        }

        /// <summary>
        /// 给定三点，计算该点的角度值
        /// </summary>
        /// <param name="curNode"></param>
        /// <param name="TriNode1"></param>
        /// <param name="TriNode2"></param>
        /// <returns></returns>
        public double GetAngle(TriNode curNode, TriNode TriNode1, TriNode TriNode2)
        {
            double a = Math.Sqrt((curNode.X - TriNode1.X) * (curNode.X - TriNode1.X) + (curNode.Y - TriNode1.Y) * (curNode.Y - TriNode1.Y));
            double b = Math.Sqrt((curNode.X - TriNode2.X) * (curNode.X - TriNode2.X) + (curNode.Y - TriNode2.Y) * (curNode.Y - TriNode2.Y));
            double c = Math.Sqrt((TriNode1.X - TriNode2.X) * (TriNode1.X - TriNode2.X) + (TriNode1.Y - TriNode2.Y) * (TriNode1.Y - TriNode2.Y));

            double CosCur = (a * a + b * b - c * c) / (2 * a * b);
            if (CosCur >= 1 || CosCur <= -1)
            {
                CosCur = Math.Ceiling(CosCur);
            }

            double Angle = Math.Acos(CosCur);

            return Angle;
        }

        /// <summary>
        /// 获得建筑物的最短边
        /// </summary>
        /// <returns>最短边的两个节点</returns>
        public List<TriNode> GetShortestEdge()
        {
            List<TriNode> EdgeNodes = new List<TriNode>();

            double MinDis = 1000000;
            for (int i = 0; i < this.PointList.Count-1; i++)
            {
                if (i == 0)
                {
                    double Dis = Math.Sqrt((this.PointList[this.PointList.Count - 1].X - this.PointList[0].X) * (this.PointList[this.PointList.Count - 1].X - this.PointList[0].X)
                        + (this.PointList[this.PointList.Count - 1].Y - this.PointList[0].Y) * (this.PointList[this.PointList.Count - 1].Y - this.PointList[0].Y));
                    if (Dis < MinDis)
                    {
                        MinDis = Dis;
                        EdgeNodes.Clear();
                        EdgeNodes.Add(this.PointList[0]); EdgeNodes.Add(this.PointList[this.PointList.Count - 1]);
                    }
                }

                else
                {
                    double Dis = Math.Sqrt((this.PointList[i+1].X - this.PointList[i].X) * (this.PointList[i+1].X - this.PointList[i].X)
                       + (this.PointList[i+1].Y - this.PointList[i].Y) * (this.PointList[i+1].Y - this.PointList[i].Y));
                    if (Dis < MinDis)
                    {
                        MinDis = Dis;
                        EdgeNodes.Clear();
                        EdgeNodes.Add(this.PointList[i]); EdgeNodes.Add(this.PointList[i + 1]);
                    }
                }
            }

            return EdgeNodes;
        }
    }
}
