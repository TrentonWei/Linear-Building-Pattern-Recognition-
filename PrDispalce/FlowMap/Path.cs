using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrDispalce.FlowMap
{
    class Path
    {
        public Tuple<int, int> startPoint;//起点
        public Tuple<int, int> endPoint;//终点
        public List<Tuple<int, int>> ePath;//路径
        public double Length = 0;//长度
        public double Volume = 0;//流量

        public List<Path> FlowInPath = new List<Path>();//其它路径流入的河流
        public List<Path> FlowOutPath = new List<Path>();//流入其它的路径标识

         /// <summary>
        /// 构造函数
        /// </summary>
        public Path(Tuple<int,int> sPoint,Tuple<int,int> ePoint,List<Tuple<int,int>> ePath)
        {
            this.startPoint = sPoint;
            this.endPoint = ePoint;
            this.ePath = ePath;
        }

        /// <summary>
        /// 获取给定路径的长度
        /// </summary>
        /// <param name="ShortestPath"></param>
        /// <returns></returns>
        public double GetPathLength()
        {
            if (ePath.Count <= 1)
            {
                return Length;
            }

            for (int i = 0; i <ePath.Count - 1; i++)
            {
                if (ePath[i].Item1 == ePath[i + 1].Item1 ||
                   ePath[i].Item2 == ePath[i + 1].Item2)
                {
                    Length = Length + 1;
                }

                else
                {
                    Length = Length + Math.Sqrt(2);
                }
            }

            return Length;
        }

        /// <summary>
        /// 获取给定路径的长度
        /// </summary>
        /// <param name="ShortestPath"></param>
        /// <returns></returns>
        public double GetPathLengthType(Dictionary<Tuple<int, int>, int> GridType)
        {
            if (ePath.Count <= 1)
            {
                return Length;
            }

            for (int i = 0; i < ePath.Count - 1; i++)
            {
                if (ePath[i].Item1 == ePath[i + 1].Item1 ||
                   ePath[i].Item2 == ePath[i + 1].Item2)
                {
                    Length = (GridType[ePath[i]] + 1) / 2 + (GridType[ePath[i + 1]] + 1) / 2 + Length;
                }

                else
                {
                    Length = ((GridType[ePath[i]] + 1) / 2 + (GridType[ePath[i + 1]] + 1) / 2) * Math.Sqrt(2) + Length;
                }
            }

            return Length;
        }

        /// <summary>
        /// 获取给定路径的长度（Type类型对调）
        /// </summary>
        /// <param name="ShortestPath"></param>
        /// <returns></returns>
        public double GetPathLengthTypeRever(Dictionary<Tuple<int, int>, int> GridType)
        {
            if (ePath.Count <= 1)
            {
                return Length;
            }     

            for (int i = 0; i < ePath.Count - 1; i++)
            {
                if (ePath[i].Item1 == ePath[i + 1].Item1 ||
                   ePath[i].Item2 == ePath[i + 1].Item2)
                {
                    Length = (Math.Abs(GridType[ePath[i]]-10) + 1) / 2 + (Math.Abs(GridType[ePath[i + 1]]-10) + 1) / 2 + Length;
                }

                else
                {
                    Length = ((Math.Abs(GridType[ePath[i]]-10) + 1) / 2 + (Math.Abs(GridType[ePath[i + 1]]-10) + 1) / 2) * Math.Sqrt(2) + Length;
                }
            }

            return Length;
        }
    }
}
