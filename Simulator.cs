using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
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
        private bool _hasLoaded;
        private bool _hasSimulated;

        public Simulator()
        {
            Init();
        }

        /// <summary>
        /// 将Simualtor重置为初始状态
        /// </summary>
        public void Init()
        {
            _hasLoaded = false;
            _hasSimulated = false;
            _touchAreaMonitors.Clear();
            _judgeCheckers.Clear();
            _slideTasks.Clear();
            _operations.Clear();

            for (TouchArea area = TouchArea.A1; area < TouchArea.End; area++)
            {
                _touchAreaMonitors.Add(area, new TouchAreaMonitor(area));
            }

            // _judgeCheckers[0]存储Slide的Checker，1-8存储对应键位的Checker
            for (short i = 0; i <= 8; i++)
            {
                _judgeCheckers.Add(i, new List<JudgeCheckerBase>());
            }
        }

        /// <summary>
        /// 从majdata.json中读取谱面
        /// </summary>
        /// <param name="path"></param>
        public void LoadChartFromMajdata(string path)
        {
            if (HasLoaded)
            {
                Init();
            }

            string text = File.ReadAllText(path, Encoding.UTF8);
            JObject data = JsonConvert.DeserializeObject<JObject>(text);

            JArray timingList = data.Value<JArray>("timingList");

            foreach (JObject noteGroup in timingList)
            {
                float groupTime = noteGroup.Value<float>("time");
                foreach (JObject note in noteGroup.Value<JArray>("noteList"))
                {
                    switch(note.Value<int>("noteType"))
                    {
                        case 0:
                            // Tap
                            AddTap(Tools.GetNoteInfo(noteGroup, note), note.Value<short>("startPosition"), groupTime);
                            break;
                        case 1:
                            // Slide-Tap
                            AddTap("*"+Tools.GetNoteInfo(noteGroup, note), note.Value<short>("startPosition"), groupTime);
                            // Slide
                            SlideData slide = new SlideData(
                                SlideData.SlideContentFromFullSlideText(note.Value<string>("noteContent")),
                                groupTime, note.Value<float>("slideStartTime"), note.Value<float>("slideTime"));
                            AddSlide(Tools.GetNoteInfo(noteGroup, note), slide);
                            break;
                        case 2:
                            // Hold
                            AddHold(Tools.GetNoteInfo(noteGroup, note), note.Value<short>("startPosition"), groupTime, note.Value<float>("holdTime"));
                            break;
                        default:
                            // Invalid
                            break;
                    }
                }
            }

            _hasLoaded = true;
        }

        /// <summary>
        /// [未实现]
        /// 从maidata.txt中读取谱面
        /// </summary>
        /// <param name="path"></param>
        public void LoadChartFromSimai(string path)
        {
            // TODO: Load From Maidata.txt
        }

        public void Simulate()
        {
            if (!_hasLoaded)
            {
                throw new Exception("Please load the chart first.");
            }

            // 让checker以judgeTime为顺序排序
            for (short button = 0; button <= 8; button++)
            {
                _judgeCheckers[button].Sort((JudgeCheckerBase a, JudgeCheckerBase b) =>
                {
                    if (a.JudgeTime > b.JudgeTime)
                    {
                        return 1;
                    }
                    else if (a.JudgeTime < b.JudgeTime)
                    {
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                });
            }
            // 让operations按时间排序
            var operationSorted = from objDict in _operations orderby objDict.Key select objDict;

            foreach (KeyValuePair<float, List<TouchOperation>> item in operationSorted)
            {
                float curTime = item.Key;
                List<TouchOperation> ops = item.Value;
                ops.Sort((TouchOperation a, TouchOperation b) => { return a.Method - b.Method; });

                /// 1. 遍历并判定所有checker的TooLate
                /// 2. 处理所有On事件 有A区上沿触发就调用对应button的judgeChecker
                /// 3. 处理SlideTask
                /// 4. 处理所有的Off事件 有A区下沿触发就调用对应button的judgeChecker
                /// 5. 处理SlideTask

                // 处理TooLate
                for (short button = 0; button<=8; button++)
                {
                    foreach (JudgeCheckerBase checker in _judgeCheckers[button])
                    {
                        if (checker.HasJudged)
                        {
                            continue;
                        }
                        if (checker.IsNoteTooLate(curTime))
                        {
                            checker.Judge(curTime);
                        }
                    }
                }

                Tools.BreakPoint();

                // 处理所有On事件 有A区上沿触发就调用对应button的judgeChecker
                int opIndex = 0;
                for (opIndex = 0; opIndex < ops.Count; opIndex++)
                {
                    TouchOperation operation = ops[opIndex];
                    if (operation.Method == AreaMethod.Out)
                    {
                        break;
                    }

                    if (_touchAreaMonitors[operation.Area].Activate() && operation.Area.IsA())
                    {
                        // A区上沿触发
                        foreach (JudgeCheckerBase checker in _judgeCheckers[operation.Area.GetButton()])
                        {
                            // 找到第一个可判定的checker并判定
                            if (checker.HasJudged || checker.IsNoteEarlyIgnored(curTime))
                            {
                                continue;
                            }

                            checker.Judge(curTime);
                            break;
                        }
                    }
                }

                Tools.BreakPoint();

                // 处理SlideTask
                foreach (SlideTask task in _slideTasks)
                {
                    task.CheckSlide(curTime);
                }

                Tools.BreakPoint();

                // 处理所有的Off事件 有A区下沿触发就调用对应button的judgeChecker
                for (; opIndex < ops.Count; opIndex++)
                {
                    TouchOperation operation = ops[opIndex];

                    if (_touchAreaMonitors[operation.Area].Deactivate() && operation.Area.IsA())
                    {
                        // A区下沿触发
                        foreach (JudgeCheckerBase checker in _judgeCheckers[operation.Area.GetButton()])
                        {
                            if (checker.HasJudged || checker.IsNoteEarlyIgnored(curTime) || checker.GetType() != typeof(HoldJudgeChecker))
                            {
                                continue;
                            }

                            ((HoldJudgeChecker)checker).JudgeTail(curTime);
                        }
                    }
                }

                Tools.BreakPoint();

                // 处理SlideTask
                foreach (SlideTask task in _slideTasks)
                {
                    task.CheckSlide(curTime);
                }

                Tools.BreakPoint();
            }

            _hasSimulated = true;
        }

        public string GetSimulateResult(ResultLevel level)
        {
            if (!_hasSimulated)
            {
                throw new Exception("Please simulate the chart first.");
            }

            JudgeResult resultFast = JudgeResult.FastGood;
            JudgeResult resultLate = JudgeResult.Miss;
            if (level == ResultLevel.Critical)
            {
                resultFast = JudgeResult.Critical;
                resultLate = JudgeResult.Critical;
            }
            else if (level == ResultLevel.Perfect)
            {
                resultFast = JudgeResult.FastPerfect;
                resultLate = JudgeResult.LatePerfect;
            }
            else
            {
                resultFast = JudgeResult.None;
                resultLate = JudgeResult.None;
            }

            string result = "";
            for (short button = 0; button <= 8; button++)
            {
                foreach (JudgeCheckerBase checker in _judgeCheckers[button])
                {
                    if (!(checker.JudgeResult >= resultFast && checker.JudgeResult <= resultLate))
                    {
                        result += string.Concat(
                            checker.NoteInfo,
                            " got ",
                            checker.JudgeResult,
                            "\n");
                    }
                }
            }

            return result;
        }

        public void AddOperation(float _time, TouchOperation _op)
        {
            if (!_operations.ContainsKey(_time))
            {
                _operations.Add(_time, new List<TouchOperation>());
            }
            _operations[_time].Add(_op);
        }

        public void AddTap(string noteInfo, short button, float judgeTime)
        {
            // 注册Tap判定
            _judgeCheckers[button].Add(new TapJudgeChecker(noteInfo, judgeTime));
            // 添加操作
            TouchArea area = TouchAreaEnum.FromButton(TouchArea.A1, button);
            AddOperation(judgeTime, new TouchOperation(area, AreaMethod.In));
            AddOperation(judgeTime, new TouchOperation(area, AreaMethod.Out));
        }

        public void AddHold(string noteInfo, short button, float judgeTime, float holdTime)
        {
            // 注册Hold操作
            _judgeCheckers[button].Add(new HoldJudgeChecker(noteInfo, judgeTime, holdTime));
            // 添加操作
            TouchArea area = TouchAreaEnum.FromButton(TouchArea.A1, button);
            AddOperation(judgeTime, new TouchOperation(area, AreaMethod.In));
            AddOperation(judgeTime + holdTime, new TouchOperation(area, AreaMethod.Out));
        }

        public void AddSlide(string noteInfo, SlideData slide)
        {
            // 注册Slide操作
            SlideJudgeChecker slideJudgeChecker = new SlideJudgeChecker(noteInfo, slide);
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
        public bool HasLoaded { get => _hasLoaded; }
        public bool HasSimulated { get => _hasSimulated; }
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

        internal AreaMethod Method { get => _method; set => _method = value; }
        internal TouchArea Area { get => _area; set => _area = value; }
    }

    enum ResultLevel
    {
        Critical, // 非Critical Perfect都会被输出
        Perfect, // 非Perfect都会被输出
        All, // 所有判定都会输出
    }
}
