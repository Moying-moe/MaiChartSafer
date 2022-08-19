namespace MaiChartSafer
{
    class SlideOrigin
    {
        private SlideType _slideType;
        private short _endButton;

        public SlideOrigin(string slideContent)
        {
            _slideType = SlideTypeEnum.SlideTypeConvert(slideContent);
            switch (_slideType)
            {
                // 处理4种终点不在slideContent[2]的情况
                case SlideType.Curve_L:
                case SlideType.Curve_R:
                case SlideType.Turn_L:
                case SlideType.Turn_R:
                    _endButton = (short)(slideContent[3] - '0');
                    break;
                default:
                    _endButton = (short)(slideContent[2] - '0');
                    break;
            }
        }

        /// <summary>
        /// 根据SlideData创建原始Slide
        /// </summary>
        /// <param name="slideData"></param>
        public SlideOrigin(SlideData slideData)
        {
            _slideType = slideData.SlideType;
            _endButton = (short)((slideData.EndButton - slideData.StartButton + 8) % 8 + 1);
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(SlideOrigin))
            {
                return false;
            }
            SlideOrigin sobj = (SlideOrigin)obj;
            return _slideType == sobj._slideType && _endButton == sobj._endButton;
        }

        public override int GetHashCode()
        {
            string slideString = ToString();
            return slideString.GetHashCode();
        }

        public override string ToString()
        {
            return string.Concat(
                "[SlideOrigin 1",
                _slideType,
                _endButton.ToString(),
                "]"
                );
        }
    }

    class SlideData
    {
        private SlideType _slideType;
        private short _startButton;
        private short _endButton;

        public SlideData(string slideContent)
        {
            _slideType = SlideTypeEnum.SlideTypeConvert(slideContent);
            _startButton = (short)(slideContent[0] - '0');
            switch (_slideType)
            {
                // 处理4种终点不在slideContent[2]的情况
                case SlideType.Curve_L:
                case SlideType.Curve_R:
                case SlideType.Turn_L:
                case SlideType.Turn_R:
                    _endButton = (short)(slideContent[3] - '0');
                    break;
                default:
                    _endButton = (short)(slideContent[2] - '0');
                    break;
            }
        }

        public short StartButton { get => _startButton;}
        public short EndButton { get => _endButton;}
        internal SlideType SlideType { get => _slideType;}

        /// <summary>
        /// 获得本Slide对应的原始Slide（即起点为1的Slide）
        /// </summary>
        /// <returns>对应的原始Slide</returns>
        public SlideOrigin GetOrigin()
        {
            return new SlideOrigin(this);
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(SlideData))
            {
                return false;
            }
            SlideData sobj = (SlideData)obj;
            return _slideType == sobj._slideType && _startButton == sobj._startButton && _endButton == sobj._endButton;
        }

        public override int GetHashCode()
        {
            string slideString = ToString();
            return slideString.GetHashCode();
        }

        public override string ToString()
        {
            return string.Concat(
                "[SlideData ",
                _startButton.ToString(),
                _slideType,
                _endButton.ToString(),
                "]"
                );
        }
    }

    /// <summary>
    /// Slide类型枚举类型
    /// </summary>
    enum SlideType
    {
        Begin,
        Straight, // 直线 -
        Circle_L, // 贴边逆时针 < 此处符号特指起点为7812时的情况
        Circle_R, // 贴边顺时针 > 此处符号特指起点为7812时的情况
        Corner, // 折线 v
        Round_L, // 中央圆 p
        Round_R, // 中央圆 q
        Thunder_L, // 闪电 s
        Thunder_R, // 闪电 z
        Curve_L, // 曲线 pp
        Curve_R, // 曲线 qq
        Turn_L, // 转折 V 转折点在逆时针方向 如1V7?  4V2?
        Turn_R, // 转折 V 转折点在顺时针方向 如1V3?  4V6?
        Fan, // 扇形 w
        End,
        Invalid = -1,
    }

    static class SlideTypeEnum
    {
        public static SlideType SlideTypeConvert(string slideContent)
        {
            string slideTypeChar;
            int startButton;
            int endButton;

            string tempTypeChar = slideContent.Substring(1, 2);
            if (tempTypeChar == "pp" || tempTypeChar == "qq")
            {
                slideTypeChar = tempTypeChar;
                startButton = slideContent[0] - '0';
                endButton = slideContent[3] - '0';
            }
            else
            {
                slideTypeChar = slideContent.Substring(1, 1);
                startButton = slideContent[0] - '0';
                endButton = slideContent[2] - '0';
            }

            switch (slideTypeChar)
            {
                case "-":
                    return SlideType.Straight;
                case "v":
                    return SlideType.Corner;
                case "p":
                    return SlideType.Round_L;
                case "q":
                    return SlideType.Round_R;
                case "s":
                    return SlideType.Thunder_L;
                case "z":
                    return SlideType.Thunder_R;
                case "pp":
                    return SlideType.Curve_L;
                case "qq":
                    return SlideType.Curve_R;
                case "w":
                    return SlideType.Fan;
                case "^":
                    { 
                        // 短弧线类型 计算起点和终点的相对位置 并根据结果判断该弧线为顺时针或逆时针
                        int delta = (endButton - startButton + 8) % 8 + 1;
                        if (delta >= 2 && delta <= 4)
                        {
                            return SlideType.Circle_R;
                        }
                        else if (delta >= 6 && delta <= 8)
                        {
                            return SlideType.Circle_L;
                        }
                        else
                        {
                            return SlideType.Invalid;
                        }
                    }
                case ">":
                    // 向右弧线类型 根据起点位置判断该弧线为顺时针或逆时针
                    if (startButton >= 3 && startButton <= 6)
                    {
                        return SlideType.Circle_L;
                    }
                    else
                    {
                        return SlideType.Circle_R;
                    }
                case "<":
                    // 同上 向左弧线类型
                    if (startButton >= 3 && startButton <= 6)
                    {
                        return SlideType.Circle_R;
                    }
                    else
                    {
                        return SlideType.Circle_L;
                    }
                case "V":
                    {
                        // 转折型 计算出终点（转折点）相对位置后 根据结果判断为顺时针或逆时针
                        int delta = (endButton - startButton + 8) % 8 + 1;
                        if (delta == 3)
                        {
                            return SlideType.Turn_R;
                        }
                        else if (delta == 7)
                        {
                            return SlideType.Turn_L;
                        }
                        else
                        {
                            return SlideType.Invalid;
                        }
                    }
                default:
                    return SlideType.Invalid;
            }
        }
    }
}