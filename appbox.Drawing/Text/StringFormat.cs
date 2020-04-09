using System;

namespace appbox.Drawing
{
    public sealed class StringFormat : IDisposable
    {

        public static StringFormat GenericDefault = new StringFormat();
        public static StringFormat GenericTypographic = CreateGenericTypographic();
        public static StringFormat CreateGenericTypographic()
        {
            StringFormatFlags formatFlags = StringFormatFlags.FitBlackBox
                | StringFormatFlags.LineLimit | StringFormatFlags.NoClip;
            var sf = new StringFormat(formatFlags, 0);
            sf.Trimming = StringTrimming.None;
            return sf;
        }

        //private StringDigitSubstitute _substritute = StringDigitSubstitute.User;
        //private CharacterRange[] _charRanges;
        //private float _firstTabOffset = 0;
        //private float[] _tabStops = null;

        public StringAlignment Alignment { get; set; } = StringAlignment.Near;

        public StringAlignment LineAlignment { get; set; } = StringAlignment.Near;

        public StringFormatFlags FormatFlags { get; set; } = 0;

        public StringTrimming Trimming { get; set; } = StringTrimming.Character;

        public StringFormat() { }

        public StringFormat(StringFormatFlags options)
        {
            FormatFlags = options;
        }

        public StringFormat(StringFormatFlags options, int language)
        {
            FormatFlags = options;
        }

        public void SetTabStops(float firstTabOffset, float[] tabStops)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            //do nothing
        }
    }
}