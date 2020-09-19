using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;


namespace ShellSquare.Witsml.Client
{
    public static class XsdSchemas
    {
        private static Dictionary<string, XmlSchemaSet> m_ReadSchema = new Dictionary<string, XmlSchemaSet>();
        private static Dictionary<string, XmlSchemaSet> m_WriteSchema = new Dictionary<string, XmlSchemaSet>();
        private static Dictionary<string, XmlSchemaSet> m_UpdateSchema = new Dictionary<string, XmlSchemaSet>();
        private static Dictionary<string, XmlSchemaSet> m_DeleteSchema = new Dictionary<string, XmlSchemaSet>();

        public static XmlSchemaSet GetReadSchema(string key)
        {
            XmlSchemaSet xmlSchema = null;
            if (m_ReadSchema.ContainsKey(key))
            {
                xmlSchema = m_ReadSchema[key];
            }
            else
            {
                string location = AppDomain.CurrentDomain.BaseDirectory;
                string folderPath = Path.Combine(location, @"Schemas\WITSML141\WITSMLV141\Read");

                xmlSchema = new XmlSchemaSet();
                foreach (var file in Directory.EnumerateFiles(folderPath, $"obj_{key}.xsd"))
                {
                    XmlReader reader = new XmlTextReader(file);
                    XmlSchema schema = XmlSchema.Read(reader, null);
                    xmlSchema.Add(schema);
                }

                m_ReadSchema.Add(key, xmlSchema);
            }


            return xmlSchema;
        }


        public static XmlSchemaSet GetWriteSchema(string key)
        {
            XmlSchemaSet xmlSchema = null;
            if (m_WriteSchema.ContainsKey(key))
            {
                xmlSchema = m_WriteSchema[key];
            }
            else
            {
                string location = AppDomain.CurrentDomain.BaseDirectory;
                string folderPath = Path.Combine(location, @"Schemas\WITSML141\WITSMLV141\Write");

                xmlSchema = new XmlSchemaSet();
                foreach (var file in Directory.EnumerateFiles(folderPath, $"obj_{key}.xsd"))
                {
                    XmlReader reader = new XmlTextReader(file);
                    XmlSchema schema = XmlSchema.Read(reader, null);
                    xmlSchema.Add(schema);
                }

                m_WriteSchema.Add(key, xmlSchema);
            }


            return xmlSchema;
        }

        public static XmlSchemaSet GetUpdateSchema(string key)
        {
            XmlSchemaSet xmlSchema = null;
            if (m_UpdateSchema.ContainsKey(key))
            {
                xmlSchema = m_UpdateSchema[key];
            }
            else
            {
                string location = AppDomain.CurrentDomain.BaseDirectory;
                string folderPath = Path.Combine(location, @"Schemas\WITSML141\WITSMLV141\Update");

                xmlSchema = new XmlSchemaSet();
                foreach (var file in Directory.EnumerateFiles(folderPath, $"obj_{key}.xsd"))
                {
                    XmlReader reader = new XmlTextReader(file);
                    XmlSchema schema = XmlSchema.Read(reader, null);
                    xmlSchema.Add(schema);
                }

                m_UpdateSchema.Add(key, xmlSchema);
            }


            return xmlSchema;
        }

        public static XmlSchemaSet GetDeleteSchema(string key)
        {
            XmlSchemaSet xmlSchema = null;
            if (m_DeleteSchema.ContainsKey(key))
            {
                xmlSchema = m_DeleteSchema[key];
            }
            else
            {
                string location = AppDomain.CurrentDomain.BaseDirectory;
                string folderPath = Path.Combine(location, @"Schemas\WITSML141\WITSMLV141\Delete");

                xmlSchema = new XmlSchemaSet();
                foreach (var file in Directory.EnumerateFiles(folderPath, $"obj_{key}.xsd"))
                {
                    XmlReader reader = new XmlTextReader(file);
                    XmlSchema schema = XmlSchema.Read(reader, null);
                    xmlSchema.Add(schema);
                }

                m_DeleteSchema.Add(key, xmlSchema);
            }


            return xmlSchema;
        }


        internal static List<WitsmlElement> ProcessElement(XmlSchemaElement elem, WitsmlElementTree parent, string path = "", int level = 0)
        {
            
            List<WitsmlElement> results = new List<WitsmlElement>();

            if (elem.Name == "documentInfo")
            {
                return results;
            }

            string newPath = $"{path}\\{elem.Name}";

            WitsmlElement node = new WitsmlElement();
            node.Name = elem.Name;
            node.Level = level;
            node.Path = newPath;

            if (elem.MinOccurs > 0)
            {
                node.IsRequired = true;
            }

            results.Add(node);
            WitsmlElementTree child = new WitsmlElementTree(node);
            parent.Children.Add(child);

            if (elem.ElementSchemaType is XmlSchemaComplexType)
            {
                XmlSchemaComplexType ct = elem.ElementSchemaType as XmlSchemaComplexType;

                if (level != 0)
                {
                    foreach (System.Collections.DictionaryEntry obj in ct.AttributeUses)
                    {
                        XmlSchemaAttribute attribute = (obj.Value as XmlSchemaAttribute);
                        node = new WitsmlElement();
                        node.Name = attribute.Name.ToString();
                        node.Level = level + 1;
                        node.IsAttribute = true;
                        node.Path = $"{newPath}\\@{node.Name}";

                        if (attribute.Use == XmlSchemaUse.Required)
                        {
                            node.IsRequired = true;
                        }

                        results.Add(node);
                        child.Attributes.Add(node);

                    }
                }

                

                level = level + 1;
                var r = ProcessSchemaObject(ct.ContentTypeParticle, child, newPath, level);
                results.AddRange(r);
            }
            else if (elem.ElementSchemaType is XmlSchemaSimpleType)
            {
                XmlSchemaSimpleType simpleType = elem.ElementSchemaType as XmlSchemaSimpleType;
                if (simpleType.Content != null)
                {
                    XmlSchemaSimpleTypeRestriction restriction = simpleType.Content as XmlSchemaSimpleTypeRestriction;

                    if (restriction != null)
                    {
                        foreach (var f in restriction.Facets)
                        {
                            //if (f is XmlSchemaPatternFacet)
                            //{
                            //    var value = ((XmlSchemaPatternFacet)f).Value;
                                
                            //}
                            //else 
                            
                            if (f is XmlSchemaEnumerationFacet)
                            {
                                var value = ((XmlSchemaEnumerationFacet)f).Value;
                                node.Restrictions.Add(value); 
                            }
                        }

                    }
                }

            }

            return results;
        }

        private static List<WitsmlElement> ProcessSequence(XmlSchemaSequence sequence, WitsmlElementTree parent, string path, int level)
        {
            return ProcessItemCollection(sequence.Items, parent, path, level);
        }

        private static List<WitsmlElement> ProcessChoice(XmlSchemaChoice choice, WitsmlElementTree parent, string path, int level)
        {

            return ProcessItemCollection(choice.Items, parent, path, level);
        }

        private static List<WitsmlElement> ProcessItemCollection(XmlSchemaObjectCollection objs, WitsmlElementTree parent, string path, int level)
        {
            List<WitsmlElement> results = new List<WitsmlElement>();
            foreach (XmlSchemaObject obj in objs)
            {
                var r = ProcessSchemaObject(obj, parent, path, level);
                results.AddRange(r);
            }

            return results;
        }

        private static List<WitsmlElement> ProcessSchemaObject(XmlSchemaObject obj, WitsmlElementTree parent, string path, int level)
        {
            List<WitsmlElement> results = new List<WitsmlElement>();
            List<WitsmlElement> r = new List<WitsmlElement>();
            if (obj is XmlSchemaElement)
            {                
                r = ProcessElement(obj as XmlSchemaElement, parent, path,  level);
            }
            else if (obj is XmlSchemaChoice)
            {
                r = ProcessChoice(obj as XmlSchemaChoice, parent, path, level);
            }
            else if (obj is XmlSchemaSequence)
            {
                r = ProcessSequence(obj as XmlSchemaSequence, parent, path, level);
            }

            results.AddRange(r);

            return results;
        }
    }
}
