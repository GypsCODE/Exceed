using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Resources.Datagram {
    [StructLayout(LayoutKind.Sequential)]
    public struct Proc {
        [DefaultValue((byte)DatagramID.proc)]
        public DatagramID DatagramID;
        public ushort Target;
        public ProcType Type;
        public float Modifier;
        public int Duration;
    }
}
