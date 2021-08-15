using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Data;
using AuxStructureLib.IO;
using AuxStructureLib.ConflictLib;
using System.Windows.Forms;


namespace PrDispalce.工具类.CollabrativeDisplacement
{
    class ForceComputation
    {
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new FeatureHandle();//基础工具类
        //PrDispalce.工具类.MultipleLevelDisplace mDisplace = new MultipleLevelDisplace();//多层次移位工具

        //初始移位量计算
        public List<VertexForce> InitialForceList = null;
        public List<Force> CombinationInitialForceList = null;

        //基于场的移位量计算(只考虑道路)
        public List<VertexForce> ForceListforRoadsField = null;
        public List<Force> CombinationForceListforRoadsField = null;

        //基于场的移位量计算(只考虑两个建筑物)
        public List<VertexForce> ForceListforBuildingField = null;
        public List<Force> CombinationForceListforBuildingField = null;

        /// <summary>
        /// 求取每个节点的受力
        /// 首先，对于每一个节点，计算其受力个数，受力方向
        /// 其次，对于每一个节点受力计算方向，并按等级重新更新受力
        /// </summary>
        /// <param name="conflictList"></param> 冲突列表
        public void InitialForceComputation(List<ConflictBase> conflictList)
        {
            InitialForceList = new List<VertexForce>();
            CombinationInitialForceList = new List<Force>();

            #region 计算每个点的各受力分量
            foreach (ConflictBase conflict in conflictList)
            {
                Conflict_R curConflict = conflict as Conflict_R;
                double d = curConflict.DisThreshold;

                #region 当冲突是面面冲突时（考虑面的面积）
                if (curConflict.Type == "RR")
                {                   
                    PolygonObject leftObject = curConflict.Skel_arc.LeftMapObj as PolygonObject;
                    PolygonObject RightObject = curConflict.Skel_arc.RightMapObj as PolygonObject;
                    double larea = leftObject.Area;
                    double rarea = RightObject.Area;
                    double area = larea + rarea;
                    double lw = rarea / area;
                    double rw = larea / area;

                    #region 冲突左节点受力
                    double f = lw * (d - curConflict.Distance);
                    double r = Math.Sqrt((curConflict.LeftPoint.Y - curConflict.RightPoint.Y) * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) + (curConflict.LeftPoint.X - curConflict.RightPoint.X) * (curConflict.LeftPoint.X - curConflict.RightPoint.X));
                    double s = (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                    double c = (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                    //这里将力平分给两个对象
                    double fx = f * c;
                    double fy = f * s;
                    int ID = curConflict.LeftPoint.ID;
                    Force force = new Force(ID, fx, fy, s, c, f);
                    VertexForce vForce = this.GetvForcebyIndex(ID, InitialForceList);
                    if (vForce == null)
                    {
                        vForce = new VertexForce(ID);
                        InitialForceList.Add(vForce);
                    }
                    vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                    #endregion

                    #region 冲突右节点受力
                    f = rw * (d - curConflict.Distance);
                    s = -1 * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                    c = -1 * (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                    //这里将力平分给两个对象
                    fx = f * c;
                    fy = f * s;
                    ID = curConflict.RightPoint.ID;
                    force = new Force(ID, fx, fy, s, c, f);
                    vForce = this.GetvForcebyIndex(ID, InitialForceList);
                    if (vForce == null)
                    {
                        vForce = new VertexForce(ID);
                        InitialForceList.Add(vForce);
                    }
                    vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                    #endregion

                }
                #endregion

                #region 当冲突是线面冲突时
                else if (curConflict.Type == "RL")
                {
                    #region 线在左边
                    if (curConflict.Skel_arc.LeftMapObj.FeatureType == FeatureType.PolylineType)//线在左边
                    {
                        double f = d - curConflict.Distance;
                        double r = Math.Sqrt((curConflict.LeftPoint.Y - curConflict.RightPoint.Y) * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) + (curConflict.LeftPoint.X - curConflict.RightPoint.X) * (curConflict.LeftPoint.X - curConflict.RightPoint.X));
                        double s = -1 * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                        double c = -1 * (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                        //这里将力平分给两个对象
                        double fx = f * c;
                        double fy = f * s;
                        int ID = curConflict.RightPoint.ID;
                        Force force = new Force(ID, fx, fy, s, c, f);
                        VertexForce vForce = this.GetvForcebyIndex(ID, InitialForceList);

                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            InitialForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组


                        //线上的边界点
                        f = 0;
                        s = 0;
                        c = 0;
                        //这里将力平分给两个对象
                        fx = 0;
                        fy = 0;
                        ID = curConflict.LeftPoint.ID;
                        force = new Force(ID, fx, fy, f);
                        force.IsBouldPoint = true;
                        vForce = this.GetvForcebyIndex(ID, InitialForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            InitialForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                    }
                    #endregion

                    #region 线在右边
                    else if (curConflict.Skel_arc.RightMapObj.FeatureType == FeatureType.PolylineType)
                    {
                        double f = d - curConflict.Distance;
                        double r = Math.Sqrt((curConflict.LeftPoint.Y - curConflict.RightPoint.Y) * (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) + (curConflict.LeftPoint.X - curConflict.RightPoint.X) * (curConflict.LeftPoint.X - curConflict.RightPoint.X));
                        double s = (curConflict.LeftPoint.Y - curConflict.RightPoint.Y) / r;
                        double c = (curConflict.LeftPoint.X - curConflict.RightPoint.X) / r;
                        //这里将力平分给两个对象
                        double fx = f * c;
                        double fy = f * s;
                        int ID = curConflict.LeftPoint.ID;
                        Force force = new Force(ID, fx, fy, s, c, f);
                        VertexForce vForce = this.GetvForcebyIndex(ID, InitialForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            InitialForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组


                        //线上的边界点
                        f = 0;
                        s = 0;
                        c = 0;
                        //这里将力平分给两个对象
                        fx = 0;
                        fy = 0;
                        ID = curConflict.RightPoint.ID;
                        force = new Force(ID, fx, fy, f);
                        force.IsBouldPoint = true;
                        vForce = this.GetvForcebyIndex(ID, InitialForceList);
                        if (vForce == null)
                        {
                            vForce = new VertexForce(ID);
                            InitialForceList.Add(vForce);
                        }
                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                    }
                    #endregion
                }
                #endregion
            }
            #endregion

            #region 求合力  先求最大力，以该力的方向为X轴方向建立局部坐标系，求四个主方向上的最大力，最后就合力
            foreach (VertexForce vForce in InitialForceList)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    CombinationInitialForceList.Add(vForce.forceList[0]);
                }
                else if (vForce.forceList.Count > 1)
                {
                    int index = 0;
                    double maxFx = 0;
                    double minFx = 0;
                    double maxFy = 0;
                    double minFy = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    maxFx = maxF.F;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i != index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            if (minFx > fx) minFx = fx;
                            if (maxFy < fy) maxFy = fy;
                            if (minFy > fy) minFy = fy;
                        }
                    }
                    double FFx = maxFx + minFx;
                    double FFy = maxFy + minFy;
                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    double f = Math.Sqrt(Fx * Fx + Fy * Fy);
                    Force rForce = new Force(vForce.ID, Fx, Fy, f);
                    CombinationInitialForceList.Add(rForce);
                }
            }

            #endregion
        }
  
        /// <summary>
        /// 求取每个节点基于场的受力(只考虑道路) 基于V图
        /// </summary>
        /// <param name="width"></param> 道路扩张宽度
        /// <param name="FielderListforRoads"></param> 移位场V图
        /// <param name="PnList"></param> //邻近图点集
        /// <param name="PeList"></param> //邻近图边集
        /// <param name="Parameter"></param>  //衰减控制参数
        public void FieldBasedForceComputation1(double width, List<List<VoronoiPolygon>> FielderListforRoads,List<ProxiNode> PnList,List<ProxiEdge> PeList,double Parameter)
        {
            ForceListforRoadsField = new List<VertexForce>();
            CombinationForceListforRoadsField = new List<Force>();

            #region 计算每个节点的受力
            for (int i = 0; i < FielderListforRoads.Count; i++)
            {
                List<VoronoiPolygon> OneLevelBuildings = FielderListforRoads[i];
                for (int j = 0; j < OneLevelBuildings.Count; j++)
                {
                    VoronoiPolygon Vp = OneLevelBuildings[j];

                    #region 找到多边形对应的顶点
                    for (int m = 0; m < PnList.Count; m++)
                    {
                        ProxiNode Pn = PnList[m];
                        if (Vp.Point.X == Pn.X && Vp.Point.Y == Pn.Y)
                        {
                            #region 对于靠近道路的建筑物
                            if (i == 0)
                            {
                                for (int n = 0; n < PeList.Count; n++)
                                {
                                    #region 找到顶点对应靠近道路的边
                                    ProxiEdge Pe = PeList[n];
                                    double f = Math.Sqrt((Pe.Node1.X - Pe.Node2.X) * (Pe.Node1.X - Pe.Node2.X) + (Pe.Node1.Y - Pe.Node2.Y) * (Pe.Node1.Y - Pe.Node2.Y));
                                    if ((Pe.Node1.X == Vp.Point.X && Pe.Node1.Y == Vp.Point.Y && Pe.Node2.FeatureType == FeatureType.PolylineType))
                                    {
                                        double cos = (Pe.Node1.X - Pe.Node2.X) / f;
                                        double sin = (Pe.Node1.Y - Pe.Node2.Y) / f;
                                        Force force = new Force(Pn.ID, cos, sin, sin, cos, 1);
                                        VertexForce vForce = this.GetvForcebyIndex(Pn.ID, ForceListforRoadsField);

                                        if (vForce == null)
                                        {
                                            vForce = new VertexForce(Pn.ID);
                                            ForceListforRoadsField.Add(vForce);
                                        }
                                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                                    }

                                    if ((Pe.Node1.FeatureType == FeatureType.PolylineType && Pe.Node2.X == Vp.Point.X && Pe.Node2.Y == Vp.Point.Y))
                                    {
                                        double cos = (Pe.Node2.X - Pe.Node1.X) / f;
                                        double sin = (Pe.Node2.Y - Pe.Node1.Y) / f;
                                        Force force = new Force(Pn.ID, cos, sin, sin, cos, 1);
                                        VertexForce vForce = this.GetvForcebyIndex(Pn.ID, ForceListforRoadsField);

                                        if (vForce == null)
                                        {
                                            vForce = new VertexForce(Pn.ID);
                                            ForceListforRoadsField.Add(vForce);
                                        }
                                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                                    }
                                    #endregion
                                }
                            }
                            #endregion

                            #region 对于不靠近道路的建筑物
                            if (i > 0)
                            {
                                for (int n = 0; n < PeList.Count; n++)
                                {
                                    #region 找到节点对应的上一层建筑物
                                    ProxiEdge Pe = PeList[n];
                                    double f = Math.Sqrt((Pe.Node1.X - Pe.Node2.X) * (Pe.Node1.X - Pe.Node2.X) + (Pe.Node1.Y - Pe.Node2.Y) * (Pe.Node1.Y - Pe.Node2.Y));

                                    ProxiNode Pn1 = Pe.Node1; ProxiNode Pn2 = Pe.Node2;
                                    if (Pe.Node1.X == Vp.Point.X && Pe.Node1.Y == Vp.Point.Y)
                                    {
                                        for (int k = 0; k < FielderListforRoads[i - 1].Count; k++)
                                        {
                                            if (Pn2.X == FielderListforRoads[i - 1][k].Point.X && Pn2.Y == FielderListforRoads[i - 1][k].Point.Y)
                                            {
                                                double cos = (Pe.Node1.X - Pe.Node2.X) / f;
                                                double sin = (Pe.Node1.Y - Pe.Node2.Y) / f;
                                                Force force = new Force(Pn.ID, cos, sin, sin, cos, 1);
                                                VertexForce vForce = this.GetvForcebyIndex(Pn.ID, ForceListforRoadsField);

                                                if (vForce == null)
                                                {
                                                    vForce = new VertexForce(Pn.ID);
                                                    ForceListforRoadsField.Add(vForce);
                                                }
                                                vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                                            }
                                        }
                                    }

                                    if (Pe.Node2.X == Vp.Point.X && Pe.Node2.Y == Vp.Point.Y)
                                    {
                                        for (int k = 0; k < FielderListforRoads[i - 1].Count; k++)
                                        {
                                            if (Pn1.X == FielderListforRoads[i - 1][k].Point.X && Pn1.Y == FielderListforRoads[i - 1][k].Point.Y)
                                            {
                                                double cos = (Pe.Node2.X - Pe.Node1.X) / f;
                                                double sin = (Pe.Node2.Y - Pe.Node1.Y) / f;
                                                Force force = new Force(Pn.ID, cos, sin, sin, cos, f);
                                                VertexForce vForce = this.GetvForcebyIndex(Pn.ID, ForceListforRoadsField);

                                                if (vForce == null)
                                                {
                                                    vForce = new VertexForce(Pn.ID);
                                                    ForceListforRoadsField.Add(vForce);
                                                }
                                                vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                                            }
                                        }
                                    }
                                    #endregion
                                }
                            }
                            #endregion

                            break;//找到多边形对应的顶点后退出当前循环
                        }                         
                    }
                    #endregion
                }
            }
            #endregion 

            #region 计算每个节点合力方向，并更新受力
            foreach (VertexForce vForce in ForceListforRoadsField)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    #region 更新受力
                    double level = -1;
                    for (int i = 0; i < FielderListforRoads.Count; i++)
                    {
                        for (int j = 0; j < FielderListforRoads[i].Count; j++)
                        {
                            if (PnList[vForce.forceList[0].ID].X == FielderListforRoads[i][j].Point.X && PnList[vForce.forceList[0].ID].Y == FielderListforRoads[i][j].Point.Y)
                            {
                                level = i;
                                break;
                            }
                        }

                        if (level != -1)
                        {
                            break;
                        }
                    }

                    vForce.forceList[0].F = width - width * (level/ Parameter);
                    vForce.forceList[0].Fx = vForce.forceList[0].F * vForce.forceList[0].Cos;
                    vForce.forceList[0].Fy = vForce.forceList[0].F * vForce.forceList[0].Sin;
                    CombinationForceListforRoadsField.Add(vForce.forceList[0]);
                    #endregion
                }
                else if (vForce.forceList.Count > 1)
                {
                    #region 计算合力方向
                    int index = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    double FFx = maxF.F;
                    double FFy = 0;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i != index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            FFx = FFx + fx;
                            FFy = FFy + fy;
                        }
                    }

                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    #endregion

                    #region 更新受力
                    double level = -1;
                    for (int i = 0; i < FielderListforRoads.Count; i++)
                    {
                        for (int j = 0; j < FielderListforRoads[i].Count; j++)
                        {
                            if (PnList[vForce.forceList[0].ID].X == FielderListforRoads[i][j].Point.X && PnList[vForce.forceList[0].ID].Y == FielderListforRoads[i][j].Point.Y)
                            {
                                level = i;
                                break;
                            }
                        }

                        if (level != -1)
                        {
                            break;
                        }
                    }

                    double f = width - width * (level / Parameter);
                    double cos = Fx / (Math.Sqrt(Fx * Fx + Fy * Fy));
                    double sin = Fy / (Math.Sqrt(Fx * Fx + Fy * Fy));
                    Force rForce = new Force(vForce.ID, f*cos, f*sin,sin,cos,f);
                    CombinationForceListforRoadsField.Add(rForce);
                    #endregion
                }
            }
            #endregion
        }

        /// <summary>
        /// 求取每个节点基于场的受力（只考虑道路）基于给定的建筑物场
        /// </summary>
        /// <param name="width"></param> 道路扩张宽度
        /// <param name="FielderListforRoads"></param> 移位场建筑物
        /// <param name="PnList"></param> 邻近图点集
        /// <param name="PeList"></param> 邻近图边集
        /// <param name="Parameter"></param> 衰减控制参数
        public void FieldBasedForceComputation2(double width, List<List<PolygonObject>> FielderListforRoads, List<ProxiNode> PnList, List<ProxiEdge> PeList, double Parameter)
        {
            ForceListforRoadsField = new List<VertexForce>();
            CombinationForceListforRoadsField = new List<Force>();

            #region 计算每个节点的受力
            for (int i = 0; i < FielderListforRoads.Count; i++)
            {
                List<PolygonObject> OneLevelBuildings = FielderListforRoads[i];
                for (int j = 0; j < OneLevelBuildings.Count; j++)
                {
                    PolygonObject vp = OneLevelBuildings[j];

                    #region 找到多边形对应的顶点
                    for (int m = 0; m < PnList.Count; m++)
                    {
                        ProxiNode Pn = PnList[m];
                        if (vp.CalProxiNode().X == Pn.X && vp.CalProxiNode().Y == Pn.Y)
                        {
                            #region 对于靠近道路的建筑物
                            if (i == 0)
                            {
                                for (int n = 0; n < PeList.Count; n++)
                                {
                                    #region 找到顶点对应靠近道路的边
                                    ProxiEdge Pe = PeList[n];
                                    double f = Math.Sqrt((Pe.Node1.X - Pe.Node2.X) * (Pe.Node1.X - Pe.Node2.X) + (Pe.Node1.Y - Pe.Node2.Y) * (Pe.Node1.Y - Pe.Node2.Y));
                                    if ((Pe.Node1.X == vp.CalProxiNode().X && Pe.Node1.Y == vp.CalProxiNode().Y && Pe.Node2.FeatureType == FeatureType.PolylineType))
                                    {
                                        double cos = (Pe.Node1.X - Pe.Node2.X) / f;
                                        double sin = (Pe.Node1.Y - Pe.Node2.Y) / f;
                                        Force force = new Force(Pn.ID, cos, sin, sin, cos, 1);
                                        VertexForce vForce = this.GetvForcebyIndex(Pn.ID, ForceListforRoadsField);

                                        if (vForce == null)
                                        {
                                            vForce = new VertexForce(Pn.ID);
                                            ForceListforRoadsField.Add(vForce);
                                        }
                                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                                    }

                                    if ((Pe.Node1.FeatureType == FeatureType.PolylineType && Pe.Node2.X == vp.CalProxiNode().X && Pe.Node2.Y == vp.CalProxiNode().Y))
                                    {
                                        double cos = (Pe.Node2.X - Pe.Node1.X) / f;
                                        double sin = (Pe.Node2.Y - Pe.Node1.Y) / f;
                                        Force force = new Force(Pn.ID, cos, sin, sin, cos, 1);
                                        VertexForce vForce = this.GetvForcebyIndex(Pn.ID, ForceListforRoadsField);

                                        if (vForce == null)
                                        {
                                            vForce = new VertexForce(Pn.ID);
                                            ForceListforRoadsField.Add(vForce);
                                        }
                                        vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                                    }
                                    #endregion
                                }
                            }
                            #endregion

                            #region 对于不靠近道路的建筑物
                            if (i > 0)
                            {
                                for (int n = 0; n < PeList.Count; n++)
                                {
                                    #region 找到节点对应的上一层建筑物
                                    ProxiEdge Pe = PeList[n];
                                    double f = Math.Sqrt((Pe.Node1.X - Pe.Node2.X) * (Pe.Node1.X - Pe.Node2.X) + (Pe.Node1.Y - Pe.Node2.Y) * (Pe.Node1.Y - Pe.Node2.Y));

                                    ProxiNode Pn1 = Pe.Node1; ProxiNode Pn2 = Pe.Node2;
                                    if (Pe.Node1.X ==vp.CalProxiNode().X && Pe.Node1.Y == vp.CalProxiNode().Y)
                                    {
                                        for (int k = 0; k < FielderListforRoads[i - 1].Count; k++)
                                        {
                                            if (Pn2.X == FielderListforRoads[i - 1][k].CalProxiNode().X && Pn2.Y == FielderListforRoads[i - 1][k].CalProxiNode().Y)
                                            {
                                                double cos = (Pe.Node1.X - Pe.Node2.X) / f;
                                                double sin = (Pe.Node1.Y - Pe.Node2.Y) / f;
                                                Force force = new Force(Pn.ID, cos, sin, sin, cos, 1);
                                                VertexForce vForce = this.GetvForcebyIndex(Pn.ID, ForceListforRoadsField);

                                                if (vForce == null)
                                                {
                                                    vForce = new VertexForce(Pn.ID);
                                                    ForceListforRoadsField.Add(vForce);
                                                }
                                                vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                                            }
                                        }
                                    }

                                    if (Pe.Node2.X == vp.CalProxiNode().X && Pe.Node2.Y == vp.CalProxiNode().Y)
                                    {
                                        for (int k = 0; k < FielderListforRoads[i - 1].Count; k++)
                                        {
                                            if (Pn1.X == FielderListforRoads[i - 1][k].CalProxiNode().X && Pn1.Y == FielderListforRoads[i - 1][k].CalProxiNode().Y)
                                            {
                                                double cos = (Pe.Node2.X - Pe.Node1.X) / f;
                                                double sin = (Pe.Node2.Y - Pe.Node1.Y) / f;
                                                Force force = new Force(Pn.ID, cos, sin, sin, cos, f);
                                                VertexForce vForce = this.GetvForcebyIndex(Pn.ID, ForceListforRoadsField);

                                                if (vForce == null)
                                                {
                                                    vForce = new VertexForce(Pn.ID);
                                                    ForceListforRoadsField.Add(vForce);
                                                }
                                                vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                                            }
                                        }
                                    }
                                    #endregion
                                }
                            }
                            #endregion

                            break;//找到多边形对应的顶点后退出当前循环
                        }
                    }
                    #endregion
                }
            }
            #endregion

            #region 计算每个节点合力方向，并更新受力
            foreach (VertexForce vForce in ForceListforRoadsField)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    #region 更新受力
                    double level = -1;
                    for (int i = 0; i < FielderListforRoads.Count; i++)
                    {
                        for (int j = 0; j < FielderListforRoads[i].Count; j++)
                        {
                            if (PnList[vForce.forceList[0].ID].X == FielderListforRoads[i][j].CalProxiNode().X && PnList[vForce.forceList[0].ID].Y == FielderListforRoads[i][j].CalProxiNode().Y)
                            {
                                level = i;
                                break;
                            }
                        }

                        if (level != -1)
                        {
                            break;
                        }
                    }

                    vForce.forceList[0].F = width - width * (level / Parameter);
                    vForce.forceList[0].Fx = vForce.forceList[0].F * vForce.forceList[0].Cos;
                    vForce.forceList[0].Fy = vForce.forceList[0].F * vForce.forceList[0].Sin;
                    CombinationForceListforRoadsField.Add(vForce.forceList[0]);
                    #endregion
                }
                else if (vForce.forceList.Count > 1)
                {
                    #region 计算合力方向
                    int index = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    double FFx = maxF.F;
                    double FFy = 0;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i != index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            FFx = FFx + fx;
                            FFy = FFy + fy;
                        }
                    }

                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    #endregion

                    #region 更新受力
                    double level = -1;
                    for (int i = 0; i < FielderListforRoads.Count; i++)
                    {
                        for (int j = 0; j < FielderListforRoads[i].Count; j++)
                        {
                            if (PnList[vForce.forceList[0].ID].X == FielderListforRoads[i][j].CalProxiNode().X && PnList[vForce.forceList[0].ID].Y == FielderListforRoads[i][j].CalProxiNode().Y)
                            {
                                level = i;
                                break;
                            }
                        }

                        if (level != -1)
                        {
                            break;
                        }
                    }

                    double f = width - width * (level / Parameter);
                    double cos = Fx / (Math.Sqrt(Fx * Fx + Fy * Fy));
                    double sin = Fy / (Math.Sqrt(Fx * Fx + Fy * Fy));
                    Force rForce = new Force(vForce.ID, f * cos, f * sin, sin, cos, f);
                    CombinationForceListforRoadsField.Add(rForce);
                    #endregion
                }
            }
            #endregion
        }

        /// <summary>
        /// 求取每个节点基于场的受力（只考虑冲突的两个建筑物） 基于给定的建筑物场
        /// </summary>
        /// <param name="FielderListforBuildings"></param> 移位场建筑物
        /// <param name="PnList"></param>邻近图点集
        /// <param name="PeList"></param>邻近图边集
        /// <param name="Parameter"></param>衰减控制参数
        /// level 考虑建筑物的场的层数 level=0表示一层；level=1表示两层
        /// MniDis 建筑物间的最小间隔
        public void FieldBasedForceComputationforBuildings(List<List<PolygonObject>> FielderListforBuildings,List<ProxiNode> PnList,List<ProxiEdge> PeList,double Parameter, int level,double MniDis)
        {
            if (level + 1 > FielderListforBuildings.Count)
            {               
                MessageBox.Show("给定的建筑物层数过大");
                return;
            }

            ForceListforBuildingField = new List<VertexForce>();
            CombinationForceListforBuildingField = new List<Force>();

            #region 考虑第一层的两个建筑物（移位方向及移位距离）
            PolygonObject vp1 = FielderListforBuildings[0][0];
            PolygonObject vp2 = FielderListforBuildings[0][1];         
            List<double> OutForce = ForceComputationforTwoBuildings(MniDis,vp1,vp2,PeList);

            for (int i = 0; i < PnList.Count; i++)//力赋值
            {
                if (PnList[i].TagID == vp1.ID && vp1.FeatureType==FeatureType.PolygonType)
                {
                    Force force = new Force(PnList[i].ID, OutForce[0], OutForce[1], OutForce[1], OutForce[0], 1);
                    VertexForce vForce = this.GetvForcebyIndex(PnList[i].ID, ForceListforBuildingField);

                    if (vForce == null)
                    {
                        vForce = new VertexForce(PnList[i].ID);
                        ForceListforBuildingField.Add(vForce);
                    }
                    vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                }

                if (PnList[i].TagID == vp2.ID && PnList[i].FeatureType == FeatureType.PolygonType)
                {
                    Force force = new Force(PnList[i].ID, OutForce[2], OutForce[3], OutForce[3], OutForce[2], 1);
                    VertexForce vForce = this.GetvForcebyIndex(PnList[i].ID, ForceListforBuildingField);

                    if (vForce == null)
                    {
                        vForce = new VertexForce(PnList[i].ID);
                        ForceListforBuildingField.Add(vForce);
                    }
                    vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                }
            }
            #endregion

            #region 其余层建筑物移位方向确定
            for (int i = 1; i < level; i++)
            {
                List<PolygonObject> OneLevelBuildings = FielderListforBuildings[i];
                for (int j = 0; j < OneLevelBuildings.Count; j++)
                {
                    PolygonObject vp = OneLevelBuildings[j];

                    #region 找到多边形对应的顶点
                    for (int m = 0; m < PnList.Count; m++)
                    {
                        ProxiNode Pn = PnList[m];
                        if (vp.CalProxiNode().X == Pn.X && vp.CalProxiNode().Y == Pn.Y)
                        {
                            for (int n = 0; n < PeList.Count; n++)
                            {
                                #region 找到节点对应的上一层建筑物
                                ProxiEdge Pe = PeList[n];
                                double f = Math.Sqrt((Pe.Node1.X - Pe.Node2.X) * (Pe.Node1.X - Pe.Node2.X) + (Pe.Node1.Y - Pe.Node2.Y) * (Pe.Node1.Y - Pe.Node2.Y));

                                ProxiNode Pn1 = Pe.Node1; ProxiNode Pn2 = Pe.Node2;
                                if (Pe.Node1.X == vp.CalProxiNode().X && Pe.Node1.Y == vp.CalProxiNode().Y)
                                {
                                    for (int k = 0; k < FielderListforBuildings[i - 1].Count; k++)
                                    {
                                        if (Pn2.X == FielderListforBuildings[i - 1][k].CalProxiNode().X && Pn2.Y == FielderListforBuildings[i - 1][k].CalProxiNode().Y)
                                        {
                                            double cos = (Pe.Node1.X - Pe.Node2.X) / f;
                                            double sin = (Pe.Node1.Y - Pe.Node2.Y) / f;
                                            Force force = new Force(Pn.ID, cos, sin, sin, cos, 1);
                                            VertexForce vForce = this.GetvForcebyIndex(Pn.ID, ForceListforBuildingField);

                                            if (vForce == null)
                                            {
                                                vForce = new VertexForce(Pn.ID);
                                                ForceListforBuildingField.Add(vForce);
                                            }
                                            vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                                        }
                                    }
                                }

                                if (Pe.Node2.X == vp.CalProxiNode().X && Pe.Node2.Y == vp.CalProxiNode().Y)
                                {
                                    for (int k = 0; k < FielderListforBuildings[i - 1].Count; k++)
                                    {
                                        if (Pn1.X == FielderListforBuildings[i - 1][k].CalProxiNode().X && Pn1.Y == FielderListforBuildings[i - 1][k].CalProxiNode().Y)
                                        {
                                            double cos = (Pe.Node2.X - Pe.Node1.X) / f;
                                            double sin = (Pe.Node2.Y - Pe.Node1.Y) / f;
                                            Force force = new Force(Pn.ID, cos, sin, sin, cos, f);
                                            VertexForce vForce = this.GetvForcebyIndex(Pn.ID, ForceListforBuildingField);

                                            if (vForce == null)
                                            {
                                                vForce = new VertexForce(Pn.ID);
                                                ForceListforBuildingField.Add(vForce);
                                            }
                                            vForce.forceList.Add(force);//将当前的受力加入VertexForce数组
                                        }
                                    }
                                }
                                #endregion
                            }

                            break;//找到多边形对应的顶点后退出当前循环   
                        }                      
                    }
                    #endregion
                }
            }
            #endregion

            #region 计算每个节点合力方向，并更新受力
            foreach (VertexForce vForce in ForceListforBuildingField)
            {
                if (vForce.forceList.Count == 1)//当只受一个力作用时
                {
                    #region 更新受力
                    double plevel = -1;
                    for (int i = 0; i < FielderListforBuildings.Count; i++)
                    {
                        for (int j = 0; j < FielderListforBuildings[i].Count; j++)
                        {
                            if (PnList[vForce.forceList[0].ID].X == FielderListforBuildings[i][j].CalProxiNode().X && PnList[vForce.forceList[0].ID].Y == FielderListforBuildings[i][j].CalProxiNode().Y)
                            {
                                plevel = i;
                                break;
                            }
                        }

                        if (plevel != -1)
                        {
                            break;
                        }
                    }

                    vForce.forceList[0].F = OutForce[4] - OutForce[4] * (plevel / Parameter);
                    vForce.forceList[0].Fx = vForce.forceList[0].F * vForce.forceList[0].Cos;
                    vForce.forceList[0].Fy = vForce.forceList[0].F * vForce.forceList[0].Sin;
                    CombinationForceListforBuildingField.Add(vForce.forceList[0]);
                    #endregion
                }
                else if (vForce.forceList.Count > 1)
                {
                    #region 计算合力方向
                    int index = 0;
                    Force maxF = GetMaxForce(out  index, vForce.forceList);
                    double FFx = maxF.F;
                    double FFy = 0;
                    double s = maxF.Sin;
                    double c = maxF.Cos;

                    for (int i = 0; i < vForce.forceList.Count; i++)
                    {

                        if (i != index)
                        {
                            Force F = vForce.forceList[i];
                            double fx = F.Fx * c + F.Fy * s;
                            double fy = F.Fy * c - F.Fx * s;

                            FFx = FFx + fx;
                            FFy = FFy + fy;
                        }
                    }

                    double Fx = FFx * c - FFy * s;
                    double Fy = FFx * s + FFy * c;
                    #endregion

                    #region 更新受力
                    double plevel = -1;
                    for (int i = 0; i < FielderListforBuildings.Count; i++)
                    {
                        for (int j = 0; j < FielderListforBuildings[i].Count; j++)
                        {
                            if (PnList[vForce.forceList[0].ID].X == FielderListforBuildings[i][j].CalProxiNode().X && PnList[vForce.forceList[0].ID].Y == FielderListforBuildings[i][j].CalProxiNode().Y)
                            {
                                plevel = i;
                                break;
                            }
                        }

                        if (plevel != -1)
                        {
                            break;
                        }
                    }

                    double f = OutForce[4] - OutForce[4] * (plevel / Parameter);
                    double cos = Fx / (Math.Sqrt(Fx * Fx + Fy * Fy));
                    double sin = Fy / (Math.Sqrt(Fx * Fx + Fy * Fy));
                    Force rForce = new Force(vForce.ID, f * cos, f * sin, sin, cos, f);
                    CombinationForceListforBuildingField.Add(rForce);
                    #endregion
                }
            }
            #endregion

        }

        /// <summary>
        /// 求取两个冲突建筑物的初始移位量(根据给定的冲突边，计算两个建筑物的冲突量)
        /// </summary>
        /// <param name="MiniDis"></param> 最小距离
        /// <param name="Po1"></param> 建筑物1
        /// <param name="Po2"></param> 建筑物2
        /// <returns></returns>
        public List<double> ForceComputationforTwoBuildings(double MiniDis,PolygonObject Po1,PolygonObject Po2,List<ProxiEdge> PeList)
        {
            List<double> OutForce = new List<double>();

            //polygonobject和polygon的转换
            IPolygon mPolygon = ObjectConvert(Po1);
            IPolygon nPolygon = ObjectConvert(Po2);

            //求polygon的相交
            ITopologicalOperator mTopologicalOperator = mPolygon as ITopologicalOperator;
            IGeometry Geo = mTopologicalOperator.Intersect(nPolygon as IGeometry, esriGeometryDimension.esriGeometry2Dimension);

            #region 两建筑物相交
            if (!Geo.IsEmpty)
            {
                #region 计算两建筑物重心连线方向
                IArea mArea = mPolygon as IArea;
                IArea nArea = nPolygon as IArea;
                IPoint mPoint = mArea.Centroid;
                IPoint nPoint = nArea.Centroid;
                double k = (mPoint.Y - nPoint.Y) / (mPoint.X - nPoint.X);
                double Length = Math.Sqrt((mPoint.X - nPoint.X) * (mPoint.X - nPoint.X) + (mPoint.Y - nPoint.Y) * (mPoint.Y - nPoint.Y));
                #endregion

                #region 穿过相交区域重心，且方向与两建筑物重心连线方向一致的直线与相交区域的交线
                //IPolygon gPolygon = Geo as IPolygon;
                //IArea gArea = gPolygon as IArea;
                //IPoint gPoint = gArea.Centroid;

                //IPoint Point1 = new PointClass();
                //IPoint Point2 = new PointClass();

                //Point1.X = gPoint.X + 5000; Point1.Y = gPoint.Y + 5000 * k;
                //Point2.X = gPoint.X - 5000; Point2.Y = gPoint.Y - 5000 * k;
                //ILine ShortLine = new LineClass();
                //ShortLine.FromPoint = Point1;
                //ShortLine.ToPoint = Point2;

                //IPolyline plLine = new PolylineClass();
                //plLine.FromPoint = ShortLine.FromPoint;
                //plLine.ToPoint = ShortLine.ToPoint;
                //ITopologicalOperator GeoTopo = Geo as ITopologicalOperator;
                //IGeometry LineGeo = GeoTopo.Intersect(plLine, esriGeometryDimension.esriGeometry1Dimension);
                //IPolyline geoLine = LineGeo as IPolyline;
                #endregion

                #region 受力计算
                //double MinDis = geoLine.Length; 
                double f = MiniDis * 1.5;
                double cos1=(mPoint.X-nPoint.X)/Length;
                double sin1 = (mPoint.Y - nPoint.Y) / Length;
                double cos2 = (nPoint.X - mPoint.X) / Length;
                double sin2 = (nPoint.Y - mPoint.Y) / Length;

                OutForce.Add(cos1); OutForce.Add(sin1);OutForce.Add(cos2); OutForce.Add(sin2);
                OutForce.Add(f / 2);
                #endregion
            }
            #endregion

            #region 两建筑物不相交
            else
            {
                for (int i = 0; i < PeList.Count; i++)//首先找到建筑物对应的邻近边
                {
                    ProxiEdge Pe = PeList[i];
                    ProxiNode Pn1 = Pe.Node1; ProxiNode Pn2 = Pe.Node2;

                    if ((Pn1.TagID == Po1.ID && Po1.FeatureType==FeatureType.PolygonType) && (Pn2.TagID == Po2.ID && Po2.FeatureType==FeatureType.PolygonType))
                    {
                        double NearDistance = Pe.NearestEdge.NearestDistance;

                        //IPolygon testPolygon1 = ObjectConvert(Po1); IPolygon testPolygon2 = ObjectConvert(Po2);
                        //IProximityOperator testPro = testPolygon1 as IProximityOperator;
                        //double TestDis = testPro.ReturnDistance(testPolygon2 as IGeometry);

                        NearestPoint nPn1 = Pe.NearestEdge.Point1; NearestPoint nPn2 = Pe.NearestEdge.Point2;
                        //double test = Math.Sqrt((nPn1.X - nPn2.X) * (nPn1.X - nPn2.X) + (nPn1.Y - nPn2.Y) * (nPn1.Y - nPn2.Y));
                        //MessageBox.Show(NearDistance.ToString() + test.ToString());

                        if (nPn1.ID == Po1.ID)
                        {
                            double cos1 = (nPn1.X - nPn2.X) / NearDistance;
                            double sin1 = (nPn1.Y - nPn2.Y) / NearDistance;
                            double cos2 = (nPn2.X - nPn1.X) / NearDistance;
                            double sin2 = (nPn2.Y - nPn1.Y) / NearDistance;
                            double f = MiniDis - NearDistance;

                            OutForce.Add(cos1); OutForce.Add(sin1);OutForce.Add(cos2); OutForce.Add(sin2);
                            OutForce.Add(f / 2);
                        }

                        else
                        {
                            double cos1 = (nPn2.X - nPn1.X) / NearDistance;
                            double sin1 = (nPn2.Y - nPn1.Y) / NearDistance;
                            double cos2 = (nPn1.X - nPn2.X) / NearDistance;
                            double sin2 = (nPn1.Y - nPn2.Y) / NearDistance;
                            double f = MiniDis - NearDistance;

                            OutForce.Add(cos1); OutForce.Add(sin1); OutForce.Add(cos2); OutForce.Add(sin2);
                            OutForce.Add(f / 2);
                        }
                      
                        break;
                    }

                    if ((Pn1.TagID == Po2.ID && Po2.FeatureType==FeatureType.PolygonType) && (Pn2.TagID == Po1.ID && Po1.FeatureType==FeatureType.PolygonType))
                    {
                        double NearDistance = Pe.NearestEdge.NearestDistance;
                        NearestPoint nPn1 = Pe.NearestEdge.Point1; NearestPoint nPn2 = Pe.NearestEdge.Point2;
                    
                        if (nPn1.ID==Po1.ID)
                        {
                            double cos1 = (nPn1.X - nPn2.X) / NearDistance;
                            double sin1 = (nPn1.Y - nPn2.Y) / NearDistance;
                            double cos2 = (nPn2.X - nPn1.X) / NearDistance;
                            double sin2 = (nPn2.Y - nPn1.Y) / NearDistance;
                            double f = MiniDis - NearDistance;

                            OutForce.Add(cos1); OutForce.Add(sin1);
                            OutForce.Add(cos2); OutForce.Add(sin2);
                            OutForce.Add(f / 2);
                        }

                        else
                        {
                            double cos1 = (nPn2.X - nPn1.X) / NearDistance;
                            double sin1 = (nPn2.Y - nPn1.Y) / NearDistance;
                            double cos2 = (nPn1.X - nPn2.X) / NearDistance;
                            double sin2 = (nPn1.Y - nPn2.Y) / NearDistance;
                            double f = MiniDis - NearDistance;

                            OutForce.Add(cos1); OutForce.Add(sin1);
                            OutForce.Add(cos2); OutForce.Add(sin2);
                            OutForce.Add(f / 2);
                        }

                        break;
                    }
                }           
            }
            #endregion

            return OutForce;
        }
        
        /// <summary>
        /// 通过索引号获取对应顶点的受力值VertexForce
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private VertexForce GetvForcebyIndex(int index, List<VertexForce> vForceList)
        {
            foreach (VertexForce curvF in vForceList)
            {
                if (curvF.ID == index)
                    return curvF;
            }
            return null;
        }

        /// <summary>
        /// 通过索引号获取受力值Force
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Force GetForcebyIndex(int index,List<Force> ForceList)
        {
            foreach (Force curF in ForceList)
            {
                if (curF.ID == index)
                    return curF;
            }
            return null;
        }

        /// <summary>
        /// 通过索引号获取最大的受力值
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private Force GetMaxForce(out int index, List<Force> ForceList)
        {
            double MaxF = -1;
            index = 0;
            for (int i = 0; i < ForceList.Count; i++)
            {
                if (ForceList[i].F > MaxF)
                {
                    index = i;
                    MaxF = ForceList[i].F;
                }
            }
            return ForceList[index];
        }

        /// <summary>
        /// 将polygonobject转换成ipolygon
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

        /// <summary>
        /// 输出每个节点的受力
        /// </summary>
        /// <param name="ForceList"></param> 节点的受力列表
        /// <param name="OutPath"></param> 输出路径
        /// <param name="fileName"></param> 输出名称
        /// <param name="pSpatialReference"></param> 投影
        public void ForceOut(List<Force> ForceList,string OutPath, string fileName, ISpatialReference pSpatialReference,List<ProxiNode> PnList)
        {
            //创建一个线图层
            IFeatureClass pFeatureClass = pFeatureHandle.createLineshapefile(pSpatialReference, OutPath, fileName);
            pFeatureHandle.AddField(pFeatureClass, "f", esriFieldType.esriFieldTypeDouble);
            pFeatureHandle.AddField(pFeatureClass, "sin", esriFieldType.esriFieldTypeDouble);
            pFeatureHandle.AddField(pFeatureClass, "cos", esriFieldType.esriFieldTypeDouble);

            foreach(Force f in ForceList)
            {
                for (int i = 0; i < PnList.Count; i++)
                {
                    if (f.ID == PnList[i].ID)
                    {
                        IPolyline pLine = new PolylineClass();
                        IPoint Point1 = new PointClass();
                        IPoint Point2 = new PointClass();

                        Point1.X = PnList[i].X; Point1.Y = PnList[i].Y;
                        Point2.X = PnList[i].X + (f.F * f.Cos) * 3; Point2.Y = Point1.Y + (f.F * f.Sin) * 3;
                        pLine.FromPoint = Point1; pLine.ToPoint = Point2;

                        IFeature feature = pFeatureClass.CreateFeature();
                        feature.Shape = pLine as IGeometry;

                        #region 输出属性值
                        IFields sFields = feature.Fields;
                        int sfnum = sFields.FieldCount;
                        for (int j = 0; j < sfnum; j++)
                        {
                            if (sFields.get_Field(j).Name == "f")
                            {
                                int field1 = sFields.FindField("f");
                                feature.set_Value(field1, f.F);
                                feature.Store();
                            }

                            if (sFields.get_Field(j).Name == "sin")
                            {
                                int field1 = sFields.FindField("sin");
                                feature.set_Value(field1, f.Sin);
                                feature.Store();
                            }

                            if (sFields.get_Field(j).Name == "cos")
                            {
                                int field1 = sFields.FindField("cos");
                                feature.set_Value(field1, f.Cos);
                                feature.Store();
                            }
                        }
                        #endregion

                        break;
                    }
                }
            }
        }

         /// <summary>
         /// 根据节点受力更新节点位置（是否顾及拓扑关系）更新节点坐标及节点对应多边形坐标
         /// </summary>
         /// <param name="PnList"></param> 节点集合
         /// <param name="VD"></param> v图
         /// <param name="IsTopCos"></param> 是否强制维护拓扑关系
        public void UpdataCoordsforPGbyForce_Group(List<ProxiNode> PnList, VoronoiDiagram VD, bool IsTopCos, SMap Map, List<Force> CombinationForceListforField)
        {
            foreach (ProxiNode curNode in PnList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                VoronoiPolygon vp = null;
                Force force = this.GetForcebyIndex(index, CombinationForceListforField);

                #region 非分组建筑物
                if (fType == FeatureType.PolygonType)
                {
                    PolygonObject po = Map.GetObjectbyID(tagID, fType) as PolygonObject;
                    double curDx0 = 0;
                    double curDy0 = 0;
                    double curDx = 0;
                    double curDy = 0;
                    if (force != null)
                    {
                        curDx0 = force.Fx;
                        curDy0 = force.Fy;
                        curDx = force.Fx;
                        curDy = force.Fy;
                        if (IsTopCos == true)
                        {
                            vp = VD.GetVPbyIDandType(tagID, fType);
                            vp.TopologicalConstraint(curDx0, curDy0, 0.001, out curDx, out curDy);
                        }
                        //纠正拓扑错误
                        curNode.X += curDx;
                        curNode.Y += curDy;

                        //更新多边形点集的每一个点坐标
                        foreach (TriNode curPoint in po.PointList)
                        {
                            curPoint.X += curDx;
                            curPoint.Y += curDy;
                        }
                    }                 
                }
                #endregion

                #region 处理建筑物分组情况（即建筑物当做一个整体）
                //else if (fType == FeatureType.Group)
                //{
                //    GroupofMapObject group = GroupofMapObject.GetGroup(tagID, this.Groups);
                //    double curDx0 = 0;
                //    double curDy0 = 0;
                //    double curDx = 0;
                //    double curDy = 0;
                //    if (force != null)
                //    {
                //        curDx0 = this.fV.GetForcebyIndex(index).Fx;
                //        curDy0 = this.fV.GetForcebyIndex(index).Fy;
                //        curDx = this.fV.GetForcebyIndex(index).Fx;
                //        curDy = this.fV.GetForcebyIndex(index).Fy;
                //        if (this.IsTopCos == true)
                //        {
                //            foreach (PolygonObject obj in group.ListofObjects)
                //            {
                //                tagID = obj.ID;
                //                fType = obj.FeatureType;
                //                vp = this.VD.GetVPbyIDandType(tagID, fType);
                //                vp.TopologicalConstraint(curDx0, curDx0, 0.001, out curDx, out curDy);
                //                curDx0 = curDx;
                //                curDy0 = curDy;
                //            }
                //            this.D[3 * index, 0] = curDx;
                //            this.D[3 * index + 1, 0] = curDy;
                //        }
                //        //纠正拓扑错误
                //        curNode.X += curDx;
                //        curNode.Y += curDy;
                //        foreach (PolygonObject obj in group.ListofObjects)
                //        {
                //            foreach (TriNode curPoint in obj.PointList)
                //            {
                //                curPoint.X += curDx;
                //                curPoint.Y += curDy;
                //            }
                //        }
                //    }
                //    else
                //    {
                //        this.D[3 * index, 0] = curDx;
                //        this.D[3 * index + 1, 0] = curDy;
                //    }
                //}
                #endregion
            }
        }

        /// <summary>
        /// 根据节点受力更新节点位置（不顾及拓扑关系）只更新节点对应多边形的坐标
        /// </summary>
        /// <param name="PnList"></param> 节点集合
        /// <param name="VD"></param> v图
        /// <param name="IsTopCos"></param> 是否强制维护拓扑关系
        public void UpdataCoordsforPGbyForce_Group2(List<ProxiNode> PnList, SMap Map, List<Force> CombinationForceListforField)
        {
            foreach (ProxiNode curNode in PnList)
            {
                int index = curNode.ID;
                int tagID = curNode.TagID;
                FeatureType fType = curNode.FeatureType;
                //VoronoiPolygon vp = null;
                Force force = this.GetForcebyIndex(index, CombinationForceListforField);

                #region 非分组建筑物
                if (fType == FeatureType.PolygonType)
                {
                    PolygonObject po = Map.GetObjectbyID(tagID, fType) as PolygonObject;
                    if (po != null)
                    {
                        double curDx = 0;
                        double curDy = 0;
                        if (force != null)
                        {
                            curDx = force.Fx;
                            curDy = force.Fy;

                            //更新多边形点集的每一个点坐标
                            foreach (TriNode curPoint in po.PointList)
                            {
                                curPoint.X += curDx;
                                curPoint.Y += curDy;
                            }
                        }
                    }
                }
                #endregion

                #region 处理建筑物分组情况（即建筑物当做一个整体）
                //else if (fType == FeatureType.Group)
                //{
                //    GroupofMapObject group = GroupofMapObject.GetGroup(tagID, this.Groups);
                //    double curDx0 = 0;
                //    double curDy0 = 0;
                //    double curDx = 0;
                //    double curDy = 0;
                //    if (force != null)
                //    {
                //        curDx0 = this.fV.GetForcebyIndex(index).Fx;
                //        curDy0 = this.fV.GetForcebyIndex(index).Fy;
                //        curDx = this.fV.GetForcebyIndex(index).Fx;
                //        curDy = this.fV.GetForcebyIndex(index).Fy;
                //        if (this.IsTopCos == true)
                //        {
                //            foreach (PolygonObject obj in group.ListofObjects)
                //            {
                //                tagID = obj.ID;
                //                fType = obj.FeatureType;
                //                vp = this.VD.GetVPbyIDandType(tagID, fType);
                //                vp.TopologicalConstraint(curDx0, curDx0, 0.001, out curDx, out curDy);
                //                curDx0 = curDx;
                //                curDy0 = curDy;
                //            }
                //            this.D[3 * index, 0] = curDx;
                //            this.D[3 * index + 1, 0] = curDy;
                //        }
                //        //纠正拓扑错误
                //        curNode.X += curDx;
                //        curNode.Y += curDy;
                //        foreach (PolygonObject obj in group.ListofObjects)
                //        {
                //            foreach (TriNode curPoint in obj.PointList)
                //            {
                //                curPoint.X += curDx;
                //                curPoint.Y += curDy;
                //            }
                //        }
                //    }
                //    else
                //    {
                //        this.D[3 * index, 0] = curDx;
                //        this.D[3 * index + 1, 0] = curDy;
                //    }
                //}
                #endregion
            }
        }
    }
}
