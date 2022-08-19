using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaiChartSafer
{
    /// <summary>
    /// Note模拟器
    /// </summary>
    class Simulator : Singleton<Simulator>
    {
        private Dictionary<TouchArea, TouchAreaMonitor> _touchAreaMonitors = new Dictionary<TouchArea, TouchAreaMonitor>();
        private Dictionary<short, List<JudgeCheckerBase>> _judgeCheckers = new Dictionary<short, List<JudgeCheckerBase>>();
        private List<SlideTask> _slideTasks = new List<SlideTask>();
        private Dictionary<float, List<TouchOperation>> _operations = new Dictionary<float, List<TouchOperation>>();

        public Simulator()
        {
            for (TouchArea area = TouchArea.A1; area<TouchArea.End; area++)
            {
                _touchAreaMonitors.Add(area, new TouchAreaMonitor(area));
            }

            // _judgeCheckers[0]存储Slide的Checker，1-8存储对应键位的Checker
            for (short i = 0; i <= 8; i++)
            {
                _judgeCheckers.Add(i, new List<JudgeCheckerBase>());
            }
        }

        public void AddOperation(float _time, TouchOperation _op)
        {
            if (!_operations.ContainsKey(_time))
            {
                _operations.Add(_time, new List<TouchOperation>());
            }
            _operations[_time].Add(_op);
        }

        public void AddTap(short button, float judgeTime)
        {
            // 注册Tap判定
            _judgeCheckers[button].Add(new TapJudgeChecker(judgeTime));
            // 添加操作
            TouchArea area = TouchAreaEnum.FromButton(TouchArea.A1, button);
            AddOperation(judgeTime, new TouchOperation(area, AreaMethod.In));
            AddOperation(judgeTime, new TouchOperation(area, AreaMethod.Out));
        }

        public void AddHold(short button, float judgeTime, float holdTime)
        {
            // 注册Hold操作
            _judgeCheckers[button].Add(new HoldJudgeChecker(judgeTime, holdTime));
            // 添加操作
            TouchArea area = TouchAreaEnum.FromButton(TouchArea.A1, button);
            AddOperation(judgeTime, new TouchOperation(area, AreaMethod.In));
            AddOperation(judgeTime + holdTime, new TouchOperation(area, AreaMethod.Out));
        }

        public void AddSlide(SlideData slide)
        {
            // 注册Slide操作
            SlideJudgeChecker slideJudgeChecker = new SlideJudgeChecker(slide);
            _judgeCheckers[0].Add(slideJudgeChecker);
            // 注册SlideTask
            _slideTasks.Add(new SlideTask(slide, this, slideJudgeChecker));
            // 添加操作
            List<SlideOperation> slideTime = Singleton<SlideTouchTime>.Instance.GetOperationList(slide);
            foreach (SlideOperation each in slideTime)
            {
                AddOperation(slide.LaunchTime + slide.SlideTime * each.time, new TouchOperation(each.area, each.method));
            }
        }

        internal Dictionary<TouchArea, TouchAreaMonitor> TouchAreaMonitors { get => _touchAreaMonitors; }
        internal Dictionary<short, List<JudgeCheckerBase>> JudgeCheckers { get => _judgeCheckers; }
    }

    class TouchOperation
    {
        private TouchArea _area;
        private AreaMethod _method;

        public TouchOperation(TouchArea area, AreaMethod method)
        {
            _area = area;
            _method = method;
        }
    }
}
