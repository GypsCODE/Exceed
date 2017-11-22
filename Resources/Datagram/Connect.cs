using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Resources.Datagram {
    [StructLayout(LayoutKind.Sequential)]
    public struct Connect {
        [DefaultValue((byte)DatagramID.connect)]
        public DatagramID DatagramID;
        public ushort Guid;
        public int Mapseed;

        public Connect(ushort guid, int mapseed) : this() {
            DatagramID = DatagramID.connect;
            Guid = guid;
            Mapseed = mapseed;
        }
    }
}
