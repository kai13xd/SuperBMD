using static System.Buffers.Binary.BinaryPrimitives;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SuperBMD.Rigging;
using SuperBMD.Util;
using System;
namespace Kai
{
    public ref struct EndianBinaryReader
    {
        public int Position = 0;
        public Stack<int> rememberPos = new(3);
        private Span<byte> buffer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EndianBinaryReader(string filepath)
        {
            buffer = File.ReadAllBytes(filepath);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(int position) => Position = position;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(int bytes = 1) => Position += bytes;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remember() => rememberPos.Push(Position);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Recall() => Position = rememberPos.Pop();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Align(int alignment)
        {
            int remainder;
            if ((remainder = Position % alignment) == 0)
                return;
            Position += alignment - remainder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBool() => buffer[Position++] == 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte() => buffer[Position++];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ReadBytes(int count)
        {
            var buff = new byte[count];
            for (int i = 0; i < count; i++)
                buff[i] = buffer[Position++];
            return buff;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte() => (sbyte)buffer[Position++];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char ReadChar() => (char)buffer[Position++];
        public ushort ReadUShort()
        {
            var value = ReadUInt16BigEndian(buffer.Slice(Position));
            Position += 2;
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadShort()
        {
            var value = ReadInt16BigEndian(buffer.Slice(Position));
            Position += 2;
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt()
        {
            var value = ReadInt32BigEndian(buffer.Slice(Position));
            Position += 4;
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt()
        {
            var value = ReadUInt32BigEndian(buffer.Slice(Position));
            Position += 4;
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadLong()
        {
            var value = ReadInt64BigEndian(buffer.Slice(Position));
            Position += 8;
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUlong()
        {
            var value = ReadUInt64BigEndian(buffer.Slice(Position));
            Position += 8;
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public System.Half ReadHalf()
        {
            var value = ReadHalfBigEndian(buffer.Slice(2));
            Position += 2;
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadFloat()
        {
            var value = ReadSingleBigEndian(buffer.Slice(Position));
            Position += 4;
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            var value = ReadDoubleBigEndian(buffer.Slice(Position));
            Position += 8;
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PeekInt() => ReadInt32BigEndian(buffer.Slice(Position));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte PeekByte(int offset = 0) => buffer[Position + offset];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char PeekChar(int offset = 0) => (char)buffer[Position + offset];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe string ReadString(char terminator)
        {
            Span<char> chars = stackalloc char[32];
            int i = 0;
            do
            {
                chars[i] = ReadChar();

            } while (chars[i++] != terminator);

            return new string(chars.Slice(0, i - 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe string ReadString(int charCount)
        {
            var s = new String('\0', charCount);
            if (s.Length > 0)
            {
                fixed (char* dst = s)
                fixed (byte* src = &buffer[Position])
                    AsciiToUnicode(dst, src, s.Length);
            }
            Position += charCount;
            return s;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OpenTK.Mathematics.Vector3 ReadVector3()
        {
            var vec = new OpenTK.Mathematics.Vector3();
            for (int i = 0; i < 3; i++)
                vec[i] = ReadFloat();
            return vec;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void AsciiToUnicode(char* dst, byte* src, int c)
        {
            for (; c > 0 && ((long)dst & 0xF) != 0; c--)
                *dst++ = (char)*src++;
            for (; (c -= 0x10) >= 0; src += 0x10, dst += 0x10)
                Vector.Widen(Unsafe.AsRef<Vector<byte>>(src),
                             out Unsafe.AsRef<Vector<ushort>>(dst + 0),
                             out Unsafe.AsRef<Vector<ushort>>(dst + 8));
            for (c += 0x10; c > 0; c--)
                *dst++ = (char)*src++;
        }
    }

    //Stack-based file writer
    public unsafe ref struct EndianBinaryWriter
    {
        private const int bufferSize = 8;
        public Span<Byte> SpanView = new byte[bufferSize];
        private FileStream fileStream;
        public int Position => (int)fileStream.Position;
        public int FileLength => (int)fileStream.Length;

        public EndianBinaryWriter(string fileName)
        {
            fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
        }


        public void Dispose()
        {
            fileStream.Dispose();
            fileStream.Close();
        }
        public void Close()
        {
            Dispose();
        }

        public void Seek(int position)
        {
            fileStream.Seek((long)position, SeekOrigin.Begin);
        }
        public void SeekEnd() => fileStream.Position = fileStream.Length;
        public void Skip(int bytes = 1) => fileStream.Position += bytes;

        public void Write<T>(T val) where T : IEquatable<T>
        {
            var size = Marshal.SizeOf<T>();
            switch (val)
            {
                case byte value:
                    fileStream.WriteByte(value);
                    break;
                case sbyte value:
                    fileStream.WriteByte((byte)value);
                    break;
                case char value:
                    fileStream.WriteByte((byte)value);
                    break;
                case bool value:
                    fileStream.WriteByte((byte)(value ? 1 : 0));
                    break;
                case short value:
                    WriteInt16BigEndian(SpanView, value);
                    fileStream.Write(SpanView.Slice(0, size));
                    break;
                case ushort value:
                    WriteUInt16BigEndian(SpanView, value);
                    fileStream.Write(SpanView.Slice(0, size));
                    break;
                case int value:
                    WriteInt32BigEndian(SpanView, value);
                    fileStream.Write(SpanView.Slice(0, size));
                    break;
                case uint value:
                    WriteUInt32BigEndian(SpanView, value);
                    fileStream.Write(SpanView.Slice(0, size));
                    break;
                case System.Half value:
                    WriteHalfBigEndian(SpanView, value);
                    fileStream.Write(SpanView.Slice(0, size));
                    break;
                case float value:
                    WriteSingleBigEndian(SpanView, value);
                    fileStream.Write(SpanView.Slice(0, size));
                    break;
                case double value:
                    WriteDoubleBigEndian(SpanView, value);
                    fileStream.Write(SpanView.Slice(0, size));
                    break;
                default:
                    throw new Exception("Numeric type not supported!");
            }
        }

        public void Write(OpenTK.Mathematics.Vector4 vector)
        {
            Write(vector.X);
            Write(vector.Y);
            Write(vector.Z);
            Write(vector.W);
        }

        public void Write(OpenTK.Mathematics.Vector3 vector)
        {
            Write(vector.X);
            Write(vector.Y);
            Write(vector.Z);
        }

        public void Write(OpenTK.Mathematics.Vector2 vec2)
        {
            Write(vec2.X);
            Write(vec2.Y);
        }

        public void Write(SuperBMD.Util.Color color)
        {
            Write((byte)(color.R * 255));
            Write((byte)(color.G * 255));
            Write((byte)(color.B * 255));
            Write((byte)(color.A * 255));
        }

        public void Write(string str)
        {
            foreach (var c in str)
                Write(c);
        }

        public void Write(ReadOnlySpan<char> span)
        {
            foreach (var value in span)
                Write(value);
        }

        public void Write(Span<byte> span)
        {
            foreach (var value in span)
                Write(value);
        }

        public void Write(Matrix3x4 mat)
        {
            Write(mat.Row0);
            Write(mat.Row1);
            Write(mat.Row2);
        }

        public void Write(Matrix4 mat)
        {
            Write(mat.Row0);
            Write(mat.Row1);
            Write(mat.Row2);
            Write(mat.Row3);
        }

        const string padding = "Kai was here Kai was here Kai was here Kai was here";
        public void PadAlign(int alignment)
        {
            long nextAligned = (FileLength + (alignment - 1)) & ~(alignment - 1);
            long delta = nextAligned - FileLength;
            fileStream.Position = (long)FileLength;
            for (int i = 0; i < delta; i++)
            {
                fileStream.WriteByte((byte)padding[i]);
            }
        }
        public void PadAlign(int alignment, int offset)
        {
            long nextAligned = (offset + (alignment - 1)) & ~(alignment - 1);
            long delta = nextAligned - offset;
            fileStream.Position = (long)FileLength;
            for (int i = 0; i < delta; i++)
            {
                fileStream.WriteByte((byte)padding[i]);
            }
        }
        public void PadAlignZero(int alignment)
        {
            long nextAligned = (FileLength + (alignment - 1)) & ~(alignment - 1);
            long delta = nextAligned - FileLength;
            fileStream.Position = (long)FileLength;
            for (int i = 0; i < delta; i++)
            {
                fileStream.WriteByte((byte)0);
            }
        }
        public void Write(BoundingSphere sphere)
        {
            Write(sphere.Radius);
            Write(sphere.Min);
            Write(sphere.Max);
        }
        public void Write(Bone bone)
        {
            Write(bone.MatrixType);
            Write(bone.InheritParentScale);
            Write((sbyte)-1);

            ushort[] compressRot = J3DUtility.CompressRotation(bone.Rotation.ToEulerAngles());

            Write(bone.Scale);
            Write(compressRot[0]);
            Write(compressRot[1]);
            Write(compressRot[2]);
            Write((short)-1);
            Write(bone.Translation);
            Write(bone.Bounds);
        }
    }
}