using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PrDispalce.地图要素;

namespace PrDispalce.工具类
{
    public enum FeatureType1
    {
        PointType1,
        PolylineType1,
        PolygonType1,
        Unknown
    }

    public abstract class Cluster
    {
        public int ID = -1;
        public int clusterLabel=0;

        public abstract FeatureType1 FeatureType
        {
            get;
        }

    }
    
    #region 点群目标
    /// <summary>
    /// 点群目标
    /// </summary>
    public class PointCluster:Cluster
    {
          public override FeatureType1 FeatureType
        {
            get { return FeatureType1.PointType1; }
        }
        public List<PointObject> pointList;

        public PointCluster(List<PointObject> mpointList, int mclusterLaber)
        {
            pointList = mpointList;
            clusterLabel = mclusterLaber;
        }
        public PointCluster()
        {
        }
        #region 初始化聚类簇
        public static List<PointCluster> initialCluster(List<PointObject> mPotObjects)
        {
            List<PointCluster> originalClusters = new List<PointCluster>();

                        for (int j = 0; j < mPotObjects.Count; j++)
                        {
                            PointObject mPoint = mPotObjects[j] ;
                            PointCluster mCluster = new PointCluster();
                            mCluster.clusterLabel = 0;
                            List<PointObject> mPointList1 = new List<PointObject>();
                            mCluster.pointList = mPotObjects;
                            mCluster.clusterLabel = mPotObjects[j].ID;  //初始类簇号为各原数据给定的ID号
                            mCluster.pointList.Add(mPoint);
                            originalClusters.Add(mCluster);
                        }
             
            return originalClusters;
        }
        #endregion
        #region 根据最短距离合并簇类
        public static List<PointCluster> mergeCluster(ref List<PointCluster> clusters, int mergeIndexA, int mergeIndexB)
        {
            PointCluster tempCluster = null, tempClusterA = null, tempClusterB = null;
            int nBreak = 0;//计算找到的次数，以提前退出循环，节省时间
            if (mergeIndexA != mergeIndexB)//分属于不同的类簇,则合并.
            {    // 将cluster[mergeIndexB]中的DataPoint加入到 cluster[mergeIndexA]				
                for (int i = 0; i < clusters.Count; i++)
                {//把mergeIndexA所在类簇提取出来。
                    tempCluster = clusters[i];//以下程序找出mergeIndexA and B所在类簇，并从链表中删除 B类簇。
                    if (tempCluster.clusterLabel == mergeIndexA)
                    {
                        nBreak++;//计算找到的次数，以提前退出循环，节省时间
                        tempClusterA = clusters[i];
                    }
                    if (tempCluster.clusterLabel == mergeIndexB)
                    {                                          //并从链表中删除 B类簇。
                        tempClusterB = clusters[i];
                        nBreak++;//计算找到的次数，以提前退出循环，节省时间		   
                        if (clusters.Count == 1)
                        {
                            clusters.Remove(clusters[i]);
                            break;
                        }
                        else if (clusters.Count > 1 && mergeIndexB == (clusters[clusters.Count - 1]).clusterLabel)
                        {
                            clusters.Remove(clusters[clusters.Count - 1]);
                        }
                        else if (clusters.Count > 1 && mergeIndexB != (clusters[clusters.Count - 1]).clusterLabel)
                        {
                            clusters.Remove(clusters[i]);
                            if (i != 0)
                                i--;
                        }
                    }//end for  if(tempCluster.clusterLabel==mergeIndexB)
                    if (nBreak == 2)
                    {//找到两个类簇，则提前退出循环，节省时间。
                        nBreak = 0;
                        break;
                    }
                }		// end for for(list<sCluster>::iterator iter
                List<PointObject> dpA = new List<PointObject>();
                dpA = tempClusterA.pointList;			//把clusterA中的数据集提取出来。
                List<PointObject> dpB = new List<PointObject>();
                dpB = tempClusterB.pointList;

                for (int j = 0; j < dpB.Count; j++)
                {
                    dpA.Add(dpB[j]);//把B中数据存入到A.
                }
                tempClusterA.pointList = dpA;
                for (int k = 0; k < clusters.Count; k++)
                {
                    tempCluster = clusters[k];
                    if (tempCluster.clusterLabel == tempClusterA.clusterLabel)
                    {
                        clusters[k] = tempClusterA;
                        break;
                    }
                }
            }
            return clusters;

        }
        #endregion
        #region 聚类分析的主函数
        public static List<PointCluster> startAnalysis(List<PointObject> dataPoints, double threshold)
        {
            List<PointCluster> finalClusters = new List<PointCluster>();
            List<PointCluster> originalClusters = new List<PointCluster>();
            originalClusters = initialCluster(dataPoints);//数据集初始化;//originalClusters为类簇序列
            finalClusters = originalClusters;
            //printCluster(originalClusters);//打印聚类情况
            PointCluster ClusterA, ClusterB;
            List<PointObject> dataobjetsA = new List<PointObject>();
            List<PointObject> dataobjetsB = new List<PointObject>();
            int clusterLabelA, clusterLabelB;
            while (true)//聚类数为ClusterNum
            {
                double minDist = 9999999.9999;//maxvalue
                int mergeIndexA = 0;
                int mergeIndexB = 0;

                for (int i = 0; i < finalClusters.Count; i++)
                {
                    for (int j = 0; j < finalClusters.Count; j++)
                    {
                        if (i != j)
                        {
                            ClusterA = finalClusters[i];
                            ClusterB = finalClusters[j];
                            dataobjetsA = ClusterA.pointList;//类簇中的数据集
                            clusterLabelA = ClusterA.clusterLabel;//类簇标号
                            dataobjetsB = ClusterB.pointList;//类簇中的数据集
                            clusterLabelB = ClusterB.clusterLabel;//类簇标号
                            for (int k = 0; k < dataobjetsA.Count; k++)
                            {
                                for (int m = 0; m < dataobjetsB.Count; m++)
                                {
                                    double tempDis = 0.0;
                                       
                                    tempDis = (dataobjetsA[k] as PointObject).GetMiniDistance( dataobjetsB[m] as PointObject);
                                               
                                         
                                    /*
                                        case FeatureType.PolygonType:
                                                 {
                                                     tempDis = (dataobjetsA[k] as PolygonObject).GetMiniDistance(dataobjetsB[m] as PolygonObject);
                                                     break;
                                                 }
                                        case FeatureType.PolylineType:
                                            {
                                                 tempDis = (dataobjetsA[k] as PolylineObject).GetMiniDistance(dataobjetsB[m] as PolylineObject);
                                                break;
                                            }

                                         default:
                                         break;

                                    */
                                    
                                    if (tempDis < minDist)
                                    {
                                        minDist = tempDis;//每次都是距离最小的两个合并。
                                        mergeIndexA = clusterLabelA;
                                        mergeIndexB = clusterLabelB;
                                    }
                                }
                            }
                        }
                    }
                }

                if (minDist >= threshold)
                    break;
                
                finalClusters = PointCluster.mergeCluster(ref finalClusters, mergeIndexA, mergeIndexB);//mergeIndexA, B 是类簇标号
            }

            return finalClusters;
        }
        #endregion
   
    }
 #endregion
    #region  线群目标
    public class PolyLineCluster:Cluster
    {
          public override FeatureType1 FeatureType
        {
            get { return FeatureType1.PolylineType1; }
        }
         public List<PolylineObject> polylineList;

         public PolyLineCluster(List<PolylineObject> mpolylineList, int mclusterLaber)
        {
             polylineList = mpolylineList;
             clusterLabel = mclusterLaber;
        }
        public PolyLineCluster()
        {
        }
        #region 初始化聚类簇
        public static List<PolyLineCluster> initialCluster(List<PolylineObject> mPolylineList)
        {
            List<PolyLineCluster> originalClusters = new List<PolyLineCluster>();

                        for (int j = 0; j < mPolylineList.Count; j++)
                        {
                            PolylineObject mPolyLine = mPolylineList[j];
                            PolyLineCluster mCluster = new PolyLineCluster();
                            mCluster.clusterLabel = 0;
                            List<PolylineObject> mPolylinelst1 = new List<PolylineObject>();
                            mCluster.polylineList = mPolylinelst1;
                            mCluster.clusterLabel = mPolylineList[j].ID;  //初始类簇号为各原数据给定的ID号
                            mCluster.polylineList.Add(mPolyLine);
                            originalClusters.Add(mCluster);
                        }        
                    
            return originalClusters;
        }
        #endregion
        #region 根据最短距离合并簇类
        public static List<PolyLineCluster> mergeCluster(ref List<PolyLineCluster> clusters, int mergeIndexA, int mergeIndexB)
        {
            PolyLineCluster tempCluster = null, tempClusterA = null, tempClusterB = null;
            int nBreak = 0;//计算找到的次数，以提前退出循环，节省时间
            if (mergeIndexA != mergeIndexB)//分属于不同的类簇,则合并.
            {    // 将cluster[mergeIndexB]中的DataPoint加入到 cluster[mergeIndexA]				
                for (int i = 0; i < clusters.Count; i++)
                {//把mergeIndexA所在类簇提取出来。
                    tempCluster = clusters[i];//以下程序找出mergeIndexA and B所在类簇，并从链表中删除 B类簇。
                    if (tempCluster.clusterLabel == mergeIndexA)
                    {
                        nBreak++;//计算找到的次数，以提前退出循环，节省时间
                        tempClusterA = clusters[i];
                    }
                    if (tempCluster.clusterLabel == mergeIndexB)
                    {                                          //并从链表中删除 B类簇。
                        tempClusterB = clusters[i];
                        nBreak++;//计算找到的次数，以提前退出循环，节省时间		   
                        if (clusters.Count == 1)
                        {
                            clusters.Remove(clusters[i]);
                            break;
                        }
                        else if (clusters.Count > 1 && mergeIndexB == (clusters[clusters.Count - 1]).clusterLabel)
                        {
                            clusters.Remove(clusters[clusters.Count - 1]);
                        }
                        else if (clusters.Count > 1 && mergeIndexB != (clusters[clusters.Count - 1]).clusterLabel)
                        {
                            clusters.Remove(clusters[i]);
                            if (i != 0)
                                i--;
                        }
                    }//end for  if(tempCluster.clusterLabel==mergeIndexB)
                    if (nBreak == 2)
                    {//找到两个类簇，则提前退出循环，节省时间。
                        nBreak = 0;
                        break;
                    }
                }		// end for for(list<sCluster>::iterator iter
                List<PolylineObject> dpA = new List<PolylineObject>();
                dpA = tempClusterA.polylineList;			//把clusterA中的数据集提取出来。
                List<PolylineObject> dpB = new List<PolylineObject>();
                dpB = tempClusterB.polylineList;

                for (int j = 0; j < dpB.Count; j++)
                {
                    dpA.Add(dpB[j]);//把B中数据存入到A.
                }
                tempClusterA.polylineList = dpA;
                for (int k = 0; k < clusters.Count; k++)
                {
                    tempCluster = clusters[k];
                    if (tempCluster.clusterLabel == tempClusterA.clusterLabel)
                    {
                        clusters[k] = tempClusterA;
                        break;
                    }
                }
            }
            return clusters;

        }
        #endregion
        #region 聚类分析的主函数
        public static List<PolyLineCluster> startAnalysis(List<PolylineObject> polineObjets, double threshold)
        {
            List<PolyLineCluster> finalClusters = new List<PolyLineCluster>();
            List<PolyLineCluster> originalClusters = new List<PolyLineCluster>();
            originalClusters = initialCluster(polineObjets);//数据集初始化;//originalClusters为类簇序列
            finalClusters = originalClusters;
            //printCluster(originalClusters);//打印聚类情况
            PolyLineCluster ClusterA, ClusterB;
            List<PolylineObject> dataobjetsA = new List<PolylineObject>();
            List<PolylineObject> dataobjetsB = new List<PolylineObject>();
            int clusterLabelA, clusterLabelB;
            while (true)//聚类数为ClusterNum
            {
                double minDist = 9999999.9999;//maxvalue
                int mergeIndexA = 0;
                int mergeIndexB = 0;

                for (int i = 0; i < finalClusters.Count; i++)
                {
                    for (int j = 0; j < finalClusters.Count; j++)
                    {
                        if (i != j)
                        {
                            ClusterA = finalClusters[i];
                            ClusterB = finalClusters[j];
                            dataobjetsA = ClusterA.polylineList;//类簇中的数据集
                            clusterLabelA = ClusterA.clusterLabel;//类簇标号
                            dataobjetsB = ClusterB.polylineList;//类簇中的数据集
                            clusterLabelB = ClusterB.clusterLabel;//类簇标号
                            for (int k = 0; k < dataobjetsA.Count; k++)
                            {
                                for (int m = 0; m < dataobjetsB.Count; m++)
                                {
                                    double tempDis = 0.0;   
                                    tempDis = (dataobjetsA[k]).GetMiniDistance( dataobjetsB[m]);
                                    if ((tempDis < minDist)&&(tempDis>0.0000001))
                                    {
                                        minDist = tempDis;//每次都是距离最小的两个合并。
                                        mergeIndexA = clusterLabelA;
                                        mergeIndexB = clusterLabelB;
                                    }
                                }
                            }
                        }
                    }
                }

                if ((minDist >= threshold)||minDist<0.0000001)
                    break;
                
                finalClusters = PolyLineCluster.mergeCluster(ref finalClusters, mergeIndexA, mergeIndexB);//mergeIndexA, B 是类簇标号
            }

            return finalClusters;
        }
        #endregion
    }
#endregion
    #region  面群目标
    public class PolygonCluster:Cluster,ICloneable
    {
          public override FeatureType1 FeatureType
        {
            get { return FeatureType1.PolygonType1; }
        }
         public List<PolygonObject> polygonList;
         public int sign = 0;
         public PolygonCluster(List<PolygonObject> mpolygonList, int mclusterLaber)
        {
             polygonList = mpolygonList;
             clusterLabel = mclusterLaber;
        }
         public PolygonCluster(List<PolygonObject> mpolygonList)
         {
             polygonList = mpolygonList;
         }
        public PolygonCluster()
        {
        }

        public object Clone()
        {
            PolygonCluster cPolygonCluster = new PolygonCluster();
            cPolygonCluster.polygonList = this.polygonList;
            cPolygonCluster.ID = this.ID;
            cPolygonCluster.clusterLabel = this.clusterLabel;
            return cPolygonCluster;
        }

        #region 初始化聚类簇
        public static List<PolygonCluster> initialCluster(List<PolygonObject> mPolygonList)
        {
            List<PolygonCluster> originalClusters = new List<PolygonCluster>();

                        for (int j = 0; j <mPolygonList .Count; j++)
                        {
                            PolygonObject mPolygon = mPolygonList[j];
                            PolygonCluster mCluster = new PolygonCluster();
                            mCluster.clusterLabel = 0;
                            List<PolygonObject> mPolygonList1 = new List<PolygonObject>();
                            mCluster.polygonList = mPolygonList1;
                            mCluster.clusterLabel = mPolygonList[j].ID;  //初始类簇号为各原数据给定的ID号
                            mCluster.polygonList.Add(mPolygon);
                            originalClusters.Add(mCluster);
                        }        
            return originalClusters;
        }
        #endregion
        #region 根据最短距离合并簇类
        public static List<PolygonCluster> mergeCluster(ref List<PolygonCluster> clusters, int mergeIndexA, int mergeIndexB)
        {
            PolygonCluster tempCluster = null, tempClusterA = null, tempClusterB = null;
            int nBreak = 0;//计算找到的次数，以提前退出循环，节省时间
            if (mergeIndexA != mergeIndexB)//分属于不同的类簇,则合并.
            {    // 将cluster[mergeIndexB]中的DataPoint加入到 cluster[mergeIndexA]				
                for (int i = 0; i < clusters.Count; i++)
                {//把mergeIndexA所在类簇提取出来。
                    tempCluster = clusters[i];//以下程序找出mergeIndexA and B所在类簇，并从链表中删除 B类簇。
                    if (tempCluster.clusterLabel == mergeIndexA)
                    {
                        nBreak++;//计算找到的次数，以提前退出循环，节省时间
                        tempClusterA = clusters[i];
                    }
                    if (tempCluster.clusterLabel == mergeIndexB)
                    {                                          //并从链表中删除 B类簇。
                        tempClusterB = clusters[i];
                        nBreak++;//计算找到的次数，以提前退出循环，节省时间		   
                        if (clusters.Count == 1)
                        {
                            clusters.Remove(clusters[i]);
                            break;
                        }
                        else if (clusters.Count > 1 && mergeIndexB == (clusters[clusters.Count - 1]).clusterLabel)
                        {
                            clusters.Remove(clusters[clusters.Count - 1]);
                        }
                        else if (clusters.Count > 1 && mergeIndexB != (clusters[clusters.Count - 1]).clusterLabel)
                        {
                            clusters.Remove(clusters[i]);
                            if (i != 0)
                                i--;
                        }
                    }//end for  if(tempCluster.clusterLabel==mergeIndexB)
                    if (nBreak == 2)
                    {//找到两个类簇，则提前退出循环，节省时间。
                        nBreak = 0;
                        break;
                    }
                }		// end for for(list<sCluster>::iterator iter
                List<PolygonObject> dpA = new List<PolygonObject>();
                dpA = tempClusterA.polygonList;			//把clusterA中的数据集提取出来。
                List<PolygonObject> dpB = new List<PolygonObject>();
                dpB = tempClusterB.polygonList;

                for (int j = 0; j < dpB.Count; j++)
                {
                    dpA.Add(dpB[j]);//把B中数据存入到A.
                }
                tempClusterA.polygonList= dpA;
                for (int k = 0; k < clusters.Count; k++)
                {
                    tempCluster = clusters[k];
                    if (tempCluster.clusterLabel == tempClusterA.clusterLabel)
                    {
                        clusters[k] = tempClusterA;
                        break;
                    }
                }
            }
            return clusters;

        }
        #endregion
        #region 面聚类分析的主函数
        public static List<PolygonCluster> startAnalysis(List<PolygonObject> polgonObjets, double threshold)
        {
            List<PolygonCluster> finalClusters = new List<PolygonCluster>();
            List<PolygonCluster> originalClusters = new List<PolygonCluster>();
            originalClusters = initialCluster(polgonObjets);//数据集初始化;//originalClusters为类簇序列
            finalClusters = originalClusters;
            //printCluster(originalClusters);//打印聚类情况
            PolygonCluster ClusterA, ClusterB;
            List<PolygonObject> dataobjetsA = new List<PolygonObject>();
            List<PolygonObject> dataobjetsB = new List<PolygonObject>();
            int clusterLabelA, clusterLabelB;
            while (true)//聚类数为ClusterNum
            {
                double minDist = 9999999.9999;//maxvalue
                int mergeIndexA = 0;
                int mergeIndexB = 0;

                for (int i = 0; i < finalClusters.Count; i++)
                {
                    for (int j = 0; j < finalClusters.Count; j++)
                    {
                        if (i != j)
                        {
                            ClusterA = finalClusters[i];
                            ClusterB = finalClusters[j];
                            dataobjetsA = ClusterA.polygonList;//类簇中的数据集
                            clusterLabelA = ClusterA.clusterLabel;//类簇标号
                            dataobjetsB = ClusterB.polygonList;//类簇中的数据集
                            clusterLabelB = ClusterB.clusterLabel;//类簇标号
                            for (int k = 0; k < dataobjetsA.Count; k++)
                            {
                                for (int m = 0; m < dataobjetsB.Count; m++)
                                {
                                    double tempDis = 0.0;   
                                    tempDis = (dataobjetsA[k]).GetMiniDistance( dataobjetsB[m]);
                                    if (tempDis < minDist)
                                    {
                                        minDist = tempDis;//每次都是距离最小的两个合并。
                                        mergeIndexA = clusterLabelA;
                                        mergeIndexB = clusterLabelB;
                                    }
                                }
                            }
                        }
                    }
                }

                if (minDist >= threshold)
                    break;
                
                finalClusters = PolygonCluster.mergeCluster(ref finalClusters, mergeIndexA, mergeIndexB);//mergeIndexA, B 是类簇标号
            }

            return finalClusters;
        }
        #endregion
    }
#endregion

}

