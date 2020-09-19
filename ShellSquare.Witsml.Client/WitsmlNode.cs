using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellSquare.Witsml.Client
{
    public class WitsmlNode
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public List<WitsmlNode> Attributes { get; private set; } = new List<WitsmlNode>();

        public List<WitsmlNode> Children { get; private set; } = new List<WitsmlNode>();

        public string AttributesString
        {
            get
            {
                string s = "";
                foreach (var a in Attributes)
                {
                    s += $" {a.Name ?? string.Empty}=\"{a.Value ?? string.Empty}\"";
                }

                return s;
            }
        }
    }
}
