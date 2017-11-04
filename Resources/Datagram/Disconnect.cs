﻿using System;

namespace Resources.Datagram {
    public class Disconnect {
        public DatagramID DatagramID {
            get { return (DatagramID)data[0]; }
            private set { data[0] = (byte)value; }
        }
        public ushort Guid {
            get { return BitConverter.ToUInt16(data, 1); }
            set { BitConverter.GetBytes(value).CopyTo(data, 1);}
        }

        public byte[] data;

        public Disconnect() {
            data = new byte[3];
            DatagramID = DatagramID.disconnect;
        }
        public Disconnect(byte[] data) {
            this.data = data;
        }
    }
}
