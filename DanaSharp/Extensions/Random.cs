using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DanaSharp.Extensions
{
    public static class RandomExtensions
    {
        static Random rand = new Random();

        public static IEnumerable<TValue> RandomValues<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            return Enumerable.ToList(dict.Values).RandomValues();
        }

        public static IEnumerable<TValue> RandomValues<TValue>(this IList<TValue> values)
        {
            int size = values.Count;
            while (true)
            {
                yield return values[rand.Next(size)];
            }
        }
    }
}
