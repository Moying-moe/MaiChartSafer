using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaiChartSafer
{
    /// <summary>
    /// 监控每个判定区按下情况
    /// </summary>
    class TouchAreaMonitor
    {
        private readonly TouchArea _area;
        private uint _activeSignal;

        public TouchAreaMonitor(TouchArea area)
        {
            _area = area;
            _activeSignal = 0;
        }

        internal TouchArea Area { get => _area; }

        public bool IsOn()
        {
            return _activeSignal != 0;
        }

        public bool IsOff()
        {
            return _activeSignal == 0;
        }

        /// <summary>
        /// 为本判定区添加一个激活源
        /// </summary>
        /// <returns>如果本次激活导致上沿触发则返回true</returns>
        public bool activate()
        {
            _activeSignal++;
            return _activeSignal == 1;
        }

        /// <summary>
        /// 为本判定区去除一个激活源
        /// </summary>
        /// <returns>如果本次去激活导致下沿触发则返回true</returns>
        public bool deactivate()
        {
            if (IsOff())
            {
                return false;
            }
            _activeSignal--;
            return _activeSignal == 0;
        }
    }
}
