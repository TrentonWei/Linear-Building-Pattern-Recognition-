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
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        #region 参数(图层参数)
        ILayer pLayer;
        #endregion

        #region 建筑物邻近图生成
        private void 建筑物群邻近图生成ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.工具窗体.ProximityCreate ProximtyCreatefrm = new 工具窗体.ProximityCreate(this.axMapControl1.Map);
            ProximtyCreatefrm.Show();
        }
        #endregion

        #region 重心邻近图生成
        private void 重心邻近图生成ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.工具窗体.VGraphfrm vGraphfrm = new 工具窗体.VGraphfrm(this.axMapControl1.Map);
            vGraphfrm.Show();
        }
        #endregion

        #region 冲突探测
        private void 冲突探测ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.工具窗体.ConflictDetect ConflictDetectfrm = new 工具窗体.ConflictDetect(this.axMapControl1.Map);
            ConflictDetectfrm.Show();
        }
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

        #region MST生成
        private void mST生成ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.工具窗体.MSTCreate MstCreateFrm = new 工具窗体.MSTCreate(this.axMapControl1.Map, this.axMapControl1);
            MstCreateFrm.Show();
        }
        #endregion

        #region 建立节点与街道和建筑物的关系
        private void 建立节点与街道和建筑物的关系ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.工具窗体.MSTtoPP MSTtoPPfrm = new 工具窗体.MSTtoPP(this.axMapControl1.Map);
            MSTtoPPfrm.Show();
        }
        #endregion

        #region 原始比例射线移位
        private void 原始比例射线移位ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.工具窗体.pDisplace pDisplacefrm = new 工具窗体.pDisplace(this.axMapControl1.Map);
            pDisplacefrm.Show();
        }
        #endregion

        #region 线线邻近关系探测
        private void 线线邻近关系探测ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.工具窗体.ProximityDetect proximitydetectfrm = new 工具窗体.ProximityDetect(this.axMapControl1.Map);
            proximitydetectfrm.Show();
        }
        #endregion

        #region 基于建筑物邻近图冲突探测
        private void 基于建筑物邻近图冲突探测ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.工具窗体.ConflictDetect2 pConflictDetect2frm = new 工具窗体.ConflictDetect2(this.axMapControl1.Map,this.axMapControl1);
            pConflictDetect2frm.Show();
        }
        #endregion

        #region 经典比例射线移位（顾及建筑物的面积）
        private void 经典比例射线移位ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.工具窗体.pDisplaceTraditional pDisplaceTraditionalfrm = new 工具窗体.pDisplaceTraditional(this.axMapControl1.Map);
            pDisplaceTraditionalfrm.Show();
        }
        #endregion

        #region 一条或两条道路冲突建筑物移动
        private void 与一条道路冲突建筑物移动ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.工具窗体.FeiBuildingDisplaceForm pFeiBuildingDisplacefrm = new 工具窗体.FeiBuildingDisplaceForm(this.axMapControl1.Map);
            pFeiBuildingDisplacefrm.Show();
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

        #region 建筑物合并测试
        private void 建筑物合并ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            PrDispalce.工具窗体.PolygonAggregation PolygonAggragationFrm = new 工具窗体.PolygonAggregation(this.axMapControl1.Map);
            PolygonAggragationFrm.Show();
        }
        #endregion

        #region 5.8协同移位框架构建
        private void 协同移位框架ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //PrDispalce.工具窗体.MultipleLavelDisplace MultipleLevelDisplaceFrm = new 工具窗体.MultipleLavelDisplace(this.axMapControl1.Map);
            //MultipleLevelDisplaceFrm.Show();
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

        #region 协同移位（整体）-邻近关系建立（V图+邻近图）
        private void 邻近关系建立ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.协同移位_整体_.ProximityBuilder ProximityBuilderfrm = new 协同移位_整体_.ProximityBuilder(this.axMapControl1.Map);
            ProximityBuilderfrm.Show();
        }
        #endregion

        #region 协同移位（整体）-邻近图建立（MST+RNG）
        private void 邻近图建立ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.协同移位_整体_.ProximityGraphBuilder ProximityGraphBuilderfrm = new 协同移位_整体_.ProximityGraphBuilder(this.axMapControl1.Map);
            ProximityGraphBuilderfrm.Show();
        }
        #endregion

        #region  协同移位（整体）-基于场的受力计算
        private void 基于场的受力计算ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.协同移位_整体_.ForceComputation pForceComputation = new 协同移位_整体_.ForceComputation(this.axMapControl1.Map);
            pForceComputation.Show();
        }
        #endregion

        #region 多力源场及次生冲突解决 
        private void 协同移位ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            PrDispalce.协同移位_整体_.CollaborativeDisplacement cDis = new 协同移位_整体_.CollaborativeDisplacement(this.axMapControl1.Map);
            cDis.Show();
        }
        #endregion

        #region
        private void 协同移位ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            PrDispalce.协同移位_整体_.WholeCollaborativeDisplacement wCDis = new 协同移位_整体_.WholeCollaborativeDisplacement(this.axMapControl1.Map);
            wCDis.Show();
        }
        #endregion

        #region 分步协同移位
        private void 分步协同移位ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.协同移位_整体_.StepCollabrativeDisplacement scDis = new 协同移位_整体_.StepCollabrativeDisplacement(this.axMapControl1.Map);
            scDis.Show();
        }
        #endregion

        #region 直线模式识别
        private void 直线模式ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.模式识别.LinearPattern LinearPatterfrm = new 模式识别.LinearPattern(this.axMapControl1.Map);
            LinearPatterfrm.Show();
        }
        #endregion

        #region mesh simplify
        private void meshSimplifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.工具窗体.MeshSimplify meshfrm = new 工具窗体.MeshSimplify(this.axMapControl1.Map,this.axMapControl1);
            meshfrm.Show();
        }
        #endregion

        #region 不相似建筑物典型化
        private void nonSimilarMeshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.典型化.ArbitrarilyMesh AM = new 典型化.ArbitrarilyMesh(this.axMapControl1.Map, this.axMapControl1);
            AM.Show();
        }
        #endregion

        #region 提取单个建筑物
        private void 提取单个建筑物ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.建筑物群空间特征分析小工具.BuildingGet bgFrm = new 建筑物群空间特征分析小工具.BuildingGet(this.axMapControl1.Map);
            bgFrm.Show();
        }
        #endregion

        #region 提取综合算子
        private void 综合算子提取ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.建筑物群空间特征分析小工具.GeneralizationOperatorInfer BoIFrm = new 建筑物群空间特征分析小工具.GeneralizationOperatorInfer(this.axMapControl1.Map);
            BoIFrm.Show();
        }
        #endregion

        #region GroupMesh
        private void groupMeshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.典型化.GroupMesh GMFrm = new 典型化.GroupMesh(this.axMapControl1.Map,this.axMapControl1);
            GMFrm.Show();
        }
        #endregion

        #region 建筑物聚合
        private void 建筑物聚合ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.建筑物聚合.ProgressiveAggregation PAFrm = new 建筑物聚合.ProgressiveAggregation(this.axMapControl1.Map, this.axMapControl1);
            PAFrm.Show();
        }
        #endregion

        #region 群组空间特征计算
        private void 群组空间特征计算ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.建筑物群空间特征分析小工具.BuildingFeatureComputation BFC = new 建筑物群空间特征分析小工具.BuildingFeatureComputation(this.axMapControl1);
            BFC.Show();
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

        #region FlowMapInitial
        private void flowMapInitialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.FlowMap.FlowMap FM = new FlowMap.FlowMap(this.axMapControl1); 
            FM.Show();
        }
        #endregion

        /// <summary>
        /// 重心生成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 重心生成ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            工具窗体.CenterGet CGF = new 工具窗体.CenterGet(this.axMapControl1);
            CGF.Show();
        }
    }
}
