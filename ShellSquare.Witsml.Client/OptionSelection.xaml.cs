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
using System.Windows.Shapes;

namespace ShellSquare.Witsml.Client
{
    /// <summary>
    /// Interaction logic for OptionSelection.xaml
    /// </summary>
    public partial class OptionSelection : Window
    {
        public Action<string> OptionsSelected;
        public OptionSelection()
        {
            InitializeComponent();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            string options = string.Empty;

            if (DataVersion.IsChecked == true)
            {
                options = $"dataVersion={DataversionSelection.Text};"; 
            }

            if(ReturnElement.IsChecked == true)
            {
                options += $"returnElements={ReturnElementSelection.Text};";
            }

            OptionsSelected(options);
            this.Close();
        }
    }
}
