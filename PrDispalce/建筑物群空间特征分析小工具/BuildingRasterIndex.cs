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


namespace PrDispalce.建筑物群空间特征分析小工具
{
    class BuildingRasterIndex
    {
        /// <summary>
        /// 参数信息
        /// </summary>
        public Dictionary<List<int>, List<int>> GridBuilding = new Dictionary<List<int>, List<int>>();//每一个格网对应的建筑物List
        public Dictionary<int, List<int>> BuildingGrid = new Dictionary<int, List<int>>();//每一个建筑物对应的格网List
        public Dictionary<List<int>, List<double>> GridInfor = new Dictionary<List<int>, List<double>>();//存储每一个Grid对应的四点坐标

        /// <summary>
        /// 构造函数
        /// </summary>
        public BuildingRasterIndex()
        {
        }

        /// <summary>
        /// 计算给定图层的索引及对应索引的信息
        /// </summary>
        /// <param name="Layer">给定图层</param>
        /// <param name="GridSize"> 给定格网大小[正方形格网]</param>
        public void GetGrid(ILayer Layer,double GridSize)
        {
            #region 获取图层的范围
            IGeoDataset gDataset = Layer as IGeoDataset;
            IEnvelope Envelope = gDataset.Extent;

            int Dx = (int)Math.Ceiling(Envelope.Width / GridSize);
            int Dy = (int)Math.Ceiling(Envelope.Height / GridSize);
            #endregion

            #region 计算每一个Grid的信息
            for (int i = 0; i < Dx; i++)
            {
                for (int j = 0; j < Dy; j++)
                {
                    double XMin = Envelope.XMin + i * GridSize;
                    double Ymin = Envelope.YMin + j * GridSize;
                    double Xmax = XMin + GridSize;
                    double Ymax = Ymin + GridSize;

                    if (Xmax > Envelope.XMax)
                    {
                        Xmax = Envelope.XMax;
                    }

                    if (Ymax > Envelope.YMax)
                    {
                        Ymax = Envelope.YMax;
                    }

                    List<int> GridId = new List<int>(); GridId.Add(i); GridId.Add(j);//存储Grid的编号，ij
                    List<double> GridCoor = new List<double>(); GridCoor.Add(XMin); GridCoor.Add(Xmax); GridCoor.Add(Ymin); GridCoor.Add(Ymax);//存储Grid的坐标信息，Xmin，Ymin，Xmax，Ymax
                    GridInfor.Add(GridId, GridCoor);
                }
            }
            #endregion
        }

        /// <summary>
        /// 计算每个格网中的建筑物
        /// </summary>
        /// <param name="Layer"></param>
        public void GetGridBuilding(ILayer Layer)
        {
            foreach (KeyValuePair<List<int>, List<double>> kv in GridInfor)
            {
                List<int> BuildingList = new List<int>();

                IFeatureLayer bFeaturelayer = Layer as IFeatureLayer;
                if (bFeaturelayer.FeatureClass.FeatureCount(null) > 0)
                {
                    IQueryFilter queryFilter = new QueryFilterClass();
                    IFeatureCursor pFeatureCursor = bFeaturelayer.FeatureClass.Search(queryFilter, false);
                    IFeature bFeature = pFeatureCursor.NextFeature();
                    while (bFeature != null)
                    {
                        IPolygon4 pPolygon = (IPolygon4)bFeature.Shape;
                        IArea pArea = pPolygon as IArea;

                        if (pArea.Centroid.X > kv.Value[0] && pArea.Centroid.X < kv.Value[1] && pArea.Centroid.Y > kv.Value[2] && pArea.Centroid.Y < kv.Value[3])
                        {
                            BuildingList.Add((int)bFeature.get_Value(0));
                        }

                        bFeature = pFeatureCursor.NextFeature();
                    }
                }

                GridBuilding.Add(kv.Key, BuildingList);
            }
        }

        /// <summary>
        /// 计算每一个建筑物对应的Grid
        /// </summary>
        public void GetBuildingGrid()
        {
            foreach(KeyValuePair<List<int>,List<int>> kvp in GridBuilding)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    BuildingGrid.Add(kvp.Value[i], kvp.Key);
                }
            }
        }
    }
}
