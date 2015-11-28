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
    class TestSortedSimple
    {
        private static ConnectionMultiplexer Redis = null;        
        private static string keyTest = "{Tests:SortedSets:1}";

        internal static void Execute(ConnectionMultiplexer redis)
        {
            Redis = redis;
            var db = Redis.GetDatabase();

            Console.Clear();

            SimpleTest();

            Console.Write("<ENTER> to close ");
            Console.ReadLine();
        }


        private static void SimpleTest()
        {
            var key = keyTest + ":wtf";
            var db = Redis.GetDatabase();

            db.SortedSetRemoveRangeByRank(key, 0, -1);

            db.SortedSetAdd(key, "Alice", -10);
            db.SortedSetAdd(key, "Bob", -20);
            db.SortedSetAdd(key, "Claire", -30);
            db.SortedSetAdd(key, "Doris", -40);

            // Score existiert, aber der Eintrag ist neu
            db.SortedSetAdd(key, "Egon", -30);

            // Eintrag existiert, Score aktualisieren
            var score = db.SortedSetScore(key, "Bob").GetValueOrDefault();
            db.SortedSetAdd(key, "Bob", score - 40);

            var allEntries = db.SortedSetRangeByRankWithScores(key);
            Console.WriteLine("Punkte Name");
            foreach (var entry in allEntries)
                Console.WriteLine("{0,6} {1}", -entry.Score, entry.Element);
        }



    }
}
