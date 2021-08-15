using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrDispalce.典型化
{
    /// <summary>
    /// 节点数据结构
    /// </summary>
    class Point
    {
        public double X; //x坐标
        public double Y; //y坐标

        bool BoundaryLabel = false;//是否是边界点
        bool IntersectedLabel = false;//是否是相交点

        public Point(double x, double y)
        {
            this.X = x;
            this.Y= y;
        }
    }
}
