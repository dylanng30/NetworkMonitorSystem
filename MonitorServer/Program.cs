using System;
using MonitorServer.Controllers;

namespace MonitorServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Monitor Server";
            ServerController server = new ServerController();
            server.Start();
        }
    }
}