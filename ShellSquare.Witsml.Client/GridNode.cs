using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ShellSquare.Witsml.Client
{
    internal class GridNode : INotifyPropertyChanged
    {
        private bool m_Selected;
        private string m_Value;
        public bool Selected { get
            {
                return m_Selected;
            }
            set
            {
                m_Selected = value;
                OnPropertyChanged();
            }
        }

        public bool IsAttribute { get; set; }

        public int Level { get; set; }
        public string Name { get; set; }
        public string Value { get
            {
                return m_Value;
            }
            set
            {
                m_Value = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
