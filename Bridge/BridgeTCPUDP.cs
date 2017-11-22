﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

using Resources;
using Resources.Packet;
using Resources.Packet.Part;
using Resources.Datagram;

namespace Bridge {
    static class BridgeTCPUDP {
        public static UdpClient udpToServer;
        public static TcpClient tcpToServer, tcpToClient;
        public static TcpListener tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 12345); //hardcoded because client port can't be changed
        public static BinaryWriter swriter, cwriter;
        public static BinaryReader sreader, creader;
        public static ushort guid;
        public static Form1 form;
        public static bool connected = false;
        public static Dictionary<long, EntityUpdate> players = new Dictionary<long, EntityUpdate>();
        public static ushort lastTarget;

        public static void Connect() {
            form.Log("connecting...", Color.DarkGray);
            string serverIP = form.textBoxServerIP.Text;
            int serverPort = (int)form.numericUpDownPort.Value;

            try {
                tcpToServer = new TcpClient() { NoDelay = true };
                tcpToServer.Connect(serverIP, serverPort);

                udpToServer = new UdpClient(tcpToServer.Client.LocalEndPoint as IPEndPoint);
                udpToServer.Connect(serverIP, serverPort);
            }
            catch (SocketException) {//connection refused
                Close();
                form.Log("failed\n", Color.Red);
                form.EnableButtons();
                return;
            }
            form.Log("connected\n", Color.Green);

            Stream stream = tcpToServer.GetStream();
            swriter = new BinaryWriter(stream);
            sreader = new BinaryReader(stream);

            form.Log("checking version...", Color.DarkGray);
            swriter.Write(Database.bridgeVersion);
            if (!sreader.ReadBoolean()) {
                form.Log("mismatch\n", Color.Red);
                form.buttonDisconnect.Invoke(new Action(form.buttonDisconnect.PerformClick));
                return;
            }
            form.Log("match\n", Color.Green);
            form.Log("logging in...", Color.DarkGray);
            swriter.Write(form.textBoxUsername.Text);
            swriter.Write(form.textBoxPassword.Text);
            swriter.Write(NetworkInterface.GetAllNetworkInterfaces().Where(nic => nic.OperationalStatus == OperationalStatus.Up).Select(nic => nic.GetPhysicalAddress().ToString()).FirstOrDefault());
            switch ((AuthResponse)sreader.ReadByte()) {
                case AuthResponse.success:
                    if (sreader.ReadBoolean()) {//if banned
                        MessageBox.Show(sreader.ReadString());//ban message
                        form.Log("you are banned\n", Color.Red);
                        goto default;
                    }
                    break;
                case AuthResponse.unknownUser:
                    form.Log("unknown username\n", Color.Red);
                    goto default;
                case AuthResponse.wrongPassword:
                    form.Log("wrong password\n", Color.Red);
                    goto default;
                default:
                    form.buttonDisconnect.Invoke(new Action(form.buttonDisconnect.PerformClick));
                    return;
            }
            form.Log("success\n", Color.Green);
            connected = true;
            
            swriter.Write((byte)0);//request query
            new Thread(new ThreadStart(ListenFromServerTCP)).Start();
            new Thread(new ThreadStart(ListenFromServerUDP)).Start();
            ListenFromClientTCP();
        }
        public static void Close() {
            connected = false;
            form.Invoke(new Action(() => form.listBoxPlayers.Items.Clear()));
            LingerOption lingerOption = new LingerOption(true, 0);
            try {
                udpToServer.Close();
                udpToServer = null;
            }
            catch { }
            try {
                tcpToServer.LingerState = lingerOption;
                tcpToServer.Client.Close();
                tcpToServer.Close();
                udpToServer = null;
            }
            catch { }
            try {
                tcpToClient.LingerState = lingerOption;
                tcpToClient.Client.Close();
                tcpToClient.Close();
                udpToServer = null;
            }
            catch { }
            try {
                tcpListener.Stop();
            }
            catch { }
            players.Clear();
        }

        public static void ListenFromClientTCP() {
            while (connected) {
                bool WSAcancellation = false;
                try {
                    tcpListener.Start();
                    WSAcancellation = true;
                    tcpToClient = tcpListener.AcceptTcpClient();
                }
                catch (SocketException ex) {
                    if (!WSAcancellation) {
                        MessageBox.Show(ex.Message + "\n\nProbably Can't start listening for the client because the CubeWorld default port (12345) is already in use by another program. Do you have a CubeWorld server or another instance of the bridge already running on your computer?\n\nIf you don't know how to fix this, restarting your computer will likely help", "Error");
                    }
                    return;
                }
                finally {
                    tcpListener.Stop();
                }

                form.Log("client connected\n", Color.Green);
                tcpToClient.NoDelay = true;
                creader = new BinaryReader(tcpToClient.GetStream());
                cwriter = new BinaryWriter(tcpToClient.GetStream());
                int packetID;

                while (true) {
                    try {
                        packetID = creader.ReadInt32();
                        ProcessClientPacket(packetID);
                    }
                    catch (IOException) {
                        if (connected) {
                            SendUDP(new Disconnect() { Guid = guid }.GetBytes());
                        }
                        break;
                    }
                    catch (ObjectDisposedException) { }
                }
                players.Clear();
                form.Log("client disconnected\n", Color.Red);
            }
        }
        public static void ListenFromServerTCP() {
            while (true) {
                try {
                    ProcessServerPacket(sreader.ReadInt32()); //we can use byte here because it doesn't contain vanilla packets
                }
                catch (IOException) {
                    if (connected) {
                        form.Log("Connection to Server lost\n", Color.Red);
                        Close();
                        form.EnableButtons();
                    }
                    break;
                }
            }
        }
        public static void ListenFromServerUDP() {
            SendUDP(new byte[1] { (byte)DatagramID.dummy });//to allow incoming UDP packets
            IPEndPoint source = null;
            try {
                while (true) {
                    byte[] datagram = udpToServer.Receive(ref source);
                    try {
                        ProcessDatagram(datagram);
                    }
                    catch (IOException) {
                        return;
                    }
                }
            }
            catch (SocketException) {
                //when UDPclient is closed
            }
            catch (IOException) {
                //when bridge tries to pass a packet to
                //the client while the client disconnects
            }
        }

        public static void ProcessDatagram(byte[] datagram) {
            var serverUpdate = new ServerUpdate();
            switch ((DatagramID)datagram[0]) {
                case DatagramID.entityUpdate:
                    #region entityUpdate
                    var entityUpdate = new EntityUpdate(datagram);

                    if (entityUpdate.guid == guid) {
                        CwRam.Teleport(entityUpdate.position.Value);
                        break;
                    }
                    else {
                        entityUpdate.Write(cwriter);
                    }

                    if (players.ContainsKey(entityUpdate.guid)) {
                        entityUpdate.Merge(players[entityUpdate.guid]);
                    }
                    else {
                        players.Add(entityUpdate.guid, entityUpdate);
                    }

                    if (entityUpdate.name != null) {
                        RefreshPlayerlist();
                    }
                    break;
                #endregion
                case DatagramID.attack:
                    #region attack
                    var attack = Tools.FromBytes<Attack>(datagram);

                    var hit = new Hit() {
                        target = attack.Target,
                        damage = attack.Damage,
                        critical = attack.Critical ? 1 : 0,
                        stuntime = attack.Stuntime,
                        position = players[attack.Target].position.Value,
                        isYellow = attack.Skill,
                        type = attack.Type,
                        showlight = (byte)(attack.ShowLight ? 1 : 0)
                    };
                    serverUpdate.hits.Add(hit);
                    serverUpdate.Write(cwriter);
                    break;
                #endregion
                case DatagramID.shoot:
                    #region shoot
                    var shootDatagram = Tools.FromBytes<Resources.Datagram.Shoot>(datagram);

                    var shootPacket = new Resources.Packet.Shoot() {
                        position = shootDatagram.Position,
                        velocity = shootDatagram.Velocity,
                        scale = shootDatagram.Scale,
                        particles = shootDatagram.Particles,
                        projectile = shootDatagram.Projectile,
                        chunkX = (int)shootDatagram.Position.x / 0x1000000,
                        chunkY = (int)shootDatagram.Position.y / 0x1000000
                    };
                    serverUpdate.shoots.Add(shootPacket);
                    serverUpdate.Write(cwriter);
                    break;
                #endregion
                case DatagramID.proc:
                    #region proc
                    var proc = Tools.FromBytes<Proc>(datagram);

                    var passiveProc = new PassiveProc() {
                        target = proc.Target,
                        type = proc.Type,
                        modifier = proc.Modifier,
                        duration = proc.Duration
                    };
                    serverUpdate.passiveProcs.Add(passiveProc);
                    serverUpdate.Write(cwriter);
                    break;
                #endregion
                case DatagramID.chat:
                    #region chat
                    var chat = Tools.FromBytes <Chat>(datagram);
                    var chatMessage = new ChatMessage() {
                        sender = chat.Sender,
                        message = chat.Text
                    };
                    try {
                        chatMessage.Write(cwriter, true);
                    }
                    catch (Exception ex) {
                        if (!(ex is NullReferenceException || ex is ObjectDisposedException)) {
                            throw;
                        }
                    }
                    if (chat.Sender == 0) {
                        form.Log(chat.Text + "\n", Color.Magenta);
                    }
                    else {
                        form.Log(players[chat.Sender].name + ": ", Color.Cyan);
                        form.Log(chat.Text + "\n", Color.White);
                    }
                    break;
                #endregion
                case DatagramID.time:
                    #region time
                    var igt = Tools.FromBytes<InGameTime>(datagram);

                    var time = new Time() {
                        time = igt.Time
                    };
                    time.Write(cwriter);
                    break;
                #endregion
                case DatagramID.interaction:
                    #region interaction
                    var interaction = Tools.FromBytes<Interaction>(datagram);

                    var entityAction = new EntityAction() {
                        chunkX = interaction.ChunkX,
                        chunkY = interaction.ChunkY,
                        index = interaction.Index,
                        type = ActionType.staticInteraction
                    };
                    //serverUpdate..Add();
                    //serverUpdate.Write(cwriter);
                    break;
                #endregion
                case DatagramID.staticUpdate:
                    #region staticUpdate
                    var staticUpdate = Tools.FromBytes<StaticUpdate>(datagram);

                    var staticEntity = new StaticEntity() {
                        chunkX = (int)(staticUpdate.Position.x / (65536 * 256)),
                        chunkY = (int)(staticUpdate.Position.y / (65536 * 256)),
                        id = staticUpdate.Id,
                        type = (int)staticUpdate.Type,
                        position = staticUpdate.Position,
                        rotation = (int)staticUpdate.Direction,
                        size = staticUpdate.Size,
                        closed = staticUpdate.Closed ? 1 : 0,
                        time = staticUpdate.Time,
                        guid = staticUpdate.User
                    };
                    var staticServerUpdate = new ServerUpdate();
                    staticServerUpdate.statics.Add(staticEntity);
                    staticServerUpdate.Write(cwriter, true);
                    break;
                #endregion
                case DatagramID.block:
                    //var block = new Block(datagram);
                    //TODO
                    break;
                case DatagramID.particle:
                    #region particle
                    var particleDatagram = Tools.FromBytes<Resources.Datagram.Particle>(datagram);

                    var particleSubPacket = new Resources.Packet.Part.Particle() {
                        position = particleDatagram.Position,
                        velocity = particleDatagram.Velocity,
                        color = new Resources.Utilities.FloatVector() {
                            x = particleDatagram.Color.R / 255,
                            y = particleDatagram.Color.G / 255,
                            z = particleDatagram.Color.B / 255
                        },
                        alpha = particleDatagram.Color.A / 255,
                        size = particleDatagram.Size,
                        count = particleDatagram.Count,
                        type = particleDatagram.Type,
                        spread = particleDatagram.Spread
                    };
                    serverUpdate.particles.Add(particleSubPacket);
                    serverUpdate.Write(cwriter, true);
                    break;
                #endregion
                case DatagramID.connect:
                    #region connect
                    var connect = Tools.FromBytes<Connect>(datagram);
                    guid = connect.Guid;

                    var join = new Join() {
                        guid = guid,
                        junk = new byte[0x1168]
                    };
                    join.Write(cwriter, true);

                    var mapseed = new MapSeed() {
                        seed = connect.Mapseed
                    };
                    mapseed.Write(cwriter, true);
                    break;
                #endregion
                case DatagramID.disconnect:
                    #region disconnect
                    var disconnect = Tools.FromBytes<Disconnect>(datagram);
                    var pdc = new EntityUpdate() {
                        guid = disconnect.Guid,
                        hostility = 255, //workaround for DC because i dont like packet2
                        HP = 0
                    };
                    pdc.Write(cwriter);
                    players.Remove(disconnect.Guid);
                    RefreshPlayerlist();
                    break;
                #endregion
                case DatagramID.specialMove:
                    var specialMove = Tools.FromBytes<SpecialMove>(datagram);
                    switch (specialMove.Id) {
                        case SpecialMoveID.taunt:
                            if (players.ContainsKey(specialMove.Guid)) {
                                CwRam.Teleport(players[specialMove.Guid].position.Value);
                                CwRam.Freeze(5000);
                            }
                            break;
                        case SpecialMoveID.cursedArrow:
                            break;
                        case SpecialMoveID.arrowRain:
                            break;
                        case SpecialMoveID.shrapnel:
                            break;
                        case SpecialMoveID.smokeBomb:
                            var smoke = new ServerUpdate();
                            smoke.particles.Add(new Resources.Packet.Part.Particle() {
                                count = 1000,
                                spread = 5f,
                                type = ParticleType.noGravity,
                                size = 5f,
                                velocity = new Resources.Utilities.FloatVector(),
                                color = new Resources.Utilities.FloatVector() {
                                    x = 1f,
                                    y = 1f,
                                    z = 1f
                                },
                                alpha = 1f,
                                position = players[specialMove.Guid].position.Value
                            });
                            smoke.Write(cwriter);
                            break;
                        case SpecialMoveID.iceWave:
                            break;
                        case SpecialMoveID.confusion:
                            break;
                        case SpecialMoveID.shadowStep:
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }
        public static void ProcessClientPacket(int packetID) {
            switch ((PacketID)packetID) {
                case PacketID.entityUpdate:
                    #region entityUpdate
                    var entityUpdate = new EntityUpdate(creader);
                    if (players.ContainsKey(entityUpdate.guid)) {
                        entityUpdate.Filter(players[entityUpdate.guid]);
                        entityUpdate.Merge(players[entityUpdate.guid]);
                    }
                    else {
                        players.Add(entityUpdate.guid, entityUpdate);
                    }
                    if (entityUpdate.name != null) {
                        RefreshPlayerlist();
                    }
                    SendUDP(entityUpdate.Data);
                    break;
                #endregion
                case PacketID.entityAction:
                    #region entity action
                    EntityAction entityAction = new EntityAction(creader);
                    switch (entityAction.type) {
                        case ActionType.talk:
                            break;
                        case ActionType.staticInteraction:
                            break;
                        case ActionType.pickup:
                            break;
                        case ActionType.drop: //send item back to dropper because dropping is disabled to prevent chatspam
                            if (form.radioButtonDestroy.Checked) {
                                new ChatMessage() { message = "item destroyed" }.Write(cwriter);
                            }
                            else {
                                var serverUpdate = new ServerUpdate();
                                var pickup = new Pickup() { guid = guid, item = entityAction.item };
                                serverUpdate.pickups.Add(pickup);
                                if (form.radioButtonDuplicate.Checked) {
                                    serverUpdate.pickups.Add(pickup);
                                }
                                serverUpdate.Write(cwriter);
                            }
                            break;
                        case ActionType.callPet:
                            var petCall = new SpecialMove() {
                                Guid = guid
                            };
                            SendUDP(petCall.GetBytes());
                            break;
                        default:
                            //unknown type
                            break;
                    }
                    break;
                #endregion
                case PacketID.hit:
                    #region hit
                    var hit = new Hit(creader);
                    var attack = new Attack() {
                        Target = (ushort)hit.target,
                        Damage = hit.damage,
                        Stuntime = hit.stuntime,
                        Skill = hit.isYellow,
                        Type = hit.type,
                        ShowLight = hit.showlight == 1,
                        Critical = hit.critical == 1
                    };
                    SendUDP(attack.GetBytes());
                    lastTarget = attack.Target;
                    break;
                #endregion
                case PacketID.passiveProc:
                    #region passiveProc
                    var passiveProc = new PassiveProc(creader);

                    var proc = new Proc() {
                        Target = (ushort)passiveProc.target,
                        Type = passiveProc.type,
                        Modifier = passiveProc.modifier,
                        Duration = passiveProc.duration
                    };
                    SendUDP(proc.GetBytes());

                    break;
                #endregion
                case PacketID.shoot:
                    #region shoot
                    var shootPacket = new Resources.Packet.Shoot(creader);

                    var shootDatagram = new Resources.Datagram.Shoot() {
                        Position = shootPacket.position,
                        Velocity = shootPacket.velocity,
                        Scale = shootPacket.scale,
                        Particles = shootPacket.particles,
                        Projectile = shootPacket.projectile
                    };
                    SendUDP(shootDatagram.GetBytes());
                    break;
                #endregion
                case PacketID.chat:
                    #region chat
                    var chatMessage = new ChatMessage(creader);

                    if (chatMessage.message.ToLower() == @"/plane") {
                        Console.Beep();
                        var serverUpdate = new ServerUpdate();
                        //var model = VoxModel.Parse("chr_sword.vox");
                        //foreach (var voxel in model) {
                        //    serverUpdate.blockDeltas.Add(new BlockDelta() {
                        //        position = new Resources.Utilities.IntVector() {
                        //            x = 8286946 + voxel.position.x,
                        //            y = 8344456 + voxel.position.y,
                        //            z = 220 + voxel.position.z,
                        //        },
                        //        color = voxel.color,
                        //        type = 1
                        //    });
                        //}
                        serverUpdate.Write(cwriter);
                    }
                    else {
                        SendUDP(new Chat(guid, chatMessage.message).GetBytes());
                    }
                    break;
                #endregion
                case PacketID.chunk:
                    #region chunk
                    var chunk = new Chunk(creader);
                    break;
                #endregion
                case PacketID.sector:
                    #region sector
                    var sector = new Sector(creader);
                    break;
                #endregion
                case PacketID.version:
                    #region version
                    var version = new ProtocolVersion(creader);
                    if (version.version != 3) {
                        version.version = 3;
                        version.Write(cwriter, true);
                    }
                    else {
                        var connect = new Connect();
                        SendUDP(connect.GetBytes());
                    }
                    break;
                #endregion
                default:
                    form.Log("unknown client packet\n", Color.Magenta);
                    break;
            }
        }
        public static void ProcessServerPacket(int packetID) {
            switch (packetID) {
                case 0:
                    var query = new Query(sreader);
                    foreach (var item in query.players) {
                        if (!players.ContainsKey(item.Key)) {
                            players.Add(item.Key, new EntityUpdate());
                        }
                        players[item.Key].guid = item.Key;
                        players[item.Key].name = item.Value;
                    }
                    form.Invoke(new Action(form.listBoxPlayers.Items.Clear));
                    foreach (var playerData in players.Values) {
                        form.Invoke(new Action(() => form.listBoxPlayers.Items.Add(playerData.name)));
                    }
                    break;
                case 4:
                    new ServerUpdate(sreader).Write(cwriter);
                    break;
                default:
                    MessageBox.Show("unknown server packet received");
                    break;
            }
        }

        public static void RefreshPlayerlist() {
            form.Invoke((Action)form.listBoxPlayers.Items.Clear);
            foreach (var player in players.Values) {
                form.Invoke(new Action(() => form.listBoxPlayers.Items.Add(player.name)));
            }
        }

        public static void OnHotkey(int hotkeyID) {
            HotkeyID hotkey = (HotkeyID)hotkeyID;
            if (hotkey == HotkeyID.teleport_to_town) {
                CwRam.SetMode(Mode.teleport_to_city, 0);
                return;
            }

            bool spec = players[guid].specialization == 1;
            bool space = hotkeyID == 1;
            switch ((EntityClass)players[guid].entityClass) {
                case EntityClass.Rogue when spec:
                    #region ninja
                    if (hotkey == HotkeyID.ctrlSpace) {
                        #region dash
                        CwRam.SetMode(Mode.spin_run, 0);
                        #endregion
                        break;
                    }
                    #region blink
                    if (players.ContainsKey(lastTarget)) {
                        CwRam.Teleport(players[guid].position.Value);
                    }
                    #endregion
                    break;
                #endregion
                case EntityClass.Rogue:
                    #region assassin
                    if (hotkey == HotkeyID.ctrlSpace) {
                        #region confusion
                        var specialMove = new SpecialMove() {
                            Guid = guid,
                            Id = SpecialMoveID.confusion,
                        };
                        SendUDP(specialMove.GetBytes());
                        #endregion
                    }
                    else {
                        #region shadow step
                        var specialMove = new SpecialMove() {
                            Guid = guid,
                            Id = SpecialMoveID.shadowStep,
                        };
                        SendUDP(specialMove.GetBytes());
                        #endregion
                    }
                    break;
                #endregion
                case EntityClass.Warrior when spec:
                    #region guardian
                    if (hotkey == HotkeyID.ctrlSpace) {
                        #region taunt
                        var specialMove = new SpecialMove() {
                            Guid = lastTarget,
                            Id = SpecialMoveID.taunt,
                        };
                        SendUDP(specialMove.GetBytes());
                        #endregion
                    }
                    else {
                        #region steel wall
                        CwRam.SetMode(Mode.boss_skill_block, 0);
                        #endregion
                    }
                    break;
                #endregion
                case EntityClass.Warrior:
                    #region berserk
                    if (hotkey == HotkeyID.ctrlSpace) {
                        #region boulder toss
                        CwRam.SetMode(Mode.boulder_toss, 0);
                        #endregion
                    }
                    else {
                        #region earth shatter
                        CwRam.SetMode(Mode.earth_shatter, 0);
                        #endregion
                    }
                    break;
                #endregion
                case EntityClass.Mage when spec:
                    #region watermage
                    if (hotkey == HotkeyID.ctrlSpace) {
                        #region splash
                        CwRam.SetMode(Mode.splash, 0);
                        #endregion
                    }
                    else {
                        #region ice wave
                        //TODO
                        #endregion
                    }
                    break;
                #endregion
                case EntityClass.Mage:
                    #region firemage
                    if (hotkey == HotkeyID.ctrlSpace) {
                        #region lava
                        CwRam.SetMode(Mode.lava, 0);
                        #endregion
                    }
                    else {
                        #region beam
                        CwRam.SetMode(Mode.fireray, 0);
                        #endregion
                    }
                    break;
                #endregion
                case EntityClass.Ranger when spec:
                    #region scout
                    if (hotkey == HotkeyID.ctrlSpace) {
                        #region shrapnel
                        //TODO
                        #endregion
                    }
                    else {
                        #region smoke bomb
                        var specialMove = new SpecialMove() {
                            Guid = guid,
                            Id = SpecialMoveID.smokeBomb,
                        };
                        SendUDP(specialMove.GetBytes());

                        var fakeSmoke = new ServerUpdate();
                        fakeSmoke.particles.Add(new Resources.Packet.Part.Particle() {
                            count = 1000,
                            spread = 5f,
                            type = ParticleType.noSpreadNoRotation,
                            size = 0.3f,
                            velocity = new Resources.Utilities.FloatVector(),
                            color = new Resources.Utilities.FloatVector() {
                                x = 1f,
                                y = 1f,
                                z = 1f
                            },
                            alpha = 1f,
                            position = players[specialMove.Guid].position.Value
                        });
                        fakeSmoke.Write(cwriter);
                        #endregion
                    }
                    break;
                #endregion
                case EntityClass.Ranger:
                    #region sniper
                    if (hotkey == HotkeyID.ctrlSpace) {
                        #region cursed arrow
                        //TODO
                        #endregion
                    }
                    else {
                        #region arrow rain
                        //TODO
                        #endregion
                    }
                    break;
                #endregion
                default:
                    break;
            }
            CwRam.memory.WriteInt(CwRam.EntityStart + 0x1164, 3);//mana cubes
        }

        public static void SendUDP(byte[] data) {
            udpToServer.Send(data, data.Length);
        }
    }
}
