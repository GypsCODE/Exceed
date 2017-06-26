﻿using System;
using System.Threading;

namespace Server {
    class Program {
        static void Main(string[] args) {
            ServerTCP TCPserver = new ServerTCP(12345);
            ServerUDP UDPserver = new ServerUDP(12346);
            while(true) {
                var text = Console.ReadLine();
                if(text == "exit")
                    break;
                else {
                    var p = new Resources.Datagram.Chat(text) {
                        Sender = 0
                    };
                    UDPserver.UDPSendToAll(p.data, null);
                }
            }
        }
    }
}
