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
    class BasicStruct
    {
        public int Id = -1;//每一个结构对应的编号
        public int Type = -1;//表示博士论文中结构划分：每一个结构对应的类型1=非结构；2=凸部；3凹部；4；阶梯型
        //表示投稿论文中结构划分：每一个结构对应一种类型1=转折；2=凸部；3=凹部；4=阶梯型；5=corner
        public List<TriNode> NodeList = null;//结构中包含的节点
        public List<PolylineObject> PolylineList = null;//结构中包含的边

        public List<double> OrthAngle = new List<double>();//表示结构中每一个点对应的角度 

        /// <summary>
        /// 构造函数
        /// </summary>
        public BasicStruct(int Id,int Type,List<TriNode> NodeList,List<PolylineObject> PolylineList)
        {
            this.Id = Id;
            this.Type = Type;
            this.NodeList = NodeList;
            this.PolylineList = PolylineList;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public BasicStruct(int Id, int Type, List<TriNode> NodeList, List<PolylineObject> PolylineList,List<double> NodeAngle)
        {
            this.Id = Id;
            this.Type = Type;
            this.NodeList = NodeList;
            this.PolylineList = PolylineList;
            this.OrthAngle = NodeAngle;
        }
    }
}
