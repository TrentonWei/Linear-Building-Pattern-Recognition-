using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Display;
//using ESRI.ArcGIS.DataManagementTools;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Geometry;
using PrDispalce.地图要素;

namespace PrDispalce.工具类
{
     class Displacement
     {

        #region 运用点群聚类移位
        public void MoveSingleFeature(ref IFeature pFeature, double dx, double dy)//直接在原数据的基础上移位
        {
            IPolygon pPolygon = pFeature.Shape as IPolygon;
            ITransform2D pPolygonTrans = (ITransform2D)pPolygon;
            pPolygonTrans.Move(dx, dy);
            pFeature.Shape = pPolygon as IGeometry;
            pFeature.Store();
        }

        public PolygonObject MoveSingleFeatures(PolygonObject mPolygon,double dx, double dy)//重新生成移位后的数据，保证原数据不被破坏
        {
                    int potofpolyCount = mPolygon.PointList.Count;
                    List<TriDot> pointObtlst = new List<TriDot>();
                    for(int s = 0; s < potofpolyCount;s++ )
                    {
                        TriDot newTridot = new TriDot((mPolygon.PointList[s].X + dx), (mPolygon.PointList[s].Y + dy));
                        pointObtlst.Add(newTridot);
                    }
                    int polyId = mPolygon.ID;
                    PolygonObject newPolyObt = new PolygonObject(polyId,pointObtlst);
                    newPolyObt.Label = mPolygon.Label;

                    newPolyObt.Level = mPolygon.Level;
                    newPolyObt.ConflictIDs = mPolygon.ConflictIDs;

                    return newPolyObt;
        }
        public PolygonObject MoveSingleFeatures1(PolygonObject mPolygon, double dx, double dy)//重新生成移位后的数据，保证原数据不被破坏
        {
            int potofpolyCount = mPolygon.PointList.Count;
            List<TriDot> pointObtlst = new List<TriDot>();
            for (int s = 0; s < potofpolyCount; s++)
            {
                TriDot newTridot = new TriDot((mPolygon.PointList[s].X + dx), (mPolygon.PointList[s].Y + dy));
                pointObtlst.Add(newTridot);
            }
            int polyId = mPolygon.ID;
            PolygonObject newPolyObt = new PolygonObject(polyId, pointObtlst);
           // newPolyObt.Label = mPolygon.Label;

            newPolyObt.CID = mPolygon.CID;
            newPolyObt.Level = mPolygon.Level;
            newPolyObt.Type = mPolygon.Type;
            return newPolyObt;
        }
        #endregion
    }
}
