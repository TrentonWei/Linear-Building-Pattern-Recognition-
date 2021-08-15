using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesRaster;

namespace PrDispalce.FlowMap
{
    class QuadNode
    {
        public QuadNode()
        {
            this.Rectangle = new Envelope();
            this.Childs = new QuadNode[4];
        }

        public QuadNode(QuadNode other)
        {
            this.Rectangle = other.Rectangle;
            this.Childs = other.Childs;
        }
        public QuadNode(Envelope rect, QuadNode[] childs)
        {
            this.Rectangle = rect;
            this.Childs = childs;
        }
        /// <summary>
        /// 块的矩形
        /// </summary>
        public Envelope Rectangle { get; set; }
        /// <summary>
        /// 四个子节点
        /// </summary>
        public QuadNode[] Childs { get; set; }
        /// <summary>
        /// 块的编码
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 块的值
        /// </summary>
        public List<IPoint> Value { get; set; }
    }
}
