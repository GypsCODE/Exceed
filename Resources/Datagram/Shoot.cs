using Resources.Utilities;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Resources.Datagram {
    [StructLayout(LayoutKind.Sequential)]
    public struct Shoot {
        [DefaultValue((byte)DatagramID.shoot)]
        public DatagramID DatagramID;
        public LongVector Position;
        public FloatVector Velocity;
        public float Scale;
        public float Particles;
        public Projectile Projectile;
    }
}
