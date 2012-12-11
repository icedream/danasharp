using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DanaSharp
{
    public class RawLine
    {
        public string Source;
        public string Reply;
        public string[] Arguments;

        public RawLine(string line)
        {
            string[] l = line.Split(' ');

            if (!l[0].StartsWith(":"))
            {
                Source = null;
                Reply = l[0];
                l = l.Skip(1).ToArray<string>();
            }
            else
            {
                Source = l[0].TrimStart(':');
                Reply = l[1];
                l = l.Skip(2).ToArray<string>();
            }

            List<string> arguments = new List<string>();

            int i = 0;
            while (i < l.Length)
            {
                // Last argument
                if (l[i].StartsWith(":"))
                {
                    arguments.Add(string.Join(" ", l.Skip(i)).Substring(1));
                    break;
                }
                
                // Other arguments
                arguments.Add(l[i]);
                i++;
            }

            Arguments = arguments.ToArray<string>();
        }
    }
}
