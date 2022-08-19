using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaiChartSafer
{
    /// <summary>
    /// 存储和管理SlidePath数据
    /// 以单例模式运行
    /// SlidePath是表示某种Slide会按顺序经过哪些判定区的数据结构
    /// </summary>
    class SlidePath
    {
        private Dictionary<SlideOrigin, List<TouchAreaGroup>> _slidePath = new Dictionary<SlideOrigin, List<TouchAreaGroup>>();

        public SlidePath()
        {
            LoadFromFile("./SlidePath.json");
        }

        public List<TouchAreaGroup> GetPath(SlideData slide)
        {
            SlideOrigin sOrigin = slide.GetOrigin();
            List<TouchAreaGroup> originArea = new List<TouchAreaGroup>();
            for (int i = 0; i < _slidePath[sOrigin].Count; i++)
            {
                originArea.Add(_slidePath[sOrigin][i].Clone());
                originArea[i].Rotate((short)(slide.StartButton - 1));
            }
            return originArea;
        }

        /// <summary>
        /// 从文件中载入SlidePath数据
        /// </summary>
        /// <param name="path">SlidePath.json的路径</param>
        private void LoadFromFile(string path)
        {
            string text = File.ReadAllText(path, Encoding.UTF8);
            JObject data = JsonConvert.DeserializeObject<JObject>(text);

            IEnumerable<JProperty> slideContents = data.Properties();
            foreach (JProperty item in slideContents)
            {
                SlideOrigin slideOrigin = new SlideOrigin(item.Name);
                List<TouchAreaGroup> touchAreaList = new List<TouchAreaGroup>();
                foreach (JToken each in item.Value)
                {
                    touchAreaList.Add(TouchAreaGroup.FromString(each.ToString()));
                }

                _slidePath.Add(slideOrigin, touchAreaList);
            }
        }
    }
}
