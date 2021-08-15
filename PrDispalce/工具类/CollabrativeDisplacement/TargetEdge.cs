using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AuxStructureLib;

namespace PrDispalce.工具类.CollabrativeDisplacement
{
    //用来说明用于处理的冲突边的属性
    class TargetEdge
    {
        public ProxiEdge pTargetEdge = null;
        public bool InitialConflictLabel = false;
        public int sLabel = -1;//标识当前edge在某个集合中的排序
    }

    class PatternTargetEdge
    {
        public ProxiEdge pTargetEdge = null;
        public int ConflictType = 0;//0表示只有端点冲突；1表示存在内点冲突；2表示只有内点冲突
    }
}
