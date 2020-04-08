using System;

namespace appbox.Drawing
{
    internal struct TextLayoutLine
    {
        TextLayout layout; //todo: check need it?
        internal int startByteIndex; //start of line as byte index into layout->text
        internal int byteLength; //length of line in bytes
        internal int startCharIndex;//start of unichar index info layout->text
        internal float offsetX; //用于非左对齐时的偏移值
        internal float[] widths;

        internal TextLayoutLine(TextLayout layout)
        {
            this.layout = layout;
            startByteIndex = 0;
            byteLength = 0;
            startCharIndex = 0;
            offsetX = 0;
            widths = null;
        }

        /// 根据字符Index找到光标位置
        internal float GetCursorPosition(int charIndex)
        {
            var x = offsetX;
            var curCharIndex = startCharIndex;
            while (curCharIndex < charIndex
                   && (curCharIndex - startCharIndex) <= widths.Length - 1) //ToDO:&&判断用于临时修复PropertyGrid的IndexOutOfRange问题
            {
                x += widths[curCharIndex - startCharIndex];
                curCharIndex += 1;
            }
            return x;
        }

        /// 根据x值找到对应的字符Index, 等价于Pango.TextLine.XToIndex
        internal int GetCharIndex(float x)
        {
            var curX = offsetX;
            if (curX >= x) //居中或右对齐，或x==0的情况
            {
                return startCharIndex;
            }

            var curCharIndex = startCharIndex;
            float w = 0.0f;
            for (int i = 0; i < widths.Length; i++)
            {
                w = widths[i];
                if (w == 0.0f || curX + w / 2 >= x) //加字符宽度的一半
                {
                    return curCharIndex;
                }
                else if (curX + w >= x) //加字符完整宽度
                {
                    return curCharIndex + 1;
                }
                else
                {
                    curX += w;
                    curCharIndex += 1;
                }
            }
            return curCharIndex; //fix compile
        }
    }

}