﻿using SuperBMD.Materials;
using Assimp;
namespace SuperBMD.Util
{
    public static class WrapModeUtility
    {
        public static BinaryTextureImage.WrapModes ToGXWrapMode(this TextureWrapMode mode)
        {
            switch (mode)
            {
                case TextureWrapMode.Clamp:
                    return BinaryTextureImage.WrapModes.ClampToEdge;
                case TextureWrapMode.Mirror:
                    return BinaryTextureImage.WrapModes.MirroredRepeat;
                case TextureWrapMode.Wrap:
                    return BinaryTextureImage.WrapModes.Repeat;
                case TextureWrapMode.Decal:
                    return BinaryTextureImage.WrapModes.ClampToEdge;
                default:
                    throw new ArgumentException("mode");
            }
        }

        public static Assimp.TextureWrapMode ToAssImpWrapMode(this BinaryTextureImage.WrapModes mode)
        {
            switch (mode)
            {
                case BinaryTextureImage.WrapModes.ClampToEdge:
                    return TextureWrapMode.Clamp;
                case BinaryTextureImage.WrapModes.MirroredRepeat:
                    return TextureWrapMode.Mirror;
                case BinaryTextureImage.WrapModes.Repeat:
                    return TextureWrapMode.Wrap;
                default:
                    throw new ArgumentException("mode");
            }
        }
    }
}
