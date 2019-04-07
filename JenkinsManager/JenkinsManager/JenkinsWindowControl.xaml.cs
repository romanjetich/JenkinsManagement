namespace JenkinsManager
{
    using JenkinsNET;
    using JenkinsNET.Models;
    using JenkinsNET.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Xml.Linq;

    /// <summary>
    /// Interaction logic for JenkinsWindowControl.
    /// </summary>
    public partial class JenkinsWindowControl : UserControl
    {
        JenkinsClient jenkinsClient = null;
        JenkinsJobRunner jenkinsJobRunner = null;
        Dictionary<string, List<ParameterFields>> jobData = new Dictionary<string, List<ParameterFields>>();
        List<ParameterFields> fields;
        /// <summary>
        /// Initializes a new instance of the <see cref="JenkinsWindowControl"/> class.
        /// </summary>
        public JenkinsWindowControl()
        {
            this.InitializeComponent();
            jenkinsClient = new JenkinsClient
            {
                BaseUrl = "http://localhost:8080/",
                UserName = "admin",
                ApiToken = "118a716bc175f3a101c1e063e299d073a4",
            };
            var jenkins = jenkinsClient.Get();
            this.jobs.SelectionChanged += Jobs_SelectionChanged;
            this.ParamName.SelectionChanged += ParamName_SelectionChanged;

            foreach (var job in jenkins.Jobs)
            {
                fields = new List<ParameterFields>();
                this.jobs.Items.Add(job.Name);
                JenkinsJobBase jenkinsJob = jenkinsClient.Jobs.Get<JenkinsJobBase>(job.Name) as JenkinsJobBase;
                foreach (var action in jenkinsJob.Actions)
                {
                    FindAllNodes(action.Node);
                }
                jobData[job.Name] = fields;
            }
            if(this.jobs.Items.Count > 0)
            {
                this.jobs.SelectedIndex = 0;
                string firstJob = this.jobs.SelectedValue.ToString();
                if (jobData[firstJob].Count > 0)
                {
                    foreach (var param in jobData[firstJob])
                    {
                        this.ParamName.Items.Add(param.Name);
                    }

                    this.ParamName.SelectedIndex = 0;
                    this.ParamValue.Text = jobData[firstJob][0].DefaultValue;
                }
                else
                {
                    this.ParamName.IsEnabled = false;
                    this.ParamValue.IsEnabled = false;
                    this.btnAddParam.IsEnabled = false;
                }
            }
        }

        private void ParamName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).Items.Count == 0) return;
            var jobName = jobs.SelectedValue.ToString();
            var paramName = ((ComboBox)sender).SelectedValue.ToString();
            foreach (var param in jobData[jobName])
            {
                if(param.Name == paramName)
                {
                    ParamValue.Text = param.DefaultValue;
                }
            }
        }

        private void Jobs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var jobName = ((ComboBox)sender).SelectedValue.ToString();
            this.ParamName.Items.Clear();
            this.ParamName.Text = "";
            this.ParamValue.Text = "";

            if (jobData[jobName].Count > 0)
            {
                foreach (var param in jobData[jobName])
                {
                    this.ParamName.Items.Add(param.Name);
                }

                this.ParamName.SelectedIndex = 0;
                this.ParamValue.Text = jobData[jobName][0].DefaultValue;
            }
            else
            {
                this.ParamName.IsEnabled = false;
                this.ParamValue.IsEnabled = false;
                this.btnAddParam.IsEnabled = false;
            }
        }

        private void FindAllNodes(XNode node)
        {
            if (node == null) return;
            //Console.WriteLine(node.);
            if (node.NodeType == System.Xml.XmlNodeType.Element)
            {
                var element = node as XElement;
                if (element == null) return;
                var firstNode = element.FirstNode as XElement;
                if (firstNode == null) {
                    FindAllNodes(element.NextNode);
                    return;
                }

                if (firstNode.Name.LocalName == "parameterDefinition")
                {
                    ParameterFields field = new ParameterFields();
                    FindAllNodes(firstNode, ref field);
                }
                else
                {
                    FindAllNodes(element.NextNode);
                }                  
            }            
        }

        private void FindAllNodes(XNode node, ref ParameterFields field)
        {
            if (node == null) return;

            var element = node as XElement;
            var firstNode = element?.FirstNode as XElement;
            if (firstNode == null) return;

            var value = firstNode?.Value;
            if (firstNode.Name.LocalName.ToLowerInvariant().Contains("name"))
            {
                field.Name = value;
            }
            else if (firstNode.Name.LocalName.ToLowerInvariant().Contains("value"))
            {
                if (field.DefaultValue == value)
                {
                    field.FieldType = element.FirstAttribute.Value.ToString();
                    FillField(element.NextNode, ref field);
                    FillField(element.NextNode.NextNode, ref field);
                    fields.Add(field);
                    field = new ParameterFields();
                    FindAllNodes(element.Parent.NextNode, ref field);
                    return;
                }
                else
                {
                    field.DefaultValue = value;
                }

            }
            
            FindAllNodes(firstNode, ref field);
            //FindAllNodes(element.NextNode, ref field);

        }

        private void FillField(XNode node, ref ParameterFields field)
        {
            if (node == null) return;

            var element = node as XElement;
            if (element.Name.LocalName.ToLowerInvariant().Contains("name"))
            {
                field.Name = element.Value;
            }
            else if (element.Name.LocalName.ToLowerInvariant().Contains("value"))
            {
                field.DefaultValue = element.Value;
            }
            else if (element.Name.LocalName.ToLowerInvariant().Contains("description"))
            {
                field.Description = element.Value;
            }
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private async void button1_Click(object sender, RoutedEventArgs e)
        {
            jenkinsJobRunner = new JenkinsJobRunner(jenkinsClient) {
                BuildTimeout = 60 * 60
            };

            jenkinsJobRunner.StatusChanged += JenkinsJobRunner_StatusChanged;
            jobInfos.Items.Add($"{DateTime.Now.ToLongTimeString()}: start");
            var result = await jenkinsJobRunner.RunAsync(this.jobs.SelectedValue.ToString());
            jobInfos.Items.Add($"{DateTime.Now.ToLongTimeString()}: Duration {result.Duration} ms");
            jobInfos.Items.Add($"{DateTime.Now.ToLongTimeString()}: Result {result.Result}");
        }

        private void JenkinsJobRunner_StatusChanged()
        {
            jobInfos.Items.Add($"{DateTime.Now.ToLongTimeString()}: {jenkinsJobRunner.Status}");
        }

        internal class ParameterFields
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string DefaultValue { get; set; }
            public string FieldType { get; set; }
        }

        private void BtnAddParam_Click(object sender, RoutedEventArgs e)
        {
            if (ParamValue.Text == "") MessageBox.Show("Value darf nicht leer sein");
        }
    }
}