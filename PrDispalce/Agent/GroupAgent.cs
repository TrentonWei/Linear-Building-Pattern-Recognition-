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
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using AuxStructureLib;

namespace PrDispalce.Agent
{
    class GroupAgent
    {
        public int ID;
        public int GroupType;//团的类型（街心、街边、街角）
        public List<PolygonObject> PolygonObjectList;//团中建筑物
        public List<ConflictGroupAgent> ConflictGroupAgentList;//存放建筑物冲突团
        public List<blConflictAgent> blConflictAgentList;
    }
}
