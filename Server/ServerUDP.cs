﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using Resources;
using Resources.Datagram;
using System.IO;
using System.Threading;

namespace Server {
    class ServerUDP {
        //public class Player {
        //    public TcpClient tcp;
        //    public BinaryWriter writer;
        //    public BinaryReader reader;

        //    public Player(TcpClient client, ServerUDP server) {
        //        tcp = client;
        //        writer = new BinaryWriter(tcp.GetStream());
        //        reader = new BinaryReader(tcp.GetStream());
        //        Task.Factory.StartNew(() => Listen(server));
        //    }

        //    public void Listen(ServerUDP server) {
        //        int packetID = -1;
        //        while(tcp.Connected) {
        //            try {
        //                packetID = reader.ReadInt32();
        //            } catch(IOException) {
        //                server.Kick(this);
        //                break;
        //            }
        //            server.ProcessTCP( packetID, this);
        //        }
        //    }
        //}
        
        UdpClient udp;
        Dictionary<ulong, Player> connections = new Dictionary<ulong, Player>();
        TcpListener listener;

        Tuple<string, string> encryptionKeys = Hashing.CreateKeyPair();

        public ServerUDP(int port) {
            udp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            Task.Factory.StartNew(UDPListen);
            listener = new TcpListener(IPAddress.Any, port);
            Task.Factory.StartNew(TCPListen);
            //loop sending player list to bridge
        }

        public void TCPListen() {
            while(true) {
                Player player = new Player(listener.AcceptTcpClient());
                player.writer.Write(encryptionKeys.Item2);

                var userLength = player.reader.ReadInt32();
                var username = Hashing.Decrypt(encryptionKeys.Item1, player.reader.ReadBytes(userLength));
                
                //get data from db
                var salt = new byte[16];
                var hash = new byte[20];
                player.writer.Write(salt); //send salt

                var hashLength = player.reader.ReadInt32();
                var clientHash = Hashing.DecryptB(encryptionKeys.Item1, player.reader.ReadBytes(hashLength));

                player.writer.Write(0); //No db for now
                /*
                if(hash.SequenceEqual(clientHash)) {
                    player.writer.Write(0); // success;
                } else {
                    player.writer.Write(1); // Wrong password
                }*/
            }
        }
        public void UDPListen() {
            IPEndPoint source = null;
            while(true) {
                byte[] datagram = udp.Receive(ref source);
                ProcessDatagram(datagram, source);
            }
        }

        public void UDPSendToAll(byte[] data, IPEndPoint source = null) {
            foreach(var player in connections.Values) {
                var adress = player.tcp.Client.RemoteEndPoint as IPEndPoint;
                if(adress != source)
                    udp.Send(data, data.Length, adress);
            }
        }

        public void Alert(string message, ConsoleColor color = ConsoleColor.White) {
             var p = new Chat(message) {
                Sender = 0
            };
            UDPSendToAll(p.data);
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = oldColor;
        }
        
        public void Kick(Player player) {
            throw new NotImplementedException();
            
            Disconnect p = new Disconnect() {
                //Guid = player.guid
            };

            UDPSendToAll(p.data);
        }

        public void ProcessPacket(int packetID, Player player) {

        }
        public void ProcessDatagram(byte[] datagram, IPEndPoint source) {
            var datagramID = (Database.DatagramID)datagram[0];
            switch(datagramID) {
                case Database.DatagramID.entityUpdate:
                    break;
                case Database.DatagramID.hit:
                    break;
                case Database.DatagramID.shoot:
                    break;
                case Database.DatagramID.proc:
                    break;
                case Database.DatagramID.chat:
                    break;
                case Database.DatagramID.time:
                    break;
                case Database.DatagramID.interaction:
                    break;
                case Database.DatagramID.staticUpdate:
                    break;
                case Database.DatagramID.block:
                    break;
                case Database.DatagramID.particle:
                    break;
                case Database.DatagramID.connect:
                    break;
                case Database.DatagramID.disconnect:
                    break;
                case Database.DatagramID.players:
                    break;
                default:
                    Console.WriteLine("unknown DatagramID: " + datagramID);
                    break;
            }
        }
    }
}
