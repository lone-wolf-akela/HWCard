using System;
using System.Collections.Generic;
using System.Linq;

namespace HWCard_Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Server Starting...");
            int deck_size = 50;
            int hand_size = 5;
            Server server = new(deck_size, hand_size);
            server.SetupAndConnect();
            server.Run();
        }
    }
}