using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DanaSharp
{
    public class WeightedRandomization
    {
        public static T Choose<T>(Weighted<T>[] list)
        {
            if (list.Count() == 0)
            {
                return default(T);
            }

            int totalweight = list.Sum(c => c.Weight);
            Random rand = new Random();
            int choice = rand.Next(totalweight);
            int sum = 0;

            foreach (var obj in list)
            {
                for (int i = sum; i < obj.Weight + sum; i++)
                {
                    if (i >= choice)
                    {
                        return obj.Value;
                    }
                }
                sum += obj.Weight;
            }

            return list.First().Value;
        }
    }

    public interface IWeighted
    {
        int Weight { get; set; }
    }

    public class Weighted<T> : IWeighted
    {
        public T Value { get; set; }
        public int Weight { get; set; }

        public Weighted(T value, int weight)
        {
            this.Value = value;
        }
    }
}
