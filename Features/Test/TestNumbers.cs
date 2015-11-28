using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Threading;
using System.Diagnostics;


namespace Features.Test
{
    class TestNumbers
    {
        private static ConnectionMultiplexer Redis = null;

        static volatile bool g_Terminate = false;
        static volatile int g_ConfigNr = 0;
        static volatile int g_WorkInput = 0;


        public static void WorkerThread()
        {
            int cfgNr = g_ConfigNr;

            var workerID = String.Format("{0}:{1}:{2}",
                Environment.MachineName,
                Process.GetCurrentProcess().Id,
                Thread.CurrentThread.ManagedThreadId);

            var rnd = new Random();
            var db = Redis.GetDatabase();
            while (!g_Terminate)
            {
                int workerNo = -1;
                string key = "";

                while (true)
                {
                    ++workerNo;
                    key = String.Format("WorkerNo:{0}", workerNo);

                    db.StringSet(key, workerID, TimeSpan.FromSeconds(rnd.Next(10)+3), When.NotExists, CommandFlags.DemandMaster);
                    if (db.StringGet(key, CommandFlags.DemandMaster).CompareTo(workerID) == 0)
                        break;
                }
                db.HashSet("WorkerList", workerNo, workerID);
                Redis.GetSubscriber().Publish("ClusterCtrl", "reconfigure");
                Console.WriteLine("{0} allocated number {1}", workerID, workerNo);

                while (!g_Terminate)
                {
                    Thread.Sleep(50);
                    var value = g_WorkInput;
                    if ((value != 0) && ((value % 10) == workerNo))
                    {
                        g_WorkInput = 0;
                        Console.Write("{0} with number {1} handles {2} ", workerID, workerNo, value);
                        value += rnd.Next(51) + 17;
                        value /= 3;
                        Redis.GetSubscriber().Publish("Work.Next", value);
                        Console.WriteLine("-> {0}", value);
                    }

                    if (db.StringGet(key, CommandFlags.DemandMaster).CompareTo(workerID) != 0)
                    {
                        Console.WriteLine("{0} assigned number {1} timed out, renewing lease ...", workerID, workerNo);
                        break;
                    }
                }
            }
        }



        private static void HandleClusterCtrl(RedisValue msg)
        {
            var data = msg.ToString().Split( new char[] {'|'});
            //Console.WriteLine("Received ClusterCtrl({0})", data[0]);
            switch (data[0])
            { 
                case "reconfigure":
                    ++g_ConfigNr;
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }
        }


        private static void HandleWorkpack(RedisValue msg)
        {
            g_WorkInput = (int)msg;
        }


        internal static void Execute(ConnectionMultiplexer redis)
        {
            Redis = redis;

            const int TEST = 10;
            var threads = new List<Thread>();

            Console.Write("Creating worker threads ... ");
            for (var i = 0; i < TEST; ++i)
            {
                var thread = new Thread(new ThreadStart(WorkerThread));
                threads.Add(thread);
            }
            Console.WriteLine(" OK");

            Console.Write("Starting worker threads ");
            foreach (var thread in threads)
            {
                thread.Start();
                Console.Write(".");
            }
            Console.WriteLine(" OK.");


            Console.WriteLine("Subscribing to a bunch of channels ...");
            var sub = Redis.GetSubscriber();
            sub.Subscribe("ClusterCtrl", (channel, msg) => { HandleClusterCtrl(msg); });
            sub.Subscribe("Work.*", (channel, msg) => { HandleWorkpack(msg); });

            Console.WriteLine("Initiating some work ...");
            Redis.GetSubscriber().Publish("Work.First", 42);
            Thread.Sleep(3 * 60 * 1000);

            g_Terminate = true;
            Console.WriteLine("Waiting for threads to terminate ...");
            while (threads.Count > 0)
                if (threads[0].Join(4096))
                    threads.RemoveAt(0);
            Console.WriteLine("Waiting for threads to terminate ... OK.");

        }


    }
}
