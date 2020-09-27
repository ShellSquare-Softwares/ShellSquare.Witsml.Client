using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShellSquare.Witsml.Client
{
    internal class GridNode : INotifyPropertyChanged
    {
        internal GridNode(WitsmlElement element)
        {
            Element = element;
        }

        public WitsmlElement Element { get; private set; }
        public bool IsAttribute
        {
            get
            {
                return Element.IsAttribute;
            }
            set
            {
                Element.IsAttribute = value;
            }
        }
        public bool IsRequired
        {
            get
            {

                return Element.IsRequired;

            }
            set
            {
                Element.IsRequired = value;
            }
        }
        public int Level
        {
            get
            {
                return Element.Level;
            }
            set
            {
                Element.Level = value;
            }
        }
        public string Name
        {
            get
            {
                return Element.Name;
            }
            set
            {
                Element.Name = value;
            }
        }

        public bool Selected
        {
            get
            {
                return Element.Selected;
            }
            set
            {
                Element.Selected = value;
                OnPropertyChanged();
            }
        }

        public string Value
        {
            get
            {
                return Element.Value;
            }
            set
            {
                Element.Value = value;
                OnPropertyChanged();
            }
        }

        public string DisplayName
        {
            get
            {
                string displayName = $"{Space}{Name}";
                return displayName;
            }
        }

        public string Space
        {
            get
            {
                string space = "";
                for (int i = 0; i < Level; i++)
                {
                    space = space + "    ";
                }
                return space;
            }
        }

        private bool m_IsPopupOpen = false;
        public bool IsPopupOpen { 
            get
            {
                return m_IsPopupOpen;
            }
            set
            {
                m_IsPopupOpen = value;
                OnPropertyChanged();
            }
        }

        public List<string> Restrictions
        {
            get
            {
                return Element.Restrictions;
            }
        }

        public Visibility RestrictionSelection
        {
            get
            {
                if(this.Restrictions.Count > 0)
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        internal void Notify()
        {
            OnPropertyChanged("Selected");
            OnPropertyChanged("Value");
        }
    }
}
