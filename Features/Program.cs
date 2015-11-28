using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using StackExchange.Redis;
using System.Threading;
using System.Diagnostics;
using Features.Test;

namespace Features
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
                Console.Title = "Redis Features Showcase";
                Redis = InitializeConnection();

                Console.WriteLine("Pick one:");
                Console.WriteLine("  1 - Numbers");
                Console.WriteLine("  2 - Floats, Ints");
                Console.WriteLine("  3 - Bitmaps");
                Console.WriteLine("  4 - Sets");
                Console.WriteLine("  5 - Sorted Sets (simple)");
                Console.WriteLine("  6 - Sorted Sets (airports)");
                Console.WriteLine("  7 - Transactions");
                Console.WriteLine("  8 - Hashes");
                Console.WriteLine("");

                while (true)
                {
                    switch (Console.ReadKey(true).KeyChar)
                    {
                        case '1': TestNumbers.Execute(Redis); return;
                        case '2': TestFloatsInts.Execute(Redis); return;
                        case '3': TestBitmaps.Execute(Redis); return;
                        case '4': TestSets.Execute(Redis); return;
                        case '5': TestSortedSimple.Execute(Redis); return;
                        case '6': TestSortedSets.Execute(Redis); return;
                        case '7': TestTransactions.Execute(Redis); return;
                        case '8': TestHash.Execute(Redis); return;
                        default: break;
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
