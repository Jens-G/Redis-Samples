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
    class TestConsumerGroups
    {
        private static ConnectionMultiplexer Redis = null;

        private static IDatabase db;
        private static string Group;
        private static string Consumer; // or sender

        private const string STREAM_NAME = "TestConsumerGroups";
        private static List<Task> Processing = new List<Task>();
        private static RedisValue? LastConsumedMsgID = null;

        private struct TaskResult
        {
            internal int events;
            internal string group;
            internal string consumer;
            internal bool sender;
        }


        internal static void Execute(ConnectionMultiplexer redis, string aConsumerGroup)
        {
            Redis = redis;

            db = Redis.GetDatabase();
            Group = aConsumerGroup;
            Consumer = Process.GetCurrentProcess().Id.ToString();

            var cancel = new CancellationTokenSource();
            var tasks = new List<Task<TaskResult>>();
            var bProducer = string.IsNullOrEmpty(Group);
            Console.WriteLine("Press ENTER to stop");

            if (bProducer)
                tasks.Add(CreateEvents(cancel.Token));
            else
                tasks.Add(ReceiveEvents(cancel.Token));
            Console.ReadLine();

            Console.Write("Stopping ...");
            cancel.Cancel();
            Utils.WaitForCompletion(tasks);
            Console.Write("\r              \r");
            PrintResults(tasks).Wait();
            Console.WriteLine("Completed.");

            Console.WriteLine("");
            Console.ReadLine();
        }

        private struct GroupSummary
        {
            internal bool sender;
            internal int events;
            internal int consumers;
        }

        private async static Task PrintResults(List<Task<TaskResult>> tasks)
        {
            var groups = new Dictionary<string, GroupSummary>();

            Console.WriteLine("\rSummary:");
            var info = await db.StreamInfoAsync(STREAM_NAME);
            Console.WriteLine("- {0} consumer groups associated", info.ConsumerGroupCount);
            Console.WriteLine("- last message: {0} (entryID = {1})",
                info.LastEntry.Id,
                (from entry in info.LastEntry.Values where entry.Name.Equals("eventID") select entry.Value).FirstOrDefault()
                );

            var grpsinfo = await db.StreamGroupInfoAsync(STREAM_NAME);
            foreach (var ginf in grpsinfo)
            {
                Console.WriteLine("- Group has {0} consumers and {1} pending messages", ginf.ConsumerCount, ginf.PendingMessageCount);
            }

            foreach (var entry in tasks)
            {
                var result = entry.Result;
                Console.WriteLine(
                    "- Process {0} {1} {2} events",
                    result.consumer,
                    result.sender ? "created" : "received",
                    result.events
                    );

                // group summary
                if (!groups.TryGetValue(result.group, out GroupSummary data))
                {
                    data = new GroupSummary()
                    {
                        sender = result.sender,
                        consumers = 0,
                        events = 0
                    };
                }
                data.events += result.events;
                data.consumers++;
                groups[result.group] = data;
            }
        }


        private static void InitGroup(CancellationToken cancel)
        {
            try
            {
                var tasks = new List<Task>()
                {
                    db.StreamCreateConsumerGroupAsync(STREAM_NAME, Group)
                };
                Utils.WaitForCompletion(tasks);
            }
            catch (RedisServerException e)
            {
                Console.Write("Initializing ...");
                Console.WriteLine(e.Message);
            }

        }

        private async static Task PrintGroupInfo(CancellationToken cancel)
        {
            var info = await db.StreamGroupInfoAsync(STREAM_NAME);
            var group = info.FirstOrDefault((grp) => { return grp.Name.Equals(Group); });
            Console.WriteLine(
                "Group {0} has {1} consumers and {2} pending messages",
                group.Name,
                group.ConsumerCount,
                group.PendingMessageCount
                );

            if (group.PendingMessageCount > 0)
                await CheckPendingMessages(cancel);
        }

        private async static Task CheckPendingMessages(CancellationToken cancel)
        {
            Console.WriteLine("Checking pending messages ...");
            const int ALLOWED_IDLE_MSEC = 1 * 60 * 1000;

            bool bMoreData = true;
            RedisValue? nextMsg = null;
            while (bMoreData)
            {
                bMoreData = false;
                foreach (var msg in await db.StreamPendingMessagesAsync(STREAM_NAME, Group, 10, RedisValue.Null, nextMsg))
                {
                    LastConsumedMsgID = msg.MessageId;
                    nextMsg = LastConsumedMsgID + "1"; // continue with next one 
                    bMoreData = true;
                    Console.Write('.');

                    // try to claim it, checking against ALLOWED_IDLE_MSEC
                    var entries = await db.StreamClaimAsync(STREAM_NAME, Group, Consumer, ALLOWED_IDLE_MSEC, new RedisValue[1] { LastConsumedMsgID.Value });
                    foreach (var entry in entries)
                    {
                        if (!entry.IsNull)  // something to process
                        {
                            Console.Write('\r');
                            await ProcessMessageAsync(entry, cancel);
                        } else {
                            Console.WriteLine("\r- {0} left pending", LastConsumedMsgID.Value);
                        }
                    }
                }
            }
        }

        private static async Task<TaskResult> ReceiveEvents(CancellationToken cancel)
        {
            Console.WriteLine("Consumer name is {0}", Consumer);
            int events = 0;
            try
            {
                await Task.Run(async () =>
                {
                    await PrintGroupInfo(cancel);

                    /* There are two possible ways to call the API:
                     * - using special ID ">" to get messages that were never delivered to other consumers so far
                     * - areal ID like "0-0" to provide us with the history of pending (that is, not ACKed) messages
                     * Note that both cases are exclusive, either-or. 
                     */

                    Console.WriteLine("Checking history");
                    RedisValue? position = LastConsumedMsgID ?? "0";
                    var history = true; // Since we want to get history first, then newly added messages, we need a flag to control that.
                    while (!cancel.IsCancellationRequested)
                    {
                        // return next N elements, depending on order and minID
                        var lastEvents = events;
                        foreach (var entry in await db.StreamReadGroupAsync(STREAM_NAME, Group, Consumer, position))
                        {
                            ++events;
                            if (history)
                                position = entry.Id;

                            await ProcessMessageAsync(entry, cancel);
                        }

                        // switch to "new events" mode once the history has been consumed
                        if ((lastEvents == events) && history)
                        {
                            history = false;
                            position = null;  // only needed as long as we iterating history
                            Console.WriteLine("Switching to current mode (new entries)", Group, Consumer);
                        }

                        Processing.RemoveAll((task) => { return task.IsCompleted; });
                    }
                }, cancel);
            }
            catch (TaskCanceledException)
            {
                // may happen
            }

            Utils.WaitForCompletion(Processing);

            return new TaskResult()
            {
                sender = false,
                events = events,
                group = Group,
                consumer = Consumer
            };
        }

        private static Task ProcessMessageAsync(StreamEntry entry, CancellationToken cancel)
        {
            var sb = new StringBuilder();
            if ( entry.Values != null)
                foreach (var value in entry.Values)
                    sb.AppendFormat(" {0} = {1}", value.Name, value.Value);
            Console.WriteLine("Reading {0}:{1}", entry.Id, sb.ToString());

            // processing completed successfully, so acknowledge this to the queue
            Processing.Add(Task.Run(async () => {
                await Task.Delay(5000, cancel);  // simulate processing
                await db.StreamAcknowledgeAsync(STREAM_NAME, Group, entry.Id);
                Console.WriteLine("Acknowleding {0}:{1}", entry.Id, sb.ToString());
            }, cancel));

            return Task.CompletedTask;
        }

        private static async Task<TaskResult> CreateEvents(CancellationToken cancel)
        {
            var events = 0;

            try
            {
                await Task.Run(async () =>
                {
                    var rnd = new Random();

                    while (!cancel.IsCancellationRequested)
                    {
                        var data = new NameValueEntry[1] {
                        new NameValueEntry("eventID", events + 1)
                        };
                        var id = await db.StreamAddAsync(STREAM_NAME, data, null, 16, true);

                        ++events;
                        Console.Write("\r{0} events generated", events);
                        await Task.Delay(300, cancel);
                    }
                });
            }
            catch (TaskCanceledException)
            {
                // may happen
            }

            return new TaskResult()
            {
                sender = true,
                events = events,
                group = "generator",
                consumer = Process.GetCurrentProcess().Id.ToString()
            };
        }


    }
}
