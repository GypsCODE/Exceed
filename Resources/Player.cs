﻿using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using Resources.Packet;

namespace Resources {
    public class Player {
        public TcpClient tcp;
        public BinaryWriter writer;
        public BinaryReader reader;
        public bool playing = false;
        public bool available = true;
        public bool admin = false;
        public IPEndPoint IpEndPoint { get; private set; }
        public EntityUpdate entityData = new EntityUpdate();
        public string MAC;
        public ushort lastTarget;
        public Stopwatch lagMeter;

        public Player(TcpClient client) {
            tcp = client;
            tcp.NoDelay = true;

            Stream stream = tcp.GetStream();
            writer = new BinaryWriter(stream);
            reader = new BinaryReader(stream);

            IpEndPoint = tcp.Client.RemoteEndPoint as IPEndPoint;
            lagMeter = new Stopwatch();
            lagMeter.Start();
        }
    }
}