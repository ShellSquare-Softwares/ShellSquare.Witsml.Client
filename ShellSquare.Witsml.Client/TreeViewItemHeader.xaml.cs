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
    /// Interaction logic for TreeViewItemHeader.xaml
    /// </summary>
    public partial class TreeViewItemHeader : UserControl
    {
        public TreeViewItemHeader()
        {
            InitializeComponent();
        }



        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HeaderText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register("HeaderText", typeof(string), typeof(TreeViewItemHeader), new PropertyMetadata(""));




        public string AttributeText
        {
            get { return (string)GetValue(AttributeTextProperty); }
            set { SetValue(AttributeTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AttributeText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AttributeTextProperty =
            DependencyProperty.Register("AttributeText", typeof(string), typeof(TreeViewItemHeader), new PropertyMetadata(""));




        public string ValueText
        {
            get { return (string)GetValue(ValueTextProperty); }
            set { SetValue(ValueTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValueText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueTextProperty =
            DependencyProperty.Register("ValueText", typeof(string), typeof(TreeViewItemHeader), new PropertyMetadata(""));



        private void TextBox_SelectAll(object sender, MouseButtonEventArgs e)
        {
            TextBox tb = (sender as TextBox);
            if (tb != null)
            {
                tb.SelectAll();
            }
        }

        private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox tb = (sender as TextBox);
            if (tb != null)
            {
                tb.SelectAll();
            }
        }

        private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBox tb = (sender as TextBox);
            if (tb != null)
            {
                if (!tb.IsKeyboardFocusWithin)
                {
                    e.Handled = true;
                    tb.Focus();
                }
            }
        }
    }
}
