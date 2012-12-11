using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DanaSharp
{
    public class WhoisReply
    {
        public string Nickname { get; set; }
        public string Username { get; set; }
        public string Hostname { get; set; }
        public string Realname { get; set; }
        public string[] Channels { get; set; }
        public string Server { get; set; }
        public string ServerInfo { get; set; }
    }
}
