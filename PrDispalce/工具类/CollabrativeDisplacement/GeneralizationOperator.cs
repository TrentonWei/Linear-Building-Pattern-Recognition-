using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Geometry;
using PrDispalce.地图要素;
using ESRI.ArcGIS.CartographyTools;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Geoprocessor;
using PrDispalce.工具类;
using AuxStructureLib.IO;
using AuxStructureLib.ConflictLib;

namespace PrDispalce.工具类.CollabrativeDisplacement
{
    class GeneralizationOperator
    {
        #region 两个建筑物合并操作（在polygon中添加合并建筑物，并删除合并的两个建筑物）//合并的方法是求多个建筑物的最小凸包
        public PolygonObject BuildingAggreation(PolygonObject PolygonObject1, PolygonObject PolygonObject2, IMap pMap, string name, bool OrthogonalLabel)
        {
            #region 将两个建筑物写入虚拟图层
            PrDispalce.工具类.FeatureHandle pFeatureHandle = new FeatureHandle();
            PolygonObject mPolygonObject = new PolygonObject();
            IPolygon Polygon1 = ObjectConvert(PolygonObject1);
            IPolygon Polygon2 = ObjectConvert(PolygonObject2);
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
            BuildingAggregation.out_feature_class = @"C:\Users\Administrator\Desktop\建筑物合并测试\建筑物合并\" + name;
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
            IWorkspace pWorkspace = pWorkspaceFactory.OpenFromFile(@"C:\Users\Administrator\Desktop\建筑物合并测试\建筑物合并", 0);
            IFeatureWorkspace pFeatureWorkspace = pWorkspace as IFeatureWorkspace;
            IFeatureClass pFC = pFeatureWorkspace.OpenFeatureClass(name);

            if (pFC.FeatureCount(null) > 1)
            {
                BuildingAggregation.out_feature_class = @"C:\Users\Administrator\Desktop\建筑物合并测试\建筑物合并\" + name + "non";
                BuildingAggregation.orthogonality_option = "NON_ORTHOGONAL";
                gp.Execute(BuildingAggregation, null);
                pFC = pFeatureWorkspace.OpenFeatureClass(name + "non");
            }

            IFeature FinalFeature = pFC.GetFeature(0);
            IPolygon FinalPolygon = FinalFeature.Shape as IPolygon;
            mPolygonObject = PolygonConvert(FinalPolygon);
            mPolygonObject.Type = PolygonObject1.Type;
            mPolygonObject.ID = PolygonObject1.ID;
            mPolygonObject.IDList.Add(PolygonObject1.ID);
            mPolygonObject.IDList.Add(PolygonObject2.ID);
            mPolygonObject.IDList = mPolygonObject.IDList.Distinct().ToList();//去重
            return mPolygonObject;
            #endregion
        }
        #endregion

        /// <summary>
        /// 将polygonobject转换成polygon
        /// </summary>
        /// <param name="pPolygonObject"></param>
        /// <returns></returns>
        public IPolygon ObjectConvert(PolygonObject pPolygonObject)
        {
            Ring ring1 = new RingClass();
            object missing = Type.Missing;

            IPoint curResultPoint = new PointClass();
            TriDot curPoint = null;
            if (pPolygonObject != null)
            {
                for (int i = 0; i < pPolygonObject.PointList.Count; i++)
                {
                    curPoint = pPolygonObject.PointList[i];
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    ring1.AddPoint(curResultPoint, ref missing, ref missing);
                }
            }

            IGeometryCollection pointPolygon = new PolygonClass();
            pointPolygon.AddGeometry(ring1 as IGeometry, ref missing, ref missing);
            IPolygon pPolygon = pointPolygon as IPolygon;
            pPolygon.SimplifyPreserveFromTo();
            return pPolygon;
        }

        /// <summary>
        /// polygon转换成polygonobject
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <returns></returns>
        public PolygonObject PolygonConvert(IPolygon pPolygon)
        {
            PolygonObject mPolygonObject = new PolygonObject();

            int ppID = 0;//（polygonobject自己的编号，应该无用）
            int pID = 0;//重心编号（应该无用，故没用编号）
            List<TriDot> trilist = new List<TriDot>();
            //Polygon的点集
            IPointCollection pointSet = pPolygon as IPointCollection;
            int count = pointSet.PointCount;
            double curX;
            double curY;
            //ArcGIS中，多边形的首尾点重复存储
            for (int i = 0; i < count; i++)
            {
                curX = pointSet.get_Point(i).X;
                curY = pointSet.get_Point(i).Y;
                //初始化每个点对象
                TriDot tPoint = new TriDot(curX, curY, ppID, FeatureType.PolygonType);
                trilist.Add(tPoint);
            }
            //生成自己写的多边形
            mPolygonObject = new PolygonObject(ppID, trilist, pID);

            return mPolygonObject;
        }
    }
}
