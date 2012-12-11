using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DanaSharp.Extensions
{
    public static class RandomExtensions
    {
        public static IEnumerable<TValue> RandomValues<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            Random rand = new Random();
            return Enumerable.ToList(dict.Values).RandomValues();
        }

        public static IEnumerable<TValue> RandomValues<TValue>(this IList<TValue> values)
        {
            Random rand = new Random();
            int size = values.Count;
            while (true)
            {
                yield return values[rand.Next(size)];
            }
        }
    }
}
