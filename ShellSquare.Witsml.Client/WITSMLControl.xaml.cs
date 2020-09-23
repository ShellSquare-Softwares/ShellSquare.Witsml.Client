﻿using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
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
        private DispatcherTimer m_DelayFlagUpdate;
        private DispatcherTimer m_RequestEditorChangeMonitor;
        private OptionSelection m_OptionSelection;
        private TabHeaderControl m_TabHeader;
        private bool m_ProgrammaticallyChanging = false;
        private bool m_ValueTextBoxFocused = false;
        public string ViewDeterminant = "T";//T stands for Tree View and X stands for XML view.U FOR UNKNOWN TYPE


        private readonly SearchPanel searchPanel;
        public WITSMLControl(TabHeaderControl tabHeader)
        {
            m_TabHeader = tabHeader;
            InitializeComponent();
            LoadPreference();
            LoadTemplateList();

            searchPanel = SearchPanel.Install(responceEditor);


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
            var template = (string)Templates.SelectedValue;
            try
            {
                m_ProgrammaticallyChanging = true;
                requestGrid.ItemsSource = LoadFromSchema(template.ToLower());
                requestEditor.Text = RequestGridToXml();
                m_ProgrammaticallyChanging = false;
            }
            catch (Exception ex)
            {
                DisplayError($"Failed with the message: {ex.Message}");
            }
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

            if (tempParent.Children.Count > 0)
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
        private string RequestGridToXml()
        {
            XDocument document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));

            if (WitsmlElementTree.Root != null)
            {
                document.Add(WitsmlElementTree.Root.ToXElement());
                document = ApplyRootAttribute(document);
            }

            string result = "";
            using (var writer = new StringWriterWithEncoding(Encoding.UTF8))
            {
                var settings = new XmlWriterSettings()
                {
                    Indent = true
                };

                using (var xw = XmlWriter.Create(writer, settings))
                {
                    document.WriteTo(xw);
                    xw.Flush();

                    result = writer.ToString();
                }
            }

            return result;
        }
        private List<GridNode> RequestXmlToGrid(string request)
        {
            WitsmlElementTree tempParent = new WitsmlElementTree(null);

            XElement element = XElement.Parse(request);
            List<GridNode> result = ParseRequest(element, tempParent, "");

            if (tempParent.Children.Count > 0)
            {
                WitsmlElementTree.Root = tempParent.Children[0];
                WitsmlElementTree.Root.Namespace = GetNameSpace();
            }
            else
            {
                WitsmlElementTree.Root = null;
            }

            return result;
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
                if (element.Value != "")
                {
                    w.Value = element.Value;
                }
            }

            w.Selected = true;

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
                        w.Selected = true;
                        w.Value = item.Value;
                        GridNode attribute = new GridNode(w);
                        result.Add(attribute);
                        child.Attributes.Add(w);
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
            if (m_ProgrammaticallyChanging == false)
            {
                m_ProgrammaticallyChanging = true;
                if (m_RequestEditorChangeMonitor == null)
                {

                    m_RequestEditorChangeMonitor = new DispatcherTimer();
                    m_RequestEditorChangeMonitor.Interval = TimeSpan.FromMilliseconds(1000);

                    m_RequestEditorChangeMonitor.Tick += new EventHandler(this.HandleRequestEditorChange);
                }
                m_RequestEditorChangeMonitor.Stop();
                m_RequestEditorChangeMonitor.Start();
            }
        }

        private void HandleRequestEditorChange(object sender, EventArgs e)
        {
            var timer = sender as DispatcherTimer;
            if (timer == null)
            {
                return;
            }
            timer.Stop();

            string request = requestEditor.Text;
            LoadRequestGrid(request);

            if (request == requestEditor.Text)
            {
                timer.Stop();
            }
        }

        private void ValueTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            m_ValueTextBoxFocused = true;
        }

        private void ValueTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            m_ValueTextBoxFocused = false;
        }

        private void OnValueTextChanged(object sender, TextChangedEventArgs e)
        {
            if (m_ValueTextBoxFocused == true)
            {
                m_ProgrammaticallyChanging = true;
                var txt = sender as TextBox;
                if (txt != null)
                {
                    var node = txt.Tag as GridNode;
                    if (node != null)
                    {
                        node.Value = txt.Text;
                        node.Selected = true;
                        LoadRequestEditor();
                    }
                }
            }
        }



        public void OnValueSelctionClicked(object sender, EventArgs e)
        {

        }

        private void LoadRequestGrid(string request)
        {
            try
            {
                requestGrid.ItemsSource = RequestXmlToGrid(request);
            }
            catch (Exception ex)
            {
                DisplayError($"Failed with the message: {ex.Message}");
            }

            ResetFlag();
        }

        private void LoadRequestEditor()
        {
            try
            {
                requestEditor.Text = RequestGridToXml();
            }
            catch (Exception ex)
            {
                DisplayError($"Failed with the message: {ex.Message}");
            }

            ResetFlag();
        }


        private void ResetFlag()
        {
            if (m_DelayFlagUpdate == null)
            {

                m_DelayFlagUpdate = new DispatcherTimer();
                m_DelayFlagUpdate.Interval = TimeSpan.FromMilliseconds(200);

                m_DelayFlagUpdate.Tick += (s, e) =>
                {
                    var timer = s as DispatcherTimer;
                    if (timer == null)
                    {
                        return;
                    }
                    timer.Stop();


                    m_ProgrammaticallyChanging = false;


                };
            }
            m_DelayFlagUpdate.Stop();
            m_DelayFlagUpdate.Start();

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
            if (m_ProgrammaticallyChanging == false)
            {
                m_ProgrammaticallyChanging = true;
                if (requestGrid.SelectedItems.Count > 0)
                {
                    foreach (GridNode node in requestGrid.SelectedItems)
                    {
                        node.Selected = true;
                    }
                }

                LoadRequestEditor();
            }
        }

        private void RequestGridDeselect_Click(object sender, RoutedEventArgs e)
        {
            if (m_ProgrammaticallyChanging == false)
            {
                m_ProgrammaticallyChanging = true;
                if (requestGrid.SelectedItems.Count > 0)
                {
                    foreach (GridNode node in requestGrid.SelectedItems)
                    {
                        node.Selected = false;
                    }
                }

                LoadRequestEditor();
            }
        }

        private void RequestGridSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (m_ProgrammaticallyChanging == false)
            {
                m_ProgrammaticallyChanging = true;
                var nodes = requestGrid.ItemsSource as List<GridNode>;

                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        node.Selected = true;
                    }
                }

                LoadRequestEditor();
            }
        }

        private void RequestGridDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            if (m_ProgrammaticallyChanging == false)
            {
                m_ProgrammaticallyChanging = true;
                var nodes = requestGrid.ItemsSource as List<GridNode>;

                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        node.Selected = false;
                    }
                }

                LoadRequestEditor();
            }
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (m_ProgrammaticallyChanging == false)
            {
                m_ProgrammaticallyChanging = true;
                var nodes = requestGrid.ItemsSource as List<GridNode>;

                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        node.Value = "";
                    }
                }

                LoadRequestEditor();
            }
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            FreshLoad();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (m_ProgrammaticallyChanging == false)
            {
                m_ProgrammaticallyChanging = true;
                var check = sender as CheckBox;
                if (check != null)
                {
                    var node = check.Tag as GridNode;
                    if (node != null)
                    {
                        node.Selected = check.IsChecked.Value;
                        LoadRequestEditor();
                    }
                }
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
                        gridNode.Selected = true;

                        if (m_ProgrammaticallyChanging == false)
                        {
                            m_ProgrammaticallyChanging = true;
                            LoadRequestEditor();
                        }
                    }

                }
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProgressDisplay.Visibility = Visibility.Visible;

                logDataGrid.Visibility = Visibility.Collapsed;
                Grid.SetRowSpan(treeView, 2);

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
                    LoadResponceGrid(doc);
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

        private void LoadResponceGrid(XDocument doc)
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

        private void GetOption_Click(object sender, RoutedEventArgs e)
        {
            m_OptionSelection = new OptionSelection();
            m_OptionSelection.OptionsSelected = OptionsSelected;
            m_OptionSelection.ShowDialog();
        }

        private void OptionsSelected(string options)
        {
            this.Dispatcher.Invoke(() =>
            {
                Options.Text = options;
            });
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


                        if (childElement.Name.LocalName.ToLower() == "logdata")
                        {
                            logDataGrid.Visibility = Visibility.Visible;
                            Grid.SetRowSpan(treeView, 1);

                            DataTable dt = new DataTable();
                            DisplayLogdata(childElement, ref dt);
                            logDataGrid.ItemsSource = new DataView(dt);
                        }
                        else
                        {
                            var children = GetChildren(childElement);
                            childNode.Children.AddRange(children);

                            result.Add(childNode);
                        }
                        break;
                    case XmlNodeType.Text:
                        break;
                }
            }

            return result;
        }

        private void DisplayLogdata(XElement element, ref DataTable dt)
        {
            foreach (XNode child in element.Nodes())
            {
                switch (child.NodeType)
                {
                    case XmlNodeType.Element:
                        XElement childElement = child as XElement;
                        DisplayLogdata(childElement, ref dt);
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

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProgressDisplay.Visibility = Visibility.Visible;

                logDataGrid.Visibility = Visibility.Collapsed;
                Grid.SetRowSpan(treeView, 2);

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

                    LoadResponceGrid(doc);

                    //WitsmlNode witsmlNode = new WitsmlNode();
                    //witsmlNode.Name = doc.Root.Name.LocalName;
                    //if (doc.Root.HasElements)
                    //{
                    //    var children = GetChildren(doc.Root);
                    //    if (children != null)
                    //    {
                    //        witsmlNode.Children.AddRange(children);
                    //    }
                    //}
                    //else
                    //{
                    //    witsmlNode.Value = doc.Root.Value;
                    //}


                    //var nodeList = new List<WitsmlNode>();
                    //nodeList.Add(witsmlNode);

                    //treeView.ItemsSource = nodeList;
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

        private void ChooseButton_Click(object sender, RoutedEventArgs e)
        {
            Servers servers = new Servers();
            servers.Load();
            servers.ServerSelected = OnServerSelected;
            servers.ShowDialog();
        }

        private void OnServerSelected(string url, string username, string password)
        {
            ServerUrl.Text = url;
            UserName.Text = username;
            Password.Password = password;
        }

        private void XmlToggle_Click(object sender, RoutedEventArgs e)
        {
            if (XmlToggle.IsChecked == false)
            {
                ViewDeterminant = "T";//TREE
                responceEditor.Visibility = Visibility.Collapsed;
                treeEditor.Visibility = Visibility.Visible;
            }
            else
            {
                ViewDeterminant = "X";//XML
                responceEditor.Visibility = Visibility.Visible;
                treeEditor.Visibility = Visibility.Collapsed;
            }
        }
        private void EnableTree_Click(object sender, RoutedEventArgs e)
        {
            if (!EnableTree.IsChecked.Value)
            {
                XmlToggle.IsChecked = false;
                responceEditor.Visibility = Visibility.Visible;
                treeEditor.Visibility = Visibility.Collapsed;
                XmlToggle.Visibility = Visibility.Collapsed;
            }
            else
            {
                XmlToggle.IsChecked = false;
                responceEditor.Visibility = Visibility.Collapsed;
                treeEditor.Visibility = Visibility.Visible;
                XmlToggle.Visibility = Visibility.Visible;

                XDocument doc = XDocument.Parse(responceEditor.Text);
                LoadResponceGrid(doc);
            }
        }


        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            //if (txtSearch.Text.Trim() == "") 
            //{
            //    txtSearch.Text = "Search";
            //    return;
            //}

            if (txtSearch.Text.Trim() == "Search")
            {
                txtSearch.Clear();
                return;
            }
        }

        private void treeCollapseAll_Click(object sender, RoutedEventArgs e)
        {

            treeExpandAll.Visibility = Visibility.Visible;
            treeCollapseAll.Visibility = Visibility.Collapsed;
            treeView.SetExpansion(isExpanded: false);


        }

        public void CollapseTreeviewItems(TreeViewItem Item)
        {
            Item.IsExpanded = false;
            foreach (TreeViewItem item in Item.Items)
            {
                item.IsExpanded = false;

                if (item.HasItems)
                    CollapseTreeviewItems(item);
            }

        }

        private void treeExpandAll_Click(object sender, RoutedEventArgs e)
        {
            treeCollapseAll.Visibility = Visibility.Visible;
            treeExpandAll.Visibility = Visibility.Collapsed;
            treeView.SetExpansion(isExpanded: true);
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            //TreeViewItem tv = obj as TreeViewItem;
            if (ViewDeterminant == "T")
            {
                if (this.treeView != null)
                    FindControlItem(this.treeView);
            }

            if (ViewDeterminant == "X")
            {
                    
                    findIteminAvalon();
            }
            //FindControlItem(VisualTreeHelper.GetChild(obj as DependencyObject, i));
        }

        private void findIteminAvalon()
        {
            searchPanel.SearchPattern = txtSearch.Text;
            searchPanel.Open();
            searchPanel.Visibility = Visibility.Collapsed;
            searchPanel.Reactivate();
            
            //responceEditor.TextArea.TextView.LineTransformers.Add(new ColorizeAvalonEdit(txtSearch.Text));
            //responceEditor.TextArea.SelectionChanged += textEditor_TextArea_SelectionChanged;
        }
        void textEditor_TextArea_SelectionChanged(object sender, EventArgs e)
        {
            this.responceEditor.TextArea.TextView.Redraw();
        }
        public void FindControlItem(DependencyObject obj)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                //ListViewItem lv = obj as ListViewItem;
                //DataGridCell dg = obj as DataGridCell;
                TreeViewItem tv = obj as TreeViewItem;
                if (tv != null)
                {
                    HighlightText(tv);
                }
                FindControlItem(VisualTreeHelper.GetChild(obj as DependencyObject, i));
            }
        }


        private void HighlightText(Object itx)
        {
            if (itx != null)
            {
                if (itx is TextBlock)
                {
                    Regex regex = new Regex("(" + txtSearch.Text + ")", RegexOptions.IgnoreCase);
                    TextBlock tb = itx as TextBlock;
                    if (txtSearch.Text.Length == 0)
                    {
                        string str = tb.Text;
                        tb.Inlines.Clear();
                        tb.Inlines.Add(str);
                        return;
                    }
                    string[] substrings = regex.Split(tb.Text);
                    tb.Inlines.Clear();
                    foreach (var item in substrings)
                    {
                        if (regex.Match(item).Success)
                        {
                            Run runx = new Run(item);
                            runx.Background = Brushes.LightBlue;
                            tb.Inlines.Add(runx);
                        }
                        else
                        {
                            tb.Inlines.Add(item);
                        }
                    }
                    // return;
                }
                else
                {
                    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(itx as DependencyObject); i++)
                    {
                        HighlightText(VisualTreeHelper.GetChild(itx as DependencyObject, i));
                    }
                }
            }
        }


        /********* Not used code *****************************/



        //private void Search_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    try
        //    {
        //        int pointer = 1;
        //        string searchedText = Search.Text;
        //        if (responceEditor.Visibility == Visibility.Visible)
        //        {
        //            Color backgroundColor = (Color)ColorConverter.ConvertFromString("Yellow");
        //            Color fontColor = (Color)ColorConverter.ConvertFromString("Black");
        //            int counter = 1;
        //            if (searchedText.Length != 0)
        //            {
        //                while (counter < 3)
        //                {
        //                    int index = responceEditor.Text.ToLower().IndexOf(searchedText.ToLower(), pointer);
        //                    //if keyword not found
        //                    if (index == -1)
        //                    {
        //                        break;
        //                    }
        //                    else
        //                    {
        //                        //else TODO
        //                        responceEditor.Select(index, searchedText.Length);
        //                        //   to highlight
        //                        responceEditor.TextArea.SelectionBrush = new SolidColorBrush(backgroundColor);
        //                        responceEditor.TextArea.SelectionForeground = new SolidColorBrush(fontColor);
        //                        pointer = index + searchedText.Length;
        //                        //TODO

        //                    }
        //                    counter++;
        //                }
        //            }
        //            else
        //            {
        //                responceEditor.Select(0, 0);
        //            }


        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        DisplayError($"<message>Failed with the message: {ex.Message} </message>");
        //    }
        //}

    }

    public static class TreeViewExtensions
    {
        public static void SetExpansion(this TreeView treeView, bool isExpanded) =>
          SetExpansion((ItemsControl)treeView, isExpanded);

        static void SetExpansion(ItemsControl parent, bool isExpanded)
        {
            if (parent is TreeViewItem tvi)
                tvi.IsExpanded = isExpanded;
            if (parent != null)
            {
                if (parent.HasItems)
                    foreach (var item in parent.Items.Cast<object>()
                  .Select(i => GetTreeViewItem(parent, i, isExpanded)))
                        SetExpansion(item, isExpanded);
            }
        }
        static TreeViewItem GetTreeViewItem(
          ItemsControl parent, object item, bool isExpanded)
        {
            if (item is TreeViewItem tvi)
                return tvi;

            var result = ContainerFromItem(parent, item);
            if (result == null && isExpanded)
            {
                parent.UpdateLayout();
                result = ContainerFromItem(parent, item);
            }
            return result;
        }

        static TreeViewItem ContainerFromItem(ItemsControl parent, object item) =>
          (TreeViewItem)parent.ItemContainerGenerator.ContainerFromItem(item);
    }


    public class ColorizeAvalonEdit : DocumentColorizingTransformer
    {
        private readonly string _selectedText;

        public ColorizeAvalonEdit(string selectedText)
        {
            _selectedText = selectedText;
        }
        int start = 0;
        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            
            int index;

            if (_selectedText.Trim().Length != 0)
            {

                while ((index = text.IndexOf(_selectedText, start, StringComparison.Ordinal)) >= 0)
                {
                    base.ChangeLinePart(
                        lineStartOffset + index, // startOffset
                        lineStartOffset + index+5 , // endOffset
                        (VisualLineElement element) =>
                        {
                            // This lambda gets called once for every VisualLineElement
                            // between the specified offsets.
                            Typeface tf = element.TextRunProperties.Typeface;
                            // Replace the typeface with a modified version of
                            // the same typeface
                            element.TextRunProperties.SetTypeface(new Typeface(
                                    tf.FontFamily,
                                    FontStyles.Italic,
                                    FontWeights.Bold,
                                    tf.Stretch
                                ));
                        });
                    start = index + 1; // search for next occurrence
                }
            }
        }
    }
    public class MarkSameWord : DocumentColorizingTransformer
    {
        private readonly string _selectedText;

        public MarkSameWord(string selectedText)
        {
            _selectedText = selectedText;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (string.IsNullOrEmpty(_selectedText))
            {
                return;
            }

            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            int start = 0;
            int index;

            while ((index = text.IndexOf(_selectedText, start, StringComparison.Ordinal)) >= 0)
            {
                ChangeLinePart(
                  lineStartOffset + index, // startOffset
                  lineStartOffset + index + _selectedText.Length, // endOffset
                  element => element.TextRunProperties.SetBackgroundBrush(Brushes.LightSkyBlue));
                start = index + 1; // search for next occurrence
            }
        }
    }
}
