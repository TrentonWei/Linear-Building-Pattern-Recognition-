using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuxStructureLib
{
    public class Interpretation
    {
        /// <summary>
        /// 线
        /// </summary>
        public List<PolylineObject> PLList = null;
        /// <summary>
        /// 面
        /// </summary>
        public List<PolygonObject> PPList = null;
        /// <summary>
        /// 顶点列表
        /// </summary>
        public List<TriNode> VextexList = null;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="netWork">道路网</param>
        public Interpretation(List<PolylineObject> plList, List<PolygonObject> ppList, List<TriNode>  vextexList)
        {
            this.PLList = plList;
            this.PPList = ppList;
            this.VextexList = vextexList;
        }

        /// <summary>
        /// 加密(点有偏移)[double转float类型来实现]
        /// </summary>
        public void Interpretate(int k)
        {
            float w = ComAveLineLength();       //计算平均线段长度
            int count = this.VextexList.Count;
            w = w / k;
            if (PLList != null)
            {
                foreach (PolylineObject curPL in PLList)
                {
                    int pCount = curPL.PointList.Count;
                    if (pCount < 2)
                    {
                        continue;
                    }
                    int LID = curPL.ID;
                    List<TriNode> newPointList = new List<TriNode>();
                    List<TriNode> resPList = null;
                    for (int i = 0; i < pCount - 1; i++)
                    {
                        TriNode p1 = curPL.PointList[i];
                        TriNode p2 = curPL.PointList[i + 1];
                        resPList = InterpretateLine(p1, p2, w, LID);
                        newPointList.Add(p1);
                        if (resPList != null && resPList.Count != 0)
                        {
                            for (int j = 0; j < resPList.Count; j++)
                            {
                                TriNode p = resPList[j];
                                p.ID = count;

                                p.FeatureType = FeatureType.PolylineType;

                                this.VextexList.Add(p);
                                count++;
                            }
                            newPointList.AddRange(resPList);
                        }
                        //newPointList.Add(p2);
                    }
                    newPointList.Add(curPL.PointList[pCount - 1]);
                    curPL.PointList = newPointList;
                }
            }
            if (PPList != null)
            {

                foreach (PolygonObject curPP in PPList)
                {
                    int pCount = curPP.PointList.Count;
                    if (pCount < 3)
                    {
                        continue;
                    }
                    int PID = curPP.ID;
                    List<TriNode> newPointList = new List<TriNode>();
                    List<TriNode> resPList = null;
                    for (int i = 0; i < pCount; i++)
                    {
                        TriNode p1 = null;
                        TriNode p2 = null;
                        if (i == pCount - 1)
                        {
                            p1 = curPP.PointList[pCount - 1];
                            p2 = curPP.PointList[0];
                        }
                        else
                        {
                            p1 = curPP.PointList[i];
                            p2 = curPP.PointList[i + 1];
                        }

                        resPList = InterpretateLine(p1, p2, w, PID);
                        newPointList.Add(p1);
                        if (resPList != null && resPList.Count != 0)
                        {
                            for (int j = 0; j < resPList.Count; j++)
                            {
                                TriNode p = resPList[j];
                                p.ID = count;

                                p.FeatureType = FeatureType.PolygonType;

                                this.VextexList.Add(p);
                                count++;
                            }
                            newPointList.AddRange(resPList);
                        }
                    }
                    curPP.PointList = newPointList;
                }
            }
        }

        /// <summary>
        /// 加密（点无偏移）
        /// </summary>
        /// <param name="k"></param>
        /// MinDis是否考虑最短长度
        public void Interpretate2(int k)
        {
            double w = ComAveLineLength();       //计算平均线段长度
            int count = this.VextexList.Count;
            w = w / k;
            if (PLList != null)
            {
                foreach (PolylineObject curPL in PLList)
                {
                    int pCount = curPL.PointList.Count;
                    if (pCount < 2)
                    {
                        continue;
                    }
                    int LID = curPL.ID;
                    List<TriNode> newPointList = new List<TriNode>();
                    List<TriNode> resPList = null;
                    for (int i = 0; i < pCount - 1; i++)
                    {
                        TriNode p1 = curPL.PointList[i];
                        TriNode p2 = curPL.PointList[i + 1];
                        resPList = InterpretateLine2(p1, p2, w, LID);
                        newPointList.Add(p1);
                        if (resPList != null && resPList.Count != 0)
                        {
                            for (int j = 0; j < resPList.Count; j++)
                            {
                                TriNode p = resPList[j];
                                p.ID = count;

                                p.FeatureType = FeatureType.PolylineType;

                                this.VextexList.Add(p);
                                count++;
                            }
                            newPointList.AddRange(resPList);
                        }
                        //newPointList.Add(p2);
                    }
                    newPointList.Add(curPL.PointList[pCount - 1]);
                    curPL.PointList = newPointList;
                }
            }
            if (PPList != null)
            {

                foreach (PolygonObject curPP in PPList)
                {
                    int pCount = curPP.PointList.Count;
                    if (pCount < 3)
                    {
                        continue;
                    }
                    int PID = curPP.ID;
                    List<TriNode> newPointList = new List<TriNode>();
                    List<TriNode> resPList = null;
                    for (int i = 0; i < pCount; i++)
                    {
                        TriNode p1 = null;
                        TriNode p2 = null;
                        if (i == pCount - 1)
                        {
                            p1 = curPP.PointList[pCount - 1];
                            p2 = curPP.PointList[0];
                        }
                        else
                        {
                            p1 = curPP.PointList[i];
                            p2 = curPP.PointList[i + 1];
                        }

                        resPList = InterpretateLine2(p1, p2, w, PID, true);
                        newPointList.Add(p1);
                        if (resPList != null && resPList.Count != 0)
                        {
                            for (int j = 0; j < resPList.Count; j++)
                            {
                                TriNode p = resPList[j];
                                p.ID = count;

                                p.FeatureType = FeatureType.PolygonType;

                                this.VextexList.Add(p);
                                count++;
                            }
                            newPointList.AddRange(resPList);
                        }
                    }
                    curPP.PointList = newPointList;
                }
            }
        }

        /// <summary>
        /// 计算线段平均长度
        /// </summary>
        /// <returns>平均长度</returns>
        private float ComAveLineLength()
        {
            int count = 0;
            double sum = 0;
            if (PLList != null)
            {
                foreach (PolylineObject curL in PLList)
                {
                    int pCount = curL.PointList.Count;
                    for (int i = 0; i < pCount - 1; i++)
                    {
                        TriNode p1 = curL.PointList[i];
                        TriNode p2 = curL.PointList[i + 1];
                        double length = Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                        sum = sum + length;
                        count++;
                    }
                }
            }

            if (PPList != null)
            {
                foreach (PolygonObject curP in PPList)
                {
                    int pCount = curP.PointList.Count;
                    for (int i = 0; i < pCount; i++)
                    {
                        TriNode p1 = null;
                        TriNode p2 = null;
                        if (i == pCount - 1)
                        {
                            p1 = curP.PointList[pCount - 1];
                            p2 = curP.PointList[0];

                        }
                        else
                        {
                            p1 = curP.PointList[i];
                            p2 = curP.PointList[i + 1];
                        }
                        double length = Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                        sum = sum + length;
                        count++;
                    }
                }
            }


            if (count == 0)
                return -1f;
            return (float)(sum / count);
        }

        /// <summary>
        /// 向一段线段中添加加密点（点有偏移）[点的偏移通过控制Double与Float类型的转换来实现]
        /// </summary>
        /// <param name="p1">起点</param>
        /// <param name="p2">终点</param>
        /// <param name="w">平均线段长度</param>
        /// <returns></returns>
        private List<TriNode> InterpretateLine(TriNode p1, TriNode p2, float w, int RID)
        {

            float length = (float)Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
            if (w >= length)
                return null;
            int k = (int)Math.Floor((length / w));
            List<TriNode> res = new List<TriNode>();
            for (int i = 1; i < k; i++)
            {
                double f = i * w / (length - i * w);
                TriNode p = new TriNode();
                p.X = (float)((p1.X + f * p2.X) / (1 + f));
                p.Y = (float)((p1.Y + f * p2.Y) / (1 + f));
                p.TagValue = RID;
  
                res.Add(p);
            }

            double fk = k * w / (length - k * w);
            TriNode pk = new TriNode();
            pk.X = (float)((p1.X + fk * p2.X) / (1 + fk));
            pk.Y = (float)((p1.Y + fk * p2.Y) / (1 + fk));
            pk.TagValue = RID;
            length = (float)Math.Sqrt((pk.X - p2.X) * (pk.X - p2.X) + (pk.Y - p2.Y) * (pk.Y - p2.Y));

            if (length > 0.5 * w)
            {
                res.Add(pk);
            }

            return res;
        }

        /// <summary>
        /// 向一段线段中添加加密点（点无偏移）
        /// </summary>
        /// <param name="p1">起点</param>
        /// <param name="p2">终点</param>
        /// <param name="w">平均线段长度</param>
        /// <returns></returns>
        private List<TriNode> InterpretateLine2(TriNode p1, TriNode p2, double w, int RID)
        {
            double length = Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
            if (w >= length)
                return null;
            int k = (int)Math.Floor((length / w));
            List<TriNode> res = new List<TriNode>();
            for (int i = 1; i < k; i++)
            {
                double f = i * w / (length - i * w);
                TriNode p = new TriNode();
                p.X = ((p1.X + f * p2.X) / (1 + f));
                p.Y = ((p1.Y + f * p2.Y) / (1 + f));
                p.TagValue = RID;

                res.Add(p);
            }

            double fk = k * w / (length - k * w);
            TriNode pk = new TriNode();
            pk.X = ((p1.X + fk * p2.X) / (1 + fk));
            pk.Y = ((p1.Y + fk * p2.Y) / (1 + fk));
            pk.TagValue = RID;
            length = Math.Sqrt((pk.X - p2.X) * (pk.X - p2.X) + (pk.Y - p2.Y) * (pk.Y - p2.Y));

            if (length > 0.5 * w)
            {
                res.Add(pk);
            }

            return res;
        }

        /// <summary>
        /// 向一段线段中添加加密点（点无偏移）
        /// </summary>
        /// <param name="p1">起点</param>
        /// <param name="p2">终点</param>
        /// <param name="w">平均线段长度</param>
        /// MinDis 考虑MinDis，若线段长度较短，也最少插入一个点
        /// <returns></returns>
        private List<TriNode> InterpretateLine2(TriNode p1, TriNode p2, double w, int RID, bool MinDis)
        {
            double length = Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
            List<TriNode> res = new List<TriNode>();

            if (w * 2 >= length)
            {
                if (MinDis)
                {
                    TriNode p = new TriNode();
                    p.X = (p1.X + p2.X) / 2;
                    p.Y = (p1.Y + p2.Y) / 2;
                    p.TagValue = RID;

                    res.Add(p);
                    return res;
                }

                else
                {
                    {
                        return null;
                    }
                }
            }


            int k = (int)Math.Floor((length / w));
            for (int i = 1; i < k; i++)
            {
                double f = i * w / (length - i * w);
                TriNode p = new TriNode();
                p.X = ((p1.X + f * p2.X) / (1 + f));
                p.Y = ((p1.Y + f * p2.Y) / (1 + f));
                p.TagValue = RID;

                res.Add(p);
            }

            //double fk = k * w / (length - k * w);
            //TriNode pk = new TriNode();
            //pk.X = ((p1.X + fk * p2.X) / (1 + fk));
            //pk.Y = ((p1.Y + fk * p2.Y) / (1 + fk));
            //pk.TagValue = RID;
            //length = Math.Sqrt((pk.X - p2.X) * (pk.X - p2.X) + (pk.Y - p2.Y) * (pk.Y - p2.Y));

            //if (length > 0.5 * w)
            //{
                //res.Add(pk);
            //}

            return res;

        }
    }
}
