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
    class TestHash
    {
        private static ConnectionMultiplexer Redis = null;


        internal static void Execute(ConnectionMultiplexer redis)
        {
            Redis = redis;

            SimpleStore();
            HashStore();

            Console.ReadLine();
        }


        private static void SimpleStore()
        {
            var db = Redis.GetDatabase();
            const string key = "{Tests:Kunde:1}";

            // Werte einfach speichern 
            db.StringSet(key + ":Name", "Mustermann");
            db.StringSet(key + ":Vorname", "Mario");
            db.StringSet(key + ":Saldo", -815.47);

            // Es wurde gerade wieder eine Mahnung verschickt
            if (db.StringIncrement(key + ":Mahnungen") >= 3)
            {
                // Drei offene Mahnungen? Sperre für 30 Tage setzen, falls nicht bereits gesperrt!
                db.StringSet(key + ":Sperre", true, TimeSpan.FromDays(30), When.NotExists, CommandFlags.FireAndForget);
            }


            // Alle Daten flink lesen
            Console.WriteLine("Informationen über Kunde #1");
            var keys = new RedisKey[5] { 
                key + ":Name",
                key + ":Vorname",
                key + ":Saldo",
                key + ":Mahnungen",
                key + ":Sperre" 
            };
            var data = db.StringGet(keys);
            for (var i = 0; i < keys.Length; ++i)
            {
                Console.WriteLine("{0}: {1}", keys[i], data[i]);
            }
            Console.WriteLine("");
        }


        private static void HashStore()
        {
            var db = Redis.GetDatabase();
            const string key = "{Tests:Kunde:2}";

            // mit Hashes kann man zusammengehörige Daten gruppieren
            HashEntry[] fields = {
                new HashEntry( "Name", "Musterfrau" ),
                new HashEntry( "Vorname", "Traudel" ),
                new HashEntry( "Saldo", 14.99 )
            };
            db.HashSet( key, fields);

            // Guthaben! Schnell etwa Werbung verschicken ...
            if (db.HashIncrement(key, "Flyer") < 5)
            {
                // Nächsten Flyer erst in 14 Tagen 
                // -> Hash unterstützt leider kein TTL ...
                db.StringSet(key + ":Sperre", true, TimeSpan.FromDays(14), When.NotExists, CommandFlags.FireAndForget);
            }

            // Alle Daten flink lesen
            Console.WriteLine("Informationen über Kunde #2");
            foreach (var entry in db.HashGetAll(key))
            { 
                Console.WriteLine( "{0}: {1}", entry.Name, entry.Value);
            }
            Console.WriteLine("");
        }


    }
}
