using System;

namespace appbox.Drawing
{
    public struct CharacterRange
    {

        public int First;
        public int Length;

        public CharacterRange(int first, int length)
        {
            First = first;
            Length = length;
        }

        public override int GetHashCode()
        {
            return First ^ Length;
        }

    }
}