using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Threading;
using System.Diagnostics;
using Features;
using System.IO;


namespace Features.Test
{
    class TestSets
    {
        private static ConnectionMultiplexer Redis = null;
        private static string keyTest = "{Tests:Sets:1}";

        internal static void Execute(ConnectionMultiplexer redis)
        {
            Redis = redis;
            var db = Redis.GetDatabase();

            FillKnownBirthdays();

            while (true)
            {
                Console.Clear();

                Console.Write("Tell me your birthday! First the day (1..31) ");
                int tag = Math.Max(1, Math.Min(31, Int32.Parse(Console.ReadLine())));
                Console.Write("Great! Now the month (1..12) ");
                int monat = Math.Max(1, Math.Min(12, Int32.Parse(Console.ReadLine())));
                Console.Write("And the year please (four digits) ");
                int year = Math.Max(0, Math.Min(DateTime.Now.Year, Int32.Parse(Console.ReadLine())));
                Console.WriteLine("");

                var keyTag = keyTest + ":day:" + tag.ToString();
                var keyMonat = keyTest + ":month:" + monat.ToString();
                var keyJahr = keyTest + ":year:" + monat.ToString();

                var matches = db.SetMembers(keyJahr);
                Console.WriteLine("In the same year " + matches.Length.ToString() + " people were also born.");
                /* don't print, too much stuff
                foreach (var match in matches)
                    Console.WriteLine("- " + match);
                Console.WriteLine("");
                */

                matches = db.SetMembers(keyMonat);
                Console.WriteLine(matches.Length.ToString() + " people have their birthday in the same month.");
                /* don't print, too much stuff
                foreach (var match in matches)
                    Console.WriteLine("- " + match);
                Console.WriteLine("");
                */

                matches = db.SetMembers(keyTag);
                Console.WriteLine("At the " + tag.ToString() + ". of a month " + matches.Length.ToString() + " people have their birthday.");
                /* don't print, too much stuff
                foreach (var match in matches)
                    Console.WriteLine("- " + match);
                Console.WriteLine("");
                */

                Console.WriteLine("");

                matches = db.SetCombine(SetOperation.Intersect, new RedisKey[] { keyTag, keyMonat, keyJahr });
                Console.WriteLine("At the exact date when you were born " + matches.Length.ToString() + " other people were born:");
                foreach (var match in matches)
                    Console.WriteLine("- " + match);
                Console.WriteLine("");

                matches = db.SetCombine(SetOperation.Intersect, new RedisKey[] { keyTag, keyMonat });
                Console.WriteLine("You share your birthday with " + matches.Length.ToString() + " other people:");
                foreach (var match in matches)
                    Console.WriteLine("- " + match);
                Console.WriteLine("");

                Console.WriteLine("");

                Console.WriteLine("Let's just pick a random one from the same day:");
                Console.WriteLine("- " + db.SetRandomMember(keyTag));
                Console.WriteLine("");

                Console.WriteLine("How awesome is that?");
                Console.WriteLine("<ENTER> to try again");
                Console.ReadLine();
            }
        }


        private static void AddBDay(int day, int month, int year, string name)
        {
            var db = Redis.GetDatabase();
            db.SetAdd(keyTest + ":day:" + day.ToString(), name);
            db.SetAdd(keyTest + ":month:" + month.ToString(), name);
            db.SetAdd(keyTest + ":year:" + year.ToString(), name);
        }


        private static void FillKnownBirthdays()
        {
            // Source:
            // http://mydatamaster.com/free-downloads/
            // The PHP dump SQL file needs to be manually converted into CSV
            // Notepad++ is recommended for this task, takes only 5 minutes (if you do it right)
            var reader = new StreamReader(@"E:\D\TPCPP\_Testdaten\Birthdays\famousbirthdays.csv");
            var number = 0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if ((number++ % 512) == 0)
                    Console.Write("\rReading file ... {0} lines", number);

                // expected format is: ID, "YYYY-MM-DD", "name and detail info"
                // the ID field is ignored here, but since the data contain it ...
                var data = Tools.SplitCSVLine(line);
                Debug.Assert(data.Count == 3);
                var name = data[2];
                var date = data[1].Split('-');  // split YYYY-MM-DD

                // extract date pieces
                var year = int.Parse(date[0]);
                var month = int.Parse(date[1]);
                var day = int.Parse(date[2]);

                AddBDay(day, month, year, name);
            }
            Console.WriteLine("\rReading file ... {0} lines", number);
        }



    }
}
