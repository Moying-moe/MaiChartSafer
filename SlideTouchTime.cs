using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaiChartSafer
{
    class SlideTouchTime
    {
        public TouchArea area;

    }

    /// <summary>
    /// 判定区操作枚举类型
    /// </summary>
    enum AreaMethod
    {
        Begin,
        In,
        Out,
        End,
        Invalid = -1,
    }
}
