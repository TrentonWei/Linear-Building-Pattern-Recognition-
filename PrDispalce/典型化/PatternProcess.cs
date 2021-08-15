using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PrDispalce.典型化;
using AuxStructureLib;

namespace PrDispalce.典型化
{
    //涉及pattern的操作
    class PatternProcess
    {
        /// <summary>
        /// 将给定的两个pattern变成一个
        /// </summary>
        /// <param name="Pattern1"></param> 更重要的pattern
        /// <param name="Pattern"></param>
        /// <returns></returns>
        public Pattern MergeTwoPatterns(Pattern Pattern1,Pattern Pattern2)
        {
            Pattern NewPattern = new Pattern(Pattern1.PatternID,Pattern1.NormalizedPatternObjects);
            NewPattern.NormalizedPatternObjects = Pattern1.NormalizedPatternObjects;

            #region 获取Pattern1的轴线
            double X1 = Pattern1.NormalizedPatternObjects[0].CalProxiNode().X;
            double Y1 = Pattern1.NormalizedPatternObjects[0].CalProxiNode().Y;

            double X2 = Pattern1.NormalizedPatternObjects[Pattern1.NormalizedPatternObjects.Count - 1].CalProxiNode().X;
            double Y2 = Pattern1.NormalizedPatternObjects[Pattern1.NormalizedPatternObjects.Count - 1].CalProxiNode().Y;

            double k = (Y2 - Y1) / (X2 - X1);
            double b = Y1 - k * X1;
            #endregion

            #region 将Pattern2中投影点在pattern1外的建筑物Projected到pattern1上
            for (int i = 0; i < Pattern2.NormalizedPatternObjects.Count; i++)
            {
                double XMin = Pattern1.NormalizedPatternObjects[0].CalProxiNode().X;double YMin=Pattern1.NormalizedPatternObjects[0].CalProxiNode().Y;
                double XMax = Pattern1.NormalizedPatternObjects[Pattern1.NormalizedPatternObjects.Count - 1].CalProxiNode().X;
                double YMax = Pattern1.NormalizedPatternObjects[Pattern1.NormalizedPatternObjects.Count - 1].CalProxiNode().Y;

                if (XMin > XMax)
                {
                    double Cache = XMax;
                    XMax = XMin;
                    XMin = Cache;
                }

                if (YMin > YMax)
                {
                    double Cache = YMax;
                    YMax = YMin;
                    YMin = Cache;
                }

                if (Pattern2.NormalizedPatternObjects[i].CalProxiNode().X < XMin || Pattern2.NormalizedPatternObjects[i].CalProxiNode().X > XMax
                    && Pattern2.NormalizedPatternObjects[i].CalProxiNode().Y < YMin || Pattern2.NormalizedPatternObjects[i].CalProxiNode().Y > YMax)
                {
                    double NewX = (Pattern2.NormalizedPatternObjects[i].CalProxiNode().X + k * Pattern2.NormalizedPatternObjects[i].CalProxiNode().Y - k * b) / (1 + k * k);
                    double NewY = (k * Pattern2.NormalizedPatternObjects[i].CalProxiNode().X + k * k * Pattern2.NormalizedPatternObjects[i].CalProxiNode().Y + b) / (1 + k * k);

                    PolygonObject NewPolygonObject = new PolygonObject(Pattern2.NormalizedPatternObjects[i].ID, Pattern2.NormalizedPatternObjects[i].PointList);

                    double curDx = NewX - Pattern2.NormalizedPatternObjects[i].CalProxiNode().X;
                    double curDy = NewY - Pattern2.NormalizedPatternObjects[i].CalProxiNode().Y;

                    //更新多边形点集的每一个点坐标
                    foreach (TriNode curPoint in NewPolygonObject.PointList)
                    {
                        curPoint.X += curDx;
                        curPoint.Y += curDy;
                    }

                    NewPattern.NormalizedPatternObjects.Add(NewPolygonObject);
                }
            }

            #endregion

            return NewPattern;
        }
    }
}
