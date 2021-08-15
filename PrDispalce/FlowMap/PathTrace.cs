using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrDispalce.FlowMap
{
    /// <summary>
    /// 给定点到Grids中所有点的路径搜索
    /// </summary>
    /// 
    [Serializable]
    class PathTrace
    {

        [Serializable]
        class Node
        {
            public Tuple<int, int> GridID;//Node本身的节点编号
            public Tuple<int,int> LevelSort;//搜索所处的层级（层级+在当前层中的排序）
            public List<Tuple<int,int>> Childs { get; set; }
        }

        Dictionary<Tuple<int, int>, Node> PathtraceRes = new Dictionary<Tuple<int, int>, Node>();//表示每一个节点对应的层级、编号和子节点
        public Tuple<int, int> startPoint;//起点

        /// <summary>
        /// 宽度优先搜索方法（不考虑格网的权重）
        /// 下1；左2；上3；右4；左下5；左上6；右上7；右下8
        /// </summary>
        /// <param name="StartPoint">起点</param>
        /// <param name="KeyList">格网</param>
        /// <returns></returns>
        public void MazeAlg(List<Tuple<int, int>> JudgeList, List<Tuple<int, int>> KeyList)
        {
            #region 遍历更新
            int LEVEL = 0;//标识每一个点的层级
            if (JudgeList.Count > 0)
            {
                LEVEL++;

                foreach (Tuple<int, int> IJ in JudgeList)
                {
                    if (KeyList.Contains(IJ))
                    {
                        KeyList.Remove(IJ);
                    }
                }

                JudgeList = this.GetJudgeList(JudgeList, KeyList, LEVEL);
                this.MazeAlg(JudgeList, KeyList);
            }
            #endregion
        }

        /// <summary>
        /// 宽度优先搜索方法（考虑格网的权重）
        /// 下1；左2；上3；右4；左下5；左上6；右上7；右下8
        /// </summary>
        /// <param name="StartPoint">起点</param>
        /// <param name="KeyList">格网权重编码</param>
        /// Type=1，当前邻近的要素最多只能有两个；
        /// Type=2，对当前邻近的要素数量不做限制；
        /// <returns></returns>
        public void MazeAlg(List<Tuple<int, int>> JudgeList, Dictionary<Tuple<int, int>, double> KeyList,int Type)
        {
            #region 遍历更新
            int LEVEL = 0;//标识每一个点的层级
            if (JudgeList.Count > 0)
            {
                LEVEL++;

                foreach (Tuple<int, int> IJ in JudgeList)
                {
                    if (KeyList.Keys.Contains(IJ)) 
                    {
                        KeyList.Remove(IJ);
                    }
                }

                JudgeList = this.GetJudgeList(JudgeList, KeyList, LEVEL, Type, null);
                this.MazeAlg(JudgeList, KeyList,Type);
            }
            #endregion
        }

        /// <summary>
        /// 限定搜索方向的宽度优先搜索方法（考虑格网权重）
        /// </summary>
        /// <param name="JudgeList">起点</param>
        /// <param name="KeyList">格网权重编码</param>
        /// <param name="Type"></param>
        /// Type=1，当前邻近的要素最多只能有两个；
        /// Type=2，对当前邻近的要素数量不做限制；
        /// <param name="DirList"></param>对当前邻近的要素数量按照方向约束获取邻近目标（方向做限制）
        /// <param name="endPoint"></param>
        public void MazeAlg(List<Tuple<int, int>> JudgeList, Dictionary<Tuple<int, int>, double> KeyList, int Type, List<int> DirList, Tuple<int, int> endPoint)
        {
            #region 遍历更新
            int LEVEL = 0;//标识每一个点的层级
            if (JudgeList.Count > 0 && !JudgeList.Contains(endPoint))
            {
                LEVEL++;

                foreach (Tuple<int, int> IJ in JudgeList)
                {
                    if (KeyList.Keys.Contains(IJ))
                    {
                        KeyList.Remove(IJ);
                    }
                }

                JudgeList = this.GetJudgeList(JudgeList, KeyList, LEVEL, Type, DirList);
                this.MazeAlg(JudgeList, KeyList, Type, DirList, endPoint);
            }
            #endregion
        }

        /// <summary>
        /// 限定搜索方向的宽度优先搜索方法（考虑格网权重）
        /// </summary>
        /// <param name="JudgeList">起点</param>
        /// <param name="KeyList">格网权重编码</param>
        /// <param name="Type"></param>
        /// Type=1，当前邻近的要素最多只能有两个；
        /// Type=2，对当前邻近的要素数量不做限制；
        /// <param name="DirList"></param>对当前邻近的要素数量按照方向约束获取邻近目标（方向做限制）
        public void MazeAlg(List<Tuple<int, int>> JudgeList, Dictionary<Tuple<int, int>, double> KeyList, int Type, List<int> DirList)
        {
            #region 遍历更新
            int LEVEL = 0;//标识每一个点的层级
            if (JudgeList.Count >0)
            {
                LEVEL++;

                foreach (Tuple<int, int> IJ in JudgeList)
                {
                    if (KeyList.Keys.Contains(IJ))
                    {
                        KeyList.Remove(IJ);
                    }
                }

                JudgeList = this.GetJudgeList(JudgeList, KeyList, LEVEL, Type, DirList);
                this.MazeAlg(JudgeList, KeyList, Type, DirList);
            }
            #endregion
        }

        /// <summary>
        /// 限定搜索方向的宽度优先搜索方法（考虑格网权重）[考虑网格距离差异]
        /// </summary>
        /// <param name="JudgeList">起点</param>
        /// <param name="KeyList">格网权重编码</param>
        /// <param name="Type"></param>
        /// Type=1，当前邻近的要素最多只能有两个；
        /// Type=2，对当前邻近的要素数量不做限制；
        /// <param name="DirList"></param>对当前邻近的要素数量按照方向约束获取邻近目标（方向做限制）
        public void MazeAlgDis(List<Tuple<int, int>> JudgeList, Dictionary<Tuple<int, int>, double> KeyList, int Type, List<int> DirList,Dictionary<Tuple<int,int>,int> GridType)
        {
            #region 遍历更新
            int LEVEL = 0;//标识每一个点的层级
            if (JudgeList.Count > 0)
            {
                LEVEL++;

                foreach (Tuple<int, int> IJ in JudgeList)
                {
                    if (KeyList.Keys.Contains(IJ))
                    {
                        KeyList.Remove(IJ);
                    }
                }

                JudgeList = this.GetJudgeListDis(JudgeList, KeyList, LEVEL, Type, DirList,GridType);
                this.MazeAlgDis(JudgeList, KeyList, Type, DirList, GridType);
            }
            #endregion
        }

        /// <summary>
        /// 限定搜索方向的宽度优先搜索方法（考虑格网权重）[考虑网格距离差异]
        /// </summary>
        /// <param name="JudgeList">起点</param>
        /// <param name="KeyList">格网权重编码</param>
        /// <param name="Type"></param>
        /// Type=1，当前邻近的要素最多只能有两个；
        /// Type=2，对当前邻近的要素数量不做限制；
        /// <param name="DirList"></param>对当前邻近的要素数量按照方向约束获取邻近目标（方向做限制）
        public void MazeAlgDis(List<Tuple<int, int>> JudgeList, Dictionary<Tuple<int, int>, double> KeyList, int Type, List<int> DirList, Dictionary<Tuple<int, int>, int> GridType, Dictionary<Tuple<int, int>, int> GridVisit)
        {
            #region 遍历更新
            int LEVEL = 0;//标识每一个点的层级
            if (JudgeList.Count > 0)
            {
                LEVEL++;

                foreach (Tuple<int, int> IJ in JudgeList)
                {
                    if (KeyList.Keys.Contains(IJ))
                    {
                        int Test1 = GridType[IJ];
                        int Test2 = GridVisit[IJ];

                        if (Test1 > 0)
                        {
                            int testloc = 0;
                        }

                        if (GridVisit[IJ] == 0)
                        {
                            KeyList.Remove(IJ);
                        }
                        else
                        {
                            GridVisit[IJ]--;
                            int testLoc = 0;
                        }
                    }
                }

                JudgeList = this.GetJudgeListDis(JudgeList, KeyList, LEVEL, Type, DirList, GridType,GridVisit);
                this.MazeAlgDis(JudgeList, KeyList, Type, DirList, GridType,GridVisit);
            }
            #endregion
        }

        /// <summary>
        /// 宽度优先搜索方法（考虑格网的权重）按照搜索顺序搜索，输出结果包含了顺序！！
        /// 下1；左2；上3；右4；左下5；左上6；右上7；右下8
        /// </summary>
        /// <param name="TargetGrid">目标格网</param>
        /// <param name="RemainedGrids">现存的格网</param>
        /// 备注：该阶段移除了判断的要素
        /// Type=1限制最邻近的要素最多只能有两个；Type=2不对最邻近的方向个数做限制
        /// DirList=限定寻找的方向
        /// <returns></returns>
        public List<Tuple<int, int>> GetNearGrids(Tuple<int, int> TargetGrid, Dictionary<Tuple<int, int>, double> RemainedGrids, int Type, List<int> DirList)
        {
            List<Tuple<int, int>> NearGrids = new List<Tuple<int, int>>();
            Dictionary<Tuple<int, int>, double> CacheGridWeigh = new Dictionary<Tuple<int, int>, double>();

            #region 添加key和对应的Weight，存储进CacheGridWeigh
            double weighi1j = 0;
            Tuple<int, int> i1j = new Tuple<int, int>(TargetGrid.Item1 + 1, TargetGrid.Item2);//上侧目标
            if (RemainedGrids.Keys.Contains(i1j))
            {
                if (DirList != null )
                {
                    if (this.RightDir(TargetGrid, i1j, DirList))
                    {
                        weighi1j = RemainedGrids[i1j];
                        CacheGridWeigh.Add(i1j, weighi1j);
                    }
                }

                else
                {
                    weighi1j = RemainedGrids[i1j];
                    CacheGridWeigh.Add(i1j, weighi1j);
                }
            }

            double weighij_1 = 0;
            Tuple<int, int> ij_1 = new Tuple<int, int>(TargetGrid.Item1, TargetGrid.Item2 - 1);//左侧目标
            if (RemainedGrids.Keys.Contains(ij_1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, ij_1, DirList))
                    {
                        weighij_1 = RemainedGrids[ij_1];
                        CacheGridWeigh.Add(ij_1, weighij_1);
                    }
                }

                else
                {
                    weighij_1 = RemainedGrids[ij_1];
                    CacheGridWeigh.Add(ij_1, weighij_1);
                }
            }

            double weighi_1j = 0;
            Tuple<int, int> i_1j = new Tuple<int, int>(TargetGrid.Item1 - 1, TargetGrid.Item2);//下侧目标
            if (RemainedGrids.Keys.Contains(i_1j))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i_1j, DirList))
                    {
                        weighi_1j = RemainedGrids[i_1j];
                        CacheGridWeigh.Add(i_1j, weighi_1j);
                    }
                }

                else
                {
                    weighi_1j = RemainedGrids[i_1j];
                    CacheGridWeigh.Add(i_1j, weighi_1j);
                }
            }

            double weighij1 = 0;
            Tuple<int, int> ij1 = new Tuple<int, int>(TargetGrid.Item1, TargetGrid.Item2 + 1);//右侧目标
            if (RemainedGrids.Keys.Contains(ij1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, ij1, DirList))
                    {
                        weighij1 = RemainedGrids[ij1];
                        CacheGridWeigh.Add(ij1, weighij1);
                    }
                }

                else
                {
                    weighij1 = RemainedGrids[ij1];
                    CacheGridWeigh.Add(ij1, weighij1);
                }
            }

            double weighi1j_1 = 0;
            Tuple<int, int> i1j_1 = new Tuple<int, int>(TargetGrid.Item1 + 1, TargetGrid.Item2 - 1);//右下侧目标
            if (RemainedGrids.Keys.Contains(i1j_1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i1j_1, DirList))
                    {
                        weighi1j_1 = RemainedGrids[i1j_1];
                        CacheGridWeigh.Add(i1j_1, weighi1j_1);
                    }
                }

                else
                {
                    weighi1j_1 = RemainedGrids[i1j_1];
                    CacheGridWeigh.Add(i1j_1, weighi1j_1);
                }
            }

            double weighi_1j_1 = 0;
            Tuple<int, int> i_1j_1 = new Tuple<int, int>(TargetGrid.Item1 - 1, TargetGrid.Item2 - 1);//左下侧目标
            if (RemainedGrids.Keys.Contains(i_1j_1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i_1j_1, DirList))
                    {
                        weighi_1j_1 = RemainedGrids[i_1j_1];
                        CacheGridWeigh.Add(i_1j_1, weighi_1j_1);
                    }
                }

                else
                {
                    weighi_1j_1 = RemainedGrids[i_1j_1];
                    CacheGridWeigh.Add(i_1j_1, weighi_1j_1);
                }
            }

            double weighi_1j1 = 0;
            Tuple<int, int> i_1j1 = new Tuple<int, int>(TargetGrid.Item1 - 1, TargetGrid.Item2 + 1);//左上侧目标
            if (RemainedGrids.Keys.Contains(i_1j1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i_1j1, DirList))
                    {
                        weighi_1j1 = RemainedGrids[i_1j1];
                        CacheGridWeigh.Add(i_1j1, weighi_1j1);
                    }
                }

                else
                {
                    weighi_1j1 = RemainedGrids[i_1j1];
                    CacheGridWeigh.Add(i_1j1, weighi_1j1);
                }
            }

            double weighi1j1 = 0;
            Tuple<int, int> i1j1 = new Tuple<int, int>(TargetGrid.Item1 + 1, TargetGrid.Item2 + 1);//右上侧目标
            if (RemainedGrids.Keys.Contains(i1j1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i1j1, DirList))
                    {
                        weighi1j1 = RemainedGrids[i1j1];
                        CacheGridWeigh.Add(i1j1, weighi1j1);
                    }
                }

                else
                {
                    weighi1j1 = RemainedGrids[i1j1];
                    CacheGridWeigh.Add(i1j1, weighi1j1);
                }
            }
            #endregion

            int TestLocation = 0;

            #region 按顺序添加
            while (CacheGridWeigh.Keys.Count > 0)
            {
                Tuple<int, int> TargetKeys = CacheGridWeigh.Keys.ToList()[0];

                for (int i = 1; i < CacheGridWeigh.Keys.Count; i++)
                {
                    if (CacheGridWeigh[CacheGridWeigh.Keys.ToList()[i]] > CacheGridWeigh[TargetKeys])
                    {
                        TargetKeys = CacheGridWeigh.Keys.ToList()[i];
                    }
                }

                if (RemainedGrids.Keys.Contains(TargetKeys))
                {
                    NearGrids.Add(TargetKeys);
                    CacheGridWeigh.Remove(TargetKeys);
                    RemainedGrids.Remove(TargetKeys);
                }

                #region 添加邻近数量约束
                if (Type == 1 && NearGrids.Count >= 2)
                {
                    break;
                }
                #endregion
            }
            #endregion

            return NearGrids;
        }

        /// <summary>
        /// 宽度优先搜索方法（考虑格网的权重）按照搜索顺序搜索，输出结果包含了顺序！！
        /// 下1；左2；上3；右4；左下5；左上6；右上7；右下8
        /// </summary>
        /// <param name="TargetGrid">目标格网</param>
        /// <param name="RemainedGrids">现存的格网</param>
        /// 备注：该阶段移除了判断的要素
        /// Type=1限制最邻近的要素最多只能有两个；Type=2不对最邻近的方向个数做限制
        /// DirList=限定寻找的方向
        /// <returns></returns>
        public List<Tuple<int, int>> GetNearGridsDisType(Tuple<int, int> TargetGrid, Dictionary<Tuple<int, int>, double> RemainedGrids, int Type, List<int> DirList,Dictionary<Tuple<int,int>,int> GridType)
        {
            List<Tuple<int, int>> NearGrids = new List<Tuple<int, int>>();
            Dictionary<Tuple<int, int>, double> CacheGridWeigh = new Dictionary<Tuple<int, int>, double>();

            #region 添加key和对应的Weight，存储进CacheGridWeigh
            double weighi1j = 0;
            Tuple<int, int> i1j = new Tuple<int, int>(TargetGrid.Item1 + 1, TargetGrid.Item2);//上侧目标
            if (RemainedGrids.Keys.Contains(i1j))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i1j, DirList))
                    {
                        weighi1j = RemainedGrids[i1j];
                        CacheGridWeigh.Add(i1j, weighi1j);
                    }
                }

                else
                {
                    weighi1j = RemainedGrids[i1j];
                    CacheGridWeigh.Add(i1j, weighi1j);
                }
            }

            double weighij_1 = 0;
            Tuple<int, int> ij_1 = new Tuple<int, int>(TargetGrid.Item1, TargetGrid.Item2 - 1);//左侧目标
            if (RemainedGrids.Keys.Contains(ij_1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, ij_1, DirList))
                    {
                        weighij_1 = RemainedGrids[ij_1];
                        CacheGridWeigh.Add(ij_1, weighij_1);
                    }
                }

                else
                {
                    weighij_1 = RemainedGrids[ij_1];
                    CacheGridWeigh.Add(ij_1, weighij_1);
                }
            }

            double weighi_1j = 0;
            Tuple<int, int> i_1j = new Tuple<int, int>(TargetGrid.Item1 - 1, TargetGrid.Item2);//下侧目标
            if (RemainedGrids.Keys.Contains(i_1j))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i_1j, DirList))
                    {
                        weighi_1j = RemainedGrids[i_1j];
                        CacheGridWeigh.Add(i_1j, weighi_1j);
                    }
                }

                else
                {
                    weighi_1j = RemainedGrids[i_1j];
                    CacheGridWeigh.Add(i_1j, weighi_1j);
                }
            }

            double weighij1 = 0;
            Tuple<int, int> ij1 = new Tuple<int, int>(TargetGrid.Item1, TargetGrid.Item2 + 1);//右侧目标
            if (RemainedGrids.Keys.Contains(ij1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, ij1, DirList))
                    {
                        weighij1 = RemainedGrids[ij1];
                        CacheGridWeigh.Add(ij1, weighij1);
                    }
                }

                else
                {
                    weighij1 = RemainedGrids[ij1];
                    CacheGridWeigh.Add(ij1, weighij1);
                }
            }

            double weighi1j_1 = 0;
            Tuple<int, int> i1j_1 = new Tuple<int, int>(TargetGrid.Item1 + 1, TargetGrid.Item2 - 1);//右下侧目标
            if (RemainedGrids.Keys.Contains(i1j_1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i1j_1, DirList))
                    {
                        weighi1j_1 = RemainedGrids[i1j_1];
                        CacheGridWeigh.Add(i1j_1, weighi1j_1);
                    }
                }

                else
                {
                    weighi1j_1 = RemainedGrids[i1j_1];
                    CacheGridWeigh.Add(i1j_1, weighi1j_1);
                }
            }

            double weighi_1j_1 = 0;
            Tuple<int, int> i_1j_1 = new Tuple<int, int>(TargetGrid.Item1 - 1, TargetGrid.Item2 - 1);//左下侧目标
            if (RemainedGrids.Keys.Contains(i_1j_1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i_1j_1, DirList))
                    {
                        weighi_1j_1 = RemainedGrids[i_1j_1];
                        CacheGridWeigh.Add(i_1j_1, weighi_1j_1);
                    }
                }

                else
                {
                    weighi_1j_1 = RemainedGrids[i_1j_1];
                    CacheGridWeigh.Add(i_1j_1, weighi_1j_1);
                }
            }

            double weighi_1j1 = 0;
            Tuple<int, int> i_1j1 = new Tuple<int, int>(TargetGrid.Item1 - 1, TargetGrid.Item2 + 1);//左上侧目标
            if (RemainedGrids.Keys.Contains(i_1j1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i_1j1, DirList))
                    {
                        weighi_1j1 = RemainedGrids[i_1j1];
                        CacheGridWeigh.Add(i_1j1, weighi_1j1);
                    }
                }

                else
                {
                    weighi_1j1 = RemainedGrids[i_1j1];
                    CacheGridWeigh.Add(i_1j1, weighi_1j1);
                }
            }

            double weighi1j1 = 0;
            Tuple<int, int> i1j1 = new Tuple<int, int>(TargetGrid.Item1 + 1, TargetGrid.Item2 + 1);//右上侧目标
            if (RemainedGrids.Keys.Contains(i1j1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i1j1, DirList))
                    {
                        weighi1j1 = RemainedGrids[i1j1];
                        CacheGridWeigh.Add(i1j1, weighi1j1);
                    }
                }

                else
                {
                    weighi1j1 = RemainedGrids[i1j1];
                    CacheGridWeigh.Add(i1j1, weighi1j1);
                }
            }
            #endregion

            int TestLocation = 0;

            #region 按顺序添加
            while (CacheGridWeigh.Keys.Count > 0)
            {
                Tuple<int, int> TargetKeys = CacheGridWeigh.Keys.ToList()[0];

                for (int i = 1; i < CacheGridWeigh.Keys.Count; i++)
                {
                    //#region 计算距离
                    //double TargetDis = 0;
                    //if (TargetKeys.Item1 == TargetGrid.Item1 || TargetKeys.Item2 == TargetGrid.Item2)
                    //{
                    //    TargetDis = (GridType[TargetKeys] + 1) / 2 + (GridType[TargetGrid] + 1) / 2;
                    //}
                    //else
                    //{
                    //    TargetDis = TargetDis = (GridType[TargetKeys] + 1) / 2 * Math.Sqrt(2) + (GridType[TargetGrid] + 1) / 2 * Math.Sqrt(2);
                    //}

                    //double CacheDis = 0;
                    //if (CacheGridWeigh.Keys.ToList()[i].Item1 == TargetGrid.Item1 || CacheGridWeigh.Keys.ToList()[i].Item2 == TargetGrid.Item2)
                    //{
                    //    CacheDis = (GridType[CacheGridWeigh.Keys.ToList()[i]] + 1) / 2 + (GridType[TargetGrid] + 1) / 2;
                    //}
                    //else
                    //{
                    //    CacheDis = (GridType[CacheGridWeigh.Keys.ToList()[i]] + 1) / 2 * Math.Sqrt(2) + (GridType[TargetGrid] + 1) / 2 * Math.Sqrt(2);
                    //}
                    //#endregion

                    //if ((CacheGridWeigh[CacheGridWeigh.Keys.ToList()[i]]+1)/CacheDis> (CacheGridWeigh[TargetKeys]+1)/TargetDis)//+1是为了防止出现可能存在0值的情况
                    //{
                    //    TargetKeys = CacheGridWeigh.Keys.ToList()[i];
                    //}

                    double WTest1 = CacheGridWeigh[CacheGridWeigh.Keys.ToList()[i]];
                    double WTest2 = CacheGridWeigh[TargetKeys];

                    int TTest1 = GridType[CacheGridWeigh.Keys.ToList()[i]];
                    int TTest2 = GridType[TargetKeys];

                    double Test1 = (CacheGridWeigh[CacheGridWeigh.Keys.ToList()[i]] - GridType[CacheGridWeigh.Keys.ToList()[i]] * 10000000);
                    double Test2 = (CacheGridWeigh[TargetKeys] - CacheGridWeigh[TargetKeys] * 10000000);

                    ///保证Type小的区域优先被访问
                    if ((CacheGridWeigh[CacheGridWeigh.Keys.ToList()[i]] - GridType[CacheGridWeigh.Keys.ToList()[i]] * 10000000) > (CacheGridWeigh[TargetKeys] - GridType[TargetKeys] * 10000000))
                    {
                        TargetKeys = CacheGridWeigh.Keys.ToList()[i];
                    }
                }

                if (RemainedGrids.Keys.Contains(TargetKeys))
                {
                    NearGrids.Add(TargetKeys);
                    CacheGridWeigh.Remove(TargetKeys);
                    RemainedGrids.Remove(TargetKeys);
                }

                #region 添加邻近数量约束
                if (Type == 1 && NearGrids.Count >= 2)
                {
                    break;
                }
                #endregion
            }
            #endregion

            return NearGrids;
        }

        /// 宽度优先搜索方法（考虑格网的权重）按照搜索顺序搜索，输出结果包含了顺序！！
        /// 下1；左2；上3；右4；左下5；左上6；右上7；右下8
        /// </summary>
        /// <param name="TargetGrid">目标格网</param>
        /// <param name="RemainedGrids">现存的格网</param>
        /// 备注：该阶段移除了判断的要素
        /// Type=1限制最邻近的要素最多只能有两个；Type=2不对最邻近的方向个数做限制
        /// DirList=限定寻找的方向
        /// <returns></returns>
        public List<Tuple<int, int>> GetNearGridsDisType(Tuple<int, int> TargetGrid, Dictionary<Tuple<int, int>, double> RemainedGrids, int Type, List<int> DirList, Dictionary<Tuple<int, int>, int> GridType, Dictionary<Tuple<int, int>, int> GridVisit)
        {
            List<Tuple<int, int>> NearGrids = new List<Tuple<int, int>>();
            Dictionary<Tuple<int, int>, double> CacheGridWeigh = new Dictionary<Tuple<int, int>, double>();

            if (RemainedGrids.Keys.Contains(TargetGrid) && GridType[TargetGrid] > 0 && GridVisit[TargetGrid] != 0)
            {
                int Test1 = GridType[TargetGrid];//测试
                int Test2 = GridVisit[TargetGrid];
                NearGrids.Add(TargetGrid);
            }

            else
            {
                #region 添加key和对应的Weight，存储进CacheGridWeigh
                double weighi1j = 0;
                Tuple<int, int> i1j = new Tuple<int, int>(TargetGrid.Item1 + 1, TargetGrid.Item2);//上侧目标
                if (RemainedGrids.Keys.Contains(i1j))
                {
                    if (DirList != null)
                    {
                        if (this.RightDir(TargetGrid, i1j, DirList))
                        {
                            weighi1j = RemainedGrids[i1j];
                            CacheGridWeigh.Add(i1j, weighi1j);
                        }
                    }

                    else
                    {
                        weighi1j = RemainedGrids[i1j];
                        CacheGridWeigh.Add(i1j, weighi1j);
                    }
                }

                double weighij_1 = 0;
                Tuple<int, int> ij_1 = new Tuple<int, int>(TargetGrid.Item1, TargetGrid.Item2 - 1);//左侧目标
                if (RemainedGrids.Keys.Contains(ij_1))
                {
                    if (DirList != null)
                    {
                        if (this.RightDir(TargetGrid, ij_1, DirList))
                        {
                            weighij_1 = RemainedGrids[ij_1];
                            CacheGridWeigh.Add(ij_1, weighij_1);
                        }
                    }

                    else
                    {
                        weighij_1 = RemainedGrids[ij_1];
                        CacheGridWeigh.Add(ij_1, weighij_1);
                    }
                }

                double weighi_1j = 0;
                Tuple<int, int> i_1j = new Tuple<int, int>(TargetGrid.Item1 - 1, TargetGrid.Item2);//下侧目标
                if (RemainedGrids.Keys.Contains(i_1j))
                {
                    if (DirList != null)
                    {
                        if (this.RightDir(TargetGrid, i_1j, DirList))
                        {
                            weighi_1j = RemainedGrids[i_1j];
                            CacheGridWeigh.Add(i_1j, weighi_1j);
                        }
                    }

                    else
                    {
                        weighi_1j = RemainedGrids[i_1j];
                        CacheGridWeigh.Add(i_1j, weighi_1j);
                    }
                }

                double weighij1 = 0;
                Tuple<int, int> ij1 = new Tuple<int, int>(TargetGrid.Item1, TargetGrid.Item2 + 1);//右侧目标
                if (RemainedGrids.Keys.Contains(ij1))
                {
                    if (DirList != null)
                    {
                        if (this.RightDir(TargetGrid, ij1, DirList))
                        {
                            weighij1 = RemainedGrids[ij1];
                            CacheGridWeigh.Add(ij1, weighij1);
                        }
                    }

                    else
                    {
                        weighij1 = RemainedGrids[ij1];
                        CacheGridWeigh.Add(ij1, weighij1);
                    }
                }

                double weighi1j_1 = 0;
                Tuple<int, int> i1j_1 = new Tuple<int, int>(TargetGrid.Item1 + 1, TargetGrid.Item2 - 1);//右下侧目标
                if (RemainedGrids.Keys.Contains(i1j_1))
                {
                    if (DirList != null)
                    {
                        if (this.RightDir(TargetGrid, i1j_1, DirList))
                        {
                            weighi1j_1 = RemainedGrids[i1j_1];
                            CacheGridWeigh.Add(i1j_1, weighi1j_1);
                        }
                    }

                    else
                    {
                        weighi1j_1 = RemainedGrids[i1j_1];
                        CacheGridWeigh.Add(i1j_1, weighi1j_1);
                    }
                }

                double weighi_1j_1 = 0;
                Tuple<int, int> i_1j_1 = new Tuple<int, int>(TargetGrid.Item1 - 1, TargetGrid.Item2 - 1);//左下侧目标
                if (RemainedGrids.Keys.Contains(i_1j_1))
                {
                    if (DirList != null)
                    {
                        if (this.RightDir(TargetGrid, i_1j_1, DirList))
                        {
                            weighi_1j_1 = RemainedGrids[i_1j_1];
                            CacheGridWeigh.Add(i_1j_1, weighi_1j_1);
                        }
                    }

                    else
                    {
                        weighi_1j_1 = RemainedGrids[i_1j_1];
                        CacheGridWeigh.Add(i_1j_1, weighi_1j_1);
                    }
                }

                double weighi_1j1 = 0;
                Tuple<int, int> i_1j1 = new Tuple<int, int>(TargetGrid.Item1 - 1, TargetGrid.Item2 + 1);//左上侧目标
                if (RemainedGrids.Keys.Contains(i_1j1))
                {
                    if (DirList != null)
                    {
                        if (this.RightDir(TargetGrid, i_1j1, DirList))
                        {
                            weighi_1j1 = RemainedGrids[i_1j1];
                            CacheGridWeigh.Add(i_1j1, weighi_1j1);
                        }
                    }

                    else
                    {
                        weighi_1j1 = RemainedGrids[i_1j1];
                        CacheGridWeigh.Add(i_1j1, weighi_1j1);
                    }
                }

                double weighi1j1 = 0;
                Tuple<int, int> i1j1 = new Tuple<int, int>(TargetGrid.Item1 + 1, TargetGrid.Item2 + 1);//右上侧目标
                if (RemainedGrids.Keys.Contains(i1j1))
                {
                    if (DirList != null)
                    {
                        if (this.RightDir(TargetGrid, i1j1, DirList))
                        {
                            weighi1j1 = RemainedGrids[i1j1];
                            CacheGridWeigh.Add(i1j1, weighi1j1);
                        }
                    }

                    else
                    {
                        weighi1j1 = RemainedGrids[i1j1];
                        CacheGridWeigh.Add(i1j1, weighi1j1);
                    }
                }
                #endregion

                int TestLocation = 0;

                #region 按顺序添加
                while (CacheGridWeigh.Keys.Count > 0)
                {
                    Tuple<int, int> TargetKeys = CacheGridWeigh.Keys.ToList()[0];

                    for (int i = 1; i < CacheGridWeigh.Keys.Count; i++)
                    {
                        //#region 计算距离
                        //double TargetDis = 0;
                        //if (TargetKeys.Item1 == TargetGrid.Item1 || TargetKeys.Item2 == TargetGrid.Item2)
                        //{
                        //    TargetDis = (GridType[TargetKeys] + 1) / 2 + (GridType[TargetGrid] + 1) / 2;
                        //}
                        //else
                        //{
                        //    TargetDis = TargetDis = (GridType[TargetKeys] + 1) / 2 * Math.Sqrt(2) + (GridType[TargetGrid] + 1) / 2 * Math.Sqrt(2);
                        //}

                        //double CacheDis = 0;
                        //if (CacheGridWeigh.Keys.ToList()[i].Item1 == TargetGrid.Item1 || CacheGridWeigh.Keys.ToList()[i].Item2 == TargetGrid.Item2)
                        //{
                        //    CacheDis = (GridType[CacheGridWeigh.Keys.ToList()[i]] + 1) / 2 + (GridType[TargetGrid] + 1) / 2;
                        //}
                        //else
                        //{
                        //    CacheDis = (GridType[CacheGridWeigh.Keys.ToList()[i]] + 1) / 2 * Math.Sqrt(2) + (GridType[TargetGrid] + 1) / 2 * Math.Sqrt(2);
                        //}
                        //#endregion

                        //if ((CacheGridWeigh[CacheGridWeigh.Keys.ToList()[i]]+1)/CacheDis> (CacheGridWeigh[TargetKeys]+1)/TargetDis)//+1是为了防止出现可能存在0值的情况
                        //{
                        //    TargetKeys = CacheGridWeigh.Keys.ToList()[i];
                        //}

                        double WTest1 = CacheGridWeigh[CacheGridWeigh.Keys.ToList()[i]];//测试
                        double WTest2 = CacheGridWeigh[TargetKeys];

                        int TTest1 = GridType[CacheGridWeigh.Keys.ToList()[i]];
                        int TTest2 = GridType[TargetKeys];

                        double Test1 = (CacheGridWeigh[CacheGridWeigh.Keys.ToList()[i]] - GridType[CacheGridWeigh.Keys.ToList()[i]] * 10000000);
                        double Test2 = (CacheGridWeigh[TargetKeys] - CacheGridWeigh[TargetKeys] * 10000000);

                        ///保证Type小的区域优先被访问
                        if ((CacheGridWeigh[CacheGridWeigh.Keys.ToList()[i]] - GridType[CacheGridWeigh.Keys.ToList()[i]] * 10000000) > (CacheGridWeigh[TargetKeys] - GridType[TargetKeys] * 10000000))
                        {
                            TargetKeys = CacheGridWeigh.Keys.ToList()[i];
                        }
                    }

                    if (RemainedGrids.Keys.Contains(TargetKeys))
                    {
                        NearGrids.Add(TargetKeys);
                        CacheGridWeigh.Remove(TargetKeys);

                        if (GridVisit[TargetKeys] == 0)
                        {
                            RemainedGrids.Remove(TargetKeys);
                        }
                        //(测试用)
                    }

                    #region 添加邻近数量约束
                    if (Type == 1 && NearGrids.Count >= 2)
                    {
                        break;
                    }
                    #endregion
                }
                #endregion
            }

            return NearGrids;
        }

        /// <summary>
        /// 宽度优先搜索方法（考虑格网的权重）按照搜索顺序搜索，输出结果包含了顺序！！
        /// 下1；左2；上3；右4；左下5；左上6；右上7；右下8
        /// </summary>
        /// <param name="TargetGrid">目标格网</param>
        /// <param name="RemainedGrids">现存的格网</param>
        /// 备注：该阶段移除了判断的要素
        /// Type=1限制最邻近的要素最多只能有两个；Type=2不对最邻近的方向个数做限制
        /// DirList=限定寻找的方向
        /// <returns></returns>
        public List<Tuple<int, int>> GetNearGridsDis(Tuple<int, int> TargetGrid, Dictionary<Tuple<int, int>, double> RemainedGrids, int Type, List<int> DirList)
        {
            List<Tuple<int, int>> NearGrids = new List<Tuple<int, int>>();
            Dictionary<Tuple<int, int>, double> CacheGridWeigh = new Dictionary<Tuple<int, int>, double>();

            #region 添加key和对应的Weight，存储进CacheGridWeigh
            double weighi1j = 0;
            Tuple<int, int> i1j = new Tuple<int, int>(TargetGrid.Item1 + 1, TargetGrid.Item2);//上侧目标
            if (RemainedGrids.Keys.Contains(i1j))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i1j, DirList))
                    {
                        weighi1j = RemainedGrids[i1j];
                        CacheGridWeigh.Add(i1j, weighi1j);
                    }
                }

                else
                {
                    weighi1j = RemainedGrids[i1j];
                    CacheGridWeigh.Add(i1j, weighi1j);
                }
            }

            double weighij_1 = 0;
            Tuple<int, int> ij_1 = new Tuple<int, int>(TargetGrid.Item1, TargetGrid.Item2 - 1);//左侧目标
            if (RemainedGrids.Keys.Contains(ij_1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, ij_1, DirList))
                    {
                        weighij_1 = RemainedGrids[ij_1];
                        CacheGridWeigh.Add(ij_1, weighij_1);
                    }
                }

                else
                {
                    weighij_1 = RemainedGrids[ij_1];
                    CacheGridWeigh.Add(ij_1, weighij_1);
                }
            }

            double weighi_1j = 0;
            Tuple<int, int> i_1j = new Tuple<int, int>(TargetGrid.Item1 - 1, TargetGrid.Item2);//下侧目标
            if (RemainedGrids.Keys.Contains(i_1j))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i_1j, DirList))
                    {
                        weighi_1j = RemainedGrids[i_1j];
                        CacheGridWeigh.Add(i_1j, weighi_1j);
                    }
                }

                else
                {
                    weighi_1j = RemainedGrids[i_1j];
                    CacheGridWeigh.Add(i_1j, weighi_1j);
                }
            }

            double weighij1 = 0;
            Tuple<int, int> ij1 = new Tuple<int, int>(TargetGrid.Item1, TargetGrid.Item2 + 1);//右侧目标
            if (RemainedGrids.Keys.Contains(ij1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, ij1, DirList))
                    {
                        weighij1 = RemainedGrids[ij1];
                        CacheGridWeigh.Add(ij1, weighij1);
                    }
                }

                else
                {
                    weighij1 = RemainedGrids[ij1];
                    CacheGridWeigh.Add(ij1, weighij1);
                }
            }

            double weighi1j_1 = 0;
            Tuple<int, int> i1j_1 = new Tuple<int, int>(TargetGrid.Item1 + 1, TargetGrid.Item2 - 1);//右下侧目标
            if (RemainedGrids.Keys.Contains(i1j_1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i1j_1, DirList))
                    {
                        weighi1j_1 = RemainedGrids[i1j_1];
                        CacheGridWeigh.Add(i1j_1, weighi1j_1);
                    }
                }

                else
                {
                    weighi1j_1 = RemainedGrids[i1j_1];
                    CacheGridWeigh.Add(i1j_1, weighi1j_1);
                }
            }

            double weighi_1j_1 = 0;
            Tuple<int, int> i_1j_1 = new Tuple<int, int>(TargetGrid.Item1 - 1, TargetGrid.Item2 - 1);//左下侧目标
            if (RemainedGrids.Keys.Contains(i_1j_1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i_1j_1, DirList))
                    {
                        weighi_1j_1 = RemainedGrids[i_1j_1];
                        CacheGridWeigh.Add(i_1j_1, weighi_1j_1);
                    }
                }

                else
                {
                    weighi_1j_1 = RemainedGrids[i_1j_1];
                    CacheGridWeigh.Add(i_1j_1, weighi_1j_1);
                }
            }

            double weighi_1j1 = 0;
            Tuple<int, int> i_1j1 = new Tuple<int, int>(TargetGrid.Item1 - 1, TargetGrid.Item2 + 1);//左上侧目标
            if (RemainedGrids.Keys.Contains(i_1j1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i_1j1, DirList))
                    {
                        weighi_1j1 = RemainedGrids[i_1j1];
                        CacheGridWeigh.Add(i_1j1, weighi_1j1);
                    }
                }

                else
                {
                    weighi_1j1 = RemainedGrids[i_1j1];
                    CacheGridWeigh.Add(i_1j1, weighi_1j1);
                }
            }

            double weighi1j1 = 0;
            Tuple<int, int> i1j1 = new Tuple<int, int>(TargetGrid.Item1 + 1, TargetGrid.Item2 + 1);//右上侧目标
            if (RemainedGrids.Keys.Contains(i1j1))
            {
                if (DirList != null)
                {
                    if (this.RightDir(TargetGrid, i1j1, DirList))
                    {
                        weighi1j1 = RemainedGrids[i1j1];
                        CacheGridWeigh.Add(i1j1, weighi1j1);
                    }
                }

                else
                {
                    weighi1j1 = RemainedGrids[i1j1];
                    CacheGridWeigh.Add(i1j1, weighi1j1);
                }
            }
            #endregion

            int TestLocation = 0;

            #region 按顺序添加
            while (CacheGridWeigh.Keys.Count > 0)
            {
                Tuple<int, int> TargetKeys = CacheGridWeigh.Keys.ToList()[0];

                for (int i = 1; i < CacheGridWeigh.Keys.Count; i++)
                {
                    #region 计算距离
                    double TargetDis = 0;
                    if (TargetKeys.Item1 == TargetGrid.Item1 || TargetKeys.Item2 == TargetGrid.Item2)
                    {
                        TargetDis = 1;
                    }
                    else
                    {
                        TargetDis = Math.Sqrt(2);
                    }

                    double CacheDis = 0;
                    if (CacheGridWeigh.Keys.ToList()[i].Item1 == TargetGrid.Item1 || CacheGridWeigh.Keys.ToList()[i].Item2 == TargetGrid.Item2)
                    {
                        CacheDis = 1;
                    }
                    else
                    {
                        CacheDis = Math.Sqrt(2);
                    }
                    #endregion

                    if (CacheGridWeigh[CacheGridWeigh.Keys.ToList()[i]] / CacheDis > CacheGridWeigh[TargetKeys] / TargetDis)
                    {
                        TargetKeys = CacheGridWeigh.Keys.ToList()[i];
                    }
                }

                if (RemainedGrids.Keys.Contains(TargetKeys))
                {
                    NearGrids.Add(TargetKeys);
                    CacheGridWeigh.Remove(TargetKeys);
                    RemainedGrids.Remove(TargetKeys);
                }

                #region 添加邻近数量约束
                if (Type == 1 && NearGrids.Count >= 2)
                {
                    break;
                }
                #endregion
            }
            #endregion

            return NearGrids;
        }

        /// <summary>
        /// 判断endPoint是否在tarPoint的给定方向范围内
        /// </summary>
        /// <param name="tarPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="DirList"></param>
        /// <returns></returns>
        public bool RightDir(Tuple<int, int> tarPoint, Tuple<int, int> endPoint, List<int> DirList)
        {
            bool DirLabel = false;

            #region 获取限定方向(方向编码：1-8,1正下，顺时针编码)
            int IADD = endPoint.Item1 - tarPoint.Item1;
            int JADD = endPoint.Item2 - tarPoint.Item2;
            int Dir = 0;

            if (IADD == 0)
            {
                if (JADD > 0)
                {
                    Dir = 7;
                }

                if (JADD < 0)
                {
                    Dir = 3;
                }
            }

            else if (IADD > 0)
            {
                if (JADD > 0)
                {
                    Dir = 6;
                }

                if (JADD == 0)
                {
                    Dir = 5;
                }

                if (JADD < 0)
                {
                    Dir = 4;
                }
            }

            else if (IADD < 0)
            {
                if (JADD > 0)
                {
                    Dir = 8;
                }

                if (JADD == 0)
                {
                    Dir = 1;
                }

                if (JADD < 0)
                {
                    Dir = 2;
                }
            }
            #endregion

            if (DirList.Contains(Dir))
            {
                DirLabel = true;
            }

            return DirLabel;
        }

        /// <summary>
        /// 宽度优先搜索方法（不考虑格网的权重）按照搜索顺序搜索，输出结果包含了顺序！！
        /// 下1；左2；上3；右4；左下5；左上6；右上7；右下8
        /// </summary>
        /// <param name="TargetGrid">目标格网</param>
        /// <param name="RemainedGrids">现存的格网</param>
        /// 备注：该阶段移除了判断的要素
        /// <returns></returns>
        public List<Tuple<int, int>> GetNearGrids(Tuple<int, int> TargetGrid, List<Tuple<int, int>> RemainedGrids)
        {
            List<Tuple<int, int>> NearGrids = new List<Tuple<int, int>>();

            #region 判断过程 (需要明确新New的元素是否能判断存在，需确认!!!)
            Tuple<int, int> i1j = new Tuple<int, int>(TargetGrid.Item1 + 1, TargetGrid.Item2); if (RemainedGrids.Contains(i1j)) { NearGrids.Add(i1j); RemainedGrids.Remove(i1j); }
            Tuple<int, int> ij_1 = new Tuple<int, int>(TargetGrid.Item1, TargetGrid.Item2 - 1); if (RemainedGrids.Contains(ij_1)) { NearGrids.Add(ij_1); RemainedGrids.Remove(ij_1); }
            Tuple<int, int> i_1j = new Tuple<int, int>(TargetGrid.Item1 - 1, TargetGrid.Item2); if (RemainedGrids.Contains(i_1j)) { NearGrids.Add(i_1j); RemainedGrids.Remove(i_1j); }
            Tuple<int, int> ij1 = new Tuple<int, int>(TargetGrid.Item1, TargetGrid.Item2 + 1); if (RemainedGrids.Contains(ij1)) { NearGrids.Add(ij1); RemainedGrids.Remove(ij1); }
            Tuple<int, int> i1j_1 = new Tuple<int, int>(TargetGrid.Item1 + 1, TargetGrid.Item2 - 1); if (RemainedGrids.Contains(i1j_1)) { NearGrids.Add(i1j_1); RemainedGrids.Remove(i1j_1); }
            Tuple<int, int> i_1j_1 = new Tuple<int, int>(TargetGrid.Item1 - 1, TargetGrid.Item2 - 1); if (RemainedGrids.Contains(i_1j_1)) { NearGrids.Add(i_1j_1); RemainedGrids.Remove(i_1j_1); }
            Tuple<int, int> i_1j1 = new Tuple<int, int>(TargetGrid.Item1 - 1, TargetGrid.Item2 + 1); if (RemainedGrids.Contains(i_1j1)) { NearGrids.Add(i_1j1); RemainedGrids.Remove(i_1j1); }
            Tuple<int, int> i1j1 = new Tuple<int, int>(TargetGrid.Item1 + 1, TargetGrid.Item2 + 1); if (RemainedGrids.Contains(i1j1)) { NearGrids.Add(i1j1); RemainedGrids.Remove(i1j1); }
            #endregion

            return NearGrids;
        }

        /// <summary>
        /// 宽度优先搜索方法（考虑格网的权重）
        /// 下1；左2；上3；右4；左下5；左上6；右上7；右下8
        /// </summary>
        /// <param name="JudgeList">目标格网集合（按顺序排列）</param>
        /// <param name="KeyList">现存的格网权重编码</param>
        /// LEVEL当前层级
        /// Type=1，当前邻近的要素最多只能有两个；
        /// Type=2，对当前邻近的要素数量不做限制；
        /// <returns></returns>
        public List<Tuple<int, int>> GetJudgeList(List<Tuple<int, int>> JudgeList, Dictionary<Tuple<int, int>,double> KeyList, int LEVEL,int Type,List<int> DirList)
        {
            List<Tuple<int, int>> OutJudgeList = new List<Tuple<int, int>>();

            for (int i = 0; i < JudgeList.Count; i++)
            {
                Node CacheNode = new Node();
                CacheNode.GridID = JudgeList[i];
                CacheNode.LevelSort = new Tuple<int, int>(LEVEL, i);//搜索所处的层级（当前层+在当前层中的排序）
                List<Tuple<int, int>> NearGrids = new List<Tuple<int, int>>();
                if (LEVEL == 1)
                {
                    NearGrids = this.GetNearGrids(JudgeList[i], KeyList, 2, DirList);
                }
                else 
                {
                    NearGrids = this.GetNearGrids(JudgeList[i], KeyList, Type, DirList);
                }

                
                CacheNode.Childs = NearGrids;
                OutJudgeList.AddRange(NearGrids);///生成对应的Node

                PathtraceRes.Add(JudgeList[i], CacheNode);//添加入字典
            }

            return OutJudgeList;
        }

        /// <summary>
        /// 宽度优先搜索方法（考虑格网的权重）[考虑网格距离差异]
        /// 下1；左2；上3；右4；左下5；左上6；右上7；右下8
        /// </summary>
        /// <param name="JudgeList">目标格网集合（按顺序排列）</param>
        /// <param name="KeyList">现存的格网权重编码</param>
        /// LEVEL当前层级
        /// Type=1，当前邻近的要素最多只能有两个；
        /// Type=2，对当前邻近的要素数量不做限制；
        /// <returns></returns>
        public List<Tuple<int, int>> GetJudgeListDis(List<Tuple<int, int>> JudgeList, Dictionary<Tuple<int, int>, double> KeyList, int LEVEL, int Type, List<int> DirList,Dictionary<Tuple<int,int>,int> GridType)
        {
            List<Tuple<int, int>> OutJudgeList = new List<Tuple<int, int>>();

            for (int i = 0; i < JudgeList.Count; i++)
            {
                Node CacheNode = new Node();
                CacheNode.GridID = JudgeList[i];
                CacheNode.LevelSort = new Tuple<int, int>(LEVEL, i);//搜索所处的层级（当前层+在当前层中的排序）
                List<Tuple<int, int>> NearGrids = new List<Tuple<int, int>>();
                if (LEVEL == 1)
                {
                    NearGrids = this.GetNearGridsDisType(JudgeList[i], KeyList, 2, DirList,GridType);
                }
                else
                {
                    NearGrids = this.GetNearGridsDisType(JudgeList[i], KeyList, Type, DirList,GridType);
                }


                CacheNode.Childs = NearGrids;
                OutJudgeList.AddRange(NearGrids);///生成对应的Node

                if (PathtraceRes.Keys.Contains(JudgeList[i]))
                {
                    PathtraceRes.Remove(JudgeList[i]);
                    PathtraceRes.Add(JudgeList[i], CacheNode);//添加入字典
                }
                else
                {
                    PathtraceRes.Add(JudgeList[i], CacheNode);//添加入字典
                }
            }

            return OutJudgeList;
        }

        /// <summary>
        /// 宽度优先搜索方法（考虑格网的权重）[考虑网格距离差异]
        /// 下1；左2；上3；右4；左下5；左上6；右上7；右下8
        /// </summary>
        /// <param name="JudgeList">目标格网集合（按顺序排列）</param>
        /// <param name="KeyList">现存的格网权重编码</param>
        /// LEVEL当前层级
        /// Type=1，当前邻近的要素最多只能有两个；
        /// Type=2，对当前邻近的要素数量不做限制；
        /// <returns></returns>
        public List<Tuple<int, int>> GetJudgeListDis(List<Tuple<int, int>> JudgeList, Dictionary<Tuple<int, int>, double> KeyList, int LEVEL, int Type, List<int> DirList, Dictionary<Tuple<int, int>, int> GridType, Dictionary<Tuple<int, int>, int> GridVisit)
        {
            List<Tuple<int, int>> OutJudgeList = new List<Tuple<int, int>>();

            for (int i = 0; i < JudgeList.Count; i++)
            {
                Node CacheNode = new Node();
                CacheNode.GridID = JudgeList[i];
                CacheNode.LevelSort = new Tuple<int, int>(LEVEL, i);//搜索所处的层级（当前层+在当前层中的排序）
                List<Tuple<int, int>> NearGrids = new List<Tuple<int, int>>();
                if (LEVEL == 1)
                {
                    NearGrids = this.GetNearGridsDisType(JudgeList[i], KeyList, 2, DirList, GridType,GridVisit);
                }
                else
                {
                    NearGrids = this.GetNearGridsDisType(JudgeList[i], KeyList, Type, DirList, GridType, GridVisit);
                }


                CacheNode.Childs = NearGrids;
                OutJudgeList.AddRange(NearGrids);///生成对应的Node

                if (PathtraceRes.Keys.Contains(JudgeList[i]))//添加入字典
                {
                    PathtraceRes.Remove(JudgeList[i]);
                    PathtraceRes.Add(JudgeList[i], CacheNode);//添加入字典
                }

                else
                { 
                    PathtraceRes.Add(JudgeList[i], CacheNode);//添加入字典
                }
            }

            OutJudgeList=OutJudgeList.Distinct().ToList();
            return OutJudgeList;
        }


        /// <summary>
        /// 宽度优先搜索方法（不考虑格网的权重）
        /// 下1；左2；上3；右4；左下5；左上6；右上7；右下8
        /// </summary>
        /// <param name="JudgeList">目标格网集合（按顺序排列）</param>
        /// <param name="KeyList">现存的格网</param>
        /// LEVEL当前层级
        /// <returns></returns>
        public List<Tuple<int, int>> GetJudgeList(List<Tuple<int, int>> JudgeList, List<Tuple<int, int>> KeyList, int LEVEL)
        {
            List<Tuple<int, int>> OutJudgeList = new List<Tuple<int, int>>();

            for (int i = 0; i < JudgeList.Count; i++)
            {
                Node CacheNode = new Node();
                CacheNode.GridID = JudgeList[i];
                CacheNode.LevelSort=new Tuple<int,int>(LEVEL,i);
                List<Tuple<int, int>> NearGrids = this.GetNearGrids(JudgeList[i], KeyList);
                CacheNode.Childs = NearGrids;
                OutJudgeList.AddRange(NearGrids);///生成对应的Node

                PathtraceRes.Add(JudgeList[i], CacheNode);//添加入字典
            }

            return OutJudgeList;
        }

        /// <summary>
        /// 获得起点到终点的最短路径[起点-终点网格编码]（起点指整个FlowMap的起点）
        /// </summary>
        /// <param name="ePoint">起点</param>
        /// <param name="sPoint">终点</param>
        /// Type=1,表示不考虑起点与终点之间的方向关系
        /// Type=2，表示路径搜索时，考虑了起点与终点之间的方向关系
        /// <returns></returns>
        public List<Tuple<int, int>> GetShortestPath(Tuple<int, int> ePoint, Tuple<int, int> sPoint)
        {
            List<Tuple<int, int>> ShortestPath = new List<Tuple<int, int>>();

            #region 计算过程
            ShortestPath.Add(ePoint);

            //bool Testloaction = (ePoint != sPoint);
            while ((ePoint.Item1 != sPoint.Item1) || (ePoint.Item2 != sPoint.Item2))//避免出现重复问题
            {
                Tuple<int, int> TestePoint = this.GetFather(ePoint);
                ePoint = this.GetFather(ePoint);
                ShortestPath.Add(ePoint);

                ///需要考虑可能不存在路径的情况
                if (ePoint == null)
                {
                    return null;
                }
            }
            #endregion

            return ShortestPath;
        }

        /// <summary>
        /// 获得起点到终点的最短路径（包含了给定的路径）
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="PathNode"></param>
        /// <param name="Paths"></param>
        /// <param name="Weight"></param>
        /// <returns></returns>
        public List<Tuple<int, int>> GetShortestPath(Tuple<int, int> endPoint, List<Tuple<int, int>> PathNode, Dictionary<Tuple<int,int>,Path> Paths, double Weight)
        {
            List<Tuple<int, int>> TargetPath = null;
            double MinLength = 100000;
            for (int i = 0; i < PathNode.Count; i++)
            {
                List<Tuple<int, int>> CacheShortPath = this.GetShortestPath(PathNode[i],endPoint);
                double CacheShortPathLength = this.GetPathLength(CacheShortPath);
                double TotalLength = CacheShortPathLength + Paths[PathNode[i]].Length * Weight;

                if (TotalLength < MinLength)
                {
                    MinLength = TotalLength;
                    List<Tuple<int, int>> CachePath = Paths[PathNode[i]].ePath.ToList();
                    CacheShortPath.RemoveAt(0);//移除第一个要素，避免存在重复元素

                    CachePath.AddRange(CacheShortPath);
                    TargetPath = CachePath;
                }
            }

            return TargetPath;
        }

        /// <summary>
        /// 判断ePoint向sPoint延伸的限制性方向
        /// </summary>
        /// <param name="sPoint"></param>
        /// <param name="ePoint"></param>
        /// <returns></returns>
        public List<int> GetConDir(Tuple<int, int> sPoint, Tuple<int, int> ePoint)
        {
            List<int> DirList = new List<int>();//获取限定的方向列表

            #region 获取限定方向(方向编码：1-8,1正下，顺时针编码)
            int IADD = sPoint.Item1 - ePoint.Item1;
            int JADD = sPoint.Item2 - ePoint.Item2;

            if (IADD == 0)
            {
                if (JADD > 0)
                {
                    DirList.Add(4); DirList.Add(5); DirList.Add(6);
                }

                if (JADD < 0)
                {
                    DirList.Add(2); DirList.Add(1); DirList.Add(8);
                }
            }

            else if (IADD > 0)
            {
                if (JADD > 0)
                {
                    DirList.Add(5); DirList.Add(6); DirList.Add(7);
                }

                if (JADD == 0)
                {
                    DirList.Add(6); DirList.Add(7); DirList.Add(8);
                }

                if (JADD < 0)
                {
                    DirList.Add(7); DirList.Add(8); DirList.Add(1);
                }
            }

            else if (IADD < 0)
            {
                if (JADD > 0)
                {
                    DirList.Add(5); DirList.Add(4); DirList.Add(3);
                }

                if (JADD == 0)
                {
                    DirList.Add(4); DirList.Add(3); DirList.Add(2);
                }

                if (JADD < 0)
                {
                    DirList.Add(3); DirList.Add(2); DirList.Add(1);
                }
            }
            #endregion

            //DirList.Sort();
            return DirList;
        }

        /// <summary>
        /// 获取给定路径的长度
        /// </summary>
        /// <param name="ShortestPath"></param>
        /// <returns></returns>
        public double GetPathLength(List<Tuple<int, int>> ShortestPath)
        {
            double Length = 0;

            for (int i = 0; i < ShortestPath.Count-1; i++)
            {
                if (ShortestPath[i].Item1 == ShortestPath[i + 1].Item1 ||
                   ShortestPath[i].Item2 == ShortestPath[i + 1].Item2)
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
        /// 获取给定格网的上一路径；而且是返回第一个路径
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public Tuple<int, int> GetFather(Tuple<int, int> endPoint)
        {
            foreach (KeyValuePair<Tuple<int, int>, Node> kv in PathtraceRes)
            {
                if (kv.Value.Childs.Contains(endPoint))
                {
                    return kv.Key;
                }
            }

            return null;
        }
    }
}
