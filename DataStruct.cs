using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MaiChartSafer
{
    static class Tools
    {
        public static string GetNoteInfo(JObject noteGroup, JObject note)
        {
            int codeLine = noteGroup.Value<int>("rawTextPositionY");
            int codeColumn = noteGroup.Value<int>("rawTextPositionX");
            string groupContent = noteGroup.Value<string>("notesContent");
            string noteContent = note.Value<string>("noteContent");
            if (groupContent != noteContent)
            {
                noteContent = string.Concat(
                    "`", noteContent, "` in `",
                    groupContent, "`"
                    );
            }
            else
            {
                noteContent = string.Concat("`", noteContent, "`");
            }

            return string.Concat(
                Tools.TimeFormat(noteGroup.Value<float>("time")), " ",
                noteContent, " ",
                string.Format("({0}L,{1}c)", codeLine, codeColumn));
        }

        public static string TimeFormat(float time)
        {
            int minute = (int)(time / 60);
            float second = time - minute * 60;
            return string.Format("{0:00}:{1:00.00}", minute, second);
        }

        public static void BreakPoint() { }
    }

    /// <summary>
    /// 判定区枚举类型
    /// </summary>
    enum TouchArea
    {
        Begin,
        A1, A2, A3, A4, A5, A6, A7, A8,
        B1, B2, B3, B4, B5, B6, B7, B8,
        C,
        End,
        Invalid = -1,
    }
    static class TouchAreaEnum
    {
        private static Dictionary<string, TouchArea> _stringToEnumDict = InitDict();

        private static Dictionary<string, TouchArea> InitDict()
        {
            Dictionary<string, TouchArea> dict = new Dictionary<string, TouchArea>();
            for (TouchArea area = TouchArea.A1; area < TouchArea.End; area++) 
            {
                dict.Add(area.ToString(), area);
            }

            return dict;
        }

        /// <summary>
        /// 从判定区字符串初始化判定区
        /// </summary>
        /// <param name="_str">判定区字符串</param>
        /// <returns>判定区枚举值</returns>
        public static TouchArea FromString(string _str)
        {
            if (_stringToEnumDict.ContainsKey(_str))
            {
                return _stringToEnumDict[_str];
            }
            Debug.Print(_str);
            return TouchArea.Invalid;
        }

        public static bool IsA(this TouchArea self)
        {
            return self >= TouchArea.A1 && self <= TouchArea.A8;
        }

        public static bool IsB(this TouchArea self)
        {
            return self >= TouchArea.B1 && self <= TouchArea.B8;
        }

        /// <summary>
        /// 获得判定区的键位
        /// </summary>
        /// <param name="self">判定区</param>
        /// <returns>判定区对应的键位，如A1对应1</returns>
        public static short GetButton(this TouchArea self)
        {
            if (self.IsA())
            {
                return (short)(self - TouchArea.A1 + 1);
            }
            else if (self.IsB())
            {
                return (short)(self - TouchArea.B1 + 1);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// 根据判定区类型和键位 返回判定区
        /// </summary>
        /// <param name="areaBase">如果是A区则传入TouchArea.A1，B区则传入TouchArea.B1</param>
        /// <param name="button">判定区对应的键位</param>
        /// <returns>对应的判定区</returns>
        public static TouchArea FromButton(TouchArea areaBase, short button)
        {
            return areaBase + button - 1;
        }

        /// <summary>
        /// 将对象判定区旋转后返回
        /// </summary>
        /// <param name="self">原判定区</param>
        /// <param name="delta">旋转角，范围为-8到8</param>
        /// <returns>新判定区</returns>
        public static TouchArea Rotate(this TouchArea self, short delta)
        {
            if (self == TouchArea.C)
            {
                return TouchArea.C;
            }
            else
            {
                short newButton = (short)(self.GetButton() + delta);
                newButton = (short)((newButton + 7) % 8 + 1);
                if (self.IsA())
                {
                    return TouchAreaEnum.FromButton(TouchArea.A1, newButton);
                }
                else
                {
                    return TouchAreaEnum.FromButton(TouchArea.B1, newButton);
                }
            }
        }
    }
    /// <summary>
    /// OR判定区实现
    /// </summary>
    class TouchAreaGroup
    {
        private List<TouchArea> _touchAreas = new List<TouchArea>();

        public override string ToString()
        {
            return string.Join("/", _touchAreas);
        }

        public TouchAreaGroup(params TouchArea[] areas)
        {
            foreach (TouchArea each in areas)
            {
                _touchAreas.Add(each);
            }
        }

        public TouchAreaGroup Clone()
        {
            return new TouchAreaGroup(_touchAreas.ToArray());
        }

        internal List<TouchArea> TouchAreas { get => _touchAreas;}

        public bool IsOr()
        {
            return _touchAreas.Count > 1;
        }

        public void Rotate(short delta)
        {
            for (int i = 0; i < _touchAreas.Count; i++)
            {
                _touchAreas[i] = _touchAreas[i].Rotate(delta);
            }
        }

        public static TouchAreaGroup FromString(string _str)
        {
            string[] areaStrs = _str.Split('/');
            TouchArea[] areas = new TouchArea[areaStrs.Length];
            for (int i = 0; i < areaStrs.Length; i++) 
            {
                areas[i] = TouchAreaEnum.FromString(areaStrs[i]);
            }
            return new TouchAreaGroup(areas);
        }
    }
}
