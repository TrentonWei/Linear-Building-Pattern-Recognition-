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
using ESRI.ArcGIS.DataSourcesRaster;

namespace PrDispalce.FlowMap
{
    class QuadTree
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="depth">树的深度</param>
        public QuadTree(QuadNode root, int depth)
        {
            this.Root = root;
            this.Depth = depth;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public QuadTree()
        {
            this.Root = new QuadNode();
            this.Depth = 0;
        }
        /// <summary>
        /// 获取树的根节点
        /// </summary>
        public QuadNode Root { get; set; }
        /// <summary>
        /// 获取树的深度
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// 构建四叉树
        /// </summary>
        /// <param name="depth">深度</param>
        /// <param name="rect">节点的矩形</param>
        /// <param name="list">数组</param>
        /// <returns></returns>
        public QuadTree BuildQuadtree(int depth, Envelope rect, List<IPoint> PointList)
        {
            QuadTree tree = new QuadTree();
            tree.Depth = depth;
            tree.Root.Code = "C";//初始化编码是空
            this.BuildSubTree(tree.Root, depth, rect, PointList);
            return tree;
        }

        /// <summary>
        /// 构建四叉子树
        /// </summary>
        /// <param name="node">父节点</param>
        /// <param name="depth">深度</param>
        /// <param name="rect">矩形</param>
        /// <param name="list">数组</param>
        private void BuildSubTree(QuadNode node, int depth, Envelope rect, List<IPoint> PointList)
        {
            if (depth != 0)
            {
                List<IPoint> PointInRect1 = this.GetPointInRect(PointList, rect);
                if (PointInRect1.Count <= 1)
                {
                    //这个区域全部相同的话就不用继续分割了
                    node.Rectangle = rect;
                    node.Code = node.Code;
                    node.Value = PointInRect1;
                }
                else
                {
                    //执行分割
                    node.Childs = new QuadNode[4] { new QuadNode(), new QuadNode(), new QuadNode(), new QuadNode() };
                    Envelope[] subRectangles = DivideRectange(rect);//将传入的矩形进行分割

                    #region 执行分割
                    List<IPoint> PointInRect2 = this.GetPointInRect(PointList, subRectangles[0]);
                    if (PointInRect2.Count <= 1)
                    {
                        //如果这个区域像素值全相等
                        node.Childs[0].Code = node.Code + "0";//编码为父节点的编码+当前区域编码
                        node.Childs[0].Rectangle = subRectangles[0];//子节点的矩形设置
                        node.Value = PointInRect2;
                    }
                    else
                    {
                        node.Childs[0].Code = node.Code + "0";//设置编码，供子节点使用
                        BuildSubTree(node.Childs[0], depth - 1, subRectangles[0], PointList);//递归调用创建编码函数
                    }

                    List<IPoint> PointInRect3 = this.GetPointInRect(PointList, subRectangles[1]);
                    if (PointInRect3.Count <= 1)
                    {

                        node.Childs[1].Code = node.Code + "1";
                        node.Childs[1].Rectangle = subRectangles[1];
                        node.Value = PointInRect3;
                    }
                    else
                    {
                        node.Childs[1].Code = node.Code + "1";
                        BuildSubTree(node.Childs[1], depth - 1, subRectangles[1], PointList);
                    }
                    List<IPoint> PointInRect4 = this.GetPointInRect(PointList, subRectangles[2]);
                    if (PointInRect4.Count <= 1)
                    {
                        node.Childs[2].Code = node.Code + "2";
                        node.Childs[2].Rectangle = subRectangles[2];
                        node.Value = PointInRect4;
                    }
                    else
                    {

                        node.Childs[2].Code = node.Code + "2";
                        BuildSubTree(node.Childs[2], depth - 1, subRectangles[2], PointList);
                    }
                    List<IPoint> PointInRect5 = this.GetPointInRect(PointList, subRectangles[3]);
                    if (PointInRect5.Count <= 1)
                    {
                        node.Childs[3].Code = node.Code + "3";
                        node.Childs[3].Rectangle = subRectangles[3];
                        node.Value = PointInRect5;
                    }
                    else
                    {
                        node.Childs[3].Code = node.Code + "3";
                        BuildSubTree(node.Childs[3], depth - 1, subRectangles[3], PointList);
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// 获得给定矩形中的点集
        /// </summary>
        /// <param name="PointList"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        public List<IPoint> GetPointInRect(List<IPoint> PointList, Envelope rect)
        {
            List<IPoint> ReturnPoints = new List<IPoint>();

            for (int i = 0; i < PointList.Count; i++)
            {
                double Point_X = PointList[i].X;
                double Point_Y = PointList[i].Y;

                if (Point_X > rect.XMin && Point_Y > rect.YMin && Point_X < rect.XMax && Point_Y < rect.YMax)
                {
                    ReturnPoints.Add(PointList[i]);
                }
            }

            return ReturnPoints;
        }

        /// <summary>
        /// 分割矩形区域
        /// </summary>
        /// <param name="rect">矩形</param>
        /// <returns>分割后的4个矩形</returns>
        private static Envelope[] DivideRectange(Envelope rect)
        {
            /* 
               |------------|-----------|
               |  [0]       |     [1]   |
               |------------|-----------|
               |  [2]       |     [3]   |
               |------------|-----------|
            */

            Envelope[] retRectangles = new Envelope[4];
            double MinX = rect.XMin; double midX = (rect.XMin + rect.XMax) / 2; double MaxX = rect.XMax;
            double MinY = rect.YMin; double midY = (rect.YMin + rect.YMax) / 2; double MaxY = rect.YMax;
            retRectangles[0].XMin = MinX; retRectangles[0].XMax = midX; retRectangles[0].YMin = midY; retRectangles[0].YMax = MaxY;
            retRectangles[1].XMin = midX; retRectangles[0].XMax = MaxX; retRectangles[0].YMin = midY; retRectangles[0].YMax = MaxY;
            retRectangles[2].XMin = MinX; retRectangles[0].XMax = midX; retRectangles[0].YMin = MinY; retRectangles[0].YMax = midY;
            retRectangles[3].XMin = midX; retRectangles[0].XMax = MaxX; retRectangles[0].YMin = MinY; retRectangles[0].YMax = midY;

            return retRectangles;
        }

        /// <summary>
        /// 遍历树
        /// </summary>
        /// <param name="node">树</param>
        /// <returns></returns>
        public static List<QuadNode> TraverseTree(QuadTree node)
        {
            List<QuadNode> list = new List<QuadNode>();
            QuadNode root = node.Root;
            if (root.Childs[0] != null)
            {
                TraverseSubTree(root.Childs[0], list);//调用遍历子树函数
            }
            if (root.Childs[1] != null)
            {
                TraverseSubTree(root.Childs[1], list);
            }
            if (root.Childs[2] != null)
            {
                TraverseSubTree(root.Childs[2], list);
            }
            if (root.Childs[3] != null)
            {
                TraverseSubTree(root.Childs[3], list);
            }
            return list;
        }

        /// <summary>
        /// 遍历子树
        /// </summary>
        /// <param name="node">树节点</param>
        /// <param name="list">list集合</param>
        private static void TraverseSubTree(QuadNode node, List<QuadNode> list)
        {
            //这个地方必须加width!=0的判断。
            if (!string.IsNullOrEmpty(node.Code) && node.Rectangle.Width != 0)
            {
                list.Add(node);
            }

            if (node.Childs[0] != null)
            {
                TraverseSubTree(node.Childs[0], list);//递归调用遍历子树
            }
            if (node.Childs[1] != null)
            {
                TraverseSubTree(node.Childs[1], list);
            }
            if (node.Childs[2] != null)
            {
                TraverseSubTree(node.Childs[2], list);
            }
            if (node.Childs[3] != null)
            {
                TraverseSubTree(node.Childs[3], list);
            }
        }
    }
}

