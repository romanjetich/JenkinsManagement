using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JenkinsManager.Options
{
    public class OptionPage : DialogPage
    {
        private string host = "localhost";
        private string key = "";
        private string user = "admin";
        private int port = 8080;

        [Category("Jenkins Settings")]
        [DisplayName("Host")]
        [Description("Host")]
        public string Host
        {
            get { return host; }
            set { host = value; }
        }

        [Category("Jenkins Settings")]
        [DisplayName("Port")]
        [Description("Port")]
        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        [Category("Jenkins Settings")]
        [DisplayName("User")]
        [Description("User name")]
        public string User
        {
            get { return user; }
            set { user = value; }
        }

        [Category("Jenkins Settings")]
        [DisplayName("Key")]
        [Description("Authentication Key")]
        public string Key
        {
            get { return key; }
            set { key = value; }
        }
    }
}
