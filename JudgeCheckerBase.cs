using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaiChartSafer
{
    abstract class JudgeCheckerBase
    {
        protected float _judgeTime;
        protected bool _hasJudged;
        protected JudgeResult _judgeResult;
        protected float _judgeDeltaTime;

        protected string _noteInfo;
        protected float _sortTime;

        public JudgeCheckerBase(string noteInfo, float sortTime)
        {
            _hasJudged = false;
            _judgeResult = JudgeResult.None;
            _noteInfo = noteInfo;
            _sortTime = sortTime;
        }

        /// <summary>
        /// note是否已判定
        /// </summary>
        public bool HasJudged { get => _hasJudged; }
        /// <summary>
        /// note的判定结果，为JudgeResult.None表示未判定
        /// </summary>
        internal JudgeResult JudgeResult { get => _judgeResult; }
        public float JudgeTime { get => _judgeTime; }
        public string NoteInfo { get => _noteInfo; }
        public float JudgeDeltaTime { get => _judgeDeltaTime; }
        public float SortTime { get => _sortTime;  }

        /// <summary>
        /// 是否过早需要忽略 即note是否还没进入判定区
        /// </summary>
        /// <param name="curTime">当前模拟时间</param>
        /// <returns>如果未进入判定区，返回true，否则返回false</returns>
        public abstract bool IsNoteEarlyIgnored(float curTime);
        /// <summary>
        /// 是否过晚
        /// 仅判断，不会改变note的判定。仍需手动调用Judge来判定
        /// </summary>
        /// <param name="curTime">当前模拟时间</param>
        /// <returns>如果过晚，返回true，否则返回false</returns>
        public abstract bool IsNoteTooLate(float curTime);
        /// <summary>
        /// 根据目前的模拟时间设置note的判定
        /// </summary>
        /// <param name="curTime"></param>
        /// <returns>是否进行了判定</returns>
        public abstract bool Judge(float curTime);
    }

    /// <summary>
    /// Tap判定检查器
    /// Tap创建时 在ButtonMonitor中对应的键位上创建并加入一个Checker
    /// ButtonMonitor中 对应A区被激活时 尝试判定一次Checker
    /// </summary>
    class TapJudgeChecker : JudgeCheckerBase
    {
        public TapJudgeChecker(string noteInfo, float sortTime, float judgeTime) : base(noteInfo, sortTime)
        {
            _judgeTime = judgeTime;
        }

        public override bool IsNoteEarlyIgnored(float curTime)
        {
            return curTime < _judgeTime + TapJudgeTiming.JudgeStart;
        }

        public override bool IsNoteTooLate(float curTime)
        {
            return curTime >= _judgeTime + TapJudgeTiming.MissStart;
        }

        public override bool Judge(float curTime)
        {
            float deltaTime = curTime - _judgeTime;

            if (IsNoteEarlyIgnored(curTime) || HasJudged)
            {
                return false;
            }

            if (IsNoteTooLate(curTime))
            {
                this._judgeResult = JudgeResult.Miss;
            }
            else if (deltaTime >= TapJudgeTiming.FastGoodStart && deltaTime < TapJudgeTiming.FastGoodEnd)
            {
                this._judgeResult = JudgeResult.FastGood;
            }
            else if (deltaTime < TapJudgeTiming.FastGreatEnd)
            {
                // 等价于 else if (deltaTime >= TapJudgeTiming.FastGreatStart && deltaTime < TapJudgeTiming.FastGreatEnd) 下同
                this._judgeResult = JudgeResult.FastGreat;
            }
            else if (deltaTime < TapJudgeTiming.FastPerfectEnd)
            {
                this._judgeResult = JudgeResult.FastPerfect;
            }
            else if (deltaTime < TapJudgeTiming.CriticalEnd)
            {
                this._judgeResult = JudgeResult.Critical;
            }
            else if (deltaTime < TapJudgeTiming.LatePerfectEnd)
            {
                this._judgeResult = JudgeResult.LatePerfect;
            }
            else if (deltaTime < TapJudgeTiming.LateGreatEnd)
            {
                this._judgeResult = JudgeResult.LateGreat;
            }
            else if (deltaTime < TapJudgeTiming.LateGoodEnd)
            {
                this._judgeResult = JudgeResult.LateGood;
            }
            else
            {
                this._judgeResult = JudgeResult.Invalid;
                return false;
            }
            this._hasJudged = true;
            this._judgeDeltaTime = deltaTime;
            return true;
        }
    }

    /// <summary>
    /// Hold判定检查器
    /// Hold创建时 在ButtonMonitor中对应键位上创建并加入一个Checker
    /// ButtonMonitor中 对应A区被激活时 尝试调用JudgeHead
    ///                 对应A区结束激活时 尝试调用Judge
    /// </summary>
    class HoldJudgeChecker : JudgeCheckerBase
    {
        private JudgeResult _headJudgeResult;
        private bool _hasHeadJudged;
        private float _holdTime;

        public bool HasHeadJudged { get => _hasHeadJudged; set => _hasHeadJudged = value; }

        public HoldJudgeChecker(string noteInfo, float sortTime, float judgeTime, float holdTime) : base(noteInfo, sortTime)
        {
            _judgeTime = judgeTime;
            _holdTime = holdTime;
            _headJudgeResult = JudgeResult.None;
            _hasHeadJudged = false;
        }

        public override bool IsNoteEarlyIgnored(float curTime)
        {
            return curTime < _judgeTime + TapJudgeTiming.JudgeStart;
        }

        public override bool IsNoteTooLate(float curTime)
        {
            if (!HasHeadJudged)
            {
                // 在旧框中 Hold头判如果没命中 则直接Miss
                return curTime >= _judgeTime + TapJudgeTiming.MissStart;
            }
            else
            {
                // 如果头部已经进行了判定 则直到尾判拖判才会TooLate
                return curTime >= _judgeTime + _holdTime + HoldJudgeTiming.MissStart;
            }
        }

        /// <summary>
        /// Hold头部判定
        /// </summary>
        /// <param name="curTime">当前模拟时间</param>
        /// <returns>是否进行了判定</returns>
        public override bool Judge(float curTime)
        {
            float deltaTime = curTime - _judgeTime;

            if (IsNoteEarlyIgnored(curTime) || HasJudged)
            {
                return false;
            }

            if (IsNoteTooLate(curTime))
            {
                this._headJudgeResult = JudgeResult.Miss;
            }
            else if (deltaTime >= TapJudgeTiming.FastGoodStart && deltaTime < TapJudgeTiming.FastGoodEnd)
            {
                this._headJudgeResult = JudgeResult.FastGood;
            }
            else if (deltaTime < TapJudgeTiming.FastGreatEnd)
            {
                this._headJudgeResult = JudgeResult.FastGreat;
            }
            else if (deltaTime < TapJudgeTiming.FastPerfectEnd)
            {
                this._headJudgeResult = JudgeResult.FastPerfect;
            }
            else if (deltaTime < TapJudgeTiming.CriticalEnd)
            {
                this._headJudgeResult = JudgeResult.Critical;
            }
            else if (deltaTime < TapJudgeTiming.LatePerfectEnd)
            {
                this._headJudgeResult = JudgeResult.LatePerfect;
            }
            else if (deltaTime < TapJudgeTiming.LateGreatEnd)
            {
                this._headJudgeResult = JudgeResult.LateGreat;
            }
            else if (deltaTime < TapJudgeTiming.LateGoodEnd)
            {
                this._headJudgeResult = JudgeResult.LateGood;
            }
            else
            {
                this._headJudgeResult = JudgeResult.Invalid;
                return false;
            }
            this._hasHeadJudged = true;
            this._judgeDeltaTime = deltaTime;
            return true;
        }

        /// <summary>
        /// Hold尾部(Release)判定
        /// </summary>
        /// <param name="curTime">当前模拟时间</param>
        public bool JudgeTail(float curTime)
        {
            float deltaTime = curTime - _judgeTime - _holdTime;

            // 如果还未进入范围 已经判定过 或者还未进行头部判定 则不进行Release判定
            if (IsNoteEarlyIgnored(curTime) || HasJudged || !HasHeadJudged)
            {
                return false;
            }

            if (deltaTime < HoldJudgeTiming.JudgeStart)
            {
                // 非常及早的就松开 判定为FastGood
                // 不过理论上这条逻辑应该不会被调用 因为模拟器总是尝试以正确的方式操作 不会提前松开
                this._judgeResult = JudgeResult.FastGood;
            }
            else if (deltaTime >= HoldJudgeTiming.FastGoodStart && deltaTime < HoldJudgeTiming.FastGoodEnd)
            {
                this._judgeResult = JudgeResult.FastGood;
            }
            else if (deltaTime < HoldJudgeTiming.FastGreatEnd)
            {
                this._judgeResult = JudgeResult.FastGreat;
            }
            else if (deltaTime < HoldJudgeTiming.FastPerfectEnd)
            {
                this._judgeResult = JudgeResult.FastPerfect;
            }
            else if (deltaTime < HoldJudgeTiming.CriticalEnd)
            {
                this._judgeResult = JudgeResult.Critical;
            }
            else if (deltaTime < HoldJudgeTiming.LatePerfectEnd)
            {
                this._judgeResult = JudgeResult.LatePerfect;
            }
            else if (deltaTime < HoldJudgeTiming.LateGreatEnd)
            {
                this._judgeResult = JudgeResult.LateGreat;
            }
            else if (deltaTime < HoldJudgeTiming.LateGoodEnd)
            {
                this._judgeResult = JudgeResult.LateGood;
            }
            else if (deltaTime >= HoldJudgeTiming.MissStart)
            {
                // 拖判
                this._judgeResult = JudgeResult.LateGood;
            }

            // 最后根据头判和尾判 选择较差的一个判定
            this._judgeResult = JudgeResultEnum.GetMin(this._judgeResult, this._headJudgeResult);
            this._hasJudged = true;
            if (this._judgeResult != this._headJudgeResult)
            {
                this._judgeDeltaTime = deltaTime;
            }
            return true;
        }
    }

    /// <summary>
    /// Slide判定检查器
    /// Slide创建时 在TouchAreaMonitor中创建Slide任务 并将本Checker绑定至任务
    /// TouchAreaMonitor中 当一个Slide任务被查询时 查询绑定的Checker其是否可以接受判定
    /// TouchAreaMonitor中 当一个Slide任务被完成时 查询绑定的Checker并判定
    /// </summary>
    class SlideJudgeChecker : JudgeCheckerBase
    {
        private float _startJudgeTime;

        public SlideJudgeChecker(string noteInfo, float sortTime, SlideData slide) : base(noteInfo, sortTime)
        {
            _judgeTime = slide.LastAreaTime;
            _startJudgeTime = slide.StartTime;
        }

        public override bool IsNoteEarlyIgnored(float curTime)
        {
            return curTime < _startJudgeTime;
        }

        public override bool IsNoteTooLate(float curTime)
        {
            return curTime >= _judgeTime + SlideJudgeTiming.MissStart;
        }

        public override bool Judge(float curTime)
        {
            float deltaTime = curTime - _judgeTime;

            if (IsNoteEarlyIgnored(curTime) || HasJudged)
            {
                return false;
            }

            if (IsNoteTooLate(curTime))
            {
                this._judgeResult = JudgeResult.Miss;
            }
            else if (deltaTime < SlideJudgeTiming.JudgeStart)
            {
                // 过早滑完 判FastGood
                this._judgeResult = JudgeResult.FastGood;
            }
            else if (deltaTime >= SlideJudgeTiming.FastGoodStart && deltaTime < SlideJudgeTiming.FastGoodEnd)
            {
                this._judgeResult = JudgeResult.FastGood;
            }
            else if (deltaTime < SlideJudgeTiming.FastGreatEnd)
            {
                this._judgeResult = JudgeResult.FastGreat;
            }
            else if (deltaTime < SlideJudgeTiming.FastPerfectEnd)
            {
                this._judgeResult = JudgeResult.FastPerfect;
            }
            else if (deltaTime < SlideJudgeTiming.CriticalEnd)
            {
                this._judgeResult = JudgeResult.Critical;
            }
            else if (deltaTime < SlideJudgeTiming.LatePerfectEnd)
            {
                this._judgeResult = JudgeResult.LatePerfect;
            }
            else if (deltaTime < SlideJudgeTiming.LateGreatEnd)
            {
                this._judgeResult = JudgeResult.LateGreat;
            }
            else if (deltaTime < SlideJudgeTiming.LateGoodEnd)
            {
                this._judgeResult = JudgeResult.LateGood;
            }
            else
            {
                this._judgeResult = JudgeResult.Invalid;
                return false;
            }
            this._hasJudged = true;
            this._judgeDeltaTime = deltaTime;
            return true;
        }
    }

    enum JudgeResult
    {
        Begin,
        None,
        FastGood,
        FastGreat,
        FastPerfect,
        Critical,
        LatePerfect,
        LateGreat,
        LateGood,
        Miss,
        End,
        Invalid = -1,
    }
    static class JudgeResultEnum
    {
        public static short GetLevel(this JudgeResult self)
        {
            switch(self)
            {
                case JudgeResult.Critical:
                    return 4;
                case JudgeResult.FastPerfect:
                case JudgeResult.LatePerfect:
                    return 3;
                case JudgeResult.FastGreat:
                case JudgeResult.LateGreat:
                    return 2;
                case JudgeResult.FastGood:
                case JudgeResult.LateGood:
                    return 1;
                case JudgeResult.Miss:
                    return 0;
                default:
                    return -1;
            }
        }

        /// <summary>
        /// 返回参数中最差的判定
        /// </summary>
        /// <param name="judges">判定列表</param>
        /// <returns></returns>
        public static JudgeResult GetMin(params JudgeResult[] judges)
        {
            JudgeResult curMin = JudgeResult.Critical;
            foreach (JudgeResult each in judges)
            {
                if (each.GetLevel() == -1)
                {
                    continue;
                }
                if (each.GetLevel() < curMin.GetLevel())
                {
                    curMin = each;
                }
            }
            return curMin;
        }

        /// <summary>
        /// 返回参数中最好的判定
        /// </summary>
        /// <param name="judges">判定列表</param>
        /// <returns></returns>
        public static JudgeResult GetMax(params JudgeResult[] judges)
        {
            JudgeResult curMax = JudgeResult.Miss;
            foreach (JudgeResult each in judges)
            {
                if (each.GetLevel() == -1)
                {
                    continue;
                }
                if (each.GetLevel() > curMax.GetLevel())
                {
                    curMax = each;
                }
            }
            return curMax;
        }
    }

    /// <summary>
    /// Tap判定区间
    /// 所有区间均左闭右开 [Start, End)
    /// </summary>
    static class TapJudgeTiming
    {
        public static readonly float FrameRate = 60f;
        public static readonly float JudgeStart = -9f / FrameRate;
        public static readonly float FastGoodStart = JudgeStart;
        public static readonly float FastGoodEnd = -6f / FrameRate;
        public static readonly float FastGreatStart = FastGoodEnd;
        public static readonly float FastGreatEnd = -3f / FrameRate;
        public static readonly float FastPerfectStart = FastGreatEnd;
        public static readonly float FastPerfectEnd = -1f / FrameRate;
        public static readonly float CriticalStart = FastPerfectEnd;
        public static readonly float CriticalEnd = +1f / FrameRate;
        public static readonly float LatePerfectStart = CriticalEnd;
        public static readonly float LatePerfectEnd = +3f / FrameRate;
        public static readonly float LateGreatStart = LatePerfectEnd;
        public static readonly float LateGreatEnd = +6f / FrameRate;
        public static readonly float LateGoodStart = LateGreatEnd;
        public static readonly float LateGoodEnd = +9f / FrameRate;
        public static readonly float MissStart = LateGoodEnd;
    }

    /// <summary>
    /// Hold尾部判定区间
    /// 所有区间均左闭右开 [Start, End)
    /// </summary>
    static class HoldJudgeTiming
    {
        public static readonly float FrameRate = 60f;
        public static readonly float JudgeStart = -9f / FrameRate;
        public static readonly float FastGoodStart = JudgeStart;
        public static readonly float FastGoodEnd = -6f / FrameRate;
        public static readonly float FastGreatStart = FastGoodEnd;
        public static readonly float FastGreatEnd = -3f / FrameRate;
        public static readonly float FastPerfectStart = FastGreatEnd;
        public static readonly float FastPerfectEnd = -1f / FrameRate;
        public static readonly float CriticalStart = FastPerfectEnd;
        public static readonly float CriticalEnd = +1f / FrameRate;
        public static readonly float LatePerfectStart = CriticalEnd;
        public static readonly float LatePerfectEnd = +8f / FrameRate;
        public static readonly float LateGreatStart = LatePerfectEnd;
        public static readonly float LateGreatEnd = +11f / FrameRate;
        public static readonly float LateGoodStart = LateGreatEnd;
        public static readonly float LateGoodEnd = +14f / FrameRate;
        public static readonly float MissStart = LateGoodEnd;
    }

    /// <summary>
    /// Slide结束判定区间
    /// 所有区间均左闭右开 [Start, End)
    /// </summary>
    static class SlideJudgeTiming
    {
        public static readonly float FrameRate = 60f;
        public static readonly float JudgeStart = -36f / FrameRate;
        public static readonly float FastGoodStart = JudgeStart;
        public static readonly float FastGoodEnd = -26f / FrameRate;
        public static readonly float FastGreatStart = FastGoodEnd;
        public static readonly float FastGreatEnd = -14f / FrameRate;
        public static readonly float FastPerfectStart = FastGreatEnd;
        public static readonly float FastPerfectEnd = -14f / FrameRate;
        public static readonly float CriticalStart = FastPerfectEnd;
        public static readonly float CriticalEnd = +14f / FrameRate;
        public static readonly float LatePerfectStart = CriticalEnd;
        public static readonly float LatePerfectEnd = +14f / FrameRate;
        public static readonly float LateGreatStart = LatePerfectEnd;
        public static readonly float LateGreatEnd = +26f / FrameRate;
        public static readonly float LateGoodStart = LateGreatEnd;
        public static readonly float LateGoodEnd = +36f / FrameRate;
        public static readonly float MissStart = LateGoodEnd;
    }
}
