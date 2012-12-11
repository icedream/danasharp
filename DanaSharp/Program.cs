using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.Xml.XPath;

using System.Threading;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.IO;

using System.Diagnostics;

using DanaSharp.Extensions;

namespace DanaSharp
{
    public class DanaScully
    {
        int logX = 0, logY = 2;
        ManualResetEventSlim inpLock = new ManualResetEventSlim(true);

        public XmlDocument XmlConfiguration { get; set; }

        public string Server { get { return XmlConfiguration.SelectSingleNode("//config/server").InnerText; } }
        public string Password { get { return 
            XmlConfiguration.SelectSingleNode("//config/password") != null
            ? XmlConfiguration.SelectSingleNode("//config/password").InnerText
            : null; } }
        public ushort Port { get { return ushort.Parse(XmlConfiguration.SelectSingleNode("//config/port").InnerText); } }

        public string Nickname { get { return XmlConfiguration.SelectSingleNode("//config/nick").InnerText; } }
        public string Username { get { return XmlConfiguration.SelectSingleNode("//config/user").InnerText; } }
        public string Realname { get { return XmlConfiguration.SelectSingleNode("//config/real").InnerText; } }
        public byte Usermode { get { return 0; } }

        public XmlNode[] Channels { get { return new List<XmlNode>(XmlConfiguration.SelectNodes("//config/channel").Cast<XmlNode>()).ToArray<XmlNode>(); } }
        private XmlNode NickServInfo { get { return XmlConfiguration.SelectSingleNode("//config/nickserv"); } }
        public string NickServPassword
        {
            get
            {
                return
                    NickServInfo != null && NickServInfo.Attributes["password"] != null
                    ? NickServInfo.Attributes["password"].Value
                    : null;
            }
        }
        public string NickServUsername
        {
            get
            {
                return
                    NickServInfo.Attributes["username"] != null
                    ? NickServInfo.Attributes["username"].Value
                    : null;
            }
        }
        
        static void Main(string[] args)
        {
            Console.WriteLine("Loading configuration...");

            var fb = new DanaScully();
            fb.Start();

            Console.WriteLine("Loading finished.");
        }

        public DanaScully()
        {
            XmlConfiguration = new XmlDocument();
            XmlConfiguration.Load("Settings.xml");
        }

        public void LogLine(string line)
        {
            Log(line + Environment.NewLine);
        }

        public void LogLine(string line, params object[] args)
        {
            LogLine(string.Format(line, args));
        }

        public void Log(string line)
        {
            inpLock.Reset();
            Console.CursorVisible = false;
            int inpX = Console.CursorLeft;
            int inpY = Console.CursorTop;
            Console.SetCursorPosition(logX, logY);
            Console.Write(line);
            if (Console.CursorTop == Console.WindowHeight - 2)
                Console.WriteLine();
            logX = Console.CursorLeft;
            logY = Console.CursorTop;
            Console.SetCursorPosition(inpX, inpY);
            Console.CursorVisible = true;
            inpLock.Set();
        }

        public void Log(string line, params object[] args)
        {
            Log(string.Format(line, args));
        }

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, currentLineCursor);
        }

        public string ReadInputLine()
        {
            Console.CursorLeft = 0;
            Console.CursorTop = Console.WindowHeight - 2;
            Console.Write(">");
            ConsoleKeyInfo key;
            StringBuilder s = new StringBuilder();
            while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                inpLock.Wait();
                switch(key.Key)
                {
                    case ConsoleKey.Enter:
                        break;
                    case ConsoleKey.Backspace:
                        if (s.Length == 0)
                            break;
                        s.Remove(s.Length - 1, 1);
                        Console.Write("\r>");
                        if (s.Length > Console.WindowWidth - 3)
                        {
                            Console.Write(s.ToString().Substring(s.Length - Console.WindowWidth + 3));
                        }
                        else
                        {
                            Console.Write(s.ToString());
                        }
                        Console.Write(" " + key.KeyChar);
                        break;
                    default:
                        if (!char.IsSymbol(key.KeyChar) && !char.IsLetterOrDigit(key.KeyChar) && !char.IsWhiteSpace(key.KeyChar) && !char.IsPunctuation(key.KeyChar))
                            break;
                        s.Append(key.KeyChar);
                        if (Console.CursorLeft == Console.WindowWidth - 1)
                        {
                            Console.CursorLeft = 1;
                            Console.Write(s.ToString().Substring(s.Length - Console.WindowWidth + 2));
                        }
                        else
                            Console.Write(key.KeyChar);
                        break;
                }
            }
            Console.CursorLeft = 1;
            for(int i = Console.CursorLeft; i < Console.WindowWidth - 1; i++)
                Console.Write(" ");
            return s.ToString();
        }

        public void Start()
        {
            LogLine("Connecting...");
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(Server, Port);
            ns = new NetworkStream(sock);
            sr = new StreamReader(ns);
            sw = new StreamWriter(ns);

            LogLine("Logging in...");
            if(Password != null)
                sw.WriteLine("PASS :{0}", Password);
            sw.WriteLine("NICK {0}", Nickname);
            sw.WriteLine("USER {0} {1} * :{2}", Username, Usermode, Realname);
            sw.Flush();
            sw.AutoFlush = true;

            Task.Factory.StartNew(() => _HandleLines());

            while (sock.Connected)
            {
                var line = ReadInputLine();
                if (line.StartsWith("/"))
                    line = line.Substring(1);

                var s = line.Split(' ');
                var cmd = s[0];
                var args = s.Skip(1);
                var argline = string.Join(" ", args);

                switch (cmd)
                {
                    case "quit":
                        SendCommand("quit", "Closed via console");
                        return;
                    case "msg":
                        cmd = "privmsg";
                        goto default;
                    default:
                        SendCommand(cmd, args.ToArray<string>());
                        break;
                }
            }
        }

        #region Bot basic workers

        void _HandleLines()
        {
            LogLine("Ready.");
            while (true)
            {
                var line = ReadLine();
                ParseReply(line);
                SyncModes();
            }
        }

        void _NickServLogin()
        {
            if (NickServPassword == null)
                return;

            LogLine("Identifying via NickServ...");
            SendMessage("NickServ", "IDENTIFY " + (NickServUsername != null ? NickServUsername + " " : "") + NickServPassword);
        }

        #endregion

        #region Modes managing
        Dictionary<string, List<string[]>> modes = new Dictionary<string, List<string[]>>();
        void SyncModes()
        {
            string channel = "";
            string chars = "";
            string args = "";

            while (modes.Keys.Count > 0)
            {
                chars = args = "";

                channel = modes.Keys.First();
                LogLine("Syncing modes in {0}...", channel);
                List<string[]> modesp = modes.Values.First();

                while (modesp.Count > 0)
                {
                    string[] modep = modesp.First();
                    modesp.Remove(modep);
                    chars += modep[0];
                    args += " " + string.Join(" ", modep.Skip(1));
                }

                //SendCommand("MODE", channel, string.Format("{0}{1}", chars, args));
                sw.WriteLine("MODE {0} {1}{2}", channel, chars, args);
                modes.Remove(channel);
            }
        }
        void AddMode(string channel, char mode)
        {
            _chanmode(channel);
            _setmode(channel, "+" + mode);
        }
        void AddMode(string channel, char mode, string target)
        {
            _chanmode(channel);
            _setmode(channel, "+" + mode, target);
        }
        void RemoveMode(string channel, char mode)
        {
            _chanmode(channel);
            _setmode(channel, "-" + mode);
        }
        void RemoveMode(string channel, char mode, string target)
        {
            _chanmode(channel);
            _setmode(channel, "-" + mode, target);
        }
        void _chanmode(string channel)
        {
            if(!modes.ContainsKey(channel.ToLower()))
                modes.Add(channel.ToLower(), new List<string[]>());
        }
        void _setmode(string channel, params string[] arguments)
        {
            channel = channel.ToLower();
            modes[channel].Add(arguments);
        }
        #endregion

        #region Networking stuff
        Socket sock;
        NetworkStream ns;
        StreamReader sr;
        StreamWriter sw;
        #endregion

        RawLine ReadLine()
        {
            string line = sr.ReadLine();
            Debug.WriteLine("RECV: " + line);
            return new RawLine(line);
        }
        RawLine ReadLine(int timeout_ms)
        {
            int timeout_old = ns.ReadTimeout;
            RawLine l = null;
            try
            {
                ns.ReadTimeout = timeout_ms;
                l = ReadLine();
            }
            catch (IOException)
            {
                l = null;
            }
            finally
            {
                ns.ReadTimeout = timeout_old;
            }
            return l;
        }

        void SendCommand(string command)
        {
            Debug.WriteLine("SEND: " + command.ToUpper());
            sw.WriteLine(command.ToUpper());
        }
        void SendCommand(string command, params string[] arguments)
        {
            arguments = (from a in arguments select a.Contains(" ") ? ":" + a : a).ToArray<string>();

            Debug.WriteLine("SEND: " + command.ToUpper().Trim() + " " + string.Join(" ", arguments));
            sw.WriteLine("{0} {1}", command.ToUpper().Trim(), string.Join(" ", arguments));
        }

        void SendMessage(string target, string text)
        {
            SendMessage(new[] { target }, text);
        }
        void SendMessage(string[] targets, string text)
        {
            SendCommand("privmsg", string.Join(",", from t in targets select t.Split('!').First()), text);
        }

        void SendAction(string target, string action)
        { SendMessage(target, string.Format("\x01{0} {1}\x01", "ACTION", action)); }
        void SendAction(string[] targets, string action)
        { SendMessage(targets, string.Format("\x01{0} {1}\x01", "ACTION", action)); }

        void SendNotice(string target, string text)
        {
            SendNotice(new[] { target }, text);
        }
        void SendNotice(string[] targets, string text)
        {
            SendCommand("notice", string.Join(",", from t in targets select t.Split('!').First()), text);
        }

        public WhoReply Who(string searchpattern)
        {
            SendCommand("who", searchpattern);

            var r = new WhoReply();
            RawLine raw;

            while ((raw = ReadLine()).Reply != "315") // while raw line is not end of who reply 
            {
                if (!r.Feed(raw))
                    ParseReply(raw);
            }

            return r;
        }

        public WhoisReply Whois(string searchpattern)
        {
            SendCommand("whois", searchpattern);

            var reply = new WhoisReply();
            var raw = ReadLine();

            while (raw.Reply != "318")
            {
                raw.Arguments = raw.Arguments.Skip(1).ToArray<string>(); // Skip our own nickname
                switch (raw.Reply)
                {
                    case "311":
                        reply.Nickname = raw.Arguments[0];
                        reply.Username = raw.Arguments[1];
                        reply.Realname = raw.Arguments[3];
                        break;
                    case "312":
                        reply.Nickname = raw.Arguments[0];
                        reply.Server = raw.Arguments[1];
                        reply.ServerInfo = raw.Arguments[2];
                        break;
                    case "319":
                        reply.Nickname = raw.Arguments[0];
                        reply.Channels = raw.Arguments[1].Split(' ');
                        break;
                }

                raw = ReadLine();
            }

            return reply;
        }

        void ParseReply(RawLine reply)
        {
            switch (reply.Reply.ToLower())
            {
                case "ping":
                    // Auto-pong
                    SendCommand("pong", reply.Arguments[0]);
                    break;
                case "privmsg":
                    ParseMessage(reply.Source, reply.Arguments[0], reply.Arguments[1]);
                    break;
                case "invite":
                    {
                        SendCommand("join", reply.Arguments.Last());
                        var n = XmlConfiguration.SelectNodes("//config").Item(0).AppendChild(XmlConfiguration.CreateElement("channel"));
                        n.Attributes.Append(XmlConfiguration.CreateAttribute("name")).Value = reply.Arguments.Last();
                        XmlConfiguration.Save("Settings.xml");
                    }
                    break;
                case "004":
                    {
                        _NickServLogin();
                        SendCommand("join", string.Join(",", from n in XmlConfiguration.SelectNodes("//config/channel").Cast<XmlNode>() select n.Attributes["name"].Value));
                    }
                    break;
                default:
                    // Ignoring
                    break;
            }
        }

        void ParseMessage(string source, string target, string text)
        {
            LogLine("<" + source.Split('!').First() + "> => " + target + ": " + text);
            bool isPublic = target.StartsWith("#");

            if (isPublic)
            {
                if (text.StartsWith("!") || text.StartsWith(".") || text.StartsWith("@"))
                {
                    // Parse as command
                    // !command argument1 argument2 ... argumentN
                    string[] spl = text.Split(' ');
                    string name = spl[0].Substring(1);
                    string[] arguments = spl.Skip(1).ToArray<string>();
                    ParseChannelCommand(source, target, text.StartsWith("@"), name, arguments);
                }
            }
            else
            {
                // Parse as private command
                // /msg botname command argument1 argument2 ... argumentN
                string[] spl = text.Split(' ');
                string name = spl[0];
                string[] arguments = spl.Skip(1).ToArray<string>();
                ParsePrivateCommand(source, target, name, arguments);
            }
        }

        void ParsePrivateCommand(string source, string target, string name, string[] arguments)
        {
            LogLine("Received private command by {0}: {1} with {2} arguments", source, name, arguments.Count());
            switch (name.ToLower())
            {
                case "help":
                    SendNotice(source, string.Format("No help available yet."));
                    break;
                case "\x01lag":
                case "\x01lag\x01":
                    break;
                case "\x01version\x01":
                case "\x01userinfo\x01":
                case "\x01finger\x01":
                    SendNotice(source, string.Format("\x01VERSION {0}\x01", "DanaScully C# port by Icedream"));
                    break;
                case "\x01time":
                    break;
                default:
                    SendNotice(source, string.Format("I don't know what you mean with \x02{0}\x02.", name));
                    break;
            }
        }

        ManualResetEventSlim spinLock = new ManualResetEventSlim(false);

        void ParseChannelCommand(string source, string target, bool publicOutput, string name, string[] arguments)
        {
            LogLine("Received channel command by {0}: {1} with {2} arguments", source, name, arguments.Count());
            switch (name.ToLower())
            {
                case "spin":
                    if (spinLock.IsSet)
                    {
                        SendNotice(source.Split('!')[0], "Denied, wait a few seconds.");
                        break;
                    }

                    spinLock.Set();

                    SendAction(target, "spins the bottle...");

                    // Get users which are in the current channel. Non-away users, not us.
                    var users = (
                        from u
                            in Who(target).Users
                        where
                            !u.IsAway
                            && !u.Nickname.Equals(this.Nickname, StringComparison.OrdinalIgnoreCase)
                            && !u.Nickname.Equals(source.Split('!').First(), StringComparison.OrdinalIgnoreCase)
                        select u
                    ).ToArray();

                    SendMessage(target, "The bottle is slowing down...!");
                    Thread.Sleep(800);


                    if (users.Length == 0)
                    {
                        SendMessage(target, "The bottle stops...!");
                        Thread.Sleep(800);
                        SendMessage(target, "The bottle is directing to an empty seat.");
                    }
                    else
                    {

                        // Get 2 random users, which do something with each other...
                        var ruser = users.RandomValues().Take(1).First();

                        SendMessage(target, "The bottle stops...!");
                        Thread.Sleep(800);

                        SendMessage(target, string.Format("It lands on \x02{0}\x02!", ruser.Nickname));
                        Thread.Sleep(500);

                        // Get a random action
                        string action = new string[] {
                                        "make out with",
                                        "kiss",
                                        "make love with",
                                        "have sex with",
                                        "smooch",
                                        "french-kiss"
                                    }.RandomValues().Take(1).First();

                        // Random !spin message :>
                        SendMessage(target, new[] {
                            string.Format("\x03{0}\x02{1}\x02 must {3} \x02{2}\x02!", "04", source.Split('!')[0], ruser.Nickname, action),
                            string.Format("\x03{0}It's time for \x02{1}\x02 to {3} \x02{2}\x02!", "04", source.Split('!')[0], ruser.Nickname, action),
                            string.Format("\x03{0}Now, \x02{1}\x02, go and {3} \x02{2}\x02!", "04", source.Split('!')[0], ruser.Nickname, action),
                            string.Format("\x03{0}Alright, \x02{1}\x02 must {3} \x02{2}\x02!", "04", source.Split('!')[0], ruser.Nickname, action)
                        }.RandomValues().Take(1).First());
                    }

                    // Deny !spin requests for 3 seconds
                    Task.Factory.StartNew(() =>
                    {
                        Thread.Sleep(3000);
                        spinLock.Reset();
                    });
                    break;
            }
        }

        XmlNode GetChannelSettings(string channel)
        {
            return (from c in Channels where c.Attributes["name"].Value.Equals(channel, StringComparison.OrdinalIgnoreCase) select c).First();
        }
    }
}
