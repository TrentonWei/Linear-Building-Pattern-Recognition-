using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AuxStructureLib;
using AuxStructureLib.IO;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;

namespace PrDispalce.建筑物聚合
{
    class TinUpdate
    {
        /// <summary>
        /// 获得处理涉及到的三角形
        /// </summary>
        /// <param name="NodeList">处理过程中变化的节点</param>
        /// <param name="cdt">处理的cdt</param>
        /// <returns></returns>
        public List<Triangle> GetProcessTriangle(List<TriNode> NodeList,ConsDelaunayTin cdt)
        {
            List<Triangle> ProcessTriangle = new List<Triangle>();
            for (int i = 0; i < cdt.TriangleList.Count; i++)
            {
                for (int j = 0; j < NodeList.Count; j++)
                {
                    if (cdt.TriangleList[i].ContainPoint(NodeList[j]))
                    {
                        ProcessTriangle.Add(cdt.TriangleList[i]);
                    }
                }
            }

            return ProcessTriangle;
        }

        /// <summary>
        /// 获得后续需要继续判断的边
        /// </summary>
        /// <param name="TriangleList">处理过程中需要删除的三角形</param>
        /// <param name="cdt">处理的cdt</param>
        /// <returns></returns>
        public List<TriEdge> GetProcessEdge(List<Triangle> TriangleList,ConsDelaunayTin cdt)
        {
            List<TriEdge> ProcessEdge=new List<TriEdge>();

            #region 判断的方法：对于一个待处理的三角形，若其一条边是公共边；且不是后续待处理三角形的边，则边是后续需要处理的边
            for (int i = 0; i < TriangleList.Count; i++)
            {
                for (int j = 0; j < TriangleList[i].CommonEdgeTriangleList.Count; j++)
                {
                    if (TriangleList[i].CommonEdgeTriangleList[j].ContainEdge(TriangleList[i].edge1))
                    {
                        if (!TriangleList.Contains(TriangleList[i].CommonEdgeTriangleList[j]))
                        {
                            ProcessEdge.Add(TriangleList[i].edge1);
                        }
                    }

                    if (TriangleList[i].CommonEdgeTriangleList[j].ContainEdge(TriangleList[i].edge2))
                    {
                        if (!TriangleList.Contains(TriangleList[i].CommonEdgeTriangleList[j]))
                        {
                            ProcessEdge.Add(TriangleList[i].edge2);
                        }
                    }

                    if (TriangleList[i].CommonEdgeTriangleList[j].ContainEdge(TriangleList[i].edge3))
                    {
                        if (!TriangleList.Contains(TriangleList[i].CommonEdgeTriangleList[j]))
                        {
                            ProcessEdge.Add(TriangleList[i].edge3);
                        }
                    }
                }
            }
            #endregion

            return ProcessEdge;
        }

        /// <summary>
        /// 根据需要更新三角网（删除特定的节点；删除特定的边；删除特定的三角形）
        /// </summary>
        /// <param name="NodeList"></param>
        /// <param name="TriangleList"></param>
        /// <param name="cdt"></param>
        public void DeleteDelaunay2(List<TriNode> NodeList,List<TriEdge> EdgeList,List<Triangle> TriangleList,ConsDelaunayTin cdt)
        {
            for (int i = 0; i < NodeList.Count; i++)
            {
                if (cdt.TriNodeList.Contains(NodeList[i]))
                {
                    cdt.TriNodeList.Remove(NodeList[i]);
                }
            }

            for (int i = 0; i < TriangleList.Count; i++)
            {
                if (cdt.TriangleList.Contains(TriangleList[i]))
                {
                    cdt.TriangleList.Remove(TriangleList[i]);
                }

                #region 对于需要删除的三角形，若其是已保留边的一部分，则不删除；若其已经被删除，则不再次删除
                if (!EdgeList.Contains(TriangleList[i].edge1))
                {
                    if(cdt.TriEdgeList.Contains(TriangleList[i].edge1))
                    {
                        cdt.TriEdgeList.Remove(TriangleList[i].edge1);
                    }
                }

                if (!EdgeList.Contains(TriangleList[i].edge2))
                {
                    if (cdt.TriEdgeList.Contains(TriangleList[i].edge2))
                    {
                        cdt.TriEdgeList.Remove(TriangleList[i].edge2);
                    }
                }

                if (!EdgeList.Contains(TriangleList[i].edge3))
                {
                    if (cdt.TriEdgeList.Contains(TriangleList[i].edge3))
                    {
                        cdt.TriEdgeList.Remove(TriangleList[i].edge3);
                    }
                }
                #endregion
            }
        }

        /// <summary>
        /// 根据给定的需要更新的边重新更新三角网(没有约束边的情况下)
        /// </summary>
        /// <param name="TriEdgeList"></param>
        /// <param name="cdt"></param>
        public List<Triangle>  ReBuildDelaunay2(List<TriEdge> TriEdgeList,ConsDelaunayTin cdt)
        {
            List<Triangle> TriangleList = new List<Triangle>();

            while (TriEdgeList.Count != 0)//边的条数不等于0
            {
                TriEdge edge = TriEdgeList[0];
                TriNode point2 = new TriNode();
                point2 = TriEdge.GetBestPoint2(edge, cdt.TriNodeList);//获得最佳的第三点

                #region 加入一个三角形
                if (point2 != null)
                {
                    Triangle triangle = new Triangle(edge.startPoint, point2, edge.endPoint);

                    //避免数据精度导致重复加入已存在三角形而陷入死循环的情况
                    if (triangle.isContainedInTris2(TriangleList))
                    {
                        TriEdgeList.Remove(edge);
                        continue;
                    }

                    //附加一个判断条件（如果与已有的三角形相交面积大于0，则也不加入）
                    if (triangle.IsIntersectAsPolygon(TriangleList))
                    {
                        TriEdgeList.Remove(edge);
                        continue;
                    }

                    //附加一个判断条件（如果三角形是在同一条直线上的三角形，则不加入）
                    if (triangle.IsOnLine())
                    {
                        TriEdgeList.Remove(edge);
                        continue;
                    }

                    TriEdge edge1 = new TriEdge(edge.startPoint, point2);
                    TriEdge edge2 = new TriEdge(point2, edge.endPoint);
                    TriEdge edge3 = new TriEdge(edge.endPoint, edge.startPoint);
                    edge1.leftTriangle = triangle;
                    edge2.leftTriangle = triangle;
                    edge3.leftTriangle = triangle;
                    triangle.edge1 = edge1;
                    triangle.edge2 = edge2;
                    triangle.edge3 = edge3;
                    edge3.rightTriangle = edge.leftTriangle;
                    edge.rightTriangle = edge3.leftTriangle;
                    TriEdgeList.Remove(edge);
                    if (!TriangleList.Contains(triangle))
                    {
                        TriangleList.Add(triangle);
                    }

                    TriEdge edgeTemp = new TriEdge();
                    edgeTemp.startPoint = edge1.endPoint;
                    edgeTemp.endPoint = edge1.startPoint;
                    TriEdge sameEdge = TriEdge.FindSameEdge(TriEdgeList, edgeTemp);
                    if (sameEdge == null)
                    {
                        TriEdgeList.Add(edge1);
                    }
                    else
                    {
                        TriEdgeList.Remove(sameEdge);
                    }
                    edgeTemp.startPoint = edge2.endPoint;
                    edgeTemp.endPoint = edge2.startPoint;
                    sameEdge = TriEdge.FindSameEdge(TriEdgeList, edgeTemp);
                    if (sameEdge == null)
                    {
                        TriEdgeList.Add(edge2);
                    }
                    else
                    {
                        TriEdgeList.Remove(sameEdge);
                    }
                }
                #endregion

                #region 移除一条边
                else
                {
                    TriEdgeList.Remove(edge);
                }
                #endregion               
            }
            
            return TriangleList;
        }
    }
}
