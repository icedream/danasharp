using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DanaSharp.Extensions;

namespace DanaSharp
{
    class ChannelData
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

    class SpinData
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
                                new Weighted<String>("cuddle with", 70),
                                new Weighted<String>("give a kiss on the cheek to", 50),
                                new Weighted<String>("give a kiss on the lips to", 40),
                                new Weighted<String>("smooch", 35),
                                new Weighted<String>("french-kiss", 25),
                                new Weighted<String>("make out with", 22),
                                new Weighted<String>("go into the closet room with", 15),
                                new Weighted<String>("make love with", 10),
                                new Weighted<String>("have sex with", 5)
                            });
            return data;
        }
    }
}
