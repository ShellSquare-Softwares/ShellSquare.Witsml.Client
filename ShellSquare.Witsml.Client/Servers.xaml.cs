using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace ShellSquare.Witsml.Client
{
    /// <summary>
    /// Interaction logic for Servers.xaml
    /// </summary>
    public partial class Servers : Window
    {

        public Action<string, string, string> ServerSelected;
        private readonly ObservableCollection<ServerInfo> m_serverInfos;

        public Servers()
        {
            InitializeComponent();
            m_serverInfos = new ObservableCollection<ServerInfo>();
            ServesGrid.ItemsSource = m_serverInfos;
        }


        public void Load()
        {
            string serversJson = Properties.Settings.Default.Servers;
            m_serverInfos.Clear();

            if (string.IsNullOrWhiteSpace(serversJson) == false)
            {
                var servers = JsonConvert.DeserializeObject<List<ServerInfo>>(serversJson);
                foreach (var s in servers)
                {
                    m_serverInfos.Add(s);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (ServerInfo s in ServesGrid.SelectedItems)
            {
                ServerSelected.Invoke(s.Url, s.UserName, s.Password);
                break;
            }

            string json = JsonConvert.SerializeObject(m_serverInfos);
            Properties.Settings.Default.Servers = json;
            Properties.Settings.Default.Save();
            this.Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selecteditem = ServesGrid.SelectedItem;
            if (selecteditem != null)
            {
                m_serverInfos.RemoveAt(ServesGrid.SelectedIndex);
            }
        }
    }
}
