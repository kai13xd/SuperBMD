global using SuperBMD.Util;
namespace SuperBMD.Util
{
    /// <summary>
    /// A JSON converter for OpenTK's Vector2 class.
    /// </summary>
    class Vector2Converter : JsonConverter<Vector2>
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector2);
        }

        public override Vector2 Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
        {
            reader.Read();
            var x = reader.GetSingle();
            reader.Read();
            var y = reader.GetSingle();
            reader.Read();
            return new Vector2(x, y);
        }


        public override void Write(Utf8JsonWriter writer, Vector2 vector, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(vector.X);
            writer.WriteNumberValue(vector.Y);
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// A JSON converter for OpenTK's Vector3 class.
    /// </summary>

    class Vector3Converter : JsonConverter<Vector3>
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector3);
        }

        public override Vector3 Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
        {
            reader.Read();
            var x = reader.GetSingle();
            reader.Read();
            var y = reader.GetSingle();
            reader.Read();
            var z = reader.GetSingle();
            reader.Read();
            return new Vector3(x, y, z);
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            var vector = (Vector3)value;
            writer.WriteStartArray();
            writer.WriteNumberValue(vector.X);
            writer.WriteNumberValue(vector.Y);
            writer.WriteNumberValue(vector.Z);
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// A JSON converter for OpenTK's Vector4 class.
    /// </summary>
    class Vector4Converter : JsonConverter<Vector4>
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector4);
        }

        public override Vector4 Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
        {
            reader.Read();
            var x = reader.GetSingle();
            reader.Read();
            var y = reader.GetSingle();
            reader.Read();
            var z = reader.GetSingle();
            reader.Read();
            var w = reader.GetSingle();
            reader.Read();
            return new Vector4(x, y, z, w);
        }

        public override void Write(Utf8JsonWriter writer, Vector4 vector, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(vector.X);
            writer.WriteNumberValue(vector.Y);
            writer.WriteNumberValue(vector.Z);
            writer.WriteNumberValue(vector.W);
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// A JSON converter for OpenTK's Quaternion class.
    /// </summary>
    class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Quaternion);
        }

        public override Quaternion Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
        {

            reader.Read();
            var x = reader.GetSingle();
            reader.Read();
            var y = reader.GetSingle();
            reader.Read();
            var z = reader.GetSingle();
            reader.Read();
            var w = reader.GetSingle();
            reader.Read();
            return new Quaternion(x, y, z, w);

        }

        public override void Write(Utf8JsonWriter writer, Quaternion quaternion, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(quaternion.X);
            writer.WriteNumberValue(quaternion.Y);
            writer.WriteNumberValue(quaternion.Z);
            writer.WriteNumberValue(quaternion.W);
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// A JSON converter for OpenTK's Matrix4 class.
    /// </summary>
    class Matrix4Converter : JsonConverter<Matrix4>
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Matrix4);
        }

        public override Matrix4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var mtx = new Matrix4();
            reader.Read();
            reader.Read();
            mtx.Row0[0] = reader.GetSingle();
            reader.Read();
            mtx.Row0[1] = reader.GetSingle();
            reader.Read();
            mtx.Row0[2] = reader.GetSingle();
            reader.Read();
            mtx.Row0[3] = reader.GetSingle();
            reader.Read();
            reader.Read();
            reader.Read();
            mtx.Row1[0] = reader.GetSingle();
            reader.Read();
            mtx.Row1[1] = reader.GetSingle();
            reader.Read();
            mtx.Row1[2] = reader.GetSingle();
            reader.Read();
            mtx.Row1[3] = reader.GetSingle();
            reader.Read();
            reader.Read();
            reader.Read();
            mtx.Row2[0] = reader.GetSingle();
            reader.Read();
            mtx.Row2[1] = reader.GetSingle();
            reader.Read();
            mtx.Row2[2] = reader.GetSingle();
            reader.Read();
            mtx.Row2[3] = reader.GetSingle();
            reader.Read();
            reader.Read();
            reader.Read();
            mtx.Row3[0] = reader.GetSingle();
            reader.Read();
            mtx.Row3[1] = reader.GetSingle();
            reader.Read();
            mtx.Row3[2] = reader.GetSingle();
            reader.Read();
            mtx.Row3[3] = reader.GetSingle();
            reader.Read();
            reader.Read();
            return mtx;
        }

        public override void Write(Utf8JsonWriter writer, Matrix4 matrix, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (Vector4 row in new Vector4[] { matrix.Row0, matrix.Row1, matrix.Row2, matrix.Row3 })
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(row.X);
                writer.WriteNumberValue(row.Y);
                writer.WriteNumberValue(row.Z);
                writer.WriteNumberValue(row.W);
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// A JSON converter for OpenTK's Matrix2x3 class.
    /// </summary>
    class Matrix2x3Converter : JsonConverter<Matrix2x3>
    {

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Matrix2x3);
        }

        public override Matrix2x3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Matrix2x3 mtx = new();
            reader.Read();
            reader.Read();
            mtx.Row0.X = reader.GetSingle();
            reader.Read();
            mtx.Row0.Y = reader.GetSingle();
            reader.Read();
            mtx.Row0.Z = reader.GetSingle();
            reader.Read();
            reader.Read();
            reader.Read();
            mtx.Row1.X = reader.GetSingle();
            reader.Read();
            mtx.Row1.Y = reader.GetSingle();
            reader.Read();
            mtx.Row1.Z = reader.GetSingle();
            reader.Read();
            reader.Read();
            return mtx;
        }

        public override void Write(Utf8JsonWriter writer, Matrix2x3 matrix, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteStartArray();
            writer.WriteNumberValue(matrix.Row0.X);
            writer.WriteNumberValue(matrix.Row0.Y);
            writer.WriteNumberValue(matrix.Row0.Z);
            writer.WriteEndArray();
            writer.WriteStartArray();
            writer.WriteNumberValue(matrix.Row1.X);
            writer.WriteNumberValue(matrix.Row1.Y);
            writer.WriteNumberValue(matrix.Row1.Z);
            writer.WriteEndArray();
            writer.WriteEndArray();
        }
    }

    public class ColorConverter : JsonConverter<Color>
    {
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(Color);
        }
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Color32 color = new();
            int i = 0;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        if (reader.TryGetByte(out byte b))
                        {
                            color[i++] = b;
                        }
                        // else if (reader.TryGetSingle(out float f))
                        // {
                        //     float[] fArray = new float[4] { f, reader.GetSingle(), reader.GetSingle(), reader.GetSingle() };
                        //     color = new Color(fArray[0], fArray[1], fArray[2], fArray[3]);
                        // }
                        else
                            throw new Exception("hm");
                        continue;
                    case JsonTokenType.String: //Hex color value support
                        var colorStr = reader.GetString();
                        colorStr = colorStr.Split("#")[1];
                        var bytes = StringToByteArray(colorStr);
                        return new Color32(bytes[0], bytes[1], bytes[2], bytes[3]);

                    case JsonTokenType.StartArray:
                    case JsonTokenType.EndArray:
                        continue;
                    case JsonTokenType.EndObject:
                        return color;
                }
            }
            throw new Exception("Color could not be read from JSON.");
        }
        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            if (Arguments.ShouldExportColorAsBytes)
            {
                Color32 color = value; //Converts colors from float to bytes
                writer.WriteStartObject();
                writer.WritePropertyName("R");
                writer.WriteNumberValue(color.R);
                writer.WritePropertyName("G");
                writer.WriteNumberValue(color.G);
                writer.WritePropertyName("B");
                writer.WriteNumberValue(color.B);
                writer.WritePropertyName("A");
                writer.WriteNumberValue(color.A);
                writer.WriteEndObject();
            }
            else if (Arguments.ShouldExportColorAsHexString)
            {
                Color32 color = value; //Converts colors from float to bytes
                byte[] bytes = new byte[4] { color.R, color.G, color.B, color.A };
                int i = BitConverter.ToInt32(bytes, 0);
                writer.WriteStringValue("#" + i.ToString("X"));
            }
            // else //Write as floats
            // {
            //     writer.WriteStartObject();
            //     writer.WritePropertyName("R");
            //     writer.WriteNumberValue(value.R);
            //     writer.WritePropertyName("G");
            //     writer.WriteNumberValue(value.G);
            //     writer.WritePropertyName("B");
            //     writer.WriteNumberValue(value.B);
            //     writer.WritePropertyName("A");
            //     writer.WriteNumberValue(value.A);
            //     writer.WriteEndObject();
            // }
        }
    }
}
