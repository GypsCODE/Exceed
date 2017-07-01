﻿using System;
using System.Threading;

namespace Server {
    class Program {
        static void Main(string[] args) {
            //ServerTCP TCPserver = new ServerTCP(12345);
            ServerUDP UDPserver = new ServerUDP(12346);
            while(true) {
                var text = Console.ReadLine();
                if(text.StartsWith("help")) {
                    Console.WriteLine("help\tShows this page");
                    Console.WriteLine("exit\tCloses Server");
                    Console.WriteLine("alert\tWrites message in chat");
                    Console.WriteLine("clear\tClears console");
                } else if(text.StartsWith("exit")) {
                    break;
                } else if(text.StartsWith("alert")) {
                    var message = text.Remove(0, 5).Trim();
                    if(message.Length == 0) {
                        Console.WriteLine("Message:");
                        message = Console.ReadLine();
                    }
                    UDPserver.Alert(message);
                } else if(text.StartsWith("clear")) {
                    Console.Clear();
                } else {
                    Console.WriteLine("Unknown Command");
                }
            }
        }
    }
}
