using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace ShellSquare.Witsml.Client
{
    /// <summary>
    /// Interaction logic for WITSMLControl.xaml
    /// </summary>
    public partial class WITSMLControl : UserControl
    {
        private DispatcherTimer m_RequestEditorChangeMonitor;
        private OptionSelection M_optionSelection;
        private TabHeaderControl m_TabHeader;
        private bool m_LoadRequestGrid = false;
        private bool m_LoadRequestEditor = false;
        private string m_RequestId = "";

        public WITSMLControl(TabHeaderControl tabHeader)
        {
            m_TabHeader = tabHeader;
            InitializeComponent();
            LoadPreference();

            LoadTemplateList();
        }

        private void LoadTemplateList()
        {
            var configFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string templateConfigFilePath = Path.Combine(configFilePath, "Configuration.xml");
            var configuration = XDocument.Load(templateConfigFilePath);

            List<string> templateList = new List<string>();
            foreach (var element in configuration.Descendants("Name"))
            {
                templateList.Add(element.Value);
            }

            Templates.ItemsSource = templateList;
        }

        private void Templates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FreshLoad();
        }

        private void FreshLoad()
        {
            m_LoadRequestGrid = false;
            m_LoadRequestEditor = false;
            var template = (string)Templates.SelectedValue;
            XmlDocument document = new XmlDocument();
            try
            {

                requestGrid.ItemsSource = LoadFromSchema(template.ToLower());
                requestEditor.Text = RequestGridToXml();

            }
            catch (Exception ex)
            {
                DisplayError($"Failed with the message: {ex.Message}");
            }

            m_LoadRequestGrid = true;
            m_LoadRequestEditor = true;
        }

        private List<GridNode> LoadFromSchema(string objextName)
        {
            List<WitsmlElement> results = new List<WitsmlElement>();
            var schemaSet = XsdSchemas.GetWriteSchema(objextName.ToLower());
            schemaSet.Compile();
            WitsmlElementTree tempParent = new WitsmlElementTree(null);

            foreach (XmlSchema schema in schemaSet.Schemas())
            {
                foreach (XmlSchemaElement element in schema.Elements.Values)
                {
                    if (!(element.Name == "abstractDataObject" || element.Name == "abstractContextualObject"))
                    {
                        var r = XsdSchemas.ProcessElement(element, tempParent);
                        results.AddRange(r);

                    }
                }
            }

            if(tempParent.Children.Count > 0)
            {
                WitsmlElementTree.Root = tempParent.Children[0];
                WitsmlElementTree.Root.Namespace = GetNameSpace();
            }
            else
            {
                WitsmlElementTree.Root = null;
            }

            WitsmlElementStore.Elements.Clear();
            List<GridNode> nodes = new List<GridNode>();
            foreach (var w in results)
            {
                GridNode node = new GridNode(w);
                node.Selected = true;
                nodes.Add(node);
                WitsmlElementStore.Elements.Add(w.Path, w);
            }

            return nodes;
        }


        public static string PrettifyXML(string xml)
        {
            string result;

            MemoryStream mStream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.Unicode);
            XmlDocument document = new XmlDocument();

            try
            {
                // Load the XmlDocument with the XML.
                document.LoadXml(xml);

                writer.Formatting = System.Xml.Formatting.Indented;

                // Write the XML into a formatting XmlTextWriter
                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                mStream.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader sReader = new StreamReader(mStream);

                // Extract the text from the StreamReader.
                string formattedXml = sReader.ReadToEnd();

                result = formattedXml;
            }
            catch (XmlException)
            {
                result = xml;
            }

            mStream.Close();
            writer.Close();

            return result;
        }


        
        private void OnRequestEditorTextChanged(object sender, EventArgs e)
        {
            if (m_LoadRequestGrid)
            {
                lock (m_RequestId)
                {
                    m_RequestId = Guid.NewGuid().ToString();
                }

                if (m_RequestEditorChangeMonitor == null)
                {

                    m_RequestEditorChangeMonitor = new DispatcherTimer();
                    m_RequestEditorChangeMonitor.Interval = TimeSpan.FromMilliseconds(200);

                    m_RequestEditorChangeMonitor.Tick += new EventHandler(this.HandleRequestEditorChange);
                }
                m_RequestEditorChangeMonitor.Stop();
                m_RequestEditorChangeMonitor.Start();
            }
        }


        private async void HandleRequestEditorChange(object sender, EventArgs e)
        {
            var timer = sender as DispatcherTimer;
            if (timer == null)
            {
                return;
            }


            string request = requestEditor.Text;
            var result = await Task.Run(() => ParseRequestText(request, m_RequestId));

            lock (m_RequestId)
            {
                if (result.Item1 == m_RequestId && result.Item3 == null)
                {
                    requestGrid.ItemsSource = result.Item2;
                }
            }


            // The timer must be stopped! We want to act only once per keystroke.
            timer.Stop();
        }

        private (string, List<GridNode>, Exception) ParseRequestText(string request, string requestid)
        {
            try
            {
                WitsmlElementTree tempParent = new WitsmlElementTree(null);

                XElement element = XElement.Parse(request);
                List<GridNode> result = ParseRequest(element, tempParent, "");

                if(tempParent.Children.Count > 0)
                {
                    WitsmlElementTree.Root = tempParent.Children[0];
                    WitsmlElementTree.Root.Namespace = GetNameSpace();
                }
                else
                {
                    WitsmlElementTree.Root = null;
                }

                return (requestid, result, null);
            }
            catch (Exception ex)
            {
                return (requestid, null, ex);
            }
        }

       

        private List<GridNode> ParseRequest(XElement element, WitsmlElementTree parent, string path, int level = 0)
        {
            List<GridNode> result = new List<GridNode>();

            string name = element.Name.LocalName.ToString();
            string newPath = $"{path}\\{name}";

            if (!WitsmlElementStore.Elements.TryGetValue(newPath, out WitsmlElement w))
            {
                return result;
            }
            else
            {
                w = w.DeepCopy();
            }

            if (element.HasElements == false)
            {
                w.Value = element.Value;
            }

            GridNode node = new GridNode(w);
            result.Add(node);

            WitsmlElementTree child = new WitsmlElementTree(w);
            parent.Children.Add(child);

            if (level > 0)
            {
                foreach (var item in element.Attributes())
                {
                    var attrPath = $"{newPath}\\@{item.Name}";
                    if (WitsmlElementStore.Elements.TryGetValue(attrPath, out w))
                    {
                        w = w.DeepCopy();
                        w.Value = item.Value;
                        GridNode attribute = new GridNode(w);
                        result.Add(attribute);
                        parent.Attributes.Add(w);
                    }
                }
            }

            
            foreach (var item in element.Elements())
            {
                int l = level + 1;
                var r = ParseRequest(item, child, newPath, l);
                result.AddRange(r);
            }

            return result;

        }

        private void OnValueTextChanged(object sender, TextChangedEventArgs e)
        {
            var txt = sender as TextBox;
            if (txt != null)
            {
                var node = txt.Tag as GridNode;
                if (node != null)
                {
                    node.Value = txt.Text;

                    LoadRequestEditorAsync();
                }
            }
        }


        public void OnValueSelctionClicked(object sender, EventArgs e)
        {

        }



        private string RequestGridToXml()
        {
            var nodes = requestGrid.ItemsSource as List<GridNode>;
            XDocument document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));

            if(WitsmlElementTree.Root != null)
            {
                document.Add(WitsmlElementTree.Root.ToXElement());
                document = ApplyRootAttribute(document);
            }

            string result = "";
            using (var writer = new StringWriterWithEncoding(Encoding.UTF8))
            {
                document.Save(writer);
                result = writer.ToString();
            }


            return result;

        }


        private XDocument ApplyRootAttribute(XDocument document)
        {
            var attr = new XAttribute("version", "1.4.1.1");
            document.Root.Add(attr);

            return document;
        }

        private XNamespace GetNameSpace()
        {
            XNamespace ns = "http://www.witsml.org/schemas/1series";
            return ns;
        }


        private void RequestGridSelect_Click(object sender, RoutedEventArgs e)
        {
            if (requestGrid.SelectedItems.Count > 0)
            {
                foreach (GridNode node in requestGrid.SelectedItems)
                {
                    node.Selected = true;
                }
            }

            LoadRequestEditorAsync();
        }

        private void RequestGridDeselect_Click(object sender, RoutedEventArgs e)
        {
            if (requestGrid.SelectedItems.Count > 0)
            {
                foreach (GridNode node in requestGrid.SelectedItems)
                {
                    node.Selected = false;
                }
            }

            LoadRequestEditorAsync();
        }

        private void RequestGridSelectAll_Click(object sender, RoutedEventArgs e)
        {
            var nodes = requestGrid.ItemsSource as List<GridNode>;

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    node.Selected = true;
                }
            }

            LoadRequestEditorAsync();
        }

        private void RequestGridDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            var nodes = requestGrid.ItemsSource as List<GridNode>;

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    node.Selected = false;
                }
            }

            LoadRequestEditorAsync();
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            var nodes = requestGrid.ItemsSource as List<GridNode>;

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    node.Value = null;
                }
            }

            LoadRequestEditorAsync();
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            FreshLoad();
        }



        private void LoadRequestEditorAsync()
        {
            m_LoadRequestGrid = false;
            m_LoadRequestEditor = false;
            var template = (string)Templates.SelectedValue;
            XmlDocument document = new XmlDocument();
            try
            {
                requestEditor.Text = RequestGridToXml();

            }
            catch (Exception ex)
            {
                DisplayError($"Failed with the message: {ex.Message}");
            }

            m_LoadRequestGrid = true;
            m_LoadRequestEditor = true;
        }









        /********* Not used code *****************************/

        private void OptionsSelected(string options)
        {
            this.Dispatcher.Invoke(() =>
            {
                Options.Text = options;
            });
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProgressDisplay.Visibility = Visibility.Visible;

                TotalItems.Visibility = TotalItemCount.Visibility = Visibility.Collapsed;
                ServicePointManager.ServerCertificateValidationCallback = (snder, cert, chain, error) => true;
                WITSMLDataHandler service = new WITSMLDataHandler(ServerUrl.Text, UserName.Text, Password.Password);
                string version = service.WMLS_GetVersion();
                Version.Text = version;

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                var result = service.WMLS_GetCap("dataVersion=1.4.1.1", out string capabilities, out string message);
                stopWatch.Stop();
                elapsedTime.Text = $"{stopWatch.ElapsedMilliseconds} Milliseconds";

                if (result <= 0)
                {
                    responceEditor.Text = $"<message>Failed with the message: {message} </message>";
                    ProgressDisplay.Visibility = Visibility.Collapsed;
                    return;
                }

                treeView.ItemsSource = null;

                if (EnableTree.IsChecked.Value)
                {
                    XDocument doc = XDocument.Parse(capabilities);

                    WitsmlNode witsmlNode = new WitsmlNode();
                    witsmlNode.Name = doc.Root.Name.LocalName;
                    if (doc.Root.HasElements)
                    {
                        var children = GetChildren(doc.Root);
                        if (children != null)
                        {
                            witsmlNode.Children.AddRange(children);
                        }
                    }
                    else
                    {
                        witsmlNode.Value = doc.Root.Value;
                    }

                    
                    var nodeList = new List<WitsmlNode>();
                    nodeList.Add(witsmlNode);

                    treeView.ItemsSource = nodeList;
                }

                responceEditor.Text = capabilities;
                ProgressDisplay.Visibility = Visibility.Collapsed;

                SaveToPreference();

                m_TabHeader.Tittle = ServerUrl.Text;
            }
            catch (Exception ex)
            {
                ProgressDisplay.Visibility = Visibility.Collapsed;
                DisplayError($"Failed with the message: {ex.Message}");
            }
        }

        private void DisplayError(string message)
        {
            responceEditor.Text = message;
            var result = new List<WitsmlNode>();
            WitsmlNode witsmlNode = new WitsmlNode()
            {
                Name = "Error",
                Value = message
            };
            result.Add(witsmlNode);

            treeView.ItemsSource = result;

        }

        private void DisplayMessage(string message)
        {
            responceEditor.Text = message;
            var result = new List<WitsmlNode>();
            WitsmlNode witsmlNode = new WitsmlNode()
            {
                Name = "Message",
                Value = message
            };
            result.Add(witsmlNode);

            treeView.ItemsSource = result;
        }

        private List<WitsmlNode> GetChildren(XElement element)
        {

            var result = new List<WitsmlNode>();
            foreach (XNode child in element.Nodes())
            {
                switch (child.NodeType)
                {
                    case XmlNodeType.Element:
                        XElement childElement = child as XElement;
                        var childNode = new WitsmlNode();


                        childNode.Name = childElement.Name.LocalName;
                        if (!childElement.HasElements)
                        {
                            childNode.Value = childElement.Value;
                        }

                        foreach (var att in childElement.Attributes())
                        {
                            WitsmlNode a = new WitsmlNode();
                            a.Name = att.Name.LocalName;
                            a.Value = att.Value;
                            childNode.Attributes.Add(a);
                        }

                        var children = GetChildren(childElement);
                        childNode.Children.AddRange(children);

                        result.Add(childNode);



                        //string attributes = "";
                        //if (childElement.HasAttributes)
                        //{
                        //    foreach (var att in childElement.Attributes())
                        //    {
                        //        attributes += " " + att.Name + "=\"" + att.Value + "\"";
                        //    }
                        //}


                        //string value;

                        //if (childElement.HasElements)
                        //{
                        //    value = "";
                        //}
                        //else
                        //{
                        //    value = childElement.Value;
                        //}

                        //var level = (int)treeNode.Tag;

                        //TreeViewItemHeader header = new TreeViewItemHeader();
                        //header.HeaderText = childElement.Name.LocalName;
                        //header.ValueText = value;
                        //header.AttributeText = attributes;

                        //TreeViewItem childTreeNode = new TreeViewItem
                        //{
                        //    //Get First attribute where it is equal to value
                        //    Header = header,
                        //    //Automatically expand elements
                        //    IsExpanded = expand,
                        //    Tag = level + 1
                        //};


                        //if (childElement.Name.LocalName.ToLower() == "logdata")
                        //{
                        //    logDataGrid.Visibility = Visibility.Visible;
                        //    Grid.SetRowSpan(treeView, 1);

                        //    DataTable dt = new DataTable();
                        //    DisplayLogdata(childTreeNode, childElement, ref dt);
                        //    logDataGrid.ItemsSource = new DataView(dt);
                        //}
                        //else
                        //{
                        //    treeNode.Items.Add(childTreeNode);
                        //    BuildNodes(childTreeNode, childElement, false);
                        //}
                        break;
                    case XmlNodeType.Text:
                        //XText childText = child as XText;
                        //treeNode.Items.Add(new TreeViewItem { Header = childText.Value, });
                        break;
                }
            }

            return result;
        }

        private void DisplayLogdata(TreeViewItem treeNode, XElement element, ref DataTable dt)
        {
            foreach (XNode child in element.Nodes())
            {
                switch (child.NodeType)
                {
                    case XmlNodeType.Element:
                        XElement childElement = child as XElement;
                        DisplayLogdata(treeNode, childElement, ref dt);
                        break;
                    case XmlNodeType.Text:
                        XText childText = child as XText;
                        if (childText.Parent.Name.LocalName.ToLower() == "mnemoniclist")
                        {
                            string[] headers = childText.Value.Split(',');
                            foreach (var h in headers)
                            {
                                DataColumn column = new DataColumn()
                                {
                                    ColumnName = h,
                                    DataType = typeof(string)
                                };

                                dt.Columns.Add(column);
                            }
                        }

                        if (childText.Parent.Name.LocalName.ToLower() == "unitlist")
                        {
                            string[] units = childText.Value.Split(',');

                            for (int i = 0; i < units.Length; i++)
                            {
                                var unit = units[i];
                                if (string.IsNullOrWhiteSpace(unit) == false)
                                {
                                    dt.Columns[i].ColumnName += $" ({unit})";
                                }
                            }
                        }

                        if (childText.Parent.Name.LocalName.ToLower() == "data")
                        {
                            string[] datas = childText.Value.Split(',');
                            var row = dt.NewRow();
                            for (int i = 0; i < datas.Length; i++)
                            {
                                var d = datas[i];
                                row[i] = d;
                            }

                            dt.Rows.Add(row);
                        }

                        break;
                }
            }
        }





        private void SaveToPreference()
        {
            Properties.Settings.Default.Url = ServerUrl.Text;
            Properties.Settings.Default.UserName = UserName.Text;
            Properties.Settings.Default.Password = Password.Password;

            string serversJson = Properties.Settings.Default.Servers;
            List<ServerInfo> servers = new List<ServerInfo>();
            if (string.IsNullOrWhiteSpace(serversJson) == false)
            {
                servers = JsonConvert.DeserializeObject<List<ServerInfo>>(serversJson);
            }

            bool present = false;
            foreach (var item in servers)
            {
                if (item.Url.ToLower() == ServerUrl.Text.ToLower() && item.UserName.ToLower() == item.UserName.ToLower())
                {
                    item.Password = Password.Password;
                    present = true;
                    break;
                }
            }

            if (present == false)
            {
                servers.Add(new ServerInfo()
                {
                    Url = ServerUrl.Text,
                    UserName = UserName.Text,
                    Password = Password.Password

                });
            }


            Properties.Settings.Default.Servers = JsonConvert.SerializeObject(servers);

            Properties.Settings.Default.Save();
        }

        private void LoadPreference()
        {
            ServerUrl.Text = Properties.Settings.Default.Url ?? "";
            UserName.Text = Properties.Settings.Default.UserName ?? "";
            Password.Password = Properties.Settings.Default.Password ?? "";
            m_TabHeader.Tittle = Properties.Settings.Default.Url ?? "localhost";
        }

        private void OnTextChanged(object sender, ElapsedEventArgs e)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    DataTable modifiedDataTable = new DataTable();
                    modifiedDataTable.Columns.Add("select", typeof(bool));
                    modifiedDataTable.Columns.Add("name", typeof(string));
                    modifiedDataTable.Columns.Add("value", typeof(string));
                    modifiedDataTable.Columns.Add("type", typeof(int));
                    modifiedDataTable.Columns.Add("level", typeof(int));
                    modifiedDataTable.Columns.Add("parent", typeof(string));

                    if (!string.IsNullOrEmpty(requestEditor.Text))
                    {
                        XDocument xDocument = XDocument.Parse(requestEditor.Text);
                        ParseRequest(xDocument.Root, 0, ref modifiedDataTable, string.Empty);
                    }

                });
            }
            catch (Exception ex)
            {
            }
        }



        public DataTable CompareAndUpdateDataTable(DataTable currentDataTable, DataTable modifiedDataTable)
        {
            for (int i = 0; i < currentDataTable.Rows.Count; i++)
            {
                if (modifiedDataTable.Rows.Count != 0)
                {
                    bool isExist = false;
                    for (int j = 0; j < modifiedDataTable.Rows.Count; j++)
                    {
                        if (modifiedDataTable.Rows[j][1].ToString().Trim() == currentDataTable.Rows[i][1].ToString().Trim()//check name
                            && Convert.ToInt32(modifiedDataTable.Rows[j][4]) == Convert.ToInt32(currentDataTable.Rows[i][4])//check level
                            && modifiedDataTable.Rows[j][5].ToString().Trim() == currentDataTable.Rows[i][5].ToString().Trim())//check parent
                        {
                            currentDataTable.Rows[i][0] = true;
                            currentDataTable.Rows[i][1] = modifiedDataTable?.Rows[j][1];
                            currentDataTable.Rows[i][2] = modifiedDataTable?.Rows[j][2];
                            currentDataTable.Rows[i][3] = modifiedDataTable?.Rows[j][3];
                            currentDataTable.Rows[i][4] = modifiedDataTable?.Rows[j][4];
                            isExist = true;
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    if (!isExist)
                    {
                        currentDataTable.Rows[i][0] = false;
                    }
                }
                else
                {
                    currentDataTable.Rows[i][0] = false;
                }
            }
            return currentDataTable;
        }



        private void ParseRequest(XElement element, int level, ref DataTable dt, string parentName)
        {
            parentName = parentName + "\\";
            DataRow row;
            foreach (XNode child in element.Nodes())
            {
                switch (child.NodeType)
                {
                    case XmlNodeType.Element:
                        XElement childElement = child as XElement;

                        if (childElement.HasElements)
                        {
                            string parent = parentName + childElement.Parent.Name.LocalName;
                            row = dt.NewRow();
                            row[0] = true;
                            row[1] = new string(' ', level) + new string(' ', level) + childElement.Name.LocalName;
                            row[2] = "";
                            row[3] = 1;
                            row[4] = level;
                            row[5] = parent;

                            dt.Rows.Add(row);

                            foreach (var attribute in childElement.Attributes())
                            {
                                row = dt.NewRow();
                                row[0] = true;
                                row[1] = new string(' ', level + 1) + new string(' ', level + 1) + attribute.Name;
                                row[2] = attribute.Value;
                                row[3] = 2;
                                row[4] = level + 0;
                                row[5] = parent + "\\" + attribute.Parent.Name.LocalName;
                                dt.Rows.Add(row);
                            }

                            ParseRequest(childElement, level + 1, ref dt, parent);
                        }
                        else
                        {
                            string parent = parentName + childElement.Parent.Name.LocalName;
                            row = dt.NewRow();
                            row[0] = true;
                            row[1] = new string(' ', level) + new string(' ', level) + childElement.Name.LocalName;

                            if (childElement.FirstNode != null && childElement.FirstNode.NodeType == XmlNodeType.Text)
                            {
                                row[2] = (childElement.FirstNode as XText).Value;
                            }
                            row[3] = 0;
                            row[4] = level;
                            row[5] = parent;
                            dt.Rows.Add(row);

                            foreach (var attribute in childElement.Attributes())
                            {
                                row = dt.NewRow();
                                row[0] = true;
                                row[1] = new string(' ', level + 1) + new string(' ', level + 1) + attribute.Name;
                                row[2] = attribute.Value;
                                row[3] = 2;
                                row[4] = level + 0;
                                row[5] = parent + "\\" + attribute.Parent.Name.LocalName;
                                dt.Rows.Add(row);
                            }
                        }
                        break;
                }
            }
        }






        private void LoadEmptyTemplatesXML()
        {
            //XmlDocument document = new XmlDocument();
            //try
            //{
            //    string templatePath = string.Empty;
            //    string key = $"{Templates.SelectedValue}${m_SubTemplate}";
            //    if (m_KeyData.ContainsKey(key))
            //    {
            //        templatePath = m_KeyData[key];
            //        document.Load(templatePath);
            //        XmlNode root = document.DocumentElement;
            //        root.RemoveAll();
            //        CommonData(document);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    DisplayError($"<message>Failed with the message: {ex.Message} </message>");
            //}
        }


        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int pointer = 1;
                string searchedText = Search.Text;
                if (responceEditor.Visibility == Visibility.Visible)
                {
                    Color backgroundColor = (Color)ColorConverter.ConvertFromString("Yellow");
                    Color fontColor = (Color)ColorConverter.ConvertFromString("Black");
                    int counter = 1;
                    if (searchedText.Length != 0)
                    {
                        while (counter < 3)
                        {
                            int index = responceEditor.Text.ToLower().IndexOf(searchedText.ToLower(), pointer);
                            //if keyword not found
                            if (index == -1)
                            {
                                break;
                            }
                            else
                            {
                                //else TODO
                                responceEditor.Select(index, searchedText.Length);
                                //   to highlight
                                responceEditor.TextArea.SelectionBrush = new SolidColorBrush(backgroundColor);
                                responceEditor.TextArea.SelectionForeground = new SolidColorBrush(fontColor);
                                pointer = index + searchedText.Length;
                                //TODO

                            }
                            counter++;
                        }
                    }
                    else
                    {
                        responceEditor.Select(0, 0);
                    }


                }
            }
            catch (Exception ex)
            {
                DisplayError($"<message>Failed with the message: {ex.Message} </message>");
            }
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProgressDisplay.Visibility = Visibility.Visible;
                TotalItems.Visibility = TotalItemCount.Visibility = Visibility.Collapsed;
                WITSMLDataHandler service = new WITSMLDataHandler(ServerUrl.Text, UserName.Text, Password.Password);

                string witsmlTypein = GetType(requestEditor.Text);
                string query = requestEditor.Text;
                string optionIn = Options.Text;
                string capabilities = "";

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                var result = service.WMLS_GetFromStore(witsmlTypein, query, optionIn, capabilities, out string output, out string message);
                stopWatch.Stop();
                elapsedTime.Text = $"{stopWatch.ElapsedMilliseconds} Milliseconds";

                if (result <= 0)
                {
                    DisplayError($"Failed with the message: {message}");
                    ProgressDisplay.Visibility = Visibility.Collapsed;
                    return;
                }

                XDocument doc = XDocument.Parse(output);
                treeView.ItemsSource = null;

                if (EnableTree.IsChecked.Value)
                {
                    WitsmlNode witsmlNode = new WitsmlNode();
                    witsmlNode.Name = doc.Root.Name.LocalName;
                    if (doc.Root.HasElements)
                    {
                        var children = GetChildren(doc.Root);
                        if (children != null)
                        {
                            witsmlNode.Children.AddRange(children);
                        }
                    }
                    else
                    {
                        witsmlNode.Value = doc.Root.Value;
                    }


                    var nodeList = new List<WitsmlNode>();
                    nodeList.Add(witsmlNode);

                    treeView.ItemsSource = nodeList;
                }



                TotalItemCount.Text = $"{Convert.ToString(doc.Root?.Nodes()?.Count())} ";
                TotalItems.Visibility = TotalItemCount.Visibility = Visibility.Visible;

                output = PrettifyXML(output);
                responceEditor.Text = output;
                ProgressDisplay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ProgressDisplay.Visibility = Visibility.Collapsed;
                DisplayError($"Failed with the message: {ex.Message} ");
            }
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProgressDisplay.Visibility = Visibility.Visible;
                TotalItems.Visibility = TotalItemCount.Visibility = Visibility.Collapsed;
                WITSMLDataHandler service = new WITSMLDataHandler(ServerUrl.Text, UserName.Text, Password.Password);

                string witsmlTypein = GetType(requestEditor.Text);
                string query = requestEditor.Text;
                string optionIn = Options.Text;
                string capabilities = "";


                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                var result = service.WMLS_UpdateInStore(witsmlTypein, query, optionIn, capabilities, out string message);
                stopWatch.Stop();
                elapsedTime.Text = $"{stopWatch.ElapsedMilliseconds} Milliseconds";

                if (result <= 0)
                {
                    DisplayError($"Failed with the message: {message}");
                    ProgressDisplay.Visibility = Visibility.Collapsed;
                    return;
                }

                DisplayMessage(message);
                ProgressDisplay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                DisplayError($"Failed with the message: {ex.Message} ");
                ProgressDisplay.Visibility = Visibility.Collapsed;
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProgressDisplay.Visibility = Visibility.Visible;
                TotalItems.Visibility = TotalItemCount.Visibility = Visibility.Collapsed;
                WITSMLDataHandler service = new WITSMLDataHandler(ServerUrl.Text, UserName.Text, Password.Password);

                string witsmlTypein = GetType(requestEditor.Text);
                string query = requestEditor.Text;
                string optionIn = Options.Text;
                string capabilities = "";


                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                var result = service.WMLS_AddToStore(witsmlTypein, query, optionIn, capabilities, out string message);
                stopWatch.Stop();
                elapsedTime.Text = $"{stopWatch.ElapsedMilliseconds} Milliseconds";

                if (result <= 0)
                {
                    DisplayError($"Failed with the message: {message}");
                    ProgressDisplay.Visibility = Visibility.Collapsed;
                    return;
                }

                DisplayMessage(message);
                ProgressDisplay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                DisplayError($"Failed with the message: {ex.Message} ");
                ProgressDisplay.Visibility = Visibility.Collapsed;
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProgressDisplay.Visibility = Visibility.Visible;
                TotalItems.Visibility = TotalItemCount.Visibility = Visibility.Collapsed;
                WITSMLDataHandler service = new WITSMLDataHandler(ServerUrl.Text, UserName.Text, Password.Password);

                string witsmlTypein = GetType(requestEditor.Text);
                string query = requestEditor.Text;
                string optionIn = Options.Text;
                string capabilities = "";


                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                var result = service.WMLS_DeleteFromStore(witsmlTypein, query, optionIn, capabilities, out string message);
                stopWatch.Stop();
                elapsedTime.Text = $"{stopWatch.ElapsedMilliseconds} Milliseconds";

                if (result <= 0)
                {
                    DisplayError($"Failed with the message: {message}");
                    ProgressDisplay.Visibility = Visibility.Collapsed;
                    return;
                }

                DisplayMessage(message);
                ProgressDisplay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                DisplayError($"Failed with the message: {ex.Message} ");
                ProgressDisplay.Visibility = Visibility.Collapsed;
            }
        }



        private string GetType(string xml)
        {
            XDocument doc = XDocument.Parse(xml);
            var stringName = Templates.Text;
            if (doc.Root != null && doc.Root.FirstNode != null)
            {
                var element = doc.Root.FirstNode as XElement;
                if (element != null)
                {
                    stringName = element.Name.LocalName;
                }
            }

            return stringName.ToLower();

        }



        private void OnValueTextChanged(object sender, EventArgs eventArg)
        {
            //try
            //{
            //    if (requestGrid.IsKeyboardFocusWithin)
            //    {
            //        var valueTextBox = ((TextBox)sender).Text;
            //        if (!string.IsNullOrEmpty(valueTextBox))
            //        {
            //            var items = requestGrid.SelectedCells;
            //            foreach (var item in items)
            //            {
            //                ((DataRowView)item.Item).Row[2] = valueTextBox;
            //                ((DataRowView)item.Item).Row[0] = true;
            //            }

            //        }
            //    }
            //}
            //catch (Exception ex)
            //{

            //}
        }



        private void GetOption_Click(object sender, RoutedEventArgs e)
        {
            M_optionSelection = new OptionSelection();
            M_optionSelection.OptionsSelected = OptionsSelected;
            M_optionSelection.ShowDialog();
        }

        private void ChooseButton_Click(object sender, RoutedEventArgs e)
        {
            Servers servers = new Servers();
            servers.Load();
            servers.ServerSelected = DisplayServer;
            servers.ShowDialog();
        }

        private void DisplayServer(string url, string username, string password)
        {
            ServerUrl.Text = url;
            UserName.Text = username;
            Password.Password = password;
        }

        private void XmlToggle_Click(object sender, RoutedEventArgs e)
        {
            if (XmlToggle.IsChecked == false)
            {
                responceEditor.Visibility = Visibility.Collapsed;
                treeEditor.Visibility = Visibility.Visible;
            }
            else
            {
                responceEditor.Visibility = Visibility.Visible;
                treeEditor.Visibility = Visibility.Collapsed;
            }
        }

        private void EnableTree_Click(object sender, RoutedEventArgs e)
        {
            if (!EnableTree.IsChecked.Value)
            {
                responceEditor.Visibility = Visibility.Visible;
                treeEditor.Visibility = Visibility.Collapsed;
                XmlToggle.Visibility = Visibility.Collapsed;

            }
            else
            {
                XmlToggle.IsChecked = true;
                responceEditor.Visibility = Visibility.Collapsed;
                treeEditor.Visibility = Visibility.Visible;
                XmlToggle.Visibility = Visibility.Visible;
            }
        }

        private void OnValueSelctionClicked(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            GridNode node = button.Tag as GridNode;
            node.IsPopupOpen = true;

        }

        private void RestrictionDataGrid_Selected(object sender, RoutedEventArgs e)
        {
            DataGrid dg = sender as DataGrid;
            GridNode gridNode = dg.Tag as GridNode;
            gridNode.IsPopupOpen = false;
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            PerformGridClick(dep, gridNode);
        }

        private void PerformGridClick(DependencyObject dep, GridNode gridNode)
        {
            while ((dep != null) &&
           !(dep is DataGridCell) &&
           !(dep is DataGridColumnHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
                return;

            if (dep is DataGridColumnHeader)
            {
                DataGridColumnHeader columnHeader = dep as DataGridColumnHeader;
                // do something
            }

            if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;
                if (cell.Column.DisplayIndex == 0)
                {
                    DataGridRow row = DataGridRow.GetRowContainingElement(cell);

                    string value = row.Item as string;
                    if (value != null)
                    {
                        gridNode.Value = value;
                    }

                }
            }
        }

        
    }
}
