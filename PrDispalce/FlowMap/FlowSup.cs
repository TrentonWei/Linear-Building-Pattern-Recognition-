using System;
using System.Collections.Generic;
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
using ESRI.ArcGIS.DataSourcesRaster;

using AuxStructureLib;
using AuxStructureLib.IO;

namespace PrDispalce.FlowMap
{
    /// <summary>
    /// FlowMap生成支撑类
    /// </summary>
    class FlowSup
    {
        //备注：这里似乎针对tif格式文件有效，但是针对其它格式无效；如何处理这些问题，需要明确！！
        public void ElvUpadate(IRaster pRaster)
        {
            IRasterBandCollection rasBandCol = (IRasterBandCollection)pRaster;
            IRawBlocks rawBlocks;
            IRasterInfo rasInfo;
            IPixelBlock pb;

            // Iterate through each band of the dataset.
            for (int m = 0; m <= rasBandCol.Count - 1; m++)
            {
                // QI to IRawBlocks from IRasterBandCollection.
                rawBlocks = (IRawBlocks)rasBandCol.Item(m);
                rasInfo = rawBlocks.RasterInfo;
                // Create the pixel block.
                pb = rawBlocks.CreatePixelBlock();

                // Determine the tiling scheme for the raster dataset.

                int bStartX = (int)Math.Floor((rasInfo.Extent.Envelope.XMin -
                    rasInfo.Origin.X) / (rasInfo.BlockWidth * rasInfo.CellSize.X));
                int bEndX = (int)Math.Ceiling((rasInfo.Extent.Envelope.XMax -
                    rasInfo.Origin.X) / (rasInfo.BlockWidth * rasInfo.CellSize.X));
                int bStartY = (int)Math.Floor((rasInfo.Origin.Y -
                    rasInfo.Extent.Envelope.YMax) / (rasInfo.BlockHeight *
                    rasInfo.CellSize.Y));
                int bEndY = (int)Math.Ceiling((rasInfo.Origin.Y -
                    rasInfo.Extent.Envelope.YMin) / (rasInfo.BlockHeight *
                    rasInfo.CellSize.Y));

                // Iterate through the pixel blocks.
                for (int pbYcursor = bStartY; pbYcursor < bEndY; pbYcursor++)
                {
                    for (int pbXcursor = bStartX; pbXcursor < bEndX; pbXcursor++)
                    {
                        // Get the pixel block.
                        rawBlocks.ReadBlock(pbXcursor, pbYcursor, 0, pb);
                        System.Array safeArray;
                        // Put the pixel block into a SafeArray for manipulation.
                        safeArray = (System.Array)pb.get_SafeArray(0);

                        // Iterate through the pixels in the pixel block.
                        for (int safeArrayHeight = 0; safeArrayHeight < pb.Height;
                            safeArrayHeight++)
                        {
                            for (int safeArrayWidth = 0; safeArrayWidth < pb.Width;
                                safeArrayWidth++)
                            {
                                // Use System.Array.SetValue to write the new pixel value back into the SafeArray.
                                safeArray.SetValue(Convert.ToByte(0), safeArrayWidth,
                                    safeArrayHeight);
                            }
                        }
                        // Set the SafeArray back to the pixel block.
                        pb.set_SafeArray(0, safeArray);

                        // Write the pixel block back to the dataset.
                        rawBlocks.WriteBlock(pbXcursor, pbYcursor, 0, pb);
                    }
                }
            }

            #region 无用
            //IRaster2 raster2 = pRaster as IRaster2;
            ////创建一个光标以给定像素块大小 
            //IRasterCursor rasterCursor = raster2.CreateCursorEx(null);
            ////控制像素块级别的编辑操作 
            //IRasterEdit rasterEdit = raster2 as IRasterEdit;

            ////得到一段光栅带 
            //IRasterBandCollection bandCollection = (IRasterBandCollection)raster2;
            //System.Array pixels;
            //IPnt pnt = null;
            //object value;
            //int bandCount = bandCollection.Count;
            ////创建像素块 
            //IPixelBlock3 pixelBlock3 = null;
            //int blockWidth = 0;
            //int blockHeight = 0;

            //do
            //{
            //    pixelBlock3 = rasterCursor.PixelBlock as IPixelBlock3;
            //    blockWidth = pixelBlock3.Width;
            //    blockHeight = pixelBlock3.Height;
            //    for (int k = 0; k < bandCount; k++)
            //    {
            //        //指定平面的像素的数组 
            //        pixels = (System.Array)pixelBlock3.get_PixelData(k);

            //        for (int i = 0; i < blockWidth; i++)
            //        {
            //            for (int j = 0; j < blockHeight; j++)
            //            {
            //                value = pixels.GetValue(i, j);
            //                pixels.SetValue(Convert.ToByte(1), i, j);//注意：这里的SetValue值需与原图像的像素值保持同一类型
            //                //value = pixels.GetValue(i, j);

            //                //int testloca = 0;

            //                //}
            //            }
            //        }
            //        pixelBlock3.set_PixelData(k, pixels);
            //    }
            //    pnt = rasterCursor.TopLeft;
            //    rasterEdit.Write(pnt, (IPixelBlock)pixelBlock3);
            //    //rasterEdit.Refresh();
            //} while (rasterCursor.Next());

            //System.Runtime.InteropServices.Marshal.ReleaseComObject(rasterEdit);


            //IRasterProps rasterProps = (IRasterProps)pRaster;
            ////设置栅格数据起始点
            //IPnt pBlockSize = new Pnt();
            //pBlockSize.SetCoords(rasterProps.Width, rasterProps.Height);
            ////获取整个范围
            //IPixelBlock pPixelBlock = pRaster.CreatePixelBlock(pBlockSize);
            //// IPixelBlock3 pPixelBlock = (IPixelBlock3)pRaster.CreatePixelBlock(pBlockSize);
            ////左上点坐标
            //IPnt tlp = new Pnt();
            //tlp.SetCoords(0, 0);
            ////读入栅格
            //IRasterBandCollection pRasterBands = pRaster as IRasterBandCollection;
            //IRasterBand pRasterBand = pRasterBands.Item(0);
            //IRawPixels pRawPixels = pRasterBands.Item(0) as IRawPixels;
            //pRawPixels.Read(tlp, pPixelBlock);
            ////将pixel的值组成数组
            //System.Array pSafeArray = pPixelBlock.get_SafeArray(0) as System.Array;
            //for (int y = 0; y < rasterProps.Height; y++)
            //{
            //    for (int x = 0; x < rasterProps.Width; x++)
            //    {
            //        pSafeArray.SetValue(1, x, y);
            //    }
            //}
            //pPixelBlock.set_SafeArray(0, pSafeArray);
            ////编辑raster,将更新的值写入raster中
            //IRasterEdit rasterEdit = pRaster as IRasterEdit;
            //rasterEdit.Write(tlp, pPixelBlock);
            //rasterEdit.Refresh();        

            //Create a raster. 
            //IRaster2 raster2 = rasterDs.CreateFullRaster() as IRaster2;
            ////Create a raster cursor with a system-optimized pixel block size by passing a null.
            //IRasterCursor rasterCursor = raster2.CreateCursorEx(null);
            ////Use the IRasterEdit interface.
            //IRasterEdit rasterEdit = raster2 as IRasterEdit;
            ////Loop through each band and pixel block.
            //IRasterBandCollection bands = rasterDs as IRasterBandCollection;
            //IPixelBlock3 pixelblock3 = null;
            //long blockwidth = 0;
            //long blockheight = 0;
            //System.Array pixels;
            //IPnt tlc = null;
            //object v;
            //long bandCount = bands.Count;
            //do
            //{
            //    pixelblock3 = rasterCursor.PixelBlock as IPixelBlock3;
            //    blockwidth = pixelblock3.Width;
            //    blockheight = pixelblock3.Height;
            //    pixelblock3.Mask(255);
            //    for (int k = 0; k < bandCount; k++)
            //    {
            //        //Get the pixel array.
            //        pixels = (System.Array)pixelblock3.get_PixelData(k);
            //        for (long i = 0; i < blockwidth; i++)
            //        {
            //            for (long j = 0; j < blockheight; j++)
            //            {
            //                //Get the pixel value.
            //                v = pixels.GetValue(i, j);
            //                //Do something with the value.
            //            }
            //        }
            //        //Set the pixel array to the pixel block.
            //        pixelblock3.set_PixelData(k, pixels);
            //    }
            //    //Write back to the raster.
            //    tlc = rasterCursor.TopLeft;
            //    rasterEdit.Write(tlc, (IPixelBlock)pixelblock3);
            //}
            //while (rasterCursor.Next() == true);
            //System.Runtime.InteropServices.Marshal.ReleaseComObject(rasterEdit);
            #endregion
        }
 
        /// <summary>
        /// 针对制定范围构建索引
        /// </summary>
        /// <param name="ENVELOPE"></param> [MinX,MinY,MaxX,MaxY]【左下角往右上角编码】
        /// <param name="gridXY"></param>[格网横向、纵向间隔]
        /// <param name="colNum"></param>[输出列]
        /// <param name="rowNum"></param>[输出行]
        /// <returns>Tuple<int,int>=IJ;List<double>={MinX,MinY,MaxX,MaxY}</returns>
        public Dictionary<Tuple<int, int>, List<double>> GetGrid(double[] ENVELOPE, double[] gridXY, ref int colNum, ref int rowNum)
        {
            Dictionary<Tuple<int, int>, List<double>> Grids = new Dictionary<Tuple<int, int>, List<double>>();

            #region 获取格网横向纵向间隔
            double gridX = gridXY[0];
            double gridY = gridXY[1];
            #endregion

            #region 向上下左右四个方向扩展一下边界距离；计算边界
            double minX = ENVELOPE[0];
            double minY = ENVELOPE[1];
            double maxX = ENVELOPE[2];
            double maxY = ENVELOPE[3];

            minX = minX - (gridX / 2);
            maxX = maxX + (gridX / 2);
            minY = minY - (gridY / 2);
            maxY = maxY + (gridY / 2);
            #endregion

            #region 计算栅格索引行、列数目
            colNum = (int)(Math.Abs(maxX - minX) / gridX) + (isDivide(Math.Abs(maxX - minX), gridX) == true ? 0 : 1);//列数
            rowNum = (int)(Math.Abs(maxY - minY) / gridY) + (isDivide(Math.Abs(maxY - minY), gridY) == true ? 0 : 1);//行数
            #endregion

            #region 返回每一个行列的范围
            for (int i = 0; i < rowNum; i++)
            {
                for (int j = 0; j < colNum; j++)
                {
                    List<double> ExtendIJ = new List<double>();
                    double MinXIJ = j * gridX + minX;
                    double MinYIJ = i * gridY + minY;
                    double MaxXIJ = (j + 1) * gridX + minX;
                    double MaxYIJ = (i + 1) * gridY + minY;
                    ExtendIJ.Add(MinXIJ); ExtendIJ.Add(MinYIJ); ExtendIJ.Add(MaxXIJ); ExtendIJ.Add(MaxYIJ);

                    Tuple<int, int> IJ = new Tuple<int, int>(i, j);
                    Grids.Add(IJ, ExtendIJ);
                }
            }
            #endregion

            return Grids;
        }

        /// <summary>
        /// 计算获取不同区域的数据类型(只区分Land和非Land)
        /// </summary>
        /// <param name="?"></param>
        /// <param name="ObstacleFeatures"></param>
        /// <returns>0表示Land；1表示非Land</returns>
        public Dictionary<Tuple<int, int>, int> GetGridType(Dictionary<Tuple<int, int>, List<double>> Grids, List<Tuple<IGeometry, esriGeometryType>> ObstacleFeatures, double IntersectTd)
        {
            Dictionary<Tuple<int,int>,int> GridType=new Dictionary<Tuple<int,int>,int>();

            #region 判断过程
            foreach (KeyValuePair<Tuple<int, int>, List<double>> kv in Grids)
            {
                #region 网格范围
                Ring ring1 = new RingClass();
                object missing = Type.Missing;

                IPoint curResultPoint1 = new PointClass();
                IPoint curResultPoint2 = new PointClass();
                IPoint curResultPoint3 = new PointClass();
                IPoint curResultPoint4 = new PointClass();

                curResultPoint1.PutCoords(kv.Value[0], kv.Value[1]);
                curResultPoint2.PutCoords(kv.Value[2], kv.Value[1]);
                curResultPoint3.PutCoords(kv.Value[2], kv.Value[3]);
                curResultPoint4.PutCoords(kv.Value[0], kv.Value[3]);

                ring1.AddPoint(curResultPoint1, ref missing, ref missing);
                ring1.AddPoint(curResultPoint4, ref missing, ref missing);
                ring1.AddPoint(curResultPoint3, ref missing, ref missing);
                ring1.AddPoint(curResultPoint2, ref missing, ref missing);
                ring1.AddPoint(curResultPoint1, ref missing, ref missing);

                IGeometryCollection pointPolygon = new PolygonClass();
                pointPolygon.AddGeometry(ring1 as IGeometry, ref missing, ref missing);
                IPolygon pPolygon = pointPolygon as IPolygon;

                IArea pArea = pPolygon as IArea;
                //double dpArea = pArea.Area;
                #endregion

                ITopologicalOperator iTo = pPolygon as ITopologicalOperator;

                #region 判断Land区域交叉
                 bool GridValid = false;
                 if (ObstacleFeatures.Count > 0)
                 {
                     foreach (Tuple<IGeometry, esriGeometryType> pFeature in ObstacleFeatures)
                     {
                         if (pFeature.Item2 == esriGeometryType.esriGeometryPolygon)
                         {
                             IGeometry IGeo = iTo.Intersect(pFeature.Item1, esriGeometryDimension.esriGeometry2Dimension);
                             {
                                 if (!IGeo.IsEmpty)
                                 {
                                     IArea gArea = IGeo as IArea;
                                     //double dgArea = gArea.Area;
                                     //double Cachek = Math.Abs(gArea.Area) / Math.Abs(pArea.Area);

                                     if (Math.Abs(gArea.Area) / Math.Abs(pArea.Area) > IntersectTd)
                                     {
                                         GridValid = true;
                                     }
                                 }
                             }
                         }
                     }
                 }

                 #region 交叉大于一定比例才是Land
                 if (GridValid)
                 {
                     GridType.Add(kv.Key, 0);
                 }

                 else
                 {
                     GridType.Add(kv.Key, 10);
                 }
                 #endregion
                #endregion
            }
            #endregion

            return GridType;
        }
        /// <summary>
        /// 针对制定范围构建索引
        /// </summary>
        /// <param name="ENVELOPE"></param> [MinX,MinY,MaxX,MaxY]【左下角往右上角编码】
        /// <param name="gridXY"></param>[格网横向、纵向间隔]
        /// <param name="colNum"></param>[输出列]
        /// <param name="rowNum"></param>[输出行]
        /// 备注：i标识行数；j标识列数
        /// <returns>Tuple<int,int>=IJ;List<double>={MinX,MinY,MaxX,MaxY}</returns>
        public Dictionary<Tuple<int, int>, List<double>> GetGridConObstacle(double[] ENVELOPE, double[] gridXY, List<Tuple<IGeometry,esriGeometryType>> ObstacleFeatures,ref int colNum, ref int rowNum,double IntersectTd)
        {
            Dictionary<Tuple<int, int>, List<double>> Grids = new Dictionary<Tuple<int, int>, List<double>>();

            #region 获取格网横向纵向间隔
            double gridX = gridXY[0];
            double gridY = gridXY[1];
            #endregion

            #region 向上下左右四个方向扩展一下边界距离；计算边界
            double minX = ENVELOPE[0];
            double minY = ENVELOPE[1];
            double maxX = ENVELOPE[2];
            double maxY = ENVELOPE[3];

            minX = minX - (gridX / 2);
            maxX = maxX + (gridX / 2);
            minY = minY - (gridY / 2);
            maxY = maxY + (gridY / 2);
            #endregion

            #region 计算栅格索引行、列数目
            colNum = (int)(Math.Abs(maxX - minX) / gridX) + (isDivide(Math.Abs(maxX - minX), gridX) == true ? 0 : 1);//列数
            rowNum = (int)(Math.Abs(maxY - minY) / gridY) + (isDivide(Math.Abs(maxY - minY), gridY) == true ? 0 : 1);//行数
            #endregion

            #region 返回每一个行列的范围
            for (int i = 0; i < rowNum; i++)
            {
                for (int j = 0; j < colNum; j++)
                {
                    #region 网格范围
                    List<double> ExtendIJ = new List<double>();
                    double MinXIJ = j * gridX + minX;
                    double MinYIJ = i * gridY + minY;
                    double MaxXIJ = (j + 1) * gridX + minX;
                    double MaxYIJ = (i + 1) * gridY + minY;
                    ExtendIJ.Add(MinXIJ); ExtendIJ.Add(MinYIJ); ExtendIJ.Add(MaxXIJ); ExtendIJ.Add(MaxYIJ);                
                    #endregion

                    bool GridValid = false;
                    if (ObstacleFeatures.Count > 0)
                    {
                        foreach (Tuple<IGeometry, esriGeometryType> pFeature in ObstacleFeatures)
                        {
                            #region 网格范围
                            Ring ring1 = new RingClass();
                            object missing = Type.Missing;

                            IPoint curResultPoint1 = new PointClass();
                            IPoint curResultPoint2 = new PointClass();
                            IPoint curResultPoint3 = new PointClass();
                            IPoint curResultPoint4 = new PointClass();
                            
                            curResultPoint1.PutCoords(MinXIJ, MinYIJ);
                            curResultPoint2.PutCoords(MaxXIJ, MinYIJ);
                            curResultPoint3.PutCoords(MaxXIJ, MaxYIJ);
                            curResultPoint4.PutCoords(MinXIJ, MaxYIJ);
                           
                            ring1.AddPoint(curResultPoint1, ref missing, ref missing);
                            ring1.AddPoint(curResultPoint4, ref missing, ref missing);
                            ring1.AddPoint(curResultPoint3, ref missing, ref missing);
                            ring1.AddPoint(curResultPoint2, ref missing, ref missing);
                            ring1.AddPoint(curResultPoint1, ref missing, ref missing);

                            IGeometryCollection pointPolygon = new PolygonClass();
                            pointPolygon.AddGeometry(ring1 as IGeometry, ref missing, ref missing);
                            IPolygon pPolygon = pointPolygon as IPolygon;

                            IArea pArea = pPolygon as IArea;
                            //double dpArea = pArea.Area;
                            #endregion

                            ITopologicalOperator iTo = pPolygon as ITopologicalOperator;

                            #region 判断交叉
                            #region 点状障碍物
                            if (pFeature.Item2 == esriGeometryType.esriGeometryPoint)
                            {
                                IGeometry IGeo = iTo.Intersect(pFeature.Item1, esriGeometryDimension.esriGeometry0Dimension);
                                {
                                    if (!IGeo.IsEmpty)
                                    {
                                        GridValid = true;
                                    }
                                }
                            }
                            #endregion

                            #region 线状障碍物
                            if (pFeature.Item2 == esriGeometryType.esriGeometryPolyline)
                            {
                                IGeometry IGeo = iTo.Intersect(pFeature.Item1, esriGeometryDimension.esriGeometry1Dimension);
                                {
                                    if (!IGeo.IsEmpty)
                                    {
                                        GridValid = true;
                                    }
                                }
                            }
                            #endregion

                            #region 面状障碍物
                            if (pFeature.Item2 == esriGeometryType.esriGeometryPolygon)
                            {
                                IGeometry IGeo = iTo.Intersect(pFeature.Item1, esriGeometryDimension.esriGeometry2Dimension);
                                {
                                    if (!IGeo.IsEmpty)
                                    {
                                        IArea gArea = IGeo as IArea;
                                        //double dgArea = gArea.Area;
                                        double Cachek = Math.Abs(gArea.Area) / Math.Abs(pArea.Area);

                                        if (Math.Abs(gArea.Area) / Math.Abs(pArea.Area) > IntersectTd)
                                        {
                                            GridValid = true;
                                        }
                                    }
                                }
                            }
                            #endregion
                            #endregion
                        }
                    }

                    if (!GridValid)
                    {
                        Tuple<int, int> IJ = new Tuple<int, int>(i, j);
                        Grids.Add(IJ, ExtendIJ);
                    }
                }
            }
            #endregion

            return Grids;
        }

        /// <summary>
        /// 针对制定范围构建索引
        /// </summary>
        /// <param name="ENVELOPE"></param> [MinX,MinY,MaxX,MaxY]
        /// <param name="colNum"></param>[列]
        /// <param name="rowNum"></param>[行]
        /// <returns>Tuple<int,int>=IJ;List<double>={MinX,MinY,MaxX,MaxY}</returns>
        public Dictionary<Tuple<int, int>, List<double>> GetGrid(double[] ENVELOPE, int colNum, int rowNum)
        {
            Dictionary<Tuple<int, int>, List<double>> Grids = new Dictionary<Tuple<int, int>, List<double>>();

            #region 向上下左右四个方向扩展一下边界距离；计算边界
            double minX = ENVELOPE[0];
            double maxX = ENVELOPE[1];
            double minY = ENVELOPE[2];
            double maxY = ENVELOPE[3];

            minX = minX - (maxX - minX) / (2 * colNum);
            maxX = maxX + (maxX - minX) / (2 * colNum);
            minY = minY - (maxY - minX) / (2 * rowNum);
            maxY = maxY + (maxY - minY) / (2 * rowNum);
            #endregion

            double gridX = (maxX - minX) / colNum;
            double gridY = (maxY - minY) / rowNum;
            #region 返回每一个行列的范围
            for (int i = 0; i < rowNum; i++)
            {
                for (int j = 0; j < colNum; j++)
                {
                    List<double> ExtendIJ = new List<double>();
                    double MinXIJ = j * gridX + minX;
                    double MinYIJ = i * gridY + minY;
                    double MaxXIJ = (j + 1) * gridX + minX;
                    double MaxYIJ = (i + 1) * gridY + minY;
                    ExtendIJ.Add(MinXIJ); ExtendIJ.Add(MinYIJ); ExtendIJ.Add(MaxXIJ); ExtendIJ.Add(MaxYIJ);

                    Tuple<int, int> IJ = new Tuple<int, int>(i, j);
                    Grids.Add(IJ, ExtendIJ);
                }
            }
            #endregion

            return Grids;
        }

        /// <summary>
        /// 两个双精度型数字是否整除
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool isDivide(double a, double b)
        {
            double d = a % b;
            const double epsilon = 1.0e-6;
            if (d < epsilon)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 获取给定点所在的索引编号
        /// </summary>
        /// <param name="Grids"></param>
        /// <param name="PointList"></param>
        /// <returns></returns>
        public Dictionary<IPoint,Tuple<int,int>> GetNodeInGrid(Dictionary<Tuple<int,int>,List<double>> Grids,List<IPoint> PointList)
        {
            Dictionary<IPoint,Tuple<int,int>> PointIJ=new Dictionary<IPoint,Tuple<int,int>>();

            #region 判断过程
            foreach (KeyValuePair<Tuple<int, int>, List<double>> Kv in Grids)
            {
                for (int i = 0; i < PointList.Count; i++)
                {
                    double Point_X = PointList[i].X;
                    double Point_Y = PointList[i].Y;

                    if (Point_X > Kv.Value[0] && Point_Y > Kv.Value[1] && Point_X < Kv.Value[2] && Point_Y < Kv.Value[3])
                    {
                        PointIJ.Add(PointList[i], Kv.Key);
                    }
                }
            }
            #endregion

            return PointIJ;
        }

         /// <summary>
        /// 获取给定点所在的索引编号(备注：这里每一个IPoint，都最多只对应一个Grid)
        /// </summary>
        /// <param name="Grids"></param>
        /// <param name="PointList"></param>
        /// <returns></returns>
        public Dictionary<Tuple<int,int>,IPoint> GetGridContainNodes(Dictionary<Tuple<int,int>,List<double>> Grids,List<IPoint> PointList)
        {
            Dictionary<Tuple<int,int>,IPoint> PointIJ=new Dictionary<Tuple<int,int>,IPoint>();

            #region 判断过程
            foreach (KeyValuePair<Tuple<int, int>, List<double>> Kv in Grids)
            {
                for (int i = 0; i < PointList.Count; i++)
                {
                    double Point_X = PointList[i].X;
                    double Point_Y = PointList[i].Y;

                    if (Point_X > Kv.Value[0] && Point_Y > Kv.Value[1] && Point_X < Kv.Value[2] && Point_Y < Kv.Value[3])
                    {
                        PointIJ.Add(Kv.Key,PointList[i]);
                        break;//每一个Grid都最多只对应一个Grid
                    }
                }
            }
            #endregion

            return PointIJ;
        }

        /// <summary>
        /// 构建约束性的格网
        /// </summary>
        /// <param name="TargetGrids">规则格网</param>
        /// <param name="GeometryList">约束性目标集合[点、线、面的Geometry]</param>
        /// <returns></returns>
        public Dictionary<Tuple<int, int>, List<double>> GetConstrainedGrids(Dictionary<Tuple<int, int>, List<double>> TargetGrids,List<IGeometry> GeometryList)
        {
            Dictionary<Tuple<int, int>, List<double>> ConstrainedGrids = new Dictionary<Tuple<int, int>, List<double>>();

            #region 判断过程
            foreach (KeyValuePair<Tuple<int, int>, List<double>> Kv in TargetGrids)
            {
                Envelope CacheEnve = new Envelope();
                CacheEnve.XMin = Kv.Value[0]; CacheEnve.YMin = Kv.Value[1]; CacheEnve.XMax = CacheEnve.XMax; CacheEnve.YMax = CacheEnve.YMax;
                IRelationalOperator iRo = CacheEnve as IRelationalOperator;

                bool Intersect = false;
                for (int i = 0; i < GeometryList.Count; i++)
                {
                    if(iRo.Disjoint(GeometryList[i]))
                    {
                        Intersect = true;
                        break;
                    }
                }

                if (!Intersect)
                {
                    ConstrainedGrids.Add(Kv.Key, Kv.Value);
                }
            }
            #endregion

            return ConstrainedGrids;
        }

        /// <summary>
        /// 获得包含权重的格网
        /// </summary>
        /// <param name="TargetGrids"></param>
        /// <param name="PointInGrids"></param>
        /// <param name="PointFlow"></param>
        /// <param name="NearT"></param>
        /// Type=1邻域范围内的总数；邻域范围内的平均值
        /// <returns></returns>
        public Dictionary<Tuple<int, int>, double> GetWeighGrid(Dictionary<Tuple<int, int>, List<double>> TargetGrids, Dictionary<Tuple<int, int>,IPoint> PointInGrids,Dictionary<IPoint,double> PointFlow, int NearT,int Type)
        {
            Dictionary<Tuple<int,int>,double> WeighGrids=new Dictionary<Tuple<int,int>,double>();

            foreach (KeyValuePair<Tuple<int,int>,List<double>> kv in TargetGrids)
            {
                List<Tuple<int, int>> NearGrids = this.GetNearGrids(kv.Key, TargetGrids.Keys.ToList(), NearT);

                double SumFlow = 0;
                for (int i = 0; i < NearGrids.Count; i++)
                {
                    if (PointInGrids.Keys.Contains(NearGrids[i]))
                    {
                        SumFlow = SumFlow + PointFlow[PointInGrids[NearGrids[i]]];
                    }            
                }

                double Weigth = 0;
                if (Type == 1)
                {
                    Weigth = SumFlow;
                }

                else if (Type == 2)
                {
                    Weigth = SumFlow / NearGrids.Count;
                }

                WeighGrids.Add(kv.Key, Weigth);
            }

            return WeighGrids;
        }

        /// <summary>
        /// 获取给定Grid的k阶邻近（邻近要素包含了自身）
        /// </summary>
        /// <param name="TargetGrid">目标Grid</param>
        /// <param name="Grids">格网</param>
        /// <param name="NearT">k阶邻近</param>
        /// <returns></returns>
        public List<Tuple<int, int>> GetNearGrids(Tuple<int, int> TargetGrid, List<Tuple<int, int>> Grids,int NearT)
        {
            List<Tuple<int, int>> NearGrids = new List<Tuple<int, int>>();

            #region 判断过程(n阶表示2*N的邻近) 
            for (int i = -NearT + TargetGrid.Item1; i < NearT + TargetGrid.Item1 + 1; i++)
            {
                for (int j = -NearT + TargetGrid.Item2; j < NearT + TargetGrid.Item2 + 1; j++)
                {
                    Tuple<int, int> CacheGrid = new Tuple<int, int>(i, j);
                    if (Grids.Contains(CacheGrid))
                    {
                        NearGrids.Add(CacheGrid);
                    }
                }
            }
            #endregion

            return NearGrids;
        }

        /// <summary>
        /// 获得DEM内插横纵间隔的长宽
        /// </summary>
        /// <param name="PointList">点集</param>
        /// Type 计算纵横间隔的方法
        /// Type=1,计算前5%距离值的一半
        /// Type=2，计算前5%距离值的1/4
        /// <returns></returns>
        public double[] GetXY(List<IPoint> PointList, int Type)
        {
            double[] XYLength = new double[2];

            #region 计算距离值
            List<double> DistanceList = new List<double>();
            for (int i = 0; i < PointList.Count; i++)
            {
                for (int j = 0; j < PointList.Count; j++)
                {
                    if (i != j)
                    {
                        DistanceList.Add(this.GetDis(PointList[i], PointList[j]));
                    }
                }
            }

            DistanceList.Sort();
            #endregion
            //DistanceList.Reverse();

            #region 获取前5%距离的平均值的一半
            if (Type == 1)
            {
                List<double> largerList = DistanceList.Take(Convert.ToInt16(Math.Ceiling(DistanceList.Count * 0.05))).ToList<double>();
                double PixelSize = largerList.Sum() / largerList.Count;

                XYLength[0] = PixelSize / 2; XYLength[1] = PixelSize / 2;
            }
            #endregion

            #region 获取前5%距离的平均值的四分之一
            if (Type == 2)
            {
                List<double> largerList = DistanceList.Take(Convert.ToInt16(Math.Ceiling(DistanceList.Count * 0.05))).ToList<double>();
                double PixelSize = largerList.Sum() / largerList.Count;

                XYLength[0] = PixelSize / 4; XYLength[1] = PixelSize / 4;
            }


            #endregion

            return XYLength;         
        }

        /// <summary>
        /// 计算给定两个点之间的距离
        /// </summary>
        /// <param name="Point1"></param>
        /// <param name="Point2"></param>
        /// <returns></returns>
        public double GetDis(IPoint Point1, IPoint Point2)
        {
            return Math.Sqrt((Point1.X - Point2.X) * (Point1.X - Point2.X) + (Point1.Y - Point2.Y) * (Point1.Y - Point2.Y));
        }

        /// <summary>
        /// 获取每一个给定点处的内插高程
        /// </summary>
        /// <param name="OriginPoint"></param>
        /// <param name="TargetPoint"></param>
        /// <param name="OtherPoints"></param>
        /// <param name="InterType"></param> 指数衰减
        /// <returns></returns>
        public double GetElv(IPoint OriginPoint,IPoint TargetPoint,List<IPoint> OtherPoints,List<double> PointsElv, int InterType)
        {
            double Elv = 0;

            #region 计算过程
            if (InterType == 0)
            {
                for (int i = 0; i < OtherPoints.Count; i++)
                {
                    double tDis = Math.Sqrt((TargetPoint.X - OtherPoints[i].X) * (TargetPoint.X - OtherPoints[i].X) +
                        (TargetPoint.Y - OtherPoints[i].Y) * (TargetPoint.Y - OtherPoints[i].Y));

                    //double x1 = OriginPoint.X; double x2 = OtherPoints[i].X;
                    //double y1 = OriginPoint.Y; double y2 = OtherPoints[i].Y;

                    double oDis = Math.Sqrt((OriginPoint.X - OtherPoints[i].X) * (OriginPoint.X - OtherPoints[i].X) +
                        (OriginPoint.Y - OtherPoints[i].Y) * (OriginPoint.Y - OtherPoints[i].Y));

                    double CacheElv = 0;
                    if (tDis < oDis)
                    {
                        CacheElv = PointsElv[i] * Math.Exp(1-oDis / (oDis - tDis));
                    }

                    Elv = Elv + CacheElv;
                }
            }
            #endregion

            return Elv;
        }
    }
}
