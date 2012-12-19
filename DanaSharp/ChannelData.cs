using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DanaSharp.Extensions;

namespace DanaSharp
{
    public class ChannelData
    {
        public string Name { get; set; }
        public Queue<SpinData> RecentSpins { get; set; }
        public SpinData RandomSpin(WhoReplyEntry source, WhoReplyEntry[] entries)
        {
            lock (RecentSpins)
            {
                var cspin = SpinData.Randomize(source, entries);
                RecentSpins.Enqueue(cspin);
                while (RecentSpins.Count > 5)
                    RecentSpins.Dequeue();
                return cspin;
            }
        }

        public ChannelData(string channel)
        {
            this.Name = channel;
            this.RecentSpins = new Queue<SpinData>();
        }
    }

    public class SpinData
    {
        public WhoReplyEntry Source { get; set; }
        public WhoReplyEntry Target { get; set; }
        public string Action { get; set; }
        public DateTime Time { get; set; }
        public TimeSpan Interval { get { return DateTime.UtcNow - this.Time; } }

        public static SpinData Randomize(WhoReplyEntry source, WhoReplyEntry[] users)
        {
            SpinData data = new SpinData();
            data.Source = source;
            data.Target = users.RandomValues().Take(1).First();
            data.Time = DateTime.UtcNow;
            data.Action = WeightedRandomization.Choose<string>(new [] {
                                new Weighted<String>("hug", 80),
                                new Weighted<String>("cuddle with", 80),
                                new Weighted<String>("give a kiss on the cheek to", 70),
                                new Weighted<String>("give a kiss on the lips to", 70),
                                new Weighted<String>("smooch", 65),
                                new Weighted<String>("french-kiss", 60),
                                new Weighted<String>("make out with", 60),
                                new Weighted<String>("go into the closet room with", 50),
                                new Weighted<String>("make love with", 45),
                                new Weighted<String>("have sex with", 40)
                            });
            return data;
        }
    }
}
