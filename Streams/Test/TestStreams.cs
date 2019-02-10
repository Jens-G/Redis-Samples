using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Threading;
using System.Diagnostics;
using Common.Utils;

namespace Streams.Test
{
    class TestStreams
    {
        const string STREAM_NAME = "CPUHistory";

        private static ConnectionMultiplexer Redis = null;
        private static PerformanceCounter cpuCounter;

        internal static void Execute(ConnectionMultiplexer redis)
        {
            Redis = redis;
            var db = Redis.GetDatabase();

            Console.Write("Initializing ...");
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            Console.Write("\r                 \r");

            Console.WriteLine("Press ENTER to stop");
            var cancel = new CancellationTokenSource();
            var tasks = new List<Task>() {
                ReceiveEvents(db, cancel.Token),
                CreateEvents(db, cancel.Token)
            };
            Console.ReadLine();

            Console.WriteLine("Stopping ...");
            cancel.Cancel();
            Utils.WaitForCompletion(tasks);
            Console.WriteLine("Completed.");

            Console.WriteLine("");
            Console.ReadLine();
        }


        private static async Task CreateEvents(IDatabase db, CancellationToken cancel)
        {
            await Task.Run(async () =>
            {
                var messung = 0;
                while (!cancel.IsCancellationRequested)
                {
                    cpuCounter.NextValue();
                    await Task.Delay(1000);
                    var cpu = (int)cpuCounter.NextValue();

                    var data = new NameValueEntry[2] {
                        new NameValueEntry("CPU", cpu.ToString()),
                        new NameValueEntry("messung", ++messung)
                    };
                    var id = await db.StreamAddAsync(STREAM_NAME, data, null, 16, true);
                }
            }, cancel);
        }

        private static async Task ReceiveEvents(IDatabase db, CancellationToken cancel)
        {
            const int STEPS = 40;
            const char FILLER = ' ';

            var SPACING = new string(' ', 50);
            var FULLY_FILLED = new string(FILLER, STEPS) + FILLER;

            await Task.Run(async () =>  
            {
                RedisValue? minID = null;
                while (!cancel.IsCancellationRequested)
                {
                    // return next N elements, depending on order and minID
                    var processed = 0;
                    foreach (var entry in await db.StreamRangeAsync(STREAM_NAME, minID, null, 10))
                    {
                        // extract and format the data
                        var cpu = -1;
                        var sb = new StringBuilder();
                        sb.AppendFormat("{0}:", entry.Id);
                        foreach (var value in entry.Values)
                        {
                            sb.AppendFormat(" {0} = {1} ", value.Name, value.Value);
                            if (value.Name.Equals("CPU"))
                                int.TryParse(value.Value,out cpu);
                        }

                        // first print the CPU graph, then write the textual data line
                        if (cpu >= 0)
                        {
                            var count = (int)(1.0 * cpu * STEPS / 100.0);
                            Console.Write("{0}|{1}|{2,3} %\r", SPACING, FULLY_FILLED, cpu);
                            Console.Write("{0}|{1}*\r", SPACING, new string(FILLER, count));
                        }
                        Console.WriteLine(sb.ToString());

                        // increment to get the next entry
                        minID = entry.Id + "1";  
                        ++processed;
                    }

                    // nbo data, so trim the stream and wait for more 
                    if (processed == 0)
                    {
                        await db.StreamTrimAsync(STREAM_NAME, 32);
                        await Task.Delay(200);
                    }
                }
            }, cancel);
        }


    }
}
