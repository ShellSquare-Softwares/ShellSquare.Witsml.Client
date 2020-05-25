using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ShellSquare.Witsml.Client
{
    /// <summary>
    /// Interaction logic for TreeViewItemGridRow.xaml
    /// </summary>
    public partial class TreeViewItemGridRow : UserControl
    {
        public TreeViewItemGridRow()
        {
            InitializeComponent();
        }


        public void Clear()
        {
            treeviewdatagrid.Items.Clear();
            treeviewdatagrid.Columns.Clear();
        }

        public void SetHeaders(List<string> headers)
        {
            foreach (var header in headers)
            {
                DataGridTextColumn textColumn = new DataGridTextColumn();
                textColumn.Header = header;
                treeviewdatagrid.Columns.Add(textColumn);
            }
          
        }

        public void SerData(List<List<string>> dataRows)
        {
            foreach (var row in dataRows)
            {
                foreach (var item in row)
                {
                    treeviewdatagrid.Items.Add(item);
                }
            }
            
        }
    }
}
