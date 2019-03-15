namespace ImageInfo
{
    class PixelResolution
    {
        public int W { get; }
        public int H { get; }

        public PixelResolution(int w, int h)
        {
            W = w;
            H = h;
        }

        public override string ToString()
        {
            return $"{W}*{H} px";
        }
    }
}
