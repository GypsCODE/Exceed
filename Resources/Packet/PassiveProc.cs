﻿using System.IO;

namespace Resources.Packet
{
    public class PassiveProc
    {
        public const int packetID = 8;

        public ulong source;
        public ulong target;
        public byte  type;
        //3pad
        public float modifier;
        public int   duration;
        public int   unknown;
        public long  guid3;

        public void read(BinaryReader reader)
        {
            source = reader.ReadUInt64();
            target = reader.ReadUInt64();
            type = reader.ReadByte();
            reader.ReadBytes(3);//pad
            modifier = reader.ReadSingle();
            duration = reader.ReadInt32();
            unknown = reader.ReadInt32();
            guid3 = reader.ReadInt64();
        }

        public void write(BinaryWriter writer)
        {
            writer.Write(source);
            writer.Write(target);
            writer.Write(type);
            writer.Write(new byte[3]);
            writer.Write(modifier);
            writer.Write(duration);
            writer.Write(unknown);
            writer.Write(guid3);
        }
    }
}
