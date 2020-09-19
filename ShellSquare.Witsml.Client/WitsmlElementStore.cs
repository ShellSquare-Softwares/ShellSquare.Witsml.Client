using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellSquare.Witsml.Client
{
    internal class WitsmlElementStore
    {
        public static Dictionary<string, WitsmlElement> Elements { get;  private set; } = new Dictionary<string, WitsmlElement>(StringComparer.InvariantCultureIgnoreCase);
    }
}
