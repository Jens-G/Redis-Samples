using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Globalization;


namespace Features.Test
{
    class TestTransactions
    {
        private static ConnectionMultiplexer Redis = null;
        private static IDatabase db = null;
        private static Random rnd = new Random();

        private static int trans_count = 0;
        private static int error_count = 0;

        private static int thread_limit = 0;
        private static int thread_count = 0;

        private static string keyTest = "{Tests:Transact}:";
        private const int MAX_ACCOUNTS = 10;
        private const int MAX_BETRAG = 10000;

        internal static void Execute(ConnectionMultiplexer redis)
        {
            Redis = redis;
            db = Redis.GetDatabase();

            Console.Clear();

            InitValues(db);
            StartThread();

            while (true)
            {
                RefreshScreen();
                Thread.Sleep(500);

                while( Console.KeyAvailable) {
                    switch (Console.ReadKey(true).Key)
                    { 
                        case  ConsoleKey.Escape:
                            Interlocked.Exchange(ref thread_limit, 0);
                            while (thread_limit < Interlocked.Add(ref thread_count, 0))
                                Thread.Sleep(0);
                            return;
                        case ConsoleKey.Add:
                            StartThread();
                            break;
                        case ConsoleKey.Subtract:
                            StopThread();
                            break;
                        default: 
                            // nix
                            break;
                    }
                }
            }

        }

        
        private static void ThreadProc(object args)
        {
            Interlocked.Increment(ref thread_count);

            var nr = (int)args;
            while (nr <= TestTransactions.thread_limit)
                AnotherTransaction();

            Interlocked.Decrement(ref thread_count);
        }


        private static void StartThread()
        {
            while (Math.Max(thread_limit,0) < Interlocked.Add(ref thread_count, 0))
                Thread.Sleep(0);

            var nr = Interlocked.Increment( ref thread_limit);
            var thread = new Thread(ThreadProc);
            thread.Start( nr);
        }


        private static void StopThread()
        {
            if (thread_limit > 0)
                Interlocked.Decrement(ref thread_limit);
        }


        private static void InitValues(IDatabase db)
        {
            var saldo = 0;
            for( var i = 1; i < 10; ++i )
            {
                var betrag = rnd.Next(MAX_BETRAG) - (MAX_BETRAG/2);
                saldo += betrag;
                db.HashSet(keyTest + i.ToString(), "saldo", betrag);
                db.HashSet(keyTest + i.ToString(), "nummer", i);
            }
            db.HashSet(keyTest + 0.ToString(), "saldo", -saldo);
            db.HashSet(keyTest + 0.ToString(), "nummer", 0);
        }


        private static void AnotherTransaction()
        {
            var betrag = rnd.Next(MAX_BETRAG);
            var snd_nr = rnd.Next(MAX_ACCOUNTS);
            var rcv_nr = rnd.Next(MAX_ACCOUNTS);
            while( snd_nr == rcv_nr)
                rcv_nr = rnd.Next(MAX_ACCOUNTS);

            var snd_key = keyTest + snd_nr.ToString();
            var rcv_key = keyTest + rcv_nr.ToString();

            while( true)
            {
                var snd_saldo = (int)db.HashGet(snd_key, "saldo");
                var rcv_saldo = (int)db.HashGet(rcv_key, "saldo");

                // preconditions that must be met
                var trans = db.CreateTransaction();
                trans.AddCondition(Condition.HashEqual(snd_key, "saldo", snd_saldo));
                trans.AddCondition(Condition.HashEqual(rcv_key, "saldo", rcv_saldo));

                // actions to be taken
                trans.HashIncrementAsync( rcv_key, "saldo", betrag);
                trans.HashDecrementAsync( snd_key, "saldo", betrag);

                // execute everything we queued so far
                if (trans.Execute())
                {
                    Interlocked.Increment(ref trans_count);
                    break;
                }

                // try again, fail better
                Interlocked.Increment(ref error_count);
            }
        }


        private static void RefreshScreen()
        {
            const string TEMPLATE = "{0,-10}  {1,10}";

            Console.CursorTop = 0;
            Console.CursorLeft = 0;

            Console.WriteLine(String.Format(TEMPLATE, "Nummer", "Saldo"));
            Console.WriteLine("--------------------------");
            var saldo = 0;
            for (var i = 0; i < 10; ++i)
            {
                var betrag = (int)db.HashGet(keyTest + i.ToString(), "saldo");
                var nummer = db.HashGet(keyTest + i.ToString(), "nummer");
                saldo += betrag;
                Console.WriteLine(String.Format(TEMPLATE, nummer, betrag));
            }
            Console.WriteLine("--------------------------");
            Console.WriteLine(String.Format(TEMPLATE, "Total", saldo));
            Console.WriteLine("--------------------------");

            Console.WriteLine("{0} transactions completed, {1} retries.", trans_count, error_count);
            Console.WriteLine("{0} threads running.", Interlocked.Add( ref thread_count, 0));
            Console.WriteLine("");
            Console.WriteLine("Use <+> and <-> to increase or decrease the thread count.");
            Console.WriteLine("Hit <ESC> to close");
        }


    }
}
