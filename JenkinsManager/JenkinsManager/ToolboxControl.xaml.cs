using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace JenkinsManager
{
    /// <summary>
    /// Interaction logic for ToolboxControl.xaml.
    /// </summary>
    [ProvideToolboxControl("JenkinsManager.ToolboxControl", true)]
    public partial class ToolboxControl : UserControl
    {
        public ToolboxControl()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(string.Format(CultureInfo.CurrentUICulture, "We are inside {0}.Button1_Click()", this.ToString()));
        }
    }
}
