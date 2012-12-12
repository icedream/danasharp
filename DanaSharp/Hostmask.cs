using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DanaSharp
{
    public class Hostmask
    {
        private string hostmask;
        private Regex regex;

        public Hostmask(string hostmask)
        {
            this.hostmask = hostmask;
            this.regex = new Regex("^" + hostmask.Replace("?", ".?").Replace("*", ".*") + "$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        public bool IsMatch(string hostmask)
        {
            return this.regex.IsMatch(hostmask);
        }
    }
}
