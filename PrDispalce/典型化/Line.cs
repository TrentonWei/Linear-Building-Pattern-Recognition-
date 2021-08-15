using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrDispalce.典型化
{
    /// <summary>
    /// 线的数据结构
    /// </summary>
    class Line
    {
        int ID;
        public List<Point> BoundaryPointList = new List<Point>();
        public List<Point> IntersectPointList = new List<Point>();
        public List<Point> TemporaryPointList=new List<Point>();

        public Line(List<Point> BoundaryPointList)
        {
            this.BoundaryPointList = BoundaryPointList;            
        }
    }
}
