namespace SuperBMD.Materials
{
    public enum CullMode
    {
        None = 0,   // Do not cull any primitives
        Front = 1,  // Cull front-facing primitives
        Back = 2,   // Cull back-facing primitives
        All = 3     // Cull all primitives
    }
}
