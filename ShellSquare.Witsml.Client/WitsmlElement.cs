using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShellSquare.Witsml.Client
{
    public class WitsmlElement 
    {
        public bool Selected { get; set; }
        public string Path { get; set; }
        public bool IsAttribute { get; set; }
        public bool IsRequired { get; set; }
        public int Level { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public List<string> Restrictions { get; private set; } = new List<string>();

        public WitsmlElement DeepCopy()
        {
            WitsmlElement o = new WitsmlElement()
            {
                IsAttribute = this.IsAttribute,
                IsRequired = this.IsRequired,
                Level = this.Level,
                Name = this.Name,
                Value = this.Value,
                Path= Path
            };

            foreach (var s in Restrictions)
            {
                o.Restrictions.Add(s);
            }

            return o;
            
        }

    }
}
