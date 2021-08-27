using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;
using PrDispalce.工具类;
using PrDispalce.地图要素;

namespace PrDispalce
{
    public partial class MainFrm : Form
    {
        public MainFrm()
        {
            InitializeComponent();
        }

        #region 参数(图层参数)
        ILayer pLayer;
        #endregion

        #region 移除选中图层
        private void 移除图层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (axMapControl1.Map.LayerCount > 0)
                {
                    if (pLayer != null)
                    {
                        axMapControl1.Map.DeleteLayer(pLayer);
                    }
                }
            }

            catch
            {
                MessageBox.Show("移除失败");
                return;
            }
        }
        #endregion

        #region 建筑物合并测试
        private void 建筑物合并ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
            ////MultipleLevelDisplace Md = new MultipleLevelDisplace();

            //List<IFeatureLayer> layerList = new List<IFeatureLayer>();
            //IFeatureLayer PolygonFeatureLayer = pFeatureHandle.GetLayer(this.axMapControl1.Map, "D1B");
            //layerList.Add(PolygonFeatureLayer);

            //PrDispalce.地图要素.MapReady MR = new 地图要素.MapReady(layerList);
            //MR.Ready();
            //PrDispalce.地图要素.PolygonLayer polygondata = MR.PPLayer; 
            //List<PolygonObject> mpolyList = new List<PolygonObject>(); 
            //mpolyList = polygondata.PolygonList;

            //PolygonObject tPolygonObject = Md.BuildingAggreation(mpolyList[28], mpolyList[30], this.axMapControl1.Map, "合并16",true);
            //List<PolygonObject> tPolygonList = new List<PolygonObject>();
            //tPolygonList.Add(tPolygonObject);
            //SaveNewObjects.SavePolygons(tPolygonList, this.axMapControl1.Map.SpatialReference, @"C:\Users\Administrator\Desktop\协同移位实验\4.18多层次移位\建筑物合并", "test");
        }
        #endregion


        #region 点击移除图层
        private void axTOCControl1_OnMouseDown_1(object sender, ITOCControlEvents_OnMouseDownEvent e)
        {
            if (axMapControl1.LayerCount > 0)
            {
                esriTOCControlItem pItem = new esriTOCControlItem();
                //pLayer = new FeatureLayerClass();
                IBasicMap pBasicMap = new MapClass();
                object pOther = new object();
                object pIndex = new object();
                // Returns the item in the TOCControl at the specified coordinates.
                axTOCControl1.HitTest(e.x, e.y, ref pItem, ref pBasicMap, ref pLayer, ref pOther, ref pIndex);
            }

            if (e.button == 2)
            {
                this.contextMenuStrip1.Show(axTOCControl1, e.x, e.y);
            }
        }
        #endregion

        #region 相等测试
        private void 相等测试ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            //MemoryStream ms = new MemoryStream(); //无法序列化
            //System.Xml.Serialization.XmlSerializer xml = new System.Xml.Serialization.XmlSerializer(typeof(IPoint));
            //xml.Serialize(ms, pPoint1);
            //ms.Seek(0, SeekOrigin.Begin);
            //IPoint pPoint2 = xml.Deserialize(ms) as IPoint;
            //ms.Close();
            //list_b.Add(pPoint2);

            //list_a[0].Y = 0.2;
        }
        #endregion

        public static T DeepCopy<T>(T obj)
        {
            object retval;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                //序列化成流
                bf.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                //反序列化成对象
                retval = bf.Deserialize(ms);
                ms.Close();
            }
            return (T)retval;
        }

        #region 受限变形
        private void 受限变形ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        #endregion

        #region 克隆测试
        private void 克隆测试ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PolygonObject A = new PolygonObject();
            A.DisplacementLabel = false;
            PolygonObject B = new PolygonObject();
            B.DisplacementLabel = A.DisplacementLabel;
            A.DisplacementLabel = true;
        }
        #endregion

        #region 建筑物道路聚合测试
        private void 建筑物道路聚合测试ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //PrDispalce.工具窗体.BuildingRoadAggregationForm pBuildingRoadAggregationFrm = new 工具窗体.BuildingRoadAggregationForm(this.axMapControl1);
            //pBuildingRoadAggregationFrm.Show();
        }
        #endregion

        #region 协同移位（整体）-邻近图建立（MST+RNG）
        private void 邻近图建立ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.协同移位_整体_.ProximityGraphBuilder ProximityGraphBuilderfrm = new 协同移位_整体_.ProximityGraphBuilder(this.axMapControl1.Map);
            ProximityGraphBuilderfrm.Show();
        }
        #endregion

        #region 直线模式识别
        private void 直线模式ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.模式识别.LinearPattern LinearPatterfrm = new 模式识别.LinearPattern(this.axMapControl1.Map);
            LinearPatterfrm.Show();
        }
        #endregion


        #region 基于剖分的模式识别
        private void 基于剖分的模式识别ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.PatternRecognition.PRForm prf = new PatternRecognition.PRForm(this.axMapControl1);
            prf.Show();
        }
        #endregion

        #region 建筑物相似度计算
        private void 建筑物相似度计算ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.BuildingSim.BuildingSimFrm bsf = new BuildingSim.BuildingSimFrm(this.axMapControl1);
            bsf.Show();
        }
        #endregion

        private void 协同移位整体ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.协同移位_整体_.ProximityGraphBuilder ProximityGraphBuilderfrm = new 协同移位_整体_.ProximityGraphBuilder(this.axMapControl1.Map);
            ProximityGraphBuilderfrm.Show();
        }

        /// <summary>
        /// 建筑物图形剖分
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 图形剖分ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.PatternRecognition.PRForm prf = new PatternRecognition.PRForm(this.axMapControl1);
            prf.Show();
        }

        /// <summary>
        /// 建筑物空间特征计算
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 建筑物特征计算ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 建筑物相似性计算
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 建筑物相似性计算ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.BuildingSim.BuildingSimFrm bsf = new BuildingSim.BuildingSimFrm(this.axMapControl1);
            bsf.Show();
        }

        /// <summary>
        /// 直线模式识别
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 直线模式识别ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.模式识别.LinearPattern LinearPatterfrm = new 模式识别.LinearPattern(this.axMapControl1.Map);
            LinearPatterfrm.Show();
        }
    }
}
