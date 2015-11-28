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
    class TestFloatsInts
    {
        private static ConnectionMultiplexer Redis = null;


        internal static void Execute(ConnectionMultiplexer redis)
        {
            Redis = redis;

            var db = Redis.GetDatabase();

            RedisKey keyInnen = "{{Tests:EstimatingPi}}:Innen";
            RedisKey keyAussen = "{{Tests:EstimatingPi}}:Aussen";
            var rnd = new Random();
            double lastGuess = 3;
            double delta = 1;

            while(Math.Abs(delta) > 1e-10)
            {
                var x = rnd.NextDouble();
                var y = rnd.NextDouble();
                if (((x * x) + (y * y)) > 1)
                    db.StringIncrement(keyAussen);
                else
                    db.StringIncrement(keyInnen);

                if ((rnd.Next() % 2048) == 0)
                {
                    var values = db.StringGet(new RedisKey[2] { keyAussen, keyInnen });
                    var total = (int)values[0] + (int)values[1];

                    var guess = 4.0 * (double)values[1] / total;
                    delta = Math.Abs(guess - lastGuess);
                    Console.Write("\r{0} tests, Pi = {1}, Delta = {2}    \r", total, guess, delta);
                    lastGuess = guess;
                }
            }

            Console.WriteLine("");
            Console.WriteLine("");
            Console.Write("<ENTER> to close ... ");
            Console.ReadLine();
        }


    }
}
