using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using StackExchange.Redis;
using System.Threading;
using System.Diagnostics;
using Streams.Test;

namespace Streams
{
    class Program
    {
        static private ConnectionMultiplexer Redis = null;

        static void Main(string[] args)
        {
            try
            {
                Console.Title = "Redis Streams Showcase";
                Redis = Common.Redis.InitializeConnection(ConfigurationManager.AppSettings["Redis.Password"]);

                Console.WriteLine("Pick one:");
                Console.WriteLine("  1 - Write into and read values from a stream");
                Console.WriteLine("  2 - Consumer groups: Producer");
                Console.WriteLine("  3 - Consumer groups: Consumer");
                Console.WriteLine();
                Console.Write("> ");

                while (true)
                {
                    var c = Console.ReadKey(true).KeyChar;
                    Console.WriteLine(c);
                    switch (c)
                    {
                        case '1': TestStreams.Execute(Redis); return;
                        case '2': TestConsumerGroups.Execute(Redis,string.Empty); return;
                        case '3': TestConsumerGroups.Execute(Redis,EnterConsumerGroup()); return;
                        default: break;
                    }
                }

            }
            catch(Exception e)
            {
                Tools.PrintExceptionDetails(e);
                Console.Write("<ENTER> ");
                Console.ReadLine();
            }
        }

        private static string EnterConsumerGroup()
        {
            const string DEFAULT_GROUP = "default";
            Console.Write("Consumer group name (\"{0}\"): ", DEFAULT_GROUP);
            var sGroup = Console.ReadLine();
            return string.IsNullOrEmpty(sGroup) ? DEFAULT_GROUP : sGroup;
        }
    }
}
