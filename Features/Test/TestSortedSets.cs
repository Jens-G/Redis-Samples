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
using Features;

namespace Features.Test
{
    class TestSortedSets
    {
        private static ConnectionMultiplexer Redis = null;        
        private static string keyTest = "{Tests:SortedSets:2}";

        internal static void Execute(ConnectionMultiplexer redis)
        {
            Redis = redis;
            var db = Redis.GetDatabase();

            Console.Clear();

            if (db.HashLength(keyTest + ":CodeByIdent") < 1)
            {
                EmptySets();
                FillAirports();
            }

            while (true)
            {
                double lon, lat, range;

                Console.WriteLine("");
                Console.Write("Longitude: ");
                if (!Double.TryParse(Console.ReadLine(), out lon))
                    break; ;
                Console.Write("Latitude: ");
                if (!Double.TryParse(Console.ReadLine(), out lat))
                    break; ;

                while( db.SetLength(keyTest + ":matchLast") > 0)
                    db.SetPop(keyTest + ":matchLast");

                range = 0.001;
                var funde = 0;
                var start = DateTime.Now;
                while (funde < 20)
                {
                    Console.Write("\rSearching {0} km range, {1:N0} secs and {2} matches so far ...  ", DegToKm(range), (DateTime.Now - start).TotalSeconds, funde);
                    var matchLat = db.SortedSetRangeByScoreWithScores(keyTest + ":IdentByLat", lat - range, lat + range);
                    var matchLon = db.SortedSetRangeByScoreWithScores(keyTest + ":IdentByLon", lon - range, lon + range);

                    while (db.SetLength(keyTest + ":latMatch") > 0)
                        db.SetPop(keyTest + ":latMatch");
                    foreach (var match in matchLat)
                        db.SetAdd(keyTest + ":latMatch", match.Element);

                    while (db.SetLength(keyTest + ":lonMatch") > 0)
                        db.SetPop(keyTest + ":lonMatch");
                    foreach (var match in matchLon)
                        db.SetAdd(keyTest + ":lonMatch", match.Element);

                    db.SetCombineAndStore(
                        SetOperation.Intersect,
                        keyTest + ":matchThis",
                        new RedisKey[] { 
                            keyTest + ":lonMatch", 
                            keyTest + ":latMatch" 
                        });

                    db.SetCombineAndStore(
                        SetOperation.Difference,
                        keyTest + ":matchNew",
                        new RedisKey[] { 
                            keyTest + ":matchThis", 
                            keyTest + ":matchLast" 
                        });

                    db.SetCombineAndStore(
                        SetOperation.Union,
                        keyTest + ":matchLast",
                        new RedisKey[] { 
                            keyTest + ":matchThis", 
                            keyTest + ":matchLast" 
                        });

                    foreach (var elm in db.SetMembers(keyTest + ":matchNew"))
                    {
                        var ident = elm;
                        var code = db.HashGet(keyTest + ":CodeByIdent", elm);
                        var name = db.HashGet(keyTest + ":NameByIdent", elm);
                        var lat_found = db.SortedSetScore(keyTest + ":IdentByLat", elm).Value;
                        var lon_found = db.SortedSetScore(keyTest + ":IdentByLon", elm).Value;
                        var km = DegToKm( Math.Sqrt( (lat_found - lat) * (lat_found - lat) + (lon_found - lon) * (lon_found - lon)));
                        ++funde;
                        Console.WriteLine("\r- {0} {1} is {2} km away, at lat {3:N3} lon {4:N3}", code, name, km, lat_found, lon_found);
                    }

                    range = Math.Ceiling( 11000 * range) / 10000;
                }

                Console.WriteLine("\r{0} matche(s) found in {1:N0} seconds.", funde, (DateTime.Now-start).TotalSeconds);
            }

            Console.Write("<ENTER> to close ");
            Console.ReadLine();
        }


        private static double DegToKm(double degree)
        {
            return Math.Round(degree * 40000 / 360);
        }

        private static void EmptySets()
        {
            var db = Redis.GetDatabase();

            db.SortedSetRemoveRangeByScore(keyTest + ":IdentByLat", -360, +360);
            db.SortedSetRemoveRangeByScore(keyTest + ":IdentByLon", -360, +360);
        }


        private static void AddAirport(List<string> data)
        {
            try
            {
                var db = Redis.GetDatabase();

                Debug.Assert(data.Count == 18);

                // "id"
                // "ident"
                // "type"
                // "name"
                // "latitude_deg"
                // "longitude_deg"
                // "elevation_ft"
                // "continent"
                // "iso_country"
                // "iso_region"
                // "municipality"
                // "scheduled_service"
                // "gps_code"
                // "iata_code"
                // "local_code"
                // "home_link"
                // "wikipedia_link"
                // "keywords"

                var ident = data[1];
                var name = data[3];
                var lat = Double.Parse(data[4], CultureInfo.InvariantCulture);
                var lon = Double.Parse(data[5], CultureInfo.InvariantCulture);
                var code = data[13];

                if (String.IsNullOrEmpty(code))
                    code = ident;

                db.SortedSetAdd(keyTest + ":IdentByLat", ident, lat);
                db.SortedSetAdd(keyTest + ":IdentByLon", ident, lon);
                db.HashSet(keyTest + ":NameByIdent", ident, name);
                db.HashSet(keyTest + ":CodeByIdent", ident, code);
            }
            catch (Exception)
            {
                Debugger.Break();
            }
        }


        private static void FillAirports()
        {
            // Quelle: 
            // http://ourairports.com/data/
            var reader = new StreamReader(@"E:\D\TPCPP\_Testdaten\airports.csv");
            var number = 0;
            while( ! reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if((number++ % 512) == 0)
                    Console.Write("\rReading file ... {0} lines", number);
                if (number > 1)
                    AddAirport( Tools.SplitCSVLine(line));
            }
            Console.WriteLine("\rReading file ... {0} lines", number);
        }



    }
}
