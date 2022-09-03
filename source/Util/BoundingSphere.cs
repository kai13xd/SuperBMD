namespace SuperBMD.Util
{
    public class BoundingSphere
    {
        public float Radius { get; private set; } = 0;
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 Min { get; private set; } = new Vector3();
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 Max { get; private set; } = new Vector3();
        public BoundingSphere()
        {
            Min = new Vector3();
            Max = new Vector3();
        }
        public BoundingSphere(ref EndianBinaryReader reader)
        {
            Radius = reader.ReadFloat();
            Min = new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());
            Max = new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());
        }

        public void GetBoundsValues(List<Vector3> positions)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;

            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            foreach (Vector3 vec in positions)
            {
                if (vec.X > maxX)
                    maxX = vec.X;
                if (vec.Y > maxY)
                    maxY = vec.Y;
                if (vec.Z > maxZ)
                    maxZ = vec.Z;

                if (vec.X < minX)
                    minX = vec.X;
                if (vec.Y < minY)
                    minY = vec.Y;
                if (vec.Z < minZ)
                    minZ = vec.Z;
            }

            Min = new Vector3(minX, minY, minZ);
            Max = new Vector3(maxX, maxY, maxZ);
            var position = (Max + Min) / 2;
            Radius = (Max - position).Length;
        }

        public override string ToString()
        {
            return $"Radius: {Radius}\n Min: {Min}\n Max:{Max}\n";
        }
    }
}
