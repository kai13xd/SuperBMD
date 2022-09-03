using SuperBMD.Rigging;
using SuperBMD.Util;

namespace SuperBMD.Animation
{
    public enum LoopMode
    {
        Once,
        OnceReset,
        Loop,
        MirroredOnce,
        MirroredLoop
    }

    public class BCK
    {
        public string Name { get; private set; }
        public LoopMode LoopMode;
        public byte RotationFrac;
        public short Duration;

        public Track[] Tracks;

        public BCK(Assimp.Animation src_anim, List<Rigging.Bone> bone_list)
        {
            Name = src_anim.Name;
            LoopMode = LoopMode.Loop;
            RotationFrac = 0;
            Duration = (short)(src_anim.DurationInTicks * 30.0f);

            Tracks = new Track[bone_list.Count];

            for (int i = 0; i < bone_list.Count; i++)
            {
                Assimp.NodeAnimationChannel node = src_anim.NodeAnimationChannels.Find(x => x.NodeName == bone_list[i].Name);

                if (node is null)
                    Tracks[i] = Track.Identity(bone_list[i].TransformationMatrix, Duration);
                else
                    Tracks[i] = GenerateTrack(node, bone_list[i]);
            }
        }

        public BCK(ref EndianBinaryReader reader)
        {
            if (reader.ReadString(8) != "J3D1bck1")
            {
                throw new Exception("File read was not a BCK!");
            }

            int file_size = reader.ReadInt();
            int section_count = reader.ReadInt();

            reader.Skip(16);

            ReadAnk1(ref reader);
        }

        private Track GenerateTrack(Assimp.NodeAnimationChannel channel, Bone bone)
        {
            Track track = new Track();

            track.Translation = GenerateTranslationTrack(channel.PositionKeys, bone);
            track.Rotation = GenerateRotationTrack(channel.RotationKeys, bone);
            track.Scale = GenerateScaleTrack(channel.ScalingKeys, bone);

            return track;
        }

        private Keyframe[][] GenerateTranslationTrack(List<Assimp.VectorKey> keys, Bone bone)
        {
            Keyframe[] x_track = new Keyframe[keys.Count];
            Keyframe[] y_track = new Keyframe[keys.Count];
            Keyframe[] z_track = new Keyframe[keys.Count];

            for (int i = 0; i < keys.Count; i++)
            {
                Assimp.VectorKey current_key = keys[i];
                Vector3 value = new Vector3(current_key.Value.X, current_key.Value.Y, current_key.Value.Z);

                x_track[i].Key = value.X;
                x_track[i].Time = (float)current_key.Time;

                y_track[i].Key = value.Y;
                y_track[i].Time = (float)current_key.Time;

                z_track[i].Key = value.Z;
                z_track[i].Time = (float)current_key.Time;
            }

            return new Keyframe[][] { x_track, y_track, z_track };
        }

        private Keyframe[][] GenerateRotationTrack(List<Assimp.QuaternionKey> keys, Bone bone)
        {
            Keyframe[] x_track = new Keyframe[keys.Count];
            Keyframe[] y_track = new Keyframe[keys.Count];
            Keyframe[] z_track = new Keyframe[keys.Count];

            for (int i = 0; i < keys.Count; i++)
            {
                Assimp.QuaternionKey current_key = keys[i];
                Quaternion value = new Quaternion(current_key.Value.X, current_key.Value.Y, current_key.Value.Z, current_key.Value.W);
                Vector3 quat_as_vec = QuaternionExtensions.ToEulerAngles(value);

                x_track[i].Key = quat_as_vec.X;
                x_track[i].Time = (float)current_key.Time;

                y_track[i].Key = quat_as_vec.Y;
                y_track[i].Time = (float)current_key.Time;

                z_track[i].Key = quat_as_vec.Z;
                z_track[i].Time = (float)current_key.Time;
            }

            return new Keyframe[][] { x_track, y_track, z_track };
        }

        private Keyframe[][] GenerateScaleTrack(List<Assimp.VectorKey> keys, Bone bone)
        {
            Keyframe[] x_track = new Keyframe[keys.Count];
            Keyframe[] y_track = new Keyframe[keys.Count];
            Keyframe[] z_track = new Keyframe[keys.Count];

            for (int i = 0; i < keys.Count; i++)
            {
                Assimp.VectorKey current_key = keys[i];
                Vector3 value = new Vector3(current_key.Value.X, current_key.Value.Y, current_key.Value.Z);

                x_track[i].Key = value.X;
                x_track[i].Time = (float)current_key.Time;

                y_track[i].Key = value.Y;
                y_track[i].Time = (float)current_key.Time;

                z_track[i].Key = value.Z;
                z_track[i].Time = (float)current_key.Time;
            }

            return new Keyframe[][] { x_track, y_track, z_track };
        }

        private void ReadAnk1(ref EndianBinaryReader reader)
        {
            if (reader.ReadString(4) != "ANK1")
            {
                throw new Exception("Section read was not ANK1!");
            }

            int section_length = reader.ReadInt();

            LoopMode = (LoopMode)reader.ReadByte();
            RotationFrac = reader.ReadByte();
            Duration = reader.ReadShort();

            ushort keyframe_count = reader.ReadUShort();
            ushort scale_count = reader.ReadUShort();
            ushort rotation_count = reader.ReadUShort();
            ushort translation_count = reader.ReadUShort();

            int anim_offset = reader.ReadInt() + 32;
            int scale_offset = reader.ReadInt() + 32;
            int rotation_offset = reader.ReadInt() + 32;
            int translation_offset = reader.ReadInt() + 32;

            float[] scale_data = ReadFloatTable(scale_offset, scale_count, reader);
            short[] rotation_data = ReadShortTable(rotation_offset, rotation_count, reader);
            float[] translation_data = ReadFloatTable(translation_offset, translation_count, reader);

            Tracks = new Track[keyframe_count];
            reader.Seek(anim_offset);

            for (int i = 0; i < keyframe_count; i++)
            {
                Tracks[i].Translation = new Keyframe[3][];
                Tracks[i].Scale = new Keyframe[3][];
                Tracks[i].Rotation = new Keyframe[3][];

                // X components
                Tracks[i].Scale[0] = ReadFloatKeyframe(ref reader, scale_data);
                Tracks[i].Rotation[0] = ReadShortKeyframe(ref reader, rotation_data);
                Tracks[i].Translation[0] = ReadFloatKeyframe(ref reader, translation_data);

                // Y components
                Tracks[i].Scale[2] = ReadFloatKeyframe(ref reader, scale_data);
                Tracks[i].Rotation[2] = ReadShortKeyframe(ref reader, rotation_data);
                Tracks[i].Translation[2] = ReadFloatKeyframe(ref reader, translation_data);

                // Z components
                Tracks[i].Scale[1] = ReadFloatKeyframe(ref reader, scale_data);
                Tracks[i].Rotation[1] = ReadShortKeyframe(ref reader, rotation_data);
                Tracks[i].Translation[1] = ReadFloatKeyframe(ref reader, translation_data);
            }
        }

        private float[] ReadFloatTable(int offset, int count, EndianBinaryReader reader)
        {
            float[] floats = new float[count];

            reader.Seek(offset);

            for (int i = 0; i < count; i++)
            {
                floats[i] = reader.ReadFloat();
            }

            return floats;
        }

        private short[] ReadShortTable(int offset, int count, EndianBinaryReader reader)
        {
            short[] shorts = new short[count];

            reader.Seek(offset);

            for (int i = 0; i < count; i++)
            {
                shorts[i] = reader.ReadShort();
            }

            return shorts;
        }

        private Keyframe[] ReadFloatKeyframe(ref EndianBinaryReader reader, float[] data)
        {
            short count = reader.ReadShort();
            short index = reader.ReadShort();
            TangentMode tangent_mode = (TangentMode)reader.ReadShort();

            Keyframe[] key_data = new Keyframe[count];

            for (int i = 0; i < count; i++)
            {
                if (count == 1)
                {
                    key_data[i].Key = data[index];
                    continue;
                }

                int cur_index = index;

                if (tangent_mode == TangentMode.Symmetric)
                {
                    cur_index += 3 * i;
                }
                else
                {
                    cur_index += 4 * i;
                }

                key_data[i].Time = data[cur_index];
                key_data[i].Key = data[cur_index + 1];
                key_data[i].InTangent = data[cur_index + 2];

                if (tangent_mode == TangentMode.Symmetric)
                {
                    key_data[i].OutTangent = key_data[i].InTangent;
                }
                else
                {
                    key_data[i].OutTangent = data[cur_index + 3];
                }
            }

            return key_data;
        }

        private Keyframe[] ReadShortKeyframe(ref EndianBinaryReader reader, short[] data)
        {
            ushort count = reader.ReadUShort();
            ushort index = reader.ReadUShort();
            TangentMode tangent_mode = (TangentMode)reader.ReadShort();

            Keyframe[] key_data = new Keyframe[count];

            for (int i = 0; i < count; i++)
            {
                if (count == 1)
                {
                    key_data[i].Key = RotationShortToFloat(data[index], RotationFrac);
                    continue;
                }

                int cur_index = index;

                if (tangent_mode == TangentMode.Symmetric)
                {
                    cur_index += 3 * i;
                }
                else
                {
                    cur_index += 4 * i;
                }

                key_data[i].Time = data[cur_index];
                key_data[i].Key = RotationShortToFloat(data[cur_index + 1], RotationFrac);

                key_data[i].InTangent = RotationShortToFloat(data[cur_index + 2], RotationFrac);

                if (tangent_mode == TangentMode.Symmetric)
                {
                    key_data[i].OutTangent = key_data[i].InTangent;
                }
                else
                {
                    key_data[i].OutTangent = RotationShortToFloat(data[cur_index + 3], RotationFrac);
                }
            }

            return key_data;
        }

        public static float RotationShortToFloat(short rot, short rotation_frac)
        {
            float rot_scale = (float)Math.Pow(2f, rotation_frac) * (180f / 32768f);

            //float test = (rot << rotation_frac) * (360.0f / 65536.0F);
            float test = rot * rot_scale;
            return test;
        }

        public static short RotationFloatToShort(float rot, short rotation_frac)
        {
            float rot_scale = (float)Math.Pow(2f, rotation_frac) * (32768f / 180f);

            short test = (short)(rot * rot_scale);
            return test;
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            writer.Write("J3D1bck1".ToCharArray()); // Magic
            writer.Write((int)0); // Placeholder for filesize
            writer.Write((int)1); // Number of sections. It's only ever 1 for ANK1

            // These are placeholder for SVN that were never used.
            writer.Write((int)-1);
            writer.Write((int)-1);
            writer.Write((int)-1);

            // This spot, however, was used for hacking sound data into the animation.
            // It's the offset to the start of the sound data. Unsupported for now.
            writer.Write((int)-1);

            WriteANK1(ref writer);

            writer.Seek(8);
            writer.Write((int)writer.Length);
            writer.SeekEnd();
        }

        private void WriteANK1(ref EndianBinaryWriter writer)
        {
            int start_offset = writer.Length;

            int ScaleCount;
            int RotCount;
            int TransCount;

            int ScaleOffset;
            int RotOffset;
            int TransOffset;

            byte[] KeyframeData = WriteKeyframedata(out ScaleCount, out RotCount, out TransCount, out ScaleOffset, out RotOffset, out TransOffset);

            writer.Write("ANK1".ToCharArray()); // Magic
            writer.Write((int)0); // Placeholder for section size

            writer.Write((byte)LoopMode);
            writer.Write(RotationFrac);
            writer.Write(Duration);

            writer.Write((short)Tracks.Length);
            writer.Write((short)ScaleCount);
            writer.Write((short)RotCount);
            writer.Write((short)TransCount);

            writer.Write((int)0x40); // Keyframes offset
            writer.Write((int)ScaleOffset); // Scale bank offset
            writer.Write((int)RotOffset); // Rot bank offset
            writer.Write((int)TransOffset); // Trans bank offset

            writer.PadAlign(32);

            writer.Write(KeyframeData);

            writer.PadAlign(32);

            writer.Seek(start_offset + 4);
            writer.Write((int)(writer.Length - start_offset));
            writer.SeekEnd();
        }

        private byte[] WriteKeyframedata(out int ScaleCount, out int RotCount, out int TransCount, out int ScaleOffset, out int RotOffset, out int TransOffset)
        {
            List<float> scale_data = new List<float>() { 1.0f };
            List<short> rot_data = new List<short>() { 0 };
            List<float> trans_data = new List<float>() { 0.0f };
            byte[] keyframe_data;

            using (MemoryStream mem = new MemoryStream())
            {
                EndianBinaryWriter writer = new EndianBinaryWriter();

                foreach (Track t in Tracks) // Each bone
                {
                    WriteFloatKeyframe(ref writer, t.Scale[0], scale_data);
                    WriteShortKeyframe(ref writer, t.Rotation[0], rot_data);
                    WriteFloatKeyframe(ref writer, t.Translation[0], trans_data);

                    WriteFloatKeyframe(ref writer, t.Scale[1], scale_data);
                    WriteShortKeyframe(ref writer, t.Rotation[1], rot_data);
                    WriteFloatKeyframe(ref writer, t.Translation[1], trans_data);

                    WriteFloatKeyframe(ref writer, t.Scale[2], scale_data);
                    WriteShortKeyframe(ref writer, t.Rotation[2], rot_data);
                    WriteFloatKeyframe(ref writer, t.Translation[2], trans_data);
                }

                writer.PadAlignZero(32);

                ScaleOffset = (int)(writer.Position + 0x40);
                foreach (float f in scale_data)
                    writer.Write(f);

                writer.PadAlignZero(32);

                RotOffset = (int)(writer.Position + 0x40);
                foreach (short s in rot_data)
                    writer.Write(s);

                writer.PadAlignZero(32);

                TransOffset = (int)(writer.Position + 0x40);
                foreach (float f in trans_data)
                    writer.Write(f);

                keyframe_data = mem.ToArray();
            }

            ScaleCount = scale_data.Count;
            RotCount = rot_data.Count;
            TransCount = trans_data.Count;

            return keyframe_data;
        }

        private void WriteFloatKeyframe(ref EndianBinaryWriter writer, Keyframe[] keys, List<float> float_data)
        {
            if (keys.Length == 1) // Identity keyframes are easy because they always point to the first value in the list
            {
                if (!float_data.Contains(keys[0].Key))
                    float_data.Add(keys[0].Key);

                writer.Write((short)1); // Number of keys
                writer.Write((short)float_data.IndexOf(keys[0].Key)); // Index of first key
                writer.Write((short)0); // Tangent type, either piecewise or symmetric

                return;
            }

            writer.Write((short)keys.Length);
            writer.Write((short)float_data.Count);
            writer.Write((short)TangentMode.Symmetric);

            foreach (Keyframe k in keys)
            {
                float_data.Add(k.Time * 30.0f);
                float_data.Add(k.Key);
                float_data.Add(k.InTangent);
            }
        }

        private void WriteShortKeyframe(ref EndianBinaryWriter writer, Keyframe[] keys, List<short> short_data)
        {
            if (keys.Length == 1)
            {
                short rot_value = RotationFloatToShort(keys[0].Key, RotationFrac);

                if (!short_data.Contains(rot_value))
                    short_data.Add(rot_value);

                writer.Write((short)1);
                writer.Write((short)short_data.IndexOf(rot_value));
                writer.Write((short)0);

                return;
            }

            writer.Write((short)keys.Length);
            writer.Write((short)short_data.Count);
            writer.Write((short)TangentMode.Symmetric);

            foreach (Keyframe k in keys)
            {
                short_data.Add((short)(k.Time * 30.0f));
                short_data.Add(RotationFloatToShort(k.Key, RotationFrac));
                short_data.Add(RotationFloatToShort(k.InTangent, RotationFrac));
            }
        }
    }
}
