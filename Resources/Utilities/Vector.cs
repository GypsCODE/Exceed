using System.IO;
using System.Runtime.InteropServices;

namespace Resources.Utilities {
    [StructLayout(LayoutKind.Sequential)]
    public struct LongVector {
        public long x, y, z;

        public LongVector(BinaryReader reader) {
            x = reader.ReadInt64();
            y = reader.ReadInt64();
            z = reader.ReadInt64();
        }

        public void Write(BinaryWriter writer) {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IntVector {
        public int x, y, z;
        
        public IntVector(BinaryReader reader) {
            x = reader.ReadInt32();
            y = reader.ReadInt32();
            z = reader.ReadInt32();
        }

        public void Write(BinaryWriter writer) {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FloatVector {
        public float x, y, z;

        public FloatVector(BinaryReader reader) {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
        }

        public void Write(BinaryWriter writer) {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ByteVector {
        public byte x, y, z;
        
        public ByteVector(BinaryReader reader) {
            x = reader.ReadByte();
            y = reader.ReadByte();
            z = reader.ReadByte();
        }

        public void Write(BinaryWriter writer) {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
        }
    }
}
