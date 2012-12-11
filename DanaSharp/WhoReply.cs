using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace DanaSharp
{
    public class WhoReply
    {
        private List<WhoReplyEntry> _users = new List<WhoReplyEntry>();
        public WhoReplyEntry[] Users { get { return _users.ToArray<WhoReplyEntry>(); } }

        public WhoReplyEntry[] GetEntriesByHostmask(string hostmask)
        {
            Debug.WriteLine("Sorting entries by hostmask <{0}>", hostmask);
            var rx = new Regex("^" + hostmask.Replace(".", "\\.").Replace("?", ".").Replace("*", ".*") + "$", RegexOptions.Singleline);
            var s = from u in Users where rx.IsMatch(u.GetHostmask()) select u;
            Debug.WriteLine("=> Found {0} entries", s.Count());
            return s.ToArray<WhoReplyEntry>();
        }

        public WhoReplyEntry[] GetEntriesByNickname(string nickname)
        { return GetEntriesByHostmask(string.Format("{0}!{1}@{2}", nickname, "*", "*")); }

        public WhoReplyEntry[] GetEntriesByHostname(string hostname)
        { return GetEntriesByHostmask(string.Format("{0}!{1}@{2}", "*", "*", hostname)); }

        public WhoReplyEntry[] GetEntriesByHostAndUsername(string hostname, string username)
        { return GetEntriesByHostmask(string.Format("{0}!{1}@{2}", "*", username, hostname)); }

        public bool Feed(RawLine line)
        {
            if (!line.Reply.Equals("352"))
                return false;

            WhoReplyEntry s = new WhoReplyEntry(line.Arguments);
            _users.Add(s);
            Debug.WriteLine("Feeding WhoReply: Channel {0}, {1}!{2}@{3}", s.Channel, s.Nickname, s.Username, s.Hostname);

            return true;
        }
    }

    public class WhoReplyEntry
    {
        // :irc.rizon.io 352 Icedream #founders ~lolk_ AlBazCom.Gilani * Al-Ihsaani Hr :0 errlolhuh?
        public string Channel { get; private set; }
        public string Username { get; private set; }
        public string Hostname { get; private set; }
        public string Nickname { get; private set; }
        public bool IsAway { get; private set; }
        public char[] Flags { get; private set; }
        public string Prefix { get; private set; }
        public ulong HopCount { get; private set; }
        public string Realname { get; private set; }

        public WhoReplyEntry(string[] arguments)
        {
            //Target = arguments[0];
            Channel = arguments[1];
            Username = arguments[2];
            Hostname = arguments[3];
            //Unused = arguments[4];
            Nickname = arguments[5];
            IsAway = arguments[6].StartsWith("G"); // G = Away, H = Not away
            var s = from c in arguments[6] where "@+%&~!=".Contains(c) select c;
            Prefix = s.Any() ? s.First().ToString() : string.Empty;
            Flags = (from c in arguments[6].Substring(1) where !"@+%&~!=".Contains(c) select c).ToArray<char>();
            var t = arguments.Last().Split(' ');
            HopCount = ulong.Parse(t[0]);
            Realname = string.Join(" ", t.Skip(1));
        }

        public string GetHostmask()
        {
            return string.Format("{0}!{1}@{2}", Nickname, Username, Hostname);
        }
    }
}
