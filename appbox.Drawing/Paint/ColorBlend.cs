namespace appbox.Drawing
{
    public struct ColorBlend
    {
        public float[] Positions { get; set; }
        public Color[] Colors { get; set; }

        public ColorBlend(int count = 2)
        {
            Positions = new float[count];
            Colors = new Color[count];
        }
    }
}