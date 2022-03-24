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
using AuxStructureLib.IO;

//计算图形的邻近特征
namespace PrDispalce.BuildingSim
{
    class BuildingPairSim
    {
        //参数
        PublicUtil Pu = new PublicUtil();
        BuildingMeasures BM = new BuildingMeasures();
        AxMapControl pMapControl = null;
        ComFunLib CFL = new ComFunLib();
        PrDispalce.PatternRecognition.PolygonPreprocess PP = new PatternRecognition.PolygonPreprocess();
        PrDispalce.工具类.ParameterCompute PC = new 工具类.ParameterCompute();
        double PI = 3.1415926;

        #region 构造函数
        public BuildingPairSim()
        {

        }

        public BuildingPairSim(AxMapControl CacheControl)
        {
            this.pMapControl = CacheControl;
        }
        #endregion

        /// <summary>
        /// 计算两个建筑物的大小相似度(大面积建筑物与小面积建筑物面积的比值)
        /// </summary>
        /// <returns></returns>
        /// Type=0，取值在[0,1]之间；Type=1，输入的第一个建筑物面积比上第二个 
        public double SizeSimilarity(PolygonObject pObject1, PolygonObject pObject2,int Type)
        {
            double SizeSimi = 0;

            if (pObject1 == null || pObject2 == null)
            {
                SizeSimi = 0;
            }

            else
            {
                #region 取值在0/1之间
                if (Type == 0)
                {
                    if (pObject1.Area < pObject2.Area)
                    {
                        SizeSimi = pObject1.Area / pObject2.Area;
                    }

                    else
                    {
                        SizeSimi = pObject2.Area / pObject1.Area;
                    }
                }
                #endregion

                #region 输入的第一个建筑物面积除第二个的面积
                if (Type == 1)
                {
                    SizeSimi = pObject1.Area / pObject2.Area;
                }
                #endregion
            }

            return SizeSimi;
        }

        /// <summary>
        /// 计算两个建筑物的大小相似度(大面积建筑物与小面积建筑物面积的比值)
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// Type Type=0，取值在[0,1]之间；Type=1，输入的第一个建筑物面积比上第二个 
        /// <returns></returns>
        public double SizeSimilarity(IPolygon Po1, IPolygon Po2, int Type)
        {
            double SizeSimi = 0;

            IArea Area1 = Po1 as IArea;
            IArea Area2 = Po2 as IArea;

            #region 取值在0/1之间
            if (Type == 0)
            {
                if (Area1.Area < Area2.Area)
                {
                    SizeSimi = Area1.Area / Area2.Area;
                }

                else
                {
                    SizeSimi = Area2.Area / Area1.Area;
                }
            }
            #endregion

            #region 输入的第一个建筑物面积除第二个的面积
            if (Type == 1)
            {
                SizeSimi = Area1.Area / Area2.Area;
            }
            #endregion

            return SizeSimi;
        }

        /// <summary>
        /// 计算两个建筑物的边数相似度(大边数建筑物与小边数建筑物的边数比值)
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// Type Type=0，取值在[0,1]之间；Type=1，输入的第一个建筑物面积比上第二个 
        /// <returns></returns>
        public double ShapeCountSimilarity(IPolygon Po1, IPolygon Po2, int Type)
        {
            double ShapeCountSimi = 0;

            IPointCollection Po1Collection = Po1 as IPointCollection;
            IPointCollection Po2Collection = Po2 as IPointCollection;

            #region 取值在0/1之间
            if (Type == 0)
            {
                if (Po1Collection.PointCount< Po2Collection.PointCount)
                {
                    ShapeCountSimi = Po1Collection.PointCount / Po2Collection.PointCount;
                }

                else
                {
                    ShapeCountSimi = Po2Collection.PointCount / Po1Collection.PointCount;
                }
            }
            #endregion

            #region 输入的第一个建筑物面积除第二个的面积
            if (Type == 1)
            {
                ShapeCountSimi = Po1Collection.PointCount / Po2Collection.PointCount;
            }
            #endregion

            return ShapeCountSimi;
        }

        /// <summary>
        /// 计算两个建筑物的边数相似度(大边数建筑物与小边数建筑物的边数比值)
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// Type Type=0，取值在[0,1]之间；Type=1，输入的第一个建筑物面积比上第二个 
        /// <returns></returns>
        public double ShapeCountSimilarity(PolygonObject pObject1, PolygonObject pObject2, int Type)
        {
            double ShapeCountSimi = 0;
            if (pObject1 == null || pObject2 == null)
            {
                ShapeCountSimi = 0;
            }

            else
            {
                IPolygon Po1 = Pu.PolygonObjectConvert(pObject1);
                IPolygon Po2 = Pu.PolygonObjectConvert(pObject2);
                ShapeCountSimi = this.ShapeCountSimilarity(Po1, Po2, Type);
            }

            return ShapeCountSimi;
        }

        /// <summary>
        /// 计算两个建筑物的方向相似度
        /// Type=0,[0,180];Type=1,[0,90]
        /// </summary>
        /// <returns></returns>
        public double OrientationSimilarity(PolygonObject pObject1, PolygonObject pObject2, int Type)
        {
            double OrientationSimi = 0;

            if (pObject1 == null || pObject2 == null)
            {
                OrientationSimi = 0;
            }

            else
            {
                IPolygon Po1 = Pu.PolygonObjectConvert(pObject1);
                IPolygon Po2 = Pu.PolygonObjectConvert(pObject2);
                this.OrientationSimilarity(Po1,Po2,Type);
            }

            return OrientationSimi;
        }

        /// <summary>
        /// 计算两个建筑物的方向相似度
        /// Type=0,[0,180];Type=1,[0,90]
        /// </summary>
        /// <returns></returns>
        public double OrientationSimilarity(IPolygon Po1, IPolygon Po2, int Type)
        {
            double OrientationSimi = 0;

            double SBRO1 = BM.GetSMBROrientation(Po1);
            double SBRO2 = BM.GetSMBROrientation(Po2);

            #region 范围[0,180]
            if (Type == 0)
            {
                OrientationSimi = Math.Abs(SBRO1 - SBRO2);
            }
            #endregion

            #region 范围[0,90]
            if (Type == 1)
            {
                OrientationSimi = Math.Abs(SBRO1 - SBRO2);
                if (OrientationSimi > 90)
                {
                    OrientationSimi = 180 - OrientationSimi;
                }
            }
            #endregion

            return OrientationSimi;
        }

        /// <summary>
        /// 计算两个多边形的正对面积比
        /// </summary>
        /// <param name="pObject1"></param>
        /// <param name="pObject2"></param>
        /// <returns></returns>
        public double FRCompute(PolygonObject pObject1, PolygonObject pObject2)
        {
            #region 获取最小绑定矩形以及轴线
            IPolygon SMBR1 = PC.GetSMBR(PC.ObjectConvert(pObject1));
            IPointArray SMBRPoints1 = PC.GetPoints(SMBR1);
            IPoint Point11 = SMBRPoints1.get_Element(1);
            IPoint Point12 = SMBRPoints1.get_Element(2);
            IPoint Point13 = SMBRPoints1.get_Element(3);

            IPolygon SMBR2 = PC.GetSMBR(PC.ObjectConvert(pObject1));
            IPointArray SMBRPoints2 = PC.GetPoints(SMBR2);
            IPoint Point21 = SMBRPoints1.get_Element(1);
            IPoint Point22 = SMBRPoints1.get_Element(2);
            IPoint Point23 = SMBRPoints1.get_Element(3);
            #endregion

            List<Double> FRList = new List<double>();

            #region 轴线1面积比
            IPolyline Line11 = Pu.GetParaLine(Point11, Point11, Point12);//Polygon_1的轴线1 (轴线 Point11 Point12)

            ILine Line_11_12=new LineClass();Line_11_12.FromPoint=Point11;Line_11_12.ToPoint=Point12;
            ITopologicalOperator Line_11_12_Top=Line_11_12 as ITopologicalOperator;
            IPoint tPoint_Line11_Point21 = CFL.ComChuizu(Line11.FromPoint, Line11.ToPoint, Point21);
            IPoint tPoint_Line11_Point22 = CFL.ComChuizu(Line11.FromPoint, Line11.ToPoint, Point22);
            IPoint tPoint_Line11_Point23 = CFL.ComChuizu(Line11.FromPoint, Line11.ToPoint, Point23);

            ILine Line11_Point21_22=new LineClass();Line11_Point21_22.FromPoint=tPoint_Line11_Point21;Line11_Point21_22.ToPoint=tPoint_Line11_Point22;
            ILine Line11_Point22_23=new LineClass();Line11_Point22_23.FromPoint=tPoint_Line11_Point22;Line11_Point22_23.ToPoint=tPoint_Line11_Point23;

            IGeometry GeoLine11_Inter_21_22=Line_11_12_Top.Intersect(Line11_Point21_22 as IGeometry,esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine11_Inter_21_22.IsEmpty)
            {
                IGeometry GeoLine11_Combin_21_22 = Line_11_12_Top.Union(Line11_Point21_22 as IGeometry);

                ILine CacheInterLine = GeoLine11_Inter_21_22 as ILine;
                ILine CacheCombinLine = GeoLine11_Combin_21_22 as ILine;

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }

            IGeometry GeoLine11_Inter_22_23 = Line_11_12_Top.Intersect(Line11_Point22_23 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine11_Inter_22_23.IsEmpty)
            {
                IGeometry GeoLine11_Combin_22_23 = Line_11_12_Top.Union(Line11_Point22_23 as IGeometry);

                ILine CacheInterLine = GeoLine11_Inter_22_23 as ILine;
                ILine CacheCombinLine = GeoLine11_Combin_22_23 as ILine;

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }
            #endregion

            #region 轴线2面积比
            IPolyline Line12 = Pu.GetParaLine(Point12, Point12, Point13);//Polygon_1的轴线2 （轴线 Point_12 Point_13）

            ILine Line_12_13 = new LineClass(); Line_12_13.FromPoint = Point12; Line_12_13.ToPoint = Point13;
            ITopologicalOperator Line_12_13_Top = Line_12_13 as ITopologicalOperator;
            IPoint tPoint_Line12_Point21 = CFL.ComChuizu(Line12.FromPoint, Line12.ToPoint, Point21);
            IPoint tPoint_Line12_Point22 = CFL.ComChuizu(Line12.FromPoint, Line12.ToPoint, Point22);
            IPoint tPoint_Line12_Point23 = CFL.ComChuizu(Line12.FromPoint, Line12.ToPoint, Point23);

            ILine Line12_Point21_22 = new LineClass(); Line12_Point21_22.FromPoint = tPoint_Line12_Point21; Line12_Point21_22.ToPoint = tPoint_Line12_Point22;
            ILine Line12_Point22_23 = new LineClass(); Line12_Point22_23.FromPoint = tPoint_Line12_Point22; Line12_Point22_23.ToPoint = tPoint_Line12_Point23;

            IGeometry GeoLine12_Inter_21_22 = Line_12_13_Top.Intersect(Line12_Point21_22 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine12_Inter_21_22.IsEmpty)
            {
                IGeometry GeoLine12_Combin_21_22 = Line_12_13_Top.Union(Line12_Point21_22 as IGeometry);

                ILine CacheInterLine = GeoLine12_Inter_21_22 as ILine;
                ILine CacheCombinLine = GeoLine12_Combin_21_22 as ILine;

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }

            IGeometry GeoLine12_Inter_22_23 = Line_12_13_Top.Intersect(Line12_Point22_23 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine12_Inter_22_23.IsEmpty)
            {
                IGeometry GeoLine12_Combin_22_23 = Line_12_13_Top.Union(Line12_Point22_23 as IGeometry);

                ILine CacheInterLine = GeoLine12_Inter_22_23 as ILine;
                ILine CacheCombinLine = GeoLine12_Combin_22_23 as ILine;

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }
            #endregion

            #region 轴线3面积比
            IPolyline Line21 = Pu.GetParaLine(Point21, Point21, Point22);//Polygon_2的轴线1

            ILine Line_21_22 = new LineClass(); Line_21_22.FromPoint = Point21; Line_21_22.ToPoint = Point22;
            ITopologicalOperator Line_21_22_Top = Line_21_22 as ITopologicalOperator;
            IPoint tPoint_Line21_Point11 = CFL.ComChuizu(Line21.FromPoint, Line21.ToPoint, Point11);
            IPoint tPoint_Line21_Point12 = CFL.ComChuizu(Line21.FromPoint, Line21.ToPoint, Point12);
            IPoint tPoint_Line21_Point13 = CFL.ComChuizu(Line21.FromPoint, Line21.ToPoint, Point13);

            ILine Line21_Point11_12 = new LineClass(); Line21_Point11_12.FromPoint = tPoint_Line21_Point11; Line21_Point11_12.ToPoint = tPoint_Line21_Point12;
            ILine Line21_Point12_13 = new LineClass(); Line21_Point12_13.FromPoint = tPoint_Line21_Point12; Line21_Point12_13.ToPoint = tPoint_Line21_Point13;

            IGeometry GeoLine21_Inter_11_12 = Line_21_22_Top.Intersect(Line21_Point11_12 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine21_Inter_11_12.IsEmpty)
            {
                IGeometry GeoLine21_Combin_11_12 = Line_21_22_Top.Union(Line21_Point11_12 as IGeometry);

                ILine CacheInterLine = GeoLine21_Inter_11_12 as ILine;
                ILine CacheCombinLine = GeoLine21_Combin_11_12 as ILine;

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }

            IGeometry GeoLine21_Inter_12_13 = Line_21_22_Top.Intersect(Line21_Point12_13 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine21_Inter_12_13.IsEmpty)
            {
                IGeometry GeoLine21_Combin_12_13 = Line_21_22_Top.Union(Line21_Point12_13 as IGeometry);

                ILine CacheInterLine = GeoLine21_Inter_12_13 as ILine;
                ILine CacheCombinLine = GeoLine21_Combin_12_13 as ILine;

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }
            #endregion

            #region 轴线4面积比
            IPolyline Line22 = Pu.GetParaLine(Point21, Point21, Point23);//Polygon_2的轴线2

            ILine Line_22_23 = new LineClass(); Line_22_23.FromPoint = Point22; Line_22_23.ToPoint = Point23;
            ITopologicalOperator Line_22_23_Top = Line_22_23 as ITopologicalOperator;
            IPoint tPoint_Line22_Point11 = CFL.ComChuizu(Line22.FromPoint, Line22.ToPoint, Point11);
            IPoint tPoint_Line22_Point12 = CFL.ComChuizu(Line22.FromPoint, Line22.ToPoint, Point12);
            IPoint tPoint_Line22_Point13 = CFL.ComChuizu(Line22.FromPoint, Line22.ToPoint, Point13);

            ILine Line22_Point11_12 = new LineClass(); Line22_Point11_12.FromPoint = tPoint_Line22_Point11; Line22_Point11_12.ToPoint = tPoint_Line22_Point12;
            ILine Line22_Point12_13 = new LineClass(); Line22_Point12_13.FromPoint = tPoint_Line22_Point12; Line22_Point12_13.ToPoint = tPoint_Line22_Point13;

            IGeometry GeoLine22_Inter_11_12 = Line_22_23_Top.Intersect(Line22_Point11_12 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine22_Inter_11_12.IsEmpty)
            {
                IGeometry GeoLine22_Combin_11_12 = Line_22_23_Top.Union(Line22_Point11_12 as IGeometry);

                ILine CacheInterLine = GeoLine22_Inter_11_12 as ILine;
                ILine CacheCombinLine = GeoLine22_Combin_11_12 as ILine;

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }

            IGeometry GeoLine22_Inter_12_13 = Line_22_23_Top.Intersect(Line22_Point12_13 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine22_Inter_12_13.IsEmpty)
            {
                IGeometry GeoLine22_Combin_12_13 = Line_22_23_Top.Union(Line22_Point12_13 as IGeometry);

                ILine CacheInterLine = GeoLine22_Inter_12_13 as ILine;
                ILine CacheCombinLine = GeoLine22_Combin_12_13 as ILine;

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }
            #endregion

            #region 输出结果
            if (FRList.Count > 0)
            {
                return FRList.Max();
            }
            else
            {
                return 0;
            }
            #endregion
        }

        /// <summary>
        /// 计算两个多边形的正对面积比
        /// </summary>
        /// <param name="pObject1"></param>
        /// <param name="pObject2"></param>
        /// <returns></returns>
        public double FRCompute(IPolygon Po1, IPolygon Po2)
        {
            #region 可视化测试
            PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
            object PolylineSb = Sb.LineSymbolization(1, 100, 100, 100, 0);
            object PolygonSb = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 100, 100, 100);
            object PointSb = Sb.PointSymbolization(200, 200, 200);
            #endregion

            #region 获取最小绑定矩形以及轴线
            IPolygon SMBR1 = PC.GetSMBR(Po1);
            IPointArray SMBRPoints1 = PC.GetPoints(SMBR1);
            IPoint Point11 = SMBRPoints1.get_Element(1);
            IPoint Point12 = SMBRPoints1.get_Element(2);
            IPoint Point13 = SMBRPoints1.get_Element(3);

            IPolygon SMBR2 = PC.GetSMBR(Po2);
            IPointArray SMBRPoints2 = PC.GetPoints(SMBR2);
            IPoint Point21 = SMBRPoints2.get_Element(1);
            IPoint Point22 = SMBRPoints2.get_Element(2);
            IPoint Point23 = SMBRPoints2.get_Element(3);
            #endregion

            List<Double> FRList = new List<double>();

            #region 轴线1面积比
            IPolyline Line11 = Pu.GetParaLine(Point11, Point12, Point11);//Polygon_1的轴线1 (轴线 Point11 Point12)
            Line11.SimplifyNetwork();

            IPolyline Polyline_11_12 = new PolylineClass();
            Polyline_11_12.FromPoint = Point11; Polyline_11_12.ToPoint = Point12; Polyline_11_12.SimplifyNetwork();
            ITopologicalOperator Line_11_12_Top = Polyline_11_12 as ITopologicalOperator;
            //pMapControl.DrawShape(Polyline_11_12);

            IPoint tPoint_Line11_Point21 = CFL.ComChuizu(Line11.FromPoint, Line11.ToPoint, Point21);
            IPoint tPoint_Line11_Point22 = CFL.ComChuizu(Line11.FromPoint, Line11.ToPoint, Point22);
            IPoint tPoint_Line11_Point23 = CFL.ComChuizu(Line11.FromPoint, Line11.ToPoint, Point23);

            IPolyline Line11_Point21_22 = new PolylineClass(); Line11_Point21_22.FromPoint = tPoint_Line11_Point21; Line11_Point21_22.ToPoint = tPoint_Line11_Point22; Line11_Point21_22.SimplifyNetwork();
            IPolyline Line11_Point22_23 = new PolylineClass(); Line11_Point22_23.FromPoint = tPoint_Line11_Point22; Line11_Point22_23.ToPoint = tPoint_Line11_Point23; Line11_Point22_23.SimplifyNetwork();

            //pMapControl.DrawShape(Line11_Point21_22);

            IGeometry GeoLine11_Inter_21_22 = Line_11_12_Top.Intersect(Line11_Point21_22 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine11_Inter_21_22.IsEmpty)
            {
                IGeometry GeoLine11_Combin_21_22 = Line_11_12_Top.Union(Line11_Point21_22 as IGeometry);

                IPolyline CacheInterLine = GeoLine11_Inter_21_22 as IPolyline;
                IPolyline CacheCombinLine = GeoLine11_Combin_21_22 as IPolyline;

                //pMapControl.DrawShape(CacheInterLine);

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }

            IGeometry GeoLine11_Inter_22_23 = Line_11_12_Top.Intersect(Line11_Point22_23 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine11_Inter_22_23.IsEmpty)
            {
                IGeometry GeoLine11_Combin_22_23 = Line_11_12_Top.Union(Line11_Point22_23 as IGeometry);

                IPolyline CacheInterLine = GeoLine11_Inter_22_23 as IPolyline;
                IPolyline CacheCombinLine = GeoLine11_Combin_22_23 as IPolyline;

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }
            #endregion

            #region 轴线2面积比
            IPolyline Line12 = Pu.GetParaLine(Point12, Point12, Point13);//Polygon_1的轴线2 （轴线 Point_12 Point_13）

            IPolyline Line_12_13 = new PolylineClass(); Line_12_13.FromPoint = Point12; Line_12_13.ToPoint = Point13; Line_12_13.SimplifyNetwork();
            ITopologicalOperator Line_12_13_Top = Line_12_13 as ITopologicalOperator;
            IPoint tPoint_Line12_Point21 = CFL.ComChuizu(Line12.FromPoint, Line12.ToPoint, Point21);
            IPoint tPoint_Line12_Point22 = CFL.ComChuizu(Line12.FromPoint, Line12.ToPoint, Point22);
            IPoint tPoint_Line12_Point23 = CFL.ComChuizu(Line12.FromPoint, Line12.ToPoint, Point23);

            IPolyline Line12_Point21_22 = new PolylineClass(); Line12_Point21_22.FromPoint = tPoint_Line12_Point21; Line12_Point21_22.ToPoint = tPoint_Line12_Point22; Line12_Point21_22.SimplifyNetwork();
            IPolyline Line12_Point22_23 = new PolylineClass(); Line12_Point22_23.FromPoint = tPoint_Line12_Point22; Line12_Point22_23.ToPoint = tPoint_Line12_Point23; Line12_Point22_23.SimplifyNetwork();

            IGeometry GeoLine12_Inter_21_22 = Line_12_13_Top.Intersect(Line12_Point21_22 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine12_Inter_21_22.IsEmpty)
            {
                IGeometry GeoLine12_Combin_21_22 = Line_12_13_Top.Union(Line12_Point21_22 as IGeometry);

                IPolyline CacheInterLine = GeoLine12_Inter_21_22 as IPolyline;
                IPolyline CacheCombinLine = GeoLine12_Combin_21_22 as IPolyline;

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }

            IGeometry GeoLine12_Inter_22_23 = Line_12_13_Top.Intersect(Line12_Point22_23 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine12_Inter_22_23.IsEmpty)
            {
                IGeometry GeoLine12_Combin_22_23 = Line_12_13_Top.Union(Line12_Point22_23 as IGeometry);

                IPolyline CacheInterLine = GeoLine12_Inter_22_23 as IPolyline;
                IPolyline CacheCombinLine = GeoLine12_Combin_22_23 as IPolyline;

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }
            #endregion

            #region 轴线3面积比
            IPolyline Line21 = Pu.GetParaLine(Point21, Point21, Point22);//Polygon_2的轴线1

            IPolyline Line_21_22 = new PolylineClass(); Line_21_22.FromPoint = Point21; Line_21_22.ToPoint = Point22; Line_21_22.SimplifyNetwork();
            ITopologicalOperator Line_21_22_Top = Line_21_22 as ITopologicalOperator;
            IPoint tPoint_Line21_Point11 = CFL.ComChuizu(Line21.FromPoint, Line21.ToPoint, Point11);
            IPoint tPoint_Line21_Point12 = CFL.ComChuizu(Line21.FromPoint, Line21.ToPoint, Point12);
            IPoint tPoint_Line21_Point13 = CFL.ComChuizu(Line21.FromPoint, Line21.ToPoint, Point13);

            IPolyline Line21_Point11_12 = new PolylineClass(); Line21_Point11_12.FromPoint = tPoint_Line21_Point11; Line21_Point11_12.ToPoint = tPoint_Line21_Point12; Line21_Point11_12.SimplifyNetwork();
            IPolyline Line21_Point12_13 = new PolylineClass(); Line21_Point12_13.FromPoint = tPoint_Line21_Point12; Line21_Point12_13.ToPoint = tPoint_Line21_Point13; Line21_Point12_13.SimplifyNetwork();

            IGeometry GeoLine21_Inter_11_12 = Line_21_22_Top.Intersect(Line21_Point11_12 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine21_Inter_11_12.IsEmpty)
            {
                IGeometry GeoLine21_Combin_11_12 = Line_21_22_Top.Union(Line21_Point11_12 as IGeometry);

                IPolyline CacheInterLine = GeoLine21_Inter_11_12 as IPolyline;
                IPolyline CacheCombinLine = GeoLine21_Combin_11_12 as IPolyline;

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }

            IGeometry GeoLine21_Inter_12_13 = Line_21_22_Top.Intersect(Line21_Point12_13 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine21_Inter_12_13.IsEmpty)
            {
                IGeometry GeoLine21_Combin_12_13 = Line_21_22_Top.Union(Line21_Point12_13 as IGeometry);

                IPolyline CacheInterLine = GeoLine21_Inter_12_13 as IPolyline;
                IPolyline CacheCombinLine = GeoLine21_Combin_12_13 as IPolyline;

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }
            #endregion

            #region 轴线4面积比
            IPolyline Line22 = Pu.GetParaLine(Point21, Point21, Point23);//Polygon_2的轴线2

            IPolyline Line_22_23 = new PolylineClass(); Line_22_23.FromPoint = Point22; Line_22_23.ToPoint = Point23; Line_22_23.SimplifyNetwork();
            ITopologicalOperator Line_22_23_Top = Line_22_23 as ITopologicalOperator;
            IPoint tPoint_Line22_Point11 = CFL.ComChuizu(Line22.FromPoint, Line22.ToPoint, Point11);
            IPoint tPoint_Line22_Point12 = CFL.ComChuizu(Line22.FromPoint, Line22.ToPoint, Point12);
            IPoint tPoint_Line22_Point13 = CFL.ComChuizu(Line22.FromPoint, Line22.ToPoint, Point13);

            IPolyline Line22_Point11_12 = new PolylineClass(); Line22_Point11_12.FromPoint = tPoint_Line22_Point11; Line22_Point11_12.ToPoint = tPoint_Line22_Point12; Line22_Point11_12.SimplifyNetwork();
            IPolyline Line22_Point12_13 = new PolylineClass(); Line22_Point12_13.FromPoint = tPoint_Line22_Point12; Line22_Point12_13.ToPoint = tPoint_Line22_Point13; Line22_Point12_13.SimplifyNetwork();

            IGeometry GeoLine22_Inter_11_12 = Line_22_23_Top.Intersect(Line22_Point11_12 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine22_Inter_11_12.IsEmpty)
            {
                IGeometry GeoLine22_Combin_11_12 = Line_22_23_Top.Union(Line22_Point11_12 as IGeometry);

                IPolyline CacheInterLine = GeoLine22_Inter_11_12 as IPolyline;
                IPolyline CacheCombinLine = GeoLine22_Combin_11_12 as IPolyline;

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }

            IGeometry GeoLine22_Inter_12_13 = Line_22_23_Top.Intersect(Line22_Point12_13 as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
            if (!GeoLine22_Inter_12_13.IsEmpty)
            {
                IGeometry GeoLine22_Combin_12_13 = Line_22_23_Top.Union(Line22_Point12_13 as IGeometry);

                IPolyline CacheInterLine = GeoLine22_Inter_12_13 as IPolyline;
                IPolyline CacheCombinLine = GeoLine22_Combin_12_13 as IPolyline;

                double CacheFR = CacheInterLine.Length / CacheCombinLine.Length;
                FRList.Add(CacheFR);
            }
            #endregion

            #region 输出结果
            if (FRList.Count > 0)
            {
                return FRList.Max();
            }
            else
            {
                return 0;
            }
            #endregion
        }

        /// <summary>
        /// 计算两个建筑物的形状相似度(ShapeIndex)
        /// 基于ShapeIndex计算两个建筑物的相似性
        /// =1属于[0,1]；=0第一个除以第二个
        /// </summary>
        /// <returns></returns>
        public double ShapeIndexSim(PolygonObject pObject1, PolygonObject pObject2,int Type)
        {
            double ShapeSim = 0;

            if (pObject1 == null || pObject2 == null)
            {
                ShapeSim = 0;
            }

            else
            {
                IPolygon Po1 = Pu.PolygonObjectConvert(pObject1);
                IPolygon Po2 = Pu.PolygonObjectConvert(pObject2);
                ShapeSim = this.ShapeIndexSim(Po1, Po2, Type);
            }

            return ShapeSim;

        }

        /// <summary>
        /// 计算两个建筑物的形状相似度(ShapeIndex)
        /// 基于ShapeIndex计算两个建筑物的相似性
        /// =1属于[0,1]；=0第一个除以第二个
        /// </summary>
        /// <returns></returns>
        public double ShapeIndexSim(IPolygon Po1, IPolygon Po2,int Type)
        {

            double ShapeSim = 0;
            double ShapeIndex1 = BM.ShapeIndex(Po1);
            double ShapeIndex2 = BM.ShapeIndex(Po2);

            #region 第一个除以第二个
            if (Type == 0)
            {
                ShapeSim = ShapeIndex1 / ShapeIndex2;
            }
            #endregion

            if (Type == 1)
            {
                if (ShapeIndex1 > ShapeIndex2)
                {
                    ShapeSim = ShapeIndex2 / ShapeIndex1;
                }
                else
                {
                    ShapeSim = ShapeIndex1 / ShapeIndex2;
                }
            }

            return ShapeSim;
        }

        /// <summary>
        /// 获得两个建筑物基于转角函数的相似性
        /// 值越小表示两个图形越相似(不做转换)
        /// 转换后(2Pi-turningSim)/2Pi
        /// [0,1]
        /// </summary>
        /// <param name="pObject1"></param>
        /// <param name="pObject2"></param>
        /// <returns></returns>
        public double TurningAngeSim(PolygonObject pObject1, PolygonObject pObject2)
        {
            if (pObject1 == null || pObject2 == null)
            {
                return 0;
            }

            else
            {
                List<List<double>> BTurningAngleList = new List<List<double>>();

                #region 计算一对建筑物起点不同的相似度
                List<double> TurningAngleList = new List<double>();

                for (int m = 0; m < pObject1.PointList.Count; m++)
                {
                    List<List<double>> TurningAngle1 = this.GetTurningAngle(pObject1, m);

                    for (int n = 0; n < pObject2.PointList.Count; n++)
                    {
                        List<List<double>> TurningAngle2 = this.GetTurningAngle(pObject2, n);
                        double TurningAngleSim = this.GetTurningSim(TurningAngle1, TurningAngle2);
                        TurningAngleList.Add(TurningAngleSim);
                    }
                }
                #endregion

                return (2 * Math.PI - TurningAngleList.Min()) / (2 * Math.PI);//返回最小值
            }
        }

        /// <summary>
        /// 计算两个建筑物的形状相似性
        /// </summary>
        /// <param name="pObject1"></param>
        /// <param name="pObject2"></param>
        /// <param name="Type"></param>=0shapeIndex；=1转角函数；=2重叠面积
        /// <returns></returns>
        public double ShapeSimilarity(PolygonObject pObject1, PolygonObject pObject2, int Type)
        {
            double ShapeSim=0;
            if (pObject1 == null || pObject2 == null)
            {
                ShapeSim = 0;
            }

            else
            {
                if (Type == 0)
                {
                    ShapeSim = this.ShapeIndexSim(pObject1, pObject2, 0);
                }
                if (Type == 1)
                {
                    ShapeSim = this.TurningAngeSim(pObject1, pObject2);
                }
                if (Type == 2)
                {
                    ShapeSim = this.MDComputation(pObject1, pObject2);
                }
            }

            return ShapeSim;
        }

        /// <summary>
        /// 获得转角函数
        /// </summary>
        /// StartLocation=由于转角函数的起点选择对于相似度计算很大；所以StartLocation标记了起算点
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        List<List<double>> GetTurningAngle(PolygonObject pPolygon, int StartLocation)
        {
            List<List<double>> TurningAngle = new List<List<double>>();

            //获取角度
            pPolygon.GetBendAngle2(); double TotalAngle = 0;

            for (int i = 0; i < pPolygon.BendAngle.Count; i++)
            {
                List<double> tAngleDis = new List<double>();
                List<double> oAngleDis = pPolygon.BendAngle[(StartLocation + i) % pPolygon.BendAngle.Count];

                if (i == 0)
                {
                    tAngleDis.Add(0);//添加长度
                    tAngleDis.Add(oAngleDis[0] / pPolygon.Perimeter);//添加角度
                }

                if (i != 0)
                {
                    TotalAngle = TotalAngle + oAngleDis[1];
                    tAngleDis.Add(TotalAngle % (2 * PI));//添加角度
                    tAngleDis.Add(oAngleDis[0] / pPolygon.Perimeter + TurningAngle[i - 1][1]);//添加长度
                }

                TurningAngle.Add(tAngleDis);
            }

            return TurningAngle;
        }

        /// <summary>
        /// 计算两个转角函数的相似度
        /// </summary>
        /// <param name="TurningAngle1"></param>
        /// <param name="TurningAngle2"></param>
        /// <returns></returns>
        double GetTurningSim(List<List<double>> TurningAngle1, List<List<double>> TurningAngle2)
        {
            double TurningSim = 0;

            List<double> CacheTurning1 = new List<double>();
            List<double> CacheTurning2 = new List<double>();
            int i = 0; int j = 0;
            double StartDis = 0; double EndDis = 0;

            while (Math.Abs(StartDis - 1) > 0.001 && Math.Abs(EndDis - 1) > 0.001)
            {
                CacheTurning1 = TurningAngle1[i];
                CacheTurning2 = TurningAngle2[j];

                if (CacheTurning1[1] < CacheTurning2[1])
                {
                    i++;
                    EndDis = CacheTurning1[1];

                    TurningSim = (EndDis - StartDis) * Math.Abs(CacheTurning1[0] - CacheTurning2[0]) + TurningSim;
                    StartDis = CacheTurning1[1];
                }

                else
                {
                    j++;
                    EndDis = CacheTurning2[1];

                    TurningSim = (EndDis - StartDis) * Math.Abs(CacheTurning1[0] - CacheTurning2[0]) + TurningSim;
                    StartDis = CacheTurning2[1];
                }
            }

            return TurningSim;
        }

        /// <summary>
        /// 计算两个建筑物间的距离关系
        /// </summary>
        /// Type=1 重心距离；Type=2最短距离
        /// <param name="pObject1"></param>
        /// <param name="pObject2"></param>
        /// <returns></returns>
        public double BuidlingDis(PolygonObject pObject1, PolygonObject pObject2,int Type)
        {
            double Dis = 0;

            if (pObject1 == null || pObject2 == null)
            {
                Dis = 0;
            }

            else
            {
                IPolygon Po1 = Pu.PolygonObjectConvert(pObject1);
                IPolygon Po2 = Pu.PolygonObjectConvert(pObject2);
                Dis = this.BuidlingDis(Po1, Po2, Type);
            }

            return Dis;
        }

        /// <summary>
        /// 计算两个建筑物间的距离关系
        /// </summary>
        /// Type=1 重心距离；Type=2最短距离
        /// <param name="pObject1"></param>
        /// <param name="pObject2"></param>
        /// <returns></returns>
        public double BuidlingDis(IPolygon Po1, IPolygon Po2,int Type)
        {
            double Dis = 0;

            IProximityOperator IPO = Po1 as IProximityOperator;

            #region 重心距离
            if (Type == 1)
            {
                IArea IA1 = Po1 as IArea;
                IArea IA2 = Po2 as IArea;
                IPoint cPoint1 = IA1.Centroid;
                IPoint cPoint2 = IA2.Centroid;

                Dis = Math.Sqrt((cPoint1.Y - cPoint2.Y) * (cPoint1.Y - cPoint2.Y) + (cPoint1.X - cPoint2.X) * (cPoint1.X - cPoint2.X));
            }
            #endregion

            #region 最短距离
            if (Type == 2)
            {
                Dis = IPO.ReturnDistance(Po2);
            }
            #endregion


            return Dis;
        }

        /// <summary>
        /// 计算两建筑物的拓扑关系
        /// relationType=1 相离；relationType=2 相切；relationType=3 相交；
        /// </summary>
        /// <param name="pObject1"></param>
        /// <param name="pObject2"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public int TopoRelationComputation(PolygonObject pObject1, PolygonObject pObject2)
        {
            int RelationType = 0;

            if (pObject1 != null & pObject2 != null)
            {
                IPolygon Po1 = Pu.PolygonObjectConvert(pObject1);
                IPolygon Po2 = Pu.PolygonObjectConvert(pObject2);
                IRelationalOperator IRO = Po1 as IRelationalOperator;
                if (IRO.Touches(Po2))
                {
                    RelationType = 2;
                }
                else if (IRO.Overlaps(Po2))
                {
                    RelationType = 3;
                }
                else
                {
                    RelationType = 1;
                }
            }

            return RelationType;
        }

        /// <summary>
        /// 计算两建筑物的拓扑关系
        /// relationType=1 相离；relationType=2 相切；relationType=3 相交；
        /// </summary>
        /// <param name="pObject1"></param>
        /// <param name="pObject2"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        public int TopoRelationComputation(IPolygon Po1, IPolygon Po2,int Type)
        {
            int RelationType = 0;
            IRelationalOperator IRO = Po1 as IRelationalOperator;
            if (IRO.Touches(Po2))
            {
                RelationType = 2;
            }
            else if (IRO.Overlaps(Po2))
            {
                RelationType = 3;
            }
            else
            {
                RelationType = 1;
            }
            return RelationType;
        }

        /// <summary>
        /// 返回两个建筑物的空间方向矩阵
        /// </summary>
        /// <param name="pObject1"></param>
        /// <param name="pObject2"></param>
        /// <returns></returns>
        public double[,] OritationComputation(PolygonObject TargetPo, PolygonObject MatchPo)
        {
            double[,] OritationMatrix = new double[3, 3];

            if (TargetPo != null && MatchPo != null)
            {

                #region 可视化测试
                PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
                object PolygonSb = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 100, 100, 100);
                object PointSb = Sb.PointSymbolization(200, 200, 200);
                #endregion

                #region GetSMBR
                IPolygon CacheTargetPo = Pu.PolygonObjectConvert(TargetPo);
                IPolygon CacheMatchPo = Pu.PolygonObjectConvert(MatchPo);
                IArea CacheMatchArea = CacheMatchPo as IArea;
                double MatchedArea = CacheMatchArea.Area;
                IPolygon EnvelopTarget = BM.GetSMBR(CacheTargetPo);
                //pMapControl.DrawShape(EnvelopTarget, ref PolygonSb);
                #endregion

                #region Get Nodes for 9 regions
                IPointArray SMBRPoints = BM.GetPoints(EnvelopTarget);
                IPoint Point3 = SMBRPoints.get_Element(0);//MaxX and MaxY
                IPoint Point2 = SMBRPoints.get_Element(1);//MinX and MaxY
                IPoint Point1 = SMBRPoints.get_Element(2);//MinX and MinY
                IPoint Point4 = SMBRPoints.get_Element(3);//MaxX and MinY

                //pMapControl.DrawShape(Point1, ref PointSb);
                //pMapControl.DrawShape(Point2, ref PointSb);
                //pMapControl.DrawShape(Point3, ref PointSb);
                //pMapControl.DrawShape(Point4, ref PointSb);

                IPoint Point11 = new PointClass();
                IPoint Point12 = new PointClass();
                IPoint Point13 = new PointClass();
                IPoint Point14 = new PointClass();
                IPoint Point21 = new PointClass();
                IPoint Point24 = new PointClass();
                IPoint Point31 = new PointClass();
                IPoint Point34 = new PointClass();
                IPoint Point41 = new PointClass();
                IPoint Point42 = new PointClass();
                IPoint Point43 = new PointClass();
                IPoint Point44 = new PointClass();

                Point12.X = Point1.X + 1000;
                Point12.Y = (Point1.Y - Point4.Y) * 1000 / (Point1.X - Point4.X) + Point1.Y;
                Point42.X = Point4.X - 1000;
                Point42.Y = Point4.Y - (Point1.Y - Point4.Y) * 1000 / (Point1.X - Point4.X);
                //pMapControl.DrawShape(Point12, ref PointSb);
                //pMapControl.DrawShape(Point42, ref PointSb);

                Point13.X = Point2.X + 1000;
                Point13.Y = (Point2.Y - Point3.Y) * 1000 / (Point2.X - Point3.X) + Point2.Y;
                Point43.X = Point3.X - 1000;
                Point43.Y = Point3.Y - (Point2.Y - Point3.Y) * 1000 / (Point2.X - Point3.X);
                //pMapControl.DrawShape(Point13, ref PointSb);
                //pMapControl.DrawShape(Point43, ref PointSb);

                Point21.X = Point1.X + 1000;
                Point21.Y = (Point1.Y - Point2.Y) * 1000 / (Point1.X - Point2.X) + Point1.Y;
                Point24.X = Point2.X - 1000;
                Point24.Y = Point2.Y - (Point1.Y - Point2.Y) * 1000 / (Point1.X - Point2.X);
                //pMapControl.DrawShape(Point21, ref PointSb);
                //pMapControl.DrawShape(Point24, ref PointSb);

                Point31.X = Point4.X + 1000;
                Point31.Y = (Point4.Y - Point3.Y) * 1000 / (Point4.X - Point3.X) + Point4.Y;
                Point34.X = Point3.X - 1000;
                Point34.Y = Point3.Y - (Point4.Y - Point3.Y) * 1000 / (Point4.X - Point3.X);
                //pMapControl.DrawShape(Point31, ref PointSb);
                //pMapControl.DrawShape(Point34, ref PointSb);

                Point11.X = Point12.X + 1000;
                Point11.Y = (Point12.Y - Point13.Y) * 1000 / (Point12.X - Point13.X) + Point12.Y;
                Point14.X = Point13.X - 1000;
                Point14.Y = Point13.Y - (Point12.Y - Point13.Y) * 1000 / (Point12.X - Point13.X);
                //pMapControl.DrawShape(Point11, ref PointSb);
                //pMapControl.DrawShape(Point14, ref PointSb);

                Point41.X = Point42.X + 1000;
                Point41.Y = (Point42.Y - Point43.Y) * 1000 / (Point42.X - Point43.X) + Point42.Y;
                Point44.X = Point43.X - 1000;
                Point44.Y = Point43.Y - (Point42.Y - Point43.Y) * 1000 / (Point42.X - Point43.X);
                //pMapControl.DrawShape(Point41, ref PointSb);
                //pMapControl.DrawShape(Point44, ref PointSb);
                #endregion

                object missing = Type.Missing;

                #region Get 9 regions
                #region RegionEN
                Ring ringEN = new RingClass();
                ringEN.AddPoint(Point11, ref missing, ref missing);
                ringEN.AddPoint(Point12, ref missing, ref missing);
                ringEN.AddPoint(Point1, ref missing, ref missing);
                ringEN.AddPoint(Point21, ref missing, ref missing);
                IGeometryCollection pointPolygonEN = new PolygonClass();
                pointPolygonEN.AddGeometry(ringEN as IGeometry, ref missing, ref missing);
                IPolygon MBREN = pointPolygonEN as IPolygon;
                //MBREN.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBREN, ref PolygonSb);
                #endregion

                #region RegionNN
                Ring ringNN = new RingClass();
                ringNN.AddPoint(Point12, ref missing, ref missing);
                ringNN.AddPoint(Point13, ref missing, ref missing);
                ringNN.AddPoint(Point2, ref missing, ref missing);
                ringNN.AddPoint(Point1, ref missing, ref missing);
                IGeometryCollection pointPolygonNN = new PolygonClass();
                pointPolygonNN.AddGeometry(ringNN as IGeometry, ref missing, ref missing);
                IPolygon MBRNN = pointPolygonNN as IPolygon;
                MBRNN.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBRNN, ref PolygonSb);
                #endregion

                #region RegionNW
                Ring ringNW = new RingClass();
                ringNW.AddPoint(Point13, ref missing, ref missing);
                ringNW.AddPoint(Point14, ref missing, ref missing);
                ringNW.AddPoint(Point24, ref missing, ref missing);
                ringNW.AddPoint(Point2, ref missing, ref missing);
                IGeometryCollection pointPolygonNW = new PolygonClass();
                pointPolygonNW.AddGeometry(ringNW as IGeometry, ref missing, ref missing);
                IPolygon MBRNW = pointPolygonNW as IPolygon;
                MBRNW.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBRNW, ref PolygonSb);
                #endregion

                #region RegionWW
                Ring ringWW = new RingClass();
                ringWW.AddPoint(Point2, ref missing, ref missing);
                ringWW.AddPoint(Point24, ref missing, ref missing);
                ringWW.AddPoint(Point34, ref missing, ref missing);
                ringWW.AddPoint(Point3, ref missing, ref missing);
                IGeometryCollection pointPolygonWW = new PolygonClass();
                pointPolygonWW.AddGeometry(ringWW as IGeometry, ref missing, ref missing);
                IPolygon MBRWW = pointPolygonWW as IPolygon;
                MBRWW.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBRWW, ref PolygonSb);
                #endregion

                #region RegionWS
                Ring ringWS = new RingClass();
                ringWS.AddPoint(Point3, ref missing, ref missing);
                ringWS.AddPoint(Point34, ref missing, ref missing);
                ringWS.AddPoint(Point44, ref missing, ref missing);
                ringWS.AddPoint(Point43, ref missing, ref missing);
                IGeometryCollection pointPolygonWS = new PolygonClass();
                pointPolygonWS.AddGeometry(ringWS as IGeometry, ref missing, ref missing);
                IPolygon MBRWS = pointPolygonWS as IPolygon;
                MBRWS.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBRWS, ref PolygonSb);
                #endregion

                #region RegionSS
                Ring ringSS = new RingClass();
                ringSS.AddPoint(Point4, ref missing, ref missing);
                ringSS.AddPoint(Point3, ref missing, ref missing);
                ringSS.AddPoint(Point43, ref missing, ref missing);
                ringSS.AddPoint(Point42, ref missing, ref missing);
                IGeometryCollection pointPolygonSS = new PolygonClass();
                pointPolygonSS.AddGeometry(ringSS as IGeometry, ref missing, ref missing);
                IPolygon MBRSS = pointPolygonSS as IPolygon;
                MBRSS.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBRSS, ref PolygonSb);
                #endregion

                #region RegionSE
                Ring ringSE = new RingClass();
                ringSE.AddPoint(Point31, ref missing, ref missing);
                ringSE.AddPoint(Point4, ref missing, ref missing);
                ringSE.AddPoint(Point42, ref missing, ref missing);
                ringSE.AddPoint(Point41, ref missing, ref missing);
                IGeometryCollection pointPolygonSE = new PolygonClass();
                pointPolygonSE.AddGeometry(ringSE as IGeometry, ref missing, ref missing);
                IPolygon MBRSE = pointPolygonSE as IPolygon;
                MBRSE.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBRSE, ref PolygonSb);
                #endregion

                #region RegionEE
                Ring ringEE = new RingClass();
                ringEE.AddPoint(Point21, ref missing, ref missing);
                ringEE.AddPoint(Point1, ref missing, ref missing);
                ringEE.AddPoint(Point4, ref missing, ref missing);
                ringEE.AddPoint(Point31, ref missing, ref missing);
                IGeometryCollection pointPolygonEE = new PolygonClass();
                pointPolygonEE.AddGeometry(ringEE as IGeometry, ref missing, ref missing);
                IPolygon MBREE = pointPolygonEE as IPolygon;
                MBREE.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBREE, ref PolygonSb);
                #endregion
                #endregion

                #region 获得相交部分，并返回不同块的比值
                ITopologicalOperator ITO = CacheMatchPo as ITopologicalOperator;
                #region AreaEN
                IGeometry iGeoEN = ITO.Intersect(MBREN as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaEN = 0;
                if (iGeoEN != null)
                {
                    //pMapControl.DrawShape(iGeoEN, ref PolygonSb);
                    IArea iArea = iGeoEN as IArea;
                    AreaEN = iArea.Area;
                }
                #endregion
                #region AreaNN
                IGeometry iGeoNN = ITO.Intersect(MBRNN as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaNN = 0;
                if (iGeoNN != null)
                {
                    //pMapControl.DrawShape(iGeoNN, ref PolygonSb);
                    IArea iArea = iGeoNN as IArea;
                    AreaNN = iArea.Area;
                }
                #endregion
                #region AreaNW
                IGeometry iGeoNW = ITO.Intersect(MBRNW as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaNW = 0;
                if (iGeoNW != null)
                {
                    //pMapControl.DrawShape(iGeoNW, ref PolygonSb);
                    IArea iArea = iGeoNW as IArea;
                    AreaNW = iArea.Area;
                }
                #endregion
                #region AreaWW
                IGeometry iGeoWW = ITO.Intersect(MBRWW as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaWW = 0;
                if (iGeoWW != null)
                {
                    //pMapControl.DrawShape(iGeoWW, ref PolygonSb);
                    IArea iArea = iGeoWW as IArea;
                    AreaWW = iArea.Area;
                }
                #endregion
                #region AreaWS
                IGeometry iGeoWS = ITO.Intersect(MBRWS as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaWS = 0;
                if (iGeoWS != null)
                {
                    //pMapControl.DrawShape(iGeoWS, ref PolygonSb);
                    IArea iArea = iGeoWS as IArea;
                    AreaWS = iArea.Area;
                }
                #endregion
                #region AreaSS
                IGeometry iGeoSS = ITO.Intersect(MBRSS as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaSS = 0;
                if (iGeoSS != null)
                {
                    //pMapControl.DrawShape(iGeoSS, ref PolygonSb);
                    IArea iArea = iGeoSS as IArea;
                    AreaSS = iArea.Area;
                }
                #endregion
                #region AreaSE
                IGeometry iGeoSE = ITO.Intersect(MBRSE as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaSE = 0;
                if (iGeoSE != null)
                {
                    //pMapControl.DrawShape(iGeoSE, ref PolygonSb);
                    IArea iArea = iGeoSE as IArea;
                    AreaSE = iArea.Area;
                }
                #endregion
                #region AreaEE
                IGeometry iGeoEE = ITO.Intersect(MBREE as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaEE = 0;
                if (iGeoEE != null)
                {
                    //pMapControl.DrawShape(iGeoEE, ref PolygonSb);
                    IArea iArea = iGeoEE as IArea;
                    AreaEE = iArea.Area;
                }
                #endregion
                #region AreaTT
                IGeometry iGeoTT = ITO.Intersect(EnvelopTarget as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaTT = 0;
                if (iGeoTT != null)
                {
                    //pMapControl.DrawShape(iGeoTT, ref PolygonSb);
                    IArea iArea = iGeoTT as IArea;
                    AreaTT = iArea.Area;
                }
                #endregion
                #endregion

                #region 赋值
                OritationMatrix[0, 0] = AreaEN / MatchedArea;
                OritationMatrix[0, 1] = AreaNN / MatchedArea;
                OritationMatrix[0, 2] = AreaNW / MatchedArea;
                OritationMatrix[1, 0] = AreaWW / MatchedArea;
                OritationMatrix[1, 1] = AreaTT / MatchedArea;
                OritationMatrix[1, 2] = AreaEE / MatchedArea;
                OritationMatrix[2, 0] = AreaWS / MatchedArea;
                OritationMatrix[2, 1] = AreaSS / MatchedArea;
                OritationMatrix[2, 2] = AreaSE / MatchedArea;
                #endregion
            }

            return OritationMatrix;
        }

        /// <summary>
        /// 返回两个建筑物的空间方向矩阵
        /// </summary>
        /// <param name="pObject1"></param>
        /// <param name="pObject2"></param>
        /// <returns></returns>
        public double[,] OritationComputation(IPolygon Po1, IPolygon Po2)
        {
            double[,] OritationMatrix = new double[3, 3];

            if (Po1 != null && Po2 != null)
            {

                #region 可视化测试
                PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
                object PolygonSb = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 100, 100, 100);
                object PointSb = Sb.PointSymbolization(200, 200, 200);
                #endregion

                #region GetSMBR
                IPolygon CacheTargetPo = Po1;
                IPolygon CacheMatchPo = Po2;
                IArea CacheMatchArea = CacheMatchPo as IArea;
                double MatchedArea = CacheMatchArea.Area;
                IPolygon EnvelopTarget = BM.GetSMBR(CacheTargetPo);
                //pMapControl.DrawShape(EnvelopTarget, ref PolygonSb);
                #endregion

                #region Get Nodes for 9 regions
                IPointArray SMBRPoints = BM.GetPoints(EnvelopTarget);
                IPoint Point3 = SMBRPoints.get_Element(0);//MaxX and MaxY
                IPoint Point2 = SMBRPoints.get_Element(1);//MinX and MaxY
                IPoint Point1 = SMBRPoints.get_Element(2);//MinX and MinY
                IPoint Point4 = SMBRPoints.get_Element(3);//MaxX and MinY

                //pMapControl.DrawShape(Point1, ref PointSb);
                //pMapControl.DrawShape(Point2, ref PointSb);
                //pMapControl.DrawShape(Point3, ref PointSb);
                //pMapControl.DrawShape(Point4, ref PointSb);

                IPoint Point11 = new PointClass();
                IPoint Point12 = new PointClass();
                IPoint Point13 = new PointClass();
                IPoint Point14 = new PointClass();
                IPoint Point21 = new PointClass();
                IPoint Point24 = new PointClass();
                IPoint Point31 = new PointClass();
                IPoint Point34 = new PointClass();
                IPoint Point41 = new PointClass();
                IPoint Point42 = new PointClass();
                IPoint Point43 = new PointClass();
                IPoint Point44 = new PointClass();

                Point12.X = Point1.X + 1000;
                Point12.Y = (Point1.Y - Point4.Y) * 1000 / (Point1.X - Point4.X) + Point1.Y;
                Point42.X = Point4.X - 1000;
                Point42.Y = Point4.Y - (Point1.Y - Point4.Y) * 1000 / (Point1.X - Point4.X);
                //pMapControl.DrawShape(Point12, ref PointSb);
                //pMapControl.DrawShape(Point42, ref PointSb);

                Point13.X = Point2.X + 1000;
                Point13.Y = (Point2.Y - Point3.Y) * 1000 / (Point2.X - Point3.X) + Point2.Y;
                Point43.X = Point3.X - 1000;
                Point43.Y = Point3.Y - (Point2.Y - Point3.Y) * 1000 / (Point2.X - Point3.X);
                //pMapControl.DrawShape(Point13, ref PointSb);
                //pMapControl.DrawShape(Point43, ref PointSb);

                Point21.X = Point1.X + 1000;
                Point21.Y = (Point1.Y - Point2.Y) * 1000 / (Point1.X - Point2.X) + Point1.Y;
                Point24.X = Point2.X - 1000;
                Point24.Y = Point2.Y - (Point1.Y - Point2.Y) * 1000 / (Point1.X - Point2.X);
                //pMapControl.DrawShape(Point21, ref PointSb);
                //pMapControl.DrawShape(Point24, ref PointSb);

                Point31.X = Point4.X + 1000;
                Point31.Y = (Point4.Y - Point3.Y) * 1000 / (Point4.X - Point3.X) + Point4.Y;
                Point34.X = Point3.X - 1000;
                Point34.Y = Point3.Y - (Point4.Y - Point3.Y) * 1000 / (Point4.X - Point3.X);
                //pMapControl.DrawShape(Point31, ref PointSb);
                //pMapControl.DrawShape(Point34, ref PointSb);

                Point11.X = Point12.X + 1000;
                Point11.Y = (Point12.Y - Point13.Y) * 1000 / (Point12.X - Point13.X) + Point12.Y;
                Point14.X = Point13.X - 1000;
                Point14.Y = Point13.Y - (Point12.Y - Point13.Y) * 1000 / (Point12.X - Point13.X);
                //pMapControl.DrawShape(Point11, ref PointSb);
                //pMapControl.DrawShape(Point14, ref PointSb);

                Point41.X = Point42.X + 1000;
                Point41.Y = (Point42.Y - Point43.Y) * 1000 / (Point42.X - Point43.X) + Point42.Y;
                Point44.X = Point43.X - 1000;
                Point44.Y = Point43.Y - (Point42.Y - Point43.Y) * 1000 / (Point42.X - Point43.X);
                //pMapControl.DrawShape(Point41, ref PointSb);
                //pMapControl.DrawShape(Point44, ref PointSb);
                #endregion

                object missing = Type.Missing;

                #region Get 9 regions
                #region RegionEN
                Ring ringEN = new RingClass();
                ringEN.AddPoint(Point11, ref missing, ref missing);
                ringEN.AddPoint(Point12, ref missing, ref missing);
                ringEN.AddPoint(Point1, ref missing, ref missing);
                ringEN.AddPoint(Point21, ref missing, ref missing);
                IGeometryCollection pointPolygonEN = new PolygonClass();
                pointPolygonEN.AddGeometry(ringEN as IGeometry, ref missing, ref missing);
                IPolygon MBREN = pointPolygonEN as IPolygon;
                //MBREN.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBREN, ref PolygonSb);
                #endregion

                #region RegionNN
                Ring ringNN = new RingClass();
                ringNN.AddPoint(Point12, ref missing, ref missing);
                ringNN.AddPoint(Point13, ref missing, ref missing);
                ringNN.AddPoint(Point2, ref missing, ref missing);
                ringNN.AddPoint(Point1, ref missing, ref missing);
                IGeometryCollection pointPolygonNN = new PolygonClass();
                pointPolygonNN.AddGeometry(ringNN as IGeometry, ref missing, ref missing);
                IPolygon MBRNN = pointPolygonNN as IPolygon;
                MBRNN.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBRNN, ref PolygonSb);
                #endregion

                #region RegionNW
                Ring ringNW = new RingClass();
                ringNW.AddPoint(Point13, ref missing, ref missing);
                ringNW.AddPoint(Point14, ref missing, ref missing);
                ringNW.AddPoint(Point24, ref missing, ref missing);
                ringNW.AddPoint(Point2, ref missing, ref missing);
                IGeometryCollection pointPolygonNW = new PolygonClass();
                pointPolygonNW.AddGeometry(ringNW as IGeometry, ref missing, ref missing);
                IPolygon MBRNW = pointPolygonNW as IPolygon;
                MBRNW.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBRNW, ref PolygonSb);
                #endregion

                #region RegionWW
                Ring ringWW = new RingClass();
                ringWW.AddPoint(Point2, ref missing, ref missing);
                ringWW.AddPoint(Point24, ref missing, ref missing);
                ringWW.AddPoint(Point34, ref missing, ref missing);
                ringWW.AddPoint(Point3, ref missing, ref missing);
                IGeometryCollection pointPolygonWW = new PolygonClass();
                pointPolygonWW.AddGeometry(ringWW as IGeometry, ref missing, ref missing);
                IPolygon MBRWW = pointPolygonWW as IPolygon;
                MBRWW.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBRWW, ref PolygonSb);
                #endregion

                #region RegionWS
                Ring ringWS = new RingClass();
                ringWS.AddPoint(Point3, ref missing, ref missing);
                ringWS.AddPoint(Point34, ref missing, ref missing);
                ringWS.AddPoint(Point44, ref missing, ref missing);
                ringWS.AddPoint(Point43, ref missing, ref missing);
                IGeometryCollection pointPolygonWS = new PolygonClass();
                pointPolygonWS.AddGeometry(ringWS as IGeometry, ref missing, ref missing);
                IPolygon MBRWS = pointPolygonWS as IPolygon;
                MBRWS.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBRWS, ref PolygonSb);
                #endregion

                #region RegionSS
                Ring ringSS = new RingClass();
                ringSS.AddPoint(Point4, ref missing, ref missing);
                ringSS.AddPoint(Point3, ref missing, ref missing);
                ringSS.AddPoint(Point43, ref missing, ref missing);
                ringSS.AddPoint(Point42, ref missing, ref missing);
                IGeometryCollection pointPolygonSS = new PolygonClass();
                pointPolygonSS.AddGeometry(ringSS as IGeometry, ref missing, ref missing);
                IPolygon MBRSS = pointPolygonSS as IPolygon;
                MBRSS.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBRSS, ref PolygonSb);
                #endregion

                #region RegionSE
                Ring ringSE = new RingClass();
                ringSE.AddPoint(Point31, ref missing, ref missing);
                ringSE.AddPoint(Point4, ref missing, ref missing);
                ringSE.AddPoint(Point42, ref missing, ref missing);
                ringSE.AddPoint(Point41, ref missing, ref missing);
                IGeometryCollection pointPolygonSE = new PolygonClass();
                pointPolygonSE.AddGeometry(ringSE as IGeometry, ref missing, ref missing);
                IPolygon MBRSE = pointPolygonSE as IPolygon;
                MBRSE.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBRSE, ref PolygonSb);
                #endregion

                #region RegionEE
                Ring ringEE = new RingClass();
                ringEE.AddPoint(Point21, ref missing, ref missing);
                ringEE.AddPoint(Point1, ref missing, ref missing);
                ringEE.AddPoint(Point4, ref missing, ref missing);
                ringEE.AddPoint(Point31, ref missing, ref missing);
                IGeometryCollection pointPolygonEE = new PolygonClass();
                pointPolygonEE.AddGeometry(ringEE as IGeometry, ref missing, ref missing);
                IPolygon MBREE = pointPolygonEE as IPolygon;
                MBREE.SimplifyPreserveFromTo();
                //pMapControl.DrawShape(MBREE, ref PolygonSb);
                #endregion
                #endregion

                #region 获得相交部分，并返回不同块的比值
                ITopologicalOperator ITO = CacheMatchPo as ITopologicalOperator;
                #region AreaEN
                IGeometry iGeoEN = ITO.Intersect(MBREN as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaEN = 0;
                if (iGeoEN != null)
                {
                    //pMapControl.DrawShape(iGeoEN, ref PolygonSb);
                    IArea iArea = iGeoEN as IArea;
                    AreaEN = iArea.Area;
                }
                #endregion
                #region AreaNN
                IGeometry iGeoNN = ITO.Intersect(MBRNN as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaNN = 0;
                if (iGeoNN != null)
                {
                    //pMapControl.DrawShape(iGeoNN, ref PolygonSb);
                    IArea iArea = iGeoNN as IArea;
                    AreaNN = iArea.Area;
                }
                #endregion
                #region AreaNW
                IGeometry iGeoNW = ITO.Intersect(MBRNW as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaNW = 0;
                if (iGeoNW != null)
                {
                    //pMapControl.DrawShape(iGeoNW, ref PolygonSb);
                    IArea iArea = iGeoNW as IArea;
                    AreaNW = iArea.Area;
                }
                #endregion
                #region AreaWW
                IGeometry iGeoWW = ITO.Intersect(MBRWW as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaWW = 0;
                if (iGeoWW != null)
                {
                    //pMapControl.DrawShape(iGeoWW, ref PolygonSb);
                    IArea iArea = iGeoWW as IArea;
                    AreaWW = iArea.Area;
                }
                #endregion
                #region AreaWS
                IGeometry iGeoWS = ITO.Intersect(MBRWS as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaWS = 0;
                if (iGeoWS != null)
                {
                    //pMapControl.DrawShape(iGeoWS, ref PolygonSb);
                    IArea iArea = iGeoWS as IArea;
                    AreaWS = iArea.Area;
                }
                #endregion
                #region AreaSS
                IGeometry iGeoSS = ITO.Intersect(MBRSS as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaSS = 0;
                if (iGeoSS != null)
                {
                    //pMapControl.DrawShape(iGeoSS, ref PolygonSb);
                    IArea iArea = iGeoSS as IArea;
                    AreaSS = iArea.Area;
                }
                #endregion
                #region AreaSE
                IGeometry iGeoSE = ITO.Intersect(MBRSE as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaSE = 0;
                if (iGeoSE != null)
                {
                    //pMapControl.DrawShape(iGeoSE, ref PolygonSb);
                    IArea iArea = iGeoSE as IArea;
                    AreaSE = iArea.Area;
                }
                #endregion
                #region AreaEE
                IGeometry iGeoEE = ITO.Intersect(MBREE as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaEE = 0;
                if (iGeoEE != null)
                {
                    //pMapControl.DrawShape(iGeoEE, ref PolygonSb);
                    IArea iArea = iGeoEE as IArea;
                    AreaEE = iArea.Area;
                }
                #endregion
                #region AreaTT
                IGeometry iGeoTT = ITO.Intersect(EnvelopTarget as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                double AreaTT = 0;
                if (iGeoTT != null)
                {
                    //pMapControl.DrawShape(iGeoTT, ref PolygonSb);
                    IArea iArea = iGeoTT as IArea;
                    AreaTT = iArea.Area;
                }
                #endregion
                #endregion

                #region 赋值
                OritationMatrix[0, 0] = AreaEN / MatchedArea;
                OritationMatrix[0, 1] = AreaNN / MatchedArea;
                OritationMatrix[0, 2] = AreaNW / MatchedArea;
                OritationMatrix[1, 0] = AreaWW / MatchedArea;
                OritationMatrix[1, 1] = AreaTT / MatchedArea;
                OritationMatrix[1, 2] = AreaEE / MatchedArea;
                OritationMatrix[2, 0] = AreaWS / MatchedArea;
                OritationMatrix[2, 1] = AreaSS / MatchedArea;
                OritationMatrix[2, 2] = AreaSE / MatchedArea;
                #endregion
            }

            return OritationMatrix;
        }

        /// <summary>
        /// MDSim (1-(a^b)(a v b))
        /// 基于重叠面积计算两个图形的形状相似性
        /// </summary>
        /// <param name="TargetPo"></param>
        /// <param name="MatchingPo"></param>
        /// <returns></returns>
        public double MDComputation(PolygonObject oTargetPo, PolygonObject oMatchingPo)
        {
            if (oTargetPo != null && oMatchingPo != null)
            {
                PublicUtil Pu = new PublicUtil();

                IPolygon TargetPo = Pu.PolygonObjectConvert(oTargetPo);
                IPolygon MatchPo = Pu.PolygonObjectConvert(oMatchingPo);

                return this.MDComputation(TargetPo, MatchPo);
            }

            else
            {
                return 0;
            }
        }

        /// <summary>
        /// MDSim (1-(a^b)(a v b))
        /// </summary>
        /// <param name="TargetPo"></param>
        /// <param name="MatchingPo"></param>
        /// <returns></returns>
        public double MDComputation(IPolygon TargetPo, IPolygon MatchingPo)
        {
            double MDSim = 0;
            TargetPo.SimplifyPreserveFromTo();//保证拓扑正确
            MatchingPo.SimplifyPreserveFromTo();//保证拓扑正确

            if (TargetPo != null && MatchingPo != null)
            {
                #region 平移旋转指定角度
                double tSBRO = PC.GetSMBROrientation(TargetPo);//TargetPo与Matching角度
                double mSBRO = PC.GetSMBROrientation(MatchingPo);
                IArea pArea = TargetPo as IArea;
                IPoint CenterPoint = pArea.Centroid;

                PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();//可视化测试
                object PolygonSb = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 100, 100, 100);

                double rOri = (tSBRO - mSBRO) * PI / 180;
                IPolygon rPolygon = Pu.GetRotatedPolygon(MatchingPo, rOri);
                //pMapControl.DrawShape(rPolygon, ref PolygonSb);

                IPolygon pPolygon = Pu.GetPannedPolygon(rPolygon, CenterPoint);
                //pMapControl.DrawShape(pPolygon, ref PolygonSb);

                //pMapControl.Refresh();
                #endregion

                #region 计算MDSim
                ITopologicalOperator iTo = TargetPo as ITopologicalOperator;

                double idArea = 0;
                double udArea = 0;
                IGeometry iGeo = iTo.Intersect(pPolygon as IGeometry, esriGeometryDimension.esriGeometry2Dimension);
                IGeometry uGeo = iTo.Union(pPolygon as IGeometry);
                if (iGeo != null)
                {
                    IArea iArea = iGeo as IArea;
                    idArea = iArea.Area;
                }
                if (uGeo != null)
                {
                    IArea uArea = uGeo as IArea;
                    udArea = uArea.Area;
                }

                MDSim = idArea / udArea;
                #endregion
            }

            return MDSim;
        }
    }
}
