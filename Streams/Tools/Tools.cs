using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Streams
{
    internal class Tools
    {

        internal static List<string> SplitCSVLine(string line)
        {
            var result = new List<string>();
            bool inQuote = false;
            int lastQuote = 0;
            string piece = String.Empty;
            bool pendingData = false;
            foreach (var c in line)
            {
                ++lastQuote;

                switch (c)
                {
                    case ',':
                        if (inQuote)
                        {
                            piece += c;
                        }
                        else if (pendingData)
                        {
                            result.Add(piece);
                            piece = String.Empty;
                        }
                        pendingData = true;  // nach Trenner beginnt immer ein neues Teilstück
                        break;

                    case '"':
                        pendingData = true;
                        inQuote = (!inQuote);
                        if (inQuote)
                        {
                            if (lastQuote == 1)  // Sonderfall beachten: zwei "" werden zu einem "
                            {
                                piece = result[result.Count - 1] + '"';
                                result.RemoveAt(result.Count - 1);
                            }
                        }
                        else
                        {
                            result.Add(piece);
                            piece = String.Empty;
                            pendingData = false;
                        }
                        lastQuote = 0;
                        break;

                    default:
                        piece += c;
                        pendingData = true;
                        break;
                }
            }
            if (pendingData)
                result.Add(piece);

            return result;
        }

        internal static void PrintExceptionDetails(Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);

            if (e.InnerException != null)
                PrintExceptionDetails(e.InnerException);

            if ((e is AggregateException aex) && (aex.InnerExceptions != null))
                foreach (var inner in aex.InnerExceptions)
                    PrintExceptionDetails(inner);
        }
    }
}
