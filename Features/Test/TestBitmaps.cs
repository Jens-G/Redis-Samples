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
    class TestBitmaps
    {
        private static ConnectionMultiplexer Redis = null;


        internal static void Execute(ConnectionMultiplexer redis)
        {
            Redis = redis;

            var db = Redis.GetDatabase();

            RedisKey keyPrimes = "Tests:FindingPrimes";

            while (true)
            {
                Console.Write("Upper limit: ");
                int bereich;
                if( ! Int32.TryParse( Console.ReadLine(), out bereich))
                    break;
                bereich = Math.Max(2, Math.Min(Int32.MaxValue, bereich)) + 1;

                // Reste vom vorherigen Lauf löschen
                Console.Write("\rInitializing ...");
                db.StringSet(keyPrimes, "");

                // Bit am Ende des Bereiches ansprechen -> nur 1x Allokation 
                // Die Bits 0 bis (N-1) werden implizit 0 -> Negation -> 1
                db.StringSetBit(keyPrimes, bereich, false);  // allocate 
                db.StringBitOperation(Bitwise.Not, keyPrimes, keyPrimes);

                // Redis überalloziert, die Negation produziert daraus 
                // ungewollt gesetzte Bits -> zurück auf 0 mit denen!
                var count = db.StringBitCount(keyPrimes);
                while (count > bereich)
                    db.StringSetBit(keyPrimes, --count, false);
                Debug.Assert(db.StringBitCount(keyPrimes) == bereich);

                // weder 0 noch 1 sind prim
                db.StringSetBit(keyPrimes, 0, false);  // not prime
                db.StringSetBit(keyPrimes, 1, false);  // not prime

                long teiler = 2;
                long letzter = (long)Math.Ceiling( Math.Sqrt(1.0 * bereich));
                while (teiler <= letzter)
                {
                    count = db.StringBitCount(keyPrimes);
                    Console.Write("\rCurrent = {0}, {1} prime candidates remaining ...", teiler, count);

                    var ofs = 2 * teiler;
                    while (ofs <= bereich)
                    {
                        db.StringSetBit(keyPrimes, ofs, false);
                        ofs += teiler;
                    }

                    // StringBitPosition() taugt hier nicht, siehe Doku
                    while (++teiler <= letzter)
                        if (db.StringGetBit(keyPrimes, teiler))
                            break;
                }

                count = db.StringBitCount(keyPrimes);
                ClearCurrentConsoleLine();
                Console.Write("\r{0} primes in range 1 to {1}:", count, bereich-1);
                for (var i = 2; i <= bereich; ++i)
                    if (db.StringGetBit(keyPrimes, i))
                        Console.Write(String.Format(" {0}", i));
                Console.WriteLine();
                Console.WriteLine();
            }

            Console.ReadLine();
        }

        private static void ClearCurrentConsoleLine()
        {
            var template = "\r{0,"+(Console.BufferWidth-1).ToString()+"}\r";
            Console.Write(template, "");
        }


    }
}
