using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellSquare.Witsml.Client
{
    internal class Element
    {
        public Element()
        {
            Children = new List<Element>();
            Attributes = new Dictionary<string, string>();
        }
        public List<Element> Children { get; private set; }
        public Dictionary<string, string> Attributes { get; private set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public int Level { get; set; }
    }
}
