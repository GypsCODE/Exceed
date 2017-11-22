using Resources.Utilities;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Resources.Datagram {
    [StructLayout(LayoutKind.Sequential)]
    public struct StaticUpdate {
        [DefaultValue((byte)DatagramID.staticUpdate)]
        public DatagramID DatagramID;
        public ushort Id;
        public LongVector Position;
        public FloatVector Size;
        /// <summary>
        /// guid of player who interacts with it
        /// </summary>
        public ushort User;
        /// <summary>
        /// for closing animation
        /// </summary>
        public ushort Time;
        public StaticUpdateType Type;
        public StaticRotation Direction;
        public bool Closed;
    }
}
