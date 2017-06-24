﻿using System.IO;
using Resources.Utilities;

namespace Resources.Packet
{
    public class Hit
    {
        public const int packetID = 7;

        public ulong attacker;
        public ulong target;
        public float damage;
        public int critical;//bool?
        public int stuntime;
        public int paddingA;
        public LongVector position;
        public FloatVector direction = new FloatVector();
        public byte skill;
        public byte type;
        public byte showlight;
        public byte paddingB;

        public void read(BinaryReader reader)
        {
            attacker = reader.ReadUInt64();
            target = reader.ReadUInt64();
            damage = reader.ReadSingle();
            critical = reader.ReadInt32();
            stuntime = reader.ReadInt32();
            paddingA = reader.ReadInt32();
            position = new LongVector(); position.read(reader);
            direction = new FloatVector(); direction.read(reader);
            skill = reader.ReadByte();
            type = reader.ReadByte();
            showlight = reader.ReadByte();
            paddingB = reader.ReadByte();
        }

        public void write(BinaryWriter writer)
        {
            writer.Write(attacker);
            writer.Write(target);
            writer.Write(damage);
            writer.Write(critical);
            writer.Write(stuntime);
            writer.Write(paddingA);
            position.write(writer);
            direction.write(writer);
            writer.Write(skill);
            writer.Write(type);
            writer.Write(showlight); 
            writer.Write(paddingB);
        }
    }
}
