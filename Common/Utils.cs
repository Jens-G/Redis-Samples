using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Common.Utils
{
    public static class Utils
    {
        public static void WaitForCompletion(List<Task> tasks)
        {
            try
            {
                Debug.Assert(tasks != null);
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException e)
            {
                foreach (var inner in e.InnerExceptions)
                    if (!(inner is TaskCanceledException))
                        throw inner;
            }
        }


        public static void WaitForCompletion<T>(List<Task<T>> tasks)
        {
            try
            {
                Debug.Assert(tasks != null);
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException e)
            {
                foreach (var inner in e.InnerExceptions)
                    if (!(inner is TaskCanceledException))
                        throw inner;
            }
        }


    }
}
