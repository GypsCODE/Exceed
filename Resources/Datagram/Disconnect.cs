using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Resources.Datagram {
    [StructLayout(LayoutKind.Sequential)]
    public struct Disconnect {
        [DefaultValue((byte)DatagramID.disconnect)]
        public DatagramID DatagramID;
        public ushort Guid;

        public Disconnect(ushort guid) {
            DatagramID = DatagramID.disconnect;
            Guid = guid;
        }
    }
}
