﻿using System;
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
                    if (item.Element.Selected)
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
                XAttribute at = new XAttribute(a.Name, a.Value ?? string.Empty);
                element.Add(at);
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


    }
}
