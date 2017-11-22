using Resources.Utilities;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Resources.Datagram {
    [StructLayout(LayoutKind.Sequential)]
    public struct Particle {
        [DefaultValue((byte)DatagramID.particle)]
        public DatagramID DatagramID;
        public LongVector Position;
        public FloatVector Velocity;
        public byte ColorA;
        public byte ColorR;
        public byte ColorG;
        public byte ColorB;

        public Color Color {
            get => Color.FromArgb(ColorA, ColorR, ColorG, ColorB);
            set {
                ColorA = value.A;
                ColorR = value.R;
                ColorG = value.G;
                ColorB = value.B;
            }
        }
        public float Size;
        public ushort Count;
        public ParticleType Type;
        public float Spread;
    }
}
