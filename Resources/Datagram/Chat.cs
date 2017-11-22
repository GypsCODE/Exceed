using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Resources.Datagram {
    [StructLayout(LayoutKind.Sequential)]
    public struct Chat {
        [DefaultValue((byte)DatagramID.chat)]
        public DatagramID DatagramID;
        public ushort Sender;
        public byte Length;
        public string Text;

        public Chat(ushort sender, string message) {
            if(message.Length > 255) {
                throw new OverflowException("Message to long");
            }

            DatagramID = DatagramID.chat;
            Sender = sender;
            Length = (byte)message.Length;
            Text = message;
        }

        /// <summary>
        /// Temporary fix for string Marshal problems
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes() {
            var data = new byte[1 + 2 + 1 + Length];
            data[0] = (byte)DatagramID;
            BitConverter.GetBytes(Sender).CopyTo(data, 1);
            data[3] = Length;
            Encoding.ASCII.GetBytes(Text).CopyTo(data, 4);
            return data;
        }
    }
}
