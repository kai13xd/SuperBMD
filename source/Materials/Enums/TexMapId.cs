namespace SuperBMD.Materials
{
    public enum TexMapId
    {
        TexMap0,
        TexMap1,
        TexMap2,
        TexMap3,
        TexMap4,
        TexMap5,
        TexMap6,
        TexMap7,

        Null = 0xFF,

        /// <summary>
        /// Do not use!
        /// </summary>
        Disable = 0x100 // mask: disables texture look up
    }
}
