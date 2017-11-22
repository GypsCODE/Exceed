using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Resources.Datagram {
    [StructLayout(LayoutKind.Sequential)]
    public struct InGameTime {
        [DefaultValue((byte)DatagramID.time)]
        public DatagramID DatagramID;
        public int Time;

        public InGameTime(int time) {
            DatagramID = DatagramID.time;
            Time = time;
        }
    }
}
