using StackExchange.Redis;
using System;

namespace Common
{
    public class Redis
    {
        public static ConnectionMultiplexer InitializeConnection(string password)
        {
            Console.Write("Connecting ...");

            // TODO: make endpoints configurable
            var options = CreateConfigurationOptions(password);
            var redis = ConnectionMultiplexer.Connect(options);
            PrintConnectionStatus(redis);

            return redis;
        }

        public static ConfigurationOptions CreateConfigurationOptions(string password)
        {
            var options = new ConfigurationOptions();
            options.EndPoints.Add("localhost");  // default port
            options.EndPoints.Add("localhost", 7000);  // cluster ports as configured
            options.EndPoints.Add("localhost", 7001);
            options.EndPoints.Add("localhost", 7002);
            options.Password = password;
            return options;
        }

        public static void PrintConnectionStatus(ConnectionMultiplexer redis)
        {
            //Console.WriteLine("\rStatus: "+redis.GetStatus());

            Console.WriteLine("\rEndpoints connected:");
            foreach (var ep in redis.GetEndPoints(false))
                if (redis.GetServer(ep).IsConnected)
                    Console.WriteLine("- " + ep);
            Console.WriteLine("");
        }
    }
}
