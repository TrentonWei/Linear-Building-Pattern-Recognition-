using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using ESRI.ArcGIS.Geometry;

namespace PrDispalce.典型化
{
    public class Pattern
    {
        #region 参数
        public List<PolygonObject> PatternObjects = new List<PolygonObject>();//pattern中建筑物个数
        public List<PolygonObject> NormalizedPatternObjects = new List<PolygonObject>();
        public int PatternID = -1;//patternd的编号
        public double SiSim;//大小相似度
        public double OriSim;//方向相似度
        public double Importance;

        public bool bSiSim;
        public bool bOriSim;
        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">ID号</param>
        /// <param name="PatternObject">pattern中建筑物</param>
        public Pattern(int id, List<PolygonObject> PatternObject)
        {
            this.PatternID = id;
            this.PatternObjects = PatternObject;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">ID号</param>
        /// <param name="PatternObject">pattern中建筑物</param>
        /// <param name="SiSim">大小相似度</param>
        /// <param name="OriSim">方向相似度</param>
        public Pattern(int id, List<PolygonObject> PatternObject,bool SiSim,bool OriSim)
        {
            this.PatternID = id;
            this.PatternObjects = PatternObject;
            this.bSiSim = SiSim;
            this.bOriSim = OriSim;
        }

        /// <summary>
        /// 检验pattern中是否有冲突
        /// </summary>
        /// <returns></returns> true 代表有冲突，false代表无冲突
        public bool NoConflict(double MinDis)
        {
            bool ConflictLabel = false;
            for (int i = 0; i < this.PatternObjects.Count-1; i++)
            {
                PolygonObject po1 = this.PatternObjects[i];
                PolygonObject po2 = this.PatternObjects[i + 1];

                if (po1.GetMiniDistance(po2) < MinDis)
                {
                    ConflictLabel = true;
                }
            }

            return ConflictLabel;
        }

        /// <summary>
        /// 返回最相似的两个建筑物（Label=true）；返回最不相似的两个建筑物（Label=false）(不考虑距离)
        /// IndexLabel=1表示返回面积Index的两个建筑物；Index=2表示返回方向Index的两个建筑物
        /// BoundaryLabel=true 边界建筑物参与计算；boundaryLabel=false 边界建筑物不参与计算
        /// </summary>
        /// <param name="Label"></param>
        public List<PolygonObject> GetTwoObject(bool Label,int IndexLabel,bool BoundaryLabel)
        {
            List<PolygonObject> BuildingPair = null;
            Dictionary<int, double> SimilarityDic = new Dictionary<int,double>();
            SimilarityComputation Sc = new SimilarityComputation();

            if (IndexLabel == 1)
            {
                for (int i = 1; i < this.PatternObjects.Count - 2; i++)
                {
                    double SizSim = Sc.SizeSimilarity(this.PatternObjects[i], this.PatternObjects[i + 1]);
                    SimilarityDic.Add(i, SizSim);
                }
            }

            Dictionary<int, double> SortIndexList = SimilarityDic.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);//相似度按照升序排列（越接近1越相似；也就是越小越相似）

            #region 返回最相似的两个建筑物
            if (Label)
            {
                KeyValuePair<int, double> MostSim = SortIndexList.First();
                BuildingPair.Add(this.PatternObjects[MostSim.Key]);
                BuildingPair.Add(this.PatternObjects[MostSim.Key + 1]);
            }
            #endregion

            #region 返回最不相似的两个建筑物
            else
            {
                KeyValuePair<int, double> MostDisSim = SortIndexList.Last();
                BuildingPair.Add(this.PatternObjects[MostDisSim.Key]);
                BuildingPair.Add(this.PatternObjects[MostDisSim.Key + 1]);
            }
            #endregion

            return BuildingPair;
        }

        /// <summary>
        /// 返回最相似的两个建筑物（Label=true）；返回最不相似的两个建筑物（Label=false）(不考虑距离)
        /// IndexLabel=1表示返回面积Index的两个建筑物；Index=2表示返回方向Index的两个建筑物
        /// </summary> 若返回-1，则说明最相似的两个建筑物中有一个是边界建筑物
        /// <param name="Label"></param>
        public int GetTwoObjectIndex(bool Label, int IndexLabel)
        {
            if (this.PatternObjects.Count > 3)
            {
                int BuildingIndex = -1;

                Dictionary<int, double> SimilarityDic = new Dictionary<int, double>();
                SimilarityComputation Sc = new SimilarityComputation();

                if (IndexLabel == 1)
                {
                    for (int i = 1; i < this.PatternObjects.Count - 2; i++)
                    {
                        double SizSim = Sc.SizeSimilarity(this.PatternObjects[i], this.PatternObjects[i + 1]);
                        SimilarityDic.Add(i, SizSim);
                    }
                }

                Dictionary<int, double> SortIndexList = SimilarityDic.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);//相似度按照升序排列（越接近1越相似；也就是越小越相似）

                #region 返回最相似的两个建筑物（但是建筑物不能是边界上的建筑物，即保留边界上建筑物的位置在典型化中不变）
                if (Label)
                {
                    KeyValuePair<int, double> MostSim = SortIndexList.First();
                    BuildingIndex = MostSim.Key;
                }
                #endregion

                #region 返回最不相似的两个建筑物
                else
                {
                    KeyValuePair<int, double> MostDisSim = SortIndexList.Last();
                    BuildingIndex = MostDisSim.Key;
                }
                #endregion

                return BuildingIndex;
            }


            else
            {
                return -1;
            }
            
        }

        /// <summary>
        /// 将建筑物处理在一条直线上
        /// </summary>
        public void PatternNormalization()
        {
            #region 求坐标的平均值
            double Sumx = 0; double Sumy = 0;
            for (int i = 0; i < PatternObjects.Count; i++)
            {
                Sumx = Sumx + PatternObjects[i].CalProxiNode().X;
                Sumy = Sumy + PatternObjects[i].CalProxiNode().Y;
            }

            double Ax = Sumx / PatternObjects.Count; double Ay = Sumy / PatternObjects.Count;
            #endregion

            #region 求Sxx,Syy,Sxy
            double Sxx = 0; double Syy = 0; double Sxy = 0;
            for (int i = 0; i < PatternObjects.Count; i++)
            {
                Sxy = (PatternObjects[i].CalProxiNode().X - Ax) * (PatternObjects[i].CalProxiNode().Y - Ay) + Sxy;
                Sxx = (PatternObjects[i].CalProxiNode().X - Ax) * (PatternObjects[i].CalProxiNode().X - Ax) + Sxx;
                Syy = (PatternObjects[i].CalProxiNode().Y - Ay) * (PatternObjects[i].CalProxiNode().Y - Ay) + Syy;
            }
            #endregion

            #region 求k and b
            double k = 0; double b = 0;
            double k1 = -((Sxx - Syy) - Math.Sqrt((Sxx - Syy) * (Sxx - Syy) + 4 * Sxy * Sxy)) / (2 * Sxy);
            double k2 = -((Sxx - Syy) + Math.Sqrt((Sxx - Syy) * (Sxx - Syy) + 4 * Sxy * Sxy)) / (2 * Sxy);


            if (Sxx > Syy)
            {
                if (Math.Abs(k1) < 1)
                {
                    k = k1;
                    b = -k * (Ax) + Ay;
                }

                else
                {
                    k = k2;
                    b = -k * (Ax) + Ay;
                }
            }

            else
            {
                if (Math.Abs(k1) < 1)
                {
                    k = k2;
                    b = -k * (Ax) + Ay;
                }

                else
                {
                    k = k1;
                    b = -k * (Ax) + Ay;
                }
            }
            #endregion

            #region 求Normalized buildings，即垂足
            for (int i = 0; i < PatternObjects.Count; i++)
            {
                double NewX = (PatternObjects[i].CalProxiNode().X + k * PatternObjects[i].CalProxiNode().Y - k * b) / (1 + k * k);
                double NewY = (k * PatternObjects[i].CalProxiNode().X + k * k * PatternObjects[i].CalProxiNode().Y + b) / (1 + k * k);

                PolygonObject NewPolygonObject = new PolygonObject(PatternObjects[i].ID,PatternObjects[i].PointList);

                double curDx = NewX - PatternObjects[i].CalProxiNode().X;
                double curDy = NewY - PatternObjects[i].CalProxiNode().Y;

                //更新多边形点集的每一个点坐标
                foreach (TriNode curPoint in NewPolygonObject.PointList)
                {
                    curPoint.X += curDx;
                    curPoint.Y += curDy;
                }

                NormalizedPatternObjects.Add(NewPolygonObject);
            }
            #endregion
        }

        /// <summary>
        /// 获得pattern中除两侧建筑物面积最小的建筑物
        /// </summary>
        /// <returns></returns>
        public PolygonObject GetMinAreaObject()
        {
            if (this.PatternObjects.Count > 2)
            {
                PolygonObject MinPolygonObject = this.PatternObjects[1];
                for (int i = 2; i < this.PatternObjects.Count; i++)
                {
                    if (this.PatternObjects[i].Area < MinPolygonObject.Area)
                    {
                        MinPolygonObject = this.PatternObjects[i];
                    }
                }

                return MinPolygonObject;
            }

            else
            {
                return null;
            }
        }

        /// <summary>
        /// 获得pattern的长度
        /// </summary>
        /// <returns></returns>
        public double GetPatternDistance()
        {
            double SumDistance = 0;
            for (int i = 0; i < this.PatternObjects.Count - 1; i++)
            {
                double Distance = Math.Sqrt((PatternObjects[i + 1].CalProxiNode().X - PatternObjects[i].CalProxiNode().X) * (PatternObjects[i + 1].CalProxiNode().X - PatternObjects[i].CalProxiNode().X)
                    + (PatternObjects[i + 1].CalProxiNode().Y - PatternObjects[i].CalProxiNode().Y) * (PatternObjects[i + 1].CalProxiNode().Y - PatternObjects[i].CalProxiNode().Y));
                SumDistance = SumDistance + Distance;
            }

            return SumDistance;
        }

        /// <summary>
        /// 依次减少一个建筑物，计算建筑物的新位置
        /// </summary>
        /// <returns></returns>
        public List<List<double>> GetPolygonLocation()
        {
            List<List<double>> LocationList = new List<List<double>>();//返回的坐标序列
            double AverageDis = this.GetPatternDistance() / (PatternObjects.Count - 2);//平均距离
            List<double> FirstLocation = new List<double>(); FirstLocation.Add(PatternObjects[0].CalProxiNode().X);FirstLocation.Add(PatternObjects[0].CalProxiNode().Y);
            List<double> LastLocation = new List<double>(); LastLocation.Add(PatternObjects[PatternObjects.Count-1].CalProxiNode().X); LastLocation.Add(PatternObjects[PatternObjects.Count-1].CalProxiNode().Y);
            LocationList.Add(FirstLocation); LocationList.Add(LastLocation);

            List<List<double>> PointList = new List<List<double>>();
            for (int i = 0; i < this.PatternObjects.Count; i++)
            {
                List<double> CacheLocation = new List<double>();
                CacheLocation.Add(this.PatternObjects[i].CalProxiNode().X);
                CacheLocation.Add(this.PatternObjects[i].CalProxiNode().Y);
                PointList.Add(CacheLocation);
            }

            if (this.PatternObjects.Count > 3)
            {
                for (int i = 1; i < PatternObjects.Count - 2; i++)
                {
                    double CacheDistance=AverageDis;
                    double Distance = Math.Sqrt((PointList[i - 1][0] - PointList[i][0]) * (PointList[i - 1][0] - PointList[i][0])
                        + (PointList[i - 1][1] - PointList[i][1]) * (PointList[i - 1][1] - PointList[i][1]));
                    
                    while(Distance < CacheDistance)
                    {
                        CacheDistance = CacheDistance - Distance;
                        PointList.RemoveAt(i-1);
                        Distance = Math.Sqrt((PointList[i - 1][0] - PointList[i][0]) * (PointList[i - 1][0] - PointList[i][0])
                        + (PointList[i - 1][1] - PointList[i][1]) * (PointList[i - 1][1] - PointList[i][1]));
                    }
                   
                    double r = CacheDistance / (Distance - CacheDistance);
                    double x = (PointList[i-1][0] + r * PointList[i][0]) / (1 + r);
                    double y = (PointList[i-1][1] + r * PointList[i][1]) / (1 + r);
                    List<double> pList = new List<double>(); pList.Add(x); pList.Add(y);
                    PointList.Insert(i, pList);
                    LocationList.Insert(i,pList);
                }

                return LocationList;
            }

            else
            {
                return LocationList;
            }
        }

        /// <summary>
        /// 将建筑物在pattern上重排，保证pattern中建筑物没有冲突
        /// </summary>
        public void PatternRelocation(double MinDis)
        {
            while (this.NoConflict(MinDis))
            {
                List<List<double>> NewLocations = this.GetPolygonLocation();
                PolygonObject MinPolygonObject = this.GetMinAreaObject();
                this.PatternObjects.Remove(MinPolygonObject);

                for (int i = 1; i < PatternObjects.Count - 1; i++)
                {
                    double dx = NewLocations[i][0] - PatternObjects[i].CalProxiNode().X;
                    double dy = NewLocations[i][1] - PatternObjects[i].CalProxiNode().Y;

                    foreach (TriNode curPoint in PatternObjects[i].PointList)
                    {
                        curPoint.X += dx;
                        curPoint.Y += dy;
                    }
                }
            }
        }

        /// <summary>
        /// 将典型化的建筑物按顺序排列
        /// </summary>
        /// <param name="MinDist"></param>
        public void SortPatternInTypification(SMap Map,ProxiGraph pg)
        {
            this.PatternBounaryBuilding(Map, pg);//确定边界建筑物

            #region 找到边缘建筑物
            PolygonObject Po = null; int BLabel = -1;
            for (int i = 0; i < this.PatternObjects.Count; i++)
            {
                if (this.PatternObjects[i].BoundaryBuilding)
                {
                    Po = this.PatternObjects[i];
                    BLabel = i;
                    break;
                }
            }
            #endregion

            PolygonObject TemporayPolygon = this.PatternObjects[0];
            this.PatternObjects[0] = Po;
            this.PatternObjects[BLabel] = TemporayPolygon;

            #region 顺序排列建筑物
            for (int i = 0; i < this.PatternObjects.Count-1; i++)
            {
                Po = this.PatternObjects[i];
                foreach (ProxiEdge Pe in pg.PgforBuildingEdgesList)
                {
                    ProxiNode Node1 = Pe.Node1;
                    ProxiNode Node2 = Pe.Node2;

                    PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                    PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                    if (Po1 == Po && this.PatternObjects.Contains(Po2))
                    {
                        int s = this.PatternObjects.IndexOf(Po2);
                        if (s > i)
                        {
                            TemporayPolygon = this.PatternObjects[i + 1];
                            this.PatternObjects[i + 1] = Po2;
                            this.PatternObjects[s] = TemporayPolygon;
                        }
                    }

                    if (Po2 == Po && this.PatternObjects.Contains(Po1))
                    {
                        int s = this.PatternObjects.IndexOf(Po1);
                        if (s > i)
                        {
                            TemporayPolygon = this.PatternObjects[i + 1];
                            this.PatternObjects[i + 1] = Po1;
                            this.PatternObjects[s] = TemporayPolygon;
                        }
                    }
                }
            }
            #endregion 
        }

        /// <summary>
        /// 将典型化的建筑物重排
        /// </summary>
        public void PatternRelocationInTypification()
        {
            int LocationInt = -1;
            double DisplaceDis=0;
            while(PatternNeedRelocationInTypification(1,out LocationInt,out DisplaceDis))
            {
                #region 靠左边界的建筑物
                if (LocationInt == 0)
                {
                    #region 计算新位置
                    double Distance = Math.Sqrt((this.PatternObjects[0].CalProxiNode().X - this.PatternObjects[1].CalProxiNode().X) * (this.PatternObjects[0].CalProxiNode().X - this.PatternObjects[1].CalProxiNode().X)
                        + (this.PatternObjects[0].CalProxiNode().Y - this.PatternObjects[1].CalProxiNode().Y) * (this.PatternObjects[0].CalProxiNode().Y - this.PatternObjects[1].CalProxiNode().Y));
                    double r = (Distance-DisplaceDis) /DisplaceDis;

                    double NewX = (this.PatternObjects[0].CalProxiNode().X + r * this.PatternObjects[1].CalProxiNode().X) / (1 + r);
                    double NewY = (this.PatternObjects[0].CalProxiNode().Y + r * this.PatternObjects[1].CalProxiNode().Y) / (1 + r);
                    #endregion

                    #region 更新坐标
                    double curDx = NewX - this.PatternObjects[1].CalProxiNode().X;
                    double curDy = NewY - this.PatternObjects[1].CalProxiNode().Y;

                    //更新多边形点集的每一个点坐标
                    foreach (TriNode curPoint in this.PatternObjects[1].PointList)
                    {
                        curPoint.X += curDx;
                        curPoint.Y += curDy;
                    }
                    #endregion
                }
                #endregion

                #region 靠右边界的建筑物
                else if (LocationInt == this.PatternObjects.Count - 2)
                {
                    #region 计算新位置
                    double Distance = Math.Sqrt((this.PatternObjects[this.PatternObjects.Count - 1].CalProxiNode().X - this.PatternObjects[this.PatternObjects.Count - 2].CalProxiNode().X) * (this.PatternObjects[this.PatternObjects.Count - 1].CalProxiNode().X - this.PatternObjects[this.PatternObjects.Count - 2].CalProxiNode().X)
                        + (this.PatternObjects[this.PatternObjects.Count-1].CalProxiNode().Y - this.PatternObjects[this.PatternObjects.Count-2].CalProxiNode().Y) * (this.PatternObjects[this.PatternObjects.Count-1].CalProxiNode().Y - this.PatternObjects[this.PatternObjects.Count-2].CalProxiNode().Y));
                    double r = DisplaceDis / (Distance-DisplaceDis);

                    double NewX = (this.PatternObjects[this.PatternObjects.Count-2].CalProxiNode().X + r * this.PatternObjects[this.PatternObjects.Count-1].CalProxiNode().X) / (1 + r);
                    double NewY = (this.PatternObjects[this.PatternObjects.Count-2].CalProxiNode().Y + r * this.PatternObjects[this.PatternObjects.Count-1].CalProxiNode().Y) / (1 + r);
                    #endregion

                    #region 更新坐标
                    double curDx = NewX - this.PatternObjects[this.PatternObjects.Count-2].CalProxiNode().X;
                    double curDy = NewY - this.PatternObjects[this.PatternObjects.Count-2].CalProxiNode().Y;

                    //更新多边形点集的每一个点坐标
                    foreach (TriNode curPoint in this.PatternObjects[PatternObjects.Count-2].PointList)
                    {
                        curPoint.X += curDx;
                        curPoint.Y += curDy;
                    }
                    #endregion
                }
                #endregion

                else
                {
                    #region 计算新位置
                    double Distance = Math.Sqrt((this.PatternObjects[LocationInt].CalProxiNode().X - this.PatternObjects[LocationInt + 1].CalProxiNode().X) * (this.PatternObjects[LocationInt].CalProxiNode().X - this.PatternObjects[LocationInt + 1].CalProxiNode().X)
                        + (this.PatternObjects[LocationInt].CalProxiNode().Y - this.PatternObjects[LocationInt + 1].CalProxiNode().Y) * (this.PatternObjects[LocationInt].CalProxiNode().Y - this.PatternObjects[LocationInt + 1].CalProxiNode().Y));
                    double r = DisplaceDis * 0.5 / (Distance - 0.5 * DisplaceDis);

                    double NewX1 = (this.PatternObjects[LocationInt].CalProxiNode().X + r * this.PatternObjects[LocationInt+1].CalProxiNode().X) / (1 + r);
                    double NewY1 = (this.PatternObjects[LocationInt].CalProxiNode().Y + r * this.PatternObjects[LocationInt+1].CalProxiNode().Y) / (1 + r);

                    double NewX2 = (this.PatternObjects[LocationInt].CalProxiNode().X + 1/r * this.PatternObjects[LocationInt + 1].CalProxiNode().X) / (1 + 1/r);
                    double NewY2 = (this.PatternObjects[LocationInt].CalProxiNode().Y + 1/r * this.PatternObjects[LocationInt + 1].CalProxiNode().Y) / (1 + 1/r);
                    #endregion

                    #region 更新坐标
                    double curDx1 = NewX1 - this.PatternObjects[LocationInt].CalProxiNode().X;
                    double curDy1 = NewY1 - this.PatternObjects[LocationInt].CalProxiNode().Y;

                    //更新多边形点集的每一个点坐标
                    foreach (TriNode curPoint in this.PatternObjects[LocationInt].PointList)
                    {
                        curPoint.X += curDx1;
                        curPoint.Y += curDy1;
                    }

                    double curDx2 = NewX2 - this.PatternObjects[LocationInt+1].CalProxiNode().X;
                    double curDy2 = NewY2 - this.PatternObjects[LocationInt+1].CalProxiNode().Y;

                    //更新多边形点集的每一个点坐标
                    foreach (TriNode curPoint in this.PatternObjects[LocationInt+1].PointList)
                    {
                        curPoint.X += curDx2;
                        curPoint.Y += curDy2;
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// 判断Pattern中建筑物是否需要重新定位
        /// </summary>Label表示判断条件类型
        /// <returns>false=不需要调整；true=需要调整</returns>
        bool PatternNeedRelocationInTypification(int Label,out int LocationInt,out double DisplaceDis)
        {
            bool RelocationLable = false;
            LocationInt = -1;
            DisplaceDis=0;

            #region 计算平均距离
            double SumDis = 0;
            List<double> DistanceList = new List<double>();
            for (int i = 0; i < this.PatternObjects.Count - 1; i++)
            {
                PolygonObject Po1 = this.PatternObjects[i];
                PolygonObject Po2 = this.PatternObjects[i + 1];

                IPolygon Polygon1 = this.ObjectConvert(Po1);
                IPolygon Polygon2 = this.ObjectConvert(Po2);

                IProximityOperator IPO = Polygon1 as IProximityOperator;
                double Distance = IPO.ReturnDistance(Polygon2 as IGeometry);
                DistanceList.Add(Distance);

                SumDis = SumDis + Distance;
            }

            double AverageDis = SumDis / (this.PatternObjects.Count - 1);
            #endregion

            double Dis = DistanceList.Max();
            if ((Dis - AverageDis) > 0.2 * AverageDis)
            {
                RelocationLable = true;
                LocationInt = DistanceList.IndexOf(Dis);
                DisplaceDis = Dis - AverageDis;
            }
            return RelocationLable;
        }

        /// <summary>
        /// 确定Pattern中的边界建筑物(true是边界建筑物，false是非边界建筑物)
        /// </summary>
        /// <param name="Map"></param>
        /// <param name="pg"></param>
        public void PatternBounaryBuilding(SMap Map, ProxiGraph pg)
        {

            for (int i = 0; i < this.PatternObjects.Count; i++)
            {
                int Count = 0;

                for (int j = 0; j < pg.PgforBuildingEdgesList.Count; j++)
                {
                    ProxiEdge Pe = pg.PgforBuildingEdgesList[j];

                    ProxiNode Node1 = Pe.Node1;
                    ProxiNode Node2 = Pe.Node2;

                    PolygonObject Po1 = Map.GetObjectbyID(Node1.TagID, Node1.FeatureType) as PolygonObject;
                    PolygonObject Po2 = Map.GetObjectbyID(Node2.TagID, Node2.FeatureType) as PolygonObject;

                    if (Po1 == this.PatternObjects[i] && this.PatternObjects.Contains(Po2))
                    {
                        Count = Count + 1;
                    }

                    else if (Po2 == this.PatternObjects[i] && this.PatternObjects.Contains(Po1))
                    {
                        Count = Count + 1;
                    }
                }

                if (Count == 1)
                {
                    this.PatternObjects[i].BoundaryBuilding = true;
                }
            }
        }

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
            TriNode curPoint = null;
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
    }
}
