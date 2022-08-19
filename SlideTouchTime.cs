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
    /// 读取并管理SlideTouchTime数据
    /// 以单例模式运行
    /// SlideTouchTime中存储的是每一种slide何时进入或退出何种判定区的数据
    /// </summary>
    class SlideTouchTime : Singleton<SlideTouchTime>
    {
        private readonly Dictionary<SlideOrigin,List<SlideOperation>> _slideTouchTime = new Dictionary<SlideOrigin, List<SlideOperation>>();

        public SlideTouchTime()
        {
            LoadFromFile("./SlideTouchTime.json");
        }

        public List<SlideOperation> GetOperationList(SlideData slide)
        {
            SlideOrigin sOrigin = slide.GetOrigin();
            List<SlideOperation> originOperation = new List<SlideOperation>();
            for (int i = 0; i < _slideTouchTime[sOrigin].Count; i++)
            {
                originOperation.Add(_slideTouchTime[sOrigin][i].Clone());
                originOperation[i].Rotate((short)(slide.StartButton - 1));
            }
            return originOperation;
        }

        /// <summary>
        /// 获得slide进入最后一个区的时间比例
        /// </summary>
        /// <param name="slide"></param>
        /// <returns></returns>
        public float GetLastAreaInTime(SlideData slide)
        {
            List<SlideOperation> operations = GetOperationList(slide);
            TouchArea lastArea = operations[operations.Count - 1].area;
            for (int i = operations.Count - 2; i > -1; i--)
            {
                if (operations[i].area == lastArea && operations[i].method == AreaMethod.In)
                {
                    return operations[i].time;
                }
            }
            return 1f;
        }

        /// <summary>
        /// 从文件中载入SlideTouchTime数据
        /// </summary>
        /// <param name="path">SlideTouchTime.json的路径</param>
        private void LoadFromFile(string path)
        {
            string text = File.ReadAllText(path, Encoding.UTF8);
            JObject data = JsonConvert.DeserializeObject<JObject>(text);

            IEnumerable<JProperty> slideContents = data.Properties();
            foreach (JProperty item in slideContents)
            {
                SlideOrigin slideOrigin = new SlideOrigin(item.Name);
                List<SlideOperation> operationList = new List<SlideOperation>();
                foreach (JToken each in item.Value)
                {
                    JObject jobj = each.ToObject<JObject>();
                    operationList.Add(new SlideOperation(jobj.Value<string>("area"), jobj.Value<string>("method"), jobj.Value<float>("time")));
                }

                _slideTouchTime.Add(slideOrigin, operationList);
            }
        }
    }

    /// <summary>
    /// slide对判定区操作的信息
    /// </summary>
    class SlideOperation
    {
        public TouchArea area;
        public AreaMethod method;
        public float time;

        public SlideOperation(string areaStr, string methodStr, float time)
        {
            area = TouchAreaEnum.FromString(areaStr);
            method = AreaMethodEnum.FromString(methodStr);
            this.time = time;
        }

        public SlideOperation(TouchArea area, AreaMethod method, float time)
        {
            this.area = area;
            this.method = method;
            this.time = time;
        }

        public SlideOperation Clone()
        {
            return new SlideOperation(area, method, time);
        }

        /// <summary>
        /// 将判定区操作的area旋转
        /// </summary>
        /// <param name="delta">旋转的键位数值</param>
        public void Rotate(short delta)
        {
            area = area.Rotate(delta);
        }

        public override string ToString()
        {
            return string.Concat(
                "[SlideOperation ",
                method, " ",
                area, ",",
                time.ToString(),
                "]"
                );
        }
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

    static class AreaMethodEnum
    {
        public static AreaMethod FromString(string _str)
        {
            switch (_str)
            {
                case "in":
                    return AreaMethod.In;
                case "out":
                    return AreaMethod.Out;
                default:
                    return AreaMethod.Invalid;
            }
        }
    }
}
