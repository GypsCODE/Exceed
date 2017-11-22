using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Resources.Datagram {
    [StructLayout(LayoutKind.Sequential)]
    public struct SpecialMove {
        [DefaultValue((byte)DatagramID.specialMove)]
        public DatagramID DatagramID;
        public ushort Guid;
        public SpecialMoveID Id;
    }
}
