using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scaling.Data;
using StackExchange.Redis;

namespace Scaling.Cluster
{
    class Client
    {
        private const int MAX_SESSIONS = 4;

        internal static void Run(StackExchange.Redis.ConnectionMultiplexer Redis)
        {
            var maxOps = (int)ScalingConstants.LastOperation;
            var rnd = new Random();
            while (true)
            {
                var work = new Workpack();
                work.Instance = new InstanceDescriptor() { Id = rnd.Next(MAX_SESSIONS).ToString() };
                work.OpCode = (Operation)rnd.Next(maxOps);
                work.Input = new InputData();
                while (work.Input.SecondOperand == 0)
                {
                    work.Input.SecondOperand = rnd.NextDouble();
                }


                RedisValue[] data = { Tools.Serialize(work) };
                Redis.GetDatabase().ListRightPush(Tools.WORKQUEUE, data, CommandFlags.FireAndForget);
                Console.Write(".");
            }
        }
    }
}
