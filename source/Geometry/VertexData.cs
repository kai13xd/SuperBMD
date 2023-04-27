using SuperBMD.Geometry;
using SuperBMD.Util;

namespace SuperBMD.Geometry
{
    public class VertexData
    {
        private List<VertexAttribute> Attributes;

        public List<Vector3> Positions;
        public List<Vector3> Normals;
        public List<Color> ColorChannel0 { get; private set; }
        public List<Color> ColorChannel1 { get; private set; }
        public List<Vector2> TexCoord0 { get; private set; }
        public List<Vector2> TexCoord1 { get; private set; }
        public List<Vector2> TexCoord2 { get; private set; }
        public List<Vector2> TexCoord3 { get; private set; }
        public List<Vector2> TexCoord4 { get; private set; }
        public List<Vector2> TexCoord5 { get; private set; }
        public List<Vector2> TexCoord6 { get; private set; }
        public List<Vector2> TexCoord7 { get; private set; }

        public void flipAxis()
        {
            for (int i = 0; i < Positions.Count; i++)
            {
                Vector3 vec = Positions[i];
                float tmp = vec.Y;
                vec.Y = vec.Z;
                vec.Z = tmp;
                Positions[i] = vec;
            }
        }

        public VertexData()
        {
            Attributes = new List<VertexAttribute>();
            Positions = new List<Vector3>();
            Normals = new List<Vector3>();
            ColorChannel0 = new List<Color>();
            ColorChannel1 = new List<Color>();
            TexCoord0 = new List<Vector2>();
            TexCoord1 = new List<Vector2>();
            TexCoord2 = new List<Vector2>();
            TexCoord3 = new List<Vector2>();
            TexCoord4 = new List<Vector2>();
            TexCoord5 = new List<Vector2>();
            TexCoord6 = new List<Vector2>();
            TexCoord7 = new List<Vector2>();
        }

        public bool CheckAttribute(VertexAttribute attribute)
        {
            if (Attributes.Contains(attribute))
                return true;
            else
                return false;
        }

        public object GetAttributeData(VertexAttribute attribute)
        {
            if (!CheckAttribute(attribute))
                return null;

            switch (attribute)
            {
                case VertexAttribute.Position:
                    return Positions;
                case VertexAttribute.Normal:
                    return Normals;
                case VertexAttribute.ColorChannel0:
                    return ColorChannel0;
                case VertexAttribute.ColorChannel1:
                    return ColorChannel1;
                case VertexAttribute.TexCoord0:
                    return TexCoord0;
                case VertexAttribute.TexCoord1:
                    return TexCoord1;
                case VertexAttribute.TexCoord2:
                    return TexCoord2;
                case VertexAttribute.TexCoord3:
                    return TexCoord3;
                case VertexAttribute.TexCoord4:
                    return TexCoord4;
                case VertexAttribute.TexCoord5:
                    return TexCoord5;
                case VertexAttribute.TexCoord6:
                    return TexCoord6;
                case VertexAttribute.TexCoord7:
                    return TexCoord7;
                default:
                    throw new ArgumentException("attribute");
            }
        }

        public void SetAttributeData(VertexAttribute attribute, object data)
        {
            if (!CheckAttribute(attribute))
                Attributes.Add(attribute);

            switch (attribute)
            {
                case VertexAttribute.Position:
                    if (data.GetType() != typeof(List<Vector3>))
                        throw new ArgumentException("position data");
                    else
                        Positions = (List<Vector3>)data;
                    break;
                case VertexAttribute.Normal:
                    if (data.GetType() != typeof(List<Vector3>))
                        throw new ArgumentException("normal data");
                    else
                        Normals = (List<Vector3>)data;
                    break;
                case VertexAttribute.ColorChannel0:
                    if (data.GetType() != typeof(List<Color>))
                        throw new ArgumentException("color0 data");
                    else
                    {
                        ColorChannel0 = (List<Color>)data;
                        foreach (Color color in ColorChannel0)
                        {
                            if (color.A < 1.0)
                            {
                                Console.WriteLine("BMD has Vertex Alpha on Channel 0");
                                break;
                            }
                        }
                    }
                    break;
                case VertexAttribute.ColorChannel1:
                    if (data.GetType() != typeof(List<Color>))
                        throw new ArgumentException("color1 data");
                    else
                    {
                        ColorChannel1 = (List<Color>)data;
                        foreach (Color color in ColorChannel1)
                        {
                            if (color.A < 1.0)
                            {
                                Console.WriteLine("BMD has Vertex Alpha on Channel 1");
                                break;
                            }
                        }
                    }
                    break;
                case VertexAttribute.TexCoord0:
                    if (data.GetType() != typeof(List<Vector2>))
                        throw new ArgumentException("TexCoord0 data");
                    else
                        TexCoord0 = (List<Vector2>)data;
                    break;
                case VertexAttribute.TexCoord1:
                    if (data.GetType() != typeof(List<Vector2>))
                        throw new ArgumentException("TexCoord1 data");
                    else
                        TexCoord1 = (List<Vector2>)data;
                    break;
                case VertexAttribute.TexCoord2:
                    if (data.GetType() != typeof(List<Vector2>))
                        throw new ArgumentException("TexCoord2 data");
                    else
                        TexCoord2 = (List<Vector2>)data;
                    break;
                case VertexAttribute.TexCoord3:
                    if (data.GetType() != typeof(List<Vector2>))
                        throw new ArgumentException("TexCoord3 data");
                    else
                        TexCoord3 = (List<Vector2>)data;
                    break;
                case VertexAttribute.TexCoord4:
                    if (data.GetType() != typeof(List<Vector2>))
                        throw new ArgumentException("TexCoord4 data");
                    else
                        TexCoord4 = (List<Vector2>)data;
                    break;
                case VertexAttribute.TexCoord5:
                    if (data.GetType() != typeof(List<Vector2>))
                        throw new ArgumentException("TexCoord5 data");
                    else
                        TexCoord5 = (List<Vector2>)data;
                    break;
                case VertexAttribute.TexCoord6:
                    if (data.GetType() != typeof(List<Vector2>))
                        throw new ArgumentException("TexCoord6 data");
                    else
                        TexCoord6 = (List<Vector2>)data;
                    break;
                case VertexAttribute.TexCoord7:
                    if (data.GetType() != typeof(List<Vector2>))
                        throw new ArgumentException("TexCoord7 data");
                    else
                        TexCoord7 = (List<Vector2>)data;
                    break;
            }
        }

        public void SetAttributesFromList(List<VertexAttribute> attributes)
        {
            Attributes = new List<VertexAttribute>(attributes);
        }
    }
}
