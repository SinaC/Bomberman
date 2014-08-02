﻿using System;
using System.ServiceModel;
using Common.DataContract;
using Common.Interfaces;
using Common.Log;

namespace Client
{
    //TODO !! var/params names
    class Program
    {
        private const string MapPath = @"D:\GitHub\Bomberman\Server\map.dat"; // SinaC: only server should know map path, and client specify a map id
        public static IBombermanService Proxy { get; private set; }

        static void Main(string[] args)
        {
            var context = new InstanceContext(new BombermanCallbackService());
            var factory = new DuplexChannelFactory<IBombermanService>(context, "WSDualHttpBinding_IBombermanService");
            Proxy = factory.CreateChannel();

            Console.WriteLine("--------------------------------------");
            Console.WriteLine("-------- Welcome to Bomberman --------");
            Console.WriteLine("--------------------------------------\n\n");
            Console.WriteLine("Type your player name :\n");
            string login = Console.ReadLine();
            Player newPlayer = new Player
            {
                Id = Guid.NewGuid().GetHashCode(), // SinaC: only server can ensure uniqueness of player id, and more important: GetHashCode on 2 differents GUID may return the same result !!!
                Username = login
            };
            ConnectPlayer(newPlayer);
            Log.Initialize(@"D:\Temp\BombermanLogs", "Client_" + login +".log");
            Log.WriteLine(Log.LogLevels.Info, "Logged at " + DateTime.Now.ToShortTimeString());

            bool stop = false;
            while (!stop)
            {
                ConsoleKeyInfo keyboard = Console.ReadKey();
                switch (keyboard.Key)
                {
                    //s
                    case ConsoleKey.S:
                        //Console.WriteLine("\nEnter the path of the map.bat");
                        //StartGame(Console.ReadLine());
                        StartGame(MapPath);
                        break;
                    case ConsoleKey.UpArrow:
                        MoveTo(ActionType.MoveUp, login);
                        break;
                    case ConsoleKey.LeftArrow:
                        MoveTo(ActionType.MoveLeft, login);
                        break;
                    case ConsoleKey.RightArrow:
                        MoveTo(ActionType.MoveRight, login);
                        break;
                    case ConsoleKey.DownArrow:
                        MoveTo(ActionType.MoveDown, login);
                        break;
                    case ConsoleKey.X: // SinaC: never leave a while(true) without an exit condition
                        stop = true;
                        break;
                }
            }

            // SinaC: Clean factory properly
            try
            {
                factory.Close();
            }
            catch (Exception ex)
            {
                Log.WriteLine(Log.LogLevels.Warning, "Exception:{0}", ex);
                factory.Abort();
            }
        }

        //todo replace playername by an id ...
        private static void ConnectPlayer(Player newPlayer)
        {
            Proxy.ConnectUser(newPlayer);
        }

        private static void StartGame(string mapPath)
        {
            Proxy.StartGame(mapPath);
        }
        //todo replace playername by an id ...
        private static void MoveTo(ActionType actionType, string login)
        {
            Proxy.MovePlayerToLocation(login, actionType);
        }
    }
}
