using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;

namespace PrDispalce.典型化
{
    /// <summary>
    /// 确定新建筑物的位置和形态
    /// </summary>
    class LocAndRepNewbuilding
    {
        /// <summary>
        /// 根据给定的两个建筑物，确定新建筑物的形态与位置
        /// </summary>
        /// <param name="pObecjt1"></param>
        /// <param name="pObject2"></param>
        /// boundaryLabel=true 顾及边界位置；else 不顾及边界位置
        /// <returns></returns>
        public PolygonObject LRNewPolygon(PolygonObject pObject1,PolygonObject pObject2)
        {
            PolygonObject NewPolygon = null;
   
            double NewX = (pObject1.CalProxiNode().X * pObject1.Area + pObject2.CalProxiNode().X * pObject2.Area) / (pObject1.Area + pObject2.Area);
            double NewY = (pObject1.CalProxiNode().Y * pObject1.Area + pObject2.CalProxiNode().Y * pObject2.Area) / (pObject1.Area + pObject2.Area);

            #region 将面积大的建筑物移到新位置
            if (pObject1.Area > pObject2.Area)
            {
                NewPolygon = pObject1;               
            }

            else
            {
                NewPolygon = pObject2;
            }

            double curDx = NewX - NewPolygon.CalProxiNode().X;
            double curDy = NewY - NewPolygon.CalProxiNode().Y;

            //更新多边形点集的每一个点坐标
            foreach (TriNode curPoint in NewPolygon.PointList)
            {
                curPoint.X += curDx;
                curPoint.Y += curDy;
            }
            #endregion

            return NewPolygon;
        }

        /// <summary>
        /// 根据给定的两个建筑物，确定新建筑物的形态
        /// </summary>
        /// <param name="pObject1"></param>
        /// <param name="pObject2"></param>
        /// <returns></returns>
        public PolygonObject RNewPolygon(PolygonObject pObject1, PolygonObject pObject2)
        {
            PolygonObject NewPolygon = null;

            #region 保留面积大的建筑物
            if (pObject1.Area > pObject2.Area)
            {
                NewPolygon = pObject1;
            }

            else
            {
                NewPolygon = pObject2;
            }
            #endregion

            return NewPolygon;
        }

        /// <summary>
        /// 根据给定的pattern和需要合并的建筑物，确定pattern中其它建筑物的位置（按建筑物重心距离确定新建筑物位置）
        /// </summary>
        /// <param name="OriginalPattern"></param>
        /// <param name="BuildingIndex"></param>
        /// <returns></returns>
        public Pattern NewPattern(Pattern OriginalPattern,int BuildingIndex)
        {
            Pattern NewPattern = new Pattern(OriginalPattern.PatternID, OriginalPattern.PatternObjects, OriginalPattern.bSiSim, OriginalPattern.bOriSim);
            PolygonObject NewPolygonObject = this.LRNewPolygon(OriginalPattern.PatternObjects[BuildingIndex], OriginalPattern.PatternObjects[BuildingIndex + 1]);
            NewPattern.PatternObjects[BuildingIndex]=NewPolygonObject;NewPattern.PatternObjects.RemoveAt(BuildingIndex+1);

            #region 计算轴线的总长度
            double SumLength = 0;
            for (int i = 0; i < NewPattern.PatternObjects.Count-1; i++)
            {
                SumLength = SumLength + Math.Sqrt((NewPattern.PatternObjects[i].CalProxiNode().X - NewPattern.PatternObjects[i + 1].CalProxiNode().X) * (NewPattern.PatternObjects[i].CalProxiNode().X - NewPattern.PatternObjects[i + 1].CalProxiNode().X)
                    + (NewPattern.PatternObjects[i].CalProxiNode().Y - NewPattern.PatternObjects[i + 1].CalProxiNode().Y) * (NewPattern.PatternObjects[i].CalProxiNode().Y - NewPattern.PatternObjects[i + 1].CalProxiNode().Y));
            }

            double InterLength = SumLength / (NewPattern.PatternObjects.Count - 1);
            #endregion

            #region 计算建筑物的新位置
            for (int i = 1; i < NewPattern.PatternObjects.Count - 1; i++)
            {
                double Length = Math.Sqrt((NewPattern.PatternObjects[i - 1].CalProxiNode().X - NewPattern.PatternObjects[i].CalProxiNode().X) * (NewPattern.PatternObjects[i - 1].CalProxiNode().X - NewPattern.PatternObjects[i].CalProxiNode().X)
                    + (NewPattern.PatternObjects[i - 1].CalProxiNode().Y - NewPattern.PatternObjects[i].CalProxiNode().Y) * (NewPattern.PatternObjects[i - 1].CalProxiNode().Y - NewPattern.PatternObjects[i].CalProxiNode().Y));
                double NewX = 0; double NewY = 0; double Times = 0;
                
                if (InterLength > Length)
                {
                    Times = InterLength / (InterLength - Length) * (-1);                   
                }

                else
                {
                    Times = InterLength / (Length-InterLength);
                }

                NewX = (NewPattern.PatternObjects[i - 1].CalProxiNode().X + Times * NewPattern.PatternObjects[i].CalProxiNode().X) / (1 + Times);
                NewY = (NewPattern.PatternObjects[i - 1].CalProxiNode().Y + Times * NewPattern.PatternObjects[i].CalProxiNode().Y) / (1 + Times);

                double curDx = NewX -NewPattern.PatternObjects[i].CalProxiNode().X;
                double curDy = NewY - NewPattern.PatternObjects[i].CalProxiNode().Y;

                //更新多边形点集的每一个点坐标
                foreach (TriNode curPoint in NewPattern.PatternObjects[i].PointList)
                {
                    curPoint.X += curDx;
                    curPoint.Y += curDy;
                }
            }
            #endregion

            return NewPattern;
        }
    }
}
