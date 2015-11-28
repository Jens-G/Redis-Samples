using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using Scaling.Data;
using System.Threading;


namespace Scaling.Cluster
{
    class Worker
    {
        static private ConnectionMultiplexer Redis;

        
        internal static void Run(StackExchange.Redis.ConnectionMultiplexer redis)
        {
            Console.WriteLine("Hit <ESC> to shutdown this worker.");

            Redis = redis;
            while ((!Console.KeyAvailable) || (Console.ReadKey().Key != ConsoleKey.Escape))
            {
                var msg = Redis.GetDatabase().ListLeftPop(Tools.WORKQUEUE);
                if (!msg.IsNullOrEmpty)
                    HandleClusterWork(msg);
                else
                    Thread.Sleep(0);
            }
        }


        private static void HandleClusterWork(RedisValue msg)
        {
            var work = Tools.DeSerialize<Workpack>((byte[])msg);
            if (work == null)
                return;

            string lockId;

            Console.Write(".");
            var session = Session.LockAndRead( Redis, work.Instance, out lockId);

            switch (work.OpCode)
            {
                case Operation.None:
                    break;

                case Operation.Add:
                    session.LastResult += work.Input.SecondOperand;
                    session.Revision++;
                    break;

                case Operation.Subtract:
                    session.LastResult -= work.Input.SecondOperand;
                    session.Revision++;
                    break;

                case Operation.Multiply:
                    session.LastResult *= work.Input.SecondOperand;
                    session.Revision++;
                    break;

                case Operation.Divide:
                    session.LastResult /= work.Input.SecondOperand;
                    session.Revision++;
                    break;

                default:
                    break;
            }

            Session.WriteAndUnlock(Redis, session, lockId);
        }


    }
}
