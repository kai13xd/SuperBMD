namespace SuperBMD.Materials
{
    public enum BlendModeControl
    {
        Zero = 0,               // ! < 0.0
        One = 1,                // ! < 1.0
        SrcColor = 2,           // ! < Source Color
        InverseSrcColor = 3,    // ! < 1.0 - (Source Color)
        SrcAlpha = 4,           // ! < Source Alpha
        InverseSrcAlpha = 5,    // ! < 1.0 - (Source Alpha)
        DstAlpha = 6,           // ! < Framebuffer Alpha
        InverseDstAlpha = 7     // ! < 1.0 - (Framebuffer Alpha)
    }
}
