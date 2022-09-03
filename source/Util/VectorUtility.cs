namespace SuperBMD.Util
{
    public static class VectorUtility
    {
        public static Vector3 ToOpenTKVector3(this Assimp.Vector3D vec3)
        {
            return new Vector3(vec3.X, vec3.Y, vec3.Z);
        }

        public static Assimp.Vector3D ToVector3D(this Vector3 vec3)
        {
            return new Assimp.Vector3D(vec3.X, vec3.Y, vec3.Z);
        }

        public static Assimp.Vector3D ToVector2D(this Vector2 vec2)
        {
            return new Assimp.Vector3D(vec2.X, 1.0F - vec2.Y, 0);
        }

        public static Vector2 ToOpenTKVector2(this Assimp.Vector3D vec3)
        {
            return new Vector2(vec3.X, vec3.Y);
        }

        public static Vector2 ToOpenTKVector2(this Assimp.Vector2D vec2)
        {
            return new Vector2(vec2.X, vec2.Y);
        }

        public static Color ToSuperBMDColorRGB(this Assimp.Color3D color3)
        {
            return new Color(color3.R, color3.G, color3.B, 1.0f);
        }

        public static Color ToSuperBMDColorRGBA(this Assimp.Color4D color4)
        {
            return new Color(color4.R, color4.G, color4.B, color4.A);
        }

        public static Assimp.Matrix4x4 ToMatrix4x4(this Matrix4 mat4)
        {
            Assimp.Matrix4x4 outMat = new(mat4.M11, mat4.M12, mat4.M13, mat4.M14,
                mat4.M21, mat4.M22, mat4.M23, mat4.M24,
                mat4.M31, mat4.M32, mat4.M33, mat4.M34,
                mat4.M41, mat4.M42, mat4.M43, mat4.M44);

            outMat.Transpose();
            return outMat;
        }
    }
}
