using System;
using System.Collections.Generic;
using System.Text;

namespace PrDispalce.地图要素
{
    /// <summary>
    /// 多边形顶点
    /// </summary>
    public class TriDot :Dot
    {
        public int TagValue;   //点的标识值，点所在要素的编号
        public string label = "Point";//判断该点是不是线上的点
        public int pointID = 0;//为点设置一个独一无二的标号
        public int polylineID = 0;//该点所属线的编号ID
        /// <summary>
        /// 要素类型
        /// </summary>
        public FeatureType featureType = FeatureType.Unknown;

        /// <summary>
        /// 构造函数
        /// </summary>
        public TriDot()
        {
            
        }

        public TriDot(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">坐标X</param>
        /// <param name="y">坐标Y</param>
        /// <param name="id">编号id</param>
        public TriDot(double x, double y, int id)
        {
            X = x;
            Y = y;
            ID = id;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="tagValue">点所在要素编号</param>
        /// <param name="ftype">点所在要素类型</param>
        public TriDot(double x, double y, int tagValue, FeatureType ftype)
        {
            X = x;
            Y = y;
            this.TagValue = tagValue;
            featureType = ftype;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="id">ID号</param>
        /// <param name="tagValue">点所在要素编号</param>
        /// <param name="ftype">点所在要素类型</param>
        public TriDot(double x, double y, int id, int tagValue, FeatureType ftype)
        {
            X = x;
            Y = y;
            ID = id;
            this.TagValue = tagValue;
            featureType = ftype;
        }

        public override bool Equals(object obj)
        {
            if (obj is TriDot)
            {
                TriDot dot = obj as TriDot;
                return ((dot.X == X) && (dot.Y == Y));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (X.GetHashCode() ^ Y.GetHashCode());
        }

        #region 距离计算
        /// <summary>
        /// TriNode点到TriNode点的距离
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public double ReturnDist(TriDot node)
        {
            double xn = node.X; double yn = node.Y;
            double dist = Math.Sqrt((X - xn) * (X - xn) + (Y - yn) * (Y - yn));
            return dist;
        }
        /// <summary>
        /// TriNode点到PointObject点的距离
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public double ReturnDist(PointObject point)
        {
            TriDot node = point.Point;
            double xn = node.X; double yn = node.Y;
            double dist = Math.Sqrt((X - xn) * (X - xn) + (Y - yn) * (Y - yn));
            return dist;
        }
        /// <summary>
        /// 点到多义线的最小距离
        /// </summary>
        /// <param name="polyline">线</param>
        /// <returns></returns>
        public double ReturnDist(PolylineObject polyline)
        {
            double MiniDist = 0.0;    //总体最小距离
            //多义线的点链
            List<TriDot> pointList = polyline.PointList;
            MiniDist = ReturnDist(pointList, false);
            return MiniDist;
        }
        /// <summary>
        /// 点到多边形的最短距离
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public double ReturnDist(PolygonObject polygon)
        {
            double minidist = 0.0;
            //多边形的点链
            List<TriDot> list = polygon.PointList;
            minidist = ReturnDist(list, false);
            return minidist;
        }
        /// <summary>
        /// 点到点链的最小距离,bol为true时是垂直距离
        /// </summary>
        /// <param name="pointList">点集</param>
        /// <param name="bol">是否只垂直距离</param>
        /// <returns></returns>
        public double ReturnDist(List<TriDot> pointList,bool bol)
        {
            double MiniDist = 0.0;    //总体最小距离
            //将多义线分成一段一段，求点到每段的最小距离
            int count = pointList.Count - 1;
            for (int i = 0; i < count; i++)
            {
                #region 点到每一小段的最小距离
                double minidist = 0.0;
                //多义线的分线段的两个端点
                TriDot st = pointList[i]; TriDot ed = pointList[i + 1];
                if (st.X == ed.X)                    //若线段垂直于X轴
                {
                    if (bol == true)
                    {
                        minidist = Math.Abs(X - st.X);
                    }
                    else
                    {
                        if ((Y - st.Y) * (Y - ed.Y) <= 0)//若点在线段的垂点在线段之间或在线段端点上
                        {
                            minidist = Math.Abs(X - st.X);
                        }
                        else//点在线段的垂点在线段延长线上
                        {
                            if (Math.Abs(Y - st.Y) > Math.Abs(Y - ed.Y))
                            {
                                minidist = Math.Sqrt((X - ed.X) * (X - ed.X) +
                                    (Y - ed.Y) * (Y - ed.Y));
                            }
                            else
                            {
                                minidist = Math.Sqrt((X - st.X) * (X - st.X) +
                                    (Y - st.Y) * (Y - st.Y));
                            }
                        }
                    }

                }
                else if (st.Y == ed.Y)//若线段平行于X轴
                {
                    if (bol == true)
                    {
                        minidist = Math.Abs(Y - st.Y);
                    }
                    else
                    {
                        if ((X - st.X) * (X - ed.X) <= 0)//若点在线段的垂点在线段之间或在线段端点上
                        {
                            minidist = Math.Abs(Y - st.Y);
                        }
                        else//点在线段的垂点在线段延长线上
                        {
                            if (Math.Abs(X - st.X) > Math.Abs(X - ed.X))
                            {
                                minidist = Math.Sqrt((X - ed.X) * (X - ed.X) +
                                    (Y - ed.Y) * (Y - ed.Y));
                            }
                            else
                            {
                                minidist = Math.Sqrt((X - st.X) * (X - st.X) +
                                    (Y - st.Y) * (Y - st.Y));
                            }
                        }
                    }
                }
                else//线段为斜线
                {
                    double a = (ed.Y - st.Y) / (ed.X - st.X);
                    double b1 = ed.Y - a * ed.X;
                    double b2 = Y + X / a;
                    //垂点坐标
                    double x = (b2 - b1) / (a + 1 / a);
                    double y = a * x + b1;
                    if (bol == true)
                    {
                        minidist = Math.Sqrt((x - X) * (x - X) + (y - Y) * (y - Y));
                    }
                    else
                    {
                        if ((x - st.X) * (x - ed.X) < 0)//垂点在两点之间
                        {
                            minidist = Math.Sqrt((x - X) * (x - X) + (y - Y) * (y - Y));
                        }
                        else
                        {
                            double line1 = Math.Sqrt((X - ed.X) * (X - ed.X) +
                                    (Y - ed.Y) * (Y - ed.Y));
                            double line2 = Math.Sqrt((X - st.X) * (X - st.X) +
                                    (Y - st.Y) * (Y - st.Y));
                            if (line1 < line2)
                            {
                                minidist = line1;
                            }
                            else if (line2 < line1)
                            {
                                minidist = line2;
                            }
                        }
                    }
                }
                #endregion
                if (MiniDist == 0.0)
                {
                    MiniDist = minidist;
                }
                else
                {
                    if (minidist < MiniDist)
                    {
                        MiniDist = minidist;
                    }
                }
            }
            return MiniDist;
        }
        #endregion

        #region 返回最近点
        /// <summary>
        /// 点到node的最近点
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public TriDot ReturnNearestPoint(TriDot node)
        {
            TriDot pNode = new TriDot(X, Y, ID);
            return pNode;
        }
        /// <summary>
        /// 点到po的最近点
        /// </summary>
        /// <param name="po"></param>
        /// <returns></returns>
        public TriDot ReturnNearestPoint(PointObject po)
        {
            TriDot pNode = new TriDot(X, Y, ID);
            return pNode;
        }
        /// <summary>
        /// 点到点链的最近点
        /// </summary>
        /// <param name="list">点链</param>
        /// <param name="sel">是否只取延长线上的点，true为取，false为不取</param>
        /// <returns></returns>
        public TriDot ReturnNearestPoint(List<TriDot> list, bool sel)
        {
            double MiniDist = 0.0;    //总体最小距离
            TriDot nearestNode = null;
            //将多义线分成一段一段，求点到每段的最小距离
            int count = list.Count - 1;
            for (int i = 0; i < count; i++)
            {
                #region 点到每一小段的最小距离
                double minidist = 0.0;
                //最近点的X,Y坐标
                double minX = 0.0; double minY = 0.0;
                //多义线的分线段的两个端点
                TriDot st = list[i]; TriDot ed = list[i + 1];
                if (st.X == ed.X)//若线段垂直于X轴
                {
                    if ((Y - st.Y) * (Y - ed.Y) <= 0)//若点在线段的垂点在线段之间或在线段端点上
                    {
                        minidist = Math.Abs(X - st.X);
                        minX = st.X; minY = Y;
                    }
                    else//点在线段的垂点在线段延长线上
                    {
                        if (sel == true)   //取线段延长线上的垂点
                        {
                            minidist = Math.Abs(X - st.X);
                            minX = st.X; minY = Y;
                        }
                        else              //不取线段延长线上的垂点
                        {
                            if (Math.Abs(Y - st.Y) > Math.Abs(Y - ed.Y))
                            {
                                minidist = Math.Sqrt((X - ed.X) * (X - ed.X) +
                                    (Y - ed.Y) * (Y - ed.Y));
                                minX = ed.X; minY = ed.Y;
                            }
                            else
                            {
                                minidist = Math.Sqrt((X - st.X) * (X - st.X) +
                                    (Y - st.Y) * (Y - st.Y));
                                minX = st.X; minY = st.Y;
                            }
                        }
                    }
                }
                else if (st.Y == ed.Y)//若线段平行于X轴
                {
                    if ((X - st.X) * (X - ed.X) <= 0)//若点在线段的垂点在线段之间或在线段端点上
                    {
                        minidist = Math.Abs(Y - st.Y);
                        minX = X; minY = st.Y;
                    }
                    else//点在线段的垂点在线段延长线上
                    {
                        if (sel == true)     //取线段延长线上的垂点
                        {
                            minidist = Math.Abs(Y - st.Y);
                            minX = X; minY = st.Y;
                        }
                        else                 //不取线段延长线上的垂点
                        {
                            if (Math.Abs(X - st.X) > Math.Abs(X - ed.X))
                            {
                                minidist = Math.Sqrt((X - ed.X) * (X - ed.X) +
                                    (Y - ed.Y) * (Y - ed.Y));
                                minX = ed.X; minY = ed.Y;
                            }
                            else
                            {
                                minidist = Math.Sqrt((X - st.X) * (X - st.X) +
                                    (Y - st.Y) * (Y - st.Y));
                                minX = st.X; minY = st.Y;
                            }
                        }
                    }
                }
                else//线段为斜线
                {
                    double a = (ed.Y - st.Y) / (ed.X - st.X);
                    double b1 = ed.Y - a * ed.X;
                    double b2 = Y + X / a;
                    //垂点坐标
                    double x = (b2 - b1) / (a + 1 / a);
                    double y = a * x + b1;
                    if ((x - st.X) * (x - ed.X) < 0)//垂点在两点之间
                    {
                        minidist = Math.Sqrt((x - X) * (x - X) + (y - Y) * (y - Y));
                        minX = x; minY = y;
                    }
                    else
                    {
                        if (sel == true)     //取线段延长线上的垂点
                        {
                            minidist = Math.Sqrt((x - X) * (x - X) + (y - Y) * (y - Y));
                            minX = x; minY = y;
                        }
                        else                //不取线段延长线上的垂点
                        {
                            double line1 = Math.Sqrt((X - ed.X) * (X - ed.X) +
                                    (Y - ed.Y) * (Y - ed.Y));
                            double line2 = Math.Sqrt((X - st.X) * (X - st.X) +
                                    (Y - st.Y) * (Y - st.Y));
                            if (line1 < line2)
                            {
                                minidist = line1;
                                minX = ed.X; minY = ed.Y;
                            }
                            else if (line2 < line1)
                            {
                                minidist = line2;
                                minX = st.X; minY = st.Y;
                            }
                        }
                    }
                }
                #endregion
                if (MiniDist == 0.0)
                {
                    MiniDist = minidist;
                    nearestNode = new TriDot(minX, minY);
                }
                else
                {
                    if (minidist < MiniDist)
                    {
                        MiniDist = minidist;
                        nearestNode = new TriDot(minX, minY);
                    }
                }
            }
            return nearestNode;
        }
        #endregion
    }
}
