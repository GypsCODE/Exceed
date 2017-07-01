﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using Resources;
using Resources.Packet;
using Resources.Packet.Part;
using Resources.Datagram;

namespace Bridge {
    static class BridgeTCPUDP {
        static UdpClient udpToServer = new UdpClient();
        static TcpClient tcpToServer = new TcpClient();
        static TcpClient tcpToClient;
        static TcpListener listener;
        static BinaryWriter swriter, cwriter;
        static BinaryReader sreader, creader;
        static ushort guid;

        public static void Connect(Form1 form) {
            form.Log("connecting...");
            string serverIP = form.textBoxServerIP.Text;
            int serverPort = (int)form.numericUpDownPort.Value;
            try {
                tcpToServer.Connect(serverIP, serverPort);
                form.Log($"Connected");
            } catch(IOException ex) {
                form.Log($"Connection failed: \n{ex.Message}\n");
                return; //could not connect
            }
            swriter = new BinaryWriter(tcpToServer.GetStream());
            sreader = new BinaryReader(tcpToServer.GetStream());

            string publicKey = sreader.ReadString();

            var username = Hashing.Encrypt(publicKey, form.textBoxUsername.Text);
            swriter.Write(username.Length);
            swriter.Write(username); //Send username

            var salt = sreader.ReadBytes(16); // get salt

            var hash = Hashing.Encrypt(publicKey, Hashing.Hash(form.textBoxPassword.Text, salt));
            swriter.Write(hash.Length);
            swriter.Write(hash); //send hashed password

            switch(sreader.ReadByte()) {
                case 0: //success
                    udpToServer.Connect(serverIP, serverPort);
                    listener = new TcpListener(IPAddress.Parse("localhost"), 12345);
                    listener.Start();
                    Task.Factory.StartNew(ListenFromClientTCP);
                    ListenFromServerTCP(form);
                    break;
                case 1: //failed
                case 2: //banned
                default://unknown response
                    tcpToServer.Close();
                    break;
            }
        }

        public static void Stop() {
            listener.Stop();
            tcpToClient.Close();
            udpToServer.Close();
            tcpToServer.Close();
        }

        public static void ListenFromClientTCP() {
            tcpToClient = listener.AcceptTcpClient();
            listener.Stop();
            int packetID = -1;
            while(tcpToClient.Connected) {
                try {
                    packetID = sreader.ReadInt32();
                } catch (IOException) {
                    break;
                }
                ProcessPacket(packetID);
            }
            var dc = new Disconnect() {
                Guid = guid
            };
            SendUDP(dc.data);
            tcpToClient.Close();
        }
        public static void ListenFromServerTCP(Form1 form) {
            byte packetID = 255;
            while(tcpToServer.Connected) {
                try {
                    packetID = sreader.ReadByte(); //we can use byte here because it doesn't contain vanilla packets
                    //player list updates
                } catch(IOException) {
                    break;
                }
                //process packet
            }
            tcpToClient.Close();
        }
        public static void ListenFromServerUDP() {
            IPEndPoint source = null;
            while(true) {
                byte[] datagram = udpToServer.Receive(ref source);
                if(true) { //source == server
                    ProcessDatagram(datagram); //might require try n' catch here, we'll see
                }
            }
        }

        public static void ProcessDatagram(byte[] datagram) {
            switch ((Database.DatagramID)datagram[0]) {
                case Database.DatagramID.entityUpdate:
                    break;
                case Database.DatagramID.attack:
                    var attack = new Attack(datagram);

                    var hit = new Hit() {
                        target = attack.Target,
                        damage = attack.Damage,
                        critical = Convert.ToInt32(attack.Critical),
                        stuntime = attack.Stuntime,
                        //hit.position = attack.
                        skill = attack.Skill,
                        type = (byte)attack.Type,
                        showlight = Convert.ToByte(attack.ShowLight)
                    };
                    hit.Write(cwriter, true);
                    break;
                case Database.DatagramID.shoot:
                    var shootDatagram = new Resources.Datagram.Shoot(datagram);

                    var shootPacket = new Resources.Packet.Shoot() {
                        position = shootDatagram.Position,
                        velocity = shootDatagram.Velocity,
                        scale = shootDatagram.Scale,
                        particles = shootDatagram.Particles,
                        projectile = (int)shootDatagram.Projectile
                    };
                    shootPacket.Write(cwriter, true);
                    break;
                case Database.DatagramID.proc:
                    var proc = new Proc(datagram);

                    var passiveProc = new PassiveProc() {
                        target = proc.Target,
                        type = (byte)proc.Type,
                        modifier = proc.Modifier,
                        duration = proc.Duration
                    };
                    passiveProc.Write(cwriter, true);
                    break;
                case Database.DatagramID.chat:
                    var chat = new Chat(datagram);

                    var chatMessage = new ChatMessage() {
                        sender = chat.Sender,
                        message = chat.Text
                    };
                    chatMessage.Write(cwriter, true);
                    break;
                case Database.DatagramID.time:
                    var igt = new InGameTime(datagram);

                    var time = new Time() {
                        time = igt.Time
                    };
                    time.Write(cwriter, true);
                    break;
                case Database.DatagramID.interaction:
                    var interaction = new Interaction(datagram);

                    var entityAction = new EntityAction() {
                        chunkX = interaction.ChunkX,
                        chunkY = interaction.ChunkY,
                        index = interaction.Index,
                        type = (byte)Database.ActionType.staticInteraction
                    };
                    entityAction.Write(cwriter, true);
                    break;
                case Database.DatagramID.staticUpdate:
                    var staticUpdate = new StaticUpdate(datagram);

                    var staticEntity = new StaticEntity() {
                        chunkX = (int)(staticUpdate.Position.x / (65536 * 256)),
                        chunkY = (int)(staticUpdate.Position.y / (65536 * 256)),
                        id = staticUpdate.Id,
                        type = (int)staticUpdate.Type,
                        position = staticUpdate.Position,
                        rotation = (int)staticUpdate.Direction,
                        size = staticUpdate.Size,
                        closed = Convert.ToInt32(staticUpdate.Closed),
                        time = staticUpdate.Time,
                        guid = staticUpdate.User
                    };
                    var serverUpdate = new ServerUpdate();
                    serverUpdate.statics.Add(staticEntity);
                    serverUpdate.Write(cwriter, true);
                    break;
                case Database.DatagramID.block:
                    //var block = new Block(datagram);
                    //TODO
                    break;
                case Database.DatagramID.particle:
                    var particleDatagram = new Resources.Datagram.Particle(datagram);

                    var particleSubPacket = new Resources.Packet.Part.Particle();
                    particleSubPacket.position = particleDatagram.Position;
                    particleSubPacket.velocity = particleDatagram.Velocity;
                    particleSubPacket.color.x = particleDatagram.Color.R / 255;
                    particleSubPacket.color.y = particleDatagram.Color.G / 255;
                    particleSubPacket.color.z = particleDatagram.Color.B / 255;
                    particleSubPacket.alpha = particleDatagram.Color.A / 255;
                    particleSubPacket.size = particleDatagram.Size;
                    particleSubPacket.count = particleDatagram.Count;
                    particleSubPacket.type = (int)particleDatagram.Type;
                    particleSubPacket.spread = particleDatagram.Spread;

                    var serverUpdateP = new ServerUpdate();
                    serverUpdateP.particles.Add(particleSubPacket);
                    serverUpdateP.Write(cwriter, true);
                    break;
                case Database.DatagramID.connect:
                    var connect = new Connect(datagram);
                    guid = connect.Guid;

                    var join = new Join() {
                        guid = connect.Guid
                    };
                    join.Write(cwriter, true);

                    var mapseed = new MapSeed() {
                        seed = connect.Mapseed
                    };
                    mapseed.Write(cwriter, true);
                    break;
                case Database.DatagramID.disconnect:
                    break;
                case Database.DatagramID.players:
                    break;
                default:
                    break;
            }
        }
        public static void ProcessPacket(int packetID) {
            switch((Database.PacketID)packetID) {
                case Database.PacketID.entityUpdate:
                    #region entity update
                    var entityUpdate = new EntityUpdate(creader);
                    break;
                #endregion
                case Database.PacketID.entityAction:
                    #region entity action
                    EntityAction entityAction = new EntityAction(creader);
                    switch((Database.ActionType)entityAction.type) {
                        case Database.ActionType.talk:
                            break;

                        case Database.ActionType.staticInteraction:
                            break;

                        case Database.ActionType.pickup:
                            break;

                        case Database.ActionType.drop: //send item back to dropper because dropping is disabled to prevent chatspam
                            var pickup = new Pickup() {
                                guid = guid,
                                item = entityAction.item
                            };

                            var serverUpdate6 = new ServerUpdate();
                            serverUpdate6.pickups.Add(pickup);
                            serverUpdate6.Write(cwriter, true);
                            break;

                        case Database.ActionType.callPet:
                            break;

                        default:
                            //unknown type
                            break;
                    }
                    break;
                #endregion
                case Database.PacketID.hit:
                    #region hit
                    var hit = new Hit(creader);

                    var attack = new Attack() {
                        Target = (ushort)hit.target,
                        Damage = hit.damage,
                        Stuntime = hit.stuntime,
                        Skill = hit.skill,
                        Type = (Database.DamageType)hit.type,
                        ShowLight = Convert.ToBoolean(hit.showlight),
                        Critical = Convert.ToBoolean(hit.critical)
                    };
                    SendUDP(attack.data);
                    break;
                #endregion
                case Database.PacketID.passiveProc:
                    #region passiveProc
                    var passiveProc = new PassiveProc(sreader);

                    var proc = new Proc() {
                        Target = (ushort)passiveProc.target,
                        Type = (Database.ProcType)passiveProc.type,
                        Modifier = passiveProc.modifier,
                        Duration = passiveProc.duration
                    };
                    SendUDP(proc.data);
                    break;
                #endregion
                case Database.PacketID.shoot:
                    #region shoot
                    var shootPacket = new Resources.Packet.Shoot(sreader);

                    var shootDatagram = new Resources.Datagram.Shoot() {
                        Position = shootPacket.position,
                        Velocity = shootPacket.velocity,
                        Scale = shootPacket.scale,
                        Particles = shootPacket.particles,
                        Projectile = (Database.Projectile)shootPacket.projectile
                    };
                    SendUDP(shootDatagram.data);
                    break;
                #endregion
                case Database.PacketID.chat:
                    #region chat
                    var chatMessage = new ChatMessage(sreader);

                    var chat = new Chat(chatMessage.message) {
                        Sender = (ushort)chatMessage.sender
                    };
                    SendUDP(chat.data);
                    break;
                #endregion
                case Database.PacketID.chunk:
                    #region chunk discovered
                    var chunk = new Chunk(sreader);//currently not doing anything with this
                    break;
                #endregion
                case Database.PacketID.sector:
                    #region sector discovered
                    var sector = new Sector(sreader);//currently not doing anything with this
                    break;
                #endregion
                case Database.PacketID.version:
                    #region version
                    var version = new ProtocolVersion(sreader);
                    SendUDP(new Connect().data);
                    break;
                #endregion
                default:
                    //unknown packet id
                    break;
            }
        }

        public static void SendUDP(byte[] data) {
            udpToServer.Send(data, data.Length);
        }
    }
}
