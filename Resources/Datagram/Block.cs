using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Resources.Datagram {
    [StructLayout(LayoutKind.Sequential)]
    public struct Block {
        [DefaultValue((byte)DatagramID.block)]
        public DatagramID DatagramID;
        public short Length;
        public byte Compressed;
    }
}
