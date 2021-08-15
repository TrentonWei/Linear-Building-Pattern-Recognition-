using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Data;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Display;

namespace PrDispalce.工具类
{
    class ConflictDetect
    {
        #region 探测建筑物间冲突群,，并在字段中标识出来
        //若建筑物间最小距离小于某阈值，则认为该建筑物构成一个冲突团，则将其进行标记
        //列表中存放冲突的建筑物区域
        //ProximityClass V图
        //PolygonClass 建筑物图层
        //MinDis 建筑物间最小间隔
        //获取建筑物，获取与建筑物邻接的建筑物，判断冲突
        public void BuildingConflicDetect(IFeatureClass ProximityClass, IFeatureClass PolygonClass, double MinDis)
        {
            PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
            pFeatureHandle.AddField(PolygonClass, "BCID", esriFieldType.esriFieldTypeInteger);//增加一个标记字段
            List<List<int>> BuildingConflictArea = new List<List<int>>();//存放冲突的建筑物团
            int ProximityClassCount = ProximityClass.FeatureCount(null);
            int[,] matrixGraph = new int[ProximityClassCount, ProximityClassCount];

            #region 矩阵初始化
            for (int i = 0; i < ProximityClassCount; i++)
            {
                for (int j = 0; j < ProximityClassCount; j++)
                {
                    matrixGraph[i, j] = 0;
                }
            }
            #endregion

            #region 矩阵赋值（将有冲突的建筑物赋值为1）
            for (int i = 0; i < ProximityClassCount; i++)//遍历每个多边形
            {
                IFeature pfeature = ProximityClass.GetFeature(i);
                IPolygon VoronoiPart = pfeature.Shape as IPolygon;//V图多边形
                IRelationalOperator VRe = (IRelationalOperator)VoronoiPart;
                for (int j = 0; j < ProximityClassCount; j++)//与其它多边形相切
                {
                    if (j != i)
                    {
                        IFeature nfeature = ProximityClass.GetFeature(j);
                        IPolygon otherVpart = nfeature.Shape as IPolygon;//其它V图多边形
                        if (VRe.Touches(otherVpart))//V图中多边形相切
                        {
                            #region 获取V图pfeature多边形对应的建筑物
                            IFields vFields1 = pfeature.Fields;
                            int id1 = 0;

                            for (int k = 0; k < vFields1.FieldCount; k++)
                            {
                                if (vFields1.get_Field(k).Name == "Id")
                                {
                                    int field1 = vFields1.FindField("Id");
                                    id1 = (int)pfeature.get_Value(field1);
                                }
                            }
                            #endregion

                            #region 获取V图nfeature多边形对应的建筑物
                            IFields vFields2 = nfeature.Fields;
                            int id2 = 0;

                            for (int k = 0; k < vFields2.FieldCount; k++)
                            {
                                if (vFields2.get_Field(k).Name == "Id")
                                {
                                    int field1 = vFields2.FindField("Id");
                                    id2 = (int)nfeature.get_Value(field1);
                                }
                            }
                            #endregion

                            //多边形id1
                            IFeature featureI = PolygonClass.GetFeature(id1);
                            IPolygon polygonI = featureI.Shape as IPolygon;
                            //多边形id2
                            IFeature featureJ = PolygonClass.GetFeature(id2);
                            IPolygon polygonJ = featureJ.Shape as IPolygon;

                            //两个多边形间的距离
                            IProximityOperator polygonPro = polygonI as IProximityOperator;
                            double dist = polygonPro.ReturnDistance(polygonJ);
                            if (dist < MinDis)
                            {
                                matrixGraph[id1, id2] = matrixGraph[id2, id1] = 1;
                            }
                        }
                    }
                }
            }
            #endregion

            #region BuildingList初始化
            List<int> BuildingList = new List<int>();
            List<int> BuildingListLabel = new List<int>();
            for (int i = 0; i < ProximityClassCount; i++)
            {
                BuildingList.Add(i);
            }
            #endregion

            #region 分组
            //分组方法：任取一个建筑物开始，探测其周边的建筑物，若该建筑物与群中某建筑物冲突，则将其加入该团；并在原建筑物群中删除该建筑物。
            int p = 0;
            do
            {
                List<int> Cluster = new List<int>();
                BuildingListLabel.Add(BuildingList[0]);
                BuildingList.RemoveAt(0);
                p = p + 1;

                do
                {
                    int FirstFeatureLabel = BuildingListLabel[0];
                    IFeature pFeature = PolygonClass.GetFeature(FirstFeatureLabel);

                    pFeatureHandle.DataStore(PolygonClass, pFeature, "BCID", p);

                    Cluster.Add(BuildingListLabel[0]);
                    BuildingListLabel.RemoveAt(0);
                    for (int j = 0; j < ProximityClassCount; j++)
                    {
                        if (matrixGraph[FirstFeatureLabel, j] == 1)
                        {
                            BuildingListLabel.Add(j);
                            BuildingList.Remove(j);
                            matrixGraph[FirstFeatureLabel, j] = matrixGraph[j, FirstFeatureLabel] = 0;
                        }
                    }
                } while (BuildingListLabel.Count > 0);

                BuildingConflictArea.Add(Cluster);
            } while (BuildingList.Count > 0);
            #endregion

        }
        #endregion

        #region 探测与街道冲突的建筑物，并在字段中标记出来
        //若街道与建筑物间距离小于某阈值（街道符号宽度），则认为是冲突
        //ProximityClass V图
        //PolygonClass 建筑物图层
        //LineClass 道路图层
        //RoadWidth 道路宽度
        //获取建筑物，获取与建筑物邻近的街道，然后判断是否冲突，若冲突，记录冲突的街道（备注：由于街道是从0开始编号的，所以冲突街道是j+1，读取是减1读取）
        public void RoadConflictDetect(IFeatureClass ProximityClass, IFeatureClass PolygonClass, IFeatureClass LineClass, double RoadWidth)
        {
            PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
            pFeatureHandle.AddField(PolygonClass, "RoadID1", esriFieldType.esriFieldTypeInteger);
            pFeatureHandle.AddField(PolygonClass, "RoadID2", esriFieldType.esriFieldTypeInteger);
            //pFeatureHandle.AddField(PolygonClass, "RoadID3", esriFieldType.esriFieldTypeInteger);
            int ProximityClassCount = ProximityClass.FeatureCount(null);
            double thresDist = RoadWidth / 2;

            for (int i = 0; i < ProximityClassCount; i++)//遍历每个多边形
            {
                IFeature pfeature = ProximityClass.GetFeature(i);//
                IPolygon VoronoiPart = pfeature.Shape as IPolygon;//V图多边形
                IRelationalOperator VRe = (IRelationalOperator)VoronoiPart;

                int RoadConflictCount = 0;
                for (int j = 0; j < LineClass.FeatureCount(null); j++)//与道路相切或相交
                {
                    IFeature rfeature = LineClass.GetFeature(j);
                    IPolyline rpolyline = rfeature.Shape as IPolyline;//道路

                    if (VRe.Touches(rpolyline) || VRe.Crosses(rpolyline))//V图中多边形与行政区边界相切;需要保证判断的两要素投影坐标系一致
                    {
                        #region 获取V图多边形对应的建筑物
                        IFields vFields = pfeature.Fields;
                        int id1 = 0;

                        for (int k = 0; k < vFields.FieldCount; k++)
                        {
                            if (vFields.get_Field(k).Name == "Id")
                            {
                                int field1 = vFields.FindField("Id");
                                id1 = (int)pfeature.get_Value(field1);
                            }
                        }
                        #endregion

                        //建筑物多边形
                        IFeature buildfeature = PolygonClass.GetFeature(id1);
                        IPolygon build = buildfeature.Shape as IPolygon;
                        //多边形到街道的最短距离
                        IProximityOperator buildPro = (IProximityOperator)build;
                        double dis = buildPro.ReturnDistance(rpolyline);
                        if (dis < 2 * thresDist)
                        {
                            RoadConflictCount = RoadConflictCount + 1;
                            if (RoadConflictCount == 1) { pFeatureHandle.DataStore(PolygonClass, buildfeature, "RoadID1", j+1); }
                            if (RoadConflictCount == 2) { pFeatureHandle.DataStore(PolygonClass, buildfeature, "RoadID2", j+1); }
                            //if (RoadConflictCount == 3) { pFeatureHandle.DataStore(PolygonClass, buildfeature, "RoadID3", i); }
                            if (RoadConflictCount > 2) { MessageBox.Show("冲突街道个数过多"); break; }
                        }
                    }                  
                }
            }
        }
        #endregion
    }
}
