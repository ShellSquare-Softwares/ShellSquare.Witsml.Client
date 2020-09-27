using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Services.Description;
using System.Windows;
using System.Xml.Linq;

namespace ShellSquare.Witsml.Client
{
    internal class WitsmlElementTree
    {
        public static WitsmlElementTree Root { get; set; }

        public XNamespace Namespace { get; set; }
        public WitsmlElementTree(WitsmlElement element)
        {
            Element = element;
        }

        public WitsmlElement Element { get; private set; }
        public List<WitsmlElementTree> Children { get; private set; } = new List<WitsmlElementTree>();
        public List<WitsmlElement> Attributes { get; private set; } = new List<WitsmlElement>();

        internal bool HasActiveChildran
        {
            get
            {

                foreach (var item in Children)
                {
                    if (item.Element.Selected )
                    {
                        return true;
                    }
                }


                foreach (var item in Attributes)
                {
                    if (item.Selected)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public XElement ToXElement(XNamespace ns = null)
        {
            XElement element;
            if (Namespace != null)
            {
                element = new XElement(Namespace + Element.Name);
                ns = Namespace;
            }
            else
            {                
                element = new XElement(Element.Name);
            }


            if (element.Name.NamespaceName == "")
            {
                element.Attributes("xmlns").Remove();
                element.Name = ns + element.Name.LocalName;
            }

            if (Element.Value != null)
            {
                element.SetValue(Element.Value);
            }

            foreach (var a in Attributes)
            {
                if (a.Selected)
                {
                    XAttribute at = new XAttribute(a.Name, a.Value ?? string.Empty);
                    element.Add(at);
                }
            }

            foreach (var child in Children)
            {
                if (child.Element.Selected || child.HasActiveChildran)
                {
                    element.Add(child.ToXElement(ns));
                }
            }

            return element;
        }


        public void AddToPath(string path, WitsmlElementTree w)
        {
            if (this.Element.Path == path)
            {
                string elementName = w.Element.Name;
                int index = Children.FindLastIndex(o => o.Element.Name == elementName);

                if (index == -1)
                {
                    Children.Add(w);
                }
                else
                {
                    Children.Insert(index + 1, w);
                }

                return;
            }

            foreach (var child in Children)
            {
                child.AddToPath(path, w);
            }
        }

        public void AddToPath(string path, WitsmlElement a)
        {
            if(this.Element.Path == path)
            {
                Attributes.Add(a);
                return;
            }

            foreach (var child in Children)
            {
                child.AddToPath(path, a);
            }
        }


    }
}
