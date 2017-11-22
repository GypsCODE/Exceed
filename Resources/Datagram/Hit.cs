using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Resources.Datagram {
    [StructLayout(LayoutKind.Sequential)]
    public struct Attack {
        [DefaultValue((byte)DatagramID.attack)]
        public DatagramID DatagramID;
        public ushort Target;
        public float Damage;
        public int Stuntime;
        public byte Skill;
        public DamageType Type;

        byte booleans;
        public bool ShowLight {
            get => Tools.GetBit(booleans, 0);
            set => Tools.SetBit(ref booleans, value, 0);
        }
        public bool Critical {
            get => Tools.GetBit(booleans, 1);
            set => Tools.SetBit(ref booleans, value, 1);
        }
    }
}
