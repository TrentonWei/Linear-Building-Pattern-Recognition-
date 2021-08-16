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
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.Geoprocessing;

namespace PrDispalce.FlowMap
{
    /// <summary>
    /// BezierCurve曲线（二次和三次）
    /// </summary>
    class BezierCurve
    {
        List<IPoint> ControlPoint = new List<IPoint>();//控制点
        public List<IPoint> CurvePoint = new List<IPoint>();//贝塞尔曲线点

        /// <summary>
        /// 构造函数
        /// </summary>
        public BezierCurve()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="FPoint"></param>
        /// <param name="TPoint"></param>
        /// <param name="CPoint"></param>
        public BezierCurve( List<IPoint> CPointList)
        {
            this.ControlPoint = CPointList;
        }

        /// <summary>
        /// 生成beziercurve(二次曲线插值)
        /// Number表示内插点的数量(Number>=2)
        /// </summary>
        public void Curve2Generate(int Number)
        {
            for (double i = 0; i < 1.1; i = i + 0.1)
            {
                IPoint CachePoint = new PointClass();
                CachePoint.X = (1 - i) * (1 - i) * ControlPoint[0].X + 2 * i * (1 - i) * ControlPoint[0].X + i * i * ControlPoint[1].X;
                CachePoint.Y = (1 - i) * (1 - i) * ControlPoint[0].Y + 2 * i * (1 - i) * ControlPoint[0].Y + i * i * ControlPoint[1].Y;

                CurvePoint.Add(CachePoint);
            }
        }

        /// <summary>
        /// 生成beziercurve(三次曲线插值)
        /// Number表示内插点的数量(Number>=2)
        /// </summary>
        public void Curve3Generate(int Number)
        {
            for (double i = 0; i < 1.01; i = i + 1 / Number)
            {
                IPoint CachePoint = new PointClass();

                CachePoint.X = (1 - i) * (1 - i) * (1 - i) * ControlPoint[0].X + 3 * i * (1 - i) * (1 - i) * ControlPoint[1].X + 3 * i * i * (1 - i) * ControlPoint[1].X + i * i * i * ControlPoint[1].X;
                CachePoint.Y = (1 - i) * (1 - i) * (1 - i) * ControlPoint[0].Y + 3 * i * (1 - i) * (1 - i) * ControlPoint[1].Y + 3 * i * i * (1 - i) * ControlPoint[1].Y + i * i * i * ControlPoint[1].Y;
                CurvePoint.Add(CachePoint);
            }
        }

        /// <summary>
        /// n阶biezer曲线
        /// Number表示内插点的数量(Number>=2)
        /// </summary>
        public void CurveNGenerate(int Number)
        {
            int ControlNodeCount = ControlPoint.Count;//控制点个数
            double[,] poss=new double[ControlNodeCount,2];//控制点坐标
            double[] result=new double[2];//输出的x，y结果

            #region 获取控制点坐标
            for (int i = 0; i < ControlNodeCount; i++)
            {
                poss[i, 0] = ControlPoint[i].X;
                poss[i, 1] = ControlPoint[i].Y;
            }
            #endregion

            #region 计算杨辉三角
            int[] mi = new int[ControlNodeCount];
            mi[0] = mi[1] = 1;
            for (int i = 3; i <= ControlNodeCount; i++)
            {
                int[] t = new int[i - 1];
                for (int j = 0; j < t.Length; j++)
                {
                    t[j] = mi[j];
                }

                mi[0] = mi[i - 1] = 1;
                for (int j = 0; j < i - 2; j++)
                {
                    mi[j + 1] = t[j] + t[j + 1];
                }
            }
            #endregion

            #region 计算坐标点
            for (int i = 0; i < Number; i++)
            {        
                double t = (double)i / Number;
                IPoint CachePoint = new PointClass();
                double X = 0; double Y = 0;

                for (int j = 0; j < ControlNodeCount; j++)
                {                    
                    X += Math.Pow(1 - t, ControlNodeCount - j - 1) * poss[j, 0] * Math.Pow(t, j) * mi[j];
                    Y += Math.Pow(1 - t, ControlNodeCount - j - 1) * poss[j, 1] * Math.Pow(t, j) * mi[j];
                }

                CachePoint.X = X; CachePoint.Y = Y;
                CurvePoint.Add(CachePoint);
            }

            CurvePoint.Add(ControlPoint[ControlPoint.Count - 1]);
            #endregion
        }

        /// <summary>
        /// n阶biezer曲线
        /// Number表示内插点的数量(Number>=2)
        /// </summary>
        public void CurveNGenerate(int Number,double MoveDis)
        {
            #region 判断ControlPoint是否在同一条直线上
            bool OnLine = true;
            if (ControlPoint.Count >= 3)//若控制点数量大于3
            {
                double k = (ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) / (ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X);
                for (int i = 0; i < ControlPoint.Count - 1; i++)
                {
                    double Cachek = (ControlPoint[i].Y - ControlPoint[0].Y) / (ControlPoint[i].X - ControlPoint[0].X);

                    if (Math.Abs(Cachek - k) > 0.01)
                    {
                        OnLine = false;
                    }
                }
            }

            ///如果控制点共线（控制点只有两个点）
            if (OnLine)
            {
                IPoint CachePoint = new PointClass();
                double X0 = 0;
                double Y0 = 0;

                if ((ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) < 0.00001)
                {
                    double Dis = Math.Sqrt((ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) * (ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) + (ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X) * (ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X));
                    double MidX = ControlPoint[ControlPoint.Count - 1].X * 0.75 + ControlPoint[0].X * 0.25;//四等分点

                    X0 = MidX;
                    Y0 = ControlPoint[0].Y + Dis * MoveDis;
                }

                else if ((ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X) < 0.00001)
                {
                    double Dis = Math.Sqrt((ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) * (ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) + (ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X) * (ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X));
                    double MidY = ControlPoint[ControlPoint.Count - 1].Y * 0.75 + ControlPoint[0].Y * 0.25;//四等分点

                    X0 = ControlPoint[0].X + Dis * MoveDis;
                    Y0 = MidY;

                    CachePoint.X = ControlPoint[0].X;
                    CachePoint.Y = Y0;
                }

                else
                {
                    double Dis = Math.Sqrt((ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) * (ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) + (ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X) * (ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X));
                    double k = (ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) / (ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X);

                    double MidX = ControlPoint[ControlPoint.Count - 1].X * 0.75 + ControlPoint[0].X * 0.25;//四等分点
                    double MidY = ControlPoint[ControlPoint.Count - 1].Y * 0.75 + ControlPoint[0].Y * 0.25;//四等分点

                    X0 = MidX + Dis * (1 / Math.Sqrt(1 + k * k)) * MoveDis;
                    Y0 = MidY - Dis * (k / Math.Sqrt(1 + k * k)) * MoveDis;
                }

                CachePoint.X = X0;
                CachePoint.Y = Y0;    
                IPoint sPoint = ControlPoint[0];
                IPoint ePoint = ControlPoint[ControlPoint.Count - 1];
                ControlPoint.RemoveRange(0, ControlPoint.Count);
                ControlPoint.Add(sPoint);
                ControlPoint.Add(CachePoint);
                ControlPoint.Add(ePoint);
            }
            #endregion

            int ControlNodeCount = ControlPoint.Count;//控制点个数
            double[,] poss = new double[ControlNodeCount, 2];//控制点坐标
            double[] result = new double[2];//输出的x，y结果

            #region 获取控制点坐标
            for (int i = 0; i < ControlNodeCount; i++)
            {
                poss[i, 0] = ControlPoint[i].X;
                poss[i, 1] = ControlPoint[i].Y;
            }
            #endregion

            #region 计算杨辉三角
            int[] mi = new int[ControlNodeCount];
            mi[0] = mi[1] = 1;
            for (int i = 3; i <= ControlNodeCount; i++)
            {
                int[] t = new int[i - 1];
                for (int j = 0; j < t.Length; j++)
                {
                    t[j] = mi[j];
                }

                mi[0] = mi[i - 1] = 1;
                for (int j = 0; j < i - 2; j++)
                {
                    mi[j + 1] = t[j] + t[j + 1];
                }
            }
            #endregion

            #region 计算坐标点
            for (int i = 0; i < Number; i++)
            {
                double t = (double)i / Number;
                IPoint CachePoint = new PointClass();
                double X = 0; double Y = 0;

                for (int j = 0; j < ControlNodeCount; j++)
                {
                    X += Math.Pow(1 - t, ControlNodeCount - j - 1) * poss[j, 0] * Math.Pow(t, j) * mi[j];
                    Y += Math.Pow(1 - t, ControlNodeCount - j - 1) * poss[j, 1] * Math.Pow(t, j) * mi[j];
                }

                CachePoint.X = X; CachePoint.Y = Y;
                CurvePoint.Add(CachePoint);
            }

            CurvePoint.Add(ControlPoint[ControlPoint.Count-1]);
            #endregion
        }

        /// <summary>
        /// n阶biezer曲线
        /// Number表示内插点的数量(Number>=2)
        /// Type=0 X向右下移动；Type=1 X右上移动；Type=2 X向左下移动；Type=3 X向右上移动；Type=4 处理直线连接
        /// </summary>
        public void CurveNGenerate(int Number, double MoveDis,int Type)
        {
            #region 判断ControlPoint是否在同一条直线上
            bool OnLine = true;
            if (ControlPoint.Count >= 3)//若控制点数量大于3
            {
                double k = (ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) / (ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X);
                for (int i = 0; i < ControlPoint.Count - 1; i++)
                {
                    double Cachek = (ControlPoint[i].Y - ControlPoint[0].Y) / (ControlPoint[i].X - ControlPoint[0].X);

                    if (Math.Abs(Cachek - k) > 0.01)
                    {
                        OnLine = false;
                    }
                }
            }

            ///如果控制点共线（控制点只有两个点）
            #region 处理共线
            if (OnLine)
            {
                IPoint CachePoint = new PointClass();
              
                if (Type != 4)
                { 
                    double X0 = 0;
                    double Y0 = 0;

                    double Dis = Math.Sqrt((ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) * (ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) + (ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X) * (ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X));
                    #region 右下移动
                    if (Type == 0)
                    {
                        if ((ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) < Dis*0.1)//Y共线
                        {                            
                            double MidX = ControlPoint[ControlPoint.Count - 1].X * 0.75 + ControlPoint[0].X * 0.25;//四等分点

                            X0 = MidX;
                            Y0 = ControlPoint[0].Y - Dis * MoveDis;
                        }

                        else if ((ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X) < Dis * 0.1)//X共线
                        {                          
                            double MidY = ControlPoint[ControlPoint.Count - 1].Y * 0.75 + ControlPoint[0].Y * 0.25;//四等分点

                            X0 = ControlPoint[0].X + Dis * MoveDis;
                            Y0 = MidY;

                            //CachePoint.X = ControlPoint[0].X;
                            //CachePoint.Y = Y0;
                        }

                        else//其它
                        {                          
                            double k = (ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) / (ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X);

                            double MidX = ControlPoint[ControlPoint.Count - 1].X * 0.75 + ControlPoint[0].X * 0.25;//四等分点
                            double MidY = ControlPoint[ControlPoint.Count - 1].Y * 0.75 + ControlPoint[0].Y * 0.25;//四等分点

                            //X0 = MidX + Dis * (1 / Math.Sqrt(1 + k * k)) * MoveDis;
                            //Y0 = MidY - Dis * (k / Math.Sqrt(1 + k * k)) * MoveDis;

                            X0 = MidX + Dis * (Math.Abs(1 / Math.Sqrt(1 + k * k))) * MoveDis;
                            //Y0 = MidY;
                            Y0 = MidY - Dis * (Math.Abs(k / Math.Sqrt(1 + k * k))) * MoveDis;
                        }
                    }
                    #endregion

                    #region 右上移动
                    else if (Type == 1)
                    {
                        if ((ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) < Dis * 0.1)//Y共线
                        {
                           double MidX = ControlPoint[ControlPoint.Count - 1].X * 0.75 + ControlPoint[0].X * 0.25;//四等分点

                            X0 = MidX;
                            Y0 = ControlPoint[0].Y + Dis * MoveDis;
                        }

                        else if ((ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X) < Dis * 0.1)//X共线
                        {                            
                            double MidY = ControlPoint[ControlPoint.Count - 1].Y * 0.75 + ControlPoint[0].Y * 0.25;//四等分点

                            X0 = ControlPoint[0].X + Dis * MoveDis;
                            Y0 = MidY;

                            //CachePoint.X = ControlPoint[0].X;
                            //CachePoint.Y = Y0;
                        }

                        else//其它
                        {
                            double k = (ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) / (ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X);

                            double MidX = ControlPoint[ControlPoint.Count - 1].X * 0.75 + ControlPoint[0].X * 0.25;//四等分点
                            double MidY = ControlPoint[ControlPoint.Count - 1].Y * 0.75 + ControlPoint[0].Y * 0.25;//四等分点

                            X0 = MidX + Dis * (Math.Abs(1 / Math.Sqrt(1 + k * k))) * MoveDis;
                            //Y0 = MidY;
                            Y0 = MidY + Dis * (Math.Abs(k / Math.Sqrt(1 + k * k))) * MoveDis;
                        }
                    }
                    #endregion

                    #region 左下移动
                    else if (Type == 2)
                    {
                        if ((ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) < Dis * 0.1)//Y共线
                        {                         
                            double MidX = ControlPoint[ControlPoint.Count - 1].X * 0.75 + ControlPoint[0].X * 0.25;//四等分点

                            X0 = MidX;
                            Y0 = ControlPoint[0].Y - Dis * MoveDis;
                        }

                        else if ((ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X) < Dis * 0.1)//X共线
                        {                          
                            double MidY = ControlPoint[ControlPoint.Count - 1].Y * 0.75 + ControlPoint[0].Y * 0.25;//四等分点

                            X0 = ControlPoint[0].X - Dis * MoveDis;
                            Y0 = MidY;

                            //CachePoint.X = ControlPoint[0].X;
                            //CachePoint.Y = Y0;
                        }

                        else//其它
                        {                            
                            double k = (ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) / (ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X);

                            double MidX = ControlPoint[ControlPoint.Count - 1].X * 0.75 + ControlPoint[0].X * 0.25;//四等分点
                            double MidY = ControlPoint[ControlPoint.Count - 1].Y * 0.75 + ControlPoint[0].Y * 0.25;//四等分点

                            X0 = MidX - Dis * (Math.Abs(1 / Math.Sqrt(1 + k * k))) * MoveDis;
                            //Y0 = MidY;
                            Y0 = MidY - Dis * (Math.Abs(k / Math.Sqrt(1 + k * k))) * MoveDis;
                        }
                    }
                    #endregion

                    #region 左上移动
                    else if (Type == 3)
                    {
                        if ((ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) < Dis * 0.1)//Y共线
                        {                         
                            double MidX = ControlPoint[ControlPoint.Count - 1].X * 0.75 + ControlPoint[0].X * 0.25;//四等分点

                            X0 = MidX;
                            Y0 = ControlPoint[0].Y + Dis * MoveDis;
                        }

                        else if ((ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X) < Dis * 0.1)//X共线
                        {                            
                            double MidY = ControlPoint[ControlPoint.Count - 1].Y * 0.75 + ControlPoint[0].Y * 0.25;//四等分点

                            X0 = ControlPoint[0].X - Dis * MoveDis;
                            Y0 = MidY;

                            //CachePoint.X = ControlPoint[0].X;
                            //CachePoint.Y = Y0;
                        }

                        else//其它
                        {                           
                            double k = (ControlPoint[ControlPoint.Count - 1].Y - ControlPoint[0].Y) / (ControlPoint[ControlPoint.Count - 1].X - ControlPoint[0].X);

                            double MidX = ControlPoint[ControlPoint.Count - 1].X * 0.75 + ControlPoint[0].X * 0.25;//四等分点
                            double MidY = ControlPoint[ControlPoint.Count - 1].Y * 0.75 + ControlPoint[0].Y * 0.25;//四等分点

                            X0 = MidX - Dis * (Math.Abs(1 / Math.Sqrt(1 + k * k))) * MoveDis;
                            //Y0 = MidY;
                            Y0 = MidY + Dis * (Math.Abs(k / Math.Sqrt(1 + k * k))) * MoveDis;
                        }
                    }
                    #endregion

                    CachePoint.X = X0;
                    CachePoint.Y = Y0;
                    IPoint sPoint = ControlPoint[0];
                    IPoint ePoint = ControlPoint[ControlPoint.Count - 1];
                    ControlPoint.RemoveRange(0, ControlPoint.Count);
                    ControlPoint.Add(sPoint);
                    ControlPoint.Add(CachePoint);
                    ControlPoint.Add(ePoint);
                }
            }
            #endregion
            #endregion

            int ControlNodeCount = ControlPoint.Count;//控制点个数
            double[,] poss = new double[ControlNodeCount, 2];//控制点坐标
            double[] result = new double[2];//输出的x，y结果

            #region 获取控制点坐标
            for (int i = 0; i < ControlNodeCount; i++)
            {
                poss[i, 0] = ControlPoint[i].X;
                poss[i, 1] = ControlPoint[i].Y;
            }
            #endregion

            #region 计算杨辉三角
            int[] mi = new int[ControlNodeCount];
            mi[0] = mi[1] = 1;
            for (int i = 3; i <= ControlNodeCount; i++)
            {
                int[] t = new int[i - 1];
                for (int j = 0; j < t.Length; j++)
                {
                    t[j] = mi[j];
                }

                mi[0] = mi[i - 1] = 1;
                for (int j = 0; j < i - 2; j++)
                {
                    mi[j + 1] = t[j] + t[j + 1];
                }
            }
            #endregion

            #region 计算坐标点
            for (int i = 0; i < Number; i++)
            {
                double t = (double)i / Number;
                IPoint CachePoint = new PointClass();
                double X = 0; double Y = 0;

                for (int j = 0; j < ControlNodeCount; j++)
                {
                    X += Math.Pow(1 - t, ControlNodeCount - j - 1) * poss[j, 0] * Math.Pow(t, j) * mi[j];
                    Y += Math.Pow(1 - t, ControlNodeCount - j - 1) * poss[j, 1] * Math.Pow(t, j) * mi[j];
                }

                CachePoint.X = X; CachePoint.Y = Y;
                CurvePoint.Add(CachePoint);
            }

            if (!CurvePoint.Contains(ControlPoint[ControlPoint.Count - 1]))
            {
                CurvePoint.Add(ControlPoint[ControlPoint.Count - 1]);
            }
            #endregion
        }
    }
}
