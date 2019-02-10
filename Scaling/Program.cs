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

        static void Main(string[] args)
        {
            try
            {
                Console.Title = "Redis Workload scaling and cluster demo";
                Redis = Common.Redis.InitializeConnection(ConfigurationManager.AppSettings["Redis.Password"]);

                Console.WriteLine("Run as ...");
                Console.WriteLine("  1 = (c)lient");
                Console.WriteLine("  2 = (w)orker");
                Console.WriteLine("  3 = (m)onitor");
                Console.WriteLine("  or (q)uit");
                Console.WriteLine();
                Console.Write("> ");

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
