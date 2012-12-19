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
                sum += obj.Weight;
                Console.WriteLine("Choice is {0}, Sum current is {1}/{2}, obj.Value is {3}", choice, sum, totalweight, obj.Value);
                if (choice < sum)
                    return obj.Value;
            }

            return default(T);
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
            this.Weight = weight;
        }
    }
}
