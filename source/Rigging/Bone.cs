
namespace SuperBMD.Rigging
{
    public class Bone
    {
        public string Name { get; private set; }
        public Bone Parent { get; private set; }
        public List<Bone> Children { get; private set; }

        [JsonConverter(typeof(Matrix4Converter))]
        public Matrix4 InverseBindMatrix { get; private set; }
        [JsonConverter(typeof(Matrix4Converter))]
        public Matrix4 TransformationMatrix { get; private set; }
        public BoundingSphere Bounds { get; set; }

        public MatrixTransformType MatrixType { get; private set; }
        public bool InheritParentScale { get; private set; }
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 Scale { get; private set; }
        [JsonConverter(typeof(QuaternionConverter))]
        public OpenTK.Mathematics.Quaternion Rotation { get; private set; }
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 Translation { get; private set; }

        public Bone(string name)
        {
            Name = name;
            Children = new List<Bone>();
            Bounds = new BoundingSphere();
            Scale = Vector3.One;
        }

        public Bone(ref EndianBinaryReader reader, string name)
        {
            Children = new List<Bone>();

            Name = name;
            MatrixType = (MatrixTransformType)reader.ReadShort();
            InheritParentScale = reader.ReadBool();
            Console.WriteLine($"{name}:\nMatrixType: {MatrixType}\nInherit Scaling: {InheritParentScale}\n");
            reader.Skip(1);

            Scale = new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());

            var rot = new Vector3(reader.ReadShort(), reader.ReadShort(), reader.ReadShort()) * 0.0000958737992429f;
            Rotation = OpenTK.Mathematics.Quaternion.FromAxisAngle(new Vector3(0, 0, 1), rot.Z) *
                         OpenTK.Mathematics.Quaternion.FromAxisAngle(new Vector3(0, 1, 0), rot.Y) *
                         OpenTK.Mathematics.Quaternion.FromAxisAngle(new Vector3(1, 0, 0), rot.X);

            reader.Skip(2);

            Translation = new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());

            TransformationMatrix = Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateTranslation(Translation);
            Console.WriteLine("Bone Bounding Sphere:");
            Bounds = new BoundingSphere(ref reader);
        }

        public Bone(Node node, Bone parent)
        {
            Children = new List<Bone>();

            MatrixType = 0;
            Name = node.Name;
            Parent = parent;

            TransformationMatrix = new Matrix4(
                node.Transform.A1, node.Transform.B1, node.Transform.C1, node.Transform.D1,
                node.Transform.A2, node.Transform.B2, node.Transform.C2, node.Transform.D2,
                node.Transform.A3, node.Transform.B3, node.Transform.C3, node.Transform.D3,
                node.Transform.A4, node.Transform.B4, node.Transform.C4, node.Transform.D4);

            Scale = TransformationMatrix.ExtractScale();
            Rotation = TransformationMatrix.ExtractRotation();
            Translation = TransformationMatrix.ExtractTranslation();

            Bounds = new BoundingSphere();
        }

        public void SetInverseBindMatrix(Matrix4 matrix)
        {
            InverseBindMatrix = matrix;
        }


    }
}
