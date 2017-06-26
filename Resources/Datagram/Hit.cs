﻿using System;

namespace Resources.Datagram {
    public class Hit {
        public Database.DatagramID DatagramID {
            get { return (Database.DatagramID)data[0]; }
            private set { data[0] = (byte)value; }
        }
        public ushort Target {
            get { return BitConverter.ToUInt16(data, 1); }
            set { BitConverter.GetBytes(value).CopyTo(data, 1); }
        }
        public float Damage {
            get { return BitConverter.ToSingle(data, 3); }
            set { BitConverter.GetBytes(value).CopyTo(data, 3); }
        }
        /// <summary>
        /// Stuntime in milliseconds
        /// </summary>
        public int StunTime {
            get { return BitConverter.ToInt32(data, 7); }
            set { BitConverter.GetBytes(value).CopyTo(data, 7); }
        }
        public byte Skill {
            get { return data[11]; }
            set { data[11] = value; }
        }
        public Database.DamageType Type {
            get { return (Database.DamageType)(data[12]); }
            set { data[12] = (byte)value; }
        }
        public bool ShowLight {
            get { return data[13].GetBit(0); }
            set { data[13].SetBit(value, 0); }
        }
        public bool Critical {
            get { return data[13].GetBit(1); }
            set { data[13].SetBit(value, 1); }
        }

        public byte[] data;

        public Hit() {
            data = new byte[13];
            DatagramID = Database.DatagramID.hit;
        }

        public Hit(byte[] data) {
            this.data = data;
        }
    }
}