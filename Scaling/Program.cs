using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using StackExchange.Redis;
using System.Threading;
using System.Diagnostics;

namespace Scaling
{
    class Program
    {
        static private ConnectionMultiplexer Redis = null;

        static private ConnectionMultiplexer InitializeConnection()
        {
            Console.Write("Connecting ...");

            // TODO: make endpoints configurable
            var options = new ConfigurationOptions();
            options.EndPoints.Add("localhost");  // default port
            options.EndPoints.Add("localhost", 7000);  // cluster ports as configured
            options.EndPoints.Add("localhost", 7001);
            options.EndPoints.Add("localhost", 7002);
            options.Password = ConfigurationManager.AppSettings["Redis.Password"];
            var redis = ConnectionMultiplexer.Connect(options);

            Console.WriteLine("\rEndpoints available:              ");
            foreach (var ep in redis.GetEndPoints(false))
                if (ep.AddressFamily != System.Net.Sockets.AddressFamily.Unspecified)
                    Console.WriteLine("- " + ep);
            Console.WriteLine("");

            return redis;
        }


        static void Main(string[] args)
        {
            try
            {
                Console.Title = "Redis Workload scaling and cluster demo";
                Redis = InitializeConnection();

                Console.WriteLine("Run as ...");
                Console.WriteLine("  1 = (c)lient");
                Console.WriteLine("  2 = (w)orker");
                Console.WriteLine("  3 = (m)onitor");
                Console.WriteLine("  or (q)uit");

                while (true)
                {
                    switch (Console.ReadKey(true).KeyChar)
                    {
                        case '1':
                        case 'c':
                        case 'C':
                            Cluster.Client.Run(Redis);
                            return;

                        case '2':
                        case 'w':
                        case 'W':
                            Cluster.Worker.Run(Redis);
                            return;

                        case '3':
                        case 'm':
                        case 'M':
                            Cluster.Monitor.Run(Redis);
                            return;

                        case 'q':
                        case 'Q':
                            return;
                    }
                }

            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.Write("<ENTER> ");
                Console.ReadLine();
        }
        }


    }
}
