using Scaling.Data;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scaling.Cluster
{
    class Monitor 
    {
        static private ConnectionMultiplexer Redis;


        internal static void Run(StackExchange.Redis.ConnectionMultiplexer redis)
        {
            Redis = redis;
            var sub = Redis.GetSubscriber();

            Console.Clear();
            Refresh();

            while ((! Console.KeyAvailable) || (Console.ReadKey().Key != ConsoleKey.Escape))
            {
                Refresh();
                Thread.Sleep(1500);
            }
        }


        private static void Refresh()
        {
            var db = Redis.GetDatabase();
            const string TEMPLATE = "{0,10}  {1,10}  {2,-20}";

            var sb = new StringBuilder();
            sb.AppendLine("Monitor");
            sb.AppendLine("--------------------------------------------");
            sb.AppendLine(String.Format(TEMPLATE, "Session", "Revision", "Last value"));
            sb.AppendLine("--------------------------------------------");
            foreach (var entry in Session.SessionIds(Redis))
            {
                var id = new InstanceDescriptor() { Id = entry };
                var session = Session.ReadOnly(Redis, id);
                sb.AppendLine(String.Format(TEMPLATE, id.Id, session.Revision, session.LastResult));
            }
            sb.AppendLine("--------------------------------------------");
            sb.AppendLine( String.Format("{0} work entries in queue {1}", db.ListLength(Tools.WORKQUEUE), Tools.WORKQUEUE));
            sb.AppendLine("--------------------------------------------");
            sb.AppendLine("Hit <ESC> to shutdown this monitor.         ");

            //Console.Clear();
            Console.CursorTop = 0;
            Console.CursorLeft = 0;
            Console.WriteLine(sb);
        }


    }
}
