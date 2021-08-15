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

namespace PrDispalce.建筑物聚合
{
    class PolygonSimplify
    {
        PolygonPreprocess PP = new PolygonPreprocess();
        double Pi = 3.1415926;
        public AxMapControl pMapControl;
        public SMap pMap = new SMap();
        
        /// <summary>
        /// 构造函数1
        /// </summary>
        public PolygonSimplify()
        {
        }

        /// <summary>
        /// 获得给定最短边与多边形中结构的关系
        /// </summary>
        /// <param name="Curpo"></param>
        /// <param name="ShortestNodes"></param>
        /// <returns></returns>
        public int GetEdgeStructType(PolygonObject Curpo,List<TriNode> ShortestNodes,double PerAngle)
        {
            int EdgeStructType = -1;

            List<BasicStruct> BasicStructList = PP.GetStructedNodes(Curpo, PerAngle);//获得一个建筑物中所有定义的基础结构
            List<BasicStruct> InvovedStructList = PP.GetStructForEdge(ShortestNodes, BasicStructList);//获得最短边关联的结构

            #region 如果只关联一个结构
            if (InvovedStructList.Count == 1)
            {
                if (InvovedStructList[0].Type == 2 || InvovedStructList[0].Type == 3)
                {
                    EdgeStructType = 1;
                }

                else
                {
                    EdgeStructType = 2;
                }
            }
            #endregion

            #region 如果只关联两个结构
            if (InvovedStructList.Count == 2)
            {
                //两个都是非结构
                if (InvovedStructList[0].Type == 1 && InvovedStructList[1].Type == 1)
                {
                    EdgeStructType = 3;
                }

                //两个都是凸部或凹部
                else if ((InvovedStructList[0].Type == 2 || InvovedStructList[0].Type == 3) && (InvovedStructList[1].Type == 2 || InvovedStructList[1].Type == 3))
                {
                    EdgeStructType = 6;
                }

                else if (InvovedStructList[0].Type == 4 && InvovedStructList[1].Type == 4)
                {
                    EdgeStructType = 8;
                }

                else if((InvovedStructList[0].Type==1 && (InvovedStructList[1].Type==2||InvovedStructList[1].Type==2))
                    ||(InvovedStructList[1].Type==1 && (InvovedStructList[0].Type==2||InvovedStructList[0].Type==2)))
                {
                    EdgeStructType = 4;
                }

                else if ((InvovedStructList[0].Type == 1 && InvovedStructList[1].Type == 4)
                  || (InvovedStructList[1].Type == 1 && InvovedStructList[0].Type == 4))
                {
                    EdgeStructType = 5;
                }

                else
                {
                    EdgeStructType = 7;
                }
            }
            #endregion

            #region 如果关联三个结构
            if (InvovedStructList.Count == 3)
            {
                #region 判断阶梯状结构个数
                int StairCount=0;
                if(InvovedStructList[0].Type==4)
                {
                    StairCount++;
                }
                if(InvovedStructList[1].Type==4)
                {
                    StairCount++;
                }
                if(InvovedStructList[2].Type==4)
                {
                    StairCount++;
                }
                #endregion

                if (StairCount==3)
                {
                    EdgeStructType = 14;
                }

                else if (StairCount == 2)
                {
                    #region 找到凸部或凹部的结构
                    BasicStruct TargetStruct = null; List<BasicStruct> OtherStruct = new List<BasicStruct>();
                    for (int i = 0; i < InvovedStructList.Count; i++)
                    {
                        if (InvovedStructList[i].Type == 2 || InvovedStructList[i].Type == 3)
                        {
                            TargetStruct = InvovedStructList[i];
                        }

                        else
                        {
                            OtherStruct.Add(InvovedStructList[i]);
                        }
                    }
                    #endregion

                    #region 若该凹部或凸部存在一条边不是另外两个结构的边，则它是类型12；否则是类型13
                    bool EdgeLabel=false;
                    for (int i = 0; i < TargetStruct.PolylineList.Count;i++ )
                    {
                        if (!OtherStruct[0].PolylineList.Contains(TargetStruct.PolylineList[i]) &&
                            !OtherStruct[1].PolylineList.Contains(TargetStruct.PolylineList[i]))
                        {
                            EdgeLabel = true;
                        }
                    }

                    if (EdgeLabel)
                    {
                        EdgeStructType = 12;
                    }
                    else
                    {
                        EdgeStructType = 13;
                    }
                    #endregion
                }

                else if (StairCount == 1)
                {
                    #region 找到阶梯状结构
                    BasicStruct TargetStruct = null; List<BasicStruct> OtherStruct = new List<BasicStruct>();
                    for (int i = 0; i < InvovedStructList.Count; i++)
                    {
                        if (InvovedStructList[i].Type == 4)
                        {
                            TargetStruct = InvovedStructList[i];
                        }

                        else
                        {
                            OtherStruct.Add(InvovedStructList[i]);
                        }
                    }
                    #endregion 

                    #region 若该阶梯状结构存在一条边不是另外两个结构的边，则它是类型12；否则是类型13
                    bool EdgeLabel = false;
                    for (int i = 0; i < TargetStruct.PolylineList.Count; i++)
                    {
                        if (!OtherStruct[0].PolylineList.Contains(TargetStruct.PolylineList[i]) &&
                            !OtherStruct[1].PolylineList.Contains(TargetStruct.PolylineList[i]))
                        {
                            EdgeLabel = true;
                        }
                    }

                    if (EdgeLabel)
                    {
                        EdgeStructType = 10;
                    }
                    else
                    {
                        EdgeStructType = 11;
                    }
                    #endregion
                }

            }
            #endregion

            return EdgeStructType;
        }

        /// <summary>
        /// 对每一个结构进行处理并返回相应的结果(博士论文中的结构处理)
        /// </summary>
        /// <param name="CurPo">给定的建筑物</param>
        /// <param name="ProcessStruct">给定的结构</param>//这里有一个前提条件，即对于任何一个基础的处理结构，其处理方法是一样的
        /// <returns></returns>List<List<Node>> 处理后剩下的节点；处理时剩下的多边形；处理时所连接的直线(可能存在两条直线，阶梯型的处理);AreaType 1=表示处理的面积增加；2表示处理的面积减少
        public Dictionary<List<List<TriNode>>, int> StructProcess(PolygonObject CurPo,BasicStruct ProcessStruct,double PerAngle)
        {
            CurPo.GetBendAngle();
            Dictionary<List<List<TriNode>>, int> ProcessResult = new Dictionary<List<List<TriNode>>, int>();

            #region 是一个折线结构
            if (ProcessStruct.Type == 1)
            {
                if (CurPo.BendAngle[CurPo.PointList.IndexOf(ProcessStruct.NodeList[1])][1] > 0)
                {
                    int AreaType = 2;
                    List<TriNode> RetailedNodes = new List<TriNode>(); RetailedNodes.Add(ProcessStruct.NodeList[0]); RetailedNodes.Add(ProcessStruct.NodeList[2]);//处理后剩下的节点
                    List<TriNode> ProcessArea = new List<TriNode>(); ProcessArea = ProcessStruct.NodeList;
                    List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(RetailedNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(RetailedNodes);
                    ProcessResult.Add(ResultNodes, AreaType);
                }

                else
                {
                    int AreaType = 1;
                    List<TriNode> RetailedNodes = new List<TriNode>(); RetailedNodes.Add(ProcessStruct.NodeList[0]); RetailedNodes.Add(ProcessStruct.NodeList[2]);//处理后剩下的节点
                    List<TriNode> ProcessArea = new List<TriNode>(); ProcessArea = ProcessStruct.NodeList;
                    List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(RetailedNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(RetailedNodes);
                    ProcessResult.Add(ResultNodes, AreaType);
                }
            }
            #endregion

            #region 是一个凸部结构
            if (ProcessStruct.Type == 2)
            {
                double Dis1 = Math.Sqrt((ProcessStruct.NodeList[0].X - ProcessStruct.NodeList[1].X) * (ProcessStruct.NodeList[0].X - ProcessStruct.NodeList[1].X)
                   + (ProcessStruct.NodeList[0].Y - ProcessStruct.NodeList[1].Y) * (ProcessStruct.NodeList[0].Y - ProcessStruct.NodeList[1].Y));
                double Dis2 = Math.Sqrt((ProcessStruct.NodeList[2].X - ProcessStruct.NodeList[3].X) * (ProcessStruct.NodeList[2].X - ProcessStruct.NodeList[3].X)
                    + (ProcessStruct.NodeList[2].Y - ProcessStruct.NodeList[3].Y) * (ProcessStruct.NodeList[2].Y - ProcessStruct.NodeList[3].Y));

                if (Dis1 > Dis2)
                {
                    #region 获得操作点处的角度
                    int ProcessStructIndex = CurPo.PointList.IndexOf(ProcessStruct.NodeList[3]);
                    TriNode AnotherNode = null;
                    double BendAngle = CurPo.BendAngle[ProcessStructIndex][1];
                    if (ProcessStructIndex == 0)
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[1]))
                        {
                            AnotherNode = CurPo.PointList[CurPo.PointList.Count - 1];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[1];
                        }
                    }

                    else if (ProcessStructIndex == CurPo.PointList.Count - 1)
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[0]))
                        {
                            AnotherNode = CurPo.PointList[CurPo.PointList.Count - 2];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[0];
                        }
                    }

                    else
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[ProcessStructIndex + 1]))
                        {
                            AnotherNode = CurPo.PointList[ProcessStructIndex - 1];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[ProcessStructIndex + 1];
                        }
                    }
                    #endregion

                    #region 如果该点角度为90
                    if (Math.Abs(Math.Abs(BendAngle) - Pi / 2) < PerAngle)
                    {
                        TriNode TargetNode = this.CalIntersePoint(AnotherNode,ProcessStruct.NodeList[3], ProcessStruct.NodeList[0], ProcessStruct.NodeList[1]);
                        List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                        NewNodes.Add(ProcessStruct.NodeList[0]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[3]);
                        ProcessArea.Add(ProcessStruct.NodeList[3]); ProcessArea.Add(ProcessStruct.NodeList[2]); ProcessArea.Add(ProcessStruct.NodeList[1]); ProcessArea.Add(TargetNode);
                        ConnectLine.Add(ProcessStruct.NodeList[3]); ConnectLine.Add(TargetNode);
                        int AreaType = 2;

                        List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                        ProcessResult.Add(ResultNodes, AreaType);
                    }
                    #endregion

                    #region 如果该点角度不为90
                    else
                    {
                        TriNode TargetNode = this.CalMinDisPoint2Line(ProcessStruct.NodeList[3], ProcessStruct.NodeList[0], ProcessStruct.NodeList[1]);
                        List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                        NewNodes.Add(ProcessStruct.NodeList[0]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[3]);
                        ProcessArea.Add(ProcessStruct.NodeList[3]); ProcessArea.Add(ProcessStruct.NodeList[2]); ProcessArea.Add(ProcessStruct.NodeList[1]); ProcessArea.Add(TargetNode);
                        ConnectLine.Add(ProcessStruct.NodeList[3]); ConnectLine.Add(TargetNode);
                        int AreaType = 2;

                        List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                        ProcessResult.Add(ResultNodes, AreaType);
                    }
                    #endregion
                }

                else
                {
                    #region 获得操作点处的角度
                    int ProcessStructIndex = CurPo.PointList.IndexOf(ProcessStruct.NodeList[0]);
                    TriNode AnotherNode = null;
                    double BendAngle = CurPo.BendAngle[ProcessStructIndex][1];
                    if (ProcessStructIndex == 0)
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[1]))
                        {
                            AnotherNode = CurPo.PointList[CurPo.PointList.Count - 1];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[1];
                        }
                    }

                    else if (ProcessStructIndex == CurPo.PointList.Count - 1)
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[0]))
                        {
                            AnotherNode = CurPo.PointList[CurPo.PointList.Count - 2];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[0];
                        }
                    }

                    else
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[ProcessStructIndex + 1]))
                        {
                            AnotherNode = CurPo.PointList[ProcessStructIndex - 1];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[ProcessStructIndex + 1];
                        }
                    }
                    #endregion

                    #region 如果该点是90度
                    if (Math.Abs(Math.Abs(BendAngle) - Pi / 2) < PerAngle)
                    {
                        TriNode TargetNode = this.CalIntersePoint(AnotherNode, ProcessStruct.NodeList[0], ProcessStruct.NodeList[2], ProcessStruct.NodeList[3]);
                        List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                        NewNodes.Add(ProcessStruct.NodeList[0]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[3]);
                        ProcessArea.Add(ProcessStruct.NodeList[0]); ProcessArea.Add(ProcessStruct.NodeList[1]); ProcessArea.Add(ProcessStruct.NodeList[2]); ProcessArea.Add(TargetNode);
                        ConnectLine.Add(ProcessStruct.NodeList[0]); ConnectLine.Add(TargetNode);
                        int AreaType = 2;

                        List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                        ProcessResult.Add(ResultNodes, AreaType);
                    }
                    #endregion

                    #region 如果该点不是90度
                    else
                    {
                        TriNode TargetNode = this.CalMinDisPoint2Line(ProcessStruct.NodeList[0], ProcessStruct.NodeList[2], ProcessStruct.NodeList[3]);
                        List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                        NewNodes.Add(ProcessStruct.NodeList[0]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[3]);
                        ProcessArea.Add(ProcessStruct.NodeList[0]); ProcessArea.Add(ProcessStruct.NodeList[1]); ProcessArea.Add(ProcessStruct.NodeList[2]); ProcessArea.Add(TargetNode);
                        ConnectLine.Add(ProcessStruct.NodeList[0]); ConnectLine.Add(TargetNode);
                        int AreaType = 2;

                        List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                        ProcessResult.Add(ResultNodes, AreaType);
                    }
                    #endregion
                }
            }
            #endregion

            #region 是一个凹部结构
            if (ProcessStruct.Type == 3)
            {
                double Dis1 = Math.Sqrt((ProcessStruct.NodeList[0].X - ProcessStruct.NodeList[1].X) * (ProcessStruct.NodeList[0].X - ProcessStruct.NodeList[1].X)
                  + (ProcessStruct.NodeList[0].Y - ProcessStruct.NodeList[1].Y) * (ProcessStruct.NodeList[0].Y - ProcessStruct.NodeList[1].Y));
                double Dis2 = Math.Sqrt((ProcessStruct.NodeList[2].X - ProcessStruct.NodeList[3].X) * (ProcessStruct.NodeList[2].X - ProcessStruct.NodeList[3].X)
                    + (ProcessStruct.NodeList[2].Y - ProcessStruct.NodeList[3].Y) * (ProcessStruct.NodeList[2].Y - ProcessStruct.NodeList[3].Y));

                if (Dis1 > Dis2)
                {
                    #region 获得操作点处的角度
                    int ProcessStructIndex = CurPo.PointList.IndexOf(ProcessStruct.NodeList[3]);
                    TriNode AnotherNode = null;
                    double BendAngle = CurPo.BendAngle[ProcessStructIndex][1];
                    if (ProcessStructIndex == 0)
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[1]))
                        {
                            AnotherNode = CurPo.PointList[CurPo.PointList.Count - 1];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[1];
                        }
                    }

                    else if (ProcessStructIndex == CurPo.PointList.Count - 1)
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[0]))
                        {
                            AnotherNode = CurPo.PointList[CurPo.PointList.Count - 2];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[0];
                        }
                    }

                    else
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[ProcessStructIndex + 1]))
                        {
                            AnotherNode = CurPo.PointList[ProcessStructIndex - 1];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[ProcessStructIndex + 1];
                        }
                    }
                    #endregion

                    #region 如果该点角度为90
                    if (Math.Abs(Math.Abs(BendAngle) - Pi / 2) < PerAngle)
                    {
                        TriNode TargetNode = this.CalIntersePoint(AnotherNode,ProcessStruct.NodeList[3], ProcessStruct.NodeList[0], ProcessStruct.NodeList[1]);
                        List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                        NewNodes.Add(ProcessStruct.NodeList[0]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[3]);
                        ProcessArea.Add(ProcessStruct.NodeList[3]); ProcessArea.Add(ProcessStruct.NodeList[2]); ProcessArea.Add(ProcessStruct.NodeList[1]); ProcessArea.Add(TargetNode);
                        ConnectLine.Add(ProcessStruct.NodeList[3]); ConnectLine.Add(TargetNode);
                        int AreaType = 1;

                        List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                        ProcessResult.Add(ResultNodes, AreaType);
                    }
                    #endregion

                    #region 如果该点角度不为90
                    else
                    {
                        TriNode TargetNode = this.CalMinDisPoint2Line(ProcessStruct.NodeList[3], ProcessStruct.NodeList[0], ProcessStruct.NodeList[1]);
                        List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                        NewNodes.Add(ProcessStruct.NodeList[0]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[3]);
                        ProcessArea.Add(ProcessStruct.NodeList[3]); ProcessArea.Add(ProcessStruct.NodeList[2]); ProcessArea.Add(ProcessStruct.NodeList[1]); ProcessArea.Add(TargetNode);
                        ConnectLine.Add(ProcessStruct.NodeList[3]); ConnectLine.Add(TargetNode);
                        int AreaType = 1;

                        List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                        ProcessResult.Add(ResultNodes, AreaType);
                    }
                    #endregion
                }

                else
                {
                    #region 获得操作点处的角度
                    int ProcessStructIndex = CurPo.PointList.IndexOf(ProcessStruct.NodeList[0]);
                    TriNode AnotherNode = null;
                    double BendAngle = CurPo.BendAngle[ProcessStructIndex][1];
                    if (ProcessStructIndex == 0)
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[1]))
                        {
                            AnotherNode = CurPo.PointList[CurPo.PointList.Count - 1];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[1];
                        }
                    }

                    else if (ProcessStructIndex == CurPo.PointList.Count - 1)
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[0]))
                        {
                            AnotherNode = CurPo.PointList[CurPo.PointList.Count - 2];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[0];
                        }
                    }

                    else
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[ProcessStructIndex + 1]))
                        {
                            AnotherNode = CurPo.PointList[ProcessStructIndex - 1];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[ProcessStructIndex + 1];
                        }
                    }
                    #endregion

                    #region 如果该点是90度
                    if (Math.Abs(Math.Abs(BendAngle) - Pi / 2) < PerAngle)
                    {
                        TriNode TargetNode = this.CalIntersePoint(AnotherNode,ProcessStruct.NodeList[0], ProcessStruct.NodeList[2], ProcessStruct.NodeList[3]);
                        List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                        NewNodes.Add(ProcessStruct.NodeList[0]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[3]);
                        ProcessArea.Add(ProcessStruct.NodeList[0]); ProcessArea.Add(ProcessStruct.NodeList[1]); ProcessArea.Add(ProcessStruct.NodeList[2]); ProcessArea.Add(TargetNode);
                        ConnectLine.Add(ProcessStruct.NodeList[0]); ConnectLine.Add(TargetNode);
                        int AreaType = 1;

                        List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                        ProcessResult.Add(ResultNodes, AreaType);
                    }
                    #endregion

                    #region 如果该点不是90度
                    else
                    {
                        TriNode TargetNode = this.CalMinDisPoint2Line(ProcessStruct.NodeList[0], ProcessStruct.NodeList[2], ProcessStruct.NodeList[3]);
                        List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                        NewNodes.Add(ProcessStruct.NodeList[0]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[3]);
                        ProcessArea.Add(ProcessStruct.NodeList[0]); ProcessArea.Add(ProcessStruct.NodeList[1]); ProcessArea.Add(ProcessStruct.NodeList[2]); ProcessArea.Add(TargetNode);
                        ConnectLine.Add(ProcessStruct.NodeList[0]); ConnectLine.Add(TargetNode);
                        int AreaType = 1;

                        List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                        ProcessResult.Add(ResultNodes, AreaType);
                    }
                    #endregion
                }
            }
            #endregion

            #region 是一个阶梯型结构
            if (ProcessStruct.Type == 4)
            {
                if (CurPo.BendAngle[CurPo.PointList.IndexOf(ProcessStruct.NodeList[1])][1] < 0)//找到结构中外角为90度的点
                {
                    #region 获得操作点处的角度
                    int ProcessStructIndex = CurPo.PointList.IndexOf(ProcessStruct.NodeList[0]);
                    TriNode AnotherNode = null;
                    double BendAngle = CurPo.BendAngle[ProcessStructIndex][1];
                    if (ProcessStructIndex == 0)
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[1]))
                        {
                            AnotherNode = CurPo.PointList[CurPo.PointList.Count - 1];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[1];
                        }
                    }

                    else if (ProcessStructIndex == CurPo.PointList.Count - 1)
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[0]))
                        {
                            AnotherNode = CurPo.PointList[CurPo.PointList.Count - 2];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[0];
                        }
                    }

                    else
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[ProcessStructIndex + 1]))
                        {
                            AnotherNode = CurPo.PointList[ProcessStructIndex - 1];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[ProcessStructIndex + 1];
                        }
                    }
                    #endregion

                    #region 如果该点是90度
                    if (Math.Abs(Math.Abs(BendAngle) - Pi / 2) < PerAngle)
                    {
                        TriNode TargetNode = this.CalIntersePoint(AnotherNode,ProcessStruct.NodeList[0], ProcessStruct.NodeList[2], ProcessStruct.NodeList[3]);
                        List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                        NewNodes.Add(ProcessStruct.NodeList[0]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[3]);
                        ProcessArea.Add(ProcessStruct.NodeList[0]); ProcessArea.Add(ProcessStruct.NodeList[1]); ProcessArea.Add(ProcessStruct.NodeList[2]); ProcessArea.Add(TargetNode);

                        ConnectLine.Add(ProcessStruct.NodeList[0]); ConnectLine.Add(TargetNode); ConnectLine.Add(ProcessStruct.NodeList[2]);
                        int AreaType = 1;

                        List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                        ProcessResult.Add(ResultNodes, AreaType);
                    }
                    #endregion

                    #region 如果该点不是90度
                    else
                    {
                        TriNode TargetNode = this.CalMinDisPoint2Line(ProcessStruct.NodeList[0], ProcessStruct.NodeList[2], ProcessStruct.NodeList[3]);
                        List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                        NewNodes.Add(ProcessStruct.NodeList[0]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[3]);
                        ProcessArea.Add(ProcessStruct.NodeList[0]); ProcessArea.Add(ProcessStruct.NodeList[1]); ProcessArea.Add(ProcessStruct.NodeList[2]); ProcessArea.Add(TargetNode);

                        ConnectLine.Add(ProcessStruct.NodeList[0]); ConnectLine.Add(TargetNode); ConnectLine.Add(ProcessStruct.NodeList[2]);
                        int AreaType = 1;

                        List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                        ProcessResult.Add(ResultNodes, AreaType);
                    }
                    #endregion
                }

                else
                {
                    #region 获得操作点处的角度
                    int ProcessStructIndex = CurPo.PointList.IndexOf(ProcessStruct.NodeList[3]);
                    TriNode AnotherNode = null;
                    double BendAngle = CurPo.BendAngle[ProcessStructIndex][1];
                    if (ProcessStructIndex == 0)
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[1]))
                        {
                            AnotherNode = CurPo.PointList[CurPo.PointList.Count - 1];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[1];
                        }
                    }

                    else if (ProcessStructIndex == CurPo.PointList.Count - 1)
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[0]))
                        {
                            AnotherNode = CurPo.PointList[CurPo.PointList.Count - 2];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[0];
                        }
                    }

                    else
                    {
                        if (ProcessStruct.NodeList.Contains(CurPo.PointList[ProcessStructIndex + 1]))
                        {
                            AnotherNode = CurPo.PointList[ProcessStructIndex - 1];
                        }

                        else
                        {
                            AnotherNode = CurPo.PointList[ProcessStructIndex + 1];
                        }
                    }
                    #endregion

                    #region 如果该点是90度
                    if (Math.Abs(Math.Abs(BendAngle) - Pi / 2) < PerAngle)
                    {
                        TriNode TargetNode = this.CalIntersePoint(AnotherNode, ProcessStruct.NodeList[3], ProcessStruct.NodeList[0], ProcessStruct.NodeList[1]);
                        List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                        NewNodes.Add(ProcessStruct.NodeList[0]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[3]);
                        ProcessArea.Add(ProcessStruct.NodeList[3]); ProcessArea.Add(ProcessStruct.NodeList[2]); ProcessArea.Add(ProcessStruct.NodeList[1]); ProcessArea.Add(TargetNode);

                        ConnectLine.Add(ProcessStruct.NodeList[3]); ConnectLine.Add(TargetNode); ConnectLine.Add(ProcessStruct.NodeList[1]);
                        int AreaType = 1;

                        List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                        ProcessResult.Add(ResultNodes, AreaType);
                    }
                    #endregion

                    #region 如果该点不是90度
                    else
                    {
                        TriNode TargetNode = this.CalMinDisPoint2Line(ProcessStruct.NodeList[3], ProcessStruct.NodeList[0], ProcessStruct.NodeList[1]);
                        List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                        NewNodes.Add(ProcessStruct.NodeList[0]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[3]);
                        ProcessArea.Add(ProcessStruct.NodeList[3]); ProcessArea.Add(ProcessStruct.NodeList[2]); ProcessArea.Add(ProcessStruct.NodeList[1]); ProcessArea.Add(TargetNode);

                        ConnectLine.Add(ProcessStruct.NodeList[3]); ConnectLine.Add(TargetNode); ConnectLine.Add(ProcessStruct.NodeList[1]);
                        int AreaType = 1;

                        List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                        ProcessResult.Add(ResultNodes, AreaType);
                    }
                    #endregion
                }
            }
            #endregion

            return ProcessResult;
        }

        /// <summary>
        /// 对每一个结构进行处理并返回相应的结果(投稿论文中的结构处理)
        /// </summary>
        /// <param name="CurPo"></param>
        /// <param name="ProcessStruct"></param>处理后剩下的节点；处理时剩下的多边形；处理时所连接的直线(可能存在两条直线，阶梯型的处理);
        /// AreaType 1=表示处理的面积增加；2表示处理的面积减少
        /// <param name="PerAngle"></param>
        /// <returns></returns>
        public Dictionary<List<List<TriNode>>, int> StructProcess2(PolygonObject CurPo, BasicStruct ProcessStruct, double PerAngle)
        {
            CurPo.GetBendAngle();
            Dictionary<List<List<TriNode>>, int> ProcessResult = new Dictionary<List<List<TriNode>>, int>();

            //测试
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
            object PolygonSymbol = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);

            #region 是一个折线结构
            if (ProcessStruct.Type == 1)
            {
                if (CurPo.BendAngle[CurPo.PointList.IndexOf(ProcessStruct.NodeList[1])][1] > 0)
                {
                    int AreaType = 2;
                    List<TriNode> RetailedNodes = new List<TriNode>(); RetailedNodes.Add(ProcessStruct.NodeList[0]); RetailedNodes.Add(ProcessStruct.NodeList[2]);//处理后剩下的节点
                    List<TriNode> ProcessArea = new List<TriNode>(); ProcessArea = ProcessStruct.NodeList;
                    List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(RetailedNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(RetailedNodes);
                    ProcessResult.Add(ResultNodes, AreaType);
                }

                else
                {
                    int AreaType = 1;
                    List<TriNode> RetailedNodes = new List<TriNode>(); RetailedNodes.Add(ProcessStruct.NodeList[0]); RetailedNodes.Add(ProcessStruct.NodeList[2]);//处理后剩下的节点
                    List<TriNode> ProcessArea = new List<TriNode>(); ProcessArea = ProcessStruct.NodeList;
                    List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(RetailedNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(RetailedNodes);
                    ProcessResult.Add(ResultNodes, AreaType);
                }

                //测试
                //IPolygon CacheCurPo = this.PolygonObjectConvert(CurPo);
                //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
                //pMapControl.Map.RecalcFullExtent();
            }
            #endregion

            #region 凸部结构
            else if (ProcessStruct.Type == 2)
            {
                int StartID = -1;

                #region 获得第一个直角，判断该向那一侧做平行线
                double Dis1 = Math.Sqrt((ProcessStruct.NodeList[0].X - ProcessStruct.NodeList[1].X) * (ProcessStruct.NodeList[0].X - ProcessStruct.NodeList[1].X)
                + (ProcessStruct.NodeList[0].Y - ProcessStruct.NodeList[1].Y) * (ProcessStruct.NodeList[0].Y - ProcessStruct.NodeList[1].Y));
                double Dis2 = Math.Sqrt((ProcessStruct.NodeList[2].X - ProcessStruct.NodeList[3].X) * (ProcessStruct.NodeList[2].X - ProcessStruct.NodeList[3].X)
                    + (ProcessStruct.NodeList[2].Y - ProcessStruct.NodeList[3].Y) * (ProcessStruct.NodeList[2].Y - ProcessStruct.NodeList[3].Y));
                double Angle1 = ProcessStruct.OrthAngle[1]; double Angle2 = ProcessStruct.OrthAngle[2];

                #region 若第一个角是直角
                if (Math.Abs(Math.Abs(Angle1) - Pi / 2) < PerAngle)
                {
                    double DisAngle = Math.Abs(Angle1 - Angle2);
                    double Dis22 = Dis2 * Math.Cos(DisAngle);

                    if (Dis22 > Dis1)
                    {
                        StartID = 0;
                    }

                    else
                    {
                        StartID = 3;
                    }
                }
                #endregion

                #region 第二个角是直角
                else
                {
                    double DisAngle = Math.Abs(Angle1 - Angle2);
                    double Dis12 = Dis1 * Math.Cos(DisAngle);

                    if (Dis12 > Dis2)
                    {
                        StartID = 3;
                    }

                    else
                    {
                        StartID = 0;
                    }
                }
                #endregion
                #endregion

                #region 做平行线获得结构
                double k = (ProcessStruct.NodeList[2].Y - ProcessStruct.NodeList[1].Y) / (ProcessStruct.NodeList[2].X - ProcessStruct.NodeList[1].X);//求中间线的斜率
                TriNode CacheNode = new TriNode(ProcessStruct.NodeList[StartID].X + 1, ProcessStruct.NodeList[StartID].Y + k); 
                TriNode TargetNode = this.CalIntersePoint(ProcessStruct.NodeList[StartID], CacheNode, ProcessStruct.NodeList[StartID * (-2) / 3 + 2], ProcessStruct.NodeList[StartID * (-2) / 3 + 3]);
                List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                NewNodes.Add(ProcessStruct.NodeList[StartID]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[StartID * (-1) + 3]);
                ProcessArea.Add(ProcessStruct.NodeList[StartID]); ProcessArea.Add(ProcessStruct.NodeList[StartID / 3 + 1]); ProcessArea.Add(ProcessStruct.NodeList[StartID *(-1)/ 3 + 2]); ProcessArea.Add(TargetNode);
                ConnectLine.Add(ProcessStruct.NodeList[StartID]); ConnectLine.Add(TargetNode);
                int AreaType = 2;

                List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                ProcessResult.Add(ResultNodes, AreaType);
                #endregion

                //测试
                //IPolygon CacheCurPo = this.PolygonObjectConvert(CurPo);
                //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
                //pMapControl.Map.RecalcFullExtent();
            }
            #endregion

            #region 凹部结构
            else if (ProcessStruct.Type == 3)
            {
                int StartID = -1;

                #region 获得第一个直角，判断该向那一侧做平行线
                double Dis1 = Math.Sqrt((ProcessStruct.NodeList[0].X - ProcessStruct.NodeList[1].X) * (ProcessStruct.NodeList[0].X - ProcessStruct.NodeList[1].X)
                + (ProcessStruct.NodeList[0].Y - ProcessStruct.NodeList[1].Y) * (ProcessStruct.NodeList[0].Y - ProcessStruct.NodeList[1].Y));
                double Dis2 = Math.Sqrt((ProcessStruct.NodeList[2].X - ProcessStruct.NodeList[3].X) * (ProcessStruct.NodeList[2].X - ProcessStruct.NodeList[3].X)
                    + (ProcessStruct.NodeList[2].Y - ProcessStruct.NodeList[3].Y) * (ProcessStruct.NodeList[2].Y - ProcessStruct.NodeList[3].Y));
                double Angle1 = ProcessStruct.OrthAngle[1]; double Angle2 = ProcessStruct.OrthAngle[2];

                #region 若第一个角是直角
                if (Math.Abs(Math.Abs(Angle1) - Pi / 2) < PerAngle)
                {
                    double DisAngle = Math.Abs(Angle1 - Angle2);
                    double Dis22 = Dis2 * Math.Cos(DisAngle);

                    if (Dis22 > Dis1)
                    {
                        StartID = 0;
                    }

                    else
                    {
                        StartID = 3;
                    }
                }
                #endregion

                #region 第二个角是直角
                else
                {
                    double DisAngle = Math.Abs(Angle1 - Angle2);
                    double Dis12 = Dis1 * Math.Cos(DisAngle);

                    if (Dis12 > Dis2)
                    {
                        StartID = 3;
                    }

                    else
                    {
                        StartID = 0;
                    }
                }
                #endregion
                #endregion

                #region 做平行线处理结构
                double k = (ProcessStruct.NodeList[2].Y - ProcessStruct.NodeList[1].Y) / (ProcessStruct.NodeList[2].X - ProcessStruct.NodeList[1].X);//求中间线的斜率
                TriNode CacheNode = new TriNode(ProcessStruct.NodeList[StartID].X + 1, ProcessStruct.NodeList[StartID].Y + k); 
                TriNode TargetNode = this.CalIntersePoint(ProcessStruct.NodeList[StartID], CacheNode, ProcessStruct.NodeList[StartID * (-2) / 3 + 2], ProcessStruct.NodeList[StartID * (-2) / 3 + 3]);
                List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                NewNodes.Add(ProcessStruct.NodeList[StartID]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[StartID * (-1) + 3]);
                ProcessArea.Add(ProcessStruct.NodeList[StartID]); ProcessArea.Add(ProcessStruct.NodeList[StartID / 3 + 1]); ProcessArea.Add(ProcessStruct.NodeList[StartID * (-1) / 3 + 2]); ProcessArea.Add(TargetNode);
                ConnectLine.Add(ProcessStruct.NodeList[StartID]); ConnectLine.Add(TargetNode);
                int AreaType = 1;

                List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                ProcessResult.Add(ResultNodes, AreaType);
                #endregion

                //测试
                //IPolygon CacheCurPo = this.PolygonObjectConvert(CurPo);
                //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
                //pMapControl.Map.RecalcFullExtent();
            }
            #endregion

            #region 阶梯状结构(平行线处理，可能有错误)
            //else if (ProcessStruct.Type == 4)
            //{
            //    double k = (ProcessStruct.NodeList[2].Y - ProcessStruct.NodeList[1].Y) / (ProcessStruct.NodeList[2].X - ProcessStruct.NodeList[1].X);//求中间线的斜率
            //    int StartID = -1;

            //    #region 判断做平行线的起点
            //    #region 如果第一个bend是凸的
            //    if (ProcessStruct.OrthAngle[1] > 0)
            //    {
            //        StartID = 3;
            //    }
            //    #endregion

            //    #region 如果第二个bend是凸的
            //    else
            //    {
            //        StartID = 0;
            //    }
            //    #endregion
            //    #endregion

            //    //测试
            //    //IPolygon CacheCurPo = this.PolygonObjectConvert(CurPo);
            //    //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
            //    //pMapControl.Map.RecalcFullExtent();

            //    #region 做平行线处理结构
            //    TriNode CacheNode = new TriNode(ProcessStruct.NodeList[StartID].X + 1, ProcessStruct.NodeList[StartID].Y + k);

            //    TriNode TargetNode = this.CalIntersePoint(ProcessStruct.NodeList[StartID], CacheNode, ProcessStruct.NodeList[StartID * (-2) / 3 + 2], ProcessStruct.NodeList[StartID * (-2) / 3 + 3]);
            //    List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

            //    NewNodes.Add(ProcessStruct.NodeList[StartID]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[StartID * (-1)  + 3]);
            //    ProcessArea.Add(ProcessStruct.NodeList[StartID]); ProcessArea.Add(ProcessStruct.NodeList[StartID / 3 + 1]); ProcessArea.Add(ProcessStruct.NodeList[StartID * (-1) / 3 + 2]); ProcessArea.Add(TargetNode);
            //    ConnectLine.Add(ProcessStruct.NodeList[StartID]); ConnectLine.Add(TargetNode); ConnectLine.Add(ProcessStruct.NodeList[StartID * (-1) / 3 + 2]); 
            //    int AreaType = 1;

            //    List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
            //    ProcessResult.Add(ResultNodes, AreaType);
            //    #endregion
            //}
            #endregion

            #region 阶梯状结构（填充直角区域）：一个直角对应一个直角的处理方法；两个直角对应两种直角处理方法（这里需要细致的修改！不然，会出现问题）
            else if (ProcessStruct.Type == 4)
            {
                int AreaType = -1;
                double Angle1 = ProcessStruct.OrthAngle[1]; double Angle2 = ProcessStruct.OrthAngle[2];
                double k = (ProcessStruct.NodeList[2].Y - ProcessStruct.NodeList[1].Y) / (ProcessStruct.NodeList[2].X - ProcessStruct.NodeList[1].X);//求中间线的斜率int
                int StartID=-1;

                #region 确定平行线的起点
                #region 若两个角都是直角（填充外部）
                if (Math.Abs(Math.Abs(Angle1) - Pi / 2) < PerAngle && Math.Abs(Math.Abs(Angle2) - Pi / 2) < PerAngle)
                {
                     if (ProcessStruct.OrthAngle[1] > 0)
                     {
                         StartID = 3;
                     }
                     else
                     {
                         StartID = 0;
                     }

                     AreaType = 1;
                }
                #endregion

                #region 若第一个角是直角
                else if (Math.Abs(Math.Abs(Angle1) - Pi / 2) < PerAngle )
                {
                   StartID = 3;

                   if (Angle1 > 0)
                   {
                       AreaType = 1;
                   }

                   else
                   {
                       AreaType = 2;
                   }
                }
                #endregion

                #region 若第二个角是直角
                else if (Math.Abs(Math.Abs(Angle2) - Pi / 2) < PerAngle)
                {
                    StartID = 0;

                    if (Angle2 > 0)
                    {
                        AreaType = 1;
                    }

                    else
                    {
                        AreaType = 2;
                    }
                }
                #endregion
                #endregion

                #region 做平行线处理结构
                TriNode CacheNode = new TriNode(ProcessStruct.NodeList[StartID].X + 1, ProcessStruct.NodeList[StartID].Y + k);

                TriNode TargetNode = this.CalIntersePoint(ProcessStruct.NodeList[StartID], CacheNode, ProcessStruct.NodeList[StartID * (-2) / 3 + 2], ProcessStruct.NodeList[StartID * (-2) / 3 + 3]);
                List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                NewNodes.Add(ProcessStruct.NodeList[StartID]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[StartID * (-1) + 3]);
                ProcessArea.Add(ProcessStruct.NodeList[StartID]); ProcessArea.Add(ProcessStruct.NodeList[StartID / 3 + 1]); ProcessArea.Add(ProcessStruct.NodeList[StartID * (-1) / 3 + 2]); ProcessArea.Add(TargetNode);
                ConnectLine.Add(ProcessStruct.NodeList[StartID]); ConnectLine.Add(TargetNode); ConnectLine.Add(ProcessStruct.NodeList[StartID * (-1) / 3 + 2]);

                List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                ProcessResult.Add(ResultNodes, AreaType);
                #endregion
            }
            #endregion

            #region Corner的处理
            else if (ProcessStruct.Type == 5)
            {
                TriNode TargetNode = this.CalIntersePoint(ProcessStruct.NodeList[0], ProcessStruct.NodeList[1], ProcessStruct.NodeList[2], ProcessStruct.NodeList[3]);
                List<TriNode> NewNodes = new List<TriNode>(); List<TriNode> ProcessArea = new List<TriNode>(); List<TriNode> ConnectLine = new List<TriNode>();

                NewNodes.Add(ProcessStruct.NodeList[0]); NewNodes.Add(TargetNode); NewNodes.Add(ProcessStruct.NodeList[3]);
                ProcessArea.Add(ProcessStruct.NodeList[1]); ProcessArea.Add(TargetNode); ProcessArea.Add(ProcessStruct.NodeList[2]);
                ConnectLine.Add(ProcessStruct.NodeList[1]); ConnectLine.Add(TargetNode); ConnectLine.Add(ProcessStruct.NodeList[2]);
                int AreaType = 1;

                List<List<TriNode>> ResultNodes = new List<List<TriNode>>(); ResultNodes.Add(NewNodes); ResultNodes.Add(ProcessArea); ResultNodes.Add(ConnectLine);
                ProcessResult.Add(ResultNodes, AreaType);

                //测试
                //IPolygon CacheCurPo = this.PolygonObjectConvert(CurPo);
                //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
                //pMapControl.Map.RecalcFullExtent();
            }
            #endregion

            return ProcessResult;
        }

        /// <summary>
        /// 求点到某两点的垂足
        /// /// </summary>
        /// <param name="v1">顶点</param>
        /// <param name="v2">底边顶点1</param>
        /// <param name="v3">底边顶点2</param>
        /// <param name="nearestPoint">最近距离</param>
        /// <param name="v4">最近距离对应的点</param>
        /// <param name="isPerpendicular">最近距离是否是沿着垂线上</param>
        /// <returns>最小距离</returns>
        public TriNode CalMinDisPoint2Line(TriNode p, TriNode s, TriNode e)
        {
            double x = 0, y = 0;
            if ((e.X - s.X) == 0 && (e.Y - s.Y) != 0)//平行于y轴
            {
                x = e.X;
                y = p.Y;
            }
            else if ((e.Y - s.Y) == 0 && (e.X - s.X) != 0)//平行于X轴
            {
                x = p.X;
                y = e.Y;
            }
            else if ((e.Y - s.Y) == 0 && (e.X - s.X) == 0)
            {
                x = e.X;
                y = e.Y;
            }
            else if ((e.Y - s.Y) != 0 && (e.X - s.X) != 0)
            {
                double k = (e.Y - s.Y) / (e.X - s.X);
                x = (k * p.Y - k * s.Y + p.X + k * k * s.X) / (1 + k * k);
                y = (k * p.X - k * s.X + s.Y + k * k * p.Y) / (1 + k * k);
            }
            return new TriNode(x, y);
        }

        /// <summary>
        /// 求两条给定线段延长线的交点(只考虑存在唯一交点的情况，不考虑平行或重合)
        /// </summary>
        /// <param name="s1">第一条线的起点</param>
        /// <param name="e1">第一条线的终点</param>
        /// <param name="s2">第二条线的起点</param>
        /// <param name="e2">第二条线的终点</param>
        /// <returns></returns>
        public TriNode CalIntersePoint(TriNode s1, TriNode e1, TriNode s2, TriNode e2)
        {
            double k1 = (s1.Y - e1.Y) / (s1.X - e1.X);
            double k2 = (s2.Y - e2.Y) / (s2.X - e2.X);

            double A1 = k1; double B1 = -1; double C1 = s1.Y - s1.X * k1;
            double A2 = k2; double B2 = -1; double C2 = s2.Y - s2.X * k2;

            double m = A1 * B2 - A2 * B1;
            double X1 = (C2 * B1 - C1 * B2) / m;
            double Y1 = (C1 * A2 - C2 * A1) / m;

            return new TriNode(X1, Y1);
        }

        /// <summary>
        /// 对一个给定的建筑物进行简化(考虑的约束条件是处理单元面积的大小，若处理单元面积小，则首先处理该单元)
        /// </summary>
        /// <param name="CurPo"></param>
        /// <returns></returns>
        public PolygonObject PolygonSimplified1(PolygonObject CurPo,double PerAngle)
        {
            PolygonObject resultPolygon = null;
            double ShortestDis = 0;//最短边的距离
            List<TriNode> ShortestEdge = PP.GetShortestEdge(CurPo,out ShortestDis);//获得建筑物的最短边及其距离
            List<BasicStruct> BasicStructList = PP.GetStructedNodes(CurPo, PerAngle);//获得一个建筑物中所有定义的基础结构
            List<BasicStruct> InvovedStructList = PP.GetStructForEdge(ShortestEdge, BasicStructList);//获得最短边关联的结构
            int EdgeStructType = this.GetEdgeStructType(CurPo,ShortestEdge,PerAngle);//获得建筑物中待处理的边与对应结构的关系

            #region 第一种结构
            if (EdgeStructType == 1)
            {
                Dictionary<List<List<TriNode>>, int> ProcessResult = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                resultPolygon= this.GetFinalPolygon(CurPo, ProcessResult.FirstOrDefault().Key[1], ProcessResult.FirstOrDefault().Value);
            }
            #endregion

            #region 第二种结构
            if (EdgeStructType == 2)
            {
                Dictionary<List<List<TriNode>>, int> ProcessResult = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult.FirstOrDefault().Key[1], ProcessResult.FirstOrDefault().Value);
            }
            #endregion

            #region 第三种结构
            if (EdgeStructType == 3)
            {
                Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                #region 只单纯比较面积变化
                PolygonObject ProcessPo1 = new PolygonObject(1, ProcessResult1.FirstOrDefault().Key[1]);
                PolygonObject ProcessPo2 = new PolygonObject(2, ProcessResult2.FirstOrDefault().Key[1]);
                #endregion

                if (ProcessPo1.Area > ProcessPo2.Area)
                {
                    resultPolygon= this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                }

                else
                {
                    resultPolygon= this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                }
                
            }
            #endregion

            #region 第四种结构
            if (EdgeStructType == 4)
            {
                Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);
                
                #region 只单纯比较面积变化
                PolygonObject ProcessPo1 = new PolygonObject(1, ProcessResult1.FirstOrDefault().Key[1]);
                PolygonObject ProcessPo2 = new PolygonObject(2, ProcessResult2.FirstOrDefault().Key[1]);
                #endregion

                if (ProcessPo1.Area > ProcessPo2.Area)
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                }

                else
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                }
            }
            #endregion

            #region 第五种结构
            if (EdgeStructType == 5)
            {
                Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                #region 只单纯比较面积变化
                PolygonObject ProcessPo1 = new PolygonObject(1, ProcessResult1.FirstOrDefault().Key[1]);
                PolygonObject ProcessPo2 = new PolygonObject(2, ProcessResult2.FirstOrDefault().Key[1]);
                #endregion

                if (ProcessPo1.Area > ProcessPo2.Area)
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                }

                else
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                }
            }
            #endregion
             
            #region 第六种结构
            if (EdgeStructType == 6)
            {
                Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                #region 只单纯比较面积变化
                PolygonObject ProcessPo1 = new PolygonObject(1, ProcessResult1.FirstOrDefault().Key[1]);
                PolygonObject ProcessPo2 = new PolygonObject(2, ProcessResult2.FirstOrDefault().Key[1]);
                #endregion

                if (ProcessPo1.Area > ProcessPo2.Area)
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                }

                else
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                }
            }
            #endregion

            #region 第七种结构
            if (EdgeStructType == 7)
            {
                Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                #region 只单纯比较面积变化
                PolygonObject ProcessPo1 = new PolygonObject(1, ProcessResult1.FirstOrDefault().Key[1]);
                PolygonObject ProcessPo2 = new PolygonObject(2, ProcessResult2.FirstOrDefault().Key[1]);
                #endregion

                if (ProcessPo1.Area > ProcessPo2.Area)
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                }

                else
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                }
            }
            #endregion

            #region 第八种结构
            if (EdgeStructType == 8)
            {
                Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                #region 只单纯比较面积变化
                PolygonObject ProcessPo1 = new PolygonObject(1, ProcessResult1.FirstOrDefault().Key[1]);
                PolygonObject ProcessPo2 = new PolygonObject(2, ProcessResult2.FirstOrDefault().Key[1]);
                #endregion

                if (ProcessPo1.Area > ProcessPo2.Area)
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                }

                else
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                }
            }
            #endregion

            #region 第十种结构
            if (EdgeStructType == 10)
            {
                Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);
                Dictionary<List<List<TriNode>>, int> ProcessResult3 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                #region 只单纯比较面积变化
                PolygonObject ProcessPo1 = new PolygonObject(1, ProcessResult1.FirstOrDefault().Key[1]);
                PolygonObject ProcessPo2 = new PolygonObject(2, ProcessResult2.FirstOrDefault().Key[1]);
                PolygonObject ProcessPo3 = new PolygonObject(3, ProcessResult3.FirstOrDefault().Key[1]);
                #endregion

                if (ProcessPo1.Area < ProcessPo2.Area && ProcessPo1.Area<ProcessPo3.Area)
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                }

                else if (ProcessPo2.Area < ProcessPo1.Area && ProcessPo2.Area < ProcessPo3.Area)
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                }

                else
                {
                    resultPolygon= this.GetFinalPolygon(CurPo, ProcessResult3.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                }
              
            }
            #endregion

            #region 第十一种结构
            if (EdgeStructType == 11)
            {
                List<BasicStruct> fStructList = new List<BasicStruct>();
                for (int i = 0; i < InvovedStructList.Count; i++)
                {
                    if (InvovedStructList[i].Type != 4)
                    {
                        fStructList.Add(InvovedStructList[i]);
                    }
                }

                Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, fStructList[0], PerAngle);
                Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, fStructList[1], PerAngle);

                #region 只单纯比较面积变化
                PolygonObject ProcessPo1 = new PolygonObject(1, ProcessResult1.FirstOrDefault().Key[1]);
                PolygonObject ProcessPo2 = new PolygonObject(2, ProcessResult2.FirstOrDefault().Key[1]);
                #endregion

                if (ProcessPo1.Area > ProcessPo2.Area)
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                }

                else
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                }
            }
            #endregion

            #region 第十二种结构
            if (EdgeStructType == 12)
            {
                if (InvovedStructList[0].Type != InvovedStructList[1].Type)
                {
                    Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                    Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                    #region 只单纯比较面积变化
                    PolygonObject ProcessPo1 = new PolygonObject(1, ProcessResult1.FirstOrDefault().Key[1]);
                    PolygonObject ProcessPo2 = new PolygonObject(2, ProcessResult2.FirstOrDefault().Key[1]);
                    #endregion

                    if (ProcessPo1.Area > ProcessPo2.Area)
                    {
                        resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                    }

                    else
                    {
                        resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                    }
                }

                else
                {
                    Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                    Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[2], PerAngle);

                    #region 只单纯比较面积变化
                    PolygonObject ProcessPo1 = new PolygonObject(1, ProcessResult1.FirstOrDefault().Key[1]);
                    PolygonObject ProcessPo2 = new PolygonObject(2, ProcessResult2.FirstOrDefault().Key[1]);
                    #endregion

                    if (ProcessPo1.Area > ProcessPo2.Area)
                    {
                        resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                    }

                    else
                    {
                        resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                    }
                }
            }
            #endregion

            #region 第十三种结构
            if (EdgeStructType == 13)
            {
                Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);
                Dictionary<List<List<TriNode>>, int> ProcessResult3 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                #region 只单纯比较面积变化
                PolygonObject ProcessPo1 = new PolygonObject(1, ProcessResult1.FirstOrDefault().Key[1]);
                PolygonObject ProcessPo2 = new PolygonObject(2, ProcessResult2.FirstOrDefault().Key[1]);
                PolygonObject ProcessPo3 = new PolygonObject(3, ProcessResult3.FirstOrDefault().Key[1]);
                #endregion

                if (ProcessPo1.Area < ProcessPo2.Area && ProcessPo1.Area < ProcessPo3.Area)
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                }

                else if (ProcessPo2.Area < ProcessPo1.Area && ProcessPo2.Area < ProcessPo3.Area)
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                }

                else
                {
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult3.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                }
            }
            #endregion

            #region 第十四种结构
            if (EdgeStructType == 14)
            {
               int CountContains = 0;
               for(int i=0;i<InvovedStructList[0].PolylineList.Count;i++)
               {
                   if (InvovedStructList[1].PolylineList.Contains(InvovedStructList[0].PolylineList[i]))
                   {
                       CountContains++;
                   }
               }


               if (CountContains == 2)
               {
                   Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                   Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[2], PerAngle);

                   #region 只单纯比较面积变化
                   PolygonObject ProcessPo1 = new PolygonObject(1, ProcessResult1.FirstOrDefault().Key[1]);
                   PolygonObject ProcessPo2 = new PolygonObject(2, ProcessResult2.FirstOrDefault().Key[1]);
                   #endregion

                   if (ProcessPo1.Area > ProcessPo2.Area)
                   {
                       resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                   }

                   else
                   {
                       resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                   }
               }

               else
               {
                   Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                   Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                   #region 只单纯比较面积变化
                   PolygonObject ProcessPo1 = new PolygonObject(1, ProcessResult1.FirstOrDefault().Key[1]);
                   PolygonObject ProcessPo2 = new PolygonObject(2, ProcessResult2.FirstOrDefault().Key[1]);
                   #endregion

                   if (ProcessPo1.Area > ProcessPo2.Area)
                   {
                       resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                   }

                   else
                   {
                       resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                   }
               }
            }
            #endregion

            return resultPolygon;
        }

        /// <summary>
        /// 对一个给定的建筑物简化，建立它的层次结构
        /// </summary>
        /// <param name="InitialPo"></param>
        /// <param name="PerAngle"></param>
        /// <returns></returns>
        public List<PolygonObject> SimplifiedLevelBuilt1(PolygonObject InitialPo,double PerAngle)
        {
            List<PolygonObject> SimplifiedLevel = new List<PolygonObject>();         
            PP.DeleteSamePoint(InitialPo, 0.00000001);//需要消除重复的节点
            PP.DeleteOnLinePoint(InitialPo, Pi / 36); //需要消除在同一条直线上的点
            SimplifiedLevel.Add(InitialPo);

            while (SimplifiedLevel[SimplifiedLevel.Count-1].PointList.Count > 5)
            {
                PolygonObject CachePolygon = this.PolygonSimplified1(SimplifiedLevel[SimplifiedLevel.Count - 1],PerAngle);
                PP.DeleteSamePoint(InitialPo, 0.00000001);//需要删除重复的节点(首先删除重复节点，再做消除同一条直线上的点的操作)
                PP.DeleteOnLinePoint(CachePolygon, Pi / 36); //需要消除在同一条直线上的点
               
                SimplifiedLevel.Add(CachePolygon);
            }

            return SimplifiedLevel;
        }

        /// <summary>
        /// 对一个给定的建筑物进行简化(考虑完整的约束条件,顾及到面积与结构规则，没有顾及是否自交，没有回溯);其次，添加距离约束
        /// </summary>
        /// InitialPo 最初的建筑物
        /// CurPo简化建筑物
        /// PerAngle=表示是直角的阈值条件
        /// Dis=最短距离
        /// AreaChange=面积变化
        /// <param name="CurPo"></param>
        /// Label false表示简化成功；true表示简化失败
        /// <returns></returns>
        public PolygonObject PolygonSimplified2(PolygonObject InitialPo, PolygonObject CurPo, double PerAngle,double Dis,double AreaChangeT,out bool Label)
        {
            Label = false;//false表示简化成功；true表示简化失败
            PolygonObject resultPolygon = null;
            double ShortestDis = 0;//最短边的距离
            List<TriNode> ShortestEdge = PP.GetShortestEdge(CurPo, out ShortestDis);//获得建筑物的最短边及其距离
            List<BasicStruct> BasicStructList = PP.GetStructedNodes(CurPo, PerAngle);//获得一个建筑物中所有定义的基础结构
            List<BasicStruct> InvovedStructList = PP.GetStructForEdge(ShortestEdge, BasicStructList);//获得最短边关联的结构
            int EdgeStructType = this.GetEdgeStructType(CurPo, ShortestEdge, PerAngle);//获得建筑物中待处理的边与对应结构的关系

            if (ShortestDis < Dis)
            {
                #region 第一种结构
                if (EdgeStructType == 1)
                {
                    Dictionary<List<List<TriNode>>, int> ProcessResult = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult.FirstOrDefault().Key[1], ProcessResult.FirstOrDefault().Value);

                    #region 规则控制
                    double AreaChange = this.GetAreaChange(InitialPo, resultPolygon);
                    if (AreaChange > AreaChangeT)
                    {
                        resultPolygon = null;
                        Label = true;
                    }
                    #endregion
                }
                #endregion

                #region 第二种结构
                if (EdgeStructType == 2)
                {
                    Dictionary<List<List<TriNode>>, int> ProcessResult = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                    resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult.FirstOrDefault().Key[1], ProcessResult.FirstOrDefault().Value);

                    #region 规则控制
                    double AreaChange = this.GetAreaChange(InitialPo, resultPolygon);
                    if (AreaChange > AreaChangeT)
                    {
                        resultPolygon = null;
                        Label = true;
                    }
                    #endregion
                }
                #endregion

                #region 第三种结构
                if (EdgeStructType == 3)
                {
                    Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                    Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                    PolygonObject ProcessPo1  = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                    PolygonObject ProcessPo2  = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);

                    double AreaChange1 = this.GetAreaChange(InitialPo, ProcessPo1);
                    double AreaChange2 = this.GetAreaChange(InitialPo, ProcessPo2);

                    #region 规则控制
                    if (AreaChange1 > AreaChangeT && AreaChange2 > AreaChangeT)
                    {
                        resultPolygon = null;
                        Label = true;
                    }

                    else
                    {
                        if (AreaChange1 <= AreaChange2)
                        {
                            resultPolygon = ProcessPo1;
                        }

                        else
                        {
                            resultPolygon = ProcessPo2;
                        }
                    }
                    #endregion
                }
                #endregion

                #region 第四种结构
                if (EdgeStructType == 4)
                {
                    Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                    Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                    PolygonObject ProcessPo1 =  this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                    PolygonObject ProcessPo2 = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);

                    double AreaChange1 = this.GetAreaChange(InitialPo, ProcessPo1);
                    double AreaChange2 = this.GetAreaChange(InitialPo, ProcessPo2);

                    #region 规则控制
                    if ((InvovedStructList[0].Type == 2 || InvovedStructList[0].Type == 3) && AreaChange1 < AreaChangeT)
                    {
                        resultPolygon = ProcessPo1;
                    }

                    else if ((InvovedStructList[1].Type == 2 || InvovedStructList[1].Type == 3) && AreaChange2 < AreaChangeT)
                    {
                        resultPolygon = ProcessPo2;
                    }

                    else if (AreaChange1 > AreaChangeT && AreaChange2 > AreaChangeT)
                    {
                        resultPolygon = null;
                        Label = true;
                    }

                    else
                    {
                        if (AreaChange1 <= AreaChange2)
                        {
                            resultPolygon = ProcessPo1;
                        }

                        else
                        {
                            resultPolygon = ProcessPo2;
                        }
                    }
                    #endregion
                }
                #endregion

                #region 第五种结构
                if (EdgeStructType == 5)
                {
                    Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                    Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                    PolygonObject ProcessPo1 = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                    PolygonObject ProcessPo2 = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);

                    double AreaChange1 = this.GetAreaChange(InitialPo, ProcessPo1);
                    double AreaChange2 = this.GetAreaChange(InitialPo, ProcessPo2);

                    #region 规则控制
                    if (InvovedStructList[0].Type == 4  && AreaChange1 < AreaChangeT)
                    {
                        resultPolygon = ProcessPo1;
                    }

                    else if (InvovedStructList[1].Type == 4 && AreaChange2 < AreaChangeT)
                    {
                        resultPolygon = ProcessPo2;
                    }

                    else if (AreaChange1 > AreaChangeT && AreaChange2 > AreaChangeT)
                    {
                        resultPolygon = null;
                        Label = true;
                    }

                    else
                    {
                        if (AreaChange1 <= AreaChange2)
                        {
                            resultPolygon = ProcessPo1;
                        }

                        else
                        {
                            resultPolygon = ProcessPo2;
                        }
                    }
                    #endregion

                }
                #endregion

                #region 第六种结构
                if (EdgeStructType == 6)
                {
                    Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                    Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                    PolygonObject ProcessPo1 = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                    PolygonObject ProcessPo2 = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                    double AreaChange1 = this.GetAreaChange(InitialPo, ProcessPo1);
                    double AreaChange2 = this.GetAreaChange(InitialPo, ProcessPo2);

                    #region 规则控制
                    if (AreaChange1 > AreaChangeT && AreaChange2 > AreaChangeT)
                    {
                        resultPolygon = null;
                        Label = true;
                    }

                    else
                    {
                        if (AreaChange1 <= AreaChange2)
                        {
                            resultPolygon = ProcessPo1;
                        }

                        else
                        {
                            resultPolygon = ProcessPo2;
                        }
                    }
                    #endregion
                }
                #endregion

                #region 第七种结构
                if (EdgeStructType == 7)
                {
                    Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                    Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                    PolygonObject ProcessPo1 = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                    PolygonObject ProcessPo2 = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                    double AreaChange1 = this.GetAreaChange(InitialPo, ProcessPo1);
                    double AreaChange2 = this.GetAreaChange(InitialPo, ProcessPo2);

                    #region 规则控制
                    if (AreaChange1 > AreaChangeT && AreaChange2 > AreaChangeT)
                    {
                        resultPolygon = null;
                        Label = true;
                    }

                    else
                    {
                        if (AreaChange1 <= AreaChange2)
                        {
                            resultPolygon = ProcessPo1;
                        }

                        else
                        {
                            resultPolygon = ProcessPo2;
                        }
                    }
                    #endregion
                }
                #endregion

                #region 第八种结构
                if (EdgeStructType == 8)
                {
                    Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                    Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                    PolygonObject ProcessPo1 = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                    PolygonObject ProcessPo2 = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                    double AreaChange1 = this.GetAreaChange(InitialPo, ProcessPo1);
                    double AreaChange2 = this.GetAreaChange(InitialPo, ProcessPo2);

                    #region 规则控制
                    if (AreaChange1 > AreaChangeT && AreaChange2 > AreaChangeT)
                    {
                        resultPolygon = null;
                        Label = true;
                    }

                    else
                    {
                        if (AreaChange1 <= AreaChange2)
                        {
                            resultPolygon = ProcessPo1;
                        }

                        else
                        {
                            resultPolygon = ProcessPo2;
                        }
                    }
                    #endregion
                }
                #endregion

                #region 第十种结构
                if (EdgeStructType == 10)
                {
                    Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                    Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);
                    Dictionary<List<List<TriNode>>, int> ProcessResult3 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);
                    PolygonObject ProcessPo1 = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                    PolygonObject ProcessPo2 = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                    PolygonObject ProcessPo3 = this.GetFinalPolygon(CurPo, ProcessResult3.FirstOrDefault().Key[1], ProcessResult3.FirstOrDefault().Value);
                    double AreaChange1 = this.GetAreaChange(InitialPo, ProcessPo1);
                    double AreaChange2 = this.GetAreaChange(InitialPo, ProcessPo2);
                    double AreaChange3 = this.GetAreaChange(InitialPo, ProcessPo3);

                    #region 规则控制
                    if (AreaChange1 <= AreaChange2 & AreaChange1 <= AreaChange3)
                    {
                        if (AreaChange1 > AreaChangeT)
                        {
                            resultPolygon = null;
                            Label = true;
                        }

                        else
                        {
                            resultPolygon = ProcessPo1;
                        }
                    }

                    else if (AreaChange2 <= AreaChange1 & AreaChange2 <= AreaChange3)
                    {
                        if (AreaChange2 > AreaChangeT)
                        {
                            resultPolygon = null;
                            Label = true;
                        }

                        else
                        {
                            resultPolygon = ProcessPo2;
                        }
                    }

                    else if (AreaChange3 <= AreaChange1 & AreaChange3 <= AreaChange1)
                    {
                        if (AreaChange3 > AreaChangeT)
                        {
                            resultPolygon = null;
                            Label = true;
                        }

                        else
                        {
                            resultPolygon = ProcessPo3;
                        }
                    }
                    #endregion
                }
                #endregion

                #region 第十一种结构
                if (EdgeStructType == 11)
                {
                    List<BasicStruct> fStructList = new List<BasicStruct>();
                    for (int i = 0; i < InvovedStructList.Count; i++)
                    {
                        if (InvovedStructList[i].Type != 4)
                        {
                            fStructList.Add(InvovedStructList[i]);
                        }
                    }

                    Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, fStructList[0], PerAngle);
                    Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, fStructList[1], PerAngle);

                    PolygonObject ProcessPo1 = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                    PolygonObject ProcessPo2 = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                    double AreaChange1 = this.GetAreaChange(InitialPo, ProcessPo1);
                    double AreaChange2 = this.GetAreaChange(InitialPo, ProcessPo2);

                    #region 规则控制
                    if (AreaChange1 > AreaChangeT && AreaChange2 > AreaChangeT)
                    {
                        resultPolygon = null;
                        Label = true;
                    }

                    else
                    {
                        if (AreaChange1 <= AreaChange2)
                        {
                            resultPolygon = ProcessPo1;
                        }

                        else
                        {
                            resultPolygon = ProcessPo2;
                        }
                    }
                    #endregion
                }
                #endregion

                #region 第十二种结构
                if (EdgeStructType == 12)
                {
                    if (InvovedStructList[0].Type != InvovedStructList[1].Type)
                    {
                        Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                        Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                        PolygonObject ProcessPo1 = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                        PolygonObject ProcessPo2 = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                        double AreaChange1 = this.GetAreaChange(InitialPo, ProcessPo1);
                        double AreaChange2 = this.GetAreaChange(InitialPo, ProcessPo2);

                        #region 规则控制
                        if (AreaChange1 > AreaChangeT && AreaChange2 > AreaChangeT)
                        {
                            resultPolygon = null;
                            Label = true;
                        }

                        else
                        {
                            if (AreaChange1 <= AreaChange2)
                            {
                                resultPolygon = ProcessPo1;
                            }

                            else
                            {
                                resultPolygon = ProcessPo2;
                            }
                        }
                        #endregion
                    }

                    else
                    {
                        Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                        Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[2], PerAngle);

                        PolygonObject ProcessPo1 = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                        PolygonObject ProcessPo2 = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                        double AreaChange1 = this.GetAreaChange(InitialPo, ProcessPo1);
                        double AreaChange2 = this.GetAreaChange(InitialPo, ProcessPo2);

                        #region 规则控制
                        if (AreaChange1 > AreaChangeT && AreaChange2 > AreaChangeT)
                        {
                            resultPolygon = null;
                            Label = true;
                        }

                        else
                        {
                            if (AreaChange1 <= AreaChange2)
                            {
                                resultPolygon = ProcessPo1;
                            }

                            else
                            {
                                resultPolygon = ProcessPo2;
                            }
                        }
                        #endregion
                    }
                }
                #endregion

                #region 第十三种结构
                if (EdgeStructType == 13)
                {
                    Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                    Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);
                    Dictionary<List<List<TriNode>>, int> ProcessResult3 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);
                    PolygonObject ProcessPo1 = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                    PolygonObject ProcessPo2 = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                    PolygonObject ProcessPo3 = this.GetFinalPolygon(CurPo, ProcessResult3.FirstOrDefault().Key[1], ProcessResult3.FirstOrDefault().Value);
                    double AreaChange1 = this.GetAreaChange(InitialPo, ProcessPo1);
                    double AreaChange2 = this.GetAreaChange(InitialPo, ProcessPo2);
                    double AreaChange3 = this.GetAreaChange(InitialPo, ProcessPo3);

                    #region 规则控制
                    if (AreaChange1 <= AreaChange2 & AreaChange1 <= AreaChange3)
                    {
                        if (AreaChange1 > AreaChangeT)
                        {
                            resultPolygon = null;
                            Label = true;
                        }

                        else
                        {
                            resultPolygon = ProcessPo1;
                        }
                    }

                    else if (AreaChange2 <= AreaChange1 & AreaChange2 <= AreaChange3)
                    {
                        if (AreaChange2 > AreaChangeT)
                        {
                            resultPolygon = null;
                            Label = true;
                        }

                        else
                        {
                            resultPolygon = ProcessPo2;
                        }
                    }

                    else if (AreaChange3 <= AreaChange1 & AreaChange3 <= AreaChange1)
                    {
                        if (AreaChange3 > AreaChangeT)
                        {
                            resultPolygon = null;
                            Label = true;
                        }

                        else
                        {
                            resultPolygon = ProcessPo3;
                        }
                    }
                    #endregion

                }
                #endregion

                #region 第十四种结构
                if (EdgeStructType == 14)
                {
                    int CountContains = 0;
                    for (int i = 0; i < InvovedStructList[0].PolylineList.Count; i++)
                    {
                        if (InvovedStructList[1].PolylineList.Contains(InvovedStructList[0].PolylineList[i]))
                        {
                            CountContains++;
                        }
                    }


                    if (CountContains == 2)
                    {
                        Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                        Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[2], PerAngle);

                        PolygonObject ProcessPo1 = this.GetFinalPolygon(CurPo, ProcessResult1.FirstOrDefault().Key[1], ProcessResult1.FirstOrDefault().Value);
                        PolygonObject ProcessPo2 = this.GetFinalPolygon(CurPo, ProcessResult2.FirstOrDefault().Key[1], ProcessResult2.FirstOrDefault().Value);
                        double AreaChange1 = this.GetAreaChange(InitialPo, ProcessPo1);
                        double AreaChange2 = this.GetAreaChange(InitialPo, ProcessPo2);

                        #region 规则控制
                        if (AreaChange1 > AreaChangeT && AreaChange2 > AreaChangeT)
                        {
                            resultPolygon = null;
                            Label = true;
                        }

                        else
                        {
                            if (AreaChange1 <= AreaChange2)
                            {
                                resultPolygon = ProcessPo1;
                            }

                            else
                            {
                                resultPolygon = ProcessPo2;
                            }
                        }
                        #endregion
                    }

                    else
                    {
                        Dictionary<List<List<TriNode>>, int> ProcessResult1 = this.StructProcess(CurPo, InvovedStructList[0], PerAngle);
                        Dictionary<List<List<TriNode>>, int> ProcessResult2 = this.StructProcess(CurPo, InvovedStructList[1], PerAngle);

                        PolygonObject ProcessPo1 = new PolygonObject(1, ProcessResult1.FirstOrDefault().Key[1]);
                        PolygonObject ProcessPo2 = new PolygonObject(2, ProcessResult2.FirstOrDefault().Key[1]);
                        double AreaChange1 = this.GetAreaChange(InitialPo, ProcessPo1);
                        double AreaChange2 = this.GetAreaChange(InitialPo, ProcessPo2);

                        #region 规则控制
                        if (AreaChange1 > AreaChangeT && AreaChange2 > AreaChangeT)
                        {
                            resultPolygon = null;
                            Label = true;
                        }

                        else
                        {
                            if (AreaChange1 <= AreaChange2)
                            {
                                resultPolygon = ProcessPo1;
                            }

                            else
                            {
                                resultPolygon = ProcessPo2;
                            }
                        }
                        #endregion
                    }
                }
                #endregion
            }

            return resultPolygon;
        }

        /// <summary>
        /// 投稿论文中简化的算法流程
        /// </summary>
        /// <param name="InitialPo">原始建筑物</param>
        /// <param name="CurPo">待简化建筑物</param>
        /// <param name="PerAngle">直角的约束条件</param>
        /// <param name="Dis"><最短距离/param>
        /// <param name="AreaChangeT">面积变化阈值</param>
        /// OriChange 方向变化阈值
        /// <param name="Label">false表示简化成功；true表示简化失败</param>
        /// <returns></returns>
        public PolygonObject PolygonSimplified3(PolygonObject InitialPo, PolygonObject CurPo, double PerAngle, double Dis, double AreaChangeT, double
             OriChange,out bool Label)
        {
            PP.pMapControl = pMapControl;//测试用

            #region 符号化显示
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
            object PolygonSymbol = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);
            //IPolygon CacheCurPo = this.PolygonObjectConvert(CurPo);
            //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
            //pMapControl.Map.RecalcFullExtent();
            #endregion

            Label = false;//false表示简化成功；true表示简化失败
            PolygonObject TargetPo = null;
            double ShortestDis = 0;//最短边的距离
            List<TriNode> ShortestEdge = PP.GetShortestEdge(CurPo, out ShortestDis);//获得建筑物的最短边及其距离
            ///问题锁定：就是这里
            //IPolygon CacheCurPo = this.PolygonObjectConvert(CurPo);
            //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
            //pMapControl.Map.RecalcFullExtent();

            List<BasicStruct> BasicStructList = PP.GetStructedNodes2(CurPo, PerAngle);//获得一个建筑物中所有定义的基础结构
            List<BasicStruct> InvovedStructList = PP.GetStructForEdge(ShortestEdge, BasicStructList);//获得最短边关联的结构

            #region 获得其中处理直角结构的部分
            List<BasicStruct> SquarePart = new List<BasicStruct>();
            for (int i = 0; i < InvovedStructList.Count; i++)
            {
                if(InvovedStructList[i].Type!=1)
                {
                    SquarePart.Add(InvovedStructList[i]);
                }
            }
            #endregion

            #region 如果存在直角部分
            if (SquarePart.Count > 0)
            {
                double MinChange = 1000000;

                for (int i = 0; i < SquarePart.Count; i++)
                {
                    //CacheCurPo = this.PolygonObjectConvert(CurPo);
                    //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
                    //pMapControl.Map.RecalcFullExtent();

                    Dictionary<List<List<TriNode>>, int> ProcessResult = this.StructProcess2(CurPo, SquarePart[i], PerAngle);

                    //测试
                    //IPolygon CacheCurPo = this.PolygonObjectConvert(CurPo);
                    //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
                    //pMapControl.Map.RecalcFullExtent();

                    PolygonObject resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult.FirstOrDefault().Key[1], ProcessResult.FirstOrDefault().Value);
                    double AreaChange = this.GetAreaChange(InitialPo, resultPolygon);

                    //测试
                    IPolygon Cacheresult = this.PolygonObjectConvert(resultPolygon);
                    pMapControl.DrawShape(Cacheresult, ref PolygonSymbol);
                    pMapControl.Map.RecalcFullExtent();

                    if (AreaChange < MinChange)
                    {
                        TargetPo = resultPolygon;
                        MinChange = AreaChange;
                    }
                }

                double OrientationChange=this.GetOrientationChange(InitialPo,TargetPo);
                if (MinChange > AreaChangeT || OrientationChange > OriChange)
                {
                    TargetPo = null;
                    Label = true;
                }
            }
            #endregion

            #region 如果不存在直角部分
            else
            {
                double MinChange = 1000000;
                for (int i = 0; i < InvovedStructList.Count; i++)
                {
                    //CacheCurPo = this.PolygonObjectConvert(CurPo);
                    //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
                    //pMapControl.Map.RecalcFullExtent();

                    Dictionary<List<List<TriNode>>, int> ProcessResult = this.StructProcess2(CurPo, InvovedStructList[i], PerAngle);
                    PolygonObject resultPolygon = this.GetFinalPolygon(CurPo, ProcessResult.FirstOrDefault().Key[1], ProcessResult.FirstOrDefault().Value);
                    double AreaChange = this.GetAreaChange(InitialPo, resultPolygon);

                    if (AreaChange < MinChange)
                    {
                        TargetPo = resultPolygon;
                        MinChange = AreaChange;
                    }

                    if (MinChange > AreaChangeT)
                    {
                        TargetPo = null;
                    }
                }
            }
            #endregion

            #region 符号化显示
            //IPolygon CacheTargetPo = this.PolygonObjectConvert(TargetPo);
            //pMapControl.DrawShape(CacheTargetPo, ref PolygonSymbol);
            //pMapControl.Map.RecalcFullExtent();
            #endregion

            return TargetPo;
        }

        /// <summary>
        /// 根据处理的单元和面积情况，得到处理后的建筑物目标
        /// </summary>
        /// <param name="Curpo"></param>
        /// <param name="NodeList"></param>
        /// <param name="AreaType"></param>1=面积增加；2=面积减少
        /// <returns></returns>
        public PolygonObject GetFinalPolygon(PolygonObject Curpo, List<TriNode> NodeList, int AreaType)
        {
            IPolygon resultPolygon = null;
            IPolygon CurPolygon = this.PolygonObjectConvert(Curpo);
            PolygonObject ProcessObject = new PolygonObject(1,NodeList);
            IPolygon ProcessPolygon = this.PolygonObjectConvert(ProcessObject);

            if (AreaType == 1)
            {
                ITopologicalOperator iTo = CurPolygon as ITopologicalOperator;
                IGeometry IGeo = iTo.Union(ProcessPolygon);
                resultPolygon = IGeo as IPolygon;
            }

            else if (AreaType == 2)
            {
                ITopologicalOperator iTo = CurPolygon as ITopologicalOperator;
                IGeometry IGeo = iTo.Difference(ProcessPolygon);
                resultPolygon = IGeo as IPolygon;
            }

            #region 符号化显示（测试）
            //PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
            //object PolygonSymbol = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);

            //pMapControl.DrawShape(CurPolygon, ref PolygonSymbol);
            //pMapControl.Map.RecalcFullExtent();
            //pMapControl.DrawShape(ProcessPolygon, ref PolygonSymbol);
            //pMapControl.Map.RecalcFullExtent();
            //pMapControl.DrawShape(resultPolygon, ref PolygonSymbol);
            //pMapControl.Map.RecalcFullExtent();
            #endregion

            return PolygonConvert(resultPolygon);
        }

        /// <summary>
        /// 将建筑物转化为IPolygon
        /// </summary>
        /// <param name="pPolygonObject"></param>
        /// <returns></returns>
        IPolygon PolygonObjectConvert(PolygonObject pPolygonObject)
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

            curPoint = pPolygonObject.PointList[0];
            curResultPoint.PutCoords(curPoint.X, curPoint.Y);
            ring1.AddPoint(curResultPoint, ref missing, ref missing);

            IGeometryCollection pointPolygon = new PolygonClass();
            pointPolygon.AddGeometry(ring1 as IGeometry, ref missing, ref missing);
            IPolygon pPolygon = pointPolygon as IPolygon;

            //PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
            //object PolygonSymbol = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);

            //pMapControl.DrawShape(pPolygon, ref PolygonSymbol);
            //pMapControl.Map.RecalcFullExtent();

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
            int ppID = 0;//（polygonobject自己的编号，应该无用）
            List<TriNode> trilist = new List<TriNode>();
            //Polygon的点集
            IPointCollection pointSet = pPolygon as IPointCollection;
            int count = pointSet.PointCount;
            double curX;
            double curY;
            //ArcGIS中，多边形的首尾点重复存储
            for (int i = 0; i < count-1; i++)
            {
                curX = pointSet.get_Point(i).X;
                curY = pointSet.get_Point(i).Y;
                //初始化每个点对象
                TriNode tPoint = new TriNode(curX, curY, ppID, 1);
                trilist.Add(tPoint);
            }
            //生成自己写的多边形
            PolygonObject mPolygonObject = new PolygonObject(ppID, trilist);

            return mPolygonObject;
        }

        /// <summary>
        /// 判断连接的线是否划破多边形
        /// </summary>
        /// <param name="Curpo"></param>
        /// <param name="NodeList"></param>
        /// <returns></returns>
        public bool GetIntersect(PolygonObject Curpo, List<TriNode> NodeList)
        {
            bool IntersectLable = false;

            #region 只有一条连接边
            if (NodeList.Count == 2)
            {
                IPoint Point1 = new PointClass(); Point1.X = NodeList[0].X; Point1.Y = NodeList[0].Y;
                IPoint Point2 = new PointClass(); Point2.X = NodeList[1].X; Point2.Y = NodeList[1].Y;
                ILine CacheLine = new LineClass(); CacheLine.FromPoint = Point1; CacheLine.ToPoint = Point2;

                IPolygon CachePolygon = this.PolygonObjectConvert(Curpo);
                ITopologicalOperator iTo=CachePolygon as ITopologicalOperator;
                IGeometry iGeo = iTo.Intersect(CacheLine,esriGeometryDimension.esriGeometry0Dimension);
                IPointCollection iPc = iGeo as IPointCollection;

                if (iPc.PointCount > 2)
                {
                    IntersectLable = true;
                }
            }
            #endregion

            #region 有两条连接边
            if (NodeList.Count == 3)
            {
                IPoint Point1 = new PointClass(); Point1.X = NodeList[0].X; Point1.Y = NodeList[0].Y;
                IPoint Point2 = new PointClass(); Point2.X = NodeList[1].X; Point2.Y = NodeList[1].Y;
                IPoint Point3 = new PointClass(); Point3.X = NodeList[2].X; Point3.Y = NodeList[2].Y;
                ILine CacheLine1 = new LineClass(); CacheLine1.FromPoint = Point1; CacheLine1.ToPoint = Point2;
                ILine CacheLine2 = new LineClass(); CacheLine2.FromPoint = Point2; CacheLine2.ToPoint = Point3;

                IPolygon CachePolygon = this.PolygonObjectConvert(Curpo);
                ITopologicalOperator iTo = CachePolygon as ITopologicalOperator;
                IGeometry iGeo1 = iTo.Intersect(CacheLine1, esriGeometryDimension.esriGeometry0Dimension);
                IPointCollection iPc1 = iGeo1 as IPointCollection;
                IGeometry iGeo2 = iTo.Intersect(CacheLine2, esriGeometryDimension.esriGeometry0Dimension);
                IPointCollection iPc2 = iGeo1 as IPointCollection;

                if (iPc1.PointCount > 1)
                {
                    IntersectLable = true;
                }

                if (iPc2.PointCount > 1)
                {
                    IntersectLable = true;
                }
            }
            #endregion

            return IntersectLable;
        }

        /// <summary>
        /// 返回当前处理面积的变化率
        /// </summary>
        /// <param name="InitialPo"></param>
        /// <param name="CurPo"></param>
        /// <param name="NodeList"></param>
        /// <param name="AreaType"></param>1=面积增加；2=面积减少
        /// <returns></returns>
        public double GetAreaChange(PolygonObject InitialPo, PolygonObject CurPo)
        {
            double AreaCur = CurPo.Area;
            double AreaInitial = InitialPo.Area;
            double ChangeRate = Math.Abs(AreaInitial - AreaCur) / AreaInitial;

            return ChangeRate;
        }

        /// <summary>
        /// 返回当前处理方向的变化率
        /// </summary>
        /// <param name="InitialPo"></param>
        /// <param name="CurPo"></param>
        /// <returns></returns>
        public double GetOrientationChange(PolygonObject InitialPo, PolygonObject CurPo)
        {
            double OrientationChange = 0;

            IPolygon CacheInitialPo = PolygonObjectConvert(InitialPo);
            IPolygon CacheCurPo = PolygonObjectConvert(CurPo);

            PrDispalce.工具类.ParameterCompute PC = new 工具类.ParameterCompute();
            double Ori1 = PC.GetSMBROrientation(CacheInitialPo);
            double Ori2 = PC.GetSMBROrientation(CacheCurPo);

            OrientationChange = Math.Abs(Ori1 - Ori2);
            if (OrientationChange > 90)
            {
                OrientationChange = 180 - OrientationChange;
            }

            return OrientationChange;
        }
     }

}
