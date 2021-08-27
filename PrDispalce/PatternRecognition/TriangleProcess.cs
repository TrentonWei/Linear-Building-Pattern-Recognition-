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

namespace PrDispalce.PatternRecognition
{
    class TriangleProcess
    {
        /// <summary>
        /// 判断三角形是多变形内还是多边形外
        /// </summary>
        public void LabelInOutType(List<Triangle> TriangleList, List<PolygonObject> PoList)
        {
            for (int i = 0; i < TriangleList.Count; i++)
            {
                Triangle curTri = null;
                curTri = TriangleList[i];
                int tagID = curTri.point1.TagValue;
                if (tagID == curTri.point2.TagValue && tagID == curTri.point3.TagValue)
                {
                    if (curTri.point1.FeatureType != FeatureType.PolygonType
                        || curTri.point2.FeatureType != FeatureType.PolygonType
                        || curTri.point3.FeatureType != FeatureType.PolygonType)
                    {
                        continue;
                    }

                    else//否则判断是否位于多边形内部
                    {
                        PolygonObject curPolygon = null;
                        foreach (PolygonObject polygon in PoList)
                        {
                            if (polygon.ID == tagID)
                            {
                                curPolygon = polygon;
                                break;
                            }
                        }
                        if (curPolygon == null || curPolygon.PointList.Count < 3)
                        {
                            continue;
                        }

                        else
                        {
                            TriNode p = ComFunLib.CalCenter(curTri);

                            if (ComFunLib.IsPointinPolygon(p, curPolygon.PointList))
                            {
                                curTri.InOutType = 1;
                            }
                        }

                    }
                }
            }
        }

        /// <summary>
        /// 判断三角形是多变形内还是多边形外
        /// </summary>
        public void LabelInOutType(List<Triangle> TriangleList, PolygonObject Po)
        {
            for (int i = 0; i < TriangleList.Count; i++)
            {
                Triangle curTri = null;
                curTri = TriangleList[i];
                int tagID = curTri.point1.TagValue;
                if (tagID == curTri.point2.TagValue && tagID == curTri.point3.TagValue)
                {
                    if (curTri.point1.FeatureType != FeatureType.PolygonType
                        || curTri.point2.FeatureType != FeatureType.PolygonType
                        || curTri.point3.FeatureType != FeatureType.PolygonType)
                    {
                        continue;
                    }

                    else//否则判断是否位于多边形内部
                    {
                        PolygonObject curPolygon = Po;
                        TriNode p = ComFunLib.CalCenter(curTri);

                        if (ComFunLib.IsPointinPolygon(p, curPolygon.PointList))
                        {
                            curTri.InOutType = 1;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 标记每一个三角形共边的三角形
        /// </summary>
        public void CommonEdgeTriangleLabel(List<Triangle> TriangleList)
        {
            for (int i = 0; i < TriangleList.Count; i++)
            {
                for (int j = 0; j < TriangleList.Count; j++)
                {
                    int TouchPointNum = 0;
                    if (j != i)
                    {
                        if (TriangleList[i].points.Contains(TriangleList[j].point1))
                        {
                            TouchPointNum++;
                        }

                        if (TriangleList[i].points.Contains(TriangleList[j].point2))
                        {
                            TouchPointNum++;
                        }

                        if (TriangleList[i].points.Contains(TriangleList[j].point3))
                        {
                            TouchPointNum++;
                        }
                    }

                    if (TouchPointNum >= 2)
                    {
                        TriangleList[i].CommonEdgeTriangleList.Add(TriangleList[j]);
                    }
                }
            }
        }

        /// <summary>
        /// 对传入的cdt三角形连接的建筑物个数进行编号
        /// </summary>
        public void LabelPolygonConnect(List<Triangle> TriangleList)
        {
            for (int i = 0; i < TriangleList.Count; i++)
            {
                List<int> ConnectPolygon = new List<int>();
                Triangle curTri = TriangleList[i];

                ConnectPolygon.Add(curTri.point1.TagValue);
                ConnectPolygon.Add(curTri.point2.TagValue);
                ConnectPolygon.Add(curTri.point3.TagValue);
                List<int> DistinctedConnectPolygon = ConnectPolygon.Distinct().ToList();

                int ConnectBuildingNum = DistinctedConnectPolygon.Count;
                curTri.PolygonConnectType = ConnectBuildingNum;
            }
        }

        /// <summary>
        /// 对传入的cdt三角形连接的三角形个数进行编号（需要首先执行标记多边形共边的三角形操作）
        /// </summary>
        public void LabelTriangleConnect(List<Triangle> TriangleList)
        {
            for (int i = 0; i < TriangleList.Count; i++)
            {
                TriangleList[i].TriangleConnectType = TriangleList[i].CommonEdgeTriangleList.Count;
            }
        }

        /// <summary>
        /// 对传入的cdt三角形连接的三角形个数进行编号（顾及建筑物的内外关系，即建筑物内的三角形只连接建筑物内的三角形）
        /// </summary>
        public void LabelTriangleConnectConsiderInOut(List<Triangle> TriangleList)
        {
            for (int i = 0; i < TriangleList.Count; i++)
            {
                int NumberCount = 0;
                for (int j = 0; j < TriangleList[i].CommonEdgeTriangleList.Count; j++)
                {
                    if (TriangleList[i].InOutType == TriangleList[i].CommonEdgeTriangleList[j].InOutType)
                    {
                        NumberCount++;
                    }
                }

                TriangleList[i].TriangleConnectTypeConsiderInOut = NumberCount;
            }
        }

        /// <summary>
        /// 对传入的cdt三角形是否是凹部进行编号
        /// 判断的方法：1、在多边形外部；2、只连接一个建筑物
        /// </summary>
        public void LabelDentType(List<Triangle> TriangleList)
        {
            for (int i = 0; i < TriangleList.Count; i++)
            {
                if (TriangleList[i].InOutType == 0 && TriangleList[i].PolygonConnectType == 1)
                {
                    TriangleList[i].DentType = 1;
                }
            }
        }

        /// <summary>
        /// 获取建筑物外边缘上的三角形
        /// </summary>
        /// <param name="TriangleList"></param>
        public void LabelBoundaryBuilding(List<Triangle> TriangleList)
        {
            for (int i = 0; i < TriangleList.Count; i++)
            {
                if (TriangleList[i].InOutType == 0)
                {
                    if (TriangleList[i].CommonEdgeTriangleList.Count < 3)
                    {
                        TriangleList[i].OutBoundary = 1;
                    }
                }
            }
        }

        /// <summary>
        /// 标识三角形是否是同一个侵入或侵出上的三角形
        /// </summary>
        /// <param name="curPolygon"></param>
        /// <param name="TriangleList"></param>
        public void LabelInConvexEdge(PolygonObject curPolygon, List<Triangle> TriangleList)
        {
            Dictionary<List<TriNode>, int> ConvexDic = this.GetConvexDic(curPolygon);//获得凹部与凸部的节点

            foreach (KeyValuePair<List<TriNode>, int> kv in ConvexDic)
            {
                List<Triangle> TriangleCluster = new List<Triangle>();
                for (int i = 0; i < TriangleList.Count; i++)
                {
                    if (TriangleList[i].point1.ReturnDist(kv.Key, false) < 0.000000001 &&
                        TriangleList[i].point2.ReturnDist(kv.Key, false) < 0.000000001 &&
                        TriangleList[i].point3.ReturnDist(kv.Key, false) < 0.000000001)
                    {
                        TriangleList[i].ConnectConvexEdge = 0;//标识在同一个凹部或凸部
                    }
                }

            }

        }

        /// <summary>
        /// 这个聚类针对三角形分类而言，似乎并无多大的效果！！！
        /// 对三角形进行分类（聚成一类的三角形是一个处理单元;但是只知道第一个层次的建筑物处理单元）
        /// 判断A：1、若一个三角形是内部三角形；2、判断其是否是1类三角形（只连接三角形内的一个三角形）；3、判断其邻近共边的三角形（若邻近共边三角形
        /// 是三角形内的2类三角形，则加入当前序列）；4、更新序列，判断下一个三角形；5、终止条件：遇到连接三个三角形的三角形
        /// 判断B：1、若一个三角形是外部三角形；2、判断是否是0类三角形；3、判断其是否是1类三角形（只连接三角形内的一个三角形）；4、判断其邻近共边的三角形（若邻近共边三角形
        /// 是三角形内的2类三角形，则加入当前序列）；5、更新序列，判断下一个三角形；6、终止条件：遇到连接三个三角形的三角形；碰到边缘三角形
        /// </summary>
        /// <param name="curPolygon"></param>
        /// <param name="cdt"></param>
        /// <returns></returns>
        public List<List<Triangle>> GetTriangleCluster(PolygonObject curPolygon, List<Triangle> TriangleList)
        {
            List<List<Triangle>> TriangleClusterList = new List<List<Triangle>>();
            for (int i = 0; i < TriangleList.Count; i++)
            {
                List<Triangle> TriangleCluster = new List<Triangle>();

                #region 建筑物内部的三角形
                if (TriangleList[i].InOutType == 1)
                {
                    #region 如果是1类三角形
                    if (TriangleList[i].TriangleConnectTypeConsiderInOut == 1)
                    {
                        TriangleCluster.Add(TriangleList[i]);
                        List<Triangle> Queue = new List<Triangle>(); Queue.Add(TriangleList[i]);

                        while (Queue.Count > 0)
                        {
                            for (int j = 0; j < Queue[0].CommonEdgeTriangleList.Count; j++)
                            {
                                if (Queue[0].CommonEdgeTriangleList[j].InOutType == 1 && Queue[0].CommonEdgeTriangleList[j].TriangleConnectTypeConsiderInOut == 2)
                                {
                                    TriangleCluster.Add(Queue[0].CommonEdgeTriangleList[j]);
                                }
                            }

                            Queue.RemoveAt(0);
                        }
                    }
                    #endregion
                }
                #endregion

                #region 建筑物外部的三角形
                else if (TriangleList[i].InOutType == 0)
                {
                    if (TriangleList[i].TriangleConnectTypeConsiderInOut == 0)
                    {
                        TriangleCluster.Add(TriangleList[i]);
                    }

                    if (TriangleList[i].TriangleConnectTypeConsiderInOut == 1)
                    {
                        TriangleCluster.Add(TriangleList[i]);
                        List<Triangle> Queue = new List<Triangle>(); Queue.Add(TriangleList[i]);

                        while (Queue.Count > 0)
                        {
                            for (int j = 0; j < Queue[0].CommonEdgeTriangleList.Count; j++)
                            {
                                if (Queue[0].CommonEdgeTriangleList[j].InOutType == 0 && Queue[0].CommonEdgeTriangleList[j].TriangleConnectTypeConsiderInOut == 2
                                    && Queue[0].CommonEdgeTriangleList[j].OutBoundary == 0)
                                {
                                    TriangleCluster.Add(Queue[0].CommonEdgeTriangleList[j]);
                                }
                            }

                            Queue.RemoveAt(0);
                        }
                    }
                }
                #endregion

                if (TriangleCluster.Count > 0)
                {
                    TriangleClusterList.Add(TriangleCluster);
                }
            }

            return TriangleClusterList;
        }

        /// <summary>
        /// 按照凸部与凹部划分一个建筑物可单独处理的潜在单元(考虑三角网是内插的，即三角形若是)///只获取与一个凹部或凸部全部连接的三角形
        /// </summary>
        /// <param name="curPolygon"></param>
        /// <param name="TriangleList"></param>
        /// <returns></returns>
        public List<List<Triangle>> GetTriangleCluster2(PolygonObject curPolygon, List<Triangle> TriangleList)
        {
            List<List<Triangle>> TriangleClusterList = new List<List<Triangle>>();
            Dictionary<List<TriNode>, int> ConvexDic = this.GetConvexDic(curPolygon);//获得凹部与凸部的节点

            foreach (KeyValuePair<List<TriNode>, int> kv in ConvexDic)
            {
                List<Triangle> TriangleCluster = new List<Triangle>();
                for (int i = 0; i < TriangleList.Count; i++)
                {
                    double test1 = TriangleList[i].point1.ReturnDist(kv.Key, false);
                    double test2 = TriangleList[i].point2.ReturnDist(kv.Key, false);
                    double test3 = TriangleList[i].point3.ReturnDist(kv.Key, false);

                    if (TriangleList[i].point1.ReturnDist(kv.Key, false) < 0.000000001 &&
                        TriangleList[i].point2.ReturnDist(kv.Key, false) < 0.000000001 &&
                        TriangleList[i].point3.ReturnDist(kv.Key, false) < 0.000000001)
                    {
                        TriangleCluster.Add(TriangleList[i]);
                    }
                }

                TriangleClusterList.Add(TriangleCluster);
            }

            return TriangleClusterList;
        }

        /// <summary>
        /// 按照凸部与凹部划分一个建筑物可单独处理的潜在单元（三角网不内插）【获取可处理的凹部三角形区域,不保证三角形一定是】
        /// </summary>
        /// <param name="curPolygon"></param>
        /// <param name="TriangleList"></param>
        /// <returns></returns>
        public List<List<Triangle>> GetTriangleCluster3(PolygonObject curPolygon, List<Triangle> TriangleList)
        {
            List<List<Triangle>> OperatingTriangleList = new List<List<Triangle>>();
            List<List<Triangle>> TriangleClusterList = this.GetTriangleCluster2(curPolygon, TriangleList);

            #region 获得凹部或凸部的可处理区域
            for (int i = 0; i < TriangleClusterList.Count; i++)
            {
                for (int j = 0; j < TriangleClusterList[i].Count; j++)
                {
                    #region 若其是一个内边缘三角形（目前还没有考虑三角形构成的整体是一个凸多边形的问题）
                    if (TriangleClusterList[i][j].TriangleConnectTypeConsiderInOut == 1 &&
                        TriangleClusterList[i][j].InOutType == 1)
                    {
                        List<Triangle> QueueTriangles = new List<Triangle>();
                        List<Triangle> OperatingTriangles = new List<Triangle>();
                        OperatingTriangles.Add(TriangleClusterList[i][j]);
                        QueueTriangles.Add(TriangleClusterList[i][j]);

                        while (QueueTriangles.Count > 0)
                        {
                            for (int m = 0; m < QueueTriangles[0].CommonEdgeTriangleList.Count; m++)
                            {
                                if (QueueTriangles[0].CommonEdgeTriangleList[m].InOutType == 1 &&
                                    QueueTriangles[0].CommonEdgeTriangleList[m].TriangleConnectTypeConsiderInOut <= 2
                                    && TriangleClusterList[i].Contains(QueueTriangles[0].CommonEdgeTriangleList[m]) &&
                                    !OperatingTriangles.Contains(QueueTriangles[0].CommonEdgeTriangleList[m]))
                                {
                                    OperatingTriangles.Add(QueueTriangles[0].CommonEdgeTriangleList[m]);
                                    QueueTriangles.Add(QueueTriangles[0].CommonEdgeTriangleList[m]);
                                }
                            }

                            QueueTriangles.RemoveAt(0);
                        }

                        OperatingTriangleList.Add(OperatingTriangles);
                    }
                    #endregion

                    #region 若其是一个外边缘三角形（目前还没有考虑三角形构成的整体是一个凸多边形的问题）
                    if (TriangleClusterList[i][j].TriangleConnectTypeConsiderInOut == 1 &&
                      TriangleClusterList[i][j].OutBoundary == 0 &&
                      TriangleClusterList[i][j].InOutType == 0)
                    {
                        List<Triangle> QueueTriangles = new List<Triangle>();
                        List<Triangle> OperatingTriangles = new List<Triangle>();
                        OperatingTriangles.Add(TriangleClusterList[i][j]);
                        QueueTriangles.Add(TriangleClusterList[i][j]);

                        while (QueueTriangles.Count > 0)
                        {
                            for (int m = 0; m < QueueTriangles[0].CommonEdgeTriangleList.Count; m++)
                            {
                                if (QueueTriangles[0].CommonEdgeTriangleList[m].InOutType == 0 &&
                                    QueueTriangles[0].CommonEdgeTriangleList[m].TriangleConnectTypeConsiderInOut <= 2
                                    && TriangleClusterList[i].Contains(QueueTriangles[0].CommonEdgeTriangleList[m]) &&
                                    !OperatingTriangles.Contains(QueueTriangles[0].CommonEdgeTriangleList[m]))
                                {
                                    OperatingTriangles.Add(QueueTriangles[0].CommonEdgeTriangleList[m]);
                                    QueueTriangles.Add(QueueTriangles[0].CommonEdgeTriangleList[m]);
                                }
                            }

                            QueueTriangles.RemoveAt(0);
                        }

                        OperatingTriangleList.Add(OperatingTriangles);
                    }

                    #region 单独一个三角形构成的区域
                    if (TriangleClusterList[i][j].TriangleConnectTypeConsiderInOut == 0 &&
                        TriangleClusterList[i][j].OutBoundary == 0 &&
                        TriangleClusterList[i][j].InOutType == 0)
                    {
                        List<Triangle> OperatingTriangles = new List<Triangle>();
                        OperatingTriangles.Add(TriangleClusterList[i][j]);
                        OperatingTriangleList.Add(OperatingTriangles);
                    }
                    #endregion
                    #endregion
                }
            }
            #endregion

            return OperatingTriangleList;
        }

        /// <summary>
        /// 按照凸部与凹部划分一个建筑物可单独处理的潜在单元（三角网不内插）【获取可处理的凹部三角形区域,不保证三角形一定是】
        /// </summary>
        /// <param name="curPolygon"></param>
        /// <param name="TriangleList"></param>
        /// <returns></returns>
        public List<List<Triangle>> GetTriangleCluster4(PolygonObject curPolygon, List<Triangle> TriangleList)
        {
            List<List<Triangle>> OperatingTriangleList = new List<List<Triangle>>();
            List<List<Triangle>> TriangleClusterList = this.GetTriangleCluster2(curPolygon, TriangleList);

            #region 获得凹部或凸部的可处理区域
            for (int i = 0; i < TriangleClusterList.Count; i++)
            {
                List<List<Triangle>> CacheTriangleList = new List<List<Triangle>>();
                for (int j = 0; j < TriangleClusterList[i].Count; j++)
                {
                    #region 若其是一个内边缘三角形（目前还没有考虑三角形构成的整体是一个凸多边形的问题）
                    if (TriangleClusterList[i][j].TriangleConnectTypeConsiderInOut == 1 &&
                        TriangleClusterList[i][j].InOutType == 1)
                    {
                        List<Triangle> QueueTriangles = new List<Triangle>();
                        List<Triangle> OperatingTriangles = new List<Triangle>();
                        OperatingTriangles.Add(TriangleClusterList[i][j]);
                        QueueTriangles.Add(TriangleClusterList[i][j]);

                        while (QueueTriangles.Count > 0)
                        {
                            for (int m = 0; m < QueueTriangles[0].CommonEdgeTriangleList.Count; m++)
                            {
                                if (QueueTriangles[0].CommonEdgeTriangleList[m].InOutType == 1 &&
                                    QueueTriangles[0].CommonEdgeTriangleList[m].TriangleConnectTypeConsiderInOut <= 2
                                    && TriangleClusterList[i].Contains(QueueTriangles[0].CommonEdgeTriangleList[m]) &&
                                    !OperatingTriangles.Contains(QueueTriangles[0].CommonEdgeTriangleList[m]))
                                {
                                    OperatingTriangles.Add(QueueTriangles[0].CommonEdgeTriangleList[m]);
                                    QueueTriangles.Add(QueueTriangles[0].CommonEdgeTriangleList[m]);

                                    if (!this.ConvexPolygon(OperatingTriangles))
                                    {
                                        OperatingTriangles.RemoveAt(OperatingTriangles.Count - 1);
                                        QueueTriangles.RemoveAt(QueueTriangles.Count - 1);
                                    }
                                }
                            }

                            QueueTriangles.RemoveAt(0);
                        }

                        OperatingTriangleList.Add(OperatingTriangles);
                        CacheTriangleList.Add(OperatingTriangles);
                    }
                    #endregion

                    #region 若其是一个外边缘三角形（目前还没有考虑三角形构成的整体是一个凸多边形的问题）
                    #region 非单独三角形处理区域
                    if (TriangleClusterList[i][j].TriangleConnectTypeConsiderInOut == 1 &&
                      TriangleClusterList[i][j].OutBoundary == 0 &&
                      TriangleClusterList[i][j].InOutType == 0)
                    {
                        List<Triangle> QueueTriangles = new List<Triangle>();
                        List<Triangle> OperatingTriangles = new List<Triangle>();
                        OperatingTriangles.Add(TriangleClusterList[i][j]);
                        QueueTriangles.Add(TriangleClusterList[i][j]);

                        while (QueueTriangles.Count > 0)
                        {
                            for (int m = 0; m < QueueTriangles[0].CommonEdgeTriangleList.Count; m++)
                            {
                                if (QueueTriangles[0].CommonEdgeTriangleList[m].InOutType == 0 &&
                                    QueueTriangles[0].CommonEdgeTriangleList[m].TriangleConnectTypeConsiderInOut <= 2
                                    && TriangleClusterList[i].Contains(QueueTriangles[0].CommonEdgeTriangleList[m]) &&
                                    !OperatingTriangles.Contains(QueueTriangles[0].CommonEdgeTriangleList[m]))
                                {
                                    OperatingTriangles.Add(QueueTriangles[0].CommonEdgeTriangleList[m]);
                                    QueueTriangles.Add(QueueTriangles[0].CommonEdgeTriangleList[m]);

                                    if (!this.ConvexPolygon(OperatingTriangles))
                                    {
                                        OperatingTriangles.RemoveAt(OperatingTriangles.Count - 1);
                                        QueueTriangles.RemoveAt(QueueTriangles.Count - 1);
                                    }
                                }
                            }

                            QueueTriangles.RemoveAt(0);
                        }

                        OperatingTriangleList.Add(OperatingTriangles);
                        CacheTriangleList.Add(OperatingTriangles);
                    }
                    #endregion

                    #region 单独一个三角形构成的区域
                    if (TriangleClusterList[i][j].TriangleConnectTypeConsiderInOut == 0 &&
                        TriangleClusterList[i][j].OutBoundary == 1 &&
                        TriangleClusterList[i][j].InOutType == 0)
                    {
                        List<Triangle> OperatingTriangles = new List<Triangle>();
                        OperatingTriangles.Add(TriangleClusterList[i][j]);
                        OperatingTriangleList.Add(OperatingTriangles);
                        CacheTriangleList.Add(OperatingTriangles);
                    }
                    #endregion
                    #endregion
                }

                #region 将需要合并的三角形处理单元合并
                //#region 找到合并后的三角形
                //for (int j = 0; j < TriangleClusterList[i].Count; j++)
                //{
                //    if (TriangleClusterList[i][j].TriangleConnectTypeConsiderInOut == 3)
                //    {
                //        List<List<Triangle>> TestTriangleList = new List<List<Triangle>>();

                //        for (int m = 0; m < CacheTriangleList.Count; m++)
                //        {
                //            if (CacheTriangleList[m][CacheTriangleList[m].Count - 1].CommonEdgeTriangleList.Contains(TriangleClusterList[i][j]))
                //            {
                //                TestTriangleList.Add(CacheTriangleList[m]);
                //            }
                //        }

                //        if (TestTriangleList.Count > 1)
                //        {
                //            for (int m = 0; m < TestTriangleList.Count; m++)
                //            {
                //                CacheTriangleList.Remove(TestTriangleList[m]);
                //            }

                //            TestTriangleList[0].Add(TriangleClusterList[i][j]);
                //            for (int m = 1; m < TestTriangleList.Count; m++)
                //            {
                //                TestTriangleList[0] = TestTriangleList[0].Union(TestTriangleList[m]).ToList();
                //            }
                //            CacheTriangleList.Add(TestTriangleList[0]);
                //        }
                //    }
                //}
                //#endregion

                //#region 对三角形单元做更新
                //for (int j = 0; j < CacheTriangleList.Count; j++)
                //{
                //    OperatingTriangleList.Add(CacheTriangleList[j]);
                //}
                //#endregion
                #endregion
            }
            #endregion

            return OperatingTriangleList;
        }

        /// <summary>
        /// 按照凸部与凹部划分一个建筑物可单独处理的潜在单元（三角网不内插）【获取可处理的凹部三角形区域,不保证三角形一定是】
        /// 同时，解决多个出发点出发最终合并成一个大凸包的情况
        /// </summary>
        /// <param name="curPolygon"></param>
        /// <param name="TriangleList"></param>
        /// <returns></returns>
        public List<List<Triangle>> GetTriangleCluster5(PolygonObject curPolygon, List<Triangle> TriangleList)
        {
            List<List<Triangle>> OperatingTriangleList = new List<List<Triangle>>();
            List<List<Triangle>> TriangleClusterList = this.GetTriangleCluster2(curPolygon, TriangleList);

            #region 获得凹部或凸部的可处理区域
            for (int i = 0; i < TriangleClusterList.Count; i++)
            {
                List<List<Triangle>> CacheTriangleList = new List<List<Triangle>>();
                for (int j = 0; j < TriangleClusterList[i].Count; j++)
                {
                    #region 若其是一个内边缘三角形（目前还没有考虑三角形构成的整体是一个凸多边形的问题）
                    if (TriangleClusterList[i][j].TriangleConnectTypeConsiderInOut == 1 &&
                        TriangleClusterList[i][j].InOutType == 1)
                    {
                        List<Triangle> QueueTriangles = new List<Triangle>();
                        List<Triangle> OperatingTriangles = new List<Triangle>();
                        OperatingTriangles.Add(TriangleClusterList[i][j]);
                        QueueTriangles.Add(TriangleClusterList[i][j]);

                        while (QueueTriangles.Count > 0)
                        {
                            for (int m = 0; m < QueueTriangles[0].CommonEdgeTriangleList.Count; m++)
                            {
                                if (QueueTriangles[0].CommonEdgeTriangleList[m].InOutType == 1 &&
                                    TriangleClusterList[i].Contains(QueueTriangles[0].CommonEdgeTriangleList[m]) &&
                                    !OperatingTriangles.Contains(QueueTriangles[0].CommonEdgeTriangleList[m]))
                                {
                                    OperatingTriangles.Add(QueueTriangles[0].CommonEdgeTriangleList[m]);
                                    QueueTriangles.Add(QueueTriangles[0].CommonEdgeTriangleList[m]);

                                    if (!this.ConvexPolygon(OperatingTriangles))
                                    {
                                        OperatingTriangles.RemoveAt(OperatingTriangles.Count - 1);
                                        QueueTriangles.RemoveAt(QueueTriangles.Count - 1);
                                    }
                                }
                            }

                            QueueTriangles.RemoveAt(0);
                        }

                        OperatingTriangleList.Add(OperatingTriangles);
                        CacheTriangleList.Add(OperatingTriangles);
                    }
                    #endregion

                    #region 若其是一个外边缘三角形（目前还没有考虑三角形构成的整体是一个凸多边形的问题）
                    #region 非单独三角形处理区域
                    if (TriangleClusterList[i][j].TriangleConnectTypeConsiderInOut == 1 &&
                      TriangleClusterList[i][j].OutBoundary == 0 &&
                      TriangleClusterList[i][j].InOutType == 0)
                    {
                        List<Triangle> QueueTriangles = new List<Triangle>();
                        List<Triangle> OperatingTriangles = new List<Triangle>();
                        OperatingTriangles.Add(TriangleClusterList[i][j]);
                        QueueTriangles.Add(TriangleClusterList[i][j]);

                        while (QueueTriangles.Count > 0)
                        {
                            for (int m = 0; m < QueueTriangles[0].CommonEdgeTriangleList.Count; m++)
                            {
                                if (QueueTriangles[0].CommonEdgeTriangleList[m].InOutType == 0 &&
                                    QueueTriangles[0].CommonEdgeTriangleList[m].TriangleConnectTypeConsiderInOut <= 2
                                    && TriangleClusterList[i].Contains(QueueTriangles[0].CommonEdgeTriangleList[m]) &&
                                    !OperatingTriangles.Contains(QueueTriangles[0].CommonEdgeTriangleList[m]))
                                {
                                    OperatingTriangles.Add(QueueTriangles[0].CommonEdgeTriangleList[m]);
                                    QueueTriangles.Add(QueueTriangles[0].CommonEdgeTriangleList[m]);

                                    if (!this.ConvexPolygon(OperatingTriangles))
                                    {
                                        OperatingTriangles.RemoveAt(OperatingTriangles.Count - 1);
                                        QueueTriangles.RemoveAt(QueueTriangles.Count - 1);
                                    }
                                }
                            }

                            QueueTriangles.RemoveAt(0);
                        }

                        OperatingTriangleList.Add(OperatingTriangles);
                        CacheTriangleList.Add(OperatingTriangles);
                    }
                    #endregion

                    #region 单独一个三角形构成的区域
                    if (TriangleClusterList[i][j].TriangleConnectTypeConsiderInOut == 0 &&
                        TriangleClusterList[i][j].OutBoundary == 1 &&
                        TriangleClusterList[i][j].InOutType == 0)
                    {
                        List<Triangle> OperatingTriangles = new List<Triangle>();
                        OperatingTriangles.Add(TriangleClusterList[i][j]);
                        OperatingTriangleList.Add(OperatingTriangles);
                        CacheTriangleList.Add(OperatingTriangles);
                    }
                    #endregion
                    #endregion
                }

                #region 将需要合并的三角形处理单元合并
                //#region 找到合并后的三角形
                //for (int j = 0; j < TriangleClusterList[i].Count; j++)
                //{
                //    if (TriangleClusterList[i][j].TriangleConnectTypeConsiderInOut == 3)
                //    {
                //        List<List<Triangle>> TestTriangleList = new List<List<Triangle>>();

                //        for (int m = 0; m < CacheTriangleList.Count; m++)
                //        {
                //            if (CacheTriangleList[m][CacheTriangleList[m].Count - 1].CommonEdgeTriangleList.Contains(TriangleClusterList[i][j]))
                //            {
                //                TestTriangleList.Add(CacheTriangleList[m]);
                //            }
                //        }

                //        if (TestTriangleList.Count > 1)
                //        {
                //            for (int m = 0; m < TestTriangleList.Count; m++)
                //            {
                //                CacheTriangleList.Remove(TestTriangleList[m]);
                //            }

                //            TestTriangleList[0].Add(TriangleClusterList[i][j]);
                //            for (int m = 1; m < TestTriangleList.Count; m++)
                //            {
                //                TestTriangleList[0] = TestTriangleList[0].Union(TestTriangleList[m]).ToList();
                //            }
                //            CacheTriangleList.Add(TestTriangleList[0]);
                //        }
                //    }
                //}
                //#endregion

                //#region 对三角形单元做更新
                //for (int j = 0; j < CacheTriangleList.Count; j++)
                //{
                //    OperatingTriangleList.Add(CacheTriangleList[j]);
                //}
                //#endregion
                #endregion
            }
            #endregion

            return OperatingTriangleList;
        }

        /// <summary>
        /// 返回待处理的边对应的可处理凸部区域
        /// </summary>
        /// <returns></returns>
        public List<List<Triangle>> GetTriangleClusterForGivenEdge(List<TriNode> EdgeNodes, PolygonObject curPolygon, List<Triangle> TriangleList)
        {
            List<List<Triangle>> TriangleClusterForGivenEdge = new List<List<Triangle>>();
            List<Triangle> InvovledTriangles = this.GetTrianglesForGivenEdge(EdgeNodes, TriangleList);//获得三角形对应的边

            return TriangleClusterForGivenEdge;
        }

        /// <summary>
        /// 获得给定最短边关联的三角形
        /// </summary>
        /// <param name="EdgeNodes"></param>
        /// <param name="TriangleList"></param>
        /// <returns></returns>
        public List<Triangle> GetTrianglesForGivenEdge(List<TriNode> EdgeNodes, List<Triangle> TriangleList)
        {
            List<Triangle> InvolvedTriangles = new List<Triangle>();

            for (int i = 0; i < TriangleList.Count; i++)
            {
                if (TriangleList[i].points.Contains(EdgeNodes[0]) && TriangleList[i].points.Contains(EdgeNodes[1]))
                {
                    InvolvedTriangles.Add(TriangleList[i]);
                }
            }

            return InvolvedTriangles;
        }

        /// <summary>
        /// 获取一个建筑物的凹部和凸部（获取凹部或凸部的节点）
        /// </summary>
        /// <param name="curPolygon"></param>
        /// <returns></returns>
        public Dictionary<List<TriNode>, int> GetConvexDic(PolygonObject curPolygon)
        {
            Dictionary<List<TriNode>, int> ConvexDic = new Dictionary<List<TriNode>, int>();//存储凹部或凸部的节点（1表是凸部，0表示凹部）
            int Count = 0; List<TriNode> ConvexPoints = new List<TriNode>();
            while (Count < curPolygon.BendAngle.Count)
            {
                #region 如果是第一个点
                if (Count == 0)
                {
                    ConvexPoints.Add(curPolygon.PointList[curPolygon.PointList.Count - 1]);
                    ConvexPoints.Add(curPolygon.PointList[Count]);
                    Count++;
                }
                #endregion

                #region 如果是最后一个点
                else if (Count == curPolygon.PointList.Count - 1)
                {
                    ConvexPoints.Add(curPolygon.PointList[Count]);

                    #region 如果前后两个点异号
                    if (curPolygon.BendAngle[Count][1] * curPolygon.BendAngle[Count - 1][1] < 0)
                    {
                        if (curPolygon.BendAngle[Count][1] > 0)
                        {
                            if (!ConvexDic.Keys.Contains(ConvexPoints))
                            {
                                ConvexDic.Add(ConvexPoints, 1);
                            }
                        }

                        else if (curPolygon.BendAngle[Count][1] < 0)
                        {
                            if (!ConvexDic.Keys.Contains(ConvexPoints))
                            {
                                ConvexDic.Add(ConvexPoints, 0);
                            }
                        }

                        ConvexPoints = new List<TriNode>();
                        ConvexPoints.Add(curPolygon.PointList[Count - 1]);
                        ConvexPoints.Add(curPolygon.PointList[Count]);
                    }
                    #endregion

                    #region 如果最后一个点和第一个点角度异号
                    if (curPolygon.BendAngle[Count][1] * curPolygon.BendAngle[0][1] < 0)
                    {
                        ConvexPoints.Add(curPolygon.PointList[0]);

                        if (curPolygon.BendAngle[Count][1] > 0)
                        {
                            if (!ConvexDic.Keys.Contains(ConvexPoints))
                            {
                                ConvexDic.Add(ConvexPoints, 1);
                            }
                        }

                        else if (curPolygon.BendAngle[Count][1] < 0)
                        {
                            if (!ConvexDic.Keys.Contains(ConvexPoints))
                            {
                                ConvexDic.Add(ConvexPoints, 0);
                            }
                        }
                    }
                    #endregion

                    #region 不异号
                    else
                    {
                        #region 如果字典不为空
                        if (ConvexDic.Keys.Count > 0)
                        {
                            List<TriNode> FirstConvexPoints = ConvexDic.Keys.First();
                            for (int i = ConvexPoints.Count - 1; i >= 0; i--)
                            {
                                if (!FirstConvexPoints.Contains(ConvexPoints[i]))
                                {
                                    FirstConvexPoints.Insert(0, ConvexPoints[i]);
                                }
                            }

                            ConvexDic.Remove(ConvexDic.Keys.First());

                            if (curPolygon.BendAngle[Count][1] > 0)
                            {
                                if (!ConvexDic.Keys.Contains(FirstConvexPoints))
                                {
                                    ConvexDic.Add(FirstConvexPoints, 1);
                                }
                            }

                            else if (curPolygon.BendAngle[Count][1] < 0)
                            {
                                if (!ConvexDic.Keys.Contains(FirstConvexPoints))
                                {
                                    ConvexDic.Add(FirstConvexPoints, 0);
                                }
                            }
                        }
                        #endregion

                        #region 字典为空
                        else
                        {
                            if (curPolygon.BendAngle[Count][1] > 0)
                            {
                                if (!ConvexDic.Keys.Contains(ConvexPoints))
                                {
                                    ConvexDic.Add(ConvexPoints, 1);
                                }
                            }

                            else if (curPolygon.BendAngle[Count][1] < 0)
                            {
                                if (!ConvexDic.Keys.Contains(ConvexPoints))
                                {
                                    ConvexDic.Add(ConvexPoints, 0);
                                }
                            }
                        }
                        #endregion
                    }
                    #endregion

                    Count++;
                }
                #endregion

                #region 如果不是第一个点,也非最后一个点
                else
                {
                    ConvexPoints.Add(curPolygon.PointList[Count]);

                    #region 若前后两个点角度异号
                    if (curPolygon.BendAngle[Count][1] * curPolygon.BendAngle[Count - 1][1] < 0)
                    {
                        if (curPolygon.BendAngle[Count][1] > 0)
                        {
                            if (!ConvexDic.Keys.Contains(ConvexPoints))
                            {
                                ConvexDic.Add(ConvexPoints, 1);
                            }
                        }

                        else if (curPolygon.BendAngle[Count][1] < 0)
                        {
                            if (!ConvexDic.Keys.Contains(ConvexPoints))
                            {
                                ConvexDic.Add(ConvexPoints, 0);
                            }
                        }

                        ConvexPoints = new List<TriNode>();
                        ConvexPoints.Add(curPolygon.PointList[Count - 1]);
                        ConvexPoints.Add(curPolygon.PointList[Count]);

                        #region 考虑特殊情况
                        //if (Count == curPolygon.BendAngle.Count - 2 && ConvexDic.Keys.Count == 1)
                        //{
                        //    break;
                        //}
                        #endregion
                    }
                    #endregion

                    Count++;
                }
                #endregion
            }

            #region 一个特殊处理
            //List<TriNode> CacheNodeList=new List<TriNode>();
            //int Cacheint = -1;bool SpeacialLabel = false;
            //for (int i = 0; i < ConvexDic.Keys.Count; i++)
            //{
            //    if (ConvexDic.Keys.ToList()[i].Count == curPolygon.BendAngle.Count)
            //    {
            //        CacheNodeList = ConvexDic.Keys.ToList()[i];
            //        Cacheint = ConvexDic[ConvexDic.Keys.ToList()[i]];
            //        SpeacialLabel = true;
            //        break;
            //    }
            //}

            //if (SpeacialLabel)
            //{
            //    ConvexDic.Clear(); ConvexDic.Add(CacheNodeList, Cacheint);
            //}
            #endregion

            return ConvexDic;
        }

        /// <summary>
        /// 判断当前给定的三角形单元组合是否是一个凸多边形
        /// </summary>
        /// <param name="TriangleList"></param>
        /// <returns></returns>false表示不是凸多边形；true表示是凸多边形
        public bool ConvexPolygon(List<Triangle> TriangleList)
        {
            bool ConvexPolygonLabel = false;
            PolygonObject CachePolygon = TriangleListToPolygon(TriangleList);
            CachePolygon.GetBendAngle();//在对建筑物进行分类时，一定要先计算BendAngle
            Dictionary<List<TriNode>, int> ConvexDic = GetConvexDic(CachePolygon);
            if (ConvexDic.Keys.Count > 1)
            {
                ConvexPolygonLabel = false;
            }

            else
            {
                return true;
            }

            return ConvexPolygonLabel;
        }

        /// <summary>
        /// 给定三角形，返回该三角形组成的多边形(三角形是以一定次序顺序排列的)
        /// </summary>
        /// <param name="TriangleList"></param>
        public PolygonObject TriangleListToPolygon(List<Triangle> TriangleList)
        {
            List<TriNode> PolygonTriNode = new List<TriNode>();//存储三角形构成的建筑物的多边形

            PolygonTriNode.Add(TriangleList[0].point1);
            PolygonTriNode.Add(TriangleList[0].point2);
            PolygonTriNode.Add(TriangleList[0].point3);
            for (int i = 1; i < TriangleList.Count; i++)
            {
                #region 获得相同节点的编号
                int Point1Index = -1; int Point2Index = -1; int Point3Index = -1;
                if (PolygonTriNode.Contains(TriangleList[i].point1))
                {
                    Point1Index = PolygonTriNode.IndexOf(TriangleList[i].point1);
                }
                if (PolygonTriNode.Contains(TriangleList[i].point2))
                {
                    Point2Index = PolygonTriNode.IndexOf(TriangleList[i].point2);
                }
                if (PolygonTriNode.Contains(TriangleList[i].point3))
                {
                    Point3Index = PolygonTriNode.IndexOf(TriangleList[i].point3);
                }
                #endregion

                #region 插入节点
                if (Point1Index == -1)
                {
                    #region 特殊情况
                    if ((Point2Index == 0 && Point3Index == PolygonTriNode.Count - 1) || (Point3Index == 0 && Point2Index == PolygonTriNode.Count - 1))
                    {
                        PolygonTriNode.Add(TriangleList[i].point1);
                    }
                    #endregion

                    else if (Point2Index < Point3Index)
                    {
                        PolygonTriNode.Insert(Point3Index, TriangleList[i].point1);
                    }

                    else
                    {
                        PolygonTriNode.Insert(Point2Index, TriangleList[i].point1);
                    }
                }

                if (Point2Index == -1)
                {
                    #region 特殊情况
                    if ((Point1Index == 0 && Point3Index == PolygonTriNode.Count - 1) || (Point3Index == 0 && Point1Index == PolygonTriNode.Count - 1))
                    {
                        PolygonTriNode.Add(TriangleList[i].point2);
                    }
                    #endregion

                    else if (Point1Index < Point3Index)
                    {
                        PolygonTriNode.Insert(Point3Index, TriangleList[i].point2);
                    }

                    else
                    {
                        PolygonTriNode.Insert(Point1Index, TriangleList[i].point2);
                    }
                }

                if (Point3Index == -1)
                {
                    #region 特殊情况
                    if ((Point1Index == 0 && Point2Index == PolygonTriNode.Count - 1) || (Point2Index == 0 && Point1Index == PolygonTriNode.Count - 1))
                    {
                        PolygonTriNode.Add(TriangleList[i].point3);
                    }
                    #endregion

                    else if (Point1Index < Point2Index)
                    {
                        PolygonTriNode.Insert(Point2Index, TriangleList[i].point3);
                    }

                    else
                    {
                        PolygonTriNode.Insert(Point1Index, TriangleList[i].point3);
                    }
                }
                #endregion
            }

            PolygonObject CachePolygon = new PolygonObject(0, PolygonTriNode);
            return CachePolygon;
        }
    }
}
