using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;

namespace ShellSquare.Witsml.Client
{
    /// <summary>
    /// Interaction logic for WITSMLControl.xaml
    /// </summary>
    public partial class WITSMLControl : UserControl
    {
        private string m_Template;
        private string m_SubTemplate;
        private Dictionary<string, string> m_KeyData = new Dictionary<string, string>();
        private XDocument m_Configuration = new XDocument();
        private List<string> m_TemplateList = new List<string>();
        private string m_ConfigFilePath;
        private string m_FilePath;
        private Timer m_XMLDelayTimer = new Timer();
        private Timer m_GridDelayTimer = new Timer();
        private bool m_InitialLoad = true;
        private bool m_FromkeyPress = false;
        private OptionSelection M_optionSelection;
        private TabHeaderControl m_TabHeader;

        public WITSMLControl(TabHeaderControl tabHeader)
        {
            m_TabHeader = tabHeader;
            InitializeComponent();
            LoadPreference();
            m_ConfigFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            m_FilePath = m_ConfigFilePath;
            BindDictionary();
            LoadTemplateList();
            m_XMLDelayTimer.Interval = 5000;
            m_XMLDelayTimer.Elapsed += OnTextChanged;

            m_GridDelayTimer.Interval = 3000;
            m_GridDelayTimer.Elapsed += OnGridChanged;
        }

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

                treeView.Items.Clear();

                if (EnableTree.IsChecked.Value)
                {
                    XDocument doc = XDocument.Parse(capabilities);
                    TreeViewItem treeNode = new TreeViewItem
                    {
                        //Should be Root
                        Header = doc.Root.Name.LocalName,
                        IsExpanded = true,
                        Tag = 0
                    };
                    treeView.Items.Add(treeNode);
                    BuildNodes(treeNode, doc.Root, true);
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
            treeView.Items.Clear();

            TreeViewItem treeNode = new TreeViewItem
            {
                Header = message,
                IsExpanded = true,
                Foreground = Brushes.Red
            };
            treeView.Items.Add(treeNode);

        }

        private void DisplayMessage(string message)
        {
            responceEditor.Text = message;
            treeView.Items.Clear();

            TreeViewItem treeNode = new TreeViewItem
            {
                Header = message,
                IsExpanded = true,
                Foreground = Brushes.Blue
            };
            treeView.Items.Add(treeNode);
        }

        private void BuildNodes(TreeViewItem treeNode, XElement element, bool expand)
        {
            expand = true;
            foreach (XNode child in element.Nodes())
            {
                switch (child.NodeType)
                {
                    case XmlNodeType.Element:
                        XElement childElement = child as XElement;


                        string attributes = "";
                        if (childElement.HasAttributes)
                        {
                            foreach (var att in childElement.Attributes())
                            {
                                attributes += " " + att.Name + "=\"" + att.Value + "\"";
                            }
                        }


                        string value;

                        if (childElement.HasElements)
                        {
                            value = "";
                        }
                        else
                        {
                            value = childElement.Value;
                        }

                        var level = (int)treeNode.Tag;

                        TreeViewItemHeader header = new TreeViewItemHeader();
                        header.HeaderText = childElement.Name.LocalName;
                        header.ValueText = value;
                        header.AttributeText = attributes;

                        TreeViewItem childTreeNode = new TreeViewItem
                        {
                            //Get First attribute where it is equal to value
                            Header = header,
                            //Automatically expand elements
                            IsExpanded = expand,
                            Tag = level + 1
                        };


                        if (childElement.Name.LocalName.ToLower() == "logdata")
                        {
                            logDataGrid.Visibility = Visibility.Visible;
                            Grid.SetRowSpan(treeView, 1);

                            DataTable dt = new DataTable();
                            DisplayLogdata(childTreeNode, childElement, ref dt);
                            logDataGrid.ItemsSource = new DataView(dt);
                        }
                        else
                        {
                            treeNode.Items.Add(childTreeNode);
                            BuildNodes(childTreeNode, childElement, false);
                        }
                        break;
                    case XmlNodeType.Text:
                        //XText childText = child as XText;
                        //treeNode.Items.Add(new TreeViewItem { Header = childText.Value, });
                        break;
                }
            }
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

        private void Templates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadTemplate();
        }

        private void LoadTemplate()
        {
            try
            {
                m_InitialLoad = true;

                if (requestGrid.ItemsSource != null)
                {
                    requestGrid.ItemsSource = null;
                }
                var selectedValue = (string)Templates.SelectedValue;
                m_Template = selectedValue;
                m_SubTemplate = "All";
                LoadTemplatesXML(m_Template, m_SubTemplate);

            }
            catch (Exception ex)
            {
                DisplayError($"Failed with the message: {ex.Message}");
            }
        }

        private void LoadTemplateList()
        {
            string templateInputDataConfigFilePath = Path.Combine(m_ConfigFilePath, Common.TEMPLATE_INPUT_CONFIG_PATH);
            m_Configuration = XDocument.Load(templateInputDataConfigFilePath);

            m_TemplateList = TemplateData("Name");
            Templates.ItemsSource = m_TemplateList;
        }

        private List<string> TemplateData(string bindedTemplateData)
        {
            var controlDetails = new List<string>();
            foreach (var element in m_Configuration.Descendants(bindedTemplateData))
            {
                if (bindedTemplateData == "Name")
                {
                    m_TemplateList.Add(element.Value);
                }
                controlDetails.Add(element.Value);
            }
            return controlDetails;
        }

        private void CommonData(XmlDocument xmlRequestEditor)
        {
            string outerXmlData = xmlRequestEditor.OuterXml;
            string prettifyOuterXmlData = PrettifyXML(outerXmlData);
            requestEditor.Text = prettifyOuterXmlData;
        }

        private void BindDictionary()
        {
            m_KeyData.Add("Well$Simple", m_FilePath + Common.WELL_SIMPLE_PATH);
            m_KeyData.Add("Well$Ids", m_FilePath + Common.WELL_IDS_PATH);
            m_KeyData.Add("Well$All", m_FilePath + Common.WELL_ALL_PATH);
            m_KeyData.Add("Wellbore$Simple", m_FilePath + Common.WELLBORE_SIMPLE_PATH);
            m_KeyData.Add("Wellbore$Ids", m_FilePath + Common.WELLBORE_IDS_PATH);
            m_KeyData.Add("Wellbore$All", m_FilePath + Common.WELLBORE_ALL_PATH);
            m_KeyData.Add("Log$Simple", m_FilePath + Common.LOG_SIMPLE_PATH);
            m_KeyData.Add("Log$Ids", m_FilePath + Common.LOG_IDS_PATH);
            m_KeyData.Add("Log$All", m_FilePath + Common.LOG_ALL_PATH);
            m_KeyData.Add("attachment$Simple", m_FilePath + Common.ATTACHMENT_SIMPLE_PATH);
            m_KeyData.Add("attachment$Ids", m_FilePath + Common.ATTACHMENT_IDS_PATH);
            m_KeyData.Add("attachment$All", m_FilePath + Common.ATTACHMENT_ALL_PATH);
            m_KeyData.Add("bhaRun$Simple", m_FilePath + Common.BHARUN_SIMPLE_PATH);
            m_KeyData.Add("bhaRun$Ids", m_FilePath + Common.BHARUN_IDS_PATH);
            m_KeyData.Add("bhaRun$All", m_FilePath + Common.BHARUN_ALL_PATH);
            m_KeyData.Add("cementJob$Simple", m_FilePath + Common.CEMENTJOB_SIMPLE_PATH);
            m_KeyData.Add("cementJob$Ids", m_FilePath + Common.CEMENTJOB_IDS_PATH);
            m_KeyData.Add("cementJob$All", m_FilePath + Common.CEMENTJOB_ALL_PATH);
            m_KeyData.Add("changeLog$Simple", m_FilePath + Common.CHANGELOG_SIMPLE_PATH);
            m_KeyData.Add("changeLog$Ids", m_FilePath + Common.CHANGELOG_IDS_PATH);
            m_KeyData.Add("changeLog$All", m_FilePath + Common.CHANGELOG_ALL_PATH);
            m_KeyData.Add("convCore$Simple", m_FilePath + Common.CONVCORE_SIMPLE_PATH);
            m_KeyData.Add("convCore$Ids", m_FilePath + Common.CONVCORE_IDS_PATH);
            m_KeyData.Add("convCore$All", m_FilePath + Common.CONVCORE_ALL_PATH);
            m_KeyData.Add("coordinateReference$Simple", m_FilePath + Common.COORDINATEREFERENCE_SIMPLE_PATH);
            m_KeyData.Add("coordinateReference$Ids", m_FilePath + Common.COORDINATEREFERENCE_IDS_PATH);
            m_KeyData.Add("coordinateReference$All", m_FilePath + Common.COORDINATEREFERENCE_ALL_PATH);
            m_KeyData.Add("customObject$Simple", m_FilePath + Common.CUSTOMOBJECT_SIMPLE_PATH);
            m_KeyData.Add("customObject$Ids", m_FilePath + Common.CUSTOMOBJECT_IDS_PATH);
            m_KeyData.Add("customObject$All", m_FilePath + Common.CUSTOMOBJECT_ALL_PATH);
            m_KeyData.Add("drillReport$Simple", m_FilePath + Common.DRILLREPORT_SIMPLE_PATH);
            m_KeyData.Add("drillReport$Ids", m_FilePath + Common.DRILLREPORT_IDS_PATH);
            m_KeyData.Add("drillReport$All", m_FilePath + Common.DRILLREPORT_ALL_PATH);
            m_KeyData.Add("fluidsReport$Simple", m_FilePath + Common.FLUIDSREPORT_SIMPLE_PATH);
            m_KeyData.Add("fluidsReport$Ids", m_FilePath + Common.FLUIDSREPORT_IDS_PATH);
            m_KeyData.Add("fluidsReport$All", m_FilePath + Common.FLUIDSREPORT_ALL_PATH);
            m_KeyData.Add("formationMarker$Simple", m_FilePath + Common.FORMATIONMARKER_SIMPLE_PATH);
            m_KeyData.Add("formationMarker$Ids", m_FilePath + Common.FORMATIONMARKER_IDS_PATH);
            m_KeyData.Add("formationMarker$All", m_FilePath + Common.FORMATIONMARKER_ALL_PATH);
            m_KeyData.Add("message$Simple", m_FilePath + Common.MESSAGE_SIMPLE_PATH);
            m_KeyData.Add("message$Ids", m_FilePath + Common.MESSAGE_IDS_PATH);
            m_KeyData.Add("message$All", m_FilePath + Common.MESSAGE_ALL_PATH);
            m_KeyData.Add("mnemonicSet$Simple", m_FilePath + Common.MNEMONICSET_SIMPLE_PATH);
            m_KeyData.Add("mnemonicSet$Ids", m_FilePath + Common.MNEMONICSET_IDS_PATH);
            m_KeyData.Add("mnemonicSet$All", m_FilePath + Common.MNEMONICSET_ALL_PATH);
            m_KeyData.Add("mudLog$Simple", m_FilePath + Common.MUDLOG_SIMPLE_PATH);
            m_KeyData.Add("mudLog$Ids", m_FilePath + Common.MUDLOG_IDS_PATH);
            m_KeyData.Add("mudLog$All", m_FilePath + Common.MUDLOG_ALL_PATH);
            m_KeyData.Add("objectGroup$Simple", m_FilePath + Common.OBJECTGROUP_SIMPLE_PATH);
            m_KeyData.Add("objectGroup$Ids", m_FilePath + Common.OBJECTGROUP_IDS_PATH);
            m_KeyData.Add("objectGroup$All", m_FilePath + Common.OBJECTGROUP_ALL_PATH);
            m_KeyData.Add("opsReport$Simple", m_FilePath + Common.OPSREPORT_SIMPLE_PATH);
            m_KeyData.Add("opsReport$Ids", m_FilePath + Common.OPSREPORT_IDS_PATH);
            m_KeyData.Add("opsReport$All", m_FilePath + Common.OPSREPORT_ALL_PATH);
            m_KeyData.Add("pressureTestPlan$Simple", m_FilePath + Common.PRESSURETESTPLAN_SIMPLE_PATH);
            m_KeyData.Add("pressureTestPlan$Ids", m_FilePath + Common.PRESSURETESTPLAN_IDS_PATH);
            m_KeyData.Add("pressureTestPlan$All", m_FilePath + Common.PRESSURETESTPLAN_ALL_PATH);
            m_KeyData.Add("rig$Simple", m_FilePath + Common.RIG_SIMPLE_PATH);
            m_KeyData.Add("rig$Ids", m_FilePath + Common.RIG_IDS_PATH);
            m_KeyData.Add("rig$All", m_FilePath + Common.RIG_ALL_PATH);
            m_KeyData.Add("risk$Simple", m_FilePath + Common.RISK_SIMPLE_PATH);
            m_KeyData.Add("risk$Ids", m_FilePath + Common.RISK_IDS_PATH);
            m_KeyData.Add("risk$All", m_FilePath + Common.RISK_ALL_PATH);
            m_KeyData.Add("sidewallCore$Simple", m_FilePath + Common.SIDEWALLCORE_SIMPLE_PATH);
            m_KeyData.Add("sidewallCore$Ids", m_FilePath + Common.SIDEWALLCORE_IDS_PATH);
            m_KeyData.Add("sidewallCore$All", m_FilePath + Common.SIDEWALLCORE_ALL_PATH);
            m_KeyData.Add("stimJob$Simple", m_FilePath + Common.STIMJOB_SIMPLE_PATH);
            m_KeyData.Add("stimJob$Ids", m_FilePath + Common.STIMJOB_IDS_PATH);
            m_KeyData.Add("stimJob$All", m_FilePath + Common.STIMJOB_ALL_PATH);
            m_KeyData.Add("surveyProgram$Simple", m_FilePath + Common.SURVEYPROGRAM_SIMPLE_PATH);
            m_KeyData.Add("surveyProgram$Ids", m_FilePath + Common.SURVEYPROGRAM_IDS_PATH);
            m_KeyData.Add("surveyProgram$All", m_FilePath + Common.SURVEYPROGRAM_ALL_PATH);
            m_KeyData.Add("target$Simple", m_FilePath + Common.TARGET_SIMPLE_PATH);
            m_KeyData.Add("targetg$Ids", m_FilePath + Common.TARGET_IDS_PATH);
            m_KeyData.Add("target$All", m_FilePath + Common.TARGET_ALL_PATH);
            m_KeyData.Add("toolErrorModel$Simple", m_FilePath + Common.TOOLERRORMODEL_SIMPLE_PATH);
            m_KeyData.Add("toolErrorModel$Ids", m_FilePath + Common.TOOLERRORMODEL_IDS_PATH);
            m_KeyData.Add("toolErrorModel$All", m_FilePath + Common.TOOLERRORMODEL_ALL_PATH);
            m_KeyData.Add("toolErrorTermSet$Simple", m_FilePath + Common.TOOLERRORTERMSET_SIMPLE_PATH);
            m_KeyData.Add("toolErrorTermSet$Ids", m_FilePath + Common.TOOLERRORTERMSET_IDS_PATH);
            m_KeyData.Add("toolErrorTermSet$All", m_FilePath + Common.TOOLERRORTERMSET_ALL_PATH);
            m_KeyData.Add("trajectory$Simple", m_FilePath + Common.TRAJECTORY_SIMPLE_PATH);
            m_KeyData.Add("trajectory$Ids", m_FilePath + Common.TRAJECTORY_IDS_PATH);
            m_KeyData.Add("trajectory$All", m_FilePath + Common.TRAJECTORY_ALL_PATH);
            m_KeyData.Add("tubular$Simple", m_FilePath + Common.TUBULAR_SIMPLE_PATH);
            m_KeyData.Add("tubular$Ids", m_FilePath + Common.TUBULAR_IDS_PATH);
            m_KeyData.Add("tubular$All", m_FilePath + Common.TUBULAR_ALL_PATH);
            m_KeyData.Add("wbGeometry$Simple", m_FilePath + Common.WBGEOMETRY_SIMPLE_PATH);
            m_KeyData.Add("wbGeometry$Ids", m_FilePath + Common.WBGEOMETRY_IDS_PATH);
            m_KeyData.Add("wbGeometry$All", m_FilePath + Common.WBGEOMETRY_ALL_PATH);
        }

        private void LoadTemplatesXML(string template, string subtemplate)
        {
            XmlDocument document = new XmlDocument();
            try
            {
                string templatePath = string.Empty;
                string key = $"{template}${subtemplate}";
                if (m_KeyData.ContainsKey(key))
                {
                    templatePath = m_KeyData[key];
                    document.Load(templatePath);
                    CommonData(document);
                }
            }
            catch (Exception ex)
            {
                DisplayError($"Failed with the message: {ex.Message}");
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

        private void DelayXMLUpdation()
        {
            if (!m_XMLDelayTimer.Enabled)
            {
                m_XMLDelayTimer.Enabled = true;
            }
        }

        private void OnTextChanged(object sender, ElapsedEventArgs e)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    m_XMLDelayTimer.Enabled = false;
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

                    if (requestGrid.ItemsSource == null)
                    {
                        requestGrid.ItemsSource = new DataView(modifiedDataTable);
                    }
                    else
                    {
                        if (!m_GridDelayTimer.Enabled && m_FromkeyPress)
                        {
                            DataTable requestGridDataTable = new DataTable();
                            requestGridDataTable = ((DataView)requestGrid.ItemsSource).ToTable();

                            DataTable updatedDataTable = CompareAndUpdateDataTable(requestGridDataTable, modifiedDataTable);
                            requestGrid.ItemsSource = new DataView(updatedDataTable);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
            }
        }

        private void OnRequestEditorTextChanged(object sender, EventArgs e)
        {
            m_FromkeyPress = requestEditor.IsKeyboardFocusWithin;
            if (string.IsNullOrEmpty(requestEditor.Text))
            {
                LoadForEmptyXML();
            }
            if (m_InitialLoad)
            {
                m_InitialLoad = false;
                OnTextChanged(null, null);
            }
            else
            {
                if (!m_GridDelayTimer.Enabled)
                {
                    DelayXMLUpdation();
                }
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

        public void LoadForEmptyXML()
        {
            DataTable requestGridDataTable = new DataTable();
            requestGridDataTable = ((DataView)requestGrid.ItemsSource).ToTable();

            for (int i = 0; i < requestGridDataTable.Rows.Count; i++)
            {
                requestGridDataTable.Rows[i][0] = false;
                requestGridDataTable.Rows[i][2] = string.Empty;
            }
            requestGrid.ItemsSource = new DataView(requestGridDataTable);
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

        private void CreateXML(DataTable requestGridDataTable, XDocument xRequestEditorDocument)
        {
            try
            {
                Dictionary<int, string> storedXMLDictionary = new Dictionary<int, string>();
                xRequestEditorDocument.Root.RemoveNodes();
                string header = xRequestEditorDocument.ToString();
                string headerStarting = xRequestEditorDocument.Root.Document.Declaration + header.Substring(0, header.Length - 2) + ">";
                string headerEnding = "</" + xRequestEditorDocument.Root.Name.LocalName + ">";
                string currentstr = "";
                string nextLevelStr = "";
                string attributes = "";
                int nextType = Convert.ToInt32(requestGridDataTable.Rows[requestGridDataTable.Rows.Count - 1][3]);
                int nxtLevel = Convert.ToInt32(requestGridDataTable.Rows[requestGridDataTable.Rows.Count - 1][4]);
                for (int i = requestGridDataTable.Rows.Count - 1; i >= 0; i--)
                {
                    bool isSelected = Convert.ToBoolean(requestGridDataTable.Rows[i][0]);
                    string name = Convert.ToString(requestGridDataTable.Rows[i][1]).Trim();
                    string value = Convert.ToString(requestGridDataTable.Rows[i][2]).Trim();
                    int currentType = Convert.ToInt32(requestGridDataTable.Rows[i][3]);
                    int currentLevel = Convert.ToInt32(requestGridDataTable.Rows[i][4]);

                    if (!isSelected)
                    {
                        for (int j = i + 1; j < requestGridDataTable.Rows.Count; j++)
                        {
                            if (Convert.ToInt32(requestGridDataTable.Rows[i][3]) == 2)
                            {
                                break;
                            }
                            if ((Convert.ToInt32(requestGridDataTable.Rows[j][4]) > currentLevel) || (Convert.ToInt32(requestGridDataTable.Rows[j][4]) == currentLevel && Convert.ToInt32(requestGridDataTable.Rows[j][3]) == 2))
                            {
                                if (Convert.ToBoolean(requestGridDataTable.Rows[j][0]))
                                {
                                    isSelected = true;
                                    requestGridDataTable.Rows[i][0] = true;
                                    break;
                                }
                            }
                            else if (Convert.ToInt32(requestGridDataTable.Rows[j][4]) == currentLevel)
                            {
                                break;
                            }
                        }
                    }

                    if (isSelected)
                    {
                        if (currentType != 2)
                        {
                            if (nxtLevel == currentLevel)
                            {
                                currentstr = "<" + name + (nextType == 2 ? attributes : "") + (string.IsNullOrEmpty(value) ? "/>" : ">") + (string.IsNullOrEmpty(value) ? "" : value + "</" + name + ">") + currentstr;
                            }
                            else if (nxtLevel > currentLevel)
                            {
                                string storedData = string.Empty;
                                if (storedXMLDictionary.ContainsKey(currentLevel))
                                {
                                    storedData = storedXMLDictionary[currentLevel];
                                    storedXMLDictionary.Remove(currentLevel);
                                }
                                currentstr = "<" + name + (nextType == 2 ? attributes : "") + ">" + currentstr + "</" + name + ">" + storedData;
                                nextLevelStr = "";
                            }
                            else if (nxtLevel < currentLevel)
                            {
                                nextLevelStr = currentstr + nextLevelStr;
                                if (storedXMLDictionary.ContainsKey(currentLevel))
                                {
                                    string storedData = storedXMLDictionary[currentLevel];
                                    storedData = nextLevelStr + storedData;
                                    storedXMLDictionary[currentLevel] = storedData;
                                }
                                else
                                {
                                    storedXMLDictionary.Add(nxtLevel, nextLevelStr);
                                }
                                nextLevelStr = "";
                                currentstr = "<" + name + (nextType == 2 ? attributes : "") + (string.IsNullOrEmpty(value) ? "/>" : ">") + (string.IsNullOrEmpty(value) ? "" : value + "</" + name + ">");
                            }
                            attributes = "";
                        }

                        if (currentType != 2)
                            nxtLevel = currentLevel;

                        nextType = currentType;
                        if (currentType == 2)
                        {
                            attributes = " " + name + "=\"" + value + "\"" + attributes;
                        }
                    }
                }
                currentstr = headerStarting + currentstr + headerEnding;

                //requestGrid.ItemsSource = new DataView(requestGridDataTable);
                XmlDocument xmlRequestEditor = new XmlDocument();
                xmlRequestEditor.LoadXml(currentstr);
                CommonData(xmlRequestEditor);
            }
            catch (Exception ex)
            {
            }
        }
        private void OnGridChanged(object sender, ElapsedEventArgs e)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    DisplayRequest();
                });
            }
            catch (Exception ex)
            {
            }
        }

        private void DisplayRequest()
        {
            m_GridDelayTimer.Enabled = false;
            XDocument xRequestEditorDocument = XDocument.Parse(requestEditor.Text);
            DataTable requestGridDataTable = new DataTable();
            requestGridDataTable = ((DataView)requestGrid.ItemsSource).ToTable();
            CreateXML(requestGridDataTable, xRequestEditorDocument);
        }

        private void DelayGridUpdation()
        {
            if (!m_GridDelayTimer.Enabled)
            {
                m_GridDelayTimer.Enabled = true;
            }
        }

        private void LoadEmptyTemplatesXML()
        {
            XmlDocument document = new XmlDocument();
            try
            {
                string templatePath = string.Empty;
                string key = $"{Templates.SelectedValue}${m_SubTemplate}";
                if (m_KeyData.ContainsKey(key))
                {
                    templatePath = m_KeyData[key];
                    document.Load(templatePath);
                    XmlNode root = document.DocumentElement;
                    root.RemoveAll();
                    CommonData(document);
                }
            }
            catch (Exception ex)
            {
                DisplayError($"<message>Failed with the message: {ex.Message} </message>");
            }
        }

        private Element GetRow(int currentrowIndex, Element element, out int count)
        {
            count = 0;
            Element previousElement = null; ;
            for (int i = currentrowIndex; i < requestGrid.Items.Count; i++)
            {
                count = count + 1;
                DataRow row = ((DataRowView)requestGrid.Items[i]).Row;

                int type = (int)row.ItemArray[3];
                int level = (int)row.ItemArray[4];

                if (level == element.Level + 1)
                {
                    if (type == 2)
                    {
                        //element.Attributes.Add(row.ItemArray[1].ToString(), row.ItemArray[2].ToString());
                    }
                    else
                    {
                        Element el = new Element();
                        el.Name = row.ItemArray[1].ToString();
                        el.Value = row.ItemArray[2].ToString();
                        el.Level = level;
                        element.Children.Add(el);
                        previousElement = el;
                    }
                }
                else if (level == element.Level + 2)
                {
                    GetRow(level + 1, previousElement, out count);
                    i = i + count + 1;
                }
            }
            return element;

        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (responceEditor.Visibility == Visibility.Visible)
                {
                    responceEditor.TextArea.TextView.LineTransformers.Clear();
                    responceEditor.TextArea.TextView.LineTransformers.Add(new ColorizeAvalonEdit(Search.Text));
                }
            }
            catch (Exception ex)
            {
                DisplayError($"Failed with the message: {ex.Message}");
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
                treeView.Items.Clear();
                if (EnableTree.IsChecked.Value)
                {
                    if (doc.Descendants().Count() > 15000)
                    {

                        responceEditor.Visibility = Visibility.Visible;
                        treeEditor.Visibility = Visibility.Collapsed;
                        TreeViewItem treeNode = new TreeViewItem
                        {
                            //Should be Root
                            Header = "Output has more than 15,000 items",
                            IsExpanded = true
                        };
                        treeView.Items.Add(treeNode);
                    }
                    else
                    {
                        logDataGrid.Visibility = Visibility.Collapsed;
                        Grid.SetRowSpan(treeView, 2);
                        logDataGrid.ItemsSource = null;

                        TreeViewItem treeNode = new TreeViewItem
                        {
                            //Should be Root
                            Header = doc.Root.Name.LocalName,
                            IsExpanded = true,
                            Tag = 0

                        };
                        treeView.Items.Add(treeNode);
                        BuildNodes(treeNode, doc.Root, true);
                    }
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

        private void requestGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;
            if (cell == null || cell.IsEditing || cell.IsReadOnly)
            {
                return;
            }

            if (!cell.IsFocused)
            {
                cell.Focus();
            }
            requestGrid.BeginEdit(e);

            cell.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
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

        private void RequestGridSelect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (requestGrid.IsKeyboardFocusWithin)
                {
                    if (string.IsNullOrEmpty(requestEditor.Text))
                    {
                        LoadEmptyTemplatesXML();
                    }
                    var items = requestGrid.SelectedCells;
                    foreach (var item in items)
                    {
                        ((DataRowView)item.Item).Row[0] = true;
                    }

                    DelayGridUpdation();
                }
            }
            catch { }
        }

        private void OnValueTextChanged(object sender, EventArgs eventArg)
        {
            try
            {
                if (requestGrid.IsKeyboardFocusWithin)
                {
                    var valueTextBox = ((TextBox)sender).Text;
                    if (!string.IsNullOrEmpty(valueTextBox))
                    {
                        var items = requestGrid.SelectedCells;
                        foreach (var item in items)
                        {
                            ((DataRowView)item.Item).Row[2] = valueTextBox;
                            ((DataRowView)item.Item).Row[0] = true;
                        }

                        DelayGridUpdation();
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void RequestGridDeselect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (requestGrid.IsKeyboardFocusWithin)
                {
                    var items = requestGrid.SelectedCells;
                    foreach (var item in items)
                    {
                        ((DataRowView)item.Item).Row[0] = false;
                    }

                    DelayGridUpdation();
                }
            }
            catch { }
        }

        private void RequestGridSelectAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (DataRowView item in requestGrid.Items)
                {
                    item.Row[0] = true;
                }

                DisplayRequest();
            }
            catch { }
        }

        private void RequestGridDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (DataRowView item in requestGrid.Items)
                {
                    item.Row[0] = false;
                }

                DisplayRequest();
            }
            catch { }
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

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            LoadTemplate();
        }


        private void XmlToggle_Click(object sender, RoutedEventArgs e)
        {
            if(XmlToggle.IsChecked == false)
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


        public void SelectText(int offset, int length)
        {
            //Get the line number based off the offset.
            var line = responceEditor.Document.GetLineByOffset(offset);
            var lineNumber = line.LineNumber;

            //Select the text.
            responceEditor.SelectionStart = offset;
            responceEditor.SelectionLength = length;

            //Scroll the textEditor to the selected line.
            var visualTop = responceEditor.TextArea.TextView.GetVisualTopByDocumentLine(lineNumber);
            responceEditor.ScrollToVerticalOffset(visualTop);
        }

        private void Search_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            SelectText(10, 20);
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
    }
}
