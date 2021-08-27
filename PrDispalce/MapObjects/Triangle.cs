using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;


namespace PrDispalce.地图要素
{
    /// <summary>
    /// 基础三角形类
    /// </summary>
    public class Triangle
    {
        private TriDot Node_i;   //顶点
        private TriDot Node_j;   //顶点
        private TriDot Node_m;  //顶点
        /*连接三个不同的多变形，Type为3
         * 连接两个不同的多变形，Type为2
         * 多边形内部三角形，Type为1
         * 连接多边形和道路三角形，Type为0
         */
        private int type = -1;    //三角形类型

        public Triangle(TriDot dot1, TriDot dot2, TriDot dot3,int ftype)
        {
            Node_i = dot1; Node_j = dot2; Node_m = dot3;
            type = ftype;
        }

        public Triangle(TriDot dot1, TriDot dot2, TriDot dot3)
        {
            Node_i = dot1; Node_j = dot2; Node_m = dot3;
        }
        
        #region 方法
        /// <summary>
        /// 三角形面积
        /// </summary>
        /// <returns></returns>
        public double Get_TriangleArea()
        {
            double bi = Node_j.Y - Node_m.Y;
            double bj = Node_m.Y - Node_i.Y;
            double ci = Node_m.X - Node_j.X;
            double cj = Node_i.X - Node_m.X;
            
            return (bi * cj - bj * ci) / 2;
        }
        /// <summary>
        /// 根据编号获取三角形顶点
        /// </summary>
        /// <param name="i">顶点编号</param>
        /// <returns></returns>
        public TriDot GetNode(int i)
        {
            TriDot dot = null;
            if ((i >= 0) && (i <= 2))
            {
                if (i == 0)
                    dot = Node_i;
                else if (i == 1)
                    dot = Node_j;
                else if (i == 2)
                    dot = Node_m;
            }
            else
            {
                throw new Exception("超过了数值界限");
            }
            return dot;
        }
        /// <summary>
        /// 求三角形的最大角、最小角
        /// </summary>
        /// <param name="max">最大角</param>
        /// <param name="mini">最小角</param>
        public void GetMaxMiniAngle(ref double max, ref double mini)
        {

            double line1 = Math.Sqrt((Node_i.X - Node_j.X) * (Node_i.X - Node_j.X) + (Node_i.Y - Node_j.Y) * (Node_i.Y - Node_j.Y));
            double line2 = Math.Sqrt((Node_i.X - Node_m.X) * (Node_i.X - Node_m.X) + (Node_i.Y - Node_m.Y) * (Node_i.Y - Node_m.Y));
            double line3 = Math.Sqrt((Node_j.X - Node_m.X) * (Node_j.X - Node_m.X) + (Node_j.Y - Node_m.Y) * (Node_j.Y - Node_m.Y));
                
            #region 求最大角
            if ((line1 > line2) && (line1 > line3))
            {
                double cross = (Node_i.X - Node_m.X) * (Node_j.X - Node_m.X) + (Node_i.Y - Node_m.Y) * (Node_j.Y - Node_m.Y);
                double x = Math.Sqrt((Node_i.X - Node_m.X) * (Node_i.X - Node_m.X) + (Node_i.Y - Node_m.Y) * (Node_i.Y - Node_m.Y));
                double y = Math.Sqrt((Node_m.X - Node_j.X) * (Node_m.X - Node_j.X) + (Node_m.Y - Node_j.Y) * (Node_m.Y - Node_j.Y));
                double cos = cross / (x * y);
                max = Math.Acos(cos) * 180 / Math.PI;
            }
            else if ((line2 > line1) && (line2 > line3))
            {
                double cross = (Node_i.X - Node_j.X) * (Node_m.X - Node_j.X) + (Node_i.Y - Node_j.Y) * (Node_m.Y - Node_j.Y);
                double x = Math.Sqrt((Node_i.X - Node_j.X) * (Node_i.X - Node_j.X) + (Node_i.Y - Node_j.Y) * (Node_i.Y - Node_j.Y));
                double y = Math.Sqrt((Node_m.X - Node_j.X) * (Node_m.X - Node_j.X) + (Node_m.Y - Node_j.Y) * (Node_m.Y - Node_j.Y));
                double cos = cross / (x * y);
                max = Math.Acos(cos) * 180 / Math.PI;
            }
            else if ((line3 > line1) && (line3 > line2))
            {
                double cross = (Node_m.X - Node_i.X) * (Node_j.X - Node_i.X) + (Node_m.Y - Node_i.Y) * (Node_j.Y - Node_i.Y);
                double x = Math.Sqrt((Node_m.X - Node_i.X) * (Node_m.X - Node_i.X) + (Node_m.Y - Node_i.Y) * (Node_m.Y - Node_i.Y));
                double y = Math.Sqrt((Node_j.X - Node_i.X) * (Node_j.X - Node_i.X) + (Node_j.Y - Node_i.Y) * (Node_j.Y - Node_i.Y));
                double cos = cross / (x * y);
                max = Math.Acos(cos) * 180 / Math.PI;
            }
            #endregion
                
            #region 求最小角
            if ((line1 < line2) && (line1 < line3))
            {
                double cross = (Node_i.X - Node_m.X) * (Node_j.X - Node_m.X) + (Node_i.Y - Node_m.Y) * (Node_j.Y - Node_m.Y);
                double x = Math.Sqrt((Node_i.X - Node_m.X) * (Node_i.X - Node_m.X) + (Node_i.Y - Node_m.Y) * (Node_i.Y - Node_m.Y));
                double y = Math.Sqrt((Node_m.X - Node_j.X) * (Node_m.X - Node_j.X) + (Node_m.Y - Node_j.Y) * (Node_m.Y - Node_j.Y));
                double cos = cross / (x * y);
                mini = Math.Acos(cos) * 180 / Math.PI;
            }
            else if ((line2 < line1) && (line2 < line3))
            {
                double cross = (Node_i.X - Node_j.X) * (Node_m.X - Node_j.X) + (Node_i.Y - Node_j.Y) * (Node_m.Y - Node_j.Y);
                double x = Math.Sqrt((Node_i.X - Node_j.X) * (Node_i.X - Node_j.X) + (Node_i.Y - Node_j.Y) * (Node_i.Y - Node_j.Y));
                double y = Math.Sqrt((Node_m.X - Node_j.X) * (Node_m.X - Node_j.X) + (Node_m.Y - Node_j.Y) * (Node_m.Y - Node_j.Y));
                double cos = cross / (x * y);
                mini = Math.Acos(cos) * 180 / Math.PI;
            }
            else if ((line3 < line1) && (line3 < line2))
            {
                double cross = (Node_m.X - Node_i.X) * (Node_j.X - Node_i.X) + (Node_m.Y - Node_i.Y) * (Node_j.Y - Node_i.Y);
                double x = Math.Sqrt((Node_m.X - Node_i.X) * (Node_m.X - Node_i.X) + (Node_m.Y - Node_i.Y) * (Node_m.Y - Node_i.Y));
                double y = Math.Sqrt((Node_j.X - Node_i.X) * (Node_j.X - Node_i.X) + (Node_j.Y - Node_i.Y) * (Node_j.Y - Node_i.Y));
                double cos = cross / (x * y);
                mini = Math.Acos(cos) * 180 / Math.PI;
            }
            #endregion
        }
        /// <summary>
        /// 三角形的最长高
        /// </summary>
        /// <returns></returns>
        public double GetLongestHigh()
        {
            double longest = 0.0;
            List<TriDot> list1 = new List<TriDot>(); list1.Add(Node_i); list1.Add(Node_j);
            List<TriDot> list2 = new List<TriDot>(); list2.Add(Node_i); list2.Add(Node_m);
            List<TriDot> list3 = new List<TriDot>(); list3.Add(Node_j); list3.Add(Node_m);
            double high1 = Node_i.ReturnDist(list3, true);
            double high2 = Node_j.ReturnDist(list2, true);
            double high3 = Node_m.ReturnDist(list1, true);
            if ((high1 > high2) && (high1 > high3))
            {
                longest = high1;
            }
            else if ((high2 > high1) &&( high2 > high3))
            {
                longest = high2;
            }
            else if ((high3 > high2) && (high3 > high1))
            {
                longest = high3;
            }
            return longest;
        }
        public bool EndNode(TriDot dot)
        {
            bool bol = false;
            for (int i = 0; i < 3; i++)
            {
                TriDot pdot = this.GetNode(i);
                if ((pdot.X == dot.X) && (pdot.Y == dot.Y))
                {
                    bol = true;
                    break;
                }
            }
            return bol;
        }
        #endregion

        #region 属性
        public TriDot NodeI
        {
            get { return this.Node_i; }
            set { this.Node_i = value; }
        }
        public TriDot NodeJ
        {
            get { return this.Node_j; }
            set { this.Node_j = value; }
        }
        public TriDot NodeM
        {
            get { return this.Node_m; }
            set { this.Node_m = value; }
        }
        public int triType
        {
            get { return this.type; }
            set { this.type = value; }
        }
        #endregion
    }
}
