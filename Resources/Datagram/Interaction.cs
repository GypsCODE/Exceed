using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Resources.Datagram {
    [StructLayout(LayoutKind.Sequential)]
    public struct Interaction {
        [DefaultValue((byte)DatagramID.interaction)]
        public DatagramID DatagramID;
        public ushort ChunkX;
        public ushort ChunkY;
        public ushort Index;
    }
}
