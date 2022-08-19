using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaiChartSafer
{
    /// <summary>
    /// Slide任务
    /// </summary>
    class SlideTask
    {
        private List<TouchAreaGroup> _path;
        private SlideType _slideType;
        private int _curIndex;
        private int _curSubIndex;
        private bool _isIn;
        private SlideJudgeChecker _checker;
        private Simulator _simulator;

        public SlideTask(SlideData slide, Simulator simulator, SlideJudgeChecker checker)
        {
            _path = Singleton<SlidePath>.Instance.GetPath(slide);
            _slideType = slide.SlideType;
            _curIndex = 0;
            _curSubIndex = 0;
            _isIn = false;
            _simulator = simulator;
            _checker = checker;
        }

        public bool IsNoteEarlyIgnored(float curTime)
        {
            return _checker.IsNoteEarlyIgnored(curTime);
        }

        public bool IsNoteTooLate(float curTime)
        {
            return _checker.IsNoteTooLate(curTime);
        }

        /// <summary>
        /// 以目前的判定区按压情况 检查本slide任务是否需要变动
        /// </summary>
        public void CheckSlide(float curTime)
        {
            // 如果已完成判定 未进入判定区 或已经超出判定区 则不检查
            if (_checker.HasJudged || IsNoteEarlyIgnored(curTime) || IsNoteTooLate(curTime))
            {
                return;
            }

            bool changed = false;
            do
            {
                changed = CheckSlideArea(_curIndex, _isIn);
                if (!changed && IsNextAreaCheck())
                {
                    // 第一个判定区未变动 且允许跳区 则检查第二个判定区是否被按下
                    int nextIndex = _curIndex + 1;
                    if (nextIndex < _path.Count)
                    {
                        changed = CheckSlideArea(nextIndex, false);
                    }
                }

                if (_curIndex >= _path.Count)
                {
                    break;
                }
            }
            while (changed);

            if (_curIndex >= _path.Count)
            {
                // 全部判定完了 计算判定时间
                _checker.Judge(curTime);
            }
        }

        /// <summary>
        /// 尝试检查指定判定区
        /// </summary>
        /// <param name="index">指定的判定区</param>
        /// <param name="isin">检查In事件则为false 检查Out事件则为true</param>
        /// <returns></returns>
        public bool CheckSlideArea(int index, bool isin)
        {
            bool result = false;
            if (!isin)
            {
                // 正在检查当前判定区的In事件
                int subIndex = 0;
                foreach (TouchArea area in _path[index].TouchAreas)
                {
                    // 遍历检查OR判定区
                    if (_simulator.TouchAreaMonitors[area].IsOn())
                    {
                        this._curIndex = index;
                        this._curSubIndex = subIndex;
                        result = true;
                        this._isIn = true;
                        if (index == this._path.Count - 1)
                        {
                            this._curIndex = index + 1;
                        }
                        return result;
                    }
                    else
                    {
                        subIndex++;
                    }
                }
                return result;
            }
            else
            {
                // 正在检查当前判定区的Off事件
                if (_simulator.TouchAreaMonitors[_path[index].TouchAreas[this._curSubIndex]].IsOff())
                {
                    this._curIndex = index + 1;
                    this._curSubIndex = 0;
                    result = true;
                    this._isIn = false;
                }
                return result;
            }
        }

        /// <summary>
        /// 是否检查下一个判定区 即是否符合跳区条件
        /// </summary>
        /// <returns></returns>
        public bool IsNextAreaCheck()
        {
            if (_slideType == SlideType.Turn_L || _slideType == SlideType.Turn_R)
            {
                // 转折星星
                if (_path.Count == 5)
                {
                    // 最短的转折星星如1V35
                    // 则第二和第四个判定区不允许被跳过
                    return (_curIndex != 1 || _isIn) && (_curIndex != 3 || _isIn);
                }
                else
                {
                    // 长一点的转折星星
                    // 则第二个判定区不允许被跳过
                    return _curIndex != 1 || _isIn;
                }
            }
            else
            {
                // 其他星星
                if (_path.Count == 3)
                {
                    // 长度为3的 第二个区不允许跳过
                    return _curIndex != 1 || _isIn;
                }
                else if (_path.Count == 2)
                {
                    // 长度为2的 第一个区不允许跳过
                    return _curIndex != 0 || _isIn;
                }
                else
                {
                    // 其他星星 总能跳过
                    return true;
                }
            }
        }
    }
}
