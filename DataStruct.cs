using System.Collections.Generic;
using System.Diagnostics;

namespace MaiChartSafer
{
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
            dict.Add("A1", TouchArea.A1);
            dict.Add("A2", TouchArea.A2);
            dict.Add("A3", TouchArea.A3);
            dict.Add("A4", TouchArea.A4);
            dict.Add("A5", TouchArea.A5);
            dict.Add("A6", TouchArea.A6);
            dict.Add("A7", TouchArea.A7);
            dict.Add("A8", TouchArea.A8);
            dict.Add("B1", TouchArea.B1);
            dict.Add("B2", TouchArea.B2);
            dict.Add("B3", TouchArea.B3);
            dict.Add("B4", TouchArea.B4);
            dict.Add("B5", TouchArea.B5);
            dict.Add("B6", TouchArea.B6);
            dict.Add("B7", TouchArea.B7);
            dict.Add("B8", TouchArea.B8);
            dict.Add("C", TouchArea.C);

            return dict;
        }

        public static TouchArea FromString(string _str)
        {
            if (_stringToEnumDict.ContainsKey(_str))
            {
                return _stringToEnumDict[_str];
            }
            Debug.Print(_str);
            return TouchArea.Invalid;
        }
    }
}
