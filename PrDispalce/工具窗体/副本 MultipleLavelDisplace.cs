using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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
using PrDispalce.地图要素;
using PrDispalce.工具类;

namespace PrDispalce.工具窗体
{
    public partial class MultipleLavelDisplace : Form
    {
        public MultipleLavelDisplace(IMap cMap)
        {
            InitializeComponent();
            this.pMap = cMap;
        }

        #region 参数
        IMap pMap;
        PrDispalce.工具类.FeatureHandle pFeatureHandle = new 工具类.FeatureHandle();
        string localFilePath, fileNameExt, FilePath;
        #endregion

        #region 初始化
        private void MultipleLavelDisplace_Load(object sender, EventArgs e)
        {
            if (this.pMap.LayerCount <= 0)
                return;

            ILayer pLayer;
            string strLayerName;
            for (int i = 0; i < this.pMap.LayerCount; i++)
            {
                pLayer = this.pMap.get_Layer(i);
                strLayerName = pLayer.Name;

                IDataset LayerDataset = pLayer as IDataset;

                if (LayerDataset != null)
                {
                    IFeatureLayer pFeatureLayer = pLayer as IFeatureLayer;
                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                    {
                        this.comboBox2.Items.Add(strLayerName);
                    }

                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        this.comboBox1.Items.Add(strLayerName);
                    }
                }
            }

            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }

            if (this.comboBox2.Items.Count > 0)
            {
                this.comboBox2.SelectedIndex = 0;
            }
        }
        #endregion

        #region 多层次移位
        private void button2_Click(object sender, EventArgs e)
        {
            GeoCalculation Gc = new GeoCalculation();
            Displacement Dis = new Displacement();
            MultipleLevelDisplace Md = new MultipleLevelDisplace();

            List<PolygonCluster> list_a = new List<PolygonCluster>();//道路与建筑物冲突探测
            List<PolygonCluster> list_b = new List<PolygonCluster>();//道路与建筑物冲突探测备份
           

            #region 读取数据
            List<IFeatureLayer> layerList = new List<IFeatureLayer>();
            IFeatureLayer PolygonFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text.ToString());
            IFeatureLayer PolylineFeatureLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text.ToString());
            layerList.Add(PolygonFeatureLayer); layerList.Add(PolylineFeatureLayer);
            PrDispalce.地图要素.MapReady MR = new 地图要素.MapReady(layerList);
            MR.Ready();
            #endregion

            #region 对建筑物群进行聚类
            PrDispalce.地图要素.PolygonLayer polygondata = MR.PPLayer;
            PolylineLayer polylinedata = MR.PLLayer;//从地图中获得的线数据图层
            List<PolygonObject> mpolyList = new List<PolygonObject>(); List<PolylineObject> mpolineList = new List<PolylineObject>();
            mpolyList = polygondata.PolygonList;//从地图上获得的面目标list
            mpolineList = polylinedata.PolylineList;//从地图上获得的线目标list
            List<PolygonCluster> mCluster = PolygonCluster.startAnalysis(mpolyList, double.Parse(textBox1.Text));//建筑物聚类（最后一个参数是聚类约束）          
            #endregion

            list_a = Gc.buildingsAndRoadsConflict(mCluster, mpolineList, double.Parse(textBox2.Text));//探测类群与建筑物的冲突（最后一个参数是冲突约束）[建筑物与街道冲突]
            list_b = list_a;//对建筑物群进行备份

            #region 协同移位过程
            //操作对象是每一个PolygonCluster
            for (int i = 0; i < list_a.Count; i++)
            {
                //list_a = Gc.buildingsAndRoadsConflict(list_a, mpolineList, double.Parse(textBox1.Text));
                //list_b = list_a;
                
                bool Label=true; //标识是否进行了协同操作
                bool Label1 = false;//标识是否进行了移位，移位则进行移位结果评价（false表示没有移位，true表示有移位）；也可以标识是否存在冲突。

                #region 第一层建筑物与道路移位结果评价；并循环解决第一层建筑物与道路的冲突
                while(Label)
                {
                    list_a[i] = Md.ConflictDetectRoadBuildingforCluster(list_a[i], mpolineList, double.Parse(textBox2.Text));
                    list_b[i] = list_a[i];

                    #region 解决第一层建筑物与道路的冲突(1、首先判断群中建筑物是否存在与道路冲突的建筑物 2、对冲突建筑物进行移位)
                    for (int j = 0; j < list_a[i].polygonList.Count; j++)
                    {
                        PolygonObject mPolygonObject = list_a[i].polygonList[j];
                        if (mPolygonObject.ConflictIDs.Count > 0)
                        {
                            //if (mPolygonObject.ConflictIDs.Count > 2)//表示与多条建筑物冲突
                            //{                               
                            //    list_a[i] = list_b[i];//首先将建筑物还原
                            //                          //其次，受限变形
                            //    Label = true;
                            //    Label1 = false;
                            //    break;
                            //}

                            if (mPolygonObject.ConflictIDs.Count < 3)//表示与两条或以下建筑物冲突
                            {
                                PolygonObject pPolygonObject = Md.MoveBuildingConflictWithRoad(mPolygonObject, mpolineList, double.Parse(this.textBox2.Text) * 1.05);//建筑物群移位。实际上1就足够，但是有时会出现5.99<6这种情况，故乘1.05
                                list_a[i].polygonList[j] = pPolygonObject;
                                Label1 = true;//标识是否进行移位
                            }
                        }
                    }
                    #endregion

                    Label = false;

                    if (Label1)//如果进行了移位则评价
                    {
                        #region 1、判断上一层冲突是否解决
                        for (int j = 0; j < list_a[i].polygonList.Count; j++)
                        {
                            PolygonObject mPolygonObject = list_a[i].polygonList[j];
                            if (mPolygonObject.Level == 1)
                            {
                                for (int k = 0; k < mpolineList.Count; k++)
                                {
                                    PolylineObject mPolylinePbject = mpolineList[k];
                                    double buildtoRoadDis = mPolygonObject.GetMiniDistance(mPolylinePbject);

                                    if (buildtoRoadDis < double.Parse(this.textBox2.Text)) //若冲突未解决(即无论如何也解决不了与道路的冲突，应受限变形或删除或贴合)
                                    {
                                        list_a[i] = list_b[i];//将该团建筑物还原
                                        list_a[i].polygonList.RemoveAt(j);//将冲突建筑物删除                      

                                        //实际上，需要对该建筑物做受限变形，删除或者是贴合处理
                                        Label = true;
                                        break;
                                    }
                                }
                            }

                            //每一次只处理一个或两个建筑物，因此，在处理后，若Label，则退出判断过程，返回重新判断
                            if (Label)
                            {
                                break;
                            }
                        }
                        #endregion

                        #region 2、判断是否存在同一层次的冲突
                        if (!Label)//若进行了协同操作，则不进行此操作
                        {
                            for (int j = 0; j < list_a[i].polygonList.Count; j++)
                            {
                                PolygonObject mPolygonObject = list_a[i].polygonList[j];
                                //List<PolygonObject> cPolygonList = new List<PolygonObject>();
                                //cPolygonList.Add(mPolygonObject);
                                if (mPolygonObject.Level == 1)
                                {
                                    for (int k = 0; k < list_a[i].polygonList.Count; k++)
                                    {
                                        if (k != j)
                                        {
                                            PolygonObject nPolygonObject = list_a[i].polygonList[k];
                                            if (nPolygonObject.Level == 1)
                                            {                                               
                                                List<PolygonObject> mpolygonList = new List<PolygonObject>(); 
                                                PolygonCluster cPolygonCluster = new PolygonCluster(mpolygonList,0);
                                                //cPolygonCluster.polygonList.Clear();
                                                cPolygonCluster.polygonList.Add(mPolygonObject); cPolygonCluster.polygonList.Add(nPolygonObject);
                                                double BuildingsDis = Gc.MinDistance(cPolygonCluster.polygonList);

                                                if (BuildingsDis < double.Parse(this.textBox3.Text))
                                                {
                                                    list_a[i] = list_b[i];//将该团建筑物还原
                                                    Label = true;

                                                    #region 判断是对建筑物做删除操作还是合并操作
                                                    double Area1 = mPolygonObject.GetArea();
                                                    double Area2 = nPolygonObject.GetArea();

                                                    if (Area1 > Area2)
                                                    {
                                                        if (Area1 / Area2 > 3)//如果面积大于两倍，删除面积小的
                                                        {
                                                            list_a[i].polygonList.RemoveAt(k);
                                                        }

                                                        else //对两建筑物进行合并
                                                        {
                                                            PolygonObject tPolygonObject = Md.BuildingAggreation(mPolygonObject, nPolygonObject, pMap, i.ToString() + j.ToString() + k.ToString());
                                                            tPolygonObject.Level = 1;
                                                            list_a[i].polygonList[j] = tPolygonObject;
                                                            list_a[i].polygonList.RemoveAt(k);
                                                        }
                                                    }

                                                    else
                                                    {
                                                        if (Area2 / Area1 > 3)//如果面积大于两倍，删除面积小的
                                                        {
                                                            list_a[i].polygonList.RemoveAt(j);
                                                        }

                                                        else//对两建筑物合并
                                                        {
                                                            PolygonObject tPolygonObject = Md.BuildingAggreation(mPolygonObject, nPolygonObject, pMap, i.ToString() + j.ToString() + k.ToString());
                                                            tPolygonObject.Level = 1;
                                                            list_a[i].polygonList[j] = tPolygonObject;
                                                            list_a[i].polygonList.RemoveAt(k);
                                                        }
                                                    }
                                                    #endregion

                                                    //实际上，需要对该建筑物做受限变形或者是删除或者是贴合或者是合并或者是典型化
                                                    break;//判断有同一层次的建筑物冲突就跳出
                                                }
                                            }
                                        }
                                    }
                                }

                                if (Label)//每一次只处理一个或两个建筑物，因此，在处理后，若Label，则退出判断过程，返回重新判断
                                {
                                    break;
                                }
                            }
                        } 
                        #endregion
                    }
                   
                    #region 3、判断每个建筑物是否满足移位条件(位置精度)
                    //if (Label != true)
                    //{
                    //    for (int j = 0; j < list_a[i].polygonList.Count; j++)
                    //    {
                    //        PolygonObject mPolygonObject = list_a[i].polygonList[j];
                    //        List<PolygonObject> cPolygonList = new List<PolygonObject>();
                    //        cPolygonList.Add(mPolygonObject);
                    //        if (mPolygonObject.Level == 1)
                    //        {
                    //            PolygonObject nPolygonObject = list_a[i].polygonList[j];
                    //            cPolygonList.Add(nPolygonObject);

                    //            double BuildingsDis = Gc.MinDistance(cPolygonList);
                    //            if (BuildingsDis < double.Parse(this.textBox4.Text))
                    //            {
                    //                PolygonCluster TwoPolygonCluster = Md.FindClosestbuildings(list_a[i]); //如果超出位置精度，返回找到两个距离最近的建筑物，对其进行处理

                    //                Label = true;
                    //                break;
                    //            }
                    //        }

                    //        if (Label)//每一次只处理一个或两个建筑物，因此，在处理后，若Label，则退出判断过程，返回重新判断
                    //        {
                    //            break;
                    //        }
                    //    }
                    //}
                    #endregion

                    #region 4、次生冲突探测
                    if (!Label)//如果没有进行协同操作，即当前层冲突即通过移位解决；则判断次生冲突
                    {
                        PolygonCluster nPolygonCluster = Md.buildingsAndBuildingsConflict(list_a[i], double.Parse(this.textBox3.Text), 1, out Label1);//次生冲突探测
                        list_a[i] = nPolygonCluster;
                    }
                    #endregion

                } //while (Label);//当建筑物进行了移位且进行协同操作时，则进行冲突的判断
                #endregion

                #region 若解决第一层建筑物冲突后产生次生冲突，就循环解决建筑物内冲突
                int Level = 1;

                while (Label1 || Label)//若还存在次生冲突且未进行协同操作
                {
                    if (Label)//如果进行了协同移位操作，则重新判断冲突
                    {
                        PolygonCluster nPolygonCluster = Md.buildingsAndBuildingsConflict(list_a[i], double.Parse(this.textBox3.Text), Level, out Label1);//次生冲突探测
                        list_a[i] = nPolygonCluster;
                        list_b[i] = list_a[i];
                    }

                    #region 解决建筑物内次生冲突（首先判断是否存在次生冲突；其次通过移位解决次生冲突
                    for (int j = 0; j < list_a[i].polygonList.Count; j++)
                    {
                        PolygonObject mPolygonObject = list_a[i].polygonList[j];
                        //获取到第一个需要处理的建筑物，则直接处理

                        if (mPolygonObject.Level == Level + 1)//如果是次生冲突建筑物
                        {
                            Label1 = true;//进行了移位
                            double Dx = 0; double Dy = 0;
                            for (int k = 0; k < mPolygonObject.bbConflictIDs.Count; k++)
                            {
                                PolygonObject kPolygonObject = list_a[i].polygonList[mPolygonObject.bbConflictIDs[k]];
                                List<double> sForceList = Md.TwoBuildingsForceCompute(mPolygonObject, kPolygonObject, double.Parse(this.textBox3.Text));
                                Dx = Dx + sForceList[0]; Dy = Dy + sForceList[1];
                            }

                            PolygonObject mPolygonObject1 = Dis.MoveSingleFeatures1(mPolygonObject, Dx*1.05, Dy*1.05);//按照每个多边形的移位位置生成一个多边形(实际上1就足够，但是有时会出现5.99<6这种情况，故乘1.05)
                            mPolygonObject1.CID = mPolygonObject.CID;
                            mPolygonObject1.Level = mPolygonObject.Level;
                            list_a[i].polygonList[j] = mPolygonObject1;
                        }
                    }
                    #endregion

                    Level = Level + 1;                     
                    Label = false;

                    if (Label1)
                    {
                        #region 1、上一层次冲突是否解决
                        for (int j = 0; j < list_a[i].polygonList.Count; j++)
                        {
                            PolygonObject mPolygonObject = list_a[i].polygonList[j];

                            if (mPolygonObject.Level == Level)
                            {
                                for (int k = 0; k < mPolygonObject.bbConflictIDs.Count; k++)
                                {
                                    List<PolygonObject> mpolygonList = new List<PolygonObject>();
                                    PolygonCluster cPolygonCluster = new PolygonCluster(mpolygonList, 0);
                                    PolygonObject nPolygonObject = list_a[i].polygonList[k];
                                    cPolygonCluster.polygonList.Add(mPolygonObject); cPolygonCluster.polygonList.Add(list_a[i].polygonList[k]);

                                    double BuildingsDis = Gc.MinDistance(cPolygonCluster.polygonList);

                                    if (BuildingsDis < double.Parse(this.textBox3.Text))
                                    {
                                        list_a[i] = list_b[i];//将该团建筑物还原
                                        Label = true;
                                        Level = Level - 1;

                                        #region 判断是对建筑物做删除操作还是合并操作
                                        double Area1 = mPolygonObject.GetArea();
                                        double Area2 = nPolygonObject.GetArea();

                                        if (Area1 > Area2)
                                        {
                                            if (Area1 / Area2 > 3)//如果面积大于两倍，删除面积小的
                                            {
                                                list_a[i].polygonList.RemoveAt(k);
                                            }

                                            else //对两建筑物进行合并
                                            {
                                                PolygonObject tPolygonObject = Md.BuildingAggreation(mPolygonObject, nPolygonObject, pMap, i.ToString() + j.ToString() + k.ToString());
                                                tPolygonObject.Level = Level;
                                                list_a[i].polygonList[j] = tPolygonObject;
                                                list_a[i].polygonList.RemoveAt(k);
                                            }
                                        }

                                        else
                                        {
                                            if (Area2 / Area1 > 3)//如果面积大于两倍，删除面积小的
                                            {
                                                list_a[i].polygonList.RemoveAt(j);
                                            }

                                            else//对两建筑物合并
                                            {
                                                PolygonObject tPolygonObject = Md.BuildingAggreation(mPolygonObject, nPolygonObject, pMap, i.ToString() + j.ToString() + k.ToString());
                                                tPolygonObject.Level = Level;
                                                list_a[i].polygonList[j] = tPolygonObject;
                                                list_a[i].polygonList.RemoveAt(k);
                                            }
                                        }
                                        #endregion

                                        //实际上，需要对该建筑物做受限变形或者是删除或者是贴合或者是合并或者是典型化
                                        break;//判断有同一层次的建筑物冲突就跳出
                                    }
                                }
                            }

                            if (Label)//每一次只处理一个或两个建筑物，因此，在处理后，若Label，则退出判断过程，返回重新判断
                            {
                                break;
                            }
                        }
                        #endregion

                        #region 2、是否产生同一层次的冲突
                        if (!Label)//若已进行协同操作，则不进行此操作
                        {
                            for (int j = 0; j < list_a[i].polygonList.Count; j++)
                            {
                                PolygonObject mPolygonObject = list_a[i].polygonList[j];

                                if (mPolygonObject.Level == Level)
                                {
                                    for (int k = 0; k < list_a[i].polygonList.Count; k++)
                                    {
                                        if (k != j)
                                        {
                                            PolygonObject nPolygonObject = list_a[i].polygonList[k];

                                            if (nPolygonObject.Level == Level)
                                            {
                                                List<PolygonObject> mpolygonList = new List<PolygonObject>();
                                                PolygonCluster cPolygonCluster = new PolygonCluster(mpolygonList, 0);
                                                cPolygonCluster.polygonList.Add(mPolygonObject); cPolygonCluster.polygonList.Add(nPolygonObject);
                                                double BuildingsDis = Gc.MinDistance(cPolygonCluster.polygonList);

                                                if (BuildingsDis < double.Parse(this.textBox3.Text))
                                                {
                                                    list_a[i] = list_b[i];//将该团建筑物还原
                                                    Label = true;
                                                    Level = Level - 1;

                                                    #region 判断是对建筑物做删除操作还是合并操作
                                                    double Area1 = mPolygonObject.GetArea();
                                                    double Area2 = nPolygonObject.GetArea();

                                                    if (Area1 > Area2)
                                                    {
                                                        if (Area1 / Area2 > 3)//如果面积大于两倍，删除面积小的
                                                        {
                                                            list_a[i].polygonList.RemoveAt(k);
                                                        }

                                                        else //对两建筑物进行合并
                                                        {
                                                            PolygonObject tPolygonObject = Md.BuildingAggreation(mPolygonObject, nPolygonObject, pMap, i.ToString() + j.ToString() + k.ToString());
                                                            tPolygonObject.Level = Level;
                                                            list_a[i].polygonList[j] = tPolygonObject;
                                                            list_a[i].polygonList.RemoveAt(k);
                                                        }
                                                    }

                                                    else
                                                    {
                                                        if (Area2 / Area1 > 3)//如果面积大于两倍，删除面积小的
                                                        {
                                                            list_a[i].polygonList.RemoveAt(j);
                                                        }

                                                        else//对两建筑物合并
                                                        {
                                                            PolygonObject tPolygonObject = Md.BuildingAggreation(mPolygonObject, nPolygonObject, pMap, i.ToString() + j.ToString() + k.ToString());
                                                            tPolygonObject.Level = Level;
                                                            list_a[i].polygonList[j] = tPolygonObject;
                                                            list_a[i].polygonList.RemoveAt(k);
                                                        }
                                                    }
                                                    #endregion

                                                    //实际上，需要对该建筑物做受限变形或者是删除或者是贴合或者是合并或者是典型化
                                                    break;//判断有同一层次的建筑物冲突就跳出
                                                }
                                            }
                                        }
                                    }
                                }

                                if (Label)//每一次只处理一个或两个建筑物，因此，在处理后，若Label，则退出判断过程，返回重新判断
                                {
                                    break;
                                }
                            }
                        }
                        #endregion
                    }

                    #region 3、是否满足移位条件（位置精度）
                    //if (Label != true)
                    //{
                    //    for (int j = 0; j < list_a[i].polygonList.Count; j++)
                    //    {
                    //        PolygonObject mPolygonObject = list_a[i].polygonList[j];
                    //        List<PolygonObject> cPolygonList = new List<PolygonObject>();
                    //        cPolygonList.Add(mPolygonObject);
                    //        if (mPolygonObject.Level == Level)
                    //        {
                    //            PolygonObject nPolygonObject = list_a[i].polygonList[j];
                    //            cPolygonList.Add(nPolygonObject);

                    //            double BuildingsDis = Gc.MinDistance(cPolygonList);
                    //            if (BuildingsDis < double.Parse(this.textBox4.Text))
                    //            {

                    //                list_a[i] = list_b[i];//首先对建筑物做还原
                    //                //如果超出位置精度，需要对建筑物如何处理
                    //                Label = true;
                    //                break;
                    //            }
                    //        }

                    //        if (Label)//每一次只处理一个或两个建筑物，因此，在处理后，若Label，则退出判断过程，返回重新判断
                    //        {
                    //            break;
                    //        }
                    //    }
                    //}
                    #endregion

                    #region 4、次生冲突探测
                    if (!Label)//如果没有有协同移位操作，则判断次生冲突
                    {
                        PolygonCluster aPolygonCluster = Md.buildingsAndBuildingsConflict(list_a[i], double.Parse(this.textBox3.Text), Level, out Label1);//次生冲突探测
                        list_a[i] = aPolygonCluster;
                    }
                    #endregion
                   
                } //若还存在冲突或者进行了而协同操作（存在冲突）
                #endregion
               
            }
            #endregion

            List<PolygonObject> list_e = Gc.cluster_PolygonList(list_a);
            SaveNewObjects.SavePolygons(list_e, pMap.SpatialReference, FilePath, fileNameExt);
        }
        #endregion 

        #region 输出路径
        private void button1_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = " shp files(*.shp)|";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //获得文件路径
                localFilePath = saveFileDialog1.FileName.ToString();

                //获取文件名，不带路径
                fileNameExt = localFilePath.Substring(localFilePath.LastIndexOf("\\") + 1);

                //获取文件路径，不带文件名
                FilePath = localFilePath.Substring(0, localFilePath.LastIndexOf("\\"));
            }

            this.comboBox3.Text = localFilePath;
        }
        #endregion
    }
}
