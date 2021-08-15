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
    class Cut
    {
        public TriEdge CutEdge = null;     

        #region 属性
        public double CutLength = 0;//计算cut的长度
        public double CutAngle11 = 0;//计算Cut的角度 Angle11与Angle12是一组，表示CutStart节点角度
        public double CutAngle12 = 0;
        public double CutAngle21 = 0;//Cut的角度特征，Angle21与Angle22是一组，表示CutEnd节点角度
        public double CutAngle22 = 0;
        public double StartAngle = 0;//Cut对应起始点的角度
        public double EndAngle = 0;//Cut对应终点的角度
        public double RConP = 0;//表示Cut执行后Cut减少的凹点
        public double CConP = 0;//特征点变化个数
        public double OrthCount = 0;//表示Cut后直角数量的变化(-2,-1,0,1,2,3,4)
        public double NodeCount=0;//表示Cut节点是建筑物节点的个数（1或2）
        public double RealNodeCount = 0;//表示Cut后节点个数的增加数量(0,1,2,3)
        public bool OnBoundary = false;//标识Cut是否与建筑物的一条边重合，无效Cut
        public bool StartLable = false;//false表示CutEdge的起点是选择待切割点；true表示CutEdge的终点是选择的待切割点
        public bool StructNodeDcrese = false;//false表示结构点没减少；true表示结构点减少（是前后相同的结构点）
        public bool StructNodeDecreseOutCut = false;//false表示结构没减少；true表示结构点减少（不包括Cut）
        public bool sStructNodeDecese = false;//false表示结构没减少；true表示结构点减少
        public bool RightAngleMaintain = true;//false表示没保持直角特征；true表示保持了直角特征

        public List<TriNode> ConcaveNodes = new List<TriNode>();//Cut删除的对应凹点
        public List<Cut> IntersectCuts = new List<Cut>();//Cut相交的Cuts
        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public Cut(TriEdge pCutEdge)
        {
            this.CutEdge = pCutEdge;
        }
    }
}
