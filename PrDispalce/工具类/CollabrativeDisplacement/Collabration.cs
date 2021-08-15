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
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using AuxStructureLib;
using PrDispalce.工具类.CollabrativeDisplacement;
using PrDispalce.工具类;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.CartographyTools;

namespace PrDispalce.工具类.CollabrativeDisplacement
{
    class Collabration
    {
        /// <summary>
        /// 协同操作
        /// </summary>
        /// <param name="pTargetEdge"></param> 需要处理的冲突边
        /// <param name="map1"></param> 需要处理的地图
        /// <param name="pMap"></param> 地图视图（聚合用）
        /// <param name="name"></param> 聚合图层名称（聚合用）
        /// <param name="oLable"></param> 聚合时是否保持直角
        /// <param name="path"></param> 聚合的路径
        public void pCollabration(ProxiEdge pTargetEdge,SMap map1,IMap pMap,bool oLable,string path)
        {
            #region 如果是建筑物与道路冲突
            if (pTargetEdge.Node1.FeatureType == FeatureType.PolylineType || pTargetEdge.Node2.FeatureType == FeatureType.PolylineType)
            {
                if (pTargetEdge.Node1.FeatureType == FeatureType.PolygonType)
                {
                    for (int p = 0; p < map1.PolygonList.Count; p++)
                    {
                        if (pTargetEdge.Node1.TagID == map1.PolygonList[p].ID)
                        {
                            //将需要变形的建筑物直接删除
                            map1.PolygonList.RemoveAt(p);
                            //map1.PolygonList[p].IsReshape = true;
                            break;
                        }
                    }
                }

                if (pTargetEdge.Node2.FeatureType == FeatureType.PolygonType)
                {
                    for (int p = 0; p < map1.PolygonList.Count; p++)
                    {
                        if (pTargetEdge.Node2.TagID == map1.PolygonList[p].ID)
                        {
                            map1.PolygonList.RemoveAt(p);
                            //map1.PolygonList[p].IsReshape = true;
                            break;
                        }
                    }
                }
            }
            #endregion

            #region 如果是建筑物间冲突
            else
            {
                #region 找到对应的两个建筑物
                PolygonObject p1 = null; PolygonObject p2 = null;
                int p1Lbael = -1; int p2Label = -1;

                for (int p = 0; p < map1.PolygonList.Count; p++)
                {                  
                    if (map1.PolygonList[p].ID == pTargetEdge.Node1.TagID)
                    {
                        p1 = map1.PolygonList[p];
                        p1Lbael = p;
                    }

                    if (map1.PolygonList[p].ID == pTargetEdge.Node2.TagID)
                    {
                        p2 = map1.PolygonList[p];
                        p2Label = p;
                    }
                }
                #endregion
              
                if (p1 != null && p2 != null)
                { 
                    #region 面积小于三倍聚合
                    if (p1.Area / p2.Area < 3 || p2.Area / p1.Area < 3)
                    {
                        PolygonObject AggregationPolygon=BuildingAggreation(p1,p2,pMap,pTargetEdge.Node1.TagID.ToString()+pTargetEdge.Node2.TagID.ToString(),oLable,null);
                        if (p1Lbael > p2Label)
                        {
                            map1.PolygonList.RemoveAt(p1Lbael);
                            map1.PolygonList[p2Label] = AggregationPolygon;
                        }

                        else
                        {
                            map1.PolygonList.RemoveAt(p2Label);
                            map1.PolygonList[p1Lbael] = AggregationPolygon;
                        }
                    }
                    #endregion

                    #region 面积大于三倍删除
                    else
                    {
                        if (p1.Area > p2.Area)
                        {
                            map1.PolygonList.RemoveAt(p2Label);
                        }

                        else
                        {
                            map1.PolygonList.RemoveAt(p1Lbael);
                        }
                    }
                    #endregion
                }
            }
            #endregion
        }

        #region 两个建筑物合并操作（在polygon中添加合并建筑物，并删除合并的两个建筑物）//合并的方法是求多个建筑物的最小凸包
        public PolygonObject BuildingAggreation(PolygonObject PolygonObject1, PolygonObject PolygonObject2, IMap pMap, string name, bool OrthogonalLabel,string path)
        {
            CommonTools cT = new CommonTools();

            #region 将两个建筑物写入虚拟图层
            PrDispalce.工具类.FeatureHandle pFeatureHandle = new FeatureHandle();
            IPolygon Polygon1 = cT.ObjectConvert(PolygonObject1);
            IPolygon Polygon2 = cT.ObjectConvert(PolygonObject2);
            IFeatureLayer pFeatureLayer = pFeatureHandle.CreatePolygonFeatureLayerInmemeory(pMap, "PolygonMemoryLayer");
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            IDataset dataset = pFeatureClass as IDataset;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit wse = workspace as IWorkspaceEdit;

            wse.StartEditing(false);
            wse.StartEditOperation();

            IFeature feature1 = pFeatureClass.CreateFeature();
            feature1.Shape = Polygon1 as IGeometry;
            feature1.Store();

            IFeature feature2 = pFeatureClass.CreateFeature();
            feature2.Shape = Polygon2 as IGeometry;
            feature2.Store();

            wse.StopEditOperation();
            wse.StopEditing(true);
            #endregion

            #region 对两个建筑物进行合并
            ESRI.ArcGIS.CartographyTools.AggregatePolygons BuildingAggregation = new AggregatePolygons();
            Geoprocessor gp = new Geoprocessor();
            gp.OverwriteOutput = true;


            BuildingAggregation.in_features = pFeatureClass;
            BuildingAggregation.out_feature_class = @"C:\Users\Administrator\Desktop\建筑物合并\" + name;
            BuildingAggregation.aggregation_distance = "5000 Meters";
            BuildingAggregation.minimum_area = "0 SquareMeters";
            BuildingAggregation.minimum_hole_size = "0 SquareMeters";
            if (OrthogonalLabel)
            {
                BuildingAggregation.orthogonality_option = "ORTHOGONAL";
            }

            else
            {
                BuildingAggregation.orthogonality_option = "NON_ORTHOGONAL";
            }


            gp.Execute(BuildingAggregation, null);
            #endregion

            #region 返回合并后的建筑物
            IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();
            IWorkspace pWorkspace = pWorkspaceFactory.OpenFromFile(@"C:\Users\Administrator\Desktop\建筑物合并", 0);
            IFeatureWorkspace pFeatureWorkspace = pWorkspace as IFeatureWorkspace;
            IFeatureClass pFC = pFeatureWorkspace.OpenFeatureClass(name);

            if (pFC.FeatureCount(null) > 1)
            {
                BuildingAggregation.out_feature_class = @"C:\Users\Administrator\Desktop\建筑物合并\" + name + "non";
                BuildingAggregation.orthogonality_option = "NON_ORTHOGONAL";
                gp.Execute(BuildingAggregation, null);
                pFC = pFeatureWorkspace.OpenFeatureClass(name + "non");
            }

            IFeature FinalFeature = pFC.GetFeature(0);
            IPolygon FinalPolygon = FinalFeature.Shape as IPolygon;
            PolygonObject mPolygonObject = cT.PolygonConvert(FinalPolygon);
            mPolygonObject.ID = PolygonObject1.ID;

            mPolygonObject.IDList.Add(PolygonObject1.ID);
            mPolygonObject.IDList.Add(PolygonObject2.ID);
            mPolygonObject.IDList = mPolygonObject.IDList.Distinct().ToList();//去重
            return mPolygonObject;
            #endregion
        }
        #endregion

        #region 典型化中两个建筑物的合并操作（在polygon中添加合并建筑物，并删除合并的两个建筑物）
        public PolygonObject BuildingAggreationInTypification(PolygonObject PolygonObject1, PolygonObject PolygonObject2, IMap pMap, string name, bool OrthogonalLabel, string path)
        {
            CommonTools cT = new CommonTools();

            #region 将两个建筑物写入虚拟图层
            PrDispalce.工具类.FeatureHandle pFeatureHandle = new FeatureHandle();
            IPolygon Polygon1 = cT.ObjectConvert(PolygonObject1);
            IPolygon Polygon2 = cT.ObjectConvert(PolygonObject2);
            IFeatureLayer pFeatureLayer = pFeatureHandle.CreatePolygonFeatureLayerInmemeory(pMap, "PolygonMemoryLayer");
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;

            IDataset dataset = pFeatureClass as IDataset;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit wse = workspace as IWorkspaceEdit;

            wse.StartEditing(false);
            wse.StartEditOperation();

            IFeature feature1 = pFeatureClass.CreateFeature();
            feature1.Shape = Polygon1 as IGeometry;
            feature1.Store();

            IFeature feature2 = pFeatureClass.CreateFeature();
            feature2.Shape = Polygon2 as IGeometry;
            feature2.Store();

            wse.StopEditOperation();
            wse.StopEditing(true);
            #endregion

            #region 对两个建筑物进行合并
            ESRI.ArcGIS.CartographyTools.AggregatePolygons BuildingAggregation = new AggregatePolygons();
            Geoprocessor gp = new Geoprocessor();
            gp.OverwriteOutput = true;

            BuildingAggregation.in_features = pFeatureClass;
            BuildingAggregation.out_feature_class = @"C:\Users\10988\Desktop\实验\建筑物合并\" + name;
            BuildingAggregation.aggregation_distance = "10 Meters";
            BuildingAggregation.minimum_area = "0 SquareMeters";
            BuildingAggregation.minimum_hole_size = "0 SquareMeters";
            if (OrthogonalLabel)
            {
                BuildingAggregation.orthogonality_option = "ORTHOGONAL";
            }

            else
            {
                BuildingAggregation.orthogonality_option = "NON_ORTHOGONAL";
            }


            gp.Execute(BuildingAggregation, null);
            #endregion

            #region 返回合并后的建筑物
            IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();
            IWorkspace pWorkspace = pWorkspaceFactory.OpenFromFile(@"C:\Users\10988\Desktop\实验\建筑物合并\", 0);
            IFeatureWorkspace pFeatureWorkspace = pWorkspace as IFeatureWorkspace;
            IFeatureClass pFC = pFeatureWorkspace.OpenFeatureClass(name);

            if (pFC.FeatureCount(null) > 1)
            {
                BuildingAggregation.out_feature_class = @"C:\Users\Administrator\Desktop\实验\建筑物合并\" + name + "non";
                BuildingAggregation.orthogonality_option = "NON_ORTHOGONAL";
                gp.Execute(BuildingAggregation, null);
                pFC = pFeatureWorkspace.OpenFeatureClass(name + "non");
            }

            IFeature FinalFeature = pFC.GetFeature(0);
            IPolygon FinalPolygon = FinalFeature.Shape as IPolygon;

            PolygonObject nPolygonObject = cT.PolygonConvert(FinalPolygon);
            PolygonObject mPolygonObject = PolygonObject1;
            mPolygonObject.PointList = nPolygonObject.PointList;
            return mPolygonObject;
            #endregion
        }
        #endregion
    }
}
