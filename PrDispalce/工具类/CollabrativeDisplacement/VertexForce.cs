using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrDispalce.工具类.CollabrativeDisplacement
{
    /// <summary>
    /// 描述一个顶点的受力
    /// </summary>
    public class VertexForce
    {
        public int ID = -1;//顶点号
        public List<Force> forceList = null;//受力列表

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id"></param>
        public VertexForce(int id)
        {
            this.ID = id;
            this.forceList = new List<Force>();
        }

        /// <summary>
        /// 计算最终的受力
        /// </summary>
        /// <returns></returns>
        public Force CalResultantForce()
        {
            return null;
        }
    }
}
